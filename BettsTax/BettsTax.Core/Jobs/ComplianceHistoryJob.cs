using Microsoft.Extensions.Logging;
using Quartz;
using BettsTax.Core.Services;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class ComplianceHistoryJob : IJob
{
    private readonly ILogger<ComplianceHistoryJob> _logger;
    private readonly IComplianceTrackerService _complianceService;
    private readonly IPenaltyCalculationService _penaltyService;

    public ComplianceHistoryJob(
        ILogger<ComplianceHistoryJob> logger,
        IComplianceTrackerService complianceService,
        IPenaltyCalculationService penaltyService)
    {
        _logger = logger;
        _complianceService = complianceService;
        _penaltyService = penaltyService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting compliance history job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Update compliance history for all clients
            await _complianceService.UpdateComplianceHistoryAsync();
            
            // Recalculate penalties for overdue items
            await _penaltyService.RecalculatePenaltiesAsync();
            
            _logger.LogInformation("Compliance history job completed successfully at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing compliance history job");
            throw;
        }
    }
}
