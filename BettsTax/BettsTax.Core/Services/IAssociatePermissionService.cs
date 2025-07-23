using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IAssociatePermissionService
    {
        Task<bool> HasPermissionAsync(string associateId, int clientId, string area, AssociatePermissionLevel level);
        Task<bool> HasAnyPermissionAsync(string associateId, int clientId, string area);
        Task<List<AssociateClientPermission>> GetAssociatePermissionsAsync(string associateId);
        Task<List<Client>> GetDelegatedClientsAsync(string associateId, string area);
        Task<List<int>> GetDelegatedClientIdsAsync(string associateId, string area);
        Task<Result> GrantPermissionAsync(GrantPermissionRequest request, string adminId);
        Task<Result> RevokePermissionAsync(string associateId, int clientId, string area, string adminId, string reason = "");
        Task<Result> BulkGrantPermissionsAsync(BulkPermissionRequest request, string adminId);
        Task<Result> BulkRevokePermissionsAsync(List<int> permissionIds, string adminId, string reason = "");
        Task<Result> SetPermissionExpiryAsync(int permissionId, DateTime? expiryDate, string adminId);
        Task<List<AssociatePermissionAuditLog>> GetPermissionAuditLogAsync(string associateId, DateTime? from = null, DateTime? to = null);
        Task<List<AssociateClientPermission>> GetClientAssociatesAsync(int clientId);
        Task<List<AssociateClientPermission>> GetExpiringPermissionsAsync(int daysFromNow = 7);
        Task<Result> RenewPermissionAsync(int permissionId, DateTime newExpiryDate, string adminId);
        Task<bool> IsPermissionValidAsync(int permissionId);
        Task<AssociatePermissionLevel> GetEffectivePermissionLevelAsync(string associateId, int clientId, string area);
    }

    public class GrantPermissionRequest
    {
        public string AssociateId { get; set; } = string.Empty;
        public List<int> ClientIds { get; set; } = new();
        public string PermissionArea { get; set; } = string.Empty;
        public AssociatePermissionLevel Level { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? AmountThreshold { get; set; }
        public bool RequiresApproval { get; set; } = false;
        public string Notes { get; set; } = string.Empty;
    }

    public class BulkPermissionRequest
    {
        public List<string> AssociateIds { get; set; } = new();
        public List<int> ClientIds { get; set; } = new();
        public List<PermissionRule> Rules { get; set; } = new();
        public DateTime? ExpiryDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class PermissionRule
    {
        public string PermissionArea { get; set; } = string.Empty;
        public AssociatePermissionLevel Level { get; set; }
        public decimal? AmountThreshold { get; set; }
        public bool RequiresApproval { get; set; } = false;
    }
}