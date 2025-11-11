using BettsTax.Core.DTOs.KPI;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// KPI service implementation
/// </summary>
public class KpiService : IKpiService
{
    private readonly ILogger<KpiService> _logger;

    public KpiService(ILogger<KpiService> logger)
    {
        _logger = logger;
    }

    public async Task<KpiMetricsDto> GetKpiMetricsAsync(int? clientId = null)
    {
        _logger.LogInformation("Retrieving KPI metrics for clientId={ClientId}", clientId);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var metrics = new KpiMetricsDto
        {
            ComplianceRate = clientId.HasValue ? 95 : 92,
            AvgTimeliness = clientId.HasValue ? 88 : 85,
            PaymentCompletion = clientId.HasValue ? 98 : 87,
            DocSubmission = clientId.HasValue ? 92 : 89,
            EngagementRate = clientId.HasValue ? 85 : 78
        };

        return metrics;
    }

    public async Task<List<MonthlyTrendDto>> GetMonthlyTrendsAsync(int? clientId = null, int months = 6)
    {
        _logger.LogInformation("Retrieving monthly trends for clientId={ClientId}, months={Months}", clientId, months);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var trends = new List<MonthlyTrendDto>
        {
            new() { Month = "Aug", Compliance = 92, Timeliness = 85, Payments = 88 },
            new() { Month = "Sep", Compliance = 88, Timeliness = 82, Payments = 85 },
            new() { Month = "Oct", Compliance = 90, Timeliness = 88, Payments = 92 },
            new() { Month = "Nov", Compliance = 95, Timeliness = 90, Payments = 95 },
            new() { Month = "Dec", Compliance = 93, Timeliness = 87, Payments = 90 },
            new() { Month = "Jan", Compliance = 94, Timeliness = 91, Payments = 93 }
        };

        // If specific client requested, adjust values
        if (clientId.HasValue)
        {
            trends.ForEach(t =>
            {
                t.Compliance += 3;
                t.Timeliness += 2;
                t.Payments += 5;
            });
        }

        return trends.Take(months).ToList();
    }

    public async Task<List<ClientPerformanceDto>> GetClientPerformanceAsync(int limit = 10)
    {
        _logger.LogInformation("Retrieving client performance, limit={Limit}", limit);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var performance = new List<ClientPerformanceDto>
        {
            new() { Name = "Standard Chartered Bank SL", Score = 98 },
            new() { Name = "Sierra Leone Breweries Ltd", Score = 95 },
            new() { Name = "Orange Sierra Leone", Score = 92 },
            new() { Name = "Rokel Commercial Bank", Score = 88 },
            new() { Name = "Freetown Terminal Ltd", Score = 85 },
            new() { Name = "Sierra Rutile Limited", Score = 82 },
            new() { Name = "NRA - Revenue House", Score = 78 },
            new() { Name = "Mercury International", Score = 75 },
            new() { Name = "Africell Sierra Leone", Score = 72 },
            new() { Name = "Sierra Leone Commercial Bank", Score = 68 }
        };

        return performance.Take(limit).ToList();
    }
}
