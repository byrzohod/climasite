# ClimaSite - HVAC E-Commerce Platform

## Project Overview

ClimaSite is an online shop specializing in air conditioners, heating systems, cooling equipment, and related HVAC (Heating, Ventilation, and Air Conditioning) products.

## Tech Stack

- **Backend**: ASP.NET Core with .NET 10
- **Frontend**: Angular 19+ (latest LTS)
- **Database**: PostgreSQL 16+
- **ORM**: Entity Framework Core
- **Cache**: Redis
- **API**: RESTful with OpenAPI/Swagger documentation
- **E2E Testing**: Playwright
- **Unit Testing**: xUnit (backend), Jasmine/Karma (frontend)

## Project Structure

```
climasite/
├── src/
│   ├── ClimaSite.Api/           # ASP.NET Core Web API
│   ├── ClimaSite.Application/   # CQRS handlers, DTOs, validators
│   ├── ClimaSite.Core/          # Domain models, interfaces
│   ├── ClimaSite.Infrastructure/ # Data access, external services
│   └── ClimaSite.Web/           # Angular frontend
├── tests/
│   ├── ClimaSite.Api.Tests/     # API integration tests
│   ├── ClimaSite.Core.Tests/    # Domain unit tests
│   ├── ClimaSite.E2E/           # Playwright E2E tests
│   └── ClimaSite.Web.Tests/     # Angular unit tests
├── docs/
│   └── plans/                   # Feature implementation plans
└── scripts/                     # Build and deployment scripts
```

---

## CRITICAL: Testing Requirements

### MANDATORY Testing Policy

**EVERY feature, fix, or change MUST include comprehensive tests. Code without tests is NOT complete.**

### Test Coverage Requirements

| Test Type | Minimum Coverage | Description |
|-----------|-----------------|-------------|
| Unit Tests (Backend) | 80% | All services, handlers, validators, domain logic |
| Unit Tests (Frontend) | 70% | All services, components with logic |
| Integration Tests | All API endpoints | Every endpoint must have integration tests |
| E2E Tests | All user flows | Every feature must have end-to-end tests |

### E2E Testing Rules (CRITICAL)

1. **NO MOCKING** - E2E tests must use REAL data, REAL API calls, REAL database
2. **Self-contained tests** - Each test creates its own test data (users, products, orders)
3. **Database cleanup** - Tests clean up after themselves using correlation IDs
4. **Test isolation** - Tests must not depend on other tests or pre-existing data

### Test Data Factory Pattern

Every E2E test must use the TestDataFactory to create real data:

```typescript
// Example E2E test structure
test('user can complete checkout', async ({ page, request }) => {
  const factory = new TestDataFactory(request);

  // Create REAL data - no mocking
  const user = await factory.createUser();
  const product = await factory.createProduct({ name: 'Test AC Unit', price: 599.99 });

  // Test the actual UI flow
  await loginAs(page, user.email, 'TestPass123!');
  await page.goto(`/products/${product.slug}`);
  await page.click('[data-testid="add-to-cart"]');
  // ... complete the flow

  // Verify the result
  await expect(page).toHaveURL(/\/checkout\/confirmation/);
});
```

### Before Marking Any Task Complete

1. **Run ALL tests** - `dotnet test` and `npm test` and `npx playwright test`
2. **Verify the app runs** - Start the app and manually verify the feature works
3. **Check for console errors** - No JavaScript errors, no API errors
4. **Test edge cases** - Empty states, error states, loading states
5. **Test responsive design** - Mobile, tablet, desktop views

### Quality Checklist (MUST pass before completion)

- [ ] All existing tests still pass
- [ ] New unit tests written for all new backend services/handlers
- [ ] New unit tests written for Angular services with logic
- [ ] Integration tests written for new API endpoints
- [ ] E2E tests written for new user-facing features
- [ ] App starts without errors (`dotnet run` + `ng serve`)
- [ ] Feature works when manually tested in browser
- [ ] No TypeScript/ESLint errors
- [ ] No C# compiler warnings
- [ ] Database migrations run successfully

---

## Development Guidelines

### Backend (ASP.NET Core)

- Follow Clean Architecture principles
- Use CQRS pattern with MediatR for all operations
- Implement repository pattern for data access
- Use FluentValidation for input validation
- Apply proper exception handling with ProblemDetails
- **Write unit tests for every handler and service**

### Frontend (Angular)

- Use standalone components (no NgModules)
- Use Angular Signals for reactive state management
- Implement lazy loading for feature modules
- Follow Angular style guide
- Implement responsive design (mobile-first)
- Use Tailwind CSS for styling
- **All user-facing text must use i18n (ngx-translate)**
- **Write unit tests for services and complex components**

### Theming & Colors

- **ALL colors must be defined in `src/ClimaSite.Web/src/styles/_colors.scss`**
- Use CSS custom properties for theme switching
- Support light and dark themes
- Never hardcode colors in components

### Internationalization (i18n)

- **ALL user-facing text must be translatable**
- Use ngx-translate for runtime language switching
- Translation files: `src/ClimaSite.Web/src/assets/i18n/{lang}.json`
- Supported languages: EN (English), BG (Bulgarian), DE (German)

### Database

- Use EF Core migrations for schema changes
- Follow naming conventions: snake_case for PostgreSQL
- Use JSONB for flexible attributes (product specifications)
- Index frequently queried columns
- Use soft deletes where appropriate

### Code Style

- C#: Follow Microsoft coding conventions
- TypeScript: Use ESLint with Angular recommended rules
- Use meaningful variable and function names
- Document public APIs with XML comments (C#) and JSDoc (TS)

### Git Workflow

- Main branch: production-ready code
- Feature branches: `feature/description`
- Bug fixes: `fix/description`
- Use conventional commits

---

## Commands

```bash
# Backend
dotnet build
dotnet test                                    # Run all backend tests
dotnet run --project src/ClimaSite.Api

# Frontend
cd src/ClimaSite.Web
npm install
ng serve                                       # Start dev server
ng test                                        # Run unit tests
ng test --watch=false --browsers=ChromeHeadless  # CI mode
ng build --configuration=production

# E2E Tests (Playwright)
cd tests/ClimaSite.E2E
npx playwright install                         # Install browsers
npx playwright test                            # Run all E2E tests
npx playwright test --ui                       # Interactive UI mode
npx playwright test auth/                      # Run specific test folder

# Database
dotnet ef migrations add <MigrationName> --project src/ClimaSite.Infrastructure
dotnet ef database update --project src/ClimaSite.Infrastructure

# Full Test Suite (run before any PR)
dotnet test && cd src/ClimaSite.Web && ng test --watch=false && cd ../../tests/ClimaSite.E2E && npx playwright test
```

---

## Environment Variables

Store sensitive configuration in environment variables:

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | PostgreSQL connection string |
| `REDIS_URL` | Redis connection string |
| `JWT_SECRET` | JWT signing secret (min 32 chars) |
| `JWT_ISSUER` | JWT issuer URL |
| `JWT_AUDIENCE` | JWT audience URL |
| `STRIPE_SECRET_KEY` | Stripe API secret key |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret |
| `SMTP_HOST` | Email server host |
| `SMTP_PORT` | Email server port |
| `SMTP_USER` | Email username |
| `SMTP_PASSWORD` | Email password |

---

## Key Business Rules

- Products must have valid pricing and stock information
- Orders require user authentication (guest checkout stores email)
- Inventory updates must be transactional (use stock reservations)
- Price changes should be logged for audit
- Customer data must comply with GDPR
- Reviews require order completion for "verified purchase" badge

---

## Implementation Plans

All feature implementation plans are in `docs/plans/`:

| Plan | Task IDs | Description |
|------|----------|-------------|
| 00-master-overview.md | - | Project overview and phases |
| 01-design-system-theming.md | DST-001 to DST-015 | Colors, themes, components |
| 02-internationalization-i18n.md | I18N-001 to I18N-015 | Multi-language support |
| 03-authentication-user-management.md | AUTH-001 to AUTH-027 | JWT auth, users, roles |
| 04-product-catalog.md | CAT-001 to CAT-030 | Products, categories, variants |
| 05-shopping-cart.md | CART-001 to CART-020 | Cart, guest carts, merging |
| 06-checkout-orders.md | CHK-001 to CHK-035 | Checkout, Stripe, orders |
| 07-admin-panel.md | ADM-001 to ADM-030 | Admin dashboard, CRUD |
| 08-testing-infrastructure.md | TEST-001 to TEST-020 | Test setup, patterns |
| 09-inventory-management.md | INV-001 to INV-020 | Stock, reservations |
| 10-search-navigation.md | SRCH-001 to SRCH-024 | Search, facets, filters |
| 11-reviews-ratings.md | REV-001 to REV-022 | Reviews, ratings, moderation |
| 12-notifications-system.md | NOT-001 to NOT-020 | Email, in-app notifications |
| 13-wishlist.md | WISH-001 to WISH-019 | Wishlist, sharing |

---

## Definition of Done

A feature is ONLY complete when:

1. **Code compiles** - No build errors in backend or frontend
2. **All tests pass** - Unit, integration, AND E2E tests
3. **App runs** - Both backend and frontend start without errors
4. **Feature works** - Manually verified in browser
5. **Tests written** - New tests added for all new functionality
6. **No regressions** - Existing functionality still works
7. **Responsive** - Works on mobile, tablet, and desktop
8. **Accessible** - Keyboard navigation, screen reader support
9. **i18n ready** - All text uses translation keys
10. **Themed** - Uses centralized color variables

**DO NOT mark a task as complete until ALL of these are verified.**
