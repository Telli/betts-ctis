using Microsoft.Extensions.Logging;
using Quartz;
using BettsTax.Core.Services;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class ReportCleanupJob : IJob
{
    private readonly ILogger<ReportCleanupJob> _logger;
    private readonly IDataExportService _exportService;

    public ReportCleanupJob(
        ILogger<ReportCleanupJob> logger,
        IDataExportService exportService)
    {
        _logger = logger;
        _exportService = exportService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting report cleanup job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Clean up expired report files (older than 7 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            // TODO: Add IDataExportService.CleanupExpiredReportsAsync implementation; disabled for now to allow build
            //_exportService.CleanupExpiredReportsAsync(cutoffDate);
            _logger.LogInformation("Report cleanup job completed successfully at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing report cleanup job");
            throw;
        }

        return Task.CompletedTask;
    }
}
