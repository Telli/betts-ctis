using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class OnBehalfActionService : IOnBehalfActionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OnBehalfActionService> _logger;

        public OnBehalfActionService(ApplicationDbContext context, ILogger<OnBehalfActionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(string associateId, int clientId, string action, string entityType, int entityId, 
            object? oldValues = null, object? newValues = null, string? reason = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var onBehalfAction = new OnBehalfAction
                {
                    AssociateId = associateId,
                    ClientId = clientId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    ActionDate = DateTime.UtcNow,
                    Reason = reason,
                    ClientNotified = false,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                _context.OnBehalfActions.Add(onBehalfAction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Logged on-behalf action: {Action} on {EntityType} {EntityId} by associate {AssociateId} for client {ClientId}", 
                    action, entityType, entityId, associateId, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging on-behalf action for associate {AssociateId}, client {ClientId}", associateId, clientId);
            }
        }

        public async Task<List<OnBehalfAction>> GetClientActionsAsync(int clientId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.OnBehalfActions
                    .Include(a => a.Associate)
                    .Include(a => a.Client)
                    .Where(a => a.ClientId == clientId);

                if (from.HasValue)
                    query = query.Where(a => a.ActionDate >= from.Value);

                if (to.HasValue)  
                    query = query.Where(a => a.ActionDate <= to.Value);

                return await query
                    .OrderByDescending(a => a.ActionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions for client {ClientId}", clientId);
                return new List<OnBehalfAction>();
            }
        }

        public async Task<List<OnBehalfAction>> GetAssociateActionsAsync(string associateId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.OnBehalfActions
                    .Include(a => a.Associate)
                    .Include(a => a.Client)
                    .Where(a => a.AssociateId == associateId);

                if (from.HasValue)
                    query = query.Where(a => a.ActionDate >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.ActionDate <= to.Value);

                return await query
                    .OrderByDescending(a => a.ActionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions for associate {AssociateId}", associateId);
                return new List<OnBehalfAction>();
            }
        }

        public async Task<List<OnBehalfAction>> GetEntityActionsAsync(string entityType, int entityId)
        {
            try
            {
                return await _context.OnBehalfActions
                    .Include(a => a.Associate)
                    .Include(a => a.Client)
                    .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                    .OrderByDescending(a => a.ActionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actions for entity {EntityType} {EntityId}", entityType, entityId);
                return new List<OnBehalfAction>();
            }
        }

        public async Task<Result> NotifyClientOfActionAsync(int clientId, OnBehalfAction action)
        {
            try
            {
                // Here you could implement email/SMS notification logic
                // For now, we'll just mark it as notified
                action.ClientNotified = true;
                action.ClientNotificationDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Client {ClientId} notified of action {ActionId}", clientId, action.Id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying client {ClientId} of action {ActionId}", clientId, action.Id);
                return Result.Failure("Failed to notify client");
            }
        }

        public async Task<List<OnBehalfAction>> GetRecentActionsAsync(string associateId, int limit = 10)
        {
            try
            {
                return await _context.OnBehalfActions
                    .Include(a => a.Client)
                    .Where(a => a.AssociateId == associateId)
                    .OrderByDescending(a => a.ActionDate)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent actions for associate {AssociateId}", associateId);
                return new List<OnBehalfAction>();
            }
        }

        public async Task<Dictionary<string, object>> GetActionStatisticsAsync(string associateId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.OnBehalfActions.Where(a => a.AssociateId == associateId);

                if (from.HasValue)
                    query = query.Where(a => a.ActionDate >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.ActionDate <= to.Value);

                var actions = await query.ToListAsync();

                var stats = new Dictionary<string, object>
                {
                    ["TotalActions"] = actions.Count,
                    ["ActionsByType"] = actions.GroupBy(a => a.Action).ToDictionary(g => g.Key, g => g.Count()),
                    ["ActionsByEntityType"] = actions.GroupBy(a => a.EntityType).ToDictionary(g => g.Key, g => g.Count()),
                    ["ActionsByClient"] = actions.GroupBy(a => a.ClientId).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    ["ActionsPerDay"] = actions
                        .GroupBy(a => a.ActionDate.Date)
                        .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                    ["NotificationsPending"] = actions.Count(a => !a.ClientNotified)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting action statistics for associate {AssociateId}", associateId);
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<OnBehalfAction>> GetUnnotifiedActionsAsync(int clientId)
        {
            try
            {
                return await _context.OnBehalfActions
                    .Include(a => a.Associate)
                    .Where(a => a.ClientId == clientId && !a.ClientNotified)
                    .OrderByDescending(a => a.ActionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unnotified actions for client {ClientId}", clientId);
                return new List<OnBehalfAction>();
            }
        }

        public async Task<Result> MarkActionAsNotifiedAsync(int actionId)
        {
            try
            {
                var action = await _context.OnBehalfActions.FindAsync(actionId);
                if (action != null)
                {
                    action.ClientNotified = true;
                    action.ClientNotificationDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking action {ActionId} as notified", actionId);
                return Result.Failure("Failed to mark action as notified");
            }
        }

        public async Task<Result> BulkNotifyClientAsync(int clientId, List<int> actionIds)
        {
            try
            {
                var actions = await _context.OnBehalfActions
                    .Where(a => a.ClientId == clientId && actionIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var action in actions)
                {
                    action.ClientNotified = true;
                    action.ClientNotificationDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk notified client {ClientId} of {Count} actions", clientId, actions.Count);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk notifying client {ClientId}", clientId);
                return Result.Failure("Failed to bulk notify client");
            }
        }
    }
}