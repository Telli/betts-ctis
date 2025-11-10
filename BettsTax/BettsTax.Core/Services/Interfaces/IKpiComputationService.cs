using BettsTax.Core.DTOs;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

public interface IKpiComputationService
{
    /// <summary>
    /// Compute current KPI metrics from live data.
    /// </summary>
    Task<KpiMetricsDto> ComputeAsync(CancellationToken ct = default);

    /// <summary>
    /// Get latest cached KPI metrics (will compute if missing or stale).
    /// </summary>
    Task<KpiMetricsDto> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>
    /// Create daily KPI snapshot and persist to database.
    /// </summary>
    Task<KpiSnapshot> CreateDailySnapshotAsync(string? createdBy = null, CancellationToken ct = default);

    /// <summary>
    /// Get KPI trends using persisted snapshots.
    /// </summary>
    Task<List<KpiTrendDto>> GetKpiTrendsAsync(int days = 30, CancellationToken ct = default);

    /// <summary>
    /// Compute client-specific KPI metrics.
    /// </summary>
    Task<ClientKpiDto> ComputeClientKpiAsync(int clientId, CancellationToken ct = default);

    /// <summary>
    /// Generate KPI alerts based on thresholds.
    /// </summary>
    Task<List<KpiAlert>> GenerateAlertsAsync(KpiSnapshot snapshot, CancellationToken ct = default);

    /// <summary>
    /// Get document readiness breakdown for a client.
    /// </summary>
    Task<DocumentReadinessDto> GetDocumentReadinessAsync(int clientId, CancellationToken ct = default);

    /// <summary>
    /// Get on-time payment percentage for a client.
    /// </summary>
    Task<decimal> GetOnTimePaymentPercentageAsync(int clientId, CancellationToken ct = default);

    /// <summary>
    /// Get filing timeliness average for a client (days early/late).
    /// </summary>
    Task<decimal> GetFilingTimelinessAverageAsync(int clientId, CancellationToken ct = default);

    /// <summary>
    /// Get client engagement metrics.
    /// </summary>
    Task<ClientEngagementDto> GetClientEngagementAsync(int clientId, CancellationToken ct = default);
}
