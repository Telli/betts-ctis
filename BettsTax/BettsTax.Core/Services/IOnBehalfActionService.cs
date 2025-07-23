using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IOnBehalfActionService
    {
        Task LogActionAsync(string associateId, int clientId, string action, string entityType, int entityId, 
            object? oldValues = null, object? newValues = null, string? reason = null, string? ipAddress = null, string? userAgent = null);
        
        Task<List<OnBehalfAction>> GetClientActionsAsync(int clientId, DateTime? from = null, DateTime? to = null);
        Task<List<OnBehalfAction>> GetAssociateActionsAsync(string associateId, DateTime? from = null, DateTime? to = null);
        Task<List<OnBehalfAction>> GetEntityActionsAsync(string entityType, int entityId);
        Task<Result> NotifyClientOfActionAsync(int clientId, OnBehalfAction action);
        Task<List<OnBehalfAction>> GetRecentActionsAsync(string associateId, int limit = 10);
        Task<Dictionary<string, object>> GetActionStatisticsAsync(string associateId, DateTime? from = null, DateTime? to = null);
        Task<List<OnBehalfAction>> GetUnnotifiedActionsAsync(int clientId);
        Task<Result> MarkActionAsNotifiedAsync(int actionId);
        Task<Result> BulkNotifyClientAsync(int clientId, List<int> actionIds);
    }
}