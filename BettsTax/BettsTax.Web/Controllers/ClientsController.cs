using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Exposes client directory information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IDemoDataService demoDataService,
        ILogger<ClientsController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve the list of clients available to the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients()
    {
        try
        {
            var clients = await _demoDataService.GetClientsAsync();
            return Ok(new { success = true, data = clients });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve clients");
            return StatusCode(500, new { success = false, message = "Failed to load clients" });
        }
    }
}
