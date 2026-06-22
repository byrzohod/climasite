#!/bin/bash
# Test-design coverage lint (PROC-01, Wave 3). For every feature's test-design.md, each Scenario row
# marked `automated` must name a test file that EXISTS in the repo. Manual rows are skipped.
#
# Table shape (docs/features/_template/test-design.md):
#   | Scenario ID | Human scenario | Mode | Level | Test name / location | Status |
# The "Test name / location" cell may be `path::Method`, a markdown `code` span, or a placeholder.
#
# Exit non-zero if any automated scenario points at a missing test file.
set -uo pipefail
fail=0
found_any=0

echo "== test-design coverage lint =="
shopt -s nullglob
for td in docs/features/*/test-design.md; do
  case "$td" in *"/_template/"*) continue ;; esac
  found_any=1
  feat="$(basename "$(dirname "$td")")"
  # Read table rows: lines starting with '|' that contain an 'automated' cell.
  while IFS= read -r line; do
    # Normalize: split on '|'
    mode="$(printf '%s' "$line" | awk -F'|' '{print $4}' | tr -d ' ')"
    [ "$mode" = "automated" ] || continue
    loc="$(printf '%s' "$line" | awk -F'|' '{print $6}')"
    # Strip backticks, spaces, and a trailing ::Method selector.
    path="$(printf '%s' "$loc" | tr -d '`' | sed 's/::.*$//' | sed 's/^[[:space:]]*//; s/[[:space:]]*$//')"
    # Skip empty or placeholder paths (templated rows not yet filled).
    case "$path" in
      ""|*"<"*">"*|"—"|"-") continue ;;
    esac
    if [ ! -e "$path" ]; then
      echo "  ✗ [$feat] automated scenario references a missing test: '$path'"
      fail=1
    fi
  done < "$td"
done

if [ "$found_any" -eq 0 ]; then
  echo "  • no feature test-design.md files yet (only the template) — nothing to check"
fi
if [ "$fail" -eq 0 ]; then echo "  ✓ test-design coverage lint passed"; fi
exit "$fail"
