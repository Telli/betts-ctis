using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models
{
    /// <summary>
    /// Represents a workflow definition that can be executed
    /// </summary>
    public class Workflow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public WorkflowType Type { get; set; }

        [Required]
        public WorkflowTrigger Trigger { get; set; }

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 1; // 1-10, higher = more important

        // JSON serialized conditions for the workflow trigger
        [MaxLength(5000)]
        public string TriggerConditions { get; set; } = "{}";

        // JSON serialized workflow steps
        [MaxLength(10000)]
        public string Steps { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<WorkflowExecution> Executions { get; set; } = new List<WorkflowExecution>();
    }

    /// <summary>
    /// Represents a single execution instance of a workflow
    /// </summary>
    public class WorkflowExecution
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowId { get; set; }

        [Required]
        public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Pending;

        // JSON serialized context data for the execution
        [MaxLength(10000)]
        public string ContextData { get; set; } = "{}";

        // JSON serialized execution results
        [MaxLength(10000)]
        public string ExecutionResults { get; set; } = "{}";

        public string ErrorMessage { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Foreign key
        [ForeignKey("WorkflowId")]
        public virtual Workflow Workflow { get; set; } = null!;
    }

    /// <summary>
    /// Template for creating workflows
    /// </summary>
    public class WorkflowTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public WorkflowType Type { get; set; }

        // JSON serialized template definition
        [MaxLength(10000)]
        public string TemplateDefinition { get; set; } = "{}";

        public bool IsSystemTemplate { get; set; } = false; // System templates cannot be deleted

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Business rule for workflow automation
    /// </summary>
    public class WorkflowRule
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // e.g., "Payment", "Document", "Client"

        // JSON serialized conditions
        [MaxLength(5000)]
        public string Conditions { get; set; } = "[]";

        // JSON serialized actions
        [MaxLength(5000)]
        public string Actions { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 1; // Execution order for multiple rules

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of testing a business rule
    /// </summary>
    public class RuleTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> TestData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Workflow types
    /// </summary>
    public enum WorkflowType
    {
        PaymentApproval = 1,
        DocumentReview = 2,
        ComplianceCheck = 3,
        Notification = 4,
        TaxFiling = 5,
        ClientOnboarding = 6,
        Custom = 99
    }

    /// <summary>
    /// Workflow trigger types
    /// </summary>
    public enum WorkflowTrigger
    {
        Manual = 1,
        Scheduled = 2,
        EventBased = 3,
        ConditionBased = 4
    }

    /// <summary>
    /// Workflow execution status
    /// </summary>
    public enum WorkflowExecutionStatus
    {
        Pending = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Workflow step types for the no-code builder
    /// </summary>
    public enum WorkflowStepType
    {
        Condition = 1,
        Action = 2,
        Approval = 3,
        Notification = 4,
        Integration = 5,
        Delay = 6
    }

    /// <summary>
    /// Workflow condition operators
    /// </summary>
    public enum WorkflowConditionOperator
    {
        Equals = 1,
        NotEquals = 2,
        GreaterThan = 3,
        LessThan = 4,
        GreaterThanOrEqual = 5,
        LessThanOrEqual = 6,
        Contains = 7,
        NotContains = 8,
        IsEmpty = 9,
        IsNotEmpty = 10,
        In = 11,
        NotIn = 12
    }

    /// <summary>
    /// Workflow action types
    /// </summary>
    public enum WorkflowActionType
    {
        UpdateRecord = 1,
        SendNotification = 2,
        CreateTask = 3,
        CallWebhook = 4,
        GenerateReport = 5,
        SendEmail = 6,
        SendSms = 7,
        UpdateStatus = 8,
        AssignToUser = 9,
        Escalate = 10
    }
}