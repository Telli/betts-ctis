using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BettsTax.Core.BackgroundJobs
{
    /// <summary>
    /// Background job for monitoring compliance deadlines
    /// Runs daily to check for upcoming deadlines and send alerts
    /// </summary>
    public class ComplianceDeadlineMonitoringJob
    {
        private readonly IComplianceMonitoringWorkflow _complianceMonitoringWorkflow;
        private readonly ILogger<ComplianceDeadlineMonitoringJob> _logger;

        public ComplianceDeadlineMonitoringJob(
            IComplianceMonitoringWorkflow complianceMonitoringWorkflow,
            ILogger<ComplianceDeadlineMonitoringJob> logger)
        {
            _complianceMonitoringWorkflow = complianceMonitoringWorkflow;
            _logger = logger;
        }

        /// <summary>
        /// Execute the compliance deadline monitoring job
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting Compliance Deadline Monitoring Job at {Timestamp}", DateTime.UtcNow);

                var result = await _complianceMonitoringWorkflow.MonitorDeadlinesAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Compliance Deadline Monitoring Job completed successfully at {Timestamp}", DateTime.UtcNow);
                }
                else
                {
                    _logger.LogWarning("Compliance Deadline Monitoring Job completed with errors: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Compliance Deadline Monitoring Job");
                throw;
            }
        }
    }
}

