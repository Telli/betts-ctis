namespace BettsTax.Core.DTOs.Filing;

/// <summary>
/// Filing DTO
/// </summary>
public class FilingDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? TotalSales { get; set; }
    public decimal? TaxableSales { get; set; }
    public decimal? GstRate { get; set; }
    public decimal? OutputTax { get; set; }
    public decimal? InputTaxCredit { get; set; }
    public decimal? NetGstPayable { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Schedule row DTO
/// </summary>
public class ScheduleRowDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Taxable { get; set; }
}

/// <summary>
/// Filing document DTO
/// </summary>
public class FilingDocumentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

/// <summary>
/// Filing history DTO
/// </summary>
public class FilingHistoryDto
{
    public string Date { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
}

/// <summary>
/// Update filing DTO
/// </summary>
public class UpdateFilingDto
{
    public string? Status { get; set; }
    public decimal? TotalSales { get; set; }
    public decimal? TaxableSales { get; set; }
    public decimal? GstRate { get; set; }
    public decimal? OutputTax { get; set; }
    public decimal? InputTaxCredit { get; set; }
    public decimal? NetGstPayable { get; set; }
    public string? Notes { get; set; }
}
