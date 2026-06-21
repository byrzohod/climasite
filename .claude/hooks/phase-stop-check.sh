#!/bin/bash
# Stop hook (PROC-01, Wave 2): "refuse to finish untested". If production code under src/ changed this
# session and no test command ran, warn (or block, when flipped). Runs alongside workflow-check.sh.
#
# WARN-mode (current): prints a reminder, exit 0. BLOCK-mode (future, owner-gated): exit 2 (blocks Stop).

DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd)"
# shellcheck source=/dev/null
. "$DIR/_gate-common.sh" 2>/dev/null || exit 0   # fail open

INPUT="$(cat 2>/dev/null)"
# Avoid Stop-hook loops: if we're already inside a Stop-hook continuation, don't re-block.
[ "$(printf '%s' "$INPUT" | jq -r '.stop_hook_active // false' 2>/dev/null)" = "true" ] && exit 0
SID="$(printf '%s' "$INPUT" | jq -r '.session_id // "nosession"' 2>/dev/null)"

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
CHANGED="$( { git -C "$ROOT" diff --name-only; git -C "$ROOT" diff --cached --name-only; } 2>/dev/null \
  | grep -E '(^|/)src/' | grep -E '\.(ts|cs)$' | grep -vE '\.spec\.ts$|\.test\.ts$|Tests\.cs$|(^|/)tests/' )"
[ -z "$CHANGED" ] && exit 0

# A test ran this session?
[ -f "/tmp/climasite-test-ran-${SID}" ] && exit 0

gate_violation "production src/ changed this session but no test command ran. Run the suite (e.g. 'dotnet test ClimaSite.NoE2E.slnf' + 'ng test --watch=false') before finishing, or the change is unverified."
