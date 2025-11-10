using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ComplianceAlertType = BettsTax.Data.ComplianceAlertType;

namespace BettsTax.Core.Services;

public class ComplianceAlertService : IComplianceAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComplianceAlertService> _logger;
    private readonly ISystemSettingService _settingService;

    public ComplianceAlertService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ComplianceAlertService> logger,
        ISystemSettingService settingService)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _settingService = settingService;
    }

    public async Task<List<ComplianceAlertDto>> GetActiveAlertsAsync(int? clientId = null)
    {
        try
        {
            var query = _context.ComplianceAlertsModels
                .Where(ca => !ca.IsResolved);

            if (clientId.HasValue)
            {
                query = query.Where(ca => ca.ClientId == clientId.Value);
            }

            var alerts = await query
                .Include(ca => ca.Client)
                .OrderByDescending(ca => ca.Severity)
                .ThenBy(ca => ca.CreatedAt)
                .Select(ca => new ComplianceAlertDto
                {
                    Id = ca.Id,
                    ClientId = ca.ClientId,
                    AlertType = ca.AlertType.ToString(),
                    AlertTypeName = ca.AlertType.ToString(),
                    Severity = ca.Severity,
                    SeverityName = ca.Severity.ToString(),
                    Title = ca.Title,
                    Message = ca.Message,
                    TaxType = ca.TaxType,
                    TaxTypeName = ca.TaxType.HasValue ? ca.TaxType.Value.ToString() : null,
                    CreatedAt = ca.CreatedAt,
                    DueDate = ca.DueDate,
                    PenaltyAmount = ca.PenaltyAmount,
                    IsResolved = ca.IsResolved,
                    ResolvedAt = ca.ResolvedAt,
                    ResolvedBy = ca.ResolvedBy,
                    Resolution = ca.Resolution
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {AlertCount} active alerts for client {ClientId}", 
                alerts.Count, clientId?.ToString() ?? "all");

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<ComplianceAlertDto> CreateAlertAsync(ComplianceAlertDto alert)
    {
        try
        {
            // Check for duplicate alerts to prevent spam
            var existingAlert = await _context.ComplianceAlertsModels
                .FirstOrDefaultAsync(ca => ca.ClientId == alert.ClientId &&
                                         ca.AlertType.ToString() == alert.AlertType &&
                                         ca.TaxType == alert.TaxType &&
                                         ca.DueDate == alert.DueDate &&
                                         !ca.IsResolved &&
                                         ca.CreatedAt >= DateTime.UtcNow.AddDays(-7)); // Within last 7 days

            if (existingAlert != null)
            {
                _logger.LogInformation("Duplicate alert prevented for client {ClientId}, alert type {AlertType}", 
                    alert.ClientId, alert.AlertType);
                return MapToDto(existingAlert);
            }

            var newAlert = new BettsTax.Data.Models.ComplianceAlert
            {
                ClientId = alert.ClientId,
                AlertType = Enum.Parse<ComplianceAlertType>(alert.AlertType),
                Severity = alert.Severity,
                Title = alert.Title,
                Message = alert.Message,
                TaxType = alert.TaxType,
                DueDate = alert.DueDate,
                PenaltyAmount = alert.PenaltyAmount,
                IsResolved = false
            };

            _context.ComplianceAlertsModels.Add(newAlert);
            await _context.SaveChangesAsync();

            alert.Id = newAlert.Id;
            alert.CreatedAt = newAlert.CreatedAt;

            // Send notification if severity is high enough
            if (alert.Severity >= ComplianceAlertSeverity.Warning)
            {
                await SendAlertNotification(alert);
            }

            _logger.LogInformation("Created compliance alert {AlertId} for client {ClientId}", newAlert.Id, alert.ClientId);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating compliance alert for client {ClientId}", alert.ClientId);
            throw;
        }
    }

    public async Task<bool> ResolveAlertAsync(int alertId, string resolution, string resolvedBy)
    {
        try
        {
            var alert = await _context.ComplianceAlertsModels.FindAsync(alertId);
            if (alert == null)
                return false;

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = resolvedBy;
            alert.Resolution = resolution;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Resolved compliance alert {AlertId} by {ResolvedBy}: {Resolution}", 
                alertId, resolvedBy, resolution);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving compliance alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<bool> DismissAlertAsync(int alertId, string reason, string dismissedBy)
    {
        try
        {
            var alert = await _context.ComplianceAlertsModels.FindAsync(alertId);
            if (alert == null)
                return false;

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = dismissedBy;
            alert.Resolution = $"Dismissed: {reason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Dismissed compliance alert {AlertId} by {DismissedBy}: {Reason}", 
                alertId, dismissedBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing compliance alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<List<ComplianceAlertDto>> GetAlertHistoryAsync(int clientId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var alerts = await _context.ComplianceAlertsModels
                .Where(ca => ca.ClientId == clientId &&
                           ca.CreatedAt >= fromDate &&
                           ca.CreatedAt <= toDate)
                .Include(ca => ca.Client)
                .Include(ca => ca.ResolvedByUser)
                .OrderByDescending(ca => ca.CreatedAt)
                .Select(ca => new ComplianceAlertDto
                {
                    Id = ca.Id,
                    ClientId = ca.ClientId,
                    AlertType = ca.AlertType.ToString(),
                    AlertTypeName = ca.AlertType.ToString(),
                    Severity = ca.Severity,
                    SeverityName = ca.Severity.ToString(),
                    Title = ca.Title,
                    Message = ca.Message,
                    TaxType = ca.TaxType,
                    TaxTypeName = ca.TaxType.HasValue ? ca.TaxType.Value.ToString() : null,
                    CreatedAt = ca.CreatedAt,
                    DueDate = ca.DueDate,
                    PenaltyAmount = ca.PenaltyAmount,
                    IsResolved = ca.IsResolved,
                    ResolvedAt = ca.ResolvedAt,
                    ResolvedBy = ca.ResolvedBy,
                    Resolution = ca.Resolution
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {AlertCount} alerts for client {ClientId} from {FromDate} to {ToDate}", 
                alerts.Count, clientId, fromDate, toDate);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert history for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<bool> EscalateAlertAsync(int alertId, ComplianceAlertSeverity newSeverity, string reason)
    {
        try
        {
            var alert = await _context.ComplianceAlertsModels.FindAsync(alertId);
            if (alert == null)
                return false;

            var oldSeverity = alert.Severity;
            alert.Severity = newSeverity;
            alert.Message += $"\n\nEscalated from {oldSeverity} to {newSeverity}: {reason}";

            await _context.SaveChangesAsync();

            // Send escalation notification
            var alertDto = MapToDto(alert);
            await SendAlertNotification(alertDto, isEscalation: true);

            _logger.LogInformation("Escalated compliance alert {AlertId} from {OldSeverity} to {NewSeverity}: {Reason}", 
                alertId, oldSeverity, newSeverity, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating compliance alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task ProcessAutomaticAlertsAsync()
    {
        try
        {
            _logger.LogInformation("Starting automatic alert processing");

            await ProcessUpcomingDeadlineAlerts();
            await ProcessOverdueAlerts();
            await ProcessPenaltyAlerts();
            await ProcessComplianceScoreAlerts();
            await ProcessGstThresholdAlerts();

            _logger.LogInformation("Completed automatic alert processing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing automatic alerts");
            throw;
        }
    }

    // Private helper methods
    private async Task ProcessUpcomingDeadlineAlerts()
    {
        var upcomingDeadlines = await _context.ComplianceDeadlines
            .Where(cd => cd.DueDate >= DateTime.UtcNow &&
                        cd.DueDate <= DateTime.UtcNow.AddDays(14) &&
                        cd.Status != FilingStatus.Filed)
            .Include(cd => cd.Client)
            .ToListAsync();

        foreach (var deadline in upcomingDeadlines)
        {
            var daysRemaining = (deadline.DueDate - DateTime.UtcNow).Days;
            var severity = daysRemaining switch
            {
                <= 3 => ComplianceAlertSeverity.Critical,
                <= 7 => ComplianceAlertSeverity.Critical,
                <= 14 => ComplianceAlertSeverity.Warning,
                _ => ComplianceAlertSeverity.Info
            };

            var alert = new ComplianceAlertDto
            {
                ClientId = deadline.ClientId,
                AlertType = ComplianceAlertType.UpcomingDeadline.ToString(),
                Severity = severity,
                Title = $"{deadline.TaxType} Filing Due Soon",
                Message = $"{deadline.TaxType} filing is due on {deadline.DueDate:MMM dd, yyyy} ({daysRemaining} days remaining). " +
                         $"Estimated liability: SLE {deadline.EstimatedTaxLiability:N2}",
                TaxType = deadline.TaxType,
                DueDate = deadline.DueDate
            };

            await CreateAlertAsync(alert);
        }
    }

    private async Task ProcessOverdueAlerts()
    {
        var overdueDeadlines = await _context.ComplianceDeadlines
            .Where(cd => cd.DueDate < DateTime.UtcNow &&
                        cd.Status != FilingStatus.Filed)
            .Include(cd => cd.Client)
            .ToListAsync();

        foreach (var deadline in overdueDeadlines)
        {
            var daysOverdue = (DateTime.UtcNow - deadline.DueDate).Days;
            var severity = daysOverdue switch
            {
                <= 30 => ComplianceAlertSeverity.Critical,
                _ => ComplianceAlertSeverity.Critical
            };

            var potentialPenalty = CalculatePotentialPenalty(deadline.TaxType, deadline.EstimatedTaxLiability, daysOverdue);

            var alert = new ComplianceAlertDto
            {
                ClientId = deadline.ClientId,
                AlertType = ComplianceAlertType.MissedDeadline.ToString(),
                Severity = severity,
                Title = $"{deadline.TaxType} Filing Overdue",
                Message = $"{deadline.TaxType} filing was due on {deadline.DueDate:MMM dd, yyyy} ({daysOverdue} days overdue). " +
                         $"Potential penalty: SLE {potentialPenalty:N2}",
                TaxType = deadline.TaxType,
                DueDate = deadline.DueDate,
                PenaltyAmount = potentialPenalty
            };

            await CreateAlertAsync(alert);
        }
    }

    private async Task ProcessPenaltyAlerts()
    {
        var recentPenalties = await _context.PenaltyCalculations
            .Where(pc => pc.CalculatedAt >= DateTime.UtcNow.AddDays(-7) &&
                        pc.TotalPenalty > 1000m) // Only significant penalties
            .Include(pc => pc.Client)
            .ToListAsync();

        foreach (var penalty in recentPenalties)
        {
            var severity = penalty.TotalPenalty switch
            {
                >= 50000m => ComplianceAlertSeverity.Critical,
                >= 10000m => ComplianceAlertSeverity.Critical,
                _ => ComplianceAlertSeverity.Warning
            };

            var alert = new ComplianceAlertDto
            {
                ClientId = penalty.ClientId,
                AlertType = ComplianceAlertType.PenaltyIncurred.ToString(),
                Severity = severity,
                Title = $"Penalty Incurred - {penalty.TaxType}",
                Message = $"A penalty of SLE {penalty.TotalPenalty:N2} has been calculated for {penalty.TaxType}. " +
                         $"Days late: {penalty.DaysLate}. {(penalty.IsWaivable ? "This penalty may be waivable." : "")}",
                TaxType = penalty.TaxType,
                DueDate = penalty.DueDate,
                PenaltyAmount = penalty.TotalPenalty
            };

            await CreateAlertAsync(alert);
        }
    }

    private async Task ProcessComplianceScoreAlerts()
    {
        var recentCalculations = await _context.ComplianceCalculations
            .Where(cc => cc.CalculationDate >= DateTime.UtcNow.AddDays(-7) &&
                        cc.OverallScore < 70m) // Poor compliance scores
            .Include(cc => cc.Client)
            .ToListAsync();

        foreach (var calculation in recentCalculations)
        {
            var severity = calculation.OverallScore switch
            {
                < 50m => ComplianceAlertSeverity.Critical,
                < 60m => ComplianceAlertSeverity.Critical,
                _ => ComplianceAlertSeverity.Warning
            };

            var alert = new ComplianceAlertDto
            {
                ClientId = calculation.ClientId,
                AlertType = ComplianceAlertType.ComplianceScoreDropped.ToString(),
                Severity = severity,
                Title = "Compliance Score Alert",
                Message = $"Compliance score has dropped to {calculation.OverallScore:F1}% ({calculation.Level}). " +
                         "Immediate attention required to improve compliance standing.",
                DueDate = DateTime.UtcNow.AddDays(30) // 30 days to improve
            };

            await CreateAlertAsync(alert);
        }
    }
    private async Task ProcessGstThresholdAlerts()
    {
        decimal threshold = 0m;

        try
        {
            var thresholdSetting = await _settingService.GetSettingAsync<decimal?>("Tax.GST.RegistrationThreshold");
            if (thresholdSetting.HasValue && thresholdSetting.Value > 0)
            {
                threshold = thresholdSetting.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read GST registration threshold setting. Falling back to 0.");
        }

        if (threshold <= 0)
        {
            _logger.LogDebug("GST threshold alerts skipped because threshold is not configured (> 0).");
            return;
        }

        var clients = await _context.Clients
            .Where(c => c.Status == ClientStatus.Active)
            .Select(c => new { c.Id, c.BusinessName, c.AnnualTurnover })
            .ToListAsync();

        foreach (var client in clients)
        {
            if (client.AnnualTurnover >= threshold)
            {
                var alert = new ComplianceAlertDto
                {
                    ClientId = client.Id,
                    AlertType = ComplianceAlertType.GstRegistration.ToString(),
                    Severity = ComplianceAlertSeverity.Warning,
                    Title = "GST Registration Threshold Exceeded",
                    Message = $"{client.BusinessName} reported annual turnover of SLE {client.AnnualTurnover:N2}, exceeding the GST registration threshold of SLE {threshold:N2}.",
                    DueDate = DateTime.UtcNow.AddDays(30)
                };

                await CreateAlertAsync(alert);
            }
        }
    }

    private decimal CalculatePotentialPenalty(TaxType taxType, decimal taxLiability, int daysOverdue)
    {
        // Simplified penalty calculation - this would integrate with the full penalty service
        var monthsLate = Math.Ceiling(daysOverdue / 30.0m);
        
        var penaltyRate = taxType switch
        {
            TaxType.IncomeTax => 0.05m, // 5% per month
            TaxType.GST => 0.02m, // 2% per month  
            TaxType.PayrollTax => 0.03m, // 3% per month
            TaxType.ExciseDuty => 0.04m, // 4% per month
            _ => 0.02m
        };

        if (taxType == TaxType.PayrollTax && taxLiability < 10000m)
            return 1000m; // Fixed penalty for small payroll

        return taxLiability * penaltyRate * monthsLate;
    }

    private async Task SendAlertNotification(ComplianceAlertDto alert, bool isEscalation = false)
    {
        try
        {
            var client = await _context.Clients.FindAsync(alert.ClientId);
            if (client == null) return;

            var subject = isEscalation ? $"ESCALATED: {alert.Title}" : alert.Title;
            var message = $"Dear {client.BusinessName},\n\n{alert.Message}\n\n" +
                         "Please take immediate action to address this compliance issue.\n\n" +
                         "Best regards,\nThe Betts Firm Compliance Team";

            // This would integrate with the notification service to send emails, SMS, etc.
            _logger.LogInformation("Notification sent for alert {AlertId} to client {ClientId}", alert.Id, alert.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending alert notification for alert {AlertId}", alert.Id);
        }
    }

    private static ComplianceAlertDto MapToDto(BettsTax.Data.Models.ComplianceAlert alert)
    {
        return new ComplianceAlertDto
        {
            Id = alert.Id,
            ClientId = alert.ClientId,
            AlertType = alert.AlertType.ToString(),
            AlertTypeName = alert.AlertType.ToString(),
            Severity = alert.Severity,
            SeverityName = alert.Severity.ToString(),
            Title = alert.Title,
            Message = alert.Message,
            TaxType = alert.TaxType,
            TaxTypeName = alert.TaxType?.ToString(),
            CreatedAt = alert.CreatedAt,
            DueDate = alert.DueDate,
            PenaltyAmount = alert.PenaltyAmount,
            IsResolved = alert.IsResolved,
            ResolvedAt = alert.ResolvedAt,
            ResolvedBy = alert.ResolvedBy,
            Resolution = alert.Resolution
        };
    }
}