using BettsTax.Core.DTOs.Demo;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Provides read-only demo data used to support the frontend experience.
/// Replace with real data sources when integrating with production services.
/// </summary>
public interface IDemoDataService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? clientId, int upcomingDays);
    Task<IReadOnlyList<ClientSummaryDto>> GetClientsAsync();
    Task<IReadOnlyList<DocumentRecordDto>> GetDocumentsAsync();
    Task<PaymentsResponseDto> GetPaymentsAsync(int? clientId);
    Task<KpiSummaryDto> GetKpiSummaryAsync(int? clientId);
    Task<IReadOnlyList<ReportTypeDto>> GetReportTypesAsync();
    Task<ReportFiltersDto> GetReportFiltersAsync();
    Task<IReadOnlyList<ChatConversationDto>> GetChatConversationsAsync();
    Task<IReadOnlyList<ChatMessageDto>> GetChatMessagesAsync(int conversationId);
    Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync();
    Task<IReadOnlyList<AuditLogEntryDto>> GetAuditLogsAsync();
    Task<IReadOnlyList<TaxRateDto>> GetTaxRatesAsync();
    Task<IReadOnlyList<JobStatusDto>> GetJobStatusesAsync();
    Task<FilingWorkspaceDto?> GetActiveFilingAsync();
}
