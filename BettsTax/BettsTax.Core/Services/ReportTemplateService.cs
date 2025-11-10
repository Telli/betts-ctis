using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services;

public class ReportTemplateService : IReportTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IClientService _clientService;
    private readonly ITaxFilingService _taxFilingService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ReportTemplateService> _logger;

    public ReportTemplateService(
        ApplicationDbContext context,
        IClientService clientService,
        ITaxFilingService taxFilingService,
        IPaymentService paymentService,
        ILogger<ReportTemplateService> logger)
    {
        _context = context;
        _clientService = clientService;
        _taxFilingService = taxFilingService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<TaxFilingReportDataDto> GetTaxFilingReportDataAsync(int clientId, int taxYear)
    {
        try
        {
            var client = await _clientService.GetByIdAsync(clientId);
            if (client == null)
                throw new ArgumentException($"Client with ID {clientId} not found");

            var filings = await _context.TaxFilings
                .Where(tf => tf.ClientId == clientId && tf.TaxYear == taxYear)
                .Include(tf => tf.Payments)
                .ToListAsync();

            var reportItems = filings.Select(filing => new TaxFilingReportItem
            {
                TaxFilingId = filing.TaxFilingId,
                TaxType = filing.TaxType,
                TaxTypeName = filing.TaxType.ToString(),
                FilingDate = filing.FilingDate,
                DueDate = filing.DueDate ?? DateTime.MinValue,
                Status = filing.Status,
                StatusName = filing.Status.ToString(),
                TaxLiability = filing.TaxLiability,
                FilingReference = filing.FilingReference ?? string.Empty,
                DaysFromDue = (int)filing.FilingDate.Subtract(filing.DueDate ?? DateTime.MinValue).TotalDays
            }).ToList();

            var summary = new ReportSummary
            {
                TotalRecords = reportItems.Count,
                TotalAmount = reportItems.Sum(r => r.TaxLiability),
                FormattedTotalAmount = $"SLE {reportItems.Sum(r => r.TaxLiability):N2}",
                Metrics = new Dictionary<string, object>
                {
                    ["OnTimeFilings"] = reportItems.Count(r => r.IsOnTime),
                    ["LateFilings"] = reportItems.Count(r => !r.IsOnTime),
                    ["AverageDelay"] = reportItems.Where(r => !r.IsOnTime).Average(r => (double?)r.DaysFromDue) ?? 0
                },
                Highlights = new List<string>
                {
                    $"Total tax liability: SLE {reportItems.Sum(r => r.TaxLiability):N2}",
                    $"Filing compliance rate: {(reportItems.Count > 0 ? reportItems.Count(r => r.IsOnTime) * 100.0 / reportItems.Count : 0):F1}%"
                }
            };

            return new TaxFilingReportDataDto
            {
                Title = $"Tax Filing Report - {taxYear}",
                Subtitle = $"Report for {client.BusinessName}",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                ClientId = clientId,
                ClientName = client.BusinessName,
                TIN = client.TIN ?? "Not Available",
                TaxYear = taxYear,
                Filings = reportItems,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax filing report data for client {ClientId}, year {TaxYear}", clientId, taxYear);
            throw;
        }
    }

    public async Task<PaymentHistoryReportDataDto> GetPaymentHistoryReportDataAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var client = await _clientService.GetByIdAsync(clientId);
            if (client == null)
                throw new ArgumentException($"Client with ID {clientId} not found");

            var payments = await _context.Payments
                .Where(p => p.TaxFiling!.ClientId == clientId && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
                .Include(p => p.TaxFiling)
                .ToListAsync();

            var reportItems = payments.Select(payment => new PaymentReportItem
            {
                PaymentId = payment.PaymentId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                FormattedAmount = $"SLE {payment.Amount:N2}",
                Method = payment.Method,
                MethodName = payment.Method.ToString(),
                Status = payment.Status,
                StatusName = payment.Status.ToString(),
                Reference = payment.PaymentReference ?? string.Empty,
                TaxType = payment.TaxFiling?.TaxType,
                TaxTypeName = payment.TaxFiling?.TaxType.ToString(),
                TaxYear = payment.TaxFiling?.TaxYear
            }).ToList();

            var summary = new ReportSummary
            {
                TotalRecords = reportItems.Count,
                TotalAmount = reportItems.Sum(r => r.Amount),
                FormattedTotalAmount = $"SLE {reportItems.Sum(r => r.Amount):N2}",
                Metrics = new Dictionary<string, object>
                {
                    ["ApprovedPayments"] = reportItems.Count(r => r.Status == PaymentStatus.Approved),
                    ["PendingPayments"] = reportItems.Count(r => r.Status == PaymentStatus.Pending),
                    ["RejectedPayments"] = reportItems.Count(r => r.Status == PaymentStatus.Rejected)
                },
                Highlights = new List<string>
                {
                    $"Total payments: SLE {reportItems.Sum(r => r.Amount):N2}",
                    $"Success rate: {(reportItems.Count > 0 ? reportItems.Count(r => r.Status == PaymentStatus.Approved) * 100.0 / reportItems.Count : 0):F1}%"
                }
            };

            return new PaymentHistoryReportDataDto
            {
                Title = "Payment History Report",
                Subtitle = $"Report for {client.BusinessName} ({fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd})",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                ClientId = clientId,
                ClientName = client.BusinessName,
                TIN = client.TIN ?? "Not Available",
                FromDate = fromDate,
                ToDate = toDate,
                Payments = reportItems,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment history report data for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<ComplianceReportDataDto> GetComplianceReportDataAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var client = await _clientService.GetByIdAsync(clientId);
            if (client == null)
                throw new ArgumentException($"Client with ID {clientId} not found");

            var latestScore = await _context.ComplianceScores
                .Where(cs => cs.ClientId == clientId)
                .OrderByDescending(cs => cs.CalculatedAt)
                .FirstOrDefaultAsync();

            var filings = await _context.TaxFilings
                .Where(tf => tf.ClientId == clientId && tf.FilingDate >= fromDate && tf.FilingDate <= toDate)
                .ToListAsync();

            var complianceItems = filings
                .GroupBy(f => f.TaxType)
                .Select(g => new ComplianceReportItem
                {
                    TaxType = g.Key,
                    TaxTypeName = g.Key.ToString(),
                    TotalFilings = g.Count(),
                    OnTimeFilings = g.Count(f => f.FilingDate <= f.DueDate),
                    LateFilings = g.Count(f => f.FilingDate > f.DueDate),
                    MissedDeadlines = g.Count(f => f.SubmittedDate == null && f.DueDate < DateTime.UtcNow),
                    ComplianceRate = g.Count() > 0 ? g.Count(f => f.FilingDate <= f.DueDate) * 100m / g.Count() : 0m,
                    ComplianceGrade = GetComplianceGrade(g.Count() > 0 ? g.Count(f => f.FilingDate <= f.DueDate) * 100m / g.Count() : 0m)
                }).ToList();

            // For now, create empty penalty list since CompliancePenalty table structure needs to be defined
            var penaltyItems = new List<PenaltyReportItem>();

            var overallScore = latestScore?.OverallScore ?? 0m;
            var complianceLevel = overallScore >= 85 ? ComplianceLevel.Green :
                                 overallScore >= 70 ? ComplianceLevel.Yellow :
                                 ComplianceLevel.Red;

            var summary = new ReportSummary
            {
                TotalRecords = complianceItems.Sum(ci => ci.TotalFilings),
                TotalAmount = penaltyItems.Sum(pi => pi.PenaltyAmount),
                FormattedTotalAmount = $"SLE {penaltyItems.Sum(pi => pi.PenaltyAmount):N2}",
                Metrics = new Dictionary<string, object>
                {
                    ["OverallComplianceRate"] = complianceItems.Any() ? complianceItems.Average(ci => ci.ComplianceRate) : 0m,
                    ["TotalPenalties"] = penaltyItems.Sum(pi => pi.PenaltyAmount),
                    ["UnpaidPenalties"] = penaltyItems.Where(pi => !pi.IsPaid).Sum(pi => pi.PenaltyAmount)
                },
                Highlights = new List<string>
                {
                    $"Overall compliance score: {overallScore:F1}%",
                    $"Total penalties incurred: SLE {penaltyItems.Sum(pi => pi.PenaltyAmount):N2}",
                    $"Compliance level: {complianceLevel}"
                }
            };

            return new ComplianceReportDataDto
            {
                Title = "Compliance Report",
                Subtitle = $"Report for {client.BusinessName} ({fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd})",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                ClientId = clientId,
                ClientName = client.BusinessName,
                TIN = client.TIN ?? "Not Available",
                FromDate = fromDate,
                ToDate = toDate,
                OverallComplianceScore = overallScore,
                ComplianceLevel = complianceLevel,
                Items = complianceItems,
                Penalties = penaltyItems,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report data for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<ClientActivityReportDataDto> GetClientActivityReportDataAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var activities = await _context.ActivityTimelines
                .Where(at => at.ActivityDate >= fromDate && at.ActivityDate <= toDate)
                .Include(at => at.Client)
                .ToListAsync();

            var clientActivities = activities
                .GroupBy(a => a.ClientId)
                .Where(g => g.Key.HasValue)
                .Select(g => new ClientActivityReportItem
                {
                    ClientId = g.Key!.Value,
                    ClientName = g.First().Client?.BusinessName ?? "Unknown Client",
                    TIN = g.First().Client?.TIN ?? "Not Available",
                    LastActivity = g.Max(a => a.ActivityDate),
                    TotalActivities = g.Count(),
                    DocumentsUploaded = g.Count(a => a.ActivityType == ActivityType.DocumentUploaded),
                    PaymentsMade = g.Count(a => a.ActivityType == ActivityType.PaymentCreated),
                    TaxFilingsSubmitted = g.Count(a => a.ActivityType == ActivityType.TaxFilingSubmitted),
                    LoginCount = g.Count(a => a.ActivityType == ActivityType.ClientLoggedIn),
                    EngagementLevel = GetEngagementLevel(g.Count())
                }).ToList();

            var activityCounts = activities
                .GroupBy(a => a.ActivityType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var summary = new ReportSummary
            {
                TotalRecords = clientActivities.Count,
                TotalAmount = 0m,
                FormattedTotalAmount = "N/A",
                Metrics = new Dictionary<string, object>
                {
                    ["ActiveClients"] = clientActivities.Count,
                    ["TotalActivities"] = activities.Count,
                    ["AverageActivitiesPerClient"] = clientActivities.Any() ? clientActivities.Average(ca => ca.TotalActivities) : 0
                },
                Highlights = new List<string>
                {
                    $"Total active clients: {clientActivities.Count}",
                    $"Total activities recorded: {activities.Count}",
                    $"Most active client: {clientActivities.OrderByDescending(ca => ca.TotalActivities).FirstOrDefault()?.ClientName ?? "None"}"
                }
            };

            return new ClientActivityReportDataDto
            {
                Title = "Client Activity Report",
                Subtitle = $"Activity summary from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                FromDate = fromDate,
                ToDate = toDate,
                Activities = clientActivities,
                ActivityCounts = activityCounts,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client activity report data");
            throw;
        }
    }

    private static string GetComplianceGrade(decimal complianceRate)
    {
        return complianceRate switch
        {
            >= 95 => "A+",
            >= 90 => "A",
            >= 85 => "B+",
            >= 80 => "B",
            >= 75 => "C+",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }

    private static string GetEngagementLevel(int activityCount)
    {
        return activityCount switch
        {
            >= 50 => "Very High",
            >= 30 => "High",
            >= 15 => "Medium",
            >= 5 => "Low",
            _ => "Very Low"
        };
    }

    public async Task<DocumentSubmissionReportDataDto> GetDocumentSubmissionReportDataAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        var client = await _clientService.GetByIdAsync(clientId);
        if (client == null)
            throw new ArgumentException($"Client with ID {clientId} not found");

        var documents = await _context.Documents
            .Where(d => d.ClientId == clientId && d.UploadedAt >= fromDate && d.UploadedAt <= toDate)
            .Include(d => d.DocumentVerification)
            .ThenInclude(dv => dv!.ReviewedBy)
            .ToListAsync();

        var reportItems = documents.Select(doc => new DocumentSubmissionReportItem
        {
            DocumentId = doc.DocumentId,
            DocumentType = doc.Category.ToString(),
            FileName = doc.OriginalFileName,
            SubmittedDate = doc.UploadedAt,
            DueDate = null, // Documents don't have due dates in the current model
            Status = doc.DocumentVerification?.Status.ToString() ?? "Not Reviewed",
            ReviewedBy = doc.DocumentVerification?.ReviewedBy?.UserName ?? "Not Reviewed",
            ReviewedDate = doc.DocumentVerification?.ReviewedDate,
            ReviewNotes = doc.DocumentVerification?.ReviewNotes
        }).ToList();

        var summary = new DocumentSubmissionSummary
        {
            TotalDocuments = documents.Count,
            ApprovedDocuments = documents.Count(d => d.DocumentVerification?.Status == DocumentVerificationStatus.Verified),
            PendingDocuments = documents.Count(d => d.DocumentVerification?.Status == DocumentVerificationStatus.Submitted ||
                                                   d.DocumentVerification?.Status == DocumentVerificationStatus.UnderReview),
            RejectedDocuments = documents.Count(d => d.DocumentVerification?.Status == DocumentVerificationStatus.Rejected),
            OnTimeSubmissions = 0, // Cannot calculate without due dates
            LateSubmissions = 0
        };

        if (summary.TotalDocuments > 0)
        {
            summary.OnTimePercentage = 0; // Cannot calculate without due dates
            summary.ApprovalRate = (decimal)summary.ApprovedDocuments / summary.TotalDocuments * 100;
        }

        var processedDocs = documents.Where(d => d.DocumentVerification?.ReviewedDate.HasValue == true).ToList();
        if (processedDocs.Any())
        {
            summary.AverageProcessingDays = processedDocs.Average(d =>
                (d.DocumentVerification!.ReviewedDate!.Value - d.UploadedAt).TotalDays);
        }

        return new DocumentSubmissionReportDataDto
        {
            Title = "Document Submission Report",
            Subtitle = $"Client: {client.BusinessName} | Period: {fromDate:MM/dd/yyyy} - {toDate:MM/dd/yyyy}",
            GeneratedAt = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = client.BusinessName,
            TIN = client.TIN ?? "",
            FromDate = fromDate,
            ToDate = toDate,
            Documents = reportItems,
            Summary = summary
        };
    }

    public async Task<TaxCalendarReportDataDto> GetTaxCalendarReportDataAsync(int? clientId, int taxYear)
    {
        var fromDate = new DateTime(taxYear, 1, 1);
        var toDate = new DateTime(taxYear, 12, 31);

        var query = _context.TaxFilings.AsQueryable();
        if (clientId.HasValue)
        {
            query = query.Where(tf => tf.ClientId == clientId.Value);
        }

        var filings = await query
            .Where(tf => tf.TaxYear == taxYear)
            .Include(tf => tf.Client)
            .ToListAsync();

        var reportItems = filings.Select(filing => new TaxCalendarReportItem
        {
            DueDate = filing.DueDate ?? DateTime.MinValue,
            TaxType = filing.TaxType,
            TaxTypeName = filing.TaxType.ToString(),
            Description = $"{filing.TaxType} Filing for {taxYear}",
            ClientName = clientId.HasValue ? null : filing.Client?.BusinessName,
            Status = filing.Status,
            StatusName = filing.Status.ToString(),
            CompletedDate = filing.SubmittedDate
        }).OrderBy(item => item.DueDate).ToList();

        var summary = new TaxCalendarSummary
        {
            TotalDeadlines = reportItems.Count,
            CompletedDeadlines = reportItems.Count(r => r.Status == FilingStatus.Filed),
            OverdueDeadlines = reportItems.Count(r => r.IsOverdue),
            UpcomingDeadlines = reportItems.Count(r => r.DaysUntilDue > 0 && r.Status != FilingStatus.Filed),
            UrgentDeadlines = reportItems.Count(r => r.UrgencyLevel == "Urgent")
        };

        if (summary.TotalDeadlines > 0)
        {
            summary.CompletionRate = (decimal)summary.CompletedDeadlines / summary.TotalDeadlines * 100;
        }

        summary.DeadlinesByTaxType = reportItems
            .GroupBy(r => r.TaxType)
            .ToDictionary(g => g.Key, g => g.Count());

        var clientName = clientId.HasValue ?
            (await _clientService.GetByIdAsync(clientId.Value))?.BusinessName :
            "All Clients";

        return new TaxCalendarReportDataDto
        {
            Title = "Tax Calendar Summary Report",
            Subtitle = $"Client: {clientName} | Tax Year: {taxYear}",
            GeneratedAt = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName,
            TaxYear = taxYear,
            FromDate = fromDate,
            ToDate = toDate,
            Deadlines = reportItems,
            Summary = summary
        };
    }

    public async Task<ClientComplianceOverviewReportDataDto> GetClientComplianceOverviewReportDataAsync(DateTime fromDate, DateTime toDate)
    {
        var clients = await _context.Clients
            .ToListAsync();

        // Get related data separately to avoid complex includes
        var clientIds = clients.Select(c => c.ClientId).ToList();

        var taxFilings = await _context.TaxFilings
            .Where(tf => clientIds.Contains(tf.ClientId) && tf.FilingDate >= fromDate && tf.FilingDate <= toDate)
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => clientIds.Contains(p.ClientId) && p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .ToListAsync();

        var penaltyCalculations = await _context.PenaltyCalculations
            .Where(pc => clientIds.Contains(pc.ClientId) && pc.CalculatedAt >= fromDate && pc.CalculatedAt <= toDate)
            .ToListAsync();

        var reportItems = new List<ClientComplianceOverviewItem>();

        foreach (var client in clients)
        {
            var clientTaxFilings = taxFilings.Where(tf => tf.ClientId == client.ClientId).ToList();
            var clientPayments = payments.Where(p => p.ClientId == client.ClientId).ToList();
            var clientPenalties = penaltyCalculations.Where(pc => pc.ClientId == client.ClientId).ToList();

            var totalFilings = clientTaxFilings.Count;
            var onTimeFilings = clientTaxFilings.Count(tf => tf.SubmittedDate.HasValue && tf.SubmittedDate <= tf.DueDate);
            var lateFilings = totalFilings - onTimeFilings;

            var totalPayments = clientPayments.Count;
            var onTimePayments = clientPayments.Count(p => p.DueDate.HasValue && p.PaymentDate <= p.DueDate.Value);

            var totalPenalties = clientPenalties.Sum(pc => pc.TotalPenalty);

            var lastActivity = await GetLastActivityDateForClient(client.ClientId, fromDate, toDate);
            var complianceScore = CalculateComplianceScore(totalFilings, onTimeFilings, totalPayments, onTimePayments, totalPenalties);
            var complianceLevel = GetComplianceLevel(complianceScore);
            var riskLevel = GetRiskLevel(complianceScore, totalPenalties, lateFilings);

            var complianceIssues = new List<string>();
            if (lateFilings > 0) complianceIssues.Add($"{lateFilings} late filings");
            if (totalPayments - onTimePayments > 0) complianceIssues.Add($"{totalPayments - onTimePayments} late payments");
            if (totalPenalties > 0) complianceIssues.Add($"${totalPenalties:F2} in penalties");

            reportItems.Add(new ClientComplianceOverviewItem
            {
                ClientId = client.ClientId,
                ClientName = client.BusinessName,
                TIN = client.TIN ?? "",
                OverallComplianceScore = complianceScore,
                ComplianceLevel = complianceLevel,
                TotalFilings = totalFilings,
                OnTimeFilings = onTimeFilings,
                LateFilings = lateFilings,
                TotalPayments = totalPayments,
                OnTimePayments = onTimePayments,
                TotalPenalties = totalPenalties,
                LastActivity = lastActivity,
                RiskLevel = riskLevel,
                ComplianceIssues = complianceIssues
            });
        }

        var summary = new ComplianceOverviewSummary
        {
            TotalClients = reportItems.Count,
            HighRiskClients = reportItems.Count(r => r.RiskLevel == "High"),
            MediumRiskClients = reportItems.Count(r => r.RiskLevel == "Medium"),
            LowRiskClients = reportItems.Count(r => r.RiskLevel == "Low"),
            AverageComplianceScore = reportItems.Any() ? reportItems.Average(r => r.OverallComplianceScore) : 0,
            TotalPenaltiesIssued = reportItems.Sum(r => r.TotalPenalties),
            TotalOverdueItems = reportItems.Sum(r => r.LateFilings + (r.TotalPayments - r.OnTimePayments)),
            ClientsByComplianceLevel = reportItems.GroupBy(r => r.ComplianceLevel).ToDictionary(g => g.Key, g => g.Count())
        };

        return new ClientComplianceOverviewReportDataDto
        {
            Title = "Client Compliance Overview Report",
            Subtitle = $"Period: {fromDate:MM/dd/yyyy} - {toDate:MM/dd/yyyy}",
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Clients = reportItems.OrderByDescending(r => r.OverallComplianceScore).ToList(),
            Summary = summary
        };
    }

    public async Task<RevenueReportDataDto> GetRevenueReportDataAsync(DateTime fromDate, DateTime toDate)
    {
        var payments = await _context.Payments
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .Include(p => p.Client)
            .ToListAsync();

        var reportItems = payments.Select(payment => new RevenueReportItem
        {
            Date = payment.PaymentDate,
            ClientId = payment.ClientId,
            ClientName = payment.Client?.BusinessName ?? "Unknown Client",
            TaxType = payment.TaxType ?? TaxType.IncomeTax,
            TaxTypeName = (payment.TaxType ?? TaxType.IncomeTax).ToString(),
            Amount = payment.Amount,
            FormattedAmount = $"${payment.Amount:F2}",
            Status = payment.Status,
            StatusName = payment.Status.ToString(),
            Method = payment.Method,
            MethodName = payment.Method.ToString(),
            Reference = payment.PaymentReference ?? ""
        }).OrderByDescending(r => r.Date).ToList();

        var summary = new RevenueSummary
        {
            TotalRevenue = reportItems.Sum(r => r.Amount),
            CollectedRevenue = reportItems.Where(r => r.Status == PaymentStatus.Approved).Sum(r => r.Amount),
            PendingRevenue = reportItems.Where(r => r.Status == PaymentStatus.Pending).Sum(r => r.Amount),
            RefundedRevenue = reportItems.Where(r => r.Status == PaymentStatus.Rejected).Sum(r => r.Amount),
            TotalTransactions = reportItems.Count,
            RevenueByTaxType = reportItems.GroupBy(r => r.TaxType).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)),
            RevenueByMethod = reportItems.GroupBy(r => r.Method).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)),
            MonthlyRevenue = reportItems.GroupBy(r => r.Date.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount))
        };

        return new RevenueReportDataDto
        {
            Title = "Revenue Report",
            Subtitle = $"Period: {fromDate:MM/dd/yyyy} - {toDate:MM/dd/yyyy}",
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            RevenueItems = reportItems,
            Summary = summary
        };
    }

    public async Task<CaseManagementReportDataDto> GetCaseManagementReportDataAsync(DateTime fromDate, DateTime toDate)
    {
        var cases = await _context.CaseIssues
            .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
            .Include(c => c.Client)
            .Include(c => c.AssignedToUser)
            .ToListAsync();

        var reportItems = cases.Select(caseIssue => new CaseManagementReportItem
        {
            CaseId = caseIssue.Id,
            CaseNumber = caseIssue.CaseNumber,
            ClientId = caseIssue.ClientId,
            ClientName = caseIssue.Client?.BusinessName ?? "Unknown Client",
            IssueType = caseIssue.IssueType,
            Priority = caseIssue.Priority.ToString(),
            Status = caseIssue.Status.ToString(),
            CreatedDate = caseIssue.CreatedAt,
            ResolvedDate = caseIssue.ResolvedAt,
            AssignedTo = caseIssue.AssignedToUser?.UserName ?? "Unassigned",
            Description = caseIssue.Description,
            IsOverdue = caseIssue.IsOverdue
        }).OrderByDescending(r => r.CreatedDate).ToList();

        var resolvedCases = reportItems.Where(r => r.ResolvedDate.HasValue).ToList();
        var summary = new CaseManagementSummary
        {
            TotalCases = reportItems.Count,
            OpenCases = reportItems.Count(r => !r.ResolvedDate.HasValue),
            ResolvedCases = resolvedCases.Count,
            OverdueCases = reportItems.Count(r => r.IsOverdue),
            AverageResolutionDays = resolvedCases.Any() ? resolvedCases.Average(r => r.DaysOpen) : 0,
            CasesByPriority = reportItems.GroupBy(r => r.Priority).ToDictionary(g => g.Key, g => g.Count()),
            CasesByStatus = reportItems.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count()),
            CasesByType = reportItems.GroupBy(r => r.IssueType).ToDictionary(g => g.Key, g => g.Count())
        };

        return new CaseManagementReportDataDto
        {
            Title = "Case Management Report",
            Subtitle = $"Period: {fromDate:MM/dd/yyyy} - {toDate:MM/dd/yyyy}",
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Cases = reportItems,
            Summary = summary
        };
    }

    public async Task<EnhancedClientActivityReportDataDto> GetEnhancedClientActivityReportDataAsync(DateTime fromDate, DateTime toDate, string? clientFilter, string? activityTypeFilter)
    {
        var query = _context.Clients.AsQueryable();
        
        if (!string.IsNullOrEmpty(clientFilter))
        {
            query = query.Where(c => c.BusinessName.Contains(clientFilter) || c.ContactPerson.Contains(clientFilter) || c.TIN!.Contains(clientFilter));
        }

        var clients = await query.ToListAsync();
        var clientIds = clients.Select(c => c.ClientId).ToList();

        // Get related data separately
        var documents = await _context.Documents
            .Where(d => clientIds.Contains(d.ClientId) && d.UploadedAt >= fromDate && d.UploadedAt <= toDate)
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => clientIds.Contains(p.ClientId) && p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .ToListAsync();

        var taxFilings = await _context.TaxFilings
            .Where(tf => clientIds.Contains(tf.ClientId) && tf.FilingDate >= fromDate && tf.FilingDate <= toDate)
            .ToListAsync();

        var reportItems = new List<EnhancedClientActivityReportItem>();

        foreach (var client in clients)
        {
            var clientDocuments = documents.Where(d => d.ClientId == client.ClientId).ToList();
            var clientPayments = payments.Where(p => p.ClientId == client.ClientId).ToList();
            var clientTaxFilings = taxFilings.Where(tf => tf.ClientId == client.ClientId).ToList();

            var documentsUploaded = clientDocuments.Count;
            var paymentsMade = clientPayments.Count;
            var taxFilingsSubmitted = clientTaxFilings.Count;
            var messagesExchanged = 0; // Messages not directly linked to clients in current model

            // Get login count from activity timeline if available
            var loginCount = await _context.ActivityTimelines
                .Where(at => at.ClientId == client.ClientId &&
                           at.ActivityType == ActivityType.ClientLoggedIn &&
                           at.ActivityDate >= fromDate && at.ActivityDate <= toDate)
                .CountAsync();

            var totalActivities = documentsUploaded + paymentsMade + taxFilingsSubmitted + messagesExchanged + loginCount;
            var lastActivity = await GetLastActivityDateForClient(client.ClientId, fromDate, toDate);
            var engagementScore = CalculateEngagementScore(loginCount, totalActivities - loginCount, lastActivity);
            var engagementLevel = GetEngagementLevel(totalActivities);

            var recentActivities = new List<string>();
            if (documentsUploaded > 0) recentActivities.Add($"{documentsUploaded} documents uploaded");
            if (paymentsMade > 0) recentActivities.Add($"{paymentsMade} payments made");
            if (taxFilingsSubmitted > 0) recentActivities.Add($"{taxFilingsSubmitted} tax filings submitted");
            if (messagesExchanged > 0) recentActivities.Add($"{messagesExchanged} messages exchanged");

            reportItems.Add(new EnhancedClientActivityReportItem
            {
                ClientId = client.ClientId,
                ClientName = client.BusinessName,
                TIN = client.TIN ?? "",
                LastActivity = lastActivity,
                TotalActivities = totalActivities,
                DocumentsUploaded = documentsUploaded,
                PaymentsMade = paymentsMade,
                TaxFilingsSubmitted = taxFilingsSubmitted,
                LoginCount = loginCount,
                MessagesExchanged = messagesExchanged,
                EngagementScore = engagementScore,
                EngagementLevel = engagementLevel,
                RecentActivities = recentActivities
            });
        }

        var summary = new EnhancedActivitySummary
        {
            TotalClients = reportItems.Count,
            ActiveClients = reportItems.Count(r => r.TotalActivities > 0),
            InactiveClients = reportItems.Count(r => r.TotalActivities == 0),
            AverageEngagementScore = reportItems.Any() ? reportItems.Average(r => r.EngagementScore) : 0,
            TotalActivities = reportItems.Sum(r => r.TotalActivities),
            ActivitiesByType = new Dictionary<string, int>
            {
                ["Documents"] = reportItems.Sum(r => r.DocumentsUploaded),
                ["Payments"] = reportItems.Sum(r => r.PaymentsMade),
                ["Filings"] = reportItems.Sum(r => r.TaxFilingsSubmitted),
                ["Messages"] = reportItems.Sum(r => r.MessagesExchanged),
                ["Logins"] = reportItems.Sum(r => r.LoginCount)
            },
            ClientsByEngagementLevel = reportItems.GroupBy(r => r.EngagementLevel).ToDictionary(g => g.Key, g => g.Count())
        };

        return new EnhancedClientActivityReportDataDto
        {
            Title = "Enhanced Client Activity Report",
            Subtitle = $"Period: {fromDate:MM/dd/yyyy} - {toDate:MM/dd/yyyy}",
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            ClientFilter = clientFilter,
            ActivityTypeFilter = activityTypeFilter,
            Activities = reportItems.OrderByDescending(r => r.EngagementScore).ToList(),
            Summary = summary
        };
    }

    private async Task<DateTime> GetLastActivityDateForClient(int clientId, DateTime fromDate, DateTime toDate)
    {
        var dates = new List<DateTime>();

        var lastDocument = await _context.Documents
            .Where(d => d.ClientId == clientId && d.UploadedAt >= fromDate && d.UploadedAt <= toDate)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefaultAsync();
        if (lastDocument != null)
            dates.Add(lastDocument.UploadedAt);

        var lastPayment = await _context.Payments
            .Where(p => p.ClientId == clientId && p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
        if (lastPayment != null)
            dates.Add(lastPayment.CreatedAt);

        var lastActivity = await _context.ActivityTimelines
            .Where(at => at.ClientId == clientId && at.ActivityDate >= fromDate && at.ActivityDate <= toDate)
            .OrderByDescending(at => at.ActivityDate)
            .FirstOrDefaultAsync();
        if (lastActivity != null)
            dates.Add(lastActivity.ActivityDate);

        return dates.Any() ? dates.Max() : DateTime.MinValue;
    }

    private static decimal CalculateComplianceScore(int totalFilings, int onTimeFilings, int totalPayments, int onTimePayments, decimal totalPenalties)
    {
        if (totalFilings == 0 && totalPayments == 0) return 100;

        var filingScore = totalFilings > 0 ? (decimal)onTimeFilings / totalFilings * 50 : 50;
        var paymentScore = totalPayments > 0 ? (decimal)onTimePayments / totalPayments * 50 : 50;
        var penaltyDeduction = Math.Min(totalPenalties / 1000 * 10, 20); // Deduct up to 20 points for penalties

        return Math.Max(0, filingScore + paymentScore - penaltyDeduction);
    }

    private static ComplianceLevel GetComplianceLevel(decimal score)
    {
        return score switch
        {
            >= 80 => ComplianceLevel.Green,
            >= 60 => ComplianceLevel.Yellow,
            _ => ComplianceLevel.Red
        };
    }

    private static string GetRiskLevel(decimal complianceScore, decimal totalPenalties, int lateFilings)
    {
        if (complianceScore < 60 || totalPenalties > 5000 || lateFilings > 5)
            return "High";
        if (complianceScore < 80 || totalPenalties > 1000 || lateFilings > 2)
            return "Medium";
        return "Low";
    }

    private static decimal CalculateEngagementScore(int loginCount, int meaningfulEvents, DateTime lastActivity)
    {
        var score = 0m;
        score += Math.Min(loginCount * 2, 30);
        score += Math.Min(meaningfulEvents * 5, 40);
        
        var daysSinceLastActivity = (DateTime.UtcNow - lastActivity).TotalDays;
        if (daysSinceLastActivity <= 7) score += 30;
        else if (daysSinceLastActivity <= 14) score += 20;
        else if (daysSinceLastActivity <= 30) score += 10;

        return Math.Min(score, 100);
    }
}