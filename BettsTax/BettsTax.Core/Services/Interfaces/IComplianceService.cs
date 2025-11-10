using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.DTOs;
using BettsTax.Data;

public interface IComplianceService
{
    // Existing methods (maintained for compatibility)
    Task<ComplianceStatusSummaryDto> GetClientComplianceSummaryAsync(int clientId);
    Task<List<FilingChecklistItemDto>> GetFilingChecklistAsync(int clientId, TaxType? taxType = null);
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int clientId, int daysAhead = 30);
    Task<List<PenaltyWarningDto>> GetPenaltyWarningsAsync(int clientId);
    Task<DocumentTrackerDto> GetDocumentTrackerAsync(int clientId);
    Task<DeadlineAdherenceHistoryDto> GetDeadlineAdherenceHistoryAsync(int clientId, int months = 12);
    Task<List<ComplianceAlertDto>> GetComplianceAlertsAsync(int? clientId = null);
    Task<ComplianceMetricsDto> GetComplianceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    // New enhanced endpoints
    Task<ComplianceStatusSummaryDto> GetStatusSummaryAsync(int clientId);
    Task<FilingChecklistDto> GetFilingChecklistDetailedAsync(int clientId, TaxType taxType);
    Task<UpcomingDeadlinesDto> GetUpcomingDeadlinesDetailedAsync(int clientId, int daysAhead = 30);
    Task<List<PenaltyWarningDto>> GetPenaltyWarningsDetailedAsync(int clientId);
    Task<DocumentTrackerDto> GetDocumentTrackerDetailedAsync(int clientId);
    Task<DeadlineAdherenceHistoryDto> GetDeadlineAdherenceHistoryDetailedAsync(int clientId, int months = 12);
    Task<PenaltySimulationResultDto> SimulatePenaltyAsync(PenaltySimulationRequestDto request);
    Task CreateComplianceSnapshotAsync(int? clientId = null);
}
