using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BettsTax.Web.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AssociatePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string PermissionArea { get; }
        public AssociatePermissionLevel RequiredLevel { get; }
        public ClientIdSource ClientIdSource { get; }
        public string? ClientIdParameter { get; }

        public AssociatePermissionAttribute(string permissionArea, AssociatePermissionLevel requiredLevel, 
            ClientIdSource clientIdSource = ClientIdSource.Route, string? clientIdParameter = "clientId")
        {
            PermissionArea = permissionArea;
            RequiredLevel = requiredLevel;
            ClientIdSource = clientIdSource;
            ClientIdParameter = clientIdParameter;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // The actual permission check will be handled by the AuthorizationHandler
            // This attribute just marks the action/controller for permission requirements
        }
    }

    public class AssociatePermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionArea { get; }
        public AssociatePermissionLevel RequiredLevel { get; }
        public ClientIdSource ClientIdSource { get; }
        public string? ClientIdParameter { get; }

        public AssociatePermissionRequirement(string permissionArea, AssociatePermissionLevel requiredLevel,
            ClientIdSource clientIdSource = ClientIdSource.Route, string? clientIdParameter = "clientId")
        {
            PermissionArea = permissionArea;
            RequiredLevel = requiredLevel;
            ClientIdSource = clientIdSource;
            ClientIdParameter = clientIdParameter;
        }
    }
}