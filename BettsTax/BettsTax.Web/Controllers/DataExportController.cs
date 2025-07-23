using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataExportController : ControllerBase
    {
        private readonly IDataExportService _exportService;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<DataExportController> _logger;

        public DataExportController(
            IDataExportService exportService,
            IUserContextService userContextService,
            ILogger<DataExportController> logger)
        {
            _exportService = exportService;
            _userContextService = userContextService;
            _logger = logger;
        }

        /// <summary>
        /// Export data based on the provided request
        /// </summary>
        [HttpPost("export")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportData([FromBody] ExportRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _exportService.ExportDataAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export tax returns with optional filters
        /// </summary>
        [HttpPost("tax-returns")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportTaxReturns([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.TaxReturns;
            
            var result = await _exportService.ExportTaxReturnsAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export payments with optional filters
        /// </summary>
        [HttpPost("payments")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportPayments([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.Payments;
            
            var result = await _exportService.ExportPaymentsAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export clients with optional filters
        /// </summary>
        [HttpPost("clients")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportClients([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.Clients;
            
            var result = await _exportService.ExportClientsAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export compliance report with optional filters
        /// </summary>
        [HttpPost("compliance-report")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportComplianceReport([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.ComplianceReport;
            
            var result = await _exportService.ExportComplianceReportAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export activity log with optional filters
        /// </summary>
        [HttpPost("activity-log")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportActivityLog([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.ActivityLog;
            
            var result = await _exportService.ExportActivityLogAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export documents with optional filters
        /// </summary>
        [HttpPost("documents")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportDocuments([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.Documents;
            
            var result = await _exportService.ExportDocumentsAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export penalties with optional filters
        /// </summary>
        [HttpPost("penalties")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportPenalties([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.Penalties;
            
            var result = await _exportService.ExportPenaltiesAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Export comprehensive report with all data types
        /// </summary>
        [HttpPost("comprehensive")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportComprehensive([FromBody] ExportRequestDto request)
        {
            request.ExportType = ExportType.Comprehensive;
            
            var result = await _exportService.ExportComprehensiveReportAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Bulk export multiple data types
        /// </summary>
        [HttpPost("bulk-export")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> BulkExport([FromBody] BulkExportRequestDto request)
        {
            var result = await _exportService.BulkExportAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get export history for the current user or all users (admin only)
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetExportHistory(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool allUsers = false)
        {
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            var userId = allUsers && isAdminOrAssociate ? null : _userContextService.GetCurrentUserId();
            
            var result = await _exportService.GetExportHistoryAsync(userId, fromDate, toDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get export by ID
        /// </summary>
        [HttpGet("{exportId}")]
        [Authorize]
        public async Task<IActionResult> GetExport(string exportId)
        {
            var result = await _exportService.GetExportByIdAsync(exportId);
            
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Download export file
        /// </summary>
        [HttpGet("{exportId}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadExport(string exportId, [FromQuery] string? password = null)
        {
            var result = await _exportService.DownloadExportAsync(exportId, password);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            var exportInfo = await _exportService.GetExportByIdAsync(exportId);
            if (!exportInfo.IsSuccess)
                return NotFound();

            var fileName = exportInfo.Value.FileName;
            var contentType = GetContentType(exportInfo.Value.Format);

            return File(result.Value, contentType, fileName);
        }

        /// <summary>
        /// Delete export file
        /// </summary>
        [HttpDelete("{exportId}")]
        [Authorize]
        public async Task<IActionResult> DeleteExport(string exportId)
        {
            var result = await _exportService.DeleteExportAsync(exportId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Extend export expiry date
        /// </summary>
        [HttpPut("{exportId}/extend")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExtendExportExpiry(string exportId, [FromBody] ExtendExpiryDto request)
        {
            var result = await _exportService.ExtendExportExpiryAsync(exportId, request.NewExpiryDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Preview export data without creating the file
        /// </summary>
        [HttpPost("preview")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> PreviewExport([FromBody] ExportRequestDto request)
        {
            var result = await _exportService.PreviewExportAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Validate export request without executing it
        /// </summary>
        [HttpPost("validate")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ValidateExportRequest([FromBody] ExportRequestDto request)
        {
            var result = await _exportService.ValidateExportRequestAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage, isValid = false });

            return Ok(new { isValid = true });
        }

        /// <summary>
        /// Get export templates
        /// </summary>
        [HttpGet("templates")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetExportTemplates()
        {
            var result = await _exportService.GetExportTemplatesAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Create export template
        /// </summary>
        [HttpPost("templates")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CreateExportTemplate([FromBody] CreateExportTemplateDto request)
        {
            var result = await _exportService.CreateExportTemplateAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetExportTemplates), new { }, result.Value);
        }

        /// <summary>
        /// Run export template
        /// </summary>
        [HttpPost("templates/{templateId}/run")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RunExportTemplate(
            int templateId,
            [FromBody] RunTemplateDto? request = null)
        {
            var result = await _exportService.RunExportTemplateAsync(
                templateId, 
                request?.CustomStartDate, 
                request?.CustomEndDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get export statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetExportStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _exportService.GetExportStatisticsAsync(fromDate, toDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Cleanup expired exports (admin only)
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CleanupExpiredExports()
        {
            var result = await _exportService.CleanupExpiredExportsAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { cleaned = result.Value, message = $"Cleaned up {result.Value} expired exports" });
        }

        /// <summary>
        /// Export client data for client portal (limited to own data)
        /// </summary>
        [HttpPost("client-portal/my-data")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> ExportClientData([FromBody] ClientExportRequestDto request)
        {
            var clientId = await _userContextService.GetCurrentUserClientIdAsync();
            if (!clientId.HasValue)
                return Forbid("No client association found");

            var exportRequest = new ExportRequestDto
            {
                ExportType = request.ExportType,
                Format = request.Format,
                ClientIds = new List<int> { clientId.Value },
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PasswordProtected = request.PasswordProtected,
                Password = request.Password,
                Description = $"Client data export for {DateTime.UtcNow:yyyy-MM-dd}"
            };

            var result = await _exportService.ExportDataAsync(exportRequest);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        // Helper method to get content type for file download
        private string GetContentType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.CSV => "text/csv",
                ExportFormat.PDF => "application/pdf",
                ExportFormat.JSON => "application/json",
                ExportFormat.XML => "application/xml",
                _ => "application/octet-stream"
            };
        }
    }

    // Supporting DTOs for controller requests
    public class ExtendExpiryDto
    {
        public DateTime NewExpiryDate { get; set; }
    }

    public class RunTemplateDto
    {
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }
    }

    public class ClientExportRequestDto
    {
        public ExportType ExportType { get; set; }
        public ExportFormat Format { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool PasswordProtected { get; set; } = false;
        public string? Password { get; set; }
    }
}