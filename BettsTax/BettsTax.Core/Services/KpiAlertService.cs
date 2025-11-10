using BettsTax.Core.DTOs;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services;

public class KpiAlertService : IKpiAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KpiAlertService> _logger;
    private readonly INotificationService? _notificationService;

    public KpiAlertService(
        ApplicationDbContext context,
        ILogger<KpiAlertService> logger,
        INotificationService? notificationService = null)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ProcessAlertsAsync(List<KpiAlert> alerts, CancellationToken ct = default)
    {
        if (!alerts.Any())
            return;

        _logger.LogInformation("Processing {AlertCount} KPI alerts", alerts.Count);

        // Send notifications
        await SendAlertNotificationsAsync(alerts, ct);

        // Mark notifications as sent
        foreach (var alert in alerts)
        {
            alert.NotificationSent = true;
            alert.NotificationSentAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Processed {AlertCount} KPI alerts successfully", alerts.Count);
    }

    public async Task<List<KpiAlertDto>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        var alerts = await _context.KpiAlerts
            .Include(a => a.Client)
            .Include(a => a.KpiSnapshot)
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return alerts.Select(a => new KpiAlertDto
        {
            AlertId = a.Id,
            ClientId = a.ClientId,
            ClientName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}" : null,
            AlertType = a.AlertType,
            Message = a.Message,
            Severity = a.Severity.ToString(),
            ThresholdValue = a.ThresholdValue,
            ActualValue = a.ActualValue,
            IsResolved = a.IsResolved,
            CreatedAt = a.CreatedAt,
            ResolvedAt = a.ResolvedAt,
            ResolvedBy = a.ResolvedBy
        }).ToList();
    }

    public async Task ResolveAlertAsync(int alertId, string resolvedBy, string? notes = null, CancellationToken ct = default)
    {
        var alert = await _context.KpiAlerts.FindAsync(new object[] { alertId }, ct);
        if (alert == null)
        {
            _logger.LogWarning("Alert with ID {AlertId} not found", alertId);
            return;
        }

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.ResolvedBy = resolvedBy;
        alert.ResolutionNotes = notes;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);
    }

    public async Task SendAlertNotificationsAsync(List<KpiAlert> alerts, CancellationToken ct = default)
    {
        if (!alerts.Any())
            return;

        // Group alerts by severity for better notification formatting
        var criticalAlerts = alerts.Where(a => a.Severity == AlertSeverity.Critical).ToList();
        var highAlerts = alerts.Where(a => a.Severity == AlertSeverity.High).ToList();
        var mediumAlerts = alerts.Where(a => a.Severity == AlertSeverity.Medium).ToList();
        var lowAlerts = alerts.Where(a => a.Severity == AlertSeverity.Low).ToList();

        var notificationMessage = BuildNotificationMessage(criticalAlerts, highAlerts, mediumAlerts, lowAlerts);

        if (_notificationService != null)
        {
            try
            {
                // Get admin users to notify
                var adminUsers = await _context.Users
                    .Where(u => u.Role == "Admin" || u.Role == "Manager")
                    .ToListAsync(ct);

                foreach (var admin in adminUsers)
                {
                    await _notificationService.SendNotificationAsync(
                        admin.Id,
                        notificationMessage,
                        "General");
                }

                _logger.LogInformation("KPI alert notifications sent to {AdminCount} administrators", adminUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send KPI alert notifications");
            }
        }
        else
        {
            _logger.LogWarning("No notification service available for KPI alerts");
        }

        // Log alerts for monitoring
        foreach (var alert in alerts)
        {
            _logger.LogWarning("KPI Alert [{Severity}]: {Message} (Threshold: {Threshold}, Actual: {Actual})",
                alert.Severity, alert.Message, alert.ThresholdValue, alert.ActualValue);
        }
    }

    private static string BuildNotificationMessage(
        List<KpiAlert> criticalAlerts,
        List<KpiAlert> highAlerts,
        List<KpiAlert> mediumAlerts,
        List<KpiAlert> lowAlerts)
    {
        var message = "KPI Alert Summary:\n\n";

        if (criticalAlerts.Any())
        {
            message += "🔴 CRITICAL ALERTS:\n";
            foreach (var alert in criticalAlerts)
            {
                message += $"• {alert.Message}\n";
            }
            message += "\n";
        }

        if (highAlerts.Any())
        {
            message += "🟠 HIGH PRIORITY ALERTS:\n";
            foreach (var alert in highAlerts)
            {
                message += $"• {alert.Message}\n";
            }
            message += "\n";
        }

        if (mediumAlerts.Any())
        {
            message += "🟡 MEDIUM PRIORITY ALERTS:\n";
            foreach (var alert in mediumAlerts)
            {
                message += $"• {alert.Message}\n";
            }
            message += "\n";
        }

        if (lowAlerts.Any())
        {
            message += "🟢 LOW PRIORITY ALERTS:\n";
            foreach (var alert in lowAlerts)
            {
                message += $"• {alert.Message}\n";
            }
            message += "\n";
        }

        message += "Please review the KPI dashboard for detailed metrics and take appropriate action.";
        return message;
    }
}
