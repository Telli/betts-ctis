namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents a system user entry for administration screens.
/// </summary>
public class AdminUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditLogEntryDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ActingFor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// Represents a configurable tax rate entry.
/// </summary>
public class TaxRateDto
{
    public string Type { get; set; } = string.Empty;
    public string Rate { get; set; } = string.Empty;
    public string ApplicableTo { get; set; } = string.Empty;
}

/// <summary>
/// Represents the status of a background job/task.
/// </summary>
public class JobStatusDto
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public string BadgeVariant { get; set; } = "default";
}
