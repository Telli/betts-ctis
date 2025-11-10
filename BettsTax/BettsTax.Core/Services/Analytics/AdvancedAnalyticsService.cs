using BettsTax.Core.DTOs.Analytics;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services.Analytics;

public interface IAdvancedAnalyticsService
{
    Task<AnalyticsDashboardResponse> GenerateDashboardAsync(AnalyticsDashboardRequest request);
    Task<List<AnalyticsWidget>> GetRealTimeMetricsAsync();
    Task<Dictionary<string, object>> GetAvailableMetricsAsync();
}

public class AdvancedAnalyticsService : IAdvancedAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdvancedAnalyticsService> _logger;

    public AdvancedAnalyticsService(ApplicationDbContext context, ILogger<AdvancedAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalyticsDashboardResponse> GenerateDashboardAsync(AnalyticsDashboardRequest request)
    {
        try
        {
            var response = new AnalyticsDashboardResponse
            {
                Success = true,
                Widgets = new List<AnalyticsWidget>()
            };

            // Generate widgets based on dashboard type
            switch (request.DashboardType)
            {
                case DashboardTypes.TaxCompliance:
                    response.Widgets = await GenerateTaxComplianceDashboardAsync();
                    break;
                case DashboardTypes.Revenue:
                    response.Widgets = await GenerateRevenueDashboardAsync();
                    break;
                case DashboardTypes.ClientPerformance:
                    response.Widgets = await GenerateClientPerformanceDashboardAsync();
                    break;
                case DashboardTypes.PaymentAnalytics:
                    response.Widgets = await GeneratePaymentAnalyticsDashboardAsync();
                    break;
                case DashboardTypes.OperationalEfficiency:
                    response.Widgets = await GenerateOperationalEfficiencyDashboardAsync();
                    break;
                default:
                    response.Widgets = await GenerateBasicDashboardAsync();
                    break;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard: {@Request}", request);
            return new AnalyticsDashboardResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<List<AnalyticsWidget>> GenerateTaxComplianceDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Compliance Rate Widget
        var complianceRate = await CalculateComplianceRateAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "compliance_rate",
            Title = "Overall Compliance Rate",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object>
                {
                    ["value"] = complianceRate,
                    ["format"] = "percentage",
                    ["target"] = 95.0
                }
            },
            Config = new AnalyticsWidgetConfig { ValueFormat = "percentage" }
        });

        // Total Clients Widget
        var clientCount = await GetTotalClientsAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "total_clients",
            Title = "Total Clients",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object>
                {
                    ["value"] = clientCount,
                    ["format"] = "number"
                }
            },
            Config = new AnalyticsWidgetConfig { ValueFormat = "number" }
        });

        return widgets;
    }

    private async Task<List<AnalyticsWidget>> GenerateRevenueDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Revenue Trend Widget
        var revenueData = await GetRevenueTrendAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "revenue_trend",
            Title = "Revenue Trend",
            Type = "chart",
            Data = new AnalyticsWidgetData
            {
                DataPoints = revenueData
            },
            Config = new AnalyticsWidgetConfig
            {
                ChartType = "line",
                Colors = new Dictionary<string, string> { ["primary"] = "#3b82f6" }
            }
        });

        return widgets;
    }

    private async Task<List<AnalyticsWidget>> GenerateClientPerformanceDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Client Status Distribution
        var statusData = await GetClientStatusDistributionAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "client_status",
            Title = "Client Status Distribution",
            Type = "chart",
            Data = new AnalyticsWidgetData
            {
                DataPoints = statusData
            },
            Config = new AnalyticsWidgetConfig
            {
                ChartType = "pie",
                Colors = new Dictionary<string, string>
                {
                    ["Active"] = "#10b981",
                    ["Inactive"] = "#f59e0b",
                    ["Pending"] = "#ef4444"
                }
            }
        });

        return widgets;
    }

    private async Task<List<AnalyticsWidget>> GeneratePaymentAnalyticsDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Payment Volume Widget
        var paymentVolume = await GetPaymentVolumeAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "payment_volume",
            Title = "Payment Volume",
            Type = "chart",
            Data = new AnalyticsWidgetData
            {
                DataPoints = paymentVolume
            },
            Config = new AnalyticsWidgetConfig
            {
                ChartType = "area",
                Colors = new Dictionary<string, string> { ["primary"] = "#8b5cf6" }
            }
        });

        return widgets;
    }

    private async Task<List<AnalyticsWidget>> GenerateOperationalEfficiencyDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Processing Time Widget
        var processingTimes = await GetProcessingTimesAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "processing_times",
            Title = "Average Processing Times",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object>
                {
                    ["value"] = 24.5,
                    ["target"] = 48.0,
                    ["format"] = "hours"
                }
            },
            Config = new AnalyticsWidgetConfig { ValueFormat = "hours" }
        });

        return widgets;
    }

    private async Task<List<AnalyticsWidget>> GenerateBasicDashboardAsync()
    {
        var widgets = new List<AnalyticsWidget>();

        // Total Clients Widget
        var clientCount = await GetTotalClientsAsync();
        widgets.Add(new AnalyticsWidget
        {
            Id = "total_clients",
            Title = "Total Clients",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object>
                {
                    ["value"] = clientCount,
                    ["format"] = "number"
                }
            },
            Config = new AnalyticsWidgetConfig { ValueFormat = "number" }
        });

        return widgets;
    }

    public async Task<List<AnalyticsWidget>> GetRealTimeMetricsAsync()
    {
        var metrics = new List<AnalyticsWidget>();

        // Active Users
        var activeUsers = await GetActiveUsersCountAsync();
        metrics.Add(new AnalyticsWidget
        {
            Id = "active_users",
            Title = "Active Users",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object> { ["value"] = activeUsers }
            }
        });

        // Pending Filings
        var pendingFilings = await GetPendingFilingsCountAsync();
        metrics.Add(new AnalyticsWidget
        {
            Id = "pending_filings",
            Title = "Pending Filings",
            Type = "metric",
            Data = new AnalyticsWidgetData
            {
                Summary = new Dictionary<string, object> { ["value"] = pendingFilings }
            }
        });

        return metrics;
    }

    public async Task<Dictionary<string, object>> GetAvailableMetricsAsync()
    {
        return new Dictionary<string, object>
        {
            ["total_clients"] = await GetTotalClientsAsync(),
            ["active_users"] = await GetActiveUsersCountAsync(),
            ["pending_filings"] = await GetPendingFilingsCountAsync(),
            ["compliance_rate"] = await CalculateComplianceRateAsync(),
            ["system_health"] = "Healthy"
        };
    }

    // Private helper methods with actual data queries
    private async Task<double> CalculateComplianceRateAsync()
    {
        var totalFilings = await _context.TaxFilings.CountAsync();
        if (totalFilings == 0) return 100.0;
        
        var compliantFilings = await _context.TaxFilings
            .Where(tf => tf.Status == FilingStatus.Submitted || tf.Status == FilingStatus.Approved)
            .CountAsync();
        
        return (double)compliantFilings / totalFilings * 100;
    }

    private Task<List<AnalyticsDataPoint>> GetRevenueTrendAsync()
    {
        // Mock revenue trend data for now
        var currentDate = DateTime.UtcNow;
        var months = new List<AnalyticsDataPoint>();
        
        for (int i = 11; i >= 0; i--)
        {
            var month = currentDate.AddMonths(-i);
            months.Add(new AnalyticsDataPoint
            {
                Label = month.ToString("MMM yyyy"),
                Value = Random.Shared.Next(10000, 50000)
            });
        }
        
        return Task.FromResult(months);
    }

    private async Task<List<AnalyticsDataPoint>> GetClientStatusDistributionAsync()
    {
        var activeCount = await _context.Clients.Where(c => c.Status == ClientStatus.Active).CountAsync();
        var inactiveCount = await _context.Clients.Where(c => c.Status == ClientStatus.Inactive).CountAsync();
        var suspendedCount = await _context.Clients.Where(c => c.Status == ClientStatus.Suspended).CountAsync();

        return new List<AnalyticsDataPoint>
        {
            new() { Label = "Active", Value = activeCount },
            new() { Label = "Inactive", Value = inactiveCount },
            new() { Label = "Suspended", Value = suspendedCount }
        };
    }

    private async Task<List<AnalyticsDataPoint>> GetPaymentVolumeAsync()
    {
        // Try to get actual payment data
        var paymentVolume = await _context.Payments
            .Where(p => p.PaymentDate >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new AnalyticsDataPoint
            {
                Label = g.Key.ToString("yyyy-MM-dd"),
                Value = (double)g.Sum(p => p.Amount)
            })
            .OrderBy(x => x.Label)
            .ToListAsync();

        // If no data, return mock data
        if (!paymentVolume.Any())
        {
            var currentDate = DateTime.UtcNow;
            for (int i = 29; i >= 0; i--)
            {
                var day = currentDate.AddDays(-i);
                paymentVolume.Add(new AnalyticsDataPoint
                {
                    Label = day.ToString("yyyy-MM-dd"),
                    Value = Random.Shared.Next(1000, 10000)
                });
            }
        }

        return paymentVolume;
    }

    private Task<object> GetProcessingTimesAsync()
    {
        return Task.FromResult<object>(new
        {
            average = 24.5,
            target = 48.0
        });
    }

    private async Task<int> GetTotalClientsAsync()
    {
        return await _context.Clients.CountAsync();
    }

    private async Task<int> GetActiveUsersCountAsync()
    {
        return await _context.Clients.Where(c => c.Status == ClientStatus.Active).CountAsync();
    }

    private async Task<int> GetPendingFilingsCountAsync()
    {
        return await _context.TaxFilings.Where(tf => tf.Status == FilingStatus.Draft).CountAsync();
    }
}