---
name: db-migrate
description: Safely create, test, and apply EF Core database migrations using the expand-contract pattern. Use any time the user asks to add, modify, remove, or roll back a database migration in the climasite project.
---

# /db-migrate

Implements section 13 (Database Management) of the Agent Workflow.

## Golden Rules

1. **Backward-compatible deploys** — never drop a column/table in the same deploy that removes the code using it. Use **expand → migrate → contract**:
   - **Expand**: add new column/table; old code still works
   - **Migrate**: deploy new code that uses the new shape; backfill data
   - **Contract**: in a *later* deploy, drop the old column/table once nothing reads it
2. **Every `up` has a `down`** — generated automatically by EF Core, but verify it actually reverses the change for non-trivial migrations.
3. **Test against production-like volume** — a migration that takes 50ms on 100 rows can lock a table for minutes on 10M rows. Run `EXPLAIN ANALYZE` for any data backfill.
4. **Never hand-edit production** — every change goes through a migration committed to git.
5. **PostgreSQL caveats**: `ALTER TABLE ... ADD COLUMN` with a default rewrites the table on PG ≤ 10; OK on PG 11+. Adding NOT NULL on a non-empty table is a multi-step migration (add nullable → backfill → add NOT NULL constraint).

## Procedure

1. **Plan the change**. For non-trivial schema changes, write a short ADR in `docs/adr/` covering:
   - What is changing and why
   - Backward compatibility plan (expand-contract steps)
   - Rollback plan
   - Performance impact estimate

2. **Create the migration**:
   ```bash
   cd src/ClimaSite.Infrastructure
   dotnet ef migrations add <PascalCaseName> --startup-project ../ClimaSite.Api
   ```

3. **Review the generated `Up` and `Down`** — EF Core sometimes does the wrong thing for renames (drops + creates instead of renaming). Edit the migration if needed.

4. **Add data backfill** if required, in a separate migration or as raw SQL inside the migration. Use `migrationBuilder.Sql(...)` and keep it idempotent.

5. **Test on a fresh database**:
   ```bash
   dotnet ef database drop --force --startup-project ../ClimaSite.Api
   dotnet ef database update --startup-project ../ClimaSite.Api
   ```

6. **Test the rollback**:
   ```bash
   dotnet ef database update <PreviousMigrationName> --startup-project ../ClimaSite.Api
   ```

7. **Test on a production-sized copy** if available. Use `EXPLAIN ANALYZE` for any backfill SQL.

8. **Run all tests** (from the repo root — per-project, never a bare root `dotnet test`, which pulls in the server-dependent E2E project and produces ~200 false failures):
   ```bash
   dotnet test ClimaSite.NoE2E.slnf                       # all non-E2E tests via the solution filter
   cd src/ClimaSite.Web && ng test --watch=false --browsers=ChromeHeadless
   # E2E (needs API on :5029 + ng serve on :4200; CI runs it for you):
   #   dotnet test tests/ClimaSite.E2E
   ```

9. **Commit** with `feat(db): <description>` or `chore(db): <description>` — atomic, just the migration files and any code that depends on them.

## Removing a migration (development only — never if pushed)

```bash
dotnet ef migrations remove --startup-project ../ClimaSite.Api
```

## Expand-Contract Example: rename a column

**Don't**: a single migration that does `RenameColumn`. If old pods are still serving, they will fail.

**Do**:
1. Migration A: add the new column, copy data via SQL, deploy. Old code keeps using the old column; new code reads/writes both.
2. Migration B (a few deploys later): switch reads/writes to only the new column.
3. Migration C (after enough confidence): drop the old column.

Each step is independently shippable and rollback-safe.
