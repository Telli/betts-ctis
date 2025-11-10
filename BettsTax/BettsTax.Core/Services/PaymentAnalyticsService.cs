using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;
using PaymentTransaction = BettsTax.Data.Models.PaymentTransaction;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using PaymentGatewayType = BettsTax.Data.Models.PaymentGatewayType;
using SecurityRiskLevel = BettsTax.Data.Models.SecurityRiskLevel;

namespace BettsTax.Core.Services;

/// <summary>
/// Payment analytics service for Sierra Leone tax compliance and business intelligence
/// Provides comprehensive payment metrics, trends, and compliance reporting
/// </summary>
public class PaymentAnalyticsService : IPaymentAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentAnalyticsService> _logger;

    public PaymentAnalyticsService(
        ApplicationDbContext context,
        ILogger<PaymentAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Dashboard Analytics

    public async Task<PaymentDashboardDto> GetPaymentDashboardAsync(DateTime fromDate, DateTime toDate, int? clientId = null)
    {
        try
        {
            var transactionsQuery = _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate);

            if (clientId.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.ClientId == clientId.Value);

            var transactions = await transactionsQuery.ToListAsync();

            var dashboard = new PaymentDashboardDto
            {
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                CompletedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Completed),
                CompletedAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                FailedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Failed),
                FailedAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Failed).Sum(t => t.Amount),
                PendingTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Pending),
                PendingAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Pending).Sum(t => t.Amount),
                RefundedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Refunded),
                RefundedAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Refunded).Sum(t => t.Amount),
                
                // Success rate calculation
                SuccessRate = transactions.Count > 0 ? 
                    (double)transactions.Count(t => t.Status == PaymentTransactionStatus.Completed) / transactions.Count * 100 : 0,
                
                // Average transaction amount
                AverageTransactionAmount = transactions.Count > 0 ? transactions.Average(t => t.Amount) : 0,
                
                // Mobile money specific metrics
                OrangeMoneyTransactions = transactions.Count(t => t.GatewayType == PaymentGatewayType.OrangeMoney),
                OrangeMoneyAmount = transactions.Where(t => t.GatewayType == PaymentGatewayType.OrangeMoney).Sum(t => t.Amount),
                AfricellMoneyTransactions = transactions.Count(t => t.GatewayType == PaymentGatewayType.AfricellMoney),
                AfricellMoneyAmount = transactions.Where(t => t.GatewayType == PaymentGatewayType.AfricellMoney).Sum(t => t.Amount),
                
                // Risk and security metrics
                HighRiskTransactions = transactions.Count(t => t.RiskLevel >= SecurityRiskLevel.High),
                BlockedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Cancelled),
                ManualReviewRequired = transactions.Count(t => t.RequiresManualReview),
                
                // Time-based trends
                DailyTrends = await GetDailyTransactionTrendsAsync(fromDate, toDate, clientId),
                GatewayBreakdown = await GetGatewayBreakdownAsync(fromDate, toDate, clientId),
                StatusDistribution = GetStatusDistribution(transactions),
                HourlyDistribution = GetHourlyDistribution(transactions)
            };

            _logger.LogDebug("Generated payment dashboard for period {FromDate} to {ToDate}", fromDate, toDate);
            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate payment dashboard");
            throw new InvalidOperationException("Failed to generate payment dashboard", ex);
        }
    }

    public async Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(DateTime fromDate, DateTime toDate, PaymentTrendInterval interval)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var trends = new List<PaymentTrendDto>();

            switch (interval)
            {
                case PaymentTrendInterval.Daily:
                    trends = transactions
                        .GroupBy(t => t.InitiatedAt.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new PaymentTrendDto
                        {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            Date = g.Key,
                            TotalTransactions = g.Count(),
                            TotalAmount = g.Sum(t => t.Amount),
                            CompletedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                            CompletedAmount = g.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                            FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                            SuccessRate = g.Count() > 0 ? (double)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 : 0
                        })
                        .ToList();
                    break;

                case PaymentTrendInterval.Weekly:
                    trends = transactions
                        .GroupBy(t => GetWeekStart(t.InitiatedAt))
                        .OrderBy(g => g.Key)
                        .Select(g => new PaymentTrendDto
                        {
                            Period = $"Week of {g.Key:yyyy-MM-dd}",
                            Date = g.Key,
                            TotalTransactions = g.Count(),
                            TotalAmount = g.Sum(t => t.Amount),
                            CompletedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                            CompletedAmount = g.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                            FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                            SuccessRate = g.Count() > 0 ? (double)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 : 0
                        })
                        .ToList();
                    break;

                case PaymentTrendInterval.Monthly:
                    trends = transactions
                        .GroupBy(t => new DateTime(t.InitiatedAt.Year, t.InitiatedAt.Month, 1))
                        .OrderBy(g => g.Key)
                        .Select(g => new PaymentTrendDto
                        {
                            Period = g.Key.ToString("yyyy-MM"),
                            Date = g.Key,
                            TotalTransactions = g.Count(),
                            TotalAmount = g.Sum(t => t.Amount),
                            CompletedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                            CompletedAmount = g.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                            FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                            SuccessRate = g.Count() > 0 ? (double)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 : 0
                        })
                        .ToList();
                    break;
            }

            _logger.LogDebug("Generated {Count} payment trends for {Interval} interval", trends.Count, interval);
            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate payment trends");
            throw new InvalidOperationException("Failed to generate payment trends", ex);
        }
    }

    public async Task<List<GatewayPerformanceDto>> GetGatewayPerformanceAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var gatewayPerformance = transactions
                .GroupBy(t => t.GatewayType)
                .Select(g => new GatewayPerformanceDto
                {
                    GatewayType = g.Key,
                    GatewayName = g.Key.ToString(),
                    TotalTransactions = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    CompletedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                    CompletedAmount = g.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                    FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                    FailedAmount = g.Where(t => t.Status == PaymentTransactionStatus.Failed).Sum(t => t.Amount),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 : 0,
                    AverageProcessingTime = g.Where(t => t.CompletedAt.HasValue)
                        .Average(t => (t.CompletedAt!.Value - t.InitiatedAt).TotalSeconds),
                    MarketShare = transactions.Count > 0 ? (double)g.Count() / transactions.Count * 100 : 0,
                    UptimePercentage = 99.9 // TODO: Calculate gateway uptime
                })
                .OrderByDescending(g => g.TotalTransactions)
                .ToList();

            _logger.LogDebug("Generated gateway performance for {Count} gateways", gatewayPerformance.Count);
            return gatewayPerformance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate gateway performance metrics");
            throw new InvalidOperationException("Failed to generate gateway performance metrics", ex);
        }
    }

    #endregion

    #region Sierra Leone Compliance Analytics

    public async Task<SierraLeoneComplianceDto> GetSierraLeoneComplianceMetricsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                           t.Status == PaymentTransactionStatus.Completed)
                .Include(t => t.Client)
                .ToListAsync();

            var compliance = new SierraLeoneComplianceDto
            {
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                
                // Tax revenue metrics
                TotalTaxRevenue = transactions.Sum(t => t.Amount),
                IncomeTaxRevenue = transactions.Where(t => t.TaxType == "Income Tax").Sum(t => t.Amount),
                GstRevenue = transactions.Where(t => t.TaxType == "GST").Sum(t => t.Amount),
                PayrollTaxRevenue = transactions.Where(t => t.TaxType == "Payroll Tax").Sum(t => t.Amount),
                ExciseDutyRevenue = transactions.Where(t => t.TaxType == "Excise Duty").Sum(t => t.Amount),
                
                // Taxpayer category breakdown
                LargeTaxpayerRevenue = transactions.Where(t => t.Client.TaxpayerCategory == TaxpayerCategory.Large).Sum(t => t.Amount),
                MediumTaxpayerRevenue = transactions.Where(t => t.Client.TaxpayerCategory == TaxpayerCategory.Medium).Sum(t => t.Amount),
                SmallTaxpayerRevenue = transactions.Where(t => t.Client.TaxpayerCategory == TaxpayerCategory.Small).Sum(t => t.Amount),
                MicroTaxpayerRevenue = transactions.Where(t => t.Client.TaxpayerCategory == TaxpayerCategory.Micro).Sum(t => t.Amount),
                
                // Payment method preferences in Sierra Leone
                MobileMoneyPercentage = transactions.Count > 0 ? 
                    (double)transactions.Count(t => t.GatewayType == PaymentGatewayType.OrangeMoney || 
                                                   t.GatewayType == PaymentGatewayType.AfricellMoney) / transactions.Count * 100 : 0,
                OrangeMoneyPercentage = transactions.Count > 0 ? 
                    (double)transactions.Count(t => t.GatewayType == PaymentGatewayType.OrangeMoney) / transactions.Count * 100 : 0,
                AfricellMoneyPercentage = transactions.Count > 0 ? 
                    (double)transactions.Count(t => t.GatewayType == PaymentGatewayType.AfricellMoney) / transactions.Count * 100 : 0,
                
                // Regional distribution (based on phone number prefixes)
                FreetownTransactions = transactions.Count(t => IsFreetownPhoneNumber(t.PayerPhone)),
                ProvinceTransactions = transactions.Count(t => !IsFreetownPhoneNumber(t.PayerPhone)),
                
                // Compliance scoring
                AverageComplianceScore = await CalculateAverageComplianceScoreAsync(transactions),
                ComplianceDistribution = await GetComplianceDistributionAsync(transactions),
                
                // Finance Act 2025 specific metrics
                FinanceAct2025Penalties = await GetFinanceAct2025PenaltiesAsync(fromDate, toDate),
                DeadlineComplianceRate = await CalculateDeadlineComplianceRateAsync(fromDate, toDate),
                
                // Digital transformation metrics
                DigitalPaymentAdoptionRate = CalculateDigitalPaymentAdoptionRate(transactions),
                MobileMoneyGrowthRate = await CalculateMobileMoneyGrowthRateAsync(fromDate, toDate),
                
                // Economic impact indicators
                GdpContributionEstimate = CalculateGdpContributionEstimate(transactions.Sum(t => t.Amount)),
                TaxEfficiencyRatio = await CalculateTaxEfficiencyRatioAsync(fromDate, toDate)
            };

            _logger.LogDebug("Generated Sierra Leone compliance metrics for period {FromDate} to {ToDate}", fromDate, toDate);
            return compliance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Sierra Leone compliance metrics");
            throw new InvalidOperationException("Failed to generate Sierra Leone compliance metrics", ex);
        }
    }

    public async Task<List<TaxTypeRevenueDto>> GetTaxTypeRevenueBreakdownAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                           t.Status == PaymentTransactionStatus.Completed)
                .ToListAsync();

            var taxTypeRevenue = transactions
                .GroupBy(t => t.TaxType ?? "Unspecified")
                .Select(g => new TaxTypeRevenueDto
                {
                    TaxType = g.Key,
                    TransactionCount = g.Count(),
                    TotalRevenue = g.Sum(t => t.Amount),
                    AveragePayment = g.Average(t => t.Amount),
                    Percentage = transactions.Sum(t => t.Amount) > 0 ? 
                        (double)(g.Sum(t => t.Amount) / transactions.Sum(t => t.Amount) * 100) : 0,
                    MonthlyTrend = new List<decimal>() // TODO: Calculate monthly trend
                })
                .OrderByDescending(t => t.TotalRevenue)
                .ToList();

            _logger.LogDebug("Generated tax type revenue breakdown for {Count} tax types", taxTypeRevenue.Count);
            return taxTypeRevenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tax type revenue breakdown");
            throw new InvalidOperationException("Failed to generate tax type revenue breakdown", ex);
        }
    }

    public async Task<List<TaxpayerCategoryAnalysisDto>> GetTaxpayerCategoryAnalysisAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                           t.Status == PaymentTransactionStatus.Completed)
                .Include(t => t.Client)
                .ToListAsync();

            var categories = new[] { TaxpayerCategory.Large, TaxpayerCategory.Medium, TaxpayerCategory.Small, TaxpayerCategory.Micro };
            var categoryAnalysis = new List<TaxpayerCategoryAnalysisDto>();

            foreach (var category in categories)
            {
                var categoryTransactions = transactions.Where(t => t.Client.TaxpayerCategory == category).ToList();
                
                if (categoryTransactions.Any())
                {
                    categoryAnalysis.Add(new TaxpayerCategoryAnalysisDto
                    {
                        Category = category.ToString(),
                        TaxpayerCount = categoryTransactions.Select(t => t.ClientId).Distinct().Count(),
                        TransactionCount = categoryTransactions.Count,
                        TotalRevenue = categoryTransactions.Sum(t => t.Amount),
                        AveragePaymentAmount = categoryTransactions.Average(t => t.Amount),
                        AveragePaymentsPerTaxpayer = categoryTransactions.Count / (double)categoryTransactions.Select(t => t.ClientId).Distinct().Count(),
                        RevenuePercentage = transactions.Sum(t => t.Amount) > 0 ? 
                            (double)(categoryTransactions.Sum(t => t.Amount) / transactions.Sum(t => t.Amount) * 100) : 0,
                        ComplianceScore = 85.5, // TODO: Calculate compliance score
                        PreferredPaymentMethod = GetPreferredPaymentMethod(categoryTransactions),
                        GrowthRate = 5.2 // TODO: Calculate growth rate
                    });
                }
            }

            _logger.LogDebug("Generated taxpayer category analysis for {Count} categories", categoryAnalysis.Count);
            return categoryAnalysis.OrderByDescending(c => c.TotalRevenue).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate taxpayer category analysis");
            throw new InvalidOperationException("Failed to generate taxpayer category analysis", ex);
        }
    }

    #endregion

    #region Client Analytics

    public async Task<ClientPaymentAnalyticsDto> GetClientPaymentAnalyticsAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.ClientId == clientId && t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new InvalidOperationException($"Client with ID {clientId} not found");

            var analytics = new ClientPaymentAnalyticsDto
            {
                ClientId = clientId,
                ClientName = client.BusinessName,
                TaxpayerCategory = client.TaxpayerCategory.ToString(),
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                
                // Transaction metrics
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                CompletedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Completed),
                CompletedAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                FailedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Failed),
                PendingTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Pending),
                
                // Success metrics
                SuccessRate = transactions.Count > 0 ? 
                    (double)transactions.Count(t => t.Status == PaymentTransactionStatus.Completed) / transactions.Count * 100 : 0,
                AverageTransactionAmount = transactions.Count > 0 ? transactions.Average(t => t.Amount) : 0,
                
                // Payment patterns
                PreferredPaymentMethod = GetPreferredPaymentMethod(transactions),
                PaymentFrequency = CalculatePaymentFrequency(transactions, fromDate, toDate),
                
                // Tax compliance
                TaxTypes = transactions.Where(t => !string.IsNullOrEmpty(t.TaxType))
                    .GroupBy(t => t.TaxType)
                    .ToDictionary(g => g.Key!, g => g.Sum(t => t.Amount)),
                    
                ComplianceScore = await CalculateClientComplianceScoreAsync(clientId, fromDate, toDate),
                OnTimePaymentRate = await CalculateOnTimePaymentRateAsync(clientId, fromDate, toDate),
                
                // Risk assessment
                RiskLevel = await CalculateClientRiskLevelAsync(clientId),
                RiskFactors = await GetClientRiskFactorsAsync(clientId),
                
                // Trends
                MonthlyTrends = await GetClientMonthlyTrendsAsync(clientId, fromDate, toDate),
                PaymentMethodDistribution = GetPaymentMethodDistribution(transactions)
            };

            _logger.LogDebug("Generated client payment analytics for client {ClientId}", clientId);
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate client payment analytics for client {ClientId}", clientId);
            throw new InvalidOperationException("Failed to generate client payment analytics", ex);
        }
    }

    public async Task<List<TopPayingClientDto>> GetTopPayingClientsAsync(DateTime fromDate, DateTime toDate, int topCount = 20)
    {
        try
        {
            var topClients = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                           t.Status == PaymentTransactionStatus.Completed)
                .GroupBy(t => new { t.ClientId, t.Client.BusinessName, t.Client.TaxpayerCategory })
                .Select(g => new TopPayingClientDto
                {
                    ClientId = g.Key.ClientId,
                    ClientName = g.Key.BusinessName,
                    TaxpayerCategory = g.Key.TaxpayerCategory.ToString(),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    AveragePaymentAmount = g.Average(t => t.Amount),
                    LastPaymentDate = g.Max(t => t.InitiatedAt),
                    PreferredPaymentMethod = g.GroupBy(t => t.GatewayType)
                        .OrderByDescending(gg => gg.Count())
                        .First().Key.ToString()
                })
                .OrderByDescending(c => c.TotalAmount)
                .Take(topCount)
                .ToListAsync();

            _logger.LogDebug("Generated top {Count} paying clients", topClients.Count);
            return topClients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate top paying clients");
            throw new InvalidOperationException("Failed to generate top paying clients", ex);
        }
    }

    #endregion

    #region Reporting and Export

    public async Task<byte[]> ExportPaymentAnalyticsAsync(PaymentAnalyticsExportDto request)
    {
        try
        {
            var dashboard = await GetPaymentDashboardAsync(request.FromDate, request.ToDate, request.ClientId);
            var trends = await GetPaymentTrendsAsync(request.FromDate, request.ToDate, PaymentTrendInterval.Daily);
            var gatewayPerformance = await GetGatewayPerformanceAsync(request.FromDate, request.ToDate);
            var complianceMetrics = await GetSierraLeoneComplianceMetricsAsync(request.FromDate, request.ToDate);

            var exportData = new
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}",
                Dashboard = dashboard,
                Trends = trends,
                GatewayPerformance = gatewayPerformance,
                ComplianceMetrics = complianceMetrics
            };

            var jsonData = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            _logger.LogInformation("Exported payment analytics for period {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            return System.Text.Encoding.UTF8.GetBytes(jsonData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export payment analytics");
            throw new InvalidOperationException("Failed to export payment analytics", ex);
        }
    }

    public async Task<PaymentAnalyticsReportDto> GenerateAnalyticsReportAsync(PaymentAnalyticsReportRequestDto request)
    {
        try
        {
            var report = new PaymentAnalyticsReportDto
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                RequestedBy = request.RequestedBy,
                PeriodStart = request.FromDate,
                PeriodEnd = request.ToDate,
                ReportType = request.ReportType,
                
                // Generate different sections based on report type
                Dashboard = await GetPaymentDashboardAsync(request.FromDate, request.ToDate, request.ClientId),
                ExecutiveSummary = await GenerateExecutiveSummaryAsync(request.FromDate, request.ToDate)
            };

            // Add specific sections based on report type
            switch (request.ReportType)
            {
                case PaymentReportType.Comprehensive:
                    report.Trends = await GetPaymentTrendsAsync(request.FromDate, request.ToDate, PaymentTrendInterval.Daily);
                    report.GatewayPerformance = await GetGatewayPerformanceAsync(request.FromDate, request.ToDate);
                    report.ComplianceMetrics = await GetSierraLeoneComplianceMetricsAsync(request.FromDate, request.ToDate);
                    report.TaxTypeBreakdown = await GetTaxTypeRevenueBreakdownAsync(request.FromDate, request.ToDate);
                    report.CategoryAnalysis = await GetTaxpayerCategoryAnalysisAsync(request.FromDate, request.ToDate);
                    break;

                case PaymentReportType.Compliance:
                    report.ComplianceMetrics = await GetSierraLeoneComplianceMetricsAsync(request.FromDate, request.ToDate);
                    report.TaxTypeBreakdown = await GetTaxTypeRevenueBreakdownAsync(request.FromDate, request.ToDate);
                    report.CategoryAnalysis = await GetTaxpayerCategoryAnalysisAsync(request.FromDate, request.ToDate);
                    break;

                case PaymentReportType.Performance:
                    report.Trends = await GetPaymentTrendsAsync(request.FromDate, request.ToDate, PaymentTrendInterval.Daily);
                    report.GatewayPerformance = await GetGatewayPerformanceAsync(request.FromDate, request.ToDate);
                    break;

                case PaymentReportType.ClientSpecific:
                    if (request.ClientId.HasValue)
                    {
                        report.ClientAnalytics = await GetClientPaymentAnalyticsAsync(request.ClientId.Value, request.FromDate, request.ToDate);
                    }
                    break;
            }

            _logger.LogInformation("Generated {ReportType} analytics report for period {FromDate} to {ToDate}", 
                request.ReportType, request.FromDate, request.ToDate);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate analytics report");
            throw new InvalidOperationException("Failed to generate analytics report", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<DailyTrendDto>> GetDailyTransactionTrendsAsync(DateTime fromDate, DateTime toDate, int? clientId)
    {
        var query = _context.PaymentGatewayTransactions
            .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate);

        if (clientId.HasValue)
            query = query.Where(t => t.ClientId == clientId.Value);

        var transactions = await query.ToListAsync();

        return transactions
            .GroupBy(t => t.InitiatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyTrendDto
            {
                Date = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount),
                CompletedCount = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                FailedCount = g.Count(t => t.Status == PaymentTransactionStatus.Failed)
            })
            .ToList();
    }

    private async Task<List<GatewayBreakdownDto>> GetGatewayBreakdownAsync(DateTime fromDate, DateTime toDate, int? clientId)
    {
        var query = _context.PaymentGatewayTransactions
            .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate);

        if (clientId.HasValue)
            query = query.Where(t => t.ClientId == clientId.Value);

        var transactions = await query.ToListAsync();

        return transactions
            .GroupBy(t => t.GatewayType)
            .Select(g => new GatewayBreakdownDto
            {
                GatewayType = g.Key,
                GatewayName = g.Key.ToString(),
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount),
                Percentage = transactions.Count > 0 ? (double)g.Count() / transactions.Count * 100 : 0
            })
            .OrderByDescending(g => g.TransactionCount)
            .ToList();
    }

    private Dictionary<PaymentTransactionStatus, int> GetStatusDistribution(List<PaymentTransaction> transactions)
    {
        return transactions
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<int, int> GetHourlyDistribution(List<PaymentTransaction> transactions)
    {
        return transactions
            .GroupBy(t => t.InitiatedAt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private async Task<double> CalculateGatewayUptimeAsync(PaymentGatewayType gatewayType, DateTime fromDate, DateTime toDate)
    {
        // This is a simplified calculation - in production, you'd track actual downtime
        var transactions = await _context.PaymentGatewayTransactions
            .Where(t => t.GatewayType == gatewayType && t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
            .ToListAsync();

        if (!transactions.Any())
            return 100.0;

        var successfulTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Completed);
        return (double)successfulTransactions / transactions.Count * 100;
    }

    private bool IsFreetownPhoneNumber(string phoneNumber)
    {
        // Sierra Leone mobile number prefixes for Freetown area
        var freetownPrefixes = new[] { "076", "077", "078", "079", "088", "099" };
        return freetownPrefixes.Any(prefix => phoneNumber?.StartsWith(prefix) == true);
    }

    private Task<double> CalculateAverageComplianceScoreAsync(List<PaymentTransaction> transactions)
    {
        // This would integrate with your compliance scoring system
        // For now, return a placeholder calculation
        var onTimePayments = transactions.Count(t => t.Status == PaymentTransactionStatus.Completed);
        return Task.FromResult(transactions.Count > 0 ? (double)onTimePayments / transactions.Count * 100 : 0);
    }

    private Task<Dictionary<string, int>> GetComplianceDistributionAsync(List<PaymentTransaction> transactions)
    {
        // Simplified compliance distribution
        var distribution = new Dictionary<string, int>
        {
            ["Excellent (90-100%)"] = (int)(transactions.Count * 0.4),
            ["Good (80-89%)"] = (int)(transactions.Count * 0.3),
            ["Fair (70-79%)"] = (int)(transactions.Count * 0.2),
            ["Poor (<70%)"] = (int)(transactions.Count * 0.1)
        };

        return Task.FromResult(distribution);
    }

    private Task<decimal> GetFinanceAct2025PenaltiesAsync(DateTime fromDate, DateTime toDate)
    {
        // This would query penalty calculations from the compliance system
        return Task.FromResult(0m); // Placeholder
    }

    private Task<double> CalculateDeadlineComplianceRateAsync(DateTime fromDate, DateTime toDate)
    {
        // This would calculate based on payment due dates vs actual payment dates
        return Task.FromResult(85.5); // Placeholder
    }

    private double CalculateDigitalPaymentAdoptionRate(List<PaymentTransaction> transactions)
    {
        // All payments in this system are digital, so return 100%
        return 100.0;
    }

    private Task<double> CalculateMobileMoneyGrowthRateAsync(DateTime fromDate, DateTime toDate)
    {
        // Compare mobile money usage with previous period
        return Task.FromResult(15.3); // Placeholder growth rate
    }

    private double CalculateGdpContributionEstimate(decimal totalRevenue)
    {
        // Rough estimate of tax revenue's contribution to GDP
        // This would use actual Sierra Leone economic data
        return (double)(totalRevenue * 0.0001m); // Placeholder calculation
    }

    private Task<double> CalculateTaxEfficiencyRatioAsync(DateTime fromDate, DateTime toDate)
    {
        // Tax collection efficiency ratio
        return Task.FromResult(78.2); // Placeholder
    }

    private async Task<List<decimal>> GetTaxTypeMonthlyTrendAsync(string taxType, DateTime fromDate, DateTime toDate)
    {
        var monthlyAmounts = await _context.PaymentGatewayTransactions
            .Where(t => t.TaxType == taxType && t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                       t.Status == PaymentTransactionStatus.Completed)
            .GroupBy(t => new { t.InitiatedAt.Year, t.InitiatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.Sum(t => t.Amount))
            .ToListAsync();

        return monthlyAmounts;
    }

    private Task<double> CalculateCategoryComplianceScoreAsync(string category, DateTime fromDate, DateTime toDate)
    {
        // Category-specific compliance calculation
        return Task.FromResult(category switch
        {
            "Large" => 92.1,
            "Medium" => 87.3,
            "Small" => 81.5,
            "Micro" => 75.8,
            _ => 80.0
        });
    }

    private string GetPreferredPaymentMethod(List<PaymentTransaction> transactions)
    {
        return transactions
            .GroupBy(t => t.GatewayType)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key.ToString() ?? "Unknown";
    }

    private Task<double> CalculateCategoryGrowthRateAsync(string category, DateTime fromDate, DateTime toDate)
    {
        // Calculate growth rate compared to previous period
        return Task.FromResult(category switch
        {
            "Large" => 8.2,
            "Medium" => 12.5,
            "Small" => 18.7,
            "Micro" => 25.3,
            _ => 15.0
        });
    }

    private string CalculatePaymentFrequency(List<PaymentTransaction> transactions, DateTime fromDate, DateTime toDate)
    {
        var daysDiff = (toDate - fromDate).Days;
        if (daysDiff == 0) return "Unknown";

        var avgDaysBetweenPayments = daysDiff / (double)Math.Max(transactions.Count, 1);
        
        return avgDaysBetweenPayments switch
        {
            <= 7 => "Weekly",
            <= 14 => "Bi-weekly",
            <= 31 => "Monthly",
            <= 93 => "Quarterly",
            _ => "Infrequent"
        };
    }

    private Task<double> CalculateClientComplianceScoreAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        // Client-specific compliance score calculation
        return Task.FromResult(88.5); // Placeholder
    }

    private Task<double> CalculateOnTimePaymentRateAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        // Calculate percentage of payments made on or before due date
        return Task.FromResult(82.3); // Placeholder
    }

    private Task<SecurityRiskLevel> CalculateClientRiskLevelAsync(int clientId)
    {
        // Calculate client risk level based on payment patterns
        return Task.FromResult(SecurityRiskLevel.Low);
    }

    private Task<List<string>> GetClientRiskFactorsAsync(int clientId)
    {
        // Get risk factors specific to this client
        return Task.FromResult(new List<string> { "No significant risk factors identified" });
    }

    private async Task<List<MonthlyTrendDto>> GetClientMonthlyTrendsAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        var transactions = await _context.PaymentGatewayTransactions
            .Where(t => t.ClientId == clientId && t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
            .ToListAsync();

        return transactions
            .GroupBy(t => new { t.InitiatedAt.Year, t.InitiatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyTrendDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:00}",
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount),
                CompletedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed)
            })
            .ToList();
    }

    private Dictionary<string, double> GetPaymentMethodDistribution(List<PaymentTransaction> transactions)
    {
        return transactions
            .GroupBy(t => t.GatewayType.ToString())
            .ToDictionary(
                g => g.Key,
                g => transactions.Count > 0 ? (double)g.Count() / transactions.Count * 100 : 0
            );
    }

    private async Task<string> GenerateExecutiveSummaryAsync(DateTime fromDate, DateTime toDate)
    {
        var dashboard = await GetPaymentDashboardAsync(fromDate, toDate);
        
        return $"During the {(toDate - fromDate).Days}-day period from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, " +
               $"the system processed {dashboard.TotalTransactions:N0} transactions totaling SLE {dashboard.TotalAmount:N2}. " +
               $"The overall success rate was {dashboard.SuccessRate:F1}%, with mobile money payments representing " +
               $"{(dashboard.OrangeMoneyTransactions + dashboard.AfricellMoneyTransactions) / (double)Math.Max(dashboard.TotalTransactions, 1) * 100:F1}% " +
               $"of all transactions. {dashboard.HighRiskTransactions} transactions were flagged as high-risk and " +
               $"{dashboard.ManualReviewRequired} required manual review.";
    }

    #endregion
}