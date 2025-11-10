using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Quartz;
using System.Collections.Concurrent;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ReportsController> _logger;
    private readonly IScheduler _scheduler;

    // In-memory templates store (dev/demo)
    private static readonly ConcurrentDictionary<string, ReportTemplateModel> _templates = new();
    private static bool _templatesInitialized = false;

    public ReportsController(
        IReportService reportService,
        IUserContextService userContext,
        ILogger<ReportsController> logger,
        IScheduler scheduler)
    {
        _reportService = reportService;
        _userContext = userContext;
        _logger = logger;
        _scheduler = scheduler;

        // Seed default templates once
        if (!_templatesInitialized)
        {
            SeedDefaultTemplates();
            _templatesInitialized = true;
        }
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
    /// Cancel a pending report (only if it hasn't started processing)
    /// </summary>
    [HttpPost("cancel/{requestId}")]
    public async Task<IActionResult> CancelReport(string requestId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            var (success, message) = await CancelReportInternalAsync(requestId, userId);
            if (!success)
                return Conflict(new { message });

            return Ok(new { message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling report {RequestId}", requestId);
            return StatusCode(500, "An error occurred while cancelling the report");
        }
    }

    /// <summary>
    /// Get built-in and user-defined report templates (in-memory for development)
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        var list = _templates.Values
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToList();
        return Ok(list);
    }

    /// <summary>
    /// Get a specific report template by id
    /// </summary>
    [HttpGet("templates/{templateId}")]
    public IActionResult GetTemplateById(string templateId)
    {
        if (!_templates.TryGetValue(templateId, out var template))
            return NotFound("Template not found");

        return Ok(template);
    }

    /// <summary>
    /// Save a user-defined report template (in-memory for development)
    /// </summary>
    [HttpPost("templates")]
    public IActionResult SaveTemplate([FromBody] CreateReportTemplateModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest("Template name is required");

        var id = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var template = new ReportTemplateModel
        {
            Id = id,
            Name = model.Name,
            Description = model.Description ?? string.Empty,
            ReportType = model.ReportType,
            Parameters = model.Parameters ?? new Dictionary<string, object>(),
            IsDefault = model.IsDefault,
            CreatedAt = now,
            UpdatedAt = now
        };
        _templates[id] = template;
        return Ok(template);
    }

    /// <summary>
    /// Update an existing report template
    /// </summary>
    [HttpPut("templates/{templateId}")]
    public IActionResult UpdateTemplate(string templateId, [FromBody] CreateReportTemplateModel model)
    {
        if (!_templates.TryGetValue(templateId, out var existing))
            return NotFound("Template not found");

        existing.Name = string.IsNullOrWhiteSpace(model.Name) ? existing.Name : model.Name;
        existing.Description = model.Description ?? existing.Description;
        existing.ReportType = string.IsNullOrWhiteSpace(model.ReportType) ? existing.ReportType : model.ReportType;
        if (model.Parameters != null)
        {
            existing.Parameters = model.Parameters;
        }
        // Allow toggling default flag; in a real system restrict to Admin
        existing.IsDefault = model.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;

        return Ok(existing);
    }

    /// <summary>
    /// Delete a report template (cannot delete built-in defaults)
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    public IActionResult DeleteTemplate(string templateId)
    {
        if (!_templates.TryGetValue(templateId, out var existing))
            return NotFound("Template not found");

        if (existing.IsDefault)
            return BadRequest("Cannot delete a default template");

        _templates.TryRemove(templateId, out _);
        return Ok(new { Message = "Template deleted" });
    }

    /// <summary>
    /// Generate a quick preview for a report request (returns file content)
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] CreateReportRequestDto request)
    {
        try
        {
            var (content, contentType, fileName) = await GeneratePreviewFileAsync(request);
            return File(content, contentType, fileName);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for type {Type}", request.Type);
            return StatusCode(500, "An error occurred while generating the preview");
        }
    }

    /// <summary>
    /// Generate a quick preview for a saved template (parameters can override template parameters)
    /// </summary>
    [HttpPost("templates/{templateId}/preview")]
    public async Task<IActionResult> PreviewTemplate(string templateId, [FromBody] Dictionary<string, object>? parameters, [FromQuery] string? format = null)
    {
        if (!_templates.TryGetValue(templateId, out var template))
            return NotFound("Template not found");

        var previewFormat = ReportFormat.PDF;
        if (!string.IsNullOrWhiteSpace(format))
        {
            if (!Enum.TryParse<ReportFormat>(format, true, out previewFormat))
            {
                return BadRequest($"Unknown format: {format}");
            }
        }

        var request = new CreateReportRequestDto
        {
            Type = MapReportType(template.ReportType),
            Format = previewFormat,
            Parameters = MergeParameters(template.Parameters, parameters ?? new Dictionary<string, object>())
        };

        try
        {
            var (content, contentType, fileName) = await GeneratePreviewFileAsync(request);
            return File(content, contentType, fileName);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template preview for {TemplateId}", templateId);
            return StatusCode(500, "An error occurred while generating the preview");
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
    public async Task<IActionResult> GetReportHistory(
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] ReportType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");

            if (pageSize > 100) pageSize = 100; // Limit page size
            if (pageNumber < 1) pageNumber = 1;

            var filter = new ReportHistoryFilter
            {
                Status = status,
                Type = type,
                FromDate = fromDate,
                ToDate = toDate,
                Search = search,
                SortBy = sortBy,
                SortDir = sortDir
            };

            var reports = await _reportService.GetUserReportsAsync(userId, filter, pageSize, pageNumber);
            var totalCount = await _reportService.GetUserReportCountAsync(userId, filter);
            
            return Ok(new 
            { 
                Reports = reports,
                PageSize = pageSize,
                PageNumber = pageNumber,
                TotalCount = totalCount
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
    private async Task<(bool success, string message)> CancelReportInternalAsync(string requestId, string userId)
    {
        var report = await _reportService.GetReportStatusAsync(requestId);
        if (report == null)
            return (false, "Report not found");

        if (report.RequestedByUserId != userId && !User.IsInRole("Admin"))
            return (false, "Access denied");

        if (report.Status == ReportStatus.Completed || report.Status == ReportStatus.Failed)
            return (false, "Cannot cancel a completed or failed report");

        if (report.Status == ReportStatus.Processing)
            return (false, "Report is already processing and cannot be cancelled");

        // Attempt to delete the scheduled job
        var jobKey = new JobKey($"report-{requestId}", "reports");
        await _scheduler.DeleteJob(jobKey);

        // Mark as cancelled via service by updating DB record
        // Use a small hack: queue a delete? Instead, call a light-weight update through the service implementation
        if (_reportService is BettsTax.Core.Services.ReportService concrete)
        {
            await concrete.MarkReportCancelledAsync(requestId);
        }
        else
        {
            // No direct way to update via interface; no-op to keep method truly async
            await Task.CompletedTask;
        }

        return (true, "Report cancelled successfully");
    }

    private static Dictionary<string, object> MergeParameters(Dictionary<string, object> baseParams, Dictionary<string, object> overrides)
    {
        var result = new Dictionary<string, object>(baseParams);
        foreach (var kvp in overrides)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
    }

    private static string GetTemplateTypeDisplay(ReportType type) => type.ToString();

    private static ReportType MapReportType(string templateReportType)
    {
        // First handle known UI aliases used by the frontend
        var alias = (templateReportType ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Report type is required");

        // Map common aliases to supported core types
        switch (alias.ToLowerInvariant())
        {
            case "taxsummary":
            case "tax summary":
            case "taxcompliance":
            case "tax compliance":
                return ReportType.TaxFiling;

            case "compliancestatus":
            case "compliance status":
            case "penaltyanalysis":
            case "penalty analysis":
                return ReportType.Compliance;

            case "auditrail":
            case "audit trail":
                return ReportType.ClientActivity;

            case "monthlyreconciliation":
            case "monthly reconciliation":
            case "clientportfolio":
            case "client portfolio":
            case "kpisummary":
            case "kpi summary":
                return ReportType.FinancialSummary;
        }

        // Convert stored template type (string) to ReportType enum (case-insensitive)
        if (Enum.TryParse<ReportType>(templateReportType, true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unknown report type: {templateReportType}");
    }

    private async Task<(byte[] Content, string ContentType, string FileName)> GeneratePreviewFileAsync(CreateReportRequestDto request)
    {
        // Minimal validation and parameter parsing
        var type = request.Type;
        var format = request.Format;
        var p = request.Parameters;

        byte[] file = type switch
        {
            ReportType.TaxFiling => await _reportService.GenerateTaxFilingReportAsync(
                GetIntParameter(p, "clientId"), GetIntParameter(p, "taxYear"), format),

            ReportType.PaymentHistory => await _reportService.GeneratePaymentHistoryReportAsync(
                GetIntParameter(p, "clientId"), GetDateParameter(p, "fromDate"), GetDateParameter(p, "toDate"), format),

            ReportType.Compliance => await _reportService.GenerateComplianceReportAsync(
                GetIntParameter(p, "clientId"), GetDateParameter(p, "fromDate"), GetDateParameter(p, "toDate"), format),

            ReportType.ClientActivity => await _reportService.GenerateClientActivityReportAsync(
                GetDateParameter(p, "fromDate"), GetDateParameter(p, "toDate"), format),

            ReportType.FinancialSummary => await _reportService.GenerateFinancialSummaryReportAsync(
                TryGetIntParameter(p, "clientId"), GetDateParameter(p, "fromDate"), GetDateParameter(p, "toDate"), format),

            _ => throw new ArgumentException($"Unsupported report type: {type}")
        };

        var contentType = GetContentType(format);
        var fileName = $"Preview_{type}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{GetFileExtension(format)}";
        return (file, contentType, fileName);
    }

    private static int GetIntParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
                return element.GetInt32();
            if (int.TryParse(value?.ToString(), out var intValue))
                return intValue;
        }
        throw new ArgumentException($"Missing or invalid parameter: {key}");
    }

    private static int? TryGetIntParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
                return element.GetInt32();
            if (int.TryParse(value?.ToString(), out var intValue))
                return intValue;
        }
        return null;
    }

    private static DateTime GetDateParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                if (DateTime.TryParse(element.GetString(), out var dateValue))
                    return dateValue;
            }
            if (DateTime.TryParse(value?.ToString(), out var parsedDate))
                return parsedDate;
        }
        throw new ArgumentException($"Missing or invalid date parameter: {key}");
    }

    private static void SeedDefaultTemplates()
    {
        void Add(string id, string name, string desc, string reportType, Dictionary<string, object> parameters)
        {
            _templates[id] = new ReportTemplateModel
            {
                Id = id,
                Name = name,
                Description = desc,
                ReportType = reportType,
                Parameters = parameters,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        Add("tmpl-taxfiling", "Tax Filing (Year)", "Tax filing for a specific year", "TaxFiling",
            new Dictionary<string, object> { { "taxYear", DateTime.UtcNow.Year }, { "clientId", 1 } });
        Add("tmpl-payment-history", "Payment History (90d)", "Payments for last 90 days", "PaymentHistory",
            new Dictionary<string, object> { { "fromDate", DateTime.UtcNow.AddDays(-90) }, { "toDate", DateTime.UtcNow }, { "clientId", 1 } });
        Add("tmpl-compliance", "Compliance (This Year)", "Compliance overview for this year", "Compliance",
            new Dictionary<string, object> { { "fromDate", new DateTime(DateTime.UtcNow.Year, 1, 1) }, { "toDate", DateTime.UtcNow }, { "clientId", 1 } });
    }

    // DTOs for templates (in-memory)
    public class ReportTemplateModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateReportTemplateModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public bool IsDefault { get; set; } = false;
    }
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