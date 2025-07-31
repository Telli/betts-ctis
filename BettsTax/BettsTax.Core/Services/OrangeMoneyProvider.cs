using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
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
                var phoneNumber = FormatPhoneNumber(request.CustomerPhone);
                
                // Validate Orange SL phone number (76, 77, 78, 79)
                if (!IsValidOrangePhoneNumber(phoneNumber))
                    return Result.Failure<PaymentGatewayResponse>("Invalid Orange Money phone number");

                var apiRequest = new
                {
                    transaction_id = request.TransactionReference,
                    amount = request.Amount.ToString("F2"),
                    currency = "SLE",
                    customer_phone = phoneNumber,
                    customer_name = request.CustomerName,
                    description = request.Description ?? "Tax Payment",
                    callback_url = request.CallbackUrl,
                    return_url = request.ReturnUrl
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add Orange Money API headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-Orange-Partner-Id", _config.MerchantId);

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/payments/initiate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Orange Money API Response: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<OrangeMoneyInitiateResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = true,
                        TransactionId = apiResponse.TransactionId,
                        ProviderReference = apiResponse.OrangeReference,
                        Status = MapOrangeStatusToTransactionStatus(apiResponse.Status),
                        StatusMessage = apiResponse.Message,
                        Amount = decimal.TryParse(apiResponse.Amount, out var amt) ? amt : request.Amount,
                        Fee = decimal.TryParse(apiResponse.Fee, out var fee) ? fee : null,
                        ExpiryDate = DateTime.UtcNow.AddMinutes(15), // Orange Money 15-minute timeout
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["orange_reference"] = apiResponse.OrangeReference ?? "",
                            ["payment_token"] = apiResponse.PaymentToken ?? "",
                            ["ussd_code"] = apiResponse.UssdCode ?? ""
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<OrangeMoneyErrorResponse>(responseContent);
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Orange Money API Error: {errorResponse?.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Orange Money payment for {Reference}", request.TransactionReference);
                return Result.Failure<PaymentGatewayResponse>("Failed to initiate Orange Money payment");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-Orange-Partner-Id", _config.MerchantId);

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/payments/{providerTransactionId}/status");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonSerializer.Deserialize<OrangeMoneyStatusResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = statusResponse.Status == "COMPLETED",
                        TransactionId = statusResponse.TransactionId,
                        ProviderReference = statusResponse.OrangeReference,
                        Status = MapOrangeStatusToTransactionStatus(statusResponse.Status),
                        StatusMessage = statusResponse.Message,
                        Amount = decimal.TryParse(statusResponse.Amount, out var amt) ? amt : null,
                        Fee = decimal.TryParse(statusResponse.Fee, out var fee) ? fee : null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["last_updated"] = statusResponse.LastUpdated ?? DateTime.UtcNow.ToString(),
                            ["orange_reference"] = statusResponse.OrangeReference ?? ""
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<OrangeMoneyErrorResponse>(responseContent);
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Orange Money Status Check Error: {errorResponse?.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Orange Money payment status for {TransactionId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to check Orange Money payment status");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount)
        {
            try
            {
                var apiRequest = new
                {
                    transaction_id = providerTransactionId,
                    refund_amount = amount.ToString("F2"),
                    reason = "Tax payment refund"
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-Orange-Partner-Id", _config.MerchantId);

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/payments/{providerTransactionId}/refund", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refundResponse = JsonSerializer.Deserialize<OrangeMoneyRefundResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = refundResponse.Status == "REFUNDED",
                        TransactionId = refundResponse.RefundId,
                        ProviderReference = refundResponse.OrangeReference,
                        Status = refundResponse.Status == "REFUNDED" ? 
                            PaymentTransactionStatus.Refunded : PaymentTransactionStatus.Processing,
                        StatusMessage = refundResponse.Message,
                        Amount = decimal.TryParse(refundResponse.RefundAmount, out var amt) ? amt : amount
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<OrangeMoneyErrorResponse>(responseContent);
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Orange Money Refund Error: {errorResponse?.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Orange Money refund for {TransactionId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to process Orange Money refund");
            }
        }

        public override async Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.WebhookSecret))
                    return Result.Failure<bool>("Webhook secret not configured");

                // Orange Money webhook signature validation
                var computedSignature = ComputeHmacSha256(_config.WebhookSecret, payload);
                var isValid = string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
                
                return Result.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Orange Money webhook signature");
                return Result.Failure<bool>("Failed to validate webhook signature");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<OrangeMoneyWebhook>(webhookData);
                if (webhook == null)
                    return Result.Failure<PaymentGatewayResponse>("Invalid webhook data");

                return Result.Success(new PaymentGatewayResponse
                {
                    Success = webhook.Status == "COMPLETED",
                    TransactionId = webhook.TransactionId,
                    ProviderReference = webhook.OrangeReference,
                    Status = MapOrangeStatusToTransactionStatus(webhook.Status),
                    StatusMessage = webhook.Message,
                    Amount = decimal.TryParse(webhook.Amount, out var amt) ? amt : null,
                    Fee = decimal.TryParse(webhook.Fee, out var fee) ? fee : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Orange Money webhook");
                return Result.Failure<PaymentGatewayResponse>("Failed to process webhook");
            }
        }

        public override async Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber)
        {
            var formatted = FormatPhoneNumber(phoneNumber);
            return Result.Success(IsValidOrangePhoneNumber(formatted));
        }

        public override async Task<Result<decimal>> GetBalanceAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-Orange-Partner-Id", _config.MerchantId);

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/account/balance");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var balanceResponse = JsonSerializer.Deserialize<OrangeMoneyBalanceResponse>(responseContent);
                    return Result.Success(decimal.TryParse(balanceResponse.Balance, out var balance) ? balance : 0);
                }

                return Result.Failure<decimal>("Failed to retrieve balance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Orange Money balance");
                return Result.Failure<decimal>("Failed to retrieve balance");
            }
        }

        public override async Task<Result<MobileMoneyAccountDto>> GetAccountInfoAsync(string phoneNumber)
        {
            try
            {
                var formatted = FormatPhoneNumber(phoneNumber);
                
                if (!IsValidOrangePhoneNumber(formatted))
                    return Result.Failure<MobileMoneyAccountDto>("Invalid Orange Money phone number");

                // Orange Money doesn't typically provide account lookup
                // Return basic validation result
                return Result.Success(new MobileMoneyAccountDto
                {
                    PhoneNumber = formatted,
                    Provider = PaymentProvider.OrangeMoney,
                    IsActive = true,
                    CanReceivePayments = true,
                    MaxTransactionAmount = 50000, // 50,000 SLE typical limit
                    DailyLimit = 100000 // 100,000 SLE typical daily limit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Orange Money account info for {PhoneNumber}", phoneNumber);
                return Result.Failure<MobileMoneyAccountDto>("Failed to retrieve account info");
            }
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
                "INITIATED" => PaymentTransactionStatus.Initiated,
                "PENDING" => PaymentTransactionStatus.Pending,
                "PROCESSING" => PaymentTransactionStatus.Processing,
                "COMPLETED" => PaymentTransactionStatus.Completed,
                "FAILED" => PaymentTransactionStatus.Failed,
                "CANCELLED" => PaymentTransactionStatus.Cancelled,
                "EXPIRED" => PaymentTransactionStatus.Expired,
                "REFUNDED" => PaymentTransactionStatus.Refunded,
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

    // Orange Money API response models
    internal class OrangeMoneyInitiateResponse
    {
        public string TransactionId { get; set; } = "";
        public string OrangeReference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Fee { get; set; } = "";
        public string PaymentToken { get; set; } = "";
        public string UssdCode { get; set; } = "";
    }

    internal class OrangeMoneyStatusResponse
    {
        public string TransactionId { get; set; } = "";
        public string OrangeReference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Fee { get; set; } = "";
        public string LastUpdated { get; set; } = "";
    }

    internal class OrangeMoneyRefundResponse
    {
        public string RefundId { get; set; } = "";
        public string OrangeReference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string RefundAmount { get; set; } = "";
    }

    internal class OrangeMoneyBalanceResponse
    {
        public string Balance { get; set; } = "";
        public string Currency { get; set; } = "";
        public string LastUpdated { get; set; } = "";
    }

    internal class OrangeMoneyWebhook
    {
        public string TransactionId { get; set; } = "";
        public string OrangeReference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Fee { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }

    internal class OrangeMoneyErrorResponse
    {
        public string ErrorCode { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string Details { get; set; } = "";
    }
}