using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComplianceOverviewDto = BettsTax.Core.DTOs.ComplianceOverviewDto;
using ComplianceRiskLevel = BettsTax.Data.Models.ComplianceRiskLevel;


namespace BettsTax.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public DashboardService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(string userId)
        {
            return new DashboardDto
            {
                ClientSummary = await GetClientSummaryAsync(),
                ComplianceOverview = await GetComplianceOverviewAsync(),
                RecentActivity = await GetRecentActivityAsync(),
                UpcomingDeadlines = await GetUpcomingDeadlinesAsync(),
                PendingApprovals = await GetPendingApprovalsAsync(userId),
                Metrics = await GetDashboardMetricsAsync()
            };
        }

        public async Task<ClientSummaryDto> GetClientSummaryAsync()
        {
            var clients = await _db.Clients.AsNoTracking().ToListAsync();
            
            return new ClientSummaryDto
            {
                TotalClients = clients.Count,
                CompliantClients = clients.Count(c => c.Status == ClientStatus.Active),
                PendingClients = clients.Count(c => c.Status == ClientStatus.Inactive),
                WarningClients = 0, // This is not directly mapped in the model, could be calculated based on business logic
                OverdueClients = clients.Count(c => c.Status == ClientStatus.Suspended)
            };
        }

        public async Task<ComplianceOverviewDto> GetComplianceOverviewAsync()
        {
            var taxYears = await _db.TaxYears.AsNoTracking().ToListAsync();
            var payments = await _db.Payments.AsNoTracking().ToListAsync();
            
            // Calculate monthly revenue for the past 12 months
            var monthlyRevenue = new Dictionary<string, double>();
            var today = DateTime.Today;
            for (int i = 0; i < 12; i++)
            {
                var month = today.AddMonths(-i);
                var monthName = month.ToString("MMM yy");
                var monthlyPayments = payments.Where(p => 
                    p.CreatedAt.Year == month.Year && 
                    p.CreatedAt.Month == month.Month && 
                    p.Status == PaymentStatus.Approved);
                    
                monthlyRevenue.Add(monthName, (double)monthlyPayments.Sum(p => p.Amount));
            }

            // Get tax type breakdown (using tax year as a proxy since there's no specific tax type field)
            var taxTypeBreakdown = taxYears.GroupBy(t => t.Year)
                .ToDictionary(g => $"Tax Year {g.Key}", g => g.Count());

            return new ComplianceOverviewDto
            {
                TotalFilings = taxYears.Count,
                CompletedFilings = taxYears.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid),
                PendingFilings = taxYears.Count(t => t.Status == TaxYearStatus.Pending || t.Status == TaxYearStatus.Draft),
                LateFilings = taxYears.Count(t => t.Status == TaxYearStatus.Overdue),
                TaxTypeBreakdown = taxTypeBreakdown,
                MonthlyRevenue = monthlyRevenue
            };
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10)
        {
            var activities = new List<RecentActivityDto>();

            // Get recent documents
            var documents = await _db.Documents
                .AsNoTracking()
                .Include(d => d.Client)
                .OrderByDescending(d => d.UploadedAt)
                .Take(count)
                .ToListAsync();

            foreach (var doc in documents)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = doc.DocumentId,
                    Type = "document",
                    Action = "uploaded",
                    Description = $"Document uploaded: {doc.OriginalFileName}",
                    EntityName = doc.OriginalFileName,
                    ClientId = doc.ClientId,
                    ClientName = doc.Client?.BusinessName ?? "Unknown Client",
                    Timestamp = doc.UploadedAt,
                    UserId = string.Empty, // Document doesn't have UploadedById property
                    UserName = string.Empty // We'll leave this empty as we don't have the user info readily available
                });
            }

            // Get recent payments
            var payments = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Client)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            foreach (var payment in payments)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = payment.PaymentId,
                    Type = "payment",
                    Action = payment.Status.ToString().ToLower(),
                    Description = $"Payment {payment.Status.ToString().ToLower()}: ${payment.Amount}",
                    EntityName = $"${payment.Amount}",
                    ClientId = payment.ClientId,
                    ClientName = payment.Client?.BusinessName ?? "Unknown Client",
                    Timestamp = payment.CreatedAt,
                    UserId = string.Empty,
                    UserName = string.Empty
                });
            }

            // Get recently modified clients
            var clients = await _db.Clients
                .AsNoTracking()
                .OrderByDescending(c => c.UpdatedDate)
                .Take(count)
                .ToListAsync();

            foreach (var client in clients)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = client.ClientId,
                    Type = "client",
                    Action = "updated",
                    Description = $"Client updated: {client.BusinessName}",
                    EntityName = client.BusinessName,
                    ClientId = client.ClientId,
                    ClientName = client.BusinessName,
                    Timestamp = client.UpdatedDate,
                    UserId = client.UserId,
                    UserName = string.Empty
                });
            }

            // Return the most recent activities
            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count);
        }

        public async Task<IEnumerable<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int days = 30)
        {
            var deadlines = new List<UpcomingDeadlineDto>();
            var endDate = DateTime.Today.AddDays(days);

            // Get tax filings with upcoming deadlines
            var taxYears = await _db.TaxYears
                .AsNoTracking()
                .Include(t => t.Client)
                .Where(t => t.FilingDeadline != null && t.FilingDeadline <= endDate && 
                      (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending))
                .ToListAsync();

            foreach (var tax in taxYears)
            {
                if (tax.FilingDeadline.HasValue)
                {
                    var daysRemaining = (tax.FilingDeadline.Value - DateTime.Today).Days;
                
                    deadlines.Add(new UpcomingDeadlineDto
                    {
                        Id = tax.TaxYearId,
                        TaxType = TaxType.IncomeTax, // Default value, should be determined from context
                        TaxTypeName = "Income Tax",
                        DueDate = tax.FilingDeadline.Value,
                        DaysRemaining = daysRemaining,
                        Priority = daysRemaining <= 7 ? BettsTax.Data.ComplianceRiskLevel.High : BettsTax.Data.ComplianceRiskLevel.Medium,
                        PriorityName = daysRemaining <= 7 ? "High" : "Medium",
                        Status = FilingStatus.Draft,
                        StatusName = "Draft",
                        EstimatedTaxLiability = 0,
                        DocumentsReady = false,
                        IsOverdue = daysRemaining < 0,
                        PotentialPenalty = 0,
                        Requirements = $"Tax filing for {tax.Client?.BusinessName}"
                    });
                }
            }

            // Return the soonest deadlines first
            return deadlines.OrderBy(d => d.DueDate);
        }

        public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(string userId)
        {
            var approvals = new List<PendingApprovalDto>();

            // Get payments pending approval
            var payments = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Client)
                .Where(p => p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var payment in payments)
            {
                approvals.Add(new PendingApprovalDto
                {
                    Id = payment.PaymentId,
                    ClientId = payment.ClientId,
                    ClientName = payment.Client?.BusinessName ?? "Unknown Client",
                    Amount = payment.Amount,
                    Description = $"Payment for {payment.Client?.BusinessName ?? "Unknown Client"}",
                    SubmittedDate = payment.CreatedAt,
                    SubmittedBy = "System", // This isn't tracked in the current model
                    Type = "payment",
                    Status = payment.Status.ToString()
                });
            }

            return approvals;
        }

        public async Task<NavigationCountsDto> GetNavigationCountsAsync(string userId)
        {
            // Use efficient COUNT queries to avoid loading data into memory
            var totalClients = await _db.Clients.CountAsync();
            
            var totalTaxFilings = await _db.TaxYears.CountAsync();
            
            // Count upcoming deadlines within next 30 days
            var endDate = DateTime.Today.AddDays(30);
            var upcomingDeadlines = await _db.TaxYears
                .Where(t => t.FilingDeadline != null && 
                           t.FilingDeadline <= endDate && 
                           (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending))
                .CountAsync();
            
            // For notifications, we'll use pending approvals as a proxy
            var unreadNotifications = await _db.Payments
                .Where(p => p.Status == PaymentStatus.Pending)
                .CountAsync();

            return new NavigationCountsDto
            {
                TotalClients = totalClients,
                TotalTaxFilings = totalTaxFilings,
                UpcomingDeadlines = upcomingDeadlines,
                UnreadNotifications = unreadNotifications
            };
        }

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
        {
            // Get current month's data
            var currentMonth = DateTime.Today;
            var lastMonth = currentMonth.AddMonths(-1);

            // Calculate Compliance Rate
            var currentMonthTaxYears = await _db.TaxYears
                .Where(t => t.UpdatedDate >= currentMonth.AddDays(-30) && t.UpdatedDate <= currentMonth)
                .ToListAsync();

            var lastMonthTaxYears = await _db.TaxYears
                .Where(t => t.UpdatedDate >= lastMonth.AddDays(-30) && t.UpdatedDate <= lastMonth)
                .ToListAsync();

            var currentCompliance = currentMonthTaxYears.Count > 0
                ? (decimal)currentMonthTaxYears.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid) / currentMonthTaxYears.Count * 100
                : 0m;

            var lastCompliance = lastMonthTaxYears.Count > 0
                ? (decimal)lastMonthTaxYears.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid) / lastMonthTaxYears.Count * 100
                : 0m;

            var complianceTrend = currentCompliance - lastCompliance;

            // Calculate Filing Timeliness (average days before deadline)
            var recentFilings = await _db.TaxYears
                .Where(t => t.FilingDate != null && t.FilingDeadline != null &&
                           t.FilingDate >= currentMonth.AddDays(-30))
                .ToListAsync();

            var avgDaysBeforeDeadline = recentFilings.Count > 0
                ? (int)recentFilings.Average(t => (t.FilingDeadline!.Value - t.FilingDate!.Value).TotalDays)
                : 0;

            var lastMonthFilings = await _db.TaxYears
                .Where(t => t.FilingDate != null && t.FilingDeadline != null &&
                           t.FilingDate >= lastMonth.AddDays(-30) && t.FilingDate < currentMonth.AddDays(-30))
                .ToListAsync();

            var lastAvgDaysBeforeDeadline = lastMonthFilings.Count > 0
                ? (int)lastMonthFilings.Average(t => (t.FilingDeadline!.Value - t.FilingDate!.Value).TotalDays)
                : 0;

            var timelinessTrendDays = avgDaysBeforeDeadline - lastAvgDaysBeforeDeadline;

            // Calculate Payment On-Time Rate
            var currentPayments = await _db.Payments
                .Where(p => p.CreatedAt >= currentMonth.AddDays(-30) && p.CreatedAt <= currentMonth)
                .ToListAsync();

            var onTimePayments = currentPayments.Count(p => p.Status == PaymentStatus.Approved);
            var paymentOnTimeRate = currentPayments.Count > 0
                ? (decimal)onTimePayments / currentPayments.Count * 100
                : 0m;

            var lastMonthPayments = await _db.Payments
                .Where(p => p.CreatedAt >= lastMonth.AddDays(-30) && p.CreatedAt < currentMonth.AddDays(-30))
                .ToListAsync();

            var lastOnTimePayments = lastMonthPayments.Count(p => p.Status == PaymentStatus.Approved);
            var lastPaymentOnTimeRate = lastMonthPayments.Count > 0
                ? (decimal)lastOnTimePayments / lastMonthPayments.Count * 100
                : 0m;

            var paymentTrend = paymentOnTimeRate - lastPaymentOnTimeRate;

            // Calculate Document Submission Rate
            var requiredDocuments = await _db.TaxYears
                .Where(t => t.UpdatedDate >= currentMonth.AddDays(-30))
                .CountAsync();

            var submittedDocuments = await _db.Documents
                .Where(d => d.UploadedAt >= currentMonth.AddDays(-30))
                .CountAsync();

            var documentRate = requiredDocuments > 0
                ? (decimal)submittedDocuments / requiredDocuments * 100
                : 100m;

            var lastRequiredDocuments = await _db.TaxYears
                .Where(t => t.UpdatedDate >= lastMonth.AddDays(-30) && t.UpdatedDate < currentMonth.AddDays(-30))
                .CountAsync();

            var lastSubmittedDocuments = await _db.Documents
                .Where(d => d.UploadedAt >= lastMonth.AddDays(-30) && d.UploadedAt < currentMonth.AddDays(-30))
                .CountAsync();

            var lastDocumentRate = lastRequiredDocuments > 0
                ? (decimal)lastSubmittedDocuments / lastRequiredDocuments * 100
                : 100m;

            var documentTrend = documentRate - lastDocumentRate;

            return new DashboardMetricsDto
            {
                ComplianceRate = Math.Round(currentCompliance, 1),
                ComplianceRateTrend = complianceTrend >= 0 ? $"+{Math.Abs(Math.Round(complianceTrend, 1))}%" : $"-{Math.Abs(Math.Round(complianceTrend, 1))}%",
                ComplianceRateTrendUp = complianceTrend >= 0,

                FilingTimelinessAvgDays = Math.Max(0, avgDaysBeforeDeadline),
                FilingTimelinessTrend = timelinessTrendDays >= 0 ? $"+{Math.Abs(timelinessTrendDays)} days" : $"-{Math.Abs(timelinessTrendDays)} days",
                FilingTimelinessTrendUp = timelinessTrendDays >= 0,

                PaymentOnTimeRate = Math.Round(paymentOnTimeRate, 1),
                PaymentOnTimeRateTrend = paymentTrend >= 0 ? $"+{Math.Abs(Math.Round(paymentTrend, 1))}%" : $"-{Math.Abs(Math.Round(paymentTrend, 1))}%",
                PaymentOnTimeRateTrendUp = paymentTrend >= 0,

                DocumentSubmissionRate = Math.Round(documentRate, 1),
                DocumentSubmissionRateTrend = documentTrend >= 0 ? $"+{Math.Abs(Math.Round(documentTrend, 1))}%" : $"-{Math.Abs(Math.Round(documentTrend, 1))}%",
                DocumentSubmissionRateTrendUp = documentTrend >= 0
            };
        }

        // Client-specific dashboard methods
        public async Task<ClientDashboardDto> GetClientDashboardDataAsync(int clientId)
        {
            return new ClientDashboardDto
            {
                ComplianceOverview = await GetClientComplianceOverviewAsync(clientId),
                RecentActivity = await GetClientRecentActivityAsync(clientId),
                UpcomingDeadlines = await GetClientUpcomingDeadlinesAsync(clientId),
                BusinessInfo = await GetClientBusinessInfoAsync(clientId),
                QuickActions = await GetClientQuickActionsAsync(clientId)
            };
        }

        public async Task<ClientComplianceOverviewDto> GetClientComplianceOverviewAsync(int clientId)
        {
            var taxYears = await _db.TaxYears
                .AsNoTracking()
                .Where(t => t.ClientId == clientId)
                .ToListAsync();

            var payments = await _db.Payments
                .AsNoTracking()
                .Include(p => p.TaxFiling)
                .Where(p => p.TaxFiling.ClientId == clientId)
                .ToListAsync();

            // Calculate compliance score based on filing status and payment history
            var complianceScore = CalculateComplianceScore(taxYears, payments);
            var complianceStatus = GetComplianceStatus(complianceScore);

            // Calculate monthly payments for the past 12 months
            var monthlyPayments = new Dictionary<string, double>();
            var today = DateTime.Today;
            for (int i = 0; i < 12; i++)
            {
                var month = today.AddMonths(-i);
                var monthName = month.ToString("MMM yy");
                var monthlyPaymentSum = payments
                    .Where(p => p.CreatedAt.Year == month.Year && 
                               p.CreatedAt.Month == month.Month && 
                               p.Status == PaymentStatus.Approved)
                    .Sum(p => (double)p.Amount);
                    
                monthlyPayments.Add(monthName, monthlyPaymentSum);
            }

            // Get tax type breakdown from tax filings
            var taxFilings = await _db.TaxFilings.Where(tf => tf.ClientId == clientId).ToListAsync();
            var taxTypeBreakdown = taxFilings
                .GroupBy(t => t.TaxType.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return new ClientComplianceOverviewDto
            {
                TotalFilings = taxYears.Count,
                CompletedFilings = taxYears.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid),
                PendingFilings = taxYears.Count(t => t.Status == TaxYearStatus.Pending || t.Status == TaxYearStatus.Draft),
                LateFilings = taxYears.Count(t => t.Status == TaxYearStatus.Overdue),
                ComplianceScore = complianceScore,
                ComplianceStatus = complianceStatus,
                TaxTypeBreakdown = taxTypeBreakdown,
                MonthlyPayments = monthlyPayments
            };
        }

        public async Task<IEnumerable<RecentActivityDto>> GetClientRecentActivityAsync(int clientId, int count = 10)
        {
            var activities = new List<RecentActivityDto>();

            // Get recent documents for this client
            var documents = await _db.Documents
                .AsNoTracking()
                .Include(d => d.Client)
                .Where(d => d.ClientId == clientId)
                .OrderByDescending(d => d.UploadedAt)
                .Take(count)
                .ToListAsync();

            foreach (var doc in documents)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = doc.DocumentId,
                    Type = "document",
                    Action = "uploaded",
                    Description = $"Document uploaded: {doc.OriginalFileName}",
                    EntityName = doc.OriginalFileName,
                    ClientId = doc.ClientId,
                    ClientName = doc.Client?.BusinessName,
                    Timestamp = doc.UploadedAt,
                    UserId = null,
                    UserName = null
                });
            }

            // Get recent payments for this client
            var payments = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Client)
                .Include(p => p.TaxFiling)
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            foreach (var payment in payments)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = payment.PaymentId,
                    Type = "payment",
                    Action = payment.Status.ToString().ToLower(),
                    Description = $"Payment {payment.Status.ToString().ToLower()}: ${payment.Amount}",
                    EntityName = $"${payment.Amount}",
                    ClientId = payment.ClientId,
                    ClientName = payment.Client?.BusinessName,
                    Timestamp = payment.CreatedAt,
                    UserId = null,
                    UserName = null
                });
            }

            // Return the most recent activities
            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count);
        }

        public async Task<IEnumerable<UpcomingDeadlineDto>> GetClientUpcomingDeadlinesAsync(int clientId, int days = 30)
        {
            var deadlines = new List<UpcomingDeadlineDto>();
            var endDate = DateTime.Today.AddDays(days);

            // Get tax filings with upcoming deadlines for this client
            var taxYears = await _db.TaxYears
                .AsNoTracking()
                .Include(t => t.Client)
                .Where(t => t.ClientId == clientId &&
                           t.FilingDeadline != null && 
                           t.FilingDeadline <= endDate && 
                           (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending))
                .ToListAsync();

            foreach (var tax in taxYears)
            {
                if (tax.FilingDeadline.HasValue)
                {
                    var daysRemaining = (tax.FilingDeadline.Value - DateTime.Today).Days;
                
                    deadlines.Add(new UpcomingDeadlineDto
                    {
                        Id = tax.TaxYearId,
                        TaxType = TaxType.IncomeTax, // Default value, should be determined from context
                        TaxTypeName = "Income Tax",
                        DueDate = tax.FilingDeadline.Value,
                        DaysRemaining = daysRemaining,
                        Priority = daysRemaining <= 7 ? BettsTax.Data.ComplianceRiskLevel.High : BettsTax.Data.ComplianceRiskLevel.Medium,
                        PriorityName = daysRemaining <= 7 ? "High" : "Medium",
                        Status = FilingStatus.Draft,
                        StatusName = "Draft",
                        EstimatedTaxLiability = 0,
                        DocumentsReady = false,
                        IsOverdue = daysRemaining < 0,
                        PotentialPenalty = 0,
                        Requirements = $"Tax filing for {tax.Client?.BusinessName}"
                    });
                }
            }

            return deadlines.OrderBy(d => d.DueDate);
        }

        private async Task<ClientBusinessInfoDto> GetClientBusinessInfoAsync(int clientId)
        {
            var client = await _db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null)
                return new ClientBusinessInfoDto();

            return new ClientBusinessInfoDto
            {
                ClientId = client.ClientId,
                BusinessName = client.BusinessName,
                ContactPerson = client.ContactPerson,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                TIN = client.TIN ?? "Not Provided",
                TaxpayerCategory = client.TaxpayerCategory.ToString(),
                ClientType = client.ClientType.ToString(),
                Status = client.Status.ToString()
            };
        }

        private async Task<ClientQuickActionsDto> GetClientQuickActionsAsync(int clientId)
        {
            var client = await _db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null)
                return new ClientQuickActionsDto();

            var pendingFilings = await _db.TaxYears
                .CountAsync(t => t.ClientId == clientId && 
                               (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending));

            var overduePayments = await _db.Payments
                .CountAsync(p => p.ClientId == clientId && 
                               p.Status == PaymentStatus.Pending && 
                               p.CreatedAt < DateTime.Today.AddDays(-30));

            var upcomingDeadlines = await _db.TaxYears
                .CountAsync(t => t.ClientId == clientId &&
                               t.FilingDeadline != null && 
                               t.FilingDeadline <= DateTime.Today.AddDays(30) && 
                               (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending));

            return new ClientQuickActionsDto
            {
                CanUploadDocuments = client.Status == ClientStatus.Active,
                CanSubmitTaxFiling = client.Status == ClientStatus.Active,
                CanMakePayment = client.Status == ClientStatus.Active,
                HasPendingFilings = pendingFilings > 0,
                HasOverduePayments = overduePayments > 0,
                PendingDocumentCount = 0, // This would need document status tracking
                UpcomingDeadlineCount = upcomingDeadlines
            };
        }

        private decimal CalculateComplianceScore(IEnumerable<TaxYear> taxYears, IEnumerable<Payment> payments)
        {
            if (!taxYears.Any())
                return 100m; // New client, assume compliant

            var totalFilings = taxYears.Count();
            var completedFilings = taxYears.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid);
            var lateFilings = taxYears.Count(t => t.Status == TaxYearStatus.Overdue);

            // Base score on filing completion rate
            var filingScore = totalFilings > 0 ? (decimal)completedFilings / totalFilings * 100 : 100m;

            // Penalty for late filings
            var latePenalty = lateFilings * 10; // 10 points per late filing

            var finalScore = Math.Max(0, filingScore - latePenalty);
            return Math.Min(100, finalScore);
        }

        private string GetComplianceStatus(decimal score)
        {
            return score switch
            {
                >= 90 => "Compliant",
                >= 70 => "Warning",
                _ => "Overdue"
            };
        }
    }
}
