#!/usr/bin/env bash
# PreToolUse(Write|Edit) — dynamic-skill quarantine (Workflow §K.4).
# AI-generated skills must be DRAFTED into .claude/skills/_proposed/ and stay INERT until a
# human promotes them. Blocks creating a NEW top-level .claude/skills/<name>.md.
# Editing an EXISTING skill (maintenance) is allowed. Writing into _proposed/ is allowed.
set -euo pipefail

INPUT=$(cat)
FILE=$(printf '%s' "$INPUT" | jq -r '.tool_input.file_path // empty')
[ -z "$FILE" ] && exit 0

case "$FILE" in
  */.claude/skills/_proposed/*) exit 0 ;;            # the quarantine area itself
  */.claude/skills/*.md)
    if [ ! -e "$FILE" ]; then
      echo "BLOCKED (skill-quarantine): draft NEW skills into .claude/skills/_proposed/ — they stay INERT until a human promotes them. Generated skills may not add MCP servers, network calls, or unscoped Bash. (Workflow §K.4)" >&2
      exit 2
    fi ;;
esac
exit 0
