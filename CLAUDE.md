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

## Production-Ready Implementation Plan

This section outlines the comprehensive plan to complete the CTIS system and make it production-ready. The implementation follows the design specifications and requirements for a full-featured tax management platform.

### Implementation Status Overview

The system is currently in development with basic client management, document handling, and payment processing capabilities. The following major features need to be implemented to achieve production readiness:

#### 🔴 Critical Priority Features (Production Blockers)
1. **Enhanced KPI Dashboard System** - Real-time metrics and compliance monitoring
2. **Comprehensive Reporting System** - PDF/Excel reports with Sierra Leone formatting
3. **Advanced Compliance Monitoring** - Finance Act 2025 penalty calculations
4. **Tax Calculation Engine** - Complete Sierra Leone tax rules implementation
5. **Production Security & Compliance** - MFA, encryption, audit trails

#### 🟡 High Priority Features (Core Functionality)
6. **Integrated Communication System** - Real-time chat and support
7. **Multi-Gateway Payment Integration** - Orange Money, Africell Money
8. **Production Deployment & Launch** - Environment setup and monitoring

#### 🟢 Medium Priority Features (Enhancement)
9. **Associate Permission Management** - Enhanced templates and bulk operations
10. **Document Management Upgrade** - Version control and secure sharing
11. **Real-time Notification System** - Multi-channel delivery and scheduling
12. **Integration Testing & QA** - Comprehensive test coverage

### Detailed Implementation Tasks

#### 1. Enhanced KPI Dashboard System
**Goal**: Provide comprehensive performance metrics for firm administrators and personal compliance dashboards for clients.

**Backend Tasks**:
- [ ] Create `IKPIService` and `KPIService` in `BettsTax.Core/Services/`
- [ ] Implement KPI data models (`InternalKPIDto`, `ClientKPIDto`) in `BettsTax.Core/DTOs/`
- [ ] Add `KPIMetric` and `ComplianceScore` entities in `BettsTax.Data/`
- [ ] Create `KPIController` with role-based access in `BettsTax.Web/Controllers/`
- [ ] Implement Redis caching for KPI data with 15-minute expiration
- [ ] Add SignalR hub for real-time KPI updates

**Frontend Tasks**:
- [ ] Build `InternalKPIDashboard` component in `sierra-leone-ctis/components/kpi/`
- [ ] Create `ClientKPIDashboard` with personalized metrics
- [ ] Implement `KPICard` with Recharts trend visualization
- [ ] Add `ComplianceScoreCard` with Sierra Leone color scheme
- [ ] Create custom hooks `useKPIs()` and `useClientKPIs()`

**Testing Requirements**:
- [ ] Unit tests for KPI calculations with mock scenarios
- [ ] Integration tests for KPI data persistence
- [ ] API tests for KPI endpoints with different user roles
- [ ] End-to-end tests for KPI alerting workflow

#### 2. Comprehensive Reporting System
**Goal**: Generate detailed PDF and Excel reports for tax activities, compliance, and payment history.

**Backend Tasks**:
- [ ] Create `IReportService` and `ReportService` with template support
- [ ] Implement PDF generation using iTextSharp with Sierra Leone branding
- [ ] Add Excel generation using EPPlus with charts and formatting
- [ ] Create `ReportRequest` entity and database migration
- [ ] Implement Quartz.NET background jobs for async report generation
- [ ] Add `ReportsController` with secure file download endpoints

**Frontend Tasks**:
- [ ] Build `ReportGenerator` component with type selection and date picker
- [ ] Create `ReportHistory` component with download links
- [ ] Implement report generation progress tracking
- [ ] Add report preview functionality for PDF reports

**Testing Requirements**:
- [ ] Unit tests for report generation with sample data
- [ ] Tests for report formatting and Sierra Leone currency
- [ ] API tests for report generation workflow
- [ ] Performance tests for large report generation

#### 3. Advanced Compliance Monitoring
**Goal**: Provide comprehensive compliance dashboards with penalty calculations based on Sierra Leone Finance Act 2025.

**Backend Tasks**:
- [ ] Create `IComplianceEngine` and `ComplianceEngine` implementation
- [ ] Implement compliance scoring with weighted factors (30% filing, 30% payment, 20% documents, 20% timeliness)
- [ ] Add penalty calculation service using Finance Act 2025 rules
- [ ] Create deadline monitoring with configurable alert thresholds
- [ ] Add `ComplianceController` with status and penalty endpoints

**Frontend Tasks**:
- [ ] Build `ComplianceDashboard` with score visualization
- [ ] Create `ComplianceScoreCard` with green/yellow/red levels
- [ ] Implement `FilingStatusGrid` for each tax type
- [ ] Add `UpcomingDeadlines` with countdown timers
- [ ] Create `PenaltyWarnings` component with penalty amounts

**Testing Requirements**:
- [ ] Comprehensive unit tests for compliance calculations
- [ ] Tests for penalty calculation scenarios
- [ ] API tests for compliance endpoints
- [ ] End-to-end tests for compliance workflow

#### 4. Integrated Communication System
**Goal**: Enable real-time communication between clients and firm staff with conversation management.

**Backend Tasks**:
- [ ] Create `IChatService` and `ChatService` implementation
- [ ] Implement SignalR `ChatHub` with user groups and broadcasting
- [ ] Add `Conversation` and `InternalNote` entities
- [ ] Create conversation assignment and status tracking
- [ ] Add `ChatController` with message and assignment endpoints

**Frontend Tasks**:
- [ ] Build `ChatInterface` with real-time message display
- [ ] Create `ConversationList` for chat history navigation
- [ ] Implement `MessageInput` with typing indicators
- [ ] Add `ConversationAssignment` for staff management
- [ ] Create SignalR connection hooks

**Testing Requirements**:
- [ ] Unit tests for chat service functionality
- [ ] Tests for conversation management
- [ ] API tests for chat endpoints
- [ ] End-to-end tests for real-time messaging

#### 5. Multi-Gateway Payment Integration
**Goal**: Support multiple Sierra Leone payment methods including Orange Money and Africell Money.

**Backend Tasks**:
- [ ] Create `IPaymentGateway` interface with standardized methods
- [ ] Implement `OrangeMoneyProvider` and `AfricellMoneyProvider`
- [ ] Add `PaymentGatewayFactory` for gateway selection
- [ ] Enhance `PaymentService` with multi-gateway support
- [ ] Add payment webhook handling for status updates

**Frontend Tasks**:
- [ ] Create `PaymentForm` with payment method selection
- [ ] Build provider-specific forms (`OrangeMoneyForm`, `AfricellMoneyForm`)
- [ ] Implement payment status tracking with real-time updates
- [ ] Add payment history with receipt downloads

**Testing Requirements**:
- [ ] Unit tests for payment gateway abstraction
- [ ] Integration tests with payment provider sandboxes
- [ ] API tests for payment processing scenarios
- [ ] Security tests for payment data handling

#### 6. Tax Calculation Engine for Sierra Leone
**Goal**: Implement comprehensive tax calculations based on Finance Act 2025 with penalty matrix.

**Backend Tasks**:
- [ ] Enhance `SierraLeoneTaxCalculationService` with Finance Act 2025 rules
- [ ] Implement GST calculation (15%) with exemption handling
- [ ] Add Income Tax with progressive rates and allowances
- [ ] Create PAYE calculation with current Sierra Leone rates
- [ ] Implement penalty calculation with taxpayer category consideration
- [ ] Add `TaxCalculationController` with calculation endpoints

**Frontend Tasks**:
- [ ] Create tax calculator components for each tax type
- [ ] Build penalty calculator with scenario inputs
- [ ] Implement tax rate display with effective dates
- [ ] Add tax calculation history and comparison features

**Testing Requirements**:
- [ ] Comprehensive unit tests for all tax calculations
- [ ] Tests for penalty calculation scenarios
- [ ] API tests for tax calculation endpoints
- [ ] Validation tests for Sierra Leone tax rules

#### 7. Production Security and Compliance
**Goal**: Implement comprehensive security measures for production deployment.

**Backend Tasks**:
- [ ] Implement multi-factor authentication for admin users
- [ ] Add data encryption at rest for sensitive fields
- [ ] Enhance audit system with detailed action logging
- [ ] Implement security monitoring and threat detection
- [ ] Add OAuth2/OpenID Connect for enterprise SSO

**Frontend Tasks**:
- [ ] Add MFA setup and verification components
- [ ] Implement security settings dashboard
- [ ] Create audit log viewer for administrators
- [ ] Add security alert notifications

**Testing Requirements**:
- [ ] Security tests for authentication and authorization
- [ ] Tests for data encryption and protection
- [ ] Penetration testing with automated tools
- [ ] Audit logging validation tests

### Implementation Timeline

**Phase 1 (Weeks 1-4): Core Business Logic**
- KPI Dashboard System backend implementation
- Compliance Monitoring engine
- Tax Calculation Engine with Sierra Leone rules
- Reporting System backend

**Phase 2 (Weeks 5-8): User Interface**
- Frontend dashboard components
- Compliance monitoring interface
- Reporting interface
- Tax calculator components

**Phase 3 (Weeks 9-12): Communication & Payments**
- Real-time chat system
- Multi-gateway payment integration
- Enhanced notification system
- Document management upgrades

**Phase 4 (Weeks 13-16): Security & Production**
- Production security implementation
- Comprehensive testing and QA
- Production environment setup
- Deployment automation and monitoring

### Success Criteria

**Functional Requirements**:
- All 10 major requirement categories fully implemented
- Comprehensive test coverage (>80% backend, >70% frontend)
- Performance benchmarks met (API response <500ms, dashboard load <2s)
- Sierra Leone tax compliance validation

**Non-Functional Requirements**:
- 99.5% uptime in production
- Support for 1000+ concurrent users
- Data encryption at rest and in transit
- GDPR/data protection compliance
- Mobile responsiveness across devices

**Business Requirements**:
- Complete client onboarding workflow
- Automated tax filing reminders
- Payment processing with multiple gateways
- Comprehensive audit trails
- Real-time compliance monitoring

### Risk Mitigation

**Technical Risks**:
- Payment gateway integration complexity → Use sandbox testing and staged rollout
- Performance with large datasets → Implement caching and pagination
- Security vulnerabilities → Regular security audits and penetration testing

**Business Risks**:
- Sierra Leone tax law changes → Configurable tax rules and regular updates
- User adoption → Comprehensive training and gradual feature rollout
- Data migration → Thorough testing and backup procedures

This implementation plan provides a clear roadmap to transform the CTIS system into a production-ready, comprehensive tax management platform for The Betts Firm and their clients in Sierra Leone.