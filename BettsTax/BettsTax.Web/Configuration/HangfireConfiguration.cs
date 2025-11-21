using BettsTax.Core.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

#if HANGFIRE
using Hangfire;
#endif

namespace BettsTax.Web.Configuration
{
    /// <summary>
    /// Hangfire configuration for background job scheduling
    /// </summary>
    public static class HangfireConfiguration
    {
        /// <summary>
        /// Add Hangfire services to the dependency injection container when the Hangfire package is available.
        /// The method becomes a no-op when Hangfire is not referenced so the application can still build and run.
        /// </summary>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, string? connectionString)
        {
#if HANGFIRE
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddHangfire(config =>
                {
                    config.UseSqlServerStorage(connectionString);
                });

                services.AddHangfireServer();
            }
#else
            _ = connectionString; // Hangfire is disabled; return services unchanged.
#endif

            return services;
        }

#if HANGFIRE
        /// <summary>
        /// Configure recurring background jobs. Only runs when Hangfire is available at build time.
        /// </summary>
        public static void ConfigureRecurringJobs(IRecurringJobManager recurringJobManager, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(recurringJobManager);

            // Compliance Deadline Monitoring - Daily at 6:00 AM UTC
            recurringJobManager.AddOrUpdate<ComplianceDeadlineMonitoringJob>(
                "compliance-deadline-monitoring",
                job => job.ExecuteAsync(),
                Cron.Daily(6),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            // Communication Escalation - Every hour
            recurringJobManager.AddOrUpdate<CommunicationEscalationJob>(
                "communication-escalation",
                job => job.ExecuteAsync(),
                Cron.Hourly(),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            // Workflow Cleanup - Weekly on Sunday at 2:00 AM UTC
            recurringJobManager.AddOrUpdate<WorkflowCleanupJob>(
                "workflow-cleanup",
                job => job.ExecuteAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 2),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            // Workflow Trigger Evaluation - Every 5 minutes
            recurringJobManager.AddOrUpdate<WorkflowTriggerEvaluationJob>(
                "workflow-trigger-evaluation",
                job => job.ExecuteAsync(),
                Cron.MinuteInterval(5),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            _ = serviceProvider; // Reserved for future job dependencies.
        }
#else
        /// <summary>
        /// No-op placeholder when Hangfire is not referenced.
        /// </summary>
        public static void ConfigureRecurringJobs(object? recurringJobManager, IServiceProvider serviceProvider)
        {
            _ = recurringJobManager;
            _ = serviceProvider;
        }
#endif
    }
}

