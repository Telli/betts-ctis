using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Workflow template data transfer object
    /// </summary>
    public class WorkflowTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public int UsageCount { get; set; }
        public double Rating { get; set; }
        public List<string> Tags { get; set; } = new();
        public WorkflowTemplateDefinitionDto Definition { get; set; } = new();
    }

    /// <summary>
    /// Create workflow template request
    /// </summary>
    public class CreateWorkflowTemplateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

        public bool IsPublic { get; set; }
        public List<string> Tags { get; set; } = new();
        public WorkflowTemplateDefinitionDto Definition { get; set; } = new();
    }

    /// <summary>
    /// Update workflow template request
    /// </summary>
    public class UpdateWorkflowTemplateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public bool IsPublic { get; set; }
        public List<string> Tags { get; set; } = new();
        public WorkflowTemplateDefinitionDto Definition { get; set; } = new();
    }

    /// <summary>
    /// Workflow template definition
    /// </summary>
    public class WorkflowTemplateDefinitionDto
    {
        public List<WorkflowTemplateConditionDto> Conditions { get; set; } = new();
        public List<WorkflowTemplateActionDto> Actions { get; set; } = new();
        public Dictionary<string, WorkflowTemplateParameterDto> Parameters { get; set; } = new();
        public WorkflowTemplateMetadataDto Metadata { get; set; } = new();
    }

    /// <summary>
    /// Workflow template condition
    /// </summary>
    public class WorkflowTemplateConditionDto
    {
        public string ConditionType { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string LogicalOperator { get; set; } = "AND";
        public int Order { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsParameterized { get; set; }
        public string? ParameterName { get; set; }
    }

    /// <summary>
    /// Workflow template action
    /// </summary>
    public class WorkflowTemplateActionDto
    {
        public string ActionType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int Order { get; set; }
        public bool ContinueOnError { get; set; }
        public string? ErrorHandling { get; set; }
        public Dictionary<string, string> ParameterMappings { get; set; } = new();
    }

    /// <summary>
    /// Workflow template parameter definition
    /// </summary>
    public class WorkflowTemplateParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<object>? AllowedValues { get; set; }
        public Dictionary<string, object> Validation { get; set; } = new();
        public string? Group { get; set; }
        public int Order { get; set; }
    }

    /// <summary>
    /// Workflow template metadata
    /// </summary>
    public class WorkflowTemplateMetadataDto
    {
        public string Version { get; set; } = "1.0";
        public string? Documentation { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public Dictionary<string, string> Examples { get; set; } = new();
        public List<WorkflowTemplateVariationDto> Variations { get; set; } = new();
    }

    /// <summary>
    /// Workflow template variation
    /// </summary>
    public class WorkflowTemplateVariationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> ParameterOverrides { get; set; } = new();
    }

    /// <summary>
    /// Workflow template filter for queries
    /// </summary>
    public class WorkflowTemplateFilterDto
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? TriggerType { get; set; }
        public bool? IsPublic { get; set; }
        public string? CreatedBy { get; set; }
        public List<string>? Tags { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public double? MinRating { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    /// <summary>
    /// Create rule from template request
    /// </summary>
    public class CreateRuleFromTemplateDto
    {
        [Required, MaxLength(200)]
        public string RuleName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 1000)]
        public int Priority { get; set; } = 100;

        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Import workflow template request
    /// </summary>
    public class ImportWorkflowTemplateDto
    {
        [Required]
        public string JsonData { get; set; } = string.Empty;

        public bool OverwriteExisting { get; set; }

        public string? NewName { get; set; }

        public string? NewCategory { get; set; }
    }

    /// <summary>
    /// Workflow template usage statistics
    /// </summary>
    public class WorkflowTemplateUsageDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TotalUsage { get; set; }
        public int ActiveRules { get; set; }
        public DateTime? LastUsed { get; set; }
        public List<WorkflowTemplateUsageDetailDto> RecentUsage { get; set; } = new();
    }

    /// <summary>
    /// Workflow template usage detail
    /// </summary>
    public class WorkflowTemplateUsageDetailDto
    {
        public string RuleName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Workflow template rating request
    /// </summary>
    public class RateWorkflowTemplateDto
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Workflow template review
    /// </summary>
    public class WorkflowTemplateReviewDto
    {
        public string ReviewId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Workflow template category statistics
    /// </summary>
    public class WorkflowTemplateCategoryStatsDto
    {
        public string Category { get; set; } = string.Empty;
        public int TemplateCount { get; set; }
        public int TotalUsage { get; set; }
        public double AverageRating { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    /// <summary>
    /// Predefined workflow template categories
    /// </summary>
    public static class WorkflowTemplateCategories
    {
        public const string TaxFiling = "Tax Filing";
        public const string PaymentProcessing = "Payment Processing";
        public const string DocumentManagement = "Document Management";
        public const string ComplianceMonitoring = "Compliance Monitoring";
        public const string ClientCommunication = "Client Communication";
        public const string ReportGeneration = "Report Generation";
        public const string DataValidation = "Data Validation";
        public const string NotificationManagement = "Notification Management";
        public const string WorkflowApproval = "Workflow Approval";
        public const string SystemMaintenance = "System Maintenance";
        public const string Integration = "Integration";
        public const string Custom = "Custom";

        public static readonly List<string> AllCategories = new()
        {
            TaxFiling,
            PaymentProcessing,
            DocumentManagement,
            ComplianceMonitoring,
            ClientCommunication,
            ReportGeneration,
            DataValidation,
            NotificationManagement,
            WorkflowApproval,
            SystemMaintenance,
            Integration,
            Custom
        };
    }

    /// <summary>
    /// Common workflow trigger types
    /// </summary>
    public static class WorkflowTriggerTypes
    {
        public const string TaxFilingCreated = "TaxFilingCreated";
        public const string TaxFilingUpdated = "TaxFilingUpdated";
        public const string TaxFilingSubmitted = "TaxFilingSubmitted";
        public const string PaymentReceived = "PaymentReceived";
        public const string PaymentFailed = "PaymentFailed";
        public const string DocumentUploaded = "DocumentUploaded";
        public const string DocumentApproved = "DocumentApproved";
        public const string DocumentRejected = "DocumentRejected";
        public const string DeadlineApproaching = "DeadlineApproaching";
        public const string ComplianceStatusChanged = "ComplianceStatusChanged";
        public const string ClientRegistered = "ClientRegistered";
        public const string ClientStatusChanged = "ClientStatusChanged";
        public const string SystemAlert = "SystemAlert";
        public const string ScheduledTask = "ScheduledTask";
        public const string DataImported = "DataImported";
        public const string ReportGenerated = "ReportGenerated";

        public static readonly List<string> AllTriggerTypes = new()
        {
            TaxFilingCreated,
            TaxFilingUpdated,
            TaxFilingSubmitted,
            PaymentReceived,
            PaymentFailed,
            DocumentUploaded,
            DocumentApproved,
            DocumentRejected,
            DeadlineApproaching,
            ComplianceStatusChanged,
            ClientRegistered,
            ClientStatusChanged,
            SystemAlert,
            ScheduledTask,
            DataImported,
            ReportGenerated
        };
    }

    /// <summary>
    /// Common workflow action types
    /// </summary>
    public static class WorkflowActionTypes
    {
        public const string SendEmail = "SendEmail";
        public const string SendSms = "SendSms";
        public const string SendNotification = "SendNotification";
        public const string CreateTask = "CreateTask";
        public const string UpdateStatus = "UpdateStatus";
        public const string GenerateReport = "GenerateReport";
        public const string CallWebhook = "CallWebhook";
        public const string CreateDocument = "CreateDocument";
        public const string SendReminder = "SendReminder";
        public const string LogEvent = "LogEvent";
        public const string UpdateRecord = "UpdateRecord";
        public const string TriggerWorkflow = "TriggerWorkflow";
        public const string WaitForApproval = "WaitForApproval";
        public const string CalculatePenalty = "CalculatePenalty";
        public const string SyncExternalSystem = "SyncExternalSystem";

        public static readonly List<string> AllActionTypes = new()
        {
            SendEmail,
            SendSms,
            SendNotification,
            CreateTask,
            UpdateStatus,
            GenerateReport,
            CallWebhook,
            CreateDocument,
            SendReminder,
            LogEvent,
            UpdateRecord,
            TriggerWorkflow,
            WaitForApproval,
            CalculatePenalty,
            SyncExternalSystem
        };
    }
}