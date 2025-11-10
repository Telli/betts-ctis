namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents an available report type.
/// </summary>
public class ReportTypeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconKey { get; init; } = string.Empty;
}

/// <summary>
/// Options for report generation filters.
/// </summary>
public class ReportFiltersDto
{
    public IReadOnlyList<FilterOptionDto> Clients { get; init; } = Array.Empty<FilterOptionDto>();
    public IReadOnlyList<FilterOptionDto> TaxTypes { get; init; } = Array.Empty<FilterOptionDto>();
}

/// <summary>
/// Generic value/label pair for select inputs.
/// </summary>
public class FilterOptionDto
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
