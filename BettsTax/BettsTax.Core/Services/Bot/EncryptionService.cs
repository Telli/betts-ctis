using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// Encryption service for sensitive data like API keys
    /// Uses ASP.NET Core Data Protection API
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<EncryptionService> _logger;
        
        public EncryptionService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EncryptionService> logger)
        {
            _protector = dataProtectionProvider.CreateProtector("BettsTax.Bot.ApiKeys");
            _logger = logger;
        }
        
        public Task<string> EncryptAsync(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    return Task.FromResult(string.Empty);
                }
                
                var encrypted = _protector.Protect(plainText);
                return Task.FromResult(encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw;
            }
        }
        
        public Task<string> DecryptAsync(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText))
                {
                    return Task.FromResult(string.Empty);
                }
                
                var decrypted = _protector.Unprotect(cipherText);
                return Task.FromResult(decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw;
            }
        }
    }
}
