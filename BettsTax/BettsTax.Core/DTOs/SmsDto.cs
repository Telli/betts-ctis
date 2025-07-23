using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class SmsNotificationDto
    {
        public int SmsNotificationId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string Message { get; set; } = string.Empty;
        public SmsType Type { get; set; }
        public SmsProvider Provider { get; set; }
        public SmsStatus Status { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ProviderResponse { get; set; }
        public decimal? Cost { get; set; }
        public string? Currency { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? FailedDate { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryDate { get; set; }
        public int? TaxFilingId { get; set; }
        public int? PaymentId { get; set; }
        public int? DocumentId { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public bool IsScheduled { get; set; }
    }

    public class SendSmsDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public SmsType Type { get; set; } = SmsType.General;
        public int? ClientId { get; set; }
        public int? TaxFilingId { get; set; }
        public int? PaymentId { get; set; }
        public int? DocumentId { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public SmsProvider? PreferredProvider { get; set; }
    }

    public class BulkSmsDto
    {
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
        public SmsType Type { get; set; } = SmsType.General;
        public DateTime? ScheduledDate { get; set; }
    }

    public class SmsTemplateDto
    {
        public int SmsTemplateId { get; set; }
        public string TemplateCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public SmsType Type { get; set; }
        public Dictionary<string, string> AvailableVariables { get; set; } = new Dictionary<string, string>();
        public bool IsActive { get; set; }
        public int CharacterCount { get; set; }
    }

    public class ApplySmsTemplateDto
    {
        public int TemplateId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class SmsProviderConfigDto
    {
        public int SmsProviderConfigId { get; set; }
        public SmsProvider Provider { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? ApiUrl { get; set; }
        public string? SenderId { get; set; }
        public Dictionary<string, string> AdditionalSettings { get; set; } = new Dictionary<string, string>();
        public decimal CostPerSms { get; set; }
        public string Currency { get; set; } = "SLE";
        public int Priority { get; set; } = 1;
        public int? DailyLimit { get; set; }
        public int? MonthlyLimit { get; set; }
        public int DailyUsage { get; set; }
        public int MonthlyUsage { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }

    public class SmsScheduleDto
    {
        public int SmsScheduleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SmsType Type { get; set; }
        public int DaysBefore { get; set; }
        public string TimeOfDay { get; set; } = "09:00";
        public bool IsRecurring { get; set; }
        public int? RecurrenceIntervalDays { get; set; }
        public int SmsTemplateId { get; set; }
        public string? TemplateName { get; set; }
        public TaxType? TaxType { get; set; }
        public TaxpayerCategory? TaxpayerCategory { get; set; }
        public bool OnlyActiveClients { get; set; } = true;
        public bool IsActive { get; set; }
        public DateTime? LastRunDate { get; set; }
    }

    public class SmsStatisticsDto
    {
        public int TotalSent { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalFailed { get; set; }
        public int TotalPending { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "SLE";
        public Dictionary<SmsType, int> ByType { get; set; } = new Dictionary<SmsType, int>();
        public Dictionary<SmsProvider, int> ByProvider { get; set; } = new Dictionary<SmsProvider, int>();
        public Dictionary<string, int> DailyCount { get; set; } = new Dictionary<string, int>();
    }

    public class SmsBalanceDto
    {
        public SmsProvider Provider { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "SLE";
        public int MessagesRemaining { get; set; }
        public DateTime? LastChecked { get; set; }
    }
}