namespace BettsTax.Core.DTOs;

public class KpiTrendDto
{
    public DateTime Date { get; set; }
    public decimal ClientComplianceRate { get; set; }
    public decimal TaxFilingTimeliness { get; set; }
    public decimal PaymentCompletionRate { get; set; }
    public decimal DocumentSubmissionCompliance { get; set; }
    public decimal ClientEngagementRate { get; set; }
    public decimal OnTimePaymentPercentage { get; set; }
    public decimal FilingTimelinessAverage { get; set; }
    public decimal DocumentReadinessRate { get; set; }
    public int TotalClients { get; set; }
}

public class ClientKpiDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal OnTimePaymentPercentage { get; set; }
    public decimal FilingTimelinessAverage { get; set; } // Days early (negative) or late (positive)
    public decimal DocumentReadinessRate { get; set; }
    public decimal EngagementScore { get; set; }
    public int CompletedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public DateTime LastActivity { get; set; }
    public int LoginCount30Days { get; set; }
    public int MeaningfulEvents30Days { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    // Additional properties for compatibility
    public decimal DocumentReadiness { get; set; }
    public decimal Engagement { get; set; }
    public decimal OverallScore { get; set; }
}

public class DocumentReadinessDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TotalRequired { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public decimal ReadinessRate { get; set; }
    public List<DocumentStatusBreakdownDto> Breakdown { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int Submitted { get; set; } // Number of documents submitted
    public int OnTime { get; set; } // Number of documents submitted on time
    public decimal ReadinessPercentage { get; set; } // Percentage of readiness
    public decimal OnTimePercentage { get; set; } // Percentage of on-time submissions
}

public class DocumentStatusBreakdownDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
}

public class ClientEngagementDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public decimal EngagementScore { get; set; }
    public int LoginCount30Days { get; set; }
    public int LoginCount90Days { get; set; }
    public int MeaningfulEvents30Days { get; set; }
    public int MeaningfulEvents90Days { get; set; }
    public DateTime LastLoginDate { get; set; }
    public DateTime LastActivity { get; set; }
    public List<string> RecentActivities { get; set; } = new();
    public bool IsActiveClient { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    // Additional properties for compatibility
    public int RecentPayments { get; set; }
    public int RecentDocuments { get; set; }
    public DateTime LastActivityDate { get; set; }
}

public class KpiAlertDto
{
    public int AlertId { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? ThresholdValue { get; set; }
    public decimal? ActualValue { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}

public class KpiPerformanceMetricsDto
{
    public TimeSpan ComputationTime { get; set; }
    public int CacheHitRate { get; set; }
    public int QueriesExecuted { get; set; }
    public long MemoryUsed { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
