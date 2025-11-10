# Database Setup and Migration Guide

## Overview

This guide provides step-by-step instructions to complete the Entity Framework integration for the BettsTax application. The database schema has been created, and partial service implementation is complete.

---

## Current Implementation Status

### ✅ Completed

1. **Entity Framework Packages Installed** (`BettsTax.Web.csproj`)
   - Microsoft.EntityFrameworkCore.SqlServer v8.0.11
   - Microsoft.EntityFrameworkCore.Design v8.0.11
   - Microsoft.EntityFrameworkCore.Tools v8.0.11

2. **Entity Models Created** (`BettsTax.Web/Models/Entities/`)
   - `User.cs` - User accounts with role-based access
   - `Client.cs` - Client/company records
   - `Payment.cs` - Payment transactions
   - `Document.cs` - Document storage metadata
   - `Filing.cs` - Tax filing records
   - `FilingSchedule.cs` - Filing line items
   - `FilingDocument.cs` - Filing attachments
   - `FilingHistory.cs` - Filing audit trail

3. **DbContext Implemented** (`BettsTax.Web/Data/ApplicationDbContext.cs`)
   - All DbSets configured
   - Entity relationships defined
   - Fluent API configuration for all entities
   - Indexes and constraints configured

4. **Program.cs Updated**
   - DbContext registered with dependency injection
   - HttpContextAccessor added for user context
   - Connection string configuration

5. **Services Updated with Database Queries**
   - ✅ `ClientService.cs` - Full database implementation
   - ✅ `PaymentService.cs` - Full database implementation
   - ⚠️ Remaining services still use mock data

6. **Registration DTOs Created** (`BettsTax.Web/Models/AuthModels.cs`)
   - `RegisterRequest` - User registration input
   - `RegisterResponse` - Registration result
   - `IAuthenticationService` interface updated

###  ⚠️ Remaining Tasks

1. Update `AuthenticationService` to use database instead of in-memory users
2. Update `DashboardService` with database queries
3. Update `DocumentService` with database queries
4. Update `FilingService` with database queries
5. Update `KpiService` with database queries
6. Create database migrations
7. Create database seeder for demo data
8. Add registration endpoint to `AuthController`
9. Configure `appsettings.json` with connection string
10. Test complete flow

---

## Database Schema

###  Table: Users

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| Email | NVARCHAR(255) | NOT NULL, UNIQUE |
| PasswordHash | NVARCHAR(500) | NOT NULL |
| Role | NVARCHAR(50) | NOT NULL (Admin/Staff/Client) |
| ClientId | INT | NULL, FOREIGN KEY → Clients.Id |
| IsDemo | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| LastLoginAt | DATETIME2 | NULL |

### Table: Clients

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| Name | NVARCHAR(255) | NOT NULL |
| Tin | NVARCHAR(50) | NOT NULL, UNIQUE |
| Segment | NVARCHAR(50) | NULL |
| Industry | NVARCHAR(100) | NULL |
| Status | NVARCHAR(50) | NULL |
| ComplianceScore | DECIMAL(5,2) | NOT NULL |
| AssignedTo | NVARCHAR(255) | NULL |
| IsDemo | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| UpdatedAt | DATETIME2 | NULL |

### Table: Payments

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| ClientId | INT | NOT NULL, FOREIGN KEY → Clients.Id |
| TaxType | NVARCHAR(100) | NOT NULL |
| Period | NVARCHAR(100) | NULL |
| Amount | DECIMAL(18,2) | NOT NULL |
| Method | NVARCHAR(100) | NULL |
| Status | NVARCHAR(50) | NULL |
| Date | DATETIME2 | NOT NULL |
| ReceiptNo | NVARCHAR(100) | NULL |
| IsDemo | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |

### Table: Documents

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| Name | NVARCHAR(500) | NOT NULL |
| Type | NVARCHAR(100) | NULL |
| ClientId | INT | NOT NULL, FOREIGN KEY → Clients.Id |
| Year | INT | NOT NULL |
| TaxType | NVARCHAR(100) | NULL |
| Version | INT | NOT NULL, DEFAULT 1 |
| UploadedBy | NVARCHAR(255) | NULL |
| UploadDate | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| Hash | NVARCHAR(255) | NULL |
| Status | NVARCHAR(50) | NULL |
| FilePath | NVARCHAR(1000) | NULL |
| FileSize | BIGINT | NOT NULL |
| IsDemo | BIT | NOT NULL, DEFAULT 0 |

### Table: Filings

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| ClientId | INT | NOT NULL, FOREIGN KEY → Clients.Id |
| TaxType | NVARCHAR(100) | NOT NULL |
| Period | NVARCHAR(100) | NOT NULL |
| Status | NVARCHAR(50) | NULL |
| TotalSales | DECIMAL(18,2) | NULL |
| TaxableSales | DECIMAL(18,2) | NULL |
| GstRate | DECIMAL(5,2) | NULL |
| OutputTax | DECIMAL(18,2) | NULL |
| InputTaxCredit | DECIMAL(18,2) | NULL |
| NetGstPayable | DECIMAL(18,2) | NULL |
| Notes | NVARCHAR(MAX) | NULL |
| IsDemo | BIT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| UpdatedAt | DATETIME2 | NULL |
| SubmittedAt | DATETIME2 | NULL |

### Table: FilingSchedules

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| FilingId | INT | NOT NULL, FOREIGN KEY → Filings.Id |
| Description | NVARCHAR(500) | NOT NULL |
| Amount | DECIMAL(18,2) | NOT NULL |
| Taxable | DECIMAL(18,2) | NOT NULL |
| Order | INT | NOT NULL |

### Table: FilingDocuments

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| FilingId | INT | NOT NULL, FOREIGN KEY → Filings.Id |
| Name | NVARCHAR(500) | NOT NULL |
| Version | INT | NOT NULL |
| UploadedBy | NVARCHAR(255) | NULL |
| Date | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| FilePath | NVARCHAR(1000) | NULL |

### Table: FilingHistories

| Column | Type | Constraints |
|--------|------|-------------|
| Id | INT | PRIMARY KEY, IDENTITY |
| FilingId | INT | NOT NULL, FOREIGN KEY → Filings.Id |
| Date | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() |
| User | NVARCHAR(255) | NOT NULL |
| Action | NVARCHAR(100) | NOT NULL |
| Changes | NVARCHAR(2000) | NULL |

---

## Step-by-Step Setup Instructions

### Step 1: Configure Connection String

Edit `BettsTax/BettsTax.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BettsTaxDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "Secret": "your-super-secret-key-minimum-32-characters-long-for-production",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  }
}
```

For production, use SQL Server:
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=BettsTaxDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

### Step 2: Create Initial Migration

```bash
cd BettsTax/BettsTax.Web
dotnet ef migrations add InitialCreate
```

This generates migration files in `Migrations/` folder.

### Step 3: Update Database

```bash
dotnet ef database update
```

This creates the database and all tables.

### Step 4: Create Database Seeder

Create `BettsTax/BettsTax.Web/Data/DatabaseSeeder.cs`:

```csharp
using BettsTax.Web.Data;
using BettsTax.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDemoDataAsync()
    {
        // Check if demo data already exists
        if (await _context.Users.AnyAsync(u => u.IsDemo))
        {
            _logger.LogInformation("Demo data already exists. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding demo data...");

        // 1. Create demo users
        var demoUsers = new List<User>
        {
            new() { Email = "staff@bettsfirm.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), Role = "Staff", IsDemo = true, CreatedAt = DateTime.UtcNow },
            new() { Email = "admin@bettsfirm.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), Role = "Admin", IsDemo = true, CreatedAt = DateTime.UtcNow }
        };

        _context.Users.AddRange(demoUsers);
        await _context.SaveChangesAsync();

        // 2. Create demo clients
        var demoClients = new List<Client>
        {
            new() { Name = "Sierra Leone Breweries Ltd", Tin = "TIN001234567", Segment = "Large", Industry = "Manufacturing", Status = "Active", ComplianceScore = 95, AssignedTo = "John Kamara", IsDemo = true },
            new() { Name = "Standard Chartered Bank SL", Tin = "TIN002345678", Segment = "Large", Industry = "Financial Services", Status = "Active", ComplianceScore = 98, AssignedTo = "Sarah Conteh", IsDemo = true },
            new() { Name = "Orange Sierra Leone", Tin = "TIN003456789", Segment = "Large", Industry = "Telecommunications", Status = "Active", ComplianceScore = 92, AssignedTo = "Mohamed Sesay", IsDemo = true },
            new() { Name = "Rokel Commercial Bank", Tin = "TIN004567890", Segment = "Medium", Industry = "Financial Services", Status = "Active", ComplianceScore = 88, AssignedTo = "Fatmata Koroma", IsDemo = true },
            new() { Name = "Freetown Terminal Ltd", Tin = "TIN005678901", Segment = "Medium", Industry = "Logistics", Status = "Under Review", ComplianceScore = 75, AssignedTo = "Abdul Rahman", IsDemo = true }
        };

        _context.Clients.AddRange(demoClients);
        await _context.SaveChangesAsync();

        // 3. Create demo client user
        var clientUser = new User
        {
            Email = "client1@slbreweries.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Client",
            ClientId = demoClients[0].Id,
            IsDemo = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(clientUser);
        await _context.SaveChangesAsync();

        // 4. Create demo payments
        var demoPayments = new List<Payment>
        {
            new() { ClientId = demoClients[0].Id, TaxType = "VAT", Period = "Q1 2025", Amount = 45000, Method = "Bank Transfer", Status = "Completed", Date = new DateTime(2025, 1, 15), ReceiptNo = "RCP-2025-001", IsDemo = true },
            new() { ClientId = demoClients[1].Id, TaxType = "Corporate Tax", Period = "Q4 2024", Amount = 125000, Method = "Direct Debit", Status = "Completed", Date = new DateTime(2025, 1, 10), ReceiptNo = "RCP-2025-002", IsDemo = true },
            new() { ClientId = demoClients[2].Id, TaxType = "Withholding Tax", Period = "December 2024", Amount = 32000, Method = "Bank Transfer", Status = "Pending", Date = new DateTime(2025, 1, 20), ReceiptNo = "RCP-2025-003", IsDemo = true }
        };

        _context.Payments.AddRange(demoPayments);
        await _context.SaveChangesAsync();

        // 5. Create demo documents
        var demoDocuments = new List<Document>
        {
            new() { Name = "Tax Return 2024.pdf", Type = "Tax Return", ClientId = demoClients[0].Id, Year = 2024, TaxType = "Corporate Tax", Version = 1, UploadedBy = "John Kamara", UploadDate = new DateTime(2025, 1, 15), Hash = "a1b2c3d4e5f6", Status = "Approved", FileSize = 1024000, IsDemo = true },
            new() { Name = "Financial Statements Q4.xlsx", Type = "Financial Statement", ClientId = demoClients[1].Id, Year = 2024, TaxType = "Corporate Tax", Version = 2, UploadedBy = "Sarah Conteh", UploadDate = new DateTime(2025, 1, 10), Hash = "b2c3d4e5f6g7", Status = "Pending Review", FileSize = 512000, IsDemo = true }
        };

        _context.Documents.AddRange(demoDocuments);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Demo data seeding completed successfully.");
    }
}
```

### Step 5: Register Seeder in Program.cs

Add after `var app = builder.Build();`:

```csharp
// Seed demo data on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    var seeder = new DatabaseSeeder(context, logger);

    // Ensure database is created
    await context.Database.MigrateAsync();

    // Seed demo data
    await seeder.SeedDemoDataAsync();
}
```

### Step 6: Update AuthenticationService to Use Database

Replace the in-memory user store in `BettsTax.Web/Services/AuthenticationService.cs`:

```csharp
private readonly ApplicationDbContext _context;

public AuthenticationService(
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthenticationService> logger,
    ApplicationDbContext context)
{
    _jwtSettings = jwtSettings.Value;
    _logger = logger;
    _context = context;
}

public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
{
    // Find user in database
    var user = await _context.Users
        .Include(u => u.Client)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user == null)
    {
        return new LoginResponse
        {
            Success = false,
            Message = "Invalid email or password"
        };
    }

    // Verify password
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return new LoginResponse
        {
            Success = false,
            Message = "Invalid email or password"
        };
    }

    // Update last login
    user.LastLoginAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // Generate tokens...
    // (rest of token generation code)
}

public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
{
    // Check if user already exists
    var existingUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (existingUser != null)
    {
        return new RegisterResponse
        {
            Success = false,
            Message = "User with this email already exists"
        };
    }

    // Create client if company info provided
    int? clientId = null;
    if (!string.IsNullOrWhiteSpace(request.CompanyName))
    {
        var client = new Client
        {
            Name = request.CompanyName,
            Tin = request.Tin ?? "",
            Industry = request.Industry ?? "",
            Segment = "Small",
            Status = "Pending Verification",
            ComplianceScore = 0,
            AssignedTo = "",
            IsDemo = false
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        clientId = client.Id;
    }

    // Create user
    var user = new User
    {
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = "Client",
        ClientId = clientId,
        IsDemo = false,
        CreatedAt = DateTime.UtcNow
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return new RegisterResponse
    {
        Success = true,
        Message = "Registration successful",
        User = new UserInfo
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            Role = user.Role,
            ClientId = user.ClientId
        }
    };
}
```

### Step 7: Add Registration Endpoint to AuthController

Add to `BettsTax.Web/Controllers/AuthController.cs`:

```csharp
[HttpPost("register")]
[AllowAnonymous]
[ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    try
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new RegisterResponse
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var response = await _authService.RegisterAsync(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        _logger.LogInformation("Successful registration for email: {Email}", request.Email);
        return StatusCode(201, response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during registration");
        return StatusCode(500, new RegisterResponse
        {
            Success = false,
            Message = "An error occurred during registration"
        });
    }
}
```

### Step 8: Run and Test

```bash
cd BettsTax/BettsTax.Web
dotnet run
```

Access Swagger UI: `https://localhost:5001/swagger`

Test endpoints:
- POST /api/auth/register - Register new user
- POST /api/auth/login - Login with demo or real user
- GET /api/clients - List clients
- GET /api/payments - List payments

---

## Demo User Credentials

After seeding, you can login with:

**Staff User:**
- Email: `staff@bettsfirm.com`
- Password: `password`

**Admin User:**
- Email: `admin@bettsfirm.com`
- Password: `password`

**Client User:**
- Email: `client1@slbreweries.com`
- Password: `password`

---

## Verification Checklist

- [ ] Database created successfully
- [ ] All 8 tables exist with correct schema
- [ ] Demo users seeded (3 users)
- [ ] Demo clients seeded (5 clients)
- [ ] Demo payments seeded (3 payments)
- [ ] Demo documents seeded (2 documents)
- [ ] Login works with demo credentials
- [ ] Registration creates new user with empty data
- [ ] Client service returns database results
- [ ] Payment service returns database results
- [ ] Authorization filters data by client correctly

---

## Next Steps

1. Update remaining services (Dashboard, Document, Filing, KPI) with database queries
2. Test all API endpoints thoroughly
3. Deploy to staging environment
4. Perform security audit
5. Load testing and performance optimization
6. Production deployment

---

## Troubleshooting

**Migration Failed:**
```bash
dotnet ef migrations remove
dotnet ef migrations add InitialCreate --force
```

**Database Connection Issues:**
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure SQL Server allows TCP/IP connections

**Seeding Fails:**
- Check logs for detailed error messages
- Verify all foreign key relationships
- Ensure BCrypt.Net-Next package is installed

---

**Status**: Database schema complete, partial implementation done. Follow this guide to complete integration.
