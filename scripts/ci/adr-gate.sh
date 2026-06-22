#!/bin/bash
# ADR gate (PROC-01, Wave 3). Two checks against docs/adr/:
#   1. Filename format — every ADR matches NNN(N)-kebab.md (4-digit going forward; legacy 3-digit allowed).
#   2. No in-place edits to an existing (already-merged) ADR — decisions are immutable; write a
#      superseding ADR instead. The one legitimate edit (adding a "Superseded by …" banner) is allowed
#      via a [adr-ok] token in the latest commit message.
#
# Exit non-zero on violation (this is a hard CI gate).
set -uo pipefail
ADR_DIR="docs/adr"
fail=0

echo "== ADR gate =="

# --- Check 1: filename format ---
shopt -s nullglob
for f in "$ADR_DIR"/*.md; do
  base="$(basename "$f")"
  case "$base" in
    README.md|000-template.md) continue ;;
  esac
  if ! printf '%s' "$base" | grep -qE '^[0-9]{3,4}-[a-z0-9]+(-[a-z0-9]+)*\.md$'; then
    echo "  ✗ bad ADR filename: $base (expected NNNN-kebab-case-title.md)"
    fail=1
  fi
done

# --- Check 2: no in-place edits to existing ADRs ---
BASE_REF="${GITHUB_BASE_REF:-main}"
if git rev-parse --verify -q "origin/$BASE_REF" >/dev/null 2>&1; then
  range="origin/$BASE_REF...HEAD"
elif git rev-parse --verify -q "$BASE_REF" >/dev/null 2>&1; then
  range="$BASE_REF...HEAD"
else
  range=""
fi

if [ -n "$range" ]; then
  if git log -1 --format=%B 2>/dev/null | grep -q '\[adr-ok\]'; then
    echo "  • [adr-ok] in commit message — skipping the immutable-ADR check"
  else
    modified="$(git diff --name-only --diff-filter=M "$range" -- "$ADR_DIR" 2>/dev/null \
      | grep -E "$ADR_DIR/[0-9]{3,4}-.*\.md$" || true)"
    if [ -n "$modified" ]; then
      echo "  ✗ existing ADR(s) modified in place — decisions are immutable; add a superseding ADR instead:"
      printf '      %s\n' $modified
      echo "      (if this is only a 'Superseded by …' banner, re-commit with [adr-ok] in the message.)"
      fail=1
    fi
  fi
else
  echo "  • no base ref to diff against — skipping the immutable-ADR check"
fi

if [ "$fail" -eq 0 ]; then echo "  ✓ ADR gate passed"; fi
exit "$fail"
