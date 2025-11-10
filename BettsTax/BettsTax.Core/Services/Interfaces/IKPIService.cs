using BettsTax.Core.DTOs.KPI;

namespace BettsTax.Core.Services.Interfaces;

public interface IKPIService
{
    Task<InternalKPIDto> GetInternalKPIsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ClientKPIDto> GetClientKPIsAsync(int clientId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<KPIAlertDto>> GetKPIAlertsAsync(int? clientId = null);
    Task UpdateKPIThresholdsAsync(KPIThresholdDto thresholds);
    Task<bool> RefreshKPIDataAsync();
    Task<List<InternalKPIDto>> GetKPITrendsAsync(DateTime fromDate, DateTime toDate, string period = "Monthly");
    Task CreateKPIAlertAsync(KPIAlertDto alert, string? createdBy = null);
    Task MarkAlertAsReadAsync(int alertId, string resolvedBy);
}

public class KPIThresholdDto
{
    public decimal MinComplianceRate { get; set; } = 70m;
    public double MaxFilingDelayDays { get; set; } = 7;
    public decimal MinPaymentCompletionRate { get; set; } = 85m;
    public decimal MinDocumentCompletionRate { get; set; } = 90m;
    public decimal MinEngagementRate { get; set; } = 60m;
}