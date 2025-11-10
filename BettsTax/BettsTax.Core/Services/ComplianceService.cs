using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ComplianceRiskLevel = BettsTax.Data.Models.ComplianceRiskLevel;
using NotificationType = BettsTax.Data.Models.NotificationType;
using NotificationStatus = BettsTax.Data.Models.NotificationStatus;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BettsTax.Core.Services;

public class ComplianceService : IComplianceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ComplianceService> _logger;
    private readonly IPenaltyCalculationService _penaltyService;
    private readonly IDistributedCache _cache;

    public ComplianceService(
        ApplicationDbContext context,
        ILogger<ComplianceService> logger,
        IPenaltyCalculationService penaltyService,
        IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _penaltyService = penaltyService;
        _cache = cache;
    }

    public async Task<ComplianceStatusSummaryDto> GetClientComplianceSummaryAsync(int clientId)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            throw new ArgumentException($"Client with ID {clientId} not found");

        var filings = await _context.TaxFilings
            .Where(f => f.ClientId == clientId)
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.ClientId == clientId)
            .ToListAsync();

        var documents = await _context.Documents
            .Where(d => d.ClientId == clientId)
            .ToListAsync();

        var penalties = await _context.CompliancePenalties
            .Include(p => p.ComplianceTracker)
            .Where(p => p.ComplianceTracker != null && p.ComplianceTracker.ClientId == clientId && !p.IsWaived)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var pendingFilings = filings.Count(f => f.Status == FilingStatus.Draft || f.Status == FilingStatus.UnderReview);
        var overdueFilings = filings.Count(f => f.DueDate < now && f.Status != FilingStatus.Filed);

        var pendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending);
        var overduePayments = payments.Count(p => p.DueDate.HasValue && p.DueDate.Value < now && p.Status != PaymentStatus.Approved);

        var pendingDocuments = documents.Count(d => d.Category == DocumentCategory.TaxReturn);
        var overdueDocuments = await GetOverdueDocumentsCountAsync(clientId);

        var totalPenalties = penalties.Sum(p => p.Amount);

        // Calculate overall compliance score (0-100)
        var totalItems = filings.Count + payments.Count + documents.Count;
        var compliantItems = filings.Count(f => f.Status == FilingStatus.Filed) +
                           payments.Count(p => p.Status == PaymentStatus.Approved) +
                           documents.Count(d => d.Category == DocumentCategory.TaxReturn);

        var complianceScore = totalItems == 0 ? 100m : (decimal)compliantItems / totalItems * 100m;

        return new ComplianceStatusSummaryDto
        {
            ClientId = clientId,
            ClientName = client.BusinessName,
            OverallComplianceScore = Math.Round(complianceScore, 2),
            TotalFilingsRequired = filings.Count,
            OnTimeFilings = filings.Count(f => f.Status == FilingStatus.Filed),
            LateFilings = overdueFilings,
            PendingFilings = pendingFilings,
            TotalPenalties = totalPenalties,
            LastCalculated = DateTime.UtcNow
        };
    }

    public async Task<List<FilingChecklistItemDto>> GetFilingChecklistAsync(int clientId, TaxType? taxType = null)
    {
        var query = _context.TaxFilings
            .Where(f => f.ClientId == clientId);

        if (taxType.HasValue)
            query = query.Where(f => f.TaxType == taxType.Value);

        var filings = await query.ToListAsync();

        var result = new List<FilingChecklistItemDto>();

        foreach (var filing in filings)
        {
            var requiredDocs = await _context.DocumentRequirements
                .Where(dr => dr.ApplicableTaxType == filing.TaxType)
                .Select(dr => dr.RequirementCode)
                .ToListAsync();

            var submittedDocs = await _context.Documents
                .Where(d => d.ClientId == clientId && d.Category == DocumentCategory.TaxReturn)
                .Select(d => d.Category.ToString())
                .ToListAsync();

            result.Add(new FilingChecklistItemDto
            {
                Id = filing.TaxFilingId,
                Title = $"{filing.TaxType} Filing",
                Description = $"{filing.TaxType} Filing for {filing.TaxYear}",
                ItemType = FilingChecklistItemType.Filing,
                IsRequired = true,
                IsCompleted = filing.Status == FilingStatus.Filed,
                DueDate = filing.DueDate,
                FilingId = filing.TaxFilingId,
                FilingStatus = filing.Status
            });
        }

        return result.OrderBy(f => f.DueDate).ToList();
    }

    public async Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int clientId, int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        var deadlines = new List<UpcomingDeadlineDto>();

        // Filing deadlines
        var filingDeadlines = await _context.TaxFilings
            .Where(f => f.ClientId == clientId &&
                       f.DueDate.HasValue &&
                       f.DueDate <= cutoffDate &&
                       f.Status != FilingStatus.Filed)
            .Select(f => new UpcomingDeadlineDto
            {
                Id = f.TaxFilingId,
                TaxType = f.TaxType,
                TaxTypeName = f.TaxType.ToString(),
                DueDate = f.DueDate!.Value,
                DaysRemaining = (f.DueDate!.Value.Date - DateTime.UtcNow.Date).Days,
                Priority = f.DueDate <= DateTime.UtcNow.AddDays(7) ? BettsTax.Data.ComplianceRiskLevel.High : BettsTax.Data.ComplianceRiskLevel.Medium,
                PriorityName = f.DueDate <= DateTime.UtcNow.AddDays(7) ? "High" : "Medium",
                Status = f.Status,
                StatusName = f.Status.ToString(),
                EstimatedTaxLiability = 0,
                DocumentsReady = false,
                IsOverdue = f.DueDate < DateTime.UtcNow,
                PotentialPenalty = 0,
                Requirements = $"{f.TaxType} Filing"
            })
            .ToListAsync();

        // Payment deadlines
        var paymentDeadlines = await _context.Payments
            .Where(p => p.ClientId == clientId &&
                       p.DueDate.HasValue &&
                       p.DueDate.Value <= cutoffDate &&
                       p.Status != PaymentStatus.Approved)
            .Select(p => new UpcomingDeadlineDto
            {
                Id = p.PaymentId,
                TaxType = TaxType.IncomeTax, // Default, should be determined from context
                TaxTypeName = "Payment",
                DueDate = p.DueDate.Value,
                DaysRemaining = (p.DueDate.Value.Date - DateTime.UtcNow.Date).Days,
                Priority = p.DueDate.Value <= DateTime.UtcNow.AddDays(3) ? BettsTax.Data.ComplianceRiskLevel.High : BettsTax.Data.ComplianceRiskLevel.Medium,
                PriorityName = p.DueDate.Value <= DateTime.UtcNow.AddDays(3) ? "High" : "Medium",
                Status = FilingStatus.Draft, // Default status
                StatusName = p.Status.ToString(),
                EstimatedTaxLiability = p.Amount,
                DocumentsReady = false,
                IsOverdue = p.DueDate.Value < DateTime.UtcNow,
                PotentialPenalty = 0,
                Requirements = $"Payment - {p.Amount:C}"
            })
            .ToListAsync();

        // Document deadlines
        var documentRequirements = await _context.ClientDocumentRequirements
            .Where(cdr => cdr.ClientId == clientId && 
                         cdr.DueDate.HasValue && 
                         cdr.DueDate.Value <= cutoffDate)
            .Include(cdr => cdr.DocumentRequirement)
            .ToListAsync();

        var documentDeadlines = documentRequirements.Select(cdr => new UpcomingDeadlineDto
        {
            Type = "Document",
            Description = $"{cdr.DocumentRequirement?.DocumentType ?? "Document"} Submission",
            DueDate = cdr.DueDate ?? DateTime.UtcNow.AddDays(30),
            Priority = cdr.DueDate <= DateTime.UtcNow.AddDays(5) ? BettsTax.Data.ComplianceRiskLevel.High : BettsTax.Data.ComplianceRiskLevel.Low,
            Status = BettsTax.Data.FilingStatus.Draft
        }).ToList();

        deadlines.AddRange(filingDeadlines);
        deadlines.AddRange(paymentDeadlines);
        deadlines.AddRange(documentDeadlines);

        return deadlines.OrderBy(d => d.DueDate).ToList();
    }

    public async Task<List<PenaltyWarningDto>> GetPenaltyWarningsAsync(int clientId)
    {
        var warnings = new List<PenaltyWarningDto>();

        // Get overdue items that may incur penalties
        var overdueFilings = await _context.TaxFilings
            .Where(f => f.ClientId == clientId && 
                       f.DueDate.HasValue && 
                       f.DueDate.Value < DateTime.UtcNow &&
                       f.Status != FilingStatus.Filed)
            .ToListAsync();

        var overduePayments = await _context.Payments
            .Where(p => p.ClientId == clientId && 
                       p.DueDate.HasValue && 
                       p.DueDate.Value < DateTime.UtcNow &&
                       p.Status != PaymentStatus.Approved)
            .ToListAsync();

        foreach (var filing in overdueFilings)
        {
            var daysOverdue = (DateTime.UtcNow - filing.DueDate.Value).Days;
            var penaltyResult = await _penaltyService.CalculateLatePenaltyAsync(
                filing.TaxType, filing.Amount, filing.DueDate.Value, DateTime.UtcNow);

            warnings.Add(new PenaltyWarningDto
            {
                Type = "Late Filing",
                Description = $"{filing.TaxType} filing is {daysOverdue} days overdue",
                PotentialPenalty = penaltyResult.IsSuccess ? penaltyResult.Value.PenaltyAmount : 0m,
                EffectiveDate = filing.DueDate.Value,
                Severity = daysOverdue > 30 ? ComplianceRiskLevel.Critical : daysOverdue > 14 ? ComplianceRiskLevel.High : ComplianceRiskLevel.Medium,
                RecommendedAction = "Submit filing immediately to minimize penalties"
            });
        }

        foreach (var payment in overduePayments)
        {
            var daysOverdue = (DateTime.UtcNow - payment.DueDate.Value).Days;
            var penaltyResult = await _penaltyService.CalculateLatePenaltyAsync(
                payment.TaxType ?? TaxType.IncomeTax, payment.Amount, payment.DueDate.Value, DateTime.UtcNow);

            warnings.Add(new PenaltyWarningDto
            {
                Type = "Late Payment",
                Description = $"{payment.TaxType} payment of {payment.Amount:C} is {daysOverdue} days overdue",
                PotentialPenalty = penaltyResult.IsSuccess ? penaltyResult.Value.PenaltyAmount : 0m,
                EffectiveDate = payment.DueDate.Value,
                Severity = daysOverdue > 60 ? ComplianceRiskLevel.Critical : daysOverdue > 30 ? ComplianceRiskLevel.High : ComplianceRiskLevel.Medium,
                RecommendedAction = "Make payment immediately to avoid additional penalties"
            });
        }

        return warnings.OrderByDescending(w => w.PotentialPenalty).ToList();
    }

    public async Task<DocumentTrackerDto> GetDocumentTrackerAsync(int clientId)
    {
        var clientDocReqs = await _context.ClientDocumentRequirements
            .Where(cdr => cdr.ClientId == clientId)
            .Include(cdr => cdr.DocumentRequirement)
            .ToListAsync();

        var documents = await _context.Documents
            .Where(d => d.ClientId == clientId)
            .ToListAsync();

        var totalRequired = clientDocReqs.Count;
        var submitted = 0;
        var approved = 0;
        var rejected = 0;
        var pending = 0;
        var overdue = 0;

        var documentStatuses = new List<DocumentStatusDto>();

        foreach (var req in clientDocReqs)
        {
            // Skip if DocumentRequirement is null (shouldn't happen in normal operation)
            if (req.DocumentRequirement == null)
            {
                _logger.LogWarning("ClientDocumentRequirement {Id} has null DocumentRequirement", req.ClientDocumentRequirementId);
                continue;
            }
            
            var doc = documents.FirstOrDefault(d => d.DocumentType == req.DocumentRequirement.DocumentType);
            
            var status = doc?.Status.ToString() ?? "Not Submitted";
            var isOverdue = req.DueDate.HasValue && DateTime.UtcNow > req.DueDate.Value && doc?.Status != DocumentStatus.Approved;

            if (doc != null)
            {
                submitted++;
                switch (doc.Status)
                {
                    case DocumentStatus.Approved:
                        approved++;
                        break;
                    case DocumentStatus.Rejected:
                        rejected++;
                        break;
                    case DocumentStatus.Pending:
                        pending++;
                        break;
                }
            }

            if (isOverdue) overdue++;

            documentStatuses.Add(new DocumentStatusDto
            {
                DocumentType = req.DocumentRequirement.DocumentType,
                Status = Enum.TryParse<DocumentStatus>(status, out var docStatus) ? docStatus : DocumentStatus.Pending,
                SubmittedDate = doc?.UploadedAt,
                DueDate = req.DueDate
            });
        }

        return new DocumentTrackerDto
        {
            TotalRequired = totalRequired,
            Submitted = submitted,
            Approved = approved,
            Rejected = rejected,
            Pending = pending,
            Overdue = overdue,
            DocumentStatuses = documentStatuses.OrderBy(ds => ds.DueDate).ToList()
        };
    }

    public async Task<DeadlineAdherenceHistoryDto> GetDeadlineAdherenceHistoryAsync(int clientId, int months = 12)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var monthlyData = new List<DeadlineAdherenceMonthDto>();

        for (int i = 0; i < months; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var filings = await _context.TaxFilings
                .Where(f => f.ClientId == clientId && 
                           f.DueDate.HasValue && 
                           f.DueDate.Value >= monthStart && 
                           f.DueDate.Value <= monthEnd)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.ClientId == clientId && 
                           p.DueDate.HasValue && 
                           p.DueDate.Value >= monthStart && 
                           p.DueDate.Value <= monthEnd)
                .ToListAsync();

            var totalDeadlines = filings.Count + payments.Count;
            var metDeadlines = filings.Count(f => f.SubmittedAt.HasValue && f.SubmittedAt.Value <= f.DueDate.Value) +
                             payments.Count(p => p.ApprovedAt.HasValue && p.ApprovedAt.Value <= p.DueDate.Value);

            var adherenceRate = totalDeadlines == 0 ? 100m : (decimal)metDeadlines / totalDeadlines * 100m;

            monthlyData.Add(new DeadlineAdherenceMonthDto
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                MonthName = monthStart.ToString("MMMM"),
                TotalDeadlines = totalDeadlines,
                OnTimeFilings = metDeadlines,
                LateFilings = totalDeadlines - metDeadlines,
                MissedDeadlines = 0, // We don't track missed vs late separately here
                OnTimePercentage = Math.Round(adherenceRate, 2),
                AverageDaysEarly = 0m, // Would need additional calculation
                AverageDaysLate = 0m, // Would need additional calculation
                TotalPenalties = 0m // Would need additional calculation
            });
        }

        var overallTotal = monthlyData.Sum(m => m.TotalDeadlines);
        var overallMet = monthlyData.Sum(m => m.OnTimeFilings);
        var overallRate = overallTotal == 0 ? 100m : (decimal)overallMet / overallTotal * 100m;

        return new DeadlineAdherenceHistoryDto
        {
            MonthlyData = monthlyData,
            OverallAdherenceRate = Math.Round(overallRate, 2),
            TotalDeadlines = overallTotal,
            MetDeadlines = overallMet
        };
    }

    public async Task<List<ComplianceAlertDto>> GetComplianceAlertsAsync(int? clientId = null)
    {
        var query = _context.ComplianceAlerts.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(a => a.ClientId == clientId.Value);

        var alerts = await query
            .Include(a => a.Client)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return alerts.Select(a => new ComplianceAlertDto
        {
            AlertId = a.ComplianceAlertId,
            ClientId = a.ClientId,
            ClientName = $"{a.Client.FirstName} {a.Client.LastName}",
            AlertType = a.AlertType,
            Message = a.Message,
            Severity = a.Severity,
            CreatedAt = a.CreatedAt,
            IsResolved = a.IsResolved,
            ResolvedAt = a.ResolvedAt
        }).ToList();
    }

    public async Task<ComplianceMetricsDto> GetComplianceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        fromDate ??= DateTime.UtcNow.AddMonths(-12);
        toDate ??= DateTime.UtcNow;

        var clients = await _context.Clients.CountAsync();
        
        var filings = await _context.TaxFilings
            .Where(f => f.CreatedAt >= fromDate && f.CreatedAt <= toDate)
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .ToListAsync();

        var documents = await _context.Documents
            .Where(d => d.UploadedAt >= fromDate && d.UploadedAt <= toDate)
            .ToListAsync();

        var penalties = await _context.CompliancePenalties
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate && !p.IsWaived)
            .ToListAsync();

        var filingCompliance = filings.Count == 0 ? 100m : 
            (decimal)filings.Count(f => f.Status == FilingStatus.Filed) / filings.Count * 100m;

        var paymentCompliance = payments.Count == 0 ? 100m :
            (decimal)payments.Count(p => p.Status == PaymentStatus.Approved) / payments.Count * 100m;

        var documentCompliance = documents.Count == 0 ? 100m :
            (decimal)documents.Count(d => d.Status == DocumentStatus.Approved) / documents.Count * 100m;

        var overallCompliance = (filingCompliance + paymentCompliance + documentCompliance) / 3m;

        var clientsWithPenalties = penalties.Select(p => p.ClientId).Distinct().Count();
        var totalPenalties = penalties.Sum(p => p.Amount);

        return new ComplianceMetricsDto
        {
            OverallComplianceRate = Math.Round(overallCompliance, 2),
            FilingComplianceRate = Math.Round(filingCompliance, 2),
            PaymentComplianceRate = Math.Round(paymentCompliance, 2),
            DocumentComplianceRate = Math.Round(documentCompliance, 2),
            TotalClients = clients,
            CompliantClients = (int)(clients * overallCompliance / 100m),
            ClientsWithPenalties = clientsWithPenalties,
            TotalPenalties = totalPenalties,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<int> GetOverdueDocumentsCountAsync(int clientId)
    {
        var now = DateTime.UtcNow;
        return await _context.ClientDocumentRequirements
            .Where(cdr => cdr.ClientId == clientId && 
                         cdr.DueDate.HasValue && 
                         cdr.DueDate.Value < now &&
                         !_context.Documents.Any(d => d.ClientId == clientId && 
                                                     d.DocumentType == cdr.DocumentRequirement.DocumentType &&
                                                     d.Status == DocumentStatus.Approved))
            .CountAsync();
    }

    // New enhanced endpoint implementations with caching
    public async Task<DTOs.Compliance.ComplianceStatusSummaryDto> GetStatusSummaryAsync(int clientId)
    {
        var cacheKey = $"compliance_summary_{clientId}";
        var cachedSummary = await GetFromCacheAsync<DTOs.Compliance.ComplianceStatusSummaryDto>(cacheKey);
        
        if (cachedSummary != null && IsDataFresh(cachedSummary.LastCalculated))
        {
            return cachedSummary;
        }

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (client == null) throw new ArgumentException($"Client with ID {clientId} not found");

        var now = DateTime.UtcNow;
        var filings = await _context.TaxFilings.Where(f => f.ClientId == clientId).Include(f => f.TaxDeadline).ToListAsync();
        var documents = await _context.Documents.Where(d => d.ClientId == clientId).ToListAsync();
        var payments = await _context.Payments.Where(p => p.ClientId == clientId).ToListAsync();
        var penalties = await _context.CompliancePenalties.Where(p => p.ClientId == clientId && !p.IsWaived).ToListAsync();

        var complianceScore = CalculateComplianceScore(filings.Count(f => f.Status == FilingStatus.Filed), filings.Count, documents.Count, documents.Count);
        
        var summary = new DTOs.Compliance.ComplianceStatusSummaryDto
        {
            ClientId = clientId,
            ClientName = client.CompanyName,
            TIN = client.TIN,
            OverallComplianceScore = complianceScore,
            ComplianceLevel = GetComplianceLevel(complianceScore),
            ComplianceGrade = GetComplianceGrade(complianceScore),
            LastCalculated = now,
            TotalFilingsRequired = filings.Count,
            OnTimeFilings = filings.Count(f => f.Status == FilingStatus.Filed && f.SubmittedAt <= f.TaxDeadline.DueDate),
            LateFilings = filings.Count(f => f.Status == FilingStatus.Filed && f.SubmittedAt > f.TaxDeadline.DueDate),
            PendingFilings = filings.Count(f => f.Status != FilingStatus.Filed),
            TotalPenalties = penalties.Sum(p => p.Amount),
            PotentialPenalties = await CalculatePotentialPenaltiesAsync(clientId),
            TotalDocumentsRequired = documents.Count,
            DocumentsSubmitted = documents.Count(d => d.Status != DocumentStatus.Pending),
            DocumentsPending = documents.Count(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.UnderReview),
            DocumentsRejected = documents.Count(d => d.Status == DocumentStatus.Rejected),
            TotalPaymentsDue = payments.Where(p => p.DueDate <= now).Sum(p => p.Amount),
            PaymentsMade = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
            PaymentsOverdue = payments.Where(p => p.DueDate < now && p.Status != PaymentStatus.Completed).Sum(p => p.Amount),
            RiskLevel = CalculateRiskLevel(complianceScore, penalties.Sum(p => p.Amount), 0),
            RecentAlerts = await GetRecentAlertsAsync(clientId, 5),
            NearestDeadlines = await GetNearestDeadlinesAsync(clientId, 3)
        };

        // Cache the result for 30 minutes
        await SetCacheAsync(cacheKey, summary, TimeSpan.FromMinutes(30));
        return summary;
    }

    public async Task<FilingChecklistDto> GetFilingChecklistDetailedAsync(int clientId, TaxType taxType)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        if (client == null) throw new ArgumentException($"Client with ID {clientId} not found");

        var checklistItems = new List<FilingChecklistItemDto>();
        var filing = await _context.TaxFilings.Include(f => f.TaxDeadline)
            .FirstOrDefaultAsync(f => f.ClientId == clientId && f.TaxType == taxType);

        if (filing != null)
        {
            checklistItems.Add(new FilingChecklistItemDto
            {
                Id = filing.TaxFilingId,
                Title = $"{taxType} Tax Filing",
                ItemType = FilingChecklistItemType.Filing,
                IsRequired = true,
                IsCompleted = filing.Status == FilingStatus.Filed,
                DueDate = filing.TaxDeadline?.DueDate,
                FilingId = filing.TaxFilingId,
                FilingStatus = filing.Status
            });
        }

        var summary = new FilingChecklistSummaryDto
        {
            TotalItems = checklistItems.Count,
            CompletedItems = checklistItems.Count(i => i.IsCompleted),
            CompletionPercentage = checklistItems.Count > 0 ? (decimal)checklistItems.Count(i => i.IsCompleted) / checklistItems.Count * 100 : 0
        };

        return new FilingChecklistDto
        {
            ClientId = clientId,
            ClientName = client.CompanyName,
            TaxType = taxType,
            ChecklistItems = checklistItems,
            Summary = summary
        };
    }

    public async Task<PenaltySimulationResultDto> SimulatePenaltyAsync(PenaltySimulationRequestDto request)
    {
        var daysLate = request.ProposedFilingDate.HasValue ? Math.Max(0, (int)(request.ProposedFilingDate.Value - request.DueDate).TotalDays) : 0;
        var penaltyResult = await _penaltyService.CalculatePenaltyAsync(request.TaxType, request.TaxLiability, request.DueDate, request.ProposedFilingDate ?? DateTime.UtcNow);

        return new PenaltySimulationResultDto
        {
            Request = request,
            DaysLate = daysLate,
            TotalPenalty = penaltyResult.IsSuccess ? penaltyResult.Value.PenaltyAmount : 0m,
            CalculatedAt = DateTime.UtcNow
        };
    }

    public async Task CreateComplianceSnapshotAsync(int? clientId = null)
    {
        // Implementation for compliance snapshot creation
        _logger.LogInformation("Creating compliance snapshot for client {ClientId}", clientId);
    }

    // Helper methods
    private decimal CalculateComplianceScore(int onTimeFilings, int totalFilings, int submittedDocs, int totalDocs)
    {
        if (totalFilings == 0 && totalDocs == 0) return 100m;
        var filingScore = totalFilings > 0 ? (decimal)onTimeFilings / totalFilings * 50 : 50;
        var docScore = totalDocs > 0 ? (decimal)submittedDocs / totalDocs * 50 : 50;
        return filingScore + docScore;
    }

    private ComplianceLevel GetComplianceLevel(decimal score) => score >= 90 ? ComplianceLevel.Green : score >= 75 ? ComplianceLevel.Yellow : ComplianceLevel.Red;
    private string GetComplianceGrade(decimal score) => score >= 90 ? "A" : score >= 75 ? "B" : "C";
    private ComplianceRiskLevel CalculateRiskLevel(decimal score, decimal penalties, int alerts) => score < 50 || penalties > 1000 ? ComplianceRiskLevel.High : ComplianceRiskLevel.Medium;
    private async Task<decimal> CalculatePotentialPenaltiesAsync(int clientId) => 0m;
    private async Task<List<DTOs.Compliance.ComplianceAlertDto>> GetRecentAlertsAsync(int clientId, int count) => new();
    private async Task<List<DTOs.Compliance.UpcomingDeadlineDto>> GetNearestDeadlinesAsync(int clientId, int count) => new();
    private ComplianceRiskLevel GetDeadlinePriority(DateTime dueDate, FilingStatus status) => ComplianceRiskLevel.Medium;
    private ComplianceRiskLevel GetPenaltySeverity(decimal penalty) => penalty > 1000 ? ComplianceRiskLevel.High : ComplianceRiskLevel.Medium;
    private List<string> GetRecommendedActions(TaxType taxType, int daysLate) => new() { "File immediately", "Contact tax advisor" };
    private decimal CalculateAverageDaysEarly(List<TaxFiling> filings) => 0m;
    private decimal CalculateAverageDaysLate(List<TaxFiling> filings) => 0m;
    private string CalculateTrend(List<DeadlineAdherenceMonthDto> data) => "Stable";
    private List<string> GenerateRecommendations(List<DeadlineAdherenceMonthDto> data) => new() { "Maintain current compliance levels" };
    private List<string> GeneratePenaltyRecommendations(PenaltyCalculationDto penalty, int daysLate) => new() { "File as soon as possible" };

    // Stub implementations for remaining methods
    public async Task<UpcomingDeadlinesDto> GetUpcomingDeadlinesDetailedAsync(int clientId, int daysAhead = 30) => new() { ClientId = clientId };
    public async Task<List<DTOs.Compliance.PenaltyWarningDto>> GetPenaltyWarningsDetailedAsync(int clientId) => new();
    public async Task<DTOs.Compliance.DocumentTrackerDto> GetDocumentTrackerDetailedAsync(int clientId) => new() { ClientId = clientId };
    public async Task<DTOs.Compliance.DeadlineAdherenceHistoryDto> GetDeadlineAdherenceHistoryDetailedAsync(int clientId, int months = 12) => new() { ClientId = clientId };

    // Caching helper methods
    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedData))
                return null;

            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving data from cache for key {Key}", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T data, TimeSpan expiration)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(data);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetStringAsync(key, serializedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache data for key {Key}", key);
        }
    }

    private static bool IsDataFresh(DateTime lastCalculated, int maxAgeMinutes = 30)
    {
        return DateTime.UtcNow - lastCalculated < TimeSpan.FromMinutes(maxAgeMinutes);
    }
}
