---
name: security-review
description: Full OWASP Top 10 security audit + dependency vulnerability scan + secrets check + data privacy review + auth flow audit. Use this whenever the user mentions security, OWASP, vulnerabilities, CVE, injection, XSS, CSRF, SQL injection, authentication, authorization, secrets, API keys, dependency audit, supply chain, threat model, before merging any auth/payment/user-data feature, before every release, or after touching session/token/password/login/oauth flows.
---

# /security-review - Comprehensive security audit

## When to use

Invoke this skill:
- **After completing every feature** -- before PR merge
- **Before every release** -- full security pass
- **After dependency changes** -- verify no new vulnerabilities
- **Periodically** -- even if no changes (new CVEs emerge)

Security must be considered at every step, not as an afterthought.

## OWASP Top 10 Checklist

Review every feature against the OWASP Top 10:

### 1. Broken Access Control
- [ ] Verify authorization on every endpoint
- [ ] Check that users can only access their own data
- [ ] Test for IDOR (Insecure Direct Object References)
- [ ] Role-based access control correctly applied
- [ ] No client-side-only authorization checks

### 2. Cryptographic Failures
- [ ] Use strong encryption for sensitive data at rest and in transit
- [ ] Never store passwords in plaintext
- [ ] Use bcrypt, argon2, or scrypt for password hashing
- [ ] TLS/HTTPS for all network communication
- [ ] No weak ciphers or protocols

### 3. Injection
- [ ] Parameterize all database queries (no string concatenation)
- [ ] Sanitize user inputs
- [ ] Never concatenate user input into queries, commands, or templates
- [ ] Use prepared statements for SQL
- [ ] Escape shell arguments properly
- [ ] Template engines have auto-escaping enabled

### 4. Insecure Design
- [ ] Threat modeling done during planning, not after implementation
- [ ] Defense-in-depth applied (multiple layers of security)
- [ ] Rate limit sensitive operations
- [ ] Fail securely (default deny, not default allow)

### 5. Security Misconfiguration
- [ ] Remove default credentials
- [ ] Disable unnecessary features and ports
- [ ] Set proper CORS headers
- [ ] Set Content Security Policy (CSP) headers
- [ ] Set other security headers: HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- [ ] Keep dependencies updated
- [ ] No debug mode in production
- [ ] No verbose error messages exposed to users

### 6. Vulnerable and Outdated Components
- [ ] Audit dependencies regularly (`npm audit`, `pip audit`, `cargo audit`, etc.)
- [ ] Pin dependency versions in lock files
- [ ] Remove unused dependencies
- [ ] Monitor security advisories for used packages
- [ ] Renovate/Dependabot configured for automated updates

### 7. Identification and Authentication Failures
- [ ] Implement proper session management
- [ ] Use MFA where appropriate
- [ ] Rate limit login attempts
- [ ] Secure password reset flows (no enumeration, expiring tokens)
- [ ] Strong password requirements without being punitive
- [ ] Session timeout appropriate to sensitivity
- [ ] Secure cookie flags (HttpOnly, Secure, SameSite)

### 8. Software and Data Integrity Failures
- [ ] Verify data integrity (checksums, signatures) for critical data
- [ ] Validate all input on the server side
- [ ] Use CSRF tokens on state-changing operations
- [ ] Validate webhook signatures from external sources
- [ ] Sign build artifacts (SBOM, provenance)

### 9. Security Logging & Monitoring Failures
- [ ] Log security events (logins, failures, access to sensitive data)
- [ ] Never log sensitive data (passwords, tokens, PII, credit cards)
- [ ] Set up alerting for anomalies (repeated auth failures, privilege escalation attempts)
- [ ] Audit trail is tamper-evident
- [ ] Log retention appropriate to compliance requirements

### 10. Server-Side Request Forgery (SSRF)
- [ ] Validate and sanitize all URLs before making requests
- [ ] Restrict outbound requests to allowlisted domains
- [ ] Don't follow redirects blindly
- [ ] Block requests to internal IP ranges (169.254.x, 10.x, 172.16-31.x, 192.168.x, 127.x)

## Dependency Management

- [ ] **Automated dependency updates** configured (e.g., Renovate, Dependabot)
- [ ] **Lock files committed** (`package-lock.json`, `yarn.lock`, `Cargo.lock`, etc.)
- [ ] **Dependency audit in CI** as a required check (`npm audit`, `pip audit`, equivalent)
- [ ] **Major version upgrades** reviewed and tested -- not auto-merged
- [ ] **Unused dependencies removed** -- every dependency is attack surface
- [ ] **License compliance** -- aware of dependency licenses (especially GPL in proprietary projects)

## Data Privacy (when handling personal data)

- [ ] **Data minimization** -- only collect what's actually needed
- [ ] **Data inventory** -- know what personal data is stored, where, and why
- [ ] **User data rights** -- data export and deletion capabilities built in
- [ ] **Consent management** -- clear, unambiguous consent for data collection
- [ ] **Breach notification procedure** documented (72-hour window for GDPR)
- [ ] **Privacy by design** -- privacy implications considered during planning

This section applies when the project handles personal data. Skip if not applicable.

## Secrets Management

- [ ] All secrets in `.env` files, never in code or git
- [ ] `.env.example` with dummy values committed as documentation
- [ ] Different secrets for dev/staging/production
- [ ] Rotate compromised secrets immediately
- [ ] Never log secrets, even in debug mode
- [ ] Use a secrets manager for production (AWS Secrets Manager, HashiCorp Vault, etc.)

## Security Review Checklist (feature-level)

At the end of every feature or phase, verify:

- [ ] No secrets or credentials in committed code
- [ ] All user inputs validated and sanitized (server-side)
- [ ] Authentication and authorization checks on all protected routes
- [ ] SQL/NoSQL injection protection (parameterized queries)
- [ ] XSS prevention (output encoding, CSP headers)
- [ ] CSRF protection on state-changing operations
- [ ] Rate limiting on authentication and sensitive endpoints
- [ ] Proper error handling that doesn't leak internal details
- [ ] Dependencies audited for known vulnerabilities
- [ ] HTTPS enforced, security headers set
- [ ] File upload validation (type, size, content scanning)
- [ ] API responses don't expose unnecessary data
- [ ] Logging captures security events without logging sensitive data

## How to run this skill

1. Read the current state of the code and dependencies
2. Go through each checklist item systematically
3. For each item, mark: ✓ verified, ✗ failed, or N/A
4. For every failure, propose and apply a fix
5. Re-verify all failed items
6. Generate a summary report with:
   - Items verified
   - Items that failed and were fixed
   - Items that are N/A with reason
   - Remaining items that need manual review

## Complementary tools

- **Dependency scanning**: `npm audit`, `pip audit`, `cargo audit`, Snyk, Trivy
- **Static analysis**: Semgrep, SonarQube, CodeQL
- **Secret scanning**: GitLeaks, TruffleHog (run in pre-commit and CI)
- **Container scanning**: Trivy, Grype, Snyk Container
- **SBOM generation**: Syft, CycloneDX tools
