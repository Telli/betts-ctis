using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum PaymentProvider
    {
        // Mobile Money Providers
        OrangeMoney,
        AfricellMoney,
        
        // Local Banks
        SierraLeoneCommercialBank,
        RoyalBankSL,
        FirstBankSL,
        UnionTrustBank,
        AccessBankSL,
        
        // International
        PayPal,
        Stripe,
        
        // Traditional
        BankTransfer,
        Cash,
        Cheque
    }

    public enum PaymentTransactionStatus
    {
        Initiated,
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Expired,
        Refunded,
        PartialRefund
    }

    public enum MobileMoneyOperatorCode
    {
        Orange = 76, // Orange SL prefix
        Africell = 77 // Africell SL prefix (77, 78, 79)
    }

    // Enhanced Payment entity to support integrated payments
    public class PaymentTransaction
    {
        public int PaymentTransactionId { get; set; }
        
        // Link to existing payment
        public int PaymentId { get; set; }
        
        // Provider and transaction details
        public PaymentProvider Provider { get; set; }
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
        
        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ProviderTransactionId { get; set; }
        
        [MaxLength(100)]
        public string? ProviderReference { get; set; }
        
        // Amount and currency
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [MaxLength(3)]
        public string Currency { get; set; } = "SLE"; // Sierra Leone Leone
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExchangeRate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProviderFee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? NetAmount { get; set; }
        
        // Customer details for mobile money
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }
        
        [MaxLength(100)]
        public string? CustomerName { get; set; }
        
        [MaxLength(50)]
        public string? CustomerAccountNumber { get; set; }
        
        // Bank transfer details
        [MaxLength(50)]
        public string? BankCode { get; set; }
        
        [MaxLength(100)]
        public string? BankName { get; set; }
        
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }
        
        [MaxLength(100)]
        public string? BankAccountName { get; set; }
        
        // Transaction metadata
        [MaxLength(2000)]
        public string? ProviderResponse { get; set; }
        
        [MaxLength(500)]
        public string? FailureReason { get; set; }
        
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? InitiatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? FailedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        // Retry and webhook handling
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public DateTime? NextRetryDate { get; set; }
        public DateTime? LastWebhookDate { get; set; }
        
        // Navigation properties
        public Payment? Payment { get; set; }
        public List<PaymentWebhookLog> WebhookLogs { get; set; } = new();
    }

    // Payment provider configurations
    public class PaymentProviderConfig
    {
        public int PaymentProviderConfigId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [MaxLength(200)]
        public string? ApiUrl { get; set; }
        
        [MaxLength(100)]
        public string? ApiKey { get; set; }
        
        [MaxLength(100)]
        public string? ApiSecret { get; set; }
        
        [MaxLength(50)]
        public string? MerchantId { get; set; }
        
        [MaxLength(100)]
        public string? WebhookSecret { get; set; }
        
        [MaxLength(200)]
        public string? WebhookUrl { get; set; }
        
        // Configuration as JSON
        [MaxLength(2000)]
        public string? AdditionalSettings { get; set; }
        
        // Fees and limits
        [Column(TypeName = "decimal(5,4)")]
        public decimal FeePercentage { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedFee { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DailyLimit { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyLimit { get; set; }
        
        // Status and priority
        public bool IsActive { get; set; } = true;
        public bool IsTestMode { get; set; } = true;
        public int Priority { get; set; } = 1; // Lower number = higher priority
        
        [MaxLength(3)]
        public string SupportedCurrency { get; set; } = "SLE";
        
        // Usage tracking
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyUsage { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyUsage { get; set; } = 0;
        
        public DateTime? LastUsedDate { get; set; }
        
        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        
        public List<PaymentTransaction> Transactions { get; set; } = new();
    }

    // Webhook logging for payment callbacks
    public class PaymentWebhookLog
    {
        public int PaymentWebhookLogId { get; set; }
        
        public int PaymentTransactionId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(50)]
        public string WebhookType { get; set; } = string.Empty; // payment.completed, payment.failed, etc.
        
        [MaxLength(5000)]
        public string RequestBody { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string RequestHeaders { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ResponseStatus { get; set; }
        
        [MaxLength(2000)]
        public string? ProcessingResult { get; set; }
        
        public bool IsProcessed { get; set; } = false;
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedDate { get; set; }
        
        [MaxLength(39)] // IPv6 max length
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        // Navigation properties
        public PaymentTransaction? PaymentTransaction { get; set; }
    }

    // Payment method configurations for different regions
    public class PaymentMethodConfig
    {
        public int PaymentMethodConfigId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(100)]
        public string? IconUrl { get; set; }
        
        [MaxLength(7)] // #FFFFFF
        public string? BrandColor { get; set; }
        
        // Regional availability
        [MaxLength(2)]
        public string CountryCode { get; set; } = "SL"; // Sierra Leone
        
        [MaxLength(3)]
        public string Currency { get; set; } = "SLE";
        
        // UI configuration
        public int DisplayOrder { get; set; } = 1;
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool RequiresPhone { get; set; } = false;
        public bool RequiresAccount { get; set; } = false;
        
        // Limits
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }
        
        // Target audience
        public bool AvailableForClients { get; set; } = true;
        public bool AvailableForDiaspora { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    // Payment gateway responses and status mapping
    public class PaymentStatusMapping
    {
        public int PaymentStatusMappingId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(100)]
        public string ProviderStatus { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? ProviderMessage { get; set; }
        
        public PaymentTransactionStatus MappedStatus { get; set; }
        
        public bool IsSuccess { get; set; }
        public bool IsFinal { get; set; } // If this status is final (no more updates expected)
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}