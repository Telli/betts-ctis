using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Webhook registration entity
    /// </summary>
    public class WebhookRegistration
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> EventTypes { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public WebhookSettings Settings { get; set; } = new();
        public WebhookSecuritySettings SecuritySettings { get; set; } = new();
        public WebhookStatistics? Statistics { get; set; }
    }

    /// <summary>
    /// Webhook configuration settings
    /// </summary>
    public class WebhookSettings
    {
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 60;
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeUserData { get; set; } = false;
        public string ContentType { get; set; } = "application/json";
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
        public List<WebhookFilter> FilterConditions { get; set; } = new();
    }

    /// <summary>
    /// Webhook security settings
    /// </summary>
    public class WebhookSecuritySettings
    {
        public bool RequireSignature { get; set; } = true;
        public string SignatureHeader { get; set; } = "X-Webhook-Signature";
        public List<string> AllowedIpRanges { get; set; } = new();
        public int RateLimitPerHour { get; set; } = 1000;
        public bool ValidateSSL { get; set; } = true;
        public List<string> AllowedUserAgents { get; set; } = new();
    }

    /// <summary>
    /// Webhook delivery statistics
    /// </summary>
    public class WebhookStatistics
    {
        public Guid WebhookId { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public DateTime? LastDeliveryAt { get; set; }
        public double AverageResponseTime { get; set; }
        public double SuccessRate => TotalDeliveries > 0 ? (double)SuccessfulDeliveries / TotalDeliveries * 100 : 0;
        public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Webhook filter condition
    /// </summary>
    public class WebhookFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = "equals"; // equals, not_equals, contains, starts_with, ends_with, greater_than, less_than, exists, not_exists
        public object? Value { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Webhook delivery log entry
    /// </summary>
    public class WebhookDeliveryLog
    {
        public Guid Id { get; set; }
        public Guid WebhookId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequestPayload { get; set; }
        public string? ResponseBody { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Request to register new webhook
    /// </summary>
    public class RegisterWebhookRequest
    {
        public string Url { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? EventTypes { get; set; }
        public bool TestOnRegistration { get; set; } = true;

        // Settings
        public int? TimeoutSeconds { get; set; }
        public int? MaxRetryAttempts { get; set; }
        public int? RetryDelaySeconds { get; set; }
        public bool? IncludeHeaders { get; set; }
        public bool? IncludeUserData { get; set; }
        public string? ContentType { get; set; }
        public Dictionary<string, string>? CustomHeaders { get; set; }
        public List<WebhookFilter>? FilterConditions { get; set; }

        // Security settings
        public bool? RequireSignature { get; set; }
        public string? SignatureHeader { get; set; }
        public List<string>? AllowedIpRanges { get; set; }
        public int? RateLimitPerHour { get; set; }
    }

    /// <summary>
    /// Request to update webhook
    /// </summary>
    public class UpdateWebhookRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? EventTypes { get; set; }
        public bool? IsActive { get; set; }
        public WebhookSettings? Settings { get; set; }
        public WebhookSecuritySettings? SecuritySettings { get; set; }
    }

    /// <summary>
    /// Request to send webhook event
    /// </summary>
    public class SendWebhookEventRequest
    {
        public string EventType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string>? TargetWebhooks { get; set; } // Optional: specific webhooks to target
        public int Priority { get; set; } = 0; // 0 = normal, 1 = high, 2 = critical
    }

    /// <summary>
    /// Webhook delivery result
    /// </summary>
    public class WebhookDeliveryResult
    {
        public Guid WebhookId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResponseBody { get; set; }
        public int AttemptNumber { get; set; } = 1;
    }

    /// <summary>
    /// Webhook broadcast result
    /// </summary>
    public class WebhookBroadcastResult
    {
        public string EventType { get; set; } = string.Empty;
        public int TotalWebhooks { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public List<WebhookDeliveryResult> DeliveryResults { get; set; } = new();
        public TimeSpan TotalProcessingTime { get; set; }
        public DateTime BroadcastAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Webhook analytics data
    /// </summary>
    public class WebhookAnalytics
    {
        public TimeSpan Period { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageResponseTime { get; set; }
        public double SuccessRate => TotalDeliveries > 0 ? (double)SuccessfulDeliveries / TotalDeliveries * 100 : 0;
        public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
        public Dictionary<string, int> StatusCodeBreakdown { get; set; } = new();
        public Dictionary<string, int> TopFailureReasons { get; set; } = new();
        public List<DailyWebhookStats> DailyStats { get; set; } = new();
        public List<HourlyWebhookStats> HourlyStats { get; set; } = new();
        public WebhookPerformanceMetrics Performance { get; set; } = new();
    }

    /// <summary>
    /// Daily webhook statistics
    /// </summary>
    public class DailyWebhookStats
    {
        public DateTime Date { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageResponseTime { get; set; }
        public Dictionary<string, int> EventTypes { get; set; } = new();
    }

    /// <summary>
    /// Hourly webhook statistics
    /// </summary>
    public class HourlyWebhookStats
    {
        public DateTime Hour { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageResponseTime { get; set; }
    }

    /// <summary>
    /// Webhook performance metrics
    /// </summary>
    public class WebhookPerformanceMetrics
    {
        public double P50ResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public int FastestResponseTime { get; set; }
        public int SlowestResponseTime { get; set; }
        public double UptimePercentage { get; set; }
        public int ConsecutiveFailures { get; set; }
        public DateTime? LastFailureAt { get; set; }
        public Dictionary<string, double> EndpointPerformance { get; set; } = new();
    }

    /// <summary>
    /// Webhook configuration options
    /// </summary>
    public class WebhookOptions
    {
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int DefaultMaxRetryAttempts { get; set; } = 3;
        public int DefaultRetryDelaySeconds { get; set; } = 60;
        public int DefaultRateLimitPerHour { get; set; } = 1000;
        public bool AllowInsecureUrls { get; set; } = false;
        public int RetentionDays { get; set; } = 90;
        public int MaxPayloadSize { get; set; } = 1048576; // 1MB
        public List<string> AllowedDomains { get; set; } = new();
        public List<string> BlockedDomains { get; set; } = new();
        public bool EnableDeliveryTracking { get; set; } = true;
        public bool EnableAnalytics { get; set; } = true;
        public string UserAgent { get; set; } = "BettsTax-Webhook/1.0";
    }

    /// <summary>
    /// Webhook event types enumeration
    /// </summary>
    public static class WebhookEventTypes
    {
        // System events
        public const string SystemStartup = "system.startup";
        public const string SystemShutdown = "system.shutdown";
        public const string SystemError = "system.error";

        // User events
        public const string UserCreated = "user.created";
        public const string UserUpdated = "user.updated";
        public const string UserDeleted = "user.deleted";
        public const string UserLogin = "user.login";
        public const string UserLogout = "user.logout";

        // Client events
        public const string ClientCreated = "client.created";
        public const string ClientUpdated = "client.updated";
        public const string ClientDeleted = "client.deleted";
        public const string ClientStatusChanged = "client.status_changed";

        // Document events
        public const string DocumentUploaded = "document.uploaded";
        public const string DocumentProcessed = "document.processed";
        public const string DocumentVerified = "document.verified";
        public const string DocumentRejected = "document.rejected";

        // Workflow events
        public const string WorkflowStarted = "workflow.started";
        public const string WorkflowCompleted = "workflow.completed";
        public const string WorkflowFailed = "workflow.failed";
        public const string WorkflowStepCompleted = "workflow.step_completed";

        // Payment events
        public const string PaymentReceived = "payment.received";
        public const string PaymentFailed = "payment.failed";
        public const string PaymentRefunded = "payment.refunded";

        // Compliance events
        public const string ComplianceDeadline = "compliance.deadline";
        public const string ComplianceCompleted = "compliance.completed";
        public const string ComplianceOverdue = "compliance.overdue";

        // Notification events
        public const string NotificationSent = "notification.sent";
        public const string NotificationFailed = "notification.failed";

        // Integration events
        public const string IntegrationConnected = "integration.connected";
        public const string IntegrationDisconnected = "integration.disconnected";
        public const string IntegrationSync = "integration.sync";
        public const string IntegrationError = "integration.error";

        // Custom events
        public const string Custom = "custom";
        public const string Test = "webhook.test";
        public const string All = "*";

        /// <summary>
        /// Gets all available event types
        /// </summary>
        public static List<string> GetAllEventTypes()
        {
            return new List<string>
            {
                SystemStartup, SystemShutdown, SystemError,
                UserCreated, UserUpdated, UserDeleted, UserLogin, UserLogout,
                ClientCreated, ClientUpdated, ClientDeleted, ClientStatusChanged,
                DocumentUploaded, DocumentProcessed, DocumentVerified, DocumentRejected,
                WorkflowStarted, WorkflowCompleted, WorkflowFailed, WorkflowStepCompleted,
                PaymentReceived, PaymentFailed, PaymentRefunded,
                ComplianceDeadline, ComplianceCompleted, ComplianceOverdue,
                NotificationSent, NotificationFailed,
                IntegrationConnected, IntegrationDisconnected, IntegrationSync, IntegrationError,
                Custom, Test, All
            };
        }

        /// <summary>
        /// Gets event types by category
        /// </summary>
        public static Dictionary<string, List<string>> GetEventTypesByCategory()
        {
            return new Dictionary<string, List<string>>
            {
                ["System"] = new() { SystemStartup, SystemShutdown, SystemError },
                ["User"] = new() { UserCreated, UserUpdated, UserDeleted, UserLogin, UserLogout },
                ["Client"] = new() { ClientCreated, ClientUpdated, ClientDeleted, ClientStatusChanged },
                ["Document"] = new() { DocumentUploaded, DocumentProcessed, DocumentVerified, DocumentRejected },
                ["Workflow"] = new() { WorkflowStarted, WorkflowCompleted, WorkflowFailed, WorkflowStepCompleted },
                ["Payment"] = new() { PaymentReceived, PaymentFailed, PaymentRefunded },
                ["Compliance"] = new() { ComplianceDeadline, ComplianceCompleted, ComplianceOverdue },
                ["Notification"] = new() { NotificationSent, NotificationFailed },
                ["Integration"] = new() { IntegrationConnected, IntegrationDisconnected, IntegrationSync, IntegrationError },
                ["Special"] = new() { Custom, Test, All }
            };
        }
    }

    /// <summary>
    /// Webhook validation result
    /// </summary>
    public class WebhookValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public WebhookHealthStatus HealthStatus { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
        public TimeSpan? ResponseTime { get; set; }
    }

    /// <summary>
    /// Webhook health status
    /// </summary>
    public enum WebhookHealthStatus
    {
        Healthy = 0,
        Warning = 1,
        Critical = 2,
        Unreachable = 3,
        Unknown = 4
    }

    /// <summary>
    /// Webhook retry policy
    /// </summary>
    public class WebhookRetryPolicy
    {
        public int MaxAttempts { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromHours(1);
        public double BackoffMultiplier { get; set; } = 2.0;
        public bool EnableJitter { get; set; } = true;
        public List<int> RetriableStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };
        public List<string> RetriableErrors { get; set; } = new() { "timeout", "connection", "network" };
    }

    /// <summary>
    /// Webhook batch delivery request
    /// </summary>
    public class BatchWebhookRequest
    {
        public List<SendWebhookEventRequest> Events { get; set; } = new();
        public int MaxConcurrentDeliveries { get; set; } = 10;
        public bool StopOnFirstFailure { get; set; } = false;
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<string, object> BatchMetadata { get; set; } = new();
    }

    /// <summary>
    /// Webhook batch delivery result
    /// </summary>
    public class BatchWebhookResult
    {
        public int TotalEvents { get; set; }
        public int SuccessfulEvents { get; set; }
        public int FailedEvents { get; set; }
        public List<WebhookBroadcastResult> Results { get; set; } = new();
        public TimeSpan TotalProcessingTime { get; set; }
        public Dictionary<string, object> BatchMetadata { get; set; } = new();
    }
}