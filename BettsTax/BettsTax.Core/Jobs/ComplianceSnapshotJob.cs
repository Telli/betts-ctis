using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class ComplianceSnapshotJob : IJob
{
    private readonly ILogger<ComplianceSnapshotJob> _logger;
    private readonly IComplianceService _complianceService;

    public ComplianceSnapshotJob(
        ILogger<ComplianceSnapshotJob> logger,
        IComplianceService complianceService)
    {
        _logger = logger;
        _complianceService = complianceService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting compliance snapshot job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Create daily compliance snapshots for all clients
            await _complianceService.CreateComplianceSnapshotAsync();
            
            _logger.LogInformation("Compliance snapshot job completed successfully at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing compliance snapshot job");
            throw;
        }
    }
}
