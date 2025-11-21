using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs.Communication
{
    /// <summary>
    /// Communication Routing DTO
    /// </summary>
    public class CommunicationRoutingDto
    {
        public Guid Id { get; set; }
        public int ClientId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? SentBy { get; set; }
        public string? AssignedTo { get; set; }
        public string? EscalatedTo { get; set; }
        public int EscalationLevel { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public int ResponseTimeMinutes { get; set; }
        public List<CommunicationRoutingStepDto> RoutingSteps { get; set; } = new();
    }

    /// <summary>
    /// Communication Routing Step DTO
    /// </summary>
    public class CommunicationRoutingStepDto
    {
        public Guid Id { get; set; }
        public string StepType { get; set; } = string.Empty;
        public string? HandledBy { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Communication Routing Rule DTO
    /// </summary>
    public class CommunicationRoutingRuleDto
    {
        public Guid Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AssignToRole { get; set; } = string.Empty;
        public int EscalationThresholdMinutes { get; set; }
        public string? EscalateToRole { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Communication Escalation Rule DTO
    /// </summary>
    public class CommunicationEscalationRuleDto
    {
        public Guid Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public int EscalationLevel { get; set; }
        public string EscalateToRole { get; set; } = string.Empty;
        public int TimeThresholdMinutes { get; set; }
        public string? Condition { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Communication Statistics DTO
    /// </summary>
    public class CommunicationStatisticsDto
    {
        public int TotalMessages { get; set; }
        public int ResolvedCount { get; set; }
        public int PendingCount { get; set; }
        public int EscalatedCount { get; set; }
        public decimal ResolutionRate { get; set; }
        public int AverageResponseTimeMinutes { get; set; }
        public int CriticalPriorityCount { get; set; }
        public int HighPriorityCount { get; set; }
    }

    /// <summary>
    /// Request to receive and route a message
    /// </summary>
    public class ReceiveAndRouteMessageRequest
    {
        public int ClientId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to assign a message
    /// </summary>
    public class AssignMessageRequest
    {
        public Guid RoutingId { get; set; }
        public string AssignToUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to escalate a message
    /// </summary>
    public class EscalateMessageRequest
    {
        public Guid RoutingId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request to resolve a message
    /// </summary>
    public class ResolveMessageRequest
    {
        public Guid RoutingId { get; set; }
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to create routing rule
    /// </summary>
    public class CreateRoutingRuleRequest
    {
        public string RuleName { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AssignToRole { get; set; } = string.Empty;
        public int EscalationThresholdMinutes { get; set; }
        public string? EscalateToRole { get; set; }
    }

    /// <summary>
    /// Request to create escalation rule
    /// </summary>
    public class CreateEscalationRuleRequest
    {
        public string RuleName { get; set; } = string.Empty;
        public int EscalationLevel { get; set; }
        public string EscalateToRole { get; set; } = string.Empty;
        public int TimeThresholdMinutes { get; set; }
    }
}

