using BettsTax.Core.DTOs.Payment;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Interface for payment analytics service
/// Provides comprehensive payment metrics, trends, and compliance reporting
/// </summary>
public interface IPaymentAnalyticsService
{
    // Dashboard Analytics
    Task<PaymentDashboardDto> GetPaymentDashboardAsync(DateTime fromDate, DateTime toDate, int? clientId = null);
    Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(DateTime fromDate, DateTime toDate, PaymentTrendInterval interval);
    Task<List<GatewayPerformanceDto>> GetGatewayPerformanceAsync(DateTime fromDate, DateTime toDate);

    // Sierra Leone Compliance Analytics
    Task<SierraLeoneComplianceDto> GetSierraLeoneComplianceMetricsAsync(DateTime fromDate, DateTime toDate);
    Task<List<TaxTypeRevenueDto>> GetTaxTypeRevenueBreakdownAsync(DateTime fromDate, DateTime toDate);
    Task<List<TaxpayerCategoryAnalysisDto>> GetTaxpayerCategoryAnalysisAsync(DateTime fromDate, DateTime toDate);

    // Client Analytics
    Task<ClientPaymentAnalyticsDto> GetClientPaymentAnalyticsAsync(int clientId, DateTime fromDate, DateTime toDate);
    Task<List<TopPayingClientDto>> GetTopPayingClientsAsync(DateTime fromDate, DateTime toDate, int topCount = 20);

    // Reporting and Export
    Task<byte[]> ExportPaymentAnalyticsAsync(PaymentAnalyticsExportDto request);
    Task<PaymentAnalyticsReportDto> GenerateAnalyticsReportAsync(PaymentAnalyticsReportRequestDto request);
}