using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<AuditLog> AuditLogs { get; set; }
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
    }
}
