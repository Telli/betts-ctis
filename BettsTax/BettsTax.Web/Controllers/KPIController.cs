using BettsTax.Core.DTOs.KPI;
using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KPIController : ControllerBase
{
    private readonly IKPIService _kpiService;
    private readonly ILogger<KPIController> _logger;

    public KPIController(IKPIService kpiService, ILogger<KPIController> logger)
    {
        _kpiService = kpiService;
        _logger = logger;
    }

    /// <summary>
    /// Get internal KPIs for firm administrators
    /// </summary>
    [HttpGet("internal")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<InternalKPIDto>> GetInternalKPIs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var kpis = await _kpiService.GetInternalKPIsAsync(fromDate, toDate);
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving internal KPIs");
            return StatusCode(500, "An error occurred while retrieving KPI data");
        }
    }

    /// <summary>
    /// Get KPI trends over time for administrators
    /// </summary>
    [HttpGet("internal/trends")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<InternalKPIDto>>> GetKPITrends(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string period = "Monthly")
    {
        try
        {
            var trends = await _kpiService.GetKPITrendsAsync(fromDate, toDate, period);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI trends from {FromDate} to {ToDate}", fromDate, toDate);
            return StatusCode(500, "An error occurred while retrieving KPI trend data");
        }
    }

    /// <summary>
    /// Get client-specific KPIs (accessible by client or their associates)
    /// </summary>
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<ClientKPIDto>> GetClientKPIs(
        int clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Authorization check: Admin/SystemAdmin can access any client, 
            // Clients can only access their own data
            if (userRole != "Admin" && userRole != "SystemAdmin")
            {
                var userClientId = User.FindFirst("ClientId")?.Value;
                if (userClientId != clientId.ToString())
                {
                    return Forbid("You can only access your own KPI data");
                }
            }

            var kpis = await _kpiService.GetClientKPIsAsync(clientId, fromDate, toDate);
            return Ok(kpis);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client KPIs for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while retrieving client KPI data");
        }
    }

    /// <summary>
    /// Get current user's KPI data (for client dashboard)
    /// </summary>
    [HttpGet("my-kpis")]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<ClientKPIDto>> GetMyKPIs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var clientIdClaim = User.FindFirst("ClientId")?.Value;
            if (!int.TryParse(clientIdClaim, out var clientId))
            {
                return BadRequest("Client ID not found in token");
            }

            var kpis = await _kpiService.GetClientKPIsAsync(clientId, fromDate, toDate);
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPIs for current user");
            return StatusCode(500, "An error occurred while retrieving your KPI data");
        }
    }

    /// <summary>
    /// Get KPI alerts for administrators or specific client
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<List<KPIAlertDto>>> GetKPIAlerts([FromQuery] int? clientId = null)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Non-admin users can only see their own alerts
            if (userRole != "Admin" && userRole != "SystemAdmin")
            {
                var userClientId = User.FindFirst("ClientId")?.Value;
                if (int.TryParse(userClientId, out var parsedClientId))
                {
                    clientId = parsedClientId;
                }
            }

            var alerts = await _kpiService.GetKPIAlertsAsync(clientId);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI alerts for client {ClientId}", clientId);
            return StatusCode(500, "An error occurred while retrieving KPI alerts");
        }
    }

    /// <summary>
    /// Update KPI thresholds (admin only)
    /// </summary>
    [HttpPut("thresholds")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> UpdateKPIThresholds([FromBody] KPIThresholdDto thresholds)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _kpiService.UpdateKPIThresholdsAsync(thresholds);
            
            _logger.LogInformation("KPI thresholds updated by user {UserId}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
            return Ok(new { message = "KPI thresholds updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI thresholds");
            return StatusCode(500, "An error occurred while updating KPI thresholds");
        }
    }

    /// <summary>
    /// Refresh all KPI data (admin only)
    /// </summary>
    [HttpPost("refresh")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> RefreshKPIData()
    {
        try
        {
            var success = await _kpiService.RefreshKPIDataAsync();
            
            if (success)
            {
                _logger.LogInformation("KPI data refresh initiated by user {UserId}", 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    
                return Ok(new { message = "KPI data refresh completed successfully" });
            }
            else
            {
                return StatusCode(500, "KPI data refresh failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KPI data refresh");
            return StatusCode(500, "An error occurred while refreshing KPI data");
        }
    }

    /// <summary>
    /// Mark a KPI alert as read
    /// </summary>
    [HttpPut("alerts/{alertId}/read")]
    public async Task<ActionResult> MarkAlertAsRead(int alertId)
    {
        try
        {
            await _kpiService.MarkAlertAsReadAsync(alertId);
            return Ok(new { message = "Alert marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking alert {AlertId} as read", alertId);
            return StatusCode(500, "An error occurred while updating the alert");
        }
    }

    /// <summary>
    /// Create a manual KPI alert (admin only)
    /// </summary>
    [HttpPost("alerts")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> CreateKPIAlert([FromBody] KPIAlertDto alert)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _kpiService.CreateKPIAlertAsync(alert);
            
            _logger.LogInformation("Manual KPI alert created by user {UserId}: {Title}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, alert.Title);
                
            return Ok(new { message = "KPI alert created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI alert");
            return StatusCode(500, "An error occurred while creating the KPI alert");
        }
    }
}