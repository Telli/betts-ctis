using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class DiasporaPaymentService : IDiasporaPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DiasporaPaymentService> _logger;
        private readonly IPaymentIntegrationService _paymentIntegrationService;
        private readonly ICurrencyExchangeService _currencyExchangeService;
        private readonly IEmailService _emailService;
        private readonly IActivityTimelineService _activityService;

        // Country mappings for diaspora populations
        private readonly Dictionary<string, DiasporaCountryInfoDto> _diasporaCountries;

        public DiasporaPaymentService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<DiasporaPaymentService> logger,
            IPaymentIntegrationService paymentIntegrationService,
            ICurrencyExchangeService currencyExchangeService,
            IEmailService emailService,
            IActivityTimelineService activityService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _paymentIntegrationService = paymentIntegrationService;
            _currencyExchangeService = currencyExchangeService;
            _emailService = emailService;
            _activityService = activityService;

            _diasporaCountries = InitializeDiasporaCountries();
        }

        public async Task<Result<DiasporaPaymentResponseDto>> InitiateDiasporaPaymentAsync(DiasporaPaymentInitiateDto dto)
        {
            try
            {
                // Determine best payment provider based on country and currency
                var countryInfo = await GetCountryInfoAsync(dto.CustomerCountry ?? "US");
                if (!countryInfo.IsSuccess)
                    return Result.Failure<DiasporaPaymentResponseDto>("Country not supported for diaspora payments");

                // Auto-select provider if not specified
                if (dto.Provider == default)
                {
                    dto.Provider = DetermineOptimalProvider(dto.CustomerCountry, dto.PreferredCurrency, countryInfo.Value);
                }

                // Route to appropriate provider
                return dto.Provider switch
                {
                    PaymentProvider.PayPal => await InitiatePayPalPaymentAsync(dto),
                    PaymentProvider.Stripe => await InitiateStripePaymentAsync(dto),
                    _ => Result.Failure<DiasporaPaymentResponseDto>("Unsupported payment provider for diaspora payments")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating diaspora payment for PaymentId {PaymentId}", dto.PaymentId);
                return Result.Failure<DiasporaPaymentResponseDto>("An error occurred while initiating the diaspora payment");
            }
        }

        public async Task<Result<DiasporaPaymentResponseDto>> InitiatePayPalPaymentAsync(DiasporaPaymentInitiateDto dto)
        {
            try
            {
                // Get currency conversion
                var conversionResult = await GetCurrencyConversionAsync(
                    await GetPaymentAmountAsync(dto.PaymentId), "SLE", dto.PreferredCurrency);
                
                if (!conversionResult.IsSuccess)
                    return Result.Failure<DiasporaPaymentResponseDto>($"Currency conversion failed: {conversionResult.ErrorMessage}");

                var conversion = conversionResult.Value;

                // Create standard payment integration request
                var paymentRequest = new InitiatePaymentDto
                {
                    PaymentId = dto.PaymentId,
                    Provider = PaymentProvider.PayPal,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    ReturnUrl = dto.ReturnUrl ?? GenerateReturnUrl("paypal", "success"),
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["cancel_url"] = dto.CancelUrl ?? GenerateReturnUrl("paypal", "cancel"),
                        ["customer_country"] = dto.CustomerCountry ?? "",
                        ["customer_city"] = dto.CustomerCity ?? "",
                        ["customer_address"] = dto.CustomerAddress ?? "",
                        ["preferred_currency"] = dto.PreferredCurrency,
                        ["original_amount_sle"] = conversion.Amount.ToString(),
                        ["exchange_rate"] = conversion.ExchangeRate.ToString(),
                        ["diaspora_client"] = "true"
                    }
                };

                var result = await _paymentIntegrationService.InitiatePaymentAsync(paymentRequest);
                if (!result.IsSuccess)
                    return Result.Failure<DiasporaPaymentResponseDto>(result.ErrorMessage);

                var paymentTransaction = result.Value;

                // Log diaspora payment activity
                await _activityService.LogPaymentActivityAsync(
                    dto.PaymentId,
                    ActivityType.PaymentCreated,
                    $"Diaspora payment initiated via PayPal for {dto.CustomerCountry} - {conversion.ConvertedAmount:C} {dto.PreferredCurrency}");

                var response = new DiasporaPaymentResponseDto
                {
                    PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                    TransactionReference = paymentTransaction.TransactionReference,
                    Provider = PaymentProvider.PayPal,
                    ProviderName = "PayPal",
                    Status = paymentTransaction.Status,
                    StatusDescription = GetDiasporaStatusDescription(paymentTransaction.Status),
                    OriginalAmountSLE = conversion.Amount,
                    ConvertedAmount = conversion.ConvertedAmount,
                    ConvertedCurrency = dto.PreferredCurrency,
                    ExchangeRate = conversion.ExchangeRate,
                    ExchangeRateDate = conversion.RateDate,
                    PaymentUrl = GetPaymentUrlFromAdditionalData(paymentTransaction),
                    ProviderTransactionId = paymentTransaction.ProviderTransactionId,
                    ProviderFee = paymentTransaction.ProviderFee,
                    NetAmount = paymentTransaction.NetAmount,
                    CreatedDate = paymentTransaction.CreatedDate,
                    ExpiryDate = paymentTransaction.ExpiryDate,
                    Instructions = "You will be redirected to PayPal to complete your payment securely.",
                    NextSteps = new List<string>
                    {
                        "Click the payment link to go to PayPal",
                        "Log in to your PayPal account or pay as a guest",
                        "Review and confirm the payment details",
                        "Complete the payment to finalize your tax payment"
                    }
                };

                return Result.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayPal diaspora payment");
                return Result.Failure<DiasporaPaymentResponseDto>("Failed to initiate PayPal payment");
            }
        }

        public async Task<Result<DiasporaPaymentResponseDto>> InitiateStripePaymentAsync(DiasporaPaymentInitiateDto dto)
        {
            try
            {
                // Get currency conversion
                var conversionResult = await GetCurrencyConversionAsync(
                    await GetPaymentAmountAsync(dto.PaymentId), "SLE", dto.PreferredCurrency);
                
                if (!conversionResult.IsSuccess)
                    return Result.Failure<DiasporaPaymentResponseDto>($"Currency conversion failed: {conversionResult.ErrorMessage}");

                var conversion = conversionResult.Value;

                // Create standard payment integration request
                var paymentRequest = new InitiatePaymentDto
                {
                    PaymentId = dto.PaymentId,
                    Provider = PaymentProvider.Stripe,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["customer_country"] = dto.CustomerCountry ?? "",
                        ["customer_city"] = dto.CustomerCity ?? "",
                        ["customer_address"] = dto.CustomerAddress ?? "",
                        ["customer_postal_code"] = dto.CustomerPostalCode ?? "",
                        ["preferred_currency"] = dto.PreferredCurrency,
                        ["original_amount_sle"] = conversion.Amount.ToString(),
                        ["exchange_rate"] = conversion.ExchangeRate.ToString(),
                        ["diaspora_client"] = "true",
                        ["save_payment_method"] = dto.SavePaymentMethod.ToString()
                    }
                };

                var result = await _paymentIntegrationService.InitiatePaymentAsync(paymentRequest);
                if (!result.IsSuccess)
                    return Result.Failure<DiasporaPaymentResponseDto>(result.ErrorMessage);

                var paymentTransaction = result.Value;

                // Log diaspora payment activity
                await _activityService.LogPaymentActivityAsync(
                    dto.PaymentId,
                    ActivityType.PaymentCreated,
                    $"Diaspora payment initiated via Stripe for {dto.CustomerCountry} - {conversion.ConvertedAmount:C} {dto.PreferredCurrency}");

                var response = new DiasporaPaymentResponseDto
                {
                    PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                    TransactionReference = paymentTransaction.TransactionReference,
                    Provider = PaymentProvider.Stripe,
                    ProviderName = "Stripe",
                    Status = paymentTransaction.Status,
                    StatusDescription = GetDiasporaStatusDescription(paymentTransaction.Status),
                    OriginalAmountSLE = conversion.Amount,
                    ConvertedAmount = conversion.ConvertedAmount,
                    ConvertedCurrency = dto.PreferredCurrency,
                    ExchangeRate = conversion.ExchangeRate,
                    ExchangeRateDate = conversion.RateDate,
                    ClientSecret = GetClientSecretFromAdditionalData(paymentTransaction),
                    ProviderTransactionId = paymentTransaction.ProviderTransactionId,
                    ProviderFee = paymentTransaction.ProviderFee,
                    NetAmount = paymentTransaction.NetAmount,
                    CreatedDate = paymentTransaction.CreatedDate,
                    ExpiryDate = paymentTransaction.ExpiryDate,
                    Instructions = "Use the provided client secret to complete payment with Stripe Elements on your website.",
                    NextSteps = new List<string>
                    {
                        "Enter your payment card details securely",
                        "Review the payment amount and currency",
                        "Click 'Pay Now' to complete the transaction",
                        "Wait for payment confirmation"
                    }
                };

                return Result.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Stripe diaspora payment");
                return Result.Failure<DiasporaPaymentResponseDto>("Failed to initiate Stripe payment");
            }
        }

        public async Task<Result<DiasporaPaymentStatusDto>> GetDiasporaPaymentStatusAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Set<PaymentTransaction>()
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p.Client)
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<DiasporaPaymentStatusDto>("Transaction not found");

                // Check if this is a diaspora payment
                if (!IsDiasporaPayment(transaction))
                    return Result.Failure<DiasporaPaymentStatusDto>("Not a diaspora payment");

                // Get current status from provider
                var statusResult = await _paymentIntegrationService.CheckPaymentStatusAsync(transactionId);
                if (statusResult.IsSuccess)
                {
                    transaction = await _context.Set<PaymentTransaction>()
                        .Include(pt => pt.Payment)
                            .ThenInclude(p => p.Client)
                        .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);
                }

                var status = BuildDiasporaPaymentStatus(transaction);
                return Result.Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting diaspora payment status for transaction {TransactionId}", transactionId);
                return Result.Failure<DiasporaPaymentStatusDto>("Failed to get payment status");
            }
        }

        public async Task<Result<CurrencyConversionDto>> GetCurrencyConversionAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            try
            {
                var exchangeInfo = await _currencyExchangeService.GetExchangeRateInfoAsync(fromCurrency, toCurrency);
                if (!exchangeInfo.IsSuccess)
                    return Result.Failure<CurrencyConversionDto>(exchangeInfo.ErrorMessage);

                var convertResult = await _currencyExchangeService.ConvertAmountAsync(amount, fromCurrency, toCurrency);
                if (!convertResult.IsSuccess)
                    return Result.Failure<CurrencyConversionDto>(convertResult.ErrorMessage);

                var conversion = new CurrencyConversionDto
                {
                    Amount = amount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    ConvertedAmount = convertResult.Value,
                    ExchangeRate = exchangeInfo.Value.Rate,
                    RateDate = exchangeInfo.Value.LastUpdated,
                    RateSource = exchangeInfo.Value.Source
                };

                return Result.Success(conversion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currency conversion from {From} to {To}", fromCurrency, toCurrency);
                return Result.Failure<CurrencyConversionDto>("Failed to get currency conversion");
            }
        }

        public async Task<Result<List<DiasporaCountryInfoDto>>> GetDiasporaCountriesAsync()
        {
            try
            {
                var countries = _diasporaCountries.Values.ToList();
                
                // Update availability based on current provider status
                foreach (var country in countries)
                {
                    foreach (var provider in country.AvailableProviders.ToList())
                    {
                        var testResult = await _paymentIntegrationService.TestPaymentProviderAsync(provider);
                        if (!testResult.IsSuccess || !testResult.Value)
                        {
                            country.AvailableProviders.Remove(provider);
                        }
                    }
                }

                return Result.Success(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting diaspora countries");
                return Result.Failure<List<DiasporaCountryInfoDto>>("Failed to get diaspora countries");
            }
        }

        public async Task<Result<DiasporaCountryInfoDto>> GetCountryInfoAsync(string countryCode)
        {
            try
            {
                if (!_diasporaCountries.TryGetValue(countryCode.ToUpper(), out var country))
                    return Result.Failure<DiasporaCountryInfoDto>("Country not supported");

                // Test provider availability
                var availableProviders = new List<PaymentProvider>();
                foreach (var provider in country.AvailableProviders)
                {
                    var testResult = await _paymentIntegrationService.TestPaymentProviderAsync(provider);
                    if (testResult.IsSuccess && testResult.Value)
                    {
                        availableProviders.Add(provider);
                    }
                }

                country.AvailableProviders = availableProviders;
                return Result.Success(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting country info for {CountryCode}", countryCode);
                return Result.Failure<DiasporaCountryInfoDto>("Failed to get country info");
            }
        }

        // Placeholder implementations for remaining interface methods
        public Task<Result<DiasporaPaymentStatusDto>> GetDiasporaPaymentStatusByReferenceAsync(string reference) => throw new NotImplementedException();
        public Task<Result<ExchangeRateQuoteDto>> GetExchangeRateQuoteAsync(decimal amount, string fromCurrency, string toCurrency) => throw new NotImplementedException();
        public Task<Result<List<string>>> GetSupportedCurrenciesAsync() => throw new NotImplementedException();
        public Task<Result<List<PaymentMethodConfigurationDto>>> GetPaymentMethodsForCountryAsync(string countryCode, string currency) => throw new NotImplementedException();
        public Task<Result<DiasporaPaymentStatusDto>> HandlePayPalReturnAsync(string orderId, string payerId) => throw new NotImplementedException();
        public Task<Result<DiasporaPaymentStatusDto>> HandleStripeConfirmationAsync(string paymentIntentId) => throw new NotImplementedException();
        public Task<Result<bool>> ProcessDiasporaWebhookAsync(PaymentProvider provider, string webhookData, string signature) => throw new NotImplementedException();
        public Task<Result<DiasporaPaymentSummaryDto>> GetDiasporaPaymentSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null) => throw new NotImplementedException();
        public Task<Result<List<DiasporaPaymentStatusDto>>> GetDiasporaTransactionsAsync(string? countryCode = null, string? currency = null, PaymentProvider? provider = null) => throw new NotImplementedException();
        public Task<Result<bool>> RefundDiasporaPaymentAsync(int transactionId, decimal? partialAmount = null, string reason = "") => throw new NotImplementedException();
        public Task<Result<bool>> CancelDiasporaPaymentAsync(int transactionId, string reason = "") => throw new NotImplementedException();
        public Task<Result<bool>> RetryFailedDiasporaPaymentAsync(int transactionId) => throw new NotImplementedException();
        public Task<Result<string>> GeneratePaymentReceiptAsync(int transactionId, string format = "PDF") => throw new NotImplementedException();
        public Task<Result<bool>> SendPaymentConfirmationEmailAsync(int transactionId) => throw new NotImplementedException();
        public Task<Result<List<string>>> GetPaymentInstructionsAsync(PaymentProvider provider, string countryCode) => throw new NotImplementedException();

        // Helper methods
        private async Task<decimal> GetPaymentAmountAsync(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            return payment?.Amount ?? 0;
        }

        private PaymentProvider DetermineOptimalProvider(string? countryCode, string currency, DiasporaCountryInfoDto countryInfo)
        {
            // Prefer PayPal for most international payments, Stripe for cards
            if (countryInfo.AvailableProviders.Contains(PaymentProvider.PayPal) && 
                (currency == "USD" || currency == "EUR" || currency == "GBP"))
            {
                return PaymentProvider.PayPal;
            }

            if (countryInfo.AvailableProviders.Contains(PaymentProvider.Stripe))
            {
                return PaymentProvider.Stripe;
            }

            return countryInfo.AvailableProviders.FirstOrDefault();
        }

        private string GetDiasporaStatusDescription(PaymentTransactionStatus status)
        {
            return status switch
            {
                PaymentTransactionStatus.Initiated => "Payment initiated - redirecting to payment provider",
                PaymentTransactionStatus.Pending => "Waiting for payment completion",
                PaymentTransactionStatus.Processing => "Payment being processed",
                PaymentTransactionStatus.Completed => "Payment completed successfully",
                PaymentTransactionStatus.Failed => "Payment failed - please try again",
                PaymentTransactionStatus.Cancelled => "Payment cancelled",
                PaymentTransactionStatus.Expired => "Payment session expired",
                PaymentTransactionStatus.Refunded => "Payment refunded",
                _ => status.ToString()
            };
        }

        private string? GetPaymentUrlFromAdditionalData(PaymentTransactionDto transaction)
        {
            // Parse additional data to extract payment URL for PayPal
            return null; // Implementation would parse JSON metadata
        }

        private string? GetClientSecretFromAdditionalData(PaymentTransactionDto transaction)
        {
            // Parse additional data to extract client secret for Stripe
            return null; // Implementation would parse JSON metadata
        }

        private bool IsDiasporaPayment(PaymentTransaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.Metadata))
                return false;

            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.Metadata);
                return metadata?.ContainsKey("diaspora_client") == true;
            }
            catch
            {
                return false;
            }
        }

        private DiasporaPaymentStatusDto BuildDiasporaPaymentStatus(PaymentTransaction transaction)
        {
            var metadata = ParseMetadata(transaction.Metadata);
            
            return new DiasporaPaymentStatusDto
            {
                PaymentTransactionId = transaction.PaymentTransactionId,
                TransactionReference = transaction.TransactionReference,
                Provider = transaction.Provider,
                Status = transaction.Status,
                StatusDescription = GetDiasporaStatusDescription(transaction.Status),
                OriginalAmountSLE = GetMetadataValue(metadata, "original_amount_sle", transaction.Amount),
                ConvertedAmount = transaction.NetAmount ?? transaction.Amount,
                ConvertedCurrency = GetMetadataValue(metadata, "preferred_currency", "USD"),
                CreatedDate = transaction.CreatedDate,
                CompletedDate = transaction.CompletedDate,
                ExpiryDate = transaction.ExpiryDate,
                ProviderTransactionId = transaction.ProviderTransactionId,
                FailureReason = transaction.FailureReason,
                ProgressPercentage = CalculateProgressPercentage(transaction.Status),
                ProgressSteps = BuildProgressSteps(transaction.Status),
                CanCancel = CanCancelTransaction(transaction.Status),
                CanRefund = CanRefundTransaction(transaction.Status),
                AvailableActions = GetAvailableActions(transaction.Status)
            };
        }

        private Dictionary<string, object> ParseMetadata(string? metadata)
        {
            try
            {
                return string.IsNullOrEmpty(metadata) ? new Dictionary<string, object>() :
                    JsonSerializer.Deserialize<Dictionary<string, object>>(metadata) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        private T GetMetadataValue<T>(Dictionary<string, object> metadata, string key, T defaultValue)
        {
            if (metadata.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private int CalculateProgressPercentage(PaymentTransactionStatus status)
        {
            return status switch
            {
                PaymentTransactionStatus.Initiated => 25,
                PaymentTransactionStatus.Pending => 50,
                PaymentTransactionStatus.Processing => 75,
                PaymentTransactionStatus.Completed => 100,
                PaymentTransactionStatus.Failed => 0,
                PaymentTransactionStatus.Cancelled => 0,
                PaymentTransactionStatus.Expired => 0,
                _ => 0
            };
        }

        private List<PaymentProgressStep> BuildProgressSteps(PaymentTransactionStatus status)
        {
            var steps = new List<PaymentProgressStep>
            {
                new() { StepName = "Initiated", Description = "Payment request created", Status = PaymentStepStatus.Completed },
                new() { StepName = "Provider Processing", Description = "Sent to payment provider", Status = GetStepStatus(status, PaymentTransactionStatus.Pending) },
                new() { StepName = "Customer Action", Description = "Customer completing payment", Status = GetStepStatus(status, PaymentTransactionStatus.Processing) },
                new() { StepName = "Completed", Description = "Payment confirmed and processed", Status = GetStepStatus(status, PaymentTransactionStatus.Completed) }
            };

            return steps;
        }

        private PaymentStepStatus GetStepStatus(PaymentTransactionStatus currentStatus, PaymentTransactionStatus stepStatus)
        {
            if (currentStatus == PaymentTransactionStatus.Failed || currentStatus == PaymentTransactionStatus.Cancelled)
                return PaymentStepStatus.Failed;
            
            if (currentStatus >= stepStatus)
                return PaymentStepStatus.Completed;
            
            if (currentStatus == stepStatus)
                return PaymentStepStatus.InProgress;
            
            return PaymentStepStatus.Pending;
        }

        private bool CanCancelTransaction(PaymentTransactionStatus status)
        {
            return status == PaymentTransactionStatus.Initiated || status == PaymentTransactionStatus.Pending;
        }

        private bool CanRefundTransaction(PaymentTransactionStatus status)
        {
            return status == PaymentTransactionStatus.Completed;
        }

        private List<string> GetAvailableActions(PaymentTransactionStatus status)
        {
            var actions = new List<string>();
            
            if (CanCancelTransaction(status))
                actions.Add("cancel");
            
            if (CanRefundTransaction(status))
                actions.Add("refund");
            
            if (status == PaymentTransactionStatus.Failed)
                actions.Add("retry");
            
            actions.Add("view_details");
            actions.Add("download_receipt");
            
            return actions;
        }

        private string GenerateReturnUrl(string provider, string type)
        {
            return $"https://ctis.bettsfirm.sl/payments/{provider}/{type}";
        }

        private Dictionary<string, DiasporaCountryInfoDto> InitializeDiasporaCountries()
        {
            return new Dictionary<string, DiasporaCountryInfoDto>
            {
                ["US"] = new()
                {
                    CountryCode = "US",
                    CountryName = "United States",
                    Currency = "USD",
                    CurrencySymbol = "$",
                    AvailableProviders = new() { PaymentProvider.PayPal, PaymentProvider.Stripe },
                    PopularPaymentMethods = new() { "Credit Card", "PayPal", "Bank Transfer" },
                    EstimatedProcessingTime = 24,
                    RequiredFields = new() { "email", "name" },
                    SpecialInstructions = "US residents can use PayPal or credit/debit cards for fast processing."
                },
                ["GB"] = new()
                {
                    CountryCode = "GB",
                    CountryName = "United Kingdom",
                    Currency = "GBP",
                    CurrencySymbol = "£",
                    AvailableProviders = new() { PaymentProvider.PayPal, PaymentProvider.Stripe },
                    PopularPaymentMethods = new() { "Credit Card", "PayPal", "Bank Transfer" },
                    EstimatedProcessingTime = 24,
                    RequiredFields = new() { "email", "name" }
                },
                ["CA"] = new()
                {
                    CountryCode = "CA",
                    CountryName = "Canada",
                    Currency = "CAD",
                    CurrencySymbol = "C$",
                    AvailableProviders = new() { PaymentProvider.PayPal, PaymentProvider.Stripe },
                    PopularPaymentMethods = new() { "Credit Card", "PayPal" },
                    EstimatedProcessingTime = 24,
                    RequiredFields = new() { "email", "name" }
                },
                ["AU"] = new()
                {
                    CountryCode = "AU",
                    CountryName = "Australia",
                    Currency = "AUD",
                    CurrencySymbol = "A$",
                    AvailableProviders = new() { PaymentProvider.PayPal, PaymentProvider.Stripe },
                    PopularPaymentMethods = new() { "Credit Card", "PayPal" },
                    EstimatedProcessingTime = 24,
                    RequiredFields = new() { "email", "name" }
                },
                ["DE"] = new()
                {
                    CountryCode = "DE",
                    CountryName = "Germany",
                    Currency = "EUR",
                    CurrencySymbol = "€",
                    AvailableProviders = new() { PaymentProvider.PayPal, PaymentProvider.Stripe },
                    PopularPaymentMethods = new() { "Credit Card", "PayPal", "SEPA" },
                    EstimatedProcessingTime = 48,
                    RequiredFields = new() { "email", "name" }
                }
            };
        }
    }
}