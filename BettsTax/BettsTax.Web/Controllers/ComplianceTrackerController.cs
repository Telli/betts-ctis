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
    public class ComplianceTrackerController : ControllerBase
    {
        private readonly IComplianceTrackerService _complianceTrackerService;
        private readonly IPenaltyCalculationService _penaltyCalculationService;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<ComplianceTrackerController> _logger;

        public ComplianceTrackerController(
            IComplianceTrackerService complianceTrackerService,
            IPenaltyCalculationService penaltyCalculationService,
            IUserContextService userContextService,
            ILogger<ComplianceTrackerController> logger)
        {
            _complianceTrackerService = complianceTrackerService;
            _penaltyCalculationService = penaltyCalculationService;
            _userContextService = userContextService;
            _logger = logger;
        }

        /// <summary>
        /// Get compliance tracker for a specific client, tax year, and tax type
        /// </summary>
        [HttpGet("client/{clientId}/tax-year/{taxYearId}/tax-type/{taxType}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetComplianceTracker(int clientId, int taxYearId, TaxType taxType)
        {
            var result = await _complianceTrackerService.GetComplianceTrackerAsync(clientId, taxYearId, taxType);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get all compliance trackers for a client
        /// </summary>
        [HttpGet("client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientCompliance(int clientId)
        {
            // Ensure user can only access their own data or admin/associate can access any
            var currentUserId = _userContextService.GetCurrentUserId();
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own compliance data");
            }

            var result = await _complianceTrackerService.GetClientComplianceAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get compliance trackers with filtering options
        /// </summary>
        [HttpPost("search")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetComplianceTrackers([FromBody] ComplianceFilterDto filter)
        {
            var result = await _complianceTrackerService.GetComplianceTrackersAsync(filter);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Update compliance status for a tracker
        /// </summary>
        [HttpPut("{complianceTrackerId}/status")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> UpdateComplianceStatus(int complianceTrackerId, [FromBody] UpdateComplianceStatusDto updateDto)
        {
            if (complianceTrackerId != updateDto.ComplianceTrackerId)
                return BadRequest("Compliance tracker ID mismatch");

            var result = await _complianceTrackerService.UpdateComplianceStatusAsync(updateDto);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get overall compliance dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetComplianceDashboard()
        {
            var result = await _complianceTrackerService.GetComplianceDashboardAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get client-specific compliance dashboard
        /// </summary>
        [HttpGet("dashboard/client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientComplianceDashboard(int clientId)
        {
            // Ensure user can only access their own data or admin/associate can access any
            var currentUserId = _userContextService.GetCurrentUserId();
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own compliance dashboard");
            }

            var result = await _complianceTrackerService.GetClientComplianceDashboardAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get active alerts
        /// </summary>
        [HttpGet("alerts")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetActiveAlerts([FromQuery] int? clientId = null)
        {
            var result = await _complianceTrackerService.GetActiveAlertsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get active alerts for a client (client portal)
        /// </summary>
        [HttpGet("alerts/client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientActiveAlerts(int clientId)
        {
            // Ensure user can only access their own data
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own alerts");
            }

            var result = await _complianceTrackerService.GetActiveAlertsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new compliance alert
        /// </summary>
        [HttpPost("alerts")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CreateAlert([FromBody] CreateComplianceAlertDto createDto)
        {
            var result = await _complianceTrackerService.CreateAlertAsync(createDto);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetActiveAlerts), new { }, result.Value);
        }

        /// <summary>
        /// Mark an alert as read
        /// </summary>
        [HttpPut("alerts/{alertId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAlertAsRead(int alertId)
        {
            var result = await _complianceTrackerService.MarkAlertAsReadAsync(alertId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Get pending actions
        /// </summary>
        [HttpGet("actions")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetPendingActions([FromQuery] int? clientId = null)
        {
            var result = await _complianceTrackerService.GetPendingActionsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get pending actions for a client (client portal)
        /// </summary>
        [HttpGet("actions/client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientPendingActions(int clientId)
        {
            // Ensure user can only access their own data
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own actions");
            }

            var result = await _complianceTrackerService.GetPendingActionsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a new compliance action
        /// </summary>
        [HttpPost("actions")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CreateAction([FromBody] CreateComplianceActionDto createDto)
        {
            var result = await _complianceTrackerService.CreateActionAsync(createDto);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetPendingActions), new { }, result.Value);
        }

        /// <summary>
        /// Complete a compliance action
        /// </summary>
        [HttpPut("actions/{actionId}/complete")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CompleteAction(int actionId, [FromBody] CompleteActionDto completeDto)
        {
            var result = await _complianceTrackerService.CompleteActionAsync(actionId, completeDto.Notes);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Calculate penalty for specific parameters
        /// </summary>
        [HttpPost("penalties/calculate")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CalculatePenalty([FromBody] CalculatePenaltyDto penaltyDto)
        {
            var result = await _complianceTrackerService.CalculatePenaltyAsync(penaltyDto);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get penalties for a client
        /// </summary>
        [HttpGet("penalties/client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientPenalties(int clientId)
        {
            // Ensure user can only access their own data
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own penalty information");
            }

            var result = await _complianceTrackerService.GetClientPenaltiesAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Apply a calculated penalty to a compliance tracker
        /// </summary>
        [HttpPost("{complianceTrackerId}/penalties/apply")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ApplyPenalty(int complianceTrackerId, [FromBody] PenaltyCalculationResultDto penalty)
        {
            var result = await _complianceTrackerService.ApplyPenaltyAsync(complianceTrackerId, penalty);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Waive a penalty
        /// </summary>
        [HttpPut("penalties/{penaltyId}/waive")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> WaivePenalty(int penaltyId, [FromBody] WaivePenaltyDto waiveDto)
        {
            var result = await _complianceTrackerService.WaivePenaltyAsync(penaltyId, waiveDto.Reason);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Get active insights
        /// </summary>
        [HttpGet("insights")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetActiveInsights([FromQuery] int? clientId = null)
        {
            var result = await _complianceTrackerService.GetActiveInsightsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get active insights for a client (client portal)
        /// </summary>
        [HttpGet("insights/client/{clientId}")]
        [Authorize(Policy = "ClientPortal")]
        public async Task<IActionResult> GetClientActiveInsights(int clientId)
        {
            // Ensure user can only access their own data
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            if (!isAdminOrAssociate)
            {
                var userClientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (userClientId != clientId)
                    return Forbid("You can only access your own insights");
            }

            var result = await _complianceTrackerService.GetActiveInsightsAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Generate insight for a compliance tracker
        /// </summary>
        [HttpPost("{complianceTrackerId}/insights/generate")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GenerateInsight(int complianceTrackerId)
        {
            var result = await _complianceTrackerService.GenerateInsightAsync(complianceTrackerId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Mark insight as implemented
        /// </summary>
        [HttpPut("insights/{insightId}/implement")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> MarkInsightAsImplemented(int insightId)
        {
            var result = await _complianceTrackerService.MarkInsightAsImplementedAsync(insightId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true });
        }

        /// <summary>
        /// Run compliance check for a specific client or all clients
        /// </summary>
        [HttpPost("run-compliance-check")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RunComplianceCheck([FromQuery] int? clientId = null)
        {
            var result = await _complianceTrackerService.RunComplianceCheckAsync(clientId);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true, message = "Compliance check completed" });
        }

        /// <summary>
        /// Process overdue compliance items
        /// </summary>
        [HttpPost("process-overdue")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ProcessOverdueCompliance()
        {
            var result = await _complianceTrackerService.ProcessOverdueComplianceAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true, message = "Overdue compliance processing completed" });
        }

        /// <summary>
        /// Generate compliance alerts
        /// </summary>
        [HttpPost("generate-alerts")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GenerateComplianceAlerts()
        {
            var result = await _complianceTrackerService.GenerateComplianceAlertsAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true, message = "Compliance alerts generated" });
        }

        /// <summary>
        /// Get compliance trends
        /// </summary>
        [HttpGet("trends/compliance")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetComplianceTrends(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var result = await _complianceTrackerService.GetComplianceTrendsAsync(fromDate, toDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get penalty trends
        /// </summary>
        [HttpGet("trends/penalties")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetPenaltyTrends(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var result = await _complianceTrackerService.GetPenaltyTrendsAsync(fromDate, toDate);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get risk analysis
        /// </summary>
        [HttpGet("risk-analysis")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetRiskAnalysis()
        {
            var result = await _complianceTrackerService.GetRiskAnalysisAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Get penalty rules for specific tax type and penalty type
        /// </summary>
        [HttpGet("penalty-rules")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetPenaltyRules([FromQuery] TaxType taxType, [FromQuery] PenaltyType penaltyType)
        {
            var result = await _penaltyCalculationService.GetPenaltyRulesAsync(taxType, penaltyType);
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        /// <summary>
        /// Seed default penalty rules (admin only)
        /// </summary>
        [HttpPost("penalty-rules/seed")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SeedPenaltyRules()
        {
            var result = await _penaltyCalculationService.SeedDefaultPenaltyRulesAsync();
            
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { success = true, message = "Penalty rules seeded successfully" });
        }
    }

    // Supporting DTOs for request bodies
    public class CompleteActionDto
    {
        public string? Notes { get; set; }
    }

    public class WaivePenaltyDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}