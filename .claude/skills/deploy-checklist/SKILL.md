---
name: deploy-checklist
description: Pre-deployment verification checklist -- environment parity, container hardening (multi-stage, distroless, non-root, Trivy scan), health checks (liveness/readiness/startup), graceful shutdown (SIGTERM, connection draining), feature flags, rollback procedure tested. Use this whenever the user mentions deploying, shipping to production, going live, release prep, deploy gate, prod readiness, or before any prod deploy.
---

# /deploy-checklist - Pre-deployment verification

## When to use

Invoke this skill:
- **Before every deployment** (staging or production)
- **Before every release**
- **When setting up a new environment**

## Environment Parity Check

Before deploying, verify dev/staging/prod parity:

- [ ] Local development mirrors production (Docker Compose or equivalent)
- [ ] Staging uses same database engine and version as production
- [ ] Staging uses same configuration shape as production (same env var structure)
- [ ] Configuration via environment variables, with `.env.example` documented
- [ ] All environment variables validated at application startup (fail fast)
- [ ] Infrastructure as Code used for cloud resources (Terraform, Pulumi, etc.)

## Containerization Check

If using Docker (which most projects should):

- [ ] **Multi-stage builds** used -- no build tools shipped in runtime image
- [ ] **Minimal base images** (slim or distroless variants)
- [ ] **Non-root user** -- `USER` directive set, never runs as root in production
- [ ] **`.dockerignore`** excludes `.git`, `node_modules`, `.env`, test files, docs
- [ ] **Base image versions pinned** -- no `:latest` tag in production Dockerfiles
- [ ] **Dockerfile linting** runs in CI (e.g., Hadolint)
- [ ] **Container vulnerability scanning** in CI (Trivy, Snyk, Grype, or equivalent)

## Health Checks & Probes

For containerized or cloud-deployed apps:

- [ ] **Liveness probe** implemented -- lightweight check, "is the process alive?" (do NOT check external dependencies)
- [ ] **Readiness probe** implemented -- "can this instance handle traffic?" (checks DB, cache, critical dependencies)
- [ ] **Startup probe** implemented if app has slow startup (cache warming, migrations)
- [ ] `/health` endpoint returns structured status
- [ ] Load balancer configured to use health checks for routing decisions
- [ ] Kubernetes probes configured correctly (if using K8s)

## Graceful Shutdown

In-flight requests must complete cleanly during deploys:

- [ ] App listens for **SIGTERM** and stops accepting new requests
- [ ] In-flight work finishes before exit
- [ ] **Shutdown timeout** set (e.g., 30 seconds max)
- [ ] **Connection draining** implemented for WebSockets, long-polling, streaming responses
- [ ] Database transactions committed or rolled back cleanly
- [ ] **Kubernetes preStop hook** adds delay for load balancer deregistration (if using K8s)

Without graceful shutdown, zero-downtime deployments are impossible regardless of deployment strategy.

## Rollback Plan

Every deploy must be reversible:

- [ ] **Tested rollback procedure** -- can you roll back in under 5 minutes?
- [ ] **Database migrations reversible** -- every `up` has a `down` script
- [ ] **Previous version artifacts available** for quick revert
- [ ] **Rollback runbook** documented (step-by-step, not "rollback the deploy")
- [ ] Team knows who can trigger a rollback

## Feature Flags

Decouple deploy from release:

- [ ] New features deploy behind a feature flag, disabled by default
- [ ] Feature flag can be toggled without redeploy (kill switch)
- [ ] Flag rollout plan documented (% rollout, user targeting, etc.)
- [ ] Flag cleanup tracked -- stale flags become tech debt
- [ ] Implementation appropriate to complexity (env var, DB toggle, or service like LaunchDarkly)

## Progressive Delivery (when applicable)

For high-traffic production services:

- [ ] Canary deployment plan (route small % of traffic to new version)
- [ ] Blue-green deployment plan (instant switch, keep old version ready)
- [ ] Rollback trigger defined (error rate threshold, latency threshold)
- [ ] Monitoring in place to detect issues during rollout

Not needed for every project -- use judgment based on traffic and risk.

## CI/CD Pipeline Verification

- [ ] All tests pass (unit, integration, e2e, UI, performance, accessibility)
- [ ] Security scan passes (dependency audit, container scan)
- [ ] Build artifacts generated and stored
- [ ] Deployment runs in the correct order (DB migrations BEFORE app deployment)
- [ ] Smoke tests run after deployment

## Pre-Deploy Final Verification

Just before hitting deploy:

- [ ] CI pipeline is green
- [ ] All PR comments and review feedback addressed
- [ ] Release notes written (see `/release`)
- [ ] Team notified (if applicable)
- [ ] Monitoring dashboards open and ready
- [ ] Rollback plan fresh in memory
- [ ] Database backup taken (for any deploy touching the DB)

## Post-Deploy Verification

Immediately after the deploy:

- [ ] Health checks passing on all instances
- [ ] No error rate spike in monitoring
- [ ] Critical user flows smoke-tested
- [ ] Logs show the new version running
- [ ] Version endpoint returns the expected version
- [ ] Performance metrics stable
- [ ] Error tracking shows no new issues

## If Something Goes Wrong

Don't try to fix forward under pressure. Rollback first, investigate later:
1. Execute rollback procedure (`/rollback` -- revert-first; or `/feature-flag` kill-switch if the change is flagged)
2. Verify the rollback succeeded -- metrics stable, errors gone (`/verify-work`)
3. Communicate to users if there was impact (status page)
4. Run **`/incident-response`** -- it owns the full loop (triage -> mitigate -> communicate -> resolve/verify -> schedule the postmortem), wired to [[../Agent Workflow]] §19; the blameless postmortem (§19.2) is its last step

## Production-Specific Checks

Before deploying to production (not needed for staging):

- [ ] Deploying during a low-traffic window (if applicable)
- [ ] On-call person is aware and available
- [ ] Status page ready to update if needed
- [ ] Rollback procedure verified in staging first
- [ ] All secrets configured in production secrets manager
- [ ] Monitoring alerts configured for new endpoints/features
