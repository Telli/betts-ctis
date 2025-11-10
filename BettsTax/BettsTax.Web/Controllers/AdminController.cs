using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Administrative data endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDemoDataService demoDataService,
        ILogger<AdminController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    /// <summary>
    /// Retrieve all system users.
    /// </summary>
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _demoDataService.GetAdminUsersAsync();
            return Ok(new { success = true, data = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve admin users");
            return StatusCode(500, new { success = false, message = "Failed to load users" });
        }
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs()
    {
        try
        {
            var logs = await _demoDataService.GetAuditLogsAsync();
            return Ok(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return StatusCode(500, new { success = false, message = "Failed to load audit logs" });
        }
    }

    [HttpGet("tax-rates")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxRates()
    {
        try
        {
            var rates = await _demoDataService.GetTaxRatesAsync();
            return Ok(new { success = true, data = rates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tax rates");
            return StatusCode(500, new { success = false, message = "Failed to load tax rates" });
        }
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobStatuses()
    {
        try
        {
            var jobs = await _demoDataService.GetJobStatusesAsync();
            return Ok(new { success = true, data = jobs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve job statuses");
            return StatusCode(500, new { success = false, message = "Failed to load job statuses" });
        }
    }
}
