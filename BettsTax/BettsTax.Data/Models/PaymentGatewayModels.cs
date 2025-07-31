using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models;

// Payment Gateway Types specific to Sierra Leone
public enum PaymentGatewayType
{
    OrangeMoney = 1,
    AfricellMoney = 2,
    BankTransfer = 3,
    CreditCard = 4,
    PayPal = 5,
    Stripe = 6,
    Cash = 7
}

// Transaction statuses for mobile money flows
public enum PaymentTransactionStatus
{
    Initiated = 0,
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Expired = 6,
    Refunded = 7,
    PartialRefund = 8,
    Disputed = 9,
    Chargeback = 10,
    Settled = 11,
    DeadLetter = 12
}

// Payment types specific to tax payments
public enum PaymentPurpose
{
    TaxPayment = 1,
    PenaltyPayment = 2,
    InterestPayment = 3,
    FilingFee = 4,
    ServiceFee = 5,
    AdvancePayment = 6,
    Refund = 7,
    Other = 8
}

// Security levels for fraud detection
public enum SecurityRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

// Webhook event types
public enum WebhookEventType
{
    PaymentInitiated = 1,
    PaymentCompleted = 2,
    PaymentFailed = 3,
    PaymentRefunded = 4,
    PaymentDisputed = 5,
    PaymentSettled = 6,
    AccountUpdated = 7,
    SecurityAlert = 8
}

// Main payment gateway configuration
public class PaymentGatewayConfig
{
    [Key]
    public int Id { get; set; }
    
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
    
    // API Configuration (encrypted)
    [Required]
    [StringLength(2000)]
    public string ApiEndpoint { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string ApiKey { get; set; } = string.Empty; // Encrypted
    
    [StringLength(500)]
    public string ApiSecret { get; set; } = string.Empty; // Encrypted
    
    [StringLength(500)]
    public string MerchantId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string WebhookSecret { get; set; } = string.Empty; // Encrypted
    
    // Mobile Money Specific Config
    [StringLength(20)]
    public string ShortCode { get; set; } = string.Empty; // USSD short code
    
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;
    
    // Transaction Limits (in Sierra Leone Leones)
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinAmount { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxAmount { get; set; } = 1000000; // 1M SLE default
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyLimit { get; set; } = 5000000; // 5M SLE daily
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyLimit { get; set; } = 50000000; // 50M SLE monthly
    
    // Fee Structure
    [Column(TypeName = "decimal(5,4)")]
    public decimal FeePercentage { get; set; } = 0.0250m; // 2.5% default
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal FixedFee { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinFee { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxFee { get; set; } = 50000; // 50k SLE max fee
    
    // Timing Configuration
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes default
    
    public int RetryAttempts { get; set; } = 3;
    
    public int RetryDelaySeconds { get; set; } = 30;
    
    // Status and Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation Properties
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public List<PaymentTransaction> Transactions { get; set; } = new();
    public List<PaymentWebhookLog> WebhookLogs { get; set; } = new();
}

// Enhanced payment transaction with mobile money support
public class PaymentTransaction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string TransactionReference { get; set; } = string.Empty; // Our internal reference
    
    [StringLength(100)]
    public string ExternalReference { get; set; } = string.Empty; // Gateway reference
    
    [StringLength(100)]
    public string ProviderTransactionId { get; set; } = string.Empty; // Mobile money provider ID
    
    // Client and Payment Info
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public PaymentGatewayType GatewayType { get; set; }
    
    [Required]
    public int GatewayConfigId { get; set; }
    
    [Required]
    public PaymentPurpose Purpose { get; set; }
    
    [StringLength(100)]
    public string TaxType { get; set; } = string.Empty; // Income Tax, GST, Payroll Tax, etc.
    
    // Amount Details (in Sierra Leone Leones)
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; } // Amount after fees
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "SLE"; // Sierra Leone Leone
    
    // Payer Information
    [Required]
    [StringLength(20)]
    public string PayerPhone { get; set; } = string.Empty; // Mobile number for mobile money
    
    [StringLength(100)]
    public string PayerName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string PayerEmail { get; set; } = string.Empty;
    
    // Transaction Details
    [Required]
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string StatusMessage { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string ErrorCode { get; set; } = string.Empty;
    
    // Security and Fraud Detection
    public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Low;
    
    [StringLength(1000)]
    public string RiskFactors { get; set; } = string.Empty; // JSON array of risk factors
    
    public bool RequiresManualReview { get; set; } = false;
    
    [StringLength(450)]
    public string? ReviewedBy { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    // Timestamps
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime? FailedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
    
    // Retry Logic
    public int RetryCount { get; set; } = 0;
    
    public int RetryAttempts { get; set; } = 0;
    
    public DateTime? LastRetryAt { get; set; }
    
    public DateTime? NextRetryAt { get; set; }
    
    // Reconciliation
    public bool IsReconciled { get; set; } = false;
    
    public DateTime? ReconciledAt { get; set; }
    
    [StringLength(450)]
    public string? ReconciledBy { get; set; }
    
    // Webhook Response Data (JSON)
    [StringLength(4000)]
    public string? WebhookData { get; set; }
    
    // Navigation Properties
    public Client Client { get; set; } = null!;
    public PaymentGatewayConfig GatewayConfig { get; set; } = null!;
    public ApplicationUser? ReviewedByUser { get; set; }
    public ApplicationUser? ReconciledByUser { get; set; }
    public List<PaymentTransactionLog> TransactionLogs { get; set; } = new();
    public List<PaymentRefund> Refunds { get; set; } = new();
}

// Transaction audit log for complete traceability
public class PaymentTransactionLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TransactionId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // INITIATED, PROCESSED, COMPLETED, etc.
    
    [Required]
    public PaymentTransactionStatus PreviousStatus { get; set; }
    
    [Required]
    public PaymentTransactionStatus NewStatus { get; set; }
    
    [StringLength(2000)]
    public string Details { get; set; } = string.Empty;
    
    [StringLength(4000)]
    public string? RequestData { get; set; } // JSON
    
    [StringLength(4000)]
    public string? ResponseData { get; set; } // JSON
    
    [StringLength(100)]
    public string? ErrorCode { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation Properties
    public PaymentTransaction Transaction { get; set; } = null!;
}

// Refund management
public class PaymentRefund
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int OriginalTransactionId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string RefundReference { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string ExternalRefundReference { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundAmount { get; set; }
    
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
    
    [StringLength(2000)]
    public string StatusMessage { get; set; } = string.Empty;
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    [Required]
    [StringLength(450)]
    public string RequestedBy { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string? ApprovedBy { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    // Navigation Properties
    public PaymentTransaction OriginalTransaction { get; set; } = null!;
    public ApplicationUser RequestedByUser { get; set; } = null!;
    public ApplicationUser? ApprovedByUser { get; set; }
}

// Webhook event logging for debugging and audit
public class PaymentWebhookLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int GatewayConfigId { get; set; }
    
    [Required]
    public WebhookEventType EventType { get; set; }
    
    [StringLength(100)]
    public string? TransactionReference { get; set; }
    
    [Required]
    [StringLength(4000)]
    public string RequestBody { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string RequestHeaders { get; set; } = string.Empty; // JSON
    
    [StringLength(2000)]
    public string? ResponseBody { get; set; }
    
    public int ResponseStatusCode { get; set; } = 200;
    
    public bool IsProcessed { get; set; } = false;
    
    public DateTime? ProcessedAt { get; set; }
    
    [StringLength(1000)]
    public string? ProcessingError { get; set; }
    
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Signature Verification
    [StringLength(500)]
    public string? SignatureHeader { get; set; }
    
    public bool IsSignatureValid { get; set; } = false;
    
    // Navigation Properties
    public PaymentGatewayConfig GatewayConfig { get; set; } = null!;
}

// Mobile money provider specific configurations
public class MobileMoneyProvider
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty; // "Orange Money", "Africell Money"
    
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty; // "OM", "AM"
    
    [Required]
    [StringLength(10)]
    public string CountryCode { get; set; } = "SL"; // Sierra Leone
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "SLE";
    
    // Network Configuration
    [Required]
    [StringLength(20)]
    public string ShortCode { get; set; } = string.Empty; // USSD short code
    
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string ApiBaseUrl { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string ApiVersion { get; set; } = "v1";
    
    // Mobile Number Validation
    [Required]
    [StringLength(20)]
    public string PhonePrefix { get; set; } = string.Empty; // e.g., "232", "077", "078"
    
    public int MinPhoneLength { get; set; } = 8;
    
    public int MaxPhoneLength { get; set; } = 15;
    
    [StringLength(100)]
    public string PhoneValidationRegex { get; set; } = string.Empty;
    
    // Operational Settings
    public bool IsActive { get; set; } = true;
    
    public bool SupportsInquiry { get; set; } = true;
    
    public bool SupportsRefund { get; set; } = true;
    
    public bool SupportsWebhooks { get; set; } = true;
    
    // Default Limits (can be overridden in gateway config)
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultMinAmount { get; set; } = 1000; // 1,000 SLE
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultMaxAmount { get; set; } = 1000000; // 1M SLE
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultDailyLimit { get; set; } = 5000000; // 5M SLE
    
    // Default Fee Structure
    [Column(TypeName = "decimal(5,4)")]
    public decimal DefaultFeePercentage { get; set; } = 0.0250m; // 2.5%
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultFixedFee { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultMinFee { get; set; } = 500; // 500 SLE
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultMaxFee { get; set; } = 50000; // 50k SLE
    
    // Timing Configuration
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes
    
    public int DefaultRetryAttempts { get; set; } = 3;
    
    public int DefaultRetryDelaySeconds { get; set; } = 30;
    
    // Status and Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation Properties
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}

// Payment fraud detection rules
public class PaymentFraudRule
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string RuleName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    // Rule Conditions (JSON)
    [Required]
    [StringLength(4000)]
    public string Conditions { get; set; } = string.Empty;
    
    // Actions to take when rule is triggered
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // BLOCK, REVIEW, FLAG, NOTIFY
    
    [Required]
    public SecurityRiskLevel RiskLevel { get; set; }
    
    public int Priority { get; set; } = 1; // Higher number = higher priority
    
    // Statistics
    public int TriggerCount { get; set; } = 0;
    
    public DateTime? LastTriggered { get; set; }
    
    // Status and Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation Properties
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}