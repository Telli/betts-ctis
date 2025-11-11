using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Filing;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Filings Controller - Manages tax filings
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilingsController : ControllerBase
{
    private readonly IFilingService _filingService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<FilingsController> _logger;

    public FilingsController(
        IFilingService filingService,
        Services.IAuthorizationService authorizationService,
        ILogger<FilingsController> logger)
    {
        _filingService = filingService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get filing by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFiling(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            // Security check - verify user can access this filing's client data
            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to filing {FilingId}", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            _logger.LogInformation("User {UserId} retrieved filing {FilingId}", userId, id);

            return Ok(new { success = true, data = filing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving filing" });
        }
    }

    /// <summary>
    /// Get schedules for a filing
    /// </summary>
    [HttpGet("{id}/schedules")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilingSchedules(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify filing exists and user has access
            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to filing {FilingId} schedules", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var schedules = await _filingService.GetFilingSchedulesAsync(id);

            _logger.LogInformation("User {UserId} retrieved schedules for filing {FilingId}", userId, id);

            return Ok(new { success = true, data = schedules });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schedules for filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving schedules" });
        }
    }

    /// <summary>
    /// Get documents for a filing
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilingDocuments(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify filing exists and user has access
            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to filing {FilingId} documents", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var documents = await _filingService.GetFilingDocumentsAsync(id);

            _logger.LogInformation("User {UserId} retrieved documents for filing {FilingId}", userId, id);

            return Ok(new { success = true, data = documents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving documents" });
        }
    }

    /// <summary>
    /// Get history for a filing
    /// </summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilingHistory(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify filing exists and user has access
            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to filing {FilingId} history", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var history = await _filingService.GetFilingHistoryAsync(id);

            _logger.LogInformation("User {UserId} retrieved history for filing {FilingId}", userId, id);

            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving history" });
        }
    }

    /// <summary>
    /// Update a filing
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFiling(int id, [FromBody] UpdateFilingDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify filing exists and user has access
            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized update attempt by user {UserId} for filing {FilingId}", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid filing data", errors = ModelState });
            }

            var updatedFiling = await _filingService.UpdateFilingAsync(id, dto);

            _logger.LogInformation("User {UserId} updated filing {FilingId}", userId, id);

            return Ok(new { success = true, data = updatedFiling });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while updating filing" });
        }
    }

    /// <summary>
    /// Submit a filing
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitFiling(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify filing exists and user has access
            var filing = await _filingService.GetFilingByIdAsync(id);
            if (filing == null)
            {
                return NotFound(new { success = false, message = "Filing not found" });
            }

            if (!_authorizationService.CanAccessClientData(User, filing.ClientId))
            {
                _logger.LogWarning("Unauthorized submit attempt by user {UserId} for filing {FilingId}", userId, id);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var submittedFiling = await _filingService.SubmitFilingAsync(id);

            _logger.LogInformation("User {UserId} submitted filing {FilingId}", userId, id);

            return Ok(new { success = true, data = submittedFiling });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting filing {FilingId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while submitting filing" });
        }
    }
}
