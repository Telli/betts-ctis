using BettsTax.Core.DTOs.Compliance;
using BettsTax.Data;
using BettsTax.Data.Models;
using DeadlinePriority = BettsTax.Data.DeadlinePriority;

namespace BettsTax.Core.Services.Interfaces;

public interface IComplianceEngine
{
    Task<ComplianceOverviewDto> GetComplianceOverviewAsync(int clientId);
    Task<ComplianceDashboardDto> GetComplianceDashboardAsync(int clientId);
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int daysAhead = 30);
    Task<List<ComplianceAlertDto>> GetActiveAlertsAsync(int? clientId = null);
    Task<ComplianceRiskAssessmentDto> CalculateRiskAssessmentAsync(int clientId);
    Task<decimal> CalculateComplianceScoreAsync(int clientId, DateTime? asOfDate = null);
    Task RefreshComplianceDataAsync(int? clientId = null);
    Task<List<ComplianceActionItemDto>> GetActionPlanAsync(int clientId);
    Task<ComplianceActionItemDto> CreateActionItemAsync(int clientId, ComplianceActionItemDto actionItem);
    Task<bool> UpdateActionItemAsync(int actionItemId, ComplianceActionItemDto actionItem);
    Task<bool> CompleteActionItemAsync(int actionItemId, string completionNotes, string completedBy);
}

public interface IPenaltyCalculationService
{
    Task<PenaltyCalculationDto> CalculatePenaltyAsync(int clientId, TaxType taxType, DateTime dueDate, DateTime? filingDate, decimal taxLiability);
    Task<List<PenaltyCalculationDto>> CalculateAllPenaltiesAsync(int clientId);
    Task<BettsTax.Data.Models.FinanceAct2025Rule> GetApplicableRuleAsync(TaxType taxType, DateTime dueDate);
    Task<List<BettsTax.Data.Models.FinanceAct2025Rule>> GetAllRulesAsync();
    Task<bool> UpdateRuleAsync(BettsTax.Data.Models.FinanceAct2025Rule rule);
    Task<decimal> CalculateInterestAsync(decimal principal, decimal annualRate, int daysLate);
    Task<bool> IsPenaltyWaivableAsync(PenaltyCalculationDto penalty);
    Task<string> GetWaiverConditionsAsync(PenaltyCalculationDto penalty);
}

public interface IDeadlineMonitoringService
{
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int daysAhead = 30);
    Task<List<UpcomingDeadlineDto>> GetOverdueItemsAsync(int? clientId = null);
    Task<bool> CreateDeadlineAlertAsync(int clientId, TaxType taxType, DateTime dueDate);
    Task<bool> UpdateDeadlineStatusAsync(int deadlineId, FilingStatus status);
    Task<List<UpcomingDeadlineDto>> GetDeadlinesByPriorityAsync(int clientId, DeadlinePriority priority);
    Task<bool> MarkDeadlineCompletedAsync(int deadlineId, DateTime completionDate);
    Task ScheduleAutomaticDeadlineAlertsAsync();
}

public interface IComplianceAlertService
{
    Task<List<ComplianceAlertDto>> GetActiveAlertsAsync(int? clientId = null);
    Task<ComplianceAlertDto> CreateAlertAsync(ComplianceAlertDto alert);
    Task<bool> ResolveAlertAsync(int alertId, string resolution, string resolvedBy);
    Task<bool> DismissAlertAsync(int alertId, string reason, string dismissedBy);
    Task<List<ComplianceAlertDto>> GetAlertHistoryAsync(int clientId, DateTime fromDate, DateTime toDate);
    Task<bool> EscalateAlertAsync(int alertId, ComplianceAlertSeverity newSeverity, string reason);
    Task ProcessAutomaticAlertsAsync();
}

public interface IFinanceAct2025Service
{
    Task<List<BettsTax.Data.Models.FinanceAct2025Rule>> GetActiveRulesAsync();
    Task<BettsTax.Data.Models.FinanceAct2025Rule?> GetRuleByTaxTypeAsync(TaxType taxType);
    Task<decimal> GetPenaltyRateAsync(TaxType taxType, int daysLate);
    Task<decimal> GetInterestRateAsync(TaxType taxType);
    Task<int> GetGracePeriodAsync(TaxType taxType);
    Task<decimal> GetMaxPenaltyPercentageAsync(TaxType taxType);
    Task<bool> IsRuleActiveAsync(string ruleId, DateTime effectiveDate);
    Task<List<string>> GetComplianceRequirementsAsync(TaxType taxType, TaxpayerCategory category);
}