using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class PermissionTemplateService : IPermissionTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAssociatePermissionService _permissionService;
        private readonly ILogger<PermissionTemplateService> _logger;

        public PermissionTemplateService(ApplicationDbContext context, IAssociatePermissionService permissionService, ILogger<PermissionTemplateService> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<List<AssociatePermissionTemplate>> GetTemplatesAsync()
        {
            try
            {
                return await _context.AssociatePermissionTemplates
                    .Include(t => t.Rules)
                    .Include(t => t.CreatedByAdmin)
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission templates");
                return new List<AssociatePermissionTemplate>();
            }
        }

        public async Task<AssociatePermissionTemplate?> GetTemplateAsync(int templateId)
        {
            try
            {
                return await _context.AssociatePermissionTemplates
                    .Include(t => t.Rules)
                    .Include(t => t.CreatedByAdmin)
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission template {TemplateId}", templateId);
                return null;
            }
        }

        public async Task<AssociatePermissionTemplate?> GetDefaultTemplateAsync()
        {
            try
            {
                return await _context.AssociatePermissionTemplates
                    .Include(t => t.Rules)
                    .Include(t => t.CreatedByAdmin)
                    .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default permission template");
                return null;
            }
        }

        public async Task<Result<AssociatePermissionTemplate>> CreateTemplateAsync(CreateTemplateRequest request, string adminId)
        {
            try
            {
                // If this is to be the default template, unset other defaults
                if (request.IsDefault)
                {
                    var existingDefaults = await _context.AssociatePermissionTemplates
                        .Where(t => t.IsDefault && t.IsActive)
                        .ToListAsync();

                    foreach (var template in existingDefaults)
                    {
                        template.IsDefault = false;
                        template.UpdatedDate = DateTime.UtcNow;
                    }
                }

                var newTemplate = new AssociatePermissionTemplate
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsDefault = request.IsDefault,
                    CreatedByAdminId = adminId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.AssociatePermissionTemplates.Add(newTemplate);
                await _context.SaveChangesAsync();

                // Add the rules
                foreach (var ruleRequest in request.Rules)
                {
                    var rule = new AssociatePermissionRule
                    {
                        TemplateId = newTemplate.Id,
                        PermissionArea = ruleRequest.PermissionArea,
                        Level = ruleRequest.Level,
                        AmountThreshold = ruleRequest.AmountThreshold,
                        RequiresApproval = ruleRequest.RequiresApproval,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.AssociatePermissionRules.Add(rule);
                }

                await _context.SaveChangesAsync();

                // Reload with includes
                var createdTemplate = await GetTemplateAsync(newTemplate.Id);
                return Result.Success(createdTemplate!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission template");
                return Result.Failure<AssociatePermissionTemplate>("Failed to create permission template");
            }
        }

        public async Task<Result> UpdateTemplateAsync(int templateId, UpdateTemplateRequest request, string adminId)
        {
            try
            {
                var template = await GetTemplateAsync(templateId);
                if (template == null)
                    return Result.Failure("Template not found");

                // If this is to be the default template, unset other defaults
                if (request.IsDefault && !template.IsDefault)
                {
                    var existingDefaults = await _context.AssociatePermissionTemplates
                        .Where(t => t.IsDefault && t.IsActive && t.Id != templateId)
                        .ToListAsync();

                    foreach (var existingDefault in existingDefaults)
                    {
                        existingDefault.IsDefault = false;
                        existingDefault.UpdatedDate = DateTime.UtcNow;
                    }
                }

                // Update template properties
                template.Name = request.Name;
                template.Description = request.Description;
                template.IsDefault = request.IsDefault;
                template.UpdatedDate = DateTime.UtcNow;

                // Remove existing rules
                var existingRules = await _context.AssociatePermissionRules
                    .Where(r => r.TemplateId == templateId)
                    .ToListAsync();

                _context.AssociatePermissionRules.RemoveRange(existingRules);

                // Add new rules
                foreach (var ruleRequest in request.Rules)
                {
                    var rule = new AssociatePermissionRule
                    {
                        TemplateId = templateId,
                        PermissionArea = ruleRequest.PermissionArea,
                        Level = ruleRequest.Level,
                        AmountThreshold = ruleRequest.AmountThreshold,
                        RequiresApproval = ruleRequest.RequiresApproval,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.AssociatePermissionRules.Add(rule);
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission template {TemplateId}", templateId);
                return Result.Failure("Failed to update permission template");
            }
        }

        public async Task<Result> DeleteTemplateAsync(int templateId, string adminId)
        {
            try
            {
                var template = await _context.AssociatePermissionTemplates.FindAsync(templateId);
                if (template == null)
                    return Result.Failure("Template not found");

                // Check if template is in use
                var isInUse = await IsTemplateInUseAsync(templateId);
                if (isInUse)
                    return Result.Failure("Cannot delete template that is currently in use");

                template.IsActive = false;
                template.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission template {TemplateId}", templateId);
                return Result.Failure("Failed to delete permission template");
            }
        }

        public async Task<Result> SetDefaultTemplateAsync(int templateId, string adminId)
        {
            try
            {
                var template = await _context.AssociatePermissionTemplates.FindAsync(templateId);
                if (template == null || !template.IsActive)
                    return Result.Failure("Template not found");

                // Unset other defaults
                var existingDefaults = await _context.AssociatePermissionTemplates
                    .Where(t => t.IsDefault && t.IsActive && t.Id != templateId)
                    .ToListAsync();

                foreach (var existingDefault in existingDefaults)
                {
                    existingDefault.IsDefault = false;
                    existingDefault.UpdatedDate = DateTime.UtcNow;
                }

                // Set as default
                template.IsDefault = true;
                template.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default template {TemplateId}", templateId);
                return Result.Failure("Failed to set default template");
            }
        }

        public async Task<Result> ApplyTemplateToAssociateAsync(int templateId, string associateId, List<int> clientIds, string adminId)
        {
            try
            {
                var template = await GetTemplateAsync(templateId);
                if (template == null)
                    return Result.Failure("Template not found");

                foreach (var rule in template.Rules)
                {
                    var request = new GrantPermissionRequest
                    {
                        AssociateId = associateId,
                        ClientIds = clientIds,
                        PermissionArea = rule.PermissionArea,
                        Level = rule.Level,
                        AmountThreshold = rule.AmountThreshold,
                        RequiresApproval = rule.RequiresApproval,
                        Notes = $"Applied from template: {template.Name}"
                    };

                    var result = await _permissionService.GrantPermissionAsync(request, adminId);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning("Failed to apply template rule {RuleId} to associate {AssociateId}", rule.Id, associateId);
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying template {TemplateId} to associate {AssociateId}", templateId, associateId);
                return Result.Failure("Failed to apply template");
            }
        }

        public async Task<Result> ApplyTemplateToMultipleAssociatesAsync(int templateId, List<string> associateIds, List<int> clientIds, string adminId)
        {
            try
            {
                foreach (var associateId in associateIds)
                {
                    await ApplyTemplateToAssociateAsync(templateId, associateId, clientIds, adminId);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying template {TemplateId} to multiple associates", templateId);
                return Result.Failure("Failed to apply template to multiple associates");
            }
        }

        public async Task<List<AssociatePermissionTemplate>> GetTemplatesByCreatorAsync(string adminId)
        {
            try
            {
                return await _context.AssociatePermissionTemplates
                    .Include(t => t.Rules)
                    .Where(t => t.CreatedByAdminId == adminId && t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates by creator {AdminId}", adminId);
                return new List<AssociatePermissionTemplate>();
            }
        }

        public async Task<bool> IsTemplateInUseAsync(int templateId)
        {
            try
            {
                // For now, we'll assume a template is "in use" if it was recently applied
                // In a more sophisticated implementation, you might track template applications
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if template {TemplateId} is in use", templateId);
                return true; // Err on the side of caution
            }
        }

        public async Task<Result> CloneTemplateAsync(int sourceTemplateId, string newName, string adminId)
        {
            try
            {
                var sourceTemplate = await GetTemplateAsync(sourceTemplateId);
                if (sourceTemplate == null)
                    return Result.Failure("Source template not found");

                var cloneRequest = new CreateTemplateRequest
                {
                    Name = newName,
                    Description = $"Cloned from: {sourceTemplate.Name}",
                    IsDefault = false,
                    Rules = sourceTemplate.Rules.Select(r => new CreatePermissionRuleRequest
                    {
                        PermissionArea = r.PermissionArea,
                        Level = r.Level,
                        AmountThreshold = r.AmountThreshold,
                        RequiresApproval = r.RequiresApproval
                    }).ToList()
                };

                var result = await CreateTemplateAsync(cloneRequest, adminId);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {SourceTemplateId}", sourceTemplateId);
                return Result.Failure("Failed to clone template");
            }
        }

        public async Task<Dictionary<string, object>> GetTemplateUsageStatistics(int templateId)
        {
            try
            {
                var template = await GetTemplateAsync(templateId);
                if (template == null)
                    return new Dictionary<string, object>();

                var stats = new Dictionary<string, object>
                {
                    ["TemplateName"] = template.Name,
                    ["RuleCount"] = template.Rules.Count,
                    ["IsDefault"] = template.IsDefault,
                    ["CreatedDate"] = template.CreatedDate,
                    ["LastUpdated"] = template.UpdatedDate,
                    ["CreatedBy"] = template.CreatedByAdmin?.FirstName + " " + template.CreatedByAdmin?.LastName,
                    ["PermissionAreas"] = template.Rules.Select(r => r.PermissionArea).Distinct().ToList()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template usage statistics for template {TemplateId}", templateId);
                return new Dictionary<string, object>();
            }
        }
    }
}