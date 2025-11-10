using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Data;
using BettsTax.Data.Models;

// Aliases to resolve ambiguity
using DataWorkflowRule = BettsTax.Data.WorkflowRule;
using DataWorkflowTemplate = BettsTax.Data.WorkflowTemplate;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Interface for workflow automation engine
    /// </summary>
    public interface IWorkflowEngineService
    {
        /// <summary>
        /// Creates a new workflow
        /// </summary>
        Task<Workflow> CreateWorkflowAsync(Workflow workflow);

        /// <summary>
        /// Updates an existing workflow
        /// </summary>
        Task<Workflow> UpdateWorkflowAsync(Guid workflowId, Workflow workflow);

        /// <summary>
        /// Deletes a workflow
        /// </summary>
        Task DeleteWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Gets a workflow by ID
        /// </summary>
        Task<Workflow?> GetWorkflowAsync(Guid workflowId);

        /// <summary>
        /// Gets all workflows with optional filtering
        /// </summary>
        Task<IEnumerable<Workflow>> GetWorkflowsAsync(WorkflowType? type = null, bool? isActive = null);

        /// <summary>
        /// Triggers workflow execution based on event
        /// </summary>
        Task<WorkflowExecution> TriggerWorkflowAsync(Guid workflowId, Dictionary<string, object> contextData);

        /// <summary>
        /// Evaluates trigger conditions for workflows
        /// </summary>
        Task<IEnumerable<Workflow>> EvaluateTriggersAsync(string eventType, Dictionary<string, object> eventData);

        /// <summary>
        /// Executes a workflow step
        /// </summary>
        Task ExecuteWorkflowStepAsync(Guid executionId, int stepIndex);

        /// <summary>
        /// Gets workflow execution status
        /// </summary>
        Task<WorkflowExecution?> GetWorkflowExecutionAsync(Guid executionId);

        /// <summary>
        /// Gets workflow executions for a workflow
        /// </summary>
        Task<IEnumerable<WorkflowExecution>> GetWorkflowExecutionsAsync(Guid workflowId, int page = 1, int pageSize = 20);

        /// <summary>
        /// Creates a workflow from template
        /// </summary>
        Task<Workflow> CreateWorkflowFromTemplateAsync(Guid templateId, string name, Dictionary<string, object> parameters);

        /// <summary>
        /// Gets available workflow templates
        /// </summary>
        Task<IEnumerable<DataWorkflowTemplate>> GetWorkflowTemplatesAsync(bool includeSystemTemplates = true);

        /// <summary>
        /// Validates workflow definition
        /// </summary>
        Task<ValidationResult> ValidateWorkflowAsync(Workflow workflow);
    }

    /// <summary>
    /// Interface for no-code workflow rule builder
    /// </summary>
    public interface IWorkflowRuleBuilderService
    {
        /// <summary>
        /// Gets available condition operators
        /// </summary>
        IEnumerable<WorkflowConditionOperator> GetConditionOperators();

        /// <summary>
        /// Gets available action types
        /// </summary>
        IEnumerable<WorkflowActionType> GetActionTypes();

        /// <summary>
        /// Gets available trigger types
        /// </summary>
        IEnumerable<BettsTax.Data.Models.WorkflowTrigger> GetTriggerTypes();

        /// <summary>
        /// Gets available workflow step types
        /// </summary>
        IEnumerable<WorkflowStepType> GetStepTypes();

        /// <summary>
        /// Gets available fields for a given entity type
        /// </summary>
        Task<IEnumerable<string>> GetAvailableFieldsAsync(string entityType);

        /// <summary>
        /// Builds workflow condition from rule definition
        /// </summary>
        Task<string> BuildConditionExpressionAsync(Dictionary<string, object> ruleDefinition);

        /// <summary>
        /// Builds workflow action from action definition
        /// </summary>
        Task<string> BuildActionExpressionAsync(Dictionary<string, object> actionDefinition);

        /// <summary>
        /// Validates a rule definition
        /// </summary>
        Task<ValidationResult> ValidateRuleAsync(Dictionary<string, object> ruleDefinition);

        // New methods for WorkflowRule management
        /// <summary>
        /// Creates a new business rule
        /// </summary>
        Task<DataWorkflowRule> CreateRuleAsync(DataWorkflowRule rule);

        /// <summary>
        /// Gets all business rules with optional filtering
        /// </summary>
        Task<IEnumerable<DataWorkflowRule>> GetRulesAsync(string? entityType = null, bool? isActive = null);

        /// <summary>
        /// Gets a specific business rule by ID
        /// </summary>
        Task<DataWorkflowRule> GetRuleAsync(Guid id);

        /// <summary>
        /// Updates an existing business rule
        /// </summary>
        Task<DataWorkflowRule> UpdateRuleAsync(Guid id, DataWorkflowRule rule);

        /// <summary>
        /// Deletes a business rule
        /// </summary>
        Task DeleteRuleAsync(Guid id);

        /// <summary>
        /// Activates or deactivates a business rule
        /// </summary>
        Task ToggleRuleStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// Tests a business rule against sample data
        /// </summary>
        Task<RuleTestResult> TestRuleAsync(Guid id, Dictionary<string, object> testData);
    }

    /// <summary>
    /// Validation result for workflows and rules
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
