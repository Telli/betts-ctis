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
using BettsTax.Web.Middleware;

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

// Register jwt generator
builder.Services.AddScoped<BettsTax.Web.Services.JwtTokenGenerator>();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

// Health checks
builder.Services.AddHealthChecks();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("SierraLeonePolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
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

// HTTP context accessor for user context service
builder.Services.AddHttpContextAccessor();

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

// Client enrollment services
builder.Services.AddScoped<BettsTax.Core.Services.IClientEnrollmentService, BettsTax.Core.Services.ClientEnrollmentService>();
builder.Services.AddScoped<BettsTax.Core.Services.IEmailService, BettsTax.Core.Services.EmailService>();
builder.Services.AddScoped<BettsTax.Core.Services.EmailTemplateService>();
builder.Services.AddScoped<BettsTax.Core.Services.ISecureTokenGenerator, BettsTax.Core.Services.SecureTokenGenerator>();

// System settings services
builder.Services.AddScoped<BettsTax.Core.Services.ISystemSettingService, BettsTax.Core.Services.SystemSettingService>();

// Document verification services
builder.Services.AddScoped<BettsTax.Core.Services.IDocumentVerificationService, BettsTax.Core.Services.DocumentVerificationService>();

// Activity timeline services
builder.Services.AddScoped<BettsTax.Core.Services.IActivityTimelineService, BettsTax.Core.Services.ActivityTimelineService>();
builder.Services.AddScoped<BettsTax.Core.Services.ActivityLogger>();

// Message services
builder.Services.AddScoped<BettsTax.Core.Services.IMessageService, BettsTax.Core.Services.MessageService>();

// SMS services
builder.Services.AddScoped<BettsTax.Core.Services.ISmsService, BettsTax.Core.Services.SmsService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.OrangeSLSmsProvider>();
builder.Services.AddHostedService<BettsTax.Web.Services.SmsBackgroundService>();

// Payment integration services
builder.Services.AddScoped<BettsTax.Core.Services.IPaymentIntegrationService, BettsTax.Core.Services.PaymentIntegrationService>();
builder.Services.AddHttpClient<BettsTax.Core.Services.OrangeMoneyProvider>();
builder.Services.AddHttpClient<BettsTax.Core.Services.AfricellMoneyProvider>();

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

builder.Services.AddScoped<BettsTax.Web.Filters.AuditActionFilter>();

// User context and authorization services
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IAuthorizationHandler, ClientDataAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ClientPortalAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminOrAssociateAuthorizationHandler>();

// Associate Permission Authorization Handler
builder.Services.AddScoped<IAuthorizationHandler, BettsTax.Web.Authorization.AssociatePermissionHandler>();

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

// Seed roles & admin (demo data temporarily disabled due to FK constraints)
using (var scope = app.Services.CreateScope())
{
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
    // TODO: Fix FK constraints in demo data seeder
    // await BettsTax.Data.DbSeeder.SeedDemoDataAsync(scope.ServiceProvider);
}

app.Run();


