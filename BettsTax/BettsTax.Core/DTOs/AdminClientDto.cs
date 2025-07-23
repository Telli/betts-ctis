using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class AdminClientOverviewDto
    {
        public int ClientId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TaxpayerCategory { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
        public bool HasUserAccount { get; set; }
        public string? AssignedAssociateName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int TotalTaxFilings { get; set; }
        public int PendingFilings { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class AdminClientDetailDto : AdminClientOverviewDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? TIN { get; set; }
        public decimal AnnualTurnover { get; set; }
        public string? UserEmail { get; set; }
        public DateTime? UserLastLogin { get; set; }
        public int CompletedFilings { get; set; }
        public int OverdueFilings { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public int PendingPayments { get; set; }
        public int TotalDocuments { get; set; }
        public IEnumerable<Document> RecentDocuments { get; set; } = new List<Document>();
    }

    public class AdminClientStatsDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int SuspendedClients { get; set; }
        public int ClientsWithPortalAccess { get; set; }
        public int RecentRegistrations { get; set; }
        public int RecentActiveUsers { get; set; }
    }

    public class AuditLogDto
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IPAddress { get; set; }
        public bool IsSuccess { get; set; }
        public string ActionType { get; set; } = string.Empty;
    }

    public class ClientActivationDto
    {
        public int ClientId { get; set; }
        public bool Activate { get; set; }
        public string? Reason { get; set; }
    }

    public class AssignAssociateDto
    {
        public int ClientId { get; set; }
        public string AssociateUserId { get; set; } = string.Empty;
    }

    public class AdminAssociateDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AssignedClientsCount { get; set; }
        public bool IsActive { get; set; }
    }
}