using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public interface ISmsProvider
    {
        SmsProvider ProviderType { get; }
        Task<Result<SmsProviderResponse>> SendSmsAsync(string phoneNumber, string message, string? senderId = null);
        Task<Result<SmsProviderResponse>> GetDeliveryStatusAsync(string messageId);
        Task<Result<decimal>> GetBalanceAsync();
        Task<Result<bool>> ValidateConfigurationAsync();
    }

    public class SmsProviderResponse
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? Cost { get; set; }
        public int? MessageParts { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    // Base class for SMS providers
    public abstract class BaseSmsProvider : ISmsProvider
    {
        protected readonly SmsProviderConfig _config;
        protected readonly ILogger _logger;

        protected BaseSmsProvider(SmsProviderConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public abstract SmsProvider ProviderType { get; }
        public abstract Task<Result<SmsProviderResponse>> SendSmsAsync(string phoneNumber, string message, string? senderId = null);
        public abstract Task<Result<SmsProviderResponse>> GetDeliveryStatusAsync(string messageId);
        public abstract Task<Result<decimal>> GetBalanceAsync();
        
        public virtual Task<Result<bool>> ValidateConfigurationAsync()
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
                return Task.FromResult(Result.Failure<bool>("API key is not configured"));
            
            if (string.IsNullOrEmpty(_config.ApiUrl))
                return Task.FromResult(Result.Failure<bool>("API URL is not configured"));
            
            return Task.FromResult(Result.Success<bool>(true));
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
}