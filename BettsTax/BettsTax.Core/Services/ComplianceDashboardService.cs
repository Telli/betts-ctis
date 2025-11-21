using System;
using System.Collections.Generic;
using System.Linq;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services;

public class ComplianceDashboardService : IComplianceDashboardService
{
    private readonly ApplicationDbContext _context;

    public ComplianceDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ComplianceOverviewSummaryDto> GetOverviewAsync()
    {
        var trackers = await _context.ComplianceTrackers
            .AsNoTracking()
            .Include(t => t.Alerts)
            .ToListAsync();

        var totalClients = await _context.Clients.CountAsync();

        var overview = new ComplianceOverviewSummaryDto
        {
            TotalClients = totalClients,
            Compliant = trackers.Count(t => t.Status == ComplianceStatus.Compliant),
            AtRisk = trackers.Count(t => t.Status == ComplianceStatus.AtRisk || t.Status == ComplianceStatus.PartiallyCompliant || t.Status == ComplianceStatus.UnderReview),
            Overdue = trackers.Count(t => IsTrackerOverdue(t)),
            AverageScore = trackers.Count == 0 ? 100 : Math.Round(trackers.Average(t => t.ComplianceScore), 2),
            TotalOutstanding = trackers.Sum(t => t.OutstandingBalance + t.OutstandingPenalties),
            TotalAlerts = trackers.Sum(t => t.Alerts.Count(a => a.IsActive))
        };

        return overview;
    }

    public async Task<IReadOnlyList<ComplianceDashboardItemDto>> GetItemsAsync(ComplianceDashboardFilterDto filters)
    {
        var query = _context.ComplianceTrackers
            .AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.TaxYear)
            .Include(t => t.Alerts.Where(a => a.IsActive))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Status) && Enum.TryParse<ComplianceStatus>(filters.Status, true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filters.Priority) && Enum.TryParse<ComplianceRiskLevel>(filters.Priority, true, out var priority))
        {
            query = query.Where(t => t.RiskLevel == priority);
        }

        if (!string.IsNullOrWhiteSpace(filters.Type) && Enum.TryParse<TaxType>(filters.Type, true, out var taxType))
        {
            query = query.Where(t => t.TaxType == taxType);
        }

        if (filters.ClientId.HasValue)
        {
            query = query.Where(t => t.ClientId == filters.ClientId.Value);
        }

        if (filters.FromDate.HasValue)
        {
            query = query.Where(t => t.LastUpdated >= filters.FromDate.Value);
        }

        if (filters.ToDate.HasValue)
        {
            query = query.Where(t => t.LastUpdated <= filters.ToDate.Value);
        }

        var trackers = await query
            .OrderByDescending(t => t.RiskLevel)
            .ThenByDescending(t => t.LastUpdated)
            .Take(200)
            .ToListAsync();

        var items = trackers.Select(t => new ComplianceDashboardItemDto
        {
            Id = t.ComplianceTrackerId,
            Type = t.TaxType.ToString(),
            Description = $"{t.TaxType} filing for {t.TaxYear?.Year ?? DateTime.UtcNow.Year}",
            Status = t.Status.ToString(),
            DueDate = t.FilingDueDate,
            LastUpdated = t.LastUpdated,
            Priority = t.RiskLevel.ToString(),
            Penalty = t.OutstandingPenalties,
            ClientId = t.ClientId,
            ClientName = t.Client?.BusinessName,
            TaxYear = t.TaxYear?.Year ?? DateTime.UtcNow.Year,
            Category = DetermineCategory(t),
            ComplianceScore = Math.Round(t.ComplianceScore, 2),
            DaysOverdue = Math.Max(t.DaysOverdueForFiling, t.DaysOverdueForPayment),
            Alerts = t.Alerts.Count,
            TaxType = t.TaxType.ToString()
        }).ToList();

        return items;
    }

    public async Task<IReadOnlyList<ComplianceTaxTypeBreakdownDto>> GetTaxTypeBreakdownAsync()
    {
        var trackers = await _context.ComplianceTrackers
            .AsNoTracking()
            .ToListAsync();

        var breakdown = trackers
            .GroupBy(t => t.TaxType)
            .Select(g => new ComplianceTaxTypeBreakdownDto
            {
                TaxType = g.Key.ToString(),
                ClientCount = g.Select(t => t.ClientId).Distinct().Count(),
                ComplianceRate = g.Count() == 0 ? 0 : Math.Round(g.Count(t => t.Status == ComplianceStatus.Compliant) / (decimal)g.Count() * 100, 2),
                AverageScore = g.Count() == 0 ? 0 : Math.Round(g.Average(t => t.ComplianceScore), 2),
                OutstandingAmount = g.Sum(t => t.OutstandingBalance + t.OutstandingPenalties)
            })
            .OrderByDescending(b => b.ClientCount)
            .ToList();

        return breakdown;
    }

    public async Task<IReadOnlyList<FilingChecklistMatrixRowDto>> GetFilingChecklistMatrixAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var filings = await _context.TaxFilings
            .AsNoTracking()
            .Where(f => f.TaxYear == targetYear)
            .ToListAsync();

        var rows = filings
            .GroupBy(f => f.TaxType)
            .Select(g => new FilingChecklistMatrixRowDto
            {
                TaxType = g.Key.ToString(),
                Status = new QuarterStatusDto
                {
                    Q1 = ResolveQuarterStatus(g, 1),
                    Q2 = ResolveQuarterStatus(g, 2),
                    Q3 = ResolveQuarterStatus(g, 3),
                    Q4 = ResolveQuarterStatus(g, 4)
                }
            })
            .OrderBy(r => r.TaxType)
            .ToList();

        return rows;
    }

    public async Task<IReadOnlyList<PenaltyWarningSummaryDto>> GetPenaltyWarningsAsync(int top = 5)
    {
        var now = DateTime.UtcNow;
        var filings = await _context.TaxFilings
            .Include(f => f.Client)
            .Where(f => f.DueDate.HasValue && f.DueDate < now && f.Status != FilingStatus.Filed)
            .ToListAsync();

        var payments = await _context.Payments
            .Include(p => p.Client)
            .Where(p => p.DueDate.HasValue && p.DueDate < now && p.Status != PaymentStatus.Approved)
            .ToListAsync();

        var warnings = new List<PenaltyWarningSummaryDto>();

        warnings.AddRange(filings.Select(f => new PenaltyWarningSummaryDto
        {
            Type = "Late Filing",
            ClientName = f.Client?.BusinessName ?? "Unknown",
            Reason = $"{f.TaxType} filing overdue since {f.DueDate:MMM dd}",
            EstimatedAmount = f.Amount,
            DaysOverdue = (int)(now - f.DueDate!.Value).TotalDays,
            FilingId = f.TaxFilingId
        }));

        warnings.AddRange(payments.Select(p => new PenaltyWarningSummaryDto
        {
            Type = "Late Payment",
            ClientName = p.Client?.BusinessName ?? "Unknown",
            Reason = $"Payment of {p.Amount:C0} overdue",
            EstimatedAmount = p.Amount,
            DaysOverdue = (int)(now - p.DueDate!.Value).TotalDays,
            PaymentId = p.PaymentId
        }));

        return warnings
            .OrderByDescending(w => w.EstimatedAmount)
            .ThenByDescending(w => w.DaysOverdue)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<DocumentRequirementProgressDto>> GetDocumentRequirementsAsync()
    {
        var requirements = await _context.DocumentRequirements
            .AsNoTracking()
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        var clientRequirements = await _context.ClientDocumentRequirements
            .AsNoTracking()
            .ToListAsync();

        var documents = await _context.Documents
            .AsNoTracking()
            .ToListAsync();

        var progress = requirements.Select(req =>
        {
            var assignments = clientRequirements.Where(cr => cr.DocumentRequirementId == req.DocumentRequirementId).ToList();
            var submittedDocs = documents.Where(d => d.DocumentType == req.DocumentType).ToList();
            var approved = submittedDocs.Count(d => d.Status == DocumentStatus.Approved);

            var requiredCount = assignments.Count;
            var submitted = Math.Min(submittedDocs.Count, requiredCount);
            var percentage = requiredCount == 0 ? 100 : (int)Math.Round(submitted / (decimal)requiredCount * 100, MidpointRounding.AwayFromZero);

            return new DocumentRequirementProgressDto
            {
                Name = req.Name,
                Required = requiredCount,
                Submitted = submitted,
                Approved = Math.Min(approved, requiredCount),
                Progress = percentage
            };
        })
        .Where(p => p.Required > 0)
        .ToList();

        return progress;
    }

    public async Task<IReadOnlyList<ComplianceTimelineEventDto>> GetTimelineAsync(int top = 5)
    {
        var activities = await _context.ActivityTimelines
            .AsNoTracking()
            .OrderByDescending(a => a.ActivityDate)
            .Take(top)
            .ToListAsync();

        return activities.Select(a => new ComplianceTimelineEventDto
        {
            Date = a.ActivityDate,
            Event = a.Title,
            Status = MapPriorityToStatus(a.Priority),
            Details = string.IsNullOrWhiteSpace(a.Description) ? null : a.Description
        }).ToList();
    }

    private static bool IsTrackerOverdue(ComplianceTracker tracker)
    {
        return (tracker.IsFilingRequired && !tracker.IsFilingComplete && tracker.FilingDueDate < DateTime.UtcNow) ||
               (tracker.IsPaymentRequired && !tracker.IsPaymentComplete && tracker.PaymentDueDate < DateTime.UtcNow);
    }

    private static string DetermineCategory(ComplianceTracker tracker)
    {
        if (!tracker.IsFilingComplete && tracker.IsFilingRequired)
        {
            return "filing";
        }

        if (!tracker.IsPaymentComplete && tracker.IsPaymentRequired)
        {
            return "payment";
        }

        return tracker.IsDocumentationComplete ? "complete" : "documentation";
    }

    private static string ResolveQuarterStatus(IEnumerable<TaxFiling> filings, int quarter)
    {
        var quarterFilings = filings.Where(f => f.DueDate.HasValue && GetQuarter(f.DueDate.Value) == quarter).ToList();
        if (!quarterFilings.Any())
        {
            return "n/a";
        }

        if (quarterFilings.All(f => f.Status == FilingStatus.Filed))
        {
            return "complete";
        }

        if (quarterFilings.Any(f => f.DueDate < DateTime.UtcNow && f.Status != FilingStatus.Filed))
        {
            return "overdue";
        }

        return "pending";
    }

    private static int GetQuarter(DateTime date) => (date.Month - 1) / 3 + 1;

    private static string MapPriorityToStatus(ActivityPriority priority) => priority switch
    {
        ActivityPriority.Critical => "error",
        ActivityPriority.High => "warning",
        ActivityPriority.Low => "success",
        _ => "success"
    };
}
