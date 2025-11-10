using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Enhanced ERP integration manager for orchestrating multiple accounting system integrations
    /// </summary>
    public interface IErpIntegrationManager
    {
        #region Connection Management
        
        /// <summary>
        /// Gets all configured integrations for a client
        /// </summary>
        Task<Result<List<ErpIntegrationDto>>> GetClientIntegrationsAsync(int clientId);

        /// <summary>
        /// Configures a new ERP integration for a client
        /// </summary>
        Task<Result<ErpIntegrationDto>> ConfigureIntegrationAsync(ErpIntegrationConfigDto config);

        /// <summary>
        /// Updates an existing ERP integration configuration
        /// </summary>
        Task<Result<ErpIntegrationDto>> UpdateIntegrationAsync(int integrationId, ErpIntegrationConfigDto config);

        /// <summary>
        /// Removes an ERP integration
        /// </summary>
        Task<Result> RemoveIntegrationAsync(int integrationId);

        /// <summary>
        /// Tests connectivity for all client integrations
        /// </summary>
        Task<Result<List<ErpConnectionHealthDto>>> TestAllConnectionsAsync(int clientId);

        #endregion

        #region Data Synchronization

        /// <summary>
        /// Performs full synchronization for all client integrations
        /// </summary>
        Task<Result<ErpFullSyncResultDto>> PerformFullSyncAsync(int clientId, ErpSyncOptionsDto options);

        /// <summary>
        /// Performs incremental synchronization since last sync
        /// </summary>
        Task<Result<ErpIncrementalSyncResultDto>> PerformIncrementalSyncAsync(int clientId, ErpSyncOptionsDto options);

        /// <summary>
        /// Syncs specific data types to/from ERP systems
        /// </summary>
        Task<Result<ErpSyncResultDto>> SyncDataTypeAsync(int clientId, ErpDataType dataType, ErpSyncDirection direction, ErpSyncOptionsDto options);

        /// <summary>
        /// Bulk synchronizes multiple clients (for administrative operations)
        /// </summary>
        Task<Result<ErpBulkSyncResultDto>> BulkSyncClientsAsync(List<int> clientIds, ErpSyncOptionsDto options);

        #endregion

        #region Data Mapping and Transformation

        /// <summary>
        /// Gets field mapping configuration for a client's integration
        /// </summary>
        Task<Result<ErpFieldMappingDto>> GetFieldMappingAsync(int integrationId);

        /// <summary>
        /// Updates field mapping configuration
        /// </summary>
        Task<Result<ErpFieldMappingDto>> UpdateFieldMappingAsync(int integrationId, ErpFieldMappingDto mapping);

        /// <summary>
        /// Gets available fields from the ERP system for mapping
        /// </summary>
        Task<Result<List<ErpFieldDefinitionDto>>> GetErpFieldsAsync(int integrationId, ErpDataType dataType);

        /// <summary>
        /// Validates field mapping configuration
        /// </summary>
        Task<Result<ErpMappingValidationDto>> ValidateMappingAsync(int integrationId, ErpFieldMappingDto mapping);

        #endregion

        #region Monitoring and Analytics

        /// <summary>
        /// Gets integration health status for all client integrations
        /// </summary>
        Task<Result<ErpHealthDashboardDto>> GetHealthDashboardAsync(int clientId);

        /// <summary>
        /// Gets sync history and analytics
        /// </summary>
        Task<Result<PagedResult<ErpSyncHistoryDto>>> GetSyncHistoryAsync(int clientId, ErpSyncHistoryFilterDto filter);

        /// <summary>
        /// Gets integration performance metrics
        /// </summary>
        Task<Result<ErpPerformanceMetricsDto>> GetPerformanceMetricsAsync(int integrationId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets error analysis and recommendations
        /// </summary>
        Task<Result<ErpErrorAnalysisDto>> AnalyzeErrorsAsync(int integrationId, DateTime startDate, DateTime endDate);

        #endregion

        #region Conflict Resolution

        /// <summary>
        /// Gets data conflicts requiring manual resolution
        /// </summary>
        Task<Result<List<ErpDataConflictDto>>> GetPendingConflictsAsync(int clientId);

        /// <summary>
        /// Resolves a data conflict
        /// </summary>
        Task<Result> ResolveConflictAsync(int conflictId, ErpConflictResolutionDto resolution);

        /// <summary>
        /// Configures automatic conflict resolution rules
        /// </summary>
        Task<Result<ErpConflictRulesDto>> ConfigureConflictRulesAsync(int integrationId, ErpConflictRulesDto rules);

        #endregion

        #region Webhooks and Real-time Integration

        /// <summary>
        /// Configures webhook endpoints for real-time data updates
        /// </summary>
        Task<Result<ErpWebhookConfigDto>> ConfigureWebhooksAsync(int integrationId, ErpWebhookConfigDto config);

        /// <summary>
        /// Processes incoming webhook from ERP system
        /// </summary>
        Task<Result> ProcessWebhookAsync(string integrationKey, string webhookPayload, Dictionary<string, string> headers);

        /// <summary>
        /// Gets webhook delivery status and logs
        /// </summary>
        Task<Result<PagedResult<ErpWebhookLogDto>>> GetWebhookLogsAsync(int integrationId, ErpWebhookLogFilterDto filter);

        #endregion

        #region Data Export and Import

        /// <summary>
        /// Exports client data in ERP-compatible format
        /// </summary>
        Task<Result<ErpExportResultDto>> ExportDataAsync(int clientId, ErpExportOptionsDto options);

        /// <summary>
        /// Imports data from ERP system with validation
        /// </summary>
        Task<Result<ErpImportResultDto>> ImportDataAsync(int integrationId, ErpImportOptionsDto options);

        /// <summary>
        /// Gets export templates for different ERP systems
        /// </summary>
        Task<Result<List<ErpExportTemplateDto>>> GetExportTemplatesAsync(string erpSystem);

        #endregion

        #region System Administration

        /// <summary>
        /// Gets system-wide integration statistics
        /// </summary>
        Task<Result<ErpSystemStatsDto>> GetSystemStatsAsync();

        /// <summary>
        /// Performs maintenance operations on integrations
        /// </summary>
        Task<Result<ErpMaintenanceResultDto>> PerformMaintenanceAsync(ErpMaintenanceOptionsDto options);

        /// <summary>
        /// Gets integration audit trail
        /// </summary>
        Task<Result<PagedResult<ErpAuditLogDto>>> GetAuditTrailAsync(ErpAuditFilterDto filter);

        #endregion
    }
}