# GitHub Copilot Instructions for Betts CTIS

This file provides guidance to GitHub Copilot when working with code in this repository.

## Project Overview

**Betts CTIS** (Client Tax Information System) is a comprehensive tax management platform for The Betts Firm in Sierra Leone.

### Technology Stack

**Backend:**
- ASP.NET Core 9.0 Web API (.NET 9)
- Entity Framework Core 9.0 with PostgreSQL
- JWT Authentication with ASP.NET Core Identity
- AutoMapper for DTOs, FluentValidation for input validation
- Serilog for structured logging
- xUnit, Moq, and FluentAssertions for testing

**Frontend:**
- Next.js 15.2.4 with React 19 and TypeScript
- shadcn/ui component library (Radix UI primitives)
- TailwindCSS for styling
- React Hook Form with Zod validation
- Lucide React icons

## Architecture

### Clean Architecture Pattern

The backend follows clean architecture with clear separation of concerns:

```
BettsTax/
├── BettsTax.Web/          # API Controllers, Middleware, Configuration
├── BettsTax.Core/         # Business Logic, Services, DTOs, Validation
├── BettsTax.Data/         # Data Access, EF Core Models, Repositories
├── BettsTax.Shared/       # Common Utilities, Extensions
├── BettsTax.Core.Tests/   # Unit Tests for Business Logic
├── BettsTax.Web.Tests/    # Integration Tests for API Endpoints
└── BettsTax.Data.Tests/   # Data Layer Tests
```

### Key Principles

1. **Dependency Injection**: All services are registered and injected via DI container
2. **Repository Pattern**: Data access abstracted through repositories
3. **Service Layer**: Business logic encapsulated in service classes
4. **DTO Pattern**: Data transfer objects for API communication
5. **Validation**: FluentValidation for input validation, Zod for frontend validation

## Development Commands

### Backend (.NET)

```bash
# Navigate to backend
cd BettsTax

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run Web API (development)
cd BettsTax.Web && dotnet run

# Run all tests
dotnet test

# Run specific test project
dotnet test BettsTax.Core.Tests

# Entity Framework migrations
dotnet ef migrations add MigrationName --project BettsTax.Data --startup-project BettsTax.Web
dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web

# Create production build
dotnet publish -c Release
```

### Frontend (Next.js)

```bash
# Navigate to frontend
cd sierra-leone-ctis

# Install dependencies (use pnpm)
pnpm install

# Run development server
pnpm dev

# Build for production
pnpm build

# Run production build
pnpm start

# Run linter
pnpm lint
```

## Coding Standards

### Backend (.NET)

1. **Naming Conventions:**
   - PascalCase for classes, methods, properties, public fields
   - camelCase for private fields, parameters, local variables
   - Prefix interfaces with 'I' (e.g., `IClientService`)
   - Suffix DTOs with 'Dto' (e.g., `ClientDto`)

2. **Code Organization:**
   - One class per file
   - Group related functionality in folders
   - Keep controllers thin - delegate to services
   - Use async/await for I/O operations

3. **Error Handling:**
   - Use custom exceptions for business logic errors
   - Log errors with Serilog structured logging
   - Return appropriate HTTP status codes
   - Include correlation IDs for request tracking

4. **Testing:**
   - Write unit tests for all business logic
   - Use Moq for mocking dependencies
   - Use FluentAssertions for readable assertions
   - Maintain >80% code coverage for Core layer

### Frontend (TypeScript/React)

1. **Component Structure:**
   - Use functional components with hooks
   - Prefer named exports for components
   - Keep components focused and single-purpose
   - Use TypeScript interfaces for props

2. **File Organization:**
   - Place related components in feature folders
   - Reusable UI components in `components/ui/`
   - Page components in `app/` directory
   - Services in `lib/services/`

3. **Styling:**
   - Use TailwindCSS utility classes
   - Follow shadcn/ui component patterns
   - Use Sierra Leone theme colors: sierra-blue, sierra-gold
   - Maintain mobile-first responsive design

4. **State Management:**
   - Use React Context for global state
   - React Hook Form for form state
   - Server state via API calls (no global cache yet)

## Security Guidelines

### Critical Security Rules

1. **Never commit secrets:**
   - Use environment variables for sensitive data
   - Keep `.env` files in `.gitignore`
   - Use `.env.example` templates without actual secrets

2. **Authentication & Authorization:**
   - All API endpoints require JWT authentication except login/register
   - Use role-based authorization (Client, Associate, Admin, SystemAdmin)
   - Validate user permissions in service layer

3. **Input Validation:**
   - Validate all user input on both frontend (Zod) and backend (FluentValidation)
   - Sanitize file uploads
   - Use parameterized queries (EF Core handles this)

4. **Data Protection:**
   - Encrypt sensitive fields at rest
   - Use HTTPS for all communications
   - Implement audit logging for sensitive operations

### Environment Variables

Required environment variables (never use actual values):

```bash
DB_HOST=localhost
DB_PORT=5432
DB_NAME=BettsTaxDb
DB_USER=secure_user
DB_PASSWORD=secure_password_minimum_12_chars
JWT_SECRET_KEY=minimum_32_character_cryptographic_secret
```

## Sierra Leone Specific Requirements

### Tax Compliance

1. **Tax Categories:**
   - Large Taxpayers
   - Medium Taxpayers
   - Small Taxpayers
   - Micro Taxpayers

2. **Tax Types:**
   - Income Tax
   - GST (15%)
   - Payroll Tax (PAYE)
   - Excise Duty

3. **Finance Act 2025:**
   - Follow current Sierra Leone tax legislation
   - Implement penalty calculations per NRA guidelines
   - Support compliance scoring and deadline monitoring

### Localization

- Currency: Sierra Leone Leone (SLE)
- Date format: DD/MM/YYYY
- Primary language: English
- Future: Krio localization support

## Database Guidelines

### Entity Framework Core

1. **Migrations:**
   - Use descriptive migration names
   - Review generated migration scripts
   - Test migrations on development database first
   - Always backup production before applying migrations

2. **Model Design:**
   - Use data annotations for simple constraints
   - Use Fluent API for complex configurations
   - Include audit fields: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
   - Implement soft deletes where appropriate

3. **Queries:**
   - Use async methods for all database operations
   - Include only necessary related data with `.Include()`
   - Use `.AsNoTracking()` for read-only queries
   - Implement pagination for large result sets

## Testing Strategy

### Backend Tests

1. **Unit Tests (BettsTax.Core.Tests):**
   - Test business logic in isolation
   - Mock all dependencies
   - Follow AAA pattern (Arrange, Act, Assert)
   - Test edge cases and error conditions

2. **Integration Tests (BettsTax.Web.Tests):**
   - Test API endpoints end-to-end
   - Use in-memory database or test database
   - Verify HTTP responses and status codes
   - Test authentication and authorization

3. **Data Tests (BettsTax.Data.Tests):**
   - Test repository operations
   - Verify EF Core configurations
   - Test complex queries

### Frontend Tests

Currently no test infrastructure configured. Consider adding:
- Jest or Vitest for unit testing
- React Testing Library for component tests
- Playwright for E2E tests

## Common Patterns

### Adding a New Feature

1. **Backend:**
   - Create entity in `BettsTax.Data/Models/`
   - Add DbSet to `ApplicationDbContext`
   - Create migration
   - Create DTO in `BettsTax.Core/DTOs/`
   - Create service interface and implementation in `BettsTax.Core/Services/`
   - Add AutoMapper profile
   - Create controller in `BettsTax.Web/Controllers/`
   - Write unit and integration tests

2. **Frontend:**
   - Create API service in `lib/services/`
   - Create component in appropriate directory
   - Add routing if needed
   - Implement form with validation
   - Style with TailwindCSS

### API Endpoint Pattern

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _service;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(IExampleService service, ILogger<ExampleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExampleDto>>> GetAll()
    {
        try
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items");
            return StatusCode(500, "An error occurred");
        }
    }
}
```

### React Component Pattern

```typescript
import { useState } from 'react';
import { Button } from '@/components/ui/button';

interface ExampleProps {
  title: string;
  onSubmit: (data: string) => void;
}

export function Example({ title, onSubmit }: ExampleProps) {
  const [value, setValue] = useState('');

  const handleSubmit = () => {
    onSubmit(value);
  };

  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-bold">{title}</h2>
      <Button onClick={handleSubmit}>Submit</Button>
    </div>
  );
}
```

## Performance Considerations

1. **Backend:**
   - Use async/await for I/O operations
   - Implement caching for frequently accessed data
   - Use pagination for large datasets
   - Optimize database queries with proper indexing

2. **Frontend:**
   - Lazy load components when appropriate
   - Optimize images (use Next.js Image component)
   - Minimize bundle size
   - Use React.memo for expensive components

## Deployment Notes

- **Production**: Docker containerization with docker-compose
- **Database**: PostgreSQL 16 with regular backups
- **Reverse Proxy**: Nginx with SSL termination
- **Monitoring**: Prometheus + Grafana stack
- **Logging**: Centralized logging with Loki

## Additional Resources

- See `CLAUDE.md` for comprehensive project documentation
- See `BettsTax/README.md` for detailed backend documentation
- See `.env.example` templates for environment configuration
- See workflow files in `.github/workflows/` for CI/CD setup

## Support

For questions or issues:
- Check existing documentation first
- Review similar implementations in the codebase
- Follow established patterns and conventions
- Maintain consistency with existing code style
