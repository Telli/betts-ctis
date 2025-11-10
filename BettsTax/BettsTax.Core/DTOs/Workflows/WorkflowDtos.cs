using System.ComponentModel.DataAnnotations;
using BettsTax.Data;
using BettsTax.Data.Models;

namespace BettsTax.Core.DTOs.Workflows;

// Phase 3 Enhanced Workflow DTOs - building on existing workflow infrastructure

public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public List<WorkflowStepInstanceDto> StepInstances { get; set; } = new();
    public List<ApprovalRequest> Approvals { get; set; } = new();
}

public class WorkflowStepInstanceDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowStepId { get; set; }
    public WorkflowStepInstanceStatus Status { get; set; }
    public Dictionary<string, object> Input { get; set; } = new();
    public Dictionary<string, object> Output { get; set; } = new();
    public string? AssignedTo { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public class WorkflowStepResult
{
    public Guid StepInstanceId { get; set; }
    public bool IsCompleted { get; set; }
    public bool RequiresApproval { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Guid? NextStepId { get; set; }
}

public class ApprovalRequest
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid WorkflowStepInstanceId { get; set; }
    public string RequiredApprover { get; set; } = string.Empty;
    public WorkflowApprovalStatus Status { get; set; }
    public string? Comments { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RespondedBy { get; set; }
}

public class WorkflowTriggerDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowTriggerType Type { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTriggerRequest
{
    public string Name { get; set; } = string.Empty;
    public WorkflowTriggerType Type { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class WorkflowAnalyticsDto
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int CompletedExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime? LastExecution { get; set; }
    public List<WorkflowStepAnalytics> StepAnalytics { get; set; } = new();
}

public class WorkflowStepAnalytics
{
    public Guid StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int CompletedExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageExecutionTime { get; set; }
}

public class WorkflowMetricsDto
{
    public int TotalWorkflows { get; set; }
    public int ActiveWorkflows { get; set; }
    public int TotalInstances { get; set; }
    public int RunningInstances { get; set; }
    public int PendingApprovals { get; set; }
    public double OverallSuccessRate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class WorkflowDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowType Type { get; set; }
    public BettsTax.Data.Models.WorkflowTrigger TriggerType { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
}

public class StartWorkflowInstanceRequest
{
    [Required]
    public Guid WorkflowId { get; set; }

    [Required]
    public Dictionary<string, object> Variables { get; set; } = new();
}

public class CancelWorkflowInstanceRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class ExecuteWorkflowStepRequest
{
    public Dictionary<string, object> Inputs { get; set; } = new();
}

public class CompleteWorkflowStepRequest
{
    public Dictionary<string, object> Outputs { get; set; } = new();
}

public class AssignWorkflowStepRequest
{
    [Required]
    public string AssigneeId { get; set; } = string.Empty;
}

public class WorkflowApprovalCommandRequest
{
    [Required]
    public string ApproverId { get; set; } = string.Empty;

    public string? Comments { get; set; }
}

public class WorkflowApprovalDecisionRequest
{
    public string? Comments { get; set; }
}

public class EvaluateTriggersRequest
{
    [Required]
    public string EventType { get; set; } = string.Empty;

    public Dictionary<string, object> EventData { get; set; } = new();
}