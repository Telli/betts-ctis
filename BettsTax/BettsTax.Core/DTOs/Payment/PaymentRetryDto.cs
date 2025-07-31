using BettsTax.Data.Models;
using BettsTax.Data;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;

namespace BettsTax.Core.DTOs.Payment;

// Retry result DTOs
public class PaymentRetryResultDto
{
    public bool Success { get; set; }
    public int TransactionId { get; set; }
    public int AttemptNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ShouldRetryAgain { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public TimeSpan Duration { get; set; }
}

// Retry attempt DTOs
public class PaymentRetryAttemptDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string AttemptedBy { get; set; } = string.Empty;
    public PaymentRetryStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? GatewayResponse { get; set; }
    public int? Duration { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

// Dead letter queue DTOs
public class PaymentDeadLetterQueueDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string OriginalTransactionReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DeadLetterStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

// Search DTOs
public class PaymentSearchDto
{
    public int? ClientId { get; set; }
    public PaymentGatewayType? GatewayType { get; set; }
    public PaymentTransactionStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? PayerPhone { get; set; }
    public string? TransactionReference { get; set; }
    public SecurityRiskLevel? RiskLevel { get; set; }
    public bool? RequiresManualReview { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "InitiatedAt";
    public string SortDirection { get; set; } = "desc";
}