using System.Security.Claims;
using BettsTax.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services
{
    public interface IUserContextService
    {
        string? GetCurrentUserId();
        string? GetCurrentUserRole();
        Task<int?> GetCurrentUserClientIdAsync();
        Task<bool> IsClientUserAsync();
        Task<bool> IsAdminOrAssociateAsync();
        Task<bool> CanAccessClientDataAsync(int clientId);
    }

    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public UserContextService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetCurrentUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public async Task<int?> GetCurrentUserClientIdAsync()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return null;

            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);
            
            return client?.ClientId;
        }

        public Task<bool> IsClientUserAsync()
        {
            var role = GetCurrentUserRole();
            return Task.FromResult(role == "Client");
        }

        public Task<bool> IsAdminOrAssociateAsync()
        {
            var role = GetCurrentUserRole();
            return Task.FromResult(role == "Admin" || role == "Associate" || role == "SystemAdmin");
        }

        public async Task<bool> CanAccessClientDataAsync(int clientId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return false;

            // Admin and SystemAdmin can access any client data
            if (role == "Admin" || role == "SystemAdmin")
                return true;

            // Associates can access clients assigned to them
            if (role == "Associate")
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.ClientId == clientId && c.AssignedAssociateId == userId);
                return client != null;
            }

            // Client users can only access their own data
            if (role == "Client")
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.ClientId == clientId && c.UserId == userId);
                return client != null;
            }

            return false;
        }
    }
}