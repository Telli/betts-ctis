using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Service for managing workflow rules and conditions
    /// </summary>
    public interface IWorkflowRuleService
    {
        /// <summary>
        /// Create a new workflow rule
        /// </summary>
        Task<Result<WorkflowRuleDto>> CreateRuleAsync(CreateWorkflowRuleDto request);

        /// <summary>
        /// Update an existing workflow rule
        /// </summary>
        Task<Result<WorkflowRuleDto>> UpdateRuleAsync(int ruleId, UpdateWorkflowRuleDto request);

        /// <summary>
        /// Delete a workflow rule
        /// </summary>
        Task<Result> DeleteRuleAsync(int ruleId);

        /// <summary>
        /// Get a workflow rule by ID
        /// </summary>
        Task<Result<WorkflowRuleDto>> GetRuleAsync(int ruleId);

        /// <summary>
        /// Get all workflow rules with filtering and pagination
        /// </summary>
        Task<Result<BettsTax.Shared.PagedResult<WorkflowRuleDto>>> GetRulesAsync(WorkflowRuleFilterDto filter);

        /// <summary>
        /// Test a workflow rule against sample data
        /// </summary>
        Task<Result<WorkflowRuleTestResultDto>> TestRuleAsync(int ruleId, object testData);

        /// <summary>
        /// Evaluate workflow rules for a specific trigger
        /// </summary>
        Task<Result<List<WorkflowActionDto>>> EvaluateRulesAsync(string triggerType, object data);

        /// <summary>
        /// Get available trigger types for rule creation
        /// </summary>
        Task<Result<List<WorkflowTriggerTypeDto>>> GetAvailableTriggersAsync();

        /// <summary>
        /// Get available condition types for a trigger
        /// </summary>
        Task<Result<List<WorkflowConditionTypeDto>>> GetConditionTypesAsync(string triggerType);

        /// <summary>
        /// Get available action types for workflows
        /// </summary>
        Task<Result<List<WorkflowActionTypeDto>>> GetActionTypesAsync();

        /// <summary>
        /// Clone an existing workflow rule
        /// </summary>
        Task<Result<WorkflowRuleDto>> CloneRuleAsync(int ruleId, string newName);

        /// <summary>
        /// Enable or disable a workflow rule
        /// </summary>
        Task<Result> ToggleRuleStatusAsync(int ruleId, bool isActive);

        /// <summary>
        /// Get workflow rule execution history
        /// </summary>
        Task<Result<BettsTax.Shared.PagedResult<WorkflowExecutionHistoryDto>>> GetExecutionHistoryAsync(
            WorkflowExecutionHistoryFilterDto filter);
    }

    /// <summary>
    /// Service for managing workflow templates
    /// </summary>
    public interface IWorkflowTemplateService
    {
        /// <summary>
        /// Create a new workflow template
        /// </summary>
        Task<Result<WorkflowTemplateDto>> CreateTemplateAsync(CreateWorkflowTemplateDto request);

        /// <summary>
        /// Update a workflow template
        /// </summary>
        Task<Result<WorkflowTemplateDto>> UpdateTemplateAsync(int templateId, UpdateWorkflowTemplateDto request);

        /// <summary>
        /// Delete a workflow template
        /// </summary>
        Task<Result> DeleteTemplateAsync(int templateId);

        /// <summary>
        /// Get a workflow template by ID
        /// </summary>
        Task<Result<WorkflowTemplateDto>> GetTemplateAsync(int templateId);

        /// <summary>
        /// Get all workflow templates with filtering
        /// </summary>
        Task<Result<BettsTax.Shared.PagedResult<WorkflowTemplateDto>>> GetTemplatesAsync(WorkflowTemplateFilterDto filter);

        /// <summary>
        /// Create workflow rule from template
        /// </summary>
        Task<Result<WorkflowRuleDto>> CreateRuleFromTemplateAsync(int templateId, CreateRuleFromTemplateDto request);

        /// <summary>
        /// Get available template categories
        /// </summary>
        Task<Result<List<string>>> GetTemplateCategoriesAsync();

        /// <summary>
        /// Import workflow template from JSON
        /// </summary>
        Task<Result<WorkflowTemplateDto>> ImportTemplateAsync(ImportWorkflowTemplateDto request);

        /// <summary>
        /// Export workflow template to JSON
        /// </summary>
        Task<Result<string>> ExportTemplateAsync(int templateId);
    }

    /// <summary>
    /// Service for executing workflow rules
    /// </summary>
    public interface IWorkflowExecutionService
    {
        /// <summary>
        /// Execute workflow rules for a trigger event
        /// </summary>
        Task<Result<WorkflowExecutionResultDto>> ExecuteWorkflowAsync(WorkflowTriggerEventDto triggerEvent);

        /// <summary>
        /// Execute a specific workflow rule
        /// </summary>
        Task<Result<WorkflowExecutionResultDto>> ExecuteRuleAsync(int ruleId, object data);

        /// <summary>
        /// Get execution status for a workflow run
        /// </summary>
        Task<Result<WorkflowExecutionStatusDto>> GetExecutionStatusAsync(string executionId);

        /// <summary>
        /// Cancel a running workflow execution
        /// </summary>
        Task<Result> CancelExecutionAsync(string executionId);

        /// <summary>
        /// Retry a failed workflow execution
        /// </summary>
        Task<Result<WorkflowExecutionResultDto>> RetryExecutionAsync(string executionId);

        /// <summary>
        /// Get workflow execution metrics
        /// </summary>
        Task<Result<WorkflowMetricsDto>> GetWorkflowMetricsAsync(WorkflowMetricsFilterDto filter);
    }
}