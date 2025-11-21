using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers.Admin
{
    /// <summary>
    /// Admin controller for managing configurable deadline rules
    /// Phase 3: Configurable Deadline Rules
    /// </summary>
    [ApiController]
    [Route("api/admin/deadline-rules")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class DeadlineRulesController : ControllerBase
    {
        private readonly IDeadlineRuleService _deadlineRuleService;
        private readonly ILogger<DeadlineRulesController> _logger;
        
        public DeadlineRulesController(
            IDeadlineRuleService deadlineRuleService,
            ILogger<DeadlineRulesController> logger)
        {
            _deadlineRuleService = deadlineRuleService;
            _logger = logger;
        }
        
        #region Deadline Rules
        
        /// <summary>
        /// Get all active deadline rules
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DeadlineRuleConfiguration>>> GetActiveRules([FromQuery] TaxType? taxType = null)
        {
            var result = await _deadlineRuleService.GetActiveRulesAsync(taxType);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Get deadline rule by ID
        /// </summary>
        [HttpGet("{ruleId}")]
        public async Task<ActionResult<DeadlineRuleConfiguration>> GetRuleById(int ruleId)
        {
            var result = await _deadlineRuleService.GetRuleByIdAsync(ruleId);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.ErrorMessage);
        }
        
        /// <summary>
        /// Create new deadline rule
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeadlineRuleConfiguration>> CreateRule([FromBody] DeadlineRuleConfiguration rule)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _deadlineRuleService.CreateRuleAsync(rule);
            return result.IsSuccess 
                ? CreatedAtAction(nameof(GetRuleById), new { ruleId = result.Value.DeadlineRuleConfigurationId }, result.Value)
                : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Update existing deadline rule
        /// </summary>
        [HttpPut("{ruleId}")]
        public async Task<ActionResult<DeadlineRuleConfiguration>> UpdateRule(int ruleId, [FromBody] DeadlineRuleConfiguration rule)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _deadlineRuleService.UpdateRuleAsync(ruleId, rule);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Delete deadline rule
        /// </summary>
        [HttpDelete("{ruleId}")]
        public async Task<ActionResult<bool>> DeleteRule(int ruleId)
        {
            var result = await _deadlineRuleService.DeleteRuleAsync(ruleId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Activate deadline rule
        /// </summary>
        [HttpPost("{ruleId}/activate")]
        public async Task<ActionResult<bool>> ActivateRule(int ruleId)
        {
            var result = await _deadlineRuleService.ActivateRuleAsync(ruleId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Deactivate deadline rule
        /// </summary>
        [HttpPost("{ruleId}/deactivate")]
        public async Task<ActionResult<bool>> DeactivateRule(int ruleId)
        {
            var result = await _deadlineRuleService.DeactivateRuleAsync(ruleId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Calculate deadline for a tax type
        /// </summary>
        [HttpPost("calculate")]
        public async Task<ActionResult<DateTime>> CalculateDeadline(
            [FromQuery] TaxType taxType,
            [FromQuery] DateTime triggerDate,
            [FromQuery] int? clientId = null)
        {
            var result = await _deadlineRuleService.CalculateDeadlineAsync(taxType, triggerDate, clientId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        #endregion
        
        #region Public Holidays
        
        /// <summary>
        /// Get public holidays for a year
        /// </summary>
        [HttpGet("holidays/{year}")]
        public async Task<ActionResult<List<PublicHoliday>>> GetHolidays(int year)
        {
            var result = await _deadlineRuleService.GetHolidaysAsync(year);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Add public holiday
        /// </summary>
        [HttpPost("holidays")]
        public async Task<ActionResult<PublicHoliday>> AddHoliday([FromBody] PublicHoliday holiday)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _deadlineRuleService.AddHolidayAsync(holiday);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Delete public holiday
        /// </summary>
        [HttpDelete("holidays/{holidayId}")]
        public async Task<ActionResult<bool>> DeleteHoliday(int holidayId)
        {
            var result = await _deadlineRuleService.DeleteHolidayAsync(holidayId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        #endregion
        
        #region Client Extensions
        
        /// <summary>
        /// Grant deadline extension to client
        /// </summary>
        [HttpPost("extensions")]
        public async Task<ActionResult<ClientDeadlineExtension>> GrantExtension([FromBody] ClientDeadlineExtension extension)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _deadlineRuleService.GrantExtensionAsync(extension);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Get client deadline extensions
        /// </summary>
        [HttpGet("extensions/client/{clientId}")]
        public async Task<ActionResult<List<ClientDeadlineExtension>>> GetClientExtensions(int clientId)
        {
            var result = await _deadlineRuleService.GetClientExtensionsAsync(clientId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Revoke deadline extension
        /// </summary>
        [HttpPost("extensions/{extensionId}/revoke")]
        public async Task<ActionResult<bool>> RevokeExtension(int extensionId)
        {
            var result = await _deadlineRuleService.RevokeExtensionAsync(extensionId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        #endregion
    }
}
