using BettsTax.Data;

namespace BettsTax.Core.DTOs.Reports;

public class ReportHistoryFilter
{
    public ReportStatus? Status { get; set; }
    public ReportType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; } // requestedAt, completedAt, status, type, fileSize
    public string? SortDir { get; set; } // asc | desc
}
