using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BettsTax.Core.Services;
using BettsTax.Data;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/associate-dashboard")]
    [Authorize(Roles = "Associate,Admin,SystemAdmin")]
    public class AssociateDashboardController : ControllerBase
    {
        private readonly IAssociatePermissionService _permissionService;
        private readonly IClientDelegationService _delegationService;
        private readonly IOnBehalfActionService _onBehalfActionService;
        private readonly ITaxFilingService _taxFilingService;
        private readonly IClientService _clientService;
        private readonly ILogger<AssociateDashboardController> _logger;

        public AssociateDashboardController(
            IAssociatePermissionService permissionService,
            IClientDelegationService delegationService,
            IOnBehalfActionService onBehalfActionService,
            ITaxFilingService taxFilingService,
            IClientService clientService,
            ILogger<AssociateDashboardController> logger)
        {
            _permissionService = permissionService;
            _delegationService = delegationService;
            _onBehalfActionService = onBehalfActionService;
            _taxFilingService = taxFilingService;
            _clientService = clientService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive dashboard data for associate
        /// </summary>
        [HttpGet("{associateId}")]
        public async Task<ActionResult<object>> GetAssociateDashboard(string associateId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Associates can only access their own dashboard unless they're admin
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own dashboard");
                }

                // Get all required data in parallel
                var delegationStatsTask = _delegationService.GetDelegationStatisticsAsync(associateId);
                var recentActionsTask = _onBehalfActionService.GetRecentActionsAsync(associateId, 5);
                var delegatedClientsTask = _permissionService.GetDelegatedClientsAsync(associateId, "TaxFilings");
                var expiringPermissionsTask = _permissionService.GetExpiringPermissionsAsync(7);
                var actionStatsTask = _onBehalfActionService.GetActionStatisticsAsync(associateId);

                await Task.WhenAll(
                    delegationStatsTask,
                    recentActionsTask,
                    delegatedClientsTask,
                    expiringPermissionsTask,
                    actionStatsTask
                );

                var delegationStats = await delegationStatsTask;
                var recentActions = await recentActionsTask;
                var delegatedClients = await delegatedClientsTask;
                var expiringPermissions = await expiringPermissionsTask;
                var actionStats = await actionStatsTask;

                // Filter expiring permissions for this associate
                var associateExpiringPermissions = expiringPermissions.Where(p => p.AssociateId == associateId).ToList();

                // Get upcoming deadlines for delegated clients
                var upcomingDeadlines = await _taxFilingService.GetUpcomingDeadlinesAsync(30);
                var delegatedClientIds = delegatedClients.Select(c => c.ClientId).ToList();
                var relevantDeadlines = upcomingDeadlines.Where(d => delegatedClientIds.Contains(d.ClientId)).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        summary = new
                        {
                            totalClients = (int)delegationStats["TotalClients"],
                            totalPermissions = (int)delegationStats["TotalPermissions"],
                            expiringPermissions = associateExpiringPermissions.Count,
                            recentActions = recentActions.Count,
                            upcomingDeadlines = relevantDeadlines.Count
                        },
                        delegatedClients = delegatedClients.Take(5).Select(client => new
                        {
                            clientId = client.ClientId,
                            businessName = client.BusinessName,
                            contactPerson = client.ContactPerson,
                            taxpayerCategory = client.TaxpayerCategory,
                            hasUpcomingDeadlines = relevantDeadlines.Any(d => d.ClientId == client.ClientId)
                        }),
                        recentActions = recentActions.Select(action => new
                        {
                            id = action.Id,
                            action = action.Action,
                            entityType = action.EntityType,
                            entityId = action.EntityId,
                            clientName = action.Client?.BusinessName,
                            actionDate = action.ActionDate,
                            reason = action.Reason
                        }),
                        upcomingDeadlines = relevantDeadlines.Take(5).Select(deadline => new
                        {
                            taxFilingId = deadline.TaxFilingId,
                            clientName = deadline.ClientName,
                            taxType = deadline.TaxType,
                            dueDate = deadline.DueDate,
                            status = deadline.Status,
                            daysUntilDue = (deadline.DueDate.Date - DateTime.UtcNow.Date).Days
                        }),
                        permissionAlerts = new
                        {
                            expiringPermissions = associateExpiringPermissions.Select(permission => new
                            {
                                id = permission.Id,
                                clientName = permission.Client?.BusinessName,
                                permissionArea = permission.PermissionArea,
                                expiryDate = permission.ExpiryDate,
                                daysUntilExpiry = permission.ExpiryDate.HasValue 
                                    ? (permission.ExpiryDate.Value.Date - DateTime.UtcNow.Date).Days 
                                    : (int?)null
                            })
                        },
                        statistics = new
                        {
                            permissionsByArea = (Dictionary<string, int>)delegationStats["PermissionsByArea"],
                            actionsByType = (Dictionary<string, int>)actionStats["ActionsByType"],
                            actionsByEntityType = (Dictionary<string, int>)actionStats["ActionsByEntityType"],
                            actionsPerDay = ((Dictionary<string, int>)actionStats["ActionsPerDay"]).Take(7).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get delegated client summaries for associate
        /// </summary>
        [HttpGet("{associateId}/clients")]
        public async Task<ActionResult<object>> GetDelegatedClientSummaries(
            string associateId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? area = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own delegated clients");
                }

                var clients = await _permissionService.GetDelegatedClientsAsync(associateId, area ?? "TaxFilings");
                
                // Apply pagination
                var totalCount = clients.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var skip = (page - 1) * pageSize;
                var paginatedClients = clients.Skip(skip).Take(pageSize).ToList();

                // Enhance with additional data
                var clientSummaries = new List<object>();
                foreach (var client in paginatedClients)
                {
                    var recentActions = await _onBehalfActionService.GetClientActionsAsync(client.ClientId, DateTime.UtcNow.AddDays(-30));
                    var upcomingDeadlines = await _taxFilingService.GetUpcomingDeadlinesAsync(30);
                    var clientDeadlines = upcomingDeadlines.Where(d => d.ClientId == client.ClientId).ToList();

                    clientSummaries.Add(new
                    {
                        clientId = client.ClientId,
                        businessName = client.BusinessName,
                        contactPerson = client.ContactPerson,
                        email = client.Email,
                        phoneNumber = client.PhoneNumber,
                        tin = client.TIN,
                        taxpayerCategory = client.TaxpayerCategory,
                        clientType = client.ClientType,
                        isActive = client.Status == BettsTax.Data.ClientStatus.Active,
                        recentActionsCount = recentActions.Count,
                        upcomingDeadlinesCount = clientDeadlines.Count,
                        lastActionDate = recentActions.FirstOrDefault()?.ActionDate,
                        nextDeadline = clientDeadlines.OrderBy(d => d.DueDate).FirstOrDefault()?.DueDate
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = clientSummaries,
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
                _logger.LogError(ex, "Error getting delegated client summaries for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recent actions feed for associate
        /// </summary>
        [HttpGet("{associateId}/recent-actions")]
        public async Task<ActionResult<object>> GetRecentActionsFeed(
            string associateId,
            [FromQuery] int limit = 20,
            [FromQuery] DateTime? since = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own action feed");
                }

                var actions = await _onBehalfActionService.GetAssociateActionsAsync(
                    associateId, 
                    since ?? DateTime.UtcNow.AddDays(-7), 
                    DateTime.UtcNow);

                var recentActions = actions.Take(limit).Select(action => new
                {
                    id = action.Id,
                    action = action.Action,
                    entityType = action.EntityType,
                    entityId = action.EntityId,
                    clientId = action.ClientId,
                    clientName = action.Client?.BusinessName,
                    actionDate = action.ActionDate,
                    reason = action.Reason,
                    clientNotified = action.ClientNotified,
                    oldValues = action.OldValues,
                    newValues = action.NewValues
                });

                return Ok(new
                {
                    success = true,
                    data = recentActions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent actions feed for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get permission alerts and warnings for associate
        /// </summary>
        [HttpGet("{associateId}/permission-alerts")]
        public async Task<ActionResult<object>> GetPermissionAlerts(string associateId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own permission alerts");
                }

                var expiringPermissions = await _permissionService.GetExpiringPermissionsAsync(30);
                var associateExpiringPermissions = expiringPermissions.Where(p => p.AssociateId == associateId).ToList();

                var alerts = new List<object>();

                // Expiring permissions alerts
                foreach (var permission in associateExpiringPermissions)
                {
                    var daysUntilExpiry = permission.ExpiryDate.HasValue 
                        ? (permission.ExpiryDate.Value.Date - DateTime.UtcNow.Date).Days 
                        : (int?)null;

                    var severity = daysUntilExpiry switch
                    {
                        <= 1 => "critical",
                        <= 3 => "high",
                        <= 7 => "medium",
                        _ => "low"
                    };

                    alerts.Add(new
                    {
                        type = "permission_expiry",
                        severity,
                        title = $"Permission expiring for {permission.Client?.BusinessName}",
                        message = $"{permission.PermissionArea} permission expires in {daysUntilExpiry} day{(daysUntilExpiry != 1 ? "s" : "")}",
                        clientId = permission.ClientId,
                        permissionId = permission.Id,
                        expiryDate = permission.ExpiryDate,
                        daysUntilExpiry
                    });
                }

                // Get unnotified actions
                var delegatedClients = await _permissionService.GetDelegatedClientsAsync(associateId, "TaxFilings");
                var unnotifiedCount = 0;
                foreach (var client in delegatedClients)
                {
                    var unnotified = await _onBehalfActionService.GetUnnotifiedActionsAsync(client.ClientId);
                    unnotifiedCount += unnotified.Count;
                }

                if (unnotifiedCount > 0)
                {
                    alerts.Add(new
                    {
                        type = "unnotified_actions",
                        severity = "medium",
                        title = "Pending client notifications",
                        message = $"{unnotifiedCount} action{(unnotifiedCount != 1 ? "s" : "")} pending client notification",
                        count = unnotifiedCount
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        alerts = alerts.OrderByDescending(a => ((dynamic)a).severity == "critical" ? 4 :
                                                              ((dynamic)a).severity == "high" ? 3 :
                                                              ((dynamic)a).severity == "medium" ? 2 : 1),
                        summary = new
                        {
                            total = alerts.Count,
                            critical = alerts.Count(a => ((dynamic)a).severity == "critical"),
                            high = alerts.Count(a => ((dynamic)a).severity == "high"),
                            medium = alerts.Count(a => ((dynamic)a).severity == "medium"),
                            low = alerts.Count(a => ((dynamic)a).severity == "low")
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission alerts for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get workload statistics for associate
        /// </summary>
        [HttpGet("{associateId}/workload")]
        public async Task<ActionResult<object>> GetWorkloadStatistics(
            string associateId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                if (!User.IsInRole("Admin") && !User.IsInRole("SystemAdmin") && userId != associateId)
                {
                    return Forbid("Can only access your own workload statistics");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var actionStats = await _onBehalfActionService.GetActionStatisticsAsync(associateId, from, to);
                var delegationStats = await _delegationService.GetDelegationStatisticsAsync(associateId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        period = new
                        {
                            from,
                            to,
                            days = (to.Date - from.Date).Days
                        },
                        clients = new
                        {
                            total = (int)delegationStats["TotalClients"],
                            permissionsByArea = (Dictionary<string, int>)delegationStats["PermissionsByArea"]
                        },
                        actions = new
                        {
                            total = (int)actionStats["TotalActions"],
                            byType = (Dictionary<string, int>)actionStats["ActionsByType"],
                            byEntityType = (Dictionary<string, int>)actionStats["ActionsByEntityType"],
                            averagePerDay = (int)actionStats["TotalActions"] / Math.Max((to.Date - from.Date).Days, 1),
                            dailyBreakdown = (Dictionary<string, int>)actionStats["ActionsPerDay"]
                        },
                        notifications = new
                        {
                            pending = (int)actionStats["NotificationsPending"]
                        },
                        workloadLevel = GetWorkloadLevel((int)delegationStats["TotalClients"], (int)actionStats["TotalActions"], (to.Date - from.Date).Days)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workload statistics for associate {AssociateId}", associateId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private static string GetWorkloadLevel(int clientCount, int actionCount, int days)
        {
            var avgActionsPerDay = actionCount / Math.Max(days, 1);
            
            if (clientCount > 30 || avgActionsPerDay > 20)
                return "Very High";
            else if (clientCount > 20 || avgActionsPerDay > 15)
                return "High";
            else if (clientCount > 10 || avgActionsPerDay > 10)
                return "Medium";
            else
                return "Low";
        }
    }
}