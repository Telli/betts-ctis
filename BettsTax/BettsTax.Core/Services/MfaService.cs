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
using BettsTax.Core.Utilities;

namespace BettsTax.Core.Services;

/// <summary>
/// Multi-Factor Authentication service for enhanced security
/// Provides TOTP, SMS, and Email-based MFA with backup codes
/// Implements industry-standard security practices for Sierra Leone operations
/// </summary>
public class MfaService : IMfaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MfaService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    // TOTP configuration
    private const int TotpValidityPeriod = 30; // seconds
    private const int TotpDigits = 6;
    private const int TotpSecretLength = 32;
    
    // SMS/Email code configuration
    private const int CodeLength = 6;
    private const int CodeValidityMinutes = 5;
    private const int MaxAttempts = 3;

    public MfaService(
        ApplicationDbContext context,
        ILogger<MfaService> logger,
        IConfiguration configuration,
        IEncryptionService encryptionService,
        INotificationService notificationService,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _encryptionService = encryptionService;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    #region MFA Setup

    public async Task<MfaSetupResultDto> SetupMfaAsync(string userId, MfaSetupDto setupDto)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var existingConfig = await _context.UserMfaConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId);

            UserMfaConfiguration mfaConfig;
            if (existingConfig == null)
            {
                mfaConfig = new UserMfaConfiguration
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserMfaConfigurations.Add(mfaConfig);
            }
            else
            {
                mfaConfig = existingConfig;
                mfaConfig.UpdatedAt = DateTime.UtcNow;
            }

            // Update configuration
            mfaConfig.IsEnabled = setupDto.IsEnabled;
            mfaConfig.PreferredMethod = setupDto.PreferredMethod;
            mfaConfig.IsTotpEnabled = setupDto.IsTotpEnabled;
            mfaConfig.IsSmsEnabled = setupDto.IsSmsEnabled;
            mfaConfig.IsEmailEnabled = setupDto.IsEmailEnabled;
            mfaConfig.IsBackupCodesEnabled = setupDto.IsBackupCodesEnabled;
            mfaConfig.PhoneNumber = setupDto.PhoneNumber;
            mfaConfig.Email = setupDto.Email;

            var result = new MfaSetupResultDto { Success = true };

            // Generate TOTP secret if TOTP is enabled
            if (setupDto.IsTotpEnabled)
            {
                var totpSecret = GenerateTotpSecret();
                mfaConfig.TotpSecret = await _encryptionService.EncryptAsync(totpSecret, "MFA");
                
                result.TotpSecret = totpSecret;
                result.QrCodeUrl = GenerateQrCodeUrl(user.Email!, totpSecret);

                _logger.LogInformation("TOTP enabled for user {UserId}", userId);
            }

            // Generate backup codes if enabled
            if (setupDto.IsBackupCodesEnabled)
            {
                var backupCodes = GenerateBackupCodes();
                mfaConfig.BackupCodes = await _encryptionService.EncryptAsync(
                    JsonSerializer.Serialize(backupCodes.Select(c => new { Code = c, Used = false })), 
                    "MFA");
                
                result.BackupCodes = backupCodes;

                _logger.LogInformation("Backup codes generated for user {UserId}", userId);
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "MFA_SETUP", "UserMfaConfiguration", mfaConfig.Id.ToString(),
                "Multi-factor authentication configured");

            result.Message = "MFA setup completed successfully";
            
            _logger.LogInformation("MFA setup completed for user {UserId} with methods: {Methods}", 
                userId, string.Join(", ", GetEnabledMethods(setupDto)));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup MFA for user {UserId}", userId);
            return new MfaSetupResultDto 
            { 
                Success = false, 
                Message = "MFA setup failed. Please try again." 
            };
        }
    }

    public async Task<MfaSetupDto?> GetMfaConfigurationAsync(string userId)
    {
        try
        {
            var config = await _context.UserMfaConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (config == null)
                return null;

            return new MfaSetupDto
            {
                IsEnabled = config.IsEnabled,
                PreferredMethod = config.PreferredMethod,
                IsTotpEnabled = config.IsTotpEnabled,
                IsSmsEnabled = config.IsSmsEnabled,
                IsEmailEnabled = config.IsEmailEnabled,
                IsBackupCodesEnabled = config.IsBackupCodesEnabled,
                PhoneNumber = config.PhoneNumber,
                Email = config.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MFA configuration for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> DisableMfaAsync(string userId, string disabledBy)
    {
        try
        {
            var config = await _context.UserMfaConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (config == null)
                return false;

            var oldConfig = JsonSerializer.Serialize(config);

            config.IsEnabled = false;
            config.IsTotpEnabled = false;
            config.IsSmsEnabled = false;
            config.IsEmailEnabled = false;
            config.IsBackupCodesEnabled = false;
            config.TotpSecret = null;
            config.BackupCodes = null;
            config.UpdatedAt = DateTime.UtcNow;

            // Cancel any pending challenges
            var pendingChallenges = await _context.MfaChallenges
                .Where(c => c.UserId == userId && c.Status == MfaChallengeStatus.Pending)
                .ToListAsync();

            foreach (var challenge in pendingChallenges)
            {
                challenge.Status = MfaChallengeStatus.Cancelled;
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(disabledBy, "MFA_DISABLE", "UserMfaConfiguration", config.Id.ToString(),
                $"MFA disabled for user {userId}");

            _logger.LogWarning("MFA disabled for user {UserId} by {DisabledBy}", userId, disabledBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable MFA for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region MFA Challenge

    public async Task<MfaChallengeDto?> CreateChallengeAsync(string userId, MfaChallengeRequestDto request, 
        string ipAddress, string userAgent)
    {
        try
        {
            var config = await _context.UserMfaConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsEnabled);

            if (config == null)
                throw new InvalidOperationException("MFA not configured for user");

            // Validate method is enabled
            if (!IsMethodEnabled(config, request.Method))
                throw new InvalidOperationException($"MFA method {request.Method} is not enabled");

            // Cancel any existing pending challenges
            var existingChallenges = await _context.MfaChallenges
                .Where(c => c.UserId == userId && c.Status == MfaChallengeStatus.Pending)
                .ToListAsync();

            foreach (var existing in existingChallenges)
            {
                existing.Status = MfaChallengeStatus.Cancelled;
            }

            // Create new challenge
            var challenge = new MfaChallenge
            {
                UserId = userId,
                MfaConfigurationId = config.Id,
                ChallengeId = Guid.NewGuid().ToString("N"),
                Method = request.Method,
                Status = MfaChallengeStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeValidityMinutes),
                MaxAttempts = MaxAttempts,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.MfaChallenges.Add(challenge);

            var challengeDto = new MfaChallengeDto
            {
                ChallengeId = challenge.ChallengeId,
                Method = challenge.Method,
                MethodName = challenge.Method.ToString(),
                Status = challenge.Status,
                StatusName = challenge.Status.ToString(),
                ExpiresAt = challenge.ExpiresAt,
                AttemptsRemaining = challenge.MaxAttempts
            };

            // Handle method-specific challenge creation
            switch (request.Method)
            {
                case MfaMethod.Sms:
                    if (string.IsNullOrEmpty(config.PhoneNumber))
                        throw new InvalidOperationException("Phone number not configured for SMS MFA");
                    
                    var smsCode = GenerateNumericCode();
                    challenge.Code = smsCode;
                    challenge.Status = MfaChallengeStatus.Sent;
                    
                    await _notificationService.SendSmsAsync(config.PhoneNumber, 
                        $"Your BettsTax verification code is: {smsCode}. Valid for {CodeValidityMinutes} minutes.");
                    
                    challengeDto.Message = $"Verification code sent to {MaskPhoneNumber(config.PhoneNumber)}";
                    break;

                case MfaMethod.Email:
                    var emailAddress = request.Email ?? config.Email;
                    if (string.IsNullOrEmpty(emailAddress))
                        throw new InvalidOperationException("Email address not configured for Email MFA");
                    
                    var emailCode = GenerateNumericCode();
                    challenge.Code = emailCode;
                    challenge.Status = MfaChallengeStatus.Sent;
                    
                    await _notificationService.SendEmailAsync(emailAddress, "BettsTax Verification Code",
                        $"Your verification code is: {emailCode}. This code is valid for {CodeValidityMinutes} minutes.");
                    
                    challengeDto.Message = $"Verification code sent to {MaskEmail(emailAddress)}";
                    break;

                case MfaMethod.Totp:
                    challengeDto.Message = "Enter the 6-digit code from your authenticator app";
                    break;

                case MfaMethod.BackupCode:
                    challengeDto.Message = "Enter one of your backup codes";
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported MFA method: {request.Method}");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA challenge created for user {UserId} using method {Method}", 
                userId, request.Method);

            return challengeDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MFA challenge for user {UserId}", userId);
            throw new InvalidOperationException("Failed to create MFA challenge", ex);
        }
    }

    public async Task<MfaVerificationResultDto> VerifyChallengeAsync(string userId, MfaVerificationDto verification,
        string ipAddress, string userAgent)
    {
        try
        {
            var challenge = await _context.MfaChallenges
                .Include(c => c.MfaConfiguration)
                .FirstOrDefaultAsync(c => c.ChallengeId == verification.ChallengeId && 
                                        c.UserId == userId &&
                                        c.Status == MfaChallengeStatus.Pending);

            if (challenge == null)
            {
                await LogSecurityEvent(userId, "MFA_INVALID_CHALLENGE", SecurityEventSeverity.Medium,
                    "Invalid MFA challenge attempted", ipAddress, userAgent);
                
                return new MfaVerificationResultDto 
                { 
                    Success = false, 
                    Message = "Invalid or expired challenge" 
                };
            }

            // Check expiration
            if (DateTime.UtcNow > challenge.ExpiresAt)
            {
                challenge.Status = MfaChallengeStatus.Expired;
                await _context.SaveChangesAsync();
                
                return new MfaVerificationResultDto 
                { 
                    Success = false, 
                    Message = "Challenge expired" 
                };
            }

            // Check attempt limit
            if (challenge.AttemptCount >= challenge.MaxAttempts)
            {
                challenge.Status = MfaChallengeStatus.Failed;
                await _context.SaveChangesAsync();
                
                await LogSecurityEvent(userId, "MFA_MAX_ATTEMPTS", SecurityEventSeverity.High,
                    "Maximum MFA attempts exceeded", ipAddress, userAgent);
                
                return new MfaVerificationResultDto 
                { 
                    Success = false, 
                    Message = "Maximum attempts exceeded" 
                };
            }

            challenge.AttemptCount++;

            bool isValid = false;
            string? invalidReason = null;

            // Verify based on method
            switch (challenge.Method)
            {
                case MfaMethod.Sms:
                case MfaMethod.Email:
                    isValid = challenge.Code == verification.Code;
                    invalidReason = "Invalid verification code";
                    break;

                case MfaMethod.Totp:
                    var totpSecret = await _encryptionService.DecryptAsync(
                        challenge.MfaConfiguration.TotpSecret!, "MFA");
                    isValid = ValidateTotpCode(verification.Code, totpSecret);
                    invalidReason = "Invalid authenticator code";
                    break;

                case MfaMethod.BackupCode:
                    var backupCodesJson = await _encryptionService.DecryptAsync(
                        challenge.MfaConfiguration.BackupCodes!, "MFA");
                    var backupCodes = JsonSerializer.Deserialize<List<dynamic>>(backupCodesJson)!;
                    
                    var matchingCode = backupCodes.FirstOrDefault(c => 
                        c.GetProperty("Code").GetString() == verification.Code &&
                        !c.GetProperty("Used").GetBoolean());
                    
                    if (matchingCode != null)
                    {
                        // Mark backup code as used
                        var updatedCodes = backupCodes.Select(c => new
                        {
                            Code = c.GetProperty("Code").GetString(),
                            Used = c.GetProperty("Code").GetString() == verification.Code || 
                                   c.GetProperty("Used").GetBoolean()
                        }).ToList();

                        challenge.MfaConfiguration.BackupCodes = await _encryptionService.EncryptAsync(
                            JsonSerializer.Serialize(updatedCodes), "MFA");
                        
                        isValid = true;
                    }
                    else
                    {
                        invalidReason = "Invalid or already used backup code";
                    }
                    break;
            }

            if (isValid)
            {
                challenge.Status = MfaChallengeStatus.Verified;
                challenge.CompletedAt = DateTime.UtcNow;
                challenge.VerifiedAt = DateTime.UtcNow;
                
                // Update MFA configuration last used
                challenge.MfaConfiguration.LastUsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Generate access token or session
                var accessToken = GenerateAccessToken(userId);
                var expiresAt = DateTime.UtcNow.AddHours(8);

                // Audit log
                await _auditService.LogAsync(userId, "MFA_VERIFIED", "MfaChallenge", challenge.Id.ToString(),
                    $"MFA verification successful using {challenge.Method}");

                _logger.LogInformation("MFA verification successful for user {UserId} using method {Method}", 
                    userId, challenge.Method);

                return new MfaVerificationResultDto
                {
                    Success = true,
                    Message = "Verification successful",
                    AccessToken = accessToken,
                    ExpiresAt = expiresAt,
                    IsDeviceRemembered = verification.RememberDevice
                };
            }
            else
            {
                if (challenge.AttemptCount >= challenge.MaxAttempts)
                {
                    challenge.Status = MfaChallengeStatus.Failed;
                    
                    await LogSecurityEvent(userId, "MFA_FAILED", SecurityEventSeverity.High,
                        $"MFA verification failed after {challenge.MaxAttempts} attempts", ipAddress, userAgent);
                }

                await _context.SaveChangesAsync();

                return new MfaVerificationResultDto
                {
                    Success = false,
                    Message = invalidReason ?? "Verification failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify MFA challenge for user {UserId}", userId);
            return new MfaVerificationResultDto 
            { 
                Success = false, 
                Message = "Verification failed. Please try again." 
            };
        }
    }

    public async Task<bool> IsMfaEnabledAsync(string userId)
    {
        try
        {
            var config = await _context.UserMfaConfigurations
                .FirstOrDefaultAsync(m => m.UserId == userId);

            return config?.IsEnabled == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check MFA status for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private string GenerateTotpSecret()
    {
        var bytes = new byte[TotpSecretLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encoder.ToBase32String(bytes);
    }

    private List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = BitConverter.ToUInt32(bytes) % 1000000;
            codes.Add(code.ToString("D6"));
        }
        
        return codes;
    }

    private string GenerateNumericCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes) % (uint)Math.Pow(10, CodeLength);
        return number.ToString($"D{CodeLength}");
    }

    private string GenerateQrCodeUrl(string email, string secret)
    {
        var issuer = _configuration["Application:Name"] ?? "BettsTax";
        var label = $"{issuer}:{email}";
        var otpAuthUrl = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits={TotpDigits}&period={TotpValidityPeriod}";
        
        return $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(otpAuthUrl)}";
    }

    private bool ValidateTotpCode(string code, string secret)
    {
        try
        {
            var secretBytes = Base32Encoder.FromBase32String(secret);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpValidityPeriod;
            
            // Allow for time drift (check previous, current, and next time windows)
            for (int i = -1; i <= 1; i++)
            {
                var testTimestamp = timestamp + i;
                var hash = GenerateHotp(secretBytes, testTimestamp);
                var truncatedHash = TruncateHash(hash);
                var generatedCode = (truncatedHash % 1000000).ToString("D6");
                
                if (code == generatedCode)
                    return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate TOTP code");
            return false;
        }
    }

    private byte[] GenerateHotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);
        
        using var hmac = new HMACSHA1(key);
        return hmac.ComputeHash(counterBytes);
    }

    private int TruncateHash(byte[] hash)
    {
        var offset = hash[hash.Length - 1] & 0x0F;
        return ((hash[offset] & 0x7F) << 24) |
               ((hash[offset + 1] & 0xFF) << 16) |
               ((hash[offset + 2] & 0xFF) << 8) |
               (hash[offset + 3] & 0xFF);
    }

    private bool IsMethodEnabled(UserMfaConfiguration config, MfaMethod method)
    {
        return method switch
        {
            MfaMethod.Totp => config.IsTotpEnabled,
            MfaMethod.Sms => config.IsSmsEnabled,
            MfaMethod.Email => config.IsEmailEnabled,
            MfaMethod.BackupCode => config.IsBackupCodesEnabled,
            _ => false
        };
    }

    private List<string> GetEnabledMethods(MfaSetupDto setup)
    {
        var methods = new List<string>();
        if (setup.IsTotpEnabled) methods.Add("TOTP");
        if (setup.IsSmsEnabled) methods.Add("SMS");
        if (setup.IsEmailEnabled) methods.Add("Email");
        if (setup.IsBackupCodesEnabled) methods.Add("BackupCodes");
        return methods;
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length <= 4) return phoneNumber;
        return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 2);
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        
        var username = parts[0];
        var domain = parts[1];
        
        if (username.Length <= 2) return email;
        return username.Substring(0, 2) + "****" + "@" + domain;
    }

    private string GenerateAccessToken(string userId)
    {
        // This would typically generate a JWT token or session token
        // For now, return a simple token
        return Guid.NewGuid().ToString("N");
    }

    private async Task LogSecurityEvent(string userId, string eventType, SecurityEventSeverity severity,
        string description, string ipAddress, string userAgent)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                Severity = severity,
                Category = SecurityEventCategory.Authentication,
                Title = $"MFA Security Event: {eventType}",
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow,
                RiskScore = severity switch
                {
                    SecurityEventSeverity.Low => 20,
                    SecurityEventSeverity.Medium => 50,
                    SecurityEventSeverity.High => 80,
                    SecurityEventSeverity.Critical => 100,
                    _ => 10
                }
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event for user {UserId}", userId);
        }
    }

    #endregion
}

// Extension method for Base32 encoding
public static class Base32Extensions
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string ToBase32String(this byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;

        var sb = new StringBuilder();
        int bytesPosition = 0;
        int bitsLeft = 8;
        int mask = 0;
        int currentByte = bytes[bytesPosition];

        while (bytesPosition < bytes.Length || bitsLeft < 8)
        {
            if (bitsLeft < 5)
            {
                if (bytesPosition < bytes.Length - 1)
                {
                    currentByte <<= 8;
                    currentByte |= bytes[++bytesPosition];
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    currentByte <<= pad;
                    bitsLeft += pad;
                }
            }

            mask = 31;
            sb.Append(Base32Alphabet[mask & (currentByte >> (bitsLeft - 5))]);
            bitsLeft -= 5;
        }

        return sb.ToString();
    }

    public static byte[] FromBase32String(string base32)
    {
        if (string.IsNullOrEmpty(base32)) return Array.Empty<byte>();

        var bytes = new List<byte>();
        int bitsLeft = 0;
        int currentByte = 0;

        foreach (char c in base32.ToUpper())
        {
            int value = Base32Alphabet.IndexOf(c);
            if (value < 0) continue;

            currentByte = (currentByte << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bytes.Add((byte)(currentByte >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }

        return bytes.ToArray();
    }
}