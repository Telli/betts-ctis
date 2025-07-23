using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class ActivityTimelineDto
    {
        public int ActivityTimelineId { get; set; }
        public ActivityType ActivityType { get; set; }
        public ActivityCategory Category { get; set; }
        public ActivityPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Entity References
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientNumber { get; set; }
        
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        
        public string? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        
        // Related Entities
        public int? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        
        public int? TaxFilingId { get; set; }
        public string? TaxFilingReference { get; set; }
        
        public int? PaymentId { get; set; }
        public string? PaymentReference { get; set; }
        
        public int? MessageId { get; set; }
        
        // Metadata
        public string? Metadata { get; set; }
        public DateTime ActivityDate { get; set; }
        
        // UI Helpers
        public string Icon { get; set; } = string.Empty; // Icon name for UI
        public string Color { get; set; } = string.Empty; // Color class for UI
        public string TimeAgo { get; set; } = string.Empty; // "5 minutes ago"
        public bool IsNew { get; set; } // For highlighting new activities
    }

    public class ActivityTimelineCreateDto
    {
        public ActivityType ActivityType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ClientId { get; set; }
        public string? TargetUserId { get; set; }
        public int? DocumentId { get; set; }
        public int? TaxFilingId { get; set; }
        public int? PaymentId { get; set; }
        public int? MessageId { get; set; }
        public string? Metadata { get; set; }
        public ActivityPriority Priority { get; set; } = ActivityPriority.Normal;
        public bool IsVisibleToClient { get; set; } = true;
        public bool IsVisibleToAssociate { get; set; } = true;
        public bool IsVisibleToAdmin { get; set; } = true;
    }

    public class ActivityTimelineFilterDto
    {
        public int? ClientId { get; set; }
        public string? UserId { get; set; }
        public ActivityType? ActivityType { get; set; }
        public ActivityCategory? Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsVisibleToClient { get; set; }
        public ActivityPriority? MinPriority { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ActivityTimelineGroupDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public List<ActivityTimelineDto> Activities { get; set; } = new();
        public int TotalActivities { get; set; }
    }

    public class ActivityTimelineSummaryDto
    {
        public int TotalActivities { get; set; }
        public int TodayActivities { get; set; }
        public int WeekActivities { get; set; }
        public int MonthActivities { get; set; }
        public Dictionary<ActivityCategory, int> ActivitiesByCategory { get; set; } = new();
        public Dictionary<ActivityPriority, int> ActivitiesByPriority { get; set; } = new();
        public List<ActivityTimelineDto> RecentHighPriorityActivities { get; set; } = new();
    }

    public class ClientActivitySummaryDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime LastActivityDate { get; set; }
        public int TotalActivities { get; set; }
        public int DocumentActivities { get; set; }
        public int TaxFilingActivities { get; set; }
        public int PaymentActivities { get; set; }
        public int CommunicationActivities { get; set; }
        public ActivityTimelineDto? LastActivity { get; set; }
    }
}