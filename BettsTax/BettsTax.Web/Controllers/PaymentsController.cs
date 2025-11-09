using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Provides payment history data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IDemoDataService demoDataService,
        ILogger<PaymentsController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve payments with aggregated status totals.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments([FromQuery] int? clientId = null)
    {
        try
        {
            var response = await _demoDataService.GetPaymentsAsync(clientId);
            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payments");
            return StatusCode(500, new { success = false, message = "Failed to load payments" });
        }
    }
}
