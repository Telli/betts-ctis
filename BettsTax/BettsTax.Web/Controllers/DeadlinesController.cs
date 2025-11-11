using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Compliance;
using System.Security.Claims;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;

using System.Text;

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
        private readonly ILogger<DeadlinesController> _logger;
        private readonly ApplicationDbContext _db;

        public DeadlinesController(
            IDeadlineMonitoringService deadlineService,
            ILogger<DeadlinesController> logger,
            ApplicationDbContext db)
        {
            _deadlineService = deadlineService;
            _logger = logger;
            _db = db;
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
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                _logger.LogInformation(
                    "User {UserId} with role {Role} requesting upcoming deadlines for {Days} days, ClientId: {ClientId}",
                    userId, userRole, days, clientId?.ToString() ?? "all");

                // Get deadlines from service
                var deadlines = await _deadlineService.GetUpcomingDeadlinesAsync(clientId, days);

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
                        clientId = clientId
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOverdueDeadlines([FromQuery] int? clientId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                _logger.LogInformation(
                    "User {UserId} with role {Role} requesting overdue deadlines, ClientId: {ClientId}",
                    userId, userRole, clientId?.ToString() ?? "all");

                // Get overdue deadlines from service
                var deadlines = await _deadlineService.GetOverdueItemsAsync(clientId);

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
                        clientId = clientId
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeadlines(
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] string? priority = null,
            [FromQuery] int? clientId = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _db.ComplianceDeadlines.AsNoTracking().AsQueryable();

                if (clientId.HasValue)
                    query = query.Where(d => d.ClientId == clientId.Value);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(d => d.Requirements.Contains(category));

                if (fromDate.HasValue)
                    query = query.Where(d => d.DueDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(d => d.DueDate <= toDate.Value);

                if (!string.IsNullOrWhiteSpace(priority) && priority.ToLower() != "all")
                {
                    var p = priority.ToLower();
                    query = query.Where(d => (d.Priority == DeadlinePriority.Low && p == "low")
                                           || (d.Priority == DeadlinePriority.Medium && p == "medium")
                                           || (d.Priority == DeadlinePriority.High && p == "high")
                                           || (d.Priority == DeadlinePriority.Critical && p == "critical"));
                }

                if (!string.IsNullOrWhiteSpace(type) && type.ToLower() != "all")
                {
                    var t = type.ToLower();
                    query = query.Where(d =>
                        (d.TaxType == TaxType.GST && (t == "tax-filing" || t == "compliance"))
                        || (d.TaxType == TaxType.IncomeTax && (t == "tax-filing" || t == "compliance"))
                        || (d.TaxType == TaxType.PAYE && (t == "payment" || t == "tax-filing"))
                        || (d.TaxType == TaxType.WithholdingTax && (t == "payment" || t == "tax-filing"))
                        || (d.TaxType == TaxType.CorporateIncomeTax && (t == "tax-filing"))
                        || (d.TaxType == TaxType.ExciseDuty && (t == "tax-filing" || t == "payment"))
                    );
                }

                var now = DateTime.UtcNow.Date;
                if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
                {
                    switch (status.ToLower())
                    {
                        case "completed":
                            query = query.Where(d => d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved);
                            break;
                        case "overdue":
                            query = query.Where(d => d.DueDate < now && (d.CompletedAt == null && d.Status != FilingStatus.Filed && d.Status != FilingStatus.Approved));
                            break;
                        case "due-soon":
                            var soon = now.AddDays(7);
                            query = query.Where(d => d.DueDate >= now && d.DueDate <= soon && (d.CompletedAt == null && d.Status != FilingStatus.Filed && d.Status != FilingStatus.Approved));
                            break;
                        case "upcoming":
                            query = query.Where(d => d.DueDate > now && (d.CompletedAt == null && d.Status != FilingStatus.Filed && d.Status != FilingStatus.Approved));
                            break;
                    }
                }

                var items = await query
                    .OrderBy(d => d.DueDate)
                    .Take(1000)
                    .Select(d => new
                    {
                        id = d.Id.ToString(),
                        title = d.TaxType + " deadline",
                        type = d.TaxType == TaxType.GST || d.TaxType == TaxType.IncomeTax || d.TaxType == TaxType.CorporateIncomeTax ? "tax-filing" : "payment",
                        description = d.Requirements,
                        dueDate = d.DueDate,
                        status = d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved
                            ? "completed"
                            : (d.DueDate < now ? "overdue" : (d.DueDate <= now.AddDays(7) ? "due-soon" : "upcoming")),
                        priority = d.Priority == DeadlinePriority.High || d.Priority == DeadlinePriority.Critical ? "high" : (d.Priority == DeadlinePriority.Medium ? "medium" : "low"),
                        category = d.TaxType.ToString(),
                        clientId = d.ClientId.ToString(),
                        amount = (double?)d.EstimatedTaxLiability,
                        reminderSet = false,
                        reminderDates = Array.Empty<DateTime>(),
                        notes = d.Requirements,
                        completedDate = d.CompletedAt,
                        taxYear = d.DueDate.Year,
                        taxType = d.TaxType.ToString()
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deadlines list");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving deadlines" });
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeadlineStats([FromQuery] int? clientId = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation(
                    "User {UserId} requesting deadline statistics, ClientId: {ClientId}",
                    userId, clientId?.ToString() ?? "all");

                // Get deadlines for different time periods
                var next7DaysTask = _deadlineService.GetUpcomingDeadlinesAsync(clientId, 7);
                var next30DaysTask = _deadlineService.GetUpcomingDeadlinesAsync(clientId, 30);
                var overdueTask = _deadlineService.GetOverdueItemsAsync(clientId);

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

        // ===== CRUD endpoints =====
        public class CreateDeadlineRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = "tax-filing"; // tax-filing | payment | compliance | document
            public string Description { get; set; } = string.Empty;
            public DateTime DueDate { get; set; }
            public string Priority { get; set; } = "medium"; // high | medium | low
            public string Category { get; set; } = string.Empty;
            public int? ClientId { get; set; }
            public decimal? Amount { get; set; }
            public int? TaxYear { get; set; }
            public string? TaxType { get; set; }
            public int[]? ReminderDaysBefore { get; set; }
            public string? Notes { get; set; }
        }

        public class UpdateDeadlineRequest
        {
            public DateTime? DueDate { get; set; }
            public string? Priority { get; set; }
            public string? Description { get; set; }
            public decimal? Amount { get; set; }
            public string? Category { get; set; }
            public string? TaxType { get; set; }
            public string? Status { get; set; }
        }

        public class CompleteDeadlineRequest
        {
            public string? Notes { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeadline([FromBody] CreateDeadlineRequest req)
        {
            try
            {
                if (req.ClientId == null || req.ClientId <= 0)
                    return BadRequest(new { success = false, message = "clientId is required" });

                var taxType = ParseTaxType(req.TaxType, req.Type, req.Category);
                var priority = ParsePriority(req.Priority);

                var entity = new ComplianceDeadline
                {
                    ClientId = req.ClientId.Value,
                    TaxType = taxType,
                    DueDate = req.DueDate,
                    Status = FilingStatus.Draft,
                    EstimatedTaxLiability = req.Amount ?? 0,
                    DocumentsReady = false,
                    Priority = priority,
                    Requirements = BuildRequirements(req),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.ComplianceDeadlines.Add(entity);
                await _db.SaveChangesAsync();

                var now = DateTime.UtcNow.Date;
                var dto = new
                {
                    id = entity.Id.ToString(),
                    title = string.IsNullOrWhiteSpace(req.Title) ? entity.TaxType + " deadline" : req.Title,
                    type = InferFrontendType(entity.TaxType),
                    description = req.Description ?? entity.Requirements,
                    dueDate = entity.DueDate,
                    status = ToFrontendStatus(entity, now),
                    priority = ToFrontendPriority(entity.Priority),
                    category = entity.TaxType.ToString(),
                    clientId = entity.ClientId.ToString(),
                    amount = (double?)entity.EstimatedTaxLiability,
                    reminderSet = false,
                    reminderDates = Array.Empty<DateTime>(),
                    notes = req.Notes,
                    completedDate = entity.CompletedAt,
                    taxYear = entity.DueDate.Year,
                    taxType = entity.TaxType.ToString()
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deadline");
                return StatusCode(500, new { success = false, message = "Failed to create deadline" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeadline([FromRoute] int id, [FromBody] UpdateDeadlineRequest req)
        {
            try
            {
                var entity = await _db.ComplianceDeadlines.FirstOrDefaultAsync(d => d.Id == id);
                if (entity == null)
                    return NotFound(new { success = false, message = "Deadline not found" });

                if (req.DueDate.HasValue) entity.DueDate = req.DueDate.Value;
                if (!string.IsNullOrWhiteSpace(req.Priority)) entity.Priority = ParsePriority(req.Priority);
                if (!string.IsNullOrWhiteSpace(req.Description)) entity.Requirements = req.Description;
                if (req.Amount.HasValue) entity.EstimatedTaxLiability = req.Amount.Value;
                if (!string.IsNullOrWhiteSpace(req.Category)) entity.Requirements = (entity.Requirements ?? string.Empty) + $" | Category: {req.Category}";
                if (!string.IsNullOrWhiteSpace(req.TaxType)) entity.TaxType = ParseTaxType(req.TaxType, null, null);
                if (!string.IsNullOrWhiteSpace(req.Status))
                {
                    var s = req.Status.ToLower();
                    if (s == "completed") { entity.CompletedAt = DateTime.UtcNow; entity.Status = FilingStatus.Filed; }
                }
                entity.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var now = DateTime.UtcNow.Date;
                var dto = new
                {
                    id = entity.Id.ToString(),
                    title = entity.TaxType + " deadline",
                    type = InferFrontendType(entity.TaxType),
                    description = entity.Requirements,
                    dueDate = entity.DueDate,
                    status = ToFrontendStatus(entity, now),
                    priority = ToFrontendPriority(entity.Priority),
                    category = entity.TaxType.ToString(),
                    clientId = entity.ClientId.ToString(),
                    amount = (double?)entity.EstimatedTaxLiability,
                    reminderSet = false,
                    reminderDates = Array.Empty<DateTime>(),
                    notes = entity.Requirements,
                    completedDate = entity.CompletedAt,
                    taxYear = entity.DueDate.Year,
                    taxType = entity.TaxType.ToString()
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deadline {Id}", id);
                return StatusCode(500, new { success = false, message = "Failed to update deadline" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeadline([FromRoute] int id)
        {
            try
            {
                var entity = await _db.ComplianceDeadlines.FirstOrDefaultAsync(d => d.Id == id);
                if (entity == null)
                    return NotFound(new { success = false, message = "Deadline not found" });

                _db.ComplianceDeadlines.Remove(entity);
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deadline {Id}", id);
                return StatusCode(500, new { success = false, message = "Failed to delete deadline" });
            }
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> MarkAsCompleted([FromRoute] int id, [FromBody] CompleteDeadlineRequest req)
        {
            try
            {
                var entity = await _db.ComplianceDeadlines.FirstOrDefaultAsync(d => d.Id == id);
                if (entity == null)
                    return NotFound(new { success = false, message = "Deadline not found" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                entity.CompletedAt = req.CompletedAt ?? DateTime.UtcNow;
                entity.CompletedBy = userId;
                entity.Status = FilingStatus.Filed;
                entity.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var now = DateTime.UtcNow.Date;
                var dto = new
                {
                    id = entity.Id.ToString(),
                    title = entity.TaxType + " deadline",
                    type = InferFrontendType(entity.TaxType),
                    description = entity.Requirements,
                    dueDate = entity.DueDate,
                    status = ToFrontendStatus(entity, now),
                    priority = ToFrontendPriority(entity.Priority),
                    category = entity.TaxType.ToString(),
                    clientId = entity.ClientId.ToString(),
                    amount = (double?)entity.EstimatedTaxLiability,
                    reminderSet = false,
                    reminderDates = Array.Empty<DateTime>(),
                    notes = entity.Requirements,
                    completedDate = entity.CompletedAt,
                    taxYear = entity.DueDate.Year,
                    taxType = entity.TaxType.ToString()
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking deadline {Id} completed", id);
                return StatusCode(500, new { success = false, message = "Failed to mark deadline complete" });
            }
        }

        private static FilingStatus CompletedStatusFallback => FilingStatus.Filed;
        private static string ToFrontendPriority(DeadlinePriority p) =>
            (p == DeadlinePriority.High || p == DeadlinePriority.Critical) ? "high" : (p == DeadlinePriority.Medium ? "medium" : "low");

        private static string InferFrontendType(TaxType taxType)
        {
            return taxType == TaxType.GST || taxType == TaxType.IncomeTax || taxType == TaxType.CorporateIncomeTax ? "tax-filing" : "payment";
        }

        private static string ToFrontendStatus(ComplianceDeadline d, DateTime now)
        {
            if (d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved) return "completed";
            if (d.DueDate < now) return "overdue";
            if (d.DueDate <= now.AddDays(7)) return "due-soon";
            return "upcoming";
        }

        private static DeadlinePriority ParsePriority(string? priority)
        {
            return (priority?.ToLower()) switch
            {
                "high" => DeadlinePriority.High,
                "low" => DeadlinePriority.Low,
                "critical" => DeadlinePriority.Critical,
                _ => DeadlinePriority.Medium
            };
        }

        private static TaxType ParseTaxType(string? taxType, string? type, string? category)
        {
            if (!string.IsNullOrWhiteSpace(taxType) && Enum.TryParse<TaxType>(taxType, true, out var t)) return t;
            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TaxType>(category, true, out var tc)) return tc;
            return TaxType.GST;
        }

        private static string BuildRequirements(CreateDeadlineRequest req)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.Title)) parts.Add($"Title: {req.Title}");
            if (!string.IsNullOrWhiteSpace(req.Description)) parts.Add($"Description: {req.Description}");
            if (!string.IsNullOrWhiteSpace(req.Category)) parts.Add($"Category: {req.Category}");
            if (!string.IsNullOrWhiteSpace(req.Notes)) parts.Add($"Notes: {req.Notes}");
            return string.Join(" | ", parts);
        }


        // ===== Reminder endpoints (SMS-based minimal implementation) =====
        public class SetRemindersRequest
        {
            public int[] DaysBefore { get; set; } = Array.Empty<int>();
            public string[] Methods { get; set; } = Array.Empty<string>(); // email | sms | system
        }

        public class UpdateReminderRequest
        {
            public DateTime? ReminderDate { get; set; }
            public string? Status { get; set; } // pending | sent | failed
        }

        private static string SmsReminderId(int smsNotificationId) => $"sms-{smsNotificationId}";

        private static object ToReminderDto(SmsNotification s)
        {
            return new
            {
                id = SmsReminderId(s.SmsNotificationId),
                deadlineId = s.ProviderMessageId?.StartsWith("DL-") == true ? s.ProviderMessageId.Substring(3) : "",
                reminderDate = s.ScheduledDate ?? s.CreatedDate,
                method = "sms",
                status = s.Status switch
                {
                    SmsStatus.Sent => "sent",
                    SmsStatus.Failed => "failed",
                    _ => "pending"
                },
                createdAt = s.CreatedDate,
                sentAt = s.SentDate
            };
        }

        [HttpPost("{deadlineId}/reminders")]
        public async Task<IActionResult> SetReminders([FromRoute] int deadlineId, [FromBody] SetRemindersRequest req)
        {
            try
            {
                var deadline = await _db.ComplianceDeadlines.FirstOrDefaultAsync(d => d.Id == deadlineId);
                if (deadline == null)
                    return NotFound(new { success = false, message = "Deadline not found" });

                var created = new List<object>();
                var now = DateTime.UtcNow;

                foreach (var day in req.DaysBefore.Distinct())
                {
                    var scheduled = deadline.DueDate.Date.AddDays(-day).AddHours(9);
                    if (scheduled < now) scheduled = now.AddMinutes(1);

                    if (req.Methods.Contains("sms", StringComparer.OrdinalIgnoreCase))
                    {
                        var sms = new SmsNotification
                        {
                            PhoneNumber = "0000000000",
                            RecipientName = string.Empty,
                            ClientId = deadline.ClientId,
                            Message = $"Reminder: {deadline.TaxType} deadline due {deadline.DueDate:yyyy-MM-dd}",
                            Type = SmsType.DeadlineReminder,
                            Status = SmsStatus.Pending,
                            ProviderMessageId = $"DL-{deadline.Id}",
                            ScheduledDate = scheduled,
                            IsScheduled = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        _db.SmsNotifications.Add(sms);
                        await _db.SaveChangesAsync();
                        created.Add(ToReminderDto(sms));
                    }

                    // For email/system methods, return virtual placeholders (not persisted)
                    foreach (var m in req.Methods)
                    {
                        if (!m.Equals("sms", StringComparison.OrdinalIgnoreCase))
                        {
                            created.Add(new
                            {
                                id = $"{m.ToLower()}-virtual-{deadline.Id}-{day}",
                                deadlineId = deadline.Id.ToString(),
                                reminderDate = scheduled,
                                method = m.ToLower(),
                                status = "pending",
                                createdAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                return Ok(new { success = true, data = created });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting reminders for deadline {Id}", deadlineId);
                return StatusCode(500, new { success = false, message = "Failed to set reminders" });
            }
        }

        [HttpGet("{deadlineId}/reminders")]
        public async Task<IActionResult> GetDeadlineReminders([FromRoute] int deadlineId)
        {
            try
            {
                var items = await _db.SmsNotifications
                    .AsNoTracking()
                    .Where(s => s.Type == SmsType.DeadlineReminder && s.ProviderMessageId == $"DL-{deadlineId}")
                    .OrderBy(s => s.ScheduledDate ?? s.CreatedDate)
                    .ToListAsync();

                var dtos = items.Select(ToReminderDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for deadline {Id}", deadlineId);
                return StatusCode(500, new { success = false, message = "Failed to get reminders" });
            }
        }

        [HttpPut("reminders/{reminderId}")]
        public async Task<IActionResult> UpdateReminder([FromRoute] string reminderId, [FromBody] UpdateReminderRequest req)
        {
            try
            {
                if (!reminderId.StartsWith("sms-", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { success = false, message = "Only SMS reminders are supported for update" });

                if (!int.TryParse(reminderId.Substring(4), out var smsId))
                    return BadRequest(new { success = false, message = "Invalid reminder id" });

                var sms = await _db.SmsNotifications.FirstOrDefaultAsync(s => s.SmsNotificationId == smsId);
                if (sms == null) return NotFound(new { success = false, message = "Reminder not found" });

                if (req.ReminderDate.HasValue)
                {
                    sms.ScheduledDate = req.ReminderDate.Value;
                    sms.IsScheduled = true;
                }
                if (!string.IsNullOrWhiteSpace(req.Status))
                {
                    var st = req.Status.ToLower();
                    sms.Status = st switch
                    {
                        "sent" => SmsStatus.Sent,
                        "failed" => SmsStatus.Failed,
                        _ => SmsStatus.Pending
                    };
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, data = ToReminderDto(sms) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reminder {Id}", reminderId);
                return StatusCode(500, new { success = false, message = "Failed to update reminder" });
            }
        }

        [HttpDelete("reminders/{reminderId}")]
        public async Task<IActionResult> DeleteReminder([FromRoute] string reminderId)
        {
            try
            {
                if (!reminderId.StartsWith("sms-", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { success = false, message = "Only SMS reminders are supported for deletion" });

                if (!int.TryParse(reminderId.Substring(4), out var smsId))
                    return BadRequest(new { success = false, message = "Invalid reminder id" });

                var sms = await _db.SmsNotifications.FirstOrDefaultAsync(s => s.SmsNotificationId == smsId);
                if (sms == null) return NotFound(new { success = false, message = "Reminder not found" });

                _db.SmsNotifications.Remove(sms);
                await _db.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reminder {Id}", reminderId);
                return StatusCode(500, new { success = false, message = "Failed to delete reminder" });
            }
        }

        // ===== Calendar endpoints =====
        public class CalendarEventDto
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public string Type { get; set; } = "deadline"; // deadline | reminder
            public string Priority { get; set; } = "medium"; // high | medium | low
            public string? ClientName { get; set; }
            public decimal? Amount { get; set; }
            public string Status { get; set; } = "upcoming";
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month, [FromQuery] int? clientId = null, [FromQuery] bool includeReminders = false)
        {
            try
            {
                if (year <= 0 || month < 1 || month > 12)
                    return BadRequest(new { success = false, message = "Invalid year or month" });

                var start = new DateTime(year, month, 1);
                var end = start.AddMonths(1);

                var q = _db.ComplianceDeadlines.AsNoTracking().Where(d => d.DueDate >= start && d.DueDate < end);
                if (clientId.HasValue) q = q.Where(d => d.ClientId == clientId.Value);

                var deadlines = await q.Include(d => d.Client)
                    .OrderBy(d => d.DueDate)
                    .ToListAsync();

                var now = DateTime.UtcNow.Date;
                var events = deadlines.Select(d => new CalendarEventDto
                {
                    Id = $"d-{d.Id}",
                    Title = $"{d.TaxType} deadline",
                    Date = d.DueDate,
                    Type = "deadline",
                    Priority = d.Priority == DeadlinePriority.High || d.Priority == DeadlinePriority.Critical ? "high" : (d.Priority == DeadlinePriority.Medium ? "medium" : "low"),
                    ClientName = d.Client != null && !string.IsNullOrWhiteSpace(d.Client.BusinessName) ? d.Client.BusinessName : d.Client != null && !string.IsNullOrWhiteSpace(d.Client.Name) ? d.Client.Name : null,
                    Amount = d.EstimatedTaxLiability,
                    Status = d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved
                        ? "completed"
                        : (d.DueDate.Date < now ? "overdue" : (d.DueDate.Date <= now.AddDays(7) ? "due-soon" : "upcoming"))
                }).ToList();

                if (includeReminders)
                {
                    var sms = await _db.SmsNotifications.AsNoTracking()
                        .Where(s => s.Type == SmsType.DeadlineReminder && s.ScheduledDate >= start && s.ScheduledDate < end)
                        .ToListAsync();

                    foreach (var s in sms)
                    {
                        events.Add(new CalendarEventDto
                        {
                            Id = SmsReminderId(s.SmsNotificationId),
                            Title = "Reminder",
                            Date = s.ScheduledDate ?? s.CreatedDate,
                            Type = "reminder",
                            Priority = "medium",
                            ClientName = null,
                            Amount = null,
                            Status = s.Status == SmsStatus.Sent ? "sent" : (s.Status == SmsStatus.Failed ? "failed" : "pending")
                        });
                    }
                }

                return Ok(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building calendar for {Year}-{Month}", year, month);
                return StatusCode(500, new { success = false, message = "Failed to build calendar" });
            }
        }

        private static List<DateTime> GenerateMonthlyDates(int year)
        {
            var dates = new List<DateTime>();
            for (int m = 1; m <= 12; m++)
            {
                var nextMonth = m == 12 ? 1 : m + 1;
                var nextYear = m == 12 ? year + 1 : year;
                dates.Add(new DateTime(nextYear, nextMonth, 21));
            }
            return dates;
        }

        private static List<DateTime> GenerateQuarterlyDates(int year)
        {
            return new List<DateTime>
            {
                new DateTime(year, 4, 30),
                new DateTime(year, 7, 31),
                new DateTime(year, 10, 31),
                new DateTime(year + 1, 1, 31)
            };
        }

        [HttpGet("sierra-leone-calendar")]
        public IActionResult GetSierraLeoneTaxCalendar([FromQuery] int year)
        {
            try
            {
                if (year <= 0) year = DateTime.UtcNow.Year;

                var events = new List<CalendarEventDto>();

                // GST & Payroll - monthly
                foreach (var date in GenerateMonthlyDates(year))
                {
                    if (date.Year != year) continue; // keep within requested year
                    events.Add(new CalendarEventDto { Id = $"gst-{date:yyyyMMdd}", Title = "GST Filing Due", Date = date, Type = "deadline", Priority = "medium", Status = "upcoming" });
                    events.Add(new CalendarEventDto { Id = $"payroll-{date:yyyyMMdd}", Title = "Payroll Tax Due", Date = date, Type = "deadline", Priority = "medium", Status = "upcoming" });
                }

                // Income Tax - quarterly
                foreach (var date in GenerateQuarterlyDates(year))
                {
                    if (date.Year != year) continue;
                    events.Add(new CalendarEventDto { Id = $"income-{date:yyyyMMdd}", Title = "Income Tax Installment Due", Date = date, Type = "deadline", Priority = "high", Status = "upcoming" });
                }

                // Excise - monthly (reuse monthly dates)
                foreach (var date in GenerateMonthlyDates(year))
                {
                    if (date.Year != year) continue;
                    events.Add(new CalendarEventDto { Id = $"excise-{date:yyyyMMdd}", Title = "Excise Duty Filing Due", Date = date, Type = "deadline", Priority = "medium", Status = "upcoming" });
                }

                return Ok(new { success = true, data = events.OrderBy(e => e.Date).ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Sierra Leone tax calendar for year {Year}", year);
                return StatusCode(500, new { success = false, message = "Failed to get Sierra Leone tax calendar" });
            }
        }

        // ===== Analytics endpoint =====
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string timeRange = "6m")
        {
            try
            {
                int months = timeRange?.ToLower() switch
                {
                    "3m" => 3,
                    "6m" => 6,
                    "12m" => 12,
                    _ => 6
                };
                var now = DateTime.UtcNow;
                var start = now.AddMonths(-months);

                var deadlines = await _db.ComplianceDeadlines.AsNoTracking()
                    .Where(d => d.DueDate >= start && d.DueDate <= now)
                    .ToListAsync();

                int total = deadlines.Count;
                if (total == 0)
                {
                    return Ok(new { success = true, data = new { completionRate = 0, averageDaysToComplete = 0, overdueRate = 0, onTimeCompletionTrend = Array.Empty<object>(), deadlinesByType = new Dictionary<string, int>(), clientPerformance = Array.Empty<object>() } });
                }

                bool IsCompleted(ComplianceDeadline d) => d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved;
                bool IsOverdue(ComplianceDeadline d)
                {
                    if (IsCompleted(d)) return (d.CompletedAt ?? d.DueDate) > d.DueDate;
                    return d.DueDate < now;
                }

                var completed = deadlines.Where(IsCompleted).ToList();
                var onTime = completed.Count(d => (d.CompletedAt ?? d.DueDate) <= d.DueDate);
                var overdue = deadlines.Count(IsOverdue);

                double avgDays = completed.Any()
                    ? completed.Average(d => ((d.CompletedAt ?? d.DueDate) - d.DueDate).TotalDays)
                    : 0;

                // Trend by month
                var trend = new List<object>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var mStart = new DateTime(now.AddMonths(-i).Year, now.AddMonths(-i).Month, 1);
                    var mEnd = mStart.AddMonths(1);
                    var mSet = deadlines.Where(d => d.DueDate >= mStart && d.DueDate < mEnd).ToList();
                    var mCompleted = mSet.Where(IsCompleted).ToList();
                    var mOnTime = mCompleted.Count(d => (d.CompletedAt ?? d.DueDate) <= d.DueDate);
                    var mOverdue = mSet.Count(IsOverdue);
                    trend.Add(new { month = mStart.ToString("yyyy-MM"), onTime = mOnTime, overdue = mOverdue });
                }

                // Breakdown by type
                string FrontendType(ComplianceDeadline d) => InferFrontendType(d.TaxType);
                var byType = deadlines.GroupBy(FrontendType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Client performance
                var clientIds = deadlines.Select(d => d.ClientId).Distinct().ToList();
                var clients = await _db.Clients.AsNoTracking().Where(c => clientIds.Contains(c.ClientId) || clientIds.Contains(c.Id)).ToListAsync();
                var clientPerf = deadlines.GroupBy(d => d.ClientId).Select(g =>
                {
                    var cl = clients.FirstOrDefault(c => c.ClientId == g.Key || c.Id == g.Key);
                    var gList = g.ToList();
                    var gCompleted = gList.Where(IsCompleted).ToList();
                    var rate = gList.Count == 0 ? 0 : (int)Math.Round(100.0 * gCompleted.Count(d => (d.CompletedAt ?? d.DueDate) <= d.DueDate) / gList.Count);
                    var overdueCount = gList.Count(IsOverdue);
                    var name = cl != null && !string.IsNullOrWhiteSpace(cl.BusinessName) ? cl.BusinessName : (cl != null && !string.IsNullOrWhiteSpace(cl.Name) ? cl.Name : $"Client {g.Key}");
                    return new { clientName = name, completionRate = rate, overdueCount };
                }).OrderByDescending(x => x.overdueCount).ThenBy(x => x.clientName).ToList();

                var result = new
                {
                    completionRate = (int)Math.Round(100.0 * onTime / total),
                    averageDaysToComplete = Math.Round(avgDays, 1),
                    overdueRate = (int)Math.Round(100.0 * overdue / total),
                    onTimeCompletionTrend = trend,
                    deadlinesByType = byType,
                    clientPerformance = clientPerf
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing deadlines analytics for range {Range}", timeRange);
                return StatusCode(500, new { success = false, message = "Failed to compute analytics" });
            }
        }

        // ===== Export endpoint =====
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "csv", [FromQuery] string? status = null, [FromQuery] string? type = null, [FromQuery] string? priority = null, [FromQuery] int? clientId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var q = _db.ComplianceDeadlines.AsNoTracking().Include(d => d.Client).AsQueryable();
                // filters
                if (clientId.HasValue) q = q.Where(d => d.ClientId == clientId);
                if (fromDate.HasValue) q = q.Where(d => d.DueDate >= fromDate);
                if (toDate.HasValue) q = q.Where(d => d.DueDate <= toDate);
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    var pr = ParsePriority(priority);
                    q = q.Where(d => d.Priority == pr);
                }
                if (!string.IsNullOrWhiteSpace(type))
                {
                    var wanted = type.ToLowerInvariant();
                    q = q.Where(d => InferFrontendType(d.TaxType) == wanted);
                }
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var now = DateTime.UtcNow.Date;
                    q = status.ToLower() switch
                    {
                        "completed" => q.Where(d => d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved),
                        "overdue" => q.Where(d => d.CompletedAt == null && d.DueDate < now),
                        "due-soon" => q.Where(d => d.CompletedAt == null && d.DueDate >= now && d.DueDate <= now.AddDays(7)),
                        "upcoming" => q.Where(d => d.CompletedAt == null && d.DueDate > now.AddDays(7)),
                        _ => q
                    };
                }

                var list = await q.OrderBy(d => d.DueDate).ToListAsync();

                switch (format.ToLowerInvariant())
                {
                    case "csv":
                        var csv = new StringBuilder();
                        csv.AppendLine("Id,Client,TaxType,DueDate,Status,Priority,Amount");
                        foreach (var d in list)
                        {
                            var clientName = d.Client != null && !string.IsNullOrWhiteSpace(d.Client.BusinessName) ? d.Client.BusinessName : d.Client?.Name ?? "";
                            var st = d.CompletedAt != null || d.Status == FilingStatus.Filed || d.Status == FilingStatus.Approved ? "completed" : (d.DueDate.Date < DateTime.UtcNow.Date ? "overdue" : "upcoming");
                            csv.AppendLine($"{d.Id},\"{clientName.Replace("\"", "''")}\",{d.TaxType},{d.DueDate:yyyy-MM-dd},{st},{d.Priority},{d.EstimatedTaxLiability}");
                        }
                        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                        return File(bytes, "text/csv", $"deadlines_{DateTime.UtcNow:yyyyMMdd}.csv");

                    case "ical":
                    case "ics":
                        var sb = new StringBuilder();
                        sb.AppendLine("BEGIN:VCALENDAR");
                        sb.AppendLine("VERSION:2.0");
                        sb.AppendLine("PRODID:-//BettsTax//Deadlines//EN");
                        foreach (var d in list)
                        {
                            sb.AppendLine("BEGIN:VEVENT");
                            sb.AppendLine($"UID:deadline-{d.Id}@bettstax");
                            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}");
                            sb.AppendLine($"DTSTART;VALUE=DATE:{d.DueDate:yyyyMMdd}");
                            sb.AppendLine($"SUMMARY:{d.TaxType} deadline");
                            sb.AppendLine("END:VEVENT");
                        }
                        sb.AppendLine("END:VCALENDAR");
                        var ics = Encoding.UTF8.GetBytes(sb.ToString());
                        return File(ics, "text/calendar", $"deadlines_{DateTime.UtcNow:yyyyMMdd}.ics");

                    case "excel":
                        // Minimal: fall back to CSV for now
                        goto case "csv";
                    case "pdf":
                        // Minimal: fall back to CSV for now
                        goto case "csv";
                    default:
                        return BadRequest(new { success = false, message = "Unsupported export format" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting deadlines as {Format}", format);
                return StatusCode(500, new { success = false, message = "Failed to export deadlines" });
            }
        }

        // ===== Generate deadlines endpoint =====
        public class GenerateDeadlinesRequest
        {
            public int ClientId { get; set; }
            public int TaxYear { get; set; }
            public string ClientCategory { get; set; } = "Small"; // Large | Medium | Small | Micro
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateDeadlines([FromBody] GenerateDeadlinesRequest req)
        {
            try
            {
                if (req.ClientId <= 0 || req.TaxYear <= 0)
                    return BadRequest(new { success = false, message = "clientId and taxYear are required" });

                var category = Enum.TryParse<TaxpayerCategory>(req.ClientCategory, true, out var cat) ? cat : TaxpayerCategory.Small;

                // Determine applicable tax types
                var taxTypes = new List<TaxType> { TaxType.IncomeTax, TaxType.PayrollTax };
                if (category != TaxpayerCategory.Micro) taxTypes.Add(TaxType.GST);
                if (category == TaxpayerCategory.Large || category == TaxpayerCategory.Medium) taxTypes.Add(TaxType.ExciseDuty);

                var created = new List<object>();
                foreach (var tt in taxTypes.Distinct())
                {
                    var dates = tt == TaxType.IncomeTax ? GenerateQuarterlyDates(req.TaxYear) : GenerateMonthlyDates(req.TaxYear);
                    foreach (var date in dates)
                    {
                        if (date.Year != req.TaxYear) continue;
                        var exists = await _db.ComplianceDeadlines.AnyAsync(d => d.ClientId == req.ClientId && d.TaxType == tt && d.DueDate == date);
                        if (exists) continue;
                        var dl = new ComplianceDeadline
                        {
                            ClientId = req.ClientId,
                            TaxType = tt,
                            DueDate = date,
                            Status = FilingStatus.Draft,
                            Priority = DeadlinePriority.Medium,
                            Requirements = tt == TaxType.GST ? "Submit GST return and payment" : (tt == TaxType.PayrollTax ? "Submit payroll tax and PAYE" : "Submit tax filing and payment"),
                            EstimatedTaxLiability = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.ComplianceDeadlines.Add(dl);
                        await _db.SaveChangesAsync();

                        created.Add(new
                        {
                            id = dl.Id.ToString(),
                            title = $"{tt} deadline",
                            type = InferFrontendType(tt),
                            description = dl.Requirements,
                            dueDate = dl.DueDate,
                            status = "upcoming",
                            priority = dl.Priority == DeadlinePriority.High || dl.Priority == DeadlinePriority.Critical ? "high" : (dl.Priority == DeadlinePriority.Medium ? "medium" : "low"),
                            category = tt.ToString(),
                            clientId = req.ClientId,
                            reminderSet = false,
                            reminderDates = Array.Empty<DateTime>()
                        });
                    }
                }

                return Ok(new { success = true, data = created });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating deadlines for client {ClientId} year {Year}", req.ClientId, req.TaxYear);
                return StatusCode(500, new { success = false, message = "Failed to generate deadlines" });
            }
        }

}


}
