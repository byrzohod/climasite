# ClimaSite Validation Master Plan

> **Document Version:** 1.0
> **Created:** 2026-01-24
> **Status:** Draft - Area Catalog

---

## Purpose

Create a comprehensive validation plan for the ClimaSite codebase and functional workflows. This document lists every area that requires investigation. Each area will later receive its own subagent-produced validation document covering:
- Code paths and dependencies
- Unit/integration/E2E test coverage
- Manual verification steps
- Risks and gaps
- Required fixes and new tests

---

## Validation Area Catalog (Use This to Spawn Subagents)

### 1. Platform & Infrastructure
- Application startup (API + Web)
- Environment configuration (local/dev/prod)
- Secrets management and environment variables
- Docker/Compose services (Postgres, Redis)
- Health checks and readiness
- Database migrations and seeding
- Background jobs / schedulers (if any)

### 2. Authentication & Authorization
- Registration, login, logout
- Refresh token rotation and expiry
- Password reset and email flows
- Role-based access (Admin, User)
- Session handling and token storage
- Security headers and cookie handling

### 3. User Account & Profile
- Profile view/edit
- Password change
- Order history access
- Saved addresses management
- Preferences (language, theme)

### 4. Product Catalog
- Product CRUD (public read, admin create/update/delete)
- Categories and navigation tree
- Product variants, pricing, and stock
- Specifications (JSONB fields)
- Media/gallery handling
- Out-of-stock behavior

### 5. Search, Filters & Navigation
- Search bar and autocomplete
- Filters (price range, brand, category)
- Sorting and pagination
- Mega menu and category navigation
- Breadcrumb behavior

### 6. Cart & Cart Merging
- Add/remove/update quantity
- Guest cart behavior
- Cart merge on login
- Price calculation and totals
- Stock validation in cart

### 7. Checkout & Payments
- Multi-step checkout flow
- Shipping method selection
- Payment methods (Stripe card, PayPal, bank transfer)
- Payment intent creation and confirmation
- Error handling and retry behavior
- Order confirmation view
- Saved address selection during checkout

### 8. Orders & Fulfillment
- Order creation and persistence
- Order status lifecycle
- Order details view
- Reorder feature
- Inventory reservation/rollback

### 9. Reviews, Ratings & Q&A
- Review creation and display
- Verified purchase logic
- Review moderation flows
- Q&A creation and display

### 10. Wishlist & Favorites
- Add/remove wishlist items
- Wishlist persistence and sync
- Wishlist visibility in account

### 11. Notifications & Email
- Email templates and delivery
- In-app notification flows
- Error handling for email failures

### 12. Admin Panel
- Admin authentication and authorization
- Product, category, and inventory management
- Order management
- User management
- Review moderation
- Translation management UI
- Dashboard metrics

### 13. Internationalization (i18n)
- Translation coverage for all user-facing text
- Language switcher behavior
- Fallback handling for missing keys
- Translation management workflow

### 14. Theming & Design System
- Light/dark theme parity
- CSS variable usage (no hardcoded colors)
- Shared components (Alert, Modal, Toast, Breadcrumb)
- Responsive behavior at 320/768/1024/1280

### 15. Accessibility (WCAG)
- Keyboard navigation
- ARIA roles and labels
- Focus traps and focus order
- Screen reader behaviors
- Color contrast checks

### 16. Performance & UX
- Lazy loading for images and heavy content
- Scroll performance (throttling)
- Loading states and skeletons
- Error/empty states consistency

### 17. API Contracts & Error Handling
- REST endpoint coverage
- OpenAPI/Swagger accuracy
- Error response format consistency
- Status codes and problem details
- Request validation (FluentValidation)

### 18. Security & Compliance
- GDPR considerations and PII handling
- Rate limiting / abuse protection
- SQL injection and XSS protections
- Dependency vulnerability checks

### 19. Data Integrity & Domain Rules
- Stock reservation and rollback
- Pricing change auditing
- Order number generation
- Cart merge logic consistency
- Transaction boundaries

### 20. Testing Infrastructure & Coverage
- Unit test coverage for backend services/handlers
- Frontend unit tests for components/services
- Integration tests for API endpoints
- E2E coverage for user flows
- Flaky test detection and stability

### 21. Observability & Diagnostics
- Logging structure and correlation IDs
- Error reporting behavior
- Performance telemetry (if any)
- Health metrics

### 22. Build, CI/CD & Deployment Readiness
- Build scripts and linting
- Release configurations
- Environment-specific config validation
- Deployment smoke test checklist

---

## Subagent Output Requirements (Template)

Each area subagent must produce a document using this outline:

1. **Scope Summary**
   - Features, routes, API endpoints, and key components
2. **Code Path Map**
   - Files, classes, services, handlers, components
3. **Test Coverage Audit**
   - Unit, integration, E2E tests (list by file/test name)
4. **Manual Verification Steps**
   - Step-by-step verification flow (include data setup)
5. **Gaps & Risks**
   - Missing tests, broken logic, UX/accessibility gaps
6. **Recommended Fixes & Tests**
   - Proposed changes and new tests to add
7. **Evidence & Notes**
   - Logs, screenshots, or expected outputs

---

## Next Action

Use this document to spawn subagents for each area in the catalog. Each subagent will generate a dedicated validation doc that we will use to implement fixes and add missing tests.
