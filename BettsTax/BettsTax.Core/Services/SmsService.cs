using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BettsTax.Core.Services
{
    public class SmsService : ISmsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserContextService _userContext;
        private readonly IAuditService _auditService;
        private readonly IActivityTimelineService _activityService;
        private readonly ILogger<SmsService> _logger;
        private readonly Dictionary<SmsProvider, ISmsProvider> _providers;

        public SmsService(
            ApplicationDbContext context,
            IServiceProvider serviceProvider,
            IUserContextService userContext,
            IAuditService auditService,
            IActivityTimelineService activityService,
            ILogger<SmsService> logger)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _userContext = userContext;
            _auditService = auditService;
            _activityService = activityService;
            _logger = logger;
            _providers = new Dictionary<SmsProvider, ISmsProvider>();
        }

        public async Task<Result<SmsNotificationDto>> SendSmsAsync(SendSmsDto dto)
        {
            try
            {
                // Validate phone number
                var validationResult = await ValidatePhoneNumberAsync(dto.PhoneNumber);
                if (!validationResult.IsSuccess || !validationResult.Value)
                {
                    return Result.Failure<SmsNotificationDto>("Invalid phone number");
                }

                // Format phone number
                var formattedNumberResult = await FormatPhoneNumberAsync(dto.PhoneNumber);
                if (!formattedNumberResult.IsSuccess)
                {
                    return Result.Failure<SmsNotificationDto>("Failed to format phone number");
                }

                var formattedNumber = formattedNumberResult.Value;

                // Get client info if provided
                Client? client = null;
                if (dto.ClientId.HasValue)
                {
                    client = await _context.Clients
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.ClientId == dto.ClientId.Value);
                }

                // Create SMS notification record
                var sms = new SmsNotification
                {
                    PhoneNumber = formattedNumber,
                    RecipientName = client?.BusinessName ?? "Unknown",
                    UserId = client?.UserId,
                    ClientId = dto.ClientId,
                    Message = dto.Message,
                    Type = dto.Type,
                    Status = dto.ScheduledDate.HasValue ? SmsStatus.Pending : SmsStatus.Pending,
                    TaxFilingId = dto.TaxFilingId,
                    PaymentId = dto.PaymentId,
                    DocumentId = dto.DocumentId,
                    ScheduledDate = dto.ScheduledDate,
                    IsScheduled = dto.ScheduledDate.HasValue
                };

                // Determine provider
                var provider = await DetermineProviderAsync(formattedNumber, dto.PreferredProvider);
                if (provider == null)
                {
                    return Result.Failure<SmsNotificationDto>("No SMS provider available");
                }

                sms.Provider = provider.ProviderType;

                _context.SmsNotifications.Add(sms);
                await _context.SaveChangesAsync();

                // Send immediately if not scheduled
                if (!dto.ScheduledDate.HasValue || dto.ScheduledDate <= DateTime.UtcNow)
                {
                    var sendResult = await SendSmsViaProviderAsync(sms, provider);
                    if (!sendResult.IsSuccess)
                    {
                        sms.Status = SmsStatus.Failed;
                        sms.FailedDate = DateTime.UtcNow;
                        sms.ProviderResponse = sendResult.ErrorMessage;
                        await _context.SaveChangesAsync();
                        
                        return Result.Failure<SmsNotificationDto>($"Failed to send SMS: {sendResult.ErrorMessage}");
                    }
                }

                // Log activity
                if (dto.ClientId.HasValue)
                {
                    await _activityService.LogCommunicationActivityAsync(
                        dto.ClientId.Value,
                        ActivityType.SMSSent,
                        $"SMS sent: {dto.Type}",
                        $"To: {formattedNumber}"
                    );
                }

                // Audit log
                await _auditService.LogAsync(
                    "SMS",
                    "Send",
                    $"Sent {dto.Type} SMS to {formattedNumber}",
                    sms.SmsNotificationId.ToString()
                );

                return Result.Success<SmsNotificationDto>(MapToDto(sms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS");
                return Result.Failure<SmsNotificationDto>("Error sending SMS");
            }
        }

        public async Task<Result<List<SmsNotificationDto>>> SendBulkSmsAsync(BulkSmsDto dto)
        {
            try
            {
                var results = new List<SmsNotificationDto>();
                
                foreach (var phoneNumber in dto.PhoneNumbers)
                {
                    var sendDto = new SendSmsDto
                    {
                        PhoneNumber = phoneNumber,
                        Message = dto.Message,
                        Type = dto.Type,
                        ScheduledDate = dto.ScheduledDate
                    };

                    var result = await SendSmsAsync(sendDto);
                    if (result.IsSuccess)
                    {
                        results.Add(result.Value);
                    }
                }

                return Result.Success<List<SmsNotificationDto>>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk SMS");
                return Result.Failure<List<SmsNotificationDto>>("Error sending bulk SMS");
            }
        }

        public async Task<Result<SmsNotificationDto>> GetSmsAsync(int smsId)
        {
            try
            {
                var sms = await _context.SmsNotifications
                    .Include(s => s.User)
                    .Include(s => s.Client)
                    .FirstOrDefaultAsync(s => s.SmsNotificationId == smsId);

                if (sms == null)
                {
                    return Result.Failure<SmsNotificationDto>("SMS not found");
                }

                return Result.Success<SmsNotificationDto>(MapToDto(sms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS");
                return Result.Failure<SmsNotificationDto>("Error retrieving SMS");
            }
        }

        public async Task<Result<PagedResult<SmsNotificationDto>>> GetSmsHistoryAsync(
            string? phoneNumber = null,
            int? clientId = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.SmsNotifications
                    .Include(s => s.Client)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(phoneNumber))
                    query = query.Where(s => s.PhoneNumber.Contains(phoneNumber));

                if (clientId.HasValue)
                    query = query.Where(s => s.ClientId == clientId.Value);

                if (status.HasValue)
                    query = query.Where(s => s.Status == status.Value);

                if (fromDate.HasValue)
                    query = query.Where(s => s.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(s => s.CreatedDate <= toDate.Value);

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(s => s.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = items.Select(MapToDto).ToList();

                return Result.Success<PagedResult<SmsNotificationDto>>(new PagedResult<SmsNotificationDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS history");
                return Result.Failure<PagedResult<SmsNotificationDto>>("Error retrieving SMS history");
            }
        }

        public async Task<Result> RetrySmsAsync(int smsId)
        {
            try
            {
                var sms = await _context.SmsNotifications.FindAsync(smsId);
                if (sms == null)
                {
                    return Result.Failure("SMS not found");
                }

                if (sms.Status != SmsStatus.Failed)
                {
                    return Result.Failure("SMS is not in failed status");
                }

                // Get provider
                var provider = await GetProviderAsync(sms.Provider);
                if (provider == null)
                {
                    return Result.Failure("SMS provider not configured");
                }

                // Retry sending
                var sendResult = await SendSmsViaProviderAsync(sms, provider);
                if (!sendResult.IsSuccess)
                {
                    sms.RetryCount++;
                    sms.NextRetryDate = DateTime.UtcNow.AddMinutes(Math.Pow(2, sms.RetryCount)); // Exponential backoff
                    await _context.SaveChangesAsync();
                    
                    return Result.Failure($"Retry failed: {sendResult.ErrorMessage}");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying SMS");
                return Result.Failure("Error retrying SMS");
            }
        }

        public async Task<Result<int>> RetryFailedSmsAsync(DateTime? since = null)
        {
            try
            {
                var query = _context.SmsNotifications
                    .Where(s => s.Status == SmsStatus.Failed &&
                               s.RetryCount < 3 &&
                               (s.NextRetryDate == null || s.NextRetryDate <= DateTime.UtcNow));

                if (since.HasValue)
                    query = query.Where(s => s.FailedDate >= since.Value);

                var failedSms = await query.ToListAsync();
                var retryCount = 0;

                foreach (var sms in failedSms)
                {
                    var result = await RetrySmsAsync(sms.SmsNotificationId);
                    if (result.IsSuccess)
                        retryCount++;
                }

                return Result.Success<int>(retryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed SMS");
                return Result.Failure<int>("Error retrying failed SMS");
            }
        }

        public async Task<Result<List<SmsNotificationDto>>> GetScheduledSmsAsync()
        {
            try
            {
                var scheduledSms = await _context.SmsNotifications
                    .Include(s => s.Client)
                    .Where(s => s.IsScheduled && 
                               s.Status == SmsStatus.Pending &&
                               s.ScheduledDate > DateTime.UtcNow)
                    .OrderBy(s => s.ScheduledDate)
                    .ToListAsync();

                var dtos = scheduledSms.Select(MapToDto).ToList();
                return Result.Success<List<SmsNotificationDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled SMS");
                return Result.Failure<List<SmsNotificationDto>>("Error retrieving scheduled SMS");
            }
        }

        public async Task<Result> CancelScheduledSmsAsync(int smsId)
        {
            try
            {
                var sms = await _context.SmsNotifications.FindAsync(smsId);
                if (sms == null)
                {
                    return Result.Failure("SMS not found");
                }

                if (!sms.IsScheduled || sms.Status != SmsStatus.Pending)
                {
                    return Result.Failure("SMS is not scheduled or already sent");
                }

                sms.Status = SmsStatus.Failed;
                sms.FailedDate = DateTime.UtcNow;
                sms.ProviderResponse = "Cancelled by user";
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling scheduled SMS");
                return Result.Failure("Error cancelling SMS");
            }
        }

        public async Task<Result<int>> ProcessScheduledSmsAsync()
        {
            try
            {
                var dueSms = await _context.SmsNotifications
                    .Where(s => s.IsScheduled && 
                               s.Status == SmsStatus.Pending &&
                               s.ScheduledDate <= DateTime.UtcNow)
                    .ToListAsync();

                var processedCount = 0;

                foreach (var sms in dueSms)
                {
                    var provider = await GetProviderAsync(sms.Provider);
                    if (provider != null)
                    {
                        var result = await SendSmsViaProviderAsync(sms, provider);
                        if (result.IsSuccess)
                            processedCount++;
                    }
                }

                return Result.Success<int>(processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled SMS");
                return Result.Failure<int>("Error processing scheduled SMS");
            }
        }

        public async Task<Result<List<SmsTemplateDto>>> GetSmsTemplatesAsync(SmsType? type = null)
        {
            try
            {
                var query = _context.SmsTemplates.Where(t => t.IsActive);

                if (type.HasValue)
                    query = query.Where(t => t.Type == type.Value);

                var templates = await query
                    .OrderBy(t => t.Type)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                var dtos = templates.Select(MapToDto).ToList();
                return Result.Success<List<SmsTemplateDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS templates");
                return Result.Failure<List<SmsTemplateDto>>("Error retrieving templates");
            }
        }

        public async Task<Result<SmsTemplateDto>> GetSmsTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.SmsTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<SmsTemplateDto>("Template not found");
                }

                return Result.Success<SmsTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS template");
                return Result.Failure<SmsTemplateDto>("Error retrieving template");
            }
        }

        public async Task<Result<SmsTemplateDto>> CreateSmsTemplateAsync(SmsTemplateDto dto)
        {
            try
            {
                var existing = await _context.SmsTemplates
                    .FirstOrDefaultAsync(t => t.TemplateCode == dto.TemplateCode);

                if (existing != null)
                {
                    return Result.Failure<SmsTemplateDto>("Template code already exists");
                }

                var template = new SmsTemplate
                {
                    TemplateCode = dto.TemplateCode,
                    Name = dto.Name,
                    Description = dto.Description,
                    MessageTemplate = dto.MessageTemplate,
                    Type = dto.Type,
                    AvailableVariables = JsonSerializer.Serialize(dto.AvailableVariables),
                    CharacterCount = dto.MessageTemplate.Length
                };

                _context.SmsTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsTemplate",
                    "Create",
                    $"Created SMS template: {template.Name}",
                    template.SmsTemplateId.ToString()
                );

                return Result.Success<SmsTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SMS template");
                return Result.Failure<SmsTemplateDto>("Error creating template");
            }
        }

        public async Task<Result<SmsTemplateDto>> UpdateSmsTemplateAsync(int templateId, SmsTemplateDto dto)
        {
            try
            {
                var template = await _context.SmsTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<SmsTemplateDto>("Template not found");
                }

                template.Name = dto.Name;
                template.Description = dto.Description;
                template.MessageTemplate = dto.MessageTemplate;
                template.Type = dto.Type;
                template.AvailableVariables = JsonSerializer.Serialize(dto.AvailableVariables);
                template.CharacterCount = dto.MessageTemplate.Length;
                template.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsTemplate",
                    "Update",
                    $"Updated SMS template: {template.Name}",
                    template.SmsTemplateId.ToString()
                );

                return Result.Success<SmsTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SMS template");
                return Result.Failure<SmsTemplateDto>("Error updating template");
            }
        }

        public async Task<Result> DeleteSmsTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.SmsTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure("Template not found");
                }

                template.IsActive = false;
                template.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsTemplate",
                    "Delete",
                    $"Deactivated SMS template: {template.Name}",
                    template.SmsTemplateId.ToString()
                );

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SMS template");
                return Result.Failure("Error deleting template");
            }
        }

        public async Task<Result<SendSmsDto>> ApplySmsTemplateAsync(ApplySmsTemplateDto dto)
        {
            try
            {
                var template = await _context.SmsTemplates.FindAsync(dto.TemplateId);
                if (template == null)
                {
                    return Result.Failure<SendSmsDto>("Template not found");
                }

                var message = ReplaceVariables(template.MessageTemplate, dto.Variables);

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = dto.PhoneNumber,
                    Message = message,
                    Type = template.Type,
                    ClientId = dto.ClientId
                };

                return Result.Success<SendSmsDto>(sendDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying SMS template");
                return Result.Failure<SendSmsDto>("Error applying template");
            }
        }

        public async Task<Result<List<SmsProviderConfigDto>>> GetProviderConfigsAsync(bool activeOnly = true)
        {
            try
            {
                var query = _context.SmsProviderConfigs.AsQueryable();

                if (activeOnly)
                    query = query.Where(c => c.IsActive);

                var configs = await query
                    .OrderBy(c => c.Priority)
                    .ToListAsync();

                var dtos = configs.Select(MapToDto).ToList();
                return Result.Success<List<SmsProviderConfigDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider configs");
                return Result.Failure<List<SmsProviderConfigDto>>("Error retrieving provider configurations");
            }
        }

        public async Task<Result<SmsProviderConfigDto>> GetProviderConfigAsync(SmsProvider provider)
        {
            try
            {
                var config = await _context.SmsProviderConfigs
                    .FirstOrDefaultAsync(c => c.Provider == provider);

                if (config == null)
                {
                    return Result.Failure<SmsProviderConfigDto>("Provider configuration not found");
                }

                return Result.Success<SmsProviderConfigDto>(MapToDto(config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider config");
                return Result.Failure<SmsProviderConfigDto>("Error retrieving provider configuration");
            }
        }

        public async Task<Result<SmsProviderConfigDto>> CreateProviderConfigAsync(SmsProviderConfigDto dto)
        {
            try
            {
                var existing = await _context.SmsProviderConfigs
                    .FirstOrDefaultAsync(c => c.Provider == dto.Provider);

                if (existing != null)
                {
                    return Result.Failure<SmsProviderConfigDto>("Provider configuration already exists");
                }

                var config = new SmsProviderConfig
                {
                    Provider = dto.Provider,
                    Name = dto.Name,
                    ApiKey = dto.ApiKey, // Should be encrypted
                    ApiSecret = dto.ApiSecret, // Should be encrypted
                    ApiUrl = dto.ApiUrl,
                    SenderId = dto.SenderId,
                    AdditionalSettings = JsonSerializer.Serialize(dto.AdditionalSettings),
                    CostPerSms = dto.CostPerSms,
                    Currency = dto.Currency,
                    Priority = dto.Priority,
                    DailyLimit = dto.DailyLimit,
                    MonthlyLimit = dto.MonthlyLimit,
                    IsActive = dto.IsActive,
                    IsDefault = dto.IsDefault
                };

                // Ensure only one default provider
                if (dto.IsDefault)
                {
                    var currentDefaults = await _context.SmsProviderConfigs
                        .Where(c => c.IsDefault)
                        .ToListAsync();
                    
                    foreach (var def in currentDefaults)
                    {
                        def.IsDefault = false;
                    }
                }

                _context.SmsProviderConfigs.Add(config);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsProviderConfig",
                    "Create",
                    $"Created provider config: {config.Name}",
                    config.SmsProviderConfigId.ToString()
                );

                return Result.Success<SmsProviderConfigDto>(MapToDto(config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider config");
                return Result.Failure<SmsProviderConfigDto>("Error creating provider configuration");
            }
        }

        public async Task<Result<SmsProviderConfigDto>> UpdateProviderConfigAsync(int configId, SmsProviderConfigDto dto)
        {
            try
            {
                var config = await _context.SmsProviderConfigs.FindAsync(configId);
                if (config == null)
                {
                    return Result.Failure<SmsProviderConfigDto>("Provider configuration not found");
                }

                config.Name = dto.Name;
                config.ApiKey = dto.ApiKey; // Should be encrypted
                config.ApiSecret = dto.ApiSecret; // Should be encrypted
                config.ApiUrl = dto.ApiUrl;
                config.SenderId = dto.SenderId;
                config.AdditionalSettings = JsonSerializer.Serialize(dto.AdditionalSettings);
                config.CostPerSms = dto.CostPerSms;
                config.Currency = dto.Currency;
                config.Priority = dto.Priority;
                config.DailyLimit = dto.DailyLimit;
                config.MonthlyLimit = dto.MonthlyLimit;
                config.IsActive = dto.IsActive;
                config.UpdatedDate = DateTime.UtcNow;

                // Handle default provider change
                if (dto.IsDefault && !config.IsDefault)
                {
                    var currentDefaults = await _context.SmsProviderConfigs
                        .Where(c => c.IsDefault && c.SmsProviderConfigId != configId)
                        .ToListAsync();
                    
                    foreach (var def in currentDefaults)
                    {
                        def.IsDefault = false;
                    }
                    
                    config.IsDefault = true;
                }

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsProviderConfig",
                    "Update",
                    $"Updated provider config: {config.Name}",
                    config.SmsProviderConfigId.ToString()
                );

                return Result.Success<SmsProviderConfigDto>(MapToDto(config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider config");
                return Result.Failure<SmsProviderConfigDto>("Error updating provider configuration");
            }
        }

        public async Task<Result> SetDefaultProviderAsync(SmsProvider provider)
        {
            try
            {
                var config = await _context.SmsProviderConfigs
                    .FirstOrDefaultAsync(c => c.Provider == provider);

                if (config == null)
                {
                    return Result.Failure("Provider configuration not found");
                }

                // Remove current default
                var currentDefaults = await _context.SmsProviderConfigs
                    .Where(c => c.IsDefault)
                    .ToListAsync();
                
                foreach (var def in currentDefaults)
                {
                    def.IsDefault = false;
                }

                config.IsDefault = true;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default provider");
                return Result.Failure("Error setting default provider");
            }
        }

        public async Task<Result<SmsBalanceDto>> CheckProviderBalanceAsync(SmsProvider provider)
        {
            try
            {
                var smsProvider = await GetProviderAsync(provider);
                if (smsProvider == null)
                {
                    return Result.Failure<SmsBalanceDto>("Provider not configured");
                }

                var balanceResult = await smsProvider.GetBalanceAsync();
                if (!balanceResult.IsSuccess)
                {
                    return Result.Failure<SmsBalanceDto>(balanceResult.ErrorMessage);
                }

                var config = await _context.SmsProviderConfigs
                    .FirstOrDefaultAsync(c => c.Provider == provider);

                var dto = new SmsBalanceDto
                {
                    Provider = provider,
                    Balance = balanceResult.Value,
                    Currency = config?.Currency ?? "SLE",
                    MessagesRemaining = config != null && config.CostPerSms > 0 ? 
                        (int)(balanceResult.Value / config.CostPerSms) : 0,
                    LastChecked = DateTime.UtcNow
                };

                return Result.Success<SmsBalanceDto>(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider balance");
                return Result.Failure<SmsBalanceDto>("Error checking balance");
            }
        }

        public async Task<Result<List<SmsScheduleDto>>> GetSmsSchedulesAsync(bool activeOnly = true)
        {
            try
            {
                var query = _context.SmsSchedules
                    .Include(s => s.SmsTemplate)
                    .AsQueryable();

                if (activeOnly)
                    query = query.Where(s => s.IsActive);

                var schedules = await query
                    .OrderBy(s => s.Type)
                    .ThenBy(s => s.Name)
                    .ToListAsync();

                var dtos = schedules.Select(MapToDto).ToList();
                return Result.Success<List<SmsScheduleDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS schedules");
                return Result.Failure<List<SmsScheduleDto>>("Error retrieving schedules");
            }
        }

        public async Task<Result<SmsScheduleDto>> GetSmsScheduleAsync(int scheduleId)
        {
            try
            {
                var schedule = await _context.SmsSchedules
                    .Include(s => s.SmsTemplate)
                    .FirstOrDefaultAsync(s => s.SmsScheduleId == scheduleId);

                if (schedule == null)
                {
                    return Result.Failure<SmsScheduleDto>("Schedule not found");
                }

                return Result.Success<SmsScheduleDto>(MapToDto(schedule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS schedule");
                return Result.Failure<SmsScheduleDto>("Error retrieving schedule");
            }
        }

        public async Task<Result<SmsScheduleDto>> CreateSmsScheduleAsync(SmsScheduleDto dto)
        {
            try
            {
                var schedule = new SmsSchedule
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Type = dto.Type,
                    DaysBefore = dto.DaysBefore,
                    TimeOfDay = dto.TimeOfDay,
                    IsRecurring = dto.IsRecurring,
                    RecurrenceIntervalDays = dto.RecurrenceIntervalDays,
                    SmsTemplateId = dto.SmsTemplateId,
                    TaxType = dto.TaxType,
                    TaxpayerCategory = dto.TaxpayerCategory,
                    OnlyActiveClients = dto.OnlyActiveClients,
                    IsActive = dto.IsActive
                };

                _context.SmsSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsSchedule",
                    "Create",
                    $"Created SMS schedule: {schedule.Name}",
                    schedule.SmsScheduleId.ToString()
                );

                return await GetSmsScheduleAsync(schedule.SmsScheduleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SMS schedule");
                return Result.Failure<SmsScheduleDto>("Error creating schedule");
            }
        }

        public async Task<Result<SmsScheduleDto>> UpdateSmsScheduleAsync(int scheduleId, SmsScheduleDto dto)
        {
            try
            {
                var schedule = await _context.SmsSchedules.FindAsync(scheduleId);
                if (schedule == null)
                {
                    return Result.Failure<SmsScheduleDto>("Schedule not found");
                }

                schedule.Name = dto.Name;
                schedule.Description = dto.Description;
                schedule.Type = dto.Type;
                schedule.DaysBefore = dto.DaysBefore;
                schedule.TimeOfDay = dto.TimeOfDay;
                schedule.IsRecurring = dto.IsRecurring;
                schedule.RecurrenceIntervalDays = dto.RecurrenceIntervalDays;
                schedule.SmsTemplateId = dto.SmsTemplateId;
                schedule.TaxType = dto.TaxType;
                schedule.TaxpayerCategory = dto.TaxpayerCategory;
                schedule.OnlyActiveClients = dto.OnlyActiveClients;
                schedule.IsActive = dto.IsActive;
                schedule.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsSchedule",
                    "Update",
                    $"Updated SMS schedule: {schedule.Name}",
                    schedule.SmsScheduleId.ToString()
                );

                return await GetSmsScheduleAsync(schedule.SmsScheduleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SMS schedule");
                return Result.Failure<SmsScheduleDto>("Error updating schedule");
            }
        }

        public async Task<Result> DeleteSmsScheduleAsync(int scheduleId)
        {
            try
            {
                var schedule = await _context.SmsSchedules.FindAsync(scheduleId);
                if (schedule == null)
                {
                    return Result.Failure("Schedule not found");
                }

                schedule.IsActive = false;
                schedule.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "SmsSchedule",
                    "Delete",
                    $"Deactivated SMS schedule: {schedule.Name}",
                    schedule.SmsScheduleId.ToString()
                );

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SMS schedule");
                return Result.Failure("Error deleting schedule");
            }
        }

        public async Task<Result<int>> RunScheduledSmsAsync(int scheduleId)
        {
            try
            {
                var schedule = await _context.SmsSchedules
                    .Include(s => s.SmsTemplate)
                    .FirstOrDefaultAsync(s => s.SmsScheduleId == scheduleId);

                if (schedule == null)
                {
                    return Result.Failure<int>("Schedule not found");
                }

                if (!schedule.IsActive)
                {
                    return Result.Failure<int>("Schedule is not active");
                }

                // Find eligible clients based on schedule criteria
                var query = _context.Clients
                    .Include(c => c.User)
                    .Where(c => schedule.OnlyActiveClients ? c.Status == ClientStatus.Active : true);

                if (schedule.TaxpayerCategory.HasValue)
                    query = query.Where(c => c.TaxpayerCategory == schedule.TaxpayerCategory.Value);

                var clients = await query.ToListAsync();
                var sentCount = 0;

                foreach (var client in clients)
                {
                    if (string.IsNullOrEmpty(client.PhoneNumber))
                        continue;

                    // Find relevant deadlines based on schedule type
                    DateTime? deadline = await GetClientDeadlineAsync(client.ClientId, schedule.Type, schedule.TaxType);
                    
                    if (deadline.HasValue)
                    {
                        var daysUntilDeadline = (deadline.Value - DateTime.UtcNow).Days;
                        
                        if (daysUntilDeadline == schedule.DaysBefore)
                        {
                            // Apply template with variables
                            var variables = new Dictionary<string, string>
                            {
                                ["ClientName"] = client.BusinessName,
                                ["DueDate"] = deadline.Value.ToString("MMMM dd, yyyy"),
                                ["DaysRemaining"] = daysUntilDeadline.ToString()
                            };

                            var message = ReplaceVariables(schedule.SmsTemplate.MessageTemplate, variables);

                            var sendDto = new SendSmsDto
                            {
                                PhoneNumber = client.PhoneNumber,
                                Message = message,
                                Type = schedule.Type,
                                ClientId = client.ClientId
                            };

                            var result = await SendSmsAsync(sendDto);
                            if (result.IsSuccess)
                                sentCount++;
                        }
                    }
                }

                schedule.LastRunDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Result.Success<int>(sentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled SMS");
                return Result.Failure<int>("Error running schedule");
            }
        }

        public async Task<Result> SendDeadlineReminderAsync(int clientId, string deadline, DateTime dueDate, int daysBefore)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null || string.IsNullOrEmpty(client.PhoneNumber))
                {
                    return Result.Failure("Client not found or phone number missing");
                }

                var message = $"Dear {client.BusinessName}, Reminder: {deadline} is due on {dueDate:MMM dd, yyyy}. Please ensure timely compliance. - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = client.PhoneNumber,
                    Message = message,
                    Type = SmsType.DeadlineReminder,
                    ClientId = clientId
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline reminder");
                return Result.Failure("Error sending reminder");
            }
        }

        public async Task<Result> SendPaymentConfirmationAsync(int paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Client)
                    .Include(p => p.TaxFiling)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment?.Client == null || string.IsNullOrEmpty(payment.Client.PhoneNumber))
                {
                    return Result.Failure("Payment or client phone number not found");
                }

                var message = $"Payment Confirmed: SLE {payment.Amount:N2} received for {(payment.TaxFiling?.TaxType.ToString() ?? "Tax Payment")}. Ref: {payment.PaymentReference}. Thank you! - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = payment.Client.PhoneNumber,
                    Message = message,
                    Type = SmsType.PaymentConfirmation,
                    ClientId = payment.ClientId,
                    PaymentId = paymentId
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment confirmation");
                return Result.Failure("Error sending confirmation");
            }
        }

        public async Task<Result> SendDocumentRequestAsync(int clientId, List<string> documentNames, DateTime dueDate)
        {
            try
            {
                var client = await _context.Clients.FindAsync(clientId);
                if (client == null || string.IsNullOrEmpty(client.PhoneNumber))
                {
                    return Result.Failure("Client not found or phone number missing");
                }

                var docList = string.Join(", ", documentNames.Take(2));
                if (documentNames.Count > 2)
                    docList += $" and {documentNames.Count - 2} more";

                var message = $"Documents needed: {docList}. Please upload by {dueDate:MMM dd}. Login: ctis.bettsfirmsl.com - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = client.PhoneNumber,
                    Message = message,
                    Type = SmsType.DocumentRequest,
                    ClientId = clientId
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document request");
                return Result.Failure("Error sending request");
            }
        }

        public async Task<Result> SendTaxFilingConfirmationAsync(int taxFilingId)
        {
            try
            {
                var filing = await _context.TaxFilings
                    .Include(tf => tf.Client)
                    .FirstOrDefaultAsync(tf => tf.TaxFilingId == taxFilingId);

                if (filing?.Client == null || string.IsNullOrEmpty(filing.Client.PhoneNumber))
                {
                    return Result.Failure("Tax filing or client phone number not found");
                }

                var message = $"Tax Filed: Your {filing.TaxType} for {filing.TaxYear} has been submitted. Ref: {filing.FilingReference}. Payment due: {filing.DueDate:MMM dd} - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = filing.Client.PhoneNumber,
                    Message = message,
                    Type = SmsType.TaxFilingConfirmation,
                    ClientId = filing.ClientId,
                    TaxFilingId = taxFilingId
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tax filing confirmation");
                return Result.Failure("Error sending confirmation");
            }
        }

        public async Task<Result> SendPasswordResetAsync(string userId, string resetToken)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.ClientProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.ClientProfile == null || string.IsNullOrEmpty(user.ClientProfile.PhoneNumber))
                {
                    return Result.Failure("User or phone number not found");
                }

                var message = $"Password Reset: Your code is {resetToken}. Valid for 15 minutes. Do not share this code. - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = user.ClientProfile.PhoneNumber,
                    Message = message,
                    Type = SmsType.PasswordReset
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset SMS");
                return Result.Failure("Error sending SMS");
            }
        }

        public async Task<Result> SendTwoFactorCodeAsync(string userId, string code)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.ClientProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.ClientProfile == null || string.IsNullOrEmpty(user.ClientProfile.PhoneNumber))
                {
                    return Result.Failure("User or phone number not found");
                }

                var message = $"Security Code: {code} for your Betts Firm account. Valid for 5 minutes. Do not share. - The Betts Firm";

                var sendDto = new SendSmsDto
                {
                    PhoneNumber = user.ClientProfile.PhoneNumber,
                    Message = message,
                    Type = SmsType.TwoFactorAuth
                };

                var result = await SendSmsAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending 2FA SMS");
                return Result.Failure("Error sending SMS");
            }
        }

        public async Task<Result<SmsStatisticsDto>> GetSmsStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.SmsNotifications.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(s => s.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(s => s.CreatedDate <= toDate.Value);

                var smsData = await query.ToListAsync();

                var stats = new SmsStatisticsDto
                {
                    TotalSent = smsData.Count(s => s.Status == SmsStatus.Sent || s.Status == SmsStatus.Delivered),
                    TotalDelivered = smsData.Count(s => s.Status == SmsStatus.Delivered),
                    TotalFailed = smsData.Count(s => s.Status == SmsStatus.Failed),
                    TotalPending = smsData.Count(s => s.Status == SmsStatus.Pending),
                    TotalCost = smsData.Where(s => s.Cost.HasValue).Sum(s => s.Cost.Value),
                    ByType = smsData.GroupBy(s => s.Type)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ByProvider = smsData.GroupBy(s => s.Provider)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                // Daily count for last 30 days
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                stats.DailyCount = smsData
                    .Where(s => s.CreatedDate >= startDate)
                    .GroupBy(s => s.CreatedDate.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

                return Result.Success<SmsStatisticsDto>(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS statistics");
                return Result.Failure<SmsStatisticsDto>("Error retrieving statistics");
            }
        }

        public async Task<Result<Dictionary<string, decimal>>> GetSmsCostsByClientAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var costs = await _context.SmsNotifications
                    .Where(s => s.ClientId.HasValue && 
                               s.Cost.HasValue &&
                               s.CreatedDate >= fromDate && 
                               s.CreatedDate <= toDate)
                    .GroupBy(s => s.Client!.BusinessName)
                    .Select(g => new { ClientName = g.Key, TotalCost = g.Sum(s => s.Cost!.Value) })
                    .ToDictionaryAsync(x => x.ClientName, x => x.TotalCost);

                return Result.Success<Dictionary<string, decimal>>(costs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SMS costs by client");
                return Result.Failure<Dictionary<string, decimal>>("Error retrieving costs");
            }
        }

        public async Task<Result<List<SmsNotificationDto>>> GetFailedSmsAsync(int days = 7)
        {
            try
            {
                var since = DateTime.UtcNow.AddDays(-days);
                
                var failedSms = await _context.SmsNotifications
                    .Include(s => s.Client)
                    .Where(s => s.Status == SmsStatus.Failed && s.FailedDate >= since)
                    .OrderByDescending(s => s.FailedDate)
                    .ToListAsync();

                var dtos = failedSms.Select(MapToDto).ToList();
                return Result.Success<List<SmsNotificationDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed SMS");
                return Result.Failure<List<SmsNotificationDto>>("Error retrieving failed SMS");
            }
        }

        public async Task<Result<string>> FormatPhoneNumberAsync(string phoneNumber, string? countryCode = "232")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return Result.Failure<string>("Phone number is required");
                }

                countryCode = string.IsNullOrWhiteSpace(countryCode) ? "232" : countryCode;

                // Remove all non-digit characters
                var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

                // Remove leading zeros
                digits = digits.TrimStart('0');

                // Add country code if not present
                if (!digits.StartsWith(countryCode!, StringComparison.Ordinal))
                {
                    digits = countryCode + digits;
                }

                // Validate length (Sierra Leone: 232 + 8 digits = 11 total)
                if (digits.Length != 11)
                {
                    return Result.Failure<string>("Invalid phone number length");
                }

                return Result.Success<string>(digits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting phone number");
                return Result.Failure<string>("Error formatting phone number");
            }
        }

        public async Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return Result.Success<bool>(false);
                }

                var formatResult = await FormatPhoneNumberAsync(phoneNumber);
                if (!formatResult.IsSuccess)
                {
                    return Result.Success<bool>(false);
                }

                var formatted = formatResult.Value;

                // Check if it's a valid Sierra Leone number
                if (!string.IsNullOrEmpty(formatted) && formatted.StartsWith("232", StringComparison.Ordinal))
                {
                    var prefix = formatted.Substring(3, 2);
                    var validPrefixes = new[] { "76", "77", "78", "79", "30", "31", "32", "33", "34", "88", "80" };
                    return Result.Success<bool>(validPrefixes.Contains(prefix));
                }

                return Result.Success<bool>(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number");
                return Result.Failure<bool>("Error validating phone number");
            }
        }

        public Task<Result<int>> GetMessagePartCountAsync(string message)
        {
            try
            {
                const int singleSmsLimit = 160;
                const int multiPartLimit = 153;

                var length = message.Length;

                if (length <= singleSmsLimit)
                    return Task.FromResult(Result.Success<int>(1));

                var parts = (int)Math.Ceiling((double)length / multiPartLimit);
                return Task.FromResult(Result.Success<int>(parts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating message parts");
                return Task.FromResult(Result.Failure<int>("Error calculating message parts"));
            }
        }

        public async Task<Result<decimal>> EstimateSmsCostAsync(string message, SmsProvider? provider = null)
        {
            try
            {
                var partsResult = await GetMessagePartCountAsync(message);
                if (!partsResult.IsSuccess)
                {
                    return Result.Failure<decimal>("Failed to calculate message parts");
                }

                SmsProviderConfig? config;
                
                if (provider.HasValue)
                {
                    config = await _context.SmsProviderConfigs
                        .FirstOrDefaultAsync(c => c.Provider == provider.Value);
                }
                else
                {
                    config = await _context.SmsProviderConfigs
                        .FirstOrDefaultAsync(c => c.IsDefault);
                }

                if (config == null)
                {
                    return Result.Failure<decimal>("No provider configuration found");
                }

                var cost = partsResult.Value * config.CostPerSms;
                return Result.Success<decimal>(cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating SMS cost");
                return Result.Failure<decimal>("Error estimating cost");
            }
        }

        // Helper methods
        private async Task<ISmsProvider?> GetProviderAsync(SmsProvider providerType)
        {
            if (_providers.ContainsKey(providerType))
                return _providers[providerType];

            var config = await _context.SmsProviderConfigs
                .FirstOrDefaultAsync(c => c.Provider == providerType && c.IsActive);

            if (config == null)
                return null;

            ISmsProvider? provider = providerType switch
            {
                SmsProvider.OrangeSL => _serviceProvider.GetService<OrangeSLSmsProvider>(),
                // Add other providers as implemented
                _ => null
            };

            if (provider != null)
                _providers[providerType] = provider;

            return provider;
        }

        private async Task<ISmsProvider?> DetermineProviderAsync(string phoneNumber, SmsProvider? preferred = null)
        {
            if (preferred.HasValue)
            {
                var preferredProvider = await GetProviderAsync(preferred.Value);
                if (preferredProvider != null)
                    return preferredProvider;
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            // Determine by phone number prefix
            if (phoneNumber.StartsWith("23276", StringComparison.Ordinal) || phoneNumber.StartsWith("23277", StringComparison.Ordinal) || 
                phoneNumber.StartsWith("23278", StringComparison.Ordinal) || phoneNumber.StartsWith("23279", StringComparison.Ordinal))
            {
                return await GetProviderAsync(SmsProvider.OrangeSL);
            }

            // Default provider
            var defaultConfig = await _context.SmsProviderConfigs
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive);

            if (defaultConfig != null)
            {
                return await GetProviderAsync(defaultConfig.Provider);
            }

            return null;
        }

        private async Task<Result> SendSmsViaProviderAsync(SmsNotification sms, ISmsProvider provider)
        {
            try
            {
                var config = await _context.SmsProviderConfigs
                    .FirstOrDefaultAsync(c => c.Provider == provider.ProviderType);

                if (config == null)
                {
                    return Result.Failure("Provider configuration not found");
                }

                // Check limits
                if (config.DailyLimit.HasValue && config.DailyUsage >= config.DailyLimit.Value)
                {
                    return Result.Failure("Daily SMS limit reached");
                }

                // Send SMS
                var result = await provider.SendSmsAsync(sms.PhoneNumber, sms.Message, config.SenderId);
                
                if (result.IsSuccess && result.Value != null)
                {
                    sms.Status = SmsStatus.Sent;
                    sms.SentDate = DateTime.UtcNow;
                    sms.ProviderMessageId = result.Value.MessageId;
                    sms.Cost = result.Value.Cost ?? config.CostPerSms;
                    sms.Currency = config.Currency;

                    // Update provider usage
                    config.DailyUsage++;
                    config.MonthlyUsage++;

                    // Reset daily usage if needed
                    if (config.UsageResetDate?.Date < DateTime.UtcNow.Date)
                    {
                        config.DailyUsage = 1;
                        config.UsageResetDate = DateTime.UtcNow;
                    }
                }
                else
                {
                    sms.Status = SmsStatus.Failed;
                    sms.FailedDate = DateTime.UtcNow;
                    sms.ProviderResponse = result.ErrorMessage ?? "Unknown error";
                }

                await _context.SaveChangesAsync();
                
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Failed to send SMS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via provider");
                return Result.Failure($"Error: {ex.Message}");
            }
        }

        private async Task<DateTime?> GetClientDeadlineAsync(int clientId, SmsType smsType, TaxType? taxType)
        {
            // Logic to determine upcoming deadlines based on SMS type
            if (smsType == SmsType.DeadlineReminder && taxType.HasValue)
            {
                var filing = await _context.TaxFilings
                    .Where(tf => tf.ClientId == clientId && 
                                tf.TaxType == taxType.Value &&
                                tf.Status != FilingStatus.Filed &&
                                tf.DueDate > DateTime.UtcNow)
                    .OrderBy(tf => tf.DueDate)
                    .FirstOrDefaultAsync();

                return filing?.DueDate;
            }

            return null;
        }

        private string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            var result = template;
            
            foreach (var variable in variables)
            {
                result = Regex.Replace(result, $@"\{{{variable.Key}\}}", variable.Value, RegexOptions.IgnoreCase);
            }

            return result;
        }

        private SmsNotificationDto MapToDto(SmsNotification sms)
        {
            return new SmsNotificationDto
            {
                SmsNotificationId = sms.SmsNotificationId,
                PhoneNumber = sms.PhoneNumber,
                RecipientName = sms.RecipientName,
                UserId = sms.UserId,
                ClientId = sms.ClientId,
                ClientName = sms.Client?.BusinessName,
                Message = sms.Message,
                Type = sms.Type,
                Provider = sms.Provider,
                Status = sms.Status,
                ProviderMessageId = sms.ProviderMessageId,
                ProviderResponse = sms.ProviderResponse,
                Cost = sms.Cost,
                Currency = sms.Currency,
                CreatedDate = sms.CreatedDate,
                SentDate = sms.SentDate,
                DeliveredDate = sms.DeliveredDate,
                FailedDate = sms.FailedDate,
                RetryCount = sms.RetryCount,
                NextRetryDate = sms.NextRetryDate,
                TaxFilingId = sms.TaxFilingId,
                PaymentId = sms.PaymentId,
                DocumentId = sms.DocumentId,
                ScheduledDate = sms.ScheduledDate,
                IsScheduled = sms.IsScheduled
            };
        }

        private SmsTemplateDto MapToDto(SmsTemplate template)
        {
            var dto = new SmsTemplateDto
            {
                SmsTemplateId = template.SmsTemplateId,
                TemplateCode = template.TemplateCode,
                Name = template.Name,
                Description = template.Description,
                MessageTemplate = template.MessageTemplate,
                Type = template.Type,
                IsActive = template.IsActive,
                CharacterCount = template.CharacterCount
            };

            if (!string.IsNullOrEmpty(template.AvailableVariables))
            {
                try
                {
                    dto.AvailableVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(template.AvailableVariables) 
                        ?? new Dictionary<string, string>();
                }
                catch
                {
                    dto.AvailableVariables = new Dictionary<string, string>();
                }
            }

            return dto;
        }

        private SmsProviderConfigDto MapToDto(SmsProviderConfig config)
        {
            var dto = new SmsProviderConfigDto
            {
                SmsProviderConfigId = config.SmsProviderConfigId,
                Provider = config.Provider,
                Name = config.Name,
                ApiKey = "****", // Masked for security
                ApiSecret = "****", // Masked for security
                ApiUrl = config.ApiUrl,
                SenderId = config.SenderId,
                CostPerSms = config.CostPerSms,
                Currency = config.Currency,
                Priority = config.Priority,
                DailyLimit = config.DailyLimit,
                MonthlyLimit = config.MonthlyLimit,
                DailyUsage = config.DailyUsage,
                MonthlyUsage = config.MonthlyUsage,
                IsActive = config.IsActive,
                IsDefault = config.IsDefault
            };

            if (!string.IsNullOrEmpty(config.AdditionalSettings))
            {
                try
                {
                    dto.AdditionalSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(config.AdditionalSettings) 
                        ?? new Dictionary<string, string>();
                }
                catch
                {
                    dto.AdditionalSettings = new Dictionary<string, string>();
                }
            }

            return dto;
        }

        private SmsScheduleDto MapToDto(SmsSchedule schedule)
        {
            return new SmsScheduleDto
            {
                SmsScheduleId = schedule.SmsScheduleId,
                Name = schedule.Name,
                Description = schedule.Description,
                Type = schedule.Type,
                DaysBefore = schedule.DaysBefore,
                TimeOfDay = schedule.TimeOfDay,
                IsRecurring = schedule.IsRecurring,
                RecurrenceIntervalDays = schedule.RecurrenceIntervalDays,
                SmsTemplateId = schedule.SmsTemplateId,
                TemplateName = schedule.SmsTemplate?.Name,
                TaxType = schedule.TaxType,
                TaxpayerCategory = schedule.TaxpayerCategory,
                OnlyActiveClients = schedule.OnlyActiveClients,
                IsActive = schedule.IsActive,
                LastRunDate = schedule.LastRunDate
            };
        }
    }
}