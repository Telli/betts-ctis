namespace BettsTax.Data;

// Additional enums for KPI and new features - only enums not already defined in other files
public enum ComplianceLevel { Red = 0, Yellow = 1, Green = 2 }
public enum NotificationType 
{ 
    General = 0, 
    TaxReminder = 1, 
    PaymentConfirmation = 2, 
    DocumentRequest = 3, 
    ComplianceAlert = 4, 
    KPIAlert = 5,
    SystemMaintenance = 6
}
public enum DocumentType { TaxDocument, Receipt, Invoice, Statement, Other }
public enum DocumentStatus { Pending, Approved, Rejected, UnderReview }
public enum AuditAction { 
    Create, 
    Read, 
    Update, 
    Delete, 
    Login, 
    Logout, 
    Export, 
    Import,
    KPIRefresh,
    ThresholdUpdate
}
public enum UserRole { Client, Associate, Admin, SystemAdmin }
public enum ClientCategory { Large, Medium, Small, Micro }
public enum TaxpayerStatus { Active, Inactive, Suspended, Deregistered }

// Reporting system enums
public enum ReportType
{
    TaxFiling = 1,
    PaymentHistory = 2,
    Compliance = 3,
    ClientActivity = 4,
    FinancialSummary = 5,
    ComplianceAnalytics = 6,
    DocumentSubmission = 7,
    TaxCalendar = 8,
    ClientComplianceOverview = 9,
    Revenue = 10,
    CaseManagement = 11,
    EnhancedClientActivity = 12
}

public enum ReportFormat
{
    PDF = 1,
    Excel = 2,
    CSV = 3
}

// Payment retry system enums
public enum PaymentRetryStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5,
    InProgress = 6,
    Scheduled = 7
}

public enum DeadLetterStatus
{
    Pending = 0,
    InReview = 1,
    Resolved = 2,
    Abandoned = 3,
    Reprocessed = 4,
    Discarded = 5
}

public enum CircuitBreakerStatus
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

public enum PaymentFailureType
{
    NetworkError = 0,
    AuthenticationError = 1,
    InsufficientFunds = 2,
    InvalidPaymentMethod = 3,
    GatewayTimeout = 4,
    ServiceUnavailable = 5,
    ValidationError = 6,
    SystemError = 7,
    Unknown = 8,
    Permanent = 9
}

public enum ReportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

// Compliance monitoring enums - only enums not defined elsewhere
public enum DeadlinePriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum ComplianceAlertType 
{ 
    UpcomingDeadline = 0, 
    MissedDeadline = 1, 
    PenaltyIncurred = 2, 
    DocumentMissing = 3,
    ComplianceScoreDropped = 4,
    PaymentOverdue = 5,
    RiskLevelChanged = 6,
    UnpaidLiability = 7,
    DocumentationMissing = 8,
    ReviewRequired = 9,
    ExtensionRequested = 10,
    SystemAlert = 11,
    GstRegistration = 12
}
public enum CompliancePriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum ComplianceActionStatus { Open = 0, InProgress = 1, Completed = 2, Cancelled = 3, Overdue = 4 }
public enum ComplianceRiskImpact { Minimal = 0, Low = 1, Medium = 2, High = 3, Severe = 4 }

// Phase 3 Enhanced Workflow Automation System Enums
public enum WorkflowInstanceStatus
{
    NotStarted = 0,
    Running = 1,
    WaitingForApproval = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

public enum WorkflowStepInstanceStatus
{
    NotStarted = 0,
    Running = 1,
    WaitingForApproval = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    Cancelled = 6
}

public enum WorkflowTriggerType
{
    Manual = 0,
    Event = 1,
    Schedule = 2,
    Webhook = 3,
    FileWatch = 4
}

public enum WorkflowApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}