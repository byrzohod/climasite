#!/bin/bash
# Shared helpers for the PROC-01 phase-aware gate hooks (Wave 2).
#
# MODE is read from `.claude/hooks/gate-mode` (a single word):
#   warn  (default) — print the violation to stderr and EXIT 0 (non-blocking).
#   block           — print the violation to stderr and EXIT 2 (blocks the tool call).
#
# Staged as `warn` first (owner decision 2026-06-21). Flipping to `block` is a deliberate,
# separately-reviewed change (one word in gate-mode) and is the "hard gate" that requires owner
# sign-off — a buggy blocking PreToolUse hook can block every edit in every session.
#
# Design rule: these helpers FAIL OPEN. Any internal error must not block the user; only an explicit
# detected violation in `block` mode exits 2.

GATE_HOOK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd)"

gate_mode() {
  local f="${GATE_HOOK_DIR}/gate-mode"
  if [ -r "$f" ]; then
    local m; m="$(tr -d '[:space:]' < "$f" 2>/dev/null)"
    case "$m" in block) echo block ;; *) echo warn ;; esac
  else
    echo warn
  fi
}

# gate_violation "<message>" — record a detected violation and exit per mode.
gate_violation() {
  local msg="$1"
  if [ "$(gate_mode)" = "block" ]; then
    echo "BLOCKED (PROC-01 gate): ${msg}" >&2
    exit 2
  fi
  echo "[PROC-01 gate · warn] ${msg}" >&2
  exit 0
}
