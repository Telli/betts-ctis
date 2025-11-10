using System;
using System.Collections.Generic;
using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BettsTax.Core.Jobs;

public class WorkflowTriggerEvaluationJob : IJob
{
    private readonly IEnhancedWorkflowService _workflowService;
    private readonly ILogger<WorkflowTriggerEvaluationJob> _logger;

    public WorkflowTriggerEvaluationJob(IEnhancedWorkflowService workflowService, ILogger<WorkflowTriggerEvaluationJob> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var eventData = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["jobInstanceId"] = context.FireInstanceId,
                ["triggerKey"] = context.Trigger.Key.ToString()
            };

            var result = await _workflowService.EvaluateTriggersAsync("Scheduled", eventData);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Workflow trigger evaluation failed: {Error}", result.ErrorMessage);
                return;
            }

            var triggeredCount = result.Value?.Count ?? 0;
            if (triggeredCount == 0)
            {
                _logger.LogInformation("Workflow trigger evaluation completed with no workflows triggered");
            }
            else
            {
                _logger.LogInformation("Workflow trigger evaluation triggered {Count} workflow(s)", triggeredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while evaluating workflow triggers");
        }
    }
}
