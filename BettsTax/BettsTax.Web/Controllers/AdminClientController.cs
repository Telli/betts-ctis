using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/admin/clients")]
    [Authorize(Policy = "AdminOrAssociate")]
    public class AdminClientController : ControllerBase
    {
        private readonly IAdminClientService _adminClientService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminClientController> _logger;

        public AdminClientController(
            IAdminClientService adminClientService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminClientController> logger)
        {
            _adminClientService = adminClientService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of clients with overview information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetClients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] ClientStatus? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _adminClientService.GetClientOverviewAsync(page, pageSize, search, status);

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
                _logger.LogError(ex, "Error retrieving client overview");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed information for a specific client
        /// </summary>
        [HttpGet("{clientId}")]
        public async Task<ActionResult<object>> GetClientDetail(int clientId)
        {
            try
            {
                var client = await _adminClientService.GetClientDetailAsync(clientId);
                if (client == null)
                {
                    return NotFound(new { success = false, message = "Client not found" });
                }

                return Ok(new { success = true, data = client });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client detail for {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client statistics for admin dashboard
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetClientStats()
        {
            try
            {
                var stats = await _adminClientService.GetClientStatsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get audit logs for a specific client
        /// </summary>
        [HttpGet("{clientId}/audit-logs")]
        public async Task<ActionResult<object>> GetClientAuditLogs(
            int clientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var logs = await _adminClientService.GetClientAuditLogsAsync(clientId, page, pageSize);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Activate or deactivate a client
        /// </summary>
        [HttpPost("{clientId}/status")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> UpdateClientStatus(int clientId, [FromBody] ClientActivationDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                bool result;

                if (request.Activate)
                {
                    result = await _adminClientService.ActivateClientAsync(clientId, adminUserId);
                }
                else
                {
                    if (string.IsNullOrEmpty(request.Reason))
                    {
                        return BadRequest(new { success = false, message = "Reason is required for deactivation" });
                    }
                    result = await _adminClientService.DeactivateClientAsync(clientId, adminUserId, request.Reason);
                }

                if (!result)
                {
                    return NotFound(new { success = false, message = "Client not found" });
                }

                return Ok(new 
                { 
                    success = true, 
                    message = request.Activate ? "Client activated successfully" : "Client deactivated successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client status for {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Assign an associate to a client
        /// </summary>
        [HttpPost("{clientId}/assign-associate")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> AssignAssociate(int clientId, [FromBody] AssignAssociateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _adminClientService.AssignAssociateAsync(clientId, request.AssociateUserId, adminUserId);

                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to assign associate. Check if client and associate exist and associate has proper role." });
                }

                return Ok(new { success = true, message = "Associate assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning associate to client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get list of available associates for assignment
        /// </summary>
        [HttpGet("associates")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetAssociates()
        {
            try
            {
                var associates = await _userManager.GetUsersInRoleAsync("Associate");
                var associateList = associates.Select(a => new AdminAssociateDto
                {
                    UserId = a.Id,
                    FullName = $"{a.FirstName} {a.LastName}".Trim(),
                    Email = a.Email!,
                    IsActive = a.IsActive,
                    AssignedClientsCount = 0 // This could be calculated if needed
                }).ToList();

                return Ok(new { success = true, data = associateList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving associates list");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client activity summary for monitoring
        /// </summary>
        [HttpGet("activity-summary")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetActivitySummary([FromQuery] int days = 7)
        {
            try
            {
                // This could be enhanced to provide more detailed activity analytics
                var stats = await _adminClientService.GetClientStatsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity summary");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Export client data for reporting (placeholder for future implementation)
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> ExportClientData([FromQuery] string format = "csv")
        {
            try
            {
                // Placeholder for export functionality
                // This could generate CSV, Excel, or PDF reports
                return Ok(new { success = false, message = "Export functionality not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting client data");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}