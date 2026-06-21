#!/bin/bash
# PreToolUse(Bash) hook (PROC-01, Wave 2): when committing, require a test alongside src/ changes.
# Phase-5 enforcement — "tests ship with the code". Owner chose commit-time enforcement.
#
# WARN-mode (current): prints a reminder, never blocks. BLOCK-mode (future, owner-gated): exit 2.
# Escape: include [no-tests] in the commit message (with a justification) for genuine no-test commits.

DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd)"
# shellcheck source=/dev/null
. "$DIR/_gate-common.sh" 2>/dev/null || exit 0   # fail open

INPUT="$(cat 2>/dev/null)"
CMD="$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null)"

# Only act on `git commit`.
printf '%s' "$CMD" | grep -qE '(^|[^[:alnum:]])git[[:space:]]+commit' || exit 0
# [no-tests] escape anywhere in the command (covers -m "..[no-tests].." and heredoc bodies).
printf '%s' "$CMD" | grep -q '\[no-tests\]' && exit 0

STAGED="$(git diff --cached --name-only 2>/dev/null)"
[ -z "$STAGED" ] && exit 0

# Production source staged? (src/ code, excluding tests/specs/assets)
PROD="$(printf '%s' "$STAGED" | grep -E '(^|/)src/' | grep -E '\.(ts|cs)$' | grep -vE '\.spec\.ts$|\.test\.ts$|Tests\.cs$|(^|/)tests/')"
[ -z "$PROD" ] && exit 0

# Any test staged?
TESTS="$(printf '%s' "$STAGED" | grep -E '\.spec\.ts$|\.test\.ts$|Tests\.cs$|(^|/)tests/')"
[ -n "$TESTS" ] && exit 0

gate_violation "commit stages production src/ changes with no test (*.spec.ts / *Tests.cs / tests/**) and no [no-tests] in the message. Add a colocated test, or justify with [no-tests] in the commit body."
