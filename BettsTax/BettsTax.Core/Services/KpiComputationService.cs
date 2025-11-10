using BettsTax.Core.DTOs;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services;

/// <summary>
/// Computes and caches KPI metrics. Currently in-memory/IDistributedCache; later can persist snapshots.
/// </summary>
public class KpiComputationService : IKpiComputationService
{
    private const string CacheKey = "kpi:latest";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<KpiComputationService> _logger;

    // Simple hard-coded thresholds for now (can move to DB / settings)
    private const decimal ComplianceThreshold = 0.70m;
    private const decimal FilingTimelinessThreshold = 0.80m;
    private const decimal PaymentCompletionThreshold = 0.85m;
    private const decimal DocumentSubmissionThreshold = 0.75m;
    private const decimal EngagementThreshold = 0.60m;

    public KpiComputationService(ApplicationDbContext db, IDistributedCache cache, ILogger<KpiComputationService> logger)
    {
        _db = db;
        _context = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<KpiMetricsDto> ComputeAsync(CancellationToken ct = default)
    {
        // Fetch data in parallel where possible
        var clientsTask = _db.Clients.AsNoTracking().ToListAsync(ct);
        var taxYearsTask = _db.TaxYears.AsNoTracking().ToListAsync(ct);
        var paymentsTask = _db.Payments.AsNoTracking().ToListAsync(ct);
        // Document requirements & documents for submission compliance
        var docReqTask = _db.DocumentRequirements.AsNoTracking().ToListAsync(ct);
        var docsTask = _db.Documents.AsNoTracking().ToListAsync(ct);

        await Task.WhenAll(clientsTask, taxYearsTask, paymentsTask, docReqTask, docsTask);

        var clients = clientsTask.Result;
        var taxYears = taxYearsTask.Result;
        var payments = paymentsTask.Result;
        var requirements = docReqTask.Result;
        var documents = docsTask.Result;

        var totalClients = clients.Count;
        var compliantClients = clients.Count(c => c.Status == ClientStatus.Active); // Placeholder: refine with real compliance logic
        decimal clientComplianceRate = totalClients == 0 ? 1m : (decimal)compliantClients / totalClients;

        // Real filing timeliness calculation using actual date differences
        var filings = await _db.TaxFilings.AsNoTracking().ToListAsync(ct);
        var totalFilings = filings.Count;
        var onTimeFilings = filings.Count(f => 
            f.SubmittedAt.HasValue && f.DueDate.HasValue && 
            f.SubmittedAt.Value <= f.DueDate.Value);
        decimal filingTimeliness = totalFilings == 0 ? 1m : (decimal)onTimeFilings / totalFilings;

        // Enhanced payment completion rate: payments approved on or before due date
        var totalPaymentsDue = payments.Where(p => p.DueDate.HasValue).Count();
        var onTimePayments = payments.Count(p => 
            p.Status == PaymentStatus.Approved && 
            p.DueDate.HasValue && 
            p.ApprovedAt.HasValue && 
            p.ApprovedAt.Value <= p.DueDate.Value);
        decimal paymentCompletionRate = totalPaymentsDue == 0 ? 1m : (decimal)onTimePayments / totalPaymentsDue;

        // Enhanced document submission compliance: percentage of required documents submitted before deadline
        var clientDocReqs = await _db.ClientDocumentRequirements.AsNoTracking().ToListAsync(ct);
        var totalRequiredDocs = clientDocReqs.Count;
        var submittedOnTime = clientDocReqs.Count(cdr => 
        {
            var submittedDoc = documents.FirstOrDefault(d => 
                d.ClientId == cdr.ClientId && 
                d.DocumentType == cdr.DocumentRequirement.DocumentType);
            return submittedDoc != null && 
                   cdr.DueDate.HasValue && 
                   submittedDoc.UploadedAt <= cdr.DueDate.Value;
        });
        decimal documentSubmissionCompliance = totalRequiredDocs == 0 ? 1m : (decimal)submittedOnTime / totalRequiredDocs;

        // Engagement: clients with any activity (payment or document) in last 30 days
        var since = DateTime.UtcNow.AddDays(-30);
        var activeClientIds = new HashSet<int>(payments.Where(p => p.CreatedAt >= since).Select(p => p.ClientId)
            .Concat(documents.Where(d => d.UploadedAt >= since).Select(d => d.ClientId)));
        decimal engagementRate = totalClients == 0 ? 1m : (decimal)activeClientIds.Count / totalClients;

        var dto = new KpiMetricsDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalClients = totalClients,
            ClientComplianceRate = Round(clientComplianceRate),
            TaxFilingTimeliness = Round(filingTimeliness),
            PaymentCompletionRate = Round(paymentCompletionRate),
            DocumentSubmissionCompliance = Round(documentSubmissionCompliance),
            ClientEngagementRate = Round(engagementRate),
            ComplianceRateBelowThreshold = clientComplianceRate < ComplianceThreshold,
            FilingTimelinessBelowThreshold = filingTimeliness < FilingTimelinessThreshold,
            PaymentCompletionBelowThreshold = paymentCompletionRate < PaymentCompletionThreshold,
            DocumentSubmissionBelowThreshold = documentSubmissionCompliance < DocumentSubmissionThreshold,
            EngagementBelowThreshold = engagementRate < EngagementThreshold
        };

        // Cache serialized
        var json = JsonSerializer.Serialize(dto);
        await _cache.SetStringAsync(CacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        }, ct);

        _logger.LogInformation("KPI metrics computed and cached at {Time}", dto.GeneratedAtUtc);
        return dto;
    }

    public async Task<KpiMetricsDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (cached != null)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<KpiMetricsDto>(cached);
                if (dto != null && dto.GeneratedAtUtc > DateTime.UtcNow.AddMinutes(-5))
                    return dto;
            }
            catch { /* ignore parse failures */ }
        }
        return await ComputeAsync(ct);
    }

    public async Task<KpiSnapshot> CreateDailySnapshotAsync(string? createdBy = null, CancellationToken ct = default)
    {
        var metrics = await ComputeAsync(ct);
        
        var snapshot = new KpiSnapshot
        {
            SnapshotDate = DateTime.UtcNow.Date,
            TotalClients = metrics.TotalClients,
            ClientComplianceRate = metrics.ClientComplianceRate,
            TaxFilingTimeliness = metrics.TaxFilingTimeliness,
            PaymentCompletionRate = metrics.PaymentCompletionRate,
            DocumentSubmissionCompliance = metrics.DocumentSubmissionCompliance,
            ClientEngagementRate = metrics.ClientEngagementRate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.KpiSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        
        return snapshot;
    }

    public async Task<List<KpiTrendDto>> GetKpiTrendsAsync(int days = 30, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var snapshots = await _db.KpiSnapshots
            .Where(s => s.SnapshotDate >= startDate)
            .OrderBy(s => s.SnapshotDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return snapshots.Select(s => new KpiTrendDto
        {
            Date = s.SnapshotDate,
            ClientComplianceRate = s.ClientComplianceRate,
            TaxFilingTimeliness = s.TaxFilingTimeliness,
            PaymentCompletionRate = s.PaymentCompletionRate,
            DocumentSubmissionCompliance = s.DocumentSubmissionCompliance,
            ClientEngagementRate = s.ClientEngagementRate
        }).ToList();
    }

    public async Task<ClientKpiDto> ComputeClientKpiAsync(int clientId, CancellationToken ct = default)
    {
        var client = await _db.Clients.FindAsync(clientId);
        if (client == null)
            throw new ArgumentException($"Client {clientId} not found");

        var payments = await _db.Payments.Where(p => p.ClientId == clientId).AsNoTracking().ToListAsync(ct);
        var documents = await _db.Documents.Where(d => d.ClientId == clientId).AsNoTracking().ToListAsync(ct);
        var filings = await _db.TaxFilings.Where(f => f.ClientId == clientId).AsNoTracking().ToListAsync(ct);

        var onTimePaymentPercentage = await GetOnTimePaymentPercentageAsync(clientId, ct);
        var filingTimelinessAverage = await GetFilingTimelinessAverageAsync(clientId, ct);
        var documentReadiness = await GetDocumentReadinessAsync(clientId, ct);
        var engagement = await GetClientEngagementAsync(clientId, ct);

        return new ClientKpiDto
        {
            ClientId = clientId,
            ClientName = client.Name,
            OnTimePaymentPercentage = onTimePaymentPercentage,
            FilingTimelinessAverage = filingTimelinessAverage,
            DocumentReadiness = documentReadiness.ReadinessPercentage,
            Engagement = engagement.EngagementScore,
            OverallScore = (onTimePaymentPercentage + (filingTimelinessAverage > 0 ? 100 : 0) + engagement.EngagementScore) / 3
        };
    }

    public async Task<List<KpiAlert>> GenerateAlertsAsync(KpiSnapshot snapshot, CancellationToken ct = default)
    {
        var alerts = new List<KpiAlert>();

        if (snapshot.ClientComplianceRate < ComplianceThreshold * 100)
        {
            alerts.Add(new KpiAlert
            {
                AlertType = "ComplianceRate",
                Message = $"Client compliance rate ({snapshot.ClientComplianceRate:F1}%) is below threshold ({ComplianceThreshold * 100:F1}%)",
                Severity = AlertSeverity.High,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (snapshot.TaxFilingTimeliness < FilingTimelinessThreshold * 100)
        {
            alerts.Add(new KpiAlert
            {
                AlertType = "FilingTimeliness",
                Message = $"Tax filing timeliness ({snapshot.TaxFilingTimeliness:F1}%) is below threshold ({FilingTimelinessThreshold * 100:F1}%)",
                Severity = AlertSeverity.Medium,
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    public async Task<DocumentReadinessDto> GetDocumentReadinessAsync(int clientId, CancellationToken ct = default)
    {
        var requirements = await _db.ClientDocumentRequirements
            .Where(cdr => cdr.ClientId == clientId)
            .Include(cdr => cdr.DocumentRequirement)
            .AsNoTracking()
            .ToListAsync(ct);

        var documents = await _db.Documents
            .Where(d => d.ClientId == clientId)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalRequired = requirements.Count;
        var submitted = requirements.Count(req => 
            documents.Any(doc => doc.DocumentType == req.DocumentRequirement.DocumentType));
        var onTime = requirements.Count(req => 
        {
            var doc = documents.FirstOrDefault(d => d.DocumentType == req.DocumentRequirement.DocumentType);
            return doc != null && req.DueDate.HasValue && doc.UploadedAt <= req.DueDate.Value;
        });

        return new DocumentReadinessDto
        {
            TotalRequired = totalRequired,
            Submitted = submitted,
            OnTime = onTime,
            ReadinessPercentage = totalRequired == 0 ? 100 : (decimal)submitted / totalRequired * 100,
            OnTimePercentage = totalRequired == 0 ? 100 : (decimal)onTime / totalRequired * 100
        };
    }

    public async Task<decimal> GetOnTimePaymentPercentageAsync(int clientId, CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .Where(p => p.ClientId == clientId && p.DueDate.HasValue)
            .AsNoTracking()
            .ToListAsync(ct);

        if (!payments.Any())
            return 100m;

        var onTimePayments = payments.Count(p => 
            p.Status == PaymentStatus.Approved && 
            p.ApprovedAt.HasValue && 
            p.ApprovedAt.Value <= p.DueDate!.Value);

        return (decimal)onTimePayments / payments.Count * 100;
    }

    public async Task<decimal> GetFilingTimelinessAverageAsync(int clientId, CancellationToken ct = default)
    {
        var filings = await _db.TaxFilings
            .Where(f => f.ClientId == clientId && f.SubmittedAt.HasValue && f.DueDate.HasValue)
            .AsNoTracking()
            .ToListAsync(ct);

        if (!filings.Any())
            return 0m;

        var totalDays = filings.Sum(f => (f.DueDate!.Value - f.SubmittedAt!.Value).Days);
        return (decimal)totalDays / filings.Count;
    }

    public async Task<ClientEngagementDto> GetClientEngagementAsync(int clientId, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        
        var recentPayments = await _db.Payments
            .Where(p => p.ClientId == clientId && p.CreatedAt >= since)
            .CountAsync(ct);

        var recentDocuments = await _db.Documents
            .Where(d => d.ClientId == clientId && d.UploadedAt >= since)
            .CountAsync(ct);

        var lastActivity = await _db.Payments
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var lastDocActivity = await _db.Documents
            .Where(d => d.ClientId == clientId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => d.UploadedAt)
            .FirstOrDefaultAsync(ct);

        var mostRecentActivity = new[] { lastActivity, lastDocActivity }.Where(d => d != default).Max();

        return new ClientEngagementDto
        {
            RecentPayments = recentPayments,
            RecentDocuments = recentDocuments,
            LastActivityDate = mostRecentActivity,
            EngagementScore = Math.Min(100, (recentPayments + recentDocuments) * 10)
        };
    }

    private static decimal Round(decimal value) => Math.Round(value * 100m, 2); // store as percentage 0-100
}
