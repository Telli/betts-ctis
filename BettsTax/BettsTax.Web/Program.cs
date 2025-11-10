using BettsTax.Web.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using FluentValidation;
using Serilog;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;
using BettsTax.Web.Authorization;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.Jobs;
using BettsTax.Web.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Quartz;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Serilog configuration
// ---------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

// ---------------------------
// Add services
// ---------------------------

// SQLite connection for development
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Jwt
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = System.Text.Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

// SAML Authentication Configuration - TODO: Configure SAML properly
// builder.Services.Configure<BettsTax.Web.Options.SamlOptions>(
//     builder.Configuration.GetSection(BettsTax.Web.Options.SamlOptions.SectionName));

// builder.Services.AddAuthentication()
//     .AddSaml2(options =>
//     {
//         var samlConfig = builder.Configuration.GetSection(BettsTax.Web.Options.SamlOptions.SectionName);
//         // TODO: Configure SAML options properly
//     });

// Register jwt generator
builder.Services.AddScoped<BettsTax.Web.Services.JwtTokenGenerator>();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// Health checks
builder.Services.AddHealthChecks();

// OpenTelemetry (traces + metrics)
var paymentActivitySource = new ActivitySource("BettsTax.Payments");
builder.Services.AddSingleton(paymentActivitySource);

// Custom meter for advanced instruments
var paymentMeter = new Meter("BettsTax.Payments", "1.1.0");
builder.Services.AddSingleton(paymentMeter);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("BettsTax.Web", serviceVersion: "1.1.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
        .AddSource("BettsTax.Payments")
    .AddConsoleExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("BettsTax.Payments")
    .AddConsoleExporter());

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("SierraLeonePolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                      "http://localhost:3000",
                      "https://localhost:3000",
                      "http://localhost:3001",
                      "https://localhost:3001",
                      "http://localhost:4000",
                      "https://localhost:4000"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Production domains - configure as needed
            policy.WithOrigins(
                "https://ctis.bettsfirm.sl",
                "https://www.ctis.bettsfirm.sl"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        }
    });
});

// Security headers
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Rate limiting support
builder.Services.AddMemoryCache();

// Distributed cache for KPI system (using in-memory for development)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache();
}
else
{
    // In production, use Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
    });
}

// HTTP context accessor for user context service
builder.Services.AddHttpContextAccessor();

// SignalR for real-time chat
builder.Services.AddSignalR();

// Application services
builder.Services.AddScoped<BettsTax.Core.Services.IClientService, BettsTax.Core.Services.ClientService>();
builder.Services.AddScoped<BettsTax.Core.Services.ITaxYearService, BettsTax.Core.Services.TaxYearService>();
builder.Services.AddScoped<BettsTax.Core.Services.ITaxFilingService, BettsTax.Core.Services.TaxFilingService>();
builder.Services.AddScoped<BettsTax.Core.Services.IPaymentService, BettsTax.Core.Services.PaymentService>();
builder.Services.AddScoped<BettsTax.Core.Services.IDocumentService, BettsTax.Core.Services.DocumentService>();
builder.Services.AddScoped<BettsTax.Core.Services.IFileStorageService, BettsTax.Core.Services.FileStorageService>();
builder.Services.AddScoped<BettsTax.Core.Services.IAuditService, BettsTax.Core.Services.AuditService>();
builder.Services.AddScoped<BettsTax.Core.Services.INotificationService, BettsTax.Core.Services.NotificationService>();
builder.Services.AddScoped<BettsTax.Core.Services.IDashboardService, BettsTax.Core.Services.DashboardService>();
builder.Services.AddScoped<BettsTax.Core.Services.IAdminClientService, BettsTax.Core.Services.AdminClientService>();
builder.Services.AddScoped<BettsTax.Core.Services.ISierraLeoneTaxCalculationService, BettsTax.Core.Services.SierraLeoneTaxCalculationService>();
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.ITaxCalculationEngineService, BettsTax.Core.Services.TaxCalculationEngineService>();
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IComplianceAlertService, BettsTax.Core.Services.ComplianceAlertService>();
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IDeadlineMonitoringService, BettsTax.Core.Services.DeadlineMonitoringService>();

// Client enrollment services
builder.Services.AddScoped<BettsTax.Core.Services.IClientEnrollmentService, BettsTax.Core.Services.ClientEnrollmentService>();
builder.Services.AddScoped<BettsTax.Core.Services.IEmailService, BettsTax.Core.Services.EmailService>();
builder.Services.AddScoped<BettsTax.Core.Services.EmailTemplateService>();
builder.Services.AddScoped<BettsTax.Core.Services.ISecureTokenGenerator, BettsTax.Core.Services.SecureTokenGenerator>();

// System settings services
builder.Services.AddScoped<BettsTax.Core.Services.ISystemSettingService, BettsTax.Core.Services.SystemSettingService>();

// Document verification services
builder.Services.AddScoped<BettsTax.Core.Services.IDocumentVerificationService, BettsTax.Core.Services.DocumentVerificationService>();

// Workflow automation services - FEATURE FLAG CONTROLLED
var enableWorkflow = builder.Configuration.GetValue<bool>("Features:EnableWorkflowAutomation");
if (enableWorkflow)
{
    builder.Services.AddScoped<BettsTax.Core.Services.IWorkflowRuleService, BettsTax.Core.Services.WorkflowRuleService>();
    builder.Services.AddScoped<BettsTax.Core.Services.IWorkflowTemplateService, BettsTax.Core.Services.WorkflowTemplateService>();
    builder.Services.AddScoped<BettsTax.Core.Services.IWorkflowExecutionService, BettsTax.Core.Services.WorkflowExecutionService>();
    builder.Services.AddScoped<IEnhancedWorkflowService, EnhancedWorkflowService>();
}
builder.Services.AddScoped<BettsTax.Core.Services.ISmsService, BettsTax.Core.Services.SmsService>(); // Placeholder for SMS service

// Activity timeline services
builder.Services.AddScoped<BettsTax.Core.Services.IActivityTimelineService, BettsTax.Core.Services.ActivityTimelineService>();
builder.Services.AddScoped<BettsTax.Core.Services.ActivityLogger>();

// Message services
builder.Services.AddScoped<BettsTax.Core.Services.IMessageService, BettsTax.Core.Services.MessageService>();

// SMS services
builder.Services.AddScoped<BettsTax.Core.Services.ISmsService, BettsTax.Core.Services.SmsService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.OrangeSLSmsProvider>();
builder.Services.AddHostedService<BettsTax.Web.Services.SmsBackgroundService>();

// Document retention options & background service
builder.Services.Configure<BettsTax.Core.Options.DocumentRetentionOptions>(
    builder.Configuration.GetSection("DocumentRetention"));
builder.Services.AddHostedService<BettsTax.Core.Services.DocumentRetentionBackgroundService>();

// Payment integration services
builder.Services.AddScoped<BettsTax.Core.Services.IPaymentIntegrationService, BettsTax.Core.Services.PaymentIntegrationService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.OrangeMoneyProvider>();
builder.Services.AddHttpClient<BettsTax.Core.Services.AfricellMoneyProvider>();
builder.Services.AddScoped<PaymentWebhookProcessor>();
// New payment gateway abstraction registrations
builder.Services.AddScoped<BettsTax.Core.Services.Payments.IPaymentGateway, BettsTax.Core.Services.Payments.LocalPaymentGateway>();
builder.Services.AddScoped<BettsTax.Core.Services.Payments.IPaymentGateway, BettsTax.Core.Services.Payments.OrangeMoneyGatewayAdapter>();
builder.Services.AddScoped<BettsTax.Core.Services.Payments.IPaymentGateway, BettsTax.Core.Services.Payments.AfricellMoneyGatewayAdapter>();
// Temporarily disable SaloneSwitchGateway due to DI issues
// builder.Services.AddHttpClient<BettsTax.Core.Services.Payments.SaloneSwitchGateway>();
// builder.Services.AddScoped<BettsTax.Core.Services.Payments.IPaymentGateway, BettsTax.Core.Services.Payments.SaloneSwitchGateway>();
builder.Services.AddScoped<BettsTax.Core.Services.Payments.IPaymentGatewayFactory, BettsTax.Core.Services.Payments.PaymentGatewayFactory>();
// builder.Services.AddHostedService<BettsTax.Core.Services.Payments.SaloneSwitchPollingService>();

// Diaspora payment services (PayPal & Stripe)
builder.Services.AddScoped<BettsTax.Core.Services.IDiasporaPaymentService, BettsTax.Core.Services.DiasporaPaymentService>();
builder.Services.AddScoped<BettsTax.Core.Services.ICurrencyExchangeService, BettsTax.Core.Services.CurrencyExchangeService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.PayPalProvider>();
builder.Services.AddHttpClient<BettsTax.Core.Services.StripeProvider>();
builder.Services.AddHttpClient<BettsTax.Core.Services.CurrencyExchangeService>();

// Compliance tracking services
builder.Services.AddScoped<BettsTax.Core.Services.IComplianceTrackerService, BettsTax.Core.Services.ComplianceTrackerService>();
builder.Services.AddScoped<BettsTax.Core.Services.IPenaltyCalculationService, BettsTax.Core.Services.PenaltyCalculationService>();

// Data export services
builder.Services.AddScoped<BettsTax.Core.Services.IDataExportService, BettsTax.Core.Services.DataExportService>();
builder.Services.AddScoped<BettsTax.Core.Services.IExportFormatService, BettsTax.Core.Services.ExportFormatService>();

// Associate Permission Services
builder.Services.AddScoped<BettsTax.Core.Services.IAssociatePermissionService, BettsTax.Core.Services.AssociatePermissionService>();
builder.Services.AddScoped<BettsTax.Core.Services.IOnBehalfActionService, BettsTax.Core.Services.OnBehalfActionService>();
builder.Services.AddScoped<BettsTax.Core.Services.IClientDelegationService, BettsTax.Core.Services.ClientDelegationService>();
builder.Services.AddScoped<BettsTax.Core.Services.IPermissionTemplateService, BettsTax.Core.Services.PermissionTemplateService>();

// KPI Services
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IKPIService, BettsTax.Core.Services.KPIService>();
// New KPI computation & caching service + background refresher
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IKpiComputationService, BettsTax.Core.Services.KpiComputationService>();
builder.Services.AddHostedService<BettsTax.Web.Services.KpiBackgroundService>();

// Compliance aggregation service (CTIS Enhancement)
builder.Services.AddScoped<IComplianceService, BettsTax.Core.Services.ComplianceService>();

// Reporting Services
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IReportService, BettsTax.Core.Services.ReportService>();
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IReportTemplateService, BettsTax.Core.Services.ReportTemplateService>();
builder.Services.AddScoped<BettsTax.Core.Services.Interfaces.IReportGenerator, BettsTax.Core.Services.SimpleReportGenerator>();

// Workflow Automation Services (NEW) - FEATURE FLAG CONTROLLED
if (enableWorkflow)
{
    // TODO: Implement WorkflowEngineService and WorkflowRuleBuilderService
    // builder.Services.AddScoped<BettsTax.Core.Services.IWorkflowEngineService, BettsTax.Core.Services.WorkflowEngineService>();
    // builder.Services.AddScoped<BettsTax.Core.Services.IWorkflowRuleBuilderService, BettsTax.Core.Services.WorkflowRuleBuilderService>();
}

// Advanced Analytics & Reporting Services (NEW)
builder.Services.AddScoped<BettsTax.Core.Services.Analytics.IAdvancedQueryBuilderService, BettsTax.Core.Services.Analytics.AdvancedQueryBuilderService>();
builder.Services.AddScoped<BettsTax.Core.Services.Analytics.IAdvancedAnalyticsService, BettsTax.Core.Services.Analytics.AdvancedAnalyticsService>();

// Accounting Integration Services (NEW)
builder.Services.AddScoped<BettsTax.Core.Services.IAccountingIntegrationFactory, BettsTax.Core.Services.AccountingIntegrationFactory>();
builder.Services.AddScoped<BettsTax.Core.Services.QuickBooksIntegrationService>();
builder.Services.AddScoped<BettsTax.Core.Services.XeroIntegrationService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.QuickBooksIntegrationService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.XeroIntegrationService>();

// Quartz.NET for background jobs (CTIS Enhancement)
builder.Services.AddQuartz(q =>
{
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });

    // KPI Snapshot Job - runs daily at 2 AM
    q.AddJob<BettsTax.Core.Jobs.KpiSnapshotJob>(opts => opts.WithIdentity("KpiSnapshotJob"));
    q.AddTrigger(opts => opts
        .ForJob("KpiSnapshotJob")
        .WithIdentity("KpiSnapshotTrigger")
        .WithCronSchedule("0 0 2 * * ?"));

    // Compliance History Job - runs daily at 3 AM
    q.AddJob<BettsTax.Core.Jobs.ComplianceHistoryJob>(opts => opts.WithIdentity("ComplianceHistoryJob"));
    q.AddTrigger(opts => opts
        .ForJob("ComplianceHistoryJob")
        .WithIdentity("ComplianceHistoryTrigger")
        .WithCronSchedule("0 0 3 * * ?"));

    // Payment Reconciliation Job - runs every 2 hours
    q.AddJob<BettsTax.Core.Jobs.PaymentReconciliationJob>(opts => opts.WithIdentity("PaymentReconciliationJob"));
    q.AddTrigger(opts => opts
        .ForJob("PaymentReconciliationJob")
        .WithIdentity("PaymentReconciliationTrigger")
        .WithCronSchedule("0 0 */2 * * ?"));

    // Payment Gateway Polling Job - runs every 2 minutes
    q.AddJob<BettsTax.Core.Jobs.PaymentGatewayPollingJob>(opts => opts.WithIdentity("PaymentGatewayPollingJob"));
    q.AddTrigger(opts => opts
        .ForJob("PaymentGatewayPollingJob")
        .WithIdentity("PaymentGatewayPollingTrigger")
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever()));

    // Report Cleanup Job - runs daily at 1 AM
    q.AddJob<BettsTax.Core.Jobs.ReportCleanupJob>(opts => opts.WithIdentity("ReportCleanupJob"));
    q.AddTrigger(opts => opts
        .ForJob("ReportCleanupJob")
        .WithIdentity("ReportCleanupTrigger")
        .WithCronSchedule("0 0 1 * * ?"));

    // Report Scheduling Job - runs every 15 minutes
    q.AddJob<BettsTax.Core.Jobs.ReportSchedulingJob>(opts => opts.WithIdentity("ReportSchedulingJob"));
    q.AddTrigger(opts => opts
        .ForJob("ReportSchedulingJob")
        .WithIdentity("ReportSchedulingTrigger")
        .WithCronSchedule("0 */15 * * * ?"));

    // Compliance Snapshot Job - runs daily at 4 AM
    q.AddJob<BettsTax.Core.Jobs.ComplianceSnapshotJob>(opts => opts.WithIdentity("ComplianceSnapshotJob"));
    q.AddTrigger(opts => opts
        .ForJob("ComplianceSnapshotJob")
        .WithIdentity("ComplianceSnapshotTrigger")
        .WithCronSchedule("0 0 4 * * ?"));

    if (enableWorkflow)
    {
        q.AddJob<WorkflowTriggerEvaluationJob>(opts => opts.WithIdentity("WorkflowTriggerEvaluationJob"));
        q.AddTrigger(opts => opts
            .ForJob("WorkflowTriggerEvaluationJob")
            .WithIdentity("WorkflowTriggerEvaluationTrigger")
            .WithCronSchedule("0 */5 * * * ?"));

        q.AddJob<WorkflowInstanceCleanupJob>(opts => opts.WithIdentity("WorkflowInstanceCleanupJob"));
        q.AddTrigger(opts => opts
            .ForJob("WorkflowInstanceCleanupJob")
            .WithIdentity("WorkflowInstanceCleanupTrigger")
            .WithCronSchedule("0 30 3 * * ?"));
    }
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Register Quartz IScheduler for services that need it
builder.Services.AddSingleton(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    return schedulerFactory.GetScheduler().Result;
});

builder.Services.AddScoped<BettsTax.Web.Filters.AuditActionFilter>();

// User context and authorization services
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IAuthorizationHandler, ClientDataAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ClientPortalAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminOrAssociateAuthorizationHandler>();

// Associate Permission Authorization Handler
builder.Services.AddScoped<IAuthorizationHandler, BettsTax.Web.Authorization.AssociatePermissionHandler>();

// Register core services
builder.Services.AddScoped<IKpiComputationService, KpiComputationService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IKpiAlertService, KpiAlertService>();
builder.Services.AddScoped<IKpiPerformanceService, KpiPerformanceService>();
builder.Services.AddScoped<IReportRateLimitService, ReportRateLimitService>();

// Workflow Automation Services - FEATURE FLAG CONTROLLED
if (enableWorkflow)
{
    // TODO: Implement WorkflowEngineService and WorkflowRuleBuilderService
    // builder.Services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();
    // builder.Services.AddScoped<IWorkflowRuleBuilderService, WorkflowRuleBuilderService>();
}

// FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(BettsTax.Core.Services.TaxFilingService).Assembly);

// File upload configuration
builder.Services.Configure<FormOptions>(o => { 
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    o.ValueLengthLimit = 1024 * 1024; // 1MB
    o.MultipartHeadersLengthLimit = 16384; // 16KB
});

// Controllers (Minimal APIs or MVC can be added later)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<BettsTax.Web.Filters.AuditActionFilter>();
})
.AddJsonOptions(options =>
{
    // Handle circular references in JSON serialization
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    // Keep property names as-is (don't change casing)
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ClientPortal", p => p.AddRequirements(new ClientPortalRequirement()));
    options.AddPolicy("AdminOrAssociate", p => p.AddRequirements(new AdminOrAssociateRequirement()));
    
    // Associate Permission Policies
    options.AddPolicy("TaxFilingRead", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Read)));
    options.AddPolicy("TaxFilingCreate", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Create)));
    options.AddPolicy("TaxFilingUpdate", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Update)));
    options.AddPolicy("TaxFilingDelete", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Delete)));
    options.AddPolicy("TaxFilingSubmit", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Submit)));
    
    options.AddPolicy("PaymentRead", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Read)));
    options.AddPolicy("PaymentCreate", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Create)));
    options.AddPolicy("PaymentApprove", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Approve)));
    
    options.AddPolicy("DocumentRead", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Read)));
    options.AddPolicy("DocumentCreate", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Create)));
    options.AddPolicy("DocumentDelete", policy =>
        policy.Requirements.Add(new BettsTax.Web.Authorization.AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Delete)));
});

// Tax Authority Integration Services
builder.Services.AddScoped<ITaxAuthorityService, TaxAuthorityService>();
builder.Services.AddHttpClient<ITaxAuthorityService, TaxAuthorityService>();
// builder.Services.AddHostedService<TaxAuthorityBackgroundService>(); // Temporarily disabled due to missing configuration

// SAML Authentication Service
builder.Services.AddScoped<ISamlAuthenticationService, SamlAuthenticationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------
// Middleware
// ---------------------------

// Enable Swagger in all environments for now
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Security middleware
app.UseHsts();
app.UseHttpsRedirection();

// Serve static files from wwwroot when Static Web Assets is disabled
app.UseStaticFiles();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self'; " +
        "connect-src 'self'");
    await next();
});

// CORS
app.UseCors("SierraLeonePolicy");

// Custom middleware
app.UseMiddleware<BettsTax.Web.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<BettsTax.Web.Middleware.SimpleRateLimitMiddleware>();
app.UseClientPortalAudit(); // Client portal audit logging

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// SignalR Hub mapping
app.MapHub<BettsTax.Web.Hubs.ChatHub>("/chatHub");
app.MapHub<BettsTax.Web.Hubs.NotificationsHub>("/hubs/notifications");
app.MapHub<BettsTax.Web.Hubs.PaymentsHub>("/hubs/payments");
// Prometheus scrape endpoint
// Console exporters active. (Prometheus/OTLP exporters can be re-enabled when correct packages & extensions are available.)

// Apply EF Core migrations and seed baseline data in Development/Test to support local runs and integration tests
using (var scope = app.Services.CreateScope())
{
    var env = app.Environment.EnvironmentName;
    if (!string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
    {
        var db = scope.ServiceProvider.GetRequiredService<BettsTax.Data.ApplicationDbContext>();
        
        // Log database creation attempt
        Log.Information("Attempting to create database schema...");
        
        // For development, use EnsureCreatedAsync to create the database schema
        // This avoids migration issues and ensures all tables are created
        var created = await db.Database.EnsureCreatedAsync();
        Log.Information("Database EnsureCreatedAsync completed. Created: {Created}", created);
        
        // Check if WorkflowTriggers table exists
        try
        {
            var tableExists = await db.WorkflowTriggers.AnyAsync();
            Log.Information("WorkflowTriggers table exists and has data: {Exists}", tableExists);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking WorkflowTriggers table");
        }
    }

    await BettsTax.Data.DbSeeder.SeedRolesAsync(scope.ServiceProvider);
    await BettsTax.Data.DbSeeder.SeedAdminUserAsync(scope.ServiceProvider, app.Configuration);
    await BettsTax.Data.DocumentRequirementSeeder.SeedDocumentRequirementsAsync(scope.ServiceProvider);
    await BettsTax.Data.MessageTemplateSeeder.SeedMessageTemplatesAsync(scope.ServiceProvider);
    await BettsTax.Data.SmsTemplateSeeder.SeedSmsTemplatesAsync(scope.ServiceProvider);
    await BettsTax.Data.SmsTemplateSeeder.SeedSmsProviderConfigsAsync(scope.ServiceProvider);
    await BettsTax.Data.SmsTemplateSeeder.SeedSmsSchedulesAsync(scope.ServiceProvider);
    await BettsTax.Data.PaymentIntegrationSeeder.SeedPaymentProvidersAsync(scope.ServiceProvider);
    await BettsTax.Data.PaymentIntegrationSeeder.SeedPaymentMethodsAsync(scope.ServiceProvider);
    await BettsTax.Data.PaymentIntegrationSeeder.SeedPaymentStatusMappingsAsync(scope.ServiceProvider);
    await BettsTax.Data.ComplianceSeeder.SeedPenaltyRulesAsync(scope.ServiceProvider);
    await BettsTax.Data.ComplianceSeeder.SeedComplianceInsightsAsync(scope.ServiceProvider);
    
    // Workflow seeding - FEATURE FLAG CONTROLLED
    if (enableWorkflow)
    {
        // Temporarily disabled - tables exist but seeding has FK constraint issues
        // TODO: Fix workflow seeding foreign key constraints
        /*
        // obtain a context instance for table detection
        var ctx = scope.ServiceProvider.GetRequiredService<BettsTax.Data.ApplicationDbContext>();
        var dbExists = true;
        var hasWorkflowRuleTemplates = false;
        var hasWorkflowCoreTables = false;
        try
        {
            // For SQLite, explicitly check if the expected table exists to avoid startup crashes
            if (ctx.Database.IsSqlite())
            {
                var conn = ctx.Database.GetDbConnection();
                await conn.OpenAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('WorkflowRuleTemplates','Workflows','WorkflowTriggers','WorkflowInstances')";
                    using var reader = await cmd.ExecuteReaderAsync();
                    var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }

                    hasWorkflowRuleTemplates = tables.Contains("WorkflowRuleTemplates");
                    hasWorkflowCoreTables = tables.Contains("Workflows") && tables.Contains("WorkflowTriggers") && tables.Contains("WorkflowInstances");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            else
            {
                // For other providers, optimistically attempt seeding; if it fails, we'll catch and log below
                hasWorkflowRuleTemplates = true;
                hasWorkflowCoreTables = true;
            }
        }
        catch
        {
            dbExists = false;
        }

        if (dbExists && hasWorkflowRuleTemplates)
        {
            await BettsTax.Data.WorkflowSeeder.SeedWorkflowTemplatesAsync(scope.ServiceProvider);
        }
        else
        {
            Log.Warning("Skipping Workflow workflow-template seeding: table 'WorkflowRuleTemplates' not found. Run a migration to create it if needed.");
        }

        if (dbExists && hasWorkflowCoreTables)
        {
            await BettsTax.Data.WorkflowSeeder.SeedEnhancedWorkflowAutomationAsync(scope.ServiceProvider);
        }
        else
        {
            Log.Warning("Skipping Workflow automation seeding: required workflow tables not found. Run migrations to enable enhanced workflow seeding.");
        }
        */
    }
    
    // Seed demo data for development/testing
    try
    {
        await BettsTax.Data.DbSeeder.SeedDemoDataAsync(scope.ServiceProvider);
        Log.Information("Demo data seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error seeding demo data. Skipping demo data seeding.");
    }
}

app.Run();


// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }


