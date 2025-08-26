# Production Environment Setup (CTIS)

Purpose: Provide a concrete, reproducible guide to provision and harden the production environment for CTIS.

## 1. Infrastructure Overview
- Backend: ASP.NET Core 9 Web API
- Frontend: Next.js 15
- Database: PostgreSQL
- Cache: Redis (optional, recommended)
- Storage: Azure Blob or S3-compatible (per org choice)
- Monitoring: App Insights (or equivalent)
- Network: HTTPS with TLS 1.3, WAF/CDN optional

## 2. Prerequisites
- Provisioned cloud subscription(s)
- DNS ownership for production domain (e.g., ctis.betts.example)
- SSL/TLS certificates (managed via cloud cert manager)
- Secrets manager (Azure Key Vault or equivalent)

## 3. Network and Security
- Enforce HTTPS-only traffic
- Security headers (CSP, HSTS, X-Content-Type-Options, X-Frame-Options)
- Restrict inbound traffic via firewall/WAF
- Private networking for DB/Redis; no public exposure
- IP allow-lists for admin endpoints if feasible

## 4. Compute
- Backend: Containerized (preferred) or VM
  - Health endpoints enabled
  - Rolling/blue-green deployment strategy
- Frontend: Static build hosted behind CDN or Node server

## 5. Data Stores
- PostgreSQL
  - Provision managed instance
  - Automated backups (point-in-time restore)
  - Encrypted at rest; SSL in transit
  - Create app user with least-privilege
- Redis (optional)
  - Managed service; TLS enabled

## 6. Storage
- Blob/S3 bucket with private access by default
- Signed URL mechanism for downloads
- Virus scanning on upload (server-side)

## 7. Secrets and Config
- Store all secrets in Key Vault
- App reads via environment variables or managed identity
- See config templates in ./config/

## 8. Observability
- App Insights (or similar): traces, metrics, logs, dashboards, alerts
- Log aggregation (structured JSON logs)

## 9. Backups and DR
- Nightly DB backups, PITR
- Storage lifecycle policies
- DR plan with RTO/RPO targets

## 10. Validation Checklist
- [ ] HTTPS enforced
- [ ] DB TLS and backups verified
- [ ] Secrets not in repo; only in vault
- [ ] Health checks green
- [ ] Monitoring dashboards and alerts configured

## 11. Runbooks
- See RUNBOOK_DEPLOYMENT.md and CHECKLIST_PRELAUNCH.md

