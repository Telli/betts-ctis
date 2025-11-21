# Email Configuration Guide - The Betts Firm CTIS

## Overview

The CTIS system uses **MailKit** for email sending with fully configurable SMTP settings managed through the Admin Settings panel. Email settings are stored in the database and can be updated without restarting the application.

## Technology Stack

- **Email Library:** MailKit (NuGet package)
- **Email Templates:** MimeKit for message composition
- **Configuration:** Database-driven (SystemSetting table)
- **Security:** SSL/TLS support with encrypted password storage

## Email Settings Configuration

### Admin Settings Panel

Navigate to **Admin Settings → Email Configuration** to manage email settings:

1. **SMTP Host** - The SMTP server address (e.g., smtp.gmail.com, smtp.office365.com)
2. **SMTP Port** - The SMTP port (typically 587 for TLS, 465 for SSL)
3. **Username** - SMTP authentication username
4. **Password** - SMTP authentication password (encrypted in database)
5. **From Email** - The sender email address
6. **From Name** - The sender display name
7. **Use SSL** - Enable SSL/TLS on connection (port 465)
8. **Use TLS** - Enable STARTTLS (port 587)

### Database Storage

Email settings are stored in the `SystemSetting` table with:
- **Category:** "Email"
- **Keys:** Email.SmtpHost, Email.SmtpPort, Email.Username, Email.Password, Email.FromEmail, Email.FromName, Email.UseSSL, Email.UseTLS
- **IsEncrypted:** Password field is encrypted for security

## Supported SMTP Providers

### Gmail
```
SMTP Host: smtp.gmail.com
SMTP Port: 587
Use TLS: true
Use SSL: false
Username: your-email@gmail.com
Password: App Password (not regular password)
```

**Note:** Enable 2-Step Verification and generate an App Password at https://myaccount.google.com/apppasswords

### Office 365 / Outlook
```
SMTP Host: smtp.office365.com
SMTP Port: 587
Use TLS: true
Use SSL: false
Username: your-email@company.com
Password: Your Office 365 password
```

### SendGrid
```
SMTP Host: smtp.sendgrid.net
SMTP Port: 587
Use TLS: true
Use SSL: false
Username: apikey
Password: SG.your-api-key
```

### AWS SES (Simple Email Service)
```
SMTP Host: email-smtp.region.amazonaws.com
SMTP Port: 587
Use TLS: true
Use SSL: false
Username: SMTP username from AWS Console
Password: SMTP password from AWS Console
```

### Custom SMTP Server
```
SMTP Host: your-smtp-server.com
SMTP Port: 587 or 465
Use TLS: true (for port 587) or false (for port 465)
Use SSL: false (for port 587) or true (for port 465)
Username: your-username
Password: your-password
```

## Email Features

### Implemented Email Types

1. **Client Invitation** - Sent when inviting new clients to register
2. **Welcome Email** - Sent after successful client registration
3. **Email Verification** - Sent for email address verification
4. **Password Reset** - Sent for password reset requests
5. **Registration Completed** - Notification to associates when client completes registration
6. **Payment Notifications** - Payment status updates (initiated, completed, failed)
7. **Compliance Alerts** - Deadline and compliance notifications
8. **Document Verification** - Document status updates

### Email Service Architecture

**File:** `BettsTax.Core/Services/EmailService.cs`

```csharp
public class EmailService : IEmailService
{
    // Sends emails using MailKit with configurable SMTP settings
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        // 1. Retrieves email settings from database
        // 2. Validates SMTP configuration
        // 3. Creates MimeMessage with HTML body
        // 4. Connects to SMTP server with SSL/TLS
        // 5. Authenticates and sends message
        // 6. Logs success/failure
    }
}
```

## Testing Email Configuration

### Test Email Feature

1. Go to **Admin Settings → Email Configuration**
2. Click **"Send Test Email"** button
3. Enter recipient email address
4. Click **"Send"**
5. Check recipient inbox for test email

### Troubleshooting

**Email not sending:**
- Verify SMTP credentials are correct
- Check firewall allows outbound SMTP connections
- Verify port is not blocked (587 or 465)
- Check email logs in application

**Authentication failed:**
- Verify username and password
- For Gmail, use App Password, not regular password
- For Office 365, ensure account is active
- Check if 2-factor authentication is enabled

**Connection timeout:**
- Verify SMTP host is correct
- Check network connectivity
- Verify firewall rules
- Try different port (587 vs 465)

## Email Logging

All email sending attempts are logged with:
- Recipient email address
- Subject line
- Success/failure status
- Error messages (if failed)
- Timestamp

**Log Location:** Application logs (Serilog)

## Security Considerations

1. **Password Encryption:** Email passwords are encrypted in the database
2. **SSL/TLS:** Always use SSL or TLS for SMTP connections
3. **Credentials:** Never commit credentials to version control
4. **Environment Variables:** Use environment variables for sensitive settings
5. **Access Control:** Only Admin/SystemAdmin roles can modify email settings

## API Endpoints

### Get Email Settings
```
GET /api/adminsettings/email
Authorization: Bearer {token}
Response: EmailSettingsDto
```

### Update Email Settings
```
POST /api/adminsettings/email
Authorization: Bearer {token}
Body: EmailSettingsDto
Response: { message: "Email settings updated successfully" }
```

### Send Test Email
```
POST /api/adminsettings/email/test
Authorization: Bearer {token}
Body: { toEmail: "test@example.com", subject?: "...", body?: "..." }
Response: { message: "Test email sent successfully" }
```

## Production Deployment Checklist

- [ ] Configure SMTP settings in Admin Settings panel
- [ ] Test email sending with test email feature
- [ ] Verify email templates are rendering correctly
- [ ] Set up email monitoring and alerting
- [ ] Configure backup SMTP provider (optional)
- [ ] Test email delivery to spam folder
- [ ] Set up SPF, DKIM, DMARC records (if using custom domain)
- [ ] Monitor email sending logs
- [ ] Set up email bounce handling (optional)

## Next Steps

1. Configure email settings in Admin Settings panel
2. Send test email to verify configuration
3. Monitor email logs for any issues
4. Set up email monitoring and alerting
5. Consider implementing email queue for high-volume sending

