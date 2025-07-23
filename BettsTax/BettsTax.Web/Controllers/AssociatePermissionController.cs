using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/associate-permissions")]
    [Authorize]
    public class AssociatePermissionController : ControllerBase
    {
        private readonly IAssociatePermissionService _permissionService;
        private readonly IOnBehalfActionService _onBehalfActionService;
        private readonly ILogger<AssociatePermissionController> _logger;

        public AssociatePermissionController(
            IAssociatePermissionService permissionService,
            IOnBehalfActionService onBehalfActionService,
            ILogger<AssociatePermissionController> logger)
        {
            _permissionService = permissionService;
            _onBehalfActionService = onBehalfActionService;
            _logger = logger;
        }

        /// <summary>
        /// Get all permissions for a specific associate
        /// </summary>
        [HttpGet("{associateId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetAssociatePermissions(
            string associateId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissions = await _permissionService.GetAssociatePermissionsAsync(associateId);
                
                // Apply pagination manually
                var totalCount = permissions.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var skip = (page - 1) * pageSize;
                var paginatedPermissions = permissions.Skip(skip).Take(pageSize).ToList();

                return Ok(new
                {
                    success = true,
                    data = paginatedPermissions,
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
                _logger.LogError(ex, "Error getting permissions for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Check if associate has specific permission for client
        /// </summary>
        [HttpGet("{associateId}/check")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> CheckPermission(
            string associateId,
            [FromQuery] int clientId,
            [FromQuery] string area,
            [FromQuery] AssociatePermissionLevel level)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Associates can only check their own permissions, admins can check anyone's
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only check your own permissions");
                }

                var hasPermission = await _permissionService.HasPermissionAsync(associateId, clientId, area, level);
                var effectiveLevel = await _permissionService.GetEffectivePermissionLevelAsync(associateId, clientId, area);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        hasPermission,
                        effectiveLevel,
                        restrictions = new
                        {
                            // Add any restrictions like amount thresholds, approval requirements, etc.
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get clients delegated to associate for specific area
        /// </summary>
        [HttpGet("{associateId}/clients")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetDelegatedClients(
            string associateId,
            [FromQuery] string? area = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Associates can only get their own delegated clients
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own delegated clients");
                }

                var clients = area != null 
                    ? await _permissionService.GetDelegatedClientsAsync(associateId, area)
                    : await _permissionService.GetDelegatedClientsAsync(associateId, "TaxFilings"); // Default area

                return Ok(new
                {
                    success = true,
                    data = clients.Select(client => new
                    {
                        clientId = client.ClientId,
                        clientName = client.BusinessName,
                        businessName = client.BusinessName,
                        permissionAreas = new[] { area ?? "TaxFilings" }, // Simplified for now
                        effectivePermissions = new Dictionary<string, object>(), // Would need to fetch actual permissions
                        lastAccessDate = (DateTime?)null // Would need to implement access tracking
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delegated clients for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Grant permission to associate for specific clients
        /// </summary>
        [HttpPost("grant")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GrantPermission([FromBody] GrantPermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _permissionService.GrantPermissionAsync(request, adminId);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permission granted successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Revoke permission from associate
        /// </summary>
        [HttpDelete("{associateId}/clients/{clientId}/areas/{area}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> RevokePermission(
            string associateId,
            int clientId,
            string area,
            [FromBody] RevokePermissionRequest? request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var reason = request?.Reason ?? "Revoked by admin";
                
                var result = await _permissionService.RevokePermissionAsync(associateId, clientId, area, adminId, reason);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permission revoked successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Bulk grant permissions to multiple associates
        /// </summary>
        [HttpPost("bulk-grant")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> BulkGrantPermissions([FromBody] BulkPermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _permissionService.BulkGrantPermissionsAsync(request, adminId);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permissions granted successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk granting permissions");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Bulk revoke permissions
        /// </summary>
        [HttpPost("bulk-revoke")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> BulkRevokePermissions([FromBody] BulkRevokePermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _permissionService.BulkRevokePermissionsAsync(request.PermissionIds, adminId, request.Reason);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permissions revoked successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk revoking permissions");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Set expiry date for permission
        /// </summary>  
        [HttpPut("{permissionId}/expiry")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> SetPermissionExpiry(
            int permissionId,
            [FromBody] SetExpiryRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _permissionService.SetPermissionExpiryAsync(permissionId, request.ExpiryDate, adminId);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permission expiry updated successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting permission expiry for permission {PermissionId}", permissionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Renew permission with new expiry date
        /// </summary>
        [HttpPut("{permissionId}/renew")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> RenewPermission(
            int permissionId,
            [FromBody] RenewPermissionRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var result = await _permissionService.RenewPermissionAsync(permissionId, request.NewExpiryDate, adminId);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Permission renewed successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing permission {PermissionId}", permissionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get permissions expiring soon
        /// </summary>
        [HttpGet("expiring")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetExpiringPermissions([FromQuery] int days = 7)
        {
            try
            {
                var permissions = await _permissionService.GetExpiringPermissionsAsync(days);

                return Ok(new
                {
                    success = true,
                    data = permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring permissions");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get associates assigned to a specific client
        /// </summary>
        [HttpGet("clients/{clientId}/associates")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetClientAssociates(int clientId)
        {
            try
            {
                var permissions = await _permissionService.GetClientAssociatesAsync(clientId);

                return Ok(new
                {
                    success = true,
                    data = permissions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting associates for client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get permission audit log for associate
        /// </summary>
        [HttpGet("{associateId}/audit")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetPermissionAuditLog(
            string associateId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var auditLogs = await _permissionService.GetPermissionAuditLogAsync(associateId, from, to);
                
                // Apply pagination manually
                var totalCount = auditLogs.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var skip = (page - 1) * pageSize;
                var paginatedLogs = auditLogs.Skip(skip).Take(pageSize).ToList();

                return Ok(new
                {
                    success = true,
                    data = paginatedLogs,
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
                _logger.LogError(ex, "Error getting audit log for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get effective permission level for associate-client-area combination
        /// </summary>
        [HttpGet("{associateId}/clients/{clientId}/areas/{area}/effective-level")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetEffectivePermissionLevel(
            string associateId,
            int clientId,
            string area)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Associates can only check their own effective permissions
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only check your own permission levels");
                }

                var level = await _permissionService.GetEffectivePermissionLevelAsync(associateId, clientId, area);

                return Ok(new
                {
                    success = true,
                    data = new { level }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting effective permission level");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate if permission is still active and valid
        /// </summary>
        [HttpGet("{permissionId}/validate")]
        [Authorize(Roles = "Associate,Admin,SystemAdmin")]
        public async Task<ActionResult<object>> ValidatePermission(int permissionId)
        {
            try
            {
                var isValid = await _permissionService.IsPermissionValidAsync(permissionId);

                return Ok(new
                {
                    success = true,
                    data = new { isValid }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating permission {PermissionId}", permissionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}

// Request DTOs
public class RevokePermissionRequest
{
    public string? Reason { get; set; }
}

public class BulkRevokePermissionRequest
{
    public List<int> PermissionIds { get; set; } = new();
    public string? Reason { get; set; }
}

public class SetExpiryRequest
{
    public DateTime? ExpiryDate { get; set; }
}

public class RenewPermissionRequest
{
    public DateTime NewExpiryDate { get; set; }
}