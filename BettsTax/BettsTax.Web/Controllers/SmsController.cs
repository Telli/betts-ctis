using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> SendSms([FromBody] SendSmsDto dto)
        {
            var result = await _smsService.SendSmsAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("send-bulk")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SendBulkSms([FromBody] BulkSmsDto dto)
        {
            var result = await _smsService.SendBulkSmsAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("{smsId}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetSms(int smsId)
        {
            var result = await _smsService.GetSmsAsync(smsId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("history")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetSmsHistory(
            string? phoneNumber = null,
            int? clientId = null,
            SmsStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var result = await _smsService.GetSmsHistoryAsync(phoneNumber, clientId, status, fromDate, toDate, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("{smsId}/retry")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RetrySms(int smsId)
        {
            var result = await _smsService.RetrySmsAsync(smsId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPost("retry-failed")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RetryFailedSms(DateTime? since = null)
        {
            var result = await _smsService.RetryFailedSmsAsync(since);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { retryCount = result.Value });
        }

        [HttpGet("scheduled")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetScheduledSms()
        {
            var result = await _smsService.GetScheduledSmsAsync();
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpDelete("{smsId}/cancel")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CancelScheduledSms(int smsId)
        {
            var result = await _smsService.CancelScheduledSmsAsync(smsId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPost("process-scheduled")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> ProcessScheduledSms()
        {
            var result = await _smsService.ProcessScheduledSmsAsync();
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { processedCount = result.Value });
        }

        [HttpGet("templates")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetSmsTemplates(SmsType? type = null)
        {
            var result = await _smsService.GetSmsTemplatesAsync(type);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetSmsTemplate(int templateId)
        {
            var result = await _smsService.GetSmsTemplateAsync(templateId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("templates")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateSmsTemplate([FromBody] SmsTemplateDto dto)
        {
            var result = await _smsService.CreateSmsTemplateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetSmsTemplate), new { templateId = result.Value.SmsTemplateId }, result.Value);
        }

        [HttpPut("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateSmsTemplate(int templateId, [FromBody] SmsTemplateDto dto)
        {
            var result = await _smsService.UpdateSmsTemplateAsync(templateId, dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpDelete("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteSmsTemplate(int templateId)
        {
            var result = await _smsService.DeleteSmsTemplateAsync(templateId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPost("templates/apply")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> ApplySmsTemplate([FromBody] ApplySmsTemplateDto dto)
        {
            var result = await _smsService.ApplySmsTemplateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("providers")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetProviderConfigs(bool activeOnly = true)
        {
            var result = await _smsService.GetProviderConfigsAsync(activeOnly);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("providers/{provider}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetProviderConfig(SmsProvider provider)
        {
            var result = await _smsService.GetProviderConfigAsync(provider);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("providers")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CreateProviderConfig([FromBody] SmsProviderConfigDto dto)
        {
            var result = await _smsService.CreateProviderConfigAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPut("providers/{configId}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> UpdateProviderConfig(int configId, [FromBody] SmsProviderConfigDto dto)
        {
            var result = await _smsService.UpdateProviderConfigAsync(configId, dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPut("providers/{provider}/default")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> SetDefaultProvider(SmsProvider provider)
        {
            var result = await _smsService.SetDefaultProviderAsync(provider);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpGet("providers/{provider}/balance")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CheckProviderBalance(SmsProvider provider)
        {
            var result = await _smsService.CheckProviderBalanceAsync(provider);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("schedules")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetSmsSchedules(bool activeOnly = true)
        {
            var result = await _smsService.GetSmsSchedulesAsync(activeOnly);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("schedules/{scheduleId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetSmsSchedule(int scheduleId)
        {
            var result = await _smsService.GetSmsScheduleAsync(scheduleId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("schedules")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateSmsSchedule([FromBody] SmsScheduleDto dto)
        {
            var result = await _smsService.CreateSmsScheduleAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetSmsSchedule), new { scheduleId = result.Value.SmsScheduleId }, result.Value);
        }

        [HttpPut("schedules/{scheduleId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateSmsSchedule(int scheduleId, [FromBody] SmsScheduleDto dto)
        {
            var result = await _smsService.UpdateSmsScheduleAsync(scheduleId, dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpDelete("schedules/{scheduleId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteSmsSchedule(int scheduleId)
        {
            var result = await _smsService.DeleteSmsScheduleAsync(scheduleId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPost("schedules/{scheduleId}/run")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RunScheduledSms(int scheduleId)
        {
            var result = await _smsService.RunScheduledSmsAsync(scheduleId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { sentCount = result.Value });
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetSmsStatistics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var result = await _smsService.GetSmsStatisticsAsync(fromDate, toDate);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("costs-by-client")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetSmsCostsByClient(DateTime fromDate, DateTime toDate)
        {
            var result = await _smsService.GetSmsCostsByClientAsync(fromDate, toDate);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("failed")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetFailedSms(int days = 7)
        {
            var result = await _smsService.GetFailedSmsAsync(days);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("validate-phone")]
        public async Task<IActionResult> ValidatePhoneNumber([FromBody] PhoneValidationDto dto)
        {
            var result = await _smsService.ValidatePhoneNumberAsync(dto.PhoneNumber);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { isValid = result.Value });
        }

        [HttpPost("format-phone")]
        public async Task<IActionResult> FormatPhoneNumber([FromBody] PhoneValidationDto dto)
        {
            var result = await _smsService.FormatPhoneNumberAsync(dto.PhoneNumber, dto.CountryCode);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { formattedNumber = result.Value });
        }

        [HttpPost("estimate-cost")]
        public async Task<IActionResult> EstimateSmsCost([FromBody] EstimateCostDto dto)
        {
            var result = await _smsService.EstimateSmsCostAsync(dto.Message, dto.Provider);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            var partsResult = await _smsService.GetMessagePartCountAsync(dto.Message);
            
            return Ok(new 
            { 
                estimatedCost = result.Value,
                messageParts = partsResult.IsSuccess ? partsResult.Value : 1,
                messageLength = dto.Message.Length
            });
        }
    }

    public record PhoneValidationDto(string PhoneNumber, string? CountryCode = "232");
    public record EstimateCostDto(string Message, SmsProvider? Provider = null);
}