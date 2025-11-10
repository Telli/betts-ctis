namespace BettsTax.Core.DTOs
{
    #region Configuration DTOs

    /// <summary>
    /// ERP integration configuration
    /// </summary>
    public class ErpIntegrationConfigDto
    {
        public int ClientId { get; set; }
        public string ErpSystem { get; set; } = string.Empty; // "QuickBooks", "Xero", "SAP", etc.
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ErpSyncMode SyncMode { get; set; }
        public int SyncIntervalMinutes { get; set; } = 60;
        public DateTime? LastSyncDate { get; set; }
        public Dictionary<string, object> ConnectionSettings { get; set; } = new();
        public Dictionary<string, object> SyncSettings { get; set; } = new();
        public List<ErpDataType> EnabledDataTypes { get; set; } = new();
    }

    /// <summary>
    /// ERP integration details
    /// </summary>
    public class ErpIntegrationDto : ErpIntegrationConfigDto
    {
        public int Id { get; set; }
        public ErpIntegrationStatus Status { get; set; }
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? LastSyncError { get; set; }
        public ErpHealthStatusDto HealthStatus { get; set; } = new();
    }

    /// <summary>
    /// ERP connection health status
    /// </summary>
    public class ErpConnectionHealthDto
    {
        public int IntegrationId { get; set; }
        public string ErpSystem { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public DateTime LastCheckDate { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> HealthDetails { get; set; } = new();
    }

    /// <summary>
    /// ERP health status details
    /// </summary>
    public class ErpHealthStatusDto
    {
        public bool IsConnected { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
        public DateTime? LastHealthCheck { get; set; }
        public int FailureCount { get; set; }
        public string? LastError { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    #endregion

    #region Synchronization DTOs

    /// <summary>
    /// ERP sync options
    /// </summary>
    public class ErpSyncOptionsDto
    {
        public List<ErpDataType>? DataTypes { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool DryRun { get; set; } = false;
        public bool IgnoreErrors { get; set; } = false;
        public int BatchSize { get; set; } = 100;
        public bool ValidateOnly { get; set; } = false;
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    /// <summary>
    /// Full synchronization result
    /// </summary>
    public class ErpFullSyncResultDto
    {
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<ErpSyncResultDto> DataTypeResults { get; set; } = new();
        public int TotalRecordsProcessed { get; set; }
        public int TotalRecordsSucceeded { get; set; }
        public int TotalRecordsFailed { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Incremental synchronization result
    /// </summary>
    public class ErpIncrementalSyncResultDto : ErpFullSyncResultDto
    {
        public DateTime LastSyncDate { get; set; }
        public DateTime CurrentSyncDate { get; set; }
        public int NewRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public int DeletedRecords { get; set; }
    }

    /// <summary>
    /// Synchronization result for specific data type
    /// </summary>
    public class ErpSyncResultDto
    {
        public ErpDataType DataType { get; set; }
        public ErpSyncDirection Direction { get; set; }
        public bool IsSuccess { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsSucceeded { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ErpSyncErrorDto> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Bulk synchronization result
    /// </summary>
    public class ErpBulkSyncResultDto
    {
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ClientsProcessed { get; set; }
        public int ClientsSucceeded { get; set; }
        public int ClientsFailed { get; set; }
        public List<ErpClientSyncResultDto> ClientResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual client sync result in bulk operation
    /// </summary>
    public class ErpClientSyncResultDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public ErpFullSyncResultDto SyncResult { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Sync error details
    /// </summary>
    public class ErpSyncErrorDto
    {
        public string RecordId { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public Dictionary<string, object> RecordData { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Field Mapping DTOs

    /// <summary>
    /// Field mapping configuration
    /// </summary>
    public class ErpFieldMappingDto
    {
        public int IntegrationId { get; set; }
        public ErpDataType DataType { get; set; }
        public List<ErpFieldMapDto> FieldMaps { get; set; } = new();
        public Dictionary<string, object> TransformationRules { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual field mapping
    /// </summary>
    public class ErpFieldMapDto
    {
        public string CtisField { get; set; } = string.Empty;
        public string ErpField { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public string? ValidationRule { get; set; }
        public string? TransformationRule { get; set; }
        public ErpSyncDirection SyncDirection { get; set; } = ErpSyncDirection.Bidirectional;
    }

    /// <summary>
    /// ERP field definition
    /// </summary>
    public class ErpFieldDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string? Description { get; set; }
        public List<string> AllowedValues { get; set; } = new();
        public Dictionary<string, object> ValidationRules { get; set; } = new();
    }

    /// <summary>
    /// Mapping validation result
    /// </summary>
    public class ErpMappingValidationDto
    {
        public bool IsValid { get; set; }
        public List<ErpMappingValidationErrorDto> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> ValidationResults { get; set; } = new();
    }

    /// <summary>
    /// Mapping validation error
    /// </summary>
    public class ErpMappingValidationErrorDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error"; // Error, Warning, Info
    }

    #endregion

    #region Monitoring and Analytics DTOs

    /// <summary>
    /// Integration health dashboard
    /// </summary>
    public class ErpHealthDashboardDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int TotalIntegrations { get; set; }
        public int HealthyIntegrations { get; set; }
        public int UnhealthyIntegrations { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public DateTime? NextScheduledSync { get; set; }
        public List<ErpConnectionHealthDto> IntegrationHealth { get; set; } = new();
        public ErpDashboardMetricsDto Metrics { get; set; } = new();
    }

    /// <summary>
    /// Dashboard metrics
    /// </summary>
    public class ErpDashboardMetricsDto
    {
        public int TotalSyncsToday { get; set; }
        public int SuccessfulSyncsToday { get; set; }
        public int FailedSyncsToday { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageSyncTime { get; set; }
        public int RecordsSyncedToday { get; set; }
        public List<ErpDataTypeSyncStatsDto> DataTypeStats { get; set; } = new();
    }

    /// <summary>
    /// Data type sync statistics
    /// </summary>
    public class ErpDataTypeSyncStatsDto
    {
        public ErpDataType DataType { get; set; }
        public int RecordsSynced { get; set; }
        public int Errors { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Sync history filter
    /// </summary>
    public class ErpSyncHistoryFilterDto
    {
        public int? IntegrationId { get; set; }
        public List<ErpDataType>? DataTypes { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? SuccessOnly { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "StartTime";
        public string SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Sync history entry
    /// </summary>
    public class ErpSyncHistoryDto
    {
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        public string ErpSystem { get; set; } = string.Empty;
        public ErpDataType DataType { get; set; }
        public ErpSyncDirection Direction { get; set; }
        public ErpSyncMode SyncMode { get; set; }
        public bool IsSuccess { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsSucceeded { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TriggeredBy { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class ErpPerformanceMetricsDto
    {
        public int IntegrationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalSyncs { get; set; }
        public int SuccessfulSyncs { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MedianDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public double RecordsPerSecond { get; set; }
        public List<ErpPerformanceTrendDto> Trends { get; set; } = new();
    }

    /// <summary>
    /// Performance trend data
    /// </summary>
    public class ErpPerformanceTrendDto
    {
        public DateTime Date { get; set; }
        public int SyncCount { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public int RecordsProcessed { get; set; }
    }

    /// <summary>
    /// Error analysis result
    /// </summary>
    public class ErpErrorAnalysisDto
    {
        public int IntegrationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalErrors { get; set; }
        public List<ErpErrorCategoryDto> ErrorCategories { get; set; } = new();
        public List<ErpErrorTrendDto> ErrorTrends { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public ErpErrorSeverityStatsDto SeverityStats { get; set; } = new();
    }

    /// <summary>
    /// Error category statistics
    /// </summary>
    public class ErpErrorCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public List<ErpErrorCodeStatsDto> ErrorCodes { get; set; } = new();
    }

    /// <summary>
    /// Error code statistics
    /// </summary>
    public class ErpErrorCodeStatsDto
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime? FirstOccurrence { get; set; }
        public DateTime? LastOccurrence { get; set; }
    }

    /// <summary>
    /// Error trend data
    /// </summary>
    public class ErpErrorTrendDto
    {
        public DateTime Date { get; set; }
        public int ErrorCount { get; set; }
        public List<ErpErrorCategoryTrendDto> Categories { get; set; } = new();
    }

    /// <summary>
    /// Error category trend
    /// </summary>
    public class ErpErrorCategoryTrendDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// Error severity statistics
    /// </summary>
    public class ErpErrorSeverityStatsDto
    {
        public int CriticalErrors { get; set; }
        public int MajorErrors { get; set; }
        public int MinorErrors { get; set; }
        public int Warnings { get; set; }
    }

    #endregion

    #region Conflict Resolution DTOs

    /// <summary>
    /// Data conflict requiring resolution
    /// </summary>
    public class ErpDataConflictDto
    {
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        public string ErpSystem { get; set; } = string.Empty;
        public ErpDataType DataType { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty; // "Update", "Delete", "Duplicate"
        public Dictionary<string, object> CtisData { get; set; } = new();
        public Dictionary<string, object> ErpData { get; set; } = new();
        public DateTime ConflictDate { get; set; }
        public string ConflictReason { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public ErpConflictResolutionStrategy? SuggestedResolution { get; set; }
    }

    /// <summary>
    /// Conflict resolution
    /// </summary>
    public class ErpConflictResolutionDto
    {
        public ErpConflictResolutionStrategy Strategy { get; set; }
        public Dictionary<string, object>? CustomResolution { get; set; }
        public string? ResolutionNotes { get; set; }
        public bool ApplyToFutureConflicts { get; set; }
    }

    /// <summary>
    /// Conflict resolution rules
    /// </summary>
    public class ErpConflictRulesDto
    {
        public int IntegrationId { get; set; }
        public Dictionary<string, ErpConflictResolutionStrategy> DataTypeRules { get; set; } = new();
        public Dictionary<string, ErpConflictResolutionStrategy> FieldRules { get; set; } = new();
        public ErpConflictResolutionStrategy DefaultStrategy { get; set; } = ErpConflictResolutionStrategy.Manual;
        public bool AutoResolveMinorConflicts { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    #endregion

    #region Export/Import DTOs

    /// <summary>
    /// Export options
    /// </summary>
    public class ErpExportOptionsDto
    {
        public List<ErpDataType> DataTypes { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Format { get; set; } = "CSV"; // CSV, JSON, XML
        public string? TemplateId { get; set; }
        public Dictionary<string, object> FormatOptions { get; set; } = new();
        public bool IncludeHeaders { get; set; } = true;
        public bool CompressOutput { get; set; } = false;
    }

    /// <summary>
    /// Export result
    /// </summary>
    public class ErpExportResultDto
    {
        public bool IsSuccess { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public int RecordsExported { get; set; }
        public DateTime ExportDate { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Import options
    /// </summary>
    public class ErpImportOptionsDto
    {
        public string FilePath { get; set; } = string.Empty;
        public ErpDataType DataType { get; set; }
        public string Format { get; set; } = "CSV";
        public bool ValidateOnly { get; set; } = false;
        public bool SkipDuplicates { get; set; } = true;
        public bool UpdateExisting { get; set; } = false;
        public Dictionary<string, object> FormatOptions { get; set; } = new();
        public Dictionary<string, string> FieldMapping { get; set; } = new();
    }

    /// <summary>
    /// Import result
    /// </summary>
    public class ErpImportResultDto
    {
        public bool IsSuccess { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsImported { get; set; }
        public int RecordsSkipped { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime ImportDate { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ErpImportErrorDto> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Import error
    /// </summary>
    public class ErpImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> RowData { get; set; } = new();
    }

    /// <summary>
    /// Export template
    /// </summary>
    public class ErpExportTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ErpSystem { get; set; } = string.Empty;
        public ErpDataType DataType { get; set; }
        public List<ErpTemplateFieldDto> Fields { get; set; } = new();
        public Dictionary<string, object> FormatOptions { get; set; } = new();
    }

    /// <summary>
    /// Export template field
    /// </summary>
    public class ErpTemplateFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? Transformation { get; set; }
    }

    #endregion

    #region Webhook DTOs

    /// <summary>
    /// Webhook configuration
    /// </summary>
    public class ErpWebhookConfigDto
    {
        public int IntegrationId { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
        public List<ErpDataType> MonitoredDataTypes { get; set; } = new();
        public List<string> Events { get; set; } = new(); // create, update, delete
        public Dictionary<string, string> Headers { get; set; } = new();
        public string SecretKey { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RetryAttempts { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Webhook log filter
    /// </summary>
    public class ErpWebhookLogFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string>? Events { get; set; }
        public bool? SuccessOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "Timestamp";
        public string SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Webhook log entry
    /// </summary>
    public class ErpWebhookLogDto
    {
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        public string Event { get; set; } = string.Empty;
        public ErpDataType DataType { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int AttemptNumber { get; set; }
    }

    #endregion

    #region System Administration DTOs

    /// <summary>
    /// System-wide integration statistics
    /// </summary>
    public class ErpSystemStatsDto
    {
        public int TotalClients { get; set; }
        public int ClientsWithIntegrations { get; set; }
        public int TotalIntegrations { get; set; }
        public int ActiveIntegrations { get; set; }
        public Dictionary<string, int> IntegrationsBySystem { get; set; } = new();
        public Dictionary<ErpDataType, int> SyncsByDataType { get; set; } = new();
        public double OverallSuccessRate { get; set; }
        public int TotalSyncsToday { get; set; }
        public long TotalRecordsSynced { get; set; }
        public DateTime? LastSystemSync { get; set; }
        public ErpSystemHealthDto SystemHealth { get; set; } = new();
    }

    /// <summary>
    /// System health metrics
    /// </summary>
    public class ErpSystemHealthDto
    {
        public bool IsHealthy { get; set; }
        public int HealthyIntegrations { get; set; }
        public int UnhealthyIntegrations { get; set; }
        public int IntegrationsWithErrors { get; set; }
        public DateTime? LastHealthCheck { get; set; }
        public List<string> SystemAlerts { get; set; } = new();
    }

    /// <summary>
    /// Maintenance options
    /// </summary>
    public class ErpMaintenanceOptionsDto
    {
        public bool CleanupLogs { get; set; }
        public int LogRetentionDays { get; set; } = 90;
        public bool RefreshConnections { get; set; }
        public bool ValidateMappings { get; set; }
        public bool OptimizeDatabase { get; set; }
        public bool GenerateReports { get; set; }
        public List<int>? SpecificIntegrationIds { get; set; }
    }

    /// <summary>
    /// Maintenance result
    /// </summary>
    public class ErpMaintenanceResultDto
    {
        public bool IsSuccess { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<ErpMaintenanceTaskResultDto> TaskResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Maintenance task result
    /// </summary>
    public class ErpMaintenanceTaskResultDto
    {
        public string TaskName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Audit filter
    /// </summary>
    public class ErpAuditFilterDto
    {
        public int? IntegrationId { get; set; }
        public int? ClientId { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "Timestamp";
        public string SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Audit log entry
    /// </summary>
    public class ErpAuditLogDto
    {
        public int Id { get; set; }
        public int? IntegrationId { get; set; }
        public int? ClientId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region Enums

    /// <summary>
    /// ERP data types for synchronization
    /// </summary>
    public enum ErpDataType
    {
        Clients,
        Payments,
        Invoices,
        Expenses,
        TaxFilings,
        JournalEntries,
        Accounts,
        Contacts,
        Items,
        Reports
    }

    /// <summary>
    /// ERP integration status
    /// </summary>
    public enum ErpIntegrationStatus
    {
        Active,
        Inactive,
        Error,
        Connecting,
        Disconnected
    }

    /// <summary>
    /// Synchronization direction
    /// </summary>
    public enum ErpSyncDirection
    {
        Export,       // From CTIS to ERP
        Import,       // From ERP to CTIS
        Bidirectional // Both directions
    }

    /// <summary>
    /// Synchronization mode
    /// </summary>
    public enum ErpSyncMode
    {
        Manual,
        Scheduled,
        Automatic,
        Webhook
    }

    /// <summary>
    /// Conflict resolution strategy
    /// </summary>
    public enum ErpConflictResolutionStrategy
    {
        Manual,           // Require manual resolution
        CtisWins,        // CTIS data takes precedence
        ErpWins,         // ERP data takes precedence
        MostRecent,      // Most recently modified wins
        Custom           // Use custom resolution logic
    }

    #endregion
}