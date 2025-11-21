using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class AdminApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ILogger<AdminApiController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        // USER MANAGEMENT ENDPOINTS

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<object>> GetUsers()
        {
            try
            {
                // Get users with roles properly awaited
                var usersList = await _userManager.Users.ToListAsync();
                var users = new List<object>();
                
                foreach (var u in usersList)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    users.Add(new
                    {
                        u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        u.Email,
                        Role = roles.FirstOrDefault() ?? "Client",
                        IsActive = u.IsActive,
                        CompanyName = "",
                        LastLogin = u.LastLoginDate,
                        CreatedAt = u.CreatedDate
                    });
                }

                return Ok(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Invite new user
        /// </summary>
        [HttpPost("users/invite")]
        public async Task<ActionResult<object>> InviteUser([FromBody] InviteUserDto dto)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { success = false, message = "Name is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Role))
                {
                    return BadRequest(new { success = false, message = "Role is required" });
                }

                // Email format validation
                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(dto.Email);
                }
                catch
                {
                    return BadRequest(new { success = false, message = "Invalid email format" });
                }

                // Role validation
                var validRoles = new[] { "Client", "Associate", "Admin", "SystemAdmin" };
                if (!validRoles.Contains(dto.Role))
                {
                    return BadRequest(new { success = false, message = $"Invalid role. Allowed roles: {string.Join(", ", validRoles)}" });
                }

                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { success = false, message = "User with this email already exists" });
                }

                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.Name.Split(' ').FirstOrDefault() ?? dto.Name,
                    LastName = dto.Name.Split(' ').Skip(1).FirstOrDefault() ?? "",
                    EmailConfirmed = false,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                await _userManager.AddToRoleAsync(user, dto.Role);

                // TODO: Send invitation email

                _logger.LogInformation("User invited: {Email} with role {Role}", dto.Email, dto.Role);
                return Ok(new { success = true, message = "User invited successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<ActionResult<object>> GetUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user.Id,
                        Name = user.FirstName + " " + user.LastName,
                        user.Email,
                        Role = roles.FirstOrDefault() ?? "Client",
                        IsActive = user.IsActive,
                        CompanyName = "",
                        LastLogin = user.LastLoginDate,
                        CreatedAt = user.CreatedDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<ActionResult<object>> UpdateUser(string userId, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    var nameParts = dto.Name.Split(' ');
                    user.FirstName = nameParts.FirstOrDefault() ?? dto.Name;
                    user.LastName = nameParts.Skip(1).FirstOrDefault() ?? "";
                }

                if (!string.IsNullOrEmpty(dto.Role))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, dto.Role);
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                _logger.LogInformation("User updated: {UserId}", userId);
                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<ActionResult<object>> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Soft delete by deactivating
                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User deleted/deactivated: {UserId}", userId);
                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user role
        /// </summary>
        [HttpPatch("users/{userId}/role")]
        public async Task<ActionResult<object>> UpdateUserRole(string userId, [FromBody] UpdateRoleDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, dto.Role);

                _logger.LogInformation("User role updated: {UserId} to {Role}", userId, dto.Role);
                return Ok(new { success = true, message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user status
        /// </summary>
        [HttpPatch("users/{userId}/status")]
        public async Task<ActionResult<object>> UpdateUserStatus(string userId, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                user.IsActive = dto.IsActive;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User status updated: {UserId} to {Status}", userId, dto.IsActive ? "Active" : "Inactive");
                return Ok(new { success = true, message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // TAX RATES ENDPOINTS

        /// <summary>
        /// Get all tax rates
        /// </summary>
        [HttpGet("tax-rates")]
        public async Task<ActionResult<object>> GetTaxRates()
        {
            try
            {
                // TODO: Implement actual tax rates retrieval from database
                var taxRates = new List<object>
                {
                    new { id = 1, type = "GST", rate = 15, effectiveFrom = "2024-01-01", applicableTo = "All transactions" },
                    new { id = 2, type = "CIT", rate = 30, effectiveFrom = "2024-01-01", applicableTo = "Corporate income" },
                    new { id = 3, type = "PIT", rate = 25, effectiveFrom = "2024-01-01", applicableTo = "Personal income" },
                    new { id = 4, type = "WHT", rate = 10, effectiveFrom = "2024-01-01", applicableTo = "Withholding" }
                };

                return Ok(new { success = true, data = taxRates });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax rates");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific tax rate
        /// </summary>
        [HttpGet("tax-rates/{type}")]
        public async Task<ActionResult<object>> GetTaxRate(string type)
        {
            try
            {
                // TODO: Implement actual tax rate retrieval
                var taxRate = new { id = 1, type, rate = 15, effectiveFrom = "2024-01-01", applicableTo = "All transactions" };
                return Ok(new { success = true, data = taxRate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax rate {Type}", type);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update tax rate
        /// </summary>
        [HttpPut("tax-rates/{type}")]
        public async Task<ActionResult<object>> UpdateTaxRate(string type, [FromBody] UpdateTaxRateDto dto)
        {
            try
            {
                // TODO: Implement actual tax rate update
                _logger.LogInformation("Tax rate updated: {Type} to {Rate}%", type, dto.Rate);
                return Ok(new { success = true, message = "Tax rate updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tax rate {Type}", type);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get tax rate history
        /// </summary>
        [HttpGet("tax-rates/{type}/history")]
        public async Task<ActionResult<object>> GetTaxRateHistory(string type)
        {
            try
            {
                // TODO: Implement tax rate history retrieval
                var history = new List<object>();
                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tax rate history for {Type}", type);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // PENALTIES ENDPOINTS

        /// <summary>
        /// Get penalty rules
        /// </summary>
        [HttpGet("penalties")]
        public async Task<ActionResult<object>> GetPenalties()
        {
            try
            {
                // TODO: Implement penalty rules retrieval
                var penalties = new List<object>();
                return Ok(new { success = true, data = penalties });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving penalties");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create penalty rule
        /// </summary>
        [HttpPost("penalties")]
        public async Task<ActionResult<object>> CreatePenalty([FromBody] CreatePenaltyDto dto)
        {
            try
            {
                // TODO: Implement penalty rule creation
                _logger.LogInformation("Penalty rule created for {TaxType}", dto.TaxType);
                return Ok(new { success = true, message = "Penalty rule created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating penalty rule");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update penalty rule
        /// </summary>
        [HttpPut("penalties/{id}")]
        public async Task<ActionResult<object>> UpdatePenalty(int id, [FromBody] UpdatePenaltyDto dto)
        {
            try
            {
                // TODO: Implement penalty rule update
                _logger.LogInformation("Penalty rule updated: {Id}", id);
                return Ok(new { success = true, message = "Penalty rule updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating penalty rule {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete penalty rule
        /// </summary>
        [HttpDelete("penalties/{id}")]
        public async Task<ActionResult<object>> DeletePenalty(int id)
        {
            try
            {
                // TODO: Implement penalty rule deletion
                _logger.LogInformation("Penalty rule deleted: {Id}", id);
                return Ok(new { success = true, message = "Penalty rule deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting penalty rule {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Import excise table
        /// </summary>
        [HttpPost("penalties/import")]
        public async Task<ActionResult<object>> ImportExciseTable(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                // TODO: Implement CSV/Excel parsing and import
                return Ok(new { success = true, message = "Excise table imported successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing excise table");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // AUDIT LOGS ENDPOINTS

        /// <summary>
        /// Get audit logs with filters
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<ActionResult<object>> GetAuditLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? actor = null,
            [FromQuery] string? action = null)
        {
            try
            {
                var query = _dbContext.AuditLogs
                    .Include(a => a.User)
                    .AsQueryable();

                // Apply filters
                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= toDate.Value);
                }

                if (!string.IsNullOrEmpty(actor))
                {
                    query = query.Where(a => (a.User != null && 
                        (a.User.FirstName + " " + a.User.LastName).Contains(actor)) ||
                        a.UserId.Contains(actor));
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action.Contains(action));
                }

                var logs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(1000) // Limit to prevent performance issues
                    .Select(a => new
                    {
                        id = a.Id,
                        Timestamp = a.Timestamp,
                        Actor = a.User != null ? a.User.FirstName + " " + a.User.LastName : "System",
                        Role = _dbContext.UserRoles
                            .Where(ur => ur.UserId == a.UserId)
                            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                            .FirstOrDefault() ?? "Unknown",
                        Action = a.Action,
                        IPAddress = a.IpAddress ?? "N/A",
                        Details = a.Description ?? ""
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Export audit logs
        /// </summary>
        [HttpGet("audit-logs/export")]
        public async Task<IActionResult> ExportAuditLogs()
        {
            try
            {
                var logs = await _dbContext.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10000) // Limit export size
                    .Select(a => new
                    {
                        a.Timestamp,
                        Actor = a.User != null ? a.User.FirstName + " " + a.User.LastName : "System",
                        Role = _dbContext.UserRoles
                            .Where(ur => ur.UserId == a.UserId)
                            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                            .FirstOrDefault() ?? "Unknown",
                        a.Action,
                        a.Entity,
                        a.EntityId,
                        a.IpAddress,
                        a.Description
                    })
                    .ToListAsync();

                // Generate CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Timestamp,Actor,Role,Action,Entity,EntityId,IpAddress,Description");

                foreach (var log in logs)
                {
                    csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},\"{log.Actor}\",\"{log.Role}\",\"{log.Action}\",\"{log.Entity}\",\"{log.EntityId}\",\"{log.IpAddress}\",\"{log.Description?.Replace("\"", "\"\"") ?? ""}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // JOBS MONITOR ENDPOINTS

        /// <summary>
        /// Get job statuses
        /// </summary>
        [HttpGet("jobs")]
        public async Task<ActionResult<object>> GetJobs()
        {
            try
            {
                var jobs = new List<object>
                {
                    new { name = "ReminderScheduler", status = "Running", lastRun = DateTime.UtcNow.AddHours(-1), nextRun = DateTime.UtcNow.AddHours(23), queueSize = 0 },
                    new { name = "KPIRecalculation", status = "Running", lastRun = DateTime.UtcNow.AddHours(-6), nextRun = DateTime.UtcNow.AddHours(18), queueSize = 0 },
                    new { name = "FileScanner", status = "Running", lastRun = DateTime.UtcNow.AddMinutes(-30), nextRun = DateTime.UtcNow.AddMinutes(30), queueSize = 5 }
                };

                return Ok(new { success = true, data = jobs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job statuses");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific job status
        /// </summary>
        [HttpGet("jobs/{name}")]
        public async Task<ActionResult<object>> GetJob(string name)
        {
            try
            {
                var job = new { name, status = "Running", lastRun = DateTime.UtcNow.AddHours(-1), nextRun = DateTime.UtcNow.AddHours(23), queueSize = 0 };
                return Ok(new { success = true, data = job });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job status for {JobName}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Start job
        /// </summary>
        [HttpPost("jobs/{name}/start")]
        public async Task<ActionResult<object>> StartJob(string name)
        {
            try
            {
                // TODO: Implement job start logic
                _logger.LogInformation("Job started: {JobName}", name);
                return Ok(new { success = true, message = $"Job {name} started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting job {JobName}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Stop job
        /// </summary>
        [HttpPost("jobs/{name}/stop")]
        public async Task<ActionResult<object>> StopJob(string name)
        {
            try
            {
                // TODO: Implement job stop logic
                _logger.LogInformation("Job stopped: {JobName}", name);
                return Ok(new { success = true, message = $"Job {name} stopped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping job {JobName}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Restart job
        /// </summary>
        [HttpPost("jobs/{name}/restart")]
        public async Task<ActionResult<object>> RestartJob(string name)
        {
            try
            {
                // TODO: Implement job restart logic
                _logger.LogInformation("Job restarted: {JobName}", name);
                return Ok(new { success = true, message = $"Job {name} restarted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting job {JobName}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    // DTOs
    public record InviteUserDto(string Email, string Name, string Role);
    public record UpdateUserDto(string? Name, string? Role);
    public record UpdateRoleDto(string Role);
    public record UpdateStatusDto(bool IsActive);
    public record UpdateTaxRateDto(decimal Rate);
    public record CreatePenaltyDto(string TaxType, string Condition, decimal Amount, decimal? Percentage, string Description);
    public record UpdatePenaltyDto(string? TaxType, string? Condition, decimal? Amount, decimal? Percentage, string? Description);
}

