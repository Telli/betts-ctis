using System.Collections.Generic;

namespace BettsTax.Core.DTOs.Reports
{
    public class ReportTemplateDto
    {
        public int ReportTemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int EstimatedDurationSeconds { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public List<string> SupportedFormats { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> RequiredFields { get; set; } = new();
        public List<ReportParameterDto> Parameters { get; set; } = new();
        public Dictionary<string, object> DefaultParameterValues { get; set; } = new();
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ReportParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "boolean", "select", "number", "text", "date"
        public string Label { get; set; } = string.Empty;
        public object? Default { get; set; }
        public List<string>? Options { get; set; }
        public bool Required { get; set; }
    }

    public class CreateReportTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int EstimatedDurationSeconds { get; set; }
        public bool IsDefault { get; set; }
        public List<string> SupportedFormats { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> RequiredFields { get; set; } = new();
        public List<ReportParameterDto> Parameters { get; set; } = new();
        public Dictionary<string, object> DefaultParameterValues { get; set; } = new();
        public int DisplayOrder { get; set; }
    }

    public class UpdateReportTemplateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public List<ReportParameterDto>? Parameters { get; set; }
        public Dictionary<string, object>? DefaultParameterValues { get; set; }
    }
}
