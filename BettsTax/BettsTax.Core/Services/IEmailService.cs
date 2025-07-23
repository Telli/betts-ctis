namespace BettsTax.Core.Services
{
    public interface IEmailService
    {
        Task SendClientInvitationAsync(string email, string registrationUrl, string associateName);
        Task SendWelcomeEmailAsync(string email, string clientName);
        Task SendEmailVerificationAsync(string email, string verificationUrl);
        Task SendRegistrationCompletedNotificationAsync(string associateEmail, string clientName);
        Task SendPasswordResetAsync(string email, string resetUrl);
    }
}