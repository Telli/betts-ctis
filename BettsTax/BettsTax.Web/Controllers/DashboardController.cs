using BettsTax.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var data = await _dashboardService.GetDashboardDataAsync(userId);
            return Ok(new { success = true, data });
        }

        [HttpGet("client-summary")]
        public async Task<IActionResult> GetClientSummary()
        {
            var summary = await _dashboardService.GetClientSummaryAsync();
            return Ok(new { success = true, data = summary });
        }

        [HttpGet("compliance")]
        public async Task<IActionResult> GetComplianceOverview()
        {
            var overview = await _dashboardService.GetComplianceOverviewAsync();
            return Ok(new { success = true, data = overview });
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 10)
        {
            var activity = await _dashboardService.GetRecentActivityAsync(count);
            return Ok(new { success = true, data = activity });
        }

        [HttpGet("deadlines")]
        public async Task<IActionResult> GetUpcomingDeadlines([FromQuery] int days = 30)
        {
            var deadlines = await _dashboardService.GetUpcomingDeadlinesAsync(days);
            return Ok(new { success = true, data = deadlines });
        }

        [HttpGet("pending-approvals")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var approvals = await _dashboardService.GetPendingApprovalsAsync(userId);
            return Ok(new { success = true, data = approvals });
        }

        [HttpGet("navigation-counts")]
        public async Task<IActionResult> GetNavigationCounts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var counts = await _dashboardService.GetNavigationCountsAsync(userId);
            return Ok(new { success = true, data = counts });
        }
    }
}
