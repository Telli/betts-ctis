using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Service for managing configurable deadline rules
    /// Phase 3: Configurable Deadline Rules
    /// </summary>
    public interface IDeadlineRuleService
    {
        Task<Result<List<DeadlineRuleConfiguration>>> GetActiveRulesAsync(TaxType? taxType = null);
        Task<Result<DeadlineRuleConfiguration>> GetRuleByIdAsync(int ruleId);
        Task<Result<DeadlineRuleConfiguration>> CreateRuleAsync(DeadlineRuleConfiguration rule);
        Task<Result<DeadlineRuleConfiguration>> UpdateRuleAsync(int ruleId, DeadlineRuleConfiguration rule);
        Task<Result<bool>> DeleteRuleAsync(int ruleId);
        Task<Result<bool>> ActivateRuleAsync(int ruleId);
        Task<Result<bool>> DeactivateRuleAsync(int ruleId);
        
        Task<Result<DateTime>> CalculateDeadlineAsync(TaxType taxType, DateTime triggerDate, int? clientId = null);
        Task<Result<List<PublicHoliday>>> GetHolidaysAsync(int year);
        Task<Result<PublicHoliday>> AddHolidayAsync(PublicHoliday holiday);
        Task<Result<bool>> DeleteHolidayAsync(int holidayId);
        
        Task<Result<ClientDeadlineExtension>> GrantExtensionAsync(ClientDeadlineExtension extension);
        Task<Result<List<ClientDeadlineExtension>>> GetClientExtensionsAsync(int clientId);
        Task<Result<bool>> RevokeExtensionAsync(int extensionId);
    }
    
    public class DeadlineRuleService : IDeadlineRuleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly ILogger<DeadlineRuleService> _logger;
        
        public DeadlineRuleService(
            ApplicationDbContext context,
            IUserContextService userContextService,
            ILogger<DeadlineRuleService> logger)
        {
            _context = context;
            _userContextService = userContextService;
            _logger = logger;
        }
        
        #region Deadline Rule Management
        
        public async Task<Result<List<DeadlineRuleConfiguration>>> GetActiveRulesAsync(TaxType? taxType = null)
        {
            try
            {
                var query = _context.Set<DeadlineRuleConfiguration>()
                    .Where(r => r.IsActive && r.EffectiveDate <= DateTime.UtcNow && 
                               (r.ExpiryDate == null || r.ExpiryDate > DateTime.UtcNow));
                
                if (taxType.HasValue)
                {
                    query = query.Where(r => r.TaxType == taxType.Value);
                }
                
                var rules = await query
                    .OrderBy(r => r.TaxType)
                    .ThenByDescending(r => r.IsDefault)
                    .ToListAsync();
                
                return Result.Success(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active deadline rules");
                return Result.Failure<List<DeadlineRuleConfiguration>>("Failed to retrieve deadline rules");
            }
        }
        
        public async Task<Result<DeadlineRuleConfiguration>> GetRuleByIdAsync(int ruleId)
        {
            try
            {
                var rule = await _context.Set<DeadlineRuleConfiguration>()
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .FirstOrDefaultAsync(r => r.DeadlineRuleConfigurationId == ruleId);
                
                if (rule == null)
                {
                    return Result.Failure<DeadlineRuleConfiguration>("Deadline rule not found");
                }
                
                return Result.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deadline rule {RuleId}", ruleId);
                return Result.Failure<DeadlineRuleConfiguration>("Failed to retrieve deadline rule");
            }
        }
        
        public async Task<Result<DeadlineRuleConfiguration>> CreateRuleAsync(DeadlineRuleConfiguration rule)
        {
            try
            {
                // Validate statutory minimum
                if (rule.StatutoryMinimumDays.HasValue && rule.DaysFromTrigger < rule.StatutoryMinimumDays.Value)
                {
                    return Result.Failure<DeadlineRuleConfiguration>(
                        $"Days from trigger ({rule.DaysFromTrigger}) cannot be less than statutory minimum ({rule.StatutoryMinimumDays.Value})");
                }
                
                rule.CreatedById = _userContextService.GetCurrentUserId() ?? "System";
                rule.CreatedDate = DateTime.UtcNow;
                
                _context.Set<DeadlineRuleConfiguration>().Add(rule);
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogRuleChangeAsync(rule.DeadlineRuleConfigurationId, "Created", null, JsonSerializer.Serialize(rule));
                
                _logger.LogInformation("Created deadline rule {RuleId} for {TaxType}", 
                    rule.DeadlineRuleConfigurationId, rule.TaxType);
                
                return Result.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deadline rule");
                return Result.Failure<DeadlineRuleConfiguration>("Failed to create deadline rule");
            }
        }
        
        public async Task<Result<DeadlineRuleConfiguration>> UpdateRuleAsync(int ruleId, DeadlineRuleConfiguration updatedRule)
        {
            try
            {
                var existingRule = await _context.Set<DeadlineRuleConfiguration>()
                    .FirstOrDefaultAsync(r => r.DeadlineRuleConfigurationId == ruleId);
                
                if (existingRule == null)
                {
                    return Result.Failure<DeadlineRuleConfiguration>("Deadline rule not found");
                }
                
                // Validate statutory minimum
                if (updatedRule.StatutoryMinimumDays.HasValue && 
                    updatedRule.DaysFromTrigger < updatedRule.StatutoryMinimumDays.Value)
                {
                    return Result.Failure<DeadlineRuleConfiguration>(
                        $"Days from trigger ({updatedRule.DaysFromTrigger}) cannot be less than statutory minimum ({updatedRule.StatutoryMinimumDays.Value})");
                }
                
                var oldValues = JsonSerializer.Serialize(existingRule);
                
                // Update fields
                existingRule.RuleName = updatedRule.RuleName;
                existingRule.Description = updatedRule.Description;
                existingRule.DaysFromTrigger = updatedRule.DaysFromTrigger;
                existingRule.TriggerType = updatedRule.TriggerType;
                existingRule.AdjustForWeekends = updatedRule.AdjustForWeekends;
                existingRule.AdjustForHolidays = updatedRule.AdjustForHolidays;
                existingRule.StatutoryMinimumDays = updatedRule.StatutoryMinimumDays;
                existingRule.IsDefault = updatedRule.IsDefault;
                existingRule.EffectiveDate = updatedRule.EffectiveDate;
                existingRule.ExpiryDate = updatedRule.ExpiryDate;
                existingRule.UpdatedById = _userContextService.GetCurrentUserId();
                existingRule.UpdatedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogRuleChangeAsync(ruleId, "Updated", oldValues, JsonSerializer.Serialize(existingRule));
                
                _logger.LogInformation("Updated deadline rule {RuleId}", ruleId);
                
                return Result.Success(existingRule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deadline rule {RuleId}", ruleId);
                return Result.Failure<DeadlineRuleConfiguration>("Failed to update deadline rule");
            }
        }
        
        public async Task<Result<bool>> DeleteRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.Set<DeadlineRuleConfiguration>()
                    .FirstOrDefaultAsync(r => r.DeadlineRuleConfigurationId == ruleId);
                
                if (rule == null)
                {
                    return Result.Failure<bool>("Deadline rule not found");
                }
                
                var oldValues = JsonSerializer.Serialize(rule);
                
                _context.Set<DeadlineRuleConfiguration>().Remove(rule);
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogRuleChangeAsync(ruleId, "Deleted", oldValues, null);
                
                _logger.LogInformation("Deleted deadline rule {RuleId}", ruleId);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deadline rule {RuleId}", ruleId);
                return Result.Failure<bool>("Failed to delete deadline rule");
            }
        }
        
        public async Task<Result<bool>> ActivateRuleAsync(int ruleId)
        {
            return await ToggleRuleStatusAsync(ruleId, true);
        }
        
        public async Task<Result<bool>> DeactivateRuleAsync(int ruleId)
        {
            return await ToggleRuleStatusAsync(ruleId, false);
        }
        
        private async Task<Result<bool>> ToggleRuleStatusAsync(int ruleId, bool isActive)
        {
            try
            {
                var rule = await _context.Set<DeadlineRuleConfiguration>()
                    .FirstOrDefaultAsync(r => r.DeadlineRuleConfigurationId == ruleId);
                
                if (rule == null)
                {
                    return Result.Failure<bool>("Deadline rule not found");
                }
                
                var oldValues = JsonSerializer.Serialize(new { rule.IsActive });
                
                rule.IsActive = isActive;
                rule.UpdatedById = _userContextService.GetCurrentUserId();
                rule.UpdatedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogRuleChangeAsync(ruleId, isActive ? "Activated" : "Deactivated", 
                    oldValues, JsonSerializer.Serialize(new { rule.IsActive }));
                
                _logger.LogInformation("{Action} deadline rule {RuleId}", 
                    isActive ? "Activated" : "Deactivated", ruleId);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling deadline rule status {RuleId}", ruleId);
                return Result.Failure<bool>("Failed to update deadline rule status");
            }
        }
        
        #endregion
        
        #region Deadline Calculation
        
        public async Task<Result<DateTime>> CalculateDeadlineAsync(TaxType taxType, DateTime triggerDate, int? clientId = null)
        {
            try
            {
                // Get applicable rule
                var rulesResult = await GetActiveRulesAsync(taxType);
                if (!rulesResult.IsSuccess || !rulesResult.Value.Any())
                {
                    return Result.Failure<DateTime>($"No active deadline rule found for {taxType}");
                }
                
                var rule = rulesResult.Value.FirstOrDefault(r => r.IsDefault) ?? rulesResult.Value.First();
                
                // Calculate base deadline
                var deadline = triggerDate.AddDays(rule.DaysFromTrigger);
                
                // Apply client-specific extension if applicable
                if (clientId.HasValue)
                {
                    var extensionsResult = await GetClientExtensionsAsync(clientId.Value);
                    if (extensionsResult.IsSuccess)
                    {
                        var applicableExtension = extensionsResult.Value
                            .FirstOrDefault(e => e.TaxType == taxType && e.IsActive &&
                                               (e.ExpiryDate == null || e.ExpiryDate > DateTime.UtcNow));
                        
                        if (applicableExtension != null)
                        {
                            deadline = deadline.AddDays(applicableExtension.ExtensionDays);
                            _logger.LogInformation("Applied {Days} day extension for client {ClientId}", 
                                applicableExtension.ExtensionDays, clientId.Value);
                        }
                    }
                }
                
                // Adjust for weekends
                if (rule.AdjustForWeekends)
                {
                    deadline = AdjustForWeekend(deadline);
                }
                
                // Adjust for holidays
                if (rule.AdjustForHolidays)
                {
                    deadline = await AdjustForHolidaysAsync(deadline);
                }
                
                return Result.Success(deadline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating deadline for {TaxType}", taxType);
                return Result.Failure<DateTime>("Failed to calculate deadline");
            }
        }
        
        private DateTime AdjustForWeekend(DateTime date)
        {
            // If Saturday, move to Monday
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                return date.AddDays(2);
            }
            
            // If Sunday, move to Monday
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                return date.AddDays(1);
            }
            
            return date;
        }
        
        private async Task<DateTime> AdjustForHolidaysAsync(DateTime date)
        {
            var holidays = await GetHolidaysAsync(date.Year);
            if (!holidays.IsSuccess)
            {
                return date;
            }
            
            var holidayDates = holidays.Value.Select(h => h.Date.Date).ToHashSet();
            
            // Keep moving forward until we find a non-holiday weekday
            while (holidayDates.Contains(date.Date) || 
                   date.DayOfWeek == DayOfWeek.Saturday || 
                   date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }
            
            return date;
        }
        
        #endregion
        
        #region Holiday Management
        
        public async Task<Result<List<PublicHoliday>>> GetHolidaysAsync(int year)
        {
            try
            {
                var holidays = await _context.Set<PublicHoliday>()
                    .Where(h => h.Year == year || h.IsRecurring)
                    .OrderBy(h => h.Date)
                    .ToListAsync();
                
                return Result.Success(holidays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving holidays for year {Year}", year);
                return Result.Failure<List<PublicHoliday>>("Failed to retrieve holidays");
            }
        }
        
        public async Task<Result<PublicHoliday>> AddHolidayAsync(PublicHoliday holiday)
        {
            try
            {
                holiday.CreatedById = _userContextService.GetCurrentUserId() ?? "System";
                holiday.CreatedDate = DateTime.UtcNow;
                holiday.Year = holiday.Date.Year;
                
                _context.Set<PublicHoliday>().Add(holiday);
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogHolidayChangeAsync(holiday.PublicHolidayId, "Created", null, JsonSerializer.Serialize(holiday));
                
                _logger.LogInformation("Added public holiday {HolidayName} on {Date}", 
                    holiday.Name, holiday.Date);
                
                return Result.Success(holiday);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding public holiday");
                return Result.Failure<PublicHoliday>("Failed to add public holiday");
            }
        }
        
        public async Task<Result<bool>> DeleteHolidayAsync(int holidayId)
        {
            try
            {
                var holiday = await _context.Set<PublicHoliday>()
                    .FirstOrDefaultAsync(h => h.PublicHolidayId == holidayId);
                
                if (holiday == null)
                {
                    return Result.Failure<bool>("Public holiday not found");
                }
                
                var oldValues = JsonSerializer.Serialize(holiday);
                
                _context.Set<PublicHoliday>().Remove(holiday);
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogHolidayChangeAsync(holidayId, "Deleted", oldValues, null);
                
                _logger.LogInformation("Deleted public holiday {HolidayId}", holidayId);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting public holiday {HolidayId}", holidayId);
                return Result.Failure<bool>("Failed to delete public holiday");
            }
        }
        
        #endregion
        
        #region Client Extensions
        
        public async Task<Result<ClientDeadlineExtension>> GrantExtensionAsync(ClientDeadlineExtension extension)
        {
            try
            {
                extension.ApprovedById = _userContextService.GetCurrentUserId() ?? "System";
                extension.ApprovedDate = DateTime.UtcNow;
                extension.IsActive = true;
                
                _context.Set<ClientDeadlineExtension>().Add(extension);
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogExtensionChangeAsync(extension.ClientDeadlineExtensionId, "Granted", 
                    null, JsonSerializer.Serialize(extension));
                
                _logger.LogInformation("Granted {Days} day extension to client {ClientId} for {TaxType}", 
                    extension.ExtensionDays, extension.ClientId, extension.TaxType);
                
                return Result.Success(extension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting deadline extension");
                return Result.Failure<ClientDeadlineExtension>("Failed to grant deadline extension");
            }
        }
        
        public async Task<Result<List<ClientDeadlineExtension>>> GetClientExtensionsAsync(int clientId)
        {
            try
            {
                var extensions = await _context.Set<ClientDeadlineExtension>()
                    .Where(e => e.ClientId == clientId)
                    .Include(e => e.ApprovedBy)
                    .OrderByDescending(e => e.ApprovedDate)
                    .ToListAsync();
                
                return Result.Success(extensions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client extensions for client {ClientId}", clientId);
                return Result.Failure<List<ClientDeadlineExtension>>("Failed to retrieve client extensions");
            }
        }
        
        public async Task<Result<bool>> RevokeExtensionAsync(int extensionId)
        {
            try
            {
                var extension = await _context.Set<ClientDeadlineExtension>()
                    .FirstOrDefaultAsync(e => e.ClientDeadlineExtensionId == extensionId);
                
                if (extension == null)
                {
                    return Result.Failure<bool>("Extension not found");
                }
                
                var oldValues = JsonSerializer.Serialize(new { extension.IsActive });
                
                extension.IsActive = false;
                await _context.SaveChangesAsync();
                
                // Audit log
                await LogExtensionChangeAsync(extensionId, "Revoked", oldValues, 
                    JsonSerializer.Serialize(new { extension.IsActive }));
                
                _logger.LogInformation("Revoked deadline extension {ExtensionId}", extensionId);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking deadline extension {ExtensionId}", extensionId);
                return Result.Failure<bool>("Failed to revoke deadline extension");
            }
        }
        
        #endregion
        
        #region Audit Logging
        
        private async Task LogRuleChangeAsync(int ruleId, string action, string? oldValues, string? newValues)
        {
            var auditLog = new DeadlineRuleAuditLog
            {
                DeadlineRuleConfigurationId = ruleId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedById = _userContextService.GetCurrentUserId() ?? "System",
                ChangedDate = DateTime.UtcNow
            };
            
            _context.Set<DeadlineRuleAuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();
        }
        
        private async Task LogHolidayChangeAsync(int holidayId, string action, string? oldValues, string? newValues)
        {
            var auditLog = new DeadlineRuleAuditLog
            {
                PublicHolidayId = holidayId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedById = _userContextService.GetCurrentUserId() ?? "System",
                ChangedDate = DateTime.UtcNow
            };
            
            _context.Set<DeadlineRuleAuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();
        }
        
        private async Task LogExtensionChangeAsync(int extensionId, string action, string? oldValues, string? newValues)
        {
            var auditLog = new DeadlineRuleAuditLog
            {
                ClientDeadlineExtensionId = extensionId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedById = _userContextService.GetCurrentUserId() ?? "System",
                ChangedDate = DateTime.UtcNow
            };
            
            _context.Set<DeadlineRuleAuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();
        }
        
        #endregion
    }
}
