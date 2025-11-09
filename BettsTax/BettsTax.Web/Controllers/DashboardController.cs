using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Provides dashboard specific data sets consumed by the frontend.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDemoDataService demoDataService,
        ILogger<DashboardController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve the dashboard summary including metrics, charts and recent activity.
    /// </summary>
    /// <param name="daysAhead">The number of days ahead to include for upcoming deadlines (default 30).</param>
    /// <param name="clientId">Optional client filter.</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary([FromQuery] int daysAhead = 30, [FromQuery] int? clientId = null)
    {
        try
        {
            if (daysAhead is < 1 or > 365)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "daysAhead must be between 1 and 365"
                });
            }

            var summary = await _demoDataService.GetDashboardSummaryAsync(clientId, daysAhead);
            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve dashboard summary");
            return StatusCode(500, new { success = false, message = "Failed to load dashboard summary" });
        }
    }
}
