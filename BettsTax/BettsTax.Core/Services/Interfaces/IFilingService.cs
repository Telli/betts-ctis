using BettsTax.Core.DTOs.Filing;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Filing service interface
/// </summary>
public interface IFilingService
{
    Task<FilingDto?> GetFilingByIdAsync(int id);
    Task<List<ScheduleRowDto>> GetFilingSchedulesAsync(int filingId);
    Task<List<FilingDocumentDto>> GetFilingDocumentsAsync(int filingId);
    Task<List<FilingHistoryDto>> GetFilingHistoryAsync(int filingId);
    Task<FilingDto?> UpdateFilingAsync(int id, UpdateFilingDto dto);
    Task<FilingDto?> SubmitFilingAsync(int id);
}
