using BettsTax.Core.DTOs.Demo;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// Provides a centralized in-memory data source so that the frontend can depend
/// on the backend for all dynamic content. This should be replaced with real data
/// repositories when integrating with production systems.
/// </summary>
public class DemoDataService : IDemoDataService
{
    private readonly IDeadlineMonitoringService _deadlineService;
    private readonly ILogger<DemoDataService> _logger;

    private readonly IReadOnlyList<ClientSummaryDto> _clients;
    private readonly IReadOnlyList<DocumentRecordDto> _documents;
    private readonly IReadOnlyList<PaymentRecordDto> _payments;
    private readonly IReadOnlyList<DashboardMetricDto> _dashboardMetrics;
    private readonly IReadOnlyList<DashboardTrendPointDto> _dashboardTrend;
    private readonly IReadOnlyList<DistributionSliceDto> _dashboardDistribution;
    private readonly IReadOnlyList<DashboardActivityDto> _dashboardActivity;
    private readonly IReadOnlyList<KpiMetricDto> _internalKpiMetrics;
    private readonly IReadOnlyList<KpiMetricDto> _clientKpiMetrics;
    private readonly IReadOnlyList<KpiTrendPointDto> _kpiTrend;
    private readonly IReadOnlyList<ClientPerformanceDto> _kpiClientPerformance;
    private readonly IReadOnlyList<PerformanceBreakdownDto> _kpiBreakdown;
    private readonly IReadOnlyList<ReportTypeDto> _reportTypes;
    private readonly ReportFiltersDto _reportFilters;
    private readonly IReadOnlyList<ChatConversationDto> _chatConversations;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<ChatMessageDto>> _chatMessagesByConversation;
    private readonly IReadOnlyList<AdminUserDto> _adminUsers;
    private readonly IReadOnlyList<AuditLogEntryDto> _auditLogs;
    private readonly IReadOnlyList<TaxRateDto> _taxRates;
    private readonly IReadOnlyList<JobStatusDto> _jobStatuses;
    private readonly FilingWorkspaceDto _filingWorkspace;

    public DemoDataService(
        IDeadlineMonitoringService deadlineService,
        ILogger<DemoDataService> logger)
    {
        _deadlineService = deadlineService;
        _logger = logger;

        var today = DateTime.UtcNow.Date;

        _clients = new List<ClientSummaryDto>
        {
            new()
            {
                Id = 1,
                Name = "ABC Corporation Ltd",
                Tin = "1234567890",
                Segment = "Corporate",
                Industry = "Manufacturing",
                Status = "Active",
                ComplianceScore = 95,
                AssignedTo = "Jane Smith"
            },
            new()
            {
                Id = 2,
                Name = "XYZ Trading Company",
                Tin = "0987654321",
                Segment = "SME",
                Industry = "Retail",
                Status = "Active",
                ComplianceScore = 87,
                AssignedTo = "John Doe"
            },
            new()
            {
                Id = 3,
                Name = "Tech Solutions Inc",
                Tin = "1122334455",
                Segment = "Corporate",
                Industry = "Technology",
                Status = "Active",
                ComplianceScore = 92,
                AssignedTo = "Sarah Johnson"
            },
            new()
            {
                Id = 4,
                Name = "Global Imports Ltd",
                Tin = "5544332211",
                Segment = "Large Enterprise",
                Industry = "Import/Export",
                Status = "Active",
                ComplianceScore = 78,
                AssignedTo = "Mike Brown"
            },
            new()
            {
                Id = 5,
                Name = "Local Cafe Chain",
                Tin = "9988776655",
                Segment = "SME",
                Industry = "Food & Beverage",
                Status = "At Risk",
                ComplianceScore = 65,
                AssignedTo = "Jane Smith"
            }
        };

        _documents = new List<DocumentRecordDto>
        {
            new()
            {
                Id = 1,
                ClientId = 1,
                Name = "Financial Statements 2024.pdf",
                Type = "Financial Statement",
                Client = "ABC Corporation",
                Year = 2024,
                TaxType = "Income Tax",
                Version = 2,
                UploadedBy = "John Doe",
                UploadDate = today.AddDays(-7),
                Hash = "a3b2c1d4e5f6...",
                Status = "verified"
            },
            new()
            {
                Id = 2,
                ClientId = 2,
                Name = "Bank Statements Q3.pdf",
                Type = "Bank Statement",
                Client = "XYZ Trading",
                Year = 2025,
                TaxType = "GST",
                Version = 1,
                UploadedBy = "Jane Smith",
                UploadDate = today.AddDays(-10),
                Hash = "f6e5d4c3b2a1...",
                Status = "scanning"
            },
            new()
            {
                Id = 3,
                ClientId = 3,
                Name = "Sales Records Sept.xlsx",
                Type = "Sales Record",
                Client = "Tech Solutions",
                Year = 2025,
                TaxType = "GST",
                Version = 3,
                UploadedBy = "Mike Brown",
                UploadDate = today.AddDays(-5),
                Hash = "b4c5d6e7f8a9...",
                Status = "verified"
            },
            new()
            {
                Id = 4,
                ClientId = 1,
                Name = "Payroll Summary Q3.pdf",
                Type = "Payroll Record",
                Client = "ABC Corporation",
                Year = 2025,
                TaxType = "PAYE",
                Version = 1,
                UploadedBy = "Sarah Johnson",
                UploadDate = today.AddDays(-13),
                Hash = "c5d6e7f8a9b0...",
                Status = "verified"
            }
        };

        _payments = new List<PaymentRecordDto>
        {
            new()
            {
                Id = 1,
                ClientId = 1,
                Client = "ABC Corporation",
                TaxType = "GST",
                Period = "Q3 2025",
                Amount = 22_500m,
                Method = "Bank Transfer",
                Status = "Paid",
                Date = today.AddDays(-8),
                ReceiptNumber = "RCP-2025-001"
            },
            new()
            {
                Id = 2,
                ClientId = 2,
                Client = "XYZ Trading",
                TaxType = "Income Tax",
                Period = "2024",
                Amount = 150_000m,
                Method = "Cheque",
                Status = "Pending",
                Date = today.AddDays(-11),
                ReceiptNumber = "RCP-2025-002"
            },
            new()
            {
                Id = 3,
                ClientId = 3,
                Client = "Tech Solutions",
                TaxType = "PAYE",
                Period = "Sep 2025",
                Amount = 45_000m,
                Method = "Cash",
                Status = "Paid",
                Date = today.AddDays(-14),
                ReceiptNumber = "RCP-2025-003"
            },
            new()
            {
                Id = 4,
                ClientId = 4,
                Client = "Global Imports",
                TaxType = "Excise Duty",
                Period = "Q3 2025",
                Amount = 78_000m,
                Method = "Bank Transfer",
                Status = "Overdue",
                Date = today.AddDays(-20),
                ReceiptNumber = "RCP-2025-004"
            }
        };

        _dashboardMetrics = new List<DashboardMetricDto>
        {
            new()
            {
                Key = "clientCompliance",
                Title = "Client Compliance Rate",
                Value = "92%",
                TrendDirection = "up",
                TrendValue = "+5%",
                Subtitle = "vs last month",
                Color = "success"
            },
            new()
            {
                Key = "filingTimeliness",
                Title = "Filing Timeliness",
                Value = "15 days",
                TrendDirection = "up",
                TrendValue = "+2 days",
                Subtitle = "avg before deadline",
                Color = "primary"
            },
            new()
            {
                Key = "paymentCompletion",
                Title = "Payment Completion",
                Value = "87%",
                TrendDirection = "down",
                TrendValue = "-3%",
                Subtitle = "on-time payments",
                Color = "info"
            },
            new()
            {
                Key = "documentCompliance",
                Title = "Document Compliance",
                Value = "94%",
                TrendDirection = "up",
                TrendValue = "+8%",
                Subtitle = "submitted on time",
                Color = "success"
            }
        };

        _dashboardTrend = new List<DashboardTrendPointDto>
        {
            new() { Month = "Jan", OnTime = 85, Late = 15 },
            new() { Month = "Feb", OnTime = 88, Late = 12 },
            new() { Month = "Mar", OnTime = 92, Late = 8 },
            new() { Month = "Apr", OnTime = 87, Late = 13 },
            new() { Month = "May", OnTime = 90, Late = 10 },
            new() { Month = "Jun", OnTime = 94, Late = 6 }
        };

        _dashboardDistribution = new List<DistributionSliceDto>
        {
            new() { Name = "Fully Compliant", Value = 65, Color = "#38a169" },
            new() { Name = "Pending", Value = 20, Color = "#d69e2e" },
            new() { Name = "At Risk", Value = 10, Color = "#e53e3e" },
            new() { Name = "Non-Compliant", Value = 5, Color = "#1a202c" }
        };

        _dashboardActivity = new List<DashboardActivityDto>
        {
            new() { TimeDescription = "2 hours ago", Action = "GST Return filed for ABC Corp", User = "Jane Smith" },
            new() { TimeDescription = "4 hours ago", Action = "Document uploaded: Financial Statements", User = "John Doe" },
            new() { TimeDescription = "Yesterday", Action = "Payment processed: SLE 15,000", User = "Sarah Johnson" },
            new() { TimeDescription = "2 days ago", Action = "New client onboarded: Tech Innovations Ltd", User = "Mike Brown" }
        };

        _internalKpiMetrics = new List<KpiMetricDto>
        {
            new() { Key = "complianceRate", Title = "Compliance Rate", Value = "94%", TrendDirection = "up", TrendValue = "+3%", Subtitle = "vs last period", Color = "success" },
            new() { Key = "avgTimeliness", Title = "Avg Timeliness", Value = "17 days", TrendDirection = "up", TrendValue = "+2 days", Subtitle = "before deadline", Color = "primary" },
            new() { Key = "paymentCompletion", Title = "Payment Completion", Value = "92%", TrendDirection = "up", TrendValue = "+5%", Subtitle = "on-time payments", Color = "info" },
            new() { Key = "docSubmission", Title = "Doc Submission", Value = "91%", TrendDirection = "up", TrendValue = "+6%", Subtitle = "compliance rate", Color = "success" },
            new() { Key = "engagementRate", Title = "Engagement Rate", Value = "87%", TrendDirection = "up", TrendValue = "+2%", Subtitle = "active clients", Color = "info" }
        };

        _clientKpiMetrics = new List<KpiMetricDto>
        {
            new() { Key = "myTimeliness", Title = "My Timeliness", Value = "18 days", TrendDirection = "up", TrendValue = "+4 days", Subtitle = "avg before deadline", Color = "primary" },
            new() { Key = "onTimePayments", Title = "On-Time Payments", Value = "100%", TrendDirection = "neutral", TrendValue = "0%", Subtitle = "perfect record", Color = "success" },
            new() { Key = "documentReadiness", Title = "Document Readiness", Value = "92%", TrendDirection = "up", TrendValue = "+12%", Subtitle = "submission rate", Color = "info" },
            new() { Key = "compositeScore", Title = "Composite Score", Value = "94%", TrendDirection = "up", TrendValue = "+3%", Subtitle = "overall compliance", Color = "success" }
        };

        _kpiTrend = new List<KpiTrendPointDto>
        {
            new() { Month = "Apr", Compliance = 88, Timeliness = 12, Payments = 85 },
            new() { Month = "May", Compliance = 90, Timeliness = 15, Payments = 87 },
            new() { Month = "Jun", Compliance = 92, Timeliness = 14, Payments = 90 },
            new() { Month = "Jul", Compliance = 91, Timeliness = 16, Payments = 88 },
            new() { Month = "Aug", Compliance = 93, Timeliness = 18, Payments = 91 },
            new() { Month = "Sep", Compliance = 94, Timeliness = 17, Payments = 92 }
        };

        _kpiClientPerformance = new List<ClientPerformanceDto>
        {
            new() { Name = "ABC Corp", Score = 95 },
            new() { Name = "XYZ Trading", Score = 88 },
            new() { Name = "Tech Solutions", Score = 92 },
            new() { Name = "Global Imports", Score = 78 },
            new() { Name = "Local Cafe", Score = 85 }
        };

        _kpiBreakdown = new List<PerformanceBreakdownDto>
        {
            new() { Metric = "Filing Timeliness", Score = 95, Color = "success" },
            new() { Metric = "Payment Compliance", Score = 100, Color = "success" },
            new() { Metric = "Document Submission", Score = 92, Color = "info" },
            new() { Metric = "Response Time", Score = 88, Color = "info" }
        };

        _reportTypes = new List<ReportTypeDto>
        {
            new() { Id = "tax-filing", Name = "Tax Filing Summary", Description = "Comprehensive report of all tax filings by period and type", IconKey = "FileText" },
            new() { Id = "payment-history", Name = "Payment History", Description = "Detailed history of all tax payments and receipts", IconKey = "FileText" },
            new() { Id = "compliance", Name = "Compliance Report", Description = "Client compliance status and deadline adherence", IconKey = "FileText" },
            new() { Id = "document-submission", Name = "Document Submission", Description = "Track document submission rates and completeness", IconKey = "FileText" },
            new() { Id = "tax-calendar", Name = "Tax Calendar", Description = "Upcoming deadlines and filing requirements", IconKey = "FileText" },
            new() { Id = "revenue-processed", Name = "Revenue Processed", Description = "Total revenue processed by tax type and period", IconKey = "FileText" },
            new() { Id = "activity-logs", Name = "Activity Logs", Description = "Detailed audit trail of all system activities", IconKey = "FileText" },
            new() { Id = "case-management", Name = "Case Management", Description = "Overview of ongoing cases and issues", IconKey = "FileText" }
        };

        _reportFilters = new ReportFiltersDto
        {
            Clients = _clients
                .Select(client => new FilterOptionDto { Value = client.Id.ToString(), Label = client.Name })
                .Prepend(new FilterOptionDto { Value = "all", Label = "All Clients" })
                .ToList(),
            TaxTypes = new List<FilterOptionDto>
            {
                new() { Value = "all", Label = "All Types" },
                new() { Value = "gst", Label = "GST" },
                new() { Value = "income", Label = "Income Tax" },
                new() { Value = "paye", Label = "PAYE" },
                new() { Value = "excise", Label = "Excise Duty" }
            }
        };

        _chatConversations = new List<ChatConversationDto>
        {
            new()
            {
                Id = 1,
                ClientId = 1,
                Client = "ABC Corporation",
                Subject = "Q3 GST Filing Question",
                LastMessagePreview = "Thanks for the clarification",
                TimestampDisplay = "2 hours ago",
                Status = "open",
                UnreadCount = 0,
                AssignedTo = "John Doe"
            },
            new()
            {
                Id = 2,
                ClientId = 2,
                Client = "XYZ Trading",
                Subject = "Payment Receipt Request",
                LastMessagePreview = "I need a copy of the receipt",
                TimestampDisplay = "5 hours ago",
                Status = "pending",
                UnreadCount = 2,
                AssignedTo = "Jane Smith"
            },
            new()
            {
                Id = 3,
                ClientId = 3,
                Client = "Tech Solutions",
                Subject = "Document Upload Issue",
                LastMessagePreview = "The system won't let me upload",
                TimestampDisplay = "1 day ago",
                Status = "urgent",
                UnreadCount = 1,
                AssignedTo = "Unassigned"
            },
            new()
            {
                Id = 4,
                ClientId = 4,
                Client = "Global Imports",
                Subject = "Excise Duty Clarification",
                LastMessagePreview = "What rate should we apply?",
                TimestampDisplay = "2 days ago",
                Status = "open",
                UnreadCount = 0,
                AssignedTo = "Mike Brown"
            }
        };

        var conversationOneMessages = new List<ChatMessageDto>
        {
            new() { Id = 1, SenderType = "Client", SenderName = "Sarah Johnson", Content = "Hi, I have a question about the Q3 GST filing. Can you help?", SentAt = today.AddHours(-6).AddMinutes(-30), IsInternal = false },
            new() { Id = 2, SenderType = "Staff", SenderName = "John Doe", Content = "Of course! What would you like to know?", SentAt = today.AddHours(-6).AddMinutes(-28), IsInternal = false },
            new() { Id = 3, SenderType = "Client", SenderName = "Sarah Johnson", Content = "I'm not sure how to handle the input tax credit for imported equipment.", SentAt = today.AddHours(-6).AddMinutes(-25), IsInternal = false },
            new() { Id = 4, SenderType = "Staff", SenderName = "John Doe", Content = "[Internal Note] Need to check the current excise regulations for equipment imports", SentAt = today.AddHours(-6).AddMinutes(-24), IsInternal = true },
            new() { Id = 5, SenderType = "Staff", SenderName = "John Doe", Content = "For imported equipment, you can claim input tax credit on the GST paid at the time of import. You'll need to attach the customs declaration and payment receipt to your filing.", SentAt = today.AddHours(-6).AddMinutes(-20), IsInternal = false },
            new() { Id = 6, SenderType = "Client", SenderName = "Sarah Johnson", Content = "Thanks for the clarification! That helps a lot.", SentAt = today.AddHours(-6).AddMinutes(-15), IsInternal = false }
        };

        _chatMessagesByConversation = new Dictionary<int, IReadOnlyList<ChatMessageDto>>
        {
            [1] = conversationOneMessages,
            [2] = new List<ChatMessageDto>(),
            [3] = new List<ChatMessageDto>(),
            [4] = new List<ChatMessageDto>()
        };

        _adminUsers = new List<AdminUserDto>
        {
            new() { Id = 1, Name = "John Doe", Email = "john@bettsfirm.com", Role = "Admin", Status = "Active" },
            new() { Id = 2, Name = "Jane Smith", Email = "jane@bettsfirm.com", Role = "Staff", Status = "Active" },
            new() { Id = 3, Name = "Mike Brown", Email = "mike@bettsfirm.com", Role = "Staff", Status = "Active" },
            new() { Id = 4, Name = "Sarah Johnson", Email = "sarah@abc.com", Role = "Client", Status = "Active" }
        };

        _auditLogs = new List<AuditLogEntryDto>
        {
            new() { Id = 1, Timestamp = today.AddHours(-3).AddMinutes(-30), Actor = "John Doe", Role = "Admin", ActingFor = null, Action = "Updated GST filing for ABC Corporation", IpAddress = "192.168.1.100" },
            new() { Id = 2, Timestamp = today.AddHours(-5).AddMinutes(-45), Actor = "Jane Smith", Role = "Staff", ActingFor = "XYZ Trading", Action = "Uploaded financial statements", IpAddress = "192.168.1.101" },
            new() { Id = 3, Timestamp = today.AddHours(-7), Actor = "Mike Brown", Role = "Staff", ActingFor = null, Action = "Generated compliance report", IpAddress = "192.168.1.102" }
        };

        _taxRates = new List<TaxRateDto>
        {
            new() { Type = "Corporate Income Tax (CIT)", Rate = "30%", ApplicableTo = "Companies" },
            new() { Type = "Goods & Services Tax (GST)", Rate = "15%", ApplicableTo = "All taxable supplies" },
            new() { Type = "Minimum Alternative Tax (MAT)", Rate = "2%", ApplicableTo = "Gross revenue" },
            new() { Type = "PAYE", Rate = "Progressive", ApplicableTo = "Employees" }
        };

        _jobStatuses = new List<JobStatusDto>
        {
            new() { Name = "Reminder Scheduler", State = "Running", BadgeText = "Active", BadgeVariant = "success" },
            new() { Name = "KPI Recalculation", State = "Idle", BadgeText = "Scheduled", BadgeVariant = "outline" },
            new() { Name = "File Scanner", State = "Processing", BadgeText = "12 in queue", BadgeVariant = "warning" }
        };

        _filingWorkspace = new FilingWorkspaceDto
        {
            FilingId = "gst-q3-2025",
            ClientId = 1,
            ClientName = "ABC Corporation",
            Title = "GST Return - Q3 2025",
            TaxType = "GST",
            TaxPeriodOptions = new List<SelectionOptionDto>
            {
                new() { Value = "q3-2025", Label = "Q3 2025 (Jul-Sep)" },
                new() { Value = "q2-2025", Label = "Q2 2025 (Apr-Jun)" },
                new() { Value = "q1-2025", Label = "Q1 2025 (Jan-Mar)" }
            },
            SelectedTaxPeriod = "q3-2025",
            FilingStatusOptions = new List<SelectionOptionDto>
            {
                new() { Value = "draft", Label = "Draft" },
                new() { Value = "pending", Label = "Pending Review" },
                new() { Value = "submitted", Label = "Submitted" }
            },
            SelectedFilingStatus = "draft",
            TotalSales = 250_000m,
            TaxableSales = 250_000m,
            GstRate = 15m,
            OutputTax = 37_500m,
            InputTaxCredit = 15_000m,
            NetGstPayable = 22_500m,
            Notes = "All sales figures verified against bank statements and invoices.",
            Schedule = new List<FilingScheduleEntryDto>
            {
                new() { Id = 1, Description = "Sales Revenue", Amount = 250_000m, TaxableAmount = 250_000m },
                new() { Id = 2, Description = "Cost of Goods Sold", Amount = 150_000m, TaxableAmount = 0m },
                new() { Id = 3, Description = "Operating Expenses", Amount = 50_000m, TaxableAmount = 0m }
            },
            SupportingDocuments = new List<FilingDocumentDto>
            {
                new() { Id = 1, Name = "Financial Statements 2024", Version = 2, UploadedBy = "John Doe", UploadedAt = today.AddDays(-7) },
                new() { Id = 2, Name = "Bank Statements", Version = 1, UploadedBy = "Jane Smith", UploadedAt = today.AddDays(-10) },
                new() { Id = 3, Name = "Sales Records", Version = 3, UploadedBy = "John Doe", UploadedAt = today.AddDays(-5) }
            },
            History = new List<FilingHistoryEntryDto>
            {
                new() { Timestamp = today.AddDays(-4).AddHours(14).AddMinutes(30), User = "John Doe", Action = "Updated form data", Changes = "Revenue figures" },
                new() { Timestamp = today.AddDays(-5).AddHours(10).AddMinutes(15), User = "Jane Smith", Action = "Uploaded document", Changes = "Financial Statements v2" },
                new() { Timestamp = today.AddDays(-6).AddHours(16).AddMinutes(45), User = "John Doe", Action = "Created filing", Changes = "GST Return Q3 2025" }
            }
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? clientId, int upcomingDays)
    {
        try
        {
            var deadlines = await _deadlineService.GetUpcomingDeadlinesAsync(clientId, upcomingDays);
            var mappedDeadlines = deadlines
                .OrderBy(d => d.DueDate)
                .Take(5)
                .Select(d => new DashboardDeadlineDto
                {
                    Id = d.Id,
                    Client = string.IsNullOrWhiteSpace(d.ClientName) ? "N/A" : d.ClientName,
                    TaxType = string.IsNullOrWhiteSpace(d.TaxTypeName) ? "N/A" : d.TaxTypeName,
                    DueDate = d.DueDate,
                    Status = d.Status.ToString()
                })
                .ToList();

            return new DashboardSummaryDto
            {
                Metrics = _dashboardMetrics,
                FilingTrends = _dashboardTrend,
                ComplianceDistribution = _dashboardDistribution,
                UpcomingDeadlines = mappedDeadlines,
                RecentActivity = _dashboardActivity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to build dashboard summary");
            throw;
        }
    }

    public Task<IReadOnlyList<ClientSummaryDto>> GetClientsAsync()
    {
        return Task.FromResult(_clients);
    }

    public Task<IReadOnlyList<DocumentRecordDto>> GetDocumentsAsync()
    {
        return Task.FromResult(_documents);
    }

    public Task<PaymentsResponseDto> GetPaymentsAsync(int? clientId)
    {
        var items = _payments.AsEnumerable();

        if (clientId.HasValue)
        {
            items = items.Where(p => p.ClientId == clientId);
        }

        var filtered = items.ToList();

        var summary = new PaymentSummaryDto
        {
            Paid = filtered.Where(p => string.Equals(p.Status, "Paid", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
            Pending = filtered.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
            Overdue = filtered.Where(p => string.Equals(p.Status, "Overdue", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount)
        };

        var response = new PaymentsResponseDto
        {
            Items = filtered,
            Summary = summary
        };

        return Task.FromResult(response);
    }

    public Task<KpiSummaryDto> GetKpiSummaryAsync(int? clientId)
    {
        // For demo purposes we return the same metrics irrespective of the client filter.
        var summary = new KpiSummaryDto
        {
            InternalMetrics = _internalKpiMetrics,
            ClientMetrics = _clientKpiMetrics,
            MonthlyTrend = _kpiTrend,
            ClientPerformance = _kpiClientPerformance,
            PerformanceBreakdown = _kpiBreakdown
        };

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<ReportTypeDto>> GetReportTypesAsync()
    {
        return Task.FromResult(_reportTypes);
    }

    public Task<ReportFiltersDto> GetReportFiltersAsync()
    {
        return Task.FromResult(_reportFilters);
    }

    public Task<IReadOnlyList<ChatConversationDto>> GetChatConversationsAsync()
    {
        return Task.FromResult(_chatConversations);
    }

    public Task<IReadOnlyList<ChatMessageDto>> GetChatMessagesAsync(int conversationId)
    {
        if (_chatMessagesByConversation.TryGetValue(conversationId, out var messages))
        {
            return Task.FromResult(messages);
        }

        return Task.FromResult<IReadOnlyList<ChatMessageDto>>(Array.Empty<ChatMessageDto>());
    }

    public Task<IReadOnlyList<AdminUserDto>> GetAdminUsersAsync()
    {
        return Task.FromResult(_adminUsers);
    }

    public Task<IReadOnlyList<AuditLogEntryDto>> GetAuditLogsAsync()
    {
        return Task.FromResult(_auditLogs);
    }

    public Task<IReadOnlyList<TaxRateDto>> GetTaxRatesAsync()
    {
        return Task.FromResult(_taxRates);
    }

    public Task<IReadOnlyList<JobStatusDto>> GetJobStatusesAsync()
    {
        return Task.FromResult(_jobStatuses);
    }

    public Task<FilingWorkspaceDto?> GetActiveFilingAsync()
    {
        return Task.FromResult<FilingWorkspaceDto?>(_filingWorkspace);
    }
}
