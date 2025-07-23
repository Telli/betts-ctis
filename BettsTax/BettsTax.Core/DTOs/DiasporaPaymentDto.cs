using BettsTax.Data;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    public class DiasporaPaymentInitiateDto
    {
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        public PaymentProvider Provider { get; set; } // PayPal or Stripe
        
        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? CustomerAddress { get; set; }
        
        [MaxLength(50)]
        public string? CustomerCity { get; set; }
        
        [MaxLength(2)]
        public string? CustomerCountry { get; set; }
        
        [MaxLength(20)]
        public string? CustomerPostalCode { get; set; }
        
        [Phone]
        public string? CustomerPhone { get; set; }
        
        [Required]
        [MaxLength(3)]
        public string PreferredCurrency { get; set; } = "USD";
        
        public bool SavePaymentMethod { get; set; } = false;
        public bool SendEmailReceipt { get; set; } = true;
        
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
        
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class DiasporaPaymentResponseDto
    {
        public int PaymentTransactionId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public PaymentProvider Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public PaymentTransactionStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        
        // Amount information
        public decimal OriginalAmountSLE { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string ConvertedCurrency { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public DateTime ExchangeRateDate { get; set; }
        
        // Payment processing
        public string? PaymentUrl { get; set; } // For redirects (PayPal) 
        public string? ClientSecret { get; set; } // For Stripe Elements
        public string? ProviderTransactionId { get; set; }
        
        // Fees
        public decimal? ProviderFee { get; set; }
        public decimal? ExchangeFee { get; set; }
        public decimal? TotalFees { get; set; }
        public decimal? NetAmount { get; set; }
        
        // Timing
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        // Instructions for user
        public string? Instructions { get; set; }
        public List<string> NextSteps { get; set; } = new();
        
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class CurrencyConversionDto
    {
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(3)]
        public string FromCurrency { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(3)]
        public string ToCurrency { get; set; } = string.Empty;
        
        public decimal ConvertedAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime RateDate { get; set; }
        public string RateSource { get; set; } = string.Empty;
        public decimal? Margin { get; set; }
        public decimal? Fee { get; set; }
    }

    public class DiasporaCountryInfoDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
        public List<PaymentProvider> AvailableProviders { get; set; } = new();
        public List<string> PopularPaymentMethods { get; set; } = new();
        public decimal EstimatedProcessingTime { get; set; } // In hours
        public List<string> RequiredFields { get; set; } = new();
        public string? SpecialInstructions { get; set; }
    }

    public class PaymentMethodConfigurationDto
    {
        public PaymentProvider Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? BrandColor { get; set; }
        
        // Availability
        public List<string> SupportedCountries { get; set; } = new();
        public List<string> SupportedCurrencies { get; set; } = new();
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
        
        // Limits and fees
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? MinAmountCurrency { get; set; }
        public string? MaxAmountCurrency { get; set; }
        public decimal FeePercentage { get; set; }
        public decimal FixedFee { get; set; }
        public string FeeCurrency { get; set; } = string.Empty;
        public string FeeDescription { get; set; } = string.Empty;
        
        // Processing
        public decimal EstimatedProcessingTimeHours { get; set; }
        public bool RequiresRedirect { get; set; }
        public bool SupportsRecurring { get; set; }
        public bool SupportsRefunds { get; set; }
        
        // User experience
        public List<string> RequiredFields { get; set; } = new();
        public List<string> OptionalFields { get; set; } = new();
        public string? SetupInstructions { get; set; }
        public List<string> SupportedLanguages { get; set; } = new();
    }

    public class DiasporaPaymentStatusDto
    {
        public int PaymentTransactionId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public PaymentProvider Provider { get; set; }
        public PaymentTransactionStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public string? StatusMessage { get; set; }
        
        // Progress tracking
        public int ProgressPercentage { get; set; }
        public List<PaymentProgressStep> ProgressSteps { get; set; } = new();
        
        // Amount tracking
        public decimal OriginalAmountSLE { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string ConvertedCurrency { get; set; } = string.Empty;
        public decimal? ActualChargedAmount { get; set; }
        public decimal? ProviderFee { get; set; }
        public decimal? NetReceivedAmount { get; set; }
        
        // Timing
        public DateTime CreatedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EstimatedCompletionTime { get; set; }
        
        // Actions available
        public List<string> AvailableActions { get; set; } = new();
        public bool CanCancel { get; set; }
        public bool CanRefund { get; set; }
        public bool CanRetry { get; set; }
        
        // Provider specific info
        public string? ProviderTransactionId { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ProviderMessage { get; set; }
        
        // Failure information
        public string? FailureReason { get; set; }
        public string? FailureCode { get; set; }
        public List<string> ResolutionSteps { get; set; } = new();
        
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class PaymentProgressStep
    {
        public string StepName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PaymentStepStatus Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum PaymentStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    public class PayPalSpecificDto
    {
        public string? OrderId { get; set; }
        public string? ApprovalUrl { get; set; }
        public string? PayerId { get; set; }
        public string? PaymentId { get; set; }
        public string? BillingAgreementId { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime? ApprovalExpiryDate { get; set; }
    }

    public class StripeSpecificDto
    {
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public string? SetupIntentId { get; set; }
        public string? PaymentMethodId { get; set; }
        public bool RequiresAction { get; set; }
        public string? NextActionType { get; set; }
        public string? NextActionUrl { get; set; }
    }

    public class ExchangeRateQuoteDto
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal Margin { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string RateSource { get; set; } = string.Empty;
        public string QuoteId { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
    }

    public class DiasporaPaymentSummaryDto
    {
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }
        
        public decimal TotalAmountSLE { get; set; }
        public decimal TotalAmountUSD { get; set; }
        public decimal TotalFees { get; set; }
        public decimal NetAmount { get; set; }
        
        public Dictionary<PaymentProvider, int> TransactionsByProvider { get; set; } = new();
        public Dictionary<string, int> TransactionsByCountry { get; set; } = new();
        public Dictionary<string, decimal> AmountByCurrency { get; set; } = new();
        
        public decimal AverageTransactionAmountUSD { get; set; }
        public decimal AverageProcessingTimeHours { get; set; }
        public decimal SuccessRate { get; set; }
        
        public List<string> TopCountries { get; set; } = new();
        public List<PaymentProvider> PreferredProviders { get; set; } = new();
        
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }
}