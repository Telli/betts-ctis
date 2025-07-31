using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Data.Models;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentGatewayController : ControllerBase
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IPaymentWebhookService _webhookService;
    private readonly IPaymentRetryService _retryService;
    private readonly IPaymentFraudDetectionService _fraudService;
    private readonly IMobileMoneyProviderService _mobileMoneyService;
    private readonly IPaymentNotificationService _notificationService;
    private readonly ILogger<PaymentGatewayController> _logger;

    public PaymentGatewayController(
        IPaymentGatewayService paymentGatewayService,
        IPaymentWebhookService webhookService,
        IPaymentRetryService retryService,
        IPaymentFraudDetectionService fraudService,
        IMobileMoneyProviderService mobileMoneyService,
        IPaymentNotificationService notificationService,
        ILogger<PaymentGatewayController> logger)
    {
        _paymentGatewayService = paymentGatewayService;
        _webhookService = webhookService;
        _retryService = retryService;
        _fraudService = fraudService;
        _mobileMoneyService = mobileMoneyService;
        _notificationService = notificationService;
        _logger = logger;
    }

    #region Payment Transactions

    /// <summary>
    /// Initiate a new payment transaction
    /// </summary>
    [HttpPost("transactions")]
    public async Task<ActionResult<PaymentTransactionDto>> InitiatePayment(
        [FromBody] CreatePaymentTransactionDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token");

            // Add IP address and user agent to request
            request.IpAddress = GetClientIpAddress();
            request.UserAgent = Request.Headers["User-Agent"].ToString();

            var result = await _paymentGatewayService.InitiatePaymentAsync(request, userId);

            _logger.LogInformation("Payment initiated successfully. TransactionId: {TransactionId}, ClientId: {ClientId}", 
                result.Id, request.ClientId);

            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid payment request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to initiate payment");
            return BadRequest($"Payment initiation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment initiation");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Get payment transaction details
    /// </summary>
    [HttpGet("transactions/{id}")]
    public async Task<ActionResult<PaymentTransactionDto>> GetTransaction(int id)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            // Check if user has access to this transaction
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            return Ok(transaction);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Transaction not found: {TransactionId}", id);
            return NotFound($"Transaction not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction {TransactionId}", id);
            return StatusCode(500, "Failed to retrieve transaction");
        }
    }

    /// <summary>
    /// Search payment transactions with filters
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<PaymentTransactionDto>>> SearchTransactions(
        [FromQuery] PaymentSearchDto search)
    {
        try
        {
            // Apply user-specific filters for non-admin users
            if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (User.IsInRole("Client"))
                {
                    // Clients can only see their own transactions
                    var clientId = await GetClientIdForUser(userId);
                    search.ClientId = clientId;
                }
            }

            var transactionSearch = new PaymentTransactionSearchDto
            {
                ClientId = search.ClientId,
                GatewayType = search.GatewayType,
                Status = search.Status,
                FromDate = search.FromDate,
                ToDate = search.ToDate
            };
            var transactions = await _paymentGatewayService.SearchTransactionsAsync(transactionSearch);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search transactions");
            return StatusCode(500, "Failed to search transactions");
        }
    }

    /// <summary>
    /// Process a payment (complete the transaction)
    /// </summary>
    [HttpPost("transactions/{id}/process")]
    public async Task<ActionResult<PaymentTransactionDto>> ProcessPayment(int id)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var processDto = new ProcessPaymentDto { TransactionId = id };
            var result = await _paymentGatewayService.ProcessPaymentAsync(processDto, User.Identity?.Name ?? "System");
            
            _logger.LogInformation("Payment processed. TransactionId: {TransactionId}, Status: {Status}", 
                id, result.Status);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot process payment {TransactionId}: {Message}", id, ex.Message);
            return BadRequest($"Cannot process payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment {TransactionId}", id);
            return StatusCode(500, "Payment processing failed");
        }
    }

    /// <summary>
    /// Cancel a payment transaction
    /// </summary>
    [HttpPost("transactions/{id}/cancel")]
    public async Task<ActionResult<PaymentTransactionDto>> CancelPayment(
        int id, 
        [FromBody] CancelPaymentDto request)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _paymentGatewayService.CancelPaymentAsync(id, request.Reason, userId);
            
            _logger.LogInformation("Payment cancelled. TransactionId: {TransactionId}, Reason: {Reason}", 
                id, request.Reason);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot cancel payment {TransactionId}: {Message}", id, ex.Message);
            return BadRequest($"Cannot cancel payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel payment {TransactionId}", id);
            return StatusCode(500, "Payment cancellation failed");
        }
    }

    /// <summary>
    /// Refund a completed payment
    /// </summary>
    [HttpPost("transactions/{id}/refund")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<PaymentTransactionDto>> RefundPayment(
        int id, 
        [FromBody] RefundPaymentDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _paymentGatewayService.RefundPaymentAsync(id, request.Amount, request.Reason, userId);
            
            _logger.LogInformation("Payment refunded. TransactionId: {TransactionId}, Amount: {Amount}, Reason: {Reason}", 
                id, request.Amount, request.Reason);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot refund payment {TransactionId}: {Message}", id, ex.Message);
            return BadRequest($"Cannot refund payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {TransactionId}", id);
            return StatusCode(500, "Payment refund failed");
        }
    }

    /// <summary>
    /// Get transaction logs and audit trail
    /// </summary>
    [HttpGet("transactions/{id}/logs")]
    public async Task<ActionResult<List<PaymentTransactionLogDto>>> GetTransactionLogs(int id)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var logs = await _paymentGatewayService.GetTransactionLogsAsync(id);
            return Ok(logs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Transaction not found: {TransactionId}", id);
            return NotFound($"Transaction not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction logs {TransactionId}", id);
            return StatusCode(500, "Failed to retrieve transaction logs");
        }
    }

    #endregion

    #region Mobile Money Integration

    /// <summary>
    /// Process Orange Money payment
    /// </summary>
    [HttpPost("mobile-money/orange/{transactionId}")]
    public async Task<ActionResult<PaymentTransactionDto>> ProcessOrangeMoneyPayment(
        int transactionId,
        [FromBody] MobileMoneyPaymentDto request)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(transactionId);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            if (transaction.GatewayType != PaymentGatewayType.OrangeMoney)
                return BadRequest("Transaction is not configured for Orange Money");

            var result = await _mobileMoneyService.ProcessOrangeMoneyPaymentAsync(transactionId, request.Pin);
            
            _logger.LogInformation("Orange Money payment processed. TransactionId: {TransactionId}, Status: {Status}", 
                transactionId, result.Status);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid Orange Money PIN for transaction {TransactionId}: {Message}", transactionId, ex.Message);
            return BadRequest($"Invalid PIN: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot process Orange Money payment {TransactionId}: {Message}", transactionId, ex.Message);
            return BadRequest($"Cannot process payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Orange Money payment {TransactionId}", transactionId);
            return StatusCode(500, "Orange Money payment processing failed");
        }
    }

    /// <summary>
    /// Process Africell Money payment
    /// </summary>
    [HttpPost("mobile-money/africell/{transactionId}")]
    public async Task<ActionResult<PaymentTransactionDto>> ProcessAfricellMoneyPayment(
        int transactionId,
        [FromBody] MobileMoneyPaymentDto request)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(transactionId);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            if (transaction.GatewayType != PaymentGatewayType.AfricellMoney)
                return BadRequest("Transaction is not configured for Africell Money");

            var result = await _mobileMoneyService.ProcessAfricellMoneyPaymentAsync(transactionId, request.Pin);
            
            _logger.LogInformation("Africell Money payment processed. TransactionId: {TransactionId}, Status: {Status}", 
                transactionId, result.Status);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid Africell Money PIN for transaction {TransactionId}: {Message}", transactionId, ex.Message);
            return BadRequest($"Invalid PIN: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot process Africell Money payment {TransactionId}: {Message}", transactionId, ex.Message);
            return BadRequest($"Cannot process payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Africell Money payment {TransactionId}", transactionId);
            return StatusCode(500, "Africell Money payment processing failed");
        }
    }

    /// <summary>
    /// Validate mobile money phone number
    /// </summary>
    [HttpPost("mobile-money/validate-phone")]
    public async Task<ActionResult<PhoneValidationResultDto>> ValidatePhoneNumber(
        [FromBody] ValidatePhoneDto request)
    {
        try
        {
            var result = await _mobileMoneyService.ValidatePhoneNumberAsync(request.PhoneNumber);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid phone number validation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate phone number {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, "Phone number validation failed");
        }
    }

    /// <summary>
    /// Get mobile money provider status
    /// </summary>
    [HttpGet("mobile-money/status")]
    public async Task<ActionResult<List<MobileMoneyProviderStatusDto>>> GetProviderStatus()
    {
        try
        {
            var statuses = new List<object>
            {
                new { Provider = "OrangeMoney", Status = await _mobileMoneyService.GetProviderStatusAsync(PaymentGatewayType.OrangeMoney) },
                new { Provider = "AfricellMoney", Status = await _mobileMoneyService.GetProviderStatusAsync(PaymentGatewayType.AfricellMoney) }
            };
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mobile money provider status");
            return StatusCode(500, "Failed to retrieve provider status");
        }
    }

    #endregion

    #region Webhooks

    /// <summary>
    /// Process incoming payment webhook
    /// </summary>
    [HttpPost("webhooks/{gatewayConfigId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook(int gatewayConfigId)
    {
        try
        {
            // Read request body
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            // Extract headers
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var success = await _webhookService.ProcessWebhookAsync(
                gatewayConfigId, requestBody, headers, ipAddress, userAgent);

            if (success)
            {
                _logger.LogInformation("Webhook processed successfully for gateway config {GatewayConfigId}", gatewayConfigId);
                return Ok(new { status = "success", message = "Webhook processed successfully" });
            }
            else
            {
                _logger.LogWarning("Webhook processing failed for gateway config {GatewayConfigId}", gatewayConfigId);
                return BadRequest(new { status = "error", message = "Webhook processing failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error for gateway config {GatewayConfigId}", gatewayConfigId);
            return StatusCode(500, new { status = "error", message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get webhook logs for debugging
    /// </summary>
    [HttpGet("webhooks/logs")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<PaymentWebhookLogDto>>> GetWebhookLogs(
        [FromQuery] PaymentWebhookSearchDto search)
    {
        try
        {
            var logs = await _webhookService.GetWebhookLogsAsync(search);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook logs");
            return StatusCode(500, "Failed to retrieve webhook logs");
        }
    }

    /// <summary>
    /// Reprocess failed webhook
    /// </summary>
    [HttpPost("webhooks/logs/{logId}/reprocess")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> ReprocessWebhook(int logId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _webhookService.ReprocessWebhookAsync(logId, userId);

            if (success)
            {
                _logger.LogInformation("Webhook {LogId} reprocessed successfully by {UserId}", logId, userId);
                return Ok(new { status = "success", message = "Webhook reprocessed successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to reprocess webhook" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reprocess webhook {LogId}", logId);
            return StatusCode(500, "Webhook reprocessing failed");
        }
    }

    /// <summary>
    /// Test webhook endpoint
    /// </summary>
    [HttpPost("webhooks/test/{gatewayConfigId}")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> TestWebhookEndpoint(int gatewayConfigId)
    {
        try
        {
            var success = await _webhookService.TestWebhookEndpointAsync(gatewayConfigId);

            if (success)
            {
                return Ok(new { status = "success", message = "Webhook endpoint test successful" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Webhook endpoint test failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook endpoint for gateway config {GatewayConfigId}", gatewayConfigId);
            return StatusCode(500, "Webhook endpoint test failed");
        }
    }

    #endregion

    #region Retry Management

    /// <summary>
    /// Retry a failed payment
    /// </summary>
    [HttpPost("transactions/{id}/retry")]
    public async Task<ActionResult<PaymentRetryResultDto>> RetryPayment(int id)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _retryService.RetryPaymentAsync(id, userId);

            _logger.LogInformation("Payment retry initiated. TransactionId: {TransactionId}, Success: {Success}", 
                id, result.Success);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot retry payment {TransactionId}: {Message}", id, ex.Message);
            return BadRequest($"Cannot retry payment: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry payment {TransactionId}", id);
            return StatusCode(500, "Payment retry failed");
        }
    }

    /// <summary>
    /// Get retry attempts for a transaction
    /// </summary>
    [HttpGet("transactions/{id}/retry-attempts")]
    public async Task<ActionResult<List<PaymentRetryAttemptDto>>> GetRetryAttempts(int id)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var attempts = await _retryService.GetRetryAttemptsAsync(id);
            return Ok(attempts);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Transaction not found: {TransactionId}", id);
            return NotFound($"Transaction not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retry attempts for transaction {TransactionId}", id);
            return StatusCode(500, "Failed to retrieve retry attempts");
        }
    }

    /// <summary>
    /// Schedule a payment retry
    /// </summary>
    [HttpPost("transactions/{id}/schedule-retry")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public async Task<IActionResult> ScheduleRetry(int id, [FromBody] ScheduleRetryDto request)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _retryService.ScheduleRetryAsync(id, request.ScheduledAt, userId);

            if (success)
            {
                _logger.LogInformation("Payment retry scheduled. TransactionId: {TransactionId}, ScheduledAt: {ScheduledAt}", 
                    id, request.ScheduledAt);
                return Ok(new { status = "success", message = "Retry scheduled successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to schedule retry" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule retry for transaction {TransactionId}", id);
            return StatusCode(500, "Failed to schedule retry");
        }
    }

    /// <summary>
    /// Get retry statistics
    /// </summary>
    [HttpGet("retry/statistics")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<RetryStatisticsDto>> GetRetryStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var stats = await _retryService.GetRetryStatisticsAsync(from, to);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retry statistics");
            return StatusCode(500, "Failed to retrieve retry statistics");
        }
    }

    /// <summary>
    /// Get dead letter queue
    /// </summary>
    [HttpGet("retry/dead-letter-queue")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<PaymentDeadLetterQueueDto>>> GetDeadLetterQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var records = await _retryService.GetDeadLetterQueueAsync(page, pageSize);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dead letter queue");
            return StatusCode(500, "Failed to retrieve dead letter queue");
        }
    }

    /// <summary>
    /// Process dead letter queue item
    /// </summary>
    [HttpPost("retry/dead-letter-queue/{id}/process")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> ProcessDeadLetter(int id, [FromBody] ProcessDeadLetterDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _retryService.ProcessDeadLetterAsync(id, request.Action, userId);

            if (success)
            {
                _logger.LogInformation("Dead letter record {Id} processed with action {Action} by {UserId}", 
                    id, request.Action, userId);
                return Ok(new { status = "success", message = "Dead letter record processed successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to process dead letter record" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process dead letter record {Id}", id);
            return StatusCode(500, "Failed to process dead letter record");
        }
    }

    #endregion

    #region Fraud Detection and Security

    /// <summary>
    /// Get transaction risk factors
    /// </summary>
    [HttpGet("transactions/{id}/risk-factors")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public async Task<ActionResult<List<string>>> GetTransactionRiskFactors(int id)
    {
        try
        {
            var riskFactors = await _fraudService.GetRiskFactorsAsync(id);
            return Ok(riskFactors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risk factors for transaction {TransactionId}", id);
            return StatusCode(500, "Failed to retrieve risk factors");
        }
    }

    /// <summary>
    /// Check if transaction is suspicious
    /// </summary>
    [HttpGet("transactions/{id}/suspicious")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public async Task<ActionResult<bool>> IsTransactionSuspicious(int id)
    {
        try
        {
            var isSuspicious = await _fraudService.IsTransactionSuspiciousAsync(id);
            return Ok(new { transactionId = id, isSuspicious });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if transaction {TransactionId} is suspicious", id);
            return StatusCode(500, "Failed to check transaction");
        }
    }

    /// <summary>
    /// Block a suspicious transaction
    /// </summary>
    [HttpPost("transactions/{id}/block")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> BlockTransaction(int id, [FromBody] BlockTransactionDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _fraudService.BlockTransactionAsync(id, request.Reason, userId);

            if (success)
            {
                _logger.LogWarning("Transaction {TransactionId} blocked by {UserId}. Reason: {Reason}", 
                    id, userId, request.Reason);
                return Ok(new { status = "success", message = "Transaction blocked successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to block transaction" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block transaction {TransactionId}", id);
            return StatusCode(500, "Failed to block transaction");
        }
    }

    /// <summary>
    /// Flag a transaction for manual review
    /// </summary>
    [HttpPost("transactions/{id}/flag")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public async Task<IActionResult> FlagTransaction(int id, [FromBody] FlagTransactionDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _fraudService.FlagTransactionAsync(id, request.Reason, userId);

            if (success)
            {
                _logger.LogInformation("Transaction {TransactionId} flagged by {UserId}. Reason: {Reason}", 
                    id, userId, request.Reason);
                return Ok(new { status = "success", message = "Transaction flagged successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to flag transaction" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flag transaction {TransactionId}", id);
            return StatusCode(500, "Failed to flag transaction");
        }
    }

    /// <summary>
    /// Review a flagged transaction
    /// </summary>
    [HttpPost("transactions/{id}/review")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> ReviewTransaction(int id, [FromBody] ReviewTransactionDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _fraudService.ReviewTransactionAsync(id, request.Approve, request.ReviewNotes, userId);

            if (success)
            {
                _logger.LogInformation("Transaction {TransactionId} reviewed by {UserId}. Decision: {Decision}", 
                    id, userId, request.Approve ? "Approved" : "Rejected");
                return Ok(new { status = "success", message = "Transaction reviewed successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to review transaction" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review transaction {TransactionId}", id);
            return StatusCode(500, "Failed to review transaction");
        }
    }

    /// <summary>
    /// Get high-risk transactions
    /// </summary>
    [HttpGet("security/high-risk-transactions")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<PaymentTransactionDto>>> GetHighRiskTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var transactions = await _fraudService.GetHighRiskTransactionsAsync(page, pageSize);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get high-risk transactions");
            return StatusCode(500, "Failed to retrieve high-risk transactions");
        }
    }

    /// <summary>
    /// Get transactions requiring manual review
    /// </summary>
    [HttpGet("security/pending-review")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<PaymentTransactionDto>>> GetTransactionsRequiringReview(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var transactions = await _fraudService.GetTransactionsRequiringReviewAsync(page, pageSize);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions requiring review");
            return StatusCode(500, "Failed to retrieve transactions requiring review");
        }
    }

    /// <summary>
    /// Get security dashboard statistics
    /// </summary>
    [HttpGet("security/dashboard")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<SecurityStatsDto>> GetSecurityDashboard(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var stats = await _fraudService.GetSecurityDashboardAsync(from, to);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security dashboard");
            return StatusCode(500, "Failed to retrieve security dashboard");
        }
    }

    #endregion

    #region Notifications

    /// <summary>
    /// Send payment notification manually
    /// </summary>
    [HttpPost("transactions/{id}/notify")]
    [Authorize(Roles = "Admin,SystemAdmin,Associate")]
    public async Task<IActionResult> SendPaymentNotification(int id, [FromBody] SendNotificationDto request)
    {
        try
        {
            var transaction = await _paymentGatewayService.GetTransactionAsync(id);
            
            if (!await CanAccessTransaction(transaction))
                return Forbid("Access denied to this transaction");

            bool success = false;

            switch (request.NotificationType.ToLower())
            {
                case "completed":
                    success = await _notificationService.SendPaymentCompletedNotificationAsync(id);
                    break;
                case "failed":
                    success = await _notificationService.SendPaymentFailedNotificationAsync(id);
                    break;
                case "reminder":
                    success = await _notificationService.SendPaymentReminderAsync(id);
                    break;
                default:
                    return BadRequest("Invalid notification type");
            }

            if (success)
            {
                _logger.LogInformation("Payment notification sent. TransactionId: {TransactionId}, Type: {Type}", 
                    id, request.NotificationType);
                return Ok(new { status = "success", message = "Notification sent successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to send notification" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for transaction {TransactionId}", id);
            return StatusCode(500, "Failed to send notification");
        }
    }

    /// <summary>
    /// Get notification preferences for a client
    /// </summary>
    [HttpGet("clients/{clientId}/notification-preferences")]
    public async Task<ActionResult<PaymentNotificationPreferencesDto>> GetNotificationPreferences(int clientId)
    {
        try
        {
            // Check if user can access this client's data
            if (!await CanAccessClient(clientId))
                return Forbid("Access denied to this client");

            var preferences = await _notificationService.GetNotificationPreferencesAsync(clientId);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification preferences for client {ClientId}", clientId);
            return StatusCode(500, "Failed to retrieve notification preferences");
        }
    }

    /// <summary>
    /// Update notification preferences for a client
    /// </summary>
    [HttpPut("clients/{clientId}/notification-preferences")]
    public async Task<IActionResult> UpdateNotificationPreferences(
        int clientId, 
        [FromBody] UpdateNotificationPreferencesDto request)
    {
        try
        {
            if (!await CanAccessClient(clientId))
                return Forbid("Access denied to this client");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _notificationService.UpdateNotificationPreferencesAsync(clientId, request, userId);

            if (success)
            {
                _logger.LogInformation("Notification preferences updated for client {ClientId} by {UserId}", 
                    clientId, userId);
                return Ok(new { status = "success", message = "Preferences updated successfully" });
            }
            else
            {
                return BadRequest(new { status = "error", message = "Failed to update preferences" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for client {ClientId}", clientId);
            return StatusCode(500, "Failed to update notification preferences");
        }
    }

    #endregion

    #region Private Helper Methods

    private string GetClientIpAddress()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        return ipAddress ?? "Unknown";
    }

    private async Task<bool> CanAccessTransaction(PaymentTransactionDto transaction)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admins and SystemAdmins can access all transactions
        if (userRole == "Admin" || userRole == "SystemAdmin")
            return true;

        // Associates can access transactions for their assigned clients
        if (userRole == "Associate")
        {
            // Implementation would check if associate is assigned to the client
            return true; // Simplified for now
        }

        // Clients can only access their own transactions
        if (userRole == "Client")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var clientId = await GetClientIdForUser(userId);
            return transaction.ClientId == clientId;
        }

        return false;
    }

    private async Task<bool> CanAccessClient(int clientId)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admins and SystemAdmins can access all clients
        if (userRole == "Admin" || userRole == "SystemAdmin")
            return true;

        // Associates can access their assigned clients
        if (userRole == "Associate")
        {
            // Implementation would check if associate is assigned to the client
            return true; // Simplified for now
        }

        // Clients can only access their own data
        if (userRole == "Client")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userClientId = await GetClientIdForUser(userId);
            return clientId == userClientId;
        }

        return false;
    }

    private async Task<int?> GetClientIdForUser(string? userId)
    {
        // This would typically query the database to find the client ID for the user
        // Implementation depends on your user-client relationship structure
        return await Task.FromResult<int?>(null); // Simplified for now
    }

    #endregion
}