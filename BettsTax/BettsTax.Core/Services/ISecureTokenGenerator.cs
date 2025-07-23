namespace BettsTax.Core.Services
{
    public interface ISecureTokenGenerator
    {
        string GenerateRegistrationToken();
        string GenerateEmailVerificationToken();
        string GeneratePasswordResetToken();
        bool IsTokenExpired(DateTime expirationDate);
        DateTime GetDefaultExpirationTime(TokenType tokenType);
    }

    public enum TokenType
    {
        Registration,
        EmailVerification,
        PasswordReset
    }
}