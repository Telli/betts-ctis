using BettsTax.Web.Services;
using BettsTax.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data;
using BettsTax.Data.Models.Security;
using BettsTax.Core.Services;
using BettsTax.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using Models = BettsTax.Data.Models;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/tax-filings")]
    [Authorize]
    public class TaxFilingsController : ControllerBase
    {
        private readonly ITaxFilingService _taxFilingService;
        private readonly IAssociatePermissionService _permissionService;
        private readonly IOnBehalfActionService _onBehalfActionService;
        private readonly ITaxAuthorityService _taxAuthorityService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TaxFilingsController> _logger;

        public TaxFilingsController(ITaxFilingService taxFilingService,
            IAssociatePermissionService permissionService,
            IOnBehalfActionService onBehalfActionService,
            ITaxAuthorityService taxAuthorityService,
            ApplicationDbContext dbContext,
            ILogger<TaxFilingsController> logger)
        {
            _taxFilingService = taxFilingService;
            _permissionService = permissionService;
            _onBehalfActionService = onBehalfActionService;
            _taxAuthorityService = taxAuthorityService;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of tax filings with optional filtering
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> GetTaxFilings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] TaxType? taxType = null,
            [FromQuery] FilingStatus? status = null,
            [FromQuery] int? clientId = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _taxFilingService.GetTaxFilingsAsync(page, pageSize, search, taxType, status, clientId);

                return Ok(new
                {
                    success = true,
                    data = result.Items,
                    pagination = new
                    {
                        currentPage = result.Page,
                        pageSize = result.PageSize,
                        totalCount = result.TotalCount,
                        totalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax filings");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific tax filing by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTaxFiling(int id)
        {
            try
            {
                var taxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (taxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check authorization - clients can only see their own filings
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    // Additional authorization check would go here
                    // For now, assume service handles client-specific filtering
                }

                return Ok(new { success = true, data = taxFiling });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get tax filings for a specific client
        /// </summary>
        [HttpGet("client/{clientId}")]
        [AssociatePermission("TaxFilings", AssociatePermissionLevel.Read, ClientIdSource.Route)]
        public async Task<ActionResult<object>> GetClientTaxFilings(int clientId)
        {
            try
            {
                var taxFilings = await _taxFilingService.GetTaxFilingsByClientIdAsync(clientId);
                return Ok(new { success = true, data = taxFilings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax filings for client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get tax filings the associate can manage on behalf of clients
        /// </summary>
        [HttpGet("associate/delegated")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetDelegatedTaxFilings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] TaxType? taxType = null,
            [FromQuery] FilingStatus? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Get delegated client IDs for tax filings
                var delegatedClientIds = await _permissionService.GetDelegatedClientIdsAsync(userId, "TaxFilings");

                if (!delegatedClientIds.Any() && !User.IsInRole("Admin") && !User.IsInRole("SystemAdmin"))
                {
                    return Ok(new
                    {
                        success = true,
                        data = new List<object>(),
                        pagination = new { currentPage = 1, pageSize, totalCount = 0, totalPages = 0 }
                    });
                }

                var allResults = await _taxFilingService.GetTaxFilingsForClientsAsync(
                    delegatedClientIds, search, taxType, status);

                // Apply pagination manually
                var totalCount = allResults.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var skip = (page - 1) * pageSize;
                var paginatedResults = allResults.Skip(skip).Take(pageSize).ToList();

                return Ok(new
                {
                    success = true,
                    data = paginatedResults,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delegated tax filings for associate");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new tax filing
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> CreateTaxFiling([FromBody] CreateTaxFilingDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Check permission if associate is creating for a client
                if (User.IsInRole("Associate"))
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId, createDto.ClientId, "TaxFilings", AssociatePermissionLevel.Create);

                    if (!hasPermission)
                    {
                        return Forbid("Insufficient permissions to create tax filing for this client");
                    }
                }

                var taxFiling = await _taxFilingService.CreateTaxFilingAsync(createDto, userId);

                // Log on-behalf action if associate created for client
                if (User.IsInRole("Associate"))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId, createDto.ClientId, "Create", "TaxFiling", taxFiling.TaxFilingId,
                        oldValues: null, newValues: taxFiling,
                        reason: "Tax filing created on behalf of client",
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent: HttpContext.Request.Headers.UserAgent);
                }

                return CreatedAtAction(
                    nameof(GetTaxFiling),
                    new { id = taxFiling.TaxFilingId },
                    new { success = true, data = taxFiling });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating tax filing");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tax filing");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new tax filing on behalf of a client
        /// </summary>
        [HttpPost("client/{clientId}")]
        [AssociatePermission("TaxFilings", AssociatePermissionLevel.Create, ClientIdSource.Route)]
        public async Task<ActionResult<object>> CreateTaxFilingOnBehalf(int clientId, [FromBody] CreateTaxFilingDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                // Ensure the clientId in route matches the DTO
                createDto.ClientId = clientId;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var taxFiling = await _taxFilingService.CreateTaxFilingOnBehalfAsync(createDto, userId, clientId);

                // Log on-behalf action
                await _onBehalfActionService.LogActionAsync(
                    userId, clientId, "Create", "TaxFiling", taxFiling.TaxFilingId,
                    oldValues: null, newValues: taxFiling,
                    reason: "Tax filing created on behalf of client via on-behalf endpoint",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers.UserAgent);

                return CreatedAtAction(
                    nameof(GetTaxFiling),
                    new { id = taxFiling.TaxFilingId },
                    new { success = true, data = taxFiling, message = "Tax filing created on behalf of client" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating tax filing on behalf of client {ClientId}", clientId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tax filing on behalf of client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update an existing tax filing
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> UpdateTaxFiling(int id, [FromBody] UpdateTaxFilingDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Get the existing tax filing to check client access
                var existingTaxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (existingTaxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check permission if associate is updating
                if (User.IsInRole("Associate"))
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId, existingTaxFiling.ClientId, "TaxFilings", AssociatePermissionLevel.Update);

                    if (!hasPermission)
                    {
                        return Forbid("Insufficient permissions to update tax filing for this client");
                    }
                }

                var taxFiling = await _taxFilingService.UpdateTaxFilingAsync(id, updateDto, userId);

                // Log on-behalf action if associate updated
                if (User.IsInRole("Associate"))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId, existingTaxFiling.ClientId, "Update", "TaxFiling", id,
                        oldValues: existingTaxFiling, newValues: taxFiling,
                        reason: "Tax filing updated on behalf of client",
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent: HttpContext.Request.Headers.UserAgent);
                }

                return Ok(new { success = true, data = taxFiling });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation updating tax filing {TaxFilingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update an existing tax filing on behalf of a client
        /// </summary>
        [HttpPut("{id}/on-behalf")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> UpdateTaxFilingOnBehalf(int id, [FromBody] UpdateTaxFilingDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Get the existing tax filing to check client access
                var existingTaxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (existingTaxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check permission
                var hasPermission = await _permissionService.HasPermissionAsync(
                    userId, existingTaxFiling.ClientId, "TaxFilings", AssociatePermissionLevel.Update);

                if (!hasPermission && !User.IsInRole("Admin") && !User.IsInRole("SystemAdmin"))
                {
                    return Forbid("Insufficient permissions to update tax filing for this client");
                }

                var taxFiling = await _taxFilingService.UpdateTaxFilingOnBehalfAsync(id, updateDto, userId, existingTaxFiling.ClientId);

                // Log on-behalf action
                await _onBehalfActionService.LogActionAsync(
                    userId, existingTaxFiling.ClientId, "Update", "TaxFiling", id,
                    oldValues: existingTaxFiling, newValues: taxFiling,
                    reason: "Tax filing updated on behalf of client via on-behalf endpoint",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers.UserAgent);

                return Ok(new { success = true, data = taxFiling, message = "Tax filing updated on behalf of client" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation updating tax filing {TaxFilingId} on behalf", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tax filing {TaxFilingId} on behalf", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a tax filing (only drafts)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> DeleteTaxFiling(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _taxFilingService.DeleteTaxFilingAsync(id, userId);

                if (!result)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                return Ok(new { success = true, message = "Tax filing deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation deleting tax filing {TaxFilingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }


	        /// <summary>
	        /// Validate a tax filing before submission
	        /// </summary>
	        [HttpGet("{id}/validate")]
	        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
	        public async Task<ActionResult<object>> ValidateTaxFiling(int id)
	        {
	            try
	            {
	                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

	                var existingTaxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
	                if (existingTaxFiling == null)
	                {
	                    return NotFound(new { success = false, message = "Tax filing not found" });
	                }

	                // Check permission if associate is validating
	                if (User.IsInRole("Associate"))
	                {
	                    var hasPermission = await _permissionService.HasPermissionAsync(
	                        userId, existingTaxFiling.ClientId, "TaxFilings", AssociatePermissionLevel.Submit);

	                    if (!hasPermission)
	                    {
	                        return Forbid("Insufficient permissions to validate tax filing for this client");
	                    }
	                }

	                var validation = await _taxFilingService.ValidateTaxFilingForSubmissionAsync(id);

	                return Ok(new { success = true, data = validation });
	            }
	            catch (Exception ex)
	            {
	                _logger.LogError(ex, "Error validating tax filing {TaxFilingId}", id);
	                return StatusCode(500, new { success = false, message = "Internal server error" });
	            }
	        }

        /// <summary>
        /// Submit a tax filing for review
        /// </summary>
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> SubmitTaxFiling(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Get the existing tax filing to check client access
                var existingTaxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (existingTaxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check permission if associate is submitting
                if (User.IsInRole("Associate"))
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId, existingTaxFiling.ClientId, "TaxFilings", AssociatePermissionLevel.Submit);

                    if (!hasPermission)
                    {
                        return Forbid("Insufficient permissions to submit tax filing for this client");
                    }
                }

                var taxFiling = await _taxFilingService.SubmitTaxFilingAsync(id, userId);

                // Log on-behalf action if associate submitted
                if (User.IsInRole("Associate"))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId, existingTaxFiling.ClientId, "Submit", "TaxFiling", id,
                        oldValues: existingTaxFiling, newValues: taxFiling,
                        reason: "Tax filing submitted on behalf of client",
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent: HttpContext.Request.Headers.UserAgent);
                }

                return Ok(new { success = true, data = taxFiling, message = "Tax filing submitted for review" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation submitting tax filing {TaxFilingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Submit a tax filing for review on behalf of a client
        /// </summary>
        [HttpPost("{id}/submit-on-behalf")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> SubmitTaxFilingOnBehalf(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

                // Get the existing tax filing to check client access
                var existingTaxFiling = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (existingTaxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check permission
                var hasPermission = await _permissionService.HasPermissionAsync(
                    userId, existingTaxFiling.ClientId, "TaxFilings", AssociatePermissionLevel.Submit);

                if (!hasPermission && !User.IsInRole("Admin") && !User.IsInRole("SystemAdmin"))
                {
                    return Forbid("Insufficient permissions to submit tax filing for this client");
                }

                var taxFiling = await _taxFilingService.SubmitTaxFilingOnBehalfAsync(id, userId, existingTaxFiling.ClientId);

                // Log on-behalf action
                await _onBehalfActionService.LogActionAsync(
                    userId, existingTaxFiling.ClientId, "Submit", "TaxFiling", id,
                    oldValues: existingTaxFiling, newValues: taxFiling,
                    reason: "Tax filing submitted on behalf of client via on-behalf endpoint",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers.UserAgent);

                return Ok(new { success = true, data = taxFiling, message = "Tax filing submitted for review on behalf of client" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation submitting tax filing {TaxFilingId} on behalf", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting tax filing {TaxFilingId} on behalf", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Review a tax filing (approve/reject)
        /// </summary>
        [HttpPost("{id}/review")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> ReviewTaxFiling(int id, [FromBody] ReviewTaxFilingDto reviewDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var taxFiling = await _taxFilingService.ReviewTaxFilingAsync(id, reviewDto, userId);

                return Ok(new { success = true, data = taxFiling, message = $"Tax filing {reviewDto.Status.ToString().ToLower()}" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation reviewing tax filing {TaxFilingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get upcoming tax filing deadlines
        /// </summary>
        [HttpGet("deadlines")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> GetUpcomingDeadlines([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365) days = 30;

                var deadlines = await _taxFilingService.GetUpcomingDeadlinesAsync(days);
                return Ok(new { success = true, data = deadlines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming deadlines");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Calculate tax liability
        /// </summary>
        [HttpPost("calculate-liability")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult<object>> CalculateTaxLiability([FromBody] CalculateTaxRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var liability = await _taxFilingService.CalculateTaxLiabilityAsync(
                    request.ClientId, request.TaxType, request.TaxYear, request.TaxableAmount);

                return Ok(new { success = true, data = new { taxLiability = liability } });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation calculating tax liability");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating tax liability");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Submit tax filing to tax authority
        /// </summary>
        [HttpPost("{id}/submit-to-authority")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        [AssociatePermission("TaxFilings", AssociatePermissionLevel.Submit)]
        public async Task<ActionResult<object>> SubmitToTaxAuthority(int id)
        {
            try
            {
                var result = await _taxAuthorityService.SubmitTaxFilingAsync(id);

                if (result.Success)
                {
                    _logger.LogInformation("Tax filing {TaxFilingId} submitted to authority with reference {Reference}",
                        id, result.Reference);
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    _logger.LogWarning("Failed to submit tax filing {TaxFilingId} to authority: {Message}",
                        id, result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting tax filing {TaxFilingId} to authority", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Check tax filing status with tax authority
        /// </summary>
        [HttpGet("{id}/authority-status")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        [AssociatePermission("TaxFilings", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetTaxAuthorityStatus(int id)
        {
            try
            {
                // Get the tax filing entity with submissions
                var taxFiling = await _dbContext.TaxFilings
                    .Include(t => t.TaxAuthoritySubmissions)
                    .FirstOrDefaultAsync(t => t.TaxFilingId == id);

                if (taxFiling == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Find the latest submission for this tax filing
                var submission = taxFiling.TaxAuthoritySubmissions?
                    .OrderByDescending(s => s.SubmittedAt)
                    .FirstOrDefault();

                if (submission == null || string.IsNullOrEmpty(submission.AuthorityReference))
                {
                    return BadRequest(new { success = false, message = "No authority submission found for this tax filing" });
                }

                var result = await _taxAuthorityService.CheckFilingStatusAsync(submission.AuthorityReference);

                if (result.Success)
                {
                    _logger.LogInformation("Status check completed for tax filing {TaxFilingId}, reference {Reference}: {Status}",
                        id, submission.AuthorityReference, result.Status);
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    _logger.LogWarning("Failed to check status for tax filing {TaxFilingId}, reference {Reference}: {Message}",
                        id, submission.AuthorityReference, result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking tax authority status for tax filing {TaxFilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate tax authority configuration
        /// </summary>
        [HttpGet("authority-config/validate")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> ValidateTaxAuthorityConfig()
        {
            try
            {
                var isValid = await _taxAuthorityService.ValidateConfigurationAsync();

                if (isValid)
                {
                    return Ok(new { success = true, message = "Tax authority configuration is valid" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Tax authority configuration is invalid or unreachable" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tax authority configuration");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // FILING WORKSPACE ENDPOINTS

        /// <summary>
        /// Get complete filing workspace data
        /// </summary>
        [HttpGet("{id}/workspace")]
        public async Task<ActionResult<object>> GetFilingWorkspace(int id)
        {
            try
            {
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    // Clients can only access their own filings
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    // Associates can only access delegated client filings
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }
                // Admin and SystemAdmin have full access

                // Return complete workspace data
                return Ok(new { success = true, data = filing });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filing workspace for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get schedule rows for a filing
        /// </summary>
        [HttpGet("{id}/schedules")]
        public async Task<ActionResult<object>> GetSchedules(int id)
        {
            try
            {
                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }

                var schedules = await _dbContext.FilingSchedules
                    .Where(s => s.TaxFilingId == id)
                    .OrderBy(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.Description,
                        s.Amount,
                        s.Taxable
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = schedules });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedules for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Save schedule rows for a filing
        /// </summary>
        [HttpPost("{id}/schedules")]
        public async Task<ActionResult<object>> SaveSchedules(int id, [FromBody] List<FilingScheduleDto> schedules)
        {
            try
            {
                // Validate input
                if (schedules == null)
                {
                    return BadRequest(new { success = false, message = "Schedules data is required" });
                }

                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check filing status - only allow edits for Draft status
                if (filing.Status != FilingStatus.Draft)
                {
                    return BadRequest(new { success = false, message = "Cannot modify schedules for a filing that is not in Draft status" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only modify your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Update);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to modify this tax filing");
                    }
                }

                // Use transaction to ensure data consistency
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Delete existing schedules
                    var existing = await _dbContext.FilingSchedules
                        .Where(s => s.TaxFilingId == id)
                        .ToListAsync();
                    _dbContext.FilingSchedules.RemoveRange(existing);

                    // Add new schedules with validation
                    foreach (var schedule in schedules)
                    {
                        if (string.IsNullOrWhiteSpace(schedule.Description))
                        {
                            throw new ArgumentException("Schedule description cannot be empty");
                        }
                        if (schedule.Amount < 0 || schedule.Taxable < 0)
                        {
                            throw new ArgumentException("Schedule amounts cannot be negative");
                        }

                        _dbContext.FilingSchedules.Add(new FilingSchedule
                        {
                            TaxFilingId = id,
                            Description = schedule.Description.Trim(),
                            Amount = schedule.Amount,
                            Taxable = schedule.Taxable
                        });
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = true, message = "Schedules saved successfully" });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving schedules for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Import schedules from CSV/Excel
        /// </summary>
        [HttpPost("{id}/schedules/import")]
        public async Task<ActionResult<object>> ImportSchedules(int id, IFormFile file)
        {
            try
            {
                // File validation
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                // File size limit (10MB)
                const long maxFileSize = 10 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { success = false, message = "File size exceeds 10MB limit" });
                }

                // File type validation
                var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only CSV and Excel files are allowed" });
                }

                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Check filing status
                if (filing.Status != FilingStatus.Draft)
                {
                    return BadRequest(new { success = false, message = "Cannot import schedules for a filing that is not in Draft status" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only modify your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Update);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to modify this tax filing");
                    }
                }

                var schedules = new List<FilingSchedule>();

                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    // Skip header row
                    await reader.ReadLineAsync();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length < 3) continue;

                        schedules.Add(new FilingSchedule
                        {
                            TaxFilingId = id,
                            Description = values[0].Trim(),
                            Amount = decimal.TryParse(values[1].Trim(), out var amount) ? amount : 0,
                            Taxable = decimal.TryParse(values[2].Trim(), out var taxable) ? taxable : 0
                        });
                    }
                }

                // Delete existing schedules
                var existing = await _dbContext.FilingSchedules
                    .Where(s => s.TaxFilingId == id)
                    .ToListAsync();
                _dbContext.FilingSchedules.RemoveRange(existing);

                // Add imported schedules
                _dbContext.FilingSchedules.AddRange(schedules);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Imported {Count} schedules for filing {FilingId}", schedules.Count, id);
                return Ok(new { success = true, message = $"Imported {schedules.Count} schedules successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing schedules for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Failed to import schedules. Please check the file format." });
            }
        }

        /// <summary>
        /// Get calculated assessment summary
        /// </summary>
        [HttpGet("{id}/assessment")]
        public async Task<ActionResult<object>> GetAssessment(int id)
        {
            try
            {
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }

                // Get schedules for accurate calculation
                var schedules = await _dbContext.FilingSchedules
                    .Where(s => s.TaxFilingId == id)
                    .ToListAsync();

                var totalSales = schedules.Sum(s => s.Amount);
                var taxableSales = schedules.Sum(s => s.Taxable);
                var inputTaxCredit = schedules.Where(s => s.Description.Contains("Input", StringComparison.OrdinalIgnoreCase))
                    .Sum(s => s.Taxable * 0.15m); // Assuming 15% GST rate for input credit

                var gstRate = 15m; // Default GST rate, should come from tax rates configuration
                var outputTax = taxableSales * (gstRate / 100m);

                // Calculate assessment
                // Note: TaxFilingDto doesn't have TaxableAmount or PenaltyAmount, use 0 as default
                var assessment = new
                {
                    TotalSales = totalSales > 0 ? totalSales : 0,
                    TaxableSales = taxableSales > 0 ? taxableSales : 0,
                    GstRate = gstRate,
                    OutputTax = outputTax,
                    InputTaxCredit = inputTaxCredit,
                    Penalties = 0,
                    TotalPayable = outputTax - inputTaxCredit
                };

                return Ok(new { success = true, data = assessment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating assessment for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get documents for a filing
        /// </summary>
        [HttpGet("{id}/documents")]
        public async Task<ActionResult<object>> GetFilingDocuments(int id)
        {
            try
            {
                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }

                var documents = await _dbContext.Documents
                    .Where(d => d.TaxFilingId == id)
                    .Include(d => d.UploadedBy)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new
                    {
                        id = d.DocumentId,
                        Name = d.OriginalFileName,
                        Version = d.CurrentVersionNumber,
                        UploadedBy = d.UploadedBy != null ? d.UploadedBy.FirstName + " " + d.UploadedBy.LastName : "Unknown",
                        UploadedAt = d.UploadedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload document for a filing
        /// </summary>
        [HttpPost("{id}/documents")]
        public async Task<ActionResult<object>> UploadFilingDocument(int id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                // File size limit (50MB)
                const long maxFileSize = 50 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { success = false, message = "File size exceeds 50MB limit" });
                }

                // File type validation
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Allowed: PDF, DOC, DOCX, XLS, XLSX, JPG, PNG" });
                }

                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only upload documents to your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Update);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to upload documents for this tax filing");
                    }
                }

                // Generate unique file name
                var fileName = $"{id}_{Guid.NewGuid()}{fileExtension}";
                var uploadPath = Path.Combine("uploads", "filing-documents", fileName);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                // Save file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get user name for audit
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // Create document record
                var document = new Document
                {
                    TaxFilingId = id,
                    ClientId = filing.ClientId,
                    OriginalFileName = file.FileName,
                    StoredFileName = fileName,
                    FilePath = uploadPath,
                    StoragePath = uploadPath,
                    FileSize = file.Length,
                    Size = file.Length,
                    ContentType = file.ContentType,
                    UploadedById = userId,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Documents.Add(document);
                await _dbContext.SaveChangesAsync();

                // Log audit trail
                var auditLog = new Models.Security.AuditLog
                {
                    UserId = userId ?? "System",
                    Action = "UploadDocument",
                    Entity = "Document",
                    EntityId = document.DocumentId.ToString(),
                    Description = $"Document uploaded for filing {id}: {file.FileName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Severity = Models.Security.AuditSeverity.Low,
                    Category = Models.Security.AuditCategory.DataModification
                };
                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Document uploaded for filing {FilingId} by {UserId}: {FileName}", id, userId, file.FileName);

                return Ok(new { success = true, message = "Document uploaded successfully", data = new { document.DocumentId, document.OriginalFileName } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Download filing document
        /// </summary>
        [HttpGet("{id}/documents/{documentId}")]
        public async Task<ActionResult> DownloadFilingDocument(int id, int documentId)
        {
            try
            {
                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only download documents from your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }

                var document = await _dbContext.Documents
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.TaxFilingId == id);

                if (document == null)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                // Check if file exists - try both FilePath and StoragePath
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(),
                    !string.IsNullOrEmpty(document.FilePath) ? document.FilePath : document.StoragePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new { success = false, message = "File not found on server" });
                }

                // Log download in audit trail
                var auditLog = new Models.Security.AuditLog
                {
                    UserId = userId ?? "System",
                    Action = "DownloadDocument",
                    Entity = "Document",
                    EntityId = documentId.ToString(),
                    Description = $"Document downloaded: {document.OriginalFileName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Severity = Models.Security.AuditSeverity.Low,
                    Category = Models.Security.AuditCategory.DataModification
                };
                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return File(fileBytes, document.ContentType ?? "application/octet-stream", document.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId} for filing {FilingId}", documentId, id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get filing history/audit trail
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<object>> GetFilingHistory(int id)
        {
            try
            {
                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Read);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to access this tax filing");
                    }
                }

                // Get audit trail entries for this filing
                var history = await _dbContext.AuditLogs
                    .Where(a => a.Entity == "TaxFiling" && a.EntityId == id.ToString())
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => new
                    {
                        id = a.Id,
                        Timestamp = a.Timestamp,
                        User = a.User != null ? a.User.FirstName + " " + a.User.LastName : "System",
                        Action = a.Action,
                        Changes = a.Description ?? ""
                    })
                    .ToListAsync();

                // Also include filing status changes from the filing itself
                var filingHistory = new List<object>(history);

                if (filing.SubmittedDate.HasValue)
                {
                    filingHistory.Insert(0, new
                    {
                        id = -1,
                        Timestamp = filing.SubmittedDate.Value,
                        User = filing.SubmittedByName ?? "Unknown",
                        Action = "Submitted",
                        Changes = $"Filing submitted for review"
                    });
                }

                if (filing.ReviewedDate.HasValue)
                {
                    filingHistory.Insert(0, new
                    {
                        id = -2,
                        Timestamp = filing.ReviewedDate.Value,
                        User = filing.ReviewedByName ?? "Unknown",
                        Action = filing.Status == FilingStatus.Approved ? "Approved" : "Rejected",
                        Changes = filing.ReviewComments ?? ""
                    });
                }

                filingHistory.Insert(0, new
                {
                    id = -3,
                    Timestamp = filing.CreatedDate,
                    User = "System",
                    Action = "Created",
                    Changes = $"Filing created for {filing.TaxType} - {filing.TaxYear}"
                });

                return Ok(new { success = true, data = filingHistory.OrderByDescending(h => ((dynamic)h).Timestamp) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving history for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Save filing draft
        /// </summary>
        [HttpPost("{id}/save-draft")]
        public async Task<ActionResult<object>> SaveDraft(int id, [FromBody] object filingData)
        {
            try
            {
                // Verify filing exists and user has access
                var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
                if (filing == null)
                {
                    return NotFound(new { success = false, message = "Tax filing not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || filing.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only modify your own tax filings");
                    }
                }
                else if (userRole == "Associate")
                {
                    var hasPermission = await _permissionService.HasPermissionAsync(
                        userId!, filing.ClientId, "TaxFilings", AssociatePermissionLevel.Update);
                    if (!hasPermission)
                    {
                        return Forbid("You don't have permission to modify this tax filing");
                    }
                }

                // Parse and update filing data
                // Note: This is a simplified implementation - in production, use proper DTOs
                var updateDto = System.Text.Json.JsonSerializer.Deserialize<UpdateTaxFilingDto>(
                    filingData.ToString() ?? "{}",
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateDto != null)
                {
                    await _taxFilingService.UpdateTaxFilingAsync(id, updateDto, userId ?? "System");
                }

                // Log audit trail
                var auditLog = new Models.Security.AuditLog
                {
                    UserId = userId ?? "System",
                    Action = "SaveDraft",
                    Entity = "TaxFiling",
                    EntityId = id.ToString(),
                    Description = "Filing draft saved",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    Severity = Models.Security.AuditSeverity.Low,
                    Category = Models.Security.AuditCategory.DataModification
                };
                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Draft saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft for filing {FilingId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Request model for tax liability calculation
    /// </summary>
    public class CalculateTaxRequest
    {
        public int ClientId { get; set; }
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public decimal TaxableAmount { get; set; }
    }
}