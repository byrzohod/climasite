---
name: security-review
description: OWASP-driven security review for any feature, phase, or full project. Use at the end of every feature, before any release, and any time the user asks to security-review code or configuration.
---

# /security-review

Implements section 7 of the Agent Workflow.

## When to invoke

- End of every feature or phase
- Before every release / production deploy
- After any change to authentication, authorization, payments, or input handling
- When the user says "security review" or `/security-review`

## Procedure

1. Identify the scope (full repo, recent diff, specific module).
2. Walk through the **OWASP Top 10 checklist** below for the scope.
3. Run dependency audits: `dotnet list package --vulnerable --include-transitive` and `cd src/ClimaSite.Web && npm audit`.
4. Grep for common red flags: `grep -rE "(api[_-]?key|secret|password|token|private[_-]?key)\s*=" --include="*.cs" --include="*.ts"`.
5. Verify the **Security Review Checklist** at the bottom.
6. Report findings with severity and suggested fixes. Fix critical/high before declaring done.

## OWASP Top 10 Checklist

1. **Broken Access Control** — every endpoint authorizes correctly. Users can only see their own data. Test for IDOR (manipulating IDs in URLs).
2. **Cryptographic Failures** — sensitive data encrypted at rest and in transit. Passwords hashed with Argon2 (climasite already does this). No secrets in code.
3. **Injection** — all DB queries parameterized (EF Core handles this when used correctly — watch for raw SQL). User input never concatenated into queries, commands, or templates.
4. **Insecure Design** — threat-modeled at planning time. Defense in depth. Rate limits on sensitive operations.
5. **Security Misconfiguration** — no default credentials. CORS, CSP, HSTS, X-Frame-Options headers set. Dependencies up to date.
6. **Vulnerable Components** — `dotnet list package --vulnerable` and `npm audit` clean. No abandoned packages. Unused deps removed.
7. **Authentication Failures** — proper session/JWT handling. Refresh token rotation. Rate-limited login attempts. Secure password reset (token expiry, one-time use).
8. **Data Integrity Failures** — input validated server-side. CSRF tokens on state-changing operations. Webhook signatures verified (Stripe).
9. **Logging & Monitoring Failures** — security events logged (login, failures, privilege changes). Sensitive data NEVER logged (passwords, tokens, full PII). Alerts configured for anomalies.
10. **SSRF** — outbound URLs validated. No fetching arbitrary user-supplied URLs. Allowlists for external service calls.

## Security Review Checklist

- [ ] No secrets / credentials in committed code (run `git log -p | grep -iE "(secret|password|api_key|token)"`)
- [ ] All user inputs validated server-side (FluentValidation on every command)
- [ ] Authorization checks on every protected endpoint (`[Authorize]`, role checks)
- [ ] SQL/NoSQL injection protection (parameterized queries, no string concat into SQL)
- [ ] XSS prevention (Angular escapes by default — watch `[innerHTML]`, `bypassSecurityTrust*`)
- [ ] CSRF protection on state-changing operations
- [ ] Rate limiting on auth + sensitive endpoints
- [ ] Error responses don't leak stack traces or internal details in production
- [ ] Dependencies audited (`dotnet list package --vulnerable`, `npm audit`)
- [ ] HTTPS enforced; security headers set (HSTS, CSP, X-Frame-Options, X-Content-Type-Options)
- [ ] File uploads: type (magic bytes), size, content scanning
- [ ] API responses don't expose unnecessary fields (use DTOs, not entities directly)
- [ ] Logs capture security events without logging passwords / tokens / full credit cards
- [ ] Stripe webhook signatures verified
- [ ] JWT secret length ≥ 32 chars and stored in env var, not config

## climasite-specific notes

- Argon2 for password hashing — already in place
- JWT with refresh tokens — verify rotation on every refresh
- Stripe integration — never log card data; verify webhook signatures
- GDPR — data export and deletion endpoints required for EU users
