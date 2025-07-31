using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;
using PaymentTransaction = BettsTax.Data.Models.PaymentTransaction;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using PaymentGatewayType = BettsTax.Data.Models.PaymentGatewayType;
using PaymentGatewayConfig = BettsTax.Data.Models.PaymentGatewayConfig;
using PaymentWebhookLog = BettsTax.Data.Models.PaymentWebhookLog;
using PaymentTransactionLog = BettsTax.Data.Models.PaymentTransactionLog;
using WebhookEventType = BettsTax.Data.Models.WebhookEventType;

namespace BettsTax.Core.Services;

/// <summary>
/// Payment webhook handling service for real-time payment updates
/// Processes webhooks from Orange Money, Africell Money, and other payment providers
/// </summary>
public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentWebhookService> _logger;
    private readonly IPaymentEncryptionService _encryptionService;
    private readonly IPaymentGatewayService _paymentGatewayService;

    public PaymentWebhookService(
        ApplicationDbContext context,
        ILogger<PaymentWebhookService> logger,
        IPaymentEncryptionService encryptionService,
        IPaymentGatewayService paymentGatewayService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
        _paymentGatewayService = paymentGatewayService;
    }

    #region Webhook Processing

    public async Task<bool> ProcessWebhookAsync(
        int gatewayConfigId, 
        string requestBody, 
        Dictionary<string, string> headers, 
        string ipAddress, 
        string userAgent)
    {
        PaymentWebhookLog? webhookLog = null;
        
        try
        {
            // Get gateway configuration
            var gatewayConfig = await _context.PaymentGatewayConfigs
                .FirstOrDefaultAsync(c => c.Id == gatewayConfigId);

            if (gatewayConfig == null)
            {
                _logger.LogError("Gateway configuration {GatewayConfigId} not found for webhook processing", gatewayConfigId);
                return false;
            }

            // Parse webhook event type from request body
            var eventType = await DetermineWebhookEventTypeAsync(requestBody, gatewayConfig.GatewayType);

            // Log the incoming webhook
            webhookLog = new PaymentWebhookLog
            {
                GatewayConfigId = gatewayConfigId,
                EventType = eventType,
                RequestBody = requestBody,
                RequestHeaders = JsonSerializer.Serialize(headers),
                ReceivedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsProcessed = false,
                ResponseStatusCode = 200
            };

            _context.PaymentGatewayWebhookLogs.Add(webhookLog);
            await _context.SaveChangesAsync();

            // Validate webhook signature if present
            var signatureValid = await ValidateWebhookSignatureAsync(gatewayConfigId, requestBody, 
                headers.GetValueOrDefault("X-Signature", "") ?? headers.GetValueOrDefault("Authorization", ""));

            webhookLog.IsSignatureValid = signatureValid;
            webhookLog.SignatureHeader = headers.GetValueOrDefault("X-Signature") ?? headers.GetValueOrDefault("Authorization");

            if (!signatureValid)
            {
                _logger.LogWarning("Invalid webhook signature for gateway {GatewayType} from IP {IpAddress}", 
                    gatewayConfig.GatewayType, ipAddress);
                
                webhookLog.ProcessingError = "Invalid webhook signature";
                await _context.SaveChangesAsync();
                return false;
            }

            // Process webhook based on event type
            var success = await ProcessWebhookEventAsync(webhookLog, requestBody, gatewayConfig);

            // Update webhook log
            webhookLog.IsProcessed = true;
            webhookLog.ProcessedAt = DateTime.UtcNow;

            if (!success)
            {
                webhookLog.ProcessingError = "Failed to process webhook event";
                webhookLog.ResponseStatusCode = 500;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Webhook processed successfully. Gateway: {Gateway}, EventType: {EventType}, Success: {Success}",
                gatewayConfig.GatewayType, eventType, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook for gateway {GatewayConfigId}", gatewayConfigId);

            if (webhookLog != null)
            {
                webhookLog.ProcessingError = ex.Message;
                webhookLog.ResponseStatusCode = 500;
                await _context.SaveChangesAsync();
            }

            return false;
        }
    }

    public async Task<bool> ValidateWebhookSignatureAsync(int gatewayConfigId, string requestBody, string signature)
    {
        try
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            var gatewayConfig = await _context.PaymentGatewayConfigs
                .FirstOrDefaultAsync(c => c.Id == gatewayConfigId);

            if (gatewayConfig == null)
                return false;

            // Decrypt webhook secret
            var webhookSecret = string.IsNullOrEmpty(gatewayConfig.WebhookSecret) 
                ? "" 
                : await _encryptionService.DecryptApiKeyAsync(gatewayConfig.WebhookSecret);

            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("No webhook secret configured for gateway {GatewayType}", gatewayConfig.GatewayType);
                return true; // Allow webhooks without signature if no secret is configured
            }

            // Clean signature (remove prefixes like "sha256=")
            var cleanSignature = signature.Replace("sha256=", "").Replace("Bearer ", "");

            // Verify signature
            var isValid = await _encryptionService.VerifyWebhookSignatureAsync(requestBody, cleanSignature, webhookSecret);

            _logger.LogDebug("Webhook signature validation result: {IsValid} for gateway {GatewayType}", 
                isValid, gatewayConfig.GatewayType);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate webhook signature for gateway {GatewayConfigId}", gatewayConfigId);
            return false;
        }
    }

    public async Task<PaymentTransactionDto?> ProcessPaymentWebhookAsync(
        WebhookEventType eventType, 
        string transactionReference, 
        string webhookData)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionReference || 
                                         t.ExternalReference == transactionReference);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for webhook. Reference: {Reference}", transactionReference);
                return null;
            }

            var previousStatus = transaction.Status;

            // Update transaction based on webhook event
            switch (eventType)
            {
                case WebhookEventType.PaymentCompleted:
                    await HandlePaymentCompletedWebhookAsync(transactionReference, "", webhookData);
                    break;

                case WebhookEventType.PaymentFailed:
                    await HandlePaymentFailedWebhookAsync(transactionReference, "WEBHOOK_FAILED", "Payment failed via webhook");
                    break;

                case WebhookEventType.PaymentInitiated:
                    transaction.Status = PaymentTransactionStatus.Pending;
                    transaction.StatusMessage = "Payment initiated via webhook";
                    break;

                case WebhookEventType.PaymentRefunded:
                    transaction.Status = PaymentTransactionStatus.Refunded;
                    transaction.StatusMessage = "Payment refunded via webhook";
                    break;

                default:
                    _logger.LogWarning("Unhandled webhook event type: {EventType}", eventType);
                    return null;
            }

            // Store webhook data
            transaction.WebhookData = webhookData;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Processed webhook for transaction {Reference}. Event: {EventType}, Status: {PreviousStatus} -> {NewStatus}",
                transactionReference, eventType, previousStatus, transaction.Status);

            return await _paymentGatewayService.GetTransactionAsync(transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment webhook for transaction {Reference}", transactionReference);
            return null;
        }
    }

    public async Task<bool> HandlePaymentCompletedWebhookAsync(
        string transactionReference, 
        string externalReference, 
        string webhookData)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionReference || 
                                         t.ExternalReference == transactionReference);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for completion webhook. Reference: {Reference}", transactionReference);
                return false;
            }

            // Update transaction status
            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Completed;
            transaction.StatusMessage = "Payment completed via webhook";
            transaction.CompletedAt = DateTime.UtcNow;
            transaction.WebhookData = webhookData;

            if (!string.IsNullOrEmpty(externalReference))
                transaction.ExternalReference = externalReference;

            // Parse additional data from webhook
            await ParseWebhookDataAsync(transaction, webhookData);

            await _context.SaveChangesAsync();

            // Log transaction update
            var log = new PaymentTransactionLog
            {
                TransactionId = transaction.Id,
                Action = "WEBHOOK_COMPLETED",
                PreviousStatus = previousStatus,
                NewStatus = PaymentTransactionStatus.Completed,
                Details = $"Payment completed via webhook. External reference: {externalReference}",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Payment completed via webhook. Transaction: {Reference}, External: {ExternalReference}",
                transactionReference, externalReference);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle payment completion webhook for transaction {Reference}", transactionReference);
            return false;
        }
    }

    public async Task<bool> HandlePaymentFailedWebhookAsync(
        string transactionReference, 
        string errorCode, 
        string errorMessage)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionReference || 
                                         t.ExternalReference == transactionReference);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for failure webhook. Reference: {Reference}", transactionReference);
                return false;
            }

            // Update transaction status
            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.StatusMessage = errorMessage;
            transaction.ErrorCode = errorCode;
            transaction.FailedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log transaction update
            var log = new PaymentTransactionLog
            {
                TransactionId = transaction.Id,
                Action = "WEBHOOK_FAILED",
                PreviousStatus = previousStatus,
                NewStatus = PaymentTransactionStatus.Failed,
                Details = $"Payment failed via webhook. Error: {errorCode} - {errorMessage}",
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Payment failed via webhook. Transaction: {Reference}, Error: {ErrorCode} - {ErrorMessage}",
                transactionReference, errorCode, errorMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle payment failure webhook for transaction {Reference}", transactionReference);
            return false;
        }
    }

    #endregion

    #region Webhook Logs

    public async Task<PaymentWebhookLogDto> LogWebhookAsync(
        int gatewayConfigId, 
        WebhookEventType eventType, 
        string requestBody, 
        Dictionary<string, string> headers, 
        string ipAddress, 
        string userAgent)
    {
        try
        {
            var webhookLog = new PaymentWebhookLog
            {
                GatewayConfigId = gatewayConfigId,
                EventType = eventType,
                RequestBody = requestBody,
                RequestHeaders = JsonSerializer.Serialize(headers),
                ReceivedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsProcessed = false,
                ResponseStatusCode = 200
            };

            _context.PaymentGatewayWebhookLogs.Add(webhookLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Logged webhook for gateway {GatewayConfigId}, event {EventType}", 
                gatewayConfigId, eventType);

            return new PaymentWebhookLogDto
            {
                Id = webhookLog.Id,
                GatewayConfigId = webhookLog.GatewayConfigId,
                EventType = webhookLog.EventType,
                EventTypeName = webhookLog.EventType.ToString(),
                TransactionReference = webhookLog.TransactionReference,
                RequestBody = webhookLog.RequestBody,
                RequestHeaders = webhookLog.RequestHeaders,
                ResponseBody = webhookLog.ResponseBody,
                ResponseStatusCode = webhookLog.ResponseStatusCode,
                IsProcessed = webhookLog.IsProcessed,
                ProcessedAt = webhookLog.ProcessedAt,
                ProcessingError = webhookLog.ProcessingError,
                ReceivedAt = webhookLog.ReceivedAt,
                IpAddress = webhookLog.IpAddress,
                UserAgent = webhookLog.UserAgent,
                SignatureHeader = webhookLog.SignatureHeader,
                IsSignatureValid = webhookLog.IsSignatureValid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log webhook for gateway {GatewayConfigId}", gatewayConfigId);
            throw new InvalidOperationException("Failed to log webhook", ex);
        }
    }

    public async Task<PaymentWebhookLogDto> GetWebhookLogAsync(int logId)
    {
        try
        {
            var webhookLog = await _context.PaymentGatewayWebhookLogs
                .Include(w => w.GatewayConfig)
                .FirstOrDefaultAsync(w => w.Id == logId);

            if (webhookLog == null)
                throw new InvalidOperationException($"Webhook log with ID {logId} not found");

            return new PaymentWebhookLogDto
            {
                Id = webhookLog.Id,
                GatewayConfigId = webhookLog.GatewayConfigId,
                GatewayName = webhookLog.GatewayConfig?.Name ?? "",
                EventType = webhookLog.EventType,
                EventTypeName = webhookLog.EventType.ToString(),
                TransactionReference = webhookLog.TransactionReference,
                RequestBody = webhookLog.RequestBody,
                RequestHeaders = webhookLog.RequestHeaders,
                ResponseBody = webhookLog.ResponseBody,
                ResponseStatusCode = webhookLog.ResponseStatusCode,
                IsProcessed = webhookLog.IsProcessed,
                ProcessedAt = webhookLog.ProcessedAt,
                ProcessingError = webhookLog.ProcessingError,
                ReceivedAt = webhookLog.ReceivedAt,
                IpAddress = webhookLog.IpAddress,
                UserAgent = webhookLog.UserAgent,
                SignatureHeader = webhookLog.SignatureHeader,
                IsSignatureValid = webhookLog.IsSignatureValid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook log {LogId}", logId);
            throw new InvalidOperationException("Failed to retrieve webhook log", ex);
        }
    }

    public async Task<List<PaymentWebhookLogDto>> GetWebhookLogsAsync(PaymentWebhookSearchDto search)
    {
        try
        {
            var query = _context.PaymentGatewayWebhookLogs
                .Include(w => w.GatewayConfig)
                .AsQueryable();

            // Apply filters
            if (search.GatewayConfigId.HasValue)
                query = query.Where(w => w.GatewayConfigId == search.GatewayConfigId.Value);

            if (search.EventType.HasValue)
                query = query.Where(w => w.EventType == search.EventType.Value);

            if (!string.IsNullOrEmpty(search.TransactionReference))
                query = query.Where(w => w.TransactionReference != null && 
                                        w.TransactionReference.Contains(search.TransactionReference));

            if (search.IsProcessed.HasValue)
                query = query.Where(w => w.IsProcessed == search.IsProcessed.Value);

            if (search.IsSignatureValid.HasValue)
                query = query.Where(w => w.IsSignatureValid == search.IsSignatureValid.Value);

            if (search.FromDate.HasValue)
                query = query.Where(w => w.ReceivedAt >= search.FromDate.Value);

            if (search.ToDate.HasValue)
                query = query.Where(w => w.ReceivedAt <= search.ToDate.Value);

            if (!string.IsNullOrEmpty(search.IpAddress))
                query = query.Where(w => w.IpAddress != null && w.IpAddress.Contains(search.IpAddress));

            // Apply sorting
            query = search.SortBy.ToLower() switch
            {
                "eventtype" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(w => w.EventType) 
                    : query.OrderByDescending(w => w.EventType),
                "processed" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(w => w.IsProcessed) 
                    : query.OrderByDescending(w => w.IsProcessed),
                "gateway" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(w => w.GatewayConfig!.Name) 
                    : query.OrderByDescending(w => w.GatewayConfig!.Name),
                _ => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(w => w.ReceivedAt) 
                    : query.OrderByDescending(w => w.ReceivedAt)
            };

            // Apply pagination
            var webhookLogs = await query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .ToListAsync();

            var results = webhookLogs.Select(w => new PaymentWebhookLogDto
            {
                Id = w.Id,
                GatewayConfigId = w.GatewayConfigId,
                GatewayName = w.GatewayConfig?.Name ?? "",
                EventType = w.EventType,
                EventTypeName = w.EventType.ToString(),
                TransactionReference = w.TransactionReference,
                RequestBody = w.RequestBody,
                RequestHeaders = w.RequestHeaders,
                ResponseBody = w.ResponseBody,
                ResponseStatusCode = w.ResponseStatusCode,
                IsProcessed = w.IsProcessed,
                ProcessedAt = w.ProcessedAt,
                ProcessingError = w.ProcessingError,
                ReceivedAt = w.ReceivedAt,
                IpAddress = w.IpAddress,
                UserAgent = w.UserAgent,
                SignatureHeader = w.SignatureHeader,
                IsSignatureValid = w.IsSignatureValid
            }).ToList();

            _logger.LogDebug("Retrieved {Count} webhook logs for search criteria", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search webhook logs");
            throw new InvalidOperationException("Webhook log search failed", ex);
        }
    }

    public async Task<bool> MarkWebhookProcessedAsync(int logId, bool success, string? errorMessage = null)
    {
        try
        {
            var webhookLog = await _context.PaymentGatewayWebhookLogs
                .FirstOrDefaultAsync(w => w.Id == logId);

            if (webhookLog == null)
                return false;

            webhookLog.IsProcessed = true;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            webhookLog.ResponseStatusCode = success ? 200 : 500;

            if (!success && !string.IsNullOrEmpty(errorMessage))
                webhookLog.ProcessingError = errorMessage;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Marked webhook {LogId} as processed. Success: {Success}", logId, success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark webhook {LogId} as processed", logId);
            return false;
        }
    }

    public async Task<bool> ReprocessWebhookAsync(int logId, string reprocessedBy)
    {
        try
        {
            var webhookLog = await _context.PaymentGatewayWebhookLogs
                .Include(w => w.GatewayConfig)
                .FirstOrDefaultAsync(w => w.Id == logId);

            if (webhookLog == null)
                return false;

            // Reset processing status
            webhookLog.IsProcessed = false;
            webhookLog.ProcessedAt = null;
            webhookLog.ProcessingError = null;
            webhookLog.ResponseStatusCode = 200;

            await _context.SaveChangesAsync();

            // Reprocess webhook
            var headers = string.IsNullOrEmpty(webhookLog.RequestHeaders) 
                ? new Dictionary<string, string>() 
                : JsonSerializer.Deserialize<Dictionary<string, string>>(webhookLog.RequestHeaders) ?? new Dictionary<string, string>();

            var success = await ProcessWebhookEventAsync(webhookLog, webhookLog.RequestBody, webhookLog.GatewayConfig);

            webhookLog.IsProcessed = true;
            webhookLog.ProcessedAt = DateTime.UtcNow;

            if (!success)
            {
                webhookLog.ProcessingError = $"Reprocessing failed - initiated by {reprocessedBy}";
                webhookLog.ResponseStatusCode = 500;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook {LogId} reprocessed by {ReprocessedBy}. Success: {Success}", 
                logId, reprocessedBy, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reprocess webhook {LogId}", logId);
            return false;
        }
    }

    #endregion

    #region Webhook Management

    public async Task<bool> TestWebhookEndpointAsync(int gatewayConfigId)
    {
        try
        {
            var gatewayConfig = await _context.PaymentGatewayConfigs
                .FirstOrDefaultAsync(c => c.Id == gatewayConfigId);

            if (gatewayConfig == null)
                return false;

            // Create test webhook payload
            var testPayload = new
            {
                eventType = "test",
                transactionId = "test_transaction",
                amount = 1000,
                currency = "SLE",
                status = "completed",
                timestamp = DateTime.UtcNow.ToString("O")
            };

            var testBody = JsonSerializer.Serialize(testPayload);

            // Test webhook processing
            var testHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "User-Agent", "BettsTax-Webhook-Test/1.0" }
            };

            // Generate test signature if webhook secret is configured
            if (!string.IsNullOrEmpty(gatewayConfig.WebhookSecret))
            {
                var webhookSecret = await _encryptionService.DecryptApiKeyAsync(gatewayConfig.WebhookSecret);
                var signature = await _encryptionService.SignWebhookPayloadAsync(testBody, webhookSecret);
                testHeaders.Add("X-Signature", $"sha256={signature}");
            }

            var success = await ProcessWebhookAsync(gatewayConfigId, testBody, testHeaders, "127.0.0.1", "Test");

            _logger.LogInformation("Webhook endpoint test for gateway {GatewayConfigId}: {Success}", 
                gatewayConfigId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test webhook endpoint for gateway {GatewayConfigId}", gatewayConfigId);
            return false;
        }
    }

    public async Task<string> GenerateWebhookSignatureAsync(int gatewayConfigId, string payload)
    {
        try
        {
            var gatewayConfig = await _context.PaymentGatewayConfigs
                .FirstOrDefaultAsync(c => c.Id == gatewayConfigId);

            if (gatewayConfig == null || string.IsNullOrEmpty(gatewayConfig.WebhookSecret))
                return "";

            var webhookSecret = await _encryptionService.DecryptApiKeyAsync(gatewayConfig.WebhookSecret);
            var signature = await _encryptionService.SignWebhookPayloadAsync(payload, webhookSecret);

            _logger.LogDebug("Generated webhook signature for gateway {GatewayConfigId}", gatewayConfigId);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate webhook signature for gateway {GatewayConfigId}", gatewayConfigId);
            return "";
        }
    }

    public async Task<List<PaymentWebhookLogDto>> GetFailedWebhooksAsync(int? gatewayConfigId = null)
    {
        try
        {
            var query = _context.PaymentGatewayWebhookLogs
                .Include(w => w.GatewayConfig)
                .Where(w => !w.IsProcessed || w.ResponseStatusCode >= 400);

            if (gatewayConfigId.HasValue)
                query = query.Where(w => w.GatewayConfigId == gatewayConfigId.Value);

            var failedWebhooks = await query
                .OrderByDescending(w => w.ReceivedAt)
                .Take(100) // Limit to recent failures
                .ToListAsync();

            var results = failedWebhooks.Select(w => new PaymentWebhookLogDto
            {
                Id = w.Id,
                GatewayConfigId = w.GatewayConfigId,
                GatewayName = w.GatewayConfig?.Name ?? "",
                EventType = w.EventType,
                EventTypeName = w.EventType.ToString(),
                TransactionReference = w.TransactionReference,
                RequestBody = w.RequestBody,
                RequestHeaders = w.RequestHeaders,
                ResponseBody = w.ResponseBody,
                ResponseStatusCode = w.ResponseStatusCode,
                IsProcessed = w.IsProcessed,
                ProcessedAt = w.ProcessedAt,
                ProcessingError = w.ProcessingError,
                ReceivedAt = w.ReceivedAt,
                IpAddress = w.IpAddress,
                UserAgent = w.UserAgent,
                SignatureHeader = w.SignatureHeader,
                IsSignatureValid = w.IsSignatureValid
            }).ToList();

            _logger.LogDebug("Retrieved {Count} failed webhooks", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed webhooks");
            throw new InvalidOperationException("Failed to retrieve failed webhooks", ex);
        }
    }

    public async Task<bool> RetryFailedWebhooksAsync(List<int> webhookLogIds, string retriedBy)
    {
        try
        {
            var failedWebhooks = await _context.PaymentGatewayWebhookLogs
                .Include(w => w.GatewayConfig)
                .Where(w => webhookLogIds.Contains(w.Id))
                .ToListAsync();

            var successCount = 0;

            foreach (var webhook in failedWebhooks)
            {
                try
                {
                    var success = await ReprocessWebhookAsync(webhook.Id, retriedBy);
                    if (success)
                        successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retry webhook {WebhookId}", webhook.Id);
                }
            }

            _logger.LogInformation(
                "Retried {TotalCount} webhooks, {SuccessCount} successful, by {RetriedBy}",
                failedWebhooks.Count, successCount, retriedBy);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry failed webhooks");
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<WebhookEventType> DetermineWebhookEventTypeAsync(string requestBody, PaymentGatewayType gatewayType)
    {
        try
        {
            if (string.IsNullOrEmpty(requestBody))
                return WebhookEventType.PaymentInitiated;

            // Parse JSON to determine event type
            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            // Common patterns for different gateways
            if (root.TryGetProperty("event_type", out var eventType) ||
                root.TryGetProperty("eventType", out eventType) ||
                root.TryGetProperty("type", out eventType))
            {
                var eventTypeString = eventType.GetString()?.ToLower() ?? "";

                return eventTypeString switch
                {
                    "payment.completed" or "completed" or "success" => WebhookEventType.PaymentCompleted,
                    "payment.failed" or "failed" or "error" => WebhookEventType.PaymentFailed,
                    "payment.initiated" or "initiated" or "pending" => WebhookEventType.PaymentInitiated,
                    "payment.refunded" or "refunded" => WebhookEventType.PaymentRefunded,
                    "payment.disputed" or "disputed" => WebhookEventType.PaymentDisputed,
                    "payment.settled" or "settled" => WebhookEventType.PaymentSettled,
                    _ => WebhookEventType.PaymentInitiated
                };
            }

            // Check status field as fallback
            if (root.TryGetProperty("status", out var status))
            {
                var statusString = status.GetString()?.ToLower() ?? "";

                return statusString switch
                {
                    "completed" or "success" or "confirmed" => WebhookEventType.PaymentCompleted,
                    "failed" or "error" or "rejected" => WebhookEventType.PaymentFailed,
                    "pending" or "processing" => WebhookEventType.PaymentInitiated,
                    "refunded" => WebhookEventType.PaymentRefunded,
                    _ => WebhookEventType.PaymentInitiated
                };
            }

            return WebhookEventType.PaymentInitiated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine webhook event type from request body");
            return WebhookEventType.PaymentInitiated;
        }
    }

    private async Task<bool> ProcessWebhookEventAsync(PaymentWebhookLog webhookLog, string requestBody, PaymentGatewayConfig gatewayConfig)
    {
        try
        {
            // Extract transaction reference from webhook data
            var transactionReference = await ExtractTransactionReferenceAsync(requestBody);

            if (string.IsNullOrEmpty(transactionReference))
            {
                _logger.LogWarning("No transaction reference found in webhook data for gateway {Gateway}", 
                    gatewayConfig.GatewayType);
                return false;
            }

            webhookLog.TransactionReference = transactionReference;

            // Process based on event type
            var result = await ProcessPaymentWebhookAsync(webhookLog.EventType, transactionReference, requestBody);

            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook event for gateway {Gateway}", gatewayConfig.GatewayType);
            return false;
        }
    }

    private async Task<string?> ExtractTransactionReferenceAsync(string requestBody)
    {
        try
        {
            if (string.IsNullOrEmpty(requestBody))
                return null;

            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            // Common field names for transaction reference
            var possibleFields = new[] 
            { 
                "transaction_reference", "transactionReference", "reference", 
                "transaction_id", "transactionId", "id", "orderId", "order_id" 
            };

            foreach (var field in possibleFields)
            {
                if (root.TryGetProperty(field, out var property))
                {
                    var value = property.GetString();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract transaction reference from webhook data");
            return null;
        }
    }

    private async Task ParseWebhookDataAsync(PaymentTransaction transaction, string webhookData)
    {
        try
        {
            if (string.IsNullOrEmpty(webhookData))
                return;

            using var document = JsonDocument.Parse(webhookData);
            var root = document.RootElement;

            // Extract provider transaction ID
            if (root.TryGetProperty("provider_transaction_id", out var providerTxnId) ||
                root.TryGetProperty("external_id", out providerTxnId) ||
                root.TryGetProperty("gateway_transaction_id", out providerTxnId))
            {
                var providerId = providerTxnId.GetString();
                if (!string.IsNullOrEmpty(providerId))
                    transaction.ProviderTransactionId = providerId;
            }

            // Extract external reference if not already set
            if (string.IsNullOrEmpty(transaction.ExternalReference))
            {
                if (root.TryGetProperty("external_reference", out var extRef) ||
                    root.TryGetProperty("gateway_reference", out extRef))
                {
                    var reference = extRef.GetString();
                    if (!string.IsNullOrEmpty(reference))
                        transaction.ExternalReference = reference;
                }
            }

            _logger.LogDebug("Parsed webhook data for transaction {TransactionId}", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse webhook data for transaction {TransactionId}", transaction.Id);
        }
    }

    #endregion
}