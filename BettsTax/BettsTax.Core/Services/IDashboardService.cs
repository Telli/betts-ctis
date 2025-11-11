using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Data.Models;
using System.Threading.Tasks;
using ComplianceOverviewDto = BettsTax.Core.DTOs.ComplianceOverviewDto;

namespace BettsTax.Core.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(string userId);
        Task<ClientSummaryDto> GetClientSummaryAsync();
        Task<ComplianceOverviewDto> GetComplianceOverviewAsync();
        Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10);
        Task<IEnumerable<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int days = 30);
        Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(string userId);
        Task<NavigationCountsDto> GetNavigationCountsAsync(string userId);
        Task<DashboardMetricsDto> GetDashboardMetricsAsync();
        Task<QuickActionsResponseDto> GetQuickActionsAsync(string userId);

        // Client-specific dashboard methods
        Task<ClientDashboardDto> GetClientDashboardDataAsync(int clientId);
        Task<ClientComplianceOverviewDto> GetClientComplianceOverviewAsync(int clientId);
        Task<IEnumerable<RecentActivityDto>> GetClientRecentActivityAsync(int clientId, int count = 10);
        Task<IEnumerable<UpcomingDeadlineDto>> GetClientUpcomingDeadlinesAsync(int clientId, int days = 30);
    }
}
