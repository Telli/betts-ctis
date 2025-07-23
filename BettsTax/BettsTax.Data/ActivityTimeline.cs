using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public enum ActivityType
    {
        // Client Actions
        ClientRegistered,
        ClientLoggedIn,
        ClientProfileUpdated,
        ClientStatusChanged,
        
        // Document Actions
        DocumentUploaded,
        DocumentViewed,
        DocumentDownloaded,
        DocumentDeleted,
        DocumentVerified,
        DocumentRejected,
        DocumentRequested,
        
        // Tax Filing Actions
        TaxFilingCreated,
        TaxFilingSubmitted,
        TaxFilingReviewed,
        TaxFilingApproved,
        TaxFilingRejected,
        TaxFilingFiled,
        TaxFilingUpdated,
        
        // Payment Actions
        PaymentCreated,
        PaymentApproved,
        PaymentRejected,
        PaymentProcessed,
        PaymentReceiptGenerated,
        
        // Communication Actions
        MessageSent,
        MessageReceived,
        EmailSent,
        EmailReceived,
        SMSSent,
        NotificationSent,
        NotificationRead,
        
        // System Actions
        DeadlineApproaching,
        DeadlineMissed,
        ComplianceScoreUpdated,
        ComplianceUpdate,
        AssociateAssigned,
        AssociateChanged,
        SystemGenerated,
        
        // Administrative Actions
        UserCreated,
        UserDeactivated,
        RoleChanged,
        PermissionGranted,
        PermissionRevoked
    }

    public enum ActivityCategory
    {
        Client,
        Document,
        TaxFiling,
        Payment,
        Communication,
        System,
        Administrative
    }

    public enum ActivityPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class ActivityTimeline
    {
        public int ActivityTimelineId { get; set; }
        
        // Activity Details
        public ActivityType ActivityType { get; set; }
        public ActivityCategory Category { get; set; }
        public ActivityPriority Priority { get; set; } = ActivityPriority.Normal;
        
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        // Entity References
        public int? ClientId { get; set; }
        public string? UserId { get; set; } // User who performed the action
        public string? TargetUserId { get; set; } // User affected by the action
        
        // Related Entity IDs (stored as JSON or comma-separated)
        public int? DocumentId { get; set; }
        public int? TaxFilingId { get; set; }
        public int? PaymentId { get; set; }
        public int? MessageId { get; set; }
        
        // Metadata
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON string for additional data
        
        [MaxLength(100)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        // Timestamps
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Visibility
        public bool IsVisibleToClient { get; set; } = true;
        public bool IsVisibleToAssociate { get; set; } = true;
        public bool IsVisibleToAdmin { get; set; } = true;
        
        // Navigation properties
        public Client? Client { get; set; }
        public ApplicationUser? User { get; set; }
        public ApplicationUser? TargetUser { get; set; }
        public Document? Document { get; set; }
        public TaxFiling? TaxFiling { get; set; }
        public Payment? Payment { get; set; }
    }

    // For grouping activities by date in the UI
    public class ActivityTimelineGroup
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty; // "Today", "Yesterday", "March 15, 2024"
        public List<ActivityTimeline> Activities { get; set; } = new();
    }
}