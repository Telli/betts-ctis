using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IPermissionTemplateService
    {
        Task<List<AssociatePermissionTemplate>> GetTemplatesAsync();
        Task<AssociatePermissionTemplate?> GetTemplateAsync(int templateId);
        Task<AssociatePermissionTemplate?> GetDefaultTemplateAsync();
        Task<Result<AssociatePermissionTemplate>> CreateTemplateAsync(CreateTemplateRequest request, string adminId);
        Task<Result> UpdateTemplateAsync(int templateId, UpdateTemplateRequest request, string adminId);
        Task<Result> DeleteTemplateAsync(int templateId, string adminId);
        Task<Result> SetDefaultTemplateAsync(int templateId, string adminId);
        Task<Result> ApplyTemplateToAssociateAsync(int templateId, string associateId, List<int> clientIds, string adminId);
        Task<Result> ApplyTemplateToMultipleAssociatesAsync(int templateId, List<string> associateIds, List<int> clientIds, string adminId);
        Task<List<AssociatePermissionTemplate>> GetTemplatesByCreatorAsync(string adminId);
        Task<bool> IsTemplateInUseAsync(int templateId);
        Task<Result> CloneTemplateAsync(int sourceTemplateId, string newName, string adminId);
        Task<Dictionary<string, object>> GetTemplateUsageStatistics(int templateId);
    }

    public class CreateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreatePermissionRuleRequest> Rules { get; set; } = new();
        public bool IsDefault { get; set; } = false;
    }

    public class CreatePermissionRuleRequest
    {
        public string PermissionArea { get; set; } = string.Empty;
        public AssociatePermissionLevel Level { get; set; }
        public decimal? AmountThreshold { get; set; }
        public bool RequiresApproval { get; set; } = false;
    }

    public class UpdateTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreatePermissionRuleRequest> Rules { get; set; } = new();
        public bool IsDefault { get; set; } = false;
    }
}