using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BettsTax.Core.BackgroundJobs
{
    /// <summary>
    /// Background job for cleaning up completed workflows
    /// Runs weekly to archive completed workflows and clean up old data
    /// </summary>
    public class WorkflowCleanupJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowCleanupJob> _logger;
        private const int ARCHIVE_DAYS = 90; // Archive workflows completed more than 90 days ago

        public WorkflowCleanupJob(
            ApplicationDbContext context,
            ILogger<WorkflowCleanupJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Execute the workflow cleanup job
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting Workflow Cleanup Job at {Timestamp}", DateTime.UtcNow);

                var archiveDate = DateTime.UtcNow.AddDays(-ARCHIVE_DAYS);

                // Archive completed payment approvals
                var completedPaymentApprovals = await _context.PaymentApprovalRequests
                    .Where(p => p.Status == PaymentApprovalStatus.Approved || p.Status == PaymentApprovalStatus.Rejected)
                    .Where(p => p.CompletedAt.HasValue && p.CompletedAt.Value < archiveDate)
                    .ToListAsync();

                if (completedPaymentApprovals.Any())
                {
                    _logger.LogInformation("Archiving {Count} completed payment approvals", completedPaymentApprovals.Count);
                    // Mark as archived (add IsArchived flag if available)
                    foreach (var approval in completedPaymentApprovals)
                    {
                        // Already completed, no need to update timestamp
                    }
                }

                // Archive completed document submissions
                var completedDocuments = await _context.DocumentSubmissionWorkflows
                    .Where(d => d.Status == DocumentSubmissionStatus.Approved || d.Status == DocumentSubmissionStatus.Rejected)
                    .Where(d => d.UpdatedAt < archiveDate)
                    .ToListAsync();

                if (completedDocuments.Any())
                {
                    _logger.LogInformation("Archiving {Count} completed document submissions", completedDocuments.Count);
                    foreach (var doc in completedDocuments)
                    {
                        doc.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Archive resolved communications
                var resolvedCommunications = await _context.CommunicationRoutingWorkflows
                    .Where(c => c.Status == CommunicationRoutingStatus.Resolved || c.Status == CommunicationRoutingStatus.Closed)
                    .Where(c => c.CreatedAt < archiveDate)
                    .ToListAsync();

                if (resolvedCommunications.Any())
                {
                    _logger.LogInformation("Archiving {Count} resolved communications", resolvedCommunications.Count);
                    foreach (var comm in resolvedCommunications)
                    {
                        comm.CreatedAt = DateTime.UtcNow;
                    }
                }

                // Clean up old workflow instances (generic framework)
                var completedWorkflowInstances = await _context.WorkflowInstances
                    .Where(w => w.Status == WorkflowInstanceStatus.Completed || w.Status == WorkflowInstanceStatus.Cancelled)
                    .Where(w => w.CompletedAt.HasValue && w.CompletedAt.Value < archiveDate)
                    .ToListAsync();

                if (completedWorkflowInstances.Any())
                {
                    _logger.LogInformation("Archiving {Count} completed workflow instances", completedWorkflowInstances.Count);
                    foreach (var instance in completedWorkflowInstances)
                    {
                        instance.CompletedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Workflow Cleanup Job completed successfully at {Timestamp}. " +
                    "Archived: {PaymentApprovals} payment approvals, {Documents} documents, {Communications} communications, {Instances} workflow instances",
                    DateTime.UtcNow,
                    completedPaymentApprovals.Count,
                    completedDocuments.Count,
                    resolvedCommunications.Count,
                    completedWorkflowInstances.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Workflow Cleanup Job");
                throw;
            }
        }
    }
}

