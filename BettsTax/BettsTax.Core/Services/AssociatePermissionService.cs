using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class AssociatePermissionService : IAssociatePermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssociatePermissionService> _logger;

        public AssociatePermissionService(ApplicationDbContext context, ILogger<AssociatePermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string associateId, int clientId, string area, AssociatePermissionLevel level)
        {
            try
            {
                var permission = await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && 
                               p.ClientId == clientId && 
                               p.PermissionArea == area && 
                               p.IsActive &&
                               (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .FirstOrDefaultAsync();

                if (permission == null)
                    return false;

                return permission.Level.HasFlag(level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for associate {AssociateId}, client {ClientId}, area {Area}, level {Level}", 
                    associateId, clientId, area, level);
                return false;
            }
        }

        public async Task<bool> HasAnyPermissionAsync(string associateId, int clientId, string area)
        {
            try
            {
                return await _context.AssociateClientPermissions
                    .AnyAsync(p => p.AssociateId == associateId && 
                                  p.ClientId == clientId && 
                                  p.PermissionArea == area && 
                                  p.IsActive &&
                                  (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking any permission for associate {AssociateId}, client {ClientId}, area {Area}", 
                    associateId, clientId, area);
                return false;
            }
        }

        public async Task<List<AssociateClientPermission>> GetAssociatePermissionsAsync(string associateId)
        {
            try
            {
                return await _context.AssociateClientPermissions
                    .Include(p => p.Client)
                    .Include(p => p.GrantedByAdmin)
                    .Where(p => p.AssociateId == associateId && p.IsActive)
                    .OrderBy(p => p.PermissionArea)
                    .ThenBy(p => p.Client!.BusinessName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for associate {AssociateId}", associateId);
                return new List<AssociateClientPermission>();
            }
        }

        public async Task<List<Client>> GetDelegatedClientsAsync(string associateId, string area)
        {
            try
            {
                var clientIds = await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && 
                               p.PermissionArea == area && 
                               p.IsActive &&
                               (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .Select(p => p.ClientId)
                    .Distinct()
                    .ToListAsync();

                return await _context.Clients
                    .Where(c => clientIds.Contains(c.ClientId))
                    .OrderBy(c => c.BusinessName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delegated clients for associate {AssociateId}, area {Area}", associateId, area);
                return new List<Client>();
            }
        }

        public async Task<List<int>> GetDelegatedClientIdsAsync(string associateId, string area)
        {
            try
            {
                return await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && 
                               p.PermissionArea == area && 
                               p.IsActive &&
                               (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .Select(p => p.ClientId)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delegated client IDs for associate {AssociateId}, area {Area}", associateId, area);
                return new List<int>();
            }
        }

        public async Task<Result> GrantPermissionAsync(GrantPermissionRequest request, string adminId)
        {
            try
            {
                foreach (var clientId in request.ClientIds)
                {
                    // Check if permission already exists
                    var existingPermission = await _context.AssociateClientPermissions
                        .FirstOrDefaultAsync(p => p.AssociateId == request.AssociateId && 
                                                 p.ClientId == clientId && 
                                                 p.PermissionArea == request.PermissionArea);

                    if (existingPermission != null)
                    {
                        // Update existing permission
                        existingPermission.Level = request.Level;
                        existingPermission.ExpiryDate = request.ExpiryDate;
                        existingPermission.AmountThreshold = request.AmountThreshold;
                        existingPermission.RequiresApproval = request.RequiresApproval;
                        existingPermission.Notes = request.Notes;
                        existingPermission.IsActive = true;
                        existingPermission.UpdatedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new permission
                        var permission = new AssociateClientPermission
                        {
                            AssociateId = request.AssociateId,
                            ClientId = clientId,
                            PermissionArea = request.PermissionArea,
                            Level = request.Level,
                            GrantedDate = DateTime.UtcNow,
                            ExpiryDate = request.ExpiryDate,
                            GrantedByAdminId = adminId,
                            IsActive = true,
                            Notes = request.Notes,
                            AmountThreshold = request.AmountThreshold,
                            RequiresApproval = request.RequiresApproval
                        };

                        _context.AssociateClientPermissions.Add(permission);
                    }

                    // Log the permission change
                    var auditLog = new AssociatePermissionAuditLog
                    {
                        AssociateId = request.AssociateId,
                        ClientId = clientId,
                        Action = existingPermission != null ? "Update" : "Grant",
                        PermissionArea = request.PermissionArea,
                        OldLevel = existingPermission?.Level,
                        NewLevel = request.Level,
                        ChangedByAdminId = adminId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Permission {(existingPermission != null ? "updated" : "granted")} by admin"
                    };

                    _context.AssociatePermissionAuditLogs.Add(auditLog);
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission for associate {AssociateId}", request.AssociateId);
                return Result.Failure("Failed to grant permission");
            }
        }

        public async Task<Result> RevokePermissionAsync(string associateId, int clientId, string area, string adminId, string reason = "")
        {
            try
            {
                var permission = await _context.AssociateClientPermissions
                    .FirstOrDefaultAsync(p => p.AssociateId == associateId && 
                                             p.ClientId == clientId && 
                                             p.PermissionArea == area);

                if (permission != null)
                {
                    var oldLevel = permission.Level;
                    permission.IsActive = false;
                    permission.UpdatedDate = DateTime.UtcNow;

                    // Log the permission change
                    var auditLog = new AssociatePermissionAuditLog
                    {
                        AssociateId = associateId,
                        ClientId = clientId,
                        Action = "Revoke",
                        PermissionArea = area,
                        OldLevel = oldLevel,
                        NewLevel = AssociatePermissionLevel.None,
                        ChangedByAdminId = adminId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = reason
                    };

                    _context.AssociatePermissionAuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission for associate {AssociateId}, client {ClientId}, area {Area}", 
                    associateId, clientId, area);
                return Result.Failure("Failed to revoke permission");
            }
        }

        public async Task<Result> BulkGrantPermissionsAsync(BulkPermissionRequest request, string adminId)
        {
            try
            {
                foreach (var associateId in request.AssociateIds)
                {
                    foreach (var rule in request.Rules)
                    {
                        var grantRequest = new GrantPermissionRequest
                        {
                            AssociateId = associateId,
                            ClientIds = request.ClientIds,
                            PermissionArea = rule.PermissionArea,
                            Level = rule.Level,
                            ExpiryDate = request.ExpiryDate,
                            AmountThreshold = rule.AmountThreshold,
                            RequiresApproval = rule.RequiresApproval,
                            Notes = request.Notes
                        };

                        await GrantPermissionAsync(grantRequest, adminId);
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk granting permissions");
                return Result.Failure("Failed to bulk grant permissions");
            }
        }

        public async Task<Result> BulkRevokePermissionsAsync(List<int> permissionIds, string adminId, string reason = "")
        {
            try
            {
                var permissions = await _context.AssociateClientPermissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    var oldLevel = permission.Level;
                    permission.IsActive = false;
                    permission.UpdatedDate = DateTime.UtcNow;

                    // Log the permission change
                    var auditLog = new AssociatePermissionAuditLog
                    {
                        AssociateId = permission.AssociateId,
                        ClientId = permission.ClientId,
                        Action = "BulkRevoke",
                        PermissionArea = permission.PermissionArea,
                        OldLevel = oldLevel,
                        NewLevel = AssociatePermissionLevel.None,
                        ChangedByAdminId = adminId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = reason
                    };

                    _context.AssociatePermissionAuditLogs.Add(auditLog);
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk revoking permissions");
                return Result.Failure("Failed to bulk revoke permissions");
            }
        }

        public async Task<Result> SetPermissionExpiryAsync(int permissionId, DateTime? expiryDate, string adminId)
        {
            try
            {
                var permission = await _context.AssociateClientPermissions.FindAsync(permissionId);
                if (permission != null)
                {
                    permission.ExpiryDate = expiryDate;
                    permission.UpdatedDate = DateTime.UtcNow;

                    // Log the change
                    var auditLog = new AssociatePermissionAuditLog
                    {
                        AssociateId = permission.AssociateId,
                        ClientId = permission.ClientId,
                        Action = "SetExpiry",
                        PermissionArea = permission.PermissionArea,
                        OldLevel = permission.Level,
                        NewLevel = permission.Level,
                        ChangedByAdminId = adminId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Expiry date set to {expiryDate?.ToString("yyyy-MM-dd") ?? "never"}"
                    };

                    _context.AssociatePermissionAuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting permission expiry for permission {PermissionId}", permissionId);
                return Result.Failure("Failed to set permission expiry");
            }
        }

        public async Task<List<AssociatePermissionAuditLog>> GetPermissionAuditLogAsync(string associateId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.AssociatePermissionAuditLogs
                    .Include(a => a.Associate)
                    .Include(a => a.Client)
                    .Include(a => a.ChangedByAdmin)
                    .Where(a => a.AssociateId == associateId);

                if (from.HasValue)
                    query = query.Where(a => a.ChangeDate >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.ChangeDate <= to.Value);

                return await query
                    .OrderByDescending(a => a.ChangeDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission audit log for associate {AssociateId}", associateId);
                return new List<AssociatePermissionAuditLog>();
            }
        }

        public async Task<List<AssociateClientPermission>> GetClientAssociatesAsync(int clientId)
        {
            try
            {
                return await _context.AssociateClientPermissions
                    .Include(p => p.Associate)
                    .Where(p => p.ClientId == clientId && p.IsActive)
                    .OrderBy(p => p.Associate!.FirstName)
                    .ThenBy(p => p.Associate!.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting associates for client {ClientId}", clientId);
                return new List<AssociateClientPermission>();
            }
        }

        public async Task<List<AssociateClientPermission>> GetExpiringPermissionsAsync(int daysFromNow = 7)
        {
            try
            {
                var expiryThreshold = DateTime.UtcNow.AddDays(daysFromNow);
                
                return await _context.AssociateClientPermissions
                    .Include(p => p.Associate)
                    .Include(p => p.Client)
                    .Where(p => p.IsActive && 
                               p.ExpiryDate != null && 
                               p.ExpiryDate <= expiryThreshold)
                    .OrderBy(p => p.ExpiryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring permissions");
                return new List<AssociateClientPermission>();
            }
        }

        public async Task<Result> RenewPermissionAsync(int permissionId, DateTime newExpiryDate, string adminId)
        {
            try
            {
                var permission = await _context.AssociateClientPermissions.FindAsync(permissionId);
                if (permission != null)
                {
                    var oldExpiry = permission.ExpiryDate;
                    permission.ExpiryDate = newExpiryDate;
                    permission.UpdatedDate = DateTime.UtcNow;

                    // Log the renewal
                    var auditLog = new AssociatePermissionAuditLog
                    {
                        AssociateId = permission.AssociateId,
                        ClientId = permission.ClientId,
                        Action = "Renew",
                        PermissionArea = permission.PermissionArea,
                        OldLevel = permission.Level,
                        NewLevel = permission.Level,
                        ChangedByAdminId = adminId,
                        ChangeDate = DateTime.UtcNow,
                        Reason = $"Permission renewed from {oldExpiry?.ToString("yyyy-MM-dd") ?? "never"} to {newExpiryDate:yyyy-MM-dd}"
                    };

                    _context.AssociatePermissionAuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing permission {PermissionId}", permissionId);
                return Result.Failure("Failed to renew permission");
            }
        }

        public async Task<bool> IsPermissionValidAsync(int permissionId)
        {
            try
            {
                var permission = await _context.AssociateClientPermissions.FindAsync(permissionId);
                return permission != null && 
                       permission.IsActive && 
                       (permission.ExpiryDate == null || permission.ExpiryDate > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if permission {PermissionId} is valid", permissionId);
                return false;
            }
        }

        public async Task<AssociatePermissionLevel> GetEffectivePermissionLevelAsync(string associateId, int clientId, string area)
        {
            try
            {
                var permission = await _context.AssociateClientPermissions
                    .Where(p => p.AssociateId == associateId && 
                               p.ClientId == clientId && 
                               p.PermissionArea == area && 
                               p.IsActive &&
                               (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                    .FirstOrDefaultAsync();

                return permission?.Level ?? AssociatePermissionLevel.None;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting effective permission level for associate {AssociateId}, client {ClientId}, area {Area}", 
                    associateId, clientId, area);
                return AssociatePermissionLevel.None;
            }
        }
    }
}