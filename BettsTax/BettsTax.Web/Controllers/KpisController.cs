using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// KPIs Controller - Provides key performance indicators
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KpisController : ControllerBase
{
    private readonly IKpiService _kpiService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<KpisController> _logger;

    public KpisController(
        IKpiService kpiService,
        Services.IAuthorizationService authorizationService,
        ILogger<KpisController> logger)
    {
        _kpiService = kpiService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get KPI metrics
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKpiMetrics([FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId} KPIs", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var metrics = await _kpiService.GetKpiMetricsAsync(effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved KPI metrics", userId);

            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI metrics");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving KPI metrics" });
        }
    }

    /// <summary>
    /// Get monthly trends
    /// </summary>
    [HttpGet("monthly-trends")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMonthlyTrends(
        [FromQuery] int? clientId = null,
        [FromQuery] int months = 6)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId} trends", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var trends = await _kpiService.GetMonthlyTrendsAsync(effectiveClientId, months);

            _logger.LogInformation("User {UserId} retrieved monthly trends", userId);

            return Ok(new { success = true, data = trends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly trends");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving trends" });
        }
    }

    /// <summary>
    /// Get client performance rankings (Staff/Admin only)
    /// </summary>
    [HttpGet("client-performance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClientPerformance([FromQuery] int limit = 10)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only staff or admin can view client performance rankings
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client performance", userId);
                return StatusCode(403, new { success = false, message = "Access denied. Only staff or admin can view client performance." });
            }

            var performance = await _kpiService.GetClientPerformanceAsync(limit);

            _logger.LogInformation("User {UserId} retrieved client performance rankings", userId);

            return Ok(new { success = true, data = performance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client performance");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving client performance" });
        }
    }
}
