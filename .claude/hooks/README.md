# Claude Code hooks (ClimaSite)

Two families of hooks run in Claude Code sessions, wired in `.claude/settings.json`.

## 1. Git / secret safety (always blocking — pre-existing)
Inline `bash -c` `PreToolUse` guards that **exit 2** (block): destructive-git, push-to-main, branch-name
format, conventional-commit format, and sensitive-file writes. These are unconditional and were here
before PROC-01. Leave them alone.

## 2. PROC-01 phase-aware gates (Wave 2 — staged `warn`, flip to `block` later)
These enforce the per-feature pipeline (`docs/features/README.md`). They share `_gate-common.sh` and
read their mode from **`gate-mode`** (one word):

| Hook | Event | What it does |
|------|-------|--------------|
| `session-phase.sh` | SessionStart | Surfaces any active feature's current phase + the gate (non-blocking). |
| `require-approved-plan.sh` | PreToolUse(Edit\|Write) | Gates edits to production `src/` code behind a feature with `plan_status: approved`. Escape: `CLIMASITE_HOTFIX=1`. |
| `require-tests-on-commit.sh` | PreToolUse(Bash, `git commit`) | Requires a test staged alongside `src/` changes. Escape: `[no-tests]` in the commit message. |
| `test-ran-marker.sh` | PostToolUse(Bash) | Drops `/tmp/climasite-test-ran-<session>` when a test command runs (non-blocking). |
| `phase-stop-check.sh` | Stop | "Refuse to finish untested": warns/blocks when `src/` changed this session but no test ran. |

### Mode: warn vs block
- **`warn`** (current default): a detected violation prints `[PROC-01 gate · warn] …` to stderr and
  **exits 0** — nothing is blocked. This is the safe staging mode (owner decision 2026-06-21).
- **`block`**: a violation prints `BLOCKED (PROC-01 gate): …` and **exits 2** — the tool call (or Stop)
  is blocked.

**Flipping to `block` is the hard gate** and requires owner sign-off: a buggy blocking `PreToolUse`
hook can block *every* edit in *every* session. To flip, change the single word in `gate-mode` to
`block` (in its own reviewed PR), after the pipeline has run real features through it in `warn`.

The gate scripts **fail open**: any internal error exits 0. Only an explicitly detected violation in
`block` mode exits 2.

### Testing a hook in isolation
```bash
echo '{"tool_input":{"file_path":"src/x.ts"}}' | .claude/hooks/require-approved-plan.sh; echo "exit=$?"
echo '{"tool_input":{"command":"git commit -m \"feat: x\""}}' | .claude/hooks/require-tests-on-commit.sh; echo "exit=$?"
```

### Not yet implemented (tracked)
A **git-native backstop** (husky/commitlint) and **managed-settings locking** (so the hooks can't be
silently disabled) are recorded in the SDLC plan's defaults. They are deferred: husky is a *blocking*
git-native hook that deserves the same staged rollout + its own review, and managed-settings is
machine-level config (not committed to the repo). Tracked in `docs/features/PROC-01/STATE.md`.
