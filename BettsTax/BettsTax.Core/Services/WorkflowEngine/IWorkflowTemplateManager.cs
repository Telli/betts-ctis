using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services.WorkflowEngine
{
    /// <summary>
    /// Interface for comprehensive workflow template management
    /// </summary>
    public interface IWorkflowTemplateManager
    {
        /// <summary>
        /// Creates a new workflow template from an existing workflow
        /// </summary>
        /// <param name="request">Template creation request</param>
        /// <returns>Created workflow template</returns>
        Task<Result<WorkflowTemplate>> CreateTemplateFromWorkflowAsync(CreateTemplateRequest request);

        /// <summary>
        /// Creates a new workflow from a template
        /// </summary>
        /// <param name="request">Workflow creation request</param>
        /// <returns>Created workflow ID</returns>
        Task<Result<Guid>> CreateWorkflowFromTemplateAsync(CreateFromTemplateRequest request);

        /// <summary>
        /// Gets templates with filtering and pagination
        /// </summary>
        /// <param name="filter">Template filter criteria</param>
        /// <returns>Paged template results</returns>
        Task<Result<PagedTemplateResult>> GetTemplatesAsync(TemplateFilter filter);

        /// <summary>
        /// Gets a specific template by ID
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <returns>Workflow template</returns>
        Task<Result<WorkflowTemplate>> GetTemplateAsync(Guid templateId);

        /// <summary>
        /// Updates template information
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated template</returns>
        Task<Result<WorkflowTemplate>> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request);

        /// <summary>
        /// Deletes or deactivates a template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="hardDelete">Whether to permanently delete or just deactivate</param>
        /// <returns>Success result</returns>
        Task<Result<bool>> DeleteTemplateAsync(Guid templateId, bool hardDelete = false);

        /// <summary>
        /// Adds a review to a template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="request">Review request</param>
        /// <returns>Created review</returns>
        Task<Result<TemplateReview>> AddReviewAsync(Guid templateId, AddReviewRequest request);

        /// <summary>
        /// Gets template categories with usage statistics
        /// </summary>
        /// <returns>List of template categories</returns>
        Task<Result<List<TemplateCategoryInfo>>> GetCategoriesAsync();

        /// <summary>
        /// Gets popular templates based on usage and ratings
        /// </summary>
        /// <param name="count">Number of templates to return</param>
        /// <returns>List of popular templates</returns>
        Task<Result<List<WorkflowTemplate>>> GetPopularTemplatesAsync(int count = 10);

        /// <summary>
        /// Validates template parameters before workflow creation
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Validation result</returns>
        Task<Result<TemplateParameterValidationResult>> ValidateParametersAsync(
            Guid templateId, Dictionary<string, object> parameters);

        /// <summary>
        /// Exports template to JSON format
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="options">Export options</param>
        /// <returns>JSON representation of template</returns>
        Task<Result<string>> ExportTemplateAsync(Guid templateId, TemplateExportOptions options);
    }

    /// <summary>
    /// Request to create template from workflow
    /// </summary>
    public class CreateTemplateRequest
    {
        public Guid SourceWorkflowId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, ParameterMapping>? ParameterMappings { get; set; }
        public string? CreationContext { get; set; }
        public List<string>? Screenshots { get; set; }
        public string? Documentation { get; set; }
    }

    /// <summary>
    /// Parameter mapping configuration
    /// </summary>
    public class ParameterMapping
    {
        public string ParameterName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public ParameterValidation? Validation { get; set; }
        public List<PropertyOption>? Options { get; set; }
    }

    /// <summary>
    /// Parameter validation rules
    /// </summary>
    public class ParameterValidation
    {
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? Pattern { get; set; }
        public List<object>? AllowedValues { get; set; }
        public string? CustomValidator { get; set; }
    }

    /// <summary>
    /// Property option for dropdowns and selections
    /// </summary>
    public class PropertyOption
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Request to create workflow from template
    /// </summary>
    public class CreateFromTemplateRequest
    {
        public Guid TemplateId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Template filter criteria
    /// </summary>
    public class TemplateFilter
    {
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public List<string>? Tags { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsPublic { get; set; }
        public double? MinRating { get; set; }
        public ComplexityLevel? ComplexityLevel { get; set; }
        public string? SortBy { get; set; } = "created";
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Sort direction enumeration
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Template update request
    /// </summary>
    public class UpdateTemplateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsPublic { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Review addition request
    /// </summary>
    public class AddReviewRequest
    {
        public string ReviewerId { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Paged template results
    /// </summary>
    public class PagedTemplateResult
    {
        public List<WorkflowTemplate> Templates { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    /// <summary>
    /// Template category information
    /// </summary>
    public class TemplateCategoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageRating { get; set; }
        public int TotalUsage { get; set; }
    }

    /// <summary>
    /// Template parameter validation result
    /// </summary>
    public class TemplateParameterValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, object> ValidatedParameters { get; set; } = new();
    }

    /// <summary>
    /// Template export options
    /// </summary>
    public class TemplateExportOptions
    {
        public bool IncludeConfiguration { get; set; } = true;
        public bool IncludeStatistics { get; set; } = false;
        public bool IncludeReviews { get; set; } = false;
        public bool IncludeMetadata { get; set; } = true;
        public string? ExportFormat { get; set; } = "json";
    }

    /// <summary>
    /// Complexity level enumeration
    /// </summary>
    public enum ComplexityLevel
    {
        Simple = 1,
        Medium = 2,
        Complex = 3,
        Advanced = 4
    }

    /// <summary>
    /// Workflow metadata class
    /// </summary>
    public class WorkflowMetadata
    {
        public Guid? CreatedFromTemplateId { get; set; }
        public string? TemplateVersion { get; set; }
        public Dictionary<string, object> CustomParameters { get; set; } = new();
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public Dictionary<string, object> RuntimeStatistics { get; set; } = new();
    }
}