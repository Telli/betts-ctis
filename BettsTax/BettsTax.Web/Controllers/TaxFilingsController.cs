using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        private readonly ILogger<TaxFilingsController> _logger;

        public TaxFilingsController(ITaxFilingService taxFilingService, 
            IAssociatePermissionService permissionService, 
            IOnBehalfActionService onBehalfActionService,
            ILogger<TaxFilingsController> logger)
        {
            _taxFilingService = taxFilingService;
            _permissionService = permissionService;
            _onBehalfActionService = onBehalfActionService;
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