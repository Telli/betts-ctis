using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BettsTax.Data;
using BettsTax.Core.DTOs.KPI;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/kpi-simple")] // distinct path to avoid routing conflicts with legacy KPI service (if any)
    public class KpiController : ControllerBase
    {
        private readonly IKpiComputationService _kpi;
        private readonly BettsTax.Core.Services.INotificationService _notifications;
    private readonly ILogger<KpiController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

        public KpiController(
            IKpiComputationService kpi,
            BettsTax.Core.Services.INotificationService notifications,
            UserManager<ApplicationUser> userManager,
            ILogger<KpiController> logger)
        {
            _kpi = kpi;
            _notifications = notifications;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAdmin()
        {
            var metrics = await _kpi.GetCurrentAsync();
            await GenerateAlertsIfNeeded(metrics);
            return Ok(metrics);
        }

        [HttpGet("client")]
        [Authorize(Roles = "Client,Associate,Admin,SystemAdmin")]
        public async Task<IActionResult> GetClient()
        {
            var m = await _kpi.GetCurrentAsync();
            return Ok(new { m.GeneratedAtUtc, m.ClientComplianceRate, m.TaxFilingTimeliness, m.PaymentCompletionRate, m.DocumentSubmissionCompliance, m.ClientEngagementRate });
        }

        private async Task GenerateAlertsIfNeeded(Core.DTOs.KpiMetricsDto m)
        {
            var alerts = new List<string>();
            if (m.ComplianceRateBelowThreshold) alerts.Add($"Compliance rate dropped to {m.ClientComplianceRate}%");
            if (m.FilingTimelinessBelowThreshold) alerts.Add($"Filing timeliness {m.TaxFilingTimeliness}%");
            if (m.PaymentCompletionBelowThreshold) alerts.Add($"Payment completion {m.PaymentCompletionRate}%");
            if (m.DocumentSubmissionBelowThreshold) alerts.Add($"Document submission {m.DocumentSubmissionCompliance}%");
            if (m.EngagementBelowThreshold) alerts.Add($"Engagement {m.ClientEngagementRate}%");
            if (alerts.Count == 0) return;
            // Fetch admin + system admin users once
            var admins = _userManager.Users
                .Where(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow) // active
                .ToList(); // materialize to iterate and check roles asynchronously
            foreach (var user in admins)
            {
                try
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("Admin") && !roles.Contains("SystemAdmin")) continue;
                    foreach (var msg in alerts)
                        await _notifications.CreateAsync(user.Id, msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed creating KPI notifications for user {UserId}", user.Id);
                }
            }
        }
    }

    // New KPI controller with expected frontend endpoints
    [ApiController]
    [Route("api/kpi")]
    public class KPIController : ControllerBase
    {
        private readonly IKPIService _kpiService;
        private readonly ILogger<KPIController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public KPIController(
            IKPIService kpiService,
            UserManager<ApplicationUser> userManager,
            ILogger<KPIController> logger)
        {
            _kpiService = kpiService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("internal")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetInternalKPIs(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var kpis = await _kpiService.GetInternalKPIsAsync(fromDate, toDate);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching internal KPIs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetClientKPIs(int clientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var clientKpis = await _kpiService.GetClientKPIsAsync(clientId, fromDate, toDate);
                return Ok(clientKpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching client KPIs for client {ClientId}", clientId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("my-kpis")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetMyKPIs(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.ClientProfile == null)
                {
                    return Unauthorized("User not associated with a client");
                }

                var clientKpis = await _kpiService.GetClientKPIsAsync(user.ClientProfile.Id, fromDate, toDate);
                return Ok(clientKpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching my KPIs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("internal/trends")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetKpiTrends(DateTime fromDate, DateTime toDate, string period = "Monthly")
        {
            try
            {
                if (toDate == default)
                {
                    toDate = DateTime.UtcNow;
                }

                if (fromDate == default)
                {
                    fromDate = toDate.AddMonths(-3);
                }

                if (toDate < fromDate)
                {
                    (fromDate, toDate) = (toDate, fromDate);
                }

                var trends = await _kpiService.GetKPITrendsAsync(fromDate, toDate, period);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching KPI trends between {From} and {To}", fromDate, toDate);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("alerts")]
        [Authorize]
        public async Task<IActionResult> GetKpiAlerts(int? clientId = null)
        {
            try
            {
                var alerts = await _kpiService.GetKPIAlertsAsync(clientId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching KPI alerts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("alerts/{alertId}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAlertAsRead(int alertId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
                await _kpiService.MarkAlertAsReadAsync(alertId, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking KPI alert {AlertId} as read", alertId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("alerts")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> CreateAlert([FromBody] KPIAlertDto alert)
        {
            if (alert == null)
            {
                return BadRequest("Alert payload is required");
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _kpiService.CreateKPIAlertAsync(alert, userId);
                return Ok(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating KPI alert");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("thresholds")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateThresholds([FromBody] KPIThresholdDto thresholds)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _kpiService.UpdateKPIThresholdsAsync(thresholds);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating KPI thresholds");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("refresh")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RefreshKpis()
        {
            try
            {
                var success = await _kpiService.RefreshKPIDataAsync();
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing KPI data");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}