# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Betts CTIS** - a Client Tax Information System for The Betts Firm in Sierra Leone, comprising:
- **Backend**: ASP.NET Core 9.0 Web API (.NET 9)
- **Frontend**: Next.js 15.2.4 with React 19 and TypeScript

## Common Development Commands

### Backend (.NET)
```bash
# Navigate to backend directory
cd BettsTax

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the Web API (development)
cd BettsTax.Web && dotnet run

# Run tests
dotnet test

# Entity Framework migrations
dotnet ef migrations add MigrationName --project BettsTax.Data --startup-project BettsTax.Web
dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web

# Create production build
dotnet publish -c Release
```

### Frontend (Next.js)
```bash
# Navigate to frontend directory
cd sierra-leone-ctis

# Install dependencies
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

## Architecture Overview

### Backend Structure
```
BettsTax/
├── BettsTax.Web/          # Web API layer (Controllers, Middleware)
├── BettsTax.Core/         # Business logic (Services, DTOs, Validation)
├── BettsTax.Data/         # Data access (EF Core, Models, Migrations)
├── BettsTax.Shared/       # Common utilities
├── BettsTax.Core.Tests/   # Unit tests for business logic
├── BettsTax.Web.Tests/    # Integration tests for API
└── BettsTax.Data.Tests/   # Data layer tests
```

- **Clean Architecture**: Separation of concerns with proper dependency injection
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens with ASP.NET Core Identity
- **Logging**: Serilog with structured logging
- **Validation**: FluentValidation for input validation
- **Mapping**: AutoMapper for DTOs

### Frontend Structure
```
sierra-leone-ctis/
├── app/                   # Next.js App Router pages
│   ├── clients/          # Client management
│   ├── dashboard/        # Dashboard
│   ├── documents/        # Document management
│   └── login/            # Authentication
├── components/           # React components
│   ├── ui/              # Reusable UI components (shadcn/ui)
│   └── dashboard/       # Dashboard-specific components
├── lib/                 # Utilities and API clients
│   └── services/        # Frontend service layer
└── styles/              # Global CSS
```

- **UI Framework**: shadcn/ui built on Radix UI primitives
- **Styling**: TailwindCSS with Sierra Leone custom theme
- **Forms**: React Hook Form with Zod validation
- **Authentication**: JWT tokens with Context API
- **Icons**: Lucide React

## Key Technologies

### Backend Stack
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0 with PostgreSQL
- ASP.NET Core Identity
- JWT Authentication
- AutoMapper
- FluentValidation
- Serilog
- xUnit testing with Moq and FluentAssertions

### Frontend Stack
- Next.js 15.2.4 (App Router)
- React 19 with TypeScript
- shadcn/ui component library
- TailwindCSS for styling
- React Hook Form with Zod validation
- Lucide React icons

## Configuration

### Backend Configuration
- **Database**: PostgreSQL connection in `appsettings.json`
- **JWT**: Token configuration for authentication
- **Logging**: Serilog console output
- **API Docs**: Swagger/OpenAPI enabled

### Frontend Configuration
- **API Base URL**: `http://localhost:5000` (development)
- **Authentication**: JWT tokens in localStorage
- **Theme**: Sierra Leone colors (sierra-blue, sierra-gold)

## Domain Models

Key entities include:
- **Client**: Business entities with tax information
- **TaxYear**: Tax periods associated with clients
- **Document**: File attachments for tax documentation
- **Payment**: Financial transactions and receipts
- **ApplicationUser**: Extended Identity user with roles (Client, Associate, Admin, SystemAdmin)

## Testing

### Backend Tests
- **Unit Tests**: xUnit with Moq for mocking
- **Integration Tests**: API endpoints with in-memory database
- **Coverage**: Comprehensive test coverage for business logic

### Frontend Tests
- **No test setup configured** - consider adding Jest/Vitest for component testing

## Development Notes

### Backend Development
- Use Repository pattern for data access
- Implement proper error handling with structured logging
- Follow clean architecture principles
- Maintain comprehensive audit logging

### Frontend Development
- Use TypeScript for type safety
- Follow shadcn/ui component patterns
- Implement proper form validation with Zod
- Maintain consistent styling with TailwindCSS utilities

## Sierra Leone Specific Features

This system is specifically designed for Sierra Leone tax compliance:
- Tax categories: Large, Medium, Small, Micro taxpayers
- Tax types: Income Tax, GST, Payroll Tax, Excise Duty
- Currency: Sierra Leone Leone (SLE)
- Compliance scoring and deadline monitoring
- Penalty calculations per Sierra Leone Finance Act

## Security Considerations

- JWT tokens for API authentication
- Role-based authorization (Client, Associate, Admin, SystemAdmin)
- Input validation on both frontend and backend
- Audit logging for all user actions
- Secure file upload handling

### Environment Variable Security

**CRITICAL**: Never commit sensitive data to source control. Use environment variables for:

```bash
# Required Environment Variables (.env file)
DB_HOST=localhost
DB_PORT=5432
DB_NAME=BettsTaxDb
DB_USER=your_db_user
DB_PASSWORD=your_secure_password
JWT_SECRET_KEY=your_very_long_jwt_secret_key_minimum_32_characters
```

**Setup Instructions:**
1. Copy `.env.example` to `.env` in the BettsTax.Web directory
2. Update `.env` with your actual secure values
3. Ensure `.env` is in `.gitignore` (already configured)
4. Use `appsettings.Development.local.json` for local development overrides

**Security Best Practices:**
- Use strong, unique passwords (minimum 12 characters)
- Generate cryptographically secure JWT secrets (minimum 32 characters)
- Use dedicated database users with limited permissions
- Rotate secrets regularly in production
- Never use default credentials like "root/password"

## Database Migrations

The project uses Entity Framework Core migrations. Always:
1. Add migrations with descriptive names
2. Review generated migration scripts
3. Test migrations on development database
4. Backup production database before applying migrations