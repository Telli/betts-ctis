using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace BettsTax.Web.Middleware
{
    public class SimpleRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
        private readonly int _maxRequests = 100;

        public SimpleRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            var cacheKey = $"rate_limit_{ipAddress}";

            // Get current request count for this IP
            _cache.TryGetValue(cacheKey, out int requestCount);

            if (requestCount >= _maxRequests)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Append("Retry-After", "60");
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            // Increment the request count and set cache options
            _cache.Set(cacheKey, requestCount + 1, _window);

            // Continue pipeline
            await _next(context);
        }
    }
}
