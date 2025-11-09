using BettsTax.Core.DTOs.Compliance;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Contract for retrieving compliance deadline data.
/// </summary>
public interface IDeadlineMonitoringService
{
    /// <summary>
    /// Retrieve upcoming deadlines due within the specified window.
    /// </summary>
    Task<IReadOnlyList<DeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId, int daysAhead);

    /// <summary>
    /// Retrieve deadlines that are already overdue.
    /// </summary>
    Task<IReadOnlyList<DeadlineDto>> GetOverdueItemsAsync(int? clientId);
}
