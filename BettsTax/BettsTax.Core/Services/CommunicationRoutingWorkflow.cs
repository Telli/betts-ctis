using BettsTax.Data;
using BettsTax.Shared;
using BettsTax.Core.DTOs.Communication;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Communication Routing Workflow Service - Routes messages to appropriate handlers
    /// </summary>
    public class CommunicationRoutingWorkflow : ICommunicationRoutingWorkflow
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<CommunicationRoutingWorkflow> _logger;

        public CommunicationRoutingWorkflow(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService,
            ILogger<CommunicationRoutingWorkflow> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result<CommunicationRoutingDto>> ReceiveAndRouteMessageAsync(
            int clientId, string messageType, string subject, string content, string priority, string channel, string sentBy)
        {
            try
            {
                _logger.LogInformation("Receiving message from client {ClientId}, type {MessageType}", clientId, messageType);

                // Parse priority
                if (!Enum.TryParse<CommunicationPriority>(priority, out var priorityEnum))
                    priorityEnum = CommunicationPriority.Normal;

                var routing = new Data.CommunicationRoutingWorkflow
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    MessageType = messageType,
                    Subject = subject,
                    Content = content,
                    Priority = priorityEnum,
                    Status = CommunicationRoutingStatus.Received,
                    Channel = channel,
                    SentBy = sentBy,
                    ReceivedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Create received step
                var receivedStep = new CommunicationRoutingStep
                {
                    Id = Guid.NewGuid(),
                    CommunicationRoutingWorkflowId = routing.Id,
                    StepType = "Received",
                    HandledBy = "System",
                    CreatedAt = DateTime.UtcNow
                };

                routing.RoutingSteps.Add(receivedStep);

                // Find applicable routing rule
                var rule = await _context.CommunicationRoutingRules
                    .Where(r => r.IsActive && r.MessageType == messageType && r.Priority == priority)
                    .FirstOrDefaultAsync();

                if (rule != null)
                {
                    // Get user for role
                    var assignee = await GetUserForRoleAsync(rule.AssignToRole);
                    if (!string.IsNullOrEmpty(assignee))
                    {
                        routing.AssignedTo = assignee;
                        routing.AssignedAt = DateTime.UtcNow;
                        routing.Status = CommunicationRoutingStatus.Assigned;

                        var assignedStep = new CommunicationRoutingStep
                        {
                            Id = Guid.NewGuid(),
                            CommunicationRoutingWorkflowId = routing.Id,
                            StepType = "Assigned",
                            HandledBy = "System",
                            Notes = $"Assigned to {rule.AssignToRole}",
                            CreatedAt = DateTime.UtcNow
                        };
                        routing.RoutingSteps.Add(assignedStep);

                        // Notify assignee
                        await _notificationService.SendNotificationAsync(
                            assignee,
                            $"New {messageType}: {subject}",
                            "Email");
                    }
                }

                _context.CommunicationRoutingWorkflows.Add(routing);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(sentBy, "CREATE", "CommunicationRouting", routing.Id.ToString(),
                    $"Created communication routing for {messageType}");

                _logger.LogInformation("Message routed: {RoutingId}", routing.Id);

                return Result.Success(MapToDto(routing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving and routing message");
                return Result.Failure<CommunicationRoutingDto>($"Error receiving and routing message: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationRoutingDto>> AssignMessageAsync(
            Guid routingId, string assignToUserId, string? notes = null)
        {
            try
            {
                var routing = await _context.CommunicationRoutingWorkflows
                    .Include(r => r.RoutingSteps)
                    .FirstOrDefaultAsync(r => r.Id == routingId);

                if (routing == null)
                    return Result.Failure<CommunicationRoutingDto>("Routing not found");

                routing.AssignedTo = assignToUserId;
                routing.AssignedAt = DateTime.UtcNow;
                routing.Status = CommunicationRoutingStatus.Assigned;

                var assignedStep = new CommunicationRoutingStep
                {
                    Id = Guid.NewGuid(),
                    CommunicationRoutingWorkflowId = routing.Id,
                    StepType = "Assigned",
                    HandledBy = assignToUserId,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };
                routing.RoutingSteps.Add(assignedStep);

                await _context.SaveChangesAsync();

                // Notify assignee
                await _notificationService.SendNotificationAsync(
                    assignToUserId,
                    $"Message Assigned: {routing.Subject}",
                    "Email");

                return Result.Success(MapToDto(routing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning message");
                return Result.Failure<CommunicationRoutingDto>($"Error assigning message: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationRoutingDto>> EscalateMessageAsync(
            Guid routingId, string escalatedBy, string? reason = null)
        {
            try
            {
                var routing = await _context.CommunicationRoutingWorkflows
                    .Include(r => r.RoutingSteps)
                    .FirstOrDefaultAsync(r => r.Id == routingId);

                if (routing == null)
                    return Result.Failure<CommunicationRoutingDto>("Routing not found");

                routing.EscalationLevel++;
                routing.EscalatedAt = DateTime.UtcNow;
                routing.Status = CommunicationRoutingStatus.Escalated;
                routing.RequiresEscalation = true;

                var escalatedStep = new CommunicationRoutingStep
                {
                    Id = Guid.NewGuid(),
                    CommunicationRoutingWorkflowId = routing.Id,
                    StepType = "Escalated",
                    HandledBy = escalatedBy,
                    Notes = reason,
                    CreatedAt = DateTime.UtcNow
                };
                routing.RoutingSteps.Add(escalatedStep);

                // Find escalation rule
                var escalationRule = await _context.CommunicationEscalationRules
                    .Where(r => r.IsActive && r.EscalationLevel == routing.EscalationLevel)
                    .FirstOrDefaultAsync();

                if (escalationRule != null)
                {
                    var escalatee = await GetUserForRoleAsync(escalationRule.EscalateToRole);
                    if (!string.IsNullOrEmpty(escalatee))
                    {
                        routing.EscalatedTo = escalatee;

                        // Notify escalatee
                        await _notificationService.SendNotificationAsync(
                            escalatee,
                            $"Escalated Message: {routing.Subject}",
                            "Email");
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(escalatedBy, "ESCALATE", "CommunicationRouting", routingId.ToString(),
                    $"Escalated message to level {routing.EscalationLevel}");

                return Result.Success(MapToDto(routing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating message");
                return Result.Failure<CommunicationRoutingDto>($"Error escalating message: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationRoutingDto>> ResolveMessageAsync(
            Guid routingId, string resolvedBy, string resolutionNotes)
        {
            try
            {
                var routing = await _context.CommunicationRoutingWorkflows
                    .Include(r => r.RoutingSteps)
                    .FirstOrDefaultAsync(r => r.Id == routingId);

                if (routing == null)
                    return Result.Failure<CommunicationRoutingDto>("Routing not found");

                routing.ResolvedAt = DateTime.UtcNow;
                routing.ResolutionNotes = resolutionNotes;
                routing.Status = CommunicationRoutingStatus.Resolved;

                if (routing.AssignedAt.HasValue)
                {
                    routing.ResponseTimeMinutes = (int)(routing.ResolvedAt.Value - routing.AssignedAt.Value).TotalMinutes;
                }

                var resolvedStep = new CommunicationRoutingStep
                {
                    Id = Guid.NewGuid(),
                    CommunicationRoutingWorkflowId = routing.Id,
                    StepType = "Resolved",
                    HandledBy = resolvedBy,
                    Notes = resolutionNotes,
                    CreatedAt = DateTime.UtcNow
                };
                routing.RoutingSteps.Add(resolvedStep);

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(resolvedBy, "RESOLVE", "CommunicationRouting", routingId.ToString(),
                    $"Resolved communication routing");

                return Result.Success(MapToDto(routing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving message");
                return Result.Failure<CommunicationRoutingDto>($"Error resolving message: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationRoutingDto>> GetRoutingDetailsAsync(Guid routingId)
        {
            try
            {
                var routing = await _context.CommunicationRoutingWorkflows
                    .Include(r => r.RoutingSteps)
                    .FirstOrDefaultAsync(r => r.Id == routingId);

                if (routing == null)
                    return Result.Failure<CommunicationRoutingDto>("Routing not found");

                return Result.Success(MapToDto(routing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routing details");
                return Result.Failure<CommunicationRoutingDto>($"Error getting routing details: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingDto>>> GetPendingMessagesAsync(string userId)
        {
            try
            {
                var routings = await _context.CommunicationRoutingWorkflows
                    .Where(r => r.AssignedTo == userId && r.Status != CommunicationRoutingStatus.Resolved && r.Status != CommunicationRoutingStatus.Closed)
                    .Include(r => r.RoutingSteps)
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.ReceivedAt)
                    .ToListAsync();

                return Result.Success(routings.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending messages");
                return Result.Failure<List<CommunicationRoutingDto>>($"Error getting pending messages: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingDto>>> GetAllPendingMessagesAsync()
        {
            try
            {
                var routings = await _context.CommunicationRoutingWorkflows
                    .Where(r => r.Status != CommunicationRoutingStatus.Resolved && r.Status != CommunicationRoutingStatus.Closed)
                    .Include(r => r.RoutingSteps)
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.ReceivedAt)
                    .ToListAsync();

                return Result.Success(routings.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all pending messages");
                return Result.Failure<List<CommunicationRoutingDto>>($"Error getting all pending messages: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingDto>>> GetClientMessagesAsync(int clientId)
        {
            try
            {
                var routings = await _context.CommunicationRoutingWorkflows
                    .Where(r => r.ClientId == clientId)
                    .Include(r => r.RoutingSteps)
                    .OrderByDescending(r => r.ReceivedAt)
                    .ToListAsync();

                return Result.Success(routings.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client messages");
                return Result.Failure<List<CommunicationRoutingDto>>($"Error getting client messages: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingDto>>> GetEscalatedMessagesAsync()
        {
            try
            {
                var routings = await _context.CommunicationRoutingWorkflows
                    .Where(r => r.Status == CommunicationRoutingStatus.Escalated)
                    .Include(r => r.RoutingSteps)
                    .OrderByDescending(r => r.EscalatedAt)
                    .ToListAsync();

                return Result.Success(routings.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalated messages");
                return Result.Failure<List<CommunicationRoutingDto>>($"Error getting escalated messages: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationRoutingRuleDto>> CreateRoutingRuleAsync(
            string ruleName, string messageType, string priority, string assignToRole, int escalationThresholdMinutes, string? escalateToRole = null)
        {
            try
            {
                var rule = new CommunicationRoutingRule
                {
                    Id = Guid.NewGuid(),
                    RuleName = ruleName,
                    MessageType = messageType,
                    Priority = priority,
                    AssignToRole = assignToRole,
                    EscalationThresholdMinutes = escalationThresholdMinutes,
                    EscalateToRole = escalateToRole,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CommunicationRoutingRules.Add(rule);
                await _context.SaveChangesAsync();

                return Result.Success(MapRuleToDto(rule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating routing rule");
                return Result.Failure<CommunicationRoutingRuleDto>($"Error creating routing rule: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingRuleDto>>> GetRoutingRulesAsync()
        {
            try
            {
                var rules = await _context.CommunicationRoutingRules
                    .Where(r => r.IsActive)
                    .ToListAsync();

                return Result.Success(rules.Select(MapRuleToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routing rules");
                return Result.Failure<List<CommunicationRoutingRuleDto>>($"Error getting routing rules: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationEscalationRuleDto>> CreateEscalationRuleAsync(
            string ruleName, int escalationLevel, string escalateToRole, int timeThresholdMinutes)
        {
            try
            {
                var rule = new CommunicationEscalationRule
                {
                    Id = Guid.NewGuid(),
                    RuleName = ruleName,
                    EscalationLevel = escalationLevel,
                    EscalateToRole = escalateToRole,
                    TimeThresholdMinutes = timeThresholdMinutes,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CommunicationEscalationRules.Add(rule);
                await _context.SaveChangesAsync();

                return Result.Success(MapEscalationRuleToDto(rule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escalation rule");
                return Result.Failure<CommunicationEscalationRuleDto>($"Error creating escalation rule: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationEscalationRuleDto>>> GetEscalationRulesAsync()
        {
            try
            {
                var rules = await _context.CommunicationEscalationRules
                    .Where(r => r.IsActive)
                    .ToListAsync();

                return Result.Success(rules.Select(MapEscalationRuleToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escalation rules");
                return Result.Failure<List<CommunicationEscalationRuleDto>>($"Error getting escalation rules: {ex.Message}");
            }
        }

        public async Task<Result<CommunicationStatisticsDto>> GetCommunicationStatisticsAsync(
            int? clientId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.CommunicationRoutingWorkflows.AsQueryable();

                if (clientId.HasValue)
                    query = query.Where(r => r.ClientId == clientId.Value);

                if (from.HasValue)
                    query = query.Where(r => r.CreatedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(r => r.CreatedAt <= to.Value);

                var routings = await query.ToListAsync();

                var stats = new CommunicationStatisticsDto
                {
                    TotalMessages = routings.Count,
                    ResolvedCount = routings.Count(r => r.Status == CommunicationRoutingStatus.Resolved),
                    PendingCount = routings.Count(r => r.Status != CommunicationRoutingStatus.Resolved && r.Status != CommunicationRoutingStatus.Closed),
                    EscalatedCount = routings.Count(r => r.Status == CommunicationRoutingStatus.Escalated),
                    CriticalPriorityCount = routings.Count(r => r.Priority == CommunicationPriority.Critical),
                    HighPriorityCount = routings.Count(r => r.Priority == CommunicationPriority.High)
                };

                if (stats.TotalMessages > 0)
                {
                    stats.ResolutionRate = (decimal)stats.ResolvedCount / stats.TotalMessages * 100;
                    stats.AverageResponseTimeMinutes = (int)routings.Where(r => r.ResponseTimeMinutes > 0).Average(r => r.ResponseTimeMinutes);
                }

                return Result.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communication statistics");
                return Result.Failure<CommunicationStatisticsDto>($"Error getting communication statistics: {ex.Message}");
            }
        }

        public async Task<Result<List<CommunicationRoutingStepDto>>> GetRoutingHistoryAsync(Guid routingId)
        {
            try
            {
                var steps = await _context.CommunicationRoutingSteps
                    .Where(s => s.CommunicationRoutingWorkflowId == routingId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                return Result.Success(steps.Select(MapStepToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting routing history");
                return Result.Failure<List<CommunicationRoutingStepDto>>($"Error getting routing history: {ex.Message}");
            }
        }

        public async Task<Result> CheckAndApplyEscalationRulesAsync()
        {
            try
            {
                _logger.LogInformation("Checking escalation rules");

                var pendingRoutings = await _context.CommunicationRoutingWorkflows
                    .Where(r => r.Status != CommunicationRoutingStatus.Resolved && r.Status != CommunicationRoutingStatus.Closed)
                    .ToListAsync();

                var now = DateTime.UtcNow;

                foreach (var routing in pendingRoutings)
                {
                    if (routing.AssignedAt.HasValue)
                    {
                        var minutesElapsed = (now - routing.AssignedAt.Value).TotalMinutes;

                        var escalationRule = await _context.CommunicationEscalationRules
                            .Where(r => r.IsActive && r.TimeThresholdMinutes <= minutesElapsed && r.EscalationLevel == routing.EscalationLevel + 1)
                            .FirstOrDefaultAsync();

                        if (escalationRule != null && !routing.RequiresEscalation)
                        {
                            await EscalateMessageAsync(routing.Id, "System", $"Auto-escalated after {minutesElapsed} minutes");
                        }
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking escalation rules");
                return Result.Failure($"Error checking escalation rules: {ex.Message}");
            }
        }

        // Helper methods
        private async Task<string> GetUserForRoleAsync(string role)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == role);
            return user?.Id ?? string.Empty;
        }

        private CommunicationRoutingDto MapToDto(Data.CommunicationRoutingWorkflow routing)
        {
            return new CommunicationRoutingDto
            {
                Id = routing.Id,
                ClientId = routing.ClientId,
                MessageType = routing.MessageType,
                Subject = routing.Subject,
                Content = routing.Content,
                Priority = routing.Priority.ToString(),
                Status = routing.Status.ToString(),
                Channel = routing.Channel,
                SentBy = routing.SentBy,
                AssignedTo = routing.AssignedTo,
                EscalatedTo = routing.EscalatedTo,
                EscalationLevel = routing.EscalationLevel,
                ReceivedAt = routing.ReceivedAt,
                AssignedAt = routing.AssignedAt,
                EscalatedAt = routing.EscalatedAt,
                ResolvedAt = routing.ResolvedAt,
                ResolutionNotes = routing.ResolutionNotes,
                ResponseTimeMinutes = routing.ResponseTimeMinutes,
                RoutingSteps = routing.RoutingSteps.Select(MapStepToDto).ToList()
            };
        }

        private CommunicationRoutingStepDto MapStepToDto(CommunicationRoutingStep step)
        {
            return new CommunicationRoutingStepDto
            {
                Id = step.Id,
                StepType = step.StepType,
                HandledBy = step.HandledBy,
                Notes = step.Notes,
                CreatedAt = step.CreatedAt
            };
        }

        private CommunicationRoutingRuleDto MapRuleToDto(CommunicationRoutingRule rule)
        {
            return new CommunicationRoutingRuleDto
            {
                Id = rule.Id,
                RuleName = rule.RuleName,
                MessageType = rule.MessageType,
                Priority = rule.Priority,
                AssignToRole = rule.AssignToRole,
                EscalationThresholdMinutes = rule.EscalationThresholdMinutes,
                EscalateToRole = rule.EscalateToRole,
                IsActive = rule.IsActive
            };
        }

        private CommunicationEscalationRuleDto MapEscalationRuleToDto(CommunicationEscalationRule rule)
        {
            return new CommunicationEscalationRuleDto
            {
                Id = rule.Id,
                RuleName = rule.RuleName,
                EscalationLevel = rule.EscalationLevel,
                EscalateToRole = rule.EscalateToRole,
                TimeThresholdMinutes = rule.TimeThresholdMinutes,
                Condition = rule.Condition,
                IsActive = rule.IsActive
            };
        }
    }
}

