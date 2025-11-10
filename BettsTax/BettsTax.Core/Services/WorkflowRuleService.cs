using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Service for managing workflow rules and conditions
    /// </summary>
    public class WorkflowRuleService : IWorkflowRuleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowRuleService> _logger;

        public WorkflowRuleService(
            ApplicationDbContext context,
            ILogger<WorkflowRuleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<WorkflowRuleDto>> CreateRuleAsync(CreateWorkflowRuleDto request)
        {
            try
            {
                var rule = new WorkflowRule
                {
                    Name = request.Name,
                    Description = request.Description,
                    TriggerType = request.TriggerType,
                    IsActive = request.IsActive,
                    Priority = request.Priority,
                    CreatedBy = "system", // TODO: Get from user context
                    CreatedDate = DateTime.UtcNow
                };

                _context.WorkflowRuleEntities.Add(rule);
                await _context.SaveChangesAsync();

                // Add conditions
                foreach (var conditionDto in request.Conditions)
                {
                    var condition = new WorkflowCondition
                    {
                        WorkflowRuleId = rule.Id,
                        ConditionType = conditionDto.ConditionType,
                        FieldName = conditionDto.FieldName,
                        Operator = conditionDto.Operator,
                        Value = conditionDto.Value,
                        LogicalOperator = conditionDto.LogicalOperator,
                        Order = conditionDto.Order,
                        MetadataJson = JsonSerializer.Serialize(conditionDto.Metadata)
                    };
                    _context.WorkflowConditions.Add(condition);
                }

                // Add actions
                foreach (var actionDto in request.Actions)
                {
                    var action = new WorkflowAction
                    {
                        WorkflowRuleId = rule.Id,
                        ActionType = actionDto.ActionType,
                        ParametersJson = JsonSerializer.Serialize(actionDto.Parameters),
                        Order = actionDto.Order,
                        ContinueOnError = actionDto.ContinueOnError,
                        ErrorHandling = actionDto.ErrorHandling
                    };
                    _context.WorkflowActions.Add(action);
                }

                await _context.SaveChangesAsync();

                var result = await GetRuleAsync(rule.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow rule: {RuleName}", request.Name);
                return Result.Failure<WorkflowRuleDto>($"Error creating workflow rule: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowRuleDto>> UpdateRuleAsync(int ruleId, UpdateWorkflowRuleDto request)
        {
            try
            {
                var rule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return Result.Failure<WorkflowRuleDto>("Workflow rule not found");
                }

                // Update rule properties
                rule.Name = request.Name;
                rule.Description = request.Description;
                rule.IsActive = request.IsActive;
                rule.Priority = request.Priority;
                rule.LastModifiedDate = DateTime.UtcNow;
                rule.LastModifiedBy = "system"; // TODO: Get from user context

                // Remove existing conditions and actions
                _context.WorkflowConditions.RemoveRange(rule.Conditions);
                _context.WorkflowActions.RemoveRange(rule.Actions);

                // Add new conditions
                foreach (var conditionDto in request.Conditions)
                {
                    var condition = new WorkflowCondition
                    {
                        WorkflowRuleId = rule.Id,
                        ConditionType = conditionDto.ConditionType,
                        FieldName = conditionDto.FieldName,
                        Operator = conditionDto.Operator,
                        Value = conditionDto.Value,
                        LogicalOperator = conditionDto.LogicalOperator,
                        Order = conditionDto.Order,
                        MetadataJson = JsonSerializer.Serialize(conditionDto.Metadata)
                    };
                    _context.WorkflowConditions.Add(condition);
                }

                // Add new actions
                foreach (var actionDto in request.Actions)
                {
                    var action = new WorkflowAction
                    {
                        WorkflowRuleId = rule.Id,
                        ActionType = actionDto.ActionType,
                        ParametersJson = JsonSerializer.Serialize(actionDto.Parameters),
                        Order = actionDto.Order,
                        ContinueOnError = actionDto.ContinueOnError,
                        ErrorHandling = actionDto.ErrorHandling
                    };
                    _context.WorkflowActions.Add(action);
                }

                await _context.SaveChangesAsync();

                var result = await GetRuleAsync(rule.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow rule: {RuleId}", ruleId);
                return Result.Failure<WorkflowRuleDto>($"Error updating workflow rule: {ex.Message}");
            }
        }

        public async Task<Result> DeleteRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .Include(r => r.ExecutionHistory)
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return Result.Failure("Workflow rule not found");
                }

                // Remove related data
                _context.WorkflowConditions.RemoveRange(rule.Conditions);
                _context.WorkflowActions.RemoveRange(rule.Actions);
                _context.WorkflowExecutionHistories.RemoveRange(rule.ExecutionHistory);
                _context.WorkflowRuleEntities.Remove(rule);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted workflow rule {RuleId}: {RuleName}", ruleId, rule.Name);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow rule: {RuleId}", ruleId);
                return Result.Failure($"Error deleting workflow rule: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowRuleDto>> GetRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions.OrderBy(c => c.Order))
                    .Include(r => r.Actions.OrderBy(a => a.Order))
                    .Include(r => r.Metrics)
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return Result.Failure<WorkflowRuleDto>("Workflow rule not found");
                }

                var dto = MapToDto(rule);
                return Result<WorkflowRuleDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow rule: {RuleId}", ruleId);
                return Result.Failure<WorkflowRuleDto>($"Error getting workflow rule: {ex.Message}");
            }
        }

        public async Task<Result<BettsTax.Shared.PagedResult<WorkflowRuleDto>>> GetRulesAsync(WorkflowRuleFilterDto filter)
        {
            try
            {
                var query = _context.WorkflowRuleEntities
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .Include(r => r.Metrics)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Name))
                {
                    query = query.Where(r => r.Name.Contains(filter.Name));
                }

                if (!string.IsNullOrEmpty(filter.TriggerType))
                {
                    query = query.Where(r => r.TriggerType == filter.TriggerType);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(r => r.IsActive == filter.IsActive.Value);
                }

                if (filter.CreatedAfter.HasValue)
                {
                    query = query.Where(r => r.CreatedDate >= filter.CreatedAfter.Value);
                }

                if (filter.CreatedBefore.HasValue)
                {
                    query = query.Where(r => r.CreatedDate <= filter.CreatedBefore.Value);
                }

                if (!string.IsNullOrEmpty(filter.CreatedBy))
                {
                    query = query.Where(r => r.CreatedBy == filter.CreatedBy);
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(filter.SortBy))
                {
                    var sortDirection = filter.SortDescending ? "desc" : "asc";
                    query = query.OrderBy($"{filter.SortBy} {sortDirection}");
                }
                else
                {
                    query = query.OrderByDescending(r => r.CreatedDate);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var dtos = items.Select(MapToDto).ToList();

                var result = new BettsTax.Shared.PagedResult<WorkflowRuleDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };

                return Result<BettsTax.Shared.PagedResult<WorkflowRuleDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow rules with filter");
                return Result.Failure<BettsTax.Shared.PagedResult<WorkflowRuleDto>>($"Error getting workflow rules: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowRuleTestResultDto>> TestRuleAsync(int ruleId, object testData)
        {
            try
            {
                var rule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions.OrderBy(c => c.Order))
                    .Include(r => r.Actions.OrderBy(a => a.Order))
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return Result.Failure<WorkflowRuleTestResultDto>("Workflow rule not found");
                }

                var startTime = DateTime.UtcNow;
                var result = new WorkflowRuleTestResultDto
                {
                    Success = true,
                    ConditionResults = new List<WorkflowConditionResultDto>(),
                    ActionResults = new List<WorkflowActionResultDto>()
                };

                // Evaluate conditions
                var conditionsMatched = await EvaluateConditionsAsync(rule.Conditions, testData, result);
                result.ConditionsMatched = conditionsMatched;

                // If conditions matched, simulate actions
                if (conditionsMatched)
                {
                    await SimulateActionsAsync(rule.Actions, testData, result);
                }

                result.ExecutionTime = DateTime.UtcNow - startTime;
                return Result<WorkflowRuleTestResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing workflow rule: {RuleId}", ruleId);
                return Result.Failure<WorkflowRuleTestResultDto>($"Error testing workflow rule: {ex.Message}");
            }
        }

        public async Task<Result<List<WorkflowActionDto>>> EvaluateRulesAsync(string triggerType, object data)
        {
            try
            {
                var rules = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions.OrderBy(c => c.Order))
                    .Include(r => r.Actions.OrderBy(a => a.Order))
                    .Where(r => r.TriggerType == triggerType && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                var actionsToExecute = new List<WorkflowActionDto>();

                foreach (var rule in rules)
                {
                    var conditionsMatched = await EvaluateConditionsAsync(rule.Conditions, data);
                    if (conditionsMatched)
                    {
                        var actions = rule.Actions.Select(a => new WorkflowActionDto
                        {
                            Id = a.Id,
                            ActionType = a.ActionType,
                            Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(a.ParametersJson) ?? new(),
                            Order = a.Order,
                            ContinueOnError = a.ContinueOnError,
                            ErrorHandling = a.ErrorHandling
                        }).ToList();

                        actionsToExecute.AddRange(actions);
                    }
                }

                return Result<List<WorkflowActionDto>>.Success(actionsToExecute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rules for trigger: {TriggerType}", triggerType);
                return Result.Failure<List<WorkflowActionDto>>($"Error evaluating rules: {ex.Message}");
            }
        }

        public async Task<Result<List<WorkflowTriggerTypeDto>>> GetAvailableTriggersAsync()
        {
            try
            {
                var triggerTypes = new List<WorkflowTriggerTypeDto>
                {
                    new() {
                        Type = WorkflowTriggerTypes.TaxFilingCreated,
                        Name = "Tax Filing Created",
                        Description = "Triggered when a new tax filing is created",
                        AvailableFields = GetTaxFilingFields(),
                        SupportedEvents = new() { "Created" }
                    },
                    new() {
                        Type = WorkflowTriggerTypes.PaymentReceived,
                        Name = "Payment Received",
                        Description = "Triggered when a payment is received",
                        AvailableFields = GetPaymentFields(),
                        SupportedEvents = new() { "Received", "Confirmed" }
                    },
                    new() {
                        Type = WorkflowTriggerTypes.DocumentUploaded,
                        Name = "Document Uploaded",
                        Description = "Triggered when a document is uploaded",
                        AvailableFields = GetDocumentFields(),
                        SupportedEvents = new() { "Uploaded", "Verified" }
                    },
                    new() {
                        Type = WorkflowTriggerTypes.DeadlineApproaching,
                        Name = "Deadline Approaching",
                        Description = "Triggered when a deadline is approaching",
                        AvailableFields = GetDeadlineFields(),
                        SupportedEvents = new() { "Warning", "Critical" }
                    },
                    new() {
                        Type = WorkflowTriggerTypes.ClientRegistered,
                        Name = "Client Registered",
                        Description = "Triggered when a new client registers",
                        AvailableFields = GetClientFields(),
                        SupportedEvents = new() { "Registered", "Verified" }
                    }
                };

                return Result<List<WorkflowTriggerTypeDto>>.Success(triggerTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available triggers");
                return Result.Failure<List<WorkflowTriggerTypeDto>>($"Error getting available triggers: {ex.Message}");
            }
        }

        public async Task<Result<List<WorkflowConditionTypeDto>>> GetConditionTypesAsync(string triggerType)
        {
            try
            {
                var conditionTypes = new List<WorkflowConditionTypeDto>
                {
                    new() {
                        Type = "FieldComparison",
                        Name = "Field Comparison",
                        Description = "Compare a field value with another value",
                        SupportedOperators = GetComparisonOperators()
                    },
                    new() {
                        Type = "DateComparison",
                        Name = "Date Comparison",
                        Description = "Compare date values",
                        SupportedOperators = GetDateOperators()
                    },
                    new() {
                        Type = "NumericRange",
                        Name = "Numeric Range",
                        Description = "Check if a numeric value is within a range",
                        SupportedOperators = GetNumericOperators()
                    },
                    new() {
                        Type = "TextPattern",
                        Name = "Text Pattern",
                        Description = "Match text against a pattern",
                        SupportedOperators = GetTextOperators()
                    },
                    new() {
                        Type = "ListContains",
                        Name = "List Contains",
                        Description = "Check if a value exists in a list",
                        SupportedOperators = GetListOperators()
                    }
                };

                return Result<List<WorkflowConditionTypeDto>>.Success(conditionTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting condition types for trigger: {TriggerType}", triggerType);
                return Result.Failure<List<WorkflowConditionTypeDto>>($"Error getting condition types: {ex.Message}");
            }
        }

        public async Task<Result<List<WorkflowActionTypeDto>>> GetActionTypesAsync()
        {
            try
            {
                var actionTypes = new List<WorkflowActionTypeDto>
                {
                    new() {
                        Type = WorkflowActionTypes.SendEmail,
                        Name = "Send Email",
                        Description = "Send an email notification",
                        Parameters = GetEmailActionParameters(),
                        RequiresApproval = false
                    },
                    new() {
                        Type = WorkflowActionTypes.SendSms,
                        Name = "Send SMS",
                        Description = "Send an SMS notification",
                        Parameters = GetSmsActionParameters(),
                        RequiresApproval = false
                    },
                    new() {
                        Type = WorkflowActionTypes.SendNotification,
                        Name = "Send In-App Notification",
                        Description = "Send an in-app notification",
                        Parameters = GetNotificationActionParameters(),
                        RequiresApproval = false
                    },
                    new() {
                        Type = WorkflowActionTypes.CreateTask,
                        Name = "Create Task",
                        Description = "Create a new task or reminder",
                        Parameters = GetTaskActionParameters(),
                        RequiresApproval = false
                    },
                    new() {
                        Type = WorkflowActionTypes.UpdateStatus,
                        Name = "Update Status",
                        Description = "Update the status of a record",
                        Parameters = GetStatusActionParameters(),
                        RequiresApproval = true
                    },
                    new() {
                        Type = WorkflowActionTypes.GenerateReport,
                        Name = "Generate Report",
                        Description = "Generate and send a report",
                        Parameters = GetReportActionParameters(),
                        RequiresApproval = false
                    },
                    new() {
                        Type = WorkflowActionTypes.CallWebhook,
                        Name = "Call Webhook",
                        Description = "Make an HTTP request to an external endpoint",
                        Parameters = GetWebhookActionParameters(),
                        RequiresApproval = true
                    }
                };

                return Result<List<WorkflowActionTypeDto>>.Success(actionTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting action types");
                return Result.Failure<List<WorkflowActionTypeDto>>($"Error getting action types: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowRuleDto>> CloneRuleAsync(int ruleId, string newName)
        {
            try
            {
                var originalRule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions)
                    .Include(r => r.Actions)
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (originalRule == null)
                {
                    return Result.Failure<WorkflowRuleDto>("Original workflow rule not found");
                }

                var createDto = new CreateWorkflowRuleDto
                {
                    Name = newName,
                    Description = $"Cloned from: {originalRule.Name}",
                    TriggerType = originalRule.TriggerType,
                    IsActive = false, // Start cloned rules as inactive
                    Priority = originalRule.Priority,
                    Conditions = originalRule.Conditions.Select(c => new CreateWorkflowConditionDto
                    {
                        ConditionType = c.ConditionType,
                        FieldName = c.FieldName,
                        Operator = c.Operator,
                        Value = c.Value,
                        LogicalOperator = c.LogicalOperator,
                        Order = c.Order,
                        Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(c.MetadataJson) ?? new()
                    }).ToList(),
                    Actions = originalRule.Actions.Select(a => new CreateWorkflowActionDto
                    {
                        ActionType = a.ActionType,
                        Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(a.ParametersJson) ?? new(),
                        Order = a.Order,
                        ContinueOnError = a.ContinueOnError,
                        ErrorHandling = a.ErrorHandling
                    }).ToList()
                };

                return await CreateRuleAsync(createDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning workflow rule: {RuleId}", ruleId);
                return Result.Failure<WorkflowRuleDto>($"Error cloning workflow rule: {ex.Message}");
            }
        }

        public async Task<Result> ToggleRuleStatusAsync(int ruleId, bool isActive)
        {
            try
            {
                var rule = await _context.WorkflowRuleEntities.FindAsync(ruleId);
                if (rule == null)
                {
                    return Result.Failure("Workflow rule not found");
                }

                rule.IsActive = isActive;
                rule.LastModifiedDate = DateTime.UtcNow;
                rule.LastModifiedBy = "system"; // TODO: Get from user context

                await _context.SaveChangesAsync();

                _logger.LogInformation("Toggled workflow rule {RuleId} status to {IsActive}", ruleId, isActive);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling workflow rule status: {RuleId}", ruleId);
                return Result.Failure($"Error toggling workflow rule status: {ex.Message}");
            }
        }

        public async Task<Result<BettsTax.Shared.PagedResult<WorkflowExecutionHistoryDto>>> GetExecutionHistoryAsync(
            WorkflowExecutionHistoryFilterDto filter)
        {
            try
            {
                var query = _context.WorkflowExecutionHistories
                    .Include(h => h.ActionExecutions)
                    .AsQueryable();

                // Apply filters
                if (filter.RuleId.HasValue)
                {
                    query = query.Where(h => h.RuleId == filter.RuleId.Value);
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(h => h.Status == filter.Status);
                }

                if (!string.IsNullOrEmpty(filter.TriggerType))
                {
                    query = query.Where(h => h.TriggerType == filter.TriggerType);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(h => h.StartTime >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(h => h.StartTime <= filter.EndDate.Value);
                }

                if (!string.IsNullOrEmpty(filter.ExecutedBy))
                {
                    query = query.Where(h => h.ExecutedBy == filter.ExecutedBy);
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(filter.SortBy))
                {
                    var sortDirection = filter.SortDescending ? "desc" : "asc";
                    query = query.OrderBy($"{filter.SortBy} {sortDirection}");
                }
                else
                {
                    query = query.OrderByDescending(h => h.StartTime);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var dtos = items.Select(h => new WorkflowExecutionHistoryDto
                {
                    Id = h.Id,
                    RuleId = h.RuleId,
                    RuleName = h.RuleName,
                    Status = h.Status,
                    StartTime = h.StartTime,
                    EndTime = h.EndTime,
                    Duration = h.Duration,
                    TriggerType = h.TriggerType,
                    TriggerData = JsonSerializer.Deserialize<object>(h.TriggerDataJson),
                    ErrorMessage = h.ErrorMessage,
                    ExecutedBy = h.ExecutedBy,
                    ActionResults = h.ActionExecutions.Select(ae => new WorkflowActionResultDto
                    {
                        ActionId = ae.ActionId,
                        ActionType = ae.ActionType,
                        Success = ae.Status == "Success",
                        Result = ae.Result,
                        ErrorMessage = ae.ErrorMessage,
                        ExecutionTime = ae.Duration ?? TimeSpan.Zero
                    }).ToList()
                }).ToList();

                var result = new BettsTax.Shared.PagedResult<WorkflowExecutionHistoryDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };

                return Result<BettsTax.Shared.PagedResult<WorkflowExecutionHistoryDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow execution history");
                return Result.Failure<BettsTax.Shared.PagedResult<WorkflowExecutionHistoryDto>>($"Error getting execution history: {ex.Message}");
            }
        }

        #region Private Methods

        private WorkflowRuleDto MapToDto(WorkflowRule rule)
        {
            var metrics = rule.Metrics.FirstOrDefault() ?? new WorkflowRuleMetrics();

            return new WorkflowRuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                TriggerType = rule.TriggerType,
                IsActive = rule.IsActive,
                Priority = rule.Priority,
                CreatedDate = rule.CreatedDate,
                LastModifiedDate = rule.LastModifiedDate,
                CreatedBy = rule.CreatedBy,
                LastModifiedBy = rule.LastModifiedBy,
                Conditions = rule.Conditions.Select(c => new WorkflowConditionDto
                {
                    Id = c.Id,
                    ConditionType = c.ConditionType,
                    FieldName = c.FieldName,
                    Operator = c.Operator,
                    Value = c.Value,
                    LogicalOperator = c.LogicalOperator,
                    Order = c.Order,
                    Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(c.MetadataJson) ?? new()
                }).OrderBy(c => c.Order).ToList(),
                Actions = rule.Actions.Select(a => new WorkflowActionDto
                {
                    Id = a.Id,
                    ActionType = a.ActionType,
                    Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(a.ParametersJson) ?? new(),
                    Order = a.Order,
                    ContinueOnError = a.ContinueOnError,
                    ErrorHandling = a.ErrorHandling
                }).OrderBy(a => a.Order).ToList(),
                Metrics = new WorkflowRuleMetricsDto
                {
                    TotalExecutions = metrics.TotalExecutions,
                    SuccessfulExecutions = metrics.SuccessfulExecutions,
                    FailedExecutions = metrics.FailedExecutions,
                    SuccessRate = metrics.SuccessRate,
                    AverageExecutionTime = metrics.AverageExecutionTime
                }
            };
        }

        private async Task<bool> EvaluateConditionsAsync(
            ICollection<WorkflowCondition> conditions, 
            object data, 
            WorkflowRuleTestResultDto? testResult = null)
        {
            if (!conditions.Any())
                return true;

            var dataDict = ConvertToExpandoObject(data);
            var results = new List<bool>();

            foreach (var condition in conditions.OrderBy(c => c.Order))
            {
                var conditionResult = new WorkflowConditionResultDto
                {
                    ConditionId = condition.Id,
                    ConditionType = condition.ConditionType,
                    ExpectedValue = condition.Value
                };

                try
                {
                    var fieldValue = GetFieldValue(dataDict, condition.FieldName);
                    conditionResult.ActualValue = fieldValue?.ToString() ?? "";

                    var matched = EvaluateCondition(condition, fieldValue);
                    conditionResult.Matched = matched;
                    results.Add(matched);

                    testResult?.ConditionResults.Add(conditionResult);
                }
                catch (Exception ex)
                {
                    conditionResult.Matched = false;
                    conditionResult.ErrorMessage = ex.Message;
                    testResult?.ConditionResults.Add(conditionResult);
                    results.Add(false);
                }
            }

            // Evaluate logical operators (simplified - assumes left-to-right evaluation)
            if (results.Count == 1)
                return results[0];

            var finalResult = results[0];
            for (int i = 1; i < results.Count; i++)
            {
                var logicalOp = conditions.ElementAt(i).LogicalOperator;
                finalResult = logicalOp.ToUpper() switch
                {
                    "OR" => finalResult || results[i],
                    _ => finalResult && results[i] // Default to AND
                };
            }

            return finalResult;
        }

        private bool EvaluateCondition(WorkflowCondition condition, object? fieldValue)
        {
            return condition.Operator.ToUpper() switch
            {
                "EQUALS" => IsEqual(fieldValue, condition.Value),
                "NOTEQUALS" => !IsEqual(fieldValue, condition.Value),
                "CONTAINS" => fieldValue?.ToString()?.Contains(condition.Value) ?? false,
                "STARTSWITH" => fieldValue?.ToString()?.StartsWith(condition.Value) ?? false,
                "ENDSWITH" => fieldValue?.ToString()?.EndsWith(condition.Value) ?? false,
                "GREATERTHAN" => IsGreaterThan(fieldValue, condition.Value),
                "LESSTHAN" => IsLessThan(fieldValue, condition.Value),
                "GREATERTHANOREQUAL" => IsGreaterThanOrEqual(fieldValue, condition.Value),
                "LESSTHANOREQUAL" => IsLessThanOrEqual(fieldValue, condition.Value),
                "ISNULL" => fieldValue == null,
                "ISNOTNULL" => fieldValue != null,
                _ => false
            };
        }

        private async Task SimulateActionsAsync(
            ICollection<WorkflowAction> actions, 
            object data, 
            WorkflowRuleTestResultDto testResult)
        {
            foreach (var action in actions.OrderBy(a => a.Order))
            {
                var actionResult = new WorkflowActionResultDto
                {
                    ActionId = action.Id,
                    ActionType = action.ActionType,
                    Success = true,
                    Result = "Simulated execution - would execute in real scenario"
                };

                testResult.ActionResults.Add(actionResult);
            }
        }

        private ExpandoObject ConvertToExpandoObject(object data)
        {
            if (data is ExpandoObject expando)
                return expando;

            var result = new ExpandoObject();
            var dict = result as IDictionary<string, object>;

            if (data is Dictionary<string, object> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    dict[prop.Name] = prop.GetValue(data) ?? new object();
                }
            }

            return result;
        }

        private object? GetFieldValue(ExpandoObject data, string fieldName)
        {
            var dict = data as IDictionary<string, object>;
            
            // Support nested field access with dot notation
            var parts = fieldName.Split('.');
            object? current = dict;

            foreach (var part in parts)
            {
                if (current is IDictionary<string, object> currentDict)
                {
                    if (currentDict.ContainsKey(part))
                    {
                        current = currentDict[part];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        private bool IsEqual(object? value1, string value2)
        {
            return value1?.ToString() == value2;
        }

        private bool IsGreaterThan(object? value1, string value2)
        {
            if (decimal.TryParse(value1?.ToString(), out var num1) && 
                decimal.TryParse(value2, out var num2))
            {
                return num1 > num2;
            }

            if (DateTime.TryParse(value1?.ToString(), out var date1) && 
                DateTime.TryParse(value2, out var date2))
            {
                return date1 > date2;
            }

            return string.Compare(value1?.ToString(), value2) > 0;
        }

        private bool IsLessThan(object? value1, string value2)
        {
            if (decimal.TryParse(value1?.ToString(), out var num1) && 
                decimal.TryParse(value2, out var num2))
            {
                return num1 < num2;
            }

            if (DateTime.TryParse(value1?.ToString(), out var date1) && 
                DateTime.TryParse(value2, out var date2))
            {
                return date1 < date2;
            }

            return string.Compare(value1?.ToString(), value2) < 0;
        }

        private bool IsGreaterThanOrEqual(object? value1, string value2)
        {
            return IsGreaterThan(value1, value2) || IsEqual(value1, value2);
        }

        private bool IsLessThanOrEqual(object? value1, string value2)
        {
            return IsLessThan(value1, value2) || IsEqual(value1, value2);
        }

        #endregion

        #region Field Definitions

        private List<WorkflowFieldDto> GetTaxFilingFields()
        {
            return new List<WorkflowFieldDto>
            {
                new() { Name = "ClientId", DisplayName = "Client ID", DataType = "number", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "Status", DisplayName = "Status", DataType = "string", SupportedOperators = new() { "equals", "notequals", "contains" } },
                new() { Name = "TaxYear", DisplayName = "Tax Year", DataType = "number", SupportedOperators = new() { "equals", "greaterthan", "lessthan" } },
                new() { Name = "Amount", DisplayName = "Amount", DataType = "decimal", SupportedOperators = new() { "equals", "greaterthan", "lessthan", "between" } },
                new() { Name = "DueDate", DisplayName = "Due Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } },
                new() { Name = "CreatedDate", DisplayName = "Created Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } }
            };
        }

        private List<WorkflowFieldDto> GetPaymentFields()
        {
            return new List<WorkflowFieldDto>
            {
                new() { Name = "Amount", DisplayName = "Amount", DataType = "decimal", SupportedOperators = new() { "equals", "greaterthan", "lessthan", "between" } },
                new() { Name = "Currency", DisplayName = "Currency", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "PaymentMethod", DisplayName = "Payment Method", DataType = "string", SupportedOperators = new() { "equals", "notequals", "contains" } },
                new() { Name = "Status", DisplayName = "Status", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "ClientId", DisplayName = "Client ID", DataType = "number", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "TransactionDate", DisplayName = "Transaction Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } }
            };
        }

        private List<WorkflowFieldDto> GetDocumentFields()
        {
            return new List<WorkflowFieldDto>
            {
                new() { Name = "DocumentType", DisplayName = "Document Type", DataType = "string", SupportedOperators = new() { "equals", "notequals", "contains" } },
                new() { Name = "ClientId", DisplayName = "Client ID", DataType = "number", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "FileSize", DisplayName = "File Size", DataType = "number", SupportedOperators = new() { "equals", "greaterthan", "lessthan" } },
                new() { Name = "FileExtension", DisplayName = "File Extension", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "UploadDate", DisplayName = "Upload Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } },
                new() { Name = "Status", DisplayName = "Status", DataType = "string", SupportedOperators = new() { "equals", "notequals" } }
            };
        }

        private List<WorkflowFieldDto> GetDeadlineFields()
        {
            return new List<WorkflowFieldDto>
            {
                new() { Name = "DeadlineDate", DisplayName = "Deadline Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } },
                new() { Name = "DaysUntilDeadline", DisplayName = "Days Until Deadline", DataType = "number", SupportedOperators = new() { "equals", "greaterthan", "lessthan" } },
                new() { Name = "DeadlineType", DisplayName = "Deadline Type", DataType = "string", SupportedOperators = new() { "equals", "notequals", "contains" } },
                new() { Name = "ClientId", DisplayName = "Client ID", DataType = "number", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "Priority", DisplayName = "Priority", DataType = "string", SupportedOperators = new() { "equals", "notequals" } }
            };
        }

        private List<WorkflowFieldDto> GetClientFields()
        {
            return new List<WorkflowFieldDto>
            {
                new() { Name = "ClientType", DisplayName = "Client Type", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "Status", DisplayName = "Status", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "RegistrationDate", DisplayName = "Registration Date", DataType = "datetime", SupportedOperators = new() { "equals", "before", "after", "between" } },
                new() { Name = "Country", DisplayName = "Country", DataType = "string", SupportedOperators = new() { "equals", "notequals" } },
                new() { Name = "Industry", DisplayName = "Industry", DataType = "string", SupportedOperators = new() { "equals", "notequals", "contains" } },
                new() { Name = "AnnualRevenue", DisplayName = "Annual Revenue", DataType = "decimal", SupportedOperators = new() { "equals", "greaterthan", "lessthan", "between" } }
            };
        }

        #endregion

        #region Operator Definitions

        private List<WorkflowOperatorDto> GetComparisonOperators()
        {
            return new List<WorkflowOperatorDto>
            {
                new() { Operator = "equals", DisplayName = "Equals", RequiresValue = true, ValueType = "string" },
                new() { Operator = "notequals", DisplayName = "Not Equals", RequiresValue = true, ValueType = "string" },
                new() { Operator = "contains", DisplayName = "Contains", RequiresValue = true, ValueType = "string" },
                new() { Operator = "startswith", DisplayName = "Starts With", RequiresValue = true, ValueType = "string" },
                new() { Operator = "endswith", DisplayName = "Ends With", RequiresValue = true, ValueType = "string" },
                new() { Operator = "isnull", DisplayName = "Is Null", RequiresValue = false },
                new() { Operator = "isnotnull", DisplayName = "Is Not Null", RequiresValue = false }
            };
        }

        private List<WorkflowOperatorDto> GetDateOperators()
        {
            return new List<WorkflowOperatorDto>
            {
                new() { Operator = "equals", DisplayName = "Equals", RequiresValue = true, ValueType = "datetime" },
                new() { Operator = "before", DisplayName = "Before", RequiresValue = true, ValueType = "datetime" },
                new() { Operator = "after", DisplayName = "After", RequiresValue = true, ValueType = "datetime" },
                new() { Operator = "between", DisplayName = "Between", RequiresValue = true, ValueType = "daterange" },
                new() { Operator = "today", DisplayName = "Today", RequiresValue = false },
                new() { Operator = "yesterday", DisplayName = "Yesterday", RequiresValue = false },
                new() { Operator = "thisweek", DisplayName = "This Week", RequiresValue = false },
                new() { Operator = "thismonth", DisplayName = "This Month", RequiresValue = false }
            };
        }

        private List<WorkflowOperatorDto> GetNumericOperators()
        {
            return new List<WorkflowOperatorDto>
            {
                new() { Operator = "equals", DisplayName = "Equals", RequiresValue = true, ValueType = "number" },
                new() { Operator = "notequals", DisplayName = "Not Equals", RequiresValue = true, ValueType = "number" },
                new() { Operator = "greaterthan", DisplayName = "Greater Than", RequiresValue = true, ValueType = "number" },
                new() { Operator = "lessthan", DisplayName = "Less Than", RequiresValue = true, ValueType = "number" },
                new() { Operator = "greaterthanorequal", DisplayName = "Greater Than or Equal", RequiresValue = true, ValueType = "number" },
                new() { Operator = "lessthanorequal", DisplayName = "Less Than or Equal", RequiresValue = true, ValueType = "number" },
                new() { Operator = "between", DisplayName = "Between", RequiresValue = true, ValueType = "numberrange" }
            };
        }

        private List<WorkflowOperatorDto> GetTextOperators()
        {
            return new List<WorkflowOperatorDto>
            {
                new() { Operator = "matches", DisplayName = "Matches Pattern", RequiresValue = true, ValueType = "regex" },
                new() { Operator = "contains", DisplayName = "Contains", RequiresValue = true, ValueType = "string" },
                new() { Operator = "startswith", DisplayName = "Starts With", RequiresValue = true, ValueType = "string" },
                new() { Operator = "endswith", DisplayName = "Ends With", RequiresValue = true, ValueType = "string" },
                new() { Operator = "length", DisplayName = "Length Equals", RequiresValue = true, ValueType = "number" },
                new() { Operator = "empty", DisplayName = "Is Empty", RequiresValue = false }
            };
        }

        private List<WorkflowOperatorDto> GetListOperators()
        {
            return new List<WorkflowOperatorDto>
            {
                new() { Operator = "in", DisplayName = "In List", RequiresValue = true, ValueType = "list" },
                new() { Operator = "notin", DisplayName = "Not In List", RequiresValue = true, ValueType = "list" },
                new() { Operator = "contains", DisplayName = "Contains Any", RequiresValue = true, ValueType = "list" },
                new() { Operator = "containsall", DisplayName = "Contains All", RequiresValue = true, ValueType = "list" }
            };
        }

        #endregion

        #region Action Parameter Definitions

        private List<WorkflowActionParameterDto> GetEmailActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "to", DisplayName = "To Email Address", DataType = "email", Required = true },
                new() { Name = "subject", DisplayName = "Subject", DataType = "string", Required = true },
                new() { Name = "body", DisplayName = "Body", DataType = "html", Required = true },
                new() { Name = "cc", DisplayName = "CC Email Addresses", DataType = "emaillist", Required = false },
                new() { Name = "bcc", DisplayName = "BCC Email Addresses", DataType = "emaillist", Required = false },
                new() { Name = "template", DisplayName = "Email Template", DataType = "select", Required = false }
            };
        }

        private List<WorkflowActionParameterDto> GetSmsActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "phoneNumber", DisplayName = "Phone Number", DataType = "phone", Required = true },
                new() { Name = "message", DisplayName = "Message", DataType = "text", Required = true },
                new() { Name = "template", DisplayName = "SMS Template", DataType = "select", Required = false }
            };
        }

        private List<WorkflowActionParameterDto> GetNotificationActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "userId", DisplayName = "User ID", DataType = "string", Required = true },
                new() { Name = "title", DisplayName = "Title", DataType = "string", Required = true },
                new() { Name = "message", DisplayName = "Message", DataType = "text", Required = true },
                new() { Name = "type", DisplayName = "Notification Type", DataType = "select", Required = false, 
                       AllowedValues = new List<object> { "info", "success", "warning", "error" } }
            };
        }

        private List<WorkflowActionParameterDto> GetTaskActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "assignee", DisplayName = "Assignee", DataType = "user", Required = true },
                new() { Name = "title", DisplayName = "Task Title", DataType = "string", Required = true },
                new() { Name = "description", DisplayName = "Description", DataType = "text", Required = false },
                new() { Name = "dueDate", DisplayName = "Due Date", DataType = "datetime", Required = false },
                new() { Name = "priority", DisplayName = "Priority", DataType = "select", Required = false,
                       AllowedValues = new List<object> { "low", "medium", "high", "urgent" } }
            };
        }

        private List<WorkflowActionParameterDto> GetStatusActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "entityType", DisplayName = "Entity Type", DataType = "select", Required = true,
                       AllowedValues = new List<object> { "TaxFiling", "Payment", "Document", "Client" } },
                new() { Name = "entityId", DisplayName = "Entity ID", DataType = "string", Required = true },
                new() { Name = "newStatus", DisplayName = "New Status", DataType = "string", Required = true },
                new() { Name = "reason", DisplayName = "Reason", DataType = "text", Required = false }
            };
        }

        private List<WorkflowActionParameterDto> GetReportActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "reportType", DisplayName = "Report Type", DataType = "select", Required = true },
                new() { Name = "format", DisplayName = "Format", DataType = "select", Required = true,
                       AllowedValues = new List<object> { "PDF", "Excel", "CSV" } },
                new() { Name = "emailTo", DisplayName = "Email Recipients", DataType = "emaillist", Required = false },
                new() { Name = "parameters", DisplayName = "Report Parameters", DataType = "json", Required = false }
            };
        }

        private List<WorkflowActionParameterDto> GetWebhookActionParameters()
        {
            return new List<WorkflowActionParameterDto>
            {
                new() { Name = "url", DisplayName = "Webhook URL", DataType = "url", Required = true },
                new() { Name = "method", DisplayName = "HTTP Method", DataType = "select", Required = true,
                       AllowedValues = new List<object> { "GET", "POST", "PUT", "DELETE" } },
                new() { Name = "headers", DisplayName = "HTTP Headers", DataType = "json", Required = false },
                new() { Name = "body", DisplayName = "Request Body", DataType = "json", Required = false },
                new() { Name = "timeout", DisplayName = "Timeout (seconds)", DataType = "number", Required = false, DefaultValue = 30 }
            };
        }

        #endregion
    }
}