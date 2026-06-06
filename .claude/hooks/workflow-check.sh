#!/bin/bash
# Stateless workflow check -- only reports items that are NOT yet done.
# Reads git/filesystem state each time. No tracking files needed.
# Runs on Stop hook in Claude Code.

WARNINGS=""

# 1. Check if there are uncommitted changes (tracked files only)
CHANGES=$(git diff --name-only 2>/dev/null; git diff --cached --name-only 2>/dev/null)
if [ -z "$CHANGES" ]; then
  exit 0  # No tracked changes, nothing to check -- stay silent
fi

# 2. Check for secrets in staged changes
SECRETS=$(git diff --cached --diff-filter=ACM 2>/dev/null | grep -iE '(api[_-]?key|secret|password|token|credential|private[_-]?key)\s*[:=]' | head -5)
if [ -n "$SECRETS" ]; then
  WARNINGS="$WARNINGS\n- SECRETS DETECTED in staged changes. Review before committing."
fi

# 3. Check for sensitive file modifications
SENSITIVE=$(echo "$CHANGES" | grep -iE '\.(env|pem|key|p12|pfx)$|credentials|secrets\.(yml|yaml|json)')
if [ -n "$SENSITIVE" ]; then
  WARNINGS="$WARNINGS\n- SENSITIVE FILES modified: $SENSITIVE. Ensure these are in .gitignore."
fi

# 4. Check if on main/master branch
BRANCH=$(git branch --show-current 2>/dev/null)
if [ "$BRANCH" = "main" ] || [ "$BRANCH" = "master" ]; then
  WARNINGS="$WARNINGS\n- Working directly on $BRANCH. Create a feature branch."
fi

# 5. Check if SOURCE CODE files were changed (not just docs/config) and suggest tests
SOURCE_CHANGED=$(echo "$CHANGES" | grep -iE '\.(ts|tsx|js|jsx|cs|py|go|rs|java|rb|swift|kt|vue|svelte)$' | head -1)
if [ -n "$SOURCE_CHANGED" ]; then
  WARNINGS="$WARNINGS\n- Source code changed. Have tests been run? Have new tests been written for new functionality?"
fi

# 6. Remind about code review if many source files changed
SOURCE_COUNT=$(echo "$CHANGES" | grep -ciE '\.(ts|tsx|js|jsx|cs|py|go|rs|java|rb|swift|kt|vue|svelte)$')
if [ "$SOURCE_COUNT" -gt 3 ] 2>/dev/null; then
  WARNINGS="$WARNINGS\n- $SOURCE_COUNT source files changed. Consider running /code-review before PR."
fi

# Output only if there are warnings
if [ -n "$WARNINGS" ]; then
  echo -e "[WORKFLOW CHECK] Issues detected:$WARNINGS"
  echo ""
  echo "Also verify against CLAUDE.md: error handling, security, documentation, and memory updates for any applicable rules."
fi
