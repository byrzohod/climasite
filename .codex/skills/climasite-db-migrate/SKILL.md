---
name: climasite-db-migrate
description: Use when adding, editing, removing, testing, or reviewing EF Core migrations or schema/data model changes in ClimaSite.
---

# ClimaSite DB Migration

Use expand-contract for backward-compatible deploys:
- Expand: add new nullable/table/column while old code still works.
- Migrate: deploy code and backfill data safely.
- Contract: remove old shape in a later deploy.

Rules:
- Every `Up` must have a real `Down`.
- Never drop or rename in the same deploy old code may still depend on.
- Review EF generated migrations for accidental drop/create.
- For non-trivial changes, add an ADR covering compatibility, rollback, and performance.
- Test fresh apply and rollback with EF Core.
- Integration tests use Testcontainers, not shared Postgres.

