namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Aggregated KPI data for internal and client views.
/// </summary>
public class KpiSummaryDto
{
    public IReadOnlyList<KpiMetricDto> InternalMetrics { get; init; } = Array.Empty<KpiMetricDto>();
    public IReadOnlyList<KpiMetricDto> ClientMetrics { get; init; } = Array.Empty<KpiMetricDto>();
    public IReadOnlyList<KpiTrendPointDto> MonthlyTrend { get; init; } = Array.Empty<KpiTrendPointDto>();
    public IReadOnlyList<ClientPerformanceDto> ClientPerformance { get; init; } = Array.Empty<ClientPerformanceDto>();
    public IReadOnlyList<PerformanceBreakdownDto> PerformanceBreakdown { get; init; } = Array.Empty<PerformanceBreakdownDto>();
}

/// <summary>
/// Represents a single KPI metric card.
/// </summary>
public class KpiMetricDto
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
/// Combined data point for the monthly trend charts.
/// </summary>
public class KpiTrendPointDto
{
    public string Month { get; init; } = string.Empty;
    public decimal Compliance { get; init; }
    public decimal Timeliness { get; init; }
    public decimal Payments { get; init; }
}

/// <summary>
/// Performance snapshot for a specific client.
/// </summary>
public class ClientPerformanceDto
{
    public string Name { get; init; } = string.Empty;
    public decimal Score { get; init; }
}

/// <summary>
/// Detailed metric breakdown for client view.
/// </summary>
public class PerformanceBreakdownDto
{
    public string Metric { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public string Color { get; init; } = "info";
}
