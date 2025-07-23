using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs
{
    public class DashboardDto
    {
        public ClientSummaryDto ClientSummary { get; set; }
        public ComplianceOverviewDto ComplianceOverview { get; set; }
        public IEnumerable<RecentActivityDto> RecentActivity { get; set; }
        public IEnumerable<UpcomingDeadlineDto> UpcomingDeadlines { get; set; }
        public IEnumerable<PendingApprovalDto> PendingApprovals { get; set; }
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
        public Dictionary<string, int> TaxTypeBreakdown { get; set; }
        public Dictionary<string, double> MonthlyRevenue { get; set; }
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } // "document", "payment", "client", "filing"
        public string Action { get; set; } // "created", "updated", "deleted", "uploaded", etc.
        public string Description { get; set; }
        public string EntityName { get; set; } // Client name, document name, etc.
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    public class UpcomingDeadlineDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; } // "filing", "payment", etc.
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public bool IsUrgent { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class PendingApprovalDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string SubmittedBy { get; set; }
        public string Type { get; set; } // "payment", "filing", etc.
        public string Status { get; set; }
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
        public ClientComplianceOverviewDto ComplianceOverview { get; set; }
        public IEnumerable<RecentActivityDto> RecentActivity { get; set; }
        public IEnumerable<UpcomingDeadlineDto> UpcomingDeadlines { get; set; }
        public ClientBusinessInfoDto BusinessInfo { get; set; }
        public ClientQuickActionsDto QuickActions { get; set; }
    }

    public class ClientComplianceOverviewDto
    {
        public int TotalFilings { get; set; }
        public int CompletedFilings { get; set; }
        public int PendingFilings { get; set; }
        public int LateFilings { get; set; }
        public decimal ComplianceScore { get; set; }
        public string ComplianceStatus { get; set; } // "Compliant", "Warning", "Overdue"
        public Dictionary<string, int> TaxTypeBreakdown { get; set; }
        public Dictionary<string, double> MonthlyPayments { get; set; }
    }

    public class ClientBusinessInfoDto
    {
        public int ClientId { get; set; }
        public string BusinessName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string TIN { get; set; }
        public string TaxpayerCategory { get; set; }
        public string ClientType { get; set; }
        public string Status { get; set; }
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
