# BettsTax - Client Tax Information System for Sierra Leone

A comprehensive tax management system designed specifically for Sierra Leone's tax compliance requirements under the Finance Act 2025.

## 🏛️ Overview

BettsTax is a production-ready Client Tax Information System (CTIS) built for The Betts Firm in Sierra Leone. It provides complete tax management capabilities including client management, tax calculations, compliance monitoring, payment processing, and reporting - all designed to comply with Sierra Leone's Finance Act 2025.

## 🌍 Sierra Leone Specific Features

- **Finance Act 2025 Compliance**: Full implementation of current Sierra Leone tax laws
- **Mobile Money Integration**: Orange Money and Africell Money payment processing
- **Multi-language Support**: English with Sierra Leone Krio localization ready
- **NRA Integration**: Direct integration with National Revenue Authority systems
- **Local SMS Gateway**: Integration with Orange and Africell SMS services
- **Currency Support**: Sierra Leone Leone (SLE) with Bank of Sierra Leone exchange rates

## 🏗️ Architecture

### Backend (.NET)
- **ASP.NET Core 9.0** Web API with clean architecture
- **Entity Framework Core 9.0** with PostgreSQL
- **JWT Authentication** with ASP.NET Core Identity
- **AutoMapper** for DTOs and **FluentValidation** for input validation
- **Serilog** structured logging with Sierra Leone compliance tagging

### Frontend (Next.js)
- **Next.js 15.2.4** with App Router and React 19
- **TypeScript** for type safety
- **shadcn/ui** component library with Tailwind CSS
- **React Hook Form** with Zod validation
- **Sierra Leone themed UI** with local colors and branding

### Infrastructure
- **Docker containerization** with production-ready configurations
- **Nginx reverse proxy** with SSL termination and rate limiting
- **PostgreSQL 16** with Redis caching
- **Prometheus + Grafana** monitoring stack
- **Loki + Promtail** log aggregation

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 9 SDK (for local development)
- Node.js 18+ and pnpm (for frontend development)
- PostgreSQL 16+ (for local development)

### Production Deployment

1. **Clone the repository**
   ```bash
   git clone https://github.com/bettsfirm/betts-tax.git
   cd betts-tax
   ```

2. **Configure environment**
   ```bash
   cp .env.production.template .env.production
   # Edit .env.production with your secure values
   ```

3. **Deploy with Docker**
   ```bash
   ./scripts/deploy.sh production
   ```

4. **Access the application**
   - Application: https://betts.sl
   - API Documentation: https://betts.sl/api/swagger (if enabled)
   - Monitoring: http://localhost:3001 (Grafana)

### Development Setup

1. **Environment setup**
   ```bash
   cp .env.development.template .env.development
   # Edit with your local development values
   ```

2. **Start development services**
   ```bash
   docker-compose up -d postgres redis mailhog
   ```

3. **Run backend**
   ```bash
   cd BettsTax
   dotnet restore
   dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web
   cd BettsTax.Web && dotnet run
   ```

4. **Run frontend**
   ```bash
   cd sierra-leone-ctis
   pnpm install
   pnpm dev
   ```

## 📊 Key Features

### 🏢 Client Management
- **Multi-tier Taxpayers**: Large, Medium, Small, and Micro taxpayer categories
- **Comprehensive Profiles**: Complete client information with Sierra Leone compliance data
- **Document Management**: Secure file uploads with version control
- **Communication Hub**: Integrated messaging and notification system

### 💰 Tax Calculations
- **Finance Act 2025 Engine**: Complete implementation of current Sierra Leone tax laws
- **Multiple Tax Types**: Income Tax, GST, Payroll Tax, Excise Duty calculations
- **Penalty Matrix**: Automated penalty calculations based on NRA guidelines
- **Rate Management**: Dynamic tax rate updates and historical tracking

### 📋 Compliance Monitoring
- **Deadline Tracking**: Automated monitoring of tax filing deadlines
- **Alert System**: Proactive notifications for compliance requirements
- **Penalty Calculations**: Automatic penalty assessment with dispute management
- **Audit Trails**: Comprehensive logging for regulatory compliance

### 💳 Payment Processing
- **Mobile Money Integration**: Orange Money and Africell Money support
- **Multi-gateway Processing**: Support for multiple payment providers
- **Fraud Detection**: Advanced risk assessment and monitoring
- **Reconciliation**: Automated payment matching and reporting

### 📈 Reporting & Analytics
- **KPI Dashboards**: Real-time metrics for clients and administrators
- **Custom Reports**: PDF and Excel generation with Sierra Leone branding
- **Compliance Reports**: NRA-ready tax reports and submissions
- **Business Intelligence**: Advanced analytics for tax practice management

### 🔐 Security & Compliance
- **Multi-Factor Authentication**: TOTP, SMS, Email, and backup codes
- **Field-level Encryption**: AES-256 encryption for sensitive data
- **Audit Logging**: Comprehensive audit trails for all system activities
- **Role-based Access**: Granular permissions for different user types

## 🛠️ Development

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project BettsTax.Data --startup-project BettsTax.Web

# Update database
dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test BettsTax.Core.Tests
```

### Frontend Development
```bash
# Install dependencies
pnpm install

# Run development server
pnpm dev

# Build for production
pnpm build

# Run linter
pnpm lint
```

## 📦 Deployment

### Environment Configuration
The system uses environment-specific configuration files:
- `.env.development` - Local development settings
- `.env.production` - Production deployment settings

Never commit actual environment files to source control.

### Production Deployment Script
The `scripts/deploy.sh` script handles complete production deployment:
- Dependency checks and environment validation
- Database backups and migrations
- Docker image building and service deployment
- Health checks and monitoring setup
- SSL certificate management
- Cleanup and notification

### Monitoring and Observability
- **Prometheus**: Metrics collection from all services
- **Grafana**: Real-time dashboards and alerting
- **Loki**: Centralized log aggregation
- **Alert Manager**: Proactive issue notifications

## 🔧 Configuration

### Key Configuration Areas
- **Database**: PostgreSQL connection strings and pooling
- **Redis**: Caching and session storage configuration
- **JWT**: Token signing and validation settings
- **Email/SMS**: Integration with Sierra Leone service providers
- **Mobile Money**: Orange and Africell API configurations
- **File Storage**: Document upload and management settings

### Security Configuration
- **Encryption**: Master keys for field-level encryption
- **MFA**: Multi-factor authentication settings
- **Rate Limiting**: API throttling and abuse prevention
- **CORS**: Cross-origin resource sharing policies

## 📋 Sierra Leone Compliance

### Finance Act 2025
- Complete implementation of current tax legislation
- Automated updates for rate changes and rule modifications
- Penalty calculations based on NRA guidelines
- Integration with National Revenue Authority systems

### Data Protection
- Encryption of all sensitive taxpayer information
- Audit logging for all data access and modifications
- Secure backup and recovery procedures
- Compliance with Sierra Leone data protection requirements

### Business Continuity
- Automated database backups with encryption
- Health monitoring and alerting
- Disaster recovery procedures
- Uptime monitoring and reporting

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/new-feature`
3. Commit changes: `git commit -am 'Add new feature'`
4. Push to branch: `git push origin feature/new-feature`
5. Submit a pull request

## 📄 License

This project is proprietary software owned by The Betts Firm, Sierra Leone.

## 📞 Support

For technical support or questions:
- Email: support@betts.sl
- Phone: +232 XX XXX XXXX
- Address: [Betts Firm Address], Freetown, Sierra Leone

## 🔍 Monitoring URLs

- **Application**: https://betts.sl
- **API Health**: https://betts.sl/health
- **Grafana Dashboard**: http://localhost:3001
- **Prometheus Metrics**: http://localhost:9090
- **API Documentation**: https://betts.sl/api/swagger (production disabled)

---

**BettsTax** - Empowering Sierra Leone's Tax Compliance 🇸🇱