using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Workflows;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services;

/// <summary>
/// Phase 3: Enhanced Workflow Service - Advanced workflow instance management and approval chains
/// Building on the existing workflow infrastructure to add advanced automation features
/// </summary>
public class EnhancedWorkflowService : IEnhancedWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EnhancedWorkflowService> _logger;
    private readonly INotificationService _notificationService;

    public EnhancedWorkflowService(
        ApplicationDbContext context,
        ILogger<EnhancedWorkflowService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    #region Workflow Definitions

    public async Task<Result<List<WorkflowDefinitionDto>>> GetWorkflowDefinitionsAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.Workflows.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(w => w.IsActive);
            }

            var workflows = await query
                .OrderBy(w => w.Priority)
                .ThenBy(w => w.Name)
                .ToListAsync();

            var definitions = workflows.Select(w => new WorkflowDefinitionDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Type = w.Type,
                TriggerType = w.Trigger,
                IsActive = w.IsActive,
                Priority = w.Priority
            }).ToList();

            return Result.Success(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow definitions");
            return Result.Failure<List<WorkflowDefinitionDto>>($"Error retrieving workflow definitions: {ex.Message}");
        }
    }

    #endregion

    #region Workflow Instance Management

    public async Task<Result<WorkflowInstanceDto>> StartWorkflowInstanceAsync(Guid workflowId, Dictionary<string, object> variables, string userId)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
                return Result.Failure<WorkflowInstanceDto>("Workflow not found");

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                Name = $"{workflow.Name} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                Status = WorkflowInstanceStatus.Running,
                Variables = JsonSerializer.Serialize(variables),
                CreatedBy = userId,
                StartedAt = DateTime.UtcNow
            };

            _context.WorkflowInstances.Add(instance);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started workflow instance {InstanceId} for workflow {WorkflowId} by user {UserId}", 
                instance.Id, workflowId, userId);

            var dto = MapToDto(instance);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow instance for workflow {WorkflowId}", workflowId);
            return Result.Failure<WorkflowInstanceDto>($"Error starting workflow: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowInstanceDto>> GetWorkflowInstanceAsync(Guid instanceId)
    {
        try
        {
            var instance = await _context.WorkflowInstances
                .Include(i => i.StepInstances)
                .Include(i => i.Approvals)
                .FirstOrDefaultAsync(i => i.Id == instanceId);

            if (instance == null)
                return Result.Failure<WorkflowInstanceDto>("Workflow instance not found");

            var dto = MapToDto(instance);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow instance {InstanceId}", instanceId);
            return Result.Failure<WorkflowInstanceDto>($"Error retrieving workflow instance: {ex.Message}");
        }
    }

    public async Task<Result<List<WorkflowInstanceDto>>> GetWorkflowInstancesAsync(Guid? workflowId = null, string? status = null)
    {
        try
        {
            var query = _context.WorkflowInstances.AsQueryable();

            if (workflowId.HasValue)
                query = query.Where(i => i.WorkflowId == workflowId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkflowInstanceStatus>(status, out var statusEnum))
                query = query.Where(i => i.Status == statusEnum);

            var instances = await query
                .Include(i => i.StepInstances)
                .Include(i => i.Approvals)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var dtos = instances.Select(MapToDto).ToList();
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow instances");
            return Result.Failure<List<WorkflowInstanceDto>>($"Error retrieving workflow instances: {ex.Message}");
        }
    }

    public async Task<Result> CancelWorkflowInstanceAsync(Guid instanceId, string userId, string reason)
    {
        try
        {
            var instance = await _context.WorkflowInstances.FindAsync(instanceId);
            if (instance == null)
                return Result.Failure("Workflow instance not found");

            instance.Status = WorkflowInstanceStatus.Cancelled;
            instance.CompletedAt = DateTime.UtcNow;
            instance.CompletedBy = userId;
            instance.ErrorMessage = reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled workflow instance {InstanceId} by user {UserId}. Reason: {Reason}", 
                instanceId, userId, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow instance {InstanceId}", instanceId);
            return Result.Failure($"Error cancelling workflow instance: {ex.Message}");
        }
    }

    #endregion

    #region Step Management

    public async Task<Result<WorkflowStepResult>> ExecuteStepAsync(Guid instanceId, Guid stepId, Dictionary<string, object> inputs, string userId)
    {
        try
        {
            var instance = await _context.WorkflowInstances.FindAsync(instanceId);
            if (instance == null)
                return Result.Failure<WorkflowStepResult>("Workflow instance not found");

            var stepInstance = new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instanceId,
                WorkflowStepId = stepId,
                Status = WorkflowStepInstanceStatus.Running,
                Input = JsonSerializer.Serialize(inputs),
                StartedAt = DateTime.UtcNow
            };

            _context.WorkflowStepInstances.Add(stepInstance);
            await _context.SaveChangesAsync();

            var result = new WorkflowStepResult
            {
                StepInstanceId = stepInstance.Id,
                IsCompleted = false,
                RequiresApproval = false,
                Output = new Dictionary<string, object>()
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step {StepId} for instance {InstanceId}", stepId, instanceId);
            return Result.Failure<WorkflowStepResult>($"Error executing step: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStepResult>> CompleteStepAsync(Guid instanceId, Guid stepId, Dictionary<string, object> outputs, string userId)
    {
        try
        {
            var stepInstance = await _context.WorkflowStepInstances
                .FirstOrDefaultAsync(s => s.WorkflowInstanceId == instanceId && s.WorkflowStepId == stepId);

            if (stepInstance == null)
                return Result.Failure<WorkflowStepResult>("Step instance not found");

            stepInstance.Status = WorkflowStepInstanceStatus.Completed;
            stepInstance.Output = JsonSerializer.Serialize(outputs);
            stepInstance.CompletedAt = DateTime.UtcNow;
            stepInstance.CompletedBy = userId;

            await _context.SaveChangesAsync();

            var result = new WorkflowStepResult
            {
                StepInstanceId = stepInstance.Id,
                IsCompleted = true,
                RequiresApproval = false,
                Output = outputs
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing step {StepId} for instance {InstanceId}", stepId, instanceId);
            return Result.Failure<WorkflowStepResult>($"Error completing step: {ex.Message}");
        }
    }

    public async Task<Result> AssignStepAsync(Guid instanceId, Guid stepId, string assigneeId, string userId)
    {
        try
        {
            var stepInstance = await _context.WorkflowStepInstances
                .FirstOrDefaultAsync(s => s.WorkflowInstanceId == instanceId && s.WorkflowStepId == stepId);

            if (stepInstance == null)
                return Result.Failure("Step instance not found");

            stepInstance.AssignedTo = assigneeId;
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning step {StepId} to user {AssigneeId}", stepId, assigneeId);
            return Result.Failure($"Error assigning step: {ex.Message}");
        }
    }

    #endregion

    #region Approval Management

    public async Task<Result<ApprovalRequest>> RequestApprovalAsync(Guid instanceId, Guid stepId, string approverId, string? comments = null)
    {
        try
        {
            var stepInstance = await _context.WorkflowStepInstances
                .FirstOrDefaultAsync(s => s.WorkflowInstanceId == instanceId && s.WorkflowStepId == stepId);

            if (stepInstance == null)
                return Result.Failure<ApprovalRequest>("Step instance not found");

            var approval = new WorkflowApproval
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instanceId,
                WorkflowStepInstanceId = stepInstance.Id,
                RequiredApprover = approverId,
                Status = WorkflowApprovalStatus.Pending,
                Comments = comments,
                RequestedAt = DateTime.UtcNow
            };

            _context.WorkflowApprovals.Add(approval);
            
            // Update step instance status
            stepInstance.Status = WorkflowStepInstanceStatus.WaitingForApproval;
            
            await _context.SaveChangesAsync();

            var dto = new ApprovalRequest
            {
                Id = approval.Id,
                WorkflowInstanceId = instanceId,
                WorkflowStepInstanceId = stepInstance.Id,
                RequiredApprover = approverId,
                Status = WorkflowApprovalStatus.Pending,
                Comments = comments,
                RequestedAt = approval.RequestedAt
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting approval for step {StepId}", stepId);
            return Result.Failure<ApprovalRequest>($"Error requesting approval: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStepResult>> ApproveStepAsync(Guid approvalId, string approverId, string? comments = null)
    {
        try
        {
            var approval = await _context.WorkflowApprovals
                .Include(a => a.WorkflowStepInstance)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null)
                return Result.Failure<WorkflowStepResult>("Approval request not found");

            approval.Status = WorkflowApprovalStatus.Approved;
            approval.RespondedAt = DateTime.UtcNow;
            approval.RespondedBy = approverId;
            approval.Comments = comments;

            // Update step instance
            approval.WorkflowStepInstance.Status = WorkflowStepInstanceStatus.Completed;
            approval.WorkflowStepInstance.CompletedAt = DateTime.UtcNow;
            approval.WorkflowStepInstance.CompletedBy = approverId;

            await _context.SaveChangesAsync();

            var result = new WorkflowStepResult
            {
                StepInstanceId = approval.WorkflowStepInstance.Id,
                IsCompleted = true,
                RequiresApproval = false,
                Output = new Dictionary<string, object>()
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving step for approval {ApprovalId}", approvalId);
            return Result.Failure<WorkflowStepResult>($"Error approving step: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStepResult>> RejectStepAsync(Guid approvalId, string approverId, string comments)
    {
        try
        {
            var approval = await _context.WorkflowApprovals
                .Include(a => a.WorkflowStepInstance)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null)
                return Result.Failure<WorkflowStepResult>("Approval request not found");

            approval.Status = WorkflowApprovalStatus.Rejected;
            approval.RespondedAt = DateTime.UtcNow;
            approval.RespondedBy = approverId;
            approval.Comments = comments;

            // Update step instance
            approval.WorkflowStepInstance.Status = WorkflowStepInstanceStatus.Failed;
            approval.WorkflowStepInstance.ErrorMessage = comments;

            await _context.SaveChangesAsync();

            var result = new WorkflowStepResult
            {
                StepInstanceId = approval.WorkflowStepInstance.Id,
                IsCompleted = false,
                RequiresApproval = false,
                Output = new Dictionary<string, object>(),
                ErrorMessage = comments
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting step for approval {ApprovalId}", approvalId);
            return Result.Failure<WorkflowStepResult>($"Error rejecting step: {ex.Message}");
        }
    }

    public async Task<Result<List<ApprovalRequest>>> GetPendingApprovalsAsync(string approverId)
    {
        try
        {
            var approvals = await _context.WorkflowApprovals
                .Where(a => a.RequiredApprover == approverId && a.Status == WorkflowApprovalStatus.Pending)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            var dtos = approvals.Select(a => new ApprovalRequest
            {
                Id = a.Id,
                WorkflowInstanceId = a.WorkflowInstanceId,
                WorkflowStepInstanceId = a.WorkflowStepInstanceId,
                RequiredApprover = a.RequiredApprover,
                Status = a.Status,
                Comments = a.Comments,
                RequestedAt = a.RequestedAt,
                RespondedAt = a.RespondedAt,
                RespondedBy = a.RespondedBy
            }).ToList();

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals for user {ApproverId}", approverId);
            return Result.Failure<List<ApprovalRequest>>($"Error getting pending approvals: {ex.Message}");
        }
    }

    #endregion

    #region Analytics and Monitoring

    public async Task<Result<WorkflowAnalyticsDto>> GetWorkflowAnalyticsAsync(Guid workflowId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
                return Result.Failure<WorkflowAnalyticsDto>("Workflow not found");

            var query = _context.WorkflowInstances.Where(i => i.WorkflowId == workflowId);

            if (from.HasValue)
                query = query.Where(i => i.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(i => i.CreatedAt <= to.Value);

            var instances = await query.ToListAsync();

            var analytics = new WorkflowAnalyticsDto
            {
                WorkflowId = workflowId,
                WorkflowName = workflow.Name,
                TotalExecutions = instances.Count,
                CompletedExecutions = instances.Count(i => i.Status == WorkflowInstanceStatus.Completed),
                FailedExecutions = instances.Count(i => i.Status == WorkflowInstanceStatus.Failed),
                LastExecution = instances.OrderByDescending(i => i.CreatedAt).FirstOrDefault()?.CreatedAt
            };

            analytics.SuccessRate = analytics.TotalExecutions > 0 
                ? (double)analytics.CompletedExecutions / analytics.TotalExecutions * 100 
                : 0;

            return Result.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow analytics for workflow {WorkflowId}", workflowId);
            return Result.Failure<WorkflowAnalyticsDto>($"Error getting workflow analytics: {ex.Message}");
        }
    }

    public async Task<Result<List<WorkflowMetricsDto>>> GetWorkflowMetricsAsync()
    {
        try
        {
            var totalWorkflows = await _context.Workflows.CountAsync();
            var activeWorkflows = await _context.Workflows.CountAsync(w => w.IsActive);
            var totalInstances = await _context.WorkflowInstances.CountAsync();
            var runningInstances = await _context.WorkflowInstances.CountAsync(i => i.Status == WorkflowInstanceStatus.Running);
            var pendingApprovals = await _context.WorkflowApprovals.CountAsync(a => a.Status == WorkflowApprovalStatus.Pending);

            var completedInstances = await _context.WorkflowInstances.CountAsync(i => i.Status == WorkflowInstanceStatus.Completed);
            var overallSuccessRate = totalInstances > 0 ? (double)completedInstances / totalInstances * 100 : 0;

            var metrics = new List<WorkflowMetricsDto>
            {
                new()
                {
                    TotalWorkflows = totalWorkflows,
                    ActiveWorkflows = activeWorkflows,
                    TotalInstances = totalInstances,
                    RunningInstances = runningInstances,
                    PendingApprovals = pendingApprovals,
                    OverallSuccessRate = overallSuccessRate,
                    LastUpdated = DateTime.UtcNow
                }
            };

            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow metrics");
            return Result.Failure<List<WorkflowMetricsDto>>($"Error getting workflow metrics: {ex.Message}");
        }
    }

    #endregion

    #region Trigger Management

    public async Task<Result<WorkflowTriggerDto>> CreateTriggerAsync(Guid workflowId, CreateTriggerRequest request, string userId)
    {
        try
        {
            var workflow = await _context.Workflows.FindAsync(workflowId);
            if (workflow == null)
                return Result.Failure<WorkflowTriggerDto>("Workflow not found");

            var trigger = new BettsTax.Data.WorkflowTrigger
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                Name = request.Name,
                Type = request.Type,
                Configuration = JsonSerializer.Serialize(request.Configuration),
                CreatedBy = userId
            };

            _context.WorkflowTriggers.Add(trigger);
            await _context.SaveChangesAsync();

            var dto = new WorkflowTriggerDto
            {
                Id = trigger.Id,
                WorkflowId = workflowId,
                Name = trigger.Name,
                Type = trigger.Type,
                Configuration = request.Configuration,
                IsActive = trigger.IsActive,
                CreatedBy = trigger.CreatedBy,
                CreatedAt = trigger.CreatedAt
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trigger for workflow {WorkflowId}", workflowId);
            return Result.Failure<WorkflowTriggerDto>($"Error creating trigger: {ex.Message}");
        }
    }

    public async Task<Result<List<WorkflowTriggerDto>>> GetTriggersAsync(Guid workflowId)
    {
        try
        {
            var triggers = await _context.WorkflowTriggers
                .Where(t => t.WorkflowId == workflowId)
                .ToListAsync();

            var dtos = triggers.Select(t => new WorkflowTriggerDto
            {
                Id = t.Id,
                WorkflowId = t.WorkflowId,
                Name = t.Name,
                Type = t.Type,
                Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(t.Configuration) ?? new(),
                IsActive = t.IsActive,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting triggers for workflow {WorkflowId}", workflowId);
            return Result.Failure<List<WorkflowTriggerDto>>($"Error getting triggers: {ex.Message}");
        }
    }

    public async Task<Result> DeleteTriggerAsync(Guid triggerId, string userId)
    {
        try
        {
            var trigger = await _context.WorkflowTriggers.FindAsync(triggerId);
            if (trigger == null)
                return Result.Failure("Trigger not found");

            _context.WorkflowTriggers.Remove(trigger);
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trigger {TriggerId}", triggerId);
            return Result.Failure($"Error deleting trigger: {ex.Message}");
        }
    }

    public async Task<Result<List<Guid>>> EvaluateTriggersAsync(string eventType, Dictionary<string, object> eventData)
    {
        try
        {
            var triggers = await _context.WorkflowTriggers
                .Where(t => t.IsActive && t.Type == WorkflowTriggerType.Event)
                .ToListAsync();

            var triggeredWorkflows = new List<Guid>();

            foreach (var trigger in triggers)
            {
                // Simple trigger evaluation - in a real implementation, you'd have more sophisticated logic
                triggeredWorkflows.Add(trigger.WorkflowId);
            }

            return Result.Success(triggeredWorkflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating triggers for event type {EventType}", eventType);
            return Result.Failure<List<Guid>>($"Error evaluating triggers: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private WorkflowInstanceDto MapToDto(WorkflowInstance instance)
    {
        return new WorkflowInstanceDto
        {
            Id = instance.Id,
            WorkflowId = instance.WorkflowId,
            Name = instance.Name,
            Description = instance.Description,
            Status = instance.Status,
            Variables = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Variables) ?? new(),
            Context = JsonSerializer.Deserialize<Dictionary<string, object>>(instance.Context) ?? new(),
            CreatedBy = instance.CreatedBy,
            CreatedAt = instance.CreatedAt,
            StartedAt = instance.StartedAt,
            CompletedAt = instance.CompletedAt,
            CompletedBy = instance.CompletedBy,
            ErrorMessage = instance.ErrorMessage,
            StepInstances = instance.StepInstances?.Select(s => new WorkflowStepInstanceDto
            {
                Id = s.Id,
                WorkflowInstanceId = s.WorkflowInstanceId,
                WorkflowStepId = s.WorkflowStepId,
                Status = s.Status,
                Input = JsonSerializer.Deserialize<Dictionary<string, object>>(s.Input) ?? new(),
                Output = JsonSerializer.Deserialize<Dictionary<string, object>>(s.Output) ?? new(),
                AssignedTo = s.AssignedTo,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                CompletedBy = s.CompletedBy,
                ErrorMessage = s.ErrorMessage,
                RetryCount = s.RetryCount
            }).ToList() ?? new(),
            Approvals = instance.Approvals?.Select(a => new ApprovalRequest
            {
                Id = a.Id,
                WorkflowInstanceId = a.WorkflowInstanceId,
                WorkflowStepInstanceId = a.WorkflowStepInstanceId,
                RequiredApprover = a.RequiredApprover,
                Status = a.Status,
                Comments = a.Comments,
                RequestedAt = a.RequestedAt,
                RespondedAt = a.RespondedAt,
                RespondedBy = a.RespondedBy
            }).ToList() ?? new()
        };
    }

    #endregion
}
