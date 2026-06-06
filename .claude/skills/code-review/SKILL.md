---
name: code-review
description: Comprehensive multi-dimensional code review for any diff. Use before opening a PR, after completing a feature, or whenever the user asks to review code. Reviews against 13 dimensions and optionally invokes an external AI reviewer for an independent second opinion.
---

# /code-review

Implements section 6 of the Agent Workflow. Review every diff against the dimensions below, then iterate on fixes.

## When to invoke

- Before every PR merge — no exceptions
- After completing a feature or phase
- After any significant refactor
- When the user says "review this" or `/code-review`

## Procedure

1. **Generate the diff**
   ```bash
   git diff main...HEAD > /tmp/review-diff.txt
   # or for pre-commit:
   git diff --staged > /tmp/review-diff.txt
   ```

2. **Self-review against all 13 dimensions** (table below). For each issue, note: file, line, severity (critical / warning / suggestion), dimension, description, suggested fix.

3. **Run a subagent review** (different from the author) using the Task / Agent tool for an independent perspective. Pass it the diff and the dimensions table.

4. **External AI review (optional but recommended for non-trivial changes)**: invoke Codex CLI in quiet mode with the diff and the same review prompt. This catches blind spots the author model has.
   ```bash
   codex -q "You are a senior code reviewer. Review this diff against these dimensions: correctness, security (OWASP), performance, error handling, code quality (SOLID/DRY/KISS), testing coverage, accessibility, data modeling, API design, observability, dependencies, documentation. For each issue: file, line, severity (critical/warning/suggestion), dimension, description, suggested fix. If clean: respond LGTM.

   $(cat /tmp/review-diff.txt)"
   ```

5. **Fix the issues found** — do not just list them.

6. **Re-review** — regenerate the diff and re-run external review. Repeat until LGTM, user says stop, or max 3 rounds.

7. **Document edge cases / trade-offs** in the code or in an ADR under `docs/adr/`.

8. **Log review summary in the PR description**: rounds run, issues found, fixes applied.

## Review Dimensions

| # | Dimension | What to Check |
|---|-----------|--------------|
| 1 | Correctness | Does it do what it's supposed to? Edge cases? Off-by-one? Null handling? |
| 2 | Security | OWASP Top 10. Input validation. Auth checks. No secrets. Injection risks. |
| 3 | Performance | N+1 queries? Re-renders? Large payloads? Missing pagination? Unindexed queries? |
| 4 | Error handling | No empty catches. Standardized error format. Meaningful messages. Correlation IDs. |
| 5 | Code quality | SOLID, DRY, KISS. No needless abstractions. Clean separation of concerns. Readable names. |
| 6 | AI code smells | Excessive nesting. Copy-paste duplication. Unused imports. Over-engineering. Hardcoded values. |
| 7 | Testing | All test levels present? Tests cover behavior, not implementation? **No mocked DB/backend in E2E.** |
| 8 | Accessibility | Semantic HTML. Keyboard nav. Color contrast. ARIA labels. |
| 9 | Data modeling | Migrations backward-compatible. Schema fits access patterns. No integrity risks. |
| 10 | API design | Consistent naming. Proper status codes. Documented. Versioned if needed. |
| 11 | Observability | Logging for key ops. Metrics for new endpoints. No sensitive data in logs. |
| 12 | Dependencies | New deps justified? No vulnerable / abandoned packages? License compatible? |
| 13 | Documentation | ADR for non-obvious decisions. API docs updated. README still accurate. |

## climasite-specific extras

- All UI changes verified in **both light and dark themes**
- All user-facing text uses **i18n keys present in EN, BG, and DE**
- All interactive elements have **`data-testid` attributes**
- E2E tests use **TestDataFactory** and clean up after themselves
- No hardcoded colors — only `var(--color-*)` from `_colors.scss`

## Exit conditions

- External reviewer returns LGTM, or
- User says stop, or
- Max 3 rounds reached

The goal is not perfect code on first pass — it is catching problems before they reach main.
