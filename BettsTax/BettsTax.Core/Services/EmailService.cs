using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace BettsTax.Core.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISystemSettingService _settingService;
        private readonly ILogger<EmailService> _logger;
        private readonly EmailTemplateService _templateService;

        public EmailService(ISystemSettingService settingService, ILogger<EmailService> logger, EmailTemplateService templateService)
        {
            _settingService = settingService;
            _logger = logger;
            _templateService = templateService;
        }

        public async Task SendClientInvitationAsync(string email, string registrationUrl, string associateName)
        {
            var subject = "Welcome to The Betts Firm - Complete Your Registration";
            var body = _templateService.GetClientInvitationTemplate(registrationUrl, associateName);
            
            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Client invitation email sent to {Email} by associate {AssociateName}", email, associateName);
        }

        public async Task SendWelcomeEmailAsync(string email, string clientName)
        {
            var subject = "Welcome to The Betts Firm Client Portal";
            var body = _templateService.GetWelcomeEmailTemplate(clientName);
            
            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Welcome email sent to {Email} for client {ClientName}", email, clientName);
        }

        public async Task SendEmailVerificationAsync(string email, string verificationUrl)
        {
            var subject = "Verify Your Email Address - The Betts Firm";
            var body = _templateService.GetEmailVerificationTemplate(verificationUrl);
            
            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Email verification sent to {Email}", email);
        }

        public async Task SendRegistrationCompletedNotificationAsync(string associateEmail, string clientName)
        {
            var subject = $"New Client Registration Completed - {clientName}";
            var body = _templateService.GetRegistrationCompletedNotificationTemplate(clientName);
            
            await SendEmailAsync(associateEmail, subject, body);
            _logger.LogInformation("Registration completed notification sent to associate {AssociateEmail} for client {ClientName}", associateEmail, clientName);
        }

        public async Task SendPasswordResetAsync(string email, string resetUrl)
        {
            var subject = "Password Reset - The Betts Firm";
            var body = _templateService.GetPasswordResetTemplate(resetUrl);
            
            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Get email settings from database
                var emailSettings = await _settingService.GetEmailSettingsAsync();

                var smtpHost = emailSettings.GetValueOrDefault("Email.SmtpHost", "");
                var smtpPort = int.Parse(emailSettings.GetValueOrDefault("Email.SmtpPort", "587"));
                var username = emailSettings.GetValueOrDefault("Email.Username", "");
                var password = emailSettings.GetValueOrDefault("Email.Password", "");
                var fromEmail = emailSettings.GetValueOrDefault("Email.FromEmail", "noreply@thebettsfirmsl.com");
                var fromName = emailSettings.GetValueOrDefault("Email.FromName", "The Betts Firm");
                var useSSL = bool.Parse(emailSettings.GetValueOrDefault("Email.UseSSL", "true"));
                var useTLS = bool.Parse(emailSettings.GetValueOrDefault("Email.UseTLS", "true"));

                // Validate required settings
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Email settings not configured. Email sending skipped for {Email}", toEmail);
                    return;
                }

                // Create message using MimeKit
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send using MailKit
                using var client = new SmtpClient();
                
                // Configure security options
                var secureSocketOptions = SecureSocketOptions.Auto;
                if (useSSL)
                    secureSocketOptions = SecureSocketOptions.SslOnConnect;
                else if (useTLS)
                    secureSocketOptions = SecureSocketOptions.StartTls;
                else
                    secureSocketOptions = SecureSocketOptions.None;

                await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);

                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email} with subject '{Subject}'", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", toEmail, subject);
                throw;
            }
        }
    }
}