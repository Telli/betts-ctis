using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using BettsTax.Core.Services.Payments;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class OrangeMoneyProvider : BaseMobileMoneyProvider
    {
        private readonly HttpClient _httpClient;
        private new readonly ILogger<OrangeMoneyProvider> _logger;
        private new readonly PaymentProviderConfig _config;

        public OrangeMoneyProvider(
            HttpClient httpClient,
            ILogger<OrangeMoneyProvider> logger,
            PaymentProviderConfig config) : base(config, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public override PaymentProvider ProviderType => PaymentProvider.OrangeMoney;

        public override async Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request)
        {
            try
            {
                var phoneNumber = FormatPhoneNumber(request.CustomerPhone ?? string.Empty);

                // Validate Orange SL phone number (76, 77, 78, 79)
                if (!IsValidOrangePhoneNumber(phoneNumber))
                    return Result.Failure<PaymentGatewayResponse>("Invalid Orange Money phone number");

                // Orange Web Payment API request structure
                var apiRequest = new OrangeWebPaymentRequest
                {
                    order_id = request.TransactionReference,
                    amount = request.Amount,
                    currency = "SLE",
                    return_url = request.ReturnUrl ?? $"{_config.WebhookUrl}/payment/callback",
                    cancel_url = request.ReturnUrl ?? $"{_config.WebhookUrl}/payment/cancel",
                    notify_url = _config.WebhookUrl,
                    lang = "en",
                    reference = request.TransactionReference
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Orange Web Payment API headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Use official Orange Web Payment API endpoint
                var apiUrl = _config.ApiUrl?.TrimEnd('/') ?? "https://api.orange.com/orange-money-webpay/dev/v1";
                var response = await _httpClient.PostAsync($"{apiUrl}/webpayment", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Orange Web Payment API Response: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<OrangeWebPaymentResponse>(responseContent);

                    if (apiResponse?.pay_token != null)
                    {
                        return Result.Success(new PaymentGatewayResponse
                        {
                            Success = true,
                            TransactionId = apiResponse.order_id,
                            ProviderReference = apiResponse.pay_token,
                            Status = PaymentTransactionStatus.Initiated,
                            StatusMessage = "Payment initiated successfully",
                            Amount = request.Amount,
                            ExpiryDate = DateTime.UtcNow.AddMinutes(15), // Orange Money 15-minute timeout
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["pay_token"] = apiResponse.pay_token,
                                ["payment_url"] = apiResponse.payment_url ?? string.Empty,
                                ["order_id"] = apiResponse.order_id ?? string.Empty
                            }
                        });
                    }
                    else
                    {
                        return Result.Failure<PaymentGatewayResponse>(
                            $"Orange Web Payment API Error: Invalid response format");
                    }
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<OrangeWebPaymentError>(responseContent);

                    return Result.Failure<PaymentGatewayResponse>(
                        $"Orange Web Payment API Error: {errorResponse?.message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Orange Web Payment for {Reference}", request.TransactionReference);
                return Result.Failure<PaymentGatewayResponse>("Failed to initiate Orange Web Payment");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                // Use official Orange Web Payment API endpoint for status check
                var apiUrl = _config.ApiUrl?.TrimEnd('/') ?? "https://api.orange.com/orange-money-webpay/dev/v1";
                var response = await _httpClient.GetAsync($"{apiUrl}/transactionstatus?order_id={providerTransactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonSerializer.Deserialize<OrangeTransactionStatusResponse>(responseContent);

                    if (statusResponse != null)
                    {
                        return Result.Success(new PaymentGatewayResponse
                        {
                            Success = statusResponse.status == "SUCCESS",
                            TransactionId = statusResponse.order_id,
                            ProviderReference = statusResponse.pay_token,
                            Status = MapOrangeStatusToTransactionStatus(statusResponse.status),
                            StatusMessage = statusResponse.message,
                            Amount = decimal.TryParse(statusResponse.amount, out var amt) ? amt : null,
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["payment_date"] = statusResponse.payment_date ?? string.Empty,
                                ["pay_token"] = statusResponse.pay_token ?? string.Empty
                            }
                        });
                    }
                    else
                    {
                        return Result.Failure<PaymentGatewayResponse>("Invalid status response format");
                    }
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<OrangeWebPaymentError>(responseContent);
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Orange Web Payment Status Check Error: {errorResponse?.message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Orange Web Payment status for {TransactionId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to check Orange Web Payment status");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount)
        {
            // Orange Web Payment API may not support refunds directly
            // This is a placeholder implementation
            return Result.Failure<PaymentGatewayResponse>("Orange Web Payment API does not support refunds");
        }

        public override async Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.WebhookSecret))
                    return Result.Success(true); // No signature validation configured

                var expectedSignature = ComputeHmacSha256(_config.WebhookSecret, payload);
                return Result.Success(expectedSignature == signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                return Result.Success(false);
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData)
        {
            try
            {
                // Orange Web Payment API webhook structure
                var webhook = JsonSerializer.Deserialize<OrangeTransactionStatusResponse>(webhookData);

                if (webhook != null)
                {
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = webhook.status == "SUCCESS",
                        TransactionId = webhook.order_id,
                        ProviderReference = webhook.pay_token,
                        Status = MapOrangeStatusToTransactionStatus(webhook.status),
                        StatusMessage = webhook.message,
                        Amount = decimal.TryParse(webhook.amount, out var amt) ? amt : null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["payment_date"] = webhook.payment_date ?? string.Empty,
                            ["webhook_processed"] = true
                        }
                    });
                }
                else
                {
                    return Result.Failure<PaymentGatewayResponse>("Invalid webhook data format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Orange Web Payment webhook");
                return Result.Failure<PaymentGatewayResponse>("Failed to process webhook");
            }
        }

        public override async Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber)
        {
            return await Task.FromResult(Result.Success(IsValidOrangePhoneNumber(FormatPhoneNumber(phoneNumber))));
        }

        public override async Task<Result<decimal>> GetBalanceAsync()
        {
            // Orange Web Payment API may not provide balance endpoint
            // Return 0 as balance is not available
            return await Task.FromResult(Result.Success(0m));
        }

        public override async Task<Result<MobileMoneyAccountDto>> GetAccountInfoAsync(string phoneNumber)
        {
            var formattedPhone = FormatPhoneNumber(phoneNumber);
            if (!IsValidOrangePhoneNumber(formattedPhone))
            {
                return await Task.FromResult(Result.Failure<MobileMoneyAccountDto>("Invalid Orange Money phone number"));
            }

            return await Task.FromResult(Result.Success(new MobileMoneyAccountDto
            {
                PhoneNumber = formattedPhone,
                Provider = PaymentProvider.OrangeMoney
            }));
        }

        // Orange Money specific helper methods
        private bool IsValidOrangePhoneNumber(string phoneNumber)
        {
            // Orange SL prefixes: 76, 77, 78, 79
            if (phoneNumber.StartsWith("232"))
            {
                var prefix = phoneNumber.Substring(3, 2);
                return prefix == "76" || prefix == "77" || prefix == "78" || prefix == "79";
            }
            return false;
        }

        private PaymentTransactionStatus MapOrangeStatusToTransactionStatus(string orangeStatus)
        {
            return orangeStatus?.ToUpper() switch
            {
                "SUCCESS" => PaymentTransactionStatus.Completed,
                "PENDING" => PaymentTransactionStatus.Pending,
                "FAILED" => PaymentTransactionStatus.Failed,
                "CANCELLED" => PaymentTransactionStatus.Cancelled,
                "EXPIRED" => PaymentTransactionStatus.Expired,
                "INITIATED" => PaymentTransactionStatus.Initiated,
                "PROCESSING" => PaymentTransactionStatus.Processing,
                _ => PaymentTransactionStatus.Pending
            };
        }

        private string ComputeHmacSha256(string secret, string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    // Base class for mobile money providers
    public abstract class BaseMobileMoneyProvider : IMobileMoneyProvider
    {
        protected readonly PaymentProviderConfig _config;
        protected readonly ILogger _logger;

        protected BaseMobileMoneyProvider(PaymentProviderConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public abstract PaymentProvider ProviderType { get; }
        public abstract Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request);
        public abstract Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId);
        public abstract Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount);
        public abstract Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature);
        public abstract Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData);
        public abstract Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber);
        public abstract Task<Result<decimal>> GetBalanceAsync();
        public abstract Task<Result<MobileMoneyAccountDto>> GetAccountInfoAsync(string phoneNumber);

        public virtual async Task<Result<PaymentGatewayResponse>> SendPaymentRequestAsync(string phoneNumber, decimal amount, string reference)
        {
            var request = new PaymentGatewayRequest
            {
                TransactionReference = reference,
                Amount = amount,
                CustomerPhone = phoneNumber,
                CustomerName = "Customer"
            };

            return await InitiatePaymentAsync(request);
        }

        public virtual async Task<Result<bool>> TestConnectionAsync()
        {
            try
            {
                // Test with balance check or similar lightweight operation
                var balanceResult = await GetBalanceAsync();
                return Result.Success(balanceResult.IsSuccess);
            }
            catch
            {
                return Result.Success(false);
            }
        }

        protected string FormatPhoneNumber(string phoneNumber)
        {
            // Remove any non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // Add country code if not present (Sierra Leone: 232)
            if (!digits.StartsWith("232") && digits.Length == 8)
            {
                digits = "232" + digits;
            }
            
            return digits;
        }
    }

    // Orange Web Payment API request/response models
    internal class OrangeWebPaymentRequest
    {
        public string order_id { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string currency { get; set; } = "SLE";
        public string return_url { get; set; } = string.Empty;
        public string cancel_url { get; set; } = string.Empty;
        public string notify_url { get; set; } = string.Empty;
        public string lang { get; set; } = "en";
        public string reference { get; set; } = string.Empty;
    }

    internal class OrangeWebPaymentResponse
    {
        public string pay_token { get; set; } = string.Empty;
        public string payment_url { get; set; } = string.Empty;
        public string order_id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    internal class OrangeTransactionStatusResponse
    {
        public string order_id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string currency { get; set; } = "SLE";
        public string pay_token { get; set; } = string.Empty;
        public string payment_date { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }

    internal class OrangeWebPaymentError
    {
        public string code { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string details { get; set; } = string.Empty;
    }
}