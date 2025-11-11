using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Client;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Clients Controller - Manages client data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IClientService clientService,
        Services.IAuthorizationService authorizationService,
        ILogger<ClientsController> logger)
    {
        _clientService = clientService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all clients with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClients(
        [FromQuery] string? search = null,
        [FromQuery] string? segment = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Auto-filter for client users
            int? effectiveClientId = null;
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
                _logger.LogInformation("Client user {UserId} retrieving their own data", userId);
            }

            var clients = await _clientService.GetClientsAsync(search, segment, status, effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved {Count} clients", userId, clients.Count);

            return Ok(new { success = true, data = clients });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving clients" });
        }
    }

    /// <summary>
    /// Get client by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClientById(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, id))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId}", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var client = await _clientService.GetClientByIdAsync(id);

            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            _logger.LogInformation("User {UserId} retrieved client {ClientId}", userId, id);

            return Ok(new { success = true, data = client });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving client" });
        }
    }

    /// <summary>
    /// Create a new client (Staff/Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only staff or admin can create clients
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                _logger.LogWarning("Unauthorized client creation attempt by user {UserId}", userId);
                return StatusCode(403, new { success = false, message = "Access denied. Only staff or admin can create clients." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid client data", errors = ModelState });
            }

            var newClient = await _clientService.CreateClientAsync(dto);

            _logger.LogInformation("User {UserId} created new client {ClientId}", userId, newClient.Id);

            return StatusCode(201, new { success = true, data = newClient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            return StatusCode(500, new { success = false, message = "An error occurred while creating client" });
        }
    }

    /// <summary>
    /// Update an existing client (Staff/Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only staff or admin can update clients
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                _logger.LogWarning("Unauthorized client update attempt by user {UserId} for client {ClientId}", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied. Only staff or admin can update clients." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid client data", errors = ModelState });
            }

            var updatedClient = await _clientService.UpdateClientAsync(id, dto);

            if (updatedClient == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            _logger.LogInformation("User {UserId} updated client {ClientId}", userId, id);

            return Ok(new { success = true, data = updatedClient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating client" });
        }
    }
}
