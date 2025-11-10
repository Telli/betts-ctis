using BettsTax.Core.DTOs.KPI;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

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
                    _logger.LogInformation("Retrieved internal KPIs from cache");
                    return cached;
                }
            }

            var kpiData = await CalculateInternalKPIsAsync(fromDate, toDate);

            // Cache for 15 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(kpiData), cacheOptions);

            _logger.LogInformation("Calculated and cached internal KPIs");
            return kpiData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving internal KPIs");
            throw;
        }
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

            alerts.AddRange(kpiAlerts.Select(a => new KPIAlertDto
            {
                Id = a.Id,
                AlertType = (KPIAlertType)Enum.Parse(typeof(KPIAlertType), a.AlertType),
                Title = a.Message.Split(':')[0], // Extract title from message
                Message = a.Message,
                Severity = (KPIAlertSeverity)a.Severity,
                ClientId = a.ClientId,
                ClientName = a.Client?.BusinessName ?? a.Client?.FirstName + " " + a.Client?.LastName,
                CreatedAt = a.CreatedAt,
                IsRead = false // KpiAlert doesn't track read status
            }));

            return alerts.OrderByDescending(a => a.CreatedAt).ToList();
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
        // Implementation would calculate average days between filing due dates and actual filing dates
        // This is a simplified version
        return 5.2; // Average of 5.2 days delay
    }

    private async Task<decimal> CalculatePaymentCompletionRateAsync(DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would calculate percentage of payments completed on time
        return 87.5m;
    }

    private async Task<decimal> CalculateDocumentComplianceAsync(DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would calculate percentage of required documents submitted
        return 92.3m;
    }

    private async Task<decimal> CalculateClientEngagementRateAsync(DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would calculate client login frequency and interaction rates
        return 78.9m;
    }

    private async Task<List<TrendDataPoint>> CalculateComplianceTrendAsync(DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would return historical compliance trend data
        return new List<TrendDataPoint>
        {
            new() { Date = DateTime.Now.AddMonths(-3), Value = 82.5m, Label = "Q1" },
            new() { Date = DateTime.Now.AddMonths(-2), Value = 85.1m, Label = "Q2" },
            new() { Date = DateTime.Now.AddMonths(-1), Value = 87.3m, Label = "Q3" },
            new() { Date = DateTime.Now, Value = 89.2m, Label = "Current" }
        };
    }

    private async Task<List<TaxTypeMetrics>> CalculateTaxTypeBreakdownAsync(DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would return metrics broken down by tax type
        return new List<TaxTypeMetrics>
        {
            new() { TaxType = TaxType.GST, TotalFilings = 45, OnTimeFilings = 42, ComplianceRate = 93.3m, TotalAmount = 125000m, ClientCount = 15 },
            new() { TaxType = TaxType.IncomeTax, TotalFilings = 30, OnTimeFilings = 27, ComplianceRate = 90.0m, TotalAmount = 250000m, ClientCount = 30 },
            new() { TaxType = TaxType.PayrollTax, TotalFilings = 60, OnTimeFilings = 58, ComplianceRate = 96.7m, TotalAmount = 80000m, ClientCount = 20 }
        };
    }

    private async Task<double> CalculateClientFilingTimelinessAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would calculate average filing timeliness for specific client
        return 3.5; // 3.5 days average delay
    }

    private async Task<decimal> CalculateClientPaymentPercentageAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would calculate on-time payment percentage for specific client
        return 91.7m;
    }

    private async Task<decimal> CalculateClientDocumentReadinessAsync(int clientId)
    {
        // Implementation would calculate document readiness score for specific client
        return 88.5m;
    }

    private async Task<List<DeadlineMetric>> GetClientUpcomingDeadlinesAsync(int clientId)
    {
        // Implementation would return upcoming tax deadlines for specific client
        return new List<DeadlineMetric>
        {
            new() 
            { 
                Id = 1, 
                TaxType = TaxType.GST, 
                DueDate = DateTime.Now.AddDays(15), 
                DaysRemaining = 15, 
                Priority = (BettsTax.Core.DTOs.KPI.DeadlinePriority)BettsTax.Data.DeadlinePriority.Medium, 
                Status = FilingStatus.Submitted,
                EstimatedAmount = 25000m,
                DocumentsReady = true
            },
            new() 
            { 
                Id = 2, 
                TaxType = TaxType.IncomeTax, 
                DueDate = DateTime.Now.AddDays(45), 
                DaysRemaining = 45, 
                Priority = (BettsTax.Core.DTOs.KPI.DeadlinePriority)BettsTax.Data.DeadlinePriority.Low, 
                Status = FilingStatus.Draft,
                EstimatedAmount = 75000m,
                DocumentsReady = false
            }
        };
    }

    private async Task<List<TrendDataPoint>> GetClientFilingHistoryAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would return client's filing history trend
        return new List<TrendDataPoint>
        {
            new() { Date = DateTime.Now.AddMonths(-3), Value = 5.2m, Label = "3 months ago" },
            new() { Date = DateTime.Now.AddMonths(-2), Value = 3.8m, Label = "2 months ago" },
            new() { Date = DateTime.Now.AddMonths(-1), Value = 2.1m, Label = "Last month" },
            new() { Date = DateTime.Now, Value = 1.5m, Label = "This month" }
        };
    }

    private async Task<List<TrendDataPoint>> GetClientPaymentHistoryAsync(int clientId, DateTime? fromDate, DateTime? toDate)
    {
        // Implementation would return client's payment timeliness trend
        return new List<TrendDataPoint>
        {
            new() { Date = DateTime.Now.AddMonths(-3), Value = 85.5m, Label = "3 months ago" },
            new() { Date = DateTime.Now.AddMonths(-2), Value = 88.2m, Label = "2 months ago" },
            new() { Date = DateTime.Now.AddMonths(-1), Value = 91.1m, Label = "Last month" },
            new() { Date = DateTime.Now, Value = 94.3m, Label = "This month" }
        };
    }

    private async Task<List<KPIAlertDto>> GetOverdueFilingsAsync(int? clientId)
    {
        // Implementation would return overdue filing alerts
        var alerts = new List<KPIAlertDto>();
        
        // This would typically query the tax filing system for overdue items
        if (!clientId.HasValue || clientId == 1) // Example for client 1
        {
            alerts.Add(new KPIAlertDto
            {
                AlertType = KPIAlertType.FilingOverdue,
                Title = "GST Filing Overdue",
                Message = "GST filing for Q4 2024 is overdue by 5 days",
                Severity = KPIAlertSeverity.Error,
                ClientId = 1,
                ClientName = "Sample Client",
                CreatedAt = DateTime.Now.AddDays(-5)
            });
        }

        return alerts;
    }
}