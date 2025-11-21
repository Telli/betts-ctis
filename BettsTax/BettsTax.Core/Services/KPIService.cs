using BettsTax.Core.DTOs.KPI;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using DeadlinePriorityDto = BettsTax.Core.DTOs.KPI.DeadlinePriority;
using FilingStatusDto = BettsTax.Data.FilingStatus;

namespace BettsTax.Core.Services;

public class KPIService : IKPIService
{
    private readonly IClientService _clientService;
    private readonly ITaxFilingService _taxFilingService;
    private readonly IPaymentService _paymentService;
    private readonly IDocumentService _documentService;
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<KPIService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IKpiAlertService _kpiAlertService;

    private const string INTERNAL_KPI_CACHE_KEY = "internal_kpi_data";
    private const string CLIENT_KPI_CACHE_KEY = "client_kpi_data_{0}";
    private const int CACHE_EXPIRY_MINUTES = 15;

    public KPIService(
        IClientService clientService,
        ITaxFilingService taxFilingService,
        IPaymentService paymentService,
        IDocumentService documentService,
        INotificationService notificationService,
        IDistributedCache cache,
        ILogger<KPIService> logger,
        ApplicationDbContext context,
        IKpiAlertService kpiAlertService)
    {
        _clientService = clientService;
        _taxFilingService = taxFilingService;
        _paymentService = paymentService;
        _documentService = documentService;
        _notificationService = notificationService;
        _cache = cache;
        _logger = logger;
        _context = context;
        _kpiAlertService = kpiAlertService;
    }

    public async Task<ClientKPIDto> GetClientKPIsAsync(int clientId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var cacheKey = string.Format(CLIENT_KPI_CACHE_KEY, clientId) + $"_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cached = JsonSerializer.Deserialize<ClientKPIDto>(cachedData);
                if (cached != null)
                {
                    _logger.LogInformation("Retrieved client KPIs from cache for client {ClientId}", clientId);
                    return cached;
                }
            }

            var kpiData = await CalculateClientKPIsAsync(clientId, fromDate, toDate);
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
            };
            
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(kpiData), cacheOptions);
            
            _logger.LogInformation("Calculated and cached client KPIs for client {ClientId}", clientId);
            return kpiData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client KPIs for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<InternalKPIDto> GetInternalKPIsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var cacheKey = $"{INTERNAL_KPI_CACHE_KEY}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var cached = JsonSerializer.Deserialize<InternalKPIDto>(cachedData);
                if (cached != null)
                {
                    _logger.LogInformation("Retrieved internal KPIs from cache for range {FromDate} to {ToDate}", fromDate, toDate);
                    return cached;
                }
            }

            var kpiData = await CalculateInternalKPIsAsync(fromDate, toDate);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(kpiData), cacheOptions);
            _logger.LogInformation("Calculated and cached internal KPIs for range {FromDate} to {ToDate}", fromDate, toDate);

            return kpiData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving internal KPIs for range {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<KpiDashboardSummaryDto> GetDashboardSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var periodEnd = toDate?.ToUniversalTime() ?? DateTime.UtcNow;
            var periodStart = fromDate?.ToUniversalTime() ?? periodEnd.AddMonths(-3);

            if (periodEnd < periodStart)
            {
                (periodStart, periodEnd) = (periodEnd, periodStart);
            }

            var internalKpis = await GetInternalKPIsAsync(periodStart, periodEnd);

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Approved &&
                            p.PaymentDate >= periodStart &&
                            p.PaymentDate <= periodEnd)
                .Select(p => new { p.Amount, p.Currency })
                .ToListAsync();

            var totalRevenue = payments.Sum(p => p.Amount);
            var revenueCurrency = payments
                .Select(p => p.Currency)
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "SLE";

            var periodLengthDays = Math.Max(1d, (periodEnd - periodStart).TotalDays);
            var previousEnd = periodStart;
            var previousStart = periodStart.AddDays(-periodLengthDays);

            var previousPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Approved &&
                            p.PaymentDate >= previousStart &&
                            p.PaymentDate < previousEnd)
                .Select(p => p.Amount)
                .ToListAsync();

            var previousRevenue = previousPayments.Sum();
            decimal? revenueChange = null;
            if (previousRevenue > 0)
            {
                revenueChange = Math.Round(((totalRevenue - previousRevenue) / previousRevenue) * 100m, 1);
            }

            var activeClientsCount = await _context.Clients.CountAsync(c => c.Status == ClientStatus.Active);
            var totalClientsCount = await _context.Clients.CountAsync();

            var processedFilings = await _context.TaxFilings
                .AsNoTracking()
                .Where(tf => tf.SubmittedDate != null &&
                             tf.ReviewedDate != null &&
                             tf.SubmittedDate >= periodStart &&
                             tf.ReviewedDate <= periodEnd)
                .Select(tf => new { tf.SubmittedDate, tf.ReviewedDate })
                .ToListAsync();

            double averageProcessingDays = processedFilings.Any()
                ? processedFilings.Average(tf => (tf.ReviewedDate!.Value - tf.SubmittedDate!.Value).TotalDays)
                : 0d;

            if (averageProcessingDays <= 0)
            {
                var fallbackFilings = await _context.TaxFilings
                    .AsNoTracking()
                    .Where(tf => tf.SubmittedDate != null &&
                                 tf.SubmittedDate >= periodStart &&
                                 tf.SubmittedDate <= periodEnd)
                    .Select(tf => new { tf.SubmittedDate, tf.CreatedDate })
                    .ToListAsync();

                if (fallbackFilings.Any())
                {
                    averageProcessingDays = fallbackFilings.Average(tf => (tf.SubmittedDate!.Value - tf.CreatedDate).TotalDays);
                }
            }

            averageProcessingDays = Math.Round(averageProcessingDays, 2, MidpointRounding.AwayFromZero);

            var complianceScores = await _context.ComplianceScores
                .AsNoTracking()
                .Include(cs => cs.Client)
                .ToListAsync();

            var latestScores = complianceScores
                .GroupBy(cs => cs.ClientId)
                .Select(g => g.OrderByDescending(x => x.CalculatedAt).First())
                .ToList();

            var averageCompliance = latestScores.Any()
                ? Math.Round(latestScores.Average(cs => cs.OverallScore), 1, MidpointRounding.AwayFromZero)
                : 0m;

            var topScore = latestScores
                .OrderByDescending(cs => cs.OverallScore)
                .FirstOrDefault();

            var clients = await _context.Clients.AsNoTracking().ToListAsync();
            var segments = clients
                .GroupBy(c => c.TaxpayerCategory)
                .Select(g =>
                {
                    var clientIds = g.Select(c => c.ClientId).ToHashSet();
                    var segmentScores = latestScores.Where(cs => clientIds.Contains(cs.ClientId)).ToList();
                    var complianceRate = segmentScores.Any()
                        ? Math.Round(segmentScores.Average(cs => cs.OverallScore), 1, MidpointRounding.AwayFromZero)
                        : 0m;

                    return new KpiClientSegmentPerformanceDto
                    {
                        Segment = g.Key.ToString(),
                        ComplianceRate = complianceRate,
                        ClientCount = g.Count()
                    };
                })
                .OrderByDescending(s => s.ComplianceRate)
                .ToList();

            var summary = new KpiDashboardSummaryDto
            {
                Internal = new KpiDashboardInternalSummaryDto
                {
                    TotalRevenue = Math.Round(totalRevenue, 2, MidpointRounding.AwayFromZero),
                    RevenueCurrency = revenueCurrency,
                    RevenueChangePercentage = revenueChange,
                    ActiveClients = activeClientsCount,
                    TotalClients = totalClientsCount,
                    ComplianceRate = Math.Round(internalKpis.ClientComplianceRate, 1, MidpointRounding.AwayFromZero),
                    PaymentCompletionRate = Math.Round(internalKpis.PaymentCompletionRate, 1, MidpointRounding.AwayFromZero),
                    DocumentSubmissionRate = Math.Round(internalKpis.DocumentSubmissionCompliance, 1, MidpointRounding.AwayFromZero),
                    AverageFilingTimelinessDays = Math.Round(internalKpis.AverageFilingTimeliness, 2, MidpointRounding.AwayFromZero),
                    AverageProcessingTimeDays = averageProcessingDays,
                    ClientEngagementRate = Math.Round(internalKpis.ClientEngagementRate, 1, MidpointRounding.AwayFromZero),
                    ReferencePeriodLabel = $"{periodStart:MMM dd, yyyy} - {periodEnd:MMM dd, yyyy}"
                },
                Client = new KpiDashboardClientSummaryDto
                {
                    TotalClients = totalClientsCount,
                    ActiveClients = activeClientsCount,
                    AverageComplianceScore = averageCompliance,
                    AverageFilingTimeDays = Math.Round(internalKpis.AverageFilingTimeliness, 2, MidpointRounding.AwayFromZero),
                    TopPerformerName = topScore?.Client?.BusinessName ?? topScore?.Client?.CompanyName ?? topScore?.Client?.FirstName ?? topScore?.Client?.LastName,
                    TopPerformerComplianceScore = topScore != null ? Math.Round(topScore.OverallScore, 1, MidpointRounding.AwayFromZero) : 0m,
                    Segments = segments
                }
            };

            if (summary.Client.TopPerformerComplianceScore > 0 && string.IsNullOrWhiteSpace(summary.Client.TopPerformerName))
            {
                var topClient = clients.FirstOrDefault(c => c.ClientId == topScore!.ClientId);
                summary.Client.TopPerformerName = topClient?.BusinessName ?? topClient?.CompanyName ?? topClient?.FirstName ?? "Top Performer";
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI dashboard summary");
            throw;
        }
    }

    public async Task<List<KPIAlertDto>> GetKPIAlertsAsync(int? clientId = null)
    {
        try
        {
            var alerts = new List<KPIAlertDto>();

            // Get compliance alerts from KpiAlerts table
            var query = _context.KpiAlerts
                .Include(a => a.Client)
                .Where(a => !a.IsResolved);

            if (clientId.HasValue)
            {
                query = query.Where(a => a.ClientId == clientId.Value);
            }

            var kpiAlerts = await query
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            alerts.AddRange(kpiAlerts.Select(a =>
            {
                var parsedType = Enum.TryParse<KPIAlertType>(a.AlertType, out var alertType)
                    ? alertType
                    : KPIAlertType.ComplianceThreshold;

                return new KPIAlertDto
                {
                    Id = a.Id,
                    AlertType = parsedType,
                    Title = a.Message.Split(':')[0], // Extract title from message
                    Message = a.Message,
                    Severity = Enum.IsDefined(typeof(KPIAlertSeverity), (int)a.Severity)
                        ? (KPIAlertSeverity)a.Severity
                        : KPIAlertSeverity.Info,
                    ClientId = a.ClientId,
                    ClientName = a.Client?.BusinessName
                        ?? a.Client?.CompanyName
                        ?? string.Join(" ", new[] { a.Client?.FirstName, a.Client?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    CreatedAt = a.CreatedAt,
                    IsRead = false // KpiAlert doesn't track read status
                };
            }));

            var overdueFilings = await GetOverdueFilingsAsync(clientId);
            alerts.AddRange(overdueFilings);

            var upcomingDeadlines = await GetUpcomingDeadlineAlertsAsync(clientId);
            alerts.AddRange(upcomingDeadlines);

            return alerts
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI alerts for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task UpdateKPIThresholdsAsync(KPIThresholdDto thresholds)
    {
        try
        {
            // Store thresholds in configuration or database
            // For now, we'll store as KPI metrics
            var thresholdMetrics = new List<KPIMetric>
            {
                new() { MetricName = "MinComplianceRate", Value = thresholds.MinComplianceRate, Period = "Config", CalculatedAt = DateTime.UtcNow },
                new() { MetricName = "MaxFilingDelayDays", Value = (decimal)thresholds.MaxFilingDelayDays, Period = "Config", CalculatedAt = DateTime.UtcNow },
                new() { MetricName = "MinPaymentCompletionRate", Value = thresholds.MinPaymentCompletionRate, Period = "Config", CalculatedAt = DateTime.UtcNow },
                new() { MetricName = "MinDocumentCompletionRate", Value = thresholds.MinDocumentCompletionRate, Period = "Config", CalculatedAt = DateTime.UtcNow },
                new() { MetricName = "MinEngagementRate", Value = thresholds.MinEngagementRate, Period = "Config", CalculatedAt = DateTime.UtcNow }
            };

            foreach (var metric in thresholdMetrics)
            {
                _context.KPIMetrics.Add(metric);
            }
            await _context.SaveChangesAsync();

            // Clear cache to force recalculation
            await _cache.RemoveAsync(INTERNAL_KPI_CACHE_KEY);
            
            _logger.LogInformation("Updated KPI thresholds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI thresholds");
            throw;
        }
    }

    public async Task<bool> RefreshKPIDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting KPI data refresh");

            // Clear all KPI caches
            await _cache.RemoveAsync(INTERNAL_KPI_CACHE_KEY);

            // Recalculate internal KPIs
            await GetInternalKPIsAsync();

            // Recalculate client KPIs for all active clients
            var clients = await _clientService.GetAllAsync();
            foreach (var client in clients.Where(c => c.Status == ClientStatus.Active))
            {
                await GetClientKPIsAsync(client.ClientId);
            }

            _logger.LogInformation("Completed KPI data refresh for {ClientCount} clients", clients.Count());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KPI data refresh");
            return false;
        }
    }

    public async Task<List<InternalKPIDto>> GetKPITrendsAsync(DateTime fromDate, DateTime toDate, string period = "Monthly")
    {
        try
        {
            var trends = new List<InternalKPIDto>();
            var currentDate = fromDate;

            while (currentDate <= toDate)
            {
                var nextDate = period switch
                {
                    "Daily" => currentDate.AddDays(1),
                    "Weekly" => currentDate.AddDays(7),
                    "Monthly" => currentDate.AddMonths(1),
                    _ => currentDate.AddMonths(1)
                };

                var kpiData = await CalculateInternalKPIsAsync(currentDate, nextDate);
                kpiData.Period = period;
                trends.Add(kpiData);

                currentDate = nextDate;
            }

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI trends from {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task CreateKPIAlertAsync(KPIAlertDto alert, string? createdBy = null)
    {
        try
        {
            var kpiAlert = new KpiAlert
            {
                AlertType = alert.AlertType.ToString(),
                Message = alert.Message,
                Severity = (AlertSeverity)alert.Severity,
                ClientId = alert.ClientId,
                CreatedAt = DateTime.UtcNow
            };

            _context.KpiAlerts.Add(kpiAlert);
            await _context.SaveChangesAsync();

            // Process the alert through the alert service
            await _kpiAlertService.ProcessAlertsAsync(new List<KpiAlert> { kpiAlert });

            _logger.LogInformation("Created KPI alert: {Title}", alert.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI alert");
            throw;
        }
    }

    public async Task MarkAlertAsReadAsync(int alertId, string resolvedBy)
    {
        try
        {
            await _kpiAlertService.ResolveAlertAsync(alertId, resolvedBy, "Marked as read");
            _logger.LogInformation("Marked KPI alert {AlertId} as read by {ResolvedBy}", alertId, resolvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking KPI alert {AlertId} as read", alertId);
            throw;
        }
    }

    private async Task<InternalKPIDto> CalculateInternalKPIsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var clients = await _clientService.GetAllAsync();
        var activeClients = clients.Where(c => c.Status == ClientStatus.Active).ToList();

        var complianceScores = _context.ComplianceScores.AsQueryable();
        if (fromDate.HasValue || toDate.HasValue)
        {
            complianceScores = complianceScores.Where(cs => 
                (!fromDate.HasValue || cs.CalculatedAt >= fromDate) &&
                (!toDate.HasValue || cs.CalculatedAt <= toDate));
        }
        var complianceScoresList = complianceScores.ToList();

        var clientComplianceRate = activeClients.Any() 
            ? complianceScoresList.Where(cs => cs.OverallScore >= 70).Count() * 100m / activeClients.Count 
            : 0m;

        var averageFilingTimeliness = await CalculateAverageFilingTimelinessAsync(fromDate, toDate);
        var paymentCompletionRate = await CalculatePaymentCompletionRateAsync(fromDate, toDate);
        var documentSubmissionCompliance = await CalculateDocumentComplianceAsync(fromDate, toDate);
        var clientEngagementRate = await CalculateClientEngagementRateAsync(fromDate, toDate);

        var complianceTrend = await CalculateComplianceTrendAsync(fromDate, toDate);
        var taxTypeBreakdown = await CalculateTaxTypeBreakdownAsync(fromDate, toDate);

        return new InternalKPIDto
        {
            ClientComplianceRate = clientComplianceRate,
            AverageFilingTimeliness = averageFilingTimeliness,
            PaymentCompletionRate = paymentCompletionRate,
            DocumentSubmissionCompliance = documentSubmissionCompliance,
            ClientEngagementRate = clientEngagementRate,
            ComplianceTrend = complianceTrend,
            TaxTypeBreakdown = taxTypeBreakdown,
            CalculatedAt = DateTime.UtcNow,
            Period = "Current"
        };
    }

    private async Task<ClientKPIDto> CalculateClientKPIsAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        var client = await _clientService.GetByIdAsync(clientId);
        if (client == null)
            throw new ArgumentException($"Client with ID {clientId} not found");

        var latestScore = _context.ComplianceScores
            .Where(cs => cs.ClientId == clientId)
            .OrderByDescending(cs => cs.CalculatedAt)
            .FirstOrDefault();

        var filingTimeliness = await CalculateClientFilingTimelinessAsync(clientId, fromDate, toDate);
        var paymentPercentage = await CalculateClientPaymentPercentageAsync(clientId, fromDate, toDate);
        var documentReadiness = await CalculateClientDocumentReadinessAsync(clientId);
        var upcomingDeadlines = await GetClientUpcomingDeadlinesAsync(clientId);
        var filingHistory = await GetClientFilingHistoryAsync(clientId, fromDate, toDate);
        var paymentHistory = await GetClientPaymentHistoryAsync(clientId, fromDate, toDate);

        var overallScore = latestScore?.OverallScore ?? 0m;
        var complianceLevel = overallScore >= 85 ? ComplianceLevel.Green :
                             overallScore >= 70 ? ComplianceLevel.Yellow :
                             ComplianceLevel.Red;

        return new ClientKPIDto
        {
            MyFilingTimeliness = filingTimeliness,
            OnTimePaymentPercentage = paymentPercentage,
            DocumentReadinessScore = documentReadiness,
            ComplianceScore = overallScore,
            ComplianceLevel = complianceLevel,
            UpcomingDeadlines = upcomingDeadlines,
            FilingHistory = filingHistory,
            PaymentHistory = paymentHistory,
            CalculatedAt = DateTime.UtcNow
        };
    }

    // Helper methods for calculations
    private async Task<double> CalculateAverageFilingTimelinessAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.DateFiled != null && ty.FilingDeadline != null);

        if (fromDate.HasValue)
        {
            query = query.Where(ty => ty.DateFiled >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ty => ty.DateFiled <= toDate.Value);
        }

        var filings = await query
            .Select(ty => new { ty.DateFiled, ty.FilingDeadline })
            .ToListAsync();

        if (!filings.Any())
        {
            return 0d;
        }

        var averageDays = filings.Average(ty => (ty.FilingDeadline!.Value - ty.DateFiled!.Value).TotalDays);
        return Math.Round(averageDays, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> CalculatePaymentCompletionRateAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.DueDate != null);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.DueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.DueDate <= toDate.Value);
        }

        var payments = await query
            .Select(p => new { p.Status, p.PaymentDate, p.DueDate })
            .ToListAsync();

        if (!payments.Any())
        {
            return 0m;
        }

        var onTimePayments = payments.Count(p => p.Status == PaymentStatus.Approved && p.PaymentDate <= p.DueDate);
        return Math.Round((decimal)onTimePayments * 100 / payments.Count, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> CalculateDocumentComplianceAsync(DateTime? fromDate, DateTime? toDate)
    {
        var taxYearQuery = _context.TaxYears.AsNoTracking();

        if (fromDate.HasValue)
        {
            taxYearQuery = taxYearQuery.Where(ty => ty.FilingDeadline == null || ty.FilingDeadline >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            taxYearQuery = taxYearQuery.Where(ty => ty.FilingDeadline == null || ty.FilingDeadline <= toDate.Value);
        }

        var taxYearIds = await taxYearQuery
            .Select(ty => ty.TaxYearId)
            .ToListAsync();

        if (!taxYearIds.Any())
        {
            return 100m;
        }

        var taxYearsWithDocuments = await _context.Documents
            .AsNoTracking()
            .Where(d => d.TaxYearId != null && taxYearIds.Contains(d.TaxYearId.Value))
            .Select(d => d.TaxYearId!.Value)
            .Distinct()
            .CountAsync();

        var compliance = (decimal)taxYearsWithDocuments * 100 / taxYearIds.Count;
        return Math.Round(compliance, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> CalculateClientEngagementRateAsync(DateTime? fromDate, DateTime? toDate)
    {
        var activeClients = await _context.Clients
            .AsNoTracking()
            .Where(c => c.Status == ClientStatus.Active)
            .ToListAsync();

        if (!activeClients.Any())
        {
            return 0m;
        }

        var activeClientIds = activeClients.Select(c => c.ClientId).ToHashSet();

        var start = fromDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = toDate ?? DateTime.UtcNow;

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var clientUserIds = activeClients
            .Where(c => !string.IsNullOrWhiteSpace(c.UserId))
            .Select(c => c.UserId!)
            .Distinct()
            .ToList();

        var engagedClientIds = new HashSet<int>();

        if (clientUserIds.Any())
        {
            var engagedUsers = await _context.AuditLogs
                .AsNoTracking()
                .Where(log => log.UserId != null &&
                             clientUserIds.Contains(log.UserId) &&
                             log.Timestamp >= start &&
                             log.Timestamp <= end)
                .Select(log => log.UserId!)
                .Distinct()
                .ToListAsync();

            foreach (var userId in engagedUsers)
            {
                var client = activeClients.FirstOrDefault(c => c.UserId == userId);
                if (client != null)
                {
                    engagedClientIds.Add(client.ClientId);
                }
            }
        }

        var documentClientIds = await _context.Documents
            .AsNoTracking()
            .Where(d => d.UploadedAt >= start && d.UploadedAt <= end)
            .Select(d => d.ClientId)
            .Distinct()
            .ToListAsync();

        foreach (var clientId in documentClientIds)
        {
            if (activeClientIds.Contains(clientId))
            {
                engagedClientIds.Add(clientId);
            }
        }

        var paymentClientIds = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
            .Select(p => p.ClientId)
            .Distinct()
            .ToListAsync();

        foreach (var clientId in paymentClientIds)
        {
            if (activeClientIds.Contains(clientId))
            {
                engagedClientIds.Add(clientId);
            }
        }

        var engagedCount = engagedClientIds.Count;
        return Math.Round((decimal)engagedCount * 100 / activeClients.Count, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<List<TrendDataPoint>> CalculateComplianceTrendAsync(DateTime? fromDate, DateTime? toDate)
    {
        var end = toDate ?? DateTime.UtcNow;
        var start = fromDate ?? end.AddMonths(-5);

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var filings = await _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.DateFiled != null &&
                         ty.FilingDeadline != null &&
                         ty.DateFiled >= start &&
                         ty.DateFiled <= end)
            .Select(ty => new { ty.DateFiled, ty.FilingDeadline })
            .ToListAsync();

        var result = new List<TrendDataPoint>();
        var cursor = new DateTime(start.Year, start.Month, 1);

        while (cursor <= end)
        {
            var monthStart = cursor;
            var monthEnd = cursor.AddMonths(1).AddTicks(-1);

            var monthFilings = filings
                .Where(f => f.DateFiled >= monthStart && f.DateFiled <= monthEnd)
                .ToList();

            var compliance = monthFilings.Any()
                ? Math.Round((decimal)monthFilings.Count(f => f.FilingDeadline >= f.DateFiled) * 100 / monthFilings.Count, 1, MidpointRounding.AwayFromZero)
                : 0m;

            result.Add(new TrendDataPoint
            {
                Date = monthEnd,
                Value = compliance,
                Label = monthStart.ToString("MMM yy", CultureInfo.InvariantCulture)
            });

            cursor = cursor.AddMonths(1);
        }

        if (!result.Any())
        {
            result.Add(new TrendDataPoint
            {
                Date = end,
                Value = 0m,
                Label = end.ToString("MMM yy", CultureInfo.InvariantCulture)
            });
        }

        return result;
    }

    private async Task<List<TaxTypeMetrics>> CalculateTaxTypeBreakdownAsync(DateTime? fromDate, DateTime? toDate)
    {
        var filingsQuery = _context.TaxFilings.AsNoTracking();

        if (fromDate.HasValue)
        {
            filingsQuery = filingsQuery.Where(tf => tf.FilingDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filingsQuery = filingsQuery.Where(tf => tf.FilingDate <= toDate.Value);
        }

        var filings = await filingsQuery
            .Select(tf => new { tf.TaxType, tf.FilingDate, tf.DueDate, tf.ClientId, tf.TaxAmount })
            .ToListAsync();

        if (!filings.Any())
        {
            return new List<TaxTypeMetrics>();
        }

        return filings
            .GroupBy(tf => tf.TaxType)
            .Select(group =>
            {
                var total = group.Count();
                var onTime = group.Count(f => f.DueDate == null || f.FilingDate <= f.DueDate);
                var compliance = total > 0 ? Math.Round(onTime * 100m / total, 1, MidpointRounding.AwayFromZero) : 0m;
                var totalAmount = Math.Round(group.Sum(f => f.TaxAmount), 2, MidpointRounding.AwayFromZero);
                var dtoTaxType = Enum.TryParse<TaxType>(group.Key.ToString(), out var parsed) ? parsed : TaxType.IncomeTax;

                return new TaxTypeMetrics
                {
                    TaxType = dtoTaxType,
                    TotalFilings = total,
                    OnTimeFilings = onTime,
                    ComplianceRate = compliance,
                    TotalAmount = totalAmount,
                    ClientCount = group.Select(f => f.ClientId).Distinct().Count()
                };
            })
            .OrderByDescending(m => m.TotalFilings)
            .ToList();
    }

    private async Task<double> CalculateClientFilingTimelinessAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.ClientId == clientId && ty.DateFiled != null && ty.FilingDeadline != null);

        if (fromDate.HasValue)
        {
            query = query.Where(ty => ty.DateFiled >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(ty => ty.DateFiled <= toDate.Value);
        }

        var filings = await query
            .Select(ty => new { ty.DateFiled, ty.FilingDeadline })
            .ToListAsync();

        if (!filings.Any())
        {
            return 0d;
        }

        var averageDays = filings.Average(ty => (ty.FilingDeadline!.Value - ty.DateFiled!.Value).TotalDays);
        return Math.Round(averageDays, 2, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> CalculateClientPaymentPercentageAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.ClientId == clientId && p.DueDate != null);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.DueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.DueDate <= toDate.Value);
        }

        var payments = await query
            .Select(p => new { p.Status, p.PaymentDate, p.DueDate })
            .ToListAsync();

        if (!payments.Any())
        {
            return 0m;
        }

        var onTimePayments = payments.Count(p => p.Status == PaymentStatus.Approved && p.PaymentDate <= p.DueDate);
        return Math.Round((decimal)onTimePayments * 100 / payments.Count, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<decimal> CalculateClientDocumentReadinessAsync(int clientId)
    {
        var taxYearIds = await _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.ClientId == clientId)
            .Select(ty => ty.TaxYearId)
            .ToListAsync();

        if (!taxYearIds.Any())
        {
            return 100m;
        }

        var taxYearsWithDocuments = await _context.Documents
            .AsNoTracking()
            .Where(d => d.ClientId == clientId && d.TaxYearId != null && taxYearIds.Contains(d.TaxYearId.Value))
            .Select(d => d.TaxYearId!.Value)
            .Distinct()
            .CountAsync();

        var readiness = (decimal)taxYearsWithDocuments * 100 / taxYearIds.Count;
        return Math.Round(readiness, 1, MidpointRounding.AwayFromZero);
    }

    private async Task<List<DeadlineMetric>> GetClientUpcomingDeadlinesAsync(int clientId)
    {
        var upcoming = await _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.ClientId == clientId &&
                         ty.FilingDeadline != null &&
                         ty.FilingDeadline >= DateTime.UtcNow.AddDays(-1) &&
                         (ty.Status == TaxYearStatus.Draft || ty.Status == TaxYearStatus.Pending))
            .OrderBy(ty => ty.FilingDeadline)
            .Take(10)
            .Select(ty => new { ty.TaxYearId, ty.FilingDeadline, ty.Status })
            .ToListAsync();

        if (!upcoming.Any())
        {
            return new List<DeadlineMetric>();
        }

        var taxYearIds = upcoming.Select(u => u.TaxYearId).ToList();

        var filingTypes = await _context.TaxFilings
            .AsNoTracking()
            .Where(tf => tf.ClientId == clientId && taxYearIds.Contains(tf.TaxYearId))
            .GroupBy(tf => tf.TaxYearId)
            .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(tf => tf.UpdatedDate).First().TaxType);

        var documentsLookup = await _context.Documents
            .AsNoTracking()
            .Where(d => d.ClientId == clientId && d.TaxYearId != null && taxYearIds.Contains(d.TaxYearId.Value))
            .GroupBy(d => d.TaxYearId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Any());

        return upcoming.Select(item =>
        {
            var dueDate = item.FilingDeadline!.Value;
            var daysRemaining = (int)Math.Ceiling((dueDate - DateTime.UtcNow).TotalDays);
            var priority = daysRemaining switch
            {
                <= 0 => DeadlinePriorityDto.Critical,
                <= 7 => DeadlinePriorityDto.High,
                <= 14 => DeadlinePriorityDto.Medium,
                _ => DeadlinePriorityDto.Low
            };

            var dtoTaxType = filingTypes.TryGetValue(item.TaxYearId, out var taxType)
                ? Enum.TryParse<TaxType>(taxType.ToString(), out var parsed) ? parsed : TaxType.IncomeTax
                : TaxType.IncomeTax;

            return new DeadlineMetric
            {
                Id = item.TaxYearId,
                TaxType = dtoTaxType,
                DueDate = dueDate,
                DaysRemaining = daysRemaining,
                Priority = priority,
                Status = MapTaxYearStatus(item.Status),
                EstimatedAmount = null,
                DocumentsReady = documentsLookup.ContainsKey(item.TaxYearId) && documentsLookup[item.TaxYearId]
            };
        }).ToList();
    }

    private FilingStatusDto MapTaxYearStatus(TaxYearStatus status)
    {
        return status switch
        {
            TaxYearStatus.Pending => FilingStatusDto.Submitted,
            TaxYearStatus.Filed => FilingStatusDto.Filed,
            TaxYearStatus.Paid => FilingStatusDto.Approved,
            TaxYearStatus.Overdue => FilingStatusDto.Submitted,
            TaxYearStatus.Draft => FilingStatusDto.Draft,
            _ => FilingStatusDto.Draft
        };
    }

    private async Task<List<TrendDataPoint>> GetClientFilingHistoryAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        var end = toDate ?? DateTime.UtcNow;
        var start = fromDate ?? end.AddMonths(-5);

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var filings = await _context.TaxYears
            .AsNoTracking()
            .Where(ty => ty.ClientId == clientId &&
                         ty.DateFiled != null &&
                         ty.FilingDeadline != null &&
                         ty.DateFiled >= start &&
                         ty.DateFiled <= end)
            .Select(ty => new { ty.DateFiled, ty.FilingDeadline })
            .ToListAsync();

        var result = new List<TrendDataPoint>();
        var cursor = new DateTime(start.Year, start.Month, 1);

        while (cursor <= end)
        {
            var monthStart = cursor;
            var monthEnd = cursor.AddMonths(1).AddTicks(-1);

            var monthFilings = filings
                .Where(f => f.DateFiled >= monthStart && f.DateFiled <= monthEnd)
                .ToList();

            var averageDelay = monthFilings.Any()
                ? Math.Round(monthFilings.Average(f => (f.FilingDeadline!.Value - f.DateFiled!.Value).TotalDays), 2, MidpointRounding.AwayFromZero)
                : 0d;

            result.Add(new TrendDataPoint
            {
                Date = monthEnd,
                Value = (decimal)averageDelay,
                Label = monthStart.ToString("MMM yy", CultureInfo.InvariantCulture)
            });

            cursor = cursor.AddMonths(1);
        }

        if (!result.Any())
        {
            result.Add(new TrendDataPoint
            {
                Date = end,
                Value = 0m,
                Label = end.ToString("MMM yy", CultureInfo.InvariantCulture)
            });
        }

        return result;
    }

    private async Task<List<TrendDataPoint>> GetClientPaymentHistoryAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        var end = toDate ?? DateTime.UtcNow;
        var start = fromDate ?? end.AddMonths(-5);

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ClientId == clientId &&
                        p.DueDate != null &&
                        p.DueDate >= start &&
                        p.DueDate <= end)
            .Select(p => new { p.DueDate, p.PaymentDate, p.Status })
            .ToListAsync();

        var result = new List<TrendDataPoint>();
        var cursor = new DateTime(start.Year, start.Month, 1);

        while (cursor <= end)
        {
            var monthStart = cursor;
            var monthEnd = cursor.AddMonths(1).AddTicks(-1);

            var monthPayments = payments
                .Where(p => p.DueDate >= monthStart && p.DueDate <= monthEnd)
                .ToList();

            var onTime = monthPayments.Count(p => p.Status == PaymentStatus.Approved && p.PaymentDate <= p.DueDate);
            var percentage = monthPayments.Any()
                ? Math.Round((decimal)onTime * 100 / monthPayments.Count, 1, MidpointRounding.AwayFromZero)
                : 0m;

            result.Add(new TrendDataPoint
            {
                Date = monthEnd,
                Value = percentage,
                Label = monthStart.ToString("MMM yy", CultureInfo.InvariantCulture)
            });

            cursor = cursor.AddMonths(1);
        }

        if (!result.Any())
        {
            result.Add(new TrendDataPoint
            {
                Date = end,
                Value = 0m,
                Label = end.ToString("MMM yy", CultureInfo.InvariantCulture)
            });
        }

        return result;
    }

    private async Task<List<KPIAlertDto>> GetOverdueFilingsAsync(int? clientId)
    {
        var query = _context.TaxYears
            .AsNoTracking()
            .Include(ty => ty.Client)
            .Where(ty => ty.FilingDeadline != null &&
                         ty.FilingDeadline < DateTime.UtcNow &&
                         ty.Status != TaxYearStatus.Filed &&
                         ty.Status != TaxYearStatus.Paid);

        if (clientId.HasValue)
        {
            query = query.Where(ty => ty.ClientId == clientId.Value);
        }

        var overdueItems = await query.ToListAsync();
        var alerts = new List<KPIAlertDto>();

        foreach (var item in overdueItems)
        {
            var daysOverdue = (int)Math.Floor((DateTime.UtcNow - item.FilingDeadline!.Value).TotalDays);
            var severity = daysOverdue switch
            {
                >= 14 => KPIAlertSeverity.Critical,
                >= 7 => KPIAlertSeverity.Error,
                >= 1 => KPIAlertSeverity.Warning,
                _ => KPIAlertSeverity.Warning
            };

            alerts.Add(new KPIAlertDto
            {
                AlertType = KPIAlertType.FilingOverdue,
                Title = $"Filing overdue: {item.Year}",
                Message = $"Tax filing for {item.Year} is overdue by {daysOverdue} day{(daysOverdue == 1 ? string.Empty : "s")}",
                Severity = severity,
                ClientId = item.ClientId,
                ClientName = item.Client?.BusinessName ?? item.Client?.CompanyName ?? item.Client?.FirstName ?? item.Client?.LastName,
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    private async Task<List<KPIAlertDto>> GetUpcomingDeadlineAlertsAsync(int? clientId)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(14);

        var query = _context.TaxYears
            .AsNoTracking()
            .Include(ty => ty.Client)
            .Where(ty => ty.FilingDeadline != null &&
                         ty.FilingDeadline >= now &&
                         ty.FilingDeadline <= horizon &&
                         (ty.Status == TaxYearStatus.Draft || ty.Status == TaxYearStatus.Pending));

        if (clientId.HasValue)
        {
            query = query.Where(ty => ty.ClientId == clientId.Value);
        }

        var upcoming = await query.ToListAsync();

        if (!upcoming.Any())
        {
            return new List<KPIAlertDto>();
        }

        var taxYearIds = upcoming.Select(ty => ty.TaxYearId).ToList();

        var filingTypes = await _context.TaxFilings
            .AsNoTracking()
            .Where(tf => taxYearIds.Contains(tf.TaxYearId))
            .GroupBy(tf => tf.TaxYearId)
            .Select(g => new { TaxYearId = g.Key, TaxType = g.OrderByDescending(tf => tf.UpdatedDate).First().TaxType })
            .ToDictionaryAsync(x => x.TaxYearId, x => x.TaxType);

        var alerts = new List<KPIAlertDto>();

        foreach (var item in upcoming)
        {
            var dueDate = item.FilingDeadline!.Value;
            var daysRemaining = (int)Math.Ceiling((dueDate - now).TotalDays);
            if (daysRemaining < 0)
            {
                daysRemaining = 0;
            }

            var severity = daysRemaining switch
            {
                <= 3 => KPIAlertSeverity.Critical,
                <= 7 => KPIAlertSeverity.Error,
                <= 14 => KPIAlertSeverity.Warning,
                _ => KPIAlertSeverity.Info
            };

            var taxTypeName = filingTypes.TryGetValue(item.TaxYearId, out var taxType)
                ? taxType.ToString()
                : "Tax";

            alerts.Add(new KPIAlertDto
            {
                AlertType = KPIAlertType.UpcomingDeadline,
                Title = $"{taxTypeName} filing due soon",
                Message = $"{taxTypeName} filing for tax year {item.Year} is due on {dueDate:MMM dd, yyyy} ({daysRemaining} day{(daysRemaining == 1 ? string.Empty : "s")} remaining).",
                Severity = severity,
                ClientId = item.ClientId,
                ClientName = item.Client?.BusinessName
                    ?? item.Client?.CompanyName
                    ?? string.Join(" ", new[] { item.Client?.FirstName, item.Client?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                ActionUrl = clientId.HasValue
                    ? $"/client-portal/tax-years/{item.TaxYearId}"
                    : $"/clients/{item.ClientId}/tax-years/{item.TaxYearId}"
            });
        }

        return alerts;
    }
}