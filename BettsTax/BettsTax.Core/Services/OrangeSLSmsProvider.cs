using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class OrangeSLSmsProvider : BaseSmsProvider
    {
        private readonly HttpClient _httpClient;

        public OrangeSLSmsProvider(SmsProviderConfig config, ILogger<OrangeSLSmsProvider> logger, HttpClient httpClient) 
            : base(config, logger)
        {
            _httpClient = httpClient;
        }

        public override SmsProvider ProviderType => SmsProvider.OrangeSL;

        public override async Task<Result<SmsProviderResponse>> SendSmsAsync(string phoneNumber, string message, string? senderId = null)
        {
            try
            {
                var formattedNumber = FormatPhoneNumber(phoneNumber);
                
                // Validate phone number is Orange SL (76, 77, 78, 79)
                if (!IsOrangeNumber(formattedNumber))
                {
                    return Result.Failure<SmsProviderResponse>("Phone number is not an Orange SL number");
                }

                // Orange SL API request
                var request = new
                {
                    to = formattedNumber,
                    message = message,
                    sender = senderId ?? _config.SenderId ?? "BETTSFIRM",
                    apiKey = _config.ApiKey,
                    apiSecret = _config.ApiSecret
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.ApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OrangeApiResponse>(responseContent);
                    
                    return Result.Success<SmsProviderResponse>(new SmsProviderResponse
                    {
                        Success = true,
                        MessageId = result?.MessageId,
                        Status = "sent",
                        Cost = CalculateCost(message.Length),
                        MessageParts = CalculateMessageParts(message.Length)
                    });
                }
                else
                {
                    _logger.LogError("Orange SL SMS API error: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    
                    return Result.Failure<SmsProviderResponse>($"Failed to send SMS: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via Orange SL");
                return Result.Failure<SmsProviderResponse>($"Error sending SMS: {ex.Message}");
            }
        }

        public override async Task<Result<SmsProviderResponse>> GetDeliveryStatusAsync(string messageId)
        {
            try
            {
                var url = $"{_config.ApiUrl}/status/{messageId}?apiKey={_config.ApiKey}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OrangeStatusResponse>(content);
                    
                    return Result.Success<SmsProviderResponse>(new SmsProviderResponse
                    {
                        Success = true,
                        MessageId = messageId,
                        Status = result?.Status ?? "unknown",
                        DeliveryDate = result?.DeliveryDate
                    });
                }
                
                return Result.Failure<SmsProviderResponse>("Failed to get delivery status");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery status from Orange SL");
                return Result.Failure<SmsProviderResponse>($"Error: {ex.Message}");
            }
        }

        public override async Task<Result<decimal>> GetBalanceAsync()
        {
            try
            {
                var url = $"{_config.ApiUrl}/balance?apiKey={_config.ApiKey}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OrangeBalanceResponse>(content);
                    
                    return Result.Success<decimal>(result?.Balance ?? 0);
                }
                
                return Result.Failure<decimal>("Failed to get balance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance from Orange SL");
                return Result.Failure<decimal>($"Error: {ex.Message}");
            }
        }

        private bool IsOrangeNumber(string phoneNumber)
        {
            // Orange SL prefixes: 76, 77, 78, 79
            if (phoneNumber.StartsWith("232"))
            {
                var prefix = phoneNumber.Substring(3, 2);
                return prefix == "76" || prefix == "77" || prefix == "78" || prefix == "79";
            }
            return false;
        }

        private decimal CalculateCost(int messageLength)
        {
            var parts = CalculateMessageParts(messageLength);
            return parts * _config.CostPerSms;
        }

        private int CalculateMessageParts(int messageLength)
        {
            // SMS character limits
            const int singleSmsLimit = 160;
            const int multiPartLimit = 153;
            
            if (messageLength <= singleSmsLimit)
                return 1;
            
            return (int)Math.Ceiling((double)messageLength / multiPartLimit);
        }

        // Response models
        private class OrangeApiResponse
        {
            public string? MessageId { get; set; }
            public string? Status { get; set; }
            public string? Error { get; set; }
        }

        private class OrangeStatusResponse
        {
            public string? Status { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public string? Error { get; set; }
        }

        private class OrangeBalanceResponse
        {
            public decimal Balance { get; set; }
            public string? Currency { get; set; }
        }
    }
}