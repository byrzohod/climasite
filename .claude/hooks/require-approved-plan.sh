#!/bin/bash
# PreToolUse(Edit|Write) hook (PROC-01, Wave 2): gate edits to production code under src/ behind an
# approved feature plan. Phase-3 enforcement — "no code without an approved plan".
#
# WARN-mode (current): prints a reminder, never blocks. BLOCK-mode (future, owner-gated): exit 2.
# Escapes:
#   - CLIMASITE_HOTFIX=1 in the environment → genuine hotfix, skip the gate (edit-time escape).
#   - Non-production files (tests, specs, assets, docs) are never gated here.
#
# v1 heuristic: passes if ANY feature has plan_status: approved (an active, approved plan exists).
# A stricter edit→feature mapping can land when this flips to block.

DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd)"
# shellcheck source=/dev/null
. "$DIR/_gate-common.sh" 2>/dev/null || exit 0   # fail open if the helper is missing

INPUT="$(cat 2>/dev/null)"
FILE="$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // empty' 2>/dev/null)"
[ -z "$FILE" ] && exit 0

# Only gate production code under a src/ directory.
case "$FILE" in
  */src/*|src/*) : ;;
  *) exit 0 ;;
esac
# Skip non-production: tests/specs and asset/markup/style/config files.
case "$FILE" in
  *.spec.ts|*.test.ts|*Tests.cs|*/tests/*) exit 0 ;;
  *.md|*.json|*.scss|*.css|*.html|*.svg) exit 0 ;;
esac

# Edit-time hotfix escape.
[ "${CLIMASITE_HOTFIX:-}" = "1" ] && exit 0

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
if grep -rqsE '^plan_status:[[:space:]]*approved' "$ROOT"/docs/features/*/plan.md 2>/dev/null; then
  exit 0
fi

gate_violation "editing production code '$FILE' but no feature has 'plan_status: approved'. Run /feature-kickoff then /verify-plan first, or set CLIMASITE_HOTFIX=1 for a genuine hotfix."
