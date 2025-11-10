using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Workflow rule data transfer object
    /// </summary>
    public class WorkflowRuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public List<WorkflowConditionDto> Conditions { get; set; } = new();
        public List<WorkflowActionDto> Actions { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? LastModifiedBy { get; set; }
        public WorkflowRuleMetricsDto Metrics { get; set; } = new();
    }

    /// <summary>
    /// Create workflow rule request
    /// </summary>
    public class CreateWorkflowRuleDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Range(1, 1000)]
        public int Priority { get; set; } = 100;

        public List<CreateWorkflowConditionDto> Conditions { get; set; } = new();
        public List<CreateWorkflowActionDto> Actions { get; set; } = new();
    }

    /// <summary>
    /// Update workflow rule request
    /// </summary>
    public class UpdateWorkflowRuleDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        [Range(1, 1000)]
        public int Priority { get; set; }

        public List<CreateWorkflowConditionDto> Conditions { get; set; } = new();
        public List<CreateWorkflowActionDto> Actions { get; set; } = new();
    }

    /// <summary>
    /// Workflow condition data transfer object
    /// </summary>
    public class WorkflowConditionDto
    {
        public int Id { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string LogicalOperator { get; set; } = "AND"; // AND, OR
        public int Order { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Create workflow condition request
    /// </summary>
    public class CreateWorkflowConditionDto
    {
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
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Workflow action data transfer object
    /// </summary>
    public class WorkflowActionDto
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int Order { get; set; }
        public bool ContinueOnError { get; set; }
        public string? ErrorHandling { get; set; }
    }

    /// <summary>
    /// Create workflow action request
    /// </summary>
    public class CreateWorkflowActionDto
    {
        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public Dictionary<string, object> Parameters { get; set; } = new();
        public int Order { get; set; }
        public bool ContinueOnError { get; set; } = true;

        [MaxLength(50)]
        public string? ErrorHandling { get; set; }
    }

    /// <summary>
    /// Workflow rule filter for queries
    /// </summary>
    public class WorkflowRuleFilterDto
    {
        public string? Name { get; set; }
        public string? TriggerType { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? CreatedBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    /// <summary>
    /// Workflow rule test result
    /// </summary>
    public class WorkflowRuleTestResultDto
    {
        public bool Success { get; set; }
        public bool ConditionsMatched { get; set; }
        public List<WorkflowConditionResultDto> ConditionResults { get; set; } = new();
        public List<WorkflowActionResultDto> ActionResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Workflow condition evaluation result
    /// </summary>
    public class WorkflowConditionResultDto
    {
        public int ConditionId { get; set; }
        public string ConditionType { get; set; } = string.Empty;
        public bool Matched { get; set; }
        public string ActualValue { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Workflow action execution result
    /// </summary>
    public class WorkflowActionResultDto
    {
        public int ActionId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Workflow trigger type definition
    /// </summary>
    public class WorkflowTriggerTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<WorkflowFieldDto> AvailableFields { get; set; } = new();
        public List<string> SupportedEvents { get; set; } = new();
    }

    /// <summary>
    /// Workflow field definition
    /// </summary>
    public class WorkflowFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public List<string> SupportedOperators { get; set; } = new();
        public object? DefaultValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Workflow condition type definition
    /// </summary>
    public class WorkflowConditionTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<WorkflowOperatorDto> SupportedOperators { get; set; } = new();
    }

    /// <summary>
    /// Workflow operator definition
    /// </summary>
    public class WorkflowOperatorDto
    {
        public string Operator { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool RequiresValue { get; set; }
        public string? ValueType { get; set; }
    }

    /// <summary>
    /// Workflow action type definition
    /// </summary>
    public class WorkflowActionTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<WorkflowActionParameterDto> Parameters { get; set; } = new();
        public bool RequiresApproval { get; set; }
    }

    /// <summary>
    /// Workflow action parameter definition
    /// </summary>
    public class WorkflowActionParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<object>? AllowedValues { get; set; }
        public Dictionary<string, object> Validation { get; set; } = new();
    }

    /// <summary>
    /// Workflow rule metrics
    /// </summary>
    public class WorkflowRuleMetricsDto
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public DateTime? LastExecuted { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Workflow execution history entry
    /// </summary>
    public class WorkflowExecutionHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public int RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public object? TriggerData { get; set; }
        public List<WorkflowActionResultDto> ActionResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workflow execution history filter
    /// </summary>
    public class WorkflowExecutionHistoryFilterDto
    {
        public int? RuleId { get; set; }
        public string? Status { get; set; }
        public string? TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ExecutedBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Workflow trigger event
    /// </summary>
    public class WorkflowTriggerEventDto
    {
        public string TriggerType { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public string TriggeredBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowExecutionResultDto
    {
        public string ExecutionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int RulesExecuted { get; set; }
        public int RulesMatched { get; set; }
        public int ActionsExecuted { get; set; }
        public int ActionsSucceeded { get; set; }
        public List<WorkflowRuleExecutionDto> RuleResults { get; set; } = new();
        public TimeSpan TotalExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual workflow rule execution result
    /// </summary>
    public class WorkflowRuleExecutionDto
    {
        public int RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public bool Executed { get; set; }
        public bool Success { get; set; }
        public bool ConditionsMatched { get; set; }
        public List<WorkflowActionResultDto> ActionResults { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Workflow execution status
    /// </summary>
    public class WorkflowExecutionStatusDto
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string? CurrentAction { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EstimatedEndTime { get; set; }
        public List<string> ExecutionLog { get; set; } = new();
        public bool CanCancel { get; set; }
        public bool CanRetry { get; set; }
    }

    /// <summary>
    /// Workflow metrics
    /// </summary>
    public class WorkflowMetricsDto
    {
        public int TotalRules { get; set; }
        public int ActiveRules { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public List<WorkflowTriggerMetricDto> TriggerMetrics { get; set; } = new();
        public List<WorkflowActionMetricDto> ActionMetrics { get; set; } = new();
    }

    /// <summary>
    /// Workflow trigger metrics
    /// </summary>
    public class WorkflowTriggerMetricDto
    {
        public string TriggerType { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Workflow action metrics
    /// </summary>
    public class WorkflowActionMetricDto
    {
        public string ActionType { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Workflow metrics filter
    /// </summary>
    public class WorkflowMetricsFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string>? TriggerTypes { get; set; }
        public List<int>? RuleIds { get; set; }
        public bool IncludeInactive { get; set; }
    }
}