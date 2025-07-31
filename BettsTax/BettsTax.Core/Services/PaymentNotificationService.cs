using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Data.Models;
using PaymentTransaction = BettsTax.Data.Models.PaymentTransaction;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.DTOs;

namespace BettsTax.Core.Services;

/// <summary>
/// Payment notification service for SMS and email alerts
/// Handles payment confirmations, receipts, and status updates for Sierra Leone clients
/// </summary>
public class PaymentNotificationService : IPaymentNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentNotificationService> _logger;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;

    public PaymentNotificationService(
        ApplicationDbContext context,
        ILogger<PaymentNotificationService> logger,
        ISmsService smsService,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _smsService = smsService;
        _emailService = emailService;
    }

    #region Transaction Notifications

    public async Task<bool> SendPaymentInitiatedNotificationAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var notificationTasks = new List<Task<bool>>();

            // Send SMS notification if client has SMS enabled
            if (await ShouldSendSmsNotificationAsync(transaction.ClientId, "PaymentInitiated"))
            {
                notificationTasks.Add(SendPaymentInitiatedSmsAsync(transaction));
            }

            // Send email notification if client has email enabled
            if (await ShouldSendEmailNotificationAsync(transaction.ClientId, "PaymentInitiated"))
            {
                notificationTasks.Add(SendPaymentInitiatedEmailAsync(transaction));
            }

            if (notificationTasks.Count == 0)
            {
                _logger.LogDebug("No notification preferences enabled for transaction {TransactionId}", transactionId);
                return true;
            }

            var results = await Task.WhenAll(notificationTasks);
            var success = results.Any(r => r);

            _logger.LogInformation(
                "Payment initiated notifications sent for transaction {TransactionId}. SMS: {HasSms}, Email: {HasEmail}, Success: {Success}",
                transactionId, notificationTasks.Count > 0, notificationTasks.Count > 1, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment initiated notifications for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentCompletedNotificationAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var notificationTasks = new List<Task<bool>>();

            // Send SMS notification
            if (await ShouldSendSmsNotificationAsync(transaction.ClientId, "PaymentCompleted"))
            {
                notificationTasks.Add(SendPaymentCompletedSmsAsync(transaction));
            }

            // Send email notification
            if (await ShouldSendEmailNotificationAsync(transaction.ClientId, "PaymentCompleted"))
            {
                notificationTasks.Add(SendPaymentCompletedEmailAsync(transaction));
            }

            if (notificationTasks.Count == 0)
                return true;

            var results = await Task.WhenAll(notificationTasks);
            var success = results.Any(r => r);

            _logger.LogInformation(
                "Payment completed notifications sent for transaction {TransactionId}. Success: {Success}",
                transactionId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment completed notifications for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentFailedNotificationAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var notificationTasks = new List<Task<bool>>();

            // Send SMS notification
            if (await ShouldSendSmsNotificationAsync(transaction.ClientId, "PaymentFailed"))
            {
                notificationTasks.Add(SendPaymentFailedSmsAsync(transaction));
            }

            // Send email notification
            if (await ShouldSendEmailNotificationAsync(transaction.ClientId, "PaymentFailed"))
            {
                notificationTasks.Add(SendPaymentFailedEmailAsync(transaction));
            }

            if (notificationTasks.Count == 0)
                return true;

            var results = await Task.WhenAll(notificationTasks);
            var success = results.Any(r => r);

            _logger.LogInformation(
                "Payment failed notifications sent for transaction {TransactionId}. Success: {Success}",
                transactionId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failed notifications for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentCancelledNotificationAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var notificationTasks = new List<Task<bool>>();

            // Send SMS notification
            if (await ShouldSendSmsNotificationAsync(transaction.ClientId, "PaymentCancelled"))
            {
                notificationTasks.Add(SendPaymentCancelledSmsAsync(transaction));
            }

            // Send email notification
            if (await ShouldSendEmailNotificationAsync(transaction.ClientId, "PaymentCancelled"))
            {
                notificationTasks.Add(SendPaymentCancelledEmailAsync(transaction));
            }

            if (notificationTasks.Count == 0)
                return true;

            var results = await Task.WhenAll(notificationTasks);
            var success = results.Any(r => r);

            _logger.LogInformation(
                "Payment cancelled notifications sent for transaction {TransactionId}. Success: {Success}",
                transactionId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment cancelled notifications for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentRefundedNotificationAsync(int refundId)
    {
        try
        {
            var refund = await _context.PaymentRefunds
                .Include(r => r.OriginalTransaction)
                .ThenInclude(t => t.Client)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null)
                return false;

            var notificationTasks = new List<Task<bool>>();

            // Send SMS notification
            if (await ShouldSendSmsNotificationAsync(refund.OriginalTransaction.ClientId, "PaymentRefunded"))
            {
                notificationTasks.Add(SendRefundSmsAsync(refund));
            }

            // Send email notification
            if (await ShouldSendEmailNotificationAsync(refund.OriginalTransaction.ClientId, "PaymentRefunded"))
            {
                notificationTasks.Add(SendRefundEmailAsync(refund));
            }

            if (notificationTasks.Count == 0)
                return true;

            var results = await Task.WhenAll(notificationTasks);
            var success = results.Any(r => r);

            _logger.LogInformation(
                "Payment refund notifications sent for refund {RefundId}. Success: {Success}",
                refundId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment refund notifications for refund {RefundId}", refundId);
            return false;
        }
    }

    #endregion

    #region SMS Notifications

    public async Task<bool> SendPaymentSmsAsync(string phoneNumber, string message)
    {
        try
        {
            await _smsService.SendSmsAsync(new SendSmsDto 
            { 
                PhoneNumber = phoneNumber, 
                Message = message 
            });
            
            _logger.LogDebug("Payment SMS sent to {PhoneNumber}", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendPaymentConfirmationSmsAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var variables = new Dictionary<string, string>
            {
                { "ClientName", transaction.Client?.BusinessName ?? "Client" },
                { "Amount", transaction.Amount.ToString("N0") },
                { "Currency", transaction.Currency },
                { "TransactionRef", transaction.TransactionReference },
                { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
                { "Date", transaction.InitiatedAt.ToString("dd/MM/yyyy HH:mm") }
            };

            var message = await GetPaymentNotificationTemplateAsync("PaymentConfirmationSms", variables);
            
            // Send to client's registered phone number
            var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
            return await SendPaymentSmsAsync(clientPhone, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation SMS for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentReceiptSmsAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null || transaction.Status != BettsTax.Data.Models.PaymentTransactionStatus.Completed)
                return false;

            var variables = new Dictionary<string, string>
            {
                { "ClientName", transaction.Client?.BusinessName ?? "Client" },
                { "Amount", transaction.Amount.ToString("N0") },
                { "Fee", transaction.Fee.ToString("N0") },
                { "NetAmount", transaction.NetAmount.ToString("N0") },
                { "Currency", transaction.Currency },
                { "TransactionRef", transaction.TransactionReference },
                { "ExternalRef", transaction.ExternalReference ?? "" },
                { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
                { "CompletedDate", transaction.CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "" }
            };

            var message = await GetPaymentNotificationTemplateAsync("PaymentReceiptSms", variables);
            
            var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
            return await SendPaymentSmsAsync(clientPhone, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment receipt SMS for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendOtpSmsAsync(string phoneNumber, string otpCode)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                { "OtpCode", otpCode },
                { "ExpiryMinutes", "5" } // OTP expires in 5 minutes
            };

            var message = await GetPaymentNotificationTemplateAsync("PaymentOtpSms", variables);
            
            return await SendPaymentSmsAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    #endregion

    #region Email Notifications

    public async Task<bool> SendPaymentEmailAsync(string email, string subject, string message)
    {
        try
        {
            await _emailService.SendEmailAsync(email, subject, message);
            
            _logger.LogDebug("Payment email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPaymentConfirmationEmailAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null || string.IsNullOrEmpty(transaction.Client?.Email))
                return false;

            var variables = new Dictionary<string, string>
            {
                { "ClientName", transaction.Client.BusinessName ?? "Client" },
                { "Amount", transaction.Amount.ToString("N0") },
                { "Currency", transaction.Currency },
                { "TransactionRef", transaction.TransactionReference },
                { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
                { "PayerPhone", transaction.PayerPhone },
                { "Date", transaction.InitiatedAt.ToString("dd/MM/yyyy HH:mm") },
                { "Status", transaction.Status.ToString() }
            };

            var subject = $"Payment Confirmation - {transaction.TransactionReference}";
            var message = await GetPaymentNotificationTemplateAsync("PaymentConfirmationEmail", variables);
            
            return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation email for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentReceiptEmailAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null || transaction.Status != BettsTax.Data.Models.PaymentTransactionStatus.Completed || 
                string.IsNullOrEmpty(transaction.Client?.Email))
                return false;

            var variables = new Dictionary<string, string>
            {
                { "ClientName", transaction.Client.BusinessName ?? "Client" },
                { "ClientNumber", transaction.Client.ClientNumber ?? "" },
                { "Amount", transaction.Amount.ToString("N0") },
                { "Fee", transaction.Fee.ToString("N0") },
                { "NetAmount", transaction.NetAmount.ToString("N0") },
                { "Currency", transaction.Currency },
                { "TransactionRef", transaction.TransactionReference },
                { "ExternalRef", transaction.ExternalReference ?? "" },
                { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
                { "PayerPhone", transaction.PayerPhone },
                { "Purpose", transaction.Purpose.ToString() },
                { "CompletedDate", transaction.CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            var subject = $"Payment Receipt - {transaction.TransactionReference}";
            var message = await GetPaymentNotificationTemplateAsync("PaymentReceiptEmail", variables);
            
            return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment receipt email for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> SendPaymentFailureEmailAsync(int transactionId)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null || string.IsNullOrEmpty(transaction.Client?.Email))
                return false;

            var variables = new Dictionary<string, string>
            {
                { "ClientName", transaction.Client.BusinessName ?? "Client" },
                { "Amount", transaction.Amount.ToString("N0") },
                { "Currency", transaction.Currency },
                { "TransactionRef", transaction.TransactionReference },
                { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
                { "ErrorMessage", transaction.StatusMessage ?? "Payment processing failed" },
                { "FailedDate", transaction.FailedAt?.ToString("dd/MM/yyyy HH:mm") ?? "" },
                { "SupportEmail", "support@bettsfirm.com" },
                { "SupportPhone", "+232-XX-XXX-XXX" }
            };

            var subject = $"Payment Failed - {transaction.TransactionReference}";
            var message = await GetPaymentNotificationTemplateAsync("PaymentFailureEmail", variables);
            
            return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failure email for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Template Management

    public async Task<string> GetPaymentNotificationTemplateAsync(string templateName, Dictionary<string, string> variables)
    {
        try
        {
            // Get template from database or use default
            var template = await GetTemplateContentAsync(templateName);
            
            // Replace variables in template
            foreach (var variable in variables)
            {
                template = template.Replace($"{{{variable.Key}}}", variable.Value);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment notification template {TemplateName}", templateName);
            return GetFallbackTemplate(templateName, variables);
        }
    }

    public async Task<bool> SendTemplatedNotificationAsync(int transactionId, string templateName, Dictionary<string, string> variables)
    {
        try
        {
            var transaction = await GetTransactionWithDetailsAsync(transactionId);
            if (transaction == null)
                return false;

            var message = await GetPaymentNotificationTemplateAsync(templateName, variables);
            var success = false;

            // Send SMS if template is for SMS
            if (templateName.Contains("Sms") && await ShouldSendSmsNotificationAsync(transaction.ClientId, templateName))
            {
                var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
                success |= await SendPaymentSmsAsync(clientPhone, message);
            }

            // Send Email if template is for Email
            if (templateName.Contains("Email") && await ShouldSendEmailNotificationAsync(transaction.ClientId, templateName))
            {
                var subject = variables.GetValueOrDefault("Subject", $"Payment Notification - {transaction.TransactionReference}");
                success |= await SendPaymentEmailAsync(transaction.Client?.Email ?? "", subject, message);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated notification for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Missing Interface Methods - Stub Implementations

    public async Task<bool> SendPaymentReminderAsync(int transactionId)
    {
        // TODO: Implement payment reminder logic
        throw new NotImplementedException("Payment reminder is not yet implemented");
    }

    public async Task<bool> SendPaymentReceiptAsync(int transactionId)
    {
        // Use existing implementation
        return await SendPaymentReceiptEmailAsync(transactionId);
    }

    public async Task<bool> SendPaymentConfirmationAsync(int transactionId, string confirmationCode)
    {
        // TODO: Implement payment confirmation with code logic
        throw new NotImplementedException("Payment confirmation with code is not yet implemented");
    }

    public async Task<PaymentNotificationPreferencesDto> GetNotificationPreferencesAsync(int clientId)
    {
        // TODO: Implement notification preferences retrieval
        throw new NotImplementedException("Notification preferences retrieval is not yet implemented");
    }

    public async Task<string> GetSmsTemplateAsync(string templateName, Dictionary<string, string> variables)
    {
        // Use existing implementation
        return await GetPaymentNotificationTemplateAsync(templateName, variables);
    }

    public async Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> variables)
    {
        // Use existing implementation
        return await GetPaymentNotificationTemplateAsync(templateName, variables);
    }

    public async Task<bool> UpdateNotificationTemplateAsync(string templateName, string content, string updatedBy)
    {
        // TODO: Implement notification template update logic
        throw new NotImplementedException("Notification template update is not yet implemented");
    }

    public async Task<List<PaymentNotificationHistoryDto>> GetNotificationHistoryAsync(int transactionId)
    {
        // TODO: Implement notification history retrieval
        throw new NotImplementedException("Notification history retrieval is not yet implemented");
    }

    public async Task<List<PaymentNotificationHistoryDto>> GetClientNotificationHistoryAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        // TODO: Implement client notification history retrieval
        throw new NotImplementedException("Client notification history retrieval is not yet implemented");
    }

    public async Task<bool> UpdateDeliveryStatusAsync(string notificationId, string status, string details)
    {
        // TODO: Implement delivery status update logic
        throw new NotImplementedException("Delivery status update is not yet implemented");
    }

    public async Task<PaymentNotificationDeliveryStatsDto> GetDeliveryStatsAsync(DateTime fromDate, DateTime toDate)
    {
        // TODO: Implement delivery stats retrieval
        throw new NotImplementedException("Delivery stats retrieval is not yet implemented");
    }

    #endregion

    #region Notification Preferences

    public async Task<bool> ShouldSendSmsNotificationAsync(int clientId, string notificationType)
    {
        try
        {
            // Check client notification preferences
            // For now, default to true (send all SMS notifications)
            // In production, this would check client preferences in database
            
            _logger.LogDebug("SMS notification check for client {ClientId}, type {NotificationType}: enabled", 
                clientId, notificationType);
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SMS notification preference for client {ClientId}", clientId);
            return false;
        }
    }

    public async Task<bool> ShouldSendEmailNotificationAsync(int clientId, string notificationType)
    {
        try
        {
            // Check client notification preferences
            // For now, default to true (send all email notifications)
            // In production, this would check client preferences in database
            
            _logger.LogDebug("Email notification check for client {ClientId}, type {NotificationType}: enabled", 
                clientId, notificationType);
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email notification preference for client {ClientId}", clientId);
            return false;
        }
    }

    public async Task<bool> UpdateNotificationPreferencesAsync(int clientId, UpdateNotificationPreferencesDto request, string updatedBy)
    {
        try
        {
            // Update client notification preferences in database
            // This would store preferences like:
            // - SMS for payment completed: true/false
            // - Email for payment failed: true/false
            // - etc.
            
            _logger.LogInformation("Updated notification preferences for client {ClientId} by {UpdatedBy}", 
                clientId, updatedBy);
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for client {ClientId}", clientId);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<PaymentTransaction?> GetTransactionWithDetailsAsync(int transactionId)
    {
        return await _context.PaymentGatewayTransactions
            .Include(t => t.Client)
            .Include(t => t.GatewayConfig)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    private async Task<string> GetTemplateContentAsync(string templateName)
    {
        // In production, this would fetch from database
        // For now, return default templates
        return templateName switch
        {
            "PaymentConfirmationSms" => 
                "Payment initiated for {ClientName}. Amount: {Amount} {Currency}. Ref: {TransactionRef}. {Gateway} - {Date}",
            
            "PaymentReceiptSms" => 
                "Payment completed! {ClientName} paid {Amount} {Currency} (Fee: {Fee}). Ref: {TransactionRef}. {CompletedDate}",
            
            "PaymentFailedSms" => 
                "Payment failed for {ClientName}. Amount: {Amount} {Currency}. Ref: {TransactionRef}. Please contact support.",
            
            "PaymentCancelledSms" => 
                "Payment cancelled for {ClientName}. Amount: {Amount} {Currency}. Ref: {TransactionRef}.",
            
            "PaymentOtpSms" => 
                "Your payment verification code is: {OtpCode}. Valid for {ExpiryMinutes} minutes. Do not share this code.",
            
            "PaymentConfirmationEmail" => 
                GeneratePaymentConfirmationEmailTemplate(),
            
            "PaymentReceiptEmail" => 
                GeneratePaymentReceiptEmailTemplate(),
            
            "PaymentFailureEmail" => 
                GeneratePaymentFailureEmailTemplate(),
            
            _ => "Payment notification for transaction {TransactionRef}."
        };
    }

    private string GeneratePaymentConfirmationEmailTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Payment Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color: #1e40af;'>Payment Confirmation</h2>
    
    <p>Dear {ClientName},</p>
    
    <p>We have received your payment request with the following details:</p>
    
    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Transaction Reference:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{TransactionRef}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Amount:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Amount} {Currency}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Payment Method:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Gateway}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Date:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Date}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Status:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Status}</td>
        </tr>
    </table>
    
    <p>You will receive another notification once the payment is processed.</p>
    
    <p>Thank you for using Betts Tax Services.</p>
    
    <hr style='margin: 30px 0;'>
    <p style='font-size: 12px; color: #666;'>
        The Betts Firm<br>
        Sierra Leone Tax Compliance System<br>
        Freetown, Sierra Leone
    </p>
</body>
</html>";
    }

    private string GeneratePaymentReceiptEmailTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Payment Receipt</title>
</head>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color: #16a34a;'>Payment Receipt</h2>
    
    <p>Dear {ClientName},</p>
    
    <p>Your payment has been successfully processed. Here are the details:</p>
    
    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Client Number:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{ClientNumber}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Transaction Reference:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{TransactionRef}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>External Reference:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{ExternalRef}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Amount:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Amount} {Currency}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Processing Fee:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Fee} {Currency}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Net Amount:</td>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>{NetAmount} {Currency}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Payment Method:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Gateway}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Purpose:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Purpose}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Completed Date:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{CompletedDate}</td>
        </tr>
    </table>
    
    <p style='color: #16a34a; font-weight: bold;'>✓ Payment Successful</p>
    
    <p>This serves as your official payment receipt. Please keep this for your records.</p>
    
    <p>Thank you for your payment.</p>
    
    <hr style='margin: 30px 0;'>
    <p style='font-size: 12px; color: #666;'>
        The Betts Firm<br>
        Sierra Leone Tax Compliance System<br>
        Freetown, Sierra Leone<br>
        © {Year} All rights reserved
    </p>
</body>
</html>";
    }

    private string GeneratePaymentFailureEmailTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Payment Failed</title>
</head>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color: #dc2626;'>Payment Processing Failed</h2>
    
    <p>Dear {ClientName},</p>
    
    <p>We were unable to process your payment. Please see the details below:</p>
    
    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Transaction Reference:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{TransactionRef}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Amount:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Amount} {Currency}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Payment Method:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{Gateway}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Failed Date:</td>
            <td style='border: 1px solid #ddd; padding: 8px;'>{FailedDate}</td>
        </tr>
        <tr>
            <td style='border: 1px solid #ddd; padding: 8px; font-weight: bold;'>Error:</td>
            <td style='border: 1px solid #ddd; padding: 8px; color: #dc2626;'>{ErrorMessage}</td>
        </tr>
    </table>
    
    <p style='color: #dc2626; font-weight: bold;'>✗ Payment Failed</p>
    
    <h3>Next Steps:</h3>
    <ul>
        <li>Please check your mobile money account balance</li>
        <li>Ensure your phone number is registered for mobile money</li>
        <li>Try the payment again with the correct PIN</li>
        <li>Contact your mobile money provider if the issue persists</li>
    </ul>
    
    <p>If you continue to experience issues, please contact our support team:</p>
    <p>
        Email: {SupportEmail}<br>
        Phone: {SupportPhone}
    </p>
    
    <hr style='margin: 30px 0;'>
    <p style='font-size: 12px; color: #666;'>
        The Betts Firm<br>
        Sierra Leone Tax Compliance System<br>
        Freetown, Sierra Leone
    </p>
</body>
</html>";
    }

    private string GetGatewayDisplayName(PaymentGatewayType gatewayType)
    {
        return gatewayType switch
        {
            PaymentGatewayType.OrangeMoney => "Orange Money",
            PaymentGatewayType.AfricellMoney => "Africell Money",
            PaymentGatewayType.BankTransfer => "Bank Transfer",
            PaymentGatewayType.CreditCard => "Credit Card",
            PaymentGatewayType.Cash => "Cash Payment",
            _ => gatewayType.ToString()
        };
    }

    private string GetFallbackTemplate(string templateName, Dictionary<string, string> variables)
    {
        return $"Payment notification: {variables.GetValueOrDefault("TransactionRef", "N/A")} - {variables.GetValueOrDefault("Amount", "0")} {variables.GetValueOrDefault("Currency", "SLE")}";
    }

    // Private methods for specific notification types
    private async Task<bool> SendPaymentInitiatedSmsAsync(PaymentTransaction transaction)
    {
        var variables = new Dictionary<string, string>
        {
            { "ClientName", transaction.Client?.BusinessName ?? "Client" },
            { "Amount", transaction.Amount.ToString("N0") },
            { "Currency", transaction.Currency },
            { "TransactionRef", transaction.TransactionReference },
            { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
            { "Date", transaction.InitiatedAt.ToString("dd/MM/yyyy HH:mm") }
        };

        var message = await GetPaymentNotificationTemplateAsync("PaymentConfirmationSms", variables);
        var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
        
        return await SendPaymentSmsAsync(clientPhone, message);
    }

    private async Task<bool> SendPaymentInitiatedEmailAsync(PaymentTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.Client?.Email))
            return false;

        var variables = new Dictionary<string, string>
        {
            { "ClientName", transaction.Client.BusinessName ?? "Client" },
            { "Amount", transaction.Amount.ToString("N0") },
            { "Currency", transaction.Currency },
            { "TransactionRef", transaction.TransactionReference },
            { "Gateway", GetGatewayDisplayName(transaction.GatewayType) },
            { "PayerPhone", transaction.PayerPhone },
            { "Date", transaction.InitiatedAt.ToString("dd/MM/yyyy HH:mm") },
            { "Status", "Initiated" }
        };

        var subject = $"Payment Initiated - {transaction.TransactionReference}";
        var message = await GetPaymentNotificationTemplateAsync("PaymentConfirmationEmail", variables);
        
        return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
    }

    private async Task<bool> SendPaymentCompletedSmsAsync(PaymentTransaction transaction)
    {
        return await SendPaymentReceiptSmsAsync(transaction.Id);
    }

    private async Task<bool> SendPaymentCompletedEmailAsync(PaymentTransaction transaction)
    {
        return await SendPaymentReceiptEmailAsync(transaction.Id);
    }

    private async Task<bool> SendPaymentFailedSmsAsync(PaymentTransaction transaction)
    {
        var variables = new Dictionary<string, string>
        {
            { "ClientName", transaction.Client?.BusinessName ?? "Client" },
            { "Amount", transaction.Amount.ToString("N0") },
            { "Currency", transaction.Currency },
            { "TransactionRef", transaction.TransactionReference }
        };

        var message = await GetPaymentNotificationTemplateAsync("PaymentFailedSms", variables);
        var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
        
        return await SendPaymentSmsAsync(clientPhone, message);
    }

    private async Task<bool> SendPaymentCancelledSmsAsync(PaymentTransaction transaction)
    {
        var variables = new Dictionary<string, string>
        {
            { "ClientName", transaction.Client?.BusinessName ?? "Client" },
            { "Amount", transaction.Amount.ToString("N0") },
            { "Currency", transaction.Currency },
            { "TransactionRef", transaction.TransactionReference }
        };

        var message = await GetPaymentNotificationTemplateAsync("PaymentCancelledSms", variables);
        var clientPhone = transaction.Client?.PhoneNumber ?? transaction.PayerPhone;
        
        return await SendPaymentSmsAsync(clientPhone, message);
    }

    private async Task<bool> SendRefundSmsAsync(PaymentRefund refund)
    {
        var variables = new Dictionary<string, string>
        {
            { "ClientName", refund.OriginalTransaction.Client?.BusinessName ?? "Client" },
            { "Amount", refund.RefundAmount.ToString("N0") },
            { "Currency", "SLE" },
            { "RefundRef", refund.RefundReference },
            { "OriginalRef", refund.OriginalTransaction.TransactionReference }
        };

        var message = $"Refund processed for {variables["ClientName"]}. Amount: {variables["Amount"]} {variables["Currency"]}. Refund Ref: {variables["RefundRef"]}";
        var clientPhone = refund.OriginalTransaction.Client?.PhoneNumber ?? refund.OriginalTransaction.PayerPhone;
        
        return await SendPaymentSmsAsync(clientPhone, message);
    }

    private async Task<bool> SendRefundEmailAsync(PaymentRefund refund)
    {
        if (string.IsNullOrEmpty(refund.OriginalTransaction.Client?.Email))
            return false;

        var subject = $"Payment Refund Processed - {refund.RefundReference}";
        var message = $@"
        <h2>Payment Refund Processed</h2>
        <p>Dear {refund.OriginalTransaction.Client.BusinessName},</p>
        <p>Your refund has been processed successfully.</p>
        <p><strong>Refund Amount:</strong> {refund.RefundAmount:N0} SLE</p>
        <p><strong>Refund Reference:</strong> {refund.RefundReference}</p>
        <p><strong>Original Transaction:</strong> {refund.OriginalTransaction.TransactionReference}</p>
        <p><strong>Reason:</strong> {refund.Reason}</p>
        ";

        return await SendPaymentEmailAsync(refund.OriginalTransaction.Client.Email, subject, message);
    }

    private async Task<bool> SendPaymentFailedEmailAsync(PaymentTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.Client?.Email))
            return false;

        var subject = $"Payment Failed - {transaction.TransactionReference}";
        var message = $@"
        <h2>Payment Failed</h2>
        <p>Dear {transaction.Client.BusinessName},</p>
        <p>Your payment could not be processed.</p>
        <p><strong>Amount:</strong> {transaction.Amount:N0} {transaction.Currency}</p>
        <p><strong>Transaction Reference:</strong> {transaction.TransactionReference}</p>
        <p><strong>Payment Method:</strong> {GetGatewayDisplayName(transaction.GatewayType)}</p>
        <p><strong>Error:</strong> {transaction.StatusMessage ?? "Payment processing failed"}</p>
        <p>Please try again or contact support if the issue persists.</p>
        ";

        return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
    }

    private async Task<bool> SendPaymentCancelledEmailAsync(PaymentTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.Client?.Email))
            return false;

        var subject = $"Payment Cancelled - {transaction.TransactionReference}";
        var message = $@"
        <h2>Payment Cancelled</h2>
        <p>Dear {transaction.Client.BusinessName},</p>
        <p>Your payment has been cancelled.</p>
        <p><strong>Amount:</strong> {transaction.Amount:N0} {transaction.Currency}</p>
        <p><strong>Transaction Reference:</strong> {transaction.TransactionReference}</p>
        <p><strong>Payment Method:</strong> {GetGatewayDisplayName(transaction.GatewayType)}</p>
        <p><strong>Reason:</strong> {transaction.StatusMessage ?? "Payment cancelled by user"}</p>
        ";

        return await SendPaymentEmailAsync(transaction.Client.Email, subject, message);
    }

    #endregion
}