using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Service for managing workflow templates
    /// </summary>
    public class WorkflowTemplateService : IWorkflowTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkflowRuleService _workflowRuleService;
        private readonly ILogger<WorkflowTemplateService> _logger;

        public WorkflowTemplateService(
            ApplicationDbContext context,
            IWorkflowRuleService workflowRuleService,
            ILogger<WorkflowTemplateService> logger)
        {
            _context = context;
            _workflowRuleService = workflowRuleService;
            _logger = logger;
        }

        public async Task<Result<WorkflowTemplateDto>> CreateTemplateAsync(CreateWorkflowTemplateDto request)
        {
            try
            {
                var template = new WorkflowTemplate
                {
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    TriggerType = request.TriggerType,
                    IsPublic = request.IsPublic,
                    CreatedBy = "system", // TODO: Get from user context
                    CreatedDate = DateTime.UtcNow,
                    UsageCount = 0,
                    Rating = 0,
                    TagsJson = JsonSerializer.Serialize(request.Tags),
                    DefinitionJson = JsonSerializer.Serialize(request.Definition)
                };

                _context.WorkflowRuleTemplates.Add(template);
                await _context.SaveChangesAsync();

                var result = await GetTemplateAsync(template.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow template: {TemplateName}", request.Name);
                return Result.Failure<WorkflowTemplateDto>($"Error creating workflow template: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowTemplateDto>> UpdateTemplateAsync(int templateId, UpdateWorkflowTemplateDto request)
        {
            try
            {
                var template = await _context.WorkflowRuleTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<WorkflowTemplateDto>("Workflow template not found");
                }

                template.Name = request.Name;
                template.Description = request.Description;
                template.Category = request.Category;
                template.IsPublic = request.IsPublic;
                template.LastModifiedDate = DateTime.UtcNow;
                template.LastModifiedBy = "system"; // TODO: Get from user context
                template.TagsJson = JsonSerializer.Serialize(request.Tags);
                template.DefinitionJson = JsonSerializer.Serialize(request.Definition);

                await _context.SaveChangesAsync();

                var result = await GetTemplateAsync(template.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow template: {TemplateId}", templateId);
                return Result.Failure<WorkflowTemplateDto>($"Error updating workflow template: {ex.Message}");
            }
        }

        public async Task<Result> DeleteTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.WorkflowRuleTemplates
                    .Include(t => t.Rules)
                    .Include(t => t.Reviews)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    return Result.Failure("Workflow template not found");
                }

                // Check if template is in use
                if (template.Rules.Any())
                {
                    return Result.Failure("Cannot delete template that is currently in use by active rules");
                }

                _context.WorkflowTemplateReviews.RemoveRange(template.Reviews);
                _context.WorkflowRuleTemplates.Remove(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted workflow template {TemplateId}: {TemplateName}", templateId, template.Name);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow template: {TemplateId}", templateId);
                return Result.Failure($"Error deleting workflow template: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowTemplateDto>> GetTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.WorkflowRuleTemplates
                    .Include(t => t.Reviews)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    return Result.Failure<WorkflowTemplateDto>("Workflow template not found");
                }

                var dto = MapToDto(template);
                return Result<WorkflowTemplateDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow template: {TemplateId}", templateId);
                return Result.Failure<WorkflowTemplateDto>($"Error getting workflow template: {ex.Message}");
            }
        }

        public async Task<Result<BettsTax.Shared.PagedResult<WorkflowTemplateDto>>> GetTemplatesAsync(WorkflowTemplateFilterDto filter)
        {
            try
            {
                var query = _context.WorkflowRuleTemplates
                    .Include(t => t.Reviews)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Name))
                {
                    query = query.Where(t => t.Name.Contains(filter.Name));
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(t => t.Category == filter.Category);
                }

                if (!string.IsNullOrEmpty(filter.TriggerType))
                {
                    query = query.Where(t => t.TriggerType == filter.TriggerType);
                }

                if (filter.IsPublic.HasValue)
                {
                    query = query.Where(t => t.IsPublic == filter.IsPublic.Value);
                }

                if (!string.IsNullOrEmpty(filter.CreatedBy))
                {
                    query = query.Where(t => t.CreatedBy == filter.CreatedBy);
                }

                if (filter.CreatedAfter.HasValue)
                {
                    query = query.Where(t => t.CreatedDate >= filter.CreatedAfter.Value);
                }

                if (filter.CreatedBefore.HasValue)
                {
                    query = query.Where(t => t.CreatedDate <= filter.CreatedBefore.Value);
                }

                if (filter.MinRating.HasValue)
                {
                    query = query.Where(t => t.Rating >= filter.MinRating.Value);
                }

                if (filter.Tags != null && filter.Tags.Any())
                {
                    foreach (var tag in filter.Tags)
                    {
                        query = query.Where(t => t.TagsJson.Contains($"\"{tag}\""));
                    }
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(filter.SortBy))
                {
                    query = filter.SortBy.ToLower() switch
                    {
                        "name" => filter.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                        "category" => filter.SortDescending ? query.OrderByDescending(t => t.Category) : query.OrderBy(t => t.Category),
                        "rating" => filter.SortDescending ? query.OrderByDescending(t => t.Rating) : query.OrderBy(t => t.Rating),
                        "usage" => filter.SortDescending ? query.OrderByDescending(t => t.UsageCount) : query.OrderBy(t => t.UsageCount),
                        "created" => filter.SortDescending ? query.OrderByDescending(t => t.CreatedDate) : query.OrderBy(t => t.CreatedDate),
                        _ => query.OrderByDescending(t => t.CreatedDate)
                    };
                }
                else
                {
                    query = query.OrderByDescending(t => t.CreatedDate);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var dtos = items.Select(MapToDto).ToList();

                var result = new BettsTax.Shared.PagedResult<WorkflowTemplateDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };

                return Result<BettsTax.Shared.PagedResult<WorkflowTemplateDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow templates");
                return Result.Failure<BettsTax.Shared.PagedResult<WorkflowTemplateDto>>($"Error getting workflow templates: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowRuleDto>> CreateRuleFromTemplateAsync(int templateId, CreateRuleFromTemplateDto request)
        {
            try
            {
                var template = await _context.WorkflowRuleTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<WorkflowRuleDto>("Workflow template not found");
                }

                var definition = JsonSerializer.Deserialize<WorkflowTemplateDefinitionDto>(template.DefinitionJson);
                if (definition == null)
                {
                    return Result.Failure<WorkflowRuleDto>("Invalid template definition");
                }

                // Create conditions from template
                var conditions = definition.Conditions.Select(tc => new CreateWorkflowConditionDto
                {
                    ConditionType = tc.ConditionType,
                    FieldName = tc.FieldName,
                    Operator = tc.Operator,
                    Value = tc.IsParameterized && !string.IsNullOrEmpty(tc.ParameterName) && request.Parameters.ContainsKey(tc.ParameterName)
                        ? request.Parameters[tc.ParameterName].ToString() ?? tc.Value
                        : tc.Value,
                    LogicalOperator = tc.LogicalOperator,
                    Order = tc.Order,
                    Metadata = tc.Metadata
                }).ToList();

                // Create actions from template
                var actions = definition.Actions.Select(ta => 
                {
                    var parameters = new Dictionary<string, object>(ta.Parameters);
                    
                    // Apply parameter mappings
                    foreach (var mapping in ta.ParameterMappings)
                    {
                        if (request.Parameters.ContainsKey(mapping.Value))
                        {
                            parameters[mapping.Key] = request.Parameters[mapping.Value];
                        }
                    }

                    return new CreateWorkflowActionDto
                    {
                        ActionType = ta.ActionType,
                        Parameters = parameters,
                        Order = ta.Order,
                        ContinueOnError = ta.ContinueOnError,
                        ErrorHandling = ta.ErrorHandling
                    };
                }).ToList();

                var createRuleDto = new CreateWorkflowRuleDto
                {
                    Name = request.RuleName,
                    Description = request.Description ?? $"Created from template: {template.Name}",
                    TriggerType = template.TriggerType,
                    IsActive = request.IsActive,
                    Priority = request.Priority,
                    Conditions = conditions,
                    Actions = actions
                };

                var result = await _workflowRuleService.CreateRuleAsync(createRuleDto);
                
                if (result.IsSuccess)
                {
                    // Update template usage count
                    template.UsageCount++;
                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rule from template: {TemplateId}", templateId);
                return Result.Failure<WorkflowRuleDto>($"Error creating rule from template: {ex.Message}");
            }
        }

        public async Task<Result<List<string>>> GetTemplateCategoriesAsync()
        {
            try
            {
                var categories = await _context.WorkflowRuleTemplates
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                // Add predefined categories that might not be in use yet
                var allCategories = WorkflowTemplateCategories.AllCategories
                    .Union(categories)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return Result<List<string>>.Success(allCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template categories");
                return Result.Failure<List<string>>($"Error getting template categories: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowTemplateDto>> ImportTemplateAsync(ImportWorkflowTemplateDto request)
        {
            try
            {
                var templateData = JsonSerializer.Deserialize<WorkflowTemplateDto>(request.JsonData);
                if (templateData == null)
                {
                    return Result.Failure<WorkflowTemplateDto>("Invalid template JSON data");
                }

                // Check if template with same name already exists
                var existingTemplate = await _context.WorkflowRuleTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateData.Name);

                if (existingTemplate != null && !request.OverwriteExisting)
                {
                    return Result.Failure<WorkflowTemplateDto>("Template with this name already exists");
                }

                WorkflowTemplate template;

                if (existingTemplate != null && request.OverwriteExisting)
                {
                    // Update existing template
                    template = existingTemplate;
                    template.Description = templateData.Description;
                    template.Category = !string.IsNullOrEmpty(request.NewCategory) ? request.NewCategory : templateData.Category;
                    template.TriggerType = templateData.TriggerType;
                    template.LastModifiedDate = DateTime.UtcNow;
                    template.LastModifiedBy = "system"; // TODO: Get from user context
                    template.TagsJson = JsonSerializer.Serialize(templateData.Tags);
                    template.DefinitionJson = JsonSerializer.Serialize(templateData.Definition);
                }
                else
                {
                    // Create new template
                    template = new WorkflowTemplate
                    {
                        Name = !string.IsNullOrEmpty(request.NewName) ? request.NewName : templateData.Name,
                        Description = templateData.Description,
                        Category = !string.IsNullOrEmpty(request.NewCategory) ? request.NewCategory : templateData.Category,
                        TriggerType = templateData.TriggerType,
                        IsPublic = templateData.IsPublic,
                        CreatedBy = "system", // TODO: Get from user context
                        CreatedDate = DateTime.UtcNow,
                        UsageCount = 0,
                        Rating = 0,
                        TagsJson = JsonSerializer.Serialize(templateData.Tags),
                        DefinitionJson = JsonSerializer.Serialize(templateData.Definition)
                    };
                    _context.WorkflowRuleTemplates.Add(template);
                }

                await _context.SaveChangesAsync();

                var result = await GetTemplateAsync(template.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing workflow template");
                return Result.Failure<WorkflowTemplateDto>($"Error importing workflow template: {ex.Message}");
            }
        }

        public async Task<Result<string>> ExportTemplateAsync(int templateId)
        {
            try
            {
                var templateResult = await GetTemplateAsync(templateId);
                if (!templateResult.IsSuccess || templateResult.Value == null)
                {
                    return Result.Failure<string>("Workflow template not found");
                }

                var jsonData = JsonSerializer.Serialize(templateResult.Value, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return Result<string>.Success(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting workflow template: {TemplateId}", templateId);
                return Result.Failure<string>($"Error exporting workflow template: {ex.Message}");
            }
        }

        #region Private Methods

        private WorkflowTemplateDto MapToDto(WorkflowTemplate template)
        {
            var tags = new List<string>();
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(template.TagsJson) ?? new List<string>();
            }
            catch
            {
                // Ignore JSON errors and use empty list
            }

            var definition = new WorkflowTemplateDefinitionDto();
            try
            {
                definition = JsonSerializer.Deserialize<WorkflowTemplateDefinitionDto>(template.DefinitionJson) ?? new WorkflowTemplateDefinitionDto();
            }
            catch
            {
                // Ignore JSON errors and use empty definition
            }

            return new WorkflowTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                TriggerType = template.TriggerType,
                IsPublic = template.IsPublic,
                CreatedBy = template.CreatedBy,
                CreatedDate = template.CreatedDate,
                LastModifiedDate = template.LastModifiedDate,
                LastModifiedBy = template.LastModifiedBy,
                UsageCount = template.UsageCount,
                Rating = template.Rating,
                Tags = tags,
                Definition = definition
            };
        }

        #endregion

        #region Predefined Templates

        /// <summary>
        /// Create predefined workflow templates for common scenarios
        /// </summary>
        public async Task<Result> CreatePredefinedTemplatesAsync()
        {
            try
            {
                var templates = GetPredefinedTemplates();
                
                foreach (var template in templates)
                {
                    var existing = await _context.WorkflowRuleTemplates
                        .FirstOrDefaultAsync(t => t.Name == template.Name);
                    
                    if (existing == null)
                    {
                        await CreateTemplateAsync(template);
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating predefined templates");
                return Result.Failure($"Error creating predefined templates: {ex.Message}");
            }
        }

        private List<CreateWorkflowTemplateDto> GetPredefinedTemplates()
        {
            return new List<CreateWorkflowTemplateDto>
            {
                // Tax Filing Notification Template
                new()
                {
                    Name = "Tax Filing Due Reminder",
                    Description = "Send reminder emails when tax filing deadlines are approaching",
                    Category = WorkflowTemplateCategories.TaxFiling,
                    TriggerType = WorkflowTriggerTypes.DeadlineApproaching,
                    IsPublic = true,
                    Tags = new() { "email", "reminder", "deadline", "tax" },
                    Definition = new()
                    {
                        Conditions = new()
                        {
                            new()
                            {
                                ConditionType = "DateComparison",
                                FieldName = "DaysUntilDeadline",
                                Operator = "lessthanorequal",
                                Value = "7",
                                LogicalOperator = "AND",
                                Order = 1,
                                IsParameterized = true,
                                ParameterName = "reminderDays"
                            }
                        },
                        Actions = new()
                        {
                            new()
                            {
                                ActionType = WorkflowActionTypes.SendEmail,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "template", "TaxFilingReminder" },
                                    { "subject", "Tax Filing Deadline Approaching" }
                                },
                                Order = 1,
                                ContinueOnError = true,
                                ParameterMappings = new() { { "to", "clientEmail" } }
                            }
                        },
                        Parameters = new()
                        {
                            { "reminderDays", new WorkflowTemplateParameterDto
                                {
                                    Name = "reminderDays",
                                    DisplayName = "Reminder Days",
                                    Description = "Number of days before deadline to send reminder",
                                    DataType = "number",
                                    Required = true,
                                    DefaultValue = 7,
                                    Order = 1
                                }
                            },
                            { "clientEmail", new WorkflowTemplateParameterDto
                                {
                                    Name = "clientEmail",
                                    DisplayName = "Client Email",
                                    Description = "Email address to send reminder to",
                                    DataType = "email",
                                    Required = true,
                                    Order = 2
                                }
                            }
                        }
                    }
                },

                // Payment Confirmation Template
                new()
                {
                    Name = "Payment Confirmation Workflow",
                    Description = "Send confirmation and update status when payment is received",
                    Category = WorkflowTemplateCategories.PaymentProcessing,
                    TriggerType = WorkflowTriggerTypes.PaymentReceived,
                    IsPublic = true,
                    Tags = new() { "payment", "confirmation", "sms", "status" },
                    Definition = new()
                    {
                        Conditions = new()
                        {
                            new()
                            {
                                ConditionType = "FieldComparison",
                                FieldName = "Status",
                                Operator = "equals",
                                Value = "Confirmed",
                                LogicalOperator = "AND",
                                Order = 1
                            }
                        },
                        Actions = new()
                        {
                            new()
                            {
                                ActionType = WorkflowActionTypes.SendSms,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "template", "PaymentConfirmation" },
                                    { "message", "Your payment has been received and confirmed. Thank you!" }
                                },
                                Order = 1,
                                ContinueOnError = true,
                                ParameterMappings = new() { { "phoneNumber", "clientPhone" } }
                            },
                            new()
                            {
                                ActionType = WorkflowActionTypes.UpdateStatus,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "entityType", "TaxFiling" },
                                    { "newStatus", "Paid" },
                                    { "reason", "Payment confirmed via workflow" }
                                },
                                Order = 2,
                                ContinueOnError = false
                            }
                        },
                        Parameters = new()
                        {
                            { "clientPhone", new WorkflowTemplateParameterDto
                                {
                                    Name = "clientPhone",
                                    DisplayName = "Client Phone Number",
                                    Description = "Phone number to send confirmation to",
                                    DataType = "phone",
                                    Required = true,
                                    Order = 1
                                }
                            }
                        }
                    }
                },

                // Document Upload Template
                new()
                {
                    Name = "Document Upload Notification",
                    Description = "Notify staff when important documents are uploaded",
                    Category = WorkflowTemplateCategories.DocumentManagement,
                    TriggerType = WorkflowTriggerTypes.DocumentUploaded,
                    IsPublic = true,
                    Tags = new() { "document", "notification", "staff" },
                    Definition = new()
                    {
                        Conditions = new()
                        {
                            new()
                            {
                                ConditionType = "ListContains",
                                FieldName = "DocumentType",
                                Operator = "in",
                                Value = "TaxReturn,FinancialStatement,BankStatement",
                                LogicalOperator = "AND",
                                Order = 1,
                                IsParameterized = true,
                                ParameterName = "documentTypes"
                            }
                        },
                        Actions = new()
                        {
                            new()
                            {
                                ActionType = WorkflowActionTypes.SendNotification,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "title", "Important Document Uploaded" },
                                    { "type", "info" }
                                },
                                Order = 1,
                                ContinueOnError = true,
                                ParameterMappings = new() 
                                { 
                                    { "userId", "staffUserId" },
                                    { "message", "documentMessage" }
                                }
                            },
                            new()
                            {
                                ActionType = WorkflowActionTypes.CreateTask,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "title", "Review Uploaded Document" },
                                    { "priority", "medium" }
                                },
                                Order = 2,
                                ContinueOnError = true,
                                ParameterMappings = new() { { "assignee", "reviewerUserId" } }
                            }
                        },
                        Parameters = new()
                        {
                            { "documentTypes", new WorkflowTemplateParameterDto
                                {
                                    Name = "documentTypes",
                                    DisplayName = "Document Types",
                                    Description = "Document types that trigger notifications",
                                    DataType = "list",
                                    Required = true,
                                    DefaultValue = new List<string> { "TaxReturn", "FinancialStatement" },
                                    Order = 1
                                }
                            },
                            { "staffUserId", new WorkflowTemplateParameterDto
                                {
                                    Name = "staffUserId",
                                    DisplayName = "Staff User ID",
                                    Description = "Staff member to notify",
                                    DataType = "user",
                                    Required = true,
                                    Order = 2
                                }
                            },
                            { "reviewerUserId", new WorkflowTemplateParameterDto
                                {
                                    Name = "reviewerUserId",
                                    DisplayName = "Reviewer User ID",
                                    Description = "Staff member to assign review task",
                                    DataType = "user",
                                    Required = true,
                                    Order = 3
                                }
                            },
                            { "documentMessage", new WorkflowTemplateParameterDto
                                {
                                    Name = "documentMessage",
                                    DisplayName = "Notification Message",
                                    Description = "Message to include in notification",
                                    DataType = "text",
                                    Required = false,
                                    DefaultValue = "A new document has been uploaded and requires review.",
                                    Order = 4
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion
    }
}
