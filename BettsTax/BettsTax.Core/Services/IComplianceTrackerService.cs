using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Data; // For TaxType
using BettsTax.Shared;
// Disambiguate dashboard/trend DTOs
using ComplianceDashboardDtoAgg = BettsTax.Core.DTOs.ComplianceDashboardDto;
using ComplianceTrendDtoAgg = BettsTax.Core.DTOs.ComplianceTrendDto;

namespace BettsTax.Core.Services
{
    public interface IComplianceTrackerService
    {
        // Core compliance tracking
        Task<Result<ComplianceTrackerDto>> GetComplianceTrackerAsync(int clientId, int taxYearId, TaxType taxType);
        Task<Result<List<ComplianceTrackerDto>>> GetClientComplianceAsync(int clientId);
        Task<Result<List<ComplianceTrackerDto>>> GetComplianceTrackersAsync(ComplianceFilterDto filter);
        Task<Result<ComplianceTrackerDto>> UpdateComplianceStatusAsync(UpdateComplianceStatusDto updateDto);
        
        // Dashboard and analytics
        Task<Result<ComplianceDashboardDtoAgg>> GetComplianceDashboardAsync();
        Task<Result<ComplianceDashboardDtoAgg>> GetClientComplianceDashboardAsync(int clientId);

        // Alerts and actions
        Task<Result<List<ComplianceAlertDto>>> GetActiveAlertsAsync(int? clientId = null);
        Task<Result<List<ComplianceActionDto>>> GetPendingActionsAsync(int? clientId = null);
        Task<Result<ComplianceAlertDto>> CreateAlertAsync(CreateComplianceAlertDto createDto);
        Task<Result<ComplianceActionDto>> CreateActionAsync(CreateComplianceActionDto createDto);
        Task<Result<bool>> MarkAlertAsReadAsync(int alertId);
        Task<Result<bool>> CompleteActionAsync(int actionId, string? notes = null);
        
        // Penalty calculations
        Task<Result<PenaltyCalculationResultDto>> CalculatePenaltyAsync(CalculatePenaltyDto penaltyDto);
        Task<Result<List<CompliancePenaltyDto>>> GetClientPenaltiesAsync(int clientId);
        Task<Result<CompliancePenaltyDto>> ApplyPenaltyAsync(int complianceTrackerId, PenaltyCalculationResultDto penalty);
        Task<Result<bool>> WaivePenaltyAsync(int penaltyId, string reason);
        
        // Insights and recommendations
        Task<Result<List<ComplianceInsightDto>>> GetActiveInsightsAsync(int? clientId = null);
        Task<Result<ComplianceInsightDto>> GenerateInsightAsync(int complianceTrackerId);
        Task<Result<bool>> MarkInsightAsImplementedAsync(int insightId);
        
        // Automated compliance monitoring
        Task<Result<bool>> RunComplianceCheckAsync(int? clientId = null);
        Task<Result<bool>> ProcessOverdueComplianceAsync();
        Task<Result<bool>> GenerateComplianceAlertsAsync();
        Task<Result<bool>> UpdateComplianceHistoryAsync();
        
        // Reporting
        Task<Result<List<ComplianceTrendDtoAgg>>> GetComplianceTrendsAsync(DateTime fromDate, DateTime toDate);
        Task<Result<List<PenaltyTrendDto>>> GetPenaltyTrendsAsync(DateTime fromDate, DateTime toDate);
        Task<Result<List<RiskAnalysisDto>>> GetRiskAnalysisAsync();
    }
}