using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BettsTax.Data;
using BettsTax.Data.Models.Security;
using BettsTax.Core.DTOs.Security;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services.Security;

/// <summary>
/// Enterprise-grade encryption service for data protection
/// Provides field-level encryption, key management, and digital signatures
/// Implements AES-256 encryption with secure key rotation for Sierra Leone compliance
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EncryptionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    // Master key for encrypting other keys (should be stored in secure key management system)
    private readonly string _masterKey;
    private readonly string _masterSalt;

    public EncryptionService(
        ApplicationDbContext context,
        ILogger<EncryptionService> logger,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _auditService = auditService;

        // In production, these should come from Azure Key Vault, AWS KMS, or similar
        _masterKey = configuration["Encryption:MasterKey"] ?? throw new InvalidOperationException("Master key not configured");
        _masterSalt = configuration["Encryption:MasterSalt"] ?? throw new InvalidOperationException("Master salt not configured");
    }

    #region Encryption/Decryption

    public async Task<string> EncryptAsync(string plainText, string keyName)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var encryptionKey = await GetActiveEncryptionKeyAsync(keyName);
            if (encryptionKey == null)
                throw new InvalidOperationException($"Encryption key '{keyName}' not found or inactive");

            var keyBytes = await DecryptKeyAsync(encryptionKey.EncryptedKey);
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            // Update key usage statistics
            encryptionKey.UsageCount++;
            encryptionKey.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var base64Result = Convert.ToBase64String(result);
            _logger.LogDebug("Data encrypted using key {KeyName}", keyName);
            return base64Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data using key {KeyName}", keyName);
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(string encryptedText, string keyName)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            var encryptionKey = await GetEncryptionKeyByNameAsync(keyName);
            if (encryptionKey == null)
                throw new InvalidOperationException($"Encryption key '{keyName}' not found");

            var keyBytes = await DecryptKeyAsync(encryptionKey.EncryptedKey);
            var encryptedData = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.IV.Length];
            var cipherText = new byte[encryptedData.Length - iv.Length];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            var result = Encoding.UTF8.GetString(decryptedBytes);

            // Update key usage statistics
            encryptionKey.UsageCount++;
            encryptionKey.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Data decrypted using key {KeyName}", keyName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data using key {KeyName}", keyName);
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    #endregion

    #region Key Management

    public async Task<EncryptionKeyDto> CreateEncryptionKeyAsync(CreateEncryptionKeyDto request, string createdBy)
    {
        try
        {
            // Generate new encryption key
            byte[] keyBytes;
            using (var rng = RandomNumberGenerator.Create())
            {
                keyBytes = new byte[request.KeySize / 8];
                rng.GetBytes(keyBytes);
            }

            // Encrypt the key with master key
            var encryptedKey = await EncryptWithMasterKeyAsync(keyBytes);
            var keyHash = ComputeKeyHash(keyBytes);

            var encryptionKey = new EncryptionKey
            {
                KeyName = request.KeyName,
                Algorithm = request.Algorithm,
                KeySize = request.KeySize,
                EncryptedKey = encryptedKey,
                KeyHash = keyHash,
                Status = EncryptionKeyStatus.Active,
                KeyType = request.KeyType,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays),
                CreatedBy = createdBy,
                IsActive = true,
                Version = 1,
                RotationIntervalDays = request.RotationIntervalDays,
                AutoRotate = request.AutoRotate,
                UsageCount = 0
            };

            _context.EncryptionKeys.Add(encryptionKey);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(createdBy, "ENCRYPTION_KEY_CREATED", "EncryptionKey", encryptionKey.Id.ToString(),
                $"Created encryption key: {request.KeyName}");

            _logger.LogInformation("Encryption key created: {KeyName} by {CreatedBy}", request.KeyName, createdBy);

            return MapToDto(encryptionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create encryption key: {KeyName}", request.KeyName);
            throw new InvalidOperationException("Key creation failed", ex);
        }
    }

    public async Task<List<EncryptionKeyDto>> GetEncryptionKeysAsync(EncryptionKeyType? keyType = null, bool? activeOnly = true)
    {
        try
        {
            var query = _context.EncryptionKeys.AsQueryable();

            if (keyType.HasValue)
                query = query.Where(k => k.KeyType == keyType.Value);

            if (activeOnly == true)
                query = query.Where(k => k.IsActive && k.Status == EncryptionKeyStatus.Active);

            var keys = await query
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => MapToDto(k))
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} encryption keys", keys.Count);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve encryption keys");
            throw new InvalidOperationException("Failed to retrieve encryption keys", ex);
        }
    }

    public async Task<bool> RotateKeyAsync(int keyId, string rotatedBy)
    {
        try
        {
            var existingKey = await _context.EncryptionKeys.FindAsync(keyId);
            if (existingKey == null)
                return false;

            var oldKeyData = JsonSerializer.Serialize(existingKey);

            // Generate new key
            byte[] newKeyBytes;
            using (var rng = RandomNumberGenerator.Create())
            {
                newKeyBytes = new byte[existingKey.KeySize / 8];
                rng.GetBytes(newKeyBytes);
            }

            // Mark old key as inactive and create new version
            existingKey.IsActive = false;
            existingKey.Status = EncryptionKeyStatus.Inactive;

            var newKey = new EncryptionKey
            {
                KeyName = existingKey.KeyName,
                Algorithm = existingKey.Algorithm,
                KeySize = existingKey.KeySize,
                EncryptedKey = await EncryptWithMasterKeyAsync(newKeyBytes),
                KeyHash = ComputeKeyHash(newKeyBytes),
                Status = EncryptionKeyStatus.Active,
                KeyType = existingKey.KeyType,
                Description = $"Rotated version of {existingKey.KeyName}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(existingKey.RotationIntervalDays),
                CreatedBy = rotatedBy,
                IsActive = true,
                Version = existingKey.Version + 1,
                RotationIntervalDays = existingKey.RotationIntervalDays,
                AutoRotate = existingKey.AutoRotate,
                UsageCount = 0,
                LastRotatedAt = DateTime.UtcNow
            };

            _context.EncryptionKeys.Add(newKey);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(rotatedBy, "ENCRYPTION_KEY_ROTATED", "EncryptionKey", keyId.ToString(),
                $"Rotated encryption key: {existingKey.KeyName}");

            _logger.LogInformation("Encryption key rotated: {KeyName} (ID: {KeyId}) by {RotatedBy}", 
                existingKey.KeyName, keyId, rotatedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate encryption key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<bool> DeactivateKeyAsync(int keyId, string deactivatedBy)
    {
        try
        {
            var key = await _context.EncryptionKeys.FindAsync(keyId);
            if (key == null)
                return false;

            var oldStatus = JsonSerializer.Serialize(new { key.IsActive, key.Status });

            key.IsActive = false;
            key.Status = EncryptionKeyStatus.Inactive;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(deactivatedBy, "ENCRYPTION_KEY_DEACTIVATED", "EncryptionKey", keyId.ToString(),
                $"Deactivated encryption key: {key.KeyName}");

            _logger.LogInformation("Encryption key deactivated: {KeyName} (ID: {KeyId}) by {DeactivatedBy}", 
                key.KeyName, keyId, deactivatedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate encryption key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<EncryptionStatisticsDto> GetEncryptionStatisticsAsync()
    {
        try
        {
            var keys = await _context.EncryptionKeys.ToListAsync();
            var encryptedRecords = await _context.EncryptedData.CountAsync();
            var now = DateTime.UtcNow;

            var statistics = new EncryptionStatisticsDto
            {
                TotalKeys = keys.Count,
                ActiveKeys = keys.Count(k => k.IsActive && k.Status == EncryptionKeyStatus.Active),
                ExpiredKeys = keys.Count(k => k.ExpiresAt < now),
                KeysRequiringRotation = keys.Count(k => k.AutoRotate && 
                    k.LastRotatedAt.HasValue && 
                    k.LastRotatedAt.Value.AddDays(k.RotationIntervalDays) < now),
                TotalEncryptedRecords = encryptedRecords,
                KeyTypeBreakdown = keys.GroupBy(k => k.KeyType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                AlgorithmBreakdown = keys.GroupBy(k => k.Algorithm)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ExpiringKeys = keys
                    .Where(k => k.IsActive && k.ExpiresAt < now.AddDays(30))
                    .Select(k => MapToDto(k))
                    .ToList()
            };

            _logger.LogDebug("Generated encryption statistics: {ActiveKeys}/{TotalKeys} keys active", 
                statistics.ActiveKeys, statistics.TotalKeys);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate encryption statistics");
            throw new InvalidOperationException("Failed to generate encryption statistics", ex);
        }
    }

    #endregion

    #region Field-Level Encryption

    public async Task<bool> EncryptFieldAsync(string entityType, string entityId, string fieldName, string value, string keyName)
    {
        try
        {
            var encryptedValue = await EncryptAsync(value, keyName);
            var encryptionKey = await GetActiveEncryptionKeyAsync(keyName);

            if (encryptionKey == null)
                return false;

            // Check if field already encrypted
            var existing = await _context.EncryptedData
                .FirstOrDefaultAsync(e => e.EntityType == entityType && 
                                        e.EntityId == entityId && 
                                        e.FieldName == fieldName);

            if (existing != null)
            {
                // Update existing encrypted field
                existing.EncryptedValue = encryptedValue;
                existing.EncryptionKeyId = encryptionKey.Id;
                existing.EncryptedAt = DateTime.UtcNow;
                existing.LastAccessedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new encrypted field record
                var encryptedData = new EncryptedData
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    FieldName = fieldName,
                    EncryptionKeyId = encryptionKey.Id,
                    EncryptedValue = encryptedValue,
                    InitializationVector = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()[..16])),
                    EncryptedAt = DateTime.UtcNow,
                    EncryptedBy = "SYSTEM",
                    IsPersonalData = DetermineIfPersonalData(fieldName),
                    IsFinancialData = DetermineIfFinancialData(fieldName),
                    IsComplianceData = DetermineIfComplianceData(entityType, fieldName),
                    DataClassification = ClassifyData(entityType, fieldName)
                };

                _context.EncryptedData.Add(encryptedData);
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("Field encrypted: {EntityType}.{FieldName} for entity {EntityId}", 
                entityType, fieldName, entityId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt field {FieldName} for {EntityType} {EntityId}", 
                fieldName, entityType, entityId);
            return false;
        }
    }

    public async Task<string?> DecryptFieldAsync(string entityType, string entityId, string fieldName)
    {
        try
        {
            var encryptedData = await _context.EncryptedData
                .Include(e => e.EncryptionKey)
                .FirstOrDefaultAsync(e => e.EntityType == entityType && 
                                        e.EntityId == entityId && 
                                        e.FieldName == fieldName);

            if (encryptedData == null)
                return null;

            var decryptedValue = await DecryptAsync(encryptedData.EncryptedValue, encryptedData.EncryptionKey.KeyName);

            // Update access tracking
            encryptedData.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Field decrypted: {EntityType}.{FieldName} for entity {EntityId}", 
                entityType, fieldName, entityId);

            return decryptedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt field {FieldName} for {EntityType} {EntityId}", 
                fieldName, entityType, entityId);
            return null;
        }
    }

    #endregion

    #region Digital Signatures

    public async Task<byte[]> GenerateDigitalSignatureAsync(string data, string keyName)
    {
        try
        {
            var signingKey = await GetActiveEncryptionKeyAsync(keyName);
            if (signingKey == null || signingKey.KeyType != EncryptionKeyType.TokenSigning)
                throw new InvalidOperationException($"Signing key '{keyName}' not found or invalid type");

            var keyBytes = await DecryptKeyAsync(signingKey.EncryptedKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var signature = hmac.ComputeHash(dataBytes);

            _logger.LogDebug("Digital signature generated using key {KeyName}", keyName);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate digital signature using key {KeyName}", keyName);
            throw new InvalidOperationException("Digital signature generation failed", ex);
        }
    }

    public async Task<bool> VerifyDigitalSignatureAsync(string data, byte[] signature, string keyName)
    {
        try
        {
            var signingKey = await GetEncryptionKeyByNameAsync(keyName);
            if (signingKey == null || signingKey.KeyType != EncryptionKeyType.TokenSigning)
                return false;

            var keyBytes = await DecryptKeyAsync(signingKey.EncryptedKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var expectedSignature = hmac.ComputeHash(dataBytes);

            var isValid = signature.SequenceEqual(expectedSignature);

            _logger.LogDebug("Digital signature verification: {IsValid} using key {KeyName}", isValid, keyName);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify digital signature using key {KeyName}", keyName);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<EncryptionKey?> GetActiveEncryptionKeyAsync(string keyName)
    {
        return await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.KeyName == keyName && 
                                    k.IsActive && 
                                    k.Status == EncryptionKeyStatus.Active);
    }

    private async Task<EncryptionKey?> GetEncryptionKeyByNameAsync(string keyName)
    {
        return await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.KeyName == keyName);
    }

    private async Task<string> EncryptWithMasterKeyAsync(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword(_masterKey, _masterSalt);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private async Task<byte[]> DecryptKeyAsync(string encryptedKey)
    {
        var encryptedData = Convert.FromBase64String(encryptedKey);

        using var aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword(_masterKey, _masterSalt);

        var iv = new byte[aes.IV.Length];
        var cipherText = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return await Task.FromResult(decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length));
    }

    private byte[] DeriveKeyFromPassword(string password, string salt)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32); // 256-bit key
    }

    private string ComputeKeyHash(byte[] keyBytes)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(keyBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private EncryptionKeyDto MapToDto(EncryptionKey key)
    {
        var now = DateTime.UtcNow;
        var daysUntilExpiration = (int)(key.ExpiresAt - now).TotalDays;
        var requiresRotation = key.AutoRotate && 
                             key.LastRotatedAt.HasValue && 
                             key.LastRotatedAt.Value.AddDays(key.RotationIntervalDays) < now;

        return new EncryptionKeyDto
        {
            Id = key.Id,
            KeyName = key.KeyName,
            Algorithm = key.Algorithm,
            KeySize = key.KeySize,
            Status = key.Status,
            StatusName = key.Status.ToString(),
            KeyType = key.KeyType,
            KeyTypeName = key.KeyType.ToString(),
            Description = key.Description,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            CreatedBy = key.CreatedBy,
            IsActive = key.IsActive,
            Version = key.Version,
            LastRotatedAt = key.LastRotatedAt,
            RotationIntervalDays = key.RotationIntervalDays,
            AutoRotate = key.AutoRotate,
            UsageCount = key.UsageCount,
            DaysUntilExpiration = Math.Max(0, daysUntilExpiration),
            RequiresRotation = requiresRotation
        };
    }

    private bool DetermineIfPersonalData(string fieldName)
    {
        var personalDataFields = new[] { "name", "email", "phone", "address", "ssn", "nin", "passport" };
        return personalDataFields.Any(field => fieldName.Contains(field, StringComparison.OrdinalIgnoreCase));
    }

    private bool DetermineIfFinancialData(string fieldName)
    {
        var financialDataFields = new[] { "amount", "balance", "payment", "salary", "income", "tax", "account" };
        return financialDataFields.Any(field => fieldName.Contains(field, StringComparison.OrdinalIgnoreCase));
    }

    private bool DetermineIfComplianceData(string entityType, string fieldName)
    {
        var complianceEntities = new[] { "client", "taxreturn", "audit", "compliance" };
        return complianceEntities.Any(entity => entityType.Contains(entity, StringComparison.OrdinalIgnoreCase));
    }

    private string ClassifyData(string entityType, string fieldName)
    {
        if (DetermineIfPersonalData(fieldName)) return "Personal";
        if (DetermineIfFinancialData(fieldName)) return "Financial";
        if (DetermineIfComplianceData(entityType, fieldName)) return "Compliance";
        return "General";
    }

    #endregion
}