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

        // Payment Gateway System DbSets
        public DbSet<Models.PaymentGatewayConfig> PaymentGatewayConfigs { get; set; }
        public DbSet<Models.PaymentTransaction> PaymentGatewayTransactions { get; set; }
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.TaxFiling)
                .WithMany(tf => tf.Payments)
                .HasForeignKey(p => p.TaxFilingId)
                .OnDelete(DeleteBehavior.Restrict);

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
            
            // Security system configuration
            ConfigureSecuritySystem(modelBuilder);
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
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Models.ChatMessage>()
                .HasOne(cm => cm.ReplyToMessage)
                .WithMany()
                .HasForeignKey(cm => cm.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);

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
        }
    }
}
