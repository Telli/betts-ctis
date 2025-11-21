using BettsTax.Data;
using BettsTax.Shared;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Compliance Monitoring Workflow Service - Manages compliance tracking and deadline monitoring
    /// </summary>
    public class ComplianceMonitoringWorkflow : IComplianceMonitoringWorkflow
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ComplianceMonitoringWorkflow> _logger;

        // Penalty rates based on Finance Act 2025
        private const decimal LATE_FILING_PENALTY_RATE = 0.05m; // 5% per month
        private const decimal LATE_PAYMENT_PENALTY_RATE = 0.02m; // 2% per month
        private const decimal UNDERPAYMENT_PENALTY_RATE = 0.10m; // 10% of underpaid amount

        public ComplianceMonitoringWorkflow(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService,
            ILogger<ComplianceMonitoringWorkflow> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result> MonitorDeadlinesAsync()
        {
            try
            {
                _logger.LogInformation("Starting compliance deadline monitoring");

                var today = DateTime.UtcNow.Date;
                var monitoringItems = await _context.ComplianceMonitoringWorkflows
                    .Where(c => c.Status == ComplianceMonitoringStatus.Pending)
                    .Include(c => c.Client)
                    .ToListAsync();

                foreach (var item in monitoringItems)
                {
                    var daysUntilDue = (item.DueDate.Date - today).Days;

                    // Check for overdue
                    if (daysUntilDue < 0)
                    {
                        item.IsOverdue = true;
                        item.DaysOverdue = Math.Abs(daysUntilDue);
                        item.Status = ComplianceMonitoringStatus.Overdue;

                        if (!item.AlertSentOverdue)
                        {
                            await GenerateComplianceAlertAsync(item.Id, "Overdue");
                            item.AlertSentOverdue = true;
                        }
                    }
                    // Check for 30-day warning
                    else if (daysUntilDue == 30 && !item.AlertSent30Days)
                    {
                        await GenerateComplianceAlertAsync(item.Id, "30DayWarning");
                        item.AlertSent30Days = true;
                    }
                    // Check for 14-day warning
                    else if (daysUntilDue == 14 && !item.AlertSent14Days)
                    {
                        await GenerateComplianceAlertAsync(item.Id, "14DayWarning");
                        item.AlertSent14Days = true;
                    }
                    // Check for 10-day warning (Phase 2 requirement)
                    else if (daysUntilDue == 10 && !item.AlertSent10Days)
                    {
                        await GenerateComplianceAlertAsync(item.Id, "10DayWarning");
                        item.AlertSent10Days = true;
                    }
                    // Check for 7-day warning
                    else if (daysUntilDue == 7 && !item.AlertSent7Days)
                    {
                        await GenerateComplianceAlertAsync(item.Id, "7DayWarning");
                        item.AlertSent7Days = true;
                    }
                    // Daily reminders for last 5 days before deadline
                    else if (daysUntilDue >= 1 && daysUntilDue <= 5)
                    {
                        var lastReminderDate = item.LastDailyReminderSent?.Date;
                        var shouldSendReminder = lastReminderDate == null || lastReminderDate < today;
                        
                        if (shouldSendReminder)
                        {
                            await GenerateComplianceAlertAsync(item.Id, $"DailyReminder{daysUntilDue}Day");
                            item.LastDailyReminderSent = DateTime.UtcNow;
                        }
                    }
                    // Check for 1-day warning (if not already sent via daily reminder)
                    else if (daysUntilDue == 1 && !item.AlertSent1Day)
                    {
                        await GenerateComplianceAlertAsync(item.Id, "1DayWarning");
                        item.AlertSent1Day = true;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Compliance deadline monitoring completed");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring compliance deadlines");
                return Result.Failure($"Error monitoring compliance deadlines: {ex.Message}");
            }
        }

        public async Task<Result> UpdateComplianceStatusAsync(Guid complianceMonitoringId, string status)
        {
            try
            {
                var monitoring = await _context.ComplianceMonitoringWorkflows.FindAsync(complianceMonitoringId);
                if (monitoring == null)
                    return Result.Failure("Compliance monitoring item not found");

                if (Enum.TryParse<ComplianceMonitoringStatus>(status, out var newStatus))
                {
                    monitoring.Status = newStatus;
                    monitoring.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Result.Success();
                }

                return Result.Failure("Invalid status");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating compliance status");
                return Result.Failure($"Error updating compliance status: {ex.Message}");
            }
        }

        public async Task<Result<ComplianceMonitoringAlertDto>> GenerateComplianceAlertAsync(
            Guid complianceMonitoringId, string alertType)
        {
            try
            {
                var monitoring = await _context.ComplianceMonitoringWorkflows
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.Id == complianceMonitoringId);

                if (monitoring == null)
                    return Result.Failure<ComplianceMonitoringAlertDto>("Compliance monitoring item not found");

                var alertMessages = new Dictionary<string, string>
                {
                    { "30DayWarning", $"Your {monitoring.TaxType} filing is due in 30 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "14DayWarning", $"Your {monitoring.TaxType} filing is due in 14 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "10DayWarning", $"Your {monitoring.TaxType} filing is due in 10 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "7DayWarning", $"Your {monitoring.TaxType} filing is due in 7 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "DailyReminder5Day", $"Daily Reminder: Your {monitoring.TaxType} filing is due in 5 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "DailyReminder4Day", $"Daily Reminder: Your {monitoring.TaxType} filing is due in 4 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "DailyReminder3Day", $"Daily Reminder: Your {monitoring.TaxType} filing is due in 3 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "DailyReminder2Day", $"Daily Reminder: Your {monitoring.TaxType} filing is due in 2 days (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "DailyReminder1Day", $"Daily Reminder: Your {monitoring.TaxType} filing is due tomorrow (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "1DayWarning", $"Your {monitoring.TaxType} filing is due tomorrow (Due: {monitoring.DueDate:yyyy-MM-dd})" },
                    { "Overdue", $"Your {monitoring.TaxType} filing is overdue (Due: {monitoring.DueDate:yyyy-MM-dd})" }
                };

                var message = alertMessages.ContainsKey(alertType) ? alertMessages[alertType] : "Compliance alert";

                var alert = new ComplianceMonitoringAlert
                {
                    Id = Guid.NewGuid(),
                    ComplianceMonitoringWorkflowId = complianceMonitoringId,
                    AlertType = alertType,
                    Status = ComplianceAlertStatus.Sent,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    SentTo = monitoring.Client?.Email
                };

                _context.ComplianceMonitoringAlerts.Add(alert);
                await _context.SaveChangesAsync();

                // Send notification
                if (!string.IsNullOrEmpty(monitoring.Client?.Email))
                {
                    await _notificationService.SendNotificationAsync(
                        monitoring.Client.UserId ?? string.Empty,
                        $"Compliance Alert: {alertType}",
                        "Email");
                }

                return Result.Success(MapAlertToDto(alert));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance alert");
                return Result.Failure<ComplianceMonitoringAlertDto>($"Error generating compliance alert: {ex.Message}");
            }
        }

        public async Task<Result<decimal>> CalculatePenaltyAsync(Guid complianceMonitoringId, int daysOverdue)
        {
            try
            {
                var monitoring = await _context.ComplianceMonitoringWorkflows.FindAsync(complianceMonitoringId);
                if (monitoring == null)
                    return Result.Failure<decimal>("Compliance monitoring item not found");

                // Calculate penalty based on days overdue
                var months = Math.Ceiling((decimal)daysOverdue / 30);
                var penalty = monitoring.Amount * LATE_FILING_PENALTY_RATE * months;

                // Create penalty calculation record
                var calculation = new CompliancePenaltyCalculation
                {
                    Id = Guid.NewGuid(),
                    ComplianceMonitoringWorkflowId = complianceMonitoringId,
                    PenaltyType = "LateFiling",
                    BaseAmount = monitoring.Amount,
                    PenaltyRate = LATE_FILING_PENALTY_RATE,
                    CalculatedPenalty = penalty,
                    DaysOverdue = daysOverdue,
                    CalculationBasis = $"Late filing penalty: {LATE_FILING_PENALTY_RATE * 100}% per month for {months} months"
                };

                _context.CompliancePenaltyCalculations.Add(calculation);
                monitoring.EstimatedPenalty = penalty;
                await _context.SaveChangesAsync();

                return Result.Success(penalty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating penalty");
                return Result.Failure<decimal>($"Error calculating penalty: {ex.Message}");
            }
        }

        public async Task<Result<List<ComplianceMonitoringDto>>> GetClientComplianceAsync(int clientId)
        {
            try
            {
                var items = await _context.ComplianceMonitoringWorkflows
                    .Where(c => c.ClientId == clientId)
                    .OrderByDescending(c => c.DueDate)
                    .ToListAsync();

                return Result.Success(items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client compliance");
                return Result.Failure<List<ComplianceMonitoringDto>>($"Error getting client compliance: {ex.Message}");
            }
        }

        public async Task<Result<List<ComplianceMonitoringDto>>> GetTaxYearComplianceAsync(int taxYearId)
        {
            try
            {
                var items = await _context.ComplianceMonitoringWorkflows
                    .Where(c => c.TaxYearId == taxYearId)
                    .OrderByDescending(c => c.DueDate)
                    .ToListAsync();

                return Result.Success(items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tax year compliance");
                return Result.Failure<List<ComplianceMonitoringDto>>($"Error getting tax year compliance: {ex.Message}");
            }
        }

        public async Task<Result<List<ComplianceMonitoringDto>>> GetPendingComplianceAsync()
        {
            try
            {
                var items = await _context.ComplianceMonitoringWorkflows
                    .Where(c => c.Status == ComplianceMonitoringStatus.Pending)
                    .OrderBy(c => c.DueDate)
                    .ToListAsync();

                return Result.Success(items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending compliance");
                return Result.Failure<List<ComplianceMonitoringDto>>($"Error getting pending compliance: {ex.Message}");
            }
        }

        public async Task<Result<List<ComplianceMonitoringDto>>> GetOverdueComplianceAsync()
        {
            try
            {
                var items = await _context.ComplianceMonitoringWorkflows
                    .Where(c => c.IsOverdue)
                    .OrderBy(c => c.DueDate)
                    .ToListAsync();

                return Result.Success(items.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue compliance");
                return Result.Failure<List<ComplianceMonitoringDto>>($"Error getting overdue compliance: {ex.Message}");
            }
        }

        public async Task<Result<ComplianceMonitoringDto>> CreateComplianceMonitoringAsync(
            int clientId, int taxYearId, string taxType, DateTime dueDate, decimal amount)
        {
            try
            {
                var monitoring = new Data.ComplianceMonitoringWorkflow
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    TaxYearId = taxYearId,
                    TaxType = taxType,
                    DueDate = dueDate,
                    Amount = amount,
                    Status = ComplianceMonitoringStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ComplianceMonitoringWorkflows.Add(monitoring);
                await _context.SaveChangesAsync();

                return Result.Success(MapToDto(monitoring));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compliance monitoring");
                return Result.Failure<ComplianceMonitoringDto>($"Error creating compliance monitoring: {ex.Message}");
            }
        }

        public async Task<Result> MarkAsFiledAsync(Guid complianceMonitoringId, DateTime filedDate)
        {
            try
            {
                var monitoring = await _context.ComplianceMonitoringWorkflows.FindAsync(complianceMonitoringId);
                if (monitoring == null)
                    return Result.Failure("Compliance monitoring item not found");

                monitoring.FiledDate = filedDate;
                monitoring.Status = ComplianceMonitoringStatus.Filed;
                monitoring.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking as filed");
                return Result.Failure($"Error marking as filed: {ex.Message}");
            }
        }

        public async Task<Result> MarkAsPaidAsync(Guid complianceMonitoringId, DateTime paidDate)
        {
            try
            {
                var monitoring = await _context.ComplianceMonitoringWorkflows.FindAsync(complianceMonitoringId);
                if (monitoring == null)
                    return Result.Failure("Compliance monitoring item not found");

                monitoring.PaidDate = paidDate;
                monitoring.Status = ComplianceMonitoringStatus.Paid;
                monitoring.IsOverdue = false;
                monitoring.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking as paid");
                return Result.Failure($"Error marking as paid: {ex.Message}");
            }
        }

        public async Task<Result<ComplianceStatisticsDto>> GetComplianceStatisticsAsync(
            int? clientId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.ComplianceMonitoringWorkflows.AsQueryable();

                if (clientId.HasValue)
                    query = query.Where(c => c.ClientId == clientId.Value);

                if (from.HasValue)
                    query = query.Where(c => c.CreatedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(c => c.CreatedAt <= to.Value);

                var items = await query.ToListAsync();

                var stats = new ComplianceStatisticsDto
                {
                    TotalItems = items.Count,
                    FiledCount = items.Count(c => c.Status == ComplianceMonitoringStatus.Filed),
                    PaidCount = items.Count(c => c.Status == ComplianceMonitoringStatus.Paid),
                    OverdueCount = items.Count(c => c.IsOverdue),
                    PendingCount = items.Count(c => c.Status == ComplianceMonitoringStatus.Pending),
                    TotalPenalties = items.Sum(c => c.EstimatedPenalty ?? 0),
                    AverageDaysOverdue = items.Where(c => c.IsOverdue).Any() ? (int)items.Where(c => c.IsOverdue).Average(c => c.DaysOverdue) : 0
                };

                if (stats.TotalItems > 0)
                    stats.ComplianceRate = (decimal)(stats.FiledCount + stats.PaidCount) / stats.TotalItems * 100;

                return Result.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance statistics");
                return Result.Failure<ComplianceStatisticsDto>($"Error getting compliance statistics: {ex.Message}");
            }
        }

        public async Task<Result<List<ComplianceMonitoringAlertDto>>> GetAlertsAsync(Guid complianceMonitoringId)
        {
            try
            {
                var alerts = await _context.ComplianceMonitoringAlerts
                    .Where(a => a.ComplianceMonitoringWorkflowId == complianceMonitoringId)
                    .OrderByDescending(a => a.SentAt)
                    .ToListAsync();

                return Result.Success(alerts.Select(MapAlertToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alerts");
                return Result.Failure<List<ComplianceMonitoringAlertDto>>($"Error getting alerts: {ex.Message}");
            }
        }

        public async Task<Result<List<CompliancePenaltyCalculationDto>>> GetPenaltyCalculationsAsync(Guid complianceMonitoringId)
        {
            try
            {
                var penalties = await _context.CompliancePenaltyCalculations
                    .Where(p => p.ComplianceMonitoringWorkflowId == complianceMonitoringId)
                    .OrderByDescending(p => p.CalculatedAt)
                    .ToListAsync();

                return Result.Success(penalties.Select(MapPenaltyToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting penalty calculations");
                return Result.Failure<List<CompliancePenaltyCalculationDto>>($"Error getting penalty calculations: {ex.Message}");
            }
        }

        // Helper methods
        private ComplianceMonitoringDto MapToDto(Data.ComplianceMonitoringWorkflow monitoring)
        {
            return new ComplianceMonitoringDto
            {
                Id = monitoring.Id,
                ClientId = monitoring.ClientId,
                TaxYearId = monitoring.TaxYearId,
                TaxType = monitoring.TaxType,
                Status = monitoring.Status.ToString(),
                DueDate = monitoring.DueDate,
                FiledDate = monitoring.FiledDate,
                PaidDate = monitoring.PaidDate,
                Amount = monitoring.Amount,
                EstimatedPenalty = monitoring.EstimatedPenalty,
                IsOverdue = monitoring.IsOverdue,
                DaysOverdue = monitoring.DaysOverdue,
                Notes = monitoring.Notes,
                CreatedAt = monitoring.CreatedAt
            };
        }

        private ComplianceMonitoringAlertDto MapAlertToDto(ComplianceMonitoringAlert alert)
        {
            return new ComplianceMonitoringAlertDto
            {
                Id = alert.Id,
                AlertType = alert.AlertType,
                Status = alert.Status.ToString(),
                Message = alert.Message,
                SentAt = alert.SentAt,
                SentTo = alert.SentTo
            };
        }

        private CompliancePenaltyCalculationDto MapPenaltyToDto(CompliancePenaltyCalculation penalty)
        {
            return new CompliancePenaltyCalculationDto
            {
                Id = penalty.Id,
                PenaltyType = penalty.PenaltyType,
                BaseAmount = penalty.BaseAmount,
                PenaltyRate = penalty.PenaltyRate,
                CalculatedPenalty = penalty.CalculatedPenalty,
                DaysOverdue = penalty.DaysOverdue,
                CalculationBasis = penalty.CalculationBasis
            };
        }
    }
}

