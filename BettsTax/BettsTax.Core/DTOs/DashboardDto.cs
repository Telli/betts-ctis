using System;
using System.Collections.Generic;
using BettsTax.Core.DTOs.Compliance;

namespace BettsTax.Core.DTOs
{
    public class DashboardDto
    {
        public ClientSummaryDto ClientSummary { get; set; } = new();
        public ComplianceOverviewDto ComplianceOverview { get; set; } = new();
        public IEnumerable<RecentActivityDto> RecentActivity { get; set; } = new List<RecentActivityDto>();
        public IEnumerable<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new List<UpcomingDeadlineDto>();
        public IEnumerable<PendingApprovalDto> PendingApprovals { get; set; } = new List<PendingApprovalDto>();
    }

    public class ClientSummaryDto
    {
        public int TotalClients { get; set; }
        public int CompliantClients { get; set; }
        public int PendingClients { get; set; }
        public int WarningClients { get; set; }
        public int OverdueClients { get; set; }
    }

    public class ComplianceOverviewDto
    {
        public int TotalFilings { get; set; }
        public int CompletedFilings { get; set; }
        public int PendingFilings { get; set; }
        public int LateFilings { get; set; }
        public Dictionary<string, int> TaxTypeBreakdown { get; set; } = new();
        public Dictionary<string, double> MonthlyRevenue { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "document", "payment", "client", "filing"
        public string Action { get; set; } = string.Empty; // "created", "updated", "deleted", "uploaded", etc.
        public string Description { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty; // Client name, document name, etc.
        public int? ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }


    public class PendingApprovalDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "payment", "filing", etc.
        public string Status { get; set; } = string.Empty;
    }

    public class NavigationCountsDto
    {
        public int TotalClients { get; set; }
        public int TotalTaxFilings { get; set; }
        public int UpcomingDeadlines { get; set; }
        public int UnreadNotifications { get; set; }
    }

    // Client-specific dashboard DTOs
    public class ClientDashboardDto
    {
        public ClientComplianceOverviewDto ComplianceOverview { get; set; } = new();
        public IEnumerable<RecentActivityDto> RecentActivity { get; set; } = new List<RecentActivityDto>();
        public IEnumerable<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new List<UpcomingDeadlineDto>();
        public ClientBusinessInfoDto BusinessInfo { get; set; } = new();
        public ClientQuickActionsDto QuickActions { get; set; } = new();
    }

    public class ClientComplianceOverviewDto
    {
        public int TotalFilings { get; set; }
        public int CompletedFilings { get; set; }
        public int PendingFilings { get; set; }
        public int LateFilings { get; set; }
        public decimal ComplianceScore { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty; // "Compliant", "Warning", "Overdue"
        public Dictionary<string, int> TaxTypeBreakdown { get; set; } = new();
        public Dictionary<string, double> MonthlyPayments { get; set; } = new();
    }

    public class ClientBusinessInfoDto
    {
        public int ClientId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public string TaxpayerCategory { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ClientQuickActionsDto
    {
        public bool CanUploadDocuments { get; set; }
        public bool CanSubmitTaxFiling { get; set; }
        public bool CanMakePayment { get; set; }
        public bool HasPendingFilings { get; set; }
        public bool HasOverduePayments { get; set; }
        public int PendingDocumentCount { get; set; }
        public int UpcomingDeadlineCount { get; set; }
    }
}
