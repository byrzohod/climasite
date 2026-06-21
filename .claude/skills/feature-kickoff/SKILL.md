---
name: feature-kickoff
description: Start a new feature/fix through the gated per-feature pipeline (PROC-01). Use whenever the user wants to begin a new feature, fix, or backlog item — "start feature X", "kick off", "/feature-kickoff", or before any substantive change that will touch src/. Scaffolds docs/features/<FEAT-ID>/ from the template and registers a backlog row, then drops you into Phase 1 (Research).
---

# /feature-kickoff — start a feature through the gated pipeline

Phase 0 of the per-feature pipeline (`docs/features/README.md`). Scaffolds the feature folder and
registers its backlog row, then hands off to Phase 1 (Research). Run it before touching `src/` — the
Wave 2 hooks block code edits without an approved plan, and this is how the plan gets a home.

## Inputs

- **FEAT-ID** — backlog-style ID: a category prefix (`SEC`, `BUG`, `GAP`, `OPS`, `ARCH`, `PERF`,
  `TS`, `DOC`, `UX`, `PROC`) + the next free number in that category. If the user didn't give one,
  derive the category from the work and pick the next number (see step 1).
- **Title** — short imperative title.

## Procedure

1. **Pick the FEAT-ID.** If not supplied, choose the category prefix that fits, then compute the next
   free number in *that* category (numeric, per-prefix — not an alphabetical scan):
   ```bash
   CAT=GAP   # the chosen prefix: SEC|BUG|GAP|OPS|TS|UX|ARCH|PERF|DOC|PROC
   grep -oE "^### ${CAT}-[0-9]+" docs/project-plan/PRIORITIZED_BACKLOG.md \
     | grep -oE '[0-9]+' | sort -n | tail -1     # highest number used; your ID is that + 1 (or 01 if none)
   ```
   Confirm the ID isn't already taken: `ls docs/features/ 2>/dev/null` and re-grep the backlog.

2. **Scaffold the folder** from the template (never edit `_template/` itself):
   ```bash
   FEAT=<FEAT-ID>
   mkdir -p "docs/features/$FEAT"
   for f in research.md plan.md test-design.md STATE.md; do
     cp "docs/features/_template/$f" "docs/features/$FEAT/$f"
   done
   ```

3. **Substitute placeholders** in the copied files — replace `<FEAT-ID>` with the ID, `<short title>`
   with the title, and the date placeholders (`<YYYY-MM-DD>`) with today's date (`date +%F`). Set
   `feature_id:` in `plan.md`'s front-matter. Use Edit on each file; do **not** flip `plan_status`
   (it stays `draft`). Set `STATE.md` → **Current phase: 1 — Research**, **Last updated:** today, and
   stamp the kickoff log line. **Leave for later phases:** `<slug>` and `<feature|fix>` (set at Phase 5
   branch creation), the ADR `NNNN-<slug>` (only if Phase 2 flags one), and `<handle/agent>` (author).

4. **Register the backlog row.** Append a row to `docs/project-plan/PRIORITIZED_BACKLOG.md` in the
   house anatomy (header `### <FEAT-ID> — <title> (<P0-P3>, <Small|Medium|Large>)` then the bold
   fields). The full house anatomy also uses `Closes:` (the most common field — review-finding IDs a
   task resolves); include it when the work closes findings, omit it for net-new features. Minimal
   new-feature row:
   ```
   ### <FEAT-ID> — <title> (P?, ?)
   - **Status:** 🚧 IN PROGRESS (started <date>, pipeline `docs/features/<FEAT-ID>/`).
   - **Description:** <one-paragraph problem statement; fill from research once known>.
   - **Closes:** <review-finding IDs this resolves — omit line entirely if net-new>.
   - **Affected:** <key files — mirror research.md §2 once filled>.
   - **Acceptance:** <observable done — mirror plan.md exit-criteria once written>.
   - **Depends on:** <blocking task IDs / owner decisions, or "None">.
   ```
   Place it under the correct category section (keep P0→P3 order). **If that category section doesn't
   exist yet** (e.g. the first `PROC` item), create the section header `## N. <Name> (<PREFIX>)` in the
   existing numeric order first. If the work needs an owner decision, add it to the "Blocking decisions"
   area and flag it **Needs confirmation**.

5. **Confirm and hand off.** Print the created paths and tell the user Phase 1 (Research) is next:
   fill `docs/features/<FEAT-ID>/research.md` from the real code (grep every affected path), then
   write `plan.md`, then run `/verify-plan <FEAT-ID>`.

## Guardrails

- One commit for the kickoff is fine (`chore(<area>): kick off <FEAT-ID>`), but it is **not** a code
  change — keep `src/` out of it.
- Don't pre-fill research/plan with guesses. The whole point of the gate is that Phase 1 is grounded
  in the real working tree; the verifier (Phase 3) will reject a plan whose paths don't resolve.
- If the change is a tiny doc/config tweak that never touches `src/`, you may skip the folder — but
  anything touching `src/` needs an approved `plan.md`, so kick it off here.
