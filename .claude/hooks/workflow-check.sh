#!/bin/bash
# Stateless workflow check -- only reports items that are NOT yet done.
# Reads git/filesystem state each time. No tracking files needed.
# Runs on Stop hook in Claude Code.

WARNINGS=""

# 1. Check if there are uncommitted changes (tracked + new untracked files)
CHANGES=$(git diff --name-only 2>/dev/null; git diff --cached --name-only 2>/dev/null; git ls-files --others --exclude-standard 2>/dev/null)
if [ -z "$CHANGES" ]; then
  exit 0  # Nothing changed, nothing to check -- stay silent
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
  WARNINGS="$WARNINGS\n- Working directly on $BRANCH. Trunk mode still uses a short-lived branch + merge queue -- create one."
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

# 7. Knowledge graph: code changed but Knowledge/ not touched this session
KNOWLEDGE_TOUCHED=$(echo "$CHANGES" | grep -iE '(^|/)Knowledge/')
if [ -n "$SOURCE_CHANGED" ] && [ -z "$KNOWLEDGE_TOUCHED" ]; then
  WARNINGS="$WARNINGS\n- Code changed but Knowledge/ untouched. If any decision/component/question/risk changed, run /kb-capture (Workflow §9.5)."
fi

# 8. Tests changed -- remind about the break-the-code reality check
TESTS_CHANGED=$(echo "$CHANGES" | grep -iE '(\.test\.|\.spec\.|_test\.|/tests?/|/__tests__/)' | head -1)
if [ -n "$TESTS_CHANGED" ]; then
  WARNINGS="$WARNINGS\n- Tests changed. Did you run the break-the-code check (inject a bug, confirm the test FAILS) + mutation? (Workflow §4.5)"
fi

# 9. High-stakes unit touched -> SUGGEST a cross-vendor council (PRINT ONLY; never launches one)
HIGH_STAKES=$(echo "$CHANGES" | grep -iE '(auth|login|session|oauth|token|password|crypto|payment|billing|checkout|stripe|migration|/migrations?/|schema)')
if [ -n "$HIGH_STAKES" ]; then
  WARNINGS="$WARNINGS\n- High-stakes unit (auth/payments/migration) touched. Consider /council for a cross-vendor (Codex, read-only advisor) pass. Note: Codex egresses code to OpenAI."
fi

# 10. Resume contract: code changed but .planning/STATE.md not refreshed this session
STATE_TOUCHED=$(echo "$CHANGES" | grep -iE '(^|/)\.planning/STATE\.md$')
if [ -n "$SOURCE_CHANGED" ] && [ -z "$STATE_TOUCHED" ]; then
  WARNINGS="$WARNINGS\n- Code changed but .planning/STATE.md not updated. Run /checkpoint so a /clear or auto-compact self-heals (the SessionStart hook re-injects STATE.md)."
fi

# 11. Acceptance gate (advisory): runtime-affecting change but no /acceptance PASS report
#     anchored to the current HEAD. Non-blocking Stop reminder -- the BINDING gate is /trunk-merge.
# A change is "runtime-affecting" if it is anything that is NOT clearly docs-only.
RUNTIME_CHANGED=$(echo "$CHANGES" | grep -ivE '(\.(md|mdx|markdown|txt|rst|adoc)$|(^|/)LICENSE([.-]|$)|(^|/)NOTICE([.-]|$)|(^|/)AUTHORS$|(^|/)CODEOWNERS$|\.editorconfig$|\.gitignore$|\.gitattributes$|(^|/)docs/)' | head -1)
if [ -n "$RUNTIME_CHANGED" ]; then
  HEAD_SHA=$(git rev-parse HEAD 2>/dev/null)
  if [ -n "$HEAD_SHA" ]; then
    # FRESHNESS: a single report whose frontmatter has BOTH `verdict: PASS` and a
    # `commit: <full HEAD sha>` line. Same file must satisfy both -- a body that merely
    # mentions HEAD can't pass. Only scan *.md (never binary evidence).
    PASS=""
    if [ -d .planning/acceptance ]; then
      PASS=$(find .planning/acceptance -type f -name '*.md' -exec grep -lE '^verdict:[[:space:]]*PASS' {} + 2>/dev/null \
             | xargs -r grep -lE "^commit:[[:space:]]*$HEAD_SHA\b" 2>/dev/null)
    fi
    if [ -z "$PASS" ]; then
      WARNINGS="$WARNINGS\n- Runtime change but no /acceptance PASS report for HEAD ($HEAD_SHA) -- run /acceptance (exploratory runtime gate) and resolve issues before /trunk-merge."
    fi
  fi
fi

# Output only if there are warnings
if [ -n "$WARNINGS" ]; then
  echo -e "[WORKFLOW CHECK] Issues detected:$WARNINGS"
  echo ""
  echo "Also verify against CLAUDE.md: error handling, security, documentation, and memory updates for any applicable rules."
fi
