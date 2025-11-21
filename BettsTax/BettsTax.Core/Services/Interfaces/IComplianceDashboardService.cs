using BettsTax.Core.DTOs.Compliance;

namespace BettsTax.Core.Services.Interfaces;

public interface IComplianceDashboardService
{
    Task<ComplianceOverviewSummaryDto> GetOverviewAsync();
    Task<IReadOnlyList<ComplianceDashboardItemDto>> GetItemsAsync(ComplianceDashboardFilterDto filters);
    Task<IReadOnlyList<ComplianceTaxTypeBreakdownDto>> GetTaxTypeBreakdownAsync();
    Task<IReadOnlyList<FilingChecklistMatrixRowDto>> GetFilingChecklistMatrixAsync(int? year = null);
    Task<IReadOnlyList<PenaltyWarningSummaryDto>> GetPenaltyWarningsAsync(int top = 5);
    Task<IReadOnlyList<DocumentRequirementProgressDto>> GetDocumentRequirementsAsync();
    Task<IReadOnlyList<ComplianceTimelineEventDto>> GetTimelineAsync(int top = 5);
}
