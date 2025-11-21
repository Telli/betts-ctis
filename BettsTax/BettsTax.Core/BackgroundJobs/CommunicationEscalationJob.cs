using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BettsTax.Core.BackgroundJobs
{
    /// <summary>
    /// Background job for checking and applying communication escalation rules
    /// Runs hourly to check if messages need to be escalated based on time thresholds
    /// </summary>
    public class CommunicationEscalationJob
    {
        private readonly ICommunicationRoutingWorkflow _communicationRoutingWorkflow;
        private readonly ILogger<CommunicationEscalationJob> _logger;

        public CommunicationEscalationJob(
            ICommunicationRoutingWorkflow communicationRoutingWorkflow,
            ILogger<CommunicationEscalationJob> logger)
        {
            _communicationRoutingWorkflow = communicationRoutingWorkflow;
            _logger = logger;
        }

        /// <summary>
        /// Execute the communication escalation job
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting Communication Escalation Job at {Timestamp}", DateTime.UtcNow);

                var result = await _communicationRoutingWorkflow.CheckAndApplyEscalationRulesAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Communication Escalation Job completed successfully at {Timestamp}", DateTime.UtcNow);
                }
                else
                {
                    _logger.LogWarning("Communication Escalation Job completed with errors: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Communication Escalation Job");
                throw;
            }
        }
    }
}

