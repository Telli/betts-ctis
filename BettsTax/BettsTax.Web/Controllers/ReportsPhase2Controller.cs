using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/reports/phase2")]
[Authorize]
public class ReportsPhase2Controller : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsPhase2Controller> _logger;

    public ReportsPhase2Controller(
        IReportService reportService,
        ILogger<ReportsPhase2Controller> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate tax filing summary report for client (Phase 2)
    /// </summary>
    [HttpPost("client/{clientId}/tax-filing-summary")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
    public async Task<IActionResult> GenerateTaxFilingSummaryReport(int clientId, [FromBody] ReportPeriodRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found");

            var reportRequest = new CreateReportRequestDto
            {
                Type = ReportType.TaxFiling,
                Format = (ReportFormat)Enum.Parse(typeof(ReportFormat), request.ExportFormat ?? "PDF"),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId },
                    { "PeriodStart", request.PeriodStart },
                    { "PeriodEnd", request.PeriodEnd },
                    { "IncludePendingFilings", request.IncludePendingFilings },
                    { "GroupByTaxType", request.GroupByTaxType },
                    { "ExportFormat", request.ExportFormat ?? "PDF" },
                    { "Title", $"Tax Filing Summary - {request.PeriodStart:yyyy-MM-dd} to {request.PeriodEnd:yyyy-MM-dd}" }
                }
            };

            var requestId = await _reportService.QueueReportGenerationAsync(reportRequest, userId);
            _logger.LogInformation("Tax filing summary report queued for client {ClientId}", clientId);

            return Ok(new { RequestId = requestId, Message = "Tax filing summary report generation queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax filing summary report for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate payment history report for client (Phase 2)
    /// </summary>
    [HttpPost("client/{clientId}/payment-history")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
    public async Task<IActionResult> GeneratePaymentHistoryReport(int clientId, [FromBody] ReportPeriodRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found");

            var reportRequest = new CreateReportRequestDto
            {
                Type = ReportType.PaymentHistory,
                Format = (ReportFormat)Enum.Parse(typeof(ReportFormat), request.ExportFormat ?? "PDF"),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId },
                    { "PeriodStart", request.PeriodStart },
                    { "PeriodEnd", request.PeriodEnd },
                    { "IncludeReconciliation", true },
                    { "IncludeEvidence", request.IncludeEvidence },
                    { "GroupByMethod", request.GroupByPaymentMethod },
                    { "ExportFormat", request.ExportFormat ?? "PDF" },
                    { "Title", $"Payment History - {request.PeriodStart:yyyy-MM-dd} to {request.PeriodEnd:yyyy-MM-dd}" }
                }
            };

            var requestId = await _reportService.QueueReportGenerationAsync(reportRequest, userId);
            _logger.LogInformation("Payment history report queued for client {ClientId}", clientId);

            return Ok(new { RequestId = requestId, Message = "Payment history report generation queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment history report for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate compliance scorecard report for client (Phase 2)
    /// </summary>
    [HttpPost("client/{clientId}/compliance-scorecard")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
    public async Task<IActionResult> GenerateComplianceScorecardReport(int clientId, [FromBody] ComplianceScorecardRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found");

            var reportRequest = new CreateReportRequestDto
            {
                Type = ReportType.Compliance,
                Format = (ReportFormat)Enum.Parse(typeof(ReportFormat), request.ExportFormat ?? "PDF"),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId },
                    { "IncludeHistory", request.IncludeHistory },
                    { "HistoryMonths", request.HistoryMonths },
                    { "IncludeDeadlines", request.IncludeUpcomingDeadlines },
                    { "IncludePenaltyWarnings", request.IncludePenaltyWarnings },
                    { "ExportFormat", request.ExportFormat ?? "PDF" },
                    { "Title", $"Compliance Scorecard - {DateTime.Now:yyyy-MM-dd}" }
                }
            };

            var requestId = await _reportService.QueueReportGenerationAsync(reportRequest, userId);
            _logger.LogInformation("Compliance scorecard report queued for client {ClientId}", clientId);

            return Ok(new { RequestId = requestId, Message = "Compliance scorecard report generation queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance scorecard report for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate document submission report for client (Phase 2)
    /// </summary>
    [HttpPost("client/{clientId}/document-submission")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
    public async Task<IActionResult> GenerateDocumentSubmissionReport(int clientId, [FromBody] ReportPeriodRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found");

            var reportRequest = new CreateReportRequestDto
            {
                Type = ReportType.DocumentSubmission,
                Format = (ReportFormat)Enum.Parse(typeof(ReportFormat), request.ExportFormat ?? "PDF"),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId },
                    { "PeriodStart", request.PeriodStart },
                    { "PeriodEnd", request.PeriodEnd },
                    { "IncludePending", request.IncludePendingDocuments },
                    { "IncludeRejected", request.IncludeRejectedDocuments },
                    { "GroupByCategory", true },
                    { "ExportFormat", request.ExportFormat ?? "PDF" },
                    { "Title", $"Document Submission Report - {request.PeriodStart:yyyy-MM-dd} to {request.PeriodEnd:yyyy-MM-dd}" }
                }
            };

            var requestId = await _reportService.QueueReportGenerationAsync(reportRequest, userId);
            _logger.LogInformation("Document submission report queued for client {ClientId}", clientId);

            return Ok(new { RequestId = requestId, Message = "Document submission report generation queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document submission report for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate comprehensive client overview report (Phase 2)
    /// </summary>
    [HttpPost("client/{clientId}/overview")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
    public async Task<IActionResult> GenerateClientOverviewReport(int clientId, [FromBody] ClientOverviewReportRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found");

            var reportRequest = new CreateReportRequestDto
            {
                Type = ReportType.ClientActivity,
                Format = (ReportFormat)Enum.Parse(typeof(ReportFormat), request.ExportFormat ?? "PDF"),
                Parameters = new Dictionary<string, object>
                {
                    { "ClientId", clientId },
                    { "IncludeFilings", request.IncludeFilings },
                    { "IncludePayments", request.IncludePayments },
                    { "IncludeDocuments", request.IncludeDocuments },
                    { "IncludeCompliance", request.IncludeCompliance },
                    { "PeriodMonths", request.PeriodMonths },
                    { "ExportFormat", request.ExportFormat ?? "PDF" },
                    { "Title", $"Client Overview Report - {DateTime.Now:yyyy-MM-dd}" }
                }
            };

            var requestId = await _reportService.QueueReportGenerationAsync(reportRequest, userId);
            _logger.LogInformation("Client overview report queued for client {ClientId}", clientId);

            return Ok(new { RequestId = requestId, Message = "Client overview report generation queued" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client overview report for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Get available report templates for Phase 2 reports
    /// </summary>
    [HttpGet("templates")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public IActionResult GetPhase2ReportTemplates()
    {
        try
        {
            var templates = new[]
            {
                new { 
                    Id = "tax-filing-summary",
                    Name = "Tax Filing Summary",
                    Description = "Summary of all tax filings for a period",
                    Category = "Client Reports",
                    Parameters = new[] { "PeriodStart", "PeriodEnd", "ClientId", "TaxType" },
                    SupportedFormats = new[] { "PDF", "Excel" }
                },
                new { 
                    Id = "payment-history",
                    Name = "Payment History",
                    Description = "Detailed payment history with reconciliation status",
                    Category = "Client Reports",
                    Parameters = new[] { "PeriodStart", "PeriodEnd", "ClientId", "PaymentMethod" },
                    SupportedFormats = new[] { "PDF", "Excel", "CSV" }
                },
                new { 
                    Id = "compliance-scorecard",
                    Name = "Compliance Scorecard",
                    Description = "Visual compliance scoring and metrics",
                    Category = "Client Reports",
                    Parameters = new[] { "ClientId", "HistoryMonths" },
                    SupportedFormats = new[] { "PDF" }
                },
                new { 
                    Id = "document-submission",
                    Name = "Document Submission Report",
                    Description = "Status of document submissions by category",
                    Category = "Client Reports",
                    Parameters = new[] { "PeriodStart", "PeriodEnd", "ClientId" },
                    SupportedFormats = new[] { "PDF", "Excel" }
                },
                new { 
                    Id = "client-overview",
                    Name = "Client Overview Report",
                    Description = "Comprehensive client summary report",
                    Category = "Client Reports",
                    Parameters = new[] { "ClientId", "PeriodMonths" },
                    SupportedFormats = new[] { "PDF" }
                }
            };

            return Ok(new { Templates = templates, Generated = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Phase 2 report templates");
            return StatusCode(500, "An error occurred while getting report templates");
        }
    }
}

// Phase 2 DTOs (new ones not already defined)
public class ReportPeriodRequestDto
{
    public DateTime PeriodStart { get; set; } = DateTime.Now.AddMonths(-12);
    public DateTime PeriodEnd { get; set; } = DateTime.Now;
    public string? ExportFormat { get; set; } = "PDF";
    public bool IncludePendingFilings { get; set; } = true;
    public bool GroupByTaxType { get; set; } = true;
    public bool IncludeEvidence { get; set; } = true;
    public bool GroupByPaymentMethod { get; set; } = true;
    public bool IncludePendingDocuments { get; set; } = true;
    public bool IncludeRejectedDocuments { get; set; } = true;
}

public class ClientOverviewReportRequestDto
{
    public string? ExportFormat { get; set; } = "PDF";
    public bool IncludeFilings { get; set; } = true;
    public bool IncludePayments { get; set; } = true;
    public bool IncludeDocuments { get; set; } = true;
    public bool IncludeCompliance { get; set; } = true;
    public int PeriodMonths { get; set; } = 12;
}

public class ComplianceScorecardRequestDto
{
    public string? ExportFormat { get; set; } = "PDF";
    public bool IncludeHistory { get; set; } = true;
    public int HistoryMonths { get; set; } = 12;
    public bool IncludeUpcomingDeadlines { get; set; } = true;
    public bool IncludePenaltyWarnings { get; set; } = true;
}

// Note: ComplianceReportRequestDto is already defined in ReportsController.cs