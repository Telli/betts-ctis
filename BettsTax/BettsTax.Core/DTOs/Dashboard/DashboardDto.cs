namespace BettsTax.Core.DTOs.Dashboard;

/// <summary>
/// Dashboard metrics DTO
/// </summary>
public class DashboardMetricsDto
{
    public decimal ClientComplianceRate { get; set; }
    public int FilingTimeliness { get; set; }
    public decimal PaymentCompletion { get; set; }
    public decimal DocumentCompliance { get; set; }
}

/// <summary>
/// Filing trend data for charts
/// </summary>
public class FilingTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int OnTime { get; set; }
    public int Late { get; set; }
}

/// <summary>
/// Compliance distribution for pie chart
/// </summary>
public class ComplianceDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Upcoming deadline for dashboard widget
/// </summary>
public class UpcomingDeadlineDto
{
    public string Client { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public int DaysLeft { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Recent activity entry
/// </summary>
public class RecentActivityDto
{
    public string Time { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}
