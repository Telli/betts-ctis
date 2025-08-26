# Pre-Launch Checklist (Production)

## Security
- [ ] HTTPS/TLS enforced end-to-end
- [ ] JWT validation, refresh flow verified
- [ ] Rate limiting configured per role
- [ ] CSP/HSTS headers in place

## Data
- [ ] DB backups enabled (PITR)
- [ ] Migrations validated on staging
- [ ] Read-only service account tested

## Monitoring
- [ ] APM dashboards live
- [ ] Error alert routing verified
- [ ] Log retention policies set

## App Readiness
- [ ] Health checks passing
- [ ] Feature flags set as planned
- [ ] Report generation queues healthy

## Compliance
- [ ] Audit logging retention configured
- [ ] Access controls verified (admin/associate/client)
- [ ] Data encryption at rest/in transit validated

## Communication
- [ ] Go-live announcement prepared
- [ ] Support rota confirmed
- [ ] Rollback plan approved

