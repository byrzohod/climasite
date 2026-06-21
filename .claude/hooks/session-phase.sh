#!/bin/bash
# SessionStart hook (PROC-01, Wave 2): surface any active feature's current phase + the pipeline gate.
# Always non-blocking (stdout is injected into context). Silent when no feature is mid-flight.

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
shopt -s nullglob 2>/dev/null

active=""
for st in "$ROOT"/docs/features/*/STATE.md; do
  case "$st" in *"/_template/"*) continue ;; esac
  fid="$(basename "$(dirname "$st")")"
  phase="$(grep -m1 '^- \*\*Current phase:\*\*' "$st" 2>/dev/null | sed 's/^- \*\*Current phase:\*\* *//')"
  case "$phase" in
    ""|*Merge*|*merged*|*done*|*Done*) : ;;   # finished or unknown → skip
    *) active="${active}
  • ${fid} — phase: ${phase}" ;;
  esac
done

if [ -n "$active" ]; then
  echo "[PROC-01 pipeline] Active feature(s):${active}"
  echo "  Gates: no src/ edits until plan_status: approved (run /verify-plan); tests ship with code; see docs/features/README.md."
fi
exit 0
