using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum SmsStatus
    {
        Pending,
        Sent,
        Delivered,
        Failed,
        Expired
    }

    public enum SmsProvider
    {
        OrangeSL,
        AfricellSL,
        Twilio,
        AfricasTalking
    }

    public enum SmsType
    {
        DeadlineReminder,
        PaymentConfirmation,
        DocumentRequest,
        TaxFilingConfirmation,
        PasswordReset,
        TwoFactorAuth,
        General
    }

    public class SmsNotification
    {
        [Key]
        public int SmsNotificationId { get; set; }

        // Recipient information
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        // Message content
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public SmsType Type { get; set; } = SmsType.General;

        // Provider and delivery information
        public SmsProvider Provider { get; set; } = SmsProvider.OrangeSL;
        public SmsStatus Status { get; set; } = SmsStatus.Pending;

        [MaxLength(100)]
        public string? ProviderMessageId { get; set; }

        [MaxLength(500)]
        public string? ProviderResponse { get; set; }

        // Cost tracking
        public decimal? Cost { get; set; }
        
        [MaxLength(3)]
        public string? Currency { get; set; } = "SLE";

        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? FailedDate { get; set; }

        // Retry information
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryDate { get; set; }

        // Related entities
        public int? TaxFilingId { get; set; }
        [ForeignKey("TaxFilingId")]
        public virtual TaxFiling? TaxFiling { get; set; }

        public int? PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }

        public int? DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        // Scheduling
        public DateTime? ScheduledDate { get; set; }
        public bool IsScheduled { get; set; }

        // Template reference
        public int? SmsTemplateId { get; set; }
        [ForeignKey("SmsTemplateId")]
        public virtual SmsTemplate? SmsTemplate { get; set; }
    }

    public class SmsTemplate
    {
        [Key]
        public int SmsTemplateId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string MessageTemplate { get; set; } = string.Empty;

        public SmsType Type { get; set; }

        // Variables available in template (JSON)
        public string? AvailableVariables { get; set; }

        public bool IsActive { get; set; } = true;

        // Character count for cost estimation
        public int CharacterCount { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }

    public class SmsProviderConfig
    {
        [Key]
        public int SmsProviderConfigId { get; set; }

        [Required]
        public SmsProvider Provider { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // API Configuration (encrypted)
        [MaxLength(500)]
        public string? ApiKey { get; set; }

        [MaxLength(500)]
        public string? ApiSecret { get; set; }

        [MaxLength(200)]
        public string? ApiUrl { get; set; }

        [MaxLength(50)]
        public string? SenderId { get; set; }

        // Provider-specific settings (JSON)
        public string? AdditionalSettings { get; set; }

        // Cost per SMS
        public decimal CostPerSms { get; set; }
        
        [MaxLength(3)]
        public string Currency { get; set; } = "SLE";

        // Priority and limits
        public int Priority { get; set; } = 1;
        public int? DailyLimit { get; set; }
        public int? MonthlyLimit { get; set; }

        // Current usage
        public int DailyUsage { get; set; }
        public int MonthlyUsage { get; set; }
        public DateTime? UsageResetDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }

    public class SmsSchedule
    {
        [Key]
        public int SmsScheduleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public SmsType Type { get; set; }

        // Schedule configuration
        public int DaysBefore { get; set; } // Days before deadline
        
        [MaxLength(5)]
        public string TimeOfDay { get; set; } = "09:00"; // HH:mm format

        public bool IsRecurring { get; set; }
        public int? RecurrenceIntervalDays { get; set; }

        // Template to use
        public int SmsTemplateId { get; set; }
        [ForeignKey("SmsTemplateId")]
        public virtual SmsTemplate SmsTemplate { get; set; } = null!;

        // Filters
        public TaxType? TaxType { get; set; }
        public TaxpayerCategory? TaxpayerCategory { get; set; }
        public bool OnlyActiveClients { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public DateTime? LastRunDate { get; set; }
    }
}