using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Communication routing workflow entity - routes messages to appropriate handlers
    /// </summary>
    public class CommunicationRoutingWorkflow
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string MessageType { get; set; } = string.Empty; // "Inquiry", "Complaint", "Request", "Escalation"

        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public CommunicationPriority Priority { get; set; } = CommunicationPriority.Normal;

        [Required]
        public CommunicationRoutingStatus Status { get; set; } = CommunicationRoutingStatus.Received;

        [Required]
        [MaxLength(100)]
        public string Channel { get; set; } = string.Empty; // "Email", "Phone", "Chat", "Portal"

        [MaxLength(450)]
        public string? SentBy { get; set; }

        [MaxLength(450)]
        public string? AssignedTo { get; set; }

        [MaxLength(450)]
        public string? EscalatedTo { get; set; }

        public int EscalationLevel { get; set; } = 0;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }

        public DateTime? EscalatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        public int ResponseTimeMinutes { get; set; } = 0;

        public bool RequiresEscalation { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Client? Client { get; set; }
        public List<CommunicationRoutingStep> RoutingSteps { get; set; } = new();
        public List<CommunicationRoutingRule> AppliedRules { get; set; } = new();
    }

    /// <summary>
    /// Communication routing step entity - tracks routing history
    /// </summary>
    public class CommunicationRoutingStep
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CommunicationRoutingWorkflowId { get; set; }

        [Required]
        [MaxLength(100)]
        public string StepType { get; set; } = string.Empty; // "Received", "Assigned", "Escalated", "Resolved"

        [MaxLength(450)]
        public string? HandledBy { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public CommunicationRoutingWorkflow? CommunicationRouting { get; set; }
    }

    /// <summary>
    /// Communication routing rule entity - defines routing rules
    /// </summary>
    public class CommunicationRoutingRule
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string MessageType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Priority { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string AssignToRole { get; set; } = string.Empty; // "Support", "Manager", "Director"

        public int EscalationThresholdMinutes { get; set; } = 0;

        [MaxLength(450)]
        public string? EscalateToRole { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public List<CommunicationRoutingWorkflow> CommunicationRoutings { get; set; } = new();
    }

    /// <summary>
    /// Communication escalation rule entity - defines escalation rules
    /// </summary>
    public class CommunicationEscalationRule
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        public int EscalationLevel { get; set; }

        [Required]
        [MaxLength(450)]
        public string EscalateToRole { get; set; } = string.Empty;

        [Required]
        public int TimeThresholdMinutes { get; set; }

        [MaxLength(1000)]
        public string? Condition { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Communication priority enum
    /// </summary>
    public enum CommunicationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Communication routing status enum
    /// </summary>
    public enum CommunicationRoutingStatus
    {
        Received = 0,
        Assigned = 1,
        InProgress = 2,
        Escalated = 3,
        Resolved = 4,
        Closed = 5
    }
}

