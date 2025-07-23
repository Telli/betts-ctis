using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IPaymentIntegrationService
    {
        // Payment initiation
        Task<Result<PaymentTransactionDto>> InitiatePaymentAsync(InitiatePaymentDto dto);
        Task<Result<PaymentTransactionDto>> InitiateMobileMoneyPaymentAsync(InitiateMobileMoneyPaymentDto dto);
        Task<Result<PaymentTransactionDto>> InitiateBankTransferAsync(InitiateBankTransferDto dto);
        
        // Payment status and management
        Task<Result<PaymentTransactionDto>> GetPaymentTransactionAsync(int transactionId);
        Task<Result<PaymentTransactionDto>> GetPaymentTransactionByReferenceAsync(string reference);
        Task<Result<PaymentTransactionDto>> CheckPaymentStatusAsync(int transactionId);
        Task<Result<PaymentTransactionDto>> RefreshPaymentStatusAsync(string providerTransactionId, PaymentProvider provider);
        
        // Payment completion and callbacks
        Task<Result<bool>> CompletePaymentAsync(int transactionId, string providerReference);
        Task<Result<bool>> FailPaymentAsync(int transactionId, string reason);
        Task<Result<bool>> ProcessWebhookAsync(PaymentProvider provider, string webhookData, string signature);
        
        // Payment provider management
        Task<Result<List<PaymentMethodConfigDto>>> GetAvailablePaymentMethodsAsync(decimal amount, string countryCode = "SL");
        Task<Result<PaymentProviderConfigDto>> GetPaymentProviderConfigAsync(PaymentProvider provider);
        Task<Result<bool>> TestPaymentProviderAsync(PaymentProvider provider);
        
        // Transaction history and reporting
        Task<Result<PagedResult<PaymentTransactionDto>>> GetPaymentTransactionsAsync(int page, int pageSize, 
            PaymentTransactionStatus? status = null, PaymentProvider? provider = null, 
            DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<List<PaymentTransactionDto>>> GetClientPaymentTransactionsAsync(int clientId);
        Task<Result<PaymentTransactionSummaryDto>> GetPaymentTransactionSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);
        
        // Provider-specific operations
        Task<Result<MobileMoneyBalanceDto>> GetMobileMoneyBalanceAsync(PaymentProvider provider);
        Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber, PaymentProvider provider);
        Task<Result<decimal>> CalculateTransactionFeeAsync(decimal amount, PaymentProvider provider);
        
        // Administrative functions
        Task<Result<bool>> RetryFailedPaymentAsync(int transactionId);
        Task<Result<int>> ProcessPendingPaymentsAsync();
        Task<Result<bool>> RefundPaymentAsync(int transactionId, decimal? partialAmount = null, string reason = "");
    }

    public interface IPaymentGatewayProvider
    {
        PaymentProvider ProviderType { get; }
        Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request);
        Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId);
        Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount);
        Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature);
        Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData);
        Task<Result<bool>> TestConnectionAsync();
    }

    public interface IMobileMoneyProvider : IPaymentGatewayProvider
    {
        Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber);
        Task<Result<decimal>> GetBalanceAsync();
        Task<Result<MobileMoneyAccountDto>> GetAccountInfoAsync(string phoneNumber);
        Task<Result<PaymentGatewayResponse>> SendPaymentRequestAsync(string phoneNumber, decimal amount, string reference);
    }

    public interface IBankPaymentProvider : IPaymentGatewayProvider
    {
        Task<Result<bool>> ValidateAccountAsync(string accountNumber, string bankCode);
        Task<Result<BankAccountDto>> GetAccountInfoAsync(string accountNumber, string bankCode);
        Task<Result<PaymentGatewayResponse>> InitiateBankTransferAsync(BankTransferRequest request);
    }

    // Payment gateway request/response models
    public class PaymentGatewayRequest
    {
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "SLE";
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? Description { get; set; }
        public string? CallbackUrl { get; set; }
        public string? ReturnUrl { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class PaymentGatewayResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ProviderReference { get; set; }
        public PaymentTransactionStatus Status { get; set; }
        public string? StatusMessage { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Fee { get; set; }
        public string? PaymentUrl { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class BankTransferRequest : PaymentGatewayRequest
    {
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? Narration { get; set; }
    }

    // Background service for payment processing
    public interface IPaymentProcessingService
    {
        Task<Result<int>> ProcessPendingPaymentsAsync();
        Task<Result<int>> ProcessExpiredPaymentsAsync();
        Task<Result<int>> ProcessRetryPaymentsAsync();
        Task<Result<int>> UpdatePaymentStatusesAsync();
        Task<Result<bool>> SendPaymentNotificationsAsync();
    }
}