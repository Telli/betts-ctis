using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
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
                DueDate = filing.DueDate,
                Status = filing.Status,
                StatusName = filing.Status.ToString(),
                TaxLiability = filing.TaxLiability,
                FilingReference = filing.FilingReference ?? string.Empty,
                DaysFromDue = (int)filing.FilingDate.Subtract(filing.DueDate).TotalDays
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
                    MissedDeadlines = g.Count(f => f.FilingDate == null && f.DueDate < DateTime.UtcNow),
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
}