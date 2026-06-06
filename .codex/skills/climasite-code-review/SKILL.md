---
name: climasite-code-review
description: Use before PR merge, after a feature, after a significant refactor, or whenever the user asks for a code review in ClimaSite.
---

# ClimaSite Code Review

Review the diff first. Findings lead the response, ordered by severity with file and line references.

Check:
- Correctness, edge cases, null handling, validation, and status codes.
- Security: authz/authn, input validation, secrets, XSS, SQL/raw SQL, sensitive logging.
- Performance: N+1 queries, payload size, repeated renders, caching, pagination.
- Error handling: consistent response format, useful messages, no swallowed exceptions.
- Code quality: local patterns, CQRS boundaries, signals over UI RxJS state, no needless abstractions.
- Testing: behavior coverage, Testcontainers for integration, real backend/data in E2E.
- Accessibility and UX: semantic HTML, keyboard support, contrast, focus, `data-testid`.
- Dependencies and docs: justified packages, no vulnerable additions, ADRs for non-obvious decisions.

ClimaSite extras:
- User-facing text must use EN/BG/DE i18n keys.
- Colors must come from CSS variables in `_colors.scss`.
- Angular uses standalone components, `inject()`, signals, and `@if/@for/@switch`.

