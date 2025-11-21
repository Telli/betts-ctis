# Email Configuration Quick Start Guide

## 5-Minute Setup

### Step 1: Access Admin Settings
1. Log in as Admin or SystemAdmin
2. Navigate to **Admin Settings** → **Email Settings** tab

### Step 2: Enter SMTP Credentials

Choose your email provider and enter the credentials:

#### Gmail
```
SMTP Host: smtp.gmail.com
SMTP Port: 587
Username: your-email@gmail.com
Password: [App Password - NOT your regular password]
From Email: noreply@thebettsfirmsl.com
From Name: The Betts Firm
Use SSL: OFF
Use TLS: ON
```

**Important:** Generate an App Password at https://myaccount.google.com/apppasswords

#### Office 365
```
SMTP Host: smtp.office365.com
SMTP Port: 587
Username: your-email@company.com
Password: Your Office 365 password
From Email: noreply@thebettsfirmsl.com
From Name: The Betts Firm
Use SSL: OFF
Use TLS: ON
```

#### SendGrid
```
SMTP Host: smtp.sendgrid.net
SMTP Port: 587
Username: apikey
Password: SG.your-api-key
From Email: noreply@thebettsfirmsl.com
From Name: The Betts Firm
Use SSL: OFF
Use TLS: ON
```

### Step 3: Validate Configuration
1. Click **"Validate Settings"** button
2. You should see: "Email settings are valid and properly configured"
3. If validation fails, check the error message and correct the settings

### Step 4: Test Email
1. Click **"Send Test Email"** button
2. Enter a test email address
3. Click **"Send"**
4. Check your inbox for the test email

### Step 5: Save Settings
1. Click **"Save Email Settings"** button
2. You should see: "Email settings have been updated successfully"

## Troubleshooting

### "SMTP Host is not configured"
- Make sure SMTP Host field is not empty
- Check for typos (e.g., "smtp.gmail.com" not "smtp.gmail.com.")

### "SMTP authentication failed"
- Verify username and password are correct
- For Gmail, use App Password, not your regular password
- For Office 365, ensure your account is active
- Check if 2-factor authentication is enabled

### "Connection timeout"
- Verify SMTP host is correct
- Check network connectivity
- Try a different port (587 vs 465)
- Check firewall rules

### "Email not sending"
- Validate settings first using "Validate Settings" button
- Send a test email to verify configuration
- Check application logs for error details
- Verify SMTP credentials are correct

## Common SMTP Providers

| Provider | Host | Port | SSL | TLS |
|----------|------|------|-----|-----|
| Gmail | smtp.gmail.com | 587 | OFF | ON |
| Office 365 | smtp.office365.com | 587 | OFF | ON |
| SendGrid | smtp.sendgrid.net | 587 | OFF | ON |
| AWS SES | email-smtp.region.amazonaws.com | 587 | OFF | ON |
| Mailgun | smtp.mailgun.org | 587 | OFF | ON |

## API Endpoints

### Validate Email Settings
```bash
curl -X POST http://localhost:5000/api/adminsettings/email/validate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json"
```

### Get Email Settings
```bash
curl -X GET http://localhost:5000/api/adminsettings/email \
  -H "Authorization: Bearer {token}"
```

### Update Email Settings
```bash
curl -X POST http://localhost:5000/api/adminsettings/email \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "smtpHost": "smtp.gmail.com",
    "smtpPort": 587,
    "username": "your-email@gmail.com",
    "password": "your-app-password",
    "fromEmail": "noreply@thebettsfirmsl.com",
    "fromName": "The Betts Firm",
    "useSSL": false,
    "useTLS": true
  }'
```

### Send Test Email
```bash
curl -X POST http://localhost:5000/api/adminsettings/email/test \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "test@example.com",
    "subject": "Test Email",
    "body": "This is a test email"
  }'
```

## Security Best Practices

1. **Use App Passwords** - For Gmail, use App Passwords instead of your regular password
2. **Enable TLS** - Always use TLS (port 587) or SSL (port 465)
3. **Secure Credentials** - Passwords are encrypted in the database
4. **Admin Only** - Only Admin/SystemAdmin roles can modify email settings
5. **Audit Trail** - All changes are logged with user and timestamp

## Email Types Sent

The system automatically sends emails for:
- Client invitations
- Welcome emails
- Email verification
- Password resets
- Registration notifications
- Payment confirmations
- Compliance alerts
- Document notifications

## Support

For detailed configuration instructions, see:
- `EMAIL_CONFIGURATION_GUIDE.md` - Comprehensive guide
- `EMAIL_IMPLEMENTATION_SUMMARY.md` - Implementation details

## Monitoring

Check application logs for:
- Email sending status
- Authentication errors
- Connection issues
- Performance metrics (elapsed time)

All email operations are logged with timestamps and details.

