using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Workflow rule entity
    /// </summary>
    public class WorkflowRule
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Range(1, 1000)]
        public int Priority { get; set; } = 100;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }

        [Required, MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? LastModifiedBy { get; set; }

        // Navigation properties
        public virtual ICollection<WorkflowCondition> Conditions { get; set; } = new HashSet<WorkflowCondition>();
        public virtual ICollection<WorkflowAction> Actions { get; set; } = new HashSet<WorkflowAction>();
        public virtual ICollection<WorkflowExecutionHistory> ExecutionHistory { get; set; } = new HashSet<WorkflowExecutionHistory>();
        public virtual ICollection<WorkflowRuleMetrics> Metrics { get; set; } = new HashSet<WorkflowRuleMetrics>();

        // Foreign key properties
        public int? TemplateId { get; set; }
        public virtual WorkflowTemplate? Template { get; set; }
    }

    /// <summary>
    /// Workflow condition entity
    /// </summary>
    public class WorkflowCondition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkflowRuleId { get; set; }

        [Required, MaxLength(100)]
        public string ConditionType { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string FieldName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Operator { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [MaxLength(10)]
        public string LogicalOperator { get; set; } = "AND";

        public int Order { get; set; }

    public string MetadataJson { get; set; } = "{}";

        // Navigation properties
        public virtual WorkflowRule WorkflowRule { get; set; } = null!;
    }

    /// <summary>
    /// Workflow action entity
    /// </summary>
    public class WorkflowAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkflowRuleId { get; set; }

        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

    public string ParametersJson { get; set; } = "{}";

        public int Order { get; set; }

        public bool ContinueOnError { get; set; } = true;

        [MaxLength(50)]
        public string? ErrorHandling { get; set; }

        // Navigation properties
        public virtual WorkflowRule WorkflowRule { get; set; } = null!;
        public virtual ICollection<WorkflowActionExecution> Executions { get; set; } = new HashSet<WorkflowActionExecution>();
    }

    /// <summary>
    /// Workflow template entity
    /// </summary>
    public class WorkflowTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        [Required, MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }

        [MaxLength(450)]
        public string? LastModifiedBy { get; set; }

        public int UsageCount { get; set; }

        [Range(0, 5)]
        public double Rating { get; set; }

    public string TagsJson { get; set; } = "[]";

    public string DefinitionJson { get; set; } = "{}";

        // Navigation properties
        public virtual ICollection<WorkflowRule> Rules { get; set; } = new HashSet<WorkflowRule>();
        public virtual ICollection<WorkflowTemplateReview> Reviews { get; set; } = new HashSet<WorkflowTemplateReview>();
    }

    /// <summary>
    /// Workflow template review entity
    /// </summary>
    public class WorkflowTemplateReview
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int TemplateId { get; set; }

        [Required, MaxLength(450)]
        public string ReviewerId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ReviewerName { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsVerified { get; set; }

        // Navigation properties
        public virtual WorkflowTemplate Template { get; set; } = null!;
    }

    /// <summary>
    /// Workflow execution history entity
    /// </summary>
    public class WorkflowExecutionHistory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int RuleId { get; set; }

        [Required, MaxLength(200)]
        public string RuleName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

    public string TriggerDataJson { get; set; } = "{}";

    public string? ErrorMessage { get; set; }

        [Required, MaxLength(450)]
        public string ExecutedBy { get; set; } = string.Empty;

        public int ActionsExecuted { get; set; }
        public int ActionsSucceeded { get; set; }

        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        // Navigation properties
        public virtual WorkflowRule Rule { get; set; } = null!;
        public virtual ICollection<WorkflowActionExecution> ActionExecutions { get; set; } = new HashSet<WorkflowActionExecution>();
    }

    /// <summary>
    /// Workflow action execution entity
    /// </summary>
    public class WorkflowActionExecution
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ExecutionHistoryId { get; set; } = string.Empty;

        [Required]
        public int ActionId { get; set; }

        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

    public string? Result { get; set; }

    public string? ErrorMessage { get; set; }

        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        // Navigation properties
        public virtual WorkflowExecutionHistory ExecutionHistory { get; set; } = null!;
        public virtual WorkflowAction Action { get; set; } = null!;
    }

    /// <summary>
    /// Workflow rule metrics entity
    /// </summary>
    public class WorkflowRuleMetrics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RuleId { get; set; }

        public DateTime MetricDate { get; set; } = DateTime.UtcNow.Date;

        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }

        public long TotalExecutionTimeMs { get; set; }

        [NotMapped]
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

        [NotMapped]
        public TimeSpan AverageExecutionTime => TotalExecutions > 0 
            ? TimeSpan.FromMilliseconds((double)TotalExecutionTimeMs / TotalExecutions) 
            : TimeSpan.Zero;

        // Navigation properties
        public virtual WorkflowRule Rule { get; set; } = null!;
    }

    /// <summary>
    /// Workflow system configuration entity
    /// </summary>
    public class WorkflowSystemConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string ConfigurationKey { get; set; } = string.Empty;

        [Required]
        public string ConfigurationValue { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(450)]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workflow execution queue entity for background processing
    /// </summary>
    public class WorkflowExecutionQueue
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int RuleId { get; set; }

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

    public string TriggerDataJson { get; set; } = "{}";

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Queued";

        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public int Priority { get; set; } = 100;
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;

    public string? ErrorMessage { get; set; }

        [Required, MaxLength(450)]
        public string TriggeredBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual WorkflowRule Rule { get; set; } = null!;
    }
}