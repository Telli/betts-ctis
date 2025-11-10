using System;
using System.Collections.Generic;

namespace BettsTax.Data
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
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public WebhookStatistics? Statistics { get; set; }
        public List<WebhookDeliveryLog> DeliveryLogs { get; set; } = new();
    }

    /// <summary>
    /// Webhook delivery statistics
    /// </summary>
    public class WebhookStatistics
    {
        public Guid Id { get; set; }
        public Guid WebhookId { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public DateTime? LastDeliveryAt { get; set; }
        public double AverageResponseTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public WebhookRegistration Webhook { get; set; } = null!;
    }

    /// <summary>
    /// Webhook delivery log
    /// </summary>
    public class WebhookDeliveryLog
    {
        public Guid Id { get; set; }
        public Guid WebhookId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public bool Success { get; set; }
        public int? StatusCode { get; set; }
        public double? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequestPayload { get; set; }
        public string? ResponseBody { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }

        // Navigation properties
        public WebhookRegistration Webhook { get; set; } = null!;
    }

    /// <summary>
    /// Template review entity
    /// </summary>
    public class TemplateReview
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsVerified { get; set; }

        // Navigation properties  
        public Models.WorkflowTemplate Template { get; set; } = null!;
        public ApplicationUser Reviewer { get; set; } = null!;
    }

    /// <summary>
    /// Template statistics
    /// </summary>
    public class TemplateStatistics
    {
        public Guid Id { get; set; }
        public Guid TemplateId { get; set; }
        public int UsageCount { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public double PopularityScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Models.WorkflowTemplate Template { get; set; } = null!;
    }

    /// <summary>
    /// Workflow definition entity (for visual workflows)
    /// </summary>
    public class WorkflowDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public string Version { get; set; } = "1.0.0";
        public string ConfigurationJson { get; set; } = "{}";

        // Navigation properties
        public List<WorkflowStep> Steps { get; set; } = new();
        public ApplicationUser Creator { get; set; } = null!;
    }

    /// <summary>
    /// Workflow step entity
    /// </summary>
    public class WorkflowStep
    {
        public Guid Id { get; set; }
        public Guid WorkflowDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public int OrderIndex { get; set; }
        public string ConfigurationJson { get; set; } = "{}";
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    }

    /// <summary>
    /// Execution trend data
    /// </summary>
    public class ExecutionTrend
    {
        public DateTime Period { get; set; }
        public int ExecutionCount { get; set; }
        public double SuccessRate { get; set; }
        public double AverageExecutionTime { get; set; }
    }

    /// <summary>
    /// Error summary data
    /// </summary>
    public class ErrorSummary
    {
        public string ErrorType { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? LastOccurrence { get; set; }
        public string? SampleMessage { get; set; }
    }
}