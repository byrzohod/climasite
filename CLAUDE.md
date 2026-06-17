# ClimaSite - HVAC E-Commerce Platform

## Project Overview

ClimaSite is a production-grade online shop specializing in air conditioners, heating systems, cooling equipment, and related HVAC (Heating, Ventilation, and Air Conditioning) products. The platform supports multi-language (EN, BG, DE), multi-theme (light/dark), and provides a complete e-commerce experience.

## Current Project Status

| Feature | Status | Notes |
|---------|--------|-------|
| Design System & Theming | Complete | Light/dark themes, CSS variables |
| Internationalization | Complete | EN, BG, DE with ngx-translate |
| Authentication | Complete | JWT with refresh tokens, Argon2 |
| Product Catalog | Complete | Products, categories, variants |
| Shopping Cart | Complete | Guest cart, cart merging |
| Checkout & Orders | Complete | Stripe integration |
| Admin Panel | Complete | CRUD, dashboard |
| Reviews & Ratings | Complete | Q&A, verified purchases |
| Search & Navigation | Complete | Full-text search, facets, filters |
| Inventory Management | Complete | Stock tracking, reservations |
| Notifications System | Partial | Email notifications implemented |
| Wishlist | Complete | Backend DTO sync, public sharing, guest login merge, EN/BG/DE i18n, unit/API/E2E coverage |
| Motion/Animation System | Complete | AnimationService, flying cart, confetti, parallax |
| Performance Optimizations | Complete | Core Web Vitals, lazy loading, preconnect hints |
| Home page (v3 — Configurator-First) | Complete | Plan 18 Phase 1 done; real recommendations endpoint, tests, E2E, visual QA, a11y, and Lighthouse verified |
| Project completion plan (Plan 18) | In progress | Phase 0 and Phase 1 done; Phase 2 Wishlist slice complete; Notifications and Animation Audit 21F remain |

### Recently Completed

| Feature | Status | Description |
|---------|--------|-------------|
| Motion/Animation System | Complete | AnimationService, reduced motion support, GPU-accelerated animations |
| Flying Cart Animation | Complete | Product image flies to cart icon with arc trajectory |
| Confetti Celebration | Complete | Canvas-based confetti on order confirmation |
| Toast Notifications | Complete | Progress bar, hover pause, type-specific icons |
| Route Transitions | Complete | Fade/slide page transitions |
| Product Gallery | Complete | Lightbox, crossfade, zoom, slide animations |
| Performance Audit | Complete | Core Web Vitals optimizations, preconnect hints |
| Circular Dependency Fix | Complete | Auth interceptor refactored to fix circular imports |
| UI Improvement Plan | Complete | 45+ issues fixed across 6 phases |
| Shared Components | Complete | Alert, Modal, Toast, Breadcrumb components created |
| Accessibility (WCAG) | Complete | Focus traps, ARIA roles, keyboard navigation, screen reader support |
| Animation Audit (21F) | Partial | Phases 1-2 complete: Removed FloatingDirective, TiltEffectDirective, ParallaxDirective; simplified RevealDirective to fade/fade-up/fade-down only |
| Home v3 Completion | Complete | Configurator-first homepage, real product recommendations, translation coverage, deferred below-fold content, browser QA, and Lighthouse mobile 0.97 / desktop 1.00 |
| Wishlist Completion | Complete | Hydrated wishlist API, public share links, guest-to-login merge, concurrent add protection, translations, and full local test coverage |
| Email Outbox (ARCH-05) | Complete | Durable Postgres `outbox_messages` + `BackgroundService` worker with retry/backoff; `IEmailOutbox`/`OutboxProcessor`; ADR 0001 |
| Transactional Emails (GAP-03) | Complete | Order confirmation (transactional), order-shipped, welcome, password-reset, and admin notify-customer (BUG-16) all delivered via the outbox |
| Admin Panel CRUD (GAP-02) | Complete | Real admin products list/create/edit/deactivate + translation & related-products editors (BUG-14), customers list/detail/status, dashboard KPIs; EN/BG/DE + tests + E2E |

---

## Tech Stack

| Layer | Technology | Version |
|-------|------------|---------|
| **Backend** | ASP.NET Core | .NET 10 |
| **Frontend** | Angular | 19+ (standalone components) |
| **Styling** | Tailwind CSS | 3.4 (custom components; no Material/PrimeNG) |
| **Database** | PostgreSQL | 16+ |
| **ORM** | Entity Framework Core | 10.x |
| **Cache** | Redis | 7.x |
| **API** | RESTful | OpenAPI/Swagger |
| **E2E Testing** | Playwright | Latest |
| **Unit Testing** | xUnit (backend), Jasmine (frontend) | Latest |
| **Payments** | Stripe | stripe-dotnet, @stripe/stripe-js |

---

## Project Structure

```
climasite/
├── src/
│   ├── ClimaSite.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/            # API controllers (REST endpoints)
│   │   │   ├── Admin/              # Admin-only endpoints
│   │   │   └── *.cs                # Public endpoints
│   │   └── Program.cs              # App configuration
│   │
│   ├── ClimaSite.Application/      # Application layer (CQRS)
│   │   ├── Features/               # Feature-based organization
│   │   │   ├── Products/           # Product queries/commands
│   │   │   ├── Orders/             # Order queries/commands
│   │   │   ├── Auth/               # Authentication
│   │   │   └── Admin/              # Admin operations
│   │   ├── Common/                 # Shared DTOs, interfaces
│   │   └── Behaviors/              # MediatR pipeline behaviors
│   │
│   ├── ClimaSite.Core/             # Domain layer
│   │   ├── Entities/               # Domain entities
│   │   ├── ValueObjects/           # Value objects
│   │   ├── Interfaces/             # Repository interfaces
│   │   └── Exceptions/             # Domain exceptions
│   │
│   ├── ClimaSite.Infrastructure/   # Infrastructure layer
│   │   ├── Data/                   # EF Core DbContext, configs
│   │   ├── Repositories/           # Repository implementations
│   │   ├── Services/               # External service clients
│   │   └── Migrations/             # EF Core migrations
│   │
│   └── ClimaSite.Web/              # Angular frontend
│       └── src/
│           ├── app/
│           │   ├── core/           # Singleton services, guards
│           │   │   ├── services/   # API services
│           │   │   ├── models/     # TypeScript interfaces
│           │   │   └── layout/     # Header, footer, layout
│           │   ├── shared/         # Reusable components
│           │   │   └── components/ # Buttons, cards, etc.
│           │   ├── features/       # Feature modules
│           │   │   ├── home/
│           │   │   ├── products/
│           │   │   ├── cart/
│           │   │   ├── checkout/
│           │   │   ├── account/
│           │   │   └── admin/
│           │   └── auth/           # Auth components, guards
│           ├── assets/
│           │   └── i18n/           # Translation JSON files
│           ├── styles/
│           │   ├── _colors.scss    # ONLY source of truth for colors
│           │   └── styles.scss     # Global styles
│           └── environments/       # Environment configs
│
├── tests/
│   ├── ClimaSite.Api.Tests/        # API integration tests
│   ├── ClimaSite.Core.Tests/       # Domain unit tests
│   ├── ClimaSite.E2E/              # Playwright-for-.NET E2E tests (xUnit)
│   │   ├── Infrastructure/         # TestDataFactory, Playwright fixtures
│   │   ├── PageObjects/            # Page object models
│   │   └── Tests/                  # Test files by feature
│   └── (frontend unit tests are colocated under src/ClimaSite.Web as *.spec.ts)
│
├── docs/
│   ├── plans/                      # Implementation plans
│   ├── project-plan/               # Consolidated review, roadmap, backlog (planning hub)
│   └── skills/                     # Skill reference docs
│
└── scripts/                        # Build and deployment scripts
```

---

## API Conventions

### Endpoint Structure

```
/api/auth/*              # Authentication endpoints (public)
/api/products/*          # Product endpoints (public)
/api/categories/*        # Category endpoints (public)
/api/cart/*              # Cart endpoints (public, uses session)
/api/orders/*            # Order endpoints (requires auth)
/api/account/*           # Account endpoints (requires auth)
/api/admin/*             # Admin endpoints (requires Admin role)
```

### Common Response Patterns

```csharp
// Success with data
return Ok(result);

// Created with location header
return CreatedAtAction(nameof(Get), new { id }, result);

// Validation error
return BadRequest(ProblemDetails);

// Not found
return NotFound();

// Paginated response
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 12,
  "totalPages": 9
}
```

### Headers

```
?lang=en|bg|de                      # (query param) selects translated content — the API reads ?lang=, NOT Accept-Language
Authorization: Bearer <token>       # JWT access token
X-Correlation-Id: <guid>           # Request tracking
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
5. **Use `data-testid` attributes** - All interactive elements must have testids

### Test Data Factory Pattern

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
- [ ] Works in BOTH light and dark themes
- [ ] Works in ALL languages (EN, BG, DE)

### MANDATORY: Post-Implementation Workflow

**`main` is branch-protected. NEVER push to `main` directly — direct pushes and force-pushes are rejected. Every change ships on a feature branch via a pull request, and merges only when all six CI checks are green.**

After EVERY change, you MUST:

1. **Branch:** create a `feature/<description>` or `fix/<description>` branch off `main`.

2. **Run the tests locally — per-project, NEVER a bare `dotnet test` at the repo root** (the root solution includes the server-dependent `ClimaSite.E2E` project, which throws hundreds of false failures when run without a live API + frontend):
   ```bash
   dotnet build ClimaSite.sln
   dotnet test tests/ClimaSite.Core.Tests --no-build
   dotnet test tests/ClimaSite.Application.Tests --no-build
   dotnet test tests/ClimaSite.Api.Tests --no-build     # integration; Testcontainers (needs Docker)
   cd src/ClimaSite.Web && ng test --watch=false --browsers=ChromeHeadless
   ```
   The full E2E suite (`dotnet test tests/ClimaSite.E2E`) needs a running API on **:5029** plus `ng serve` on **:4200**; CI runs it for you. See `docs/project-plan/DEV_WORKFLOW.md` for the exact local E2E commands and env vars.

3. **Open a PR to `main`.** CI runs six required checks — **Unit Tests, Integration Tests, Frontend Unit Tests, Build Verification, E2E Tests, Test Summary**. **CI (not a local test run) is the evidence of record.**

4. **Merge only when all six checks are green.** Use conventional-commit messages. When a feature lands or a convention changes, update the status table in this file (and `docs/project-plan/PROJECT_STATUS.md` / `PRIORITIZED_BACKLOG.md`).

**This workflow is NON-NEGOTIABLE. Never push to `main`; never merge on red CI.**

---

## Agent Usage Guidelines (CRITICAL)

### Use Subagents for All Complex Tasks - MANDATORY

**Subagents are the preferred way to handle most development work. They provide significant benefits:**

#### Why Use Subagents

| Benefit | Description |
|---------|-------------|
| **Context Efficiency** | Main conversation stays fresh; each agent gets clean context for its task |
| **Parallel Execution** | Launch 4-6 agents simultaneously for independent tasks |
| **Better Performance** | Reduced token usage = faster responses in main conversation |
| **Error Isolation** | If one agent fails, others continue; easier to retry specific tasks |
| **Focused Work** | Each agent can deeply focus on one specific task without distraction |
| **Scalability** | Can handle 50+ file changes across multiple agents without context overflow |

#### When to Use Subagents (ALWAYS prefer these patterns)

1. **Feature Implementation** - Launch parallel agents for:
   - Backend service/controller
   - Frontend component/service
   - Tests
   - Documentation

2. **Bug Fixes** - Group related fixes:
   - All UI issues → 1 agent
   - All i18n issues → 1 agent
   - All API issues → 1 agent

3. **Code Exploration** - Use `explore` agent for:
   - Finding all usages of a pattern
   - Understanding codebase structure
   - Answering "how does X work?" questions

4. **Multi-file Changes** - Any task touching 5+ files should use subagents

5. **Research Tasks** - Documentation, audits, analysis

#### Subagent Types

| Type | Use For |
|------|---------|
| `general` | Implementation, fixes, writing code, tests |
| `explore` | Finding files, searching code, answering questions about codebase |

#### Example Patterns

```
# GOOD: Parallel implementation (launch all at once)
Task(subagent_type="general", prompt="Implement FlyingCartService...")
Task(subagent_type="general", prompt="Implement ConfettiService...")
Task(subagent_type="general", prompt="Add parallax effects to hero sections...")
Task(subagent_type="general", prompt="Create performance audit document...")

# GOOD: Use explore for research
Task(subagent_type="explore", prompt="Find all components using hardcoded colors...")
Task(subagent_type="explore", prompt="How does the cart merge functionality work?")

# GOOD: Batch fixes by category
Task(subagent_type="general", prompt="Fix all 15 i18n issues in these components: ...")
Task(subagent_type="general", prompt="Add loading='lazy' to all images in: ...")

# BAD: Manually reading 20 files in main conversation
# BAD: Fixing 50 issues one-by-one in main conversation
# BAD: Using grep/glob sequences when explore agent would be faster
```

#### Best Practices

1. **Be Specific** - Give agents clear, detailed prompts with file paths
2. **Include Context** - Tell agent about project structure, conventions, related files
3. **Define Output** - Specify what the agent should return (summary, file list, etc.)
4. **Parallel Launch** - Always launch independent agents in the same message
5. **Don't Over-Split** - Keep related changes in one agent (e.g., component + its styles)

---

## Development Guidelines

### Backend (ASP.NET Core)

- Follow Clean Architecture principles
- Use CQRS pattern with MediatR for all operations
- Implement repository pattern for data access
- Use FluentValidation for input validation
- Apply proper exception handling with ProblemDetails
- **Write unit tests for every handler and service**

```csharp
// Example CQRS command
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price
) : IRequest<Guid>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    // Implementation
}

// Example validator
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

### Frontend (Angular)

- **Standalone components only** - NO NgModules
- **Angular Signals for state** - NOT RxJS BehaviorSubjects
- Implement lazy loading for feature routes
- Use `inject()` function, not constructor injection
- **All user-facing text must use i18n (ngx-translate)**
- Add `data-testid` attributes to all interactive elements

```typescript
// Example Angular component with Signals
@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `...`
})
export class ProductCardComponent {
  private readonly cartService = inject(CartService);

  product = input.required<Product>();      // Signal input
  isLoading = signal(false);                // Local signal

  addToCart(): void {
    this.isLoading.set(true);
    this.cartService.addItem(this.product()).subscribe({
      next: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false)
    });
  }
}
```

### Theming & Colors (CRITICAL)

- **ALL colors must be defined in `src/ClimaSite.Web/src/styles/_colors.scss`**
- Use CSS custom properties for theme switching
- Support light and dark themes
- **NEVER hardcode colors in components**

```scss
// CORRECT - Use CSS variables
.button {
  background-color: var(--color-primary);
  color: var(--color-text-inverse);
}

// WRONG - Never do this
.button {
  background-color: #3b82f6;
  color: white;
}
```

### Internationalization (i18n)

- **ALL user-facing text must be translatable**
- Use ngx-translate for runtime language switching
- Translation files: `src/ClimaSite.Web/src/assets/i18n/{lang}.json`
- Supported languages: EN (English), BG (Bulgarian), DE (German)

```typescript
// In template
{{ 'products.title' | translate }}

// With parameters
{{ 'cart.items' | translate:{ count: itemCount } }}

// In component
constructor(private translate: TranslateService) {
  this.translate.get('messages.success').subscribe(msg => {
    // Use translated message
  });
}
```

### Database

- Use EF Core migrations for schema changes
- Follow naming conventions: **snake_case for PostgreSQL**
- Use JSONB for flexible attributes (product specifications)
- Index frequently queried columns
- Use soft deletes where appropriate

```csharp
// JSONB column for specifications
public class Product
{
    public Dictionary<string, object> Specifications { get; set; } = new();
}

// In configuration
builder.Property(p => p.Specifications)
    .HasColumnType("jsonb")
    .HasColumnName("specifications");
```

### Git Workflow

- Main branch: production-ready code
- Feature branches: `feature/description`
- Bug fixes: `fix/description`
- Use conventional commits:
  - `feat:` new feature
  - `fix:` bug fix
  - `refactor:` code refactoring
  - `docs:` documentation
  - `test:` tests
  - `chore:` maintenance

---

## Commands

```bash
# Backend — run tests PER-PROJECT. NEVER a bare root `dotnet test`: the solution includes the
# server-dependent E2E project, which throws hundreds of false failures without a live stack.
dotnet build ClimaSite.sln                                # Build all projects
dotnet test tests/ClimaSite.Core.Tests --no-build         # Domain unit tests
dotnet test tests/ClimaSite.Application.Tests --no-build   # Application unit tests
dotnet test tests/ClimaSite.Api.Tests --no-build          # Integration tests (Testcontainers; needs Docker)
dotnet test ClimaSite.NoE2E.slnf                          # All non-E2E tests via the solution filter
dotnet run --project src/ClimaSite.Api                     # Start API server (http://localhost:5029)

# Frontend
cd src/ClimaSite.Web
npm install                                    # Install dependencies
ng serve                                       # Start dev server (http://localhost:4200)
npm test                                       # i18n key check + ng test (watch mode)
ng test --watch=false --browsers=ChromeHeadless  # CI mode
ng build --configuration=production            # Production build

# E2E Tests (Playwright-for-.NET / xUnit) — needs a running API on :5029 + `ng serve` on :4200.
# CI starts both for you; for local runs see docs/project-plan/DEV_WORKFLOW.md (env vars, ports).
dotnet test tests/ClimaSite.E2E                                        # Run all E2E tests
dotnet test tests/ClimaSite.E2E --filter "FullyQualifiedName~Checkout"  # Run a subset

# Database
cd src/ClimaSite.Infrastructure
dotnet ef migrations add <Name>                # Create migration
dotnet ef database update                      # Apply migrations
dotnet ef migrations remove                    # Remove last migration

# Full local check (CI is the evidence of record — see the post-implementation workflow above)
dotnet build ClimaSite.sln && \
dotnet test ClimaSite.NoE2E.slnf && \
cd src/ClimaSite.Web && npm test -- --watch=false --browsers=ChromeHeadless

# Linting
cd src/ClimaSite.Web
ng lint                                        # Run ESLint

# Generate Angular component
ng generate component features/my-feature/components/my-component --standalone
```

---

## Environment Variables

Store sensitive configuration in environment variables or `appsettings.Development.json`:

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | PostgreSQL connection string |
| `REDIS_URL` | Redis connection string |
| `JWT_SECRET` | JWT signing secret (min 32 chars) |
| `JWT_ISSUER` | JWT issuer URL |
| `JWT_AUDIENCE` | JWT audience URL |
| `STRIPE_SECRET_KEY` | Stripe API secret key |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key (frontend) |
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
- Cart merges when guest user logs in

---

## Common Debugging Tips

### Frontend Issues

```typescript
// Check if translations are loaded
this.translate.get('test.key').subscribe(console.log);

// Debug signal values
console.log('Current value:', this.mySignal());

// Check auth state
console.log('Is authenticated:', this.authService.isAuthenticated());
console.log('User:', this.authService.user());
```

### Backend Issues

```csharp
// Check request headers
var lang = Request.Query["lang"].FirstOrDefault(); // the API selects language via ?lang=, not Accept-Language

// Debug EF queries
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);

// Check JWT claims
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
```

### E2E Test Debugging

```typescript
// Pause test and inspect
await page.pause();

// Screenshot on failure
await page.screenshot({ path: 'debug.png' });

// Console logs
page.on('console', msg => console.log('PAGE LOG:', msg.text()));
```

---

## Implementation Plans

All feature implementation plans are in `docs/plans/`:

### Archived Plans (Complete)

Plans 01-11 have been completed and archived. They covered:
- Design System & Theming (Plan 01)
- Internationalization (Plan 02)
- Authentication & User Management (Plan 03)
- Product Catalog (Plan 04)
- Shopping Cart (Plan 05)
- Checkout & Orders (Plan 06)
- Admin Panel (Plan 07)
- Testing Infrastructure (Plan 08)
- Inventory Management (Plan 09)
- Search & Navigation (Plan 10)
- Reviews & Ratings (Plan 11)

### Active Plans

| Plan | Task IDs | Description | Status |
|------|----------|-------------|--------|
| 12-notifications-system.md | NOT-001 to NOT-020 | Email, in-app notifications | Partial |
| archive/13-wishlist.md | WISH-001 to WISH-019, WISH-100 to WISH-108 | Wishlist, sharing | Complete |
| 17-future-enhancements.md | Various | Related products, translations | Complete |

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
11. **Works in both themes** - Verified in light AND dark mode
12. **Works in all languages** - Verified in EN, BG, and DE

**DO NOT mark a task as complete until ALL of these are verified.**

---

## Quick Reference

### File Locations

| What | Where |
|------|-------|
| Colors/Themes | `src/ClimaSite.Web/src/styles/_colors.scss` |
| Translations | `src/ClimaSite.Web/src/assets/i18n/*.json` |
| API Controllers | `src/ClimaSite.Api/Controllers/` |
| CQRS Handlers | `src/ClimaSite.Application/Features/` |
| Domain Entities | `src/ClimaSite.Core/Entities/` |
| EF Configs | `src/ClimaSite.Infrastructure/Data/Configurations/` |
| E2E Tests | `tests/ClimaSite.E2E/Tests/` |
| Test Factory | `tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs` |
| Plans | `docs/plans/` |
| Animation Services | `src/ClimaSite.Web/src/app/core/services/animation.service.ts` |
| Flying Cart | `src/ClimaSite.Web/src/app/core/services/flying-cart.service.ts` |
| Confetti | `src/ClimaSite.Web/src/app/core/services/confetti.service.ts` |
| Performance Audit | `docs/performance/performance-audit.md` |
| Animation Directives | `src/ClimaSite.Web/src/app/shared/directives/` |
| RevealDirective | `src/ClimaSite.Web/src/app/shared/directives/reveal.directive.ts` |

### Important URLs (Development)

| Service | URL |
|---------|-----|
| Angular Frontend | http://localhost:4200 |
| API Backend | http://localhost:5029 |
| Swagger UI | http://localhost:5029/swagger |
