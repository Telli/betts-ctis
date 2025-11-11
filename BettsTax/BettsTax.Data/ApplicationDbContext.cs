using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data.Models.Security;

namespace BettsTax.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<TaxYear> TaxYears { get; set; }
        public DbSet<TaxFiling> TaxFilings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        // Removed - using Security.AuditLog instead
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ClientInvitation> ClientInvitations { get; set; }
        public DbSet<ClientRegistration> ClientRegistrations { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<DocumentVerification> DocumentVerifications { get; set; }
        public DbSet<DocumentVerificationHistory> DocumentVerificationHistories { get; set; }
        public DbSet<DocumentRequirement> DocumentRequirements { get; set; }
        public DbSet<ClientDocumentRequirement> ClientDocumentRequirements { get; set; }
        public DbSet<ActivityTimeline> ActivityTimelines { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<MessageTemplate> MessageTemplates { get; set; }
        public DbSet<SmsNotification> SmsNotifications { get; set; }
        public DbSet<SmsTemplate> SmsTemplates { get; set; }
        public DbSet<SmsProviderConfig> SmsProviderConfigs { get; set; }
        public DbSet<SmsSchedule> SmsSchedules { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentProviderConfig> PaymentProviderConfigs { get; set; }
        public DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; }
        public DbSet<PaymentMethodConfig> PaymentMethodConfigs { get; set; }
        public DbSet<PaymentStatusMapping> PaymentStatusMappings { get; set; }
        public DbSet<ComplianceTracker> ComplianceTrackers { get; set; }
        public DbSet<CompliancePenalty> CompliancePenalties { get; set; }
        public DbSet<ComplianceAlert> ComplianceAlerts { get; set; }
        public DbSet<ComplianceAction> ComplianceActions { get; set; }
        public DbSet<PenaltyRule> PenaltyRules { get; set; }
        public DbSet<ComplianceInsight> ComplianceInsights { get; set; }
        public DbSet<DataExportHistory> DataExportHistories { get; set; }
        public DbSet<ExportTemplate> ExportTemplates { get; set; }
        public DbSet<ExportQueue> ExportQueues { get; set; }
        public DbSet<ExportAccessLog> ExportAccessLogs { get; set; }
        public DbSet<ExportStatistics> ExportStatistics { get; set; }
        
        // Associate Permission System
        public DbSet<AssociateClientPermission> AssociateClientPermissions { get; set; }
        public DbSet<AssociatePermissionTemplate> AssociatePermissionTemplates { get; set; }
        public DbSet<AssociatePermissionRule> AssociatePermissionRules { get; set; }
        public DbSet<DocumentShare> DocumentShares { get; set; }
        public DbSet<OnBehalfAction> OnBehalfActions { get; set; }
        public DbSet<AssociatePermissionAuditLog> AssociatePermissionAuditLogs { get; set; }

        // KPI System
        public DbSet<Models.KPIMetric> KPIMetrics { get; set; }
        public DbSet<Models.ComplianceScore> ComplianceScores { get; set; }
        
        // Tax System  
        public DbSet<Models.TaxRate> TaxRates { get; set; }
        public DbSet<Models.ExciseDutyRate> ExciseDutyRates { get; set; }
        public DbSet<Models.TaxPenaltyRule> TaxPenaltyRules { get; set; }
        public DbSet<Models.TaxAllowance> TaxAllowances { get; set; }
        
        // Reporting System
        public DbSet<Models.ReportRequest> ReportRequests { get; set; }
        public DbSet<Models.ReportTemplate> ReportTemplates { get; set; }

        // Compliance Monitoring System (Finance Act 2025)
        public DbSet<Models.ComplianceDeadline> ComplianceDeadlines { get; set; }
        public DbSet<Models.ComplianceAlert> ComplianceAlertsModels { get; set; }
        public DbSet<Models.ComplianceActionItem> ComplianceActionItems { get; set; }
        public DbSet<Models.FinanceAct2025Rule> FinanceAct2025Rules { get; set; }
        public DbSet<Models.ComplianceCalculation> ComplianceCalculations { get; set; }
        public DbSet<Models.PenaltyCalculation> PenaltyCalculations { get; set; }

        // Communication System DbSets
        public DbSet<Models.Conversation> Conversations { get; set; }
        public DbSet<Models.Message> ConversationMessages { get; set; }
        public DbSet<Models.ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Models.ConversationTag> ConversationTags { get; set; }
        public DbSet<Models.MessageRead> MessageReads { get; set; }
        public DbSet<Models.MessageReaction> MessageReactions { get; set; }
        public DbSet<Models.NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<Models.NotificationQueue> NotificationQueue { get; set; }
        public DbSet<Models.ChatRoom> ChatRooms { get; set; }
        public DbSet<Models.ChatRoomParticipant> ChatRoomParticipants { get; set; }
        public DbSet<Models.ChatMessage> ChatMessages { get; set; }
        public DbSet<Models.ChatRoomInvitation> ChatRoomInvitations { get; set; }
        public DbSet<Models.ChatMessageReaction> ChatMessageReactions { get; set; }
        public DbSet<Models.ChatMessageRead> ChatMessageReads { get; set; }

        // Payment Gateway System DbSets
        public DbSet<Models.PaymentGatewayConfig> PaymentGatewayConfigs { get; set; }
        public DbSet<Models.PaymentTransaction> PaymentGatewayTransactions { get; set; }

        // Phase 3: Enhanced Workflow Automation System - Additional entities only
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowStepInstance> WorkflowStepInstances { get; set; }
        public DbSet<WorkflowTrigger> WorkflowTriggers { get; set; }
        public DbSet<WorkflowApproval> WorkflowApprovals { get; set; }
        public DbSet<Models.PaymentTransactionLog> PaymentTransactionLogs { get; set; }
        public DbSet<Models.PaymentRefund> PaymentRefunds { get; set; }
        public DbSet<Models.PaymentWebhookLog> PaymentGatewayWebhookLogs { get; set; }
        public DbSet<Models.MobileMoneyProvider> MobileMoneyProviders { get; set; }
        public DbSet<Models.PaymentFraudRule> PaymentFraudRules { get; set; }

        // Security System DbSets
        public DbSet<UserMfaConfiguration> UserMfaConfigurations { get; set; }
        public DbSet<MfaChallenge> MfaChallenges { get; set; }
        public DbSet<Models.Security.AuditLog> AuditLogs { get; set; }
        public DbSet<Models.Security.SecurityEvent> SecurityEvents { get; set; }
        public DbSet<Models.Security.EncryptionKey> EncryptionKeys { get; set; }
        public DbSet<Models.Security.EncryptedData> EncryptedData { get; set; }
        public DbSet<Models.Security.SystemHealthCheck> SystemHealthChecks { get; set; }
        public DbSet<Models.Security.SecurityScan> SecurityScans { get; set; }

        // Payment Retry System (from previous implementation)
        public DbSet<Models.PaymentScheduledRetry> PaymentScheduledRetries { get; set; }
        public DbSet<Models.PaymentRetryAttempt> PaymentRetryAttempts { get; set; }
        public DbSet<Models.PaymentFailureRecord> PaymentFailureRecords { get; set; }
        public DbSet<Models.PaymentDeadLetterQueue> PaymentDeadLetterQueue { get; set; }

        // KPI System DbSets
        public DbSet<Models.KpiSnapshot> KpiSnapshots { get; set; }
        public DbSet<Models.ClientKpiMetrics> ClientKpiMetrics { get; set; }
        public DbSet<Models.KpiAlert> KpiAlerts { get; set; }

        // Case Management DbSets
        public DbSet<Models.CaseIssue> CaseIssues { get; set; }
        public DbSet<Models.CaseComment> CaseComments { get; set; }
        public DbSet<Models.CaseAttachment> CaseAttachments { get; set; }

        // Compliance History DbSets
        public DbSet<Models.ComplianceHistory> ComplianceHistories { get; set; }
        public DbSet<Models.ComplianceHistoryEvent> ComplianceHistoryEvents { get; set; }

        // Workflow Automation System DbSets
        public DbSet<Models.Workflow> Workflows { get; set; }
        public DbSet<Models.WorkflowExecution> WorkflowExecutions { get; set; }
        public DbSet<Models.WorkflowTemplate> WorkflowTemplates { get; set; }
        public DbSet<Models.WorkflowRule> WorkflowRules { get; set; }

        // Tax Authority Integration DbSets
        public DbSet<TaxAuthoritySubmission> TaxAuthoritySubmissions { get; set; }
        public DbSet<TaxAuthorityStatusCheck> TaxAuthorityStatusChecks { get; set; }

        // Accounting Integration System DbSets
        public DbSet<AccountingConnection> AccountingConnections { get; set; }
        public DbSet<AccountingMapping> AccountingMappings { get; set; }
        public DbSet<AccountingSyncHistory> AccountingSyncHistory { get; set; }
        public DbSet<AccountingTransactionMapping> AccountingTransactionMappings { get; set; }

        // Advanced Workflow Rule System DbSets
        public DbSet<WorkflowRule> WorkflowRuleEntities { get; set; }
        public DbSet<WorkflowCondition> WorkflowConditions { get; set; }
        public DbSet<WorkflowAction> WorkflowActions { get; set; }
        public DbSet<WorkflowTemplate> WorkflowRuleTemplates { get; set; }
        public DbSet<WorkflowTemplateReview> WorkflowTemplateReviews { get; set; }
        public DbSet<WorkflowExecutionHistory> WorkflowExecutionHistories { get; set; }
        public DbSet<WorkflowActionExecution> WorkflowActionExecutions { get; set; }
        public DbSet<WorkflowRuleMetrics> WorkflowRuleMetrics { get; set; }
        public DbSet<WorkflowSystemConfiguration> WorkflowSystemConfigurations { get; set; }
        public DbSet<WorkflowExecutionQueue> WorkflowExecutionQueues { get; set; }

        // Enhanced Workflow Automation System DbSets
        public DbSet<WebhookRegistration> WebhookRegistrations { get; set; }
        public DbSet<WebhookStatistics> WebhookStatistics { get; set; }
        public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; set; }
        public DbSet<TemplateReview> TemplateReviews { get; set; }
        public DbSet<TemplateStatistics> TemplateStatistics { get; set; }
        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<WorkflowStep> WorkflowSteps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure Client uses ClientId as the primary key (not the generic Id)
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ClientId);

            modelBuilder.Entity<Client>()
                .Property(c => c.ClientId)
                .ValueGeneratedOnAdd();

            // Configure Client-User relationships
            modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithOne(u => u.ClientProfile)
                .HasForeignKey<Client>(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.AssignedAssociate)
                .WithMany(u => u.AssignedClients)
                .HasForeignKey(c => c.AssignedAssociateId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure TaxFiling relationships
            modelBuilder.Entity<TaxFiling>()
                .HasOne(tf => tf.Client)
                .WithMany()
                .HasForeignKey(tf => tf.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxFiling>()
                .HasOne(tf => tf.SubmittedBy)
                .WithMany(u => u.SubmittedTaxFilings)
                .HasForeignKey(tf => tf.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxFiling>()
                .HasOne(tf => tf.ReviewedBy)
                .WithMany(u => u.ReviewedTaxFilings)
                .HasForeignKey(tf => tf.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Payment relationships
            // Ensure Payment uses PaymentId as the primary key (not the generic Id)
            modelBuilder.Entity<Payment>()
                .HasKey(p => p.PaymentId);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.TaxFiling)
                .WithMany(tf => tf.Payments)
                .HasForeignKey(p => p.TaxFilingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ApprovedBy)
                .WithMany()
                .HasForeignKey(p => p.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payment>()
                .Ignore(p => p.ApprovedBy);

            // Configure Document relationships
            modelBuilder.Entity<Document>()
                .HasOne(d => d.TaxFiling)
                .WithMany(tf => tf.Documents)
                .HasForeignKey(d => d.TaxFilingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DocumentVersion relationships
            modelBuilder.Entity<DocumentVersion>()
                .HasOne(dv => dv.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(dv => dv.UploadedBy)
                .WithMany()
                .HasForeignKey(dv => dv.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(dv => dv.DeletedBy)
                .WithMany()
                .HasForeignKey(dv => dv.DeletedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DocumentVersion indexes
            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(dv => new { dv.DocumentId, dv.VersionNumber })
                .IsUnique()
                .HasDatabaseName("IX_DocumentVersion_Document_Version");

            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(dv => dv.UploadedAt)
                .HasDatabaseName("IX_DocumentVersion_UploadedAt");

            modelBuilder.Entity<DocumentVersion>()
                .HasIndex(dv => dv.IsDeleted)
                .HasDatabaseName("IX_DocumentVersion_IsDeleted");

            // Configure default for current version number
            modelBuilder.Entity<Document>()
                .Property(d => d.CurrentVersionNumber)
                .HasDefaultValue(0);

            // Configure AuditLog relationships
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision for financial fields
            modelBuilder.Entity<TaxFiling>()
                .Property(tf => tf.TaxLiability)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // Extended Payment field configurations
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ReconciledByUser)
                .WithMany()
                .HasForeignKey(p => p.ReconciledBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ReviewedByUser)
                .WithMany()
                .HasForeignKey(p => p.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Extended Payment decimal field precision
            modelBuilder.Entity<Payment>()
                .Property(p => p.InterestAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PenaltyAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.FeeAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.TaxAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.ExchangeRate)
                .HasPrecision(18, 6);

            modelBuilder.Entity<Payment>()
                .Property(p => p.OriginalAmount)
                .HasPrecision(18, 2);

            // Extended Payment string field lengths
            modelBuilder.Entity<Payment>()
                .Property(p => p.Currency)
                .HasMaxLength(3);

            modelBuilder.Entity<Payment>()
                .Property(p => p.OriginalCurrency)
                .HasMaxLength(3);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentBatchId)
                .HasMaxLength(100);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentCategory)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentSubCategory)
                .HasMaxLength(100);

            modelBuilder.Entity<Payment>()
                .Property(p => p.ReconciliationReference)
                .HasMaxLength(200);

            modelBuilder.Entity<Payment>()
                .Property(p => p.BankStatementReference)
                .HasMaxLength(200);

            modelBuilder.Entity<Payment>()
                .Property(p => p.RetryStrategy)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .Property(p => p.NotificationMethod)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentHash)
                .HasMaxLength(256);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentChannel)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentSource)
                .HasMaxLength(50);

            modelBuilder.Entity<Payment>()
                .Property(p => p.IpAddress)
                .HasMaxLength(45);

            modelBuilder.Entity<Payment>()
                .Property(p => p.DeviceFingerprint)
                .HasMaxLength(500);

            modelBuilder.Entity<Payment>()
                .Property(p => p.UserAgent)
                .HasMaxLength(1000);

            modelBuilder.Entity<Payment>()
                .Property(p => p.ExtendedMetadata)
                .HasMaxLength(4000);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Tags)
                .HasMaxLength(1000);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CustomField1)
                .HasMaxLength(500);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CustomField2)
                .HasMaxLength(500);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CustomField3)
                .HasMaxLength(500);

            modelBuilder.Entity<Client>()
                .Property(c => c.AnnualTurnover)
                .HasPrecision(18, 2);

            // Configure ClientInvitation relationships
            modelBuilder.Entity<ClientInvitation>()
                .HasOne(ci => ci.InvitedByAssociate)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(ci => ci.InvitedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ClientRegistration relationships
            modelBuilder.Entity<ClientRegistration>()
                .HasOne(cr => cr.CompletedClient)
                .WithMany()
                .HasForeignKey(cr => cr.CompletedClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes for performance
            modelBuilder.Entity<TaxFiling>()
                .HasIndex(tf => new { tf.ClientId, tf.TaxYear, tf.TaxType })
                .HasDatabaseName("IX_TaxFiling_Client_Year_Type");

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.StoragePath)
                .HasDatabaseName("IX_Document_StoragePath");

            // Configure indexes for enrollment entities
            modelBuilder.Entity<ClientInvitation>()
                .HasIndex(ci => ci.Email)
                .HasDatabaseName("IX_ClientInvitation_Email");

            modelBuilder.Entity<ClientInvitation>()
                .HasIndex(ci => ci.Token)
                .IsUnique()
                .HasDatabaseName("IX_ClientInvitation_Token");

            modelBuilder.Entity<ClientRegistration>()
                .HasIndex(cr => cr.Email)
                .HasDatabaseName("IX_ClientRegistration_Email");

            modelBuilder.Entity<ClientRegistration>()
                .HasIndex(cr => cr.RegistrationToken)
                .IsUnique()
                .HasDatabaseName("IX_ClientRegistration_Token");

            // Configure SystemSetting relationships
            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.UpdatedByUser)
                .WithMany()
                .HasForeignKey(ss => ss.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SystemSetting indexes
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => ss.Key)
                .IsUnique()
                .HasDatabaseName("IX_SystemSetting_Key");

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => ss.Category)
                .HasDatabaseName("IX_SystemSetting_Category");

            // Configure DocumentVerification relationships
            modelBuilder.Entity<DocumentVerification>()
                .HasOne(dv => dv.Document)
                .WithOne(d => d.DocumentVerification)
                .HasForeignKey<DocumentVerification>(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentVerification>()
                .HasOne(dv => dv.ReviewedBy)
                .WithMany()
                .HasForeignKey(dv => dv.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentVerification>()
                .HasOne(dv => dv.StatusChangedBy)
                .WithMany()
                .HasForeignKey(dv => dv.StatusChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentVerification>()
                .HasOne(dv => dv.TaxFiling)
                .WithMany()
                .HasForeignKey(dv => dv.TaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure DocumentVerificationHistory relationships
            modelBuilder.Entity<DocumentVerificationHistory>()
                .HasOne(dvh => dvh.Document)
                .WithMany()
                .HasForeignKey(dvh => dvh.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentVerificationHistory>()
                .HasOne(dvh => dvh.ChangedBy)
                .WithMany()
                .HasForeignKey(dvh => dvh.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DocumentRequirement
            modelBuilder.Entity<DocumentRequirement>()
                .HasIndex(dr => dr.RequirementCode)
                .IsUnique()
                .HasDatabaseName("IX_DocumentRequirement_Code");

            modelBuilder.Entity<DocumentRequirement>()
                .HasIndex(dr => new { dr.ApplicableTaxType, dr.ApplicableTaxpayerCategory })
                .HasDatabaseName("IX_DocumentRequirement_TaxType_Category");

            // Configure ClientDocumentRequirement relationships
            modelBuilder.Entity<ClientDocumentRequirement>()
                .HasOne(cdr => cdr.Client)
                .WithMany()
                .HasForeignKey(cdr => cdr.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientDocumentRequirement>()
                .HasOne(cdr => cdr.TaxFiling)
                .WithMany()
                .HasForeignKey(cdr => cdr.TaxFilingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientDocumentRequirement>()
                .HasOne(cdr => cdr.DocumentRequirement)
                .WithMany()
                .HasForeignKey(cdr => cdr.DocumentRequirementId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientDocumentRequirement>()
                .HasOne(cdr => cdr.RequestedBy)
                .WithMany()
                .HasForeignKey(cdr => cdr.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for document verification
            modelBuilder.Entity<DocumentVerification>()
                .HasIndex(dv => new { dv.DocumentId })
                .IsUnique()
                .HasDatabaseName("IX_DocumentVerification_DocumentId");

            modelBuilder.Entity<DocumentVerification>()
                .HasIndex(dv => dv.Status)
                .HasDatabaseName("IX_DocumentVerification_Status");

            modelBuilder.Entity<ClientDocumentRequirement>()
                .HasIndex(cdr => new { cdr.ClientId, cdr.TaxFilingId })
                .HasDatabaseName("IX_ClientDocumentRequirement_Client_Filing");

            // Configure ActivityTimeline relationships
            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.Client)
                .WithMany()
                .HasForeignKey(at => at.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.User)
                .WithMany()
                .HasForeignKey(at => at.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.TargetUser)
                .WithMany()
                .HasForeignKey(at => at.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.Document)
                .WithMany()
                .HasForeignKey(at => at.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.TaxFiling)
                .WithMany()
                .HasForeignKey(at => at.TaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActivityTimeline>()
                .HasOne(at => at.Payment)
                .WithMany()
                .HasForeignKey(at => at.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes for ActivityTimeline
            modelBuilder.Entity<ActivityTimeline>()
                .HasIndex(at => at.ClientId)
                .HasDatabaseName("IX_ActivityTimeline_ClientId");

            modelBuilder.Entity<ActivityTimeline>()
                .HasIndex(at => at.ActivityDate)
                .HasDatabaseName("IX_ActivityTimeline_ActivityDate");

            modelBuilder.Entity<ActivityTimeline>()
                .HasIndex(at => new { at.ClientId, at.ActivityDate })
                .HasDatabaseName("IX_ActivityTimeline_Client_Date");

            modelBuilder.Entity<ActivityTimeline>()
                .HasIndex(at => at.ActivityType)
                .HasDatabaseName("IX_ActivityTimeline_Type");

            modelBuilder.Entity<ActivityTimeline>()
                .HasIndex(at => at.Category)
                .HasDatabaseName("IX_ActivityTimeline_Category");

            // Configure Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Client)
                .WithMany()
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.TaxFiling)
                .WithMany()
                .HasForeignKey(m => m.TaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Document)
                .WithMany()
                .HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure MessageAttachment relationships
            modelBuilder.Entity<MessageAttachment>()
                .HasOne(ma => ma.Message)
                .WithMany()
                .HasForeignKey(ma => ma.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageAttachment>()
                .HasOne(ma => ma.Document)
                .WithMany()
                .HasForeignKey(ma => ma.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Message indexes
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.RecipientId })
                .HasDatabaseName("IX_Message_Sender_Recipient");

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ClientId)
                .HasDatabaseName("IX_Message_ClientId");

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.Status)
                .HasDatabaseName("IX_Message_Status");

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SentDate)
                .HasDatabaseName("IX_Message_SentDate");

            // Configure MessageTemplate indexes
            modelBuilder.Entity<MessageTemplate>()
                .HasIndex(mt => mt.TemplateCode)
                .IsUnique()
                .HasDatabaseName("IX_MessageTemplate_Code");

            modelBuilder.Entity<MessageTemplate>()
                .HasIndex(mt => mt.Category)
                .HasDatabaseName("IX_MessageTemplate_Category");

            // Configure SmsNotification relationships
            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.User)
                .WithMany()
                .HasForeignKey(sn => sn.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.Client)
                .WithMany()
                .HasForeignKey(sn => sn.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.TaxFiling)
                .WithMany()
                .HasForeignKey(sn => sn.TaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.Payment)
                .WithMany()
                .HasForeignKey(sn => sn.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.Document)
                .WithMany()
                .HasForeignKey(sn => sn.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SmsNotification>()
                .HasOne(sn => sn.SmsTemplate)
                .WithMany()
                .HasForeignKey(sn => sn.SmsTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure SmsNotification indexes
            modelBuilder.Entity<SmsNotification>()
                .HasIndex(sn => sn.PhoneNumber)
                .HasDatabaseName("IX_SmsNotification_PhoneNumber");

            modelBuilder.Entity<SmsNotification>()
                .HasIndex(sn => sn.Status)
                .HasDatabaseName("IX_SmsNotification_Status");

            modelBuilder.Entity<SmsNotification>()
                .HasIndex(sn => sn.CreatedDate)
                .HasDatabaseName("IX_SmsNotification_CreatedDate");

            modelBuilder.Entity<SmsNotification>()
                .HasIndex(sn => new { sn.Status, sn.ScheduledDate })
                .HasDatabaseName("IX_SmsNotification_Status_Scheduled");

            // Configure SmsTemplate
            modelBuilder.Entity<SmsTemplate>()
                .HasIndex(st => st.TemplateCode)
                .IsUnique()
                .HasDatabaseName("IX_SmsTemplate_Code");

            modelBuilder.Entity<SmsTemplate>()
                .HasIndex(st => st.Type)
                .HasDatabaseName("IX_SmsTemplate_Type");

            // Configure SmsProviderConfig
            modelBuilder.Entity<SmsProviderConfig>()
                .HasIndex(spc => spc.Provider)
                .IsUnique()
                .HasDatabaseName("IX_SmsProviderConfig_Provider");

            // Configure SmsSchedule
            modelBuilder.Entity<SmsSchedule>()
                .HasOne(ss => ss.SmsTemplate)
                .WithMany()
                .HasForeignKey(ss => ss.SmsTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SmsSchedule>()
                .HasIndex(ss => ss.IsActive)
                .HasDatabaseName("IX_SmsSchedule_IsActive");

            // Configure decimal precision for SMS costs
            modelBuilder.Entity<SmsNotification>()
                .Property(sn => sn.Cost)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SmsProviderConfig>()
                .Property(spc => spc.CostPerSms)
                .HasPrecision(18, 4);

            // Configure Associate Permission System relationships
            ConfigureAssociatePermissionSystem(modelBuilder);

            // Configure KPI System relationships
            ConfigureKPISystem(modelBuilder);
            
            // Configure Reporting System relationships
            ConfigureReportingSystem(modelBuilder);
            
            // Configure Compliance Monitoring System relationships
            ConfigureComplianceMonitoringSystem(modelBuilder);
        }

        private void ConfigureAssociatePermissionSystem(ModelBuilder modelBuilder)
        {
            // Configure AssociateClientPermission relationships
            modelBuilder.Entity<AssociateClientPermission>()
                .HasOne(acp => acp.Associate)
                .WithMany(u => u.AssociatePermissions)
                .HasForeignKey(acp => acp.AssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssociateClientPermission>()
                .HasOne(acp => acp.Client)
                .WithMany()
                .HasForeignKey(acp => acp.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssociateClientPermission>()
                .HasOne(acp => acp.GrantedByAdmin)
                .WithMany(u => u.GrantedPermissions)
                .HasForeignKey(acp => acp.GrantedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AssociatePermissionTemplate relationships
            modelBuilder.Entity<AssociatePermissionTemplate>()
                .HasOne(apt => apt.CreatedByAdmin)
                .WithMany(u => u.CreatedTemplates)
                .HasForeignKey(apt => apt.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AssociatePermissionRule relationships
            modelBuilder.Entity<AssociatePermissionRule>()
                .HasOne(apr => apr.Template)
                .WithMany(apt => apt.Rules)
                .HasForeignKey(apr => apr.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TaxFiling associate delegation relationships
            modelBuilder.Entity<TaxFiling>()
                .HasOne(tf => tf.CreatedByAssociate)
                .WithMany(u => u.CreatedTaxFilings)
                .HasForeignKey(tf => tf.CreatedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxFiling>()
                .HasOne(tf => tf.LastModifiedByAssociate)
                .WithMany(u => u.ModifiedTaxFilings)
                .HasForeignKey(tf => tf.LastModifiedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Payment associate delegation relationships
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ProcessedByAssociate)
                .WithMany(u => u.ProcessedPayments)
                .HasForeignKey(p => p.ProcessedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ApprovedByAssociate)
                .WithMany(u => u.AssociateApprovedPayments)
                .HasForeignKey(p => p.ApprovedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Document associate delegation relationships
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedByAssociate)
                .WithMany(u => u.AssociateUploadedDocuments)
                .HasForeignKey(d => d.UploadedByAssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DocumentShare relationships
            modelBuilder.Entity<DocumentShare>()
                .HasOne(ds => ds.Document)
                .WithMany(d => d.SharedWith)
                .HasForeignKey(ds => ds.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentShare>()
                .HasOne(ds => ds.SharedWithUser)
                .WithMany(u => u.ReceivedDocuments)
                .HasForeignKey(ds => ds.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentShare>()
                .HasOne(ds => ds.SharedByUser)
                .WithMany(u => u.SharedDocuments)
                .HasForeignKey(ds => ds.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure OnBehalfAction relationships
            modelBuilder.Entity<OnBehalfAction>()
                .HasOne(oba => oba.Associate)
                .WithMany(u => u.OnBehalfActions)
                .HasForeignKey(oba => oba.AssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OnBehalfAction>()
                .HasOne(oba => oba.Client)
                .WithMany()
                .HasForeignKey(oba => oba.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AssociatePermissionAuditLog relationships
            modelBuilder.Entity<AssociatePermissionAuditLog>()
                .HasOne(apal => apal.Associate)
                .WithMany(u => u.PermissionAuditLogs)
                .HasForeignKey(apal => apal.AssociateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssociatePermissionAuditLog>()
                .HasOne(apal => apal.Client)
                .WithMany()
                .HasForeignKey(apal => apal.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AssociatePermissionAuditLog>()
                .HasOne(apal => apal.ChangedByAdmin)
                .WithMany(u => u.AdminPermissionChanges)
                .HasForeignKey(apal => apal.ChangedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for performance
            modelBuilder.Entity<AssociateClientPermission>()
                .HasIndex(acp => new { acp.AssociateId, acp.ClientId, acp.PermissionArea })
                .HasDatabaseName("IX_AssociatePermission_Associate_Client_Area");

            modelBuilder.Entity<AssociateClientPermission>()
                .HasIndex(acp => acp.ExpiryDate)
                .HasDatabaseName("IX_AssociatePermission_ExpiryDate");

            modelBuilder.Entity<OnBehalfAction>()
                .HasIndex(oba => new { oba.AssociateId, oba.ClientId, oba.ActionDate })
                .HasDatabaseName("IX_OnBehalfAction_Associate_Client_Date");

            modelBuilder.Entity<OnBehalfAction>()
                .HasIndex(oba => new { oba.EntityType, oba.EntityId })
                .HasDatabaseName("IX_OnBehalfAction_Entity");

            modelBuilder.Entity<DocumentShare>()
                .HasIndex(ds => new { ds.SharedWithUserId, ds.IsActive })
                .HasDatabaseName("IX_DocumentShare_User_Active");

            modelBuilder.Entity<AssociatePermissionAuditLog>()
                .HasIndex(apal => new { apal.AssociateId, apal.ChangeDate })
                .HasDatabaseName("IX_PermissionAudit_Associate_Date");

            // Configure decimal precision for permission-related amounts
            modelBuilder.Entity<AssociateClientPermission>()
                .Property(acp => acp.AmountThreshold)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AssociatePermissionRule>()
                .Property(apr => apr.AmountThreshold)
                .HasPrecision(18, 2);
        }

        private void ConfigureKPISystem(ModelBuilder modelBuilder)
        {
            // Configure KPIMetric relationships
            modelBuilder.Entity<Models.KPIMetric>()
                .HasOne(km => km.Client)
                .WithMany()
                .HasForeignKey(km => km.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ComplianceScore relationships
            modelBuilder.Entity<Models.ComplianceScore>()
                .HasOne(cs => cs.Client)
                .WithMany()
                .HasForeignKey(cs => cs.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes for KPI performance
            modelBuilder.Entity<Models.KPIMetric>()
                .HasIndex(km => new { km.MetricName, km.Period, km.CalculatedAt })
                .HasDatabaseName("IX_KPIMetric_Name_Period_Date");

            modelBuilder.Entity<Models.KPIMetric>()
                .HasIndex(km => km.ClientId)
                .HasDatabaseName("IX_KPIMetric_ClientId");

            modelBuilder.Entity<Models.KPIMetric>()
                .HasIndex(km => km.Category)
                .HasDatabaseName("IX_KPIMetric_Category");

            modelBuilder.Entity<Models.ComplianceScore>()
                .HasIndex(cs => new { cs.ClientId, cs.TaxYear })
                .IsUnique()
                .HasDatabaseName("IX_ComplianceScore_Client_Year");

            modelBuilder.Entity<Models.ComplianceScore>()
                .HasIndex(cs => cs.Level)
                .HasDatabaseName("IX_ComplianceScore_Level");

            modelBuilder.Entity<Models.ComplianceScore>()
                .HasIndex(cs => cs.CalculatedAt)
                .HasDatabaseName("IX_ComplianceScore_CalculatedAt");

            // Configure decimal precision for KPI values
            modelBuilder.Entity<Models.KPIMetric>()
                .Property(km => km.Value)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Models.ComplianceScore>()
                .Property(cs => cs.OverallScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceScore>()
                .Property(cs => cs.FilingScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceScore>()
                .Property(cs => cs.PaymentScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceScore>()
                .Property(cs => cs.DocumentScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceScore>()
                .Property(cs => cs.TimelinessScore)
                .HasPrecision(5, 2);
        }
        
        private void ConfigureReportingSystem(ModelBuilder modelBuilder)
        {
            // Configure ReportRequest relationships
            modelBuilder.Entity<Models.ReportRequest>()
                .HasOne(rr => rr.RequestedByUser)
                .WithMany()
                .HasForeignKey(rr => rr.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for reporting performance
            modelBuilder.Entity<Models.ReportRequest>()
                .HasIndex(rr => rr.RequestId)
                .IsUnique()
                .HasDatabaseName("IX_ReportRequest_RequestId");

            modelBuilder.Entity<Models.ReportRequest>()
                .HasIndex(rr => new { rr.RequestedByUserId, rr.RequestedAt })
                .HasDatabaseName("IX_ReportRequest_User_Date");

            modelBuilder.Entity<Models.ReportRequest>()
                .HasIndex(rr => rr.Status)
                .HasDatabaseName("IX_ReportRequest_Status");

            modelBuilder.Entity<Models.ReportRequest>()
                .HasIndex(rr => new { rr.Type, rr.RequestedAt })
                .HasDatabaseName("IX_ReportRequest_Type_Date");

            modelBuilder.Entity<Models.ReportRequest>()
                .HasIndex(rr => rr.ExpiresAt)
                .HasDatabaseName("IX_ReportRequest_ExpiresAt");
        }
        
        private void ConfigureComplianceMonitoringSystem(ModelBuilder modelBuilder)
        {
            // Configure ComplianceDeadline relationships
            modelBuilder.Entity<Models.ComplianceDeadline>()
                .HasOne(cd => cd.Client)
                .WithMany()
                .HasForeignKey(cd => cd.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceDeadline>()
                .HasOne(cd => cd.CompletedByUser)
                .WithMany()
                .HasForeignKey(cd => cd.CompletedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ComplianceAlert relationships
            modelBuilder.Entity<Models.ComplianceAlert>()
                .HasOne(ca => ca.Client)
                .WithMany()
                .HasForeignKey(ca => ca.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceAlert>()
                .HasOne(ca => ca.ResolvedByUser)
                .WithMany()
                .HasForeignKey(ca => ca.ResolvedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ComplianceActionItem relationships
            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasOne(cai => cai.Client)
                .WithMany()
                .HasForeignKey(cai => cai.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasOne(cai => cai.AssignedToUser)
                .WithMany()
                .HasForeignKey(cai => cai.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasOne(cai => cai.CreatedByUser)
                .WithMany()
                .HasForeignKey(cai => cai.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ComplianceCalculation relationships
            modelBuilder.Entity<Models.ComplianceCalculation>()
                .HasOne(cc => cc.Client)
                .WithMany()
                .HasForeignKey(cc => cc.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .HasOne(cc => cc.CalculatedByUser)
                .WithMany()
                .HasForeignKey(cc => cc.CalculatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PenaltyCalculation relationships
            modelBuilder.Entity<Models.PenaltyCalculation>()
                .HasOne(pc => pc.Client)
                .WithMany()
                .HasForeignKey(pc => pc.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .HasOne(pc => pc.ApplicableRule)
                .WithMany()
                .HasForeignKey(pc => pc.ApplicableRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .HasOne(pc => pc.CalculatedByUser)
                .WithMany()
                .HasForeignKey(pc => pc.CalculatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes for compliance monitoring performance
            modelBuilder.Entity<Models.ComplianceDeadline>()
                .HasIndex(cd => new { cd.ClientId, cd.DueDate })
                .HasDatabaseName("IX_ComplianceDeadline_Client_DueDate");

            modelBuilder.Entity<Models.ComplianceDeadline>()
                .HasIndex(cd => cd.TaxType)
                .HasDatabaseName("IX_ComplianceDeadline_TaxType");

            modelBuilder.Entity<Models.ComplianceDeadline>()
                .HasIndex(cd => cd.Status)
                .HasDatabaseName("IX_ComplianceDeadline_Status");

            modelBuilder.Entity<Models.ComplianceAlert>()
                .HasIndex(ca => new { ca.ClientId, ca.IsResolved })
                .HasDatabaseName("IX_ComplianceAlert_Client_Resolved");

            modelBuilder.Entity<Models.ComplianceAlert>()
                .HasIndex(ca => ca.Severity)
                .HasDatabaseName("IX_ComplianceAlert_Severity");

            modelBuilder.Entity<Models.ComplianceAlert>()
                .HasIndex(ca => ca.AlertType)
                .HasDatabaseName("IX_ComplianceAlert_AlertType");

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasIndex(cai => new { cai.ClientId, cai.Status })
                .HasDatabaseName("IX_ComplianceActionItem_Client_Status");

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasIndex(cai => cai.Priority)
                .HasDatabaseName("IX_ComplianceActionItem_Priority");

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .HasIndex(cai => cai.DueDate)
                .HasDatabaseName("IX_ComplianceActionItem_DueDate");

            modelBuilder.Entity<Models.FinanceAct2025Rule>()
                .HasIndex(far => new { far.TaxType, far.IsActive })
                .HasDatabaseName("IX_FinanceAct2025Rule_TaxType_Active");

            modelBuilder.Entity<Models.FinanceAct2025Rule>()
                .HasIndex(far => far.EffectiveDate)
                .HasDatabaseName("IX_FinanceAct2025Rule_EffectiveDate");

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .HasIndex(cc => new { cc.ClientId, cc.CalculationDate })
                .HasDatabaseName("IX_ComplianceCalculation_Client_Date");

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .HasIndex(cc => cc.Level)
                .HasDatabaseName("IX_ComplianceCalculation_Level");

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .HasIndex(pc => new { pc.ClientId, pc.TaxType })
                .HasDatabaseName("IX_PenaltyCalculation_Client_TaxType");

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .HasIndex(pc => pc.CalculatedAt)
                .HasDatabaseName("IX_PenaltyCalculation_CalculatedAt");

            // Configure decimal precision for compliance amounts
            modelBuilder.Entity<Models.ComplianceDeadline>()
                .Property(cd => cd.EstimatedTaxLiability)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceAlert>()
                .Property(ca => ca.PenaltyAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceActionItem>()
                .Property(cai => cai.EstimatedImpact)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.FinanceAct2025Rule>()
                .Property(far => far.PenaltyRate)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Models.FinanceAct2025Rule>()
                .Property(far => far.InterestRate)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Models.FinanceAct2025Rule>()
                .Property(far => far.MaxPenaltyPercentage)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.FilingScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.PaymentScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.DocumentScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.TimelinessScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.OverallScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ComplianceCalculation>()
                .Property(cc => cc.ProjectedPenalties)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .Property(pc => pc.TaxLiability)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .Property(pc => pc.BasePenalty)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .Property(pc => pc.InterestPenalty)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .Property(pc => pc.AdditionalPenalties)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PenaltyCalculation>()
                .Property(pc => pc.TotalPenalty)
                .HasPrecision(18, 2);

            // Communication system configuration
            ConfigureCommunicationModels(modelBuilder);
            
            // Payment Gateway system configuration
            ConfigurePaymentGatewaySystem(modelBuilder);
            
            // Configure security system
            ConfigureSecuritySystem(modelBuilder);
            
            // Configure KPI system
            ConfigureKpiSystem(modelBuilder);
            
            // Configure Case Management system
            ConfigureCaseManagementSystem(modelBuilder);
            ConfigureComplianceHistorySystem(modelBuilder);
        }

        private void ConfigureCommunicationModels(ModelBuilder modelBuilder)
        {
            // Conversation configuration
            modelBuilder.Entity<Models.Conversation>()
                .HasOne(c => c.Client)
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.Conversation>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.Conversation>()
                .HasOne(c => c.AssignedToUser)
                .WithMany()
                .HasForeignKey(c => c.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.Conversation>()
                .HasOne(c => c.ClosedByUser)
                .WithMany()
                .HasForeignKey(c => c.ClosedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Message configuration
            modelBuilder.Entity<Models.Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.Message>()
                .HasOne(m => m.EditedByUser)
                .WithMany()
                .HasForeignKey(m => m.EditedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.Message>()
                .HasOne(m => m.DeletedByUser)
                .WithMany()
                .HasForeignKey(m => m.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            // ConversationParticipant configuration
            modelBuilder.Entity<Models.ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ConversationParticipant>()
                .HasOne(cp => cp.LastReadMessage)
                .WithMany()
                .HasForeignKey(cp => cp.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            // MessageRead configuration
            modelBuilder.Entity<Models.MessageRead>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.ReadReceipts)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.MessageRead>()
                .HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // MessageReaction configuration
            modelBuilder.Entity<Models.MessageReaction>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.MessageReaction>()
                .HasOne(mr => mr.User)
                .WithMany()
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ConversationTag configuration
            modelBuilder.Entity<Models.ConversationTag>()
                .HasOne(ct => ct.Conversation)
                .WithMany(c => c.Tags)
                .HasForeignKey(ct => ct.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ConversationTag>()
                .HasOne(ct => ct.CreatedByUser)
                .WithMany()
                .HasForeignKey(ct => ct.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // NotificationTemplate configuration
            modelBuilder.Entity<Models.NotificationTemplate>()
                .HasOne(nt => nt.CreatedByUser)
                .WithMany()
                .HasForeignKey(nt => nt.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // NotificationQueue configuration
            modelBuilder.Entity<Models.NotificationQueue>()
                .HasOne(nq => nq.Recipient)
                .WithMany()
                .HasForeignKey(nq => nq.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.NotificationQueue>()
                .HasOne(nq => nq.Conversation)
                .WithMany()
                .HasForeignKey(nq => nq.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.NotificationQueue>()
                .HasOne(nq => nq.Message)
                .WithMany()
                .HasForeignKey(nq => nq.MessageId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatRoom configuration
            modelBuilder.Entity<Models.ChatRoom>()
                .HasOne(cr => cr.CreatedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatRoom>()
                .HasOne(cr => cr.ArchivedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.ArchivedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatRoom>()
                .HasOne(cr => cr.TopicSetByUser)
                .WithMany()
                .HasForeignKey(cr => cr.TopicSetBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatRoom>()
                .HasOne(cr => cr.Client)
                .WithMany()
                .HasForeignKey(cr => cr.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatRoomParticipant configuration
            modelBuilder.Entity<Models.ChatRoomParticipant>()
                .HasOne(crp => crp.ChatRoom)
                .WithMany(cr => cr.Participants)
                .HasForeignKey(crp => crp.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatRoomParticipant>()
                .HasOne(crp => crp.User)
                .WithMany()
                .HasForeignKey(crp => crp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage configuration
            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.ChatRoom)
                .WithMany(cr => cr.Messages)
                .HasForeignKey(cm => cm.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.EditedByUser)
                .WithMany()
                .HasForeignKey(cm => cm.EditedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.DeletedByUser)
                .WithMany()
                .HasForeignKey(cm => cm.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.PinnedByUser)
                .WithMany()
                .HasForeignKey(cm => cm.PinnedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.ReplyToMessage)
                .WithMany()
                .HasForeignKey(cm => cm.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.ThreadParent)
                .WithMany(cm => cm.ThreadReplies)
                .HasForeignKey(cm => cm.ThreadId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.RelatedTaxFiling)
                .WithMany()
                .HasForeignKey(cm => cm.RelatedTaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.RelatedPayment)
                .WithMany()
                .HasForeignKey(cm => cm.RelatedPaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.RelatedDocument)
                .WithMany()
                .HasForeignKey(cm => cm.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatRoomInvitation configuration
            modelBuilder.Entity<Models.ChatRoomInvitation>()
                .HasOne(cri => cri.ChatRoom)
                .WithMany(cr => cr.Invitations)
                .HasForeignKey(cri => cri.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatRoomInvitation>()
                .HasOne(cri => cri.InvitedUser)
                .WithMany()
                .HasForeignKey(cri => cri.InvitedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatRoomInvitation>()
                .HasOne(cri => cri.InvitedByUser)
                .WithMany()
                .HasForeignKey(cri => cri.InvitedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatMessageReaction configuration
            modelBuilder.Entity<Models.ChatMessageReaction>()
                .HasOne(cmr => cmr.ChatMessage)
                .WithMany(cm => cm.Reactions)
                .HasForeignKey(cmr => cmr.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatMessageReaction>()
                .HasOne(cmr => cmr.User)
                .WithMany()
                .HasForeignKey(cmr => cmr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessageRead configuration
            modelBuilder.Entity<Models.ChatMessageRead>()
                .HasOne(cmr => cmr.ChatMessage)
                .WithMany(cm => cm.ReadReceipts)
                .HasForeignKey(cmr => cmr.ChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ChatMessageRead>()
                .HasOne(cmr => cmr.User)
                .WithMany()
                .HasForeignKey(cmr => cmr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance
            modelBuilder.Entity<Models.Conversation>()
                .HasIndex(c => c.Status)
                .HasDatabaseName("IX_Conversation_Status");

            modelBuilder.Entity<Models.Conversation>()
                .HasIndex(c => c.CreatedAt)
                .HasDatabaseName("IX_Conversation_CreatedAt");

            modelBuilder.Entity<Models.Conversation>()
                .HasIndex(c => c.ClientId)
                .HasDatabaseName("IX_Conversation_ClientId");

            modelBuilder.Entity<Models.Message>()
                .HasIndex(m => m.ConversationId)
                .HasDatabaseName("IX_Message_ConversationId");

            modelBuilder.Entity<Models.Message>()
                .HasIndex(m => m.SentAt)
                .HasDatabaseName("IX_Message_SentAt");

            modelBuilder.Entity<Models.ConversationParticipant>()
                .HasIndex(cp => new { cp.ConversationId, cp.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ConversationParticipant_ConversationId_UserId");

            modelBuilder.Entity<Models.NotificationQueue>()
                .HasIndex(nq => nq.RecipientId)
                .HasDatabaseName("IX_NotificationQueue_RecipientId");

            modelBuilder.Entity<Models.NotificationQueue>()
                .HasIndex(nq => nq.Status)
                .HasDatabaseName("IX_NotificationQueue_Status");

            modelBuilder.Entity<Models.ChatRoomParticipant>()
                .HasIndex(crp => new { crp.ChatRoomId, crp.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ChatRoomParticipant_ChatRoomId_UserId");

            // Enhanced Chat indexes for performance
            modelBuilder.Entity<Models.ChatRoom>()
                .HasIndex(cr => cr.Type)
                .HasDatabaseName("IX_ChatRoom_Type");

            modelBuilder.Entity<Models.ChatRoom>()
                .HasIndex(cr => cr.ClientId)
                .HasDatabaseName("IX_ChatRoom_ClientId");

            modelBuilder.Entity<Models.ChatRoom>()
                .HasIndex(cr => cr.IsActive)
                .HasDatabaseName("IX_ChatRoom_IsActive");

            modelBuilder.Entity<Models.ChatRoom>()
                .HasIndex(cr => cr.LastActivityAt)
                .HasDatabaseName("IX_ChatRoom_LastActivityAt");

            modelBuilder.Entity<Models.ChatMessage>()
                .HasIndex(cm => cm.SentAt)
                .HasDatabaseName("IX_ChatMessage_SentAt");

            modelBuilder.Entity<Models.ChatMessage>()
                .HasIndex(cm => cm.Type)
                .HasDatabaseName("IX_ChatMessage_Type");

            modelBuilder.Entity<Models.ChatMessage>()
                .HasIndex(cm => cm.ThreadId)
                .HasDatabaseName("IX_ChatMessage_ThreadId");

            modelBuilder.Entity<Models.ChatMessage>()
                .HasIndex(cm => new { cm.ChatRoomId, cm.SentAt })
                .HasDatabaseName("IX_ChatMessage_ChatRoomId_SentAt");

            modelBuilder.Entity<Models.ChatMessageReaction>()
                .HasIndex(cmr => new { cmr.ChatMessageId, cmr.UserId, cmr.Reaction })
                .IsUnique()
                .HasDatabaseName("IX_ChatMessageReaction_MessageId_UserId_Reaction");

            modelBuilder.Entity<Models.ChatMessageRead>()
                .HasIndex(cmr => new { cmr.ChatMessageId, cmr.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ChatMessageRead_MessageId_UserId");

            modelBuilder.Entity<Models.ChatRoomInvitation>()
                .HasIndex(cri => new { cri.ChatRoomId, cri.InvitedUserId })
                .HasDatabaseName("IX_ChatRoomInvitation_ChatRoomId_InvitedUserId");

            modelBuilder.Entity<Models.ChatRoomInvitation>()
                .HasIndex(cri => cri.Status)
                .HasDatabaseName("IX_ChatRoomInvitation_Status");
        }

        private void ConfigurePaymentGatewaySystem(ModelBuilder modelBuilder)
        {
            // Configure PaymentGatewayConfig relationships
            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .HasOne(pgc => pgc.CreatedByUser)
                .WithMany()
                .HasForeignKey(pgc => pgc.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .HasOne(pgc => pgc.UpdatedByUser)
                .WithMany()
                .HasForeignKey(pgc => pgc.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PaymentTransaction relationships
            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasOne(pt => pt.Client)
                .WithMany()
                .HasForeignKey(pt => pt.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasOne(pt => pt.GatewayConfig)
                .WithMany(pgc => pgc.Transactions)
                .HasForeignKey(pt => pt.GatewayConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasOne(pt => pt.ReviewedByUser)
                .WithMany()
                .HasForeignKey(pt => pt.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasOne(pt => pt.ReconciledByUser)
                .WithMany()
                .HasForeignKey(pt => pt.ReconciledBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PaymentTransactionLog relationships
            modelBuilder.Entity<Models.PaymentTransactionLog>()
                .HasOne(ptl => ptl.Transaction)
                .WithMany(pt => pt.TransactionLogs)
                .HasForeignKey(ptl => ptl.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure PaymentRefund relationships
            modelBuilder.Entity<Models.PaymentRefund>()
                .HasOne(pr => pr.OriginalTransaction)
                .WithMany(pt => pt.Refunds)
                .HasForeignKey(pr => pr.OriginalTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PaymentRefund>()
                .HasOne(pr => pr.RequestedByUser)
                .WithMany()
                .HasForeignKey(pr => pr.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.PaymentRefund>()
                .HasOne(pr => pr.ApprovedByUser)
                .WithMany()
                .HasForeignKey(pr => pr.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PaymentWebhookLog relationships
            modelBuilder.Entity<Models.PaymentWebhookLog>()
                .HasOne(pwl => pwl.GatewayConfig)
                .WithMany(pgc => pgc.WebhookLogs)
                .HasForeignKey(pwl => pwl.GatewayConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MobileMoneyProvider relationships
            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .HasOne(mmp => mmp.CreatedByUser)
                .WithMany()
                .HasForeignKey(mmp => mmp.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .HasOne(mmp => mmp.UpdatedByUser)
                .WithMany()
                .HasForeignKey(mmp => mmp.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure PaymentFraudRule relationships
            modelBuilder.Entity<Models.PaymentFraudRule>()
                .HasOne(pfr => pfr.CreatedByUser)
                .WithMany()
                .HasForeignKey(pfr => pfr.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.PaymentFraudRule>()
                .HasOne(pfr => pfr.UpdatedByUser)
                .WithMany()
                .HasForeignKey(pfr => pfr.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes for payment gateway performance
            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .HasIndex(pgc => pgc.GatewayType)
                .HasDatabaseName("IX_PaymentGatewayConfig_GatewayType");

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .HasIndex(pgc => pgc.IsActive)
                .HasDatabaseName("IX_PaymentGatewayConfig_IsActive");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.TransactionReference)
                .IsUnique()
                .HasDatabaseName("IX_PaymentTransaction_TransactionReference");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.ExternalReference)
                .HasDatabaseName("IX_PaymentTransaction_ExternalReference");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => new { pt.ClientId, pt.Status })
                .HasDatabaseName("IX_PaymentTransaction_Client_Status");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => new { pt.GatewayType, pt.Status })
                .HasDatabaseName("IX_PaymentTransaction_Gateway_Status");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.PayerPhone)
                .HasDatabaseName("IX_PaymentTransaction_PayerPhone");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.InitiatedAt)
                .HasDatabaseName("IX_PaymentTransaction_InitiatedAt");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.ExpiresAt)
                .HasDatabaseName("IX_PaymentTransaction_ExpiresAt");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.IsReconciled)
                .HasDatabaseName("IX_PaymentTransaction_IsReconciled");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.RiskLevel)
                .HasDatabaseName("IX_PaymentTransaction_RiskLevel");

            modelBuilder.Entity<Models.PaymentTransaction>()
                .HasIndex(pt => pt.RequiresManualReview)
                .HasDatabaseName("IX_PaymentTransaction_RequiresManualReview");

            modelBuilder.Entity<Models.PaymentTransactionLog>()
                .HasIndex(ptl => new { ptl.TransactionId, ptl.CreatedAt })
                .HasDatabaseName("IX_PaymentTransactionLog_Transaction_CreatedAt");

            modelBuilder.Entity<Models.PaymentRefund>()
                .HasIndex(pr => pr.RefundReference)
                .IsUnique()
                .HasDatabaseName("IX_PaymentRefund_RefundReference");

            modelBuilder.Entity<Models.PaymentRefund>()
                .HasIndex(pr => new { pr.OriginalTransactionId, pr.Status })
                .HasDatabaseName("IX_PaymentRefund_Transaction_Status");

            modelBuilder.Entity<Models.PaymentWebhookLog>()
                .HasIndex(pwl => new { pwl.GatewayConfigId, pwl.ReceivedAt })
                .HasDatabaseName("IX_PaymentWebhookLog_Gateway_ReceivedAt");

            modelBuilder.Entity<Models.PaymentWebhookLog>()
                .HasIndex(pwl => pwl.IsProcessed)
                .HasDatabaseName("IX_PaymentWebhookLog_IsProcessed");

            modelBuilder.Entity<Models.PaymentWebhookLog>()
                .HasIndex(pwl => pwl.TransactionReference)
                .HasDatabaseName("IX_PaymentWebhookLog_TransactionReference");

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .HasIndex(mmp => mmp.Code)
                .IsUnique()
                .HasDatabaseName("IX_MobileMoneyProvider_Code");

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .HasIndex(mmp => mmp.Name)
                .HasDatabaseName("IX_MobileMoneyProvider_Name");

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .HasIndex(mmp => new { mmp.CountryCode, mmp.IsActive })
                .HasDatabaseName("IX_MobileMoneyProvider_Country_Active");

            modelBuilder.Entity<Models.PaymentFraudRule>()
                .HasIndex(pfr => pfr.RuleName)
                .IsUnique()
                .HasDatabaseName("IX_PaymentFraudRule_RuleName");

            modelBuilder.Entity<Models.PaymentFraudRule>()
                .HasIndex(pfr => new { pfr.IsActive, pfr.Priority })
                .HasDatabaseName("IX_PaymentFraudRule_Active_Priority");

            // Configure decimal precision for payment amounts
            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.MinAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.MaxAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.DailyLimit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.MonthlyLimit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.FeePercentage)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.FixedFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.MinFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentGatewayConfig>()
                .Property(pgc => pgc.MaxFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .Property(pt => pt.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .Property(pt => pt.Fee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentTransaction>()
                .Property(pt => pt.NetAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.PaymentRefund>()
                .Property(pr => pr.RefundAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultMinAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultMaxAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultDailyLimit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultFeePercentage)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultFixedFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultMinFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.MobileMoneyProvider>()
                .Property(mmp => mmp.DefaultMaxFee)
                .HasPrecision(18, 2);
        }

        private void ConfigureSecuritySystem(ModelBuilder modelBuilder)
        {
            // Configure UserMfaConfiguration relationships
            modelBuilder.Entity<UserMfaConfiguration>()
                .HasOne(umc => umc.User)
                .WithOne()
                .HasForeignKey<UserMfaConfiguration>(umc => umc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MfaChallenge relationships
            modelBuilder.Entity<MfaChallenge>()
                .HasOne(mc => mc.User)
                .WithMany()
                .HasForeignKey(mc => mc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MfaChallenge>()
                .HasOne(mc => mc.MfaConfiguration)
                .WithMany(umc => umc.MfaChallenges)
                .HasForeignKey(mc => mc.MfaConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AuditLog relationships (Security version)
            modelBuilder.Entity<Models.Security.AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure SecurityEvent relationships
            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasOne(se => se.User)
                .WithMany()
                .HasForeignKey(se => se.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure EncryptedData relationships
            modelBuilder.Entity<Models.Security.EncryptedData>()
                .HasOne(ed => ed.EncryptionKey)
                .WithMany()
                .HasForeignKey(ed => ed.EncryptionKeyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for security system performance
            modelBuilder.Entity<UserMfaConfiguration>()
                .HasIndex(umc => umc.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserMfaConfiguration_UserId");

            modelBuilder.Entity<MfaChallenge>()
                .HasIndex(mc => mc.ChallengeId)
                .IsUnique()
                .HasDatabaseName("IX_MfaChallenge_ChallengeId");

            modelBuilder.Entity<MfaChallenge>()
                .HasIndex(mc => new { mc.UserId, mc.Status })
                .HasDatabaseName("IX_MfaChallenge_User_Status");

            modelBuilder.Entity<MfaChallenge>()
                .HasIndex(mc => mc.ExpiresAt)
                .HasDatabaseName("IX_MfaChallenge_ExpiresAt");

            modelBuilder.Entity<Models.Security.AuditLog>()
                .HasIndex(al => new { al.UserId, al.Timestamp })
                .HasDatabaseName("IX_SecurityAuditLog_User_Timestamp");

            modelBuilder.Entity<Models.Security.AuditLog>()
                .HasIndex(al => al.Category)
                .HasDatabaseName("IX_SecurityAuditLog_Category");

            modelBuilder.Entity<Models.Security.AuditLog>()
                .HasIndex(al => al.Severity)
                .HasDatabaseName("IX_SecurityAuditLog_Severity");

            modelBuilder.Entity<Models.Security.AuditLog>()
                .HasIndex(al => al.IsComplianceRelevant)
                .HasDatabaseName("IX_SecurityAuditLog_ComplianceRelevant");

            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasIndex(se => new { se.UserId, se.Timestamp })
                .HasDatabaseName("IX_SecurityEvent_User_Timestamp");

            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasIndex(se => se.Severity)
                .HasDatabaseName("IX_SecurityEvent_Severity");

            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasIndex(se => se.Category)
                .HasDatabaseName("IX_SecurityEvent_Category");

            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasIndex(se => se.IsResolved)
                .HasDatabaseName("IX_SecurityEvent_IsResolved");

            modelBuilder.Entity<Models.Security.SecurityEvent>()
                .HasIndex(se => se.RequiresInvestigation)
                .HasDatabaseName("IX_SecurityEvent_RequiresInvestigation");

            modelBuilder.Entity<Models.Security.EncryptionKey>()
                .HasIndex(ek => ek.KeyName)
                .IsUnique()
                .HasDatabaseName("IX_EncryptionKey_KeyName");

            modelBuilder.Entity<Models.Security.EncryptionKey>()
                .HasIndex(ek => new { ek.KeyType, ek.IsActive })
                .HasDatabaseName("IX_EncryptionKey_Type_Active");

            modelBuilder.Entity<Models.Security.EncryptionKey>()
                .HasIndex(ek => ek.ExpiresAt)
                .HasDatabaseName("IX_EncryptionKey_ExpiresAt");

            modelBuilder.Entity<Models.Security.EncryptedData>()
                .HasIndex(ed => new { ed.EntityType, ed.EntityId, ed.FieldName })
                .IsUnique()
                .HasDatabaseName("IX_EncryptedData_Entity_Field");

            modelBuilder.Entity<Models.Security.EncryptedData>()
                .HasIndex(ed => ed.IsPersonalData)
                .HasDatabaseName("IX_EncryptedData_IsPersonalData");

            modelBuilder.Entity<Models.Security.EncryptedData>()
                .HasIndex(ed => ed.IsFinancialData)
                .HasDatabaseName("IX_EncryptedData_IsFinancialData");

            modelBuilder.Entity<Models.Security.SystemHealthCheck>()
                .HasIndex(shc => new { shc.Component, shc.CheckName, shc.Timestamp })
                .HasDatabaseName("IX_SystemHealthCheck_Component_Check_Timestamp");

            modelBuilder.Entity<Models.Security.SystemHealthCheck>()
                .HasIndex(shc => shc.Status)
                .HasDatabaseName("IX_SystemHealthCheck_Status");

            modelBuilder.Entity<Models.Security.SecurityScan>()
                .HasIndex(ss => new { ss.ScanType, ss.StartedAt })
                .HasDatabaseName("IX_SecurityScan_Type_StartedAt");

            modelBuilder.Entity<Models.Security.SecurityScan>()
                .HasIndex(ss => ss.Status)
                .HasDatabaseName("IX_SecurityScan_Status");

            modelBuilder.Entity<Models.Security.SecurityScan>()
                .HasIndex(ss => ss.RequiresAction)
                .HasDatabaseName("IX_SecurityScan_RequiresAction");

            // Configure payment retry system indexes
            modelBuilder.Entity<Models.PaymentScheduledRetry>()
                .HasIndex(psr => psr.ScheduledAt)
                .HasDatabaseName("IX_PaymentScheduledRetry_ScheduledAt");

            modelBuilder.Entity<Models.PaymentRetryAttempt>()
                .HasIndex(pra => new { pra.PaymentId, pra.AttemptNumber })
                .HasDatabaseName("IX_PaymentRetryAttempt_Payment_Attempt");

            modelBuilder.Entity<Models.PaymentRetryAttempt>()
                .HasIndex(pra => pra.AttemptStatus)
                .HasDatabaseName("IX_PaymentRetryAttempt_Status");

            modelBuilder.Entity<Models.PaymentDeadLetterQueue>()
                .HasIndex(pdlq => pdlq.Status)
                .HasDatabaseName("IX_PaymentDeadLetterQueue_Status");

            modelBuilder.Entity<Models.PaymentDeadLetterQueue>()
                .HasIndex(pdlq => pdlq.CreatedAt)
                .HasDatabaseName("IX_PaymentDeadLetterQueue_CreatedAt");

            // Additional indexes for performance optimization (CTIS Enhancement)
            // Payment indexes for KPI queries
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.Status, p.DueDate })
                .HasDatabaseName("IX_Payment_Status_DueDate");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.ClientId, p.PaymentDate })
                .HasDatabaseName("IX_Payment_Client_PaymentDate");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.TaxType, p.Status, p.PaymentDate })
                .HasDatabaseName("IX_Payment_TaxType_Status_PaymentDate");

            // Extended Payment field indexes for performance
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentBatchId)
                .HasDatabaseName("IX_Payment_PaymentBatchId");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentCategory)
                .HasDatabaseName("IX_Payment_PaymentCategory");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.IsReconciled)
                .HasDatabaseName("IX_Payment_IsReconciled");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ReconciledAt)
                .HasDatabaseName("IX_Payment_ReconciledAt");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.NextRetryAt)
                .HasDatabaseName("IX_Payment_NextRetryAt");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.RequiresManualReview)
                .HasDatabaseName("IX_Payment_RequiresManualReview");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.IsSuspicious)
                .HasDatabaseName("IX_Payment_IsSuspicious");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentChannel)
                .HasDatabaseName("IX_Payment_PaymentChannel");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentSource)
                .HasDatabaseName("IX_Payment_PaymentSource");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.Currency)
                .HasDatabaseName("IX_Payment_Currency");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.ClientId, p.PaymentCategory, p.Status })
                .HasDatabaseName("IX_Payment_ClientId_Category_Status");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.IsReconciled, p.ReconciledAt })
                .HasDatabaseName("IX_Payment_Reconciled_ReconciledAt");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.RequiresManualReview, p.ReviewedAt })
                .HasDatabaseName("IX_Payment_Review_ReviewedAt");

            // TaxFiling indexes for compliance queries
            modelBuilder.Entity<TaxFiling>()
                .HasIndex(tf => new { tf.ClientId, tf.TaxYear, tf.Status })
                .HasDatabaseName("IX_TaxFiling_Client_Year_Status");

            modelBuilder.Entity<TaxFiling>()
                .HasIndex(tf => new { tf.DueDate, tf.SubmittedDate })
                .HasDatabaseName("IX_TaxFiling_DueDate_SubmittedDate");

            modelBuilder.Entity<TaxFiling>()
                .HasIndex(tf => new { tf.TaxType, tf.Status, tf.DueDate })
                .HasDatabaseName("IX_TaxFiling_TaxType_Status_DueDate");

            // Document indexes for compliance tracking
            modelBuilder.Entity<Document>()
                .HasIndex(d => new { d.ClientId, d.Category })
                .HasDatabaseName("IX_Document_Client_Category");

            modelBuilder.Entity<Document>()
                .HasIndex(d => new { d.UploadedAt, d.IsDeleted })
                .HasDatabaseName("IX_Document_UploadedAt_IsDeleted");

            // Chat/Message indexes for real-time queries
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ClientId, m.CreatedDate })
                .HasDatabaseName("IX_Message_Client_CreatedDate");

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.Status, m.CreatedDate })
                .HasDatabaseName("IX_Message_Status_CreatedDate");
        }

        private void ConfigureKpiSystem(ModelBuilder modelBuilder)
        {
            // KpiSnapshot configuration
            modelBuilder.Entity<Models.KpiSnapshot>()
                .HasOne(ks => ks.CreatedByUser)
                .WithMany()
                .HasForeignKey(ks => ks.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.ClientComplianceRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.TaxFilingTimeliness)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.PaymentCompletionRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.DocumentSubmissionCompliance)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.ClientEngagementRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.OnTimePaymentPercentage)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.FilingTimelinessAverage)
                .HasPrecision(8, 2);

            modelBuilder.Entity<Models.KpiSnapshot>()
                .Property(ks => ks.DocumentReadinessRate)
                .HasPrecision(5, 2);

            // ClientKpiMetrics configuration
            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .HasOne(ckm => ckm.Client)
                .WithMany()
                .HasForeignKey(ckm => ckm.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .HasOne(ckm => ckm.KpiSnapshot)
                .WithMany()
                .HasForeignKey(ckm => ckm.KpiSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .Property(ckm => ckm.OnTimePaymentPercentage)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .Property(ckm => ckm.FilingTimelinessAverage)
                .HasPrecision(8, 2);

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .Property(ckm => ckm.DocumentReadinessRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .Property(ckm => ckm.EngagementScore)
                .HasPrecision(5, 2);

            // KpiAlert configuration
            modelBuilder.Entity<Models.KpiAlert>()
                .HasOne(ka => ka.KpiSnapshot)
                .WithMany()
                .HasForeignKey(ka => ka.KpiSnapshotId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.KpiAlert>()
                .HasOne(ka => ka.Client)
                .WithMany()
                .HasForeignKey(ka => ka.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.KpiAlert>()
                .HasOne(ka => ka.ResolvedByUser)
                .WithMany()
                .HasForeignKey(ka => ka.ResolvedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.KpiAlert>()
                .Property(ka => ka.ThresholdValue)
                .HasPrecision(8, 2);

            modelBuilder.Entity<Models.KpiAlert>()
                .Property(ka => ka.ActualValue)
                .HasPrecision(8, 2);

            // KPI system indexes
            modelBuilder.Entity<Models.KpiSnapshot>()
                .HasIndex(ks => ks.SnapshotDate)
                .HasDatabaseName("IX_KpiSnapshot_SnapshotDate");

            modelBuilder.Entity<Models.KpiSnapshot>()
                .HasIndex(ks => ks.CreatedAt)
                .HasDatabaseName("IX_KpiSnapshot_CreatedAt");

            modelBuilder.Entity<Models.ClientKpiMetrics>()
                .HasIndex(ckm => new { ckm.ClientId, ckm.KpiSnapshotId })
                .HasDatabaseName("IX_ClientKpiMetrics_Client_Snapshot");

            modelBuilder.Entity<Models.KpiAlert>()
                .HasIndex(ka => ka.AlertType)
                .HasDatabaseName("IX_KpiAlert_AlertType");

            modelBuilder.Entity<Models.KpiAlert>()
                .HasIndex(ka => ka.Severity)
                .HasDatabaseName("IX_KpiAlert_Severity");

            modelBuilder.Entity<Models.KpiAlert>()
                .HasIndex(ka => ka.IsResolved)
                .HasDatabaseName("IX_KpiAlert_IsResolved");

            modelBuilder.Entity<Models.KpiAlert>()
                .HasIndex(ka => ka.CreatedAt)
                .HasDatabaseName("IX_KpiAlert_CreatedAt");
        }

        private void ConfigureCaseManagementSystem(ModelBuilder modelBuilder)
        {
            // CaseIssue configuration
            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.Client)
                .WithMany()
                .HasForeignKey(ci => ci.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.AssignedToUser)
                .WithMany()
                .HasForeignKey(ci => ci.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.CreatedByUser)
                .WithMany()
                .HasForeignKey(ci => ci.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.ResolvedByUser)
                .WithMany()
                .HasForeignKey(ci => ci.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(ci => ci.LastUpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.RelatedTaxFiling)
                .WithMany()
                .HasForeignKey(ci => ci.RelatedTaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.RelatedPayment)
                .WithMany()
                .HasForeignKey(ci => ci.RelatedPaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.CaseIssue>()
                .HasOne(ci => ci.RelatedDocument)
                .WithMany()
                .HasForeignKey(ci => ci.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            // CaseComment configuration
            modelBuilder.Entity<Models.CaseComment>()
                .HasOne(cc => cc.CaseIssue)
                .WithMany(ci => ci.Comments)
                .HasForeignKey(cc => cc.CaseIssueId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.CaseComment>()
                .HasOne(cc => cc.CreatedByUser)
                .WithMany()
                .HasForeignKey(cc => cc.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // CaseAttachment configuration
            modelBuilder.Entity<Models.CaseAttachment>()
                .HasOne(ca => ca.CaseIssue)
                .WithMany(ci => ci.Attachments)
                .HasForeignKey(ca => ca.CaseIssueId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.CaseAttachment>()
                .HasOne(ca => ca.UploadedByUser)
                .WithMany()
                .HasForeignKey(ca => ca.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Case Management indexes
            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => ci.CaseNumber)
                .IsUnique()
                .HasDatabaseName("IX_CaseIssue_CaseNumber");

            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => ci.Status)
                .HasDatabaseName("IX_CaseIssue_Status");

            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => ci.Priority)
                .HasDatabaseName("IX_CaseIssue_Priority");

            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => ci.CreatedAt)
                .HasDatabaseName("IX_CaseIssue_CreatedAt");

            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => new { ci.ClientId, ci.Status })
                .HasDatabaseName("IX_CaseIssue_Client_Status");

            modelBuilder.Entity<Models.CaseIssue>()
                .HasIndex(ci => ci.AssignedToUserId)
                .HasDatabaseName("IX_CaseIssue_AssignedTo");

            modelBuilder.Entity<Models.CaseComment>()
                .HasIndex(cc => cc.CreatedAt)
                .HasDatabaseName("IX_CaseComment_CreatedAt");

            modelBuilder.Entity<Models.CaseAttachment>()
                .HasIndex(ca => ca.UploadedAt)
                .HasDatabaseName("IX_CaseAttachment_UploadedAt");
        }

        private void ConfigureComplianceHistorySystem(ModelBuilder modelBuilder)
        {
            // ComplianceHistory configuration
            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasOne(ch => ch.Client)
                .WithMany()
                .HasForeignKey(ch => ch.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasOne(ch => ch.CreatedByUser)
                .WithMany()
                .HasForeignKey(ch => ch.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasOne(ch => ch.UpdatedByUser)
                .WithMany()
                .HasForeignKey(ch => ch.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Decimal precision configuration
            modelBuilder.Entity<Models.ComplianceHistory>()
                .Property(ch => ch.AmountDue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .Property(ch => ch.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .Property(ch => ch.PenaltyAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .Property(ch => ch.InterestAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceHistory>()
                .Property(ch => ch.ComplianceScore)
                .HasPrecision(5, 2);

            // ComplianceHistoryEvent configuration
            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasOne(che => che.ComplianceHistory)
                .WithMany(ch => ch.Events)
                .HasForeignKey(che => che.ComplianceHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasOne(che => che.CreatedByUser)
                .WithMany()
                .HasForeignKey(che => che.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasOne(che => che.RelatedTaxFiling)
                .WithMany()
                .HasForeignKey(che => che.RelatedTaxFilingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasOne(che => che.RelatedPayment)
                .WithMany()
                .HasForeignKey(che => che.RelatedPaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasOne(che => che.RelatedDocument)
                .WithMany()
                .HasForeignKey(che => che.RelatedDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Decimal precision for events
            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .Property(che => che.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .Property(che => che.PenaltyAmount)
                .HasPrecision(18, 2);

            // Compliance History indexes
            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasIndex(ch => new { ch.ClientId, ch.TaxYear, ch.TaxType })
                .IsUnique()
                .HasDatabaseName("IX_ComplianceHistory_Client_TaxYear_TaxType");

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasIndex(ch => ch.RecordDate)
                .HasDatabaseName("IX_ComplianceHistory_RecordDate");

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasIndex(ch => ch.ComplianceScore)
                .HasDatabaseName("IX_ComplianceHistory_ComplianceScore");

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasIndex(ch => ch.RiskLevel)
                .HasDatabaseName("IX_ComplianceHistory_RiskLevel");

            modelBuilder.Entity<Models.ComplianceHistory>()
                .HasIndex(ch => ch.Status)
                .HasDatabaseName("IX_ComplianceHistory_Status");

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasIndex(che => che.EventDate)
                .HasDatabaseName("IX_ComplianceHistoryEvent_EventDate");

            modelBuilder.Entity<Models.ComplianceHistoryEvent>()
                .HasIndex(che => che.EventType)
                .HasDatabaseName("IX_ComplianceHistoryEvent_EventType");
        }
    }
}
