using BettsTax.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BettsTax.Web.Services
{
    public class SmsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SmsBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public SmsBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SmsBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessScheduledSmsAsync();
                    await RetryFailedSmsAsync();
                    await RunScheduledRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SMS background service");
                }
            }
        }

        private async Task ProcessScheduledSmsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

                var result = await smsService.ProcessScheduledSmsAsync();
                if (result.IsSuccess && result.Value > 0)
                {
                    _logger.LogInformation("Processed {Count} scheduled SMS messages", result.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled SMS");
            }
        }

        private async Task RetryFailedSmsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

                var result = await smsService.RetryFailedSmsAsync(DateTime.UtcNow.AddHours(-1));
                if (result.IsSuccess && result.Value > 0)
                {
                    _logger.LogInformation("Retried {Count} failed SMS messages", result.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed SMS");
            }
        }

        private async Task RunScheduledRemindersAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

                // Get active schedules
                var schedulesResult = await smsService.GetSmsSchedulesAsync(true);
                if (!schedulesResult.IsSuccess)
                    return;

                var currentTime = DateTime.UtcNow;
                var currentHour = currentTime.Hour;
                var currentMinute = currentTime.Minute;

                foreach (var schedule in schedulesResult.Value)
                {
                    // Check if it's time to run this schedule
                    if (TimeSpan.TryParse(schedule.TimeOfDay, out var scheduleTime))
                    {
                        // Run if current time matches schedule time (within 5-minute window)
                        if (currentHour == scheduleTime.Hours && 
                            Math.Abs(currentMinute - scheduleTime.Minutes) <= 2)
                        {
                            // Check if already ran today
                            if (schedule.LastRunDate?.Date != currentTime.Date)
                            {
                                var runResult = await smsService.RunScheduledSmsAsync(schedule.SmsScheduleId);
                                if (runResult.IsSuccess && runResult.Value > 0)
                                {
                                    _logger.LogInformation("Schedule '{Name}' sent {Count} reminder SMS", 
                                        schedule.Name, runResult.Value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled reminders");
            }
        }
    }
}