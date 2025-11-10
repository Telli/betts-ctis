using BettsTax.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs.Payment;

// Payment gateway configuration DTOs
public class PaymentGatewayConfigDto
{
    public int Id { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsTestMode { get; set; }
    public string ApiEndpoint { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal FixedFee { get; set; }
    public decimal MinFee { get; set; }
    public decimal MaxFee { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreatePaymentGatewayConfigDto
{
    [Required]
    public PaymentGatewayType GatewayType { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public bool IsTestMode { get; set; } = false;
    
    [Required]
    [StringLength(2000)]
    public string ApiEndpoint { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string ApiKey { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string ApiSecret { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string MerchantId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string WebhookSecret { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string ShortCode { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;
    
    [Range(0, 1000000)]
    public decimal MinAmount { get; set; } = 0;
    
    [Range(1, 100000000)]
    public decimal MaxAmount { get; set; } = 1000000;
    
    [Range(1000, 1000000000)]
    public decimal DailyLimit { get; set; } = 5000000;
    
    [Range(10000, 10000000000)]
    public decimal MonthlyLimit { get; set; } = 50000000;
    
    [Range(0, 1)]
    public decimal FeePercentage { get; set; } = 0.0250m;
    
    [Range(0, 100000)]
    public decimal FixedFee { get; set; } = 0;
    
    [Range(0, 50000)]
    public decimal MinFee { get; set; } = 0;
    
    [Range(0, 500000)]
    public decimal MaxFee { get; set; } = 50000;
    
    [Range(30, 3600)]
    public int TimeoutSeconds { get; set; } = 300;
    
    [Range(1, 10)]
    public int RetryAttempts { get; set; } = 3;
    
    [Range(5, 300)]
    public int RetryDelaySeconds { get; set; } = 30;
}

// Payment transaction DTOs
public class PaymentTransactionDto
{
    public int Id { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientNumber { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayName { get; set; } = string.Empty;
    public PaymentPurpose Purpose { get; set; }
    public string PurposeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "SLE";
    public string PayerPhone { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public PaymentTransactionStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public SecurityRiskLevel RiskLevel { get; set; }
    public string RiskLevelName { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public bool RequiresManualReview { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public List<PaymentTransactionLogDto> TransactionLogs { get; set; } = new();
    public List<PaymentRefundDto> Refunds { get; set; } = new();
}

public class CreatePaymentTransactionDto
{
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public PaymentGatewayType GatewayType { get; set; }
    
    [Required]
    public PaymentPurpose Purpose { get; set; }
    
    [Required]
    [Range(1, 100000000)]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "SLE";
    
    [Required]
    [StringLength(20)]
    [Phone]
    public string PayerPhone { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string PayerName { get; set; } = string.Empty;
    
    [StringLength(200)]
    [EmailAddress]
    public string PayerEmail { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    // Optional tax payment specific fields
    public int? TaxFilingId { get; set; }
    public int? TaxYearId { get; set; }
    public string? TaxType { get; set; }
    
    // Client browser/app info for fraud detection
    [StringLength(100)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Optional return URLs for web payments
    [StringLength(2000)]
    public string? SuccessUrl { get; set; }
    
    [StringLength(2000)]
    public string? FailureUrl { get; set; }
    
    [StringLength(2000)]
    public string? CancelUrl { get; set; }
}

public class ProcessPaymentDto
{
    [Required]
    public int TransactionId { get; set; }
    
    // Mobile money specific fields
    [StringLength(50)]
    public string? Pin { get; set; } // Encrypted PIN for mobile money
    
    [StringLength(20)]
    public string? OtpCode { get; set; } // OTP verification code
    
    // Additional verification data
    [StringLength(1000)]
    public string? AdditionalData { get; set; } // JSON for gateway-specific data
}

public class PaymentTransactionLogDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public PaymentTransactionStatus PreviousStatus { get; set; }
    public string PreviousStatusName { get; set; } = string.Empty;
    public PaymentTransactionStatus NewStatus { get; set; }
    public string NewStatusName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
}

// Refund DTOs
public class PaymentRefundDto
{
    public int Id { get; set; }
    public int OriginalTransactionId { get; set; }
    public string RefundReference { get; set; } = string.Empty;
    public string ExternalRefundReference { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PaymentTransactionStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? RequestedByName { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public PaymentTransactionDto? OriginalTransaction { get; set; }
}

public class CreatePaymentRefundDto
{
    [Required]
    public int OriginalTransactionId { get; set; }
    
    [Required]
    [Range(1, 100000000)]
    public decimal RefundAmount { get; set; }
    
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Notes { get; set; }
}

// Mobile money provider DTOs
public class MobileMoneyProviderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SL";
    public string Currency { get; set; } = "SLE";
    public string ShortCode { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string PhonePrefix { get; set; } = string.Empty;
    public int MinPhoneLength { get; set; }
    public int MaxPhoneLength { get; set; }
    public string PhoneValidationRegex { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool SupportsInquiry { get; set; }
    public bool SupportsRefund { get; set; }
    public bool SupportsWebhooks { get; set; }
    public decimal DefaultMinAmount { get; set; }
    public decimal DefaultMaxAmount { get; set; }
    public decimal DefaultDailyLimit { get; set; }
    public decimal DefaultFeePercentage { get; set; }
    public decimal DefaultFixedFee { get; set; }
    public decimal DefaultMinFee { get; set; }
    public decimal DefaultMaxFee { get; set; }
    public int DefaultTimeoutSeconds { get; set; }
    public int DefaultRetryAttempts { get; set; }
    public int DefaultRetryDelaySeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Webhook DTOs

// Fraud detection DTOs

// Payment analytics DTOs
public class PaymentAnalyticsDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public decimal AverageProcessingTime { get; set; } // in seconds
    public Dictionary<PaymentGatewayType, PaymentGatewayStatsDto> GatewayStats { get; set; } = new();
    public Dictionary<PaymentTransactionStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<PaymentPurpose, decimal> PurposeAmounts { get; set; } = new();
    public Dictionary<string, decimal> DailyAmounts { get; set; } = new(); // Date string -> Amount
    public Dictionary<string, int> DailyTransactionCounts { get; set; } = new(); // Date string -> Count
    public List<PaymentErrorStatsDto> TopErrors { get; set; } = new();
    public SecurityStatsDto SecurityStats { get; set; } = new();
}

public class PaymentGatewayStatsDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal AverageProcessingTime { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int PendingTransactions { get; set; }
}

public class PaymentErrorStatsDto
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public PaymentGatewayType? GatewayType { get; set; }
    public string? GatewayName { get; set; }
}

// Payment Gateway Request/Response DTOs
public class PaymentGatewayRequest
{
    public string TransactionReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SLE";
    public string PayerPhone { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
    public string? SuccessUrl { get; set; }
    public string? FailureUrl { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class PaymentGatewayResponse
{
    public bool IsSuccess { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; } = string.Empty;
    public PaymentTransactionStatus Status { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Currency { get; set; } = "SLE";
    public DateTime ProcessedAt { get; set; }
    public string? PaymentUrl { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

// Search and filter DTOs
public class PaymentTransactionSearchDto
{
    public string? TransactionReference { get; set; }
    public string? ExternalReference { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public PaymentGatewayType? GatewayType { get; set; }
    public PaymentPurpose? Purpose { get; set; }
    public PaymentTransactionStatus? Status { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? PayerPhone { get; set; }
    public string? PayerEmail { get; set; }
    public SecurityRiskLevel? RiskLevel { get; set; }
    public bool? RequiresManualReview { get; set; }
    public bool? IsReconciled { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ErrorCode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "InitiatedAt";
    public string SortDirection { get; set; } = "desc";
}