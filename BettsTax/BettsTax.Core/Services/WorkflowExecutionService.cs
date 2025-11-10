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
    /// Service for executing workflow rules
    /// </summary>
    public class WorkflowExecutionService : IWorkflowExecutionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkflowRuleService _workflowRuleService;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WorkflowExecutionService> _logger;

        public WorkflowExecutionService(
            ApplicationDbContext context,
            IWorkflowRuleService workflowRuleService,
            IEmailService emailService,
            ISmsService smsService,
            INotificationService notificationService,
            ILogger<WorkflowExecutionService> logger)
        {
            _context = context;
            _workflowRuleService = workflowRuleService;
            _emailService = emailService;
            _smsService = smsService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<Result<WorkflowExecutionResultDto>> ExecuteWorkflowAsync(WorkflowTriggerEventDto triggerEvent)
        {
            var executionId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting workflow execution {ExecutionId} for trigger {TriggerType}", 
                    executionId, triggerEvent.TriggerType);

                // Get applicable rules for this trigger
                var rulesResult = await _workflowRuleService.EvaluateRulesAsync(triggerEvent.TriggerType, triggerEvent.Data);
                if (!rulesResult.IsSuccess)
                {
                    return Result.Failure<WorkflowExecutionResultDto>(rulesResult.ErrorMessage);
                }

                var result = new WorkflowExecutionResultDto
                {
                    ExecutionId = executionId,
                    Success = true,
                    RuleResults = new List<WorkflowRuleExecutionDto>(),
                    TotalExecutionTime = TimeSpan.Zero
                };

                // Get all active rules for this trigger type
                var activeRules = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions.OrderBy(c => c.Order))
                    .Include(r => r.Actions.OrderBy(a => a.Order))
                    .Where(r => r.TriggerType == triggerEvent.TriggerType && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                result.RulesExecuted = activeRules.Count;

                foreach (var rule in activeRules)
                {
                    var ruleResult = await ExecuteSingleRuleAsync(rule, triggerEvent, executionId);
                    result.RuleResults.Add(ruleResult);

                    if (ruleResult.ConditionsMatched)
                    {
                        result.RulesMatched++;
                        result.ActionsExecuted += ruleResult.ActionResults.Count;
                        result.ActionsSucceeded += ruleResult.ActionResults.Count(a => a.Success);
                    }

                    if (!ruleResult.Success)
                    {
                        result.Success = false;
                        if (string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            result.ErrorMessage = ruleResult.ErrorMessage;
                        }
                    }
                }

                result.TotalExecutionTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Completed workflow execution {ExecutionId}: {RulesMatched}/{RulesExecuted} rules matched", 
                    executionId, result.RulesMatched, result.RulesExecuted);

                return Result<WorkflowExecutionResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow for trigger {TriggerType}", triggerEvent.TriggerType);
                return Result.Failure<WorkflowExecutionResultDto>($"Error executing workflow: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowExecutionResultDto>> ExecuteRuleAsync(int ruleId, object data)
        {
            var executionId = Guid.NewGuid().ToString();

            try
            {
                var rule = await _context.WorkflowRuleEntities
                    .Include(r => r.Conditions.OrderBy(c => c.Order))
                    .Include(r => r.Actions.OrderBy(a => a.Order))
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return Result.Failure<WorkflowExecutionResultDto>("Workflow rule not found");
                }

                var triggerEvent = new WorkflowTriggerEventDto
                {
                    TriggerType = rule.TriggerType,
                    Data = data,
                    TriggeredBy = "manual" // TODO: Get from user context
                };

                var ruleResult = await ExecuteSingleRuleAsync(rule, triggerEvent, executionId);

                var result = new WorkflowExecutionResultDto
                {
                    ExecutionId = executionId,
                    Success = ruleResult.Success,
                    RulesExecuted = 1,
                    RulesMatched = ruleResult.ConditionsMatched ? 1 : 0,
                    ActionsExecuted = ruleResult.ActionResults.Count,
                    ActionsSucceeded = ruleResult.ActionResults.Count(a => a.Success),
                    RuleResults = new() { ruleResult },
                    TotalExecutionTime = ruleResult.ExecutionTime,
                    ErrorMessage = ruleResult.ErrorMessage
                };

                return Result<WorkflowExecutionResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rule {RuleId}", ruleId);
                return Result.Failure<WorkflowExecutionResultDto>($"Error executing rule: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowExecutionStatusDto>> GetExecutionStatusAsync(string executionId)
        {
            try
            {
                var execution = await _context.WorkflowExecutionHistories
                    .FirstOrDefaultAsync(e => e.Id == executionId);

                if (execution == null)
                {
                    return Result.Failure<WorkflowExecutionStatusDto>("Execution not found");
                }

                var status = new WorkflowExecutionStatusDto
                {
                    ExecutionId = executionId,
                    Status = execution.Status,
                    StartTime = execution.StartTime,
                    CanCancel = false, // Simple implementation doesn't support cancellation
                    CanRetry = execution.Status == "Failed",
                    ExecutionLog = new List<string>()
                };

                // Calculate progress based on status
                status.Progress = execution.Status switch
                {
                    "Running" => 50,
                    "Success" => 100,
                    "Failed" => 100,
                    _ => 0
                };

                if (execution.EndTime.HasValue)
                {
                    status.EstimatedEndTime = execution.EndTime;
                }

                return Result<WorkflowExecutionStatusDto>.Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution status for {ExecutionId}", executionId);
                return Result.Failure<WorkflowExecutionStatusDto>($"Error getting execution status: {ex.Message}");
            }
        }

        public async Task<Result> CancelExecutionAsync(string executionId)
        {
            try
            {
                // Simple implementation - just mark as cancelled
                var execution = await _context.WorkflowExecutionHistories
                    .FirstOrDefaultAsync(e => e.Id == executionId);

                if (execution == null)
                {
                    return Result.Failure("Execution not found");
                }

                if (execution.Status != "Running")
                {
                    return Result.Failure("Only running executions can be cancelled");
                }

                execution.Status = "Cancelled";
                execution.EndTime = DateTime.UtcNow;
                execution.ErrorMessage = "Execution cancelled by user";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled workflow execution {ExecutionId}", executionId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling execution {ExecutionId}", executionId);
                return Result.Failure($"Error cancelling execution: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowExecutionResultDto>> RetryExecutionAsync(string executionId)
        {
            try
            {
                var originalExecution = await _context.WorkflowExecutionHistories
                    .FirstOrDefaultAsync(e => e.Id == executionId);

                if (originalExecution == null)
                {
                    return Result.Failure<WorkflowExecutionResultDto>("Original execution not found");
                }

                if (originalExecution.Status != "Failed")
                {
                    return Result.Failure<WorkflowExecutionResultDto>("Only failed executions can be retried");
                }

                var triggerData = JsonSerializer.Deserialize<object>(originalExecution.TriggerDataJson);
                var triggerEvent = new WorkflowTriggerEventDto
                {
                    TriggerType = originalExecution.TriggerType,
                    Data = triggerData ?? new object(),
                    TriggeredBy = originalExecution.ExecutedBy
                };

                var result = await ExecuteRuleAsync(originalExecution.RuleId, triggerData ?? new object());
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully retried execution {OriginalExecutionId}, new execution: {NewExecutionId}", 
                        executionId, result.Value?.ExecutionId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying execution {ExecutionId}", executionId);
                return Result.Failure<WorkflowExecutionResultDto>($"Error retrying execution: {ex.Message}");
            }
        }

        public async Task<Result<WorkflowMetricsDto>> GetWorkflowMetricsAsync(WorkflowMetricsFilterDto filter)
        {
            try
            {
                var startDate = filter.StartDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = filter.EndDate ?? DateTime.UtcNow;

                var query = _context.WorkflowExecutionHistories.AsQueryable();

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(h => h.StartTime >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(h => h.StartTime <= filter.EndDate.Value);
                }

                if (filter.TriggerTypes != null && filter.TriggerTypes.Any())
                {
                    query = query.Where(h => filter.TriggerTypes.Contains(h.TriggerType));
                }

                if (filter.RuleIds != null && filter.RuleIds.Any())
                {
                    query = query.Where(h => filter.RuleIds.Contains(h.RuleId));
                }

                var executions = await query.ToListAsync();
                var totalRules = await _context.WorkflowRuleEntities.CountAsync();
                var activeRules = await _context.WorkflowRuleEntities.CountAsync(r => r.IsActive);

                var metrics = new WorkflowMetricsDto
                {
                    TotalRules = totalRules,
                    ActiveRules = activeRules,
                    TotalExecutions = executions.Count,
                    SuccessfulExecutions = executions.Count(e => e.Status == "Success"),
                    FailedExecutions = executions.Count(e => e.Status == "Failed"),
                    SuccessRate = executions.Count > 0 ? 
                        (double)executions.Count(e => e.Status == "Success") / executions.Count * 100 : 0,
                    AverageExecutionTime = executions.Count > 0 && executions.Any(e => e.Duration.HasValue) ?
                        TimeSpan.FromTicks((long)executions.Where(e => e.Duration.HasValue).Average(e => e.Duration!.Value.Ticks)) :
                        TimeSpan.Zero
                };

                // Trigger metrics
                metrics.TriggerMetrics = executions
                    .GroupBy(e => e.TriggerType)
                    .Select(g => new WorkflowTriggerMetricDto
                    {
                        TriggerType = g.Key,
                        ExecutionCount = g.Count(),
                        SuccessCount = g.Count(e => e.Status == "Success"),
                        FailureCount = g.Count(e => e.Status == "Failed"),
                        SuccessRate = g.Count() > 0 ? (double)g.Count(e => e.Status == "Success") / g.Count() * 100 : 0
                    })
                    .ToList();

                return Result<WorkflowMetricsDto>.Success(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow metrics");
                return Result.Failure<WorkflowMetricsDto>($"Error getting workflow metrics: {ex.Message}");
            }
        }

        #region Private Methods

        private async Task<WorkflowRuleExecutionDto> ExecuteSingleRuleAsync(
            WorkflowRule rule, 
            WorkflowTriggerEventDto triggerEvent, 
            string executionId)
        {
            var startTime = DateTime.UtcNow;
            var historyId = Guid.NewGuid().ToString();

            var ruleResult = new WorkflowRuleExecutionDto
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Executed = true,
                ActionResults = new List<WorkflowActionResultDto>()
            };

            // Create execution history record
            var history = new WorkflowExecutionHistory
            {
                Id = historyId,
                RuleId = rule.Id,
                RuleName = rule.Name,
                Status = "Running",
                StartTime = startTime,
                TriggerType = triggerEvent.TriggerType,
                TriggerDataJson = JsonSerializer.Serialize(triggerEvent.Data),
                ExecutedBy = triggerEvent.TriggeredBy,
                ActionsExecuted = 0,
                ActionsSucceeded = 0
            };

            _context.WorkflowExecutionHistories.Add(history);

            try
            {
                // Evaluate conditions
                var conditionsMatched = await EvaluateRuleConditionsAsync(rule.Conditions, triggerEvent.Data);
                ruleResult.ConditionsMatched = conditionsMatched;

                if (conditionsMatched)
                {
                    // Execute actions
                    foreach (var action in rule.Actions.OrderBy(a => a.Order))
                    {
                        var actionResult = await ExecuteActionAsync(action, triggerEvent, historyId);
                        ruleResult.ActionResults.Add(actionResult);

                        history.ActionsExecuted++;
                        if (actionResult.Success)
                        {
                            history.ActionsSucceeded++;
                        }
                        else if (!action.ContinueOnError)
                        {
                            // Stop execution if action failed and ContinueOnError is false
                            break;
                        }
                    }
                }

                var allActionsSucceeded = ruleResult.ActionResults.All(a => a.Success);
                ruleResult.Success = !ruleResult.ConditionsMatched || allActionsSucceeded;
                history.Status = ruleResult.Success ? "Success" : "Failed";

                if (!ruleResult.Success)
                {
                    var failedAction = ruleResult.ActionResults.FirstOrDefault(a => !a.Success);
                    ruleResult.ErrorMessage = failedAction?.ErrorMessage ?? "Rule execution failed";
                    history.ErrorMessage = ruleResult.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rule {RuleId}: {RuleName}", rule.Id, rule.Name);
                ruleResult.Success = false;
                ruleResult.ErrorMessage = ex.Message;
                history.Status = "Failed";
                history.ErrorMessage = ex.Message;
            }
            finally
            {
                history.EndTime = DateTime.UtcNow;
                ruleResult.ExecutionTime = DateTime.UtcNow - startTime;

                await _context.SaveChangesAsync();

                // Update rule metrics
                await UpdateRuleMetricsAsync(rule.Id, ruleResult.Success, ruleResult.ExecutionTime);
            }

            return ruleResult;
        }

        private async Task<bool> EvaluateRuleConditionsAsync(ICollection<WorkflowCondition> conditions, object data)
        {
            if (!conditions.Any())
                return true;

            // Use the existing condition evaluation logic from WorkflowRuleService
            var testResult = new WorkflowRuleTestResultDto();
            return await EvaluateConditionsAsync(conditions, data, testResult);
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
                try
                {
                    var fieldValue = GetFieldValue(dataDict, condition.FieldName);
                    var matched = EvaluateCondition(condition, fieldValue);
                    results.Add(matched);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error evaluating condition {ConditionId}", condition.Id);
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

        private async Task<WorkflowActionResultDto> ExecuteActionAsync(
            WorkflowAction action, 
            WorkflowTriggerEventDto triggerEvent, 
            string historyId)
        {
            var startTime = DateTime.UtcNow;
            var actionResult = new WorkflowActionResultDto
            {
                ActionId = action.Id,
                ActionType = action.ActionType,
                Success = true
            };

            var actionExecution = new WorkflowActionExecution
            {
                ExecutionHistoryId = historyId,
                ActionId = action.Id,
                ActionType = action.ActionType,
                Status = "Running",
                StartTime = startTime
            };

            _context.WorkflowActionExecutions.Add(actionExecution);

            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(action.ParametersJson) 
                    ?? new Dictionary<string, object>();

                switch (action.ActionType)
                {
                    case WorkflowActionTypes.SendEmail:
                        await ExecuteEmailActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    case WorkflowActionTypes.SendSms:
                        await ExecuteSmsActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    case WorkflowActionTypes.SendNotification:
                        await ExecuteNotificationActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    case WorkflowActionTypes.CreateTask:
                        await ExecuteCreateTaskActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    case WorkflowActionTypes.UpdateStatus:
                        await ExecuteUpdateStatusActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    case WorkflowActionTypes.LogEvent:
                        await ExecuteLogEventActionAsync(parameters, triggerEvent, actionResult);
                        break;

                    default:
                        actionResult.Success = false;
                        actionResult.ErrorMessage = $"Unknown action type: {action.ActionType}";
                        break;
                }

                actionExecution.Status = actionResult.Success ? "Success" : "Failed";
                actionExecution.Result = actionResult.Result;
                actionExecution.ErrorMessage = actionResult.ErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionId} of type {ActionType}", 
                    action.Id, action.ActionType);
                
                actionResult.Success = false;
                actionResult.ErrorMessage = ex.Message;
                actionExecution.Status = "Failed";
                actionExecution.ErrorMessage = ex.Message;
            }
            finally
            {
                actionExecution.EndTime = DateTime.UtcNow;
                actionResult.ExecutionTime = DateTime.UtcNow - startTime;
                await _context.SaveChangesAsync();
            }

            return actionResult;
        }

        private async Task ExecuteEmailActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var to = parameters.GetValueOrDefault("to")?.ToString();
            var subject = parameters.GetValueOrDefault("subject")?.ToString();
            var body = parameters.GetValueOrDefault("body")?.ToString();

            if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                result.Success = false;
                result.ErrorMessage = "Email action requires 'to', 'subject', and 'body' parameters";
                return;
            }

            // TODO: Replace with actual email service call
            await Task.Delay(100); // Simulate email sending
            result.Result = $"Email sent to {to}";
        }

        private async Task ExecuteSmsActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var phoneNumber = parameters.GetValueOrDefault("phoneNumber")?.ToString();
            var message = parameters.GetValueOrDefault("message")?.ToString();

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                result.Success = false;
                result.ErrorMessage = "SMS action requires 'phoneNumber' and 'message' parameters";
                return;
            }

            // TODO: Replace with actual SMS service call
            await Task.Delay(100); // Simulate SMS sending
            result.Result = $"SMS sent to {phoneNumber}";
        }

        private async Task ExecuteNotificationActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var userId = parameters.GetValueOrDefault("userId")?.ToString();
            var title = parameters.GetValueOrDefault("title")?.ToString();
            var message = parameters.GetValueOrDefault("message")?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
            {
                result.Success = false;
                result.ErrorMessage = "Notification action requires 'userId', 'title', and 'message' parameters";
                return;
            }

            // TODO: Replace with actual notification service call
            await Task.Delay(50); // Simulate notification creation
            result.Result = $"Notification sent to user {userId}";
        }

        private async Task ExecuteCreateTaskActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var assignee = parameters.GetValueOrDefault("assignee")?.ToString();
            var title = parameters.GetValueOrDefault("title")?.ToString();

            if (string.IsNullOrEmpty(assignee) || string.IsNullOrEmpty(title))
            {
                result.Success = false;
                result.ErrorMessage = "CreateTask action requires 'assignee' and 'title' parameters";
                return;
            }

            // TODO: Replace with actual task creation service call
            await Task.Delay(50); // Simulate task creation
            result.Result = $"Task created and assigned to {assignee}";
        }

        private async Task ExecuteUpdateStatusActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var entityType = parameters.GetValueOrDefault("entityType")?.ToString();
            var entityId = parameters.GetValueOrDefault("entityId")?.ToString();
            var newStatus = parameters.GetValueOrDefault("newStatus")?.ToString();

            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(newStatus))
            {
                result.Success = false;
                result.ErrorMessage = "UpdateStatus action requires 'entityType', 'entityId', and 'newStatus' parameters";
                return;
            }

            // TODO: Replace with actual status update logic
            await Task.Delay(50); // Simulate status update
            result.Result = $"Updated {entityType} {entityId} status to {newStatus}";
        }

        private Task ExecuteLogEventActionAsync(
            Dictionary<string, object> parameters, 
            WorkflowTriggerEventDto triggerEvent, 
            WorkflowActionResultDto result)
        {
            var level = parameters.GetValueOrDefault("level")?.ToString() ?? "Information";
            var message = parameters.GetValueOrDefault("message")?.ToString() ?? "Workflow event logged";

            _logger.LogInformation("Workflow Log Event: {Message}", message);
            result.Result = "Event logged successfully";
            return Task.CompletedTask;
        }

        private async Task UpdateRuleMetricsAsync(int ruleId, bool success, TimeSpan executionTime)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var metrics = await _context.WorkflowRuleMetrics
                    .FirstOrDefaultAsync(m => m.RuleId == ruleId && m.MetricDate == today);

                if (metrics == null)
                {
                    metrics = new WorkflowRuleMetrics
                    {
                        RuleId = ruleId,
                        MetricDate = today,
                        TotalExecutions = 0,
                        SuccessfulExecutions = 0,
                        FailedExecutions = 0,
                        TotalExecutionTimeMs = 0
                    };
                    _context.WorkflowRuleMetrics.Add(metrics);
                }

                metrics.TotalExecutions++;
                if (success)
                {
                    metrics.SuccessfulExecutions++;
                }
                else
                {
                    metrics.FailedExecutions++;
                }
                metrics.TotalExecutionTimeMs += (long)executionTime.TotalMilliseconds;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating metrics for rule {RuleId}", ruleId);
                // Don't fail the entire workflow execution due to metrics update failure
            }
        }

        private System.Dynamic.ExpandoObject ConvertToExpandoObject(object data)
        {
            if (data is System.Dynamic.ExpandoObject expando)
                return expando;

            var result = new System.Dynamic.ExpandoObject();
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

        private object? GetFieldValue(System.Dynamic.ExpandoObject data, string fieldName)
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
    }
}
