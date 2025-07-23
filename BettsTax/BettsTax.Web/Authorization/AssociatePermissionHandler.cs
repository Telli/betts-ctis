using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace BettsTax.Web.Authorization
{
    public class AssociatePermissionHandler : AuthorizationHandler<AssociatePermissionRequirement>
    {
        private readonly IAssociatePermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AssociatePermissionHandler> _logger;

        public AssociatePermissionHandler(IAssociatePermissionService permissionService, 
            UserManager<ApplicationUser> userManager, ILogger<AssociatePermissionHandler> logger)
        {
            _permissionService = permissionService;
            _userManager = userManager;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, 
            AssociatePermissionRequirement requirement)
        {
            try
            {
                var user = context.User;
                if (!user.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("User is not authenticated for permission check");
                    context.Fail();
                    return;
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    context.Fail();
                    return;
                }

                // Check if user is Admin or SystemAdmin - they have all permissions
                if (user.IsInRole("Admin") || user.IsInRole("SystemAdmin"))
                {
                    _logger.LogDebug("User {UserId} has admin role, granting access", userId);
                    context.Succeed(requirement);
                    return;
                }

                // Get client ID from the appropriate source
                var clientId = await GetClientIdFromContextAsync(context, requirement);
                if (!clientId.HasValue)
                {
                    _logger.LogWarning("Client ID not found for permission check. Area: {Area}, Level: {Level}", 
                        requirement.PermissionArea, requirement.RequiredLevel);
                    context.Fail();
                    return;
                }

                // Check if user is the client themselves
                var applicationUser = await _userManager.FindByIdAsync(userId);
                if (applicationUser?.ClientProfile?.ClientId == clientId.Value)
                {
                    _logger.LogDebug("User {UserId} is accessing their own client data {ClientId}", userId, clientId.Value);
                    context.Succeed(requirement);
                    return;
                }

                // Check associate permissions
                var hasPermission = await _permissionService.HasPermissionAsync(
                    userId, clientId.Value, requirement.PermissionArea, requirement.RequiredLevel);

                if (hasPermission)
                {
                    _logger.LogDebug("Associate {UserId} has permission for client {ClientId}, area {Area}, level {Level}", 
                        userId, clientId.Value, requirement.PermissionArea, requirement.RequiredLevel);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("Associate {UserId} lacks permission for client {ClientId}, area {Area}, level {Level}", 
                        userId, clientId.Value, requirement.PermissionArea, requirement.RequiredLevel);
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking associate permission for user {UserId}, area {Area}, level {Level}", 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, requirement.PermissionArea, requirement.RequiredLevel);
                context.Fail();
            }
        }

        private async Task<int?> GetClientIdFromContextAsync(AuthorizationHandlerContext context, 
            AssociatePermissionRequirement requirement)
        {
            if (context.Resource is not Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvcContext)
                return null;

            var clientIdParam = requirement.ClientIdParameter ?? "clientId";

            switch (requirement.ClientIdSource)
            {
                case ClientIdSource.Route:
                    if (mvcContext.RouteData.Values.TryGetValue(clientIdParam, out var routeValue) &&
                        int.TryParse(routeValue?.ToString(), out var routeClientId))
                    {
                        return routeClientId;
                    }
                    break;

                case ClientIdSource.Query:
                    if (mvcContext.HttpContext.Request.Query.TryGetValue(clientIdParam, out var queryValue) &&
                        int.TryParse(queryValue.FirstOrDefault(), out var queryClientId))
                    {
                        return queryClientId;
                    }
                    break;

                case ClientIdSource.Body:
                    // For body source, we need to read the request body
                    // This is more complex and should be handled carefully to avoid disposing the stream
                    return await GetClientIdFromBodyAsync(mvcContext, clientIdParam);

                case ClientIdSource.Header:
                    if (mvcContext.HttpContext.Request.Headers.TryGetValue(clientIdParam, out var headerValue) &&
                        int.TryParse(headerValue.FirstOrDefault(), out var headerClientId))
                    {
                        return headerClientId;
                    }
                    break;
            }

            return null;
        }

        private async Task<int?> GetClientIdFromBodyAsync(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext context, 
            string clientIdParam)
        {
            try
            {
                var request = context.HttpContext.Request;
                if (!request.HasFormContentType && request.ContentType?.Contains("application/json") != true)
                    return null;

                if (request.HasFormContentType)
                {
                    var form = await request.ReadFormAsync();
                    if (form.TryGetValue(clientIdParam, out var formValue) &&
                        int.TryParse(formValue.FirstOrDefault(), out var formClientId))
                    {
                        return formClientId;
                    }
                }
                else
                {
                    // For JSON content, we would need to parse the JSON
                    // This is complex and might require buffering the request
                    // For now, we'll skip this implementation
                    _logger.LogWarning("JSON body parsing for client ID not implemented");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading client ID from request body");
            }

            return null;
        }
    }
}