using BettsTax.Core.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BettsTax.Web.Filters
{
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly IAuditService _audit;

        public AuditActionFilter(IAuditService audit)
        {
            _audit = audit;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var action = context.ActionDescriptor.DisplayName ?? "unknown";
            var routeData = context.RouteData.Values;
            var entity = routeData.ContainsKey("controller") ? routeData["controller"]!.ToString()! : "unknown";
            var entityId = routeData.ContainsKey("id") ? routeData["id"]!.ToString()! : "";

            await _audit.LogAsync(userId, action, entity, entityId);
        }
    }
}
