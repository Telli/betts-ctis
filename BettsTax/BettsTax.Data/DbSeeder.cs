using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data.Models.Security;

namespace BettsTax.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider sp)
        {
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = ["Admin", "Associate", "Client", "SystemAdmin"];
            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }
        }

        public static async Task SeedAdminUserAsync(IServiceProvider sp, IConfiguration config)
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            
            // Create System Admin
            var adminEmail = config["Admin:Email"] ?? "admin@thebettsfirmsl.com";
            var adminPass = config["Admin:Password"] ?? "AdminPass123!";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser 
                { 
                    UserName = adminEmail, 
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    IsActive = true,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    RegistrationSource = RegistrationSource.AdminCreated,
                    RegistrationCompletedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, adminPass);
                if (!result.Succeeded) return;
                
                await userManager.AddToRoleAsync(admin, "SystemAdmin");
            }

            // Create sample associates for testing client enrollment
            var associates = new[]
            {
                new { Email = "associate1@thebettsfirmsl.com", FirstName = "Sarah", LastName = "Bangura" },
                new { Email = "associate2@thebettsfirmsl.com", FirstName = "Mohamed", LastName = "Conteh" }
            };

            foreach (var associate in associates)
            {
                var existingUser = await userManager.FindByEmailAsync(associate.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = associate.Email,
                        Email = associate.Email,
                        FirstName = associate.FirstName,
                        LastName = associate.LastName,
                        IsActive = true,
                        EmailConfirmed = true,
                        EmailVerified = true,
                        RegistrationSource = RegistrationSource.AdminCreated,
                        RegistrationCompletedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(user, "Associate123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Associate");
                    }
                }
            }

            // Seed default system settings (Email, etc.)
            await SeedDefaultSystemSettings(sp);
            // Seed default tax settings (e.g., GST registration threshold)
            await SeedDefaultTaxSettings(sp);
            // Seed report templates
            await ReportTemplateSeeder.SeedReportTemplatesAsync(sp);
        }

        public static async Task SeedDefaultSystemSettings(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Get admin user for settings updates
            var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == "admin@thebettsfirmsl.com");
            if (admin == null) return;

            // Only seed Email defaults if not present
            if (!await context.SystemSettings.AnyAsync(s => s.Category == "Email"))
            {
                var defaultEmailSettings = new List<SystemSetting>
                {
                    new() 
                    {
                        Key = "Email.SmtpHost",
                        Value = "",
                        Description = "SMTP server hostname or IP address",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.SmtpPort",
                        Value = "587",
                        Description = "SMTP server port number (typically 587 or 25)",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.Username",
                        Value = "",
                        Description = "SMTP authentication username",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.Password",
                        Value = "",
                        Description = "SMTP authentication password",
                        Category = "Email",
                        IsEncrypted = true,
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.FromEmail",
                        Value = "noreply@thebettsfirmsl.com",
                        Description = "Default sender email address",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.FromName",
                        Value = "The Betts Firm",
                        Description = "Default sender display name",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.UseSSL",
                        Value = "true",
                        Description = "Enable SSL encryption",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    },
                    new() 
                    {
                        Key = "Email.UseTLS",
                        Value = "true",
                        Description = "Enable TLS encryption",
                        Category = "Email",
                        UpdatedByUserId = admin.Id
                    }
                };

                context.SystemSettings.AddRange(defaultEmailSettings);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedDefaultTaxSettings(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Get admin user for settings updates
            var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == "admin@thebettsfirmsl.com");
            if (admin == null) return;

            // Seed GST registration threshold if missing
            if (!await context.SystemSettings.AnyAsync(s => s.Key == "Tax.GST.RegistrationThreshold"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    Key = "Tax.GST.RegistrationThreshold",
                    Value = "0",
                    Description = "Annual turnover threshold for GST registration (SLE)",
                    Category = "Tax",
                    UpdatedByUserId = admin.Id
                });
            }

            // Seed GST rate percent if missing (default 15)
            if (!await context.SystemSettings.AnyAsync(s => s.Key == "Tax.GST.RatePercent"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    Key = "Tax.GST.RatePercent",
                    Value = "15",
                    Description = "GST standard rate (percent)",
                    Category = "Tax",
                    UpdatedByUserId = admin.Id
                });
            }

            // Seed Annual interest rate percent if missing (default 15)
            if (!await context.SystemSettings.AnyAsync(s => s.Key == "Tax.AnnualInterestRatePercent"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    Key = "Tax.AnnualInterestRatePercent",
                    Value = "15",
                    Description = "Annual interest rate for late payments (percent)",
                    Category = "Tax",
                    UpdatedByUserId = admin.Id
                });
            }

            // Seed income minimum tax rate percent if missing (default 0.5)
            if (!await context.SystemSettings.AnyAsync(s => s.Key == "Tax.Income.MinimumTaxRatePercent"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    Key = "Tax.Income.MinimumTaxRatePercent",
                    Value = "0.5",
                    Description = "Income Tax minimum tax rate for companies (percent of turnover)",
                    Category = "Tax",
                    UpdatedByUserId = admin.Id
                });
            }

            // Seed income MAT rate percent if missing (default 3)
            if (!await context.SystemSettings.AnyAsync(s => s.Key == "Tax.Income.MATRatePercent"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    Key = "Tax.Income.MATRatePercent",
                    Value = "3",
                    Description = "Minimum Alternate Tax rate (percent of turnover)",
                    Category = "Tax",
                    UpdatedByUserId = admin.Id
                });
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedDemoDataAsync(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Check if demo data already exists
            if (await context.Clients.AnyAsync())
                return;

            // Create demo users
            var demoUsers = new List<ApplicationUser>
            {
                new() {
                    UserName = "john.kamara@sierramining.sl",
                    Email = "john.kamara@sierramining.sl",
                    FirstName = "John",
                    LastName = "Kamara",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-150)
                },
                new() {
                    UserName = "fatima.sesay@freetownlogistics.sl",
                    Email = "fatima.sesay@freetownlogistics.sl",
                    FirstName = "Fatima",
                    LastName = "Sesay",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-120)
                },
                // Test users for Playwright tests
                new() {
                    UserName = "admin@bettsfirm.sl",
                    Email = "admin@bettsfirm.sl",
                    FirstName = "Test",
                    LastName = "Admin",
                    IsActive = true,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    RegistrationSource = RegistrationSource.AdminCreated,
                    RegistrationCompletedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow.AddDays(-200)
                },
                new() {
                    UserName = "associate@bettsfirm.sl",
                    Email = "associate@bettsfirm.sl",
                    FirstName = "Test",
                    LastName = "Associate",
                    IsActive = true,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    RegistrationSource = RegistrationSource.AdminCreated,
                    RegistrationCompletedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow.AddDays(-190)
                },
                new() {
                    UserName = "client@testcompany.sl",
                    Email = "client@testcompany.sl",
                    FirstName = "Test",
                    LastName = "Client",
                    IsActive = true,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    RegistrationSource = RegistrationSource.AdminCreated,
                    RegistrationCompletedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow.AddDays(-180)
                }
            };

            // Create users
            foreach (var user in demoUsers)
            {
                var password = GetUserPassword(user.Email!);
                await userManager.CreateAsync(user, password);
                
                // Assign roles based on email
                if (user.Email == "admin@bettsfirm.sl")
                    await userManager.AddToRoleAsync(user, "Admin");
                else if (user.Email == "associate@bettsfirm.sl" || user.Email?.Contains("bettsfirm") == true)
                    await userManager.AddToRoleAsync(user, "Associate");
                else
                    await userManager.AddToRoleAsync(user, "Client");
            }

            string GetUserPassword(string email)
            {
                return email switch
                {
                    "admin@bettsfirm.sl" => "Admin123!",
                    "associate@bettsfirm.sl" => "Associate123!",
                    "client@testcompany.sl" => "Client123!",
                    _ => "Demo123!"
                };
            }

            // Refresh user references after creation
            var associate = await userManager.FindByEmailAsync("associate@bettsfirm.sl");
            var clientUser1 = await userManager.FindByEmailAsync("john.kamara@sierramining.sl");
            var clientUser2 = await userManager.FindByEmailAsync("fatima.sesay@freetownlogistics.sl");
            var testClientUser = await userManager.FindByEmailAsync("client@testcompany.sl");

            // Verify we have an associate user
            if (associate == null)
            {
                throw new InvalidOperationException("Associate user not found. Cannot seed demo data.");
            }

            // Create demo clients
            var clients = new List<Client>
            {
                new() {
                    UserId = clientUser1!.Id,
                    ClientNumber = "CLN-001-2024",
                    BusinessName = "Sierra Mining Corporation",
                    ContactPerson = "John Kamara",
                    Email = "john.kamara@sierramining.sl",
                    PhoneNumber = "+232-76-123-456",
                    Address = "15 Industrial Road, Freetown",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Large,
                    AnnualTurnover = 50000000m,
                    TIN = "TIN-001-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-150),
                    UpdatedDate = DateTime.UtcNow.AddDays(-10)
                },
                new() {
                    UserId = clientUser2!.Id,
                    ClientNumber = "CLN-002-2024",
                    BusinessName = "Freetown Logistics Ltd",
                    ContactPerson = "Fatima Sesay",
                    Email = "fatima.sesay@freetownlogistics.sl",
                    PhoneNumber = "+232-78-987-654",
                    Address = "32 Wellington Street, Freetown",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Medium,
                    AnnualTurnover = 15000000m,
                    TIN = "TIN-002-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-120),
                    UpdatedDate = DateTime.UtcNow.AddDays(-5)
                },
                new() {
                    UserId = null,
                    ClientNumber = "CLN-003-2024",
                    BusinessName = "Atlantic Petroleum Services",
                    ContactPerson = "Mohamed Conteh",
                    Email = "m.conteh@atlanticpetrol.sl",
                    PhoneNumber = "+232-79-555-123",
                    Address = "78 Siaka Stevens Street, Freetown",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Large,
                    AnnualTurnover = 35000000m,
                    TIN = "TIN-003-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-90),
                    UpdatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new() {
                    UserId = null,
                    ClientNumber = "CLN-004-2024",
                    BusinessName = "Diamond Mining Co Ltd",
                    ContactPerson = "Adama Kargbo",
                    Email = "a.kargbo@diamondmining.sl",
                    PhoneNumber = "+232-77-888-999",
                    Address = "45 Kono District, Eastern Province",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Medium,
                    AnnualTurnover = 8000000m,
                    TIN = "TIN-004-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-60),
                    UpdatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new() {
                    UserId = null,
                    ClientNumber = "CLN-005-2024",
                    BusinessName = "Kono Agricultural Enterprises",
                    ContactPerson = "Ibrahim Turay",
                    Email = "i.turay@konoagri.sl",
                    PhoneNumber = "+232-76-333-444",
                    Address = "12 Farm Road, Kono",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Small,
                    AnnualTurnover = 2500000m,
                    TIN = "TIN-005-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    UpdatedDate = DateTime.UtcNow
                },
                // Test client for Playwright tests
                new() {
                    UserId = testClientUser!.Id,
                    ClientNumber = "CLN-TEST-2024",
                    BusinessName = "Test Company Ltd",
                    ContactPerson = "Test Client",
                    Email = "client@testcompany.sl",
                    PhoneNumber = "+232-76-999-888",
                    Address = "123 Test Street, Freetown",
                    ClientType = ClientType.Corporation,
                    TaxpayerCategory = TaxpayerCategory.Medium,
                    AnnualTurnover = 5000000m,
                    TIN = "TIN-TEST-2024",
                    AssignedAssociateId = associate!.Id,
                    Status = ClientStatus.Active,
                    CreatedDate = DateTime.UtcNow.AddDays(-100),
                    UpdatedDate = DateTime.UtcNow.AddDays(-5)
                }
            };

            context.Clients.AddRange(clients);
            await context.SaveChangesAsync();

            // Create demo tax filings
            var taxFilings = new List<TaxFiling>();
            var currentYear = DateTime.UtcNow.Year;
            
            foreach (var client in clients)
            {
                // Create various tax filings for each client
                var filings = new List<TaxFiling>
                {
                    new() {
                        ClientId = client.ClientId,
                        TaxType = TaxType.IncomeTax,
                        TaxYear = currentYear - 1,
                        FilingDate = DateTime.UtcNow.AddDays(-45),
                        DueDate = new DateTime(currentYear, 3, 31),
                        Status = FilingStatus.Filed,
                        TaxLiability = client.AnnualTurnover * 0.25m,
                        FilingReference = $"IT-{client.ClientNumber}-{currentYear - 1}",
                        SubmittedById = associate!.Id,
                        SubmittedDate = DateTime.UtcNow.AddDays(-45),
                        CreatedDate = DateTime.UtcNow.AddDays(-60)
                    },
                    new() {
                        ClientId = client.ClientId,
                        TaxType = TaxType.GST,
                        TaxYear = currentYear,
                        FilingDate = DateTime.UtcNow.AddDays(-15),
                        DueDate = DateTime.UtcNow.AddDays(6), // Due soon
                        Status = FilingStatus.Submitted,
                        TaxLiability = client.AnnualTurnover * 0.15m,
                        FilingReference = $"GST-{client.ClientNumber}-{currentYear}",
                        SubmittedById = associate!.Id,
                        SubmittedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedDate = DateTime.UtcNow.AddDays(-20)
                    },
                    new() {
                        ClientId = client.ClientId,
                        TaxType = TaxType.PayrollTax,
                        TaxYear = currentYear,
                        FilingDate = DateTime.UtcNow.AddDays(-30),
                        DueDate = new DateTime(currentYear, 12, 31),
                        Status = FilingStatus.Filed,
                        TaxLiability = client.AnnualTurnover * 0.05m,
                        FilingReference = $"PT-{client.ClientNumber}-{currentYear}",
                        SubmittedById = associate!.Id,
                        SubmittedDate = DateTime.UtcNow.AddDays(-30),
                        CreatedDate = DateTime.UtcNow.AddDays(-35)
                    }
                };
                taxFilings.AddRange(filings);
            }

            context.TaxFilings.AddRange(taxFilings);
            await context.SaveChangesAsync();

            // Create demo payments
            var payments = new List<Payment>();
            foreach (var filing in taxFilings.Where(f => f.Status == FilingStatus.Filed))
            {
                payments.Add(new Payment
                {
                    ClientId = filing.ClientId,
                    TaxFilingId = filing.TaxFilingId,
                    Amount = filing.TaxLiability,
                    PaymentDate = filing.FilingDate.AddDays(10),
                    Method = PaymentMethod.BankTransfer,
                    PaymentReference = $"PAY-{filing.FilingReference}",
                    Status = PaymentStatus.Approved,
                    CreatedAt = filing.FilingDate.AddDays(5)
                });
            }

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();

            // Create demo tax years
            var taxYears = new List<TaxYear>();
            foreach (var client in clients)
            {
                // Create tax years for the last 2 years and current year
                for (int yearOffset = 2; yearOffset >= 0; yearOffset--)
                {
                    var year = currentYear - yearOffset;
                    var status = yearOffset switch
                    {
                        2 => TaxYearStatus.Paid,      // 2 years ago - fully paid
                        1 => TaxYearStatus.Filed,     // Last year - filed
                        0 => TaxYearStatus.Pending,   // Current year - pending
                        _ => TaxYearStatus.Draft
                    };

                    var filingDeadline = yearOffset == 0
                        ? DateTime.UtcNow.AddDays(45)  // Current year - upcoming deadline
                        : new DateTime(year + 1, 3, 31); // Past years

                    var dateFiled = yearOffset > 0
                        ? filingDeadline.AddDays(-10)  // Filed before deadline
                        : (DateTime?)null;              // Not yet filed

                    taxYears.Add(new TaxYear
                    {
                        ClientId = client.ClientId,
                        Year = year,
                        Status = status,
                        FilingDeadline = filingDeadline,
                        DateFiled = dateFiled,
                        IncomeTaxOwed = client.AnnualTurnover * 0.25m,
                        TaxLiability = client.AnnualTurnover * 0.25m,
                        CreatedDate = new DateTime(year, 1, 1),
                        UpdatedDate = dateFiled ?? DateTime.UtcNow
                    });
                }
            }

            context.TaxYears.AddRange(taxYears);
            await context.SaveChangesAsync();

            // Create demo documents
            var documents = new List<Document>();
            var documentTypes = new[]
            {
                DocumentType.TaxReturn,
                DocumentType.FinancialStatement,
                DocumentType.Receipt,
                DocumentType.SupportingDocument
            };

            foreach (var client in clients)
            {
                // Create 3-5 documents per client
                var docCount = new Random().Next(3, 6);
                for (int i = 0; i < docCount; i++)
                {
                    var docType = documentTypes[i % documentTypes.Length];
                    var uploadDate = DateTime.UtcNow.AddDays(-((double)(docCount - i) * 5));

                    documents.Add(new Document
                    {
                        ClientId = client.ClientId,
                        DocumentType = docType,
                        OriginalFileName = $"{client.BusinessName.Replace(" ", "_")}_{docType}_{i + 1}.pdf",
                        StoredFileName = $"{Guid.NewGuid()}.pdf",
                        FilePath = $"/documents/{client.ClientId}/{Guid.NewGuid()}.pdf",
                        ContentType = "application/pdf",
                        FileSize = new Random().Next(100000, 5000000), // Random size between 100KB and 5MB
                        Description = $"{docType} for {client.BusinessName}",
                        UploadedAt = uploadDate,
                        VerificationStatus = i < 2 ? DocumentVerificationStatus.Verified : DocumentVerificationStatus.NotRequested,
                        VerifiedById = i < 2 ? associate!.Id : null,
                        VerifiedAt = i < 2 ? uploadDate.AddDays(1) : null,
                        TaxYear = currentYear - (i % 2),
                        Tags = new List<string> { docType.ToString(), client.TaxpayerCategory.ToString() },
                        CreatedAt = uploadDate,
                        UpdatedAt = uploadDate
                    });
                }
            }

            context.Documents.AddRange(documents);
            await context.SaveChangesAsync();

            // Create demo audit logs
            var auditLogs = new List<Models.Security.AuditLog>();
            foreach (var client in clients)
            {
                auditLogs.AddRange(new[]
                {
                    new Models.Security.AuditLog
                    {
                        UserId = associate!.Id,
                        Action = "Client Created",
                        Entity = "Client",
                        EntityId = client.ClientId.ToString(),
                        Operation = AuditOperation.Create,
                        Description = $"Created client: {client.BusinessName}",
                        Severity = AuditSeverity.Low,
                        Category = AuditCategory.DataModification,
                        IpAddress = "127.0.0.1",
                        Timestamp = client.CreatedDate
                    },
                    new Models.Security.AuditLog
                    {
                        UserId = associate!.Id,
                        Action = "Client Updated",
                        Entity = "Client",
                        EntityId = client.ClientId.ToString(),
                        Operation = AuditOperation.Update,
                        Description = $"Updated client information for: {client.BusinessName}",
                        Severity = AuditSeverity.Low,
                        Category = AuditCategory.DataModification,
                        IpAddress = "127.0.0.1",
                        Timestamp = client.UpdatedDate
                    }
                });
            }

            context.AuditLogs.AddRange(auditLogs);
            await context.SaveChangesAsync();
        }
    }
}
