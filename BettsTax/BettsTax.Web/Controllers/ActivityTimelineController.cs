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
    public class ActivityTimelineController : ControllerBase
    {
        private readonly IActivityTimelineService _activityService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<ActivityTimelineController> _logger;

        public ActivityTimelineController(
            IActivityTimelineService activityService,
            IUserContextService userContext,
            ILogger<ActivityTimelineController> logger)
        {
            _activityService = activityService;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CreateActivity([FromBody] ActivityTimelineCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _activityService.CreateActivityAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetActivities([FromQuery] ActivityTimelineFilterDto filter)
        {
            var result = await _activityService.GetActivitiesAsync(filter);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetClientActivities(int clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            // Check if user can access this client's data
            if (!await _userContext.CanAccessClientDataAsync(clientId))
                return Forbid();

            var result = await _activityService.GetClientActivitiesAsync(clientId, page, pageSize);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}/recent")]
        public async Task<IActionResult> GetRecentClientActivities(int clientId, [FromQuery] int count = 10)
        {
            // Check if user can access this client's data
            if (!await _userContext.CanAccessClientDataAsync(clientId))
                return Forbid();

            var result = await _activityService.GetRecentClientActivitiesAsync(clientId, count);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}/grouped")]
        public async Task<IActionResult> GetGroupedActivities(int clientId, [FromQuery] int days = 30)
        {
            // Check if user can access this client's data
            if (!await _userContext.CanAccessClientDataAsync(clientId))
                return Forbid();

            var result = await _activityService.GetGroupedActivitiesAsync(clientId, days);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetActivitySummary([FromQuery] int? clientId = null)
        {
            // If clientId is provided, check access
            if (clientId.HasValue && !await _userContext.CanAccessClientDataAsync(clientId.Value))
                return Forbid();

            var userId = await _userContext.IsClientUserAsync() ? _userContext.GetCurrentUserId() : null;
            var result = await _activityService.GetActivitySummaryAsync(clientId, userId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client-summaries")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetClientActivitySummaries()
        {
            var associateId = await _userContext.IsAdminOrAssociateAsync() ? null : _userContext.GetCurrentUserId();
            var result = await _activityService.GetClientActivitySummariesAsync(associateId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("associate/{associateId}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetAssociateClientActivities(string associateId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            // Only allow access to own activities unless admin
            var currentUserId = _userContext.GetCurrentUserId();
            var isAdmin = await _userContext.IsAdminOrAssociateAsync() && User.IsInRole("Admin");
            
            if (!isAdmin && currentUserId != associateId)
                return Forbid();

            var result = await _activityService.GetAssociateClientActivitiesAsync(associateId, page, pageSize);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("high-priority")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetHighPriorityActivities([FromQuery] int count = 20)
        {
            var associateId = await _userContext.IsAdminOrAssociateAsync() ? null : _userContext.GetCurrentUserId();
            var result = await _activityService.GetHighPriorityActivitiesAsync(associateId, count);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("system-alerts")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetSystemAlerts([FromQuery] int? clientId = null, [FromQuery] int days = 7)
        {
            var result = await _activityService.GetSystemAlertsAsync(clientId, days);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchActivities([FromQuery] string searchTerm, [FromQuery] ActivityTimelineFilterDto? filter = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term is required");

            var result = await _activityService.SearchActivitiesAsync(searchTerm, filter);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("export/csv")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportActivitiesToCsv([FromQuery] ActivityTimelineFilterDto filter)
        {
            var result = await _activityService.ExportActivitiesToCsvAsync(filter);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return File(result.Value, "text/csv", $"activities_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet("client/{clientId}/export/pdf")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ExportActivitiesToPdf(int clientId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var result = await _activityService.ExportActivitiesToPdfAsync(clientId, startDate, endDate);
            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return File(result.Value, "application/pdf", $"client_{clientId}_activities_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkActivitiesAsRead([FromBody] int clientId)
        {
            var userId = _userContext.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _activityService.MarkActivitiesAsReadAsync(clientId, userId);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpDelete("cleanup")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> DeleteOldActivities([FromQuery] int daysToKeep = 365)
        {
            var result = await _activityService.DeleteOldActivitiesAsync(daysToKeep);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        // Quick activity logging endpoints
        [HttpPost("log/login/{clientId}")]
        public async Task<IActionResult> LogClientLogin(int clientId)
        {
            var result = await _activityService.LogClientLoginAsync(clientId);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpPost("log/document/{documentId}")]
        public async Task<IActionResult> LogDocumentActivity(int documentId, [FromQuery] ActivityType activityType, [FromQuery] string? additionalInfo = null)
        {
            var result = await _activityService.LogDocumentActivityAsync(documentId, activityType, additionalInfo);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpPost("log/tax-filing/{taxFilingId}")]
        public async Task<IActionResult> LogTaxFilingActivity(int taxFilingId, [FromQuery] ActivityType activityType, [FromQuery] string? additionalInfo = null)
        {
            var result = await _activityService.LogTaxFilingActivityAsync(taxFilingId, activityType, additionalInfo);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpPost("log/payment/{paymentId}")]
        public async Task<IActionResult> LogPaymentActivity(int paymentId, [FromQuery] ActivityType activityType, [FromQuery] string? additionalInfo = null)
        {
            var result = await _activityService.LogPaymentActivityAsync(paymentId, activityType, additionalInfo);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }
    }
}