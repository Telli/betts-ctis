using BettsTax.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Helper class to simplify activity logging throughout the application
    /// </summary>
    public class ActivityLogger
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ActivityLogger> _logger;

        public ActivityLogger(IServiceProvider serviceProvider, ILogger<ActivityLogger> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task LogDocumentUploadedAsync(int documentId, int clientId, string fileName)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogDocumentActivityAsync(documentId, ActivityType.DocumentUploaded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log document upload activity");
            }
        }

        public async Task LogDocumentVerifiedAsync(int documentId, int clientId, string fileName, string reviewerName)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogDocumentActivityAsync(
                    documentId, 
                    ActivityType.DocumentVerified, 
                    $"Verified by {reviewerName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log document verification activity");
            }
        }

        public async Task LogDocumentRejectedAsync(int documentId, int clientId, string fileName, string reason)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogDocumentActivityAsync(
                    documentId, 
                    ActivityType.DocumentRejected, 
                    $"Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log document rejection activity");
            }
        }

        public async Task LogTaxFilingSubmittedAsync(int taxFilingId, int clientId, string filingReference)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogTaxFilingActivityAsync(taxFilingId, ActivityType.TaxFilingSubmitted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log tax filing submission activity");
            }
        }

        public async Task LogPaymentProcessedAsync(int paymentId, int clientId, decimal amount, string paymentReference)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogPaymentActivityAsync(paymentId, ActivityType.PaymentProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log payment processed activity");
            }
        }

        public async Task LogClientLoginAsync(int clientId, string clientName)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogClientLoginAsync(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log client login activity");
            }
        }

        public async Task LogDeadlineApproachingAsync(int clientId, string deadlineDescription, DateTime dueDate)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogActivityAsync(
                    ActivityType.DeadlineApproaching,
                    $"Deadline approaching: {deadlineDescription}",
                    $"Due date: {dueDate:yyyy-MM-dd}",
                    clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log deadline approaching activity");
            }
        }

        public async Task LogCommunicationAsync(int clientId, ActivityType communicationType, string title, string? description = null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogCommunicationActivityAsync(clientId, communicationType, title, description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log communication activity");
            }
        }

        public async Task LogSystemEventAsync(string title, string description, ActivityPriority priority = ActivityPriority.Normal)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var activityService = scope.ServiceProvider.GetRequiredService<IActivityTimelineService>();
                
                await activityService.LogActivityAsync(
                    ActivityType.SystemGenerated,
                    title,
                    description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system event activity");
            }
        }
    }
}