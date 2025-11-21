namespace BettsTax.Data.Models
{
    /// <summary>
    /// Configurable deadline rules for tax compliance
    /// Phase 3: Configurable Deadline Rules (recommended from Phase 2 analysis)
    /// </summary>
    public class DeadlineRuleConfiguration
    {
        public int DeadlineRuleConfigurationId { get; set; }
        
        /// <summary>
        /// Tax type this rule applies to
        /// </summary>
        public TaxType TaxType { get; set; }
        
        /// <summary>
        /// Rule name for identification
        /// </summary>
        public string RuleName { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the rule
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Number of days from trigger date to deadline
        /// </summary>
        public int DaysFromTrigger { get; set; }
        
        /// <summary>
        /// Trigger type (e.g., "PeriodEnd", "EmploymentStart", "DeliveryDate")
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to adjust for weekends (move to next business day)
        /// </summary>
        public bool AdjustForWeekends { get; set; } = true;
        
        /// <summary>
        /// Whether to adjust for public holidays
        /// </summary>
        public bool AdjustForHolidays { get; set; } = true;
        
        /// <summary>
        /// Statutory minimum days (cannot be reduced below this)
        /// </summary>
        public int? StatutoryMinimumDays { get; set; }
        
        /// <summary>
        /// Whether this is the default rule for the tax type
        /// </summary>
        public bool IsDefault { get; set; } = true;
        
        /// <summary>
        /// Whether this rule is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Effective date for this rule
        /// </summary>
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Expiry date for this rule (null = no expiry)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }
        
        /// <summary>
        /// Created by user ID
        /// </summary>
        public string CreatedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Created date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last updated by user ID
        /// </summary>
        public string? UpdatedById { get; set; }
        
        /// <summary>
        /// Last updated date
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public ApplicationUser? CreatedBy { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }
    }
    
    /// <summary>
    /// Client-specific deadline extensions
    /// Allows overriding default rules for specific clients
    /// </summary>
    public class ClientDeadlineExtension
    {
        public int ClientDeadlineExtensionId { get; set; }
        
        /// <summary>
        /// Client this extension applies to
        /// </summary>
        public int ClientId { get; set; }
        
        /// <summary>
        /// Tax type for the extension
        /// </summary>
        public TaxType TaxType { get; set; }
        
        /// <summary>
        /// Tax year this extension applies to (null = all years)
        /// </summary>
        public int? TaxYear { get; set; }
        
        /// <summary>
        /// Additional days beyond standard deadline
        /// </summary>
        public int ExtensionDays { get; set; }
        
        /// <summary>
        /// Reason for extension
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Approved by user ID
        /// </summary>
        public string ApprovedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Approval date
        /// </summary>
        public DateTime ApprovedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Extension expiry date
        /// </summary>
        public DateTime? ExpiryDate { get; set; }
        
        /// <summary>
        /// Whether this extension is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public Client? Client { get; set; }
        public ApplicationUser? ApprovedBy { get; set; }
    }
    
    /// <summary>
    /// Public holiday calendar for Sierra Leone
    /// Used to adjust deadlines when they fall on holidays
    /// </summary>
    public class PublicHoliday
    {
        public int PublicHolidayId { get; set; }
        
        /// <summary>
        /// Holiday name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Holiday date
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Year this holiday applies to
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// Whether this is a recurring annual holiday
        /// </summary>
        public bool IsRecurring { get; set; } = false;
        
        /// <summary>
        /// Month for recurring holidays (1-12)
        /// </summary>
        public int? RecurringMonth { get; set; }
        
        /// <summary>
        /// Day for recurring holidays (1-31)
        /// </summary>
        public int? RecurringDay { get; set; }
        
        /// <summary>
        /// Whether this is a national holiday (affects all deadlines)
        /// </summary>
        public bool IsNational { get; set; } = true;
        
        /// <summary>
        /// Description or notes
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Created by user ID
        /// </summary>
        public string CreatedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Created date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ApplicationUser? CreatedBy { get; set; }
    }
    
    /// <summary>
    /// Audit log for deadline rule changes
    /// </summary>
    public class DeadlineRuleAuditLog
    {
        public int DeadlineRuleAuditLogId { get; set; }
        
        /// <summary>
        /// Rule configuration ID (if applicable)
        /// </summary>
        public int? DeadlineRuleConfigurationId { get; set; }
        
        /// <summary>
        /// Client extension ID (if applicable)
        /// </summary>
        public int? ClientDeadlineExtensionId { get; set; }
        
        /// <summary>
        /// Holiday ID (if applicable)
        /// </summary>
        public int? PublicHolidayId { get; set; }
        
        /// <summary>
        /// Action performed (Created, Updated, Deleted, Activated, Deactivated)
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Old values (JSON)
        /// </summary>
        public string? OldValues { get; set; }
        
        /// <summary>
        /// New values (JSON)
        /// </summary>
        public string? NewValues { get; set; }
        
        /// <summary>
        /// User who made the change
        /// </summary>
        public string ChangedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Change timestamp
        /// </summary>
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Change reason/notes
        /// </summary>
        public string? Notes { get; set; }
        
        // Navigation properties
        public DeadlineRuleConfiguration? DeadlineRuleConfiguration { get; set; }
        public ClientDeadlineExtension? ClientDeadlineExtension { get; set; }
        public PublicHoliday? PublicHoliday { get; set; }
        public ApplicationUser? ChangedBy { get; set; }
    }
}
