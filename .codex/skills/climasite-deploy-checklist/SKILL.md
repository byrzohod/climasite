---
name: climasite-deploy-checklist
description: Use before ClimaSite deployment or when the user asks whether the branch is deployable, shippable, or ready for production.
---

# ClimaSite Deploy Checklist

Verify:
- Rollback path under 5 minutes and previous artifacts available.
- Migrations are backward-compatible and rollback-tested.
- Readiness checks DB/Redis; liveness is lightweight.
- Graceful shutdown and request draining are configured.
- Structured logs include correlation IDs and no secrets/PII.
- Alerts are symptom-based and runbooks updated for new failure modes.
- Dependency audits and security review are clean.
- Unit, integration, Angular, E2E, build, lint, and UI QA are green or exceptions are explicit.
- Env var docs/config are updated for any new config.

For this repo: PostgreSQL, Redis, Stripe, Angular production bundle, and health endpoints are core deploy surfaces.

