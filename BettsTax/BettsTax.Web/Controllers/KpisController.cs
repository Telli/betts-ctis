using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Access to KPI metrics and trends.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KpisController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<KpisController> _logger;

    public KpisController(
        IDemoDataService demoDataService,
        ILogger<KpisController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve KPI metrics, trend data and performance breakdowns.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis([FromQuery] int? clientId = null)
    {
        try
        {
            var summary = await _demoDataService.GetKpiSummaryAsync(clientId);
            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve KPI summary");
            return StatusCode(500, new { success = false, message = "Failed to load KPIs" });
        }
    }
}
