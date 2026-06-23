---
name: db-migrate
description: Database migration procedure with expand-contract pattern, backward-compatible schema changes, rollback scripts, backup verification, large-table migration safety, and connection management. Use this whenever the user mentions database migrations, schema changes, ALTER TABLE, adding/dropping columns, changing indexes, migration safety, zero-downtime migrations, or wants to verify a migration won't lock or break production.
---

# /db-migrate - Database migration, backup, and connection management

## When to use

Invoke this skill:
- **When adding or modifying database schema**
- **Before deploying a migration to production**
- **When setting up database backups**
- **When diagnosing connection pool issues**

## Migration Rules

### Hard rules

- Use a migration tool appropriate to the stack (e.g., EF Core Migrations, Prisma Migrate, Flyway, Alembic, Knex -- whatever fits)
- All migrations version-controlled alongside application code
- **Never hand-edit the production database** -- all changes go through migrations
- **Backward-compatible migrations** -- never drop a column/table in the same deploy that removes the code using it
- **Expand/contract is mandatory on trunk (Blueprint §C-2 / §J.5)** -- never ship a destructive (or otherwise backward-incompatible) schema change in the *same PR* as the code that depends on it. The additive **expand** migration merges first; the **contract** migration is a separate, later PR. See "Expand/Contract on Trunk" below.
- **Every `up` migration has a corresponding `down`** (rollback script)
- **Test migrations against production-like data volumes** -- a migration that works on 100 rows may lock a table with 10M rows

### Expand/Contract on Trunk -- the MANDATE (Blueprint §C-2 / §J.5)

> **Never ship a destructive schema change in the same PR as the code that depends on it.**
> **Expand -> deploy -> migrate data -> contract in a *later* PR.** Every step is backward-compatible and individually rollback-able.

This is not a style preference; it is the rule that makes schema changes safe under **trunk-based delivery** (short-lived branches + merge queue + feature flags -- see `/trunk-merge`). On trunk, `main` is always deployable and every green merge can ship at any moment. A PR that *both* drops `old_column` *and* moves the code off it is unsafe the instant it lands: any instance still running the previous build -- or any in-flight request, or a needed `git revert` of that merge -- hits a schema that no longer matches its code. **Reverting the code does not un-run the SQL**, so you cannot cleanly back it out either. Splitting the change across PRs is what keeps every point in time consistent in *both* directions (roll forward and roll back).

**The PR boundary, made concrete:**

- **Expand and contract are different PRs, shipped at different times.** Expand is purely additive and backward-compatible (old code keeps working). Contract is purely subtractive and only lands *after* nothing reads/writes the old shape. Never collapse them.
- **The expand migration runs and is verified in production *before* the dependent code merges.** Additive migrations deploy ahead of the code (see Step 6). The code that uses the new column/table can ride a later PR -- often behind a **default-off feature flag** (`/feature-flag`) so the read-switch is a flag flip, not a deploy, and is instantly reversible.
- **The contract PR is gated on real-world drain, not on the calendar.** Open it only after you have confirmed (via metrics/logs) that no running build, no flag still off, and no replay path touches the old column/table. Until then the old shape stays.
- **Each PR is independently revertible and the migration has a tested `down`.** Because no single PR straddles "drop + depend", a `git revert` of any one of them leaves schema and code consistent. (Reverting *code* never rolls back *data*/DDL -- that's `/rollback`'s boundary; for a botched migration use the `down` script or restore from backup, see Rollback below.)

Anything that is irreversible-in-place is C-2 territory: dropping or renaming a column/table, narrowing a type, adding a `NOT NULL` without a default, tightening a constraint/unique index on existing data, or any rename (= add + backfill + switch + drop). Renames are **never** in-place on trunk -- they are a full expand/contract cycle.

> Pure-additive, backward-compatible changes (a new nullable column nothing yet reads, a new table, `CREATE INDEX CONCURRENTLY`) are the **expand** step itself and may ship in one PR -- they don't break old code. The mandate is specifically about **not pairing a destructive change with its dependent code**.

`/trunk-merge` enforces this as a pre-merge gate: a unit touching the schema must use expand/contract and must carry **no destructive change in the same PR as the code using it** before it may enter the merge queue.

### Expand-Contract Pattern (for breaking schema changes)

Breaking schema changes must be split into multiple deploys (on trunk, **multiple PRs** -- see the mandate above):

**Phase 1: Expand**
- Add new column/table
- Start writing to both old and new
- Old code still reads from old column/table

**Phase 2: Migrate Data**
- Backfill data from old to new (run as a separate migration or background job)
- Verify data integrity

**Phase 3: Switch Reads**
- Update code to read from new column/table
- Deploy and verify
- Old code path still exists as fallback

**Phase 4: Contract**
- Stop writing to old column/table
- Deploy and verify
- Only after confirmation: drop the old column/table in a **separate, later migration/PR** -- never in the PR that introduced the new shape or switched the reads

**Never collapse these phases into one deploy -- and on trunk, never into one PR (§C-2).** Collapsing is how data loss happens, and it makes the change un-revertible (a `git revert` won't un-run the drop). The destructive Phase 4 migration always lands after Phases 1-3 have shipped and drained.

## Migration Process

### Step 1: Plan the migration

- What is changing?
- Is this a breaking change (requires expand-contract)?
- How large is the affected table?
- Will this lock the table during migration?
- What's the rollback plan?

### Step 2: Write the migration

- Use the project's migration tool
- Include both up and down scripts
- Add comments explaining non-obvious changes
- Review the generated SQL (ORMs sometimes generate bad SQL)

### Step 3: Test locally

- Run the migration on a development database
- Verify schema matches expectations
- Verify data integrity after migration
- Test the down migration (rollback)
- Test the application with the new schema

### Step 4: Test with production-like data

- Use a staging database with realistic data volume
- Measure migration time
- Check for table locks during migration
- If migration is slow, consider:
  - Running in batches
  - Using online DDL (MySQL) or CREATE INDEX CONCURRENTLY (PostgreSQL)
  - Running as a background job

### Step 5: Backup before production migration

- Always take a database backup before running a production migration
- Verify the backup is restorable (not just "backup taken")
- Document the backup location and timestamp

### Step 6: Deploy the migration

- Run migrations BEFORE deploying the application code (for additive changes)
- Or AFTER the application code (for contract-phase cleanups)
- Monitor the migration -- have `kill` ready if it locks for too long
- Verify success before proceeding with app deployment

### Step 7: Verify

- Run smoke tests against the new schema
- Monitor error rates for migration-related issues
- Be ready to rollback (see Rollback section below)

## Rollback

### Preparing for rollback

Every migration must have a rollback plan:

1. **Down migration script** exists and is tested
2. **Backup taken** before the migration
3. **Rollback runbook** documented for complex scenarios

### When to rollback

- Data corruption detected
- Application errors related to schema mismatch
- Performance regression caused by the migration
- Uncommitted data loss

### How to rollback

Order depends on the direction:

**If app deployed AFTER migration** (normal case):
1. Rollback app to previous version
2. Run `down` migration
3. Verify data integrity

**If migration caused data loss**:
1. Restore from backup
2. Rollback app to previous version
3. Investigate before attempting again

**If migration is still running and stuck**:
1. Kill the migration process
2. Check data state -- migration may have partial effects
3. Manually clean up if needed
4. Rollback app

## Data Modeling Principles

- Review data model as part of the planning phase, not as an afterthought
- Seed data scripts for development and testing environments
- Document entity relationships (comment block or diagram in `docs/`)
- Consider data access patterns when designing schema -- optimize for how data is read
- Add indexes for foreign keys and commonly queried columns
- Use appropriate data types (don't use VARCHAR(255) for everything)
- Consider soft deletes vs hard deletes based on data retention needs

## Connection Management

### Connection Pooling

- Configure connection pool size based on expected concurrency -- do not use defaults blindly
- Rule of thumb: pool size = (concurrent requests) * (avg queries per request) -- measure and adjust
- Monitor pool metrics: active connections, idle connections, waiting requests, pool exhaustion events
- Set appropriate timeouts:
  - **Connect timeout**: how long to wait for a connection
  - **Query timeout**: how long a query can run
  - **Idle timeout**: how long idle connections live

### Connection Health

- Validate connections before use (stale connection detection)
- Handle connection drops gracefully (automatic reconnection)
- For serverless environments: use a connection proxy (PgBouncer or cloud-native equivalent) to prevent pool exhaustion from ephemeral functions

## Backup Strategy

### Backups

- **Automated database backups**: daily minimum, hourly for critical data
- **3-2-1 rule**: 3 copies of data, on 2 different media types, with 1 offsite/cloud copy
- **At least one immutable backup**: cannot be deleted or encrypted by ransomware
- **Backup monitoring**: verify backups actually complete successfully -- a silent failure is worse than no backup (false confidence)

### Recovery

- Define **RTO** (Recovery Time Objective): how long can you be down? Minutes? Hours?
- Define **RPO** (Recovery Point Objective): how much data can you lose? Last hour? Last day?
- **Test restore procedure at least quarterly** -- an untested backup is not a backup
- **Document the restore runbook**: step-by-step instructions, not "restore from backup"

## Checklist Summary

Before any production schema change:
- [ ] Migration written with both up and down scripts
- [ ] Tested locally
- [ ] Tested against production-like data volume
- [ ] Not a breaking change, or properly split via expand-contract
- [ ] **No destructive schema change in the same PR as the code that depends on it (§C-2)** -- expand merges first, contract is a later PR
- [ ] Backup taken and verified restorable
- [ ] Rollback procedure documented
- [ ] Migration deployed in the correct order (before or after app)
- [ ] Smoke tests verified post-migration
- [ ] Monitoring shows no regressions

## Related

- [[../Agent Workflow]] -- §13 (Data & migrations), §3.3 + §J.5 ("Migrations are expand/contract" on trunk), §C-2 (never a destructive schema change in the same PR as the dependent code).
- `/trunk-merge` -- enforces §C-2 as a pre-merge gate: a schema-touching unit must use expand/contract with no destructive change in the same PR as the code using it before it can enter the merge queue.
- `/feature-flag` -- gate the read-switch (Phase 3) behind a default-off flag so flipping to the new shape is a reversible flag flip, not a deploy.
- `/rollback` -- the boundary skill when a migration goes wrong: a `git revert` rolls back **code, not data/DDL**. Use the `down` script or restore from backup for schema/data.
