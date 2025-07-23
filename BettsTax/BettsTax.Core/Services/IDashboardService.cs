using BettsTax.Core.DTOs;
using System.Threading.Tasks;

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
        
        // Client-specific dashboard methods
        Task<ClientDashboardDto> GetClientDashboardDataAsync(int clientId);
        Task<ClientComplianceOverviewDto> GetClientComplianceOverviewAsync(int clientId);
        Task<IEnumerable<RecentActivityDto>> GetClientRecentActivityAsync(int clientId, int count = 10);
        Task<IEnumerable<UpcomingDeadlineDto>> GetClientUpcomingDeadlinesAsync(int clientId, int days = 30);
    }
}
