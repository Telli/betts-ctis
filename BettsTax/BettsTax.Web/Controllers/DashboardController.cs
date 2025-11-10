using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Dashboard Controller - Provides dashboard data and metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        Services.IAuthorizationService authorizationService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard metrics
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMetrics([FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId}", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var metrics = await _dashboardService.GetMetricsAsync(effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved dashboard metrics", userId);

            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving metrics" });
        }
    }

    /// <summary>
    /// Get filing trends for charts
    /// </summary>
    [HttpGet("filing-trends")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilingTrends(
        [FromQuery] int? clientId = null,
        [FromQuery] int months = 6)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var trends = await _dashboardService.GetFilingTrendsAsync(effectiveClientId, months);

            return Ok(new { success = true, data = trends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filing trends");
            return StatusCode(500, new { success = false, message = "Error retrieving trends" });
        }
    }

    /// <summary>
    /// Get compliance distribution
    /// </summary>
    [HttpGet("compliance-distribution")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplianceDistribution([FromQuery] int? clientId = null)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var distribution = await _dashboardService.GetComplianceDistributionAsync(effectiveClientId);

            return Ok(new { success = true, data = distribution });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compliance distribution");
            return StatusCode(500, new { success = false, message = "Error retrieving distribution" });
        }
    }

    /// <summary>
    /// Get upcoming deadlines
    /// </summary>
    [HttpGet("upcoming-deadlines")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingDeadlines(
        [FromQuery] int? clientId = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var deadlines = await _dashboardService.GetUpcomingDeadlinesAsync(effectiveClientId, limit);

            return Ok(new { success = true, data = deadlines });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming deadlines");
            return StatusCode(500, new { success = false, message = "Error retrieving deadlines" });
        }
    }

    /// <summary>
    /// Get recent activity
    /// </summary>
    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int? clientId = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var activities = await _dashboardService.GetRecentActivityAsync(effectiveClientId, limit);

            return Ok(new { success = true, data = activities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return StatusCode(500, new { success = false, message = "Error retrieving activity" });
        }
    }
}
