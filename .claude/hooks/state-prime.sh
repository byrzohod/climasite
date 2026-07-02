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
# (short, useful orientation) deliberately does NOT match and stays injected. awk signals whether
# it trimmed via its exit code (3 = trimmed) so we don't infer truncation from line counts (command
# substitution strips trailing newlines, which would make a wc-based check unreliable).
set +e
primed="$(awk '
  /^## Recently done/           { trimmed=1; exit }
  /^## .*RESUME HERE/           { trimmed=1; exit }
  /^## .*Done \(all merged/     { trimmed=1; exit }
  /^## After #/                 { trimmed=1; exit }
  /^## Merged to main/          { trimmed=1; exit }
  /^## Remaining backlog after/ { trimmed=1; exit }
  { print }
  END { exit (trimmed ? 3 : 0) }
' "$STATE")"
trimmed=$?
set -e

# awk should exit 0 (no trim) or 3 (trimmed); anything else is a real failure — don't inject partial state.
if [ "$trimmed" -ne 0 ] && [ "$trimmed" -ne 3 ]; then
  echo "state-prime: failed to read/prime $STATE" >&2
  exit "$trimmed"
fi

printf '%s\n' "$primed"

if [ "$trimmed" -eq 3 ]; then
  echo
  echo "_(SessionStart trimmed older/archived sections from this injection — open \`.planning/STATE.md\` for the full file; per-PR history lives in \`CHANGELOG.md\`.)_"
fi
exit 0
