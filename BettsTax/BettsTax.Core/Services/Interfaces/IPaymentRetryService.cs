using BettsTax.Core.DTOs.Payment;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Interface for payment retry service
/// Provides intelligent retry mechanisms and failure handling
/// </summary>
public interface IPaymentRetryService
{
    // Retry Management
    Task<PaymentRetryResultDto> RetryPaymentAsync(int transactionId, string retriedBy);
    Task<bool> ScheduleRetryAsync(int transactionId, DateTime scheduledAt, string scheduledBy);
    Task<List<PaymentRetryAttemptDto>> GetRetryAttemptsAsync(int transactionId);
    Task<bool> CancelScheduledRetriesAsync(int transactionId, string cancelledBy);

    // Failure Handling
    Task<bool> HandlePermanentFailureAsync(int transactionId, string reason, string handledBy);
    Task<bool> MoveToDeadLetterQueueAsync(int transactionId, string reason);
    Task<List<PaymentDeadLetterQueueDto>> GetDeadLetterQueueAsync(int page = 1, int pageSize = 20);
    Task<bool> ProcessDeadLetterAsync(int deadLetterId, string action, string processedBy);

    // Background Processing
    Task<List<PaymentScheduledRetry>> GetPendingScheduledRetriesAsync();
    Task<bool> ProcessScheduledRetryAsync(int scheduledRetryId);
    Task<RetryStatisticsDto> GetRetryStatisticsAsync(DateTime fromDate, DateTime toDate);

    // Circuit Breaker Management
    Task<CircuitBreakerStatusDto> GetCircuitBreakerStatusAsync(PaymentGatewayType gatewayType);
    Task<bool> ResetCircuitBreakerAsync(PaymentGatewayType gatewayType, string resetBy);
}