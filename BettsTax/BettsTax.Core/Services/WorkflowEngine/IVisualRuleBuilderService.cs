using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services.WorkflowEngine
{
    /// <summary>
    /// Interface for enhanced visual no-code workflow rule builder
    /// Provides comprehensive drag-and-drop workflow creation capabilities
    /// </summary>
    public interface IVisualRuleBuilderService
    {
        /// <summary>
        /// Creates a new visual workflow from drag-and-drop components
        /// </summary>
        /// <param name="request">Visual workflow creation request</param>
        /// <returns>Created visual workflow definition</returns>
        Task<Result<VisualWorkflowDefinition>> CreateVisualWorkflowAsync(CreateVisualWorkflowRequest request);

        /// <summary>
        /// Updates an existing visual workflow
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="request">Updated workflow definition</param>
        /// <returns>Updated visual workflow definition</returns>
        Task<Result<VisualWorkflowDefinition>> UpdateVisualWorkflowAsync(Guid workflowId, CreateVisualWorkflowRequest request);

        /// <summary>
        /// Gets a visual workflow by ID
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <returns>Visual workflow definition</returns>
        Task<Result<VisualWorkflowDefinition>> GetVisualWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Gets all visual workflows with filtering options
        /// </summary>
        /// <param name="filter">Workflow filter criteria</param>
        /// <returns>List of visual workflows</returns>
        Task<Result<List<VisualWorkflowDefinition>>> GetVisualWorkflowsAsync(VisualWorkflowFilter? filter = null);

        /// <summary>
        /// Deletes a visual workflow
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <returns>Success result</returns>
        Task<Result<bool>> DeleteVisualWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Gets available workflow node types with their configurations
        /// </summary>
        /// <returns>List of available node types</returns>
        Task<Result<List<WorkflowNodeType>>> GetAvailableNodeTypesAsync();

        /// <summary>
        /// Gets entity schemas for dynamic field selection
        /// </summary>
        /// <returns>List of entity schemas</returns>
        Task<Result<List<EntitySchema>>> GetEntitySchemasAsync();

        /// <summary>
        /// Tests a visual workflow with sample data
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="testData">Test input data</param>
        /// <returns>Workflow test results</returns>
        Task<Result<WorkflowTestResult>> TestVisualWorkflowAsync(Guid workflowId, Dictionary<string, object> testData);

        /// <summary>
        /// Validates a visual workflow definition
        /// </summary>
        /// <param name="workflow">Workflow to validate</param>
        /// <returns>Validation results</returns>
        Task<Result<WorkflowValidationResult>> ValidateVisualWorkflowAsync(VisualWorkflowDefinition workflow);

        /// <summary>
        /// Publishes a visual workflow to make it active
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <returns>Published workflow</returns>
        Task<Result<VisualWorkflowDefinition>> PublishVisualWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Creates a copy of an existing visual workflow
        /// </summary>
        /// <param name="sourceWorkflowId">Source workflow identifier</param>
        /// <param name="newName">Name for the copied workflow</param>
        /// <returns>Copied workflow</returns>
        Task<Result<VisualWorkflowDefinition>> CloneVisualWorkflowAsync(Guid sourceWorkflowId, string newName);

        /// <summary>
        /// Exports a visual workflow to JSON format
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <returns>JSON representation of the workflow</returns>
        Task<Result<string>> ExportVisualWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Imports a visual workflow from JSON format
        /// </summary>
        /// <param name="jsonData">JSON workflow data</param>
        /// <param name="importOptions">Import configuration options</param>
        /// <returns>Imported workflow</returns>
        Task<Result<VisualWorkflowDefinition>> ImportVisualWorkflowAsync(string jsonData, WorkflowImportOptions? importOptions = null);

        /// <summary>
        /// Gets workflow execution history and analytics
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="dateRange">Date range for history</param>
        /// <returns>Workflow execution analytics</returns>
        Task<Result<WorkflowExecutionAnalytics>> GetWorkflowAnalyticsAsync(Guid workflowId, DateRange? dateRange = null);

        /// <summary>
        /// Gets available workflow templates
        /// </summary>
        /// <param name="category">Template category filter</param>
        /// <returns>List of workflow templates</returns>
        Task<Result<List<WorkflowTemplate>>> GetWorkflowTemplatesAsync(string? category = null);

        /// <summary>
        /// Creates a new workflow from a template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="workflowName">Name for the new workflow</param>
        /// <param name="customizations">Template customizations</param>
        /// <returns>Created workflow from template</returns>
        Task<Result<VisualWorkflowDefinition>> CreateWorkflowFromTemplateAsync(
            Guid templateId, 
            string workflowName, 
            Dictionary<string, object>? customizations = null);
    }

    /// <summary>
    /// Visual workflow filter criteria
    /// </summary>
    public class VisualWorkflowFilter
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsPublished { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public List<string>? Tags { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
    }

    /// <summary>
    /// Workflow validation result
    /// </summary>
    public class WorkflowValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public WorkflowComplexityMetrics? Complexity { get; set; }
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? NodeId { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? NodeId { get; set; }
        public string? Suggestion { get; set; }
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Workflow complexity metrics
    /// </summary>
    public class WorkflowComplexityMetrics
    {
        public int NodeCount { get; set; }
        public int ConnectionCount { get; set; }
        public int DecisionPoints { get; set; }
        public int MaxDepth { get; set; }
        public double ComplexityScore { get; set; }
        public string ComplexityLevel { get; set; } = string.Empty; // Simple, Moderate, Complex, Very Complex
    }

    /// <summary>
    /// Workflow import options
    /// </summary>
    public class WorkflowImportOptions
    {
        public bool OverwriteExisting { get; set; }
        public bool ValidateOnImport { get; set; } = true;
        public bool PreserveIds { get; set; }
        public Dictionary<string, string>? FieldMappings { get; set; }
    }

    /// <summary>
    /// Workflow execution analytics
    /// </summary>
    public class WorkflowExecutionAnalytics
    {
        public Guid WorkflowId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public DateRange Period { get; set; } = new();
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public List<ExecutionTrend> Trends { get; set; } = new();
        public List<NodePerformance> NodePerformance { get; set; } = new();
        public List<ErrorSummary> CommonErrors { get; set; } = new();
    }

    /// <summary>
    /// Date range specification
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Execution trend data point
    /// </summary>
    public class ExecutionTrend
    {
        public DateTime Date { get; set; }
        public int ExecutionCount { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
    }

    /// <summary>
    /// Node performance metrics
    /// </summary>
    public class NodePerformance
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// Error summary for analytics
    /// </summary>
    public class ErrorSummary
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int Occurrences { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public List<string> AffectedNodes { get; set; } = new();
    }

    /// <summary>
    /// Workflow template definition
    /// </summary>
    public class WorkflowTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public VisualWorkflowDefinition WorkflowDefinition { get; set; } = new();
        public TemplateMetadata Metadata { get; set; } = new();
        public List<TemplateParameter> Parameters { get; set; } = new();
        public bool IsSystemTemplate { get; set; }
        public int UsageCount { get; set; }
        public double Rating { get; set; }
    }

    /// <summary>
    /// Template parameter definition
    /// </summary>
    public class TemplateParameter
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<PropertyOption>? Options { get; set; }
    }
}