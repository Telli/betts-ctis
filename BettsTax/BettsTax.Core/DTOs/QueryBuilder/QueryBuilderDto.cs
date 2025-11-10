using System.Text.Json.Serialization;

namespace BettsTax.Core.DTOs.QueryBuilder;

public class QueryBuilderRequest
{
    public string DataSource { get; set; } = string.Empty;
    public List<QueryField> SelectFields { get; set; } = new();
    public List<QueryCondition> Conditions { get; set; } = new();
    public List<QueryGroupBy> GroupBy { get; set; } = new();
    public List<QueryOrderBy> OrderBy { get; set; } = new();
    public QueryAggregation? Aggregation { get; set; }
    public QueryPivot? Pivot { get; set; }
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public DateTimeOffset? DateRange { get; set; }
    public string? SaveAsName { get; set; }
}

public class QueryField
{
    public string FieldName { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string DataType { get; set; } = string.Empty;
    public bool IsCalculated { get; set; }
    public string? CalculationExpression { get; set; }
}

public class QueryCondition
{
    public string FieldName { get; set; } = string.Empty;
    public QueryOperator Operator { get; set; }
    public object? Value { get; set; }
    public object? SecondValue { get; set; } // For BETWEEN operations
    public QueryLogicalOperator? LogicalOperator { get; set; } // AND/OR
    public List<QueryCondition>? NestedConditions { get; set; } // For complex conditions
}

public class QueryGroupBy
{
    public string FieldName { get; set; } = string.Empty;
    public string? DateGrouping { get; set; } // day, week, month, quarter, year
}

public class QueryOrderBy
{
    public string FieldName { get; set; } = string.Empty;
    public QuerySortDirection Direction { get; set; } = QuerySortDirection.Asc;
}

public class QueryAggregation
{
    public Dictionary<string, QueryAggregationType> Aggregations { get; set; } = new();
    public List<string>? Having { get; set; } // Having clauses for aggregated data
}

public class QueryPivot
{
    public string RowField { get; set; } = string.Empty;
    public string ColumnField { get; set; } = string.Empty;
    public string ValueField { get; set; } = string.Empty;
    public QueryAggregationType AggregationType { get; set; } = QueryAggregationType.Sum;
}

public class QueryBuilderResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public QueryResultData? Data { get; set; }
    public QueryMetadata? Metadata { get; set; }
    public int TotalRows { get; set; }
    public double ExecutionTimeMs { get; set; }
}

public class QueryResultData
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<QueryColumnInfo> Columns { get; set; } = new();
    public Dictionary<string, object>? AggregatedData { get; set; }
    public PivotTableData? PivotData { get; set; }
}

public class QueryColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DisplayName { get; set; }
}

public class QueryMetadata
{
    public string GeneratedSql { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> JoinedTables { get; set; } = new();
}

public class PivotTableData
{
    public List<string> RowHeaders { get; set; } = new();
    public List<string> ColumnHeaders { get; set; } = new();
    public Dictionary<string, Dictionary<string, object?>> Data { get; set; } = new();
}

public class DataSourceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DataFieldInfo> Fields { get; set; } = new();
    public List<DataRelationInfo> Relations { get; set; } = new();
}

public class DataFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? RelatedTable { get; set; }
    public List<object>? SampleValues { get; set; }
}

public class DataRelationInfo
{
    public string RelatedTable { get; set; } = string.Empty;
    public string JoinType { get; set; } = string.Empty;
    public List<string> JoinConditions { get; set; } = new();
}

public class SavedQuery
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QueryBuilderRequest QueryDefinition { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int UsageCount { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull,
    Between,
    In,
    NotIn
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryLogicalOperator
{
    And,
    Or
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuerySortDirection
{
    Asc,
    Desc
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QueryAggregationType
{
    Count,
    Sum,
    Average,
    Min,
    Max,
    CountDistinct
}