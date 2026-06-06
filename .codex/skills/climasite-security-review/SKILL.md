---
name: climasite-security-review
description: Use at the end of a ClimaSite feature, before merge/release, or after changes to auth, authorization, payments, input handling, config, or dependencies.
---

# ClimaSite Security Review

Scope the recent diff unless the user asks for a full repo review.

Run or report blockers for:
- `dotnet list package --vulnerable --include-transitive`
- `cd src/ClimaSite.Web && npm audit`
- Secret scans over changed files and config.

Check OWASP risks:
- Broken access control and IDOR on protected resources.
- Cryptographic failures, secrets in code, weak JWT config.
- Injection in raw SQL, commands, templates, and user-provided URLs.
- Security misconfiguration: CORS, HSTS, CSP/security headers, production error detail.
- Vulnerable dependencies and unjustified new packages.
- Auth/session failures, refresh rotation, rate limiting on sensitive paths.
- Stripe webhook signature verification and no payment data logging.
- Angular XSS risks: `[innerHTML]`, sanitizer bypasses, unsafe URLs.

Fix critical/high issues before declaring ready.

