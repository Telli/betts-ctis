using BettsTax.Core.DTOs.Dashboard;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Dashboard service interface
/// </summary>
public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(int? clientId = null);
    Task<List<FilingTrendDto>> GetFilingTrendsAsync(int? clientId = null, int months = 6);
    Task<List<ComplianceDistributionDto>> GetComplianceDistributionAsync(int? clientId = null);
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int limit = 10);
    Task<List<RecentActivityDto>> GetRecentActivityAsync(int? clientId = null, int limit = 10);
}
