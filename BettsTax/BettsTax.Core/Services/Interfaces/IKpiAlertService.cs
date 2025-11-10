using BettsTax.Core.DTOs;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

public interface IKpiAlertService
{
    /// <summary>
    /// Process and broadcast KPI alerts.
    /// </summary>
    Task ProcessAlertsAsync(List<KpiAlert> alerts, CancellationToken ct = default);

    /// <summary>
    /// Get active unresolved alerts.
    /// </summary>
    Task<List<KpiAlertDto>> GetActiveAlertsAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolve an alert.
    /// </summary>
    Task ResolveAlertAsync(int alertId, string resolvedBy, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Send alert notifications to administrators.
    /// </summary>
    Task SendAlertNotificationsAsync(List<KpiAlert> alerts, CancellationToken ct = default);
}
