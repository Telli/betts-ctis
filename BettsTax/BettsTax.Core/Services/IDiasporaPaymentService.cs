using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IDiasporaPaymentService
    {
        // Payment initiation for diaspora clients
        Task<Result<DiasporaPaymentResponseDto>> InitiateDiasporaPaymentAsync(DiasporaPaymentInitiateDto dto);
        Task<Result<DiasporaPaymentResponseDto>> InitiatePayPalPaymentAsync(DiasporaPaymentInitiateDto dto);
        Task<Result<DiasporaPaymentResponseDto>> InitiateStripePaymentAsync(DiasporaPaymentInitiateDto dto);
        
        // Payment status and management
        Task<Result<DiasporaPaymentStatusDto>> GetDiasporaPaymentStatusAsync(int transactionId);
        Task<Result<DiasporaPaymentStatusDto>> GetDiasporaPaymentStatusByReferenceAsync(string reference);
        
        // Currency and conversion services
        Task<Result<CurrencyConversionDto>> GetCurrencyConversionAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<Result<ExchangeRateQuoteDto>> GetExchangeRateQuoteAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<Result<List<string>>> GetSupportedCurrenciesAsync();
        
        // Country and payment method information
        Task<Result<List<DiasporaCountryInfoDto>>> GetDiasporaCountriesAsync();
        Task<Result<DiasporaCountryInfoDto>> GetCountryInfoAsync(string countryCode);
        Task<Result<List<PaymentMethodConfigurationDto>>> GetPaymentMethodsForCountryAsync(string countryCode, string currency);
        
        // Payment completion handling
        Task<Result<DiasporaPaymentStatusDto>> HandlePayPalReturnAsync(string orderId, string payerId);
        Task<Result<DiasporaPaymentStatusDto>> HandleStripeConfirmationAsync(string paymentIntentId);
        Task<Result<bool>> ProcessDiasporaWebhookAsync(PaymentProvider provider, string webhookData, string signature);
        
        // Reporting and analytics
        Task<Result<DiasporaPaymentSummaryDto>> GetDiasporaPaymentSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<List<DiasporaPaymentStatusDto>>> GetDiasporaTransactionsAsync(string? countryCode = null, string? currency = null, PaymentProvider? provider = null);
        
        // Administrative functions
        Task<Result<bool>> RefundDiasporaPaymentAsync(int transactionId, decimal? partialAmount = null, string reason = "");
        Task<Result<bool>> CancelDiasporaPaymentAsync(int transactionId, string reason = "");
        Task<Result<bool>> RetryFailedDiasporaPaymentAsync(int transactionId);
        
        // Customer support
        Task<Result<string>> GeneratePaymentReceiptAsync(int transactionId, string format = "PDF");
        Task<Result<bool>> SendPaymentConfirmationEmailAsync(int transactionId);
        Task<Result<List<string>>> GetPaymentInstructionsAsync(PaymentProvider provider, string countryCode);
    }
}