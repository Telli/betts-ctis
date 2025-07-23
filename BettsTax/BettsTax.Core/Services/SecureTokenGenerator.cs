using System.Security.Cryptography;

namespace BettsTax.Core.Services
{
    public class SecureTokenGenerator : ISecureTokenGenerator
    {
        public string GenerateRegistrationToken()
        {
            return GenerateSecureToken();
        }

        public string GenerateEmailVerificationToken()
        {
            return GenerateSecureToken();
        }

        public string GeneratePasswordResetToken()
        {
            return GenerateSecureToken();
        }

        public bool IsTokenExpired(DateTime expirationDate)
        {
            return DateTime.UtcNow > expirationDate;
        }

        public DateTime GetDefaultExpirationTime(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Registration => DateTime.UtcNow.AddHours(48), // 48 hours for registration
                TokenType.EmailVerification => DateTime.UtcNow.AddHours(24), // 24 hours for email verification
                TokenType.PasswordReset => DateTime.UtcNow.AddHours(1), // 1 hour for password reset
                _ => DateTime.UtcNow.AddHours(1)
            };
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            
            // Convert to URL-safe base64
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}