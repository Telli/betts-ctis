namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents a filing workspace payload with all supporting data.
/// </summary>
public class FilingWorkspaceDto
{
    public string FilingId { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public IReadOnlyList<FilterOptionDto> TaxPeriodOptions { get; init; } = Array.Empty<FilterOptionDto>();
    public string SelectedTaxPeriod { get; set; } = string.Empty;
    public IReadOnlyList<FilterOptionDto> FilingStatusOptions { get; init; } = Array.Empty<FilterOptionDto>();
    public string SelectedFilingStatus { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal TaxableSales { get; set; }
    public decimal GstRate { get; set; }
    public decimal OutputTax { get; set; }
    public decimal InputTaxCredit { get; set; }
    public decimal NetGstPayable { get; set; }
    public string Notes { get; set; } = string.Empty;
    public IReadOnlyList<FilingScheduleEntryDto> Schedule { get; init; } = Array.Empty<FilingScheduleEntryDto>();
    public IReadOnlyList<FilingDocumentDto> SupportingDocuments { get; init; } = Array.Empty<FilingDocumentDto>();
    public IReadOnlyList<FilingHistoryEntryDto> History { get; init; } = Array.Empty<FilingHistoryEntryDto>();
}

/// <summary>
/// Schedule line item for a filing.
/// </summary>
public class FilingScheduleEntryDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
}

/// <summary>
/// Supporting document metadata for a filing.
/// </summary>
public class FilingDocumentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// History entry for audit trail of a filing.
/// </summary>
public class FilingHistoryEntryDto
{
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
}
