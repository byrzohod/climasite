# Runbook — Trunk merge queue + ruleset (adoption Phase B)

> **STATUS: DEFERRED — plan-blocked (2026-06-24).** The GitHub **merge queue** rule and **`evaluate`**
> (dry-run) ruleset enforcement both require a **paid plan** (merge queue → Team/Enterprise; evaluate →
> Enterprise). `byrzohod/climasite` is a personal **public repo on the Free plan**, so both are rejected
> with HTTP 422. **Current gate = the existing classic branch protection** (6 required checks incl.
> `Test Summary`, admins-included, no force-push/delete, PR-required). The `merge_group` trigger in
> `test.yml` is in place but **inert** until a queue exists. Apply the steps below **only after** the
> repo is on a supporting plan. Tracked as **OPS-11** in `docs/project-plan/PRIORITIZED_BACKLOG.md`.

ClimaSite adopted the vault's trunk-based delivery model. The **GitHub merge queue** is the real merge
gate: nothing lands on `main` except through the queue, which re-runs the full suite against
*future-main* and merges only on green.

## Pieces

- **CI trigger** — `.github/workflows/test.yml` has `merge_group: types: [checks_requested]` in `on:`
  so every job (and the `Test Summary` aggregator) re-runs on the queue's temporary `merge_group` ref.
- **Ruleset** — `.github/rulesets/trunk-default-branch-protection.json` is the source of truth:
  `pull_request` (0 approvals — AI auto-merge kept) + `merge_queue` (SQUASH, ALLGREEN) +
  `required_status_checks` requiring the **`Test Summary`** context + linear-history / non-fast-forward /
  no-deletion. Reconciled to climasite's CI (Test Summary already `needs[]` every gate).

## Apply it (evaluate → active)

The skill mandates **evaluate first** (dry-run; blocks nothing), then flip to **active** only after a
real `merge_group` run reports `Test Summary` green — never flip on red/missing.

```bash
REPO=byrzohod/climasite
DEFAULT=main
RS=.github/rulesets/trunk-default-branch-protection.json

# 1) Apply in EVALUATE (safe — reports would-block decisions without blocking):
sed "s/~DEFAULT_BRANCH/refs\/heads\/$DEFAULT/" "$RS" \
  | jq 'del(._comment) | .enforcement="evaluate"' \
  | gh api --method POST "repos/$REPO/rulesets" --input -

# 2) Confirm a normal PR still merges and a real merge_group run reports "Test Summary" green.

# 3) Flip to ACTIVE (enables the queue as the gate). This is an all-PR-blocking change — confirm first:
RS_ID=$(gh api "repos/$REPO/rulesets" -q '.[]|select(.name=="trunk-default-branch-protection")|.id'|head -1)
gh api "repos/$REPO/rulesets/$RS_ID" | jq '.enforcement="active"' \
  | gh api --method PUT "repos/$REPO/rulesets/$RS_ID" --input -
```

## Rollback

```bash
RS_ID=$(gh api repos/byrzohod/climasite/rulesets -q '.[]|select(.name=="trunk-default-branch-protection")|.id'|head -1)
gh api --method DELETE repos/byrzohod/climasite/rulesets/$RS_ID   # removes the queue/ruleset; classic branch protection remains
```

## Notes / cautions

- **Cost/benefit on a solo repo:** the queue serializes concurrent merges and re-runs the full suite per
  merge — valuable for trunk discipline, heavier latency for a single developer. It's the vault standard.
- The **classic branch protection** (6 required checks) stays in place; the ruleset adds the queue and
  supersedes it as the gate once active. Consider removing the classic protection after the ruleset is
  proven active to avoid double-gating.
- If the queue ever stalls, check that `merge_group` is still in `on:` and that `Test Summary` actually
  reports on the merge_group run.
