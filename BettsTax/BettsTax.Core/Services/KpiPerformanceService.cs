using BettsTax.Core.DTOs;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace BettsTax.Core.Services;

public class KpiPerformanceService : IKpiPerformanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<KpiPerformanceService> _logger;
    
    private const string MonthlyAggregatesCacheKey = "kpi:monthly_aggregates";
    private const string QuarterlyAggregatesCacheKey = "kpi:quarterly_aggregates";
    private static readonly TimeSpan AggregateCacheTtl = TimeSpan.FromHours(6);

    public KpiPerformanceService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<KpiPerformanceService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<KpiTrendDto>> GetMonthlyAggregatesAsync(int months = 12, CancellationToken ct = default)
    {
        var cacheKey = $"{MonthlyAggregatesCacheKey}:{months}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        
        if (cached != null)
        {
            try
            {
                var cachedData = JsonSerializer.Deserialize<List<KpiTrendDto>>(cached);
                if (cachedData != null)
                {
                    _logger.LogDebug("Retrieved monthly aggregates from cache");
                    return cachedData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached monthly aggregates");
            }
        }

        var aggregates = await ComputeMonthlyAggregatesAsync(months, ct);
        
        // Cache the results
        var json = JsonSerializer.Serialize(aggregates);
        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AggregateCacheTtl
        }, ct);

        _logger.LogInformation("Computed and cached monthly aggregates for {Months} months", months);
        return aggregates;
    }

    public async Task<List<KpiTrendDto>> GetQuarterlyAggregatesAsync(int quarters = 4, CancellationToken ct = default)
    {
        var cacheKey = $"{QuarterlyAggregatesCacheKey}:{quarters}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        
        if (cached != null)
        {
            try
            {
                var cachedData = JsonSerializer.Deserialize<List<KpiTrendDto>>(cached);
                if (cachedData != null)
                {
                    _logger.LogDebug("Retrieved quarterly aggregates from cache");
                    return cachedData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached quarterly aggregates");
            }
        }

        var aggregates = await ComputeQuarterlyAggregatesAsync(quarters, ct);
        
        // Cache the results
        var json = JsonSerializer.Serialize(aggregates);
        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AggregateCacheTtl
        }, ct);

        _logger.LogInformation("Computed and cached quarterly aggregates for {Quarters} quarters", quarters);
        return aggregates;
    }

    public async Task RefreshAggregatesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing KPI aggregates cache");

        // Clear existing caches
        await _cache.RemoveAsync(MonthlyAggregatesCacheKey, ct);
        await _cache.RemoveAsync(QuarterlyAggregatesCacheKey, ct);

        // Warm up with common requests
        await GetMonthlyAggregatesAsync(12, ct);
        await GetQuarterlyAggregatesAsync(4, ct);

        _logger.LogInformation("KPI aggregates cache refreshed");
    }

    public async Task<KpiPerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var queriesExecuted = 0;
        var memoryBefore = GC.GetTotalMemory(false);

        // Simulate some KPI computations to measure performance
        var snapshots = await _context.KpiSnapshots
            .OrderByDescending(s => s.SnapshotDate)
            .Take(30)
            .ToListAsync(ct);
        queriesExecuted++;

        var clients = await _context.Clients.CountAsync(ct);
        queriesExecuted++;

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(false);

        // Calculate cache hit rate (simplified)
        var cacheHitRate = await EstimateCacheHitRateAsync(ct);

        return new KpiPerformanceMetricsDto
        {
            ComputationTime = stopwatch.Elapsed,
            CacheHitRate = cacheHitRate,
            QueriesExecuted = queriesExecuted,
            MemoryUsed = memoryAfter - memoryBefore
        };
    }

    public async Task WarmupCachesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Warming up KPI caches");

        var tasks = new List<Task>
        {
            GetMonthlyAggregatesAsync(6, ct),
            GetMonthlyAggregatesAsync(12, ct),
            GetQuarterlyAggregatesAsync(2, ct),
            GetQuarterlyAggregatesAsync(4, ct)
        };

        await Task.WhenAll(tasks);
        _logger.LogInformation("KPI caches warmed up");
    }

    private async Task<List<KpiTrendDto>> ComputeMonthlyAggregatesAsync(int months, CancellationToken ct)
    {
        var startDate = DateTime.UtcNow.Date.AddMonths(-months);
        
        var snapshots = await _context.KpiSnapshots
            .Where(s => s.SnapshotDate >= startDate)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);

        return snapshots
            .GroupBy(s => new { s.SnapshotDate.Year, s.SnapshotDate.Month })
            .Select(g => new KpiTrendDto
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                ClientComplianceRate = g.Average(s => s.ClientComplianceRate),
                TaxFilingTimeliness = g.Average(s => s.TaxFilingTimeliness),
                PaymentCompletionRate = g.Average(s => s.PaymentCompletionRate),
                DocumentSubmissionCompliance = g.Average(s => s.DocumentSubmissionCompliance),
                ClientEngagementRate = g.Average(s => s.ClientEngagementRate),
                OnTimePaymentPercentage = g.Average(s => s.OnTimePaymentPercentage),
                FilingTimelinessAverage = g.Average(s => s.FilingTimelinessAverage),
                DocumentReadinessRate = g.Average(s => s.DocumentReadinessRate),
                TotalClients = (int)g.Average(s => s.TotalClients)
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private async Task<List<KpiTrendDto>> ComputeQuarterlyAggregatesAsync(int quarters, CancellationToken ct)
    {
        var startDate = DateTime.UtcNow.Date.AddMonths(-quarters * 3);
        
        var snapshots = await _context.KpiSnapshots
            .Where(s => s.SnapshotDate >= startDate)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);

        return snapshots
            .GroupBy(s => new { s.SnapshotDate.Year, Quarter = (s.SnapshotDate.Month - 1) / 3 + 1 })
            .Select(g => new KpiTrendDto
            {
                Date = new DateTime(g.Key.Year, g.Key.Quarter * 3 - 2, 1),
                ClientComplianceRate = g.Average(s => s.ClientComplianceRate),
                TaxFilingTimeliness = g.Average(s => s.TaxFilingTimeliness),
                PaymentCompletionRate = g.Average(s => s.PaymentCompletionRate),
                DocumentSubmissionCompliance = g.Average(s => s.DocumentSubmissionCompliance),
                ClientEngagementRate = g.Average(s => s.ClientEngagementRate),
                OnTimePaymentPercentage = g.Average(s => s.OnTimePaymentPercentage),
                FilingTimelinessAverage = g.Average(s => s.FilingTimelinessAverage),
                DocumentReadinessRate = g.Average(s => s.DocumentReadinessRate),
                TotalClients = (int)g.Average(s => s.TotalClients)
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private async Task<int> EstimateCacheHitRateAsync(CancellationToken ct)
    {
        // Simplified cache hit rate estimation
        // In a real implementation, you'd track cache hits/misses
        var cacheKeys = new[]
        {
            $"{MonthlyAggregatesCacheKey}:6",
            $"{MonthlyAggregatesCacheKey}:12",
            $"{QuarterlyAggregatesCacheKey}:2",
            $"{QuarterlyAggregatesCacheKey}:4"
        };

        var hits = 0;
        foreach (var key in cacheKeys)
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached != null) hits++;
        }

        return (hits * 100) / cacheKeys.Length;
    }
}
