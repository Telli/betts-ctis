using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services.WorkflowEngine
{
    /// <summary>
    /// Interface for comprehensive webhook delivery service
    /// Handles webhook registration, event delivery, monitoring, and analytics
    /// </summary>
    public interface IWebhookDeliveryService
    {
        /// <summary>
        /// Registers a new webhook endpoint
        /// </summary>
        /// <param name="request">Webhook registration request</param>
        /// <returns>Registered webhook configuration</returns>
        Task<Result<WebhookRegistration>> RegisterWebhookAsync(RegisterWebhookRequest request);

        /// <summary>
        /// Sends an event to all registered webhooks
        /// </summary>
        /// <param name="request">Event broadcast request</param>
        /// <returns>Broadcast results including delivery status for each webhook</returns>
        Task<Result<WebhookBroadcastResult>> SendEventAsync(SendWebhookEventRequest request);

        /// <summary>
        /// Gets webhook registrations with filtering
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <returns>List of webhook registrations</returns>
        Task<Result<List<WebhookRegistration>>> GetWebhooksAsync(WebhookFilter? filter = null);

        /// <summary>
        /// Gets a specific webhook by ID
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <returns>Webhook registration details</returns>
        Task<Result<WebhookRegistration>> GetWebhookAsync(Guid webhookId);

        /// <summary>
        /// Gets webhook delivery history with pagination
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <param name="pageSize">Number of entries per page</param>
        /// <param name="pageNumber">Page number</param>
        /// <returns>Paginated delivery history</returns>
        Task<Result<PagedResult<WebhookDeliveryLog>>> GetDeliveryHistoryAsync(
            Guid webhookId, int pageSize = 50, int pageNumber = 1);

        /// <summary>
        /// Updates webhook configuration
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated webhook configuration</returns>
        Task<Result<WebhookRegistration>> UpdateWebhookAsync(Guid webhookId, UpdateWebhookRequest request);

        /// <summary>
        /// Deactivates or removes a webhook registration
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <returns>Success result</returns>
        Task<Result<bool>> DeleteWebhookAsync(Guid webhookId);

        /// <summary>
        /// Sends a test webhook to verify endpoint connectivity
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <returns>Test delivery result</returns>
        Task<Result<WebhookDeliveryResult>> SendTestWebhookAsync(Guid webhookId);

        /// <summary>
        /// Gets comprehensive webhook analytics and performance metrics
        /// </summary>
        /// <param name="webhookId">Optional specific webhook ID, null for all webhooks</param>
        /// <param name="period">Analytics time period, defaults to 30 days</param>
        /// <returns>Detailed analytics data</returns>
        Task<Result<WebhookAnalytics>> GetWebhookAnalyticsAsync(
            Guid? webhookId = null, TimeSpan? period = null);

        /// <summary>
        /// Validates webhook endpoint health and connectivity
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <returns>Health validation result</returns>
        Task<Result<WebhookValidationResult>> ValidateWebhookHealthAsync(Guid webhookId);

        /// <summary>
        /// Sends multiple events in a batch operation
        /// </summary>
        /// <param name="request">Batch delivery request</param>
        /// <returns>Batch delivery results</returns>
        Task<Result<BatchWebhookResult>> SendBatchEventsAsync(BatchWebhookRequest request);

        /// <summary>
        /// Gets available webhook event types with descriptions
        /// </summary>
        /// <returns>List of supported event types</returns>
        Task<Result<List<WebhookEventTypeInfo>>> GetAvailableEventTypesAsync();

        /// <summary>
        /// Regenerates webhook secret for enhanced security
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <returns>Updated webhook with new secret</returns>
        Task<Result<WebhookRegistration>> RegenerateWebhookSecretAsync(Guid webhookId);

        /// <summary>
        /// Gets webhook delivery statistics summary
        /// </summary>
        /// <param name="userId">Optional user ID filter</param>
        /// <param name="period">Statistics period</param>
        /// <returns>Delivery statistics summary</returns>
        Task<Result<WebhookDeliveryStatistics>> GetDeliveryStatisticsAsync(
            string? userId = null, TimeSpan? period = null);

        /// <summary>
        /// Retries failed webhook deliveries
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <param name="maxRetries">Maximum number of failed deliveries to retry</param>
        /// <returns>Retry operation results</returns>
        Task<Result<WebhookRetryResult>> RetryFailedDeliveriesAsync(Guid webhookId, int maxRetries = 10);

        /// <summary>
        /// Exports webhook configuration and history
        /// </summary>
        /// <param name="webhookId">Webhook identifier</param>
        /// <param name="options">Export options</param>
        /// <returns>Exported data in specified format</returns>
        Task<Result<string>> ExportWebhookDataAsync(Guid webhookId, WebhookExportOptions options);

        /// <summary>
        /// Imports webhook configuration from exported data
        /// </summary>
        /// <param name="importData">Exported webhook data</param>
        /// <param name="options">Import options</param>
        /// <returns>Imported webhook registration</returns>
        Task<Result<WebhookRegistration>> ImportWebhookDataAsync(string importData, WebhookImportOptions options);

        /// <summary>
        /// Sets up webhook templates for common scenarios
        /// </summary>
        /// <param name="templateType">Type of webhook template</param>
        /// <param name="request">Basic webhook information</param>
        /// <returns>Created webhook from template</returns>
        Task<Result<WebhookRegistration>> CreateWebhookFromTemplateAsync(
            WebhookTemplateType templateType, RegisterWebhookRequest request);

        /// <summary>
        /// Gets webhook templates available for quick setup
        /// </summary>
        /// <returns>List of available webhook templates</returns>
        Task<Result<List<WebhookTemplate>>> GetWebhookTemplatesAsync();
    }

    /// <summary>
    /// Webhook event type information
    /// </summary>
    public class WebhookEventTypeInfo
    {
        public string EventType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> SampleFields { get; set; } = new();
        public string SamplePayload { get; set; } = string.Empty;
        public bool IsCustom { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Webhook delivery statistics summary
    /// </summary>
    public class WebhookDeliveryStatistics
    {
        public TimeSpan Period { get; set; }
        public int TotalWebhooks { get; set; }
        public int ActiveWebhooks { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageResponseTime { get; set; }
        public Dictionary<string, int> EventTypeDistribution { get; set; } = new();
        public Dictionary<string, double> EndpointPerformance { get; set; } = new();
        public List<WebhookHealthSummary> HealthSummary { get; set; } = new();
    }

    /// <summary>
    /// Webhook health summary
    /// </summary>
    public class WebhookHealthSummary
    {
        public Guid WebhookId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public WebhookHealthStatus HealthStatus { get; set; }
        public double SuccessRate { get; set; }
        public DateTime? LastSuccessfulDelivery { get; set; }
        public DateTime? LastFailedDelivery { get; set; }
        public int ConsecutiveFailures { get; set; }
    }

    /// <summary>
    /// Webhook retry operation result
    /// </summary>
    public class WebhookRetryResult
    {
        public Guid WebhookId { get; set; }
        public int TotalRetryAttempts { get; set; }
        public int SuccessfulRetries { get; set; }
        public int FailedRetries { get; set; }
        public List<WebhookDeliveryResult> RetryResults { get; set; } = new();
        public TimeSpan TotalRetryTime { get; set; }
    }

    /// <summary>
    /// Webhook export options
    /// </summary>
    public class WebhookExportOptions
    {
        public bool IncludeConfiguration { get; set; } = true;
        public bool IncludeStatistics { get; set; } = true;
        public bool IncludeDeliveryHistory { get; set; } = false;
        public int MaxHistoryEntries { get; set; } = 1000;
        public string ExportFormat { get; set; } = "json"; // json, csv, xml
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Webhook import options
    /// </summary>
    public class WebhookImportOptions
    {
        public bool OverwriteExisting { get; set; } = false;
        public bool ValidateOnImport { get; set; } = true;
        public bool PreserveIds { get; set; } = false;
        public bool ImportStatistics { get; set; } = false;
        public string? NewUserId { get; set; }
        public Dictionary<string, string>? UrlMappings { get; set; }
    }

    /// <summary>
    /// Webhook template types
    /// </summary>
    public enum WebhookTemplateType
    {
        BasicNotifications = 1,
        ComplianceAlerts = 2,
        DocumentProcessing = 3,
        PaymentNotifications = 4,
        SystemMonitoring = 5,
        UserActivity = 6,
        WorkflowEvents = 7,
        IntegrationSync = 8,
        CustomDashboard = 9,
        SecurityAlerts = 10
    }

    /// <summary>
    /// Webhook template definition
    /// </summary>
    public class WebhookTemplate
    {
        public WebhookTemplateType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> DefaultEventTypes { get; set; } = new();
        public WebhookSettings DefaultSettings { get; set; } = new();
        public WebhookSecuritySettings DefaultSecuritySettings { get; set; } = new();
        public List<WebhookFilter> SampleFilters { get; set; } = new();
        public string DocumentationUrl { get; set; } = string.Empty;
        public List<string> UseCase { get; set; } = new();
        public Dictionary<string, object> TemplateMetadata { get; set; } = new();
    }

    /// <summary>
    /// Webhook endpoint validation
    /// </summary>
    public class WebhookEndpointValidation
    {
        public bool IsReachable { get; set; }
        public bool AcceptsHttps { get; set; }
        public bool RespondsToPost { get; set; }
        public bool ValidatesSignature { get; set; }
        public bool HasValidSSLCertificate { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public Dictionary<string, object> EndpointInfo { get; set; } = new();
    }

    /// <summary>
    /// Webhook security audit result
    /// </summary>
    public class WebhookSecurityAuditResult
    {
        public Guid WebhookId { get; set; }
        public SecurityRiskLevel RiskLevel { get; set; }
        public List<SecurityIssue> SecurityIssues { get; set; } = new();
        public List<SecurityRecommendation> Recommendations { get; set; } = new();
        public DateTime AuditedAt { get; set; } = DateTime.UtcNow;
        public string AuditVersion { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Security risk levels
    /// </summary>
    public enum SecurityRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Security issue details
    /// </summary>
    public class SecurityIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecurityRiskLevel Severity { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> AffectedComponents { get; set; } = new();
    }

    /// <summary>
    /// Security recommendation
    /// </summary>
    public class SecurityRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public List<string> Benefits { get; set; } = new();
    }
}