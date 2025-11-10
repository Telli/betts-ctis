using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Compliance;
using BettsTax.Data;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplianceController : ControllerBase
    {
        private readonly ILogger<ComplianceController> _logger;
        private readonly IComplianceService _complianceService;

        public ComplianceController(
            ILogger<ComplianceController> logger,
            IComplianceService complianceService)
        {
            _logger = logger;
            _complianceService = complianceService;
        }

        /// <summary>
        /// Get compliance overview for the current user
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetComplianceOverview()
        {
            try
            {
                // TODO: Implement compliance overview logic
                var overview = new
                {
                    ComplianceScore = 85,
                    Status = "Good",
                    LastUpdated = DateTime.UtcNow,
                    NextDeadline = DateTime.UtcNow.AddDays(30),
                    PendingTasks = 2,
                    CompletedTasks = 8
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance overview");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get compliance items/tasks for the current user
        /// </summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetComplianceItems()
        {
            try
            {
                // TODO: Implement compliance items logic
                var items = new[]
                {
                    new
                    {
                        Id = 1,
                        Title = "File Income Tax Return",
                        DueDate = DateTime.UtcNow.AddDays(30),
                        Status = "Pending",
                        Priority = "High",
                        Category = "Tax Filing"
                    },
                    new
                    {
                        Id = 2,
                        Title = "Submit GST Declaration",
                        DueDate = DateTime.UtcNow.AddDays(15),
                        Status = "Pending",
                        Priority = "Medium",
                        Category = "Tax Filing"
                    }
                };

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        /// <summary>
        /// Get enhanced compliance dashboard with visual metrics (Phase 2)
        /// </summary>
        [HttpGet("dashboard/client/{id}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
        public async Task<IActionResult> GetComplianceDashboard(int id)
        {
            try
            {
                // Aggregate multiple service calls for comprehensive dashboard
                var summary = await _complianceService.GetClientComplianceSummaryAsync(id);
                var checklist = await _complianceService.GetFilingChecklistAsync(id);
                var deadlines = await _complianceService.GetUpcomingDeadlinesAsync(id, 60);
                var penalties = await _complianceService.GetPenaltyWarningsAsync(id);
                var documentTracker = await _complianceService.GetDocumentTrackerAsync(id);

                var dashboard = new
                {
                    Summary = summary,
                    Checklist = checklist,
                    UpcomingDeadlines = deadlines,
                    PenaltyWarnings = penalties,
                    DocumentTracker = documentTracker,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance dashboard for client {ClientId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get compliance scorecard with visual indicators (Phase 2)
        /// </summary>
        [HttpGet("scorecard/client/{id}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate,Client")]
        public async Task<IActionResult> GetComplianceScorecard(int id)
        {
            try
            {
                var summary = await _complianceService.GetClientComplianceSummaryAsync(id);
                var adherenceHistory = await _complianceService.GetDeadlineAdherenceHistoryAsync(id, 12);

                // Create enhanced scorecard with visual data
                var scorecard = new
                {
                    ClientId = id,
                    OverallScore = summary.OverallComplianceScore,
                    ComplianceLevel = summary.ComplianceLevel,
                    Grade = GetComplianceGrade(summary.OverallComplianceScore),
                    
                    // Visual indicators
                    StatusColor = GetStatusColor(summary.ComplianceLevel),
                    TrendIndicator = GetTrendIndicator(adherenceHistory),
                    
                    // Summary metrics from the DTO
                    FilingMetrics = new
                    {
                        TotalRequired = summary.TotalFilingsRequired,
                        OnTime = summary.OnTimeFilings,
                        Late = summary.LateFilings,
                        Pending = summary.PendingFilings,
                        OnTimePercentage = summary.TotalFilingsRequired > 0 
                            ? Math.Round((decimal)summary.OnTimeFilings / summary.TotalFilingsRequired * 100, 1) 
                            : 0,
                        Status = GetFilingStatus(summary),
                        Color = GetFilingColor(summary)
                    },
                    PaymentMetrics = new
                    {
                        TotalDue = summary.TotalPaymentsDue,
                        Made = summary.PaymentsMade,
                        Overdue = summary.PaymentsOverdue,
                        OnTimePercentage = summary.TotalPaymentsDue > 0 
                            ? Math.Round((summary.TotalPaymentsDue - summary.PaymentsOverdue) / summary.TotalPaymentsDue * 100, 1) 
                            : 0,
                        Status = GetPaymentStatus(summary),
                        Color = GetPaymentColor(summary)
                    },
                    DocumentMetrics = new
                    {
                        TotalRequired = summary.TotalDocumentsRequired,
                        Submitted = summary.DocumentsSubmitted,
                        Pending = summary.DocumentsPending,
                        Rejected = summary.DocumentsRejected,
                        ReadinessPercentage = summary.TotalDocumentsRequired > 0 
                            ? Math.Round((decimal)summary.DocumentsSubmitted / summary.TotalDocumentsRequired * 100, 1) 
                            : 0,
                        Status = GetDocumentStatus(summary),
                        Color = GetDocumentColor(summary)
                    },
                    
                    // Risk and alerts
                    RiskLevel = summary.RiskLevel,
                    ActiveAlerts = summary.ActiveAlertsCount,
                    HighPriorityAlerts = summary.HighPriorityAlertsCount,
                    
                    // Upcoming deadlines
                    NextDeadline = summary.NextDeadline,
                    DaysToNextDeadline = summary.DaysToNextDeadline,
                    UpcomingCount = summary.UpcomingDeadlinesCount,
                    
                    LastUpdated = DateTime.UtcNow,
                    History = adherenceHistory
                };

                return Ok(scorecard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compliance scorecard for client {ClientId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // Helper methods for scorecard visual indicators
        private string GetComplianceGrade(decimal score)
        {
            return score switch
            {
                >= 90 => "A",
                >= 80 => "B", 
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        private string GetStatusColor(ComplianceLevel level)
        {
            return level switch
            {
                ComplianceLevel.Green => "#10B981",    // Green
                ComplianceLevel.Yellow => "#F59E0B",   // Amber
                ComplianceLevel.Red => "#EF4444",      // Red
                _ => "#6B7280"                         // Gray
            };
        }

        private string GetFilingStatus(ComplianceStatusSummaryDto summary)
        {
            if (summary.PendingFilings > 0) return "Action Required";
            if (summary.LateFilings > 0) return "Some Delays";
            return "On Track";
        }

        private string GetFilingColor(ComplianceStatusSummaryDto summary)
        {
            if (summary.PendingFilings > 0 || summary.LateFilings > summary.OnTimeFilings) return "#EF4444"; // Red
            if (summary.LateFilings > 0) return "#F59E0B"; // Amber
            return "#10B981"; // Green
        }

        private string GetPaymentStatus(ComplianceStatusSummaryDto summary)
        {
            if (summary.PaymentsOverdue > 0) return "Overdue Payments";
            if (summary.TotalPaymentsDue > summary.PaymentsMade) return "Payments Due";
            return "Current";
        }

        private string GetPaymentColor(ComplianceStatusSummaryDto summary)
        {
            if (summary.PaymentsOverdue > 0) return "#EF4444"; // Red
            if (summary.TotalPaymentsDue > summary.PaymentsMade) return "#F59E0B"; // Amber
            return "#10B981"; // Green
        }

        private string GetDocumentStatus(ComplianceStatusSummaryDto summary)
        {
            var readinessPercentage = summary.TotalDocumentsRequired > 0 
                ? (decimal)summary.DocumentsSubmitted / summary.TotalDocumentsRequired * 100 
                : 100;
            
            return readinessPercentage switch
            {
                >= 90 => "Complete",
                >= 70 => "Nearly Ready",
                >= 40 => "In Progress",
                _ => "Action Needed"
            };
        }

        private string GetDocumentColor(ComplianceStatusSummaryDto summary)
        {
            var readinessPercentage = summary.TotalDocumentsRequired > 0 
                ? (decimal)summary.DocumentsSubmitted / summary.TotalDocumentsRequired * 100 
                : 100;
            
            return readinessPercentage switch
            {
                >= 80 => "#10B981", // Green
                >= 60 => "#F59E0B", // Amber
                _ => "#EF4444"      // Red
            };
        }

        private string GetTrendIndicator(DeadlineAdherenceHistoryDto history)
        {
            // Simplified trend indicator since we don't have MonthlyAdherence property
            // In a real implementation, you would access the actual history data
            if (history != null)
            {
                return "➡️ Stable"; // Default to stable for now
            }
            return "➡️ Stable";
        }
    }
}