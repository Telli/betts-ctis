using BettsTax.Data;
using SecurityAuditLog = BettsTax.Data.Models.Security.AuditLog;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogAsync(string userId, string action, string entity, string entityId, string? details = null)
        {
            try
            {
                var log = new SecurityAuditLog
                {
                    UserId = userId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Description = details,
                    Operation = BettsTax.Data.Models.Security.AuditOperation.Create,
                    Severity = BettsTax.Data.Models.Security.AuditSeverity.Medium,
                    Category = BettsTax.Data.Models.Security.AuditCategory.DataAccess,
                    Timestamp = DateTime.UtcNow
                };
                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entry for user {UserId}, action {Action}", userId, action);
            }
        }

        public async Task LogClientPortalActivityAsync(
            string userId,
            int? clientId,
            AuditActionType actionType,
            string entity,
            string entityId,
            string? details = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? requestPath = null,
            bool isSuccess = true,
            string? errorMessage = null)
        {
            try
            {
                var log = new SecurityAuditLog
                {
                    UserId = userId,
                    Action = actionType.ToString(),
                    Entity = entity,
                    EntityId = entityId,
                    Description = details,
                    Operation = actionType switch
                    {
                        AuditActionType.Create => BettsTax.Data.Models.Security.AuditOperation.Create,
                        AuditActionType.Read => BettsTax.Data.Models.Security.AuditOperation.Read,
                        AuditActionType.Update => BettsTax.Data.Models.Security.AuditOperation.Update,
                        AuditActionType.Delete => BettsTax.Data.Models.Security.AuditOperation.Delete,
                        AuditActionType.Login => BettsTax.Data.Models.Security.AuditOperation.Login,
                        _ => BettsTax.Data.Models.Security.AuditOperation.Update
                    },
                    IpAddress = ipAddress ?? "Unknown",
                    UserAgent = userAgent,
                    Severity = isSuccess ? BettsTax.Data.Models.Security.AuditSeverity.Low : BettsTax.Data.Models.Security.AuditSeverity.Medium,
                    Category = BettsTax.Data.Models.Security.AuditCategory.ClientData,
                    Timestamp = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();

                // Also log to application logs for monitoring
                if (isSuccess)
                {
                    _logger.LogInformation(
                        "Client Portal Activity: User {UserId} performed {ActionType} on {Entity} {EntityId} for Client {ClientId} from IP {IPAddress}",
                        userId, actionType, entity, entityId, clientId, ipAddress);
                }
                else
                {
                    _logger.LogWarning(
                        "Client Portal Failed Activity: User {UserId} failed {ActionType} on {Entity} {EntityId} for Client {ClientId} from IP {IPAddress}. Error: {ErrorMessage}",
                        userId, actionType, entity, entityId, clientId, ipAddress, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log client portal audit entry for user {UserId}, action {ActionType}", userId, actionType);
            }
        }

        public async Task LogSecurityEventAsync(
            string? userId,
            AuditActionType actionType,
            string details,
            string? ipAddress = null,
            string? userAgent = null,
            bool isSuccess = false,
            string? errorMessage = null)
        {
            try
            {
                var log = new SecurityAuditLog
                {
                    UserId = userId ?? "SYSTEM",
                    Action = actionType.ToString(),
                    Entity = "Security",
                    EntityId = "N/A",
                    Description = details,
                    Operation = BettsTax.Data.Models.Security.AuditOperation.Login,
                    IpAddress = ipAddress ?? "Unknown",
                    UserAgent = userAgent,
                    Severity = isSuccess ? BettsTax.Data.Models.Security.AuditSeverity.Low : BettsTax.Data.Models.Security.AuditSeverity.High,
                    Category = BettsTax.Data.Models.Security.AuditCategory.SecurityEvent,
                    Timestamp = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();

                // Security events are always important to log
                if (isSuccess)
                {
                    _logger.LogInformation(
                        "Security Event: {ActionType} by User {UserId} from IP {IPAddress}. Details: {Details}",
                        actionType, userId, ipAddress, details);
                }
                else
                {
                    _logger.LogWarning(
                        "Security Event FAILED: {ActionType} by User {UserId} from IP {IPAddress}. Details: {Details}. Error: {ErrorMessage}",
                        actionType, userId, ipAddress, details, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security audit entry for user {UserId}, action {ActionType}", userId, actionType);
            }
        }
    }
}
