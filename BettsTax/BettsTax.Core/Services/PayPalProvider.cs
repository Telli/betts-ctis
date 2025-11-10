using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
// Explicitly alias unified gateway types
using PaymentGatewayRequest = BettsTax.Core.Services.Payments.PaymentGatewayRequest;
using PaymentGatewayResponse = BettsTax.Core.Services.Payments.PaymentGatewayResponse;

namespace BettsTax.Core.Services
{
    public class PayPalProvider : IPaymentGatewayProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayPalProvider> _logger;
        private readonly PaymentProviderConfig _config;

        public PayPalProvider(
            HttpClient httpClient,
            ILogger<PayPalProvider> logger,
            PaymentProviderConfig config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public PaymentProvider ProviderType => PaymentProvider.PayPal;

        public async Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request)
        {
            try
            {
                // Get PayPal access token
                var tokenResult = await GetAccessTokenAsync();
                if (!tokenResult.IsSuccess)
                    return Result.Failure<PaymentGatewayResponse>($"Failed to get PayPal access token: {tokenResult.ErrorMessage}");

                var accessToken = tokenResult.Value;

                // Convert SLE to USD (approximate rate - should be fetched from exchange rate API)
                var amountUSD = ConvertSLEToUSD(request.Amount);

                var paymentRequest = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = request.TransactionReference,
                            amount = new
                            {
                                currency_code = "USD",
                                value = amountUSD.ToString("F2")
                            },
                            description = request.Description ?? "Tax payment for Sierra Leone tax filing",
                            custom_id = request.TransactionReference,
                            soft_descriptor = "BETTS_TAX_SL"
                        }
                    },
                    payment_source = new
                    {
                        paypal = new
                        {
                            experience_context = new
                            {
                                payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                                brand_name = "The Betts Firm - Sierra Leone Tax Services",
                                locale = "en-US",
                                landing_page = "LOGIN",
                                shipping_preference = "NO_SHIPPING",
                                user_action = "PAY_NOW",
                                return_url = request.ReturnUrl ?? $"{GetBaseUrl()}/payment/paypal/success",
                                cancel_url = $"{GetBaseUrl()}/payment/paypal/cancel"
                            }
                        }
                    },
                    application_context = new
                    {
                        brand_name = "The Betts Firm",
                        landing_page = "BILLING",
                        shipping_preference = "NO_SHIPPING",
                        user_action = "PAY_NOW"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                _httpClient.DefaultRequestHeaders.Add("PayPal-Request-Id", Guid.NewGuid().ToString());

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/v2/checkout/orders", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayPal Order Creation Response: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var paypalResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    if (paypalResponse == null)
                        return Result.Failure<PaymentGatewayResponse>("Invalid PayPal response");

                    // Get approval URL from links
                    var approvalUrl = paypalResponse.Links?
                        .FirstOrDefault(l => l.Rel == "approve")?.Href;

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = paypalResponse.Status == "CREATED",
                        TransactionId = paypalResponse.Id,
                        ProviderReference = paypalResponse.Id,
                        Status = MapPayPalStatusToTransactionStatus(paypalResponse.Status),
                        StatusMessage = $"PayPal order created - {paypalResponse.Status}",
                        Amount = amountUSD,
                        PaymentUrl = approvalUrl,
                        ExpiryDate = DateTime.UtcNow.AddHours(3), // PayPal orders expire in 3 hours
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["paypal_order_id"] = paypalResponse.Id,
                            ["approval_url"] = approvalUrl ?? "",
                            ["original_amount_sle"] = request.Amount,
                            ["exchange_rate"] = GetUSDExchangeRate(),
                            ["currency_usd"] = amountUSD
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<PayPalErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"PayPal API Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating PayPal payment for {Reference}", request.TransactionReference);
                return Result.Failure<PaymentGatewayResponse>("Failed to initiate PayPal payment");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId)
        {
            try
            {
                var tokenResult = await GetAccessTokenAsync();
                if (!tokenResult.IsSuccess)
                    return Result.Failure<PaymentGatewayResponse>($"Failed to get PayPal access token: {tokenResult.ErrorMessage}");

                var accessToken = tokenResult.Value;

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/v2/checkout/orders/{providerTransactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    if (orderResponse == null)
                        return Result.Failure<PaymentGatewayResponse>("Invalid PayPal order response");

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = orderResponse.Status == "COMPLETED",
                        TransactionId = orderResponse.Id,
                        ProviderReference = orderResponse.Id,
                        Status = MapPayPalStatusToTransactionStatus(orderResponse.Status),
                        StatusMessage = orderResponse.Status,
                        Amount = GetAmountFromOrder(orderResponse),
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["paypal_status"] = orderResponse.Status,
                            ["create_time"] = orderResponse.CreateTime ?? "",
                            ["update_time"] = orderResponse.UpdateTime ?? ""
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<PayPalErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"PayPal Status Check Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking PayPal payment status for {OrderId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to check PayPal payment status");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount)
        {
            try
            {
                var tokenResult = await GetAccessTokenAsync();
                if (!tokenResult.IsSuccess)
                    return Result.Failure<PaymentGatewayResponse>($"Failed to get PayPal access token: {tokenResult.ErrorMessage}");

                var accessToken = tokenResult.Value;

                // First, get the capture ID from the order
                var captureId = await GetCaptureIdFromOrderAsync(providerTransactionId, accessToken);
                if (string.IsNullOrEmpty(captureId))
                    return Result.Failure<PaymentGatewayResponse>("Could not find capture ID for refund");

                var amountUSD = ConvertSLEToUSD(amount);
                var refundRequest = new
                {
                    amount = new
                    {
                        currency_code = "USD",
                        value = amountUSD.ToString("F2")
                    },
                    note_to_payer = "Refund for Sierra Leone tax payment"
                };

                var jsonContent = JsonSerializer.Serialize(refundRequest, 
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/v2/payments/captures/{captureId}/refund", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refundResponse = JsonSerializer.Deserialize<PayPalRefundResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = refundResponse?.Status == "COMPLETED",
                        TransactionId = refundResponse?.Id,
                        ProviderReference = refundResponse?.Id,
                        Status = refundResponse?.Status == "COMPLETED" ? 
                            PaymentTransactionStatus.Refunded : PaymentTransactionStatus.Processing,
                        StatusMessage = refundResponse?.Status ?? "Refund processed",
                        Amount = amountUSD
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<PayPalErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"PayPal Refund Error: {errorResponse?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal refund for {OrderId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to process PayPal refund");
            }
        }

        public async Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                // PayPal webhook verification requires calling their verification API
                var tokenResult = await GetAccessTokenAsync();
                if (!tokenResult.IsSuccess)
                    return Result.Failure<bool>($"Failed to get access token: {tokenResult.ErrorMessage}");

                var verificationRequest = new
                {
                    auth_algo = "SHA256withRSA",
                    cert_id = ExtractCertIdFromHeaders(),
                    transmission_id = ExtractTransmissionIdFromHeaders(),
                    transmission_sig = signature,
                    transmission_time = ExtractTransmissionTimeFromHeaders(),
                    webhook_id = _config.WebhookSecret,
                    webhook_event = JsonSerializer.Deserialize<object>(payload)
                };

                var jsonContent = JsonSerializer.Serialize(verificationRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResult.Value}");


                return Result.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating PayPal webhook signature");
                return Result.Failure<bool>("Failed to validate webhook signature");
            }
        }

        public async Task<Result<bool>> TestConnectionAsync()
        {
            try
            {
                var tokenResult = await GetAccessTokenAsync();
                return Result.Success(tokenResult.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing PayPal connection");
                return Result.Failure<bool>("Failed to test PayPal connection");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<PayPalWebhookEvent>(webhookData,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                if (webhook == null)
                    return Result.Failure<PaymentGatewayResponse>("Invalid webhook data");

                var status = webhook.EventType switch
                {
                    "CHECKOUT.ORDER.APPROVED" => PaymentTransactionStatus.Processing,
                    "PAYMENT.CAPTURE.COMPLETED" => PaymentTransactionStatus.Completed,
                    "PAYMENT.CAPTURE.DENIED" => PaymentTransactionStatus.Failed,
                    "CHECKOUT.ORDER.CANCELLED" => PaymentTransactionStatus.Cancelled,
                    "PAYMENT.CAPTURE.REFUNDED" => PaymentTransactionStatus.Refunded,
                    _ => PaymentTransactionStatus.Pending
                };

                return Result.Success(new PaymentGatewayResponse
                {
                    Success = status == PaymentTransactionStatus.Completed,
                    TransactionId = webhook.Resource?.Id,
                    ProviderReference = webhook.Resource?.Id,
                    Status = status,
                    StatusMessage = webhook.EventType,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["event_type"] = webhook.EventType,
                        ["event_id"] = webhook.Id ?? "",
                        ["create_time"] = webhook.CreateTime ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal webhook");
                return Result.Failure<PaymentGatewayResponse>("Failed to process webhook");
            }
        }

        // Duplicate removed: TestConnectionAsync already defined above

        // Helper methods
        private async Task<Result<string>> GetAccessTokenAsync()
        {
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:{_config.ApiSecret}"));
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/v1/oauth2/token", tokenRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Success(tokenResponse?.AccessToken ?? "");
                }

                return Result.Failure<string>("Failed to get PayPal access token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal access token");
                return Result.Failure<string>("Failed to get access token");
            }
        }

        private decimal ConvertSLEToUSD(decimal sleAmount)
        {
            // Get current exchange rate (this should be from a real exchange rate service)
            var exchangeRate = GetUSDExchangeRate();
            return sleAmount / exchangeRate;
        }

        private decimal GetUSDExchangeRate()
        {
            // Approximate SLE to USD rate - should be fetched from live exchange rate API
            // As of 2024: 1 USD ≈ 20,000-25,000 SLE (highly volatile)
            return 22000m; // This should be dynamic from exchange rate service
        }

        private PaymentTransactionStatus MapPayPalStatusToTransactionStatus(string paypalStatus)
        {
            return paypalStatus?.ToUpper() switch
            {
                "CREATED" => PaymentTransactionStatus.Initiated,
                "SAVED" => PaymentTransactionStatus.Pending,
                "APPROVED" => PaymentTransactionStatus.Processing,
                "VOIDED" => PaymentTransactionStatus.Cancelled,
                "COMPLETED" => PaymentTransactionStatus.Completed,
                "PAYER_ACTION_REQUIRED" => PaymentTransactionStatus.Pending,
                _ => PaymentTransactionStatus.Pending
            };
        }

        private decimal GetAmountFromOrder(PayPalOrderResponse order)
        {
            if (order.PurchaseUnits?.Any() == true)
            {
                var amount = order.PurchaseUnits.First().Amount;
                if (decimal.TryParse(amount?.Value, out var value))
                    return value;
            }
            return 0;
        }

        private async Task<string?> GetCaptureIdFromOrderAsync(string orderId, string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/v2/checkout/orders/{orderId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var order = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    return order?.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault()?.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting capture ID for order {OrderId}", orderId);
            }

            return null;
        }

        private string GetBaseUrl()
        {
            // This should come from configuration
            return "https://ctis.bettsfirm.sl";
        }

        private string ExtractCertIdFromHeaders() => ""; // Extract from HTTP headers
        private string ExtractTransmissionIdFromHeaders() => ""; // Extract from HTTP headers
        private string ExtractTransmissionTimeFromHeaders() => ""; // Extract from HTTP headers
    }

    // PayPal API response models
    internal class PayPalTokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "";
        public int ExpiresIn { get; set; }
    }

    internal class PayPalOrderResponse
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public string CreateTime { get; set; } = "";
        public string UpdateTime { get; set; } = "";
        public PayPalLink[]? Links { get; set; }
        public PayPalPurchaseUnit[]? PurchaseUnits { get; set; }
    }

    internal class PayPalLink
    {
        public string Href { get; set; } = "";
        public string Rel { get; set; } = "";
        public string Method { get; set; } = "";
    }

    internal class PayPalPurchaseUnit
    {
        public PayPalAmount? Amount { get; set; }
        public PayPalPayments? Payments { get; set; }
    }

    internal class PayPalAmount
    {
        public string CurrencyCode { get; set; } = "";
        public string Value { get; set; } = "";
    }

    internal class PayPalPayments
    {
        public PayPalCapture[]? Captures { get; set; }
    }

    internal class PayPalCapture
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
    }

    internal class PayPalRefundResponse
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
    }

    internal class PayPalErrorResponse
    {
        public string Name { get; set; } = "";
        public string Message { get; set; } = "";
        public PayPalErrorDetail[]? Details { get; set; }
    }

    internal class PayPalErrorDetail
    {
        public string Issue { get; set; } = "";
        public string Description { get; set; } = "";
    }

    internal class PayPalWebhookEvent
    {
        public string Id { get; set; } = "";
        public string EventType { get; set; } = "";
        public string CreateTime { get; set; } = "";
        public PayPalWebhookResource? Resource { get; set; }
    }

    internal class PayPalWebhookResource
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
    }

    internal class PayPalWebhookVerificationResponse
    {
        public string VerificationStatus { get; set; } = "";
    }
}