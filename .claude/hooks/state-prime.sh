#!/usr/bin/env bash
# SessionStart hook — re-prime a fresh / cleared / compacted / resumed context from durable state.
#
# Claude Code injects a SessionStart hook's stdout as context (fires with source =
# startup | resume | clear | compact). So after you /clear, after auto-compaction, or on
# --resume, the NEXT context automatically re-reads STATE.md — no manual re-priming.
#
# Keep STATE.md LEAN (a resume contract, not a dump) — it is injected every session start.
# The agent keeps it fresh via /checkpoint at unit/phase boundaries.
set -euo pipefail

STATE=".planning/STATE.md"
[ -f "$STATE" ] || exit 0   # no project state yet — nothing to inject

echo "## Session context restored from $STATE (auto-injected by the SessionStart hook)"
echo "Continue from **Next action** below; open the referenced plan / Knowledge files as needed. If anything looks stale, re-verify against git + the active unit-plan, then run /checkpoint."
echo
cat "$STATE"
exit 0
