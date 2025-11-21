using BettsTax.Shared;
using BettsTax.Core.DTOs.Communication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BettsTax.Core.Services.Interfaces
{
    /// <summary>
    /// Communication Routing Workflow Service - Routes messages to appropriate handlers
    /// </summary>
    public interface ICommunicationRoutingWorkflow
    {
        /// <summary>
        /// Receive and route a new message
        /// </summary>
        Task<Result<CommunicationRoutingDto>> ReceiveAndRouteMessageAsync(
            int clientId,
            string messageType,
            string subject,
            string content,
            string priority,
            string channel,
            string sentBy);

        /// <summary>
        /// Assign message to a handler
        /// </summary>
        Task<Result<CommunicationRoutingDto>> AssignMessageAsync(
            Guid routingId,
            string assignToUserId,
            string? notes = null);

        /// <summary>
        /// Escalate message to higher level
        /// </summary>
        Task<Result<CommunicationRoutingDto>> EscalateMessageAsync(
            Guid routingId,
            string escalatedBy,
            string? reason = null);

        /// <summary>
        /// Resolve a message
        /// </summary>
        Task<Result<CommunicationRoutingDto>> ResolveMessageAsync(
            Guid routingId,
            string resolvedBy,
            string resolutionNotes);

        /// <summary>
        /// Get routing details
        /// </summary>
        Task<Result<CommunicationRoutingDto>> GetRoutingDetailsAsync(Guid routingId);

        /// <summary>
        /// Get pending messages for a user
        /// </summary>
        Task<Result<List<CommunicationRoutingDto>>> GetPendingMessagesAsync(string userId);

        /// <summary>
        /// Get all pending messages
        /// </summary>
        Task<Result<List<CommunicationRoutingDto>>> GetAllPendingMessagesAsync();

        /// <summary>
        /// Get messages for a client
        /// </summary>
        Task<Result<List<CommunicationRoutingDto>>> GetClientMessagesAsync(int clientId);

        /// <summary>
        /// Get escalated messages
        /// </summary>
        Task<Result<List<CommunicationRoutingDto>>> GetEscalatedMessagesAsync();

        /// <summary>
        /// Create routing rule
        /// </summary>
        Task<Result<CommunicationRoutingRuleDto>> CreateRoutingRuleAsync(
            string ruleName,
            string messageType,
            string priority,
            string assignToRole,
            int escalationThresholdMinutes,
            string? escalateToRole = null);

        /// <summary>
        /// Get routing rules
        /// </summary>
        Task<Result<List<CommunicationRoutingRuleDto>>> GetRoutingRulesAsync();

        /// <summary>
        /// Create escalation rule
        /// </summary>
        Task<Result<CommunicationEscalationRuleDto>> CreateEscalationRuleAsync(
            string ruleName,
            int escalationLevel,
            string escalateToRole,
            int timeThresholdMinutes);

        /// <summary>
        /// Get escalation rules
        /// </summary>
        Task<Result<List<CommunicationEscalationRuleDto>>> GetEscalationRulesAsync();

        /// <summary>
        /// Get communication statistics
        /// </summary>
        Task<Result<CommunicationStatisticsDto>> GetCommunicationStatisticsAsync(
            int? clientId = null,
            DateTime? from = null,
            DateTime? to = null);

        /// <summary>
        /// Get routing history
        /// </summary>
        Task<Result<List<CommunicationRoutingStepDto>>> GetRoutingHistoryAsync(Guid routingId);

        /// <summary>
        /// Check and apply escalation rules
        /// </summary>
        Task<Result> CheckAndApplyEscalationRulesAsync();
    }
}

