using System;
using System.Collections.Generic;
using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Advanced workflow trigger with comprehensive configuration
    /// </summary>
    public class AdvancedTrigger
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TriggerType Type { get; set; }
        public Guid WorkflowId { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<TriggerCondition> Conditions { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public TriggerMetadata Metadata { get; set; } = new();
        public TriggerSettings Settings { get; set; } = new();
    }

    /// <summary>
    /// Types of advanced workflow triggers
    /// </summary>
    public enum TriggerType
    {
        Schedule,    // Time-based triggers with cron expressions
        Event,       // Event-driven triggers
        Threshold,   // Data threshold triggers
        Manual,      // Manual execution triggers
        Webhook,     // HTTP webhook triggers
        FileWatcher, // File system change triggers
        Database,    // Database change triggers
        Queue        // Message queue triggers
    }

    /// <summary>
    /// Trigger condition for filtering events
    /// </summary>
    public class TriggerCondition
    {
        public string Field { get; set; } = string.Empty;
        public TriggerOperator Operator { get; set; }
        public object? Value { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Trigger condition operators
    /// </summary>
    public enum TriggerOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        IsNull,
        IsNotNull,
        In,
        NotIn
    }

    /// <summary>
    /// Trigger metadata and execution tracking
    /// </summary>
    public class TriggerMetadata
    {
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public DateTime? LastExecutedAt { get; set; }
        public DateTime? LastSuccessfulExecutionAt { get; set; }
        public int ExecutionCount { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    /// <summary>
    /// Trigger execution settings
    /// </summary>
    public class TriggerSettings
    {
        public bool AllowConcurrentExecution { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(30);
        public bool LogExecution { get; set; } = true;
        public string TimeZone { get; set; } = "UTC";
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Request to create a new trigger
    /// </summary>
    public class CreateTriggerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TriggerType Type { get; set; }
        public Guid WorkflowId { get; set; }
        public Dictionary<string, object>? Configuration { get; set; }
        public List<TriggerCondition>? Conditions { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; } = string.Empty;
        public TriggerSettings? Settings { get; set; }
    }

    /// <summary>
    /// Trigger filter criteria
    /// </summary>
    public class TriggerFilter
    {
        public string? Name { get; set; }
        public TriggerType? Type { get; set; }
        public Guid? WorkflowId { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
    }

    /// <summary>
    /// Trigger execution result
    /// </summary>
    public class TriggerExecutionResult
    {
        public string EventType { get; set; } = string.Empty;
        public int TriggersExecuted { get; set; }
        public List<WorkflowExecutionResult> ExecutionResults { get; set; } = new();
        public DateTime ExecutedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
    }

    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowExecutionResult
    {
        public Guid WorkflowId { get; set; }
        public Guid TriggerId { get; set; }
        public Guid ExecutionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool IsSuccessful { get; set; }
        public Dictionary<string, object> OutputData { get; set; } = new();
        public string? Error { get; set; }
        public List<ExecutionStep> Steps { get; set; } = new();
    }

    /// <summary>
    /// Workflow execution step result
    /// </summary>
    public class ExecutionStep
    {
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccessful { get; set; }
        public Dictionary<string, object> Output { get; set; } = new();
        public string? Error { get; set; }
    }

    /// <summary>
    /// Trigger test result
    /// </summary>
    public class TriggerTestResult
    {
        public Guid TriggerId { get; set; }
        public string TriggerName { get; set; } = string.Empty;
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TestDuration { get; set; }
        public bool ConfigurationValid { get; set; }
        public List<string> ConfigurationErrors { get; set; } = new();
        public bool ConditionsPass { get; set; }
        public List<ConditionTestResult> ConditionResults { get; set; } = new();
        public bool OverallResult { get; set; }
        public Dictionary<string, object> TestMetrics { get; set; } = new();
    }

    /// <summary>
    /// Condition test result
    /// </summary>
    public class ConditionTestResult
    {
        public TriggerCondition Condition { get; set; } = new();
        public bool Result { get; set; }
        public object? TestData { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Schedule trigger configuration
    /// </summary>
    public class ScheduleTriggerConfig
    {
        public string CronExpression { get; set; } = string.Empty;
        public string TimeZone { get; set; } = "UTC";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool RunOnce { get; set; } = false;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Event trigger configuration
    /// </summary>
    public class EventTriggerConfig
    {
        public string EventType { get; set; } = string.Empty;
        public List<string> EventSources { get; set; } = new();
        public Dictionary<string, object> EventFilters { get; set; } = new();
        public bool ProcessHistoricalEvents { get; set; } = false;
        public TimeSpan EventWindow { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Threshold trigger configuration
    /// </summary>
    public class ThresholdTriggerConfig
    {
        public string Metric { get; set; } = string.Empty;
        public TriggerOperator Operator { get; set; }
        public double Value { get; set; }
        public TimeSpan EvaluationPeriod { get; set; } = TimeSpan.FromMinutes(5);
        public int ConsecutiveBreaches { get; set; } = 1;
        public bool ResetOnNormal { get; set; } = true;
    }

    /// <summary>
    /// Webhook trigger configuration
    /// </summary>
    public class WebhookTriggerConfig
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public List<string> AllowedIPs { get; set; } = new();
        public Dictionary<string, string> RequiredHeaders { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
        public string SignatureHeader { get; set; } = "X-Signature";
        public bool ValidateSignature { get; set; } = true;
    }

    /// <summary>
    /// File watcher trigger configuration
    /// </summary>
    public class FileWatcherTriggerConfig
    {
        public string WatchPath { get; set; } = string.Empty;
        public List<string> FilePatterns { get; set; } = new();
        public List<FileChangeType> ChangeTypes { get; set; } = new();
        public bool IncludeSubdirectories { get; set; } = true;
        public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// File change types for file watcher triggers
    /// </summary>
    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    /// <summary>
    /// Database trigger configuration
    /// </summary>
    public class DatabaseTriggerConfig
    {
        public string TableName { get; set; } = string.Empty;
        public List<DatabaseOperation> Operations { get; set; } = new();
        public Dictionary<string, object> Filters { get; set; } = new();
        public List<string> MonitoredColumns { get; set; } = new();
        public bool IncludeOldValues { get; set; } = false;
    }

    /// <summary>
    /// Database operations for database triggers
    /// </summary>
    public enum DatabaseOperation
    {
        Insert,
        Update,
        Delete
    }

    /// <summary>
    /// Queue trigger configuration
    /// </summary>
    public class QueueTriggerConfig
    {
        public string QueueName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxConcurrentMessages { get; set; } = 1;
        public TimeSpan VisibilityTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxRetries { get; set; } = 3;
        public string DeadLetterQueue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trigger execution statistics
    /// </summary>
    public class TriggerExecutionStatistics
    {
        public Guid TriggerId { get; set; }
        public string TriggerName { get; set; } = string.Empty;
        public TriggerType TriggerType { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public DateTime? LastExecutedAt { get; set; }
        public DateTime? LastSuccessfulExecutionAt { get; set; }
        public List<ExecutionTrend> ExecutionTrends { get; set; } = new();
        public List<ErrorSummary> RecentErrors { get; set; } = new();
    }
}