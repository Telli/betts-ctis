using Microsoft.Extensions.Logging;
using Quartz;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class KpiSnapshotJob : IJob
{
    private readonly ILogger<KpiSnapshotJob> _logger;
    private readonly IKpiComputationService _kpiComputationService;
    private readonly IKpiAlertService _kpiAlertService;

    public KpiSnapshotJob(
        ILogger<KpiSnapshotJob> logger,
        IKpiComputationService kpiComputationService,
        IKpiAlertService kpiAlertService)
    {
        _logger = logger;
        _kpiComputationService = kpiComputationService;
        _kpiAlertService = kpiAlertService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting KPI snapshot job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Create daily KPI snapshot
            var snapshot = await _kpiComputationService.CreateDailySnapshotAsync("System");
            
            // Generate and process alerts
            var alerts = await _kpiComputationService.GenerateAlertsAsync(snapshot);
            if (alerts.Any())
            {
                await _kpiAlertService.ProcessAlertsAsync(alerts);
            }
            
            _logger.LogInformation("KPI snapshot job completed successfully at {Timestamp}. Generated {AlertCount} alerts.", 
                DateTime.UtcNow, alerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI snapshot job");
            throw;
        }
    }
}
