using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using CsvHelper;

namespace BettsTax.Core.Services
{
    public class ActivityTimelineService : IActivityTimelineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserContextService _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ActivityTimelineService> _logger;

        public ActivityTimelineService(
            ApplicationDbContext context,
            IUserContextService userContext,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ILogger<ActivityTimelineService> logger)
        {
            _context = context;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<ActivityTimelineDto>> CreateActivityAsync(ActivityTimelineCreateDto dto)
        {
            try
            {
                var activity = new ActivityTimeline
                {
                    ActivityType = dto.ActivityType,
                    Category = GetCategoryFromType(dto.ActivityType),
                    Priority = dto.Priority,
                    Title = dto.Title,
                    Description = dto.Description ?? string.Empty,
                    ClientId = dto.ClientId,
                    UserId = _userContext.GetCurrentUserId(),
                    TargetUserId = dto.TargetUserId,
                    DocumentId = dto.DocumentId,
                    TaxFilingId = dto.TaxFilingId,
                    PaymentId = dto.PaymentId,
                    MessageId = dto.MessageId,
                    Metadata = dto.Metadata,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    IsVisibleToClient = dto.IsVisibleToClient,
                    IsVisibleToAssociate = dto.IsVisibleToAssociate,
                    IsVisibleToAdmin = dto.IsVisibleToAdmin
                };

                _context.ActivityTimelines.Add(activity);
                await _context.SaveChangesAsync();

                var result = await GetActivityByIdAsync(activity.ActivityTimelineId);
                return result != null 
                    ? Result.Success<ActivityTimelineDto>(result)
                    : Result.Failure<ActivityTimelineDto>("Failed to retrieve created activity");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity timeline entry");
                return Result.Failure<ActivityTimelineDto>("Error creating activity");
            }
        }

        public async Task<Result> LogActivityAsync(ActivityType type, string title, string? description = null,
            int? clientId = null, int? documentId = null, int? taxFilingId = null,
            int? paymentId = null, string? metadata = null)
        {
            var dto = new ActivityTimelineCreateDto
            {
                ActivityType = type,
                Title = title,
                Description = description,
                ClientId = clientId,
                DocumentId = documentId,
                TaxFilingId = taxFilingId,
                PaymentId = paymentId,
                Metadata = metadata
            };

            var result = await CreateActivityAsync(dto);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
        }

        public async Task<Result> LogClientLoginAsync(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                return Result.Failure("Client not found");

            return await LogActivityAsync(
                ActivityType.ClientLoggedIn,
                $"{client.ContactPerson} logged in",
                null,
                clientId);
        }

        public async Task<Result> LogDocumentActivityAsync(int documentId, ActivityType activityType, string? additionalInfo = null)
        {
            var document = await _context.Documents
                .Include(d => d.Client)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
                return Result.Failure("Document not found");

            var title = activityType switch
            {
                ActivityType.DocumentUploaded => $"Uploaded document: {document.OriginalFileName}",
                ActivityType.DocumentViewed => $"Viewed document: {document.OriginalFileName}",
                ActivityType.DocumentDownloaded => $"Downloaded document: {document.OriginalFileName}",
                ActivityType.DocumentDeleted => $"Deleted document: {document.OriginalFileName}",
                ActivityType.DocumentVerified => $"Verified document: {document.OriginalFileName}",
                ActivityType.DocumentRejected => $"Rejected document: {document.OriginalFileName}",
                _ => $"Document activity: {document.OriginalFileName}"
            };

            return await LogActivityAsync(
                activityType,
                title,
                additionalInfo,
                document.ClientId,
                documentId);
        }

        public async Task<Result> LogTaxFilingActivityAsync(int taxFilingId, ActivityType activityType, string? additionalInfo = null)
        {
            var taxFiling = await _context.TaxFilings
                .Include(tf => tf.Client)
                .FirstOrDefaultAsync(tf => tf.TaxFilingId == taxFilingId);

            if (taxFiling == null)
                return Result.Failure("Tax filing not found");

            var title = activityType switch
            {
                ActivityType.TaxFilingCreated => $"Created {taxFiling.TaxType} filing for {taxFiling.TaxYear}",
                ActivityType.TaxFilingSubmitted => $"Submitted {taxFiling.TaxType} filing: {taxFiling.FilingReference}",
                ActivityType.TaxFilingReviewed => $"Reviewed {taxFiling.TaxType} filing: {taxFiling.FilingReference}",
                ActivityType.TaxFilingApproved => $"Approved {taxFiling.TaxType} filing: {taxFiling.FilingReference}",
                ActivityType.TaxFilingRejected => $"Rejected {taxFiling.TaxType} filing: {taxFiling.FilingReference}",
                ActivityType.TaxFilingFiled => $"Filed {taxFiling.TaxType} with SRA: {taxFiling.FilingReference}",
                _ => $"Tax filing activity: {taxFiling.FilingReference}"
            };

            return await LogActivityAsync(
                activityType,
                title,
                additionalInfo,
                taxFiling.ClientId,
                null,
                taxFilingId);
        }

        public async Task<Result> LogPaymentActivityAsync(int paymentId, ActivityType activityType, string? additionalInfo = null)
        {
            var payment = await _context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                return Result.Failure("Payment not found");

            var title = activityType switch
            {
                ActivityType.PaymentCreated => $"Payment created: {payment.Amount:C} - {payment.PaymentReference}",
                ActivityType.PaymentApproved => $"Payment approved: {payment.PaymentReference}",
                ActivityType.PaymentRejected => $"Payment rejected: {payment.PaymentReference}",
                ActivityType.PaymentProcessed => $"Payment processed: {payment.Amount:C}",
                ActivityType.PaymentReceiptGenerated => $"Receipt generated for payment: {payment.PaymentReference}",
                _ => $"Payment activity: {payment.PaymentReference}"
            };

            return await LogActivityAsync(
                activityType,
                title,
                additionalInfo,
                payment.ClientId,
                null,
                null,
                paymentId);
        }

        public async Task<Result> LogCommunicationActivityAsync(int clientId, ActivityType activityType, string title, string? description = null)
        {
            return await LogActivityAsync(activityType, title, description, clientId);
        }

        public async Task<Result<PagedResult<ActivityTimelineDto>>> GetActivitiesAsync(ActivityTimelineFilterDto filter)
        {
            try
            {
                var query = _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Include(at => at.User)
                    .Include(at => at.TargetUser)
                    .Include(at => at.Document)
                    .Include(at => at.TaxFiling)
                    .Include(at => at.Payment)
                    .AsQueryable();

                // Apply filters
                if (filter.ClientId.HasValue)
                    query = query.Where(at => at.ClientId == filter.ClientId);

                if (!string.IsNullOrEmpty(filter.UserId))
                    query = query.Where(at => at.UserId == filter.UserId);

                if (filter.ActivityType.HasValue)
                    query = query.Where(at => at.ActivityType == filter.ActivityType);

                if (filter.Category.HasValue)
                    query = query.Where(at => at.Category == filter.Category);

                if (filter.StartDate.HasValue)
                    query = query.Where(at => at.ActivityDate >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(at => at.ActivityDate <= filter.EndDate.Value);

                if (filter.IsVisibleToClient.HasValue)
                    query = query.Where(at => at.IsVisibleToClient == filter.IsVisibleToClient.Value);

                if (filter.MinPriority.HasValue)
                    query = query.Where(at => at.Priority >= filter.MinPriority.Value);

                var totalCount = await query.CountAsync();

                var activities = await query
                    .OrderByDescending(at => at.ActivityDate)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();

                return Result.Success<PagedResult<ActivityTimelineDto>>(new PagedResult<ActivityTimelineDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activities");
                return Result.Failure<PagedResult<ActivityTimelineDto>>("Error retrieving activities");
            }
        }

        public async Task<Result<List<ActivityTimelineGroupDto>>> GetGroupedActivitiesAsync(int clientId, int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var activities = await _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Include(at => at.User)
                    .Include(at => at.Document)
                    .Include(at => at.TaxFiling)
                    .Include(at => at.Payment)
                    .Where(at => at.ClientId == clientId && at.ActivityDate >= startDate)
                    .OrderByDescending(at => at.ActivityDate)
                    .ToListAsync();

                var grouped = activities
                    .GroupBy(a => a.ActivityDate.Date)
                    .Select(g => new ActivityTimelineGroupDto
                    {
                        Date = g.Key,
                        DateLabel = GetDateLabel(g.Key),
                        Activities = g.Select(MapToDto).ToList(),
                        TotalActivities = g.Count()
                    })
                    .ToList();

                return Result.Success<List<ActivityTimelineGroupDto>>(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grouped activities");
                return Result.Failure<List<ActivityTimelineGroupDto>>("Error retrieving grouped activities");
            }
        }

        public async Task<Result<ActivityTimelineSummaryDto>> GetActivitySummaryAsync(int? clientId = null, string? userId = null)
        {
            try
            {
                var query = _context.ActivityTimelines.AsQueryable();

                if (clientId.HasValue)
                    query = query.Where(at => at.ClientId == clientId);

                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(at => at.UserId == userId);

                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = now.AddDays(-30);

                var summary = new ActivityTimelineSummaryDto
                {
                    TotalActivities = await query.CountAsync(),
                    TodayActivities = await query.Where(a => a.ActivityDate >= todayStart).CountAsync(),
                    WeekActivities = await query.Where(a => a.ActivityDate >= weekStart).CountAsync(),
                    MonthActivities = await query.Where(a => a.ActivityDate >= monthStart).CountAsync()
                };

                // Activities by category
                summary.ActivitiesByCategory = await query
                    .GroupBy(a => a.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                // Activities by priority
                summary.ActivitiesByPriority = await query
                    .GroupBy(a => a.Priority)
                    .Select(g => new { Priority = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Priority, x => x.Count);

                // Recent high priority activities
                var recentHighPriority = await query
                    .Where(a => a.Priority >= ActivityPriority.High)
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(10)
                    .Include(a => a.Client)
                    .Include(a => a.User)
                    .ToListAsync();

                summary.RecentHighPriorityActivities = recentHighPriority.Select(MapToDto).ToList();

                return Result.Success<ActivityTimelineSummaryDto>(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity summary");
                return Result.Failure<ActivityTimelineSummaryDto>("Error retrieving activity summary");
            }
        }

        public async Task<Result<List<ClientActivitySummaryDto>>> GetClientActivitySummariesAsync(string? associateId = null)
        {
            try
            {
                var clientsQuery = _context.Clients.AsQueryable();

                if (!string.IsNullOrEmpty(associateId))
                    clientsQuery = clientsQuery.Where(c => c.AssignedAssociateId == associateId);

                var clients = await clientsQuery.ToListAsync();
                var summaries = new List<ClientActivitySummaryDto>();

                foreach (var client in clients)
                {
                    var activities = await _context.ActivityTimelines
                        .Where(at => at.ClientId == client.ClientId)
                        .ToListAsync();

                    if (activities.Any())
                    {
                        var summary = new ClientActivitySummaryDto
                        {
                            ClientId = client.ClientId,
                            ClientName = client.BusinessName,
                            LastActivityDate = activities.Max(a => a.ActivityDate),
                            TotalActivities = activities.Count,
                            DocumentActivities = activities.Count(a => a.Category == ActivityCategory.Document),
                            TaxFilingActivities = activities.Count(a => a.Category == ActivityCategory.TaxFiling),
                            PaymentActivities = activities.Count(a => a.Category == ActivityCategory.Payment),
                            CommunicationActivities = activities.Count(a => a.Category == ActivityCategory.Communication)
                        };

                        var lastActivity = activities.OrderByDescending(a => a.ActivityDate).FirstOrDefault();
                        if (lastActivity != null)
                        {
                            summary.LastActivity = MapToDto(lastActivity);
                        }

                        summaries.Add(summary);
                    }
                }

                return Result.Success<List<ClientActivitySummaryDto>>(summaries.OrderByDescending(s => s.LastActivityDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client activity summaries");
                return Result.Failure<List<ClientActivitySummaryDto>>("Error retrieving client activity summaries");
            }
        }

        public async Task<Result<PagedResult<ActivityTimelineDto>>> GetClientActivitiesAsync(int clientId, int page = 1, int pageSize = 50)
        {
            var filter = new ActivityTimelineFilterDto
            {
                ClientId = clientId,
                Page = page,
                PageSize = pageSize,
                IsVisibleToClient = true
            };

            return await GetActivitiesAsync(filter);
        }

        public async Task<Result<List<ActivityTimelineDto>>> GetRecentClientActivitiesAsync(int clientId, int count = 10)
        {
            try
            {
                var activities = await _context.ActivityTimelines
                    .Include(at => at.User)
                    .Include(at => at.Document)
                    .Include(at => at.TaxFiling)
                    .Include(at => at.Payment)
                    .Where(at => at.ClientId == clientId && at.IsVisibleToClient)
                    .OrderByDescending(at => at.ActivityDate)
                    .Take(count)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();
                return Result.Success<List<ActivityTimelineDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent client activities");
                return Result.Failure<List<ActivityTimelineDto>>("Error retrieving recent activities");
            }
        }

        public async Task<Result<PagedResult<ActivityTimelineDto>>> GetAssociateClientActivitiesAsync(string associateId, int page = 1, int pageSize = 50)
        {
            try
            {
                var clientIds = await _context.Clients
                    .Where(c => c.AssignedAssociateId == associateId)
                    .Select(c => c.ClientId)
                    .ToListAsync();

                var query = _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Include(at => at.User)
                    .Include(at => at.Document)
                    .Include(at => at.TaxFiling)
                    .Include(at => at.Payment)
                    .Where(at => clientIds.Contains(at.ClientId ?? 0) && at.IsVisibleToAssociate);

                var totalCount = await query.CountAsync();

                var activities = await query
                    .OrderByDescending(at => at.ActivityDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();

                return Result.Success<PagedResult<ActivityTimelineDto>>(new PagedResult<ActivityTimelineDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting associate client activities");
                return Result.Failure<PagedResult<ActivityTimelineDto>>("Error retrieving activities");
            }
        }

        public async Task<Result<List<ActivityTimelineDto>>> GetHighPriorityActivitiesAsync(string? associateId = null, int count = 20)
        {
            try
            {
                var query = _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Include(at => at.User)
                    .Where(at => at.Priority >= ActivityPriority.High);

                if (!string.IsNullOrEmpty(associateId))
                {
                    var clientIds = await _context.Clients
                        .Where(c => c.AssignedAssociateId == associateId)
                        .Select(c => c.ClientId)
                        .ToListAsync();

                    query = query.Where(at => clientIds.Contains(at.ClientId ?? 0));
                }

                var activities = await query
                    .OrderByDescending(at => at.Priority)
                    .ThenByDescending(at => at.ActivityDate)
                    .Take(count)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();
                return Result.Success<List<ActivityTimelineDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting high priority activities");
                return Result.Failure<List<ActivityTimelineDto>>("Error retrieving high priority activities");
            }
        }

        public async Task<Result<List<ActivityTimelineDto>>> GetSystemAlertsAsync(int? clientId = null, int days = 7)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var query = _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Where(at => at.Category == ActivityCategory.System && at.ActivityDate >= startDate);

                if (clientId.HasValue)
                    query = query.Where(at => at.ClientId == clientId);

                var activities = await query
                    .OrderByDescending(at => at.ActivityDate)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();
                return Result.Success<List<ActivityTimelineDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system alerts");
                return Result.Failure<List<ActivityTimelineDto>>("Error retrieving system alerts");
            }
        }

        public async Task<Result<PagedResult<ActivityTimelineDto>>> SearchActivitiesAsync(string searchTerm, ActivityTimelineFilterDto? filter = null)
        {
            try
            {
                var query = _context.ActivityTimelines
                    .Include(at => at.Client)
                    .Include(at => at.User)
                    .Include(at => at.Document)
                    .Include(at => at.TaxFiling)
                    .Include(at => at.Payment)
                    .Where(at => at.Title.Contains(searchTerm) || at.Description.Contains(searchTerm));

                if (filter != null)
                {
                    if (filter.ClientId.HasValue)
                        query = query.Where(at => at.ClientId == filter.ClientId);

                    if (filter.Category.HasValue)
                        query = query.Where(at => at.Category == filter.Category);

                    if (filter.StartDate.HasValue)
                        query = query.Where(at => at.ActivityDate >= filter.StartDate.Value);

                    if (filter.EndDate.HasValue)
                        query = query.Where(at => at.ActivityDate <= filter.EndDate.Value);
                }

                var totalCount = await query.CountAsync();
                var page = filter?.Page ?? 1;
                var pageSize = filter?.PageSize ?? 50;

                var activities = await query
                    .OrderByDescending(at => at.ActivityDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = activities.Select(MapToDto).ToList();

                return Result.Success<PagedResult<ActivityTimelineDto>>(new PagedResult<ActivityTimelineDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching activities");
                return Result.Failure<PagedResult<ActivityTimelineDto>>("Error searching activities");
            }
        }

        public async Task<Result<byte[]>> ExportActivitiesToCsvAsync(ActivityTimelineFilterDto filter)
        {
            try
            {
                var activitiesResult = await GetActivitiesAsync(filter);
                if (!activitiesResult.IsSuccess)
                    return Result.Failure<byte[]>(activitiesResult.ErrorMessage);

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(activitiesResult.Value.Items);
                writer.Flush();

                return Result.Success<byte[]>(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting activities to CSV");
                return Result.Failure<byte[]>("Error exporting activities");
            }
        }

        public async Task<Result<byte[]>> ExportActivitiesToPdfAsync(int clientId, DateTime startDate, DateTime endDate)
        {
            // PDF generation would require a library like iTextSharp or similar
            // For now, returning a placeholder
            return Result.Failure<byte[]>("PDF export not implemented");
        }

        public async Task<Result> MarkActivitiesAsReadAsync(int clientId, string userId)
        {
            try
            {
                // This would mark activities as read for a specific user
                // Could be implemented with a separate read tracking table
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking activities as read");
                return Result.Failure("Error marking activities as read");
            }
        }

        public async Task<Result> DeleteOldActivitiesAsync(int daysToKeep = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldActivities = await _context.ActivityTimelines
                    .Where(at => at.ActivityDate < cutoffDate)
                    .ToListAsync();

                _context.ActivityTimelines.RemoveRange(oldActivities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} old activity timeline entries", oldActivities.Count);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old activities");
                return Result.Failure("Error deleting old activities");
            }
        }

        private async Task<ActivityTimelineDto?> GetActivityByIdAsync(int id)
        {
            var activity = await _context.ActivityTimelines
                .Include(at => at.Client)
                .Include(at => at.User)
                .Include(at => at.TargetUser)
                .Include(at => at.Document)
                .Include(at => at.TaxFiling)
                .Include(at => at.Payment)
                .FirstOrDefaultAsync(at => at.ActivityTimelineId == id);

            return activity != null ? MapToDto(activity) : null;
        }

        private ActivityTimelineDto MapToDto(ActivityTimeline activity)
        {
            var dto = new ActivityTimelineDto
            {
                ActivityTimelineId = activity.ActivityTimelineId,
                ActivityType = activity.ActivityType,
                Category = activity.Category,
                Priority = activity.Priority,
                Title = activity.Title,
                Description = activity.Description,
                ClientId = activity.ClientId,
                ClientName = activity.Client?.BusinessName,
                ClientNumber = activity.Client?.ClientNumber,
                UserId = activity.UserId,
                UserName = activity.User != null ? $"{activity.User.FirstName} {activity.User.LastName}" : "System",
                TargetUserId = activity.TargetUserId,
                TargetUserName = activity.TargetUser != null ? $"{activity.TargetUser.FirstName} {activity.TargetUser.LastName}" : null,
                DocumentId = activity.DocumentId,
                DocumentName = activity.Document?.OriginalFileName,
                TaxFilingId = activity.TaxFilingId,
                TaxFilingReference = activity.TaxFiling?.FilingReference,
                PaymentId = activity.PaymentId,
                PaymentReference = activity.Payment?.PaymentReference,
                MessageId = activity.MessageId,
                Metadata = activity.Metadata,
                ActivityDate = activity.ActivityDate,
                TimeAgo = GetTimeAgo(activity.ActivityDate),
                IsNew = activity.ActivityDate > DateTime.UtcNow.AddHours(-24)
            };

            // Set icon and color based on activity type
            (dto.Icon, dto.Color) = GetIconAndColor(activity.ActivityType, activity.Priority);

            return dto;
        }

        private ActivityCategory GetCategoryFromType(ActivityType type)
        {
            return type switch
            {
                ActivityType.ClientRegistered or ActivityType.ClientLoggedIn or ActivityType.ClientProfileUpdated or ActivityType.ClientStatusChanged
                    => ActivityCategory.Client,
                ActivityType.DocumentUploaded or ActivityType.DocumentViewed or ActivityType.DocumentDownloaded or ActivityType.DocumentDeleted
                    or ActivityType.DocumentVerified or ActivityType.DocumentRejected or ActivityType.DocumentRequested
                    => ActivityCategory.Document,
                ActivityType.TaxFilingCreated or ActivityType.TaxFilingSubmitted or ActivityType.TaxFilingReviewed
                    or ActivityType.TaxFilingApproved or ActivityType.TaxFilingRejected or ActivityType.TaxFilingFiled or ActivityType.TaxFilingUpdated
                    => ActivityCategory.TaxFiling,
                ActivityType.PaymentCreated or ActivityType.PaymentApproved or ActivityType.PaymentRejected
                    or ActivityType.PaymentProcessed or ActivityType.PaymentReceiptGenerated
                    => ActivityCategory.Payment,
                ActivityType.MessageSent or ActivityType.MessageReceived or ActivityType.EmailSent
                    or ActivityType.EmailReceived or ActivityType.SMSSent or ActivityType.NotificationSent or ActivityType.NotificationRead
                    => ActivityCategory.Communication,
                ActivityType.UserCreated or ActivityType.UserDeactivated or ActivityType.RoleChanged
                    or ActivityType.PermissionGranted or ActivityType.PermissionRevoked
                    => ActivityCategory.Administrative,
                _ => ActivityCategory.System
            };
        }

        private (string icon, string color) GetIconAndColor(ActivityType type, ActivityPriority priority)
        {
            var icon = type switch
            {
                ActivityType.ClientRegistered => "user-plus",
                ActivityType.ClientLoggedIn => "log-in",
                ActivityType.DocumentUploaded => "upload",
                ActivityType.DocumentVerified => "check-circle",
                ActivityType.DocumentRejected => "x-circle",
                ActivityType.TaxFilingSubmitted => "send",
                ActivityType.TaxFilingApproved => "check-square",
                ActivityType.PaymentProcessed => "dollar-sign",
                ActivityType.MessageSent => "message-square",
                ActivityType.EmailSent => "mail",
                ActivityType.DeadlineApproaching => "alert-triangle",
                _ => "activity"
            };

            var color = priority switch
            {
                ActivityPriority.Critical => "text-red-600",
                ActivityPriority.High => "text-orange-600",
                ActivityPriority.Normal => "text-blue-600",
                _ => "text-gray-600"
            };

            return (icon, color);
        }

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;

            return timeSpan switch
            {
                { TotalMinutes: < 1 } => "just now",
                { TotalMinutes: < 60 } => $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago",
                { TotalHours: < 24 } => $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago",
                { TotalDays: < 7 } => $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago",
                { TotalDays: < 30 } => $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) != 1 ? "s" : "")} ago",
                { TotalDays: < 365 } => $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) != 1 ? "s" : "")} ago",
                _ => $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) != 1 ? "s" : "")} ago"
            };
        }

        private string GetDateLabel(DateTime date)
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            if (date.Date == today)
                return "Today";
            else if (date.Date == yesterday)
                return "Yesterday";
            else if (date.Date > today.AddDays(-7))
                return date.ToString("dddd"); // Day name
            else
                return date.ToString("MMMM d, yyyy");
        }

        private string? GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
        }
    }
}