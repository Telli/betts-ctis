using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Provides data required to build reports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IDemoDataService demoDataService,
        ILogger<ReportsController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// List available report definitions.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportTypes()
    {
        try
        {
            var types = await _demoDataService.GetReportTypesAsync();
            return Ok(new { success = true, data = types });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve report types");
            return StatusCode(500, new { success = false, message = "Failed to load report types" });
        }
    }

    /// <summary>
    /// Retrieve available filter options for report generation.
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportFilters()
    {
        try
        {
            var filters = await _demoDataService.GetReportFiltersAsync();
            return Ok(new { success = true, data = filters });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve report filters");
            return StatusCode(500, new { success = false, message = "Failed to load report filters" });
        }
    }
}
