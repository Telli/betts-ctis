namespace BettsTax.Core.DTOs.Analytics;

public class AnalyticsDashboardRequest
{
    public string DashboardType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ClientId { get; set; }
    public string? TaxType { get; set; }
    public List<string> Metrics { get; set; } = new();
    public string? Comparison { get; set; } // previous_period, previous_year, etc.
    public string? Granularity { get; set; } // day, week, month, quarter, year
}

public class AnalyticsDashboardResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<AnalyticsWidget> Widgets { get; set; } = new();
    public AnalyticsMetadata Metadata { get; set; } = new();
}

public class AnalyticsWidget
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // chart, metric, table, heatmap
    public AnalyticsWidgetData Data { get; set; } = new();
    public AnalyticsWidgetConfig Config { get; set; } = new();
    public int Order { get; set; }
    public string Size { get; set; } = "medium"; // small, medium, large, full-width
}

public class AnalyticsWidgetData
{
    public List<AnalyticsDataPoint> DataPoints { get; set; } = new();
    public Dictionary<string, object> Summary { get; set; } = new();
    public List<AnalyticsTableRow>? TableData { get; set; }
    public AnalyticsComparison? Comparison { get; set; }
}

public class AnalyticsDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime? Date { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AnalyticsTableRow
{
    public Dictionary<string, object> Cells { get; set; } = new();
    public string? RowId { get; set; }
    public string? Style { get; set; }
}

public class AnalyticsComparison
{
    public double CurrentPeriodValue { get; set; }
    public double PreviousPeriodValue { get; set; }
    public double PercentageChange { get; set; }
    public string Trend { get; set; } = string.Empty; // up, down, stable
    public string PeriodDescription { get; set; } = string.Empty;
}

public class AnalyticsWidgetConfig
{
    public string ChartType { get; set; } = string.Empty; // line, bar, pie, doughnut, area
    public Dictionary<string, string> Colors { get; set; } = new();
    public bool ShowLegend { get; set; } = true;
    public bool ShowTooltip { get; set; } = true;
    public string XAxisLabel { get; set; } = string.Empty;
    public string YAxisLabel { get; set; } = string.Empty;
    public bool Stacked { get; set; }
    public string ValueFormat { get; set; } = string.Empty; // currency, percentage, number
}

public class AnalyticsMetadata
{
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public Dictionary<string, string> Filters { get; set; } = new();
    public double QueryExecutionTime { get; set; }
    public int TotalDataPoints { get; set; }
}

public class RealTimeDashboardUpdate
{
    public string DashboardId { get; set; } = string.Empty;
    public string WidgetId { get; set; } = string.Empty;
    public AnalyticsWidgetData UpdatedData { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string UpdateType { get; set; } = string.Empty; // incremental, full_refresh
}

public class CustomDashboardRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DashboardWidgetDefinition> Widgets { get; set; } = new();
    public DashboardLayoutConfig Layout { get; set; } = new();
    public bool IsPublic { get; set; }
    public List<string> SharedWith { get; set; } = new();
}

public class DashboardWidgetDefinition
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public AnalyticsWidgetConfig Config { get; set; } = new();
    public DashboardWidgetPosition Position { get; set; } = new();
}

public class DashboardWidgetPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class DashboardLayoutConfig
{
    public int Columns { get; set; } = 12;
    public int RowHeight { get; set; } = 100;
    public bool IsDraggable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
}

// Predefined dashboard types
public static class DashboardTypes
{
    public const string TaxCompliance = "tax_compliance";
    public const string Revenue = "revenue";
    public const string ClientPerformance = "client_performance";
    public const string OperationalEfficiency = "operational_efficiency";
    public const string PaymentAnalytics = "payment_analytics";
    public const string DocumentManagement = "document_management";
    public const string UserActivity = "user_activity";
    public const string SystemHealth = "system_health";
}