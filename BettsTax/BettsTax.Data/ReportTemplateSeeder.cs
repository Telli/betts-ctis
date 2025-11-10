using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace BettsTax.Data
{
    public static class ReportTemplateSeeder
    {
        public static async Task SeedReportTemplatesAsync(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Check if templates already exist
            if (await context.ReportTemplates.AnyAsync())
                return;

            var templates = new List<Models.ReportTemplate>
            {
                // Tax Compliance Report
                new()
                {
                    Name = "Tax Compliance Report",
                    Description = "Comprehensive tax compliance status with Finance Act 2025 requirements",
                    ReportType = "TaxCompliance",
                    Category = "Tax",
                    Icon = "FileText",
                    EstimatedDurationSeconds = 180,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel" }),
                    Features = JsonSerializer.Serialize(new[] { "Compliance scoring", "Deadline tracking", "Penalty analysis", "Risk assessment" }),
                    RequiredFields = JsonSerializer.Serialize(new[] { "clientId" }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "includeHistory", type = "boolean", label = "Include Historical Data", @default = false },
                        new { name = "riskAssessment", type = "boolean", label = "Include Risk Assessment", @default = true },
                        new { name = "penaltyAnalysis", type = "boolean", label = "Include Penalty Analysis", @default = true }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["includeHistory"] = false,
                        ["riskAssessment"] = true,
                        ["penaltyAnalysis"] = true
                    }),
                    DisplayOrder = 1
                },

                // Payment History Report
                new()
                {
                    Name = "Payment History Report",
                    Description = "Complete payment transaction history with mobile money integration",
                    ReportType = "PaymentHistory",
                    Category = "Financial",
                    Icon = "DollarSign",
                    EstimatedDurationSeconds = 120,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel", "CSV" }),
                    Features = JsonSerializer.Serialize(new[] { "Transaction details", "Payment methods", "Reconciliation", "Trends analysis" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "paymentMethod", type = "select", label = "Payment Method", options = new[] { "All", "Orange Money", "Africell Money", "Bank Transfer" } },
                        new { name = "includeFailures", type = "boolean", label = "Include Failed Transactions", @default = false },
                        new { name = "groupByClient", type = "boolean", label = "Group by Client", @default = true }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["paymentMethod"] = "All",
                        ["includeFailures"] = false,
                        ["groupByClient"] = true
                    }),
                    DisplayOrder = 2
                },

                // Client Activity Report
                new()
                {
                    Name = "Client Activity Report",
                    Description = "Client engagement and activity analysis with compliance metrics",
                    ReportType = "ClientActivity",
                    Category = "Client Management",
                    Icon = "Users",
                    EstimatedDurationSeconds = 150,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel" }),
                    Features = JsonSerializer.Serialize(new[] { "Activity tracking", "Login analytics", "Document uploads", "Filing status" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "includeDormant", type = "boolean", label = "Include Dormant Clients", @default = false },
                        new { name = "activityThreshold", type = "number", label = "Activity Threshold (days)", @default = 30 },
                        new { name = "detailLevel", type = "select", label = "Detail Level", options = new[] { "Summary", "Detailed", "Complete" } }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["includeDormant"] = false,
                        ["activityThreshold"] = 30,
                        ["detailLevel"] = "Detailed"
                    }),
                    DisplayOrder = 3
                },

                // KPI Summary Report
                new()
                {
                    Name = "KPI Summary Report",
                    Description = "Key Performance Indicators dashboard with business metrics",
                    ReportType = "KPISummary",
                    Category = "Analytics",
                    Icon = "BarChart3",
                    EstimatedDurationSeconds = 90,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel" }),
                    Features = JsonSerializer.Serialize(new[] { "Revenue metrics", "Client metrics", "Compliance metrics", "Trend analysis" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "includeTargets", type = "boolean", label = "Include Targets", @default = true },
                        new { name = "compareWithPrevious", type = "boolean", label = "Compare with Previous Period", @default = true },
                        new { name = "breakdownLevel", type = "select", label = "Breakdown Level", options = new[] { "Monthly", "Quarterly", "Yearly" } }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["includeTargets"] = true,
                        ["compareWithPrevious"] = true,
                        ["breakdownLevel"] = "Monthly"
                    }),
                    DisplayOrder = 4
                },

                // Penalty Analysis Report
                new()
                {
                    Name = "Penalty Analysis Report",
                    Description = "Detailed penalty analysis with Finance Act 2025 calculations",
                    ReportType = "PenaltyAnalysis",
                    Category = "Compliance",
                    Icon = "AlertCircle",
                    EstimatedDurationSeconds = 120,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel" }),
                    Features = JsonSerializer.Serialize(new[] { "Penalty calculations", "Late filing analysis", "Interest calculations", "Mitigation strategies" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "severityLevel", type = "select", label = "Severity Level", options = new[] { "All", "Minor", "Major", "Critical" } },
                        new { name = "includeProjections", type = "boolean", label = "Include Projections", @default = true },
                        new { name = "mitigationStrategies", type = "boolean", label = "Include Mitigation Strategies", @default = true }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["severityLevel"] = "All",
                        ["includeProjections"] = true,
                        ["mitigationStrategies"] = true
                    }),
                    DisplayOrder = 5
                },

                // Document Summary Report
                new()
                {
                    Name = "Document Summary Report",
                    Description = "Document management and verification status report",
                    ReportType = "DocumentSummary",
                    Category = "Document Management",
                    Icon = "FileSpreadsheet",
                    EstimatedDurationSeconds = 100,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel", "CSV" }),
                    Features = JsonSerializer.Serialize(new[] { "Document tracking", "Verification status", "Missing documents", "Upload history" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "includeArchived", type = "boolean", label = "Include Archived Documents", @default = false },
                        new { name = "verificationStatus", type = "select", label = "Verification Status", options = new[] { "All", "Verified", "Pending", "Rejected" } },
                        new { name = "groupByType", type = "boolean", label = "Group by Type", @default = true }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["includeArchived"] = false,
                        ["verificationStatus"] = "All",
                        ["groupByType"] = true
                    }),
                    DisplayOrder = 6
                },

                // Revenue Analysis Report
                new()
                {
                    Name = "Revenue Analysis Report",
                    Description = "Financial revenue breakdown with trend analysis",
                    ReportType = "RevenueAnalysis",
                    Category = "Financial",
                    Icon = "TrendingUp",
                    EstimatedDurationSeconds = 110,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel" }),
                    Features = JsonSerializer.Serialize(new[] { "Revenue trends", "Client segmentation", "Tax type breakdown", "Growth analysis" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "includeCharts", type = "boolean", label = "Include Charts", @default = true },
                        new { name = "segmentByCategory", type = "boolean", label = "Segment by Category", @default = true },
                        new { name = "forecastPeriod", type = "select", label = "Forecast Period", options = new[] { "None", "3 Months", "6 Months", "12 Months" } }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["includeCharts"] = true,
                        ["segmentByCategory"] = true,
                        ["forecastPeriod"] = "3 Months"
                    }),
                    DisplayOrder = 7
                },

                // Audit Trail Report
                new()
                {
                    Name = "Audit Trail Report",
                    Description = "Complete audit trail with user actions and system events",
                    ReportType = "AuditTrail",
                    Category = "Security",
                    Icon = "Shield",
                    EstimatedDurationSeconds = 140,
                    IsDefault = true,
                    IsActive = true,
                    SupportedFormats = JsonSerializer.Serialize(new[] { "PDF", "Excel", "CSV" }),
                    Features = JsonSerializer.Serialize(new[] { "User activity", "System events", "Data modifications", "Security monitoring" }),
                    RequiredFields = JsonSerializer.Serialize(new string[] { }),
                    Parameters = JsonSerializer.Serialize(new[]
                    {
                        new { name = "eventCategory", type = "select", label = "Event Category", options = new[] { "All", "Authentication", "DataModification", "Security", "System" } },
                        new { name = "severityFilter", type = "select", label = "Severity Filter", options = new[] { "All", "Low", "Medium", "High", "Critical" } },
                        new { name = "includeSystemEvents", type = "boolean", label = "Include System Events", @default = false }
                    }),
                    DefaultParameterValues = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["eventCategory"] = "All",
                        ["severityFilter"] = "All",
                        ["includeSystemEvents"] = false
                    }),
                    DisplayOrder = 8
                }
            };

            context.ReportTemplates.AddRange(templates);
            await context.SaveChangesAsync();
        }
    }
}
