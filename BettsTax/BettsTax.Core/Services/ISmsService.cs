using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface ISmsService
    {
        // SMS operations
        Task<Result<SmsNotificationDto>> SendSmsAsync(SendSmsDto dto);
        Task<Result<List<SmsNotificationDto>>> SendBulkSmsAsync(BulkSmsDto dto);
        Task<Result<SmsNotificationDto>> GetSmsAsync(int smsId);
        Task<Result<PagedResult<SmsNotificationDto>>> GetSmsHistoryAsync(
            string? phoneNumber = null, 
            int? clientId = null, 
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1, 
            int pageSize = 20);
        
        // Retry failed messages
        Task<Result> RetrySmsAsync(int smsId);
        Task<Result<int>> RetryFailedSmsAsync(DateTime? since = null);
        
        // Scheduled SMS
        Task<Result<List<SmsNotificationDto>>> GetScheduledSmsAsync();
        Task<Result> CancelScheduledSmsAsync(int smsId);
        Task<Result<int>> ProcessScheduledSmsAsync();
        
        // Templates
        Task<Result<List<SmsTemplateDto>>> GetSmsTemplatesAsync(SmsType? type = null);
        Task<Result<SmsTemplateDto>> GetSmsTemplateAsync(int templateId);
        Task<Result<SmsTemplateDto>> CreateSmsTemplateAsync(SmsTemplateDto dto);
        Task<Result<SmsTemplateDto>> UpdateSmsTemplateAsync(int templateId, SmsTemplateDto dto);
        Task<Result> DeleteSmsTemplateAsync(int templateId);
        Task<Result<SendSmsDto>> ApplySmsTemplateAsync(ApplySmsTemplateDto dto);
        
        // Provider configuration
        Task<Result<List<SmsProviderConfigDto>>> GetProviderConfigsAsync(bool activeOnly = true);
        Task<Result<SmsProviderConfigDto>> GetProviderConfigAsync(SmsProvider provider);
        Task<Result<SmsProviderConfigDto>> CreateProviderConfigAsync(SmsProviderConfigDto dto);
        Task<Result<SmsProviderConfigDto>> UpdateProviderConfigAsync(int configId, SmsProviderConfigDto dto);
        Task<Result> SetDefaultProviderAsync(SmsProvider provider);
        Task<Result<SmsBalanceDto>> CheckProviderBalanceAsync(SmsProvider provider);
        
        // Schedules
        Task<Result<List<SmsScheduleDto>>> GetSmsSchedulesAsync(bool activeOnly = true);
        Task<Result<SmsScheduleDto>> GetSmsScheduleAsync(int scheduleId);
        Task<Result<SmsScheduleDto>> CreateSmsScheduleAsync(SmsScheduleDto dto);
        Task<Result<SmsScheduleDto>> UpdateSmsScheduleAsync(int scheduleId, SmsScheduleDto dto);
        Task<Result> DeleteSmsScheduleAsync(int scheduleId);
        Task<Result<int>> RunScheduledSmsAsync(int scheduleId);
        
        // Specific notification types
        Task<Result> SendDeadlineReminderAsync(int clientId, string deadline, DateTime dueDate, int daysBefore);
        Task<Result> SendPaymentConfirmationAsync(int paymentId);
        Task<Result> SendDocumentRequestAsync(int clientId, List<string> documentNames, DateTime dueDate);
        Task<Result> SendTaxFilingConfirmationAsync(int taxFilingId);
        Task<Result> SendPasswordResetAsync(string userId, string resetToken);
        Task<Result> SendTwoFactorCodeAsync(string userId, string code);
        
        // Statistics and reporting
        Task<Result<SmsStatisticsDto>> GetSmsStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<Dictionary<string, decimal>>> GetSmsCostsByClientAsync(DateTime fromDate, DateTime toDate);
        Task<Result<List<SmsNotificationDto>>> GetFailedSmsAsync(int days = 7);
        
        // Utility methods
        Task<Result<string>> FormatPhoneNumberAsync(string phoneNumber, string? countryCode = "232");
        Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber);
        Task<Result<int>> GetMessagePartCountAsync(string message);
        Task<Result<decimal>> EstimateSmsCostAsync(string message, SmsProvider? provider = null);
    }
}