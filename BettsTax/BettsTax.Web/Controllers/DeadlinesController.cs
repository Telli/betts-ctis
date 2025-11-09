using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Compliance;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    /// <summary>
    /// Deadlines Controller - Manages tax filing and compliance deadlines
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeadlinesController : ControllerBase
    {
        private readonly IDeadlineMonitoringService _deadlineService;
        private readonly Services.IAuthorizationService _authorizationService;
        private readonly ILogger<DeadlinesController> _logger;

        public DeadlinesController(
            IDeadlineMonitoringService deadlineService,
            Services.IAuthorizationService authorizationService,
            ILogger<DeadlinesController> logger)
        {
            _deadlineService = deadlineService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Get upcoming deadlines within specified days
        /// </summary>
        /// <param name="days">Number of days to look ahead (default: 30)</param>
        /// <param name="clientId">Optional client ID to filter by specific client</param>
        /// <returns>List of upcoming deadlines</returns>
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUpcomingDeadlines(
            [FromQuery] int days = 30,
            [FromQuery] int? clientId = null)
        {
            try
            {
                // Validate days parameter
                if (days < 1 || days > 365)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Days parameter must be between 1 and 365"
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = _authorizationService.GetUserRole(User);

                // SECURITY: Validate authorization for clientId access
                if (!_authorizationService.CanAccessClientData(User, clientId))
                {
                    _logger.LogWarning(
                        "Unauthorized access attempt: User {UserId} with role {Role} tried to access ClientId {ClientId}",
                        userId, userRole, clientId);
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied. You do not have permission to access this client's data."
                    });
                }

                // If no clientId specified and user is a client, auto-filter to their data
                var effectiveClientId = clientId;
                if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
                {
                    effectiveClientId = _authorizationService.GetUserClientId(User);
                }

                _logger.LogInformation(
                    "User {UserId} with role {Role} requesting upcoming deadlines for {Days} days, ClientId: {ClientId}",
                    userId, userRole, days, effectiveClientId?.ToString() ?? "all");

                // Get deadlines from service
                var deadlines = await _deadlineService.GetUpcomingDeadlinesAsync(effectiveClientId, days);

                _logger.LogInformation(
                    "Retrieved {Count} upcoming deadlines for user {UserId}",
                    deadlines.Count, userId);

                return Ok(new 
                { 
                    success = true, 
                    data = deadlines,
                    meta = new
                    {
                        count = deadlines.Count,
                        daysAhead = days,
                        clientId = effectiveClientId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming deadlines for clientId: {ClientId}", clientId);
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred while retrieving upcoming deadlines" 
                });
            }
        }

        /// <summary>
        /// Get overdue deadlines
        /// </summary>
        /// <param name="clientId">Optional client ID to filter by specific client</param>
        /// <returns>List of overdue deadlines</returns>
        [HttpGet("overdue")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOverdueDeadlines([FromQuery] int? clientId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = _authorizationService.GetUserRole(User);

                // SECURITY: Validate authorization for clientId access
                if (!_authorizationService.CanAccessClientData(User, clientId))
                {
                    _logger.LogWarning(
                        "Unauthorized access attempt: User {UserId} with role {Role} tried to access ClientId {ClientId}",
                        userId, userRole, clientId);
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied. You do not have permission to access this client's data."
                    });
                }

                // If no clientId specified and user is a client, auto-filter to their data
                var effectiveClientId = clientId;
                if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
                {
                    effectiveClientId = _authorizationService.GetUserClientId(User);
                }

                _logger.LogInformation(
                    "User {UserId} with role {Role} requesting overdue deadlines, ClientId: {ClientId}",
                    userId, userRole, effectiveClientId?.ToString() ?? "all");

                // Get overdue deadlines from service
                var deadlines = await _deadlineService.GetOverdueItemsAsync(effectiveClientId);

                _logger.LogInformation(
                    "Retrieved {Count} overdue deadlines for user {UserId}",
                    deadlines.Count, userId);

                return Ok(new 
                { 
                    success = true, 
                    data = deadlines,
                    meta = new
                    {
                        count = deadlines.Count,
                        clientId = effectiveClientId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue deadlines for clientId: {ClientId}", clientId);
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred while retrieving overdue deadlines" 
                });
            }
        }

        /// <summary>
        /// Get all deadlines (upcoming and overdue)
        /// </summary>
        /// <param name="days">Number of days to look ahead for upcoming deadlines (default: 30)</param>
        /// <param name="clientId">Optional client ID to filter by specific client</param>
        /// <returns>Combined list of upcoming and overdue deadlines</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDeadlines(
            [FromQuery] int days = 30,
            [FromQuery] int? clientId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = _authorizationService.GetUserRole(User);

                // SECURITY: Validate authorization for clientId access
                if (!_authorizationService.CanAccessClientData(User, clientId))
                {
                    _logger.LogWarning(
                        "Unauthorized access attempt: User {UserId} with role {Role} tried to access ClientId {ClientId}",
                        userId, userRole, clientId);
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied. You do not have permission to access this client's data."
                    });
                }

                // If no clientId specified and user is a client, auto-filter to their data
                var effectiveClientId = clientId;
                if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
                {
                    effectiveClientId = _authorizationService.GetUserClientId(User);
                }

                _logger.LogInformation(
                    "User {UserId} requesting all deadlines for {Days} days, ClientId: {ClientId}",
                    userId, days, effectiveClientId?.ToString() ?? "all");

                // Get both upcoming and overdue deadlines
                var upcomingTask = _deadlineService.GetUpcomingDeadlinesAsync(effectiveClientId, days);
                var overdueTask = _deadlineService.GetOverdueItemsAsync(effectiveClientId);

                await Task.WhenAll(upcomingTask, overdueTask);

                var upcoming = await upcomingTask;
                var overdue = await overdueTask;

                // Combine and sort by due date
                var allDeadlines = upcoming.Concat(overdue)
                    .OrderBy(d => d.DueDate)
                    .ToList();

                return Ok(new 
                { 
                    success = true, 
                    data = allDeadlines,
                    meta = new
                    {
                        totalCount = allDeadlines.Count,
                        upcomingCount = upcoming.Count,
                        overdueCount = overdue.Count,
                        daysAhead = days,
                        clientId = effectiveClientId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all deadlines for clientId: {ClientId}", clientId);
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred while retrieving deadlines" 
                });
            }
        }

        /// <summary>
        /// Get deadline statistics
        /// </summary>
        /// <param name="clientId">Optional client ID to filter by specific client</param>
        /// <returns>Deadline statistics including counts by status and priority</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeadlineStats([FromQuery] int? clientId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = _authorizationService.GetUserRole(User);

                // SECURITY: Validate authorization for clientId access
                if (!_authorizationService.CanAccessClientData(User, clientId))
                {
                    _logger.LogWarning(
                        "Unauthorized access attempt: User {UserId} with role {Role} tried to access ClientId {ClientId}",
                        userId, userRole, clientId);
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied. You do not have permission to access this client's data."
                    });
                }

                // If no clientId specified and user is a client, auto-filter to their data
                var effectiveClientId = clientId;
                if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
                {
                    effectiveClientId = _authorizationService.GetUserClientId(User);
                }

                _logger.LogInformation(
                    "User {UserId} requesting deadline statistics, ClientId: {ClientId}",
                    userId, effectiveClientId?.ToString() ?? "all");

                // Get deadlines for different time periods
                var next7DaysTask = _deadlineService.GetUpcomingDeadlinesAsync(effectiveClientId, 7);
                var next30DaysTask = _deadlineService.GetUpcomingDeadlinesAsync(effectiveClientId, 30);
                var overdueTask = _deadlineService.GetOverdueItemsAsync(effectiveClientId);

                await Task.WhenAll(next7DaysTask, next30DaysTask, overdueTask);

                var next7Days = await next7DaysTask;
                var next30Days = await next30DaysTask;
                var overdue = await overdueTask;

                // Calculate statistics
                var stats = new
                {
                    total = next30Days.Count + overdue.Count,
                    upcoming = next30Days.Count,
                    dueSoon = next7Days.Count,
                    overdue = overdue.Count,
                    thisWeek = next7Days.Count,
                    thisMonth = next30Days.Count,
                    byPriority = next30Days.Concat(overdue)
                        .GroupBy(d => d.Priority.ToString().ToLower())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    byType = next30Days.Concat(overdue)
                        .GroupBy(d => d.TaxTypeName.ToLower())
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(new 
                { 
                    success = true, 
                    data = stats 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deadline statistics for clientId: {ClientId}", clientId);
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred while retrieving deadline statistics" 
                });
            }
        }
    }
}

