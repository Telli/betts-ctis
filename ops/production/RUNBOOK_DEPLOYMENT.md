# Deployment Runbook (Production)

This document describes a safe, repeatable production deployment for CTIS.

## Pre-Reqs
- All tests green on main
- Release notes and migration plan ready
- Incident channel on-call confirmed

## Steps (Blue-Green Recommended)
1) Prepare Release
   - Tag commit as `release-YYYYMMDD-N`
   - Build artifacts (backend container, frontend build)
2) Provision Green
   - Deploy to green environment slots
   - Apply database migrations to staging DB instance; validate
3) Smoke Test Green
   - Health checks
   - Basic user flows (login, dashboard load, report request)
4) Data Migration (if required)
   - Apply migrations to production with maintenance window
   - Validate schema and seed data scripts
5) Cutover
   - Switch traffic from blue to green
   - Monitor error rate, latency, resource utilization
6) Post-Deploy
   - Run extended smoke tests
   - Announce completion

## Rollback
- Revert traffic to blue
- Roll forward hotfix or revert deployment artifacts
- Document incident and corrective actions

## Artifacts
- Backend: Docker image, tag: `ctis-api:<version>`
- Frontend: Static build or Node image `ctis-web:<version>`

## Health Endpoints
- API: `/api/health`
- Web: static 200 check

