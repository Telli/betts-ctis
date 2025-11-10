using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BettsTax.Core.DTOs.Workflows;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BettsTax.Core.Jobs;

public class WorkflowInstanceCleanupJob : IJob
{
    private readonly IEnhancedWorkflowService _workflowService;
    private readonly ILogger<WorkflowInstanceCleanupJob> _logger;

    private const string SystemUserId = "SYSTEM_WORKFLOW_JOB";

    public WorkflowInstanceCleanupJob(IEnhancedWorkflowService workflowService, ILogger<WorkflowInstanceCleanupJob> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var runningResult = await _workflowService.GetWorkflowInstancesAsync(status: WorkflowInstanceStatus.Running.ToString());
            if (!runningResult.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve running workflow instances for cleanup: {Error}", runningResult.ErrorMessage);
                return;
            }

            var waitingResult = await _workflowService.GetWorkflowInstancesAsync(status: WorkflowInstanceStatus.WaitingForApproval.ToString());
            if (!waitingResult.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve waiting-for-approval workflow instances for cleanup: {Error}", waitingResult.ErrorMessage);
            }

            var threshold = DateTime.UtcNow.AddHours(-24);
            var runningInstances = runningResult.Value ?? new List<WorkflowInstanceDto>();
            var waitingInstances = waitingResult.IsSuccess ? waitingResult.Value ?? new List<WorkflowInstanceDto>() : new List<WorkflowInstanceDto>();

            foreach (var instance in runningInstances.Where(i => i.StartedAt.HasValue && i.StartedAt.Value < threshold))
            {
                var cancelResult = await _workflowService.CancelWorkflowInstanceAsync(
                    instance.Id,
                    SystemUserId,
                    "Automatically cancelled after exceeding 24 hours runtime.");

                if (cancelResult.IsSuccess)
                {
                    _logger.LogInformation("Automatically cancelled workflow instance {InstanceId} after exceeding runtime threshold", instance.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel workflow instance {InstanceId}: {Error}", instance.Id, cancelResult.ErrorMessage);
                }
            }

            var approvalThreshold = DateTime.UtcNow.AddHours(-12);
            foreach (var instance in waitingInstances)
            {
                var pendingApprovals = instance.Approvals
                    .Where(a => a.Status == WorkflowApprovalStatus.Pending && a.RequestedAt < approvalThreshold)
                    .ToList();

                if (!pendingApprovals.Any())
                {
                    continue;
                }

                _logger.LogInformation(
                    "Workflow instance {InstanceId} has {Count} approval(s) pending for over 12 hours",
                    instance.Id,
                    pendingApprovals.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while running workflow cleanup job");
        }
    }
}