using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class StripeProvider : IPaymentGatewayProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StripeProvider> _logger;
        private readonly PaymentProviderConfig _config;

        public StripeProvider(
            HttpClient httpClient,
            ILogger<StripeProvider> logger,
            PaymentProviderConfig config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public PaymentProvider ProviderType => PaymentProvider.Stripe;

        public async Task<Result<PaymentGatewayResponse>> InitiatePaymentAsync(PaymentGatewayRequest request)
        {
            try
            {
                // Convert SLE to USD
                var amountUSD = ConvertSLEToUSD(request.Amount);
                var amountCents = (int)(amountUSD * 100); // Stripe uses cents

                // Create a PaymentIntent
                var paymentIntentRequest = new Dictionary<string, string>
                {
                    ["amount"] = amountCents.ToString(),
                    ["currency"] = "usd",
                    ["metadata[transaction_reference]"] = request.TransactionReference,
                    ["metadata[original_amount_sle]"] = request.Amount.ToString(),
                    ["metadata[exchange_rate]"] = GetUSDExchangeRate().ToString(),
                    ["metadata[client_name]"] = request.CustomerName,
                    ["description"] = request.Description ?? "Tax payment for Sierra Leone tax filing",
                    ["receipt_email"] = request.CustomerEmail ?? "",
                    ["statement_descriptor"] = "BETTS TAX SL",
                    ["statement_descriptor_suffix"] = "TAX PAYMENT"
                };

                // Add automatic payment methods
                paymentIntentRequest["automatic_payment_methods[enabled]"] = "true";
                paymentIntentRequest["automatic_payment_methods[allow_redirects]"] = "always";

                // Add return URLs for redirect-based payments
                if (!string.IsNullOrEmpty(request.ReturnUrl))
                {
                    paymentIntentRequest["return_url"] = request.ReturnUrl;
                }

                var formContent = new FormUrlEncodedContent(paymentIntentRequest);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/v1/payment_intents", formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Stripe PaymentIntent Response: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var stripeResponse = JsonSerializer.Deserialize<StripePaymentIntentResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    if (stripeResponse == null)
                        return Result.Failure<PaymentGatewayResponse>("Invalid Stripe response");

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = stripeResponse.Status != "failed",
                        TransactionId = stripeResponse.Id,
                        ProviderReference = stripeResponse.Id,
                        Status = MapStripeStatusToTransactionStatus(stripeResponse.Status),
                        StatusMessage = $"Stripe PaymentIntent {stripeResponse.Status}",
                        Amount = amountUSD,
                        ExpiryDate = DateTime.UtcNow.AddHours(24), // Stripe PaymentIntents expire in 24 hours by default
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["stripe_payment_intent_id"] = stripeResponse.Id,
                            ["client_secret"] = stripeResponse.ClientSecret ?? "",
                            ["original_amount_sle"] = request.Amount,
                            ["exchange_rate"] = GetUSDExchangeRate(),
                            ["currency_usd"] = amountUSD,
                            ["amount_cents"] = amountCents
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Stripe API Error: {errorResponse?.Error?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Stripe payment for {Reference}", request.TransactionReference);
                return Result.Failure<PaymentGatewayResponse>("Failed to initiate Stripe payment");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> CheckPaymentStatusAsync(string providerTransactionId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/v1/payment_intents/{providerTransactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var intentResponse = JsonSerializer.Deserialize<StripePaymentIntentResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    if (intentResponse == null)
                        return Result.Failure<PaymentGatewayResponse>("Invalid Stripe PaymentIntent response");

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = intentResponse.Status == "succeeded",
                        TransactionId = intentResponse.Id,
                        ProviderReference = intentResponse.Id,
                        Status = MapStripeStatusToTransactionStatus(intentResponse.Status),
                        StatusMessage = intentResponse.Status,
                        Amount = intentResponse.Amount / 100m, // Convert cents to dollars
                        Fee = intentResponse.ApplicationFeeAmount.HasValue ? intentResponse.ApplicationFeeAmount.Value / 100m : null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["stripe_status"] = intentResponse.Status,
                            ["created"] = intentResponse.Created.ToString(),
                            ["payment_method"] = intentResponse.PaymentMethod?.Type ?? "",
                            ["charges_count"] = intentResponse.Charges?.Data?.Length ?? 0
                        }
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Stripe Status Check Error: {errorResponse?.Error?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Stripe payment status for {PaymentIntentId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to check Stripe payment status");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> RefundPaymentAsync(string providerTransactionId, decimal amount)
        {
            try
            {
                // Get the charge ID from the PaymentIntent
                var chargeId = await GetChargeIdFromPaymentIntentAsync(providerTransactionId);
                if (string.IsNullOrEmpty(chargeId))
                    return Result.Failure<PaymentGatewayResponse>("Could not find charge ID for refund");

                var amountUSD = ConvertSLEToUSD(amount);
                var amountCents = (int)(amountUSD * 100);

                var refundRequest = new Dictionary<string, string>
                {
                    ["charge"] = chargeId,
                    ["amount"] = amountCents.ToString(),
                    ["reason"] = "requested_by_customer",
                    ["metadata[refund_reason]"] = "Sierra Leone tax payment refund",
                    ["metadata[original_amount_sle]"] = amount.ToString()
                };

                var formContent = new FormUrlEncodedContent(refundRequest);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

                var response = await _httpClient.PostAsync($"{_config.ApiUrl}/v1/refunds", formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refundResponse = JsonSerializer.Deserialize<StripeRefundResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    return Result.Success(new PaymentGatewayResponse
                    {
                        Success = refundResponse?.Status == "succeeded",
                        TransactionId = refundResponse?.Id,
                        ProviderReference = refundResponse?.Id,
                        Status = refundResponse?.Status == "succeeded" ? 
                            PaymentTransactionStatus.Refunded : PaymentTransactionStatus.Processing,
                        StatusMessage = refundResponse?.Status ?? "Refund processed",
                        Amount = refundResponse?.Amount / 100m ?? amountUSD
                    });
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    return Result.Failure<PaymentGatewayResponse>(
                        $"Stripe Refund Error: {errorResponse?.Error?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe refund for {PaymentIntentId}", providerTransactionId);
                return Result.Failure<PaymentGatewayResponse>("Failed to process Stripe refund");
            }
        }

        public async Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.WebhookSecret))
                    return Result.Failure<bool>("Webhook secret not configured");

                // Stripe signature format: t=timestamp,v1=signature
                var signatureParts = signature.Split(',');
                var timestamp = signatureParts.FirstOrDefault(p => p.StartsWith("t="))?.Substring(2);
                var v1Signature = signatureParts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3);

                if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(v1Signature))
                    return Result.Success(false);

                // Construct the signed payload
                var signedPayload = $"{timestamp}.{payload}";
                var computedSignature = ComputeHmacSha256(_config.WebhookSecret, signedPayload);
                
                var isValid = string.Equals(v1Signature, computedSignature, StringComparison.OrdinalIgnoreCase);
                
                // Check timestamp tolerance (5 minutes)
                if (isValid && long.TryParse(timestamp, out var webhookTimestamp))
                {
                    var webhookTime = DateTimeOffset.FromUnixTimeSeconds(webhookTimestamp);
                    var timeDifference = DateTimeOffset.UtcNow - webhookTime;
                    
                    if (Math.Abs(timeDifference.TotalMinutes) > 5)
                        isValid = false;
                }
                
                return Result.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Stripe webhook signature");
                return Result.Failure<bool>("Failed to validate webhook signature");
            }
        }

        public async Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string webhookData)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<StripeWebhookEvent>(webhookData,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                if (webhook == null)
                    return Result.Failure<PaymentGatewayResponse>("Invalid webhook data");

                var status = webhook.Type switch
                {
                    "payment_intent.created" => PaymentTransactionStatus.Initiated,
                    "payment_intent.requires_payment_method" => PaymentTransactionStatus.Pending,
                    "payment_intent.requires_confirmation" => PaymentTransactionStatus.Pending,
                    "payment_intent.requires_action" => PaymentTransactionStatus.Pending,
                    "payment_intent.processing" => PaymentTransactionStatus.Processing,
                    "payment_intent.succeeded" => PaymentTransactionStatus.Completed,
                    "payment_intent.payment_failed" => PaymentTransactionStatus.Failed,
                    "payment_intent.canceled" => PaymentTransactionStatus.Cancelled,
                    "charge.dispute.created" => PaymentTransactionStatus.Failed, // Disputed/chargeback
                    "invoice.payment_succeeded" => PaymentTransactionStatus.Completed,
                    "invoice.payment_failed" => PaymentTransactionStatus.Failed,
                    _ => PaymentTransactionStatus.Pending
                };

                return Result.Success(new PaymentGatewayResponse
                {
                    Success = status == PaymentTransactionStatus.Completed,
                    TransactionId = webhook.Data?.Object?.Id,
                    ProviderReference = webhook.Data?.Object?.Id,
                    Status = status,
                    StatusMessage = webhook.Type,
                    Amount = webhook.Data?.Object?.Amount / 100m,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["event_type"] = webhook.Type,
                        ["event_id"] = webhook.Id ?? "",
                        ["created"] = webhook.Created.ToString(),
                        ["livemode"] = webhook.Livemode
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return Result.Failure<PaymentGatewayResponse>("Failed to process webhook");
            }
        }

        public async Task<Result<bool>> TestConnectionAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/v1/account");
                return Result.Success(response.IsSuccessStatusCode);
            }
            catch
            {
                return Result.Success(false);
            }
        }

        // Helper methods
        private decimal ConvertSLEToUSD(decimal sleAmount)
        {
            var exchangeRate = GetUSDExchangeRate();
            return sleAmount / exchangeRate;
        }

        private decimal GetUSDExchangeRate()
        {
            // Approximate SLE to USD rate - should be fetched from live exchange rate API
            return 22000m; // This should be dynamic from exchange rate service
        }

        private PaymentTransactionStatus MapStripeStatusToTransactionStatus(string stripeStatus)
        {
            return stripeStatus?.ToLower() switch
            {
                "requires_payment_method" => PaymentTransactionStatus.Initiated,
                "requires_confirmation" => PaymentTransactionStatus.Pending,
                "requires_action" => PaymentTransactionStatus.Pending,
                "processing" => PaymentTransactionStatus.Processing,
                "requires_capture" => PaymentTransactionStatus.Processing,
                "succeeded" => PaymentTransactionStatus.Completed,
                "canceled" => PaymentTransactionStatus.Cancelled,
                "payment_failed" => PaymentTransactionStatus.Failed,
                _ => PaymentTransactionStatus.Pending
            };
        }

        private async Task<string?> GetChargeIdFromPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

                var response = await _httpClient.GetAsync($"{_config.ApiUrl}/v1/payment_intents/{paymentIntentId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var intent = JsonSerializer.Deserialize<StripePaymentIntentResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

                    return intent?.Charges?.Data?.FirstOrDefault()?.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting charge ID for PaymentIntent {PaymentIntentId}", paymentIntentId);
            }

            return null;
        }

        private string ComputeHmacSha256(string secret, string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    // Stripe API response models
    internal class StripePaymentIntentResponse
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public string? ClientSecret { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; } = "";
        public long Created { get; set; }
        public long? ApplicationFeeAmount { get; set; }
        public StripePaymentMethod? PaymentMethod { get; set; }
        public StripeChargeList? Charges { get; set; }
    }

    internal class StripePaymentMethod
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
    }

    internal class StripeChargeList
    {
        public StripeCharge[]? Data { get; set; }
    }

    internal class StripeCharge
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public long Amount { get; set; }
    }

    internal class StripeRefundResponse
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public long Amount { get; set; }
        public string Charge { get; set; } = "";
    }

    internal class StripeErrorResponse
    {
        public StripeError? Error { get; set; }
    }

    internal class StripeError
    {
        public string Type { get; set; } = "";
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public string Param { get; set; } = "";
    }

    internal class StripeWebhookEvent
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public long Created { get; set; }
        public bool Livemode { get; set; }
        public StripeWebhookData? Data { get; set; }
    }

    internal class StripeWebhookData
    {
        public StripeWebhookObject? Object { get; set; }
    }

    internal class StripeWebhookObject
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public long Amount { get; set; }
    }
}