using BettsTax.Core.DTOs.Payment;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Interface for payment notification service
/// Handles SMS and email notifications for payment updates
/// </summary>
public interface IPaymentNotificationService
{
    // Payment Notifications
    Task<bool> SendPaymentCompletedNotificationAsync(int transactionId);
    Task<bool> SendPaymentFailedNotificationAsync(int transactionId);
    Task<bool> SendPaymentInitiatedNotificationAsync(int transactionId);
    Task<bool> SendPaymentReminderAsync(int transactionId);

    // Receipt and Confirmation
    Task<bool> SendPaymentReceiptAsync(int transactionId);
    Task<bool> SendPaymentConfirmationAsync(int transactionId, string confirmationCode);

    // Notification Preferences
    Task<PaymentNotificationPreferencesDto> GetNotificationPreferencesAsync(int clientId);
    Task<bool> UpdateNotificationPreferencesAsync(int clientId, UpdateNotificationPreferencesDto request, string updatedBy);
    Task<bool> ShouldSendSmsNotificationAsync(int clientId, string notificationType);
    Task<bool> ShouldSendEmailNotificationAsync(int clientId, string notificationType);

    // Template Management
    Task<string> GetSmsTemplateAsync(string templateName, Dictionary<string, string> variables);
    Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> variables);
    Task<bool> UpdateNotificationTemplateAsync(string templateName, string content, string updatedBy);

    // Notification History
    Task<List<PaymentNotificationHistoryDto>> GetNotificationHistoryAsync(int transactionId);
    Task<List<PaymentNotificationHistoryDto>> GetClientNotificationHistoryAsync(int clientId, DateTime fromDate, DateTime toDate);

    // Delivery Status
    Task<bool> UpdateDeliveryStatusAsync(string notificationId, string status, string details);
    Task<PaymentNotificationDeliveryStatsDto> GetDeliveryStatsAsync(DateTime fromDate, DateTime toDate);
}