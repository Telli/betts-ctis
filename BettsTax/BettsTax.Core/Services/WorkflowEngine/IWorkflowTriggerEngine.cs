using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services.WorkflowEngine
{
    /// <summary>
    /// Interface for advanced workflow trigger engine
    /// Provides comprehensive trigger management and execution capabilities
    /// </summary>
    public interface IWorkflowTriggerEngine
    {
        /// <summary>
        /// Creates a new advanced trigger
        /// </summary>
        /// <param name="request">Trigger creation request</param>
        /// <returns>Created advanced trigger</returns>
        Task<Result<AdvancedTrigger>> CreateTriggerAsync(CreateTriggerRequest request);

        /// <summary>
        /// Updates an existing trigger
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="request">Updated trigger configuration</param>
        /// <returns>Updated trigger</returns>
        Task<Result<AdvancedTrigger>> UpdateTriggerAsync(Guid triggerId, CreateTriggerRequest request);

        /// <summary>
        /// Gets a trigger by ID
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <returns>Advanced trigger</returns>
        Task<Result<AdvancedTrigger>> GetTriggerAsync(Guid triggerId);

        /// <summary>
        /// Gets all triggers with filtering options
        /// </summary>
        /// <param name="filter">Trigger filter criteria</param>
        /// <returns>List of triggers</returns>
        Task<Result<List<AdvancedTrigger>>> GetTriggersAsync(TriggerFilter? filter = null);

        /// <summary>
        /// Deletes a trigger
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <returns>Success result</returns>
        Task<Result<bool>> DeleteTriggerAsync(Guid triggerId);

        /// <summary>
        /// Activates or deactivates a trigger
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="isActive">Active state</param>
        /// <returns>Updated trigger</returns>
        Task<Result<AdvancedTrigger>> SetTriggerActiveAsync(Guid triggerId, bool isActive);

        /// <summary>
        /// Fires an event trigger manually with event data
        /// </summary>
        /// <param name="eventType">Type of event to fire</param>
        /// <param name="eventData">Event data payload</param>
        /// <returns>Execution results for all matching triggers</returns>
        Task<Result<TriggerExecutionResult>> FireEventTriggerAsync(string eventType, Dictionary<string, object> eventData);

        /// <summary>
        /// Executes a specific trigger manually
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="executionData">Data to pass to workflow execution</param>
        /// <returns>Workflow execution result</returns>
        Task<Result<WorkflowExecutionResult>> ExecuteTriggerAsync(Guid triggerId, Dictionary<string, object> executionData);

        /// <summary>
        /// Tests a trigger configuration with sample data
        /// </summary>
        /// <param name="trigger">Trigger to test</param>
        /// <param name="testData">Test data payload</param>
        /// <returns>Test results</returns>
        Task<Result<TriggerTestResult>> TestTriggerAsync(AdvancedTrigger trigger, Dictionary<string, object> testData);

        /// <summary>
        /// Gets trigger execution statistics
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="period">Time period for statistics</param>
        /// <returns>Execution statistics</returns>
        Task<Result<TriggerExecutionStatistics>> GetTriggerStatisticsAsync(Guid triggerId, TimeSpan? period = null);

        /// <summary>
        /// Gets execution history for a trigger
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="pageSize">Number of executions per page</param>
        /// <param name="pageNumber">Page number</param>
        /// <returns>Execution history</returns>
        Task<Result<PagedResult<WorkflowExecutionResult>>> GetTriggerExecutionHistoryAsync(
            Guid triggerId, 
            int pageSize = 50, 
            int pageNumber = 1);

        /// <summary>
        /// Gets available trigger types and their configurations
        /// </summary>
        /// <returns>List of supported trigger types</returns>
        Task<Result<List<TriggerTypeInfo>>> GetSupportedTriggerTypesAsync();

        /// <summary>
        /// Validates trigger configuration before creation
        /// </summary>
        /// <param name="request">Trigger configuration to validate</param>
        /// <returns>Validation results</returns>
        Task<Result<TriggerValidationResult>> ValidateTriggerConfigurationAsync(CreateTriggerRequest request);

        /// <summary>
        /// Gets next scheduled execution times for schedule triggers
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <param name="count">Number of future executions to return</param>
        /// <returns>List of upcoming execution times</returns>
        Task<Result<List<DateTime>>> GetNextExecutionTimesAsync(Guid triggerId, int count = 10);

        /// <summary>
        /// Exports trigger configuration to JSON
        /// </summary>
        /// <param name="triggerId">Trigger identifier</param>
        /// <returns>JSON representation of trigger</returns>
        Task<Result<string>> ExportTriggerAsync(Guid triggerId);

        /// <summary>
        /// Imports trigger configuration from JSON
        /// </summary>
        /// <param name="jsonData">JSON trigger configuration</param>
        /// <param name="importOptions">Import options</param>
        /// <returns>Imported trigger</returns>
        Task<Result<AdvancedTrigger>> ImportTriggerAsync(string jsonData, TriggerImportOptions? importOptions = null);

        /// <summary>
        /// Clones an existing trigger with modifications
        /// </summary>
        /// <param name="sourceTrigger">Source trigger to clone</param>
        /// <param name="newName">Name for the cloned trigger</param>
        /// <param name="modifications">Modifications to apply</param>
        /// <returns>Cloned trigger</returns>
        Task<Result<AdvancedTrigger>> CloneTriggerAsync(
            Guid sourceTrigger, 
            string newName, 
            Dictionary<string, object>? modifications = null);

        /// <summary>
        /// Gets trigger templates for common scenarios
        /// </summary>
        /// <param name="category">Template category filter</param>
        /// <returns>List of trigger templates</returns>
        Task<Result<List<TriggerTemplate>>> GetTriggerTemplatesAsync(string? category = null);

        /// <summary>
        /// Creates a trigger from a template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="triggerName">Name for the new trigger</param>
        /// <param name="workflowId">Target workflow ID</param>
        /// <param name="parameters">Template parameters</param>
        /// <returns>Created trigger from template</returns>
        Task<Result<AdvancedTrigger>> CreateTriggerFromTemplateAsync(
            Guid templateId,
            string triggerName,
            Guid workflowId,
            Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Registers a custom event type for event triggers
        /// </summary>
        /// <param name="eventType">Event type name</param>
        /// <param name="eventSchema">Event data schema</param>
        /// <returns>Registration result</returns>
        Task<Result<bool>> RegisterEventTypeAsync(string eventType, EventSchema eventSchema);

        /// <summary>
        /// Gets registered event types
        /// </summary>
        /// <returns>List of registered event types</returns>
        Task<Result<List<EventTypeInfo>>> GetRegisteredEventTypesAsync();
    }

    /// <summary>
    /// Trigger type information
    /// </summary>
    public class TriggerTypeInfo
    {
        public TriggerType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<ConfigurationProperty> ConfigurationProperties { get; set; } = new();
        public List<string> Examples { get; set; } = new();
        public bool IsCustom { get; set; }
    }

    /// <summary>
    /// Configuration property definition
    /// </summary>
    public class ConfigurationProperty
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<PropertyOption>? Options { get; set; }
        public PropertyValidation? Validation { get; set; }
    }

    /// <summary>
    /// Trigger validation result
    /// </summary>
    public class TriggerValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public Dictionary<string, object> ValidatedConfiguration { get; set; } = new();
    }

    /// <summary>
    /// Trigger import options
    /// </summary>
    public class TriggerImportOptions
    {
        public bool OverwriteExisting { get; set; }
        public bool ValidateOnImport { get; set; } = true;
        public bool PreserveIds { get; set; }
        public Guid? TargetWorkflowId { get; set; }
        public Dictionary<string, string>? FieldMappings { get; set; }
    }

    /// <summary>
    /// Trigger template definition
    /// </summary>
    public class TriggerTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TriggerType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<TemplateParameter> Parameters { get; set; } = new();
        public TemplateMetadata Metadata { get; set; } = new();
        public bool IsSystemTemplate { get; set; }
        public int UsageCount { get; set; }
        public double Rating { get; set; }
    }

    /// <summary>
    /// Event schema definition
    /// </summary>
    public class EventSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, EventField> Fields { get; set; } = new();
        public List<string> RequiredFields { get; set; } = new();
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Event field definition
    /// </summary>
    public class EventField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// Event type information
    /// </summary>
    public class EventTypeInfo
    {
        public string EventType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EventSchema Schema { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public string RegisteredBy { get; set; } = string.Empty;
        public int TriggerCount { get; set; }
        public int RecentEvents { get; set; }
    }

    /// <summary>
    /// Paged result container
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}