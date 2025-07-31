using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IUserContextService userContext,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Queue a report for background generation
    /// </summary>
    [HttpPost("queue")]
    public async Task<IActionResult> QueueReport([FromBody] CreateReportRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            // Validate request parameters based on report type
            var validationResult = ValidateReportRequest(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ErrorMessage);

            var requestId = await _reportService.QueueReportGenerationAsync(request, userId);
            
            _logger.LogInformation("Report queued successfully with ID {RequestId} for user {UserId}", requestId, userId);

            return Ok(new { RequestId = requestId, Message = "Report generation queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing report generation");
            return StatusCode(500, "An error occurred while queueing the report");
        }
    }

    /// <summary>
    /// Get the status of a queued report
    /// </summary>
    [HttpGet("status/{requestId}")]
    public async Task<IActionResult> GetReportStatus(string requestId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            var report = await _reportService.GetReportStatusAsync(requestId);
            if (report == null)
                return NotFound("Report not found");

            // Only allow users to access their own reports (unless admin)
            if (report.RequestedByUserId != userId && !User.IsInRole("Admin"))
                return Forbid("Access denied");

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report status for {RequestId}", requestId);
            return StatusCode(500, "An error occurred while retrieving the report status");
        }
    }

    /// <summary>
    /// Get user's report history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetReportHistory([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            if (pageSize > 100) pageSize = 100; // Limit page size
            if (pageNumber < 1) pageNumber = 1;

            var reports = await _reportService.GetUserReportsAsync(userId, pageSize, pageNumber);
            
            return Ok(new 
            { 
                Reports = reports,
                PageSize = pageSize,
                PageNumber = pageNumber,
                TotalCount = reports.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report history for user");
            return StatusCode(500, "An error occurred while retrieving report history");
        }
    }

    /// <summary>
    /// Download a completed report
    /// </summary>
    [HttpGet("download/{requestId}")]
    public async Task<IActionResult> DownloadReport(string requestId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            var reportFile = await _reportService.GetReportFileAsync(requestId, userId);
            if (reportFile == null)
                return NotFound("Report file not found or has expired");

            var report = await _reportService.GetReportStatusAsync(requestId);
            if (report == null)
                return NotFound("Report not found");

            var contentType = GetContentType(report.Format);
            var fileName = GetFileName(report);

            _logger.LogInformation("Report {RequestId} downloaded by user {UserId}", requestId, userId);

            return File(reportFile, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report {RequestId}", requestId);
            return StatusCode(500, "An error occurred while downloading the report");
        }
    }

    /// <summary>
    /// Delete a report
    /// </summary>
    [HttpDelete("{requestId}")]
    public async Task<IActionResult> DeleteReport(string requestId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            var success = await _reportService.DeleteReportAsync(requestId, userId);
            if (!success)
                return NotFound("Report not found");

            _logger.LogInformation("Report {RequestId} deleted by user {UserId}", requestId, userId);

            return Ok(new { Message = "Report deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {RequestId}", requestId);
            return StatusCode(500, "An error occurred while deleting the report");
        }
    }

    /// <summary>
    /// Generate and download a small report immediately (synchronous)
    /// </summary>
    [HttpPost("generate/tax-filing")]
    public async Task<IActionResult> GenerateTaxFilingReport([FromBody] TaxFilingReportRequestDto request)
    {
        try
        {
            // Validate user has access to this client
            var hasAccess = await ValidateClientAccess(request.ClientId);
            if (!hasAccess)
                return Forbid("Access denied to client data");

            var reportData = await _reportService.GenerateTaxFilingReportAsync(
                request.ClientId, 
                request.TaxYear, 
                request.Format);

            var contentType = GetContentType(request.Format);
            var fileName = $"TaxFiling_{request.ClientId}_{request.TaxYear}.{GetFileExtension(request.Format)}";

            _logger.LogInformation("Tax filing report generated for client {ClientId}, year {TaxYear}", 
                request.ClientId, request.TaxYear);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax filing report for client {ClientId}", request.ClientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate and download payment history report immediately (synchronous)
    /// </summary>
    [HttpPost("generate/payment-history")]
    public async Task<IActionResult> GeneratePaymentHistoryReport([FromBody] PaymentHistoryReportRequestDto request)
    {
        try
        {
            var hasAccess = await ValidateClientAccess(request.ClientId);
            if (!hasAccess)
                return Forbid("Access denied to client data");

            var reportData = await _reportService.GeneratePaymentHistoryReportAsync(
                request.ClientId, 
                request.FromDate, 
                request.ToDate, 
                request.Format);

            var contentType = GetContentType(request.Format);
            var fileName = $"PaymentHistory_{request.ClientId}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.{GetFileExtension(request.Format)}";

            _logger.LogInformation("Payment history report generated for client {ClientId}", request.ClientId);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment history report for client {ClientId}", request.ClientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate and download compliance report immediately (synchronous)
    /// </summary>
    [HttpPost("generate/compliance")]
    public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequestDto request)
    {
        try
        {
            var hasAccess = await ValidateClientAccess(request.ClientId);
            if (!hasAccess)
                return Forbid("Access denied to client data");

            var reportData = await _reportService.GenerateComplianceReportAsync(
                request.ClientId, 
                request.FromDate, 
                request.ToDate, 
                request.Format);

            var contentType = GetContentType(request.Format);
            var fileName = $"Compliance_{request.ClientId}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.{GetFileExtension(request.Format)}";

            _logger.LogInformation("Compliance report generated for client {ClientId}", request.ClientId);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report for client {ClientId}", request.ClientId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate and download client activity report (Admin/Associate only)
    /// </summary>
    [HttpPost("generate/client-activity")]
    [Authorize(Policy = "AdminOrAssociate")]
    public async Task<IActionResult> GenerateClientActivityReport([FromBody] ClientActivityReportRequestDto request)
    {
        try
        {
            var reportData = await _reportService.GenerateClientActivityReportAsync(
                request.FromDate, 
                request.ToDate, 
                request.Format);

            var contentType = GetContentType(request.Format);
            var fileName = $"ClientActivity_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.{GetFileExtension(request.Format)}";

            _logger.LogInformation("Client activity report generated for period {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client activity report");
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Generate and download financial summary report (Admin/Associate only)
    /// </summary>
    [HttpPost("generate/financial-summary")]
    [Authorize(Policy = "AdminOrAssociate")]
    public async Task<IActionResult> GenerateFinancialSummaryReport([FromBody] FinancialSummaryReportRequestDto request)
    {
        try
        {
            // Validate client access if specific client requested
            if (request.ClientId.HasValue)
            {
                var hasAccess = await ValidateClientAccess(request.ClientId.Value);
                if (!hasAccess)
                    return Forbid("Access denied to client data");
            }

            var reportData = await _reportService.GenerateFinancialSummaryReportAsync(
                request.ClientId,
                request.FromDate, 
                request.ToDate, 
                request.Format);

            var contentType = GetContentType(request.Format);
            var clientPart = request.ClientId.HasValue ? $"_{request.ClientId}" : "_All";
            var fileName = $"FinancialSummary{clientPart}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.{GetFileExtension(request.Format)}";

            _logger.LogInformation("Financial summary report generated for client {ClientId}", request.ClientId);

            return File(reportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial summary report");
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    // Helper methods
    private async Task<bool> ValidateClientAccess(int clientId)
    {
        // If user is admin or associate, they have access to all clients
        if (User.IsInRole("Admin") || User.IsInRole("Associate"))
            return true;

        // If user is a client, they can only access their own data
        if (User.IsInRole("Client"))
        {
            // For now, assume client users have access to their data
            // This would need proper implementation based on user-client relationship
            return true;
        }

        return false;
    }

    private static (bool IsValid, string ErrorMessage) ValidateReportRequest(CreateReportRequestDto request)
    {
        switch (request.Type)
        {
            case ReportType.TaxFiling:
                if (!request.Parameters.ContainsKey("clientId") || !request.Parameters.ContainsKey("taxYear"))
                    return (false, "Tax filing reports require clientId and taxYear parameters");
                break;

            case ReportType.PaymentHistory:
            case ReportType.Compliance:
                if (!request.Parameters.ContainsKey("clientId") || 
                    !request.Parameters.ContainsKey("fromDate") || 
                    !request.Parameters.ContainsKey("toDate"))
                    return (false, "This report type requires clientId, fromDate, and toDate parameters");
                break;

            case ReportType.ClientActivity:
                if (!request.Parameters.ContainsKey("fromDate") || !request.Parameters.ContainsKey("toDate"))
                    return (false, "Client activity reports require fromDate and toDate parameters");
                break;

            case ReportType.FinancialSummary:
                if (!request.Parameters.ContainsKey("fromDate") || !request.Parameters.ContainsKey("toDate"))
                    return (false, "Financial summary reports require fromDate and toDate parameters");
                break;
        }

        return (true, string.Empty);
    }

    private static string GetContentType(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.PDF => "application/pdf",
            ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.CSV => "text/csv",
            _ => "application/octet-stream"
        };
    }

    private static string GetFileExtension(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.PDF => "pdf",
            ReportFormat.Excel => "xlsx",
            ReportFormat.CSV => "csv",
            _ => "bin"
        };
    }

    private static string GetFileName(ReportRequestDto report)
    {
        var extension = GetFileExtension(report.Format);
        var timestamp = report.RequestedAt.ToString("yyyyMMdd_HHmmss");
        return $"{report.Type}_{timestamp}_{report.RequestId[..8]}.{extension}";
    }
}

// Request DTOs for direct report generation
public class TaxFilingReportRequestDto
{
    public int ClientId { get; set; }
    public int TaxYear { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
}

public class PaymentHistoryReportRequestDto
{
    public int ClientId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
}

public class ComplianceReportRequestDto
{
    public int ClientId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
}

public class ClientActivityReportRequestDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
}

public class FinancialSummaryReportRequestDto
{
    public int? ClientId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
}