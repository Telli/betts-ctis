using Microsoft.EntityFrameworkCore;
using BettsTax.Web.Models.Entities;

namespace BettsTax.Web.Data;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Filing> Filings { get; set; }
    public DbSet<FilingSchedule> FilingSchedules { get; set; }
    public DbSet<FilingDocument> FilingDocuments { get; set; }
    public DbSet<FilingHistory> FilingHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Users)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Tin).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Tin).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Segment).HasMaxLength(50);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ComplianceScore).HasPrecision(5, 2);
            entity.Property(e => e.AssignedTo).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaxType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Period).HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Method).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ReceiptNo).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Payments)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.TaxType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).HasMaxLength(255);
            entity.Property(e => e.Hash).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.UploadDate).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Filing configuration
        modelBuilder.Entity<Filing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaxType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Period).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalSales).HasPrecision(18, 2);
            entity.Property(e => e.TaxableSales).HasPrecision(18, 2);
            entity.Property(e => e.GstRate).HasPrecision(5, 2);
            entity.Property(e => e.OutputTax).HasPrecision(18, 2);
            entity.Property(e => e.InputTaxCredit).HasPrecision(18, 2);
            entity.Property(e => e.NetGstPayable).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Filings)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FilingSchedule configuration
        modelBuilder.Entity<FilingSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Taxable).HasPrecision(18, 2);

            entity.HasOne(e => e.Filing)
                  .WithMany(f => f.Schedules)
                  .HasForeignKey(e => e.FilingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FilingDocument configuration
        modelBuilder.Entity<FilingDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UploadedBy).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.Date).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Filing)
                  .WithMany(f => f.Documents)
                  .HasForeignKey(e => e.FilingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FilingHistory configuration
        modelBuilder.Entity<FilingHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.User).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Changes).HasMaxLength(2000);
            entity.Property(e => e.Date).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Filing)
                  .WithMany(f => f.History)
                  .HasForeignKey(e => e.FilingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
