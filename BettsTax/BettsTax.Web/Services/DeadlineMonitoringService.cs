using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// In-memory implementation of <see cref="IDeadlineMonitoringService"/> used for demos/tests.
/// Ensures deterministic data while exercising access control paths.
/// </summary>
public class DeadlineMonitoringService : IDeadlineMonitoringService
{
    private readonly ILogger<DeadlineMonitoringService> _logger;
    private readonly List<DeadlineDto> _deadlines;

    public DeadlineMonitoringService(ILogger<DeadlineMonitoringService> logger)
    {
        _logger = logger;
        _deadlines = SeedDeadlines();
    }

    public Task<IReadOnlyList<DeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId, int daysAhead)
    {
        var now = DateTime.UtcNow.Date;
        var maxDate = now.AddDays(daysAhead);

        var results = _deadlines
            .Where(d => !d.IsCompleted && d.DueDate.Date >= now && d.DueDate.Date <= maxDate)
            .Where(d => !clientId.HasValue || d.ClientId == clientId)
            .OrderBy(d => d.DueDate)
            .ToList();

        _logger.LogDebug("Returning {Count} upcoming deadlines (clientId: {ClientId}, daysAhead: {DaysAhead})",
            results.Count, clientId?.ToString() ?? "all", daysAhead);

        return Task.FromResult<IReadOnlyList<DeadlineDto>>(results);
    }

    public Task<IReadOnlyList<DeadlineDto>> GetOverdueItemsAsync(int? clientId)
    {
        var now = DateTime.UtcNow.Date;

        var results = _deadlines
            .Where(d => !d.IsCompleted && d.DueDate.Date < now)
            .Where(d => !clientId.HasValue || d.ClientId == clientId)
            .OrderBy(d => d.DueDate)
            .ToList();

        _logger.LogDebug("Returning {Count} overdue deadlines (clientId: {ClientId})", results.Count, clientId?.ToString() ?? "all");

        return Task.FromResult<IReadOnlyList<DeadlineDto>>(results);
    }

    private static List<DeadlineDto> SeedDeadlines()
    {
        var today = DateTime.UtcNow.Date;

        return new List<DeadlineDto>
        {
            new()
            {
                Id = 1,
                ClientId = 1,
                ClientName = "ABC Corporation",
                TaxTypeName = "GST Return",
                DueDate = today.AddDays(5),
                Status = DeadlineStatus.Upcoming,
                Priority = DeadlinePriority.High,
                AssignedTo = "Jane Smith"
            },
            new()
            {
                Id = 2,
                ClientId = 1,
                ClientName = "ABC Corporation",
                TaxTypeName = "Payroll Tax",
                DueDate = today.AddDays(15),
                Status = DeadlineStatus.Upcoming,
                Priority = DeadlinePriority.Medium,
                AssignedTo = "John Doe"
            },
            new()
            {
                Id = 3,
                ClientId = 2,
                ClientName = "XYZ Trading",
                TaxTypeName = "Income Tax",
                DueDate = today.AddDays(-2),
                Status = DeadlineStatus.Overdue,
                Priority = DeadlinePriority.Critical,
                AssignedTo = "Sarah Johnson"
            },
            new()
            {
                Id = 4,
                ClientId = 3,
                ClientName = "Tech Innovations Ltd",
                TaxTypeName = "Excise Duty",
                DueDate = today.AddDays(-10),
                Status = DeadlineStatus.Overdue,
                Priority = DeadlinePriority.High,
                AssignedTo = "Mike Brown"
            },
            new()
            {
                Id = 5,
                ClientId = 2,
                ClientName = "XYZ Trading",
                TaxTypeName = "VAT Filing",
                DueDate = today.AddDays(2),
                Status = DeadlineStatus.DueSoon,
                Priority = DeadlinePriority.Critical,
                AssignedTo = "Emily Davis"
            }
        };
    }
}
