using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BettsTax.Shared;
using BettsTax.Core.DTOs.Payments;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.DTOs.Documents;
using BettsTax.Core.DTOs.Communication;

namespace BettsTax.Web.Tests.Integration;

/// <summary>
/// Custom test fixture that provides isolated database for each test class.
/// This prevents database conflicts when tests run in parallel.
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>, IDisposable
{
    private static int _databaseCounter = 0;
    private readonly string _databaseName;
    private readonly string _databasePath;

    public IntegrationTestFixture()
    {
        // Create unique database name for this test class instance
        var counter = Interlocked.Increment(ref _databaseCounter);
        _databaseName = $"BettsTax_Test_{counter}_{Guid.NewGuid():N}.db";
        _databasePath = Path.Combine(AppContext.BaseDirectory, _databaseName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use isolated test database
            // Enable workflow automation for tests
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection",
                    $"Data Source={_databasePath}"),
                new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development"),
                new KeyValuePair<string, string?>("Features:EnableWorkflowAutomation", "true")
            });
        });

        builder.ConfigureServices(services =>
        {
            // Register stub implementations for workflow services not implemented yet
            services.AddScoped<BettsTax.Core.Services.Interfaces.IPaymentApprovalWorkflow, StubPaymentApprovalWorkflow>();
            services.AddScoped<BettsTax.Core.Services.Interfaces.IComplianceMonitoringWorkflow, StubComplianceMonitoringWorkflow>();
            services.AddScoped<BettsTax.Core.Services.Interfaces.IDocumentManagementWorkflow, StubDocumentManagementWorkflow>();
            services.AddScoped<BettsTax.Core.Services.Interfaces.ICommunicationRoutingWorkflow, StubCommunicationRoutingWorkflow>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure clean database before creating host
        DeleteDatabaseIfExists();
        return base.CreateHost(builder);
    }

    private void DeleteDatabaseIfExists()
    {
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        // Also delete journal files
        var journalPath = _databasePath + "-shm";
        var walPath = _databasePath + "-wal";
        
        if (File.Exists(journalPath))
        {
            try { File.Delete(journalPath); } catch { }
        }
        
        if (File.Exists(walPath))
        {
            try { File.Delete(walPath); } catch { }
        }
    }

    public new void Dispose()
    {
        // Clean up test database after all tests in this class complete
        DeleteDatabaseIfExists();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Stub implementations for workflow services not yet implemented
public class StubPaymentApprovalWorkflow : BettsTax.Core.Services.Interfaces.IPaymentApprovalWorkflow
{
    public Task<Result<PaymentApprovalRequestDto>> RequestPaymentApprovalAsync(int paymentId, decimal amount, string requestedBy)
        => Task.FromResult(Result<PaymentApprovalRequestDto>.Success(new PaymentApprovalRequestDto()));

    public Task<Result<List<PaymentApprovalRequestDto>>> GetPendingApprovalsAsync(string approverId)
        => Task.FromResult(Result<List<PaymentApprovalRequestDto>>.Success(new List<PaymentApprovalRequestDto>()));

    public Task<Result<List<PaymentApprovalRequestDto>>> GetAllPendingApprovalsAsync()
        => Task.FromResult(Result<List<PaymentApprovalRequestDto>>.Success(new List<PaymentApprovalRequestDto>()));

    public Task<Result<PaymentApprovalRequestDto>> GetApprovalRequestAsync(Guid approvalRequestId)
        => Task.FromResult(Result<PaymentApprovalRequestDto>.Success(new PaymentApprovalRequestDto()));

    public Task<Result<PaymentApprovalRequestDto>> ApprovePaymentAsync(Guid approvalRequestId, string approverId, string? comments = null)
        => Task.FromResult(Result<PaymentApprovalRequestDto>.Success(new PaymentApprovalRequestDto()));

    public Task<Result<PaymentApprovalRequestDto>> RejectPaymentAsync(Guid approvalRequestId, string approverId, string rejectionReason)
        => Task.FromResult(Result<PaymentApprovalRequestDto>.Success(new PaymentApprovalRequestDto()));

    public Task<Result<PaymentApprovalRequestDto>> DelegateApprovalAsync(Guid approvalRequestId, string currentApproverId, string delegateToUserId, string? reason = null)
        => Task.FromResult(Result<PaymentApprovalRequestDto>.Success(new PaymentApprovalRequestDto()));

    public Task<Result<List<PaymentApprovalThresholdDto>>> GetApprovalThresholdsAsync()
        => Task.FromResult(Result<List<PaymentApprovalThresholdDto>>.Success(new List<PaymentApprovalThresholdDto>()));

    public Task<Result<PaymentApprovalThresholdDto>> UpsertApprovalThresholdAsync(PaymentApprovalThresholdDto threshold, string userId)
        => Task.FromResult(Result<PaymentApprovalThresholdDto>.Success(new PaymentApprovalThresholdDto()));

    public Task<Result<List<string>>> GetApprovalChainAsync(decimal amount)
        => Task.FromResult(Result<List<string>>.Success(new List<string>()));

    public Task<Result<List<PaymentApprovalStepDto>>> GetApprovalHistoryAsync(Guid approvalRequestId)
        => Task.FromResult(Result<List<PaymentApprovalStepDto>>.Success(new List<PaymentApprovalStepDto>()));

    public Task<Result> CancelApprovalAsync(Guid approvalRequestId, string cancelledBy, string reason)
        => Task.FromResult(Result.Success());

    public Task<Result<PaymentApprovalStatisticsDto>> GetApprovalStatisticsAsync(DateTime? from = null, DateTime? to = null)
        => Task.FromResult(Result<PaymentApprovalStatisticsDto>.Success(new PaymentApprovalStatisticsDto()));
}

public class StubComplianceMonitoringWorkflow : BettsTax.Core.Services.Interfaces.IComplianceMonitoringWorkflow
{
    public Task<Result> MonitorDeadlinesAsync()
        => Task.FromResult(Result.Success());

    public Task<Result> UpdateComplianceStatusAsync(Guid complianceMonitoringId, string status)
        => Task.FromResult(Result.Success());

    public Task<Result<ComplianceMonitoringAlertDto>> GenerateComplianceAlertAsync(Guid complianceMonitoringId, string alertType)
        => Task.FromResult(Result<ComplianceMonitoringAlertDto>.Success(new ComplianceMonitoringAlertDto()));

    public Task<Result<decimal>> CalculatePenaltyAsync(Guid complianceMonitoringId, int daysOverdue)
        => Task.FromResult(Result<decimal>.Success(0m));

    public Task<Result<List<ComplianceMonitoringDto>>> GetClientComplianceAsync(int clientId)
        => Task.FromResult(Result<List<ComplianceMonitoringDto>>.Success(new List<ComplianceMonitoringDto>()));

    public Task<Result<List<ComplianceMonitoringDto>>> GetTaxYearComplianceAsync(int taxYearId)
        => Task.FromResult(Result<List<ComplianceMonitoringDto>>.Success(new List<ComplianceMonitoringDto>()));

    public Task<Result<List<ComplianceMonitoringDto>>> GetPendingComplianceAsync()
        => Task.FromResult(Result<List<ComplianceMonitoringDto>>.Success(new List<ComplianceMonitoringDto>()));

    public Task<Result<List<ComplianceMonitoringDto>>> GetOverdueComplianceAsync()
        => Task.FromResult(Result<List<ComplianceMonitoringDto>>.Success(new List<ComplianceMonitoringDto>()));

    public Task<Result<ComplianceMonitoringDto>> CreateComplianceMonitoringAsync(int clientId, int taxYearId, string taxType, DateTime dueDate, decimal amount)
        => Task.FromResult(Result<ComplianceMonitoringDto>.Success(new ComplianceMonitoringDto()));

    public Task<Result> MarkAsFiledAsync(Guid complianceMonitoringId, DateTime filedDate)
        => Task.FromResult(Result.Success());

    public Task<Result> MarkAsPaidAsync(Guid complianceMonitoringId, DateTime paidDate)
        => Task.FromResult(Result.Success());

    public Task<Result<ComplianceStatisticsDto>> GetComplianceStatisticsAsync(int? clientId = null, DateTime? from = null, DateTime? to = null)
        => Task.FromResult(Result<ComplianceStatisticsDto>.Success(new ComplianceStatisticsDto()));

    public Task<Result<List<ComplianceMonitoringAlertDto>>> GetAlertsAsync(Guid complianceMonitoringId)
        => Task.FromResult(Result<List<ComplianceMonitoringAlertDto>>.Success(new List<ComplianceMonitoringAlertDto>()));

    public Task<Result<List<CompliancePenaltyCalculationDto>>> GetPenaltyCalculationsAsync(Guid complianceMonitoringId)
        => Task.FromResult(Result<List<CompliancePenaltyCalculationDto>>.Success(new List<CompliancePenaltyCalculationDto>()));
}

public class StubDocumentManagementWorkflow : BettsTax.Core.Services.Interfaces.IDocumentManagementWorkflow
{
    public Task<Result<DocumentSubmissionDto>> SubmitDocumentAsync(int documentId, int clientId, string documentType, string submittedBy)
        => Task.FromResult(Result<DocumentSubmissionDto>.Success(new DocumentSubmissionDto()));

    public Task<Result<DocumentSubmissionDto>> VerifyDocumentAsync(Guid submissionId, string verifiedBy, string? notes = null)
        => Task.FromResult(Result<DocumentSubmissionDto>.Success(new DocumentSubmissionDto()));

    public Task<Result<DocumentSubmissionDto>> ApproveDocumentAsync(Guid submissionId, string approvedBy, string? comments = null)
        => Task.FromResult(Result<DocumentSubmissionDto>.Success(new DocumentSubmissionDto()));

    public Task<Result<DocumentSubmissionDto>> RejectDocumentAsync(Guid submissionId, string rejectedBy, string rejectionReason)
        => Task.FromResult(Result<DocumentSubmissionDto>.Success(new DocumentSubmissionDto()));

    public Task<Result<DocumentSubmissionDto>> GetSubmissionStatusAsync(Guid submissionId)
        => Task.FromResult(Result<DocumentSubmissionDto>.Success(new DocumentSubmissionDto()));

    public Task<Result<List<DocumentSubmissionDto>>> GetClientSubmissionsAsync(int clientId)
        => Task.FromResult(Result<List<DocumentSubmissionDto>>.Success(new List<DocumentSubmissionDto>()));

    public Task<Result<List<DocumentSubmissionDto>>> GetPendingVerificationsAsync()
        => Task.FromResult(Result<List<DocumentSubmissionDto>>.Success(new List<DocumentSubmissionDto>()));

    public Task<Result<List<DocumentSubmissionDto>>> GetPendingApprovalsAsync()
        => Task.FromResult(Result<List<DocumentSubmissionDto>>.Success(new List<DocumentSubmissionDto>()));

    public Task<Result<DocumentVerificationResultDto>> AddVerificationResultAsync(Guid submissionId, string verificationType, string status, string? findings = null)
        => Task.FromResult(Result<DocumentVerificationResultDto>.Success(new DocumentVerificationResultDto()));

    public Task<Result<List<DocumentVerificationResultDto>>> GetVerificationResultsAsync(Guid submissionId)
        => Task.FromResult(Result<List<DocumentVerificationResultDto>>.Success(new List<DocumentVerificationResultDto>()));

    public Task<Result<DocumentVersionControlDto>> CreateDocumentVersionAsync(int documentId, string fileName, long fileSize, string fileHash, string uploadedBy, string? changeDescription = null)
        => Task.FromResult(Result<DocumentVersionControlDto>.Success(new DocumentVersionControlDto()));

    public Task<Result<List<DocumentVersionControlDto>>> GetDocumentVersionHistoryAsync(int documentId)
        => Task.FromResult(Result<List<DocumentVersionControlDto>>.Success(new List<DocumentVersionControlDto>()));

    public Task<Result<DocumentVersionControlDto>> GetActiveDocumentVersionAsync(int documentId)
        => Task.FromResult(Result<DocumentVersionControlDto>.Success(new DocumentVersionControlDto()));

    public Task<Result<DocumentSubmissionStatisticsDto>> GetSubmissionStatisticsAsync(int? clientId = null, DateTime? from = null, DateTime? to = null)
        => Task.FromResult(Result<DocumentSubmissionStatisticsDto>.Success(new DocumentSubmissionStatisticsDto()));

    public Task<Result<List<DocumentSubmissionStepDto>>> GetSubmissionStepsAsync(Guid submissionId)
        => Task.FromResult(Result<List<DocumentSubmissionStepDto>>.Success(new List<DocumentSubmissionStepDto>()));
}

public class StubCommunicationRoutingWorkflow : BettsTax.Core.Services.Interfaces.ICommunicationRoutingWorkflow
{
    public Task<Result<CommunicationRoutingDto>> ReceiveAndRouteMessageAsync(int clientId, string messageType, string subject, string content, string priority, string channel, string sentBy)
        => Task.FromResult(Result<CommunicationRoutingDto>.Success(new CommunicationRoutingDto()));

    public Task<Result<CommunicationRoutingDto>> AssignMessageAsync(Guid routingId, string assignToUserId, string? notes = null)
        => Task.FromResult(Result<CommunicationRoutingDto>.Success(new CommunicationRoutingDto()));

    public Task<Result<CommunicationRoutingDto>> EscalateMessageAsync(Guid routingId, string escalatedBy, string? reason = null)
        => Task.FromResult(Result<CommunicationRoutingDto>.Success(new CommunicationRoutingDto()));

    public Task<Result<CommunicationRoutingDto>> ResolveMessageAsync(Guid routingId, string resolvedBy, string resolutionNotes)
        => Task.FromResult(Result<CommunicationRoutingDto>.Success(new CommunicationRoutingDto()));

    public Task<Result<CommunicationRoutingDto>> GetRoutingDetailsAsync(Guid routingId)
        => Task.FromResult(Result<CommunicationRoutingDto>.Success(new CommunicationRoutingDto()));

    public Task<Result<List<CommunicationRoutingDto>>> GetPendingMessagesAsync(string userId)
        => Task.FromResult(Result<List<CommunicationRoutingDto>>.Success(new List<CommunicationRoutingDto>()));

    public Task<Result<List<CommunicationRoutingDto>>> GetAllPendingMessagesAsync()
        => Task.FromResult(Result<List<CommunicationRoutingDto>>.Success(new List<CommunicationRoutingDto>()));

    public Task<Result<List<CommunicationRoutingDto>>> GetClientMessagesAsync(int clientId)
        => Task.FromResult(Result<List<CommunicationRoutingDto>>.Success(new List<CommunicationRoutingDto>()));

    public Task<Result<List<CommunicationRoutingDto>>> GetEscalatedMessagesAsync()
        => Task.FromResult(Result<List<CommunicationRoutingDto>>.Success(new List<CommunicationRoutingDto>()));

    public Task<Result<CommunicationRoutingRuleDto>> CreateRoutingRuleAsync(string ruleName, string messageType, string priority, string assignToRole, int escalationThresholdMinutes, string? escalateToRole = null)
        => Task.FromResult(Result<CommunicationRoutingRuleDto>.Success(new CommunicationRoutingRuleDto()));

    public Task<Result<List<CommunicationRoutingRuleDto>>> GetRoutingRulesAsync()
        => Task.FromResult(Result<List<CommunicationRoutingRuleDto>>.Success(new List<CommunicationRoutingRuleDto>()));

    public Task<Result<CommunicationEscalationRuleDto>> CreateEscalationRuleAsync(string ruleName, int escalationLevel, string escalateToRole, int timeThresholdMinutes)
        => Task.FromResult(Result<CommunicationEscalationRuleDto>.Success(new CommunicationEscalationRuleDto()));

    public Task<Result<List<CommunicationEscalationRuleDto>>> GetEscalationRulesAsync()
        => Task.FromResult(Result<List<CommunicationEscalationRuleDto>>.Success(new List<CommunicationEscalationRuleDto>()));

    public Task<Result<CommunicationStatisticsDto>> GetCommunicationStatisticsAsync(int? clientId = null, DateTime? from = null, DateTime? to = null)
        => Task.FromResult(Result<CommunicationStatisticsDto>.Success(new CommunicationStatisticsDto()));

    public Task<Result<List<CommunicationRoutingStepDto>>> GetRoutingHistoryAsync(Guid routingId)
        => Task.FromResult(Result<List<CommunicationRoutingStepDto>>.Success(new List<CommunicationRoutingStepDto>()));

    public Task<Result> CheckAndApplyEscalationRulesAsync()
        => Task.FromResult(Result.Success());
}

