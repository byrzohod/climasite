# ClimaSite Issue Registry

> **Master tracking document for all issues found during the quality audit.**
> **Wave 1 Audit Completed: All findings consolidated below.**

---

## Statistics Dashboard

| Metric | Count |
|--------|-------|
| **Total Issues** | 129 |
| **Open** | 129 |
| **In Progress** | 0 |
| **Fixed** | 0 |
| **Won't Fix** | 0 |

### By Severity

| Severity | Open | Fixed | Total |
|----------|------|-------|-------|
| CRITICAL | 9 | 0 | 9 |
| HIGH | 31 | 0 | 31 |
| MEDIUM | 61 | 0 | 61 |
| LOW | 28 | 0 | 28 |

### By Category

| Category | Open | Fixed | Total |
|----------|------|-------|-------|
| UI | 23 | 0 | 23 |
| UX | 28 | 0 | 28 |
| A11Y | 18 | 0 | 18 |
| i18n | 32 | 0 | 32 |
| API | 27 | 0 | 27 |
| Security | 6 | 0 | 6 |
| Performance | 8 | 0 | 8 |
| Code | 5 | 0 | 5 |

### By Location

| Location | Open | Fixed | Total |
|----------|------|-------|-------|
| Home | 18 | 0 | 18 |
| Products | 18 | 0 | 18 |
| Cart | 7 | 0 | 7 |
| Checkout | 11 | 0 | 11 |
| Auth | 18 | 0 | 18 |
| i18n Files | 20 | 0 | 20 |
| API/Backend | 27 | 0 | 27 |
| Global | 10 | 0 | 10 |

---

## CRITICAL Issues (Fix First!)

| ID | Title | Category | Location |
|----|-------|----------|----------|
| HOME-003 | Hardcoded Bulgarian testimonials not i18n | i18n | Home |
| PROD-004 | Hardcoded 'white' color keyword breaks dark mode | UI | Products |
| PROD-010 | Price display swap - sale/original swapped | UX/Bug | Product Detail |
| CHK-010 | Currency hardcoded to BGN | UX | Checkout |
| CHK-011 | Checkout state not reset after order | UX | Checkout |
| API-002 | PaymentsController missing authorization | Security | API |
| API-015 | Race condition in order number generation | Data | API |
| API-016 | Stock reduction without transaction scope | Data | API |
| AUTH-018 | Missing ARIA labels on auth interactive elements | A11Y | Auth |

---

## HIGH Priority Issues

| ID | Title | Category | Location |
|----|-------|----------|----------|
| HOME-001 | Hardcoded hex colors as fallbacks | UI | Home |
| HOME-006 | Category panel images missing alt text | A11Y | Home |
| HOME-015 | Newsletter form lacks input label | A11Y | Home |
| HOME-017 | Testimonials missing carousel ARIA roles | A11Y | Home |
| HOME-018 | Footer newsletter missing aria-label | A11Y | Footer |
| PROD-001 | Hardcoded star rating color | UI | Product Detail |
| PROD-002 | Hardcoded star rating in product card | UI | Product Card |
| PROD-008 | Missing aria-labels on filter buttons | A11Y | Product List |
| PROD-009 | Missing error feedback in product list | UX | Product List |
| PROD-016 | Missing keyboard navigation for quantity | A11Y | Product Detail |
| PROD-018 | HTML entities used for icons | UI/A11Y | Product List |
| CART-004 | No remove item confirmation dialog | UX | Cart |
| CART-006 | Missing error display in cart | UX | Cart |
| CHK-003 | Missing form validation error messages | UX | Checkout |
| CHK-004 | No loading overlay during order placement | UX | Checkout |
| CHK-006 | Country list is hardcoded (only 5 countries) | UX | Checkout |
| AUTH-001 | Missing --color-success-bg CSS variable | UI | Global |
| AUTH-003 | Missing autocomplete on login form | Security/UX | Login |
| AUTH-004 | Missing autocomplete on register form | Security/UX | Register |
| AUTH-015 | No session timeout handling | Security/UX | Auth Service |
| AUTH-017 | Token storage in localStorage (XSS risk) | Security | Auth Service |
| I18N-016 | Inconsistent key casing between languages | i18n | Translation Files |
| API-003 | CreateOrder missing user context validation | Security | API |
| API-004 | GetProductBySlug returns 200 for non-existent | API | API |
| API-005 | GetCategoryBySlug missing NotFound handling | API | API |
| API-011 | TestController available in non-dev | Security | API |
| API-013 | Hardcoded currency and tax rate | UX | API |
| API-018 | EmailService not sending emails | UX | API |
| API-026 | Cart tax hardcoded to 20% VAT | UX | API |

---

## Issue Index (All Issues)

### Home Page (18 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| HOME-001 | Hardcoded hex colors as fallbacks | HIGH | UI |
| HOME-002 | Hardcoded RGBA for shadows/overlays | MEDIUM | UI |
| HOME-003 | Hardcoded Bulgarian testimonials | CRITICAL | i18n |
| HOME-004 | Newsletter button missing aria-label | MEDIUM | A11Y |
| HOME-005 | CTA button missing aria-label | MEDIUM | A11Y |
| HOME-006 | Category images missing alt text | HIGH | A11Y |
| HOME-007 | Testimonial dots missing focus indicators | MEDIUM | A11Y |
| HOME-008 | Missing data-testid on brands section | LOW | UX |
| HOME-009 | Missing data-testid on values section | LOW | UX |
| HOME-010 | Missing data-testid on process section | LOW | UX |
| HOME-011 | Missing data-testid on stats/testimonials | LOW | UX |
| HOME-012 | Header dark mode uses hardcoded rgba | MEDIUM | UI |
| HOME-013 | Mobile menu overlay hardcoded color | MEDIUM | UI |
| HOME-014 | Star rating hardcoded color in product card | MEDIUM | UI |
| HOME-015 | Newsletter form lacks input label | HIGH | A11Y |
| HOME-016 | Scroll indicator needs aria-hidden | MEDIUM | A11Y |
| HOME-017 | Testimonials missing carousel ARIA roles | HIGH | A11Y |
| HOME-018 | Footer newsletter missing aria-label | HIGH | A11Y |

### Product Pages (18 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| PROD-001 | Hardcoded star rating color | HIGH | UI |
| PROD-002 | Hardcoded star rating in product card | HIGH | UI |
| PROD-003 | Hardcoded gradient color fallbacks | MEDIUM | UI |
| PROD-004 | Hardcoded 'white' color keyword | CRITICAL | UI |
| PROD-005 | Missing i18n for view mode button titles | MEDIUM | i18n |
| PROD-006 | Missing i18n for price input placeholders | MEDIUM | i18n |
| PROD-007 | Missing data-testid on filter elements | MEDIUM | Testing |
| PROD-008 | Missing aria-labels on filter buttons | HIGH | A11Y |
| PROD-009 | Missing error feedback in product list | HIGH | UX |
| PROD-010 | Price display swap in product detail | CRITICAL | Bug |
| PROD-011 | Console.error in production code | LOW | Code |
| PROD-012 | Missing loading skeleton for filters | LOW | UX |
| PROD-013 | No error handling for filter options | MEDIUM | UX |
| PROD-014 | Missing data-testid on product detail tabs | MEDIUM | Testing |
| PROD-015 | Inconsistent disabled state styling | LOW | UI |
| PROD-016 | Missing keyboard navigation for quantity | HIGH | A11Y |
| PROD-017 | Image lazy loading without dimensions | MEDIUM | UX |
| PROD-018 | HTML entities used for icons | HIGH | UI/A11Y |

### Cart & Checkout (18 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| CART-001 | Hardcoded fallback colors in cart item | MEDIUM | UI |
| CART-002 | Hardcoded error background fallback | MEDIUM | UI |
| CART-003 | Hardcoded primary color fallback | MEDIUM | UI |
| CART-004 | No remove item confirmation | HIGH | UX |
| CART-005 | No quantity update feedback | MEDIUM | UX |
| CART-006 | Missing error display | HIGH | UX |
| CART-007 | Cart quantity input lacks data-testid | LOW | Testing |
| CHK-001 | Hardcoded success color fallback | MEDIUM | UI |
| CHK-002 | Hardcoded confirmation icon color | MEDIUM | UI |
| CHK-003 | Missing form validation error messages | HIGH | UX |
| CHK-004 | No loading overlay during order | HIGH | UX |
| CHK-005 | Hardcoded shipping prices | MEDIUM | UX |
| CHK-006 | Country list hardcoded (5 only) | HIGH | UX |
| CHK-007 | Email not pre-filled for auth users | MEDIUM | UX |
| CHK-008 | Stripe error handling incomplete | LOW | UX |
| CHK-009 | Missing billing address option | LOW | UX |
| CHK-010 | Currency hardcoded to BGN | CRITICAL | UX |
| CHK-011 | Checkout state not reset after order | CRITICAL | UX |

### Auth & Account (18 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| AUTH-001 | Missing --color-success-bg CSS variable | HIGH | UI |
| AUTH-002 | Hardcoded fallback colors in profile | MEDIUM | UI |
| AUTH-003 | Missing autocomplete on login form | HIGH | Security/UX |
| AUTH-004 | Missing autocomplete on register form | HIGH | Security/UX |
| AUTH-005 | Login password minLength mismatch | MEDIUM | UX |
| AUTH-006 | Hardcoded error messages not translated | MEDIUM | i18n |
| AUTH-007 | Missing password requirements display | MEDIUM | UX |
| AUTH-008 | Hardcoded success message in register | MEDIUM | i18n |
| AUTH-009 | Hardcoded success in forgot password | MEDIUM | i18n |
| AUTH-010 | Hardcoded success in reset password | MEDIUM | i18n |
| AUTH-011 | Hardcoded status colors in orders | LOW | UI |
| AUTH-012 | Missing data-testid on some form elements | LOW | Testing |
| AUTH-013 | rgba() usage for shadow colors | LOW | UI |
| AUTH-014 | Missing phone field validation | LOW | UX |
| AUTH-015 | No session timeout handling | HIGH | Security/UX |
| AUTH-016 | Preferences save has no feedback | MEDIUM | UX |
| AUTH-017 | Token storage in localStorage | HIGH | Security |
| AUTH-018 | Missing ARIA labels on interactive elements | CRITICAL | A11Y |

### i18n Issues (20 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| I18N-001 | Filter placeholders hardcoded | MEDIUM | i18n |
| I18N-002 | View button titles hardcoded | MEDIUM | i18n |
| I18N-003 | Admin dashboard labels hardcoded | MEDIUM | i18n |
| I18N-004 | Checkout country options hardcoded | MEDIUM | i18n |
| I18N-005 | Installation country options hardcoded | MEDIUM | i18n |
| I18N-006 | Admin moderation "Official" hardcoded | MEDIUM | i18n |
| I18N-008 | Order details "Notes" hardcoded | MEDIUM | i18n |
| I18N-009 | Contact map title hardcoded | MEDIUM | i18n |
| I18N-010 | Related products manager tooltips | MEDIUM | i18n |
| I18N-011 | Header aria labels hardcoded | MEDIUM | i18n |
| I18N-012 | Footer social aria labels hardcoded | MEDIUM | i18n |
| I18N-013 | Product gallery aria labels hardcoded | MEDIUM | i18n |
| I18N-014 | Carousel aria labels hardcoded | MEDIUM | i18n |
| I18N-015 | Category breadcrumb aria label hardcoded | MEDIUM | i18n |
| I18N-016 | Inconsistent key casing (DE vs EN/BG) | HIGH | i18n |
| I18N-017 | Missing footer keys in German | MEDIUM | i18n |
| I18N-018 | Missing home.trust.title in EN/BG | LOW | i18n |
| I18N-019 | Currency options hardcoded | LOW | i18n |
| I18N-020 | Time slot labels hardcoded | MEDIUM | i18n |
| I18N-KEYS | Missing translation keys used in code | MEDIUM | i18n |

### API/Backend Issues (27 issues)

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| API-001 | Missing ProducesResponseType attributes | MEDIUM | API Design |
| API-002 | PaymentsController missing authorization | CRITICAL | Security |
| API-003 | CreateOrder missing user context | HIGH | Security |
| API-004 | GetProductBySlug returns 200 for missing | HIGH | API |
| API-005 | GetCategoryBySlug missing NotFound | HIGH | API |
| API-006 | ReviewsController has TODO stubs | MEDIUM | Code |
| API-007 | AskQuestion endpoint missing auth | MEDIUM | Security |
| API-008 | AdminCategories Reorder N+1 | MEDIUM | Performance |
| API-009 | AdminReviews BulkApprove N+1 | MEDIUM | Performance |
| API-010 | AdminQuestions bulk endpoints N+1 | MEDIUM | Performance |
| API-011 | TestController in non-dev environments | HIGH | Security |
| API-012 | Inconsistent error response format | MEDIUM | API Design |
| API-013 | Hardcoded currency and tax rate | HIGH | UX |
| API-014 | Hardcoded shipping costs | MEDIUM | UX |
| API-015 | Race condition in order number gen | CRITICAL | Data |
| API-016 | Stock reduction without transaction | CRITICAL | Data |
| API-017 | Missing logging in most handlers | LOW | Observability |
| API-018 | EmailService not sending emails | HIGH | UX |
| API-019 | BulkAdjustStock catches generic Exception | LOW | Code |
| API-020 | AddToCart missing guest cart expiration | LOW | Data |
| API-021 | CreateReview queries orders twice | LOW | Performance |
| API-022 | AdminProducts duplicates slug logic | LOW | Code |
| API-023 | AddressesController mutates command | MEDIUM | Design |
| API-024 | LoginCommand missing IpAddress property | MEDIUM | API Design |
| API-025 | GetProducts potential N+1 for reviews | MEDIUM | Performance |
| API-026 | Cart tax hardcoded to 20% VAT | HIGH | UX |
| API-027 | InstallationController wrong CreatedAt | LOW | API Design |

---

## Recommended Fix Order

### Phase 1: CRITICAL Fixes (Day 1)
1. **PROD-010** - Price display swap (affects all users seeing wrong prices)
2. **API-002** - PaymentsController authorization (security hole)
3. **API-015** - Order number race condition (data integrity)
4. **API-016** - Stock transaction scope (data integrity)
5. **CHK-010** - Currency hardcoded to BGN (blocks international sales)
6. **CHK-011** - Checkout state not reset (corrupts user experience)
7. **HOME-003** - Hardcoded Bulgarian testimonials (blocks i18n)
8. **PROD-004** - Hardcoded 'white' (breaks dark mode)
9. **AUTH-018** - Missing ARIA labels (accessibility violation)

### Phase 2: HIGH Priority (Days 2-3)
- All HIGH issues from the table above
- Focus on UX-blocking issues first (error handling, feedback)
- Then security issues (autocomplete, session timeout)
- Then accessibility issues (alt text, aria labels)

### Phase 3: MEDIUM Priority (Days 4-5)
- Hardcoded colors and fallbacks
- i18n hardcoded text
- API design improvements
- Performance optimizations

### Phase 4: LOW Priority (Day 6+)
- Missing data-testid attributes
- Code quality improvements
- Minor UI polish

---

## How to Use This Registry

### Fixing Issues

1. Pick an issue from the appropriate phase
2. Update its status to "In Progress"
3. Make the fix and test it
4. Update status to "Fixed" with commit hash
5. Update the statistics at the top

### Issue Status Values
- **Open**: Not yet started
- **In Progress**: Currently being fixed
- **Fixed**: Fix committed, awaiting verification
- **Verified**: Fix confirmed working
- **Won't Fix**: Decided not to fix (with reason)

---

## Changelog

| Date | Action | Issues |
|------|--------|--------|
| 2026-01-22 | Wave 1 Audit Complete | 129 issues logged |

