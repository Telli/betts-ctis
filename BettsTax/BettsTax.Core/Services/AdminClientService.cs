using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BettsTax.Core.Services
{
    public interface IAdminClientService
    {
        Task<PagedResult<AdminClientOverviewDto>> GetClientOverviewAsync(int page, int pageSize, string? search = null, ClientStatus? status = null);
        Task<AdminClientDetailDto?> GetClientDetailAsync(int clientId);
        Task<AdminClientStatsDto> GetClientStatsAsync();
        Task<IEnumerable<AuditLogDto>> GetClientAuditLogsAsync(int clientId, int page = 1, int pageSize = 20);
        Task<bool> ActivateClientAsync(int clientId, string adminUserId);
        Task<bool> DeactivateClientAsync(int clientId, string adminUserId, string reason);
        Task<bool> AssignAssociateAsync(int clientId, string associateUserId, string adminUserId);
    }

    public class AdminClientService : IAdminClientService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminClientService(
            ApplicationDbContext db, 
            IMapper mapper, 
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _mapper = mapper;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<PagedResult<AdminClientOverviewDto>> GetClientOverviewAsync(
            int page, int pageSize, string? search = null, ClientStatus? status = null)
        {
            var query = _db.Clients
                .Include(c => c.User)
                .Include(c => c.AssignedAssociate)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => 
                    c.BusinessName.Contains(search) ||
                    c.ContactPerson.Contains(search) ||
                    c.Email.Contains(search) ||
                    c.ClientNumber.Contains(search));
            }

            // Apply status filter
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new AdminClientOverviewDto
                {
                    ClientId = c.ClientId,
                    ClientNumber = c.ClientNumber,
                    BusinessName = c.BusinessName,
                    ContactPerson = c.ContactPerson,
                    Email = c.Email,
                    Status = c.Status.ToString(),
                    TaxpayerCategory = c.TaxpayerCategory.ToString(),
                    ClientType = c.ClientType.ToString(),
                    HasUserAccount = !string.IsNullOrEmpty(c.UserId),
                    AssignedAssociateName = c.AssignedAssociate != null ? 
                        $"{c.AssignedAssociate.FirstName} {c.AssignedAssociate.LastName}" : null,
                    CreatedDate = c.CreatedDate,
                    UpdatedDate = c.UpdatedDate,
                    TotalTaxFilings = _db.TaxYears.Count(t => t.ClientId == c.ClientId),
                    PendingFilings = _db.TaxYears.Count(t => t.ClientId == c.ClientId && 
                        (t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending)),
                    LastActivity = _db.AuditLogs
                        .Where(a => a.ClientId == c.ClientId)
                        .OrderByDescending(a => a.Timestamp)
                        .Select(a => a.Timestamp)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResult<AdminClientOverviewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminClientDetailDto?> GetClientDetailAsync(int clientId)
        {
            var client = await _db.Clients
                .Include(c => c.User)
                .Include(c => c.AssignedAssociate)
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null)
                return null;

            // Get statistics
            var taxFilings = await _db.TaxYears.Where(t => t.ClientId == clientId).ToListAsync();
            var payments = await _db.Payments.Where(p => p.ClientId == clientId).ToListAsync();
            var documents = await _db.Documents.Where(d => d.ClientId == clientId).ToListAsync();

            return new AdminClientDetailDto
            {
                ClientId = client.ClientId,
                ClientNumber = client.ClientNumber,
                BusinessName = client.BusinessName,
                ContactPerson = client.ContactPerson,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address,
                TIN = client.TIN,
                Status = client.Status.ToString(),
                TaxpayerCategory = client.TaxpayerCategory.ToString(),
                ClientType = client.ClientType.ToString(),
                AnnualTurnover = client.AnnualTurnover,
                CreatedDate = client.CreatedDate,
                UpdatedDate = client.UpdatedDate,
                HasUserAccount = !string.IsNullOrEmpty(client.UserId),
                UserEmail = client.User?.Email,
                UserLastLogin = client.User?.LastLoginDate,
                AssignedAssociateName = client.AssignedAssociate != null ? 
                    $"{client.AssignedAssociate.FirstName} {client.AssignedAssociate.LastName}" : null,
                TotalTaxFilings = taxFilings.Count,
                CompletedFilings = taxFilings.Count(t => t.Status == TaxYearStatus.Filed || t.Status == TaxYearStatus.Paid),
                PendingFilings = taxFilings.Count(t => t.Status == TaxYearStatus.Draft || t.Status == TaxYearStatus.Pending),
                OverdueFilings = taxFilings.Count(t => t.Status == TaxYearStatus.Overdue),
                TotalPayments = payments.Count,
                TotalPaymentAmount = payments.Where(p => p.Status == PaymentStatus.Approved).Sum(p => p.Amount),
                PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending),
                TotalDocuments = documents.Count,
                RecentDocuments = documents.OrderByDescending(d => d.UploadedAt).Take(5).ToList()
            };
        }

        public async Task<AdminClientStatsDto> GetClientStatsAsync()
        {
            var totalClients = await _db.Clients.CountAsync();
            var activeClients = await _db.Clients.CountAsync(c => c.Status == ClientStatus.Active);
            var inactiveClients = await _db.Clients.CountAsync(c => c.Status == ClientStatus.Inactive);
            var suspendedClients = await _db.Clients.CountAsync(c => c.Status == ClientStatus.Suspended);
            var clientsWithAccounts = await _db.Clients.CountAsync(c => !string.IsNullOrEmpty(c.UserId));

            // Recent activity stats
            var recentRegistrations = await _db.Clients
                .Where(c => c.CreatedDate >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            var recentActivity = await _db.AuditLogs
                .Where(a => a.ActionType == AuditActionType.Login && a.Timestamp >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            return new AdminClientStatsDto
            {
                TotalClients = totalClients,
                ActiveClients = activeClients,
                InactiveClients = inactiveClients,
                SuspendedClients = suspendedClients,
                ClientsWithPortalAccess = clientsWithAccounts,
                RecentRegistrations = recentRegistrations,
                RecentActiveUsers = recentActivity
            };
        }

        public async Task<IEnumerable<AuditLogDto>> GetClientAuditLogsAsync(int clientId, int page = 1, int pageSize = 20)
        {
            var logs = await _db.AuditLogs
                .Include(a => a.User)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    AuditLogId = a.AuditLogId,
                    UserId = a.UserId,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "Unknown",
                    Action = a.Action,
                    Entity = a.Entity,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    Timestamp = a.Timestamp,
                    IPAddress = a.IPAddress,
                    IsSuccess = a.IsSuccess,
                    ActionType = a.ActionType.ToString()
                })
                .ToListAsync();

            return logs;
        }

        public async Task<bool> ActivateClientAsync(int clientId, string adminUserId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null)
                return false;

            client.Status = ClientStatus.Active;
            client.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditService.LogClientPortalActivityAsync(
                adminUserId,
                clientId,
                AuditActionType.Update,
                "Client",
                clientId.ToString(),
                $"Client {client.BusinessName} activated by admin");

            return true;
        }

        public async Task<bool> DeactivateClientAsync(int clientId, string adminUserId, string reason)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null)
                return false;

            client.Status = ClientStatus.Suspended;
            client.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditService.LogClientPortalActivityAsync(
                adminUserId,
                clientId,
                AuditActionType.Update,
                "Client",
                clientId.ToString(),
                $"Client {client.BusinessName} deactivated by admin. Reason: {reason}");

            return true;
        }

        public async Task<bool> AssignAssociateAsync(int clientId, string associateUserId, string adminUserId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null)
                return false;

            var associate = await _userManager.FindByIdAsync(associateUserId);
            if (associate == null)
                return false;

            var isAssociate = await _userManager.IsInRoleAsync(associate, "Associate");
            if (!isAssociate)
                return false;

            var previousAssociate = client.AssignedAssociateId;
            client.AssignedAssociateId = associateUserId;
            client.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditService.LogClientPortalActivityAsync(
                adminUserId,
                clientId,
                AuditActionType.Update,
                "Client",
                clientId.ToString(),
                $"Client {client.BusinessName} assigned to associate {associate.FirstName} {associate.LastName}. Previous: {previousAssociate ?? "None"}");

            return true;
        }
    }
}