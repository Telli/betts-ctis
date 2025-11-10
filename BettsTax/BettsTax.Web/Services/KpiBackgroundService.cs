using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BettsTax.Web.Services;

/// <summary>
/// Periodically recomputes KPI metrics every 5 minutes.
/// </summary>
public class KpiBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KpiBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public KpiBackgroundService(IServiceScopeFactory scopeFactory, ILogger<KpiBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KPI background service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var kpiService = scope.ServiceProvider.GetRequiredService<IKpiComputationService>();
                    await kpiService.ComputeAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing KPI metrics");
            }
            await Task.Delay(Interval, stoppingToken);
        }
        _logger.LogInformation("KPI background service stopping");
    }
}
