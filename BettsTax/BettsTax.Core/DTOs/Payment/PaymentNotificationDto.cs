using BettsTax.Data.Models;

namespace BettsTax.Core.DTOs.Payment;

// Notification preferences DTOs
public class PaymentNotificationPreferencesDto
{
    public int ClientId { get; set; }
    public bool EnableSmsNotifications { get; set; }
    public bool EnableEmailNotifications { get; set; }
    public bool NotifyOnPaymentCompleted { get; set; }
    public bool NotifyOnPaymentFailed { get; set; }
    public bool NotifyOnPaymentInitiated { get; set; }
    public bool SendPaymentReminders { get; set; }
    public bool SendPaymentReceipts { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string SmsPhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateNotificationPreferencesDto
{
    public bool EnableSmsNotifications { get; set; }
    public bool EnableEmailNotifications { get; set; }
    public bool NotifyOnPaymentCompleted { get; set; }
    public bool NotifyOnPaymentFailed { get; set; }
    public bool NotifyOnPaymentInitiated { get; set; }
    public bool SendPaymentReminders { get; set; }
    public bool SendPaymentReceipts { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string SmsPhoneNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
}

// Notification history DTOs
public class PaymentNotificationHistoryDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int ClientId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty; // SMS, Email
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sent, Delivered, Failed
    public string DeliveryDetails { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

// Delivery statistics DTOs
public class PaymentNotificationDeliveryStatsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalNotifications { get; set; }
    public int SmsNotifications { get; set; }
    public int EmailNotifications { get; set; }
    public int DeliveredNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public double DeliverySuccessRate { get; set; }
    public Dictionary<string, int> DeliveryByType { get; set; } = new();
    public Dictionary<string, int> FailuresByReason { get; set; } = new();
    public double AverageDeliveryTime { get; set; }
}

// Mobile money provider DTOs
public class PhoneValidationResultDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public PaymentGatewayType? DetectedProvider { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public string FormattedNumber { get; set; } = string.Empty;
    public bool IsFreetownNumber { get; set; }
}

public class MobileMoneyProviderStatusDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = string.Empty; // Online, Offline, Maintenance
    public DateTime LastChecked { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public double ResponseTime { get; set; }
    public double UptimePercentage { get; set; }
}

public class MobileMoneyProviderHealthDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public DateTime LastHealthCheck { get; set; }
    public double ResponseTime { get; set; }
    public double UptimePercentage { get; set; }
    public List<string> HealthChecks { get; set; } = new();
    public Dictionary<string, object> HealthMetrics { get; set; } = new();
}

// Mobile money payment DTOs
public class MobileMoneyPaymentDto
{
    public string Pin { get; set; } = string.Empty;
}

public class ValidatePhoneDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public PaymentGatewayType GatewayType { get; set; }
}

// Payment action DTOs
public class CancelPaymentDto
{
    public string Reason { get; set; } = string.Empty;
}

public class RefundPaymentDto
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ScheduleRetryDto
{
    public DateTime ScheduledAt { get; set; }
}

public class ProcessDeadLetterDto
{
    public string Action { get; set; } = string.Empty; // RETRY, RESOLVE, DISCARD
}

public class BlockTransactionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class FlagTransactionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class ReviewTransactionDto
{
    public bool Approve { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
}

public class SendNotificationDto
{
    public string NotificationType { get; set; } = string.Empty; // completed, failed, reminder
}