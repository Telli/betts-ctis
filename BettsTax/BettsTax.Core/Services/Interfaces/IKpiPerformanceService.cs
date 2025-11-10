using BettsTax.Core.DTOs;

namespace BettsTax.Core.Services.Interfaces;

public interface IKpiPerformanceService
{
    /// <summary>
    /// Get cached monthly KPI aggregates.
    /// </summary>
    Task<List<KpiTrendDto>> GetMonthlyAggregatesAsync(int months = 12, CancellationToken ct = default);

    /// <summary>
    /// Get cached quarterly KPI aggregates.
    /// </summary>
    Task<List<KpiTrendDto>> GetQuarterlyAggregatesAsync(int quarters = 4, CancellationToken ct = default);

    /// <summary>
    /// Refresh cached aggregates.
    /// </summary>
    Task RefreshAggregatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get KPI performance metrics.
    /// </summary>
    Task<KpiPerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Warm up KPI caches.
    /// </summary>
    Task WarmupCachesAsync(CancellationToken ct = default);
}
