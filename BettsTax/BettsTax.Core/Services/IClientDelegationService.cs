using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IClientDelegationService
    {
        Task<List<Client>> GetAvailableClientsForDelegationAsync(string associateId);
        Task<bool> CanAccessClientAsync(string associateId, int clientId);
        Task<List<ApplicationUser>> GetClientAssociatesAsync(int clientId);
        Task<List<ApplicationUser>> GetAvailableAssociatesAsync();
        Task<Result> RequestClientConsentAsync(int clientId, string associateId, List<string> permissionAreas, string requestReason = "");
        Task<Result> ProcessClientConsentAsync(int clientId, string associateId, bool approved, string reason);
        Task<List<ClientConsentRequest>> GetPendingConsentRequestsAsync(int clientId);
        Task<List<ClientConsentRequest>> GetAssociateConsentRequestsAsync(string associateId);
        Task<Dictionary<string, object>> GetDelegationStatisticsAsync(string associateId);
        Task<List<Client>> GetRecentAccessedClientsAsync(string associateId, int limit = 5);
        Task<Result> LogClientAccessAsync(string associateId, int clientId, string accessType = "General");
    }

    public class ClientConsentRequest
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public string AssociateId { get; set; } = string.Empty;
        public ApplicationUser? Associate { get; set; }
        public List<string> RequestedPermissionAreas { get; set; } = new();
        public string RequestReason { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public ConsentStatus Status { get; set; }
        public string? ResponseReason { get; set; }
        public DateTime? ResponseDate { get; set; }
    }

    public enum ConsentStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired
    }
}