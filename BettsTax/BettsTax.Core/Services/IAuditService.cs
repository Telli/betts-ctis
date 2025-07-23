using BettsTax.Data;

namespace BettsTax.Core.Services
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string entity, string entityId, string? details = null);
        
        Task LogClientPortalActivityAsync(
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
            string? errorMessage = null);
        
        Task LogSecurityEventAsync(
            string? userId,
            AuditActionType actionType,
            string details,
            string? ipAddress = null,
            string? userAgent = null,
            bool isSuccess = false,
            string? errorMessage = null);
    }
}
