# Database Seeding Strategy

## Overview

The system supports two types of users with different data initialization:

1. **Demo Users** (Seeded): Pre-populated with realistic data for demos and testing
2. **Real Users** (Sign-up): Start with empty database, build their own data organically

## Database Schema Requirements

### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(50) NOT NULL, -- 'Admin', 'Staff', 'Client'
    ClientId INT NULL, -- Foreign key for client users
    IsDemo BIT NOT NULL DEFAULT 0, -- Flag for demo users
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### Seed Data Structure

#### Demo Users (Pre-populated Data)

**Staff Users:**
- `staff@bettsfirm.com` (Password: secure-demo-password)
- `admin@bettsfirm.com` (Password: secure-demo-password)

**Demo Clients:**
- Sierra Leone Breweries Ltd (TIN: TIN001234567)
- Standard Chartered Bank SL (TIN: TIN002345678)
- Orange Sierra Leone (TIN: TIN003456789)
- Rokel Commercial Bank (TIN: TIN004567890)
- Freetown Terminal Ltd (TIN: TIN005678901)

**Demo Client Users:**
- `client1@slbreweries.com` → Linked to Sierra Leone Breweries Ltd
- `client2@sc.com` → Linked to Standard Chartered Bank SL

#### Real Users (Empty Data)

When a real user signs up:
1. Create user record with `IsDemo = false`
2. No pre-populated clients, payments, documents, or filings
3. User must create their own data through the application

## Service Layer Implementation

### Current State (Mock Data)
Services return hardcoded mock data for development.

### Future State (Database Integration)

All services should follow this pattern:

```csharp
public async Task<List<ClientDto>> GetClientsAsync(
    string? searchTerm = null,
    string? segment = null,
    string? status = null,
    int? clientId = null)
{
    // Query database based on user context
    var query = _context.Clients.AsQueryable();

    // Auto-filter for non-demo users
    var currentUser = _httpContextAccessor.HttpContext?.User;
    var isDemo = currentUser?.FindFirst("IsDemo")?.Value == "true";

    if (!isDemo && clientId.HasValue)
    {
        // Real users only see their own data
        query = query.Where(c => c.Id == clientId.Value);
    }
    else if (!isDemo && !_authorizationService.IsStaffOrAdmin(currentUser))
    {
        // Real client users only see their assigned client
        var userClientId = _authorizationService.GetUserClientId(currentUser);
        query = query.Where(c => c.Id == userClientId);
    }
    // Demo staff/admin users see all demo data
    // Real staff/admin users see all real data + demo data (optional)

    // Apply search filters
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(c => c.Name.Contains(searchTerm) || c.Tin.Contains(searchTerm));
    }

    // Execute query and map to DTOs
    var clients = await query.ToListAsync();
    return _mapper.Map<List<ClientDto>>(clients);
}
```

## Seeding Script Example

### Entity Framework Seeding

Create a `DatabaseSeeder.cs` class:

```csharp
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public async Task SeedDemoDataAsync()
    {
        // Only seed if no demo users exist
        if (await _context.Users.AnyAsync(u => u.IsDemo))
            return;

        // 1. Create demo staff user
        var staffUser = new User
        {
            Email = "staff@bettsfirm.com",
            Role = "Staff",
            IsDemo = true
        };
        staffUser.PasswordHash = _passwordHasher.HashPassword(staffUser, "Demo123!@#");
        _context.Users.Add(staffUser);

        // 2. Create demo clients
        var demoClients = new List<Client>
        {
            new() { Name = "Sierra Leone Breweries Ltd", Tin = "TIN001234567", Segment = "Large", Industry = "Manufacturing", Status = "Active", ComplianceScore = 95, AssignedTo = "John Kamara", IsDemo = true },
            new() { Name = "Standard Chartered Bank SL", Tin = "TIN002345678", Segment = "Large", Industry = "Financial Services", Status = "Active", ComplianceScore = 98, AssignedTo = "Sarah Conteh", IsDemo = true },
            // ... more demo clients
        };
        _context.Clients.AddRange(demoClients);
        await _context.SaveChangesAsync();

        // 3. Create demo client users
        var clientUser = new User
        {
            Email = "client1@slbreweries.com",
            Role = "Client",
            ClientId = demoClients[0].Id,
            IsDemo = true
        };
        clientUser.PasswordHash = _passwordHasher.HashPassword(clientUser, "Demo123!@#");
        _context.Users.Add(clientUser);

        // 4. Create demo payments
        var demoPayments = new List<Payment>
        {
            new() { ClientId = demoClients[0].Id, TaxType = "VAT", Period = "Q1 2025", Amount = 45000, Method = "Bank Transfer", Status = "Completed", Date = new DateTime(2025, 1, 15), ReceiptNo = "RCP-2025-001", IsDemo = true },
            // ... more demo payments
        };
        _context.Payments.AddRange(demoPayments);

        // 5. Create demo documents, filings, etc.
        // ... similar pattern

        await _context.SaveChangesAsync();
    }
}
```

### Running the Seeder

In `Program.cs`:

```csharp
var app = builder.Build();

// Seed demo data on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedDemoDataAsync();
}
```

## User Registration Flow

### New User Sign-Up

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
    // 1. Create user with IsDemo = false
    var newUser = new User
    {
        Email = dto.Email,
        Role = "Client", // Default role for new registrations
        IsDemo = false, // Real user
        ClientId = null // Will be set after onboarding
    };

    // 2. Hash password and save
    newUser.PasswordHash = _passwordHasher.HashPassword(newUser, dto.Password);
    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    // 3. Create associated client record (if applicable)
    if (dto.CompanyName != null)
    {
        var client = new Client
        {
            Name = dto.CompanyName,
            Tin = dto.Tin,
            Status = "Pending Verification",
            ComplianceScore = 0,
            IsDemo = false // Real client
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        // Link user to client
        newUser.ClientId = client.Id;
        await _context.SaveChangesAsync();
    }

    // 4. User now has empty data - no payments, documents, filings
    return Ok(new { success = true, message = "Registration successful" });
}
```

## Demo Data Identification

All tables should have an `IsDemo` column:

```sql
-- Clients
ALTER TABLE Clients ADD IsDemo BIT NOT NULL DEFAULT 0;

-- Payments
ALTER TABLE Payments ADD IsDemo BIT NOT NULL DEFAULT 0;

-- Documents
ALTER TABLE Documents ADD IsDemo BIT NOT NULL DEFAULT 0;

-- Filings
ALTER TABLE Filings ADD IsDemo BIT NOT NULL DEFAULT 0;

-- ... etc for all major tables
```

## Data Isolation

### Demo Users
- See only demo data (IsDemo = true)
- Cannot interact with real user data
- Useful for training and demonstrations

### Real Users
- See only their own data (IsDemo = false)
- Start with empty tables
- Build data organically through usage
- Cannot see demo data (optional - can be enabled for staff)

### Staff/Admin Users
- Real staff: See all real user data
- Demo staff: See all demo data
- Optionally, real staff can toggle to view demo data for training purposes

## Security Considerations

1. **Demo User Credentials**: Change default passwords in production
2. **Data Cleanup**: Provide ability to reset demo data periodically
3. **Access Control**: Ensure demo users cannot access production data
4. **Audit Logging**: Log all demo user activities separately

## Migration Plan

### Phase 1: Current (Mock Data)
- All services return hardcoded mock data
- No database queries

### Phase 2: Database Integration
- Replace mock data with Entity Framework queries
- Implement `IsDemo` flag filtering
- Create seeding scripts

### Phase 3: Production Launch
- Seed demo data for training accounts
- Enable user registration with empty data
- Monitor and optimize queries

## Testing Strategy

### Unit Tests
- Test services with both demo and real user contexts
- Verify data isolation between demo and real users
- Test empty data scenarios for new users

### Integration Tests
- Test complete user registration flow
- Verify demo data seeding
- Test authorization boundaries

### Manual Testing Checklist
- [ ] Demo user can log in and see pre-populated data
- [ ] New user registration creates empty account
- [ ] Real users cannot see demo data
- [ ] Demo users cannot see real data
- [ ] Staff can see all data in their context (demo or real)
- [ ] Client users only see their own organization's data

## Next Steps

1. Design complete database schema with `IsDemo` columns
2. Implement Entity Framework DbContext and entities
3. Create DatabaseSeeder class with all demo data
4. Update all service implementations to query database
5. Add user registration endpoint with empty data initialization
6. Test data isolation thoroughly
7. Deploy and monitor

---

**Key Principle**: Demo data is for demos and training only. Real users build their own data from scratch.
