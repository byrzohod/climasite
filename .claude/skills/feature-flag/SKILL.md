---
name: feature-flag
description: Create, list, and expire feature flags so INCOMPLETE units can merge dark to a green trunk -- decoupling deploy from release. Use this whenever a unit/wave isn't finished but its branch must merge today (trunk-based, <24h branches), whenever you need to hide in-progress code behind a default-off switch, gate a risky change for gradual rollout, or kill-switch a feature, or when the user mentions feature flags, feature toggles, flagd, OpenFeature, dark launch, gating incomplete work, merge-dark, deploy != release, or flag cleanup/expiry. Hobby tier = typed JSON/env flag file; production tier = self-hosted OpenFeature + flagd. Every flag carries a creation-date + expiry + a temp- prefix for short-lived ones; removal is a Definition-of-Done item.
---

# /feature-flag - Create / list / expire flags to merge incomplete work dark

The mechanism that lets trunk-based delivery hold its core promise: a unit that is NOT finished still merges to a **green** trunk the same day, hidden behind a **default-off** switch, so the build never goes red and no long-lived branch accumulates. Flags **decouple deploy from release** -- code ships dark on merge; the feature turns on later, independently, and can be turned back off without a revert.

This skill owns the **flag lifecycle**, not flag *evaluation logic* in app code: create a flag (with its expiry baked in), list what exists (and what's overdue), and drive expiry/removal. It is the counterpart to `/flag-cleanup` (which does the AST-based stale-flag *removal*) and `/trunk-merge` (which merges the dark code).

## When to use

Invoke this skill:
- **When a unit/wave must merge before it is fully done** (trunk-mode, short-lived branch <24h) -- wrap the incomplete path in a new **default-off** flag so it merges dark to green trunk. This is the primary case.
- **For gradual / staged rollout** -- ship to 0% -> internal -> a percentage -> 100%, decoupled from the deploy that carried the code.
- **For a kill-switch** on a risky or expensive path (a costly LLM call, a new payment route, a third-party integration that can fail) -- so it can be turned off in seconds without a deploy or a revert.
- **For an experiment / A-B** where two code paths coexist behind a flag.
- **At every Definition-of-Done check** -- to *expire* flags whose work is complete (removal is a DoD item, see §9.4) and to list flags overdue for removal.

This is a **trunk-mode mechanism** (Blueprint §J.5): ON whenever the project is trunk-based -- which is always. When in doubt, flag it: an unflagged incomplete merge is what turns the trunk red.

## When NOT to use

- **The unit is fully complete and reviewed.** Merge it on -- no flag. Flags are for *incomplete* or *risky-rollout* work, not a default wrapper around every change. A flag with no reason to exist is immediate stale-flag debt.
- **Removing a stale / expired flag from the code.** That is `/flag-cleanup` (AST-based, e.g. Piranha; review-gated; never auto-merge). This skill *flags it for removal and lists it*; `/flag-cleanup` does the surgical deletion. They are called together during `/hygiene-sweep` (Blueprint §K.6).
- **You need to merge the dark code itself / drive the queue.** That is `/trunk-merge` (short branch -> push -> merge queue -> auto-merge on green). This skill creates the switch; `/trunk-merge` ships the wrapped code.
- **Hiding a *breaking schema* change.** A flag hides code paths, not a destructive migration. Use the **expand/contract** mandate in `/db-migrate` (never drop a column in the same PR as the code using it). A flag may gate the *read-switch* step of expand/contract, but it does not replace it.
- **Backing out already-released code.** Prefer `/rollback` (revert-first beats hotfix on trunk). A kill-switch flag is the *pre-planned* off-ramp; `/rollback` is the unplanned one.
- **Long-term configuration** (region, plan tier, permanent capability). That is config, not a feature flag -- it has no expiry and no `temp-` prefix and lives in normal config, not the flag store.

## Flag taxonomy + naming

Pick the right *kind* up front -- it sets the expiry policy and whether the flag is short-lived:

| Kind | Purpose | Lifespan | Prefix | Expiry |
|---|---|---|---|---|
| **release** (the trunk case) | hide an incomplete unit until done | **short** -- days to a few weeks | `temp-` | required; **removal is a DoD item** |
| **ops / kill-switch** | turn a risky/costly path off fast | medium -- lives while the risk does | `ops-` | review-by date, not a hard delete |
| **experiment / A-B** | two paths coexist for a test | short -- the test window | `temp-exp-` | required; remove at experiment end |
| **permission / entitlement** | gate by plan/role | long-lived (this is config, see "When NOT") | `perm-` | n/a -- not a temp flag |

Naming rule: **`<prefix><project>-<unit-or-feature>`**, lowercase-kebab. The **`temp-` prefix is mandatory for every short-lived flag** -- it is the signal the `flag-expiry-warn` hook (Blueprint §E) and `/flag-cleanup` grep for. A short-lived flag without `temp-` is invisible to the cleanup machinery and becomes permanent debt.

## Tooling by tier (Blueprint §J.5)

Trusted-source-only, self-hosted, no new floating-npx surface.

- **Hobby tier -> a typed JSON / env flag file.** One checked-in file, read at boot, typed accessor in code. No service, no network, no dependency. Default for solo weekend projects.

  ```jsonc
  // flags.json  (checked into the repo; the single source of truth for hobby tier)
  {
    "$schema": "./flags.schema.json",
    "flags": {
      "temp-sonar-reid-ui": {
        "kind": "release",
        "value": false,            // default-OFF -- merges dark
        "created": "2026-06-21",
        "expiry": "2026-07-21",    // hard removal target (DoD)
        "owner": "byrzohod",
        "unit": ".planning/.../units/12/unit-plan.md",
        "reason": "VLM re-ID UI half-built; merge dark behind off switch"
      }
    }
  }
  ```
  Wrap the accessor in one typed helper (e.g. `flag("temp-sonar-reid-ui")`) so flag reads are greppable and `/flag-cleanup` can find every call site. Never read the JSON inline at call sites.

- **Production tier -> self-hosted OpenFeature + flagd.** OpenFeature is the vendor-neutral SDK (no lock-in); **flagd** is the self-hosted, file/CRD-backed evaluation daemon -- run it in-cluster, point it at a git-backed flag definition file. No SaaS flag vendor, no third-party data egress. Same lifecycle metadata (created/expiry/owner/reason) lives in the flagd definition.

In **both** tiers the flag store is **git-backed Markdown/JSON/YAML** -- auditable, reversible, diffable in review. The *evaluation* mechanism differs by tier; the *lifecycle contract* (this skill) is identical.

## Process

### Create a flag

1. **Confirm a flag is actually warranted.** The unit is incomplete (trunk case) OR the path is risky/costly/experimental. If the unit is done and reviewed, stop -- merge without a flag (see "When NOT to use").
2. **Read prior flags first.** List existing flags (next section) to avoid a duplicate, and check `Knowledge/<Project>/` for any prior ADR on flag conventions. Reuse an existing flag if one already gates this path.
3. **Pick the kind + name** from the taxonomy. Short-lived -> **`temp-` prefix, mandatory.** Compose `<prefix><project>-<unit>`.
4. **Default it OFF.** A release flag merges dark: `value: false` in prod everywhere. Any non-default state is an explicit, later, separate change. (A flag that defaults ON defeats merging dark.)
5. **Bake in the lifecycle metadata -- all of it, at creation:**
   - `created` = today's date (the create date is non-negotiable; Blueprint §J.5).
   - `expiry` = a concrete date (release/experiment flags) or a `review-by` date (ops). **No flag is created without an expiry** -- "remove when convenient" is how flags rot.
   - `owner`, `unit` (link to the `unit-plan.md`), `reason` (one line: why this exists / what turns it on).
6. **Write it to the tier store** (`flags.json` for hobby; the flagd definition for prod) and add the **typed accessor** call in code. Keep all flag reads behind the one helper so they stay greppable.
7. **Make removal a DoD line for the owning unit.** Add to that unit's Definition of Done (§9.4): *"`temp-<name>` removed (code + flag store) before the unit is DONE."* The flag's existence is a debt the unit must repay.
8. **Record the decision.** Push a short ADR via `/kb-capture` to `Knowledge/<Project>/` (what the flag gates, its expiry, what turns it on) so the next planning loop sees it.

### List flags (and find what's overdue)

9. **Enumerate every flag** in the tier store. For each, surface: kind, current default, `created`, `expiry`, owner, the owning unit, and **age vs expiry**. Then partition into:
   - **overdue** -- `expiry` has passed, or a `temp-` flag whose unit is DONE but the flag still exists. These are the `flag-expiry-warn` hook's targets (Blueprint §E) and the `/flag-cleanup` queue.
   - **active** -- not yet expired, still gating live in-progress or risk-managed code.
   - **mis-shaped** -- a short-lived flag missing the `temp-` prefix, a flag with no `expiry`, or a flag with no owning unit. Fix the shape (it is invisible to cleanup otherwise).
10. **Report, don't auto-delete.** Listing produces the queue; the actual code removal is `/flag-cleanup` (review-gated). Feed the overdue list into `/hygiene-sweep`.

### Expire / remove a flag

11. **Trigger:** the unit is DONE (DoD), the experiment ended, the rollout reached 100% and is stable, or the `expiry`/`review-by` date passed.
12. **Decide outcome before deleting code:** the kept path is the **on** path (the new feature stays) or the **off** path (experiment lost / feature dropped). Removal must collapse the branch to the surviving path -- never leave both.
13. **Hand the code deletion to `/flag-cleanup`** (AST-based, e.g. Piranha per stack; review-gated; **never auto-merged**). It deletes the flag check, the dead branch, and the typed accessor call.
14. **Delete the flag definition** from the tier store in the same change set as the code removal.
15. **Tick the DoD line** for the unit ("flag removed"). Log the removal via `/kb-capture` (supersede the create ADR). The flag is gone from both code and store, or it is NOT removed.

## Iteration & STOP

- **Create** is one-shot per incomplete/risky unit -- create, default-off, expiry baked in, DoD line added. Do not iterate; do not create speculative flags for work that isn't started.
- **List** runs at every DoD check, inside `/hygiene-sweep`, and whenever the `flag-expiry-warn` hook fires on commit. **STOP** when the overdue partition is empty -- every `temp-` flag is either still within its expiry with a live reason, or queued to `/flag-cleanup`.
- **Expire** is DONE only when the flag is removed from **both** the code (via `/flag-cleanup`) **and** the tier store, the surviving path is the sole path, and the DoD line is ticked. A flag deleted from the store but still referenced in code (or vice-versa) is NOT done -- that is a half-removed flag, the worst state.
- **The anti-goal is stale-flag accumulation** -- short-lived flags that outlive their reason and become permanent forks in the code. The repo's health is measured by *zero overdue `temp-` flags*. If overdue flags pile up, that is workflow drift -- escalate it into `/hygiene-sweep` rather than letting flags become permanent forks in the code.

## See also

- `trunk-merge` -- merges the dark code this flag hides (short branch -> queue -> auto-merge on green); the two run together per unit on trunk-mode (Blueprint §J).
- `flag-cleanup` -- the AST-based, review-gated removal of stale/expired flags; this skill *lists and queues*, `/flag-cleanup` *deletes*. Both invoked from `/hygiene-sweep` (Blueprint §K.6).
- `rollback` -- revert-first off-ramp for already-released code; a kill-switch flag is the pre-planned alternative to it.
- `db-migrate` -- a flag hides code paths, not schema; breaking schema uses expand/contract. A flag may gate the read-switch step.
- `verify-work` / Definition of Done (§9.4) -- where flag *removal* is enforced as a DoD item and where expiry is checked.
- `kb-capture` -- persists flag-creation and flag-removal decisions as ADRs in `Knowledge/<Project>/`.
- Hook `flag-expiry-warn` (Blueprint §E) -- on commit, warns when a `temp-`/expired flag is older than its expiry; this skill's "list" step resolves that warning.
