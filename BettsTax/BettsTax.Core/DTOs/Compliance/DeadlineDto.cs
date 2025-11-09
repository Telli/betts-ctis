namespace BettsTax.Core.DTOs.Compliance;

/// <summary>
/// Represents a tax filing deadline entry.
/// </summary>
public class DeadlineDto
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DeadlineStatus Status { get; set; }
    public DeadlinePriority Priority { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

/// <summary>
/// Deadline completion status.
/// </summary>
public enum DeadlineStatus
{
    Upcoming,
    DueSoon,
    Overdue,
    Completed
}

/// <summary>
/// Priority classification for upcoming deadlines.
/// </summary>
public enum DeadlinePriority
{
    Low,
    Medium,
    High,
    Critical
}
