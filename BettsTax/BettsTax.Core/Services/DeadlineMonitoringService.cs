using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelsComplianceAlert = BettsTax.Data.Models.ComplianceAlert;
using ComplianceAlertType = BettsTax.Data.ComplianceAlertType;
using DeadlinePriority = BettsTax.Data.DeadlinePriority;
using ComplianceRiskLevel = BettsTax.Data.ComplianceRiskLevel;

namespace BettsTax.Core.Services;

public class DeadlineMonitoringService : IDeadlineMonitoringService
{
    private readonly ApplicationDbContext _context;
    private readonly IComplianceAlertService _alertService;
    private readonly ILogger<DeadlineMonitoringService> _logger;

    // Sierra Leone tax calendar - standard due dates
    private readonly Dictionary<TaxType, List<DateTime>> _standardDueDates = new()
    {
        [TaxType.IncomeTax] = GenerateQuarterlyDates(2025),
        [TaxType.GST] = GenerateMonthlyDates(2025),
        [TaxType.PayrollTax] = GenerateMonthlyDates(2025),
        [TaxType.PayrollTax] = GenerateMonthlyDates(2025),
        [TaxType.ExciseDuty] = GenerateMonthlyDates(2025)
    };

    public DeadlineMonitoringService(
        ApplicationDbContext context,
        IComplianceAlertService alertService,
        ILogger<DeadlineMonitoringService> logger)
    {
        _context = context;
        _alertService = alertService;
        _logger = logger;
    }

    public async Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int daysAhead = 30)
    {
        try
        {
            var endDate = DateTime.UtcNow.AddDays(daysAhead);

            var query = _context.ComplianceDeadlines
                .Where(cd => cd.DueDate >= DateTime.UtcNow && cd.DueDate <= endDate);

            if (clientId.HasValue)
            {
                query = query.Where(cd => cd.ClientId == clientId.Value);
            }

            var deadlines = await query
                .Include(cd => cd.Client)
                .OrderBy(cd => cd.DueDate)
                .ThenBy(cd => cd.Priority)
                .Select(cd => new UpcomingDeadlineDto
                {
                    Id = cd.Id,
                    TaxType = cd.TaxType,
                    TaxTypeName = cd.TaxType.ToString(),
                    DueDate = cd.DueDate,
                    DaysRemaining = (cd.DueDate.Date - DateTime.UtcNow.Date).Days,
                    Priority = (ComplianceRiskLevel)cd.Priority,
                    PriorityName = cd.Priority.ToString(),
                    Status = cd.Status,
                    StatusName = cd.Status.ToString(),
                    EstimatedTaxLiability = cd.EstimatedTaxLiability,
                    DocumentsReady = cd.DocumentsReady,
                    IsOverdue = cd.DueDate < DateTime.UtcNow,
                    PotentialPenalty = CalculatePotentialPenalty(cd.TaxType, cd.EstimatedTaxLiability, cd.DueDate),
                    Requirements = cd.Requirements
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {DeadlineCount} upcoming deadlines for client {ClientId}", 
                deadlines.Count, clientId?.ToString() ?? "all");

            return deadlines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming deadlines for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<List<UpcomingDeadlineDto>> GetOverdueItemsAsync(int? clientId = null)
    {
        try
        {
            var query = _context.ComplianceDeadlines
                .Where(cd => cd.DueDate < DateTime.UtcNow && cd.Status != FilingStatus.Filed);

            if (clientId.HasValue)
            {
                query = query.Where(cd => cd.ClientId == clientId.Value);
            }

            var overdueItems = await query
                .Include(cd => cd.Client)
                .OrderBy(cd => cd.DueDate)
                .ThenByDescending(cd => cd.Priority)
                .Select(cd => new UpcomingDeadlineDto
                {
                    Id = cd.Id,
                    TaxType = cd.TaxType,
                    TaxTypeName = cd.TaxType.ToString(),
                    DueDate = cd.DueDate,
                    DaysRemaining = (cd.DueDate.Date - DateTime.UtcNow.Date).Days, // Will be negative
                    Priority = (ComplianceRiskLevel)cd.Priority,
                    PriorityName = cd.Priority.ToString(),
                    Status = cd.Status,
                    StatusName = cd.Status.ToString(),
                    EstimatedTaxLiability = cd.EstimatedTaxLiability,
                    DocumentsReady = cd.DocumentsReady,
                    IsOverdue = true,
                    PotentialPenalty = CalculatePotentialPenalty(cd.TaxType, cd.EstimatedTaxLiability, cd.DueDate),
                    Requirements = cd.Requirements
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {OverdueCount} overdue items for client {ClientId}", 
                overdueItems.Count, clientId?.ToString() ?? "all");

            return overdueItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue items for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<bool> CreateDeadlineAlertAsync(int clientId, TaxType taxType, DateTime dueDate)
    {
        try
        {
            // Check if deadline already exists
            var existingDeadline = await _context.ComplianceDeadlines
                .FirstOrDefaultAsync(cd => cd.ClientId == clientId && 
                                         cd.TaxType == taxType && 
                                         cd.DueDate.Date == dueDate.Date);

            if (existingDeadline != null)
            {
                _logger.LogInformation("Deadline already exists for client {ClientId}, tax type {TaxType}, due date {DueDate}", 
                    clientId, taxType, dueDate);
                return true;
            }

            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
            {
                _logger.LogWarning("Client {ClientId} not found when creating deadline alert", clientId);
                return false;
            }

            // Create new deadline
            var deadline = new ComplianceDeadline
            {
                ClientId = clientId,
                TaxType = taxType,
                DueDate = dueDate,
                Status = FilingStatus.Draft,
                EstimatedTaxLiability = EstimateTaxLiability(client, taxType),
                DocumentsReady = false,
                Priority = DeterminePriority(taxType, dueDate),
                Requirements = GetRequirements(taxType)
            };

            _context.ComplianceDeadlines.Add(deadline);
            await _context.SaveChangesAsync();

            // Create compliance alert
            await CreateAutomaticAlert(clientId, taxType, dueDate, deadline.Priority);

            _logger.LogInformation("Created deadline alert for client {ClientId}, tax type {TaxType}, due date {DueDate}", 
                clientId, taxType, dueDate);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deadline alert for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<bool> UpdateDeadlineStatusAsync(int deadlineId, FilingStatus status)
    {
        try
        {
            var deadline = await _context.ComplianceDeadlines.FindAsync(deadlineId);
            if (deadline == null)
                return false;

            deadline.Status = status;
            deadline.UpdatedAt = DateTime.UtcNow;

            if (status == FilingStatus.Filed)
            {
                deadline.CompletedAt = DateTime.UtcNow;
                deadline.CompletedBy = "System"; // This should come from current user context
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated deadline {DeadlineId} status to {Status}", deadlineId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deadline status for deadline {DeadlineId}", deadlineId);
            throw;
        }
    }

    public async Task<List<UpcomingDeadlineDto>> GetDeadlinesByPriorityAsync(int clientId, DeadlinePriority priority)
    {
        try
        {
            var deadlines = await _context.ComplianceDeadlines
                .Where(cd => cd.ClientId == clientId && cd.Priority == priority)
                .Include(cd => cd.Client)
                .OrderBy(cd => cd.DueDate)
                .Select(cd => new UpcomingDeadlineDto
                {
                    Id = cd.Id,
                    TaxType = cd.TaxType,
                    TaxTypeName = cd.TaxType.ToString(),
                    DueDate = cd.DueDate,
                    DaysRemaining = (cd.DueDate.Date - DateTime.UtcNow.Date).Days,
                    Priority = (ComplianceRiskLevel)cd.Priority,
                    PriorityName = cd.Priority.ToString(),
                    Status = cd.Status,
                    StatusName = cd.Status.ToString(),
                    EstimatedTaxLiability = cd.EstimatedTaxLiability,
                    DocumentsReady = cd.DocumentsReady,
                    IsOverdue = cd.DueDate < DateTime.UtcNow,
                    PotentialPenalty = CalculatePotentialPenalty(cd.TaxType, cd.EstimatedTaxLiability, cd.DueDate),
                    Requirements = cd.Requirements
                })
                .ToListAsync();

            return deadlines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlines by priority for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<bool> MarkDeadlineCompletedAsync(int deadlineId, DateTime completionDate)
    {
        try
        {
            var deadline = await _context.ComplianceDeadlines.FindAsync(deadlineId);
            if (deadline == null)
                return false;

            deadline.Status = FilingStatus.Filed;
            deadline.CompletedAt = completionDate;
            deadline.CompletedBy = "System"; // This should come from current user context
            deadline.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Resolve any related alerts
            var relatedAlerts = await _context.ComplianceAlertsModels
                .Where(ca => ca.ClientId == deadline.ClientId && 
                           ca.TaxType == deadline.TaxType && 
                           ca.DueDate == deadline.DueDate &&
                           !ca.IsResolved)
                .ToListAsync();

            foreach (var alert in relatedAlerts)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.Resolution = "Filing completed on time";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked deadline {DeadlineId} as completed on {CompletionDate}", 
                deadlineId, completionDate);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking deadline completed for deadline {DeadlineId}", deadlineId);
            throw;
        }
    }

    public async Task ScheduleAutomaticDeadlineAlertsAsync()
    {
        try
        {
            _logger.LogInformation("Starting automatic deadline alert scheduling");

            var activeClients = await _context.Clients
                .Where(c => c.Status == ClientStatus.Active)
                .ToListAsync();

            var totalAlertsCreated = 0;

            foreach (var client in activeClients)
            {
                // Get applicable tax types for this client
                var taxTypes = GetApplicableTaxTypes(client);

                foreach (var taxType in taxTypes)
                {
                    var upcomingDueDates = GetUpcomingDueDates(taxType, 90); // Next 90 days

                    foreach (var dueDate in upcomingDueDates)
                    {
                        var created = await CreateDeadlineAlertAsync(client.ClientId, taxType, dueDate);
                        if (created)
                            totalAlertsCreated++;
                    }
                }
            }

            _logger.LogInformation("Completed automatic deadline alert scheduling. Created {AlertCount} new alerts for {ClientCount} clients", 
                totalAlertsCreated, activeClients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling automatic deadline alerts");
            throw;
        }
    }

    // Private helper methods
    private static List<DateTime> GenerateMonthlyDates(int year)
    {
        var dates = new List<DateTime>();
        for (int month = 1; month <= 12; month++)
        {
            // Sierra Leone GST and payroll tax typically due on 21st of following month
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;
            dates.Add(new DateTime(nextYear, nextMonth, 21));
        }
        return dates;
    }

    private static List<DateTime> GenerateQuarterlyDates(int year)
    {
        return new List<DateTime>
        {
            new DateTime(year, 4, 30), // Q1 due April 30
            new DateTime(year, 7, 31), // Q2 due July 31
            new DateTime(year, 10, 31), // Q3 due October 31
            new DateTime(year + 1, 1, 31) // Q4 due January 31 next year
        };
    }

    private decimal CalculatePotentialPenalty(TaxType taxType, decimal taxLiability, DateTime dueDate)
    {
        if (dueDate >= DateTime.UtcNow) return 0m;

        var daysLate = (DateTime.UtcNow - dueDate).Days;
        
        // Simplified penalty calculation - this would integrate with the full penalty service
        var penaltyRate = taxType switch
        {
            TaxType.IncomeTax => 0.05m, // 5%
            TaxType.GST => 0.10m, // 10%
            TaxType.PayrollTax => 1000m, // Fixed SLE 1,000
            TaxType.ExciseDuty => 0.04m, // 4%
            _ => 0.02m
        };

        if (taxType == TaxType.PayrollTax)
            return penaltyRate; // Fixed amount

        return taxLiability * penaltyRate;
    }

    private decimal EstimateTaxLiability(Client client, TaxType taxType)
    {
        // This would typically look at historical data, client category, etc.
        return client.TaxpayerCategory switch
        {
            TaxpayerCategory.Large => taxType switch
            {
                TaxType.IncomeTax => 500000m,
                TaxType.GST => 200000m,
                TaxType.PayrollTax => 100000m,
                _ => 50000m
            },
            TaxpayerCategory.Medium => taxType switch
            {
                TaxType.IncomeTax => 100000m,
                TaxType.GST => 50000m,
                TaxType.PayrollTax => 25000m,
                _ => 10000m
            },
            TaxpayerCategory.Small => taxType switch
            {
                TaxType.IncomeTax => 25000m,
                TaxType.GST => 10000m,
                TaxType.PayrollTax => 5000m,
                _ => 2500m
            },
            _ => 5000m
        };
    }

    private static DeadlinePriority DeterminePriority(TaxType taxType, DateTime dueDate)
    {
        var daysUntilDue = (dueDate - DateTime.UtcNow).Days;

        return daysUntilDue switch
        {
            <= 7 => DeadlinePriority.Critical,
            <= 14 => DeadlinePriority.High,
            <= 30 => DeadlinePriority.Medium,
            _ => DeadlinePriority.Low
        };
    }

    private static string GetRequirements(TaxType taxType)
    {
        return taxType switch
        {
            TaxType.IncomeTax => "Income statement, balance sheet, supporting schedules, previous year comparison",
            TaxType.GST => "Sales records, purchase records, input tax credits, GST returns",
            TaxType.PayrollTax => "Payroll records, employee details, tax deductions, NASSIT contributions",
            TaxType.ExciseDuty => "Production records, sales records, duty calculations",
            _ => "Supporting documentation as required by NRA"
        };
    }

    private async Task CreateAutomaticAlert(int clientId, TaxType taxType, DateTime dueDate, DeadlinePriority priority)
    {
        var daysUntilDue = (dueDate - DateTime.UtcNow).Days;
        
        if (daysUntilDue <= 14) // Create alert for deadlines within 14 days
        {
            var severity = priority switch
            {
                DeadlinePriority.Critical => ComplianceAlertSeverity.Critical,
                DeadlinePriority.High => ComplianceAlertSeverity.Critical,
                DeadlinePriority.Medium => ComplianceAlertSeverity.Warning,
                _ => ComplianceAlertSeverity.Info
            };

            var alert = new ComplianceAlertDto
            {
                ClientId = clientId,
                AlertType = ComplianceAlertType.UpcomingDeadline.ToString(),
                Severity = severity,
                Title = $"{taxType} Filing Due Soon",
                Message = $"{taxType} filing is due on {dueDate:MMM dd, yyyy} ({daysUntilDue} days remaining)",
                TaxType = taxType,
                DueDate = dueDate
            };

            await _alertService.CreateAlertAsync(alert);
        }
    }

    private static List<TaxType> GetApplicableTaxTypes(Client client)
    {
        var taxTypes = new List<TaxType>();

        // All active clients need to file income tax
        taxTypes.Add(TaxType.IncomeTax);

        // GST registration threshold check
        if (client.TaxpayerCategory != TaxpayerCategory.Micro)
        {
            taxTypes.Add(TaxType.GST);
        }

        // Payroll tax if client has employees
        taxTypes.Add(TaxType.PayrollTax);

        // Withholding tax for applicable businesses
        if (client.TaxpayerCategory == TaxpayerCategory.Large || 
            client.TaxpayerCategory == TaxpayerCategory.Medium)
        {
            taxTypes.Add(TaxType.PayrollTax);
        }

        return taxTypes;
    }

    private List<DateTime> GetUpcomingDueDates(TaxType taxType, int daysAhead)
    {
        var endDate = DateTime.UtcNow.AddDays(daysAhead);
        
        if (_standardDueDates.TryGetValue(taxType, out var dueDates))
        {
            return dueDates.Where(d => d >= DateTime.UtcNow && d <= endDate).ToList();
        }

        return new List<DateTime>();
    }

    /// <summary>
    /// Calculate Payroll Tax deadline - Annual returns due January 31
    /// Phase 2 requirement: Specific Payroll Tax deadline rules
    /// </summary>
    public static DateTime CalculatePayrollTaxAnnualDeadline(int taxYear)
    {
        return new DateTime(taxYear + 1, 1, 31);
    }

    /// <summary>
    /// Calculate foreign employee filing deadline - Within 1 month of start date
    /// Phase 2 requirement: Foreign employee specific deadline
    /// </summary>
    public static DateTime CalculateForeignEmployeeFilingDeadline(DateTime employeeStartDate)
    {
        return employeeStartDate.AddMonths(1);
    }

    /// <summary>
    /// Calculate Excise Duty deadline - 21 days from delivery/import date
    /// Phase 2 requirement: Excise Duty specific deadline rules
    /// </summary>
    public static DateTime CalculateExciseDutyDeadline(DateTime deliveryOrImportDate)
    {
        var deadline = deliveryOrImportDate.AddDays(21);
        // Move to next business day if falls on weekend
        return AdjustForWeekend(deadline);
    }

    /// <summary>
    /// Calculate GST deadline - Period end + 21 days (not fixed dates)
    /// Phase 2 requirement: GST deadline based on period end
    /// </summary>
    public static DateTime CalculateGstDeadline(DateTime periodEndDate)
    {
        var deadline = periodEndDate.AddDays(21);
        return AdjustForWeekend(deadline);
    }

    /// <summary>
    /// Adjust deadline to next business day if it falls on weekend
    /// Phase 2 requirement: Holiday/weekend handling
    /// </summary>
    private static DateTime AdjustForWeekend(DateTime date)
    {
        // If Saturday, move to Monday
        if (date.DayOfWeek == DayOfWeek.Saturday)
            return date.AddDays(2);
        
        // If Sunday, move to Monday
        if (date.DayOfWeek == DayOfWeek.Sunday)
            return date.AddDays(1);
        
        return date;
    }

    /// <summary>
    /// Convert UTC time to Sierra Leone timezone (GMT)
    /// Phase 2 requirement: Timezone awareness
    /// </summary>
    public static DateTime ConvertToSierraLeoneTime(DateTime utcDateTime)
    {
        // Sierra Leone is in GMT timezone (UTC+0)
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Utc);
    }
}