using BettsTax.Data;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    public class PaymentTransactionDto
    {
        public int PaymentTransactionId { get; set; }
        public int PaymentId { get; set; }
        public PaymentProvider Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public PaymentTransactionStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string? ProviderTransactionId { get; set; }
        public string? ProviderReference { get; set; }
        
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SLE";
        public decimal? ExchangeRate { get; set; }
        public decimal? ProviderFee { get; set; }
        public decimal? NetAmount { get; set; }
        
        public string? CustomerPhone { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAccountNumber { get; set; }
        
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
        
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryDate { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? InitiatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? FailedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        // Related data
        public string? ClientName { get; set; }
        public string? PaymentReference { get; set; }
    }

    public class InitiatePaymentDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        public PaymentProvider Provider { get; set; }
        
        [Required]
        [Phone]
        public string? CustomerPhone { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [EmailAddress]
        public string? CustomerEmail { get; set; }
        
        [MaxLength(50)]
        public string? CustomerAccountNumber { get; set; }
        
        public string? ReturnUrl { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class InitiateMobileMoneyPaymentDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        public PaymentProvider Provider { get; set; }
        
        [Required]
        [Phone]
        [RegularExpression(@"^(\+232|232|0)?[0-9]{8}$", ErrorMessage = "Invalid Sierra Leone phone number")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        public bool SendSmsNotification { get; set; } = true;
    }

    public class InitiateBankTransferDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        public PaymentProvider Provider { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string BankCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Narration { get; set; }
    }

    public class PaymentMethodConfigDto
    {
        public int PaymentMethodConfigId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PaymentProvider Provider { get; set; }
        public string? IconUrl { get; set; }
        public string? BrandColor { get; set; }
        public string CountryCode { get; set; } = "SL";
        public string Currency { get; set; } = "SLE";
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }
        public bool RequiresPhone { get; set; }
        public bool RequiresAccount { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool AvailableForClients { get; set; }
        public bool AvailableForDiaspora { get; set; }
        public decimal EstimatedFee { get; set; }
        public string FeeDescription { get; set; } = string.Empty;
    }

    public class PaymentProviderConfigDto
    {
        public int PaymentProviderConfigId { get; set; }
        public PaymentProvider Provider { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsTestMode { get; set; }
        public decimal FeePercentage { get; set; }
        public decimal FixedFee { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal? DailyLimit { get; set; }
        public decimal? MonthlyLimit { get; set; }
        public decimal DailyUsage { get; set; }
        public decimal MonthlyUsage { get; set; }
        public string SupportedCurrency { get; set; } = "SLE";
        public DateTime? LastUsedDate { get; set; }
    }

    public class PaymentTransactionSummaryDto
    {
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal CompletedAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal TotalFees { get; set; }
        
        public Dictionary<PaymentProvider, int> TransactionsByProvider { get; set; } = new();
        public Dictionary<PaymentProvider, decimal> AmountByProvider { get; set; } = new();
        public Dictionary<string, int> DailyTransactions { get; set; } = new(); // Date -> Count
        public Dictionary<string, decimal> DailyAmounts { get; set; } = new(); // Date -> Amount
        
        public decimal SuccessRate { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public decimal AverageProcessingTime { get; set; } // In minutes
    }

    public class MobileMoneyBalanceDto
    {
        public PaymentProvider Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "SLE";
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; }
    }

    public class MobileMoneyAccountDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public PaymentProvider Provider { get; set; }
        public bool IsActive { get; set; }
        public bool CanReceivePayments { get; set; }
        public decimal? MaxTransactionAmount { get; set; }
        public decimal? DailyLimit { get; set; }
    }

    public class BankAccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Currency { get; set; } = "SLE";
        public bool IsActive { get; set; }
        public bool CanReceivePayments { get; set; }
        public decimal? Balance { get; set; }
    }

    public class PaymentWebhookDto
    {
        public int PaymentWebhookLogId { get; set; }
        public int PaymentTransactionId { get; set; }
        public PaymentProvider Provider { get; set; }
        public string WebhookType { get; set; } = string.Empty;
        public string RequestBody { get; set; } = string.Empty;
        public string? ResponseStatus { get; set; }
        public string? ProcessingResult { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime ReceivedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? IpAddress { get; set; }
    }

    // Request DTOs for specific operations
    public class RefundPaymentDto
    {
        [Required]
        public int TransactionId { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
        public decimal? PartialAmount { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class RetryPaymentDto
    {
        [Required]
        public int TransactionId { get; set; }
        
        public bool ForceRetry { get; set; } = false;
        public string? UpdatedCustomerPhone { get; set; }
        public string? UpdatedAccountNumber { get; set; }
    }

    public class PaymentStatusUpdateDto
    {
        public string ProviderTransactionId { get; set; } = string.Empty;
        public PaymentTransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public decimal? ActualAmount { get; set; }
        public decimal? Fee { get; set; }
        public string? ProviderReference { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    // Filter DTOs
    public class PaymentTransactionFilterDto
    {
        public PaymentTransactionStatus? Status { get; set; }
        public PaymentProvider? Provider { get; set; }
        public int? ClientId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? CustomerPhone { get; set; }
        public string? TransactionReference { get; set; }
        public bool IncludeWebhookData { get; set; } = false;
    }
}