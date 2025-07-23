using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using BettsTax.Core.Services;

namespace BettsTax.Web.Authorization
{
    public static class ClientDataOperations
    {
        public static OperationAuthorizationRequirement Read =
            new OperationAuthorizationRequirement { Name = nameof(Read) };
        
        public static OperationAuthorizationRequirement Update =
            new OperationAuthorizationRequirement { Name = nameof(Update) };
        
        public static OperationAuthorizationRequirement Delete =
            new OperationAuthorizationRequirement { Name = nameof(Delete) };
    }

    public class ClientDataAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, int>
    {
        private readonly IUserContextService _userContextService;

        public ClientDataAuthorizationHandler(IUserContextService userContextService)
        {
            _userContextService = userContextService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            int clientId)
        {
            var canAccess = await _userContextService.CanAccessClientDataAsync(clientId);
            
            if (canAccess)
            {
                context.Succeed(requirement);
            }
        }
    }

    public class ClientPortalRequirement : IAuthorizationRequirement
    {
    }

    public class ClientPortalAuthorizationHandler : AuthorizationHandler<ClientPortalRequirement>
    {
        private readonly IUserContextService _userContextService;

        public ClientPortalAuthorizationHandler(IUserContextService userContextService)
        {
            _userContextService = userContextService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ClientPortalRequirement requirement)
        {
            var isClient = await _userContextService.IsClientUserAsync();
            
            if (isClient)
            {
                context.Succeed(requirement);
            }
        }
    }

    public class AdminOrAssociateRequirement : IAuthorizationRequirement
    {
    }

    public class AdminOrAssociateAuthorizationHandler : AuthorizationHandler<AdminOrAssociateRequirement>
    {
        private readonly IUserContextService _userContextService;

        public AdminOrAssociateAuthorizationHandler(IUserContextService userContextService)
        {
            _userContextService = userContextService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminOrAssociateRequirement requirement)
        {
            var isAdminOrAssociate = await _userContextService.IsAdminOrAssociateAsync();
            
            if (isAdminOrAssociate)
            {
                context.Succeed(requirement);
            }
        }
    }
}