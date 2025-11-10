using BettsTax.Web.Services;

namespace BettsTax.Web.Services;

public class TaxAuthorityBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaxAuthorityBackgroundService> _logger;
    private readonly TimeSpan _submissionInterval = TimeSpan.FromMinutes(30); // Process submissions every 30 minutes
    private readonly TimeSpan _statusCheckInterval = TimeSpan.FromMinutes(15); // Check status every 15 minutes

    public TaxAuthorityBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TaxAuthorityBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tax Authority Background Service starting");

        // Create tasks for submission processing and status checking
        var submissionTask = ProcessSubmissionsAsync(stoppingToken);
        var statusCheckTask = ProcessStatusChecksAsync(stoppingToken);

        // Run both tasks concurrently
        await Task.WhenAll(submissionTask, statusCheckTask);
    }

    private async Task ProcessSubmissionsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var taxAuthorityService = scope.ServiceProvider.GetRequiredService<ITaxAuthorityService>();
                    await taxAuthorityService.ProcessPendingSubmissionsAsync();
                }

                _logger.LogInformation("Processed pending tax authority submissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending tax authority submissions");
            }

            await Task.Delay(_submissionInterval, stoppingToken);
        }
    }

    private async Task ProcessStatusChecksAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var taxAuthorityService = scope.ServiceProvider.GetRequiredService<ITaxAuthorityService>();
                    await taxAuthorityService.ProcessStatusChecksAsync();
                }

                _logger.LogInformation("Processed tax authority status checks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tax authority status checks");
            }

            await Task.Delay(_statusCheckInterval, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tax Authority Background Service stopping");
        await base.StopAsync(stoppingToken);
    }
}