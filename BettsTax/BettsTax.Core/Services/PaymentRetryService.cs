using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;
using PaymentTransaction = BettsTax.Data.Models.PaymentTransaction;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using PaymentGatewayType = BettsTax.Data.Models.PaymentGatewayType;
using PaymentScheduledRetry = BettsTax.Data.Models.PaymentScheduledRetry;
using PaymentRetryAttempt = BettsTax.Data.Models.PaymentRetryAttempt;
using PaymentTransactionLog = BettsTax.Data.Models.PaymentTransactionLog;
using PaymentRetryStatus = BettsTax.Data.PaymentRetryStatus;
using CircuitBreakerStatus = BettsTax.Data.CircuitBreakerStatus;
using DeadLetterStatus = BettsTax.Data.DeadLetterStatus;
using PaymentFailureRecord = BettsTax.Data.Models.PaymentFailureRecord;
using PaymentFailureType = BettsTax.Data.PaymentFailureType;
using PaymentDeadLetterQueue = BettsTax.Data.Models.PaymentDeadLetterQueue;

namespace BettsTax.Core.Services;

/// <summary>
/// Payment retry service with intelligent retry mechanisms and failure handling
/// Provides exponential backoff, circuit breaker patterns, and dead letter queue handling
/// Specifically optimized for Sierra Leone mobile money provider reliability
/// </summary>
public class PaymentRetryService : IPaymentRetryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentRetryService> _logger;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IMobileMoneyProviderService _mobileMoneyService;
    private readonly IPaymentNotificationService _notificationService;

    // Retry configuration
    private readonly Dictionary<PaymentGatewayType, RetryConfiguration> _retryConfigs;
    private readonly Dictionary<PaymentGatewayType, CircuitBreakerState> _circuitBreakers;

    public PaymentRetryService(
        ApplicationDbContext context,
        ILogger<PaymentRetryService> logger,
        IPaymentGatewayService paymentGatewayService,
        IMobileMoneyProviderService mobileMoneyService,
        IPaymentNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _paymentGatewayService = paymentGatewayService;
        _mobileMoneyService = mobileMoneyService;
        _notificationService = notificationService;

        // Initialize retry configurations for different gateways
        _retryConfigs = InitializeRetryConfigurations();
        _circuitBreakers = InitializeCircuitBreakers();
    }

    #region Retry Management

    public async Task<PaymentRetryResultDto> RetryPaymentAsync(int transactionId, string retriedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.RetryAttempts)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            var config = _retryConfigs.GetValueOrDefault(transaction.GatewayType);
            if (config == null)
                throw new InvalidOperationException($"No retry configuration found for gateway {transaction.GatewayType}");

            // Check if transaction is eligible for retry
            var eligibilityResult = await CheckRetryEligibilityAsync(transaction, config);
            if (!eligibilityResult.IsEligible)
            {
                return new PaymentRetryResultDto
                {
                    Success = false,
                    TransactionId = transactionId,
                    Message = eligibilityResult.Reason,
                    ShouldRetryAgain = false
                };
            }

            // Check circuit breaker status
            var circuitBreaker = _circuitBreakers[transaction.GatewayType];
            if (circuitBreaker.State == CircuitBreakerStatus.Open)
            {
                return new PaymentRetryResultDto
                {
                    Success = false,
                    TransactionId = transactionId,
                    Message = "Circuit breaker is open - gateway temporarily unavailable",
                    ShouldRetryAgain = true,
                    NextRetryAt = DateTime.UtcNow.AddMinutes(circuitBreaker.RecoveryTimeMinutes)
                };
            }

            // Create retry attempt record
            var retryAttempt = new PaymentRetryAttempt
            {
                TransactionId = transactionId.ToString(),
                AttemptNumber = transaction.RetryAttempts + 1,
                AttemptedAt = DateTime.UtcNow,
                AttemptedBy = retriedBy,
                Status = PaymentRetryStatus.InProgress.ToString()
            };

            _context.PaymentRetryAttempts.Add(retryAttempt);
            await _context.SaveChangesAsync();

            // Calculate retry delay based on exponential backoff
            var delay = CalculateRetryDelay(retryAttempt.AttemptNumber, config);
            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation("Delaying retry for transaction {TransactionId} by {Delay}ms", 
                    transactionId, delay.TotalMilliseconds);
                await Task.Delay(delay);
            }

            // Attempt the retry
            var retryResult = await ExecuteRetryAttemptAsync(transaction, retryAttempt, config);

            // Update circuit breaker based on result
            await UpdateCircuitBreakerAsync(transaction.GatewayType, retryResult.Success);

            // Schedule next retry if needed
            if (!retryResult.Success && retryResult.ShouldRetryAgain)
            {
                await ScheduleNextRetryAsync(transactionId, retryAttempt.AttemptNumber + 1, config);
            }

            return retryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry payment for transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Payment retry failed", ex);
        }
    }

    public async Task<bool> ScheduleRetryAsync(int transactionId, DateTime scheduledAt, string scheduledBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            var scheduledRetry = new PaymentScheduledRetry
            {
                TransactionId = transactionId,
                ScheduledAt = scheduledAt,
                ScheduledBy = scheduledBy,
                Status = PaymentRetryStatus.Scheduled.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentScheduledRetries.Add(scheduledRetry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Scheduled retry for transaction {TransactionId} at {ScheduledAt} by {ScheduledBy}", 
                transactionId, scheduledAt, scheduledBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule retry for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<List<PaymentRetryAttemptDto>> GetRetryAttemptsAsync(int transactionId)
    {
        try
        {
            var attemptsFromDb = await _context.PaymentRetryAttempts
                .Where(r => r.TransactionId == transactionId.ToString())
                .OrderByDescending(r => r.AttemptedAt)
                .ToListAsync();

            var attempts = attemptsFromDb.Select(r => new PaymentRetryAttemptDto
                {
                    Id = r.Id,
                    TransactionId = int.TryParse(r.TransactionId, out var tid) ? tid : 0,
                    AttemptNumber = r.AttemptNumber,
                    AttemptedAt = r.AttemptedAt,
                    AttemptedBy = r.AttemptedBy,
                    Status = Enum.TryParse<PaymentRetryStatus>(r.Status, out var status) ? status : PaymentRetryStatus.Pending,
                    StatusName = r.Status,
                    ErrorMessage = r.ErrorMessage,
                    GatewayResponse = r.GatewayResponse,
                    Duration = (int?)r.Duration.TotalMilliseconds,
                    NextRetryAt = r.NextRetryAt
                })
                .ToList();

            _logger.LogDebug("Retrieved {Count} retry attempts for transaction {TransactionId}", 
                attempts.Count, transactionId);

            return attempts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retry attempts for transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Failed to retrieve retry attempts", ex);
        }
    }

    public async Task<bool> CancelScheduledRetriesAsync(int transactionId, string cancelledBy)
    {
        try
        {
            var scheduledRetries = await _context.PaymentScheduledRetries
                .Where(r => r.TransactionId == transactionId && r.Status == PaymentRetryStatus.Scheduled.ToString())
                .ToListAsync();

            foreach (var retry in scheduledRetries)
            {
                retry.Status = PaymentRetryStatus.Cancelled.ToString();
                retry.UpdatedAt = DateTime.UtcNow;
                retry.UpdatedBy = cancelledBy;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled {Count} scheduled retries for transaction {TransactionId} by {CancelledBy}", 
                scheduledRetries.Count, transactionId, cancelledBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel scheduled retries for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Failure Handling

    public async Task<bool> HandlePermanentFailureAsync(int transactionId, string reason, string handledBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            // Update transaction status
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.StatusMessage = $"Permanent failure: {reason}";
            transaction.FailedAt = DateTime.UtcNow;

            // Cancel any scheduled retries
            await CancelScheduledRetriesAsync(transactionId, handledBy);

            // Create failure record
            var failureRecord = new PaymentFailureRecord
            {
                TransactionId = transactionId,
                FailureType = PaymentFailureType.Permanent.ToString(),
                Reason = reason,
                HandledBy = handledBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentFailureRecords.Add(failureRecord);

            // Log transaction update
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = "PERMANENT_FAILURE",
                PreviousStatus = transaction.Status,
                NewStatus = PaymentTransactionStatus.Failed,
                Details = $"Permanent failure handled by {handledBy}. Reason: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            // Send failure notification
            await _notificationService.SendPaymentFailedNotificationAsync(transactionId);

            _logger.LogWarning("Permanent failure handled for transaction {TransactionId}. Reason: {Reason}", 
                transactionId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle permanent failure for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> MoveToDeadLetterQueueAsync(int transactionId, string reason)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.RetryAttempts)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            var deadLetterRecord = new PaymentDeadLetterQueue
            {
                TransactionId = transactionId,
                OriginalTransactionReference = transaction.TransactionReference,
                Reason = reason,
                RetryAttempts = transaction.RetryAttempts,
                LastAttemptAt = DateTime.UtcNow,
                TransactionData = JsonSerializer.Serialize(new
                {
                    transaction.Id,
                    transaction.TransactionReference,
                    transaction.ClientId,
                    transaction.GatewayType,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.PayerPhone,
                    transaction.InitiatedAt
                }),
                CreatedAt = DateTime.UtcNow,
                Status = DeadLetterStatus.Pending.ToString()
            };

            _context.PaymentDeadLetterQueue.Add(deadLetterRecord);

            // Update transaction status
            transaction.Status = PaymentTransactionStatus.DeadLetter;
            transaction.StatusMessage = $"Moved to dead letter queue: {reason}";

            await _context.SaveChangesAsync();

            _logger.LogWarning("Transaction {TransactionId} moved to dead letter queue. Reason: {Reason}", 
                transactionId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move transaction {TransactionId} to dead letter queue", transactionId);
            return false;
        }
    }

    public async Task<List<PaymentDeadLetterQueueDto>> GetDeadLetterQueueAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var recordsFromDb = await _context.PaymentDeadLetterQueue
                .Where(d => d.Status == DeadLetterStatus.Pending.ToString())
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var records = recordsFromDb.Select(d => new PaymentDeadLetterQueueDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    OriginalTransactionReference = d.OriginalTransactionReference,
                    Reason = d.Reason,
                    RetryAttempts = d.RetryAttempts,
                    LastAttemptAt = d.LastAttemptAt,
                    CreatedAt = d.CreatedAt,
                    Status = Enum.TryParse<DeadLetterStatus>(d.Status, out var dlStatus) ? dlStatus : DeadLetterStatus.Pending,
                    StatusName = d.Status,
                    ReviewedBy = d.ReviewedBy,
                    ReviewedAt = d.ReviewedAt
                })
                .ToList();

            _logger.LogDebug("Retrieved {Count} dead letter queue records for page {Page}", records.Count, page);
            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dead letter queue records");
            throw new InvalidOperationException("Failed to retrieve dead letter queue", ex);
        }
    }

    public async Task<bool> ProcessDeadLetterAsync(int deadLetterId, string action, string processedBy)
    {
        try
        {
            var deadLetterRecord = await _context.PaymentDeadLetterQueue
                .FirstOrDefaultAsync(d => d.Id == deadLetterId);

            if (deadLetterRecord == null)
                return false;

            switch (action.ToUpper())
            {
                case "RETRY":
                    // Move back to retry queue
                    var transaction = await _context.PaymentGatewayTransactions
                        .FirstOrDefaultAsync(t => t.Id == deadLetterRecord.TransactionId);

                    if (transaction != null)
                    {
                        transaction.Status = PaymentTransactionStatus.Pending;
                        transaction.StatusMessage = "Restored from dead letter queue for retry";
                        await ScheduleRetryAsync(transaction.Id, DateTime.UtcNow.AddMinutes(5), processedBy);
                    }

                    deadLetterRecord.Status = DeadLetterStatus.Reprocessed.ToString();
                    break;

                case "RESOLVE":
                    deadLetterRecord.Status = DeadLetterStatus.Resolved.ToString();
                    break;

                case "DISCARD":
                    deadLetterRecord.Status = DeadLetterStatus.Discarded.ToString();
                    break;

                default:
                    return false;
            }

            deadLetterRecord.ReviewedBy = processedBy;
            deadLetterRecord.ReviewedAt = DateTime.UtcNow;
            deadLetterRecord.ProcessingNotes = $"Action: {action} by {processedBy}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Dead letter record {DeadLetterId} processed with action {Action} by {ProcessedBy}", 
                deadLetterId, action, processedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process dead letter record {DeadLetterId}", deadLetterId);
            return false;
        }
    }

    #endregion

    #region Background Processing

    public async Task<List<PaymentScheduledRetry>> GetPendingScheduledRetriesAsync()
    {
        try
        {
            var pendingRetries = await _context.PaymentScheduledRetries
                .Include(r => r.Transaction)
                .Where(r => r.Status == PaymentRetryStatus.Scheduled.ToString() && 
                           r.ScheduledAt <= DateTime.UtcNow)
                .OrderBy(r => r.ScheduledAt)
                .Take(50) // Process in batches
                .ToListAsync();

            _logger.LogDebug("Found {Count} pending scheduled retries", pendingRetries.Count);
            return pendingRetries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending scheduled retries");
            return new List<PaymentScheduledRetry>();
        }
    }

    public async Task<bool> ProcessScheduledRetryAsync(int scheduledRetryId)
    {
        try
        {
            var scheduledRetry = await _context.PaymentScheduledRetries
                .Include(r => r.Transaction)
                .FirstOrDefaultAsync(r => r.Id == scheduledRetryId);

            if (scheduledRetry == null)
                return false;

            // Mark as processing
            scheduledRetry.Status = PaymentRetryStatus.InProgress.ToString();
            scheduledRetry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Execute the retry
            var retryResult = await RetryPaymentAsync(scheduledRetry.TransactionId, "SYSTEM");

            // Update status based on result
            scheduledRetry.Status = retryResult.Success ? PaymentRetryStatus.Completed.ToString() : PaymentRetryStatus.Failed.ToString();
            scheduledRetry.UpdatedAt = DateTime.UtcNow;
            scheduledRetry.ProcessingNotes = retryResult.Message;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed scheduled retry {ScheduledRetryId} for transaction {TransactionId}. Success: {Success}", 
                scheduledRetryId, scheduledRetry.TransactionId, retryResult.Success);

            return retryResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled retry {ScheduledRetryId}", scheduledRetryId);
            return false;
        }
    }

    public async Task<RetryStatisticsDto> GetRetryStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var attempts = await _context.PaymentRetryAttempts
                .Where(r => r.AttemptedAt >= fromDate && r.AttemptedAt <= toDate)
                .ToListAsync();

            var stats = new RetryStatisticsDto
            {
                TotalRetryAttempts = attempts.Count,
                SuccessfulRetries = attempts.Count(r => r.Status == PaymentRetryStatus.Completed.ToString()),
                FailedRetries = attempts.Count(r => r.Status == PaymentRetryStatus.Failed.ToString()),
                AverageRetryDuration = attempts.Where(r => r.Duration != TimeSpan.Zero).Average(r => r.Duration.TotalMilliseconds),
                RetrySuccessRate = attempts.Count > 0 ? 
                    (double)attempts.Count(r => r.Status == PaymentRetryStatus.Completed.ToString()) / attempts.Count * 100 : 0,
                GatewayRetryStats = attempts
                    .GroupBy(r => r.Transaction.GatewayType)
                    .ToDictionary(g => g.Key, g => new GatewayRetryStats
                    {
                        TotalAttempts = g.Count(),
                        SuccessfulAttempts = g.Count(r => r.Status == PaymentRetryStatus.Completed.ToString()),
                        FailedAttempts = g.Count(r => r.Status == PaymentRetryStatus.Failed.ToString()),
                        AverageDuration = g.Where(r => r.Duration != TimeSpan.Zero).Average(r => r.Duration.TotalMilliseconds)
                    }),
                DeadLetterQueueSize = await _context.PaymentDeadLetterQueue
                    .CountAsync(d => d.Status == DeadLetterStatus.Pending.ToString()),
                CircuitBreakerStates = _circuitBreakers.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.State.ToString())
            };

            _logger.LogDebug("Generated retry statistics for period {FromDate} to {ToDate}", fromDate, toDate);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retry statistics");
            throw new InvalidOperationException("Failed to retrieve retry statistics", ex);
        }
    }

    #endregion

    #region Circuit Breaker Management

    public async Task<CircuitBreakerStatusDto> GetCircuitBreakerStatusAsync(PaymentGatewayType gatewayType)
    {
        try
        {
            var circuitBreaker = _circuitBreakers.GetValueOrDefault(gatewayType);
            if (circuitBreaker == null)
                throw new InvalidOperationException($"No circuit breaker found for gateway {gatewayType}");

            return new CircuitBreakerStatusDto
            {
                GatewayType = gatewayType,
                GatewayTypeName = gatewayType.ToString(),
                State = circuitBreaker.State,
                StateName = circuitBreaker.State.ToString(),
                FailureCount = circuitBreaker.FailureCount,
                LastFailureAt = circuitBreaker.LastFailureAt,
                NextRetryAt = circuitBreaker.NextRetryAt,
                IsHealthy = circuitBreaker.State == CircuitBreakerStatus.Closed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get circuit breaker status for gateway {GatewayType}", gatewayType);
            throw new InvalidOperationException("Failed to retrieve circuit breaker status", ex);
        }
    }

    public async Task<bool> ResetCircuitBreakerAsync(PaymentGatewayType gatewayType, string resetBy)
    {
        try
        {
            var circuitBreaker = _circuitBreakers.GetValueOrDefault(gatewayType);
            if (circuitBreaker == null)
                return false;

            circuitBreaker.State = CircuitBreakerStatus.Closed;
            circuitBreaker.FailureCount = 0;
            circuitBreaker.LastFailureAt = null;
            circuitBreaker.NextRetryAt = null;

            _logger.LogInformation("Circuit breaker reset for gateway {GatewayType} by {ResetBy}", 
                gatewayType, resetBy);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset circuit breaker for gateway {GatewayType}", gatewayType);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private Dictionary<PaymentGatewayType, RetryConfiguration> InitializeRetryConfigurations()
    {
        return new Dictionary<PaymentGatewayType, RetryConfiguration>
        {
            [PaymentGatewayType.OrangeMoney] = new RetryConfiguration
            {
                MaxRetryAttempts = 5,
                BaseDelayMinutes = 2,
                MaxDelayMinutes = 60,
                ExponentialBackoff = true,
                RetryableErrorCodes = new[] { "NETWORK_ERROR", "TIMEOUT", "TEMPORARY_UNAVAILABLE", "PROVIDER_BUSY" },
                CircuitBreakerThreshold = 10,
                CircuitBreakerTimeoutMinutes = 15
            },
            [PaymentGatewayType.AfricellMoney] = new RetryConfiguration
            {
                MaxRetryAttempts = 5,
                BaseDelayMinutes = 3,
                MaxDelayMinutes = 90,
                ExponentialBackoff = true,
                RetryableErrorCodes = new[] { "NETWORK_ERROR", "TIMEOUT", "SYSTEM_MAINTENANCE", "SERVICE_UNAVAILABLE" },
                CircuitBreakerThreshold = 8,
                CircuitBreakerTimeoutMinutes = 20
            },
            [PaymentGatewayType.Stripe] = new RetryConfiguration
            {
                MaxRetryAttempts = 3,
                BaseDelayMinutes = 1,
                MaxDelayMinutes = 30,
                ExponentialBackoff = true,
                RetryableErrorCodes = new[] { "rate_limit", "processing_error", "api_error" },
                CircuitBreakerThreshold = 15,
                CircuitBreakerTimeoutMinutes = 10
            }
        };
    }

    private Dictionary<PaymentGatewayType, CircuitBreakerState> InitializeCircuitBreakers()
    {
        return new Dictionary<PaymentGatewayType, CircuitBreakerState>
        {
            [PaymentGatewayType.OrangeMoney] = new CircuitBreakerState
            {
                State = CircuitBreakerStatus.Closed,
                FailureThreshold = 10,
                RecoveryTimeMinutes = 15
            },
            [PaymentGatewayType.AfricellMoney] = new CircuitBreakerState
            {
                State = CircuitBreakerStatus.Closed,
                FailureThreshold = 8,
                RecoveryTimeMinutes = 20
            },
            [PaymentGatewayType.Stripe] = new CircuitBreakerState
            {
                State = CircuitBreakerStatus.Closed,
                FailureThreshold = 15,
                RecoveryTimeMinutes = 10
            }
        };
    }

    private async Task<(bool IsEligible, string Reason)> CheckRetryEligibilityAsync(
        PaymentTransaction transaction, 
        RetryConfiguration config)
    {
        // Check retry attempt limit
        if (transaction.RetryAttempts >= config.MaxRetryAttempts)
        {
            return (false, $"Maximum retry attempts ({config.MaxRetryAttempts}) exceeded");
        }

        // Check transaction age (don't retry very old transactions)
        if (transaction.InitiatedAt < DateTime.UtcNow.AddHours(-24))
        {
            return (false, "Transaction too old for retry");
        }

        // Check if transaction is in retryable status
        var retryableStatuses = new[] 
        { 
            PaymentTransactionStatus.Failed, 
            PaymentTransactionStatus.Pending, 
            PaymentTransactionStatus.Initiated 
        };

        if (!retryableStatuses.Contains(transaction.Status))
        {
            return (false, $"Transaction status {transaction.Status} is not retryable");
        }

        // Check if last failure was due to retryable error
        var lastAttempt = await _context.PaymentRetryAttempts
            .Where(r => r.TransactionId == transaction.Id.ToString())
            .OrderByDescending(r => r.AttemptedAt)
            .FirstOrDefaultAsync();
        if (lastAttempt != null && !string.IsNullOrEmpty(lastAttempt.ErrorMessage))
        {
            var isRetryableError = config.RetryableErrorCodes.Any(code => 
                lastAttempt.ErrorMessage.Contains(code, StringComparison.OrdinalIgnoreCase));

            if (!isRetryableError)
            {
                return (false, "Last failure was due to non-retryable error");
            }
        }

        return await Task.FromResult((true, "Eligible for retry"));
    }

    private TimeSpan CalculateRetryDelay(int attemptNumber, RetryConfiguration config)
    {
        if (!config.ExponentialBackoff)
        {
            return TimeSpan.FromMinutes(config.BaseDelayMinutes);
        }

        // Exponential backoff with jitter
        var baseDelay = config.BaseDelayMinutes;
        var exponentialDelay = baseDelay * Math.Pow(2, attemptNumber - 1);
        var jitter = new Random().NextDouble() * 0.1 * exponentialDelay; // 10% jitter
        var totalDelayMinutes = Math.Min(exponentialDelay + jitter, config.MaxDelayMinutes);

        return TimeSpan.FromMinutes(totalDelayMinutes);
    }

    private async Task<PaymentRetryResultDto> ExecuteRetryAttemptAsync(
        PaymentTransaction transaction, 
        PaymentRetryAttempt retryAttempt, 
        RetryConfiguration config)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Execute retry based on gateway type
            PaymentTransactionDto? result = null;

            switch (transaction.GatewayType)
            {
                case PaymentGatewayType.OrangeMoney:
                    result = await _mobileMoneyService.ProcessOrangeMoneyPaymentAsync(transaction.Id, "");
                    break;

                case PaymentGatewayType.AfricellMoney:
                    result = await _mobileMoneyService.ProcessAfricellMoneyPaymentAsync(transaction.Id, "");
                    break;

                default:
                    var processDto = new ProcessPaymentDto
                    {
                        TransactionId = transaction.Id,
                        Pin = ""  // Empty PIN for non-mobile money transactions
                    };
                    result = await _paymentGatewayService.ProcessPaymentAsync(processDto, "PaymentRetryService");
                    break;
            }

            var duration = DateTime.UtcNow - startTime;
            var success = result != null && result.Status == PaymentTransactionStatus.Completed;

            // Update retry attempt
            retryAttempt.Status = success ? PaymentRetryStatus.Completed.ToString() : PaymentRetryStatus.Failed.ToString();
            retryAttempt.Duration = TimeSpan.FromMilliseconds((int)duration.TotalMilliseconds);
            retryAttempt.GatewayResponse = JsonSerializer.Serialize(result);

            if (!success)
            {
                retryAttempt.ErrorMessage = result?.StatusMessage ?? "Unknown error during retry";
                retryAttempt.NextRetryAt = DateTime.UtcNow.Add(CalculateRetryDelay(retryAttempt.AttemptNumber + 1, config));
            }

            await _context.SaveChangesAsync();

            return new PaymentRetryResultDto
            {
                Success = success,
                TransactionId = transaction.Id,
                AttemptNumber = retryAttempt.AttemptNumber,
                Message = success ? "Retry successful" : retryAttempt.ErrorMessage,
                ShouldRetryAgain = !success && retryAttempt.AttemptNumber < config.MaxRetryAttempts,
                NextRetryAt = retryAttempt.NextRetryAt,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            retryAttempt.Status = PaymentRetryStatus.Failed.ToString();
            retryAttempt.Duration = TimeSpan.FromMilliseconds((int)duration.TotalMilliseconds);
            retryAttempt.ErrorMessage = ex.Message;
            retryAttempt.NextRetryAt = DateTime.UtcNow.Add(CalculateRetryDelay(retryAttempt.AttemptNumber + 1, config));

            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Retry attempt {AttemptNumber} failed for transaction {TransactionId}", 
                retryAttempt.AttemptNumber, transaction.Id);

            return new PaymentRetryResultDto
            {
                Success = false,
                TransactionId = transaction.Id,
                AttemptNumber = retryAttempt.AttemptNumber,
                Message = ex.Message,
                ShouldRetryAgain = retryAttempt.AttemptNumber < config.MaxRetryAttempts,
                NextRetryAt = retryAttempt.NextRetryAt,
                Duration = duration
            };
        }
    }

    private async Task<bool> ScheduleNextRetryAsync(int transactionId, int nextAttemptNumber, RetryConfiguration config)
    {
        try
        {
            var nextRetryAt = DateTime.UtcNow.Add(CalculateRetryDelay(nextAttemptNumber, config));
            
            return await ScheduleRetryAsync(transactionId, nextRetryAt, "SYSTEM");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule next retry for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    private async Task UpdateCircuitBreakerAsync(PaymentGatewayType gatewayType, bool success)
    {
        try
        {
            var circuitBreaker = _circuitBreakers[gatewayType];
            var config = _retryConfigs[gatewayType];

            if (success)
            {
                // Reset failure count on success
                circuitBreaker.FailureCount = 0;
                if (circuitBreaker.State == CircuitBreakerStatus.HalfOpen)
                {
                    circuitBreaker.State = CircuitBreakerStatus.Closed;
                    _logger.LogInformation("Circuit breaker closed for gateway {GatewayType}", gatewayType);
                }
            }
            else
            {
                circuitBreaker.FailureCount++;
                circuitBreaker.LastFailureAt = DateTime.UtcNow;

                // Open circuit breaker if threshold reached
                if (circuitBreaker.FailureCount >= config.CircuitBreakerThreshold && 
                    circuitBreaker.State == CircuitBreakerStatus.Closed)
                {
                    circuitBreaker.State = CircuitBreakerStatus.Open;
                    circuitBreaker.NextRetryAt = DateTime.UtcNow.AddMinutes(config.CircuitBreakerTimeoutMinutes);
                    
                    _logger.LogWarning("Circuit breaker opened for gateway {GatewayType} after {FailureCount} failures", 
                        gatewayType, circuitBreaker.FailureCount);
                }
            }

            // Transition from Open to Half-Open after timeout
            if (circuitBreaker.State == CircuitBreakerStatus.Open && 
                circuitBreaker.NextRetryAt.HasValue && 
                DateTime.UtcNow >= circuitBreaker.NextRetryAt.Value)
            {
                circuitBreaker.State = CircuitBreakerStatus.HalfOpen;
                _logger.LogInformation("Circuit breaker transitioned to half-open for gateway {GatewayType}", gatewayType);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update circuit breaker for gateway {GatewayType}", gatewayType);
        }
    }

    #endregion

    #region Supporting Classes

    private class RetryConfiguration
    {
        public int MaxRetryAttempts { get; set; }
        public int BaseDelayMinutes { get; set; }
        public int MaxDelayMinutes { get; set; }
        public bool ExponentialBackoff { get; set; }
        public string[] RetryableErrorCodes { get; set; } = Array.Empty<string>();
        public int CircuitBreakerThreshold { get; set; }
        public int CircuitBreakerTimeoutMinutes { get; set; }
    }

    private class CircuitBreakerState
    {
        public CircuitBreakerStatus State { get; set; }
        public int FailureCount { get; set; }
        public int FailureThreshold { get; set; }
        public int RecoveryTimeMinutes { get; set; }
        public DateTime? LastFailureAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
    }

    #endregion
}