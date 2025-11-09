namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Summary data for the dashboard experience.
/// </summary>
public class DashboardSummaryDto
{
    public IReadOnlyList<DashboardMetricDto> Metrics { get; init; } = Array.Empty<DashboardMetricDto>();
    public IReadOnlyList<DashboardTrendPointDto> FilingTrends { get; init; } = Array.Empty<DashboardTrendPointDto>();
    public IReadOnlyList<DistributionSliceDto> ComplianceDistribution { get; init; } = Array.Empty<DistributionSliceDto>();
    public IReadOnlyList<DashboardDeadlineDto> UpcomingDeadlines { get; init; } = Array.Empty<DashboardDeadlineDto>();
    public IReadOnlyList<DashboardActivityDto> RecentActivity { get; init; } = Array.Empty<DashboardActivityDto>();
}

/// <summary>
/// Represents a high-level metric displayed on the dashboard.
/// </summary>
public class DashboardMetricDto
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string TrendDirection { get; init; } = "neutral";
    public string TrendValue { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Color { get; init; } = "default";
}

/// <summary>
/// Trend point representing filing performance for a period.
/// </summary>
public class DashboardTrendPointDto
{
    public string Month { get; init; } = string.Empty;
    public decimal OnTime { get; init; }
    public decimal Late { get; init; }
}

/// <summary>
/// Slice data for distribution charts.
/// </summary>
public class DistributionSliceDto
{
    public string Name { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string Color { get; init; } = string.Empty;
}

/// <summary>
/// Basic representation of an upcoming deadline relevant to dashboard.
/// </summary>
public class DashboardDeadlineDto
{
    public int Id { get; init; }
    public string Client { get; init; } = string.Empty;
    public string TaxType { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Recent activity feed entry.
/// </summary>
public class DashboardActivityDto
{
    public string TimeDescription { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
}
