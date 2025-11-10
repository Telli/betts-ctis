using BettsTax.Core.DTOs.Dashboard;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// Dashboard service implementation with mock data
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ILogger<DashboardService> logger)
    {
        _logger = logger;
    }

    public Task<DashboardMetricsDto> GetMetricsAsync(int? clientId = null)
    {
        _logger.LogInformation("Getting dashboard metrics for clientId: {ClientId}", clientId);

        // Mock data - replace with database queries
        var metrics = new DashboardMetricsDto
        {
            ClientComplianceRate = clientId.HasValue ? 94 : 92,
            FilingTimeliness = clientId.HasValue ? 18 : 15,
            PaymentCompletion = clientId.HasValue ? 100 : 87,
            DocumentCompliance = clientId.HasValue ? 92 : 94
        };

        return Task.FromResult(metrics);
    }

    public Task<List<FilingTrendDto>> GetFilingTrendsAsync(int? clientId = null, int months = 6)
    {
        _logger.LogInformation("Getting filing trends for {Months} months, clientId: {ClientId}", months, clientId);

        // Mock data - replace with database queries
        var trends = new List<FilingTrendDto>
        {
            new FilingTrendDto { Month = "Jan", OnTime = 85, Late = 15 },
            new FilingTrendDto { Month = "Feb", OnTime = 88, Late = 12 },
            new FilingTrendDto { Month = "Mar", OnTime = 92, Late = 8 },
            new FilingTrendDto { Month = "Apr", OnTime = 87, Late = 13 },
            new FilingTrendDto { Month = "May", OnTime = 90, Late = 10 },
            new FilingTrendDto { Month = "Jun", OnTime = 94, Late = 6 }
        };

        return Task.FromResult(trends.Take(months).ToList());
    }

    public Task<List<ComplianceDistributionDto>> GetComplianceDistributionAsync(int? clientId = null)
    {
        _logger.LogInformation("Getting compliance distribution for clientId: {ClientId}", clientId);

        // Mock data - replace with database queries
        var distribution = new List<ComplianceDistributionDto>
        {
            new ComplianceDistributionDto { Name = "Fully Compliant", Value = 65, Color = "#38a169" },
            new ComplianceDistributionDto { Name = "Pending", Value = 20, Color = "#d69e2e" },
            new ComplianceDistributionDto { Name = "At Risk", Value = 10, Color = "#e53e3e" },
            new ComplianceDistributionDto { Name = "Non-Compliant", Value = 5, Color = "#1a202c" }
        };

        return Task.FromResult(distribution);
    }

    public Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int limit = 10)
    {
        _logger.LogInformation("Getting {Limit} upcoming deadlines for clientId: {ClientId}", limit, clientId);

        // Mock data - replace with database queries
        var deadlines = new List<UpcomingDeadlineDto>
        {
            new UpcomingDeadlineDto
            {
                Client = "ABC Corp",
                Type = "GST Return",
                DueDate = "2025-11-15",
                DaysLeft = 5,
                Status = "pending"
            },
            new UpcomingDeadlineDto
            {
                Client = "XYZ Ltd",
                Type = "Income Tax",
                DueDate = "2025-11-20",
                DaysLeft = 10,
                Status = "pending"
            },
            new UpcomingDeadlineDto
            {
                Client = "Tech Solutions",
                Type = "Payroll Tax",
                DueDate = "2025-11-12",
                DaysLeft = 2,
                Status = "at-risk"
            },
            new UpcomingDeadlineDto
            {
                Client = "Global Trade",
                Type = "Excise Duty",
                DueDate = "2025-11-11",
                DaysLeft = 1,
                Status = "urgent"
            }
        };

        return Task.FromResult(deadlines.Take(limit).ToList());
    }

    public Task<List<RecentActivityDto>> GetRecentActivityAsync(int? clientId = null, int limit = 10)
    {
        _logger.LogInformation("Getting {Limit} recent activities for clientId: {ClientId}", limit, clientId);

        // Mock data - replace with database queries
        var activities = new List<RecentActivityDto>
        {
            new RecentActivityDto
            {
                Time = "2 hours ago",
                Action = "GST Return filed for ABC Corp",
                User = "Jane Smith"
            },
            new RecentActivityDto
            {
                Time = "4 hours ago",
                Action = "Document uploaded: Financial Statements",
                User = "John Doe"
            },
            new RecentActivityDto
            {
                Time = "Yesterday",
                Action = "Payment processed: SLE 15,000",
                User = "Sarah Johnson"
            },
            new RecentActivityDto
            {
                Time = "2 days ago",
                Action = "New client onboarded: Tech Innovations Ltd",
                User = "Mike Brown"
            }
        };

        return Task.FromResult(activities.Take(limit).ToList());
    }
}
