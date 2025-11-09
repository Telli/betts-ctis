using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Filing workspace endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilingsController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<FilingsController> _logger;

    public FilingsController(
        IDemoDataService demoDataService,
        ILogger<FilingsController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve the active filing workspace.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveFiling()
    {
        try
        {
            var filing = await _demoDataService.GetActiveFilingAsync();
            if (filing is null)
            {
                return NotFound(new { success = false, message = "No active filing found" });
            }

            return Ok(new { success = true, data = filing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active filing workspace");
            return StatusCode(500, new { success = false, message = "Failed to load filing workspace" });
        }
    }
}
