using BettsTax.Core.DTOs.KPI;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// KPI service interface
/// </summary>
public interface IKpiService
{
    Task<KpiMetricsDto> GetKpiMetricsAsync(int? clientId = null);
    Task<List<MonthlyTrendDto>> GetMonthlyTrendsAsync(int? clientId = null, int months = 6);
    Task<List<ClientPerformanceDto>> GetClientPerformanceAsync(int limit = 10);
}
