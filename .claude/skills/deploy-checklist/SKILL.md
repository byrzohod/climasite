---
name: deploy-checklist
description: Pre-deploy and post-deploy checklist covering rollback, migrations, feature flags, health checks, observability, and graceful shutdown. Use before any production deploy or when the user asks to deploy or ship to prod.
---

# /deploy-checklist

Implements sections 17, 28, 29 of the Agent Workflow.

## Pre-Deploy

### Reversibility
- [ ] **Every deploy must be reversible in under 5 minutes**
- [ ] Previous version's artifacts are still available (image tag, NuGet, dist bundle)
- [ ] Rollback procedure tested at least once on staging this quarter

### Database
- [ ] Migrations follow expand-contract pattern (no destructive changes to columns still in use by old code)
- [ ] Every `up` migration has a tested `down`
- [ ] Migration tested against production-like data volume
- [ ] No long-running locks on hot tables

### Feature Flags
- [ ] New features ship behind a flag, default off
- [ ] Kill-switch for any new behavior that can be toggled without redeploy
- [ ] Stale flags (>30 days post-rollout) cleaned up

### Health Checks
- [ ] **Liveness probe**: lightweight, no external deps
- [ ] **Readiness probe**: checks DB, Redis, critical deps; returns 503 when not ready
- [ ] **Startup probe**: covers slow init (cache warming, EF migrations)
- [ ] `/health` endpoint returns structured status

### Graceful Shutdown
- [ ] App handles SIGTERM: stop accepting new requests, drain in-flight, exit
- [ ] Shutdown timeout configured (~30s)
- [ ] Open DB transactions committed/rolled back, not abandoned
- [ ] (K8s) preStop hook delays shutdown long enough for LB deregistration

### Observability
- [ ] Logs are structured (JSON), include trace/correlation ID
- [ ] No sensitive data in logs (passwords, tokens, PII)
- [ ] RED metrics exposed (Rate, Errors, Duration) for new endpoints
- [ ] Distributed tracing instrumented for new code paths
- [ ] Error tracker (Sentry / equivalent) wired up
- [ ] Dashboard exists for the service: health, latency, error rate, throughput

### Alerts
- [ ] Alerts defined on symptoms (user-visible failures), not causes (CPU%)
- [ ] Every alert is actionable
- [ ] On-call runbook updated for any new failure modes (`docs/runbooks/`)

### Performance & Security
- [ ] Performance budget still met (LCP < 2.5s, FID < 100ms, CLS < 0.1, API p95 within target)
- [ ] `/security-review` clean
- [ ] Dependency audit clean (`dotnet list package --vulnerable`, `npm audit`)
- [ ] Container image vulnerability scan clean (Trivy / equivalent)

### Tests
- [ ] All unit, integration, E2E, and UI tests green
- [ ] Smoke test plan ready for post-deploy verification

### Configuration
- [ ] All required env vars set in target environment, validated at startup
- [ ] No new secrets in code; all in env / secret manager
- [ ] `.env.example` updated for any new vars

## Post-Deploy

- [ ] Run smoke tests against the deployed environment
- [ ] Watch error tracker for ≥ 30 min
- [ ] Watch dashboards: error rate, latency, throughput
- [ ] Verify health check returns 200
- [ ] Verify a real user flow end-to-end (login → core action)
- [ ] If any anomaly: roll back first, debug second
- [ ] Update auto-memory and project CLAUDE.md with anything learned

## climasite-specific notes

- Backend: ASP.NET Core (.NET 10) — listen for SIGTERM via `IHostApplicationLifetime.ApplicationStopping`
- Frontend: Angular 19 — verify production build serves correctly with brotli/gzip
- DB: PostgreSQL 16 with EF Core migrations — always test rollback on a copy
- Cache: Redis 7 — verify connection pool sizing and reconnection
- Stripe webhooks: verify signing secret matches the deployed environment
