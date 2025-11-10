using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Payment;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Payments Controller - Manages payment records
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        Services.IAuthorizationService authorizationService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all payments
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayments([FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId} payments", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var payments = await _paymentService.GetPaymentsAsync(effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved {Count} payments", userId, payments.Count);

            return Ok(new { success = true, data = payments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving payments" });
        }
    }

    /// <summary>
    /// Get payment summary statistics
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentSummary([FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId} payment summary", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var summary = await _paymentService.GetPaymentSummaryAsync(effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved payment summary", userId);

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment summary");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving payment summary" });
        }
    }

    /// <summary>
    /// Create a new payment (Staff/Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto, [FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only staff or admin can create payments
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                _logger.LogWarning("Unauthorized payment creation attempt by user {UserId}", userId);
                return StatusCode(403, new { success = false, message = "Access denied. Only staff or admin can create payments." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid payment data", errors = ModelState });
            }

            var newPayment = await _paymentService.CreatePaymentAsync(dto, clientId);

            _logger.LogInformation("User {UserId} created new payment {PaymentId}", userId, newPayment.Id);

            return StatusCode(201, new { success = true, data = newPayment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { success = false, message = "An error occurred while creating payment" });
        }
    }
}
