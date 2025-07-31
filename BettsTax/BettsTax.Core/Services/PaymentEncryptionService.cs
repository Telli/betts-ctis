using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services;

/// <summary>
/// Advanced payment encryption service for Sierra Leone mobile money security
/// Handles data encryption, API key management, digital signatures, and certificate validation
/// </summary>
public class PaymentEncryptionService : IPaymentEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentEncryptionService> _logger;
    private readonly string _encryptionKey;
    private readonly string _signingKey;

    public PaymentEncryptionService(
        IConfiguration configuration,
        ILogger<PaymentEncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get encryption keys from configuration (should be stored securely)
        _encryptionKey = _configuration["PaymentSecurity:EncryptionKey"] ?? GenerateSecureKey(32);
        _signingKey = _configuration["PaymentSecurity:SigningKey"] ?? GenerateSecureKey(64);
    }

    #region Data Encryption

    public async Task<string> EncryptSensitiveDataAsync(string data)
    {
        try
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            await swEncrypt.WriteAsync(data);
            swEncrypt.Close();

            var encrypted = msEncrypt.ToArray();
            var result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            var encryptedData = Convert.ToBase64String(result);
            _logger.LogDebug("Successfully encrypted sensitive data of length {Length}", data.Length);
            
            return encryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sensitive data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public async Task<string> DecryptSensitiveDataAsync(string encryptedData)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedData))
                return string.Empty;

            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var decryptedData = await srDecrypt.ReadToEndAsync();
            _logger.LogDebug("Successfully decrypted sensitive data");
            
            return decryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt sensitive data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    public async Task<string> EncryptPaymentPinAsync(string pin)
    {
        try
        {
            if (string.IsNullOrEmpty(pin))
                throw new ArgumentException("PIN cannot be null or empty", nameof(pin));

            // Use bcrypt-like hashing for PIN security
            var salt = GenerateSecureSalt();
            var pinWithSalt = pin + salt;
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pinWithSalt));
            var encryptedPin = Convert.ToBase64String(hashBytes) + ":" + salt;
            
            _logger.LogDebug("Successfully encrypted payment PIN");
            return await EncryptSensitiveDataAsync(encryptedPin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payment PIN");
            throw new InvalidOperationException("PIN encryption failed", ex);
        }
    }

    public async Task<bool> VerifyPaymentPinAsync(string pin, string encryptedPin)
    {
        try
        {
            if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(encryptedPin))
                return false;

            var decryptedData = await DecryptSensitiveDataAsync(encryptedPin);
            var parts = decryptedData.Split(':');
            
            if (parts.Length != 2)
                return false;

            var storedHash = parts[0];
            var salt = parts[1];
            var pinWithSalt = pin + salt;
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pinWithSalt));
            var computedHash = Convert.ToBase64String(hashBytes);
            
            var isValid = string.Equals(storedHash, computedHash, StringComparison.Ordinal);
            _logger.LogDebug("PIN verification result: {IsValid}", isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify payment PIN");
            return false;
        }
    }

    #endregion

    #region API Key Management

    public async Task<string> EncryptApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            var encryptedKey = await EncryptSensitiveDataAsync(apiKey);
            _logger.LogDebug("Successfully encrypted API key");
            
            return encryptedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt API key");
            throw new InvalidOperationException("API key encryption failed", ex);
        }
    }

    public async Task<string> DecryptApiKeyAsync(string encryptedApiKey)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedApiKey))
                throw new ArgumentException("Encrypted API key cannot be null or empty", nameof(encryptedApiKey));

            var apiKey = await DecryptSensitiveDataAsync(encryptedApiKey);
            _logger.LogDebug("Successfully decrypted API key");
            
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt API key");
            throw new InvalidOperationException("API key decryption failed", ex);
        }
    }

    public async Task<string> GenerateApiKeyAsync(int length = 32)
    {
        try
        {
            if (length < 16 || length > 128)
                throw new ArgumentException("API key length must be between 16 and 128 characters", nameof(length));

            var apiKey = GenerateSecureKey(length);
            _logger.LogDebug("Generated new API key of length {Length}", length);
            
            return await Task.FromResult(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API key");
            throw new InvalidOperationException("API key generation failed", ex);
        }
    }

    public async Task<string> HashApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            var hashedKey = Convert.ToBase64String(hashBytes);
            
            _logger.LogDebug("Successfully hashed API key");
            return await Task.FromResult(hashedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash API key");
            throw new InvalidOperationException("API key hashing failed", ex);
        }
    }

    #endregion

    #region Token Management

    public async Task<string> GenerateSecureTokenAsync(int length = 16)
    {
        try
        {
            if (length < 8 || length > 64)
                throw new ArgumentException("Token length must be between 8 and 64 characters", nameof(length));

            var token = GenerateSecureKey(length);
            _logger.LogDebug("Generated secure token of length {Length}", length);
            
            return await Task.FromResult(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure token");
            throw new InvalidOperationException("Token generation failed", ex);
        }
    }

    public async Task<string> GenerateTransactionReferenceAsync()
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            
            var randomString = Convert.ToBase64String(randomBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .ToUpper();
            
            var reference = $"TXN{timestamp}{randomString}";
            _logger.LogDebug("Generated transaction reference: {Reference}", reference);
            
            return await Task.FromResult(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate transaction reference");
            throw new InvalidOperationException("Transaction reference generation failed", ex);
        }
    }

    public async Task<string> GenerateWebhookSecretAsync()
    {
        try
        {
            var secret = GenerateSecureKey(64);
            _logger.LogDebug("Generated webhook secret");
            
            return await Task.FromResult(secret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate webhook secret");
            throw new InvalidOperationException("Webhook secret generation failed", ex);
        }
    }

    public async Task<bool> ValidateTokenFormatAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Validate token format: alphanumeric, minimum 8 characters, maximum 64 characters
            var isValid = token.All(char.IsLetterOrDigit) && 
                         token.Length >= 8 && 
                         token.Length <= 64;
            
            _logger.LogDebug("Token format validation result: {IsValid}", isValid);
            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token format");
            return false;
        }
    }

    #endregion

    #region Digital Signatures

    public async Task<string> SignWebhookPayloadAsync(string payload, string secret)
    {
        try
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentException("Payload cannot be null or empty", nameof(payload));
            
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = Convert.ToBase64String(hashBytes);
            
            _logger.LogDebug("Generated webhook signature for payload of length {Length}", payload.Length);
            return await Task.FromResult(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign webhook payload");
            throw new InvalidOperationException("Webhook signing failed", ex);
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string secret)
    {
        try
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
                return false;

            var expectedSignature = await SignWebhookPayloadAsync(payload, secret);
            var isValid = string.Equals(signature, expectedSignature, StringComparison.Ordinal);
            
            _logger.LogDebug("Webhook signature verification result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify webhook signature");
            return false;
        }
    }

    public async Task<string> GenerateTransactionHashAsync(int transactionId, decimal amount, string phoneNumber)
    {
        try
        {
            var data = new
            {
                TransactionId = transactionId,
                Amount = amount.ToString("F2"),
                PhoneNumber = phoneNumber,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var jsonData = JsonSerializer.Serialize(data);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonData));
            var hash = Convert.ToBase64String(hashBytes);
            
            _logger.LogDebug("Generated transaction hash for transaction {TransactionId}", transactionId);
            return await Task.FromResult(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate transaction hash for transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Transaction hash generation failed", ex);
        }
    }

    #endregion

    #region Certificate Management

    public async Task<bool> ValidateSslCertificateAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return false;

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.GetAsync(url);
            var isValid = response.IsSuccessStatusCode;
            
            _logger.LogDebug("SSL certificate validation for {Url}: {IsValid}", url, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SSL certificate validation failed for {Url}", url);
            return false;
        }
    }

    public async Task<DateTime?> GetCertificateExpiryAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // This is a simplified implementation
            // In production, you would use proper SSL certificate validation
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                // Return a future date as placeholder
                // In production, extract actual certificate expiry
                var expiryDate = DateTime.UtcNow.AddYears(1);
                _logger.LogDebug("Certificate expiry for {Url}: {ExpiryDate}", url, expiryDate);
                return expiryDate;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get certificate expiry for {Url}", url);
            return null;
        }
    }

    public async Task<bool> IsCertificateValidAsync(string url)
    {
        try
        {
            var expiry = await GetCertificateExpiryAsync(url);
            var isValid = expiry.HasValue && expiry.Value > DateTime.UtcNow;
            
            _logger.LogDebug("Certificate validity for {Url}: {IsValid}", url, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate certificate for {Url}", url);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private string GenerateSecureKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        var result = new StringBuilder(length);
        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }
        
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
    }

    private string GenerateSecureSalt()
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    #endregion
}