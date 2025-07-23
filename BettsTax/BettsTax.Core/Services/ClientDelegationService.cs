using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class ClientDelegationService : IClientDelegationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientDelegationService> _logger;

        public ClientDelegationService(ApplicationDbContext context, ILogger<ClientDelegationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Client>> GetAvailableClientsForDelegationAsync(string associateId)
        {
            try
            {
                // Get clients that don't already have permissions for this associate
                var existingClientIds = await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && p.IsActive)
                    .Select(p => p.ClientId)
                    .Distinct()
                    .ToListAsync();

                return await _context.Clients
                    .Where(c => !existingClientIds.Contains(c.ClientId))
                    .OrderBy(c => c.BusinessName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available clients for delegation for associate {AssociateId}", associateId);
                return new List<Client>();
            }
        }

        public async Task<bool> CanAccessClientAsync(string associateId, int clientId)
        {
            try
            {
                return await _context.AssociateClientPermissions
                    .AnyAsync(p => p.AssociateId == associateId && 
                                  p.ClientId == clientId && 
                                  p.IsActive &&
                                  (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if associate {AssociateId} can access client {ClientId}", associateId, clientId);
                return false;
            }
        }

        public async Task<List<ApplicationUser>> GetClientAssociatesAsync(int clientId)
        {
            try
            {
                var associateIds = await _context.AssociateClientPermissions
                    .Where(p => p.ClientId == clientId && p.IsActive)
                    .Select(p => p.AssociateId)
                    .Distinct()
                    .ToListAsync();

                return await _context.Users
                    .Where(u => associateIds.Contains(u.Id))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting associates for client {ClientId}", clientId);
                return new List<ApplicationUser>();
            }
        }

        public async Task<List<ApplicationUser>> GetAvailableAssociatesAsync()
        {
            try
            {
                // This is a simplified implementation - you might want to filter by role
                return await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available associates");
                return new List<ApplicationUser>();
            }
        }

        public async Task<Result> RequestClientConsentAsync(int clientId, string associateId, List<string> permissionAreas, string requestReason = "")
        {
            try
            {
                // For now, we'll create a simple consent request record
                // In a full implementation, you might want a separate ConsentRequest entity
                _logger.LogInformation("Consent request created for client {ClientId} and associate {AssociateId} for areas: {Areas}", 
                    clientId, associateId, string.Join(", ", permissionAreas));

                // You could implement email notifications here
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting client consent for client {ClientId} and associate {AssociateId}", clientId, associateId);
                return Result.Failure("Failed to request client consent");
            }
        }

        public async Task<Result> ProcessClientConsentAsync(int clientId, string associateId, bool approved, string reason)
        {
            try
            {
                // Process the consent response
                if (approved)
                {
                    _logger.LogInformation("Client {ClientId} approved associate {AssociateId} access: {Reason}", 
                        clientId, associateId, reason);
                }
                else
                {
                    _logger.LogInformation("Client {ClientId} rejected associate {AssociateId} access: {Reason}", 
                        clientId, associateId, reason);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing client consent for client {ClientId} and associate {AssociateId}", clientId, associateId);
                return Result.Failure("Failed to process client consent");
            }
        }

        public async Task<List<ClientConsentRequest>> GetPendingConsentRequestsAsync(int clientId)
        {
            try
            {
                // This would return actual consent requests if we had the entity
                // For now, return empty list
                return new List<ClientConsentRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending consent requests for client {ClientId}", clientId);
                return new List<ClientConsentRequest>();
            }
        }

        public async Task<List<ClientConsentRequest>> GetAssociateConsentRequestsAsync(string associateId)
        {
            try
            {
                // This would return actual consent requests if we had the entity
                // For now, return empty list
                return new List<ClientConsentRequest>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting consent requests for associate {AssociateId}", associateId);
                return new List<ClientConsentRequest>();
            }
        }

        public async Task<Dictionary<string, object>> GetDelegationStatisticsAsync(string associateId)
        {
            try
            {
                var permissions = await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && p.IsActive)
                    .ToListAsync();

                var stats = new Dictionary<string, object>
                {
                    ["TotalClients"] = permissions.Select(p => p.ClientId).Distinct().Count(),
                    ["TotalPermissions"] = permissions.Count,
                    ["PermissionsByArea"] = permissions.GroupBy(p => p.PermissionArea).ToDictionary(g => g.Key, g => g.Count()),
                    ["ExpiringPermissions"] = permissions.Count(p => p.ExpiryDate.HasValue && p.ExpiryDate <= DateTime.UtcNow.AddDays(30)),
                    ["RecentlyGranted"] = permissions.Count(p => p.GrantedDate >= DateTime.UtcNow.AddDays(-7))
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delegation statistics for associate {AssociateId}", associateId);
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<Client>> GetRecentAccessedClientsAsync(string associateId, int limit = 5)
        {
            try
            {
                // Get recently accessed clients based on on-behalf actions
                var recentClientIds = await _context.OnBehalfActions
                    .Where(a => a.AssociateId == associateId)
                    .OrderByDescending(a => a.ActionDate)
                    .Select(a => a.ClientId)
                    .Distinct()
                    .Take(limit)
                    .ToListAsync();

                var clients = await _context.Clients
                    .Where(c => recentClientIds.Contains(c.ClientId))
                    .ToListAsync();

                // Maintain the order from the recent actions
                return recentClientIds
                    .Select(id => clients.FirstOrDefault(c => c.ClientId == id))
                    .Where(c => c != null)
                    .Cast<Client>()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently accessed clients for associate {AssociateId}", associateId);
                return new List<Client>();
            }
        }

        public async Task<Result> LogClientAccessAsync(string associateId, int clientId, string accessType = "General")
        {
            try
            {
                // You could create a separate ClientAccess entity to track access patterns
                // For now, we'll just log it
                _logger.LogInformation("Associate {AssociateId} accessed client {ClientId} for {AccessType}", 
                    associateId, clientId, accessType);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging client access for associate {AssociateId}, client {ClientId}", associateId, clientId);
                return Result.Failure("Failed to log client access");
            }
        }
    }
}