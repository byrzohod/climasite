#!/usr/bin/env bash
# SessionStart hook — re-prime a fresh / cleared / compacted / resumed context from durable state.
#
# Claude Code injects a SessionStart hook's stdout as context (fires with source =
# startup | resume | clear | compact). So after you /clear, after auto-compaction, or on
# --resume, the NEXT context automatically re-reads STATE.md — no manual re-priming.
#
# Keep STATE.md LEAN (a resume contract, not a dump) — it is injected every session start.
# The agent keeps it fresh via /checkpoint at unit/phase boundaries.
#
# BACKSTOP: even if STATE.md accretes archived history, only its LIVE TOP is injected — this hook
# stops at the first historical/archive section header so archaeology never re-enters a fresh
# context (this is a safety net; /checkpoint keeping STATE.md lean is the primary mechanism).
set -euo pipefail

STATE=".planning/STATE.md"
[ -f "$STATE" ] || exit 0   # no project state yet — nothing to inject

echo "## Session context restored from $STATE (auto-injected by the SessionStart hook)"
echo "Continue from **Next action** below; open the referenced plan / Knowledge files as needed. If anything looks stale, re-verify against git + the active unit-plan, then run /checkpoint."
echo

# Print STATE.md up to (but not including) the first archived/historical section, if any.
# Matched on ASCII substrings so it's robust to the leading emoji. "## Foundational milestones"
# (short, useful orientation) deliberately does NOT match and stays injected.
primed="$(awk '
  /^## Recently done/           { exit }
  /^## .*RESUME HERE/           { exit }
  /^## .*Done \(all merged/     { exit }
  /^## After #/                 { exit }
  /^## Merged to main/          { exit }
  /^## Remaining backlog after/ { exit }
  { print }
' "$STATE")"

printf '%s\n' "$primed"

# If we stopped early, tell the reader where the rest is.
if [ "$(printf '%s\n' "$primed" | wc -l)" -lt "$(wc -l < "$STATE")" ]; then
  echo
  echo "_(SessionStart trimmed older/archived sections from this injection — open \`.planning/STATE.md\` for the full file; per-PR history lives in \`CHANGELOG.md\`.)_"
fi
exit 0
