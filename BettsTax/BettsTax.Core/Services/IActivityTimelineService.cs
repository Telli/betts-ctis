using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IActivityTimelineService
    {
        // Create activities
        Task<Result<ActivityTimelineDto>> CreateActivityAsync(ActivityTimelineCreateDto dto);
        Task<Result> LogActivityAsync(ActivityType type, string title, string? description = null, 
            int? clientId = null, int? documentId = null, int? taxFilingId = null, 
            int? paymentId = null, string? metadata = null);
        
        // Bulk activity logging
        Task<Result> LogClientLoginAsync(int clientId);
        Task<Result> LogDocumentActivityAsync(int documentId, ActivityType activityType, string? additionalInfo = null);
        Task<Result> LogTaxFilingActivityAsync(int taxFilingId, ActivityType activityType, string? additionalInfo = null);
        Task<Result> LogPaymentActivityAsync(int paymentId, ActivityType activityType, string? additionalInfo = null);
        Task<Result> LogCommunicationActivityAsync(int clientId, ActivityType activityType, string title, string? description = null);
        
        // Retrieve activities
        Task<Result<PagedResult<ActivityTimelineDto>>> GetActivitiesAsync(ActivityTimelineFilterDto filter);
        Task<Result<List<ActivityTimelineGroupDto>>> GetGroupedActivitiesAsync(int clientId, int days = 30);
        Task<Result<ActivityTimelineSummaryDto>> GetActivitySummaryAsync(int? clientId = null, string? userId = null);
        Task<Result<List<ClientActivitySummaryDto>>> GetClientActivitySummariesAsync(string? associateId = null);
        
        // Client-specific views
        Task<Result<PagedResult<ActivityTimelineDto>>> GetClientActivitiesAsync(int clientId, int page = 1, int pageSize = 50);
        Task<Result<List<ActivityTimelineDto>>> GetRecentClientActivitiesAsync(int clientId, int count = 10);
        
        // Associate/Admin views
        Task<Result<PagedResult<ActivityTimelineDto>>> GetAssociateClientActivitiesAsync(string associateId, int page = 1, int pageSize = 50);
        Task<Result<List<ActivityTimelineDto>>> GetHighPriorityActivitiesAsync(string? associateId = null, int count = 20);
        Task<Result<List<ActivityTimelineDto>>> GetSystemAlertsAsync(int? clientId = null, int days = 7);
        
        // Search and export
        Task<Result<PagedResult<ActivityTimelineDto>>> SearchActivitiesAsync(string searchTerm, ActivityTimelineFilterDto? filter = null);
        Task<Result<byte[]>> ExportActivitiesToCsvAsync(ActivityTimelineFilterDto filter);
        Task<Result<byte[]>> ExportActivitiesToPdfAsync(int clientId, DateTime startDate, DateTime endDate);
        
        // Utility methods
        Task<Result> MarkActivitiesAsReadAsync(int clientId, string userId);
        Task<Result> DeleteOldActivitiesAsync(int daysToKeep = 365);
    }
}