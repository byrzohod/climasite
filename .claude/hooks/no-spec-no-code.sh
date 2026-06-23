#!/usr/bin/env bash
# PreToolUse(Write|Edit) — enforce "never implement without an approved per-unit plan" (Workflow §2.5).
# Blocks writing/editing application SOURCE CODE unless a unit plan exists under .planning/.
# Tests, docs, config, planning, and Knowledge files are never gated.
# Escape hatch for a throwaway/exploratory spike:  ALLOW_EXPLORATORY=1
#
# Note: this is an EXISTENCE gate (a unit-plan.md must exist) — the finer "is THIS the
# approved plan for THIS unit" rule is enforced by /plan-tree + STATE.md, not the hook.
set -euo pipefail

INPUT=$(cat)
FILE=$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // empty')
[ -z "$FILE" ] && exit 0
[ "${ALLOW_EXPLORATORY:-0}" = "1" ] && exit 0

# Never gate non-source paths.
case "$FILE" in
  */test/*|*/tests/*|*/__tests__/*|*_test.*|*.test.*|*.spec.*) exit 0 ;;
  */.planning/*|*/Knowledge/*|*/docs/*|*/.claude/*) exit 0 ;;
  *.md|*.json|*.jsonc|*.yml|*.yaml|*.toml|*.ini|*.lock|*.txt|*.env*|*.sql|*.sh) exit 0 ;;
esac

# Only gate recognized application source-code files.
case "$FILE" in
  *.ts|*.tsx|*.js|*.jsx|*.mjs|*.cjs|*.py|*.go|*.cs|*.rs|*.java|*.rb|*.php|*.kt|*.swift|*.c|*.cc|*.cpp|*.h|*.hpp|*.scala|*.ex|*.exs) ;;
  *) exit 0 ;;
esac

if ! find .planning -name 'unit-plan.md' -type f 2>/dev/null | grep -q .; then
  echo "BLOCKED (no-spec-no-code): no .planning/**/unit-plan.md exists. Create an approved per-unit plan with /plan-tree before writing code (Workflow §2.5). For a throwaway spike, set ALLOW_EXPLORATORY=1." >&2
  exit 2
fi
exit 0
