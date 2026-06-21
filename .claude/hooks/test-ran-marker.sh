#!/bin/bash
# PostToolUse(Bash) hook (PROC-01, Wave 2): drop a per-session marker when a test command runs.
# The Stop hook reads this marker to enforce "src changed → tests must have run this session".
# Always non-blocking.

INPUT="$(cat 2>/dev/null)"
CMD="$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null)"
SID="$(printf '%s' "$INPUT" | jq -r '.session_id // "nosession"' 2>/dev/null)"

if printf '%s' "$CMD" | grep -qE '(dotnet test|ng test|npm (run )?test|jest|vitest|playwright|ClimaSite\.NoE2E)'; then
  touch "/tmp/climasite-test-ran-${SID}" 2>/dev/null || true
fi
exit 0
