# Backend API Implementation Guide

## Overview
This guide provides detailed instructions for implementing the backend API endpoints required by the production-ready frontend.

**Prerequisites:**
- .NET 8.0 SDK
- Entity Framework Core
- SQL Server or PostgreSQL
- Existing backend structure in `BettsTax/BettsTax.Web`

---

## Architecture Pattern

The existing backend follows this structure:
```
BettsTax/
├── BettsTax.Core/           # Domain layer
│   ├── DTOs/                # Data Transfer Objects
│   └── Services/Interfaces/ # Service contracts
├── BettsTax.Web/            # Presentation layer
│   ├── Controllers/         # API endpoints
│   ├── Services/            # Business logic
│   └── Models/              # View models
└── BettsTax.Tests/          # Unit tests
```

**Key Patterns Used:**
- Dependency Injection
- Repository Pattern (implied)
- DTO Pattern
- Service Layer
- Authorization via `IAuthorizationService`

---

## Priority 1: Dashboard Endpoints

### 1. Create Dashboard DTOs

**File:** `BettsTax.Core/DTOs/Dashboard/DashboardDto.cs`

```csharp
namespace BettsTax.Core.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public decimal ClientComplianceRate { get; set; }
    public int FilingTimeliness { get; set; }
    public decimal PaymentCompletion { get; set; }
    public decimal DocumentCompliance { get; set; }
}

public class FilingTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int OnTime { get; set; }
    public int Late { get; set; }
}

public class ComplianceDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class UpcomingDeadlineDto
{
    public string Client { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public int DaysLeft { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentActivityDto
{
    public string Time { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}
```

### 2. Create Dashboard Service Interface

**File:** `BettsTax.Core/Services/Interfaces/IDashboardService.cs`

```csharp
using BettsTax.Core.DTOs.Dashboard;

namespace BettsTax.Core.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(int? clientId = null);
    Task<List<FilingTrendDto>> GetFilingTrendsAsync(int? clientId = null, int months = 6);
    Task<List<ComplianceDistributionDto>> GetComplianceDistributionAsync(int? clientId = null);
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int? clientId = null, int limit = 10);
    Task<List<RecentActivityDto>> GetRecentActivityAsync(int? clientId = null, int limit = 10);
}
```

### 3. Implement Dashboard Service

**File:** `BettsTax.Web/Services/DashboardService.cs`

```csharp
using BettsTax.Core.DTOs.Dashboard;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(int? clientId = null)
    {
        // Implement logic to calculate metrics from database
        // Example implementation:
        var query = _context.Filings.AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(f => f.ClientId == clientId.Value);
        }

        var totalFilings = await query.CountAsync();
        var compliantFilings = await query.Where(f => f.IsCompliant).CountAsync();

        return new DashboardMetricsDto
        {
            ClientComplianceRate = totalFilings > 0
                ? (decimal)compliantFilings / totalFilings * 100
                : 0,
            FilingTimeliness = await CalculateAverageTimeliness(clientId),
            PaymentCompletion = await CalculatePaymentCompletion(clientId),
            DocumentCompliance = await CalculateDocumentCompliance(clientId)
        };
    }

    public async Task<List<FilingTrendDto>> GetFilingTrendsAsync(
        int? clientId = null,
        int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var query = _context.Filings
            .Where(f => f.FilingDate >= startDate);

        if (clientId.HasValue)
        {
            query = query.Where(f => f.ClientId == clientId.Value);
        }

        var trends = await query
            .GroupBy(f => new { f.FilingDate.Year, f.FilingDate.Month })
            .Select(g => new FilingTrendDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:00}",
                OnTime = g.Count(f => f.IsOnTime),
                Late = g.Count(f => !f.IsOnTime)
            })
            .OrderBy(t => t.Month)
            .ToListAsync();

        return trends;
    }

    // Implement other methods...
}
```

### 4. Create Dashboard Controller

**File:** `BettsTax.Web/Controllers/DashboardController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        Services.IAuthorizationService authorizationService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMetrics([FromQuery] int? clientId = null)
    {
        try
        {
            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Access denied"
                });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var metrics = await _dashboardService.GetMetricsAsync(effectiveClientId);

            return Ok(new
            {
                success = true,
                data = metrics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving metrics"
            });
        }
    }

    [HttpGet("filing-trends")]
    public async Task<IActionResult> GetFilingTrends(
        [FromQuery] int? clientId = null,
        [FromQuery] int months = 6)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var trends = await _dashboardService.GetFilingTrendsAsync(effectiveClientId, months);

            return Ok(new { success = true, data = trends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filing trends");
            return StatusCode(500, new { success = false, message = "Error retrieving trends" });
        }
    }

    // Implement other endpoints (compliance-distribution, upcoming-deadlines, recent-activity)
}
```

### 5. Register Services in Program.cs

```csharp
// Add to Program.cs
builder.Services.AddScoped<IDashboardService, DashboardService>();
```

---

## Priority 2: Clients Endpoints

### 1. Create Client DTOs

**File:** `BettsTax.Core/DTOs/Client/ClientDto.cs`

```csharp
namespace BettsTax.Core.DTOs.Client;

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ComplianceScore { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
}

public class UpdateClientDto
{
    public string? Name { get; set; }
    public string? Segment { get; set; }
    public string? Industry { get; set; }
    public string? Status { get; set; }
    public string? AssignedTo { get; set; }
}
```

### 2. Create Clients Controller

**File:** `BettsTax.Web/Controllers/ClientsController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.DTOs.Client;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IClientService clientService,
        Services.IAuthorizationService authorizationService,
        ILogger<ClientsController> logger)
    {
        _clientService = clientService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] string? search = null,
        [FromQuery] string? segment = null,
        [FromQuery] string? status = null)
    {
        try
        {
            // Only staff/admin can list all clients
            if (!_authorizationService.IsStaffOrAdmin(User))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var clients = await _clientService.GetClientsAsync(search, segment, status);

            return Ok(new { success = true, data = clients });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return StatusCode(500, new { success = false, message = "Error retrieving clients" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, id))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var client = await _clientService.GetClientByIdAsync(id);

            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            return Ok(new { success = true, data = client });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving client" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto)
    {
        try
        {
            var client = await _clientService.CreateClientAsync(dto);

            return CreatedAtAction(
                nameof(GetClient),
                new { id = client.Id },
                new { success = true, data = client });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            return StatusCode(500, new { success = false, message = "Error creating client" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
    {
        try
        {
            var client = await _clientService.UpdateClientAsync(id, dto);

            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            return Ok(new { success = true, data = client });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Error updating client" });
        }
    }
}
```

---

## Priority 3: Payments Endpoints

### 1. Create Payment DTOs

**File:** `BettsTax.Core/DTOs/Payment/PaymentDto.cs`

```csharp
namespace BettsTax.Core.DTOs.Payment;

public class PaymentDto
{
    public int Id { get; set; }
    public string Client { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
}

public class PaymentSummaryDto
{
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalOverdue { get; set; }
}

public class CreatePaymentDto
{
    public int ClientId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}
```

### 2. Create Payments Controller

**File:** `BettsTax.Web/Controllers/PaymentsController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        Services.IAuthorizationService authorizationService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? taxType = null,
        [FromQuery] int? clientId = null)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var payments = await _paymentService.GetPaymentsAsync(
                search, status, taxType, effectiveClientId);

            return Ok(new { success = true, data = payments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { success = false, message = "Error retrieving payments" });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetPaymentSummary([FromQuery] int? clientId = null)
    {
        try
        {
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var summary = await _paymentService.GetPaymentSummaryAsync(effectiveClientId);

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment summary");
            return StatusCode(500, new { success = false, message = "Error retrieving summary" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentAsync(dto);

            return Ok(new { success = true, data = payment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { success = false, message = "Error creating payment" });
        }
    }
}
```

---

## Database Seeding

### Create Seed Data

**File:** `BettsTax.Web/Data/SeedData.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Data;

public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedClientsAsync(context);
        await SeedPaymentsAsync(context);
        await SeedDeadlinesAsync(context);
        await SeedActivityLogsAsync(context);
    }

    private static async Task SeedClientsAsync(ApplicationDbContext context)
    {
        if (await context.Clients.AnyAsync())
            return;

        var clients = new List<Client>
        {
            new Client
            {
                Name = "ABC Corporation Ltd",
                Tin = "1234567890",
                Segment = "Corporate",
                Industry = "Manufacturing",
                Status = "Active",
                ComplianceScore = 95,
                AssignedTo = "Jane Smith",
                CreatedAt = DateTime.UtcNow
            },
            new Client
            {
                Name = "XYZ Trading Company",
                Tin = "0987654321",
                Segment = "SME",
                Industry = "Retail",
                Status = "Active",
                ComplianceScore = 87,
                AssignedTo = "John Doe",
                CreatedAt = DateTime.UtcNow
            },
            // Add more clients...
        };

        context.Clients.AddRange(clients);
        await context.SaveChangesAsync();
    }

    // Implement other seed methods...
}
```

### Run Seeding in Program.cs

```csharp
// Add to Program.cs after app.Run()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.SeedAsync(context);
}
```

---

## Testing Checklist

- [ ] All endpoints return proper status codes
- [ ] Authorization checks work correctly
- [ ] Client users can only access their own data
- [ ] Staff/Admin can access all data
- [ ] Proper error handling and logging
- [ ] DTOs match frontend TypeScript interfaces
- [ ] Pagination works for large datasets
- [ ] Filtering and search work correctly

---

## API Documentation

Generate API documentation with Swagger/OpenAPI:

```csharp
// Add to Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Betts Tax API",
        Version = "v1",
        Description = "Client Tax Information System API"
    });
});

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI();
```

---

## Security Considerations

1. **Always validate authorization** using `IAuthorizationService.CanAccessClientData()`
2. **Auto-filter for client users** - they should only see their own data
3. **Validate input** - use Data Annotations and FluentValidation
4. **Rate limiting** - implement to prevent abuse
5. **Audit logging** - log all data access and modifications
6. **HTTPS only** - enforce in production
7. **CORS** - configure properly for frontend domain

---

## Deployment Steps

1. **Update appsettings.json** with production connection string
2. **Run migrations** on production database
3. **Seed initial data** if needed
4. **Configure CORS** for production frontend URL
5. **Enable logging** and monitoring
6. **Set up health checks** endpoint
7. **Configure load balancing** if needed

---

## Performance Optimization

1. **Add indexes** on frequently queried fields
2. **Use async/await** consistently
3. **Implement caching** (Redis or in-memory)
4. **Use projection** - select only needed fields
5. **Enable response compression**
6. **Use connection pooling**
7. **Monitor query performance** with logging

---

## Next Steps

1. ✅ Implement Dashboard endpoints (Priority 1)
2. ✅ Implement Clients endpoints (Priority 1)
3. ✅ Implement Payments endpoints (Priority 1)
4. ⚠️ Implement remaining endpoints (Documents, Filings, KPIs, Chat, Admin)
5. ⚠️ Write unit tests for all services
6. ⚠️ Write integration tests for all endpoints
7. ⚠️ Set up CI/CD pipeline
8. ⚠️ Deploy to staging environment

---

## Support & Resources

- **Entity Framework Core Docs:** https://learn.microsoft.com/en-us/ef/core/
- **ASP.NET Core Docs:** https://learn.microsoft.com/en-us/aspnet/core/
- **Best Practices:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices

---

**Document Version:** 1.0
**Last Updated:** 2025-11-10
**Author:** Development Team
