using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class AfricellMoneyProvider : BaseMobileMoneyProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AfricellMoneyProvider> _logger;
        private readonly PaymentProviderConfig _config;

        public AfricellMoneyProvider(
            HttpClient httpClient,
            ILogger<AfricellMoneyProvider> logger,
            PaymentProviderConfig config) : base(config, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public override PaymentProvider ProviderType => PaymentProvider.AfricellMoney;

        public override async Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request)
        {
            try
            {
                var phoneNumber = FormatPhoneNumber(request.CustomerPhone);
                
                // Validate Africell SL phone number (77, 78, 79, 88)
                if (!IsValidAfricellPhoneNumber(phoneNumber))
                    return Result.Failure<PaymentGatewayResponse>("Invalid Africell Money phone number");

                var apiRequest = new
                {
                    merchant_id = _config.MerchantId,
                    transaction_ref = request.TransactionReference,
                    amount = request.Amount,
                    currency = "SLE",
                    msisdn = phoneNumber,
                    customer_name = request.CustomerName,
                    description = request.Description ?? "Tax Payment",
                    callback_url = request.CallbackUrl,
                    return_url = request.ReturnUrl
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add Africell Money API headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-API-Version", "1.0");

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/payment/initiate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Africell Money API Response: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<AfricellMoneyInitiateResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = apiResponse.Status == "SUCCESS",
                        TransactionId = apiResponse.TransactionId,
                        ProviderReference = apiResponse.AfricellReference,
                        Status = MapAfricellStatusToTransactionStatus(apiResponse.Status),
                        StatusMessage = apiResponse.Message,
                        Amount = apiResponse.Amount,
                        Fee = apiResponse.Fee,
                        ExpiryDate = DateTime.UtcNow.AddMinutes(10), // Africell Money 10-minute timeout
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["africell_reference"] = apiResponse.AfricellReference ?? "",
                            ["payment_code"] = apiResponse.PaymentCode ?? "",
                            ["ussd_string"] = apiResponse.UssdString ?? ""
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<AfricellMoneyErrorResponse>(responseContent);
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Africell Money API Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Africell Money payment for {Reference}", request.TransactionReference);
                return Result.Failure<PaymentGatewayResponse>("Failed to initiate Africell Money payment");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-API-Version", "1.0");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/payment/{providerTransactionId}/status");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonSerializer.Deserialize<AfricellMoneyStatusResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = statusResponse.Status == "COMPLETED",
                        TransactionId = statusResponse.TransactionId,
                        ProviderReference = statusResponse.AfricellReference,
                        Status = MapAfricellStatusToTransactionStatus(statusResponse.Status),
                        StatusMessage = statusResponse.Message,
                        Amount = statusResponse.Amount,
                        Fee = statusResponse.Fee,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["completion_time"] = statusResponse.CompletionTime ?? "",
                            ["africell_reference"] = statusResponse.AfricellReference ?? ""
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<AfricellMoneyErrorResponse>(responseContent);
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Africell Money Status Check Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Africell Money payment status for {TransactionId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to check Africell Money payment status");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount)
        {
            try
            {
                var apiRequest = new
                {
                    transaction_id = providerTransactionId,
                    refund_amount = amount,
                    reason = "Tax payment refund"
                };

                var jsonContent = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_config.ApiKey}");

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/payment/{providerTransactionId}/refund", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refundResponse = JsonSerializer.Deserialize<AfricellMoneyRefundResponse>(responseContent);
                    
                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = refundResponse.Status == "REFUNDED",
                        TransactionId = refundResponse.RefundId,
                        ProviderReference = refundResponse.AfricellReference,
                        Status = refundResponse.Status == "REFUNDED" ? 
                            PaymentTransactionStatus.Refunded : PaymentTransactionStatus.Processing,
                        StatusMessage = refundResponse.Message,
                        Amount = refundResponse.RefundAmount
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<AfricellMoneyErrorResponse>(responseContent);
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Africell Money Refund Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Africell Money refund for {TransactionId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to process Africell Money refund");
            }
        }

        public override async Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.WebhookSecret))
                    return Result.Failure<bool>("Webhook secret not configured");

                // Africell Money webhook signature validation
                var computedSignature = ComputeHmacSha1(_config.WebhookSecret, payload);
                var isValid = string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
                
                return Result.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Africell Money webhook signature");
                return Result.Failure<bool>("Failed to validate webhook signature");
            }
        }

        public override async Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<AfricellMoneyWebhook>(webhookData);
                if (webhook == null)
                    return Result.Failure<PaymentGatewayResponse>("Invalid webhook data");

                return Result.Success(new PaymentGatewayResponse
                {
                    Success = webhook.Status == "COMPLETED",
                    TransactionId = webhook.TransactionId,
                    ProviderReference = webhook.AfricellReference,
                    Status = MapAfricellStatusToTransactionStatus(webhook.Status),
                    StatusMessage = webhook.Message,
                    Amount = webhook.Amount,
                    Fee = webhook.Fee
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Africell Money webhook");
                return Result.Failure<PaymentGatewayResponse>("Failed to process webhook");
            }
        }

        public override async Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber)
        {
            var formatted = FormatPhoneNumber(phoneNumber);
            return Result.Success(IsValidAfricellPhoneNumber(formatted));
        }

        public override async Task<Result<decimal>> GetBalanceAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_config.ApiKey}");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/merchant/balance");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var balanceResponse = JsonSerializer.Deserialize<AfricellMoneyBalanceResponse>(responseContent);
                    return Result.Success(balanceResponse.Balance);
                }

                return Result.Failure<decimal>("Failed to retrieve balance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Africell Money balance");
                return Result.Failure<decimal>("Failed to retrieve balance");
            }
        }

        public override async Task<Result<MobileMoneyAccountDto>> GetAccountInfoAsync(string phoneNumber)
        {
            try
            {
                var formatted = FormatPhoneNumber(phoneNumber);
                
                if (!IsValidAfricellPhoneNumber(formatted))
                    return Result.Failure<MobileMoneyAccountDto>("Invalid Africell Money phone number");

                // Africell Money account verification endpoint
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_config.ApiKey}");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/account/verify?msisdn={formatted}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var accountResponse = JsonSerializer.Deserialize<AfricellMoneyAccountResponse>(responseContent);
                    
                    return Result.Success(new MobileMoneyAccountDto
                    {
                        PhoneNumber = formatted,
                        AccountName = accountResponse.AccountName,
                        AccountNumber = accountResponse.AccountNumber,
                        Provider = PaymentProvider.AfricellMoney,
                        IsActive = accountResponse.IsActive,
                        CanReceivePayments = accountResponse.CanReceive,
                        MaxTransactionAmount = accountResponse.MaxTransactionAmount,
                        DailyLimit = accountResponse.DailyLimit
                    });
                }

                // If account verification fails, return basic info
                return Result.Success(new MobileMoneyAccountDto
                {
                    PhoneNumber = formatted,
                    Provider = PaymentProvider.AfricellMoney,
                    IsActive = true,
                    CanReceivePayments = true,
                    MaxTransactionAmount = 75000, // 75,000 SLE typical limit
                    DailyLimit = 150000 // 150,000 SLE typical daily limit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Africell Money account info for {PhoneNumber}", phoneNumber);
                return Result.Failure<MobileMoneyAccountDto>("Failed to retrieve account info");
            }
        }

        // Africell Money specific helper methods
        private bool IsValidAfricellPhoneNumber(string phoneNumber)
        {
            // Africell SL prefixes: 77, 78, 79, 88
            if (phoneNumber.StartsWith("232"))
            {
                var prefix = phoneNumber.Substring(3, 2);
                return prefix == "77" || prefix == "78" || prefix == "79" || prefix == "88";
            }
            return false;
        }

        private PaymentTransactionStatus MapAfricellStatusToTransactionStatus(string africellStatus)
        {
            return africellStatus?.ToUpper() switch
            {
                "INITIATED" => PaymentTransactionStatus.Initiated,
                "PENDING" => PaymentTransactionStatus.Pending,
                "PROCESSING" => PaymentTransactionStatus.Processing,
                "SUCCESS" => PaymentTransactionStatus.Completed,
                "COMPLETED" => PaymentTransactionStatus.Completed,
                "FAILED" => PaymentTransactionStatus.Failed,
                "CANCELLED" => PaymentTransactionStatus.Cancelled,
                "EXPIRED" => PaymentTransactionStatus.Expired,
                "REFUNDED" => PaymentTransactionStatus.Refunded,
                _ => PaymentTransactionStatus.Pending
            };
        }

        private string ComputeHmacSha1(string secret, string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using var hmac = new System.Security.Cryptography.HMACSHA1(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    // Africell Money API response models
    internal class AfricellMoneyInitiateResponse
    {
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public string AfricellReference { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string PaymentCode { get; set; } = "";
        public string UssdString { get; set; } = "";
    }

    internal class AfricellMoneyStatusResponse
    {
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public string AfricellReference { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string CompletionTime { get; set; } = "";
    }

    internal class AfricellMoneyRefundResponse
    {
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string RefundId { get; set; } = "";
        public string AfricellReference { get; set; } = "";
        public decimal RefundAmount { get; set; }
    }

    internal class AfricellMoneyBalanceResponse
    {
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "SLE";
        public string LastUpdated { get; set; } = "";
    }

    internal class AfricellMoneyAccountResponse
    {
        public string AccountName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public bool IsActive { get; set; }
        public bool CanReceive { get; set; }
        public decimal MaxTransactionAmount { get; set; }
        public decimal DailyLimit { get; set; }
    }

    internal class AfricellMoneyWebhook
    {
        public string TransactionId { get; set; } = "";
        public string AfricellReference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string Timestamp { get; set; } = "";
    }

    internal class AfricellMoneyErrorResponse
    {
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public string ErrorCode { get; set; } = "";
        public string Details { get; set; } = "";
    }
}