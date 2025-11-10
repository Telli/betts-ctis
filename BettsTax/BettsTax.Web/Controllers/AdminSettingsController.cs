using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Web.Filters;
using System.Globalization;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(AuditActionFilter))]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _settingService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(
            ISystemSettingService settingService,
            IEmailService emailService,
            ILogger<AdminSettingsController> logger)
        {
            _settingService = settingService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Get email settings for admin configuration
        /// </summary>
        [HttpGet("email")]
        public async Task<ActionResult<EmailSettingsDto>> GetEmailSettings()
        {
            try
            {
                var settings = await _settingService.GetEmailSettingsAsync();

                var emailSettings = new EmailSettingsDto
                {
                    SmtpHost = settings.GetValueOrDefault("Email.SmtpHost", ""),
                    SmtpPort = int.Parse(settings.GetValueOrDefault("Email.SmtpPort", "587")),
                    Username = settings.GetValueOrDefault("Email.Username", ""),
                    Password = "••••••••", // Don't return actual password for security
                    FromEmail = settings.GetValueOrDefault("Email.FromEmail", "noreply@thebettsfirmsl.com"),
                    FromName = settings.GetValueOrDefault("Email.FromName", "The Betts Firm"),
                    UseSSL = bool.Parse(settings.GetValueOrDefault("Email.UseSSL", "true")),
                    UseTLS = bool.Parse(settings.GetValueOrDefault("Email.UseTLS", "true"))
                };

                return Ok(emailSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email settings");
                return StatusCode(500, new { message = "Failed to retrieve email settings" });
            }
        }

        /// <summary>
        /// Update email settings
        /// </summary>
        [HttpPost("email")]
        public async Task<ActionResult> UpdateEmailSettings([FromBody] EmailSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to identify current user");
                }

                var settings = new Dictionary<string, string>();

                // Only update password if it's not the placeholder
                var existingSettings = await _settingService.GetEmailSettingsAsync();
                var currentPassword = existingSettings.GetValueOrDefault("Email.Password", "");

                settings["Email.SmtpHost"] = dto.SmtpHost;
                settings["Email.SmtpPort"] = dto.SmtpPort.ToString();
                settings["Email.Username"] = dto.Username;
                settings["Email.Password"] = dto.Password == "••••••••" ? currentPassword : dto.Password;
                settings["Email.FromEmail"] = dto.FromEmail;
                settings["Email.FromName"] = dto.FromName;
                settings["Email.UseSSL"] = dto.UseSSL.ToString();
                settings["Email.UseTLS"] = dto.UseTLS.ToString();

                await _settingService.UpdateEmailSettingsAsync(settings, userId);

                return Ok(new { message = "Email settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email settings");
                return StatusCode(500, new { message = "Failed to update email settings" });
            }
        }

        /// <summary>
        /// Send test email to verify email configuration
        /// </summary>
        [HttpPost("email/test")]
        public async Task<ActionResult> SendTestEmail([FromBody] TestEmailDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var htmlBody = $@"
                    <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif;"">
                        <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                            <h1 style=""color: white; margin: 0;"">The Betts Firm</h1>
                        </div>
                        <div style=""padding: 2rem; background: white;"">
                            <h2>Email Configuration Test</h2>
                            <p>This is a test email to verify that your email configuration is working correctly.</p>
                            <p><strong>Test Details:</strong></p>
                            <ul>
                                <li>Sent from: The Betts Firm Email System</li>
                                <li>Date/Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                                <li>Configuration: Admin Email Settings</li>
                            </ul>
                            <p>If you received this email, your configuration is working properly!</p>
                        </div>
                    </div>";

                // Create a simple test method in email service
                await SendTestEmailDirectly(dto.ToEmail, dto.Subject, htmlBody);

                return Ok(new { message = "Test email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", dto.ToEmail);
                return BadRequest(new { message = $"Failed to send test email: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get all settings by category
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetSettingsByCategory(string category)
        {
            try
            {
                var settings = await _settingService.GetSettingsByCategoryAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for category {Category}", category);
                return StatusCode(500, new { message = "Failed to retrieve settings" });
            }
        }

        /// <summary>
        /// Get tax settings (GST registration threshold, etc.)
        /// </summary>
        [HttpGet("tax")]
        public async Task<ActionResult<TaxSettingsDto>> GetTaxSettings()
        {
            try
            {
                var threshold = await _settingService.GetSettingAsync<decimal>("Tax.GST.RegistrationThreshold");
                var gstRate = await _settingService.GetSettingAsync<decimal>("Tax.GST.RatePercent");
                var annualInterest = await _settingService.GetSettingAsync<decimal>("Tax.AnnualInterestRatePercent");
                var minTaxRate = await _settingService.GetSettingAsync<decimal>("Tax.Income.MinimumTaxRatePercent");
                var matRate = await _settingService.GetSettingAsync<decimal>("Tax.Income.MATRatePercent");
                var dto = new TaxSettingsDto
                {
                    GstRegistrationThreshold = ((decimal?)threshold) ?? 0m,
                    GstRatePercent = ((decimal?)gstRate) ?? 15m,
                    AnnualInterestRatePercent = ((decimal?)annualInterest) ?? 15m,
                    IncomeMinimumTaxRatePercent = ((decimal?)minTaxRate) ?? 0.5m,
                    IncomeMatRatePercent = ((decimal?)matRate) ?? 3m
                };
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax settings");
                return StatusCode(500, new { message = "Failed to retrieve tax settings" });
            }
        }

        /// <summary>
        /// Update tax settings
        /// </summary>
        [HttpPost("tax")]
        public async Task<ActionResult> UpdateTaxSettings([FromBody] TaxSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to identify current user");
                }

                await _settingService.SetSettingAsync(
                    key: "Tax.GST.RegistrationThreshold",
                    value: dto.GstRegistrationThreshold.ToString(CultureInfo.InvariantCulture),
                    userId: userId,
                    description: "Annual turnover threshold for GST registration (SLE)",
                    category: "Tax");

                await _settingService.SetSettingAsync(
                    key: "Tax.GST.RatePercent",
                    value: dto.GstRatePercent.ToString(CultureInfo.InvariantCulture),
                    userId: userId,
                    description: "GST standard rate (percent)",
                    category: "Tax");

                await _settingService.SetSettingAsync(
                    key: "Tax.AnnualInterestRatePercent",
                    value: dto.AnnualInterestRatePercent.ToString(CultureInfo.InvariantCulture),
                    userId: userId,
                    description: "Annual interest rate for late payments (percent)",
                    category: "Tax");

                await _settingService.SetSettingAsync(
                    key: "Tax.Income.MinimumTaxRatePercent",
                    value: dto.IncomeMinimumTaxRatePercent.ToString(CultureInfo.InvariantCulture),
                    userId: userId,
                    description: "Income Tax minimum tax rate for companies (percent of turnover)",
                    category: "Tax");

                await _settingService.SetSettingAsync(
                    key: "Tax.Income.MATRatePercent",
                    value: dto.IncomeMatRatePercent.ToString(CultureInfo.InvariantCulture),
                    userId: userId,
                    description: "Minimum Alternate Tax rate (percent of turnover)",
                    category: "Tax");

                return Ok(new { message = "Tax settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tax settings");
                return StatusCode(500, new { message = "Failed to update tax settings" });
            }
        }

        private async Task SendTestEmailDirectly(string toEmail, string subject, string body)
        {
            // Get current email settings and send test email
            var emailSettings = await _settingService.GetEmailSettingsAsync();

            var smtpHost = emailSettings.GetValueOrDefault("Email.SmtpHost", "");
            var smtpPort = int.Parse(emailSettings.GetValueOrDefault("Email.SmtpPort", "587"));
            var username = emailSettings.GetValueOrDefault("Email.Username", "");
            var password = emailSettings.GetValueOrDefault("Email.Password", "");
            var fromEmail = emailSettings.GetValueOrDefault("Email.FromEmail", "noreply@thebettsfirmsl.com");
            var fromName = emailSettings.GetValueOrDefault("Email.FromName", "The Betts Firm");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Email settings are not properly configured");
            }

            // Use MailKit directly for testing
            using var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(fromName, fromEmail));
            message.To.Add(new MimeKit.MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new MimeKit.BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            
            var useSSL = bool.Parse(emailSettings.GetValueOrDefault("Email.UseSSL", "true"));
            var useTLS = bool.Parse(emailSettings.GetValueOrDefault("Email.UseTLS", "true"));
            
            var secureSocketOptions = MailKit.Security.SecureSocketOptions.Auto;
            if (useSSL)
                secureSocketOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;
            else if (useTLS)
                secureSocketOptions = MailKit.Security.SecureSocketOptions.StartTls;
            else
                secureSocketOptions = MailKit.Security.SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}