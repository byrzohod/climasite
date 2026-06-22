#!/bin/bash
# PR-checklist gate (PROC-01 Wave 4) — a lightweight danger.js-equivalent. Fails a PR whose description
# leaves the required template sections empty, or that touches auth/payment/GDPR without mentioning a
# security review. No-ops on non-PR events (push) so it can be a required check via Test Summary.
#
# Inputs (from CI): PR_BODY env (the PR description), GITHUB_EVENT_NAME, GITHUB_BASE_REF.
set -uo pipefail

if [ "${GITHUB_EVENT_NAME:-}" != "pull_request" ]; then
  echo "Not a pull_request event (${GITHUB_EVENT_NAME:-none}) — PR checklist skipped."
  exit 0
fi

body="${PR_BODY:-}"
fail=0
echo "== PR checklist =="

if [ -z "$(printf '%s' "$body" | tr -d '[:space:]')" ]; then
  echo "::error::PR description is empty — fill in the template (Summary + Testing are required)."
  exit 1
fi

# Extract a '## <name>' section body (up to the next '## ' heading).
section() { printf '%s\n' "$body" | awk -v h="## $1" 'BEGIN{f=0} index($0,h)==1{f=1;next} /^## /{if(f)exit} f{print}'; }
# Strip HTML comments (single- and multi-line) + whitespace to test for real content.
strip() { sed -e 's/<!--[^>]*-->//g' -e '/<!--/,/-->/d' | tr -d '[:space:]'; }

if [ -z "$(section 'Summary' | strip)" ]; then
  echo "::error::The '## Summary' section is empty — describe what changed and why."; fail=1
fi
if [ -z "$(section 'Testing' | strip)" ]; then
  echo "::error::The '## Testing' section is empty — state how the change is verified."; fail=1
fi

# Security-review required when sensitive surfaces change.
BASE_REF="${GITHUB_BASE_REF:-main}"
git fetch origin "$BASE_REF" --quiet 2>/dev/null || true
changed="$(git diff --name-only "origin/${BASE_REF}...HEAD" 2>/dev/null || true)"
if printf '%s' "$changed" | grep -qiE '(Auth|Payments|Webhooks|Gdpr|Stripe)'; then
  if ! printf '%s' "$body" | grep -qi 'security-review'; then
    echo "::error::auth/payment/GDPR/Stripe paths changed but the PR body does not mention /security-review."
    fail=1
  fi
fi

if [ "$fail" -eq 0 ]; then echo "PR checklist OK"; fi
exit "$fail"
