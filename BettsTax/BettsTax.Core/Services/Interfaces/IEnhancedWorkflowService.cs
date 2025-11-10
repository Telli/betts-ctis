using BettsTax.Shared;
using BettsTax.Core.DTOs.Workflows;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Phase 3 Enhanced Workflow Automation Service - Advanced workflow instance management and approval chains
/// </summary>
public interface IEnhancedWorkflowService
{
    // Workflow Definitions
    Task<Result<List<WorkflowDefinitionDto>>> GetWorkflowDefinitionsAsync(bool includeInactive = false);

    // Enhanced Workflow Instance Management
    Task<Result<WorkflowInstanceDto>> StartWorkflowInstanceAsync(Guid workflowId, Dictionary<string, object> variables, string userId);
    Task<Result<WorkflowInstanceDto>> GetWorkflowInstanceAsync(Guid instanceId);
    Task<Result<List<WorkflowInstanceDto>>> GetWorkflowInstancesAsync(Guid? workflowId = null, string? status = null);
    Task<Result> CancelWorkflowInstanceAsync(Guid instanceId, string userId, string reason);
    
    // Advanced Step Management
    Task<Result<WorkflowStepResult>> ExecuteStepAsync(Guid instanceId, Guid stepId, Dictionary<string, object> inputs, string userId);
    Task<Result<WorkflowStepResult>> CompleteStepAsync(Guid instanceId, Guid stepId, Dictionary<string, object> outputs, string userId);
    Task<Result> AssignStepAsync(Guid instanceId, Guid stepId, string assigneeId, string userId);

    // Approval Workflow Management
    Task<Result<ApprovalRequest>> RequestApprovalAsync(Guid instanceId, Guid stepId, string approverId, string? comments = null);
    Task<Result<WorkflowStepResult>> ApproveStepAsync(Guid approvalId, string approverId, string? comments = null);
    Task<Result<WorkflowStepResult>> RejectStepAsync(Guid approvalId, string approverId, string comments);
    Task<Result<List<ApprovalRequest>>> GetPendingApprovalsAsync(string approverId);

    // Workflow Analytics and Monitoring
    Task<Result<WorkflowAnalyticsDto>> GetWorkflowAnalyticsAsync(Guid workflowId, DateTime? from = null, DateTime? to = null);
    Task<Result<List<WorkflowMetricsDto>>> GetWorkflowMetricsAsync();

    // Trigger Management
    Task<Result<WorkflowTriggerDto>> CreateTriggerAsync(Guid workflowId, CreateTriggerRequest request, string userId);
    Task<Result<List<WorkflowTriggerDto>>> GetTriggersAsync(Guid workflowId);
    Task<Result> DeleteTriggerAsync(Guid triggerId, string userId);
    Task<Result<List<Guid>>> EvaluateTriggersAsync(string eventType, Dictionary<string, object> eventData);
}