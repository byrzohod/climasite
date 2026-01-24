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
| Translation Management | Complete | Admin UI for product translations |
| Bug Fixes (Plan 18) | Complete | Category navigation, wishlist, auth timing, i18n, theme fixes |
| **Saved Addresses** | **Complete** | Full CRUD for user addresses, checkout integration |
| **Stripe Payments** | **Complete** | Payment intents, card input, bank transfer support |
| **Product Search** | **Complete** | Frontend search wired to backend API |
| **Reorder Feature** | **Complete** | Re-add previous order items to cart |

### Recently Completed

| Feature | Status | Description |
|---------|--------|-------------|
| Saved Addresses | Complete | AddressesController, AddressService, checkout saved address selector |
| Stripe Integration | Complete | PaymentsController, PaymentService, Stripe Elements |
| Product Search UI | Complete | Header search wired to product list filtering |
| Reorder | Complete | E2E test enabled, existing implementation verified |
| Mega Menu | Complete | Fixed flaky E2E test with proper waits |
| **UI Improvement Plan** | **Complete** | 45+ issues fixed across 6 phases (see docs/plans/21-ui-improvement-plan.md) |
| **Shared Components** | **Complete** | Alert, Modal, Toast, Breadcrumb components created |
| **Accessibility (WCAG)** | **Complete** | Focus traps, ARIA roles, keyboard navigation, screen reader support |
| **Performance** | **Complete** | Scroll throttling, lazy loading, memory leak fixes |
| **Motion/Animation System** | **Complete** | AnimationService, reduced motion support, GPU-accelerated animations |
| **Flying Cart Animation** | **Complete** | Product image flies to cart icon with arc trajectory |
| **Confetti Celebration** | **Complete** | Canvas-based confetti on order confirmation |
| **Parallax Effects** | **Complete** | Scroll and mouse-based parallax on hero sections |
| **Toast Notifications** | **Complete** | Progress bar, hover pause, type-specific icons |
| **Route Transitions** | **Complete** | Fade/slide page transitions |
| **Product Gallery** | **Complete** | Lightbox, crossfade, zoom, slide animations |
| **Performance Audit** | **Complete** | Core Web Vitals optimizations, preconnect hints |

---

## Tech Stack

| Layer | Technology | Version |
|-------|------------|---------|
| **Backend** | ASP.NET Core | .NET 10 |
| **Frontend** | Angular | 19+ (standalone components) |
| **Database** | PostgreSQL | 16+ |
| **ORM** | Entity Framework Core | 9.x |
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
│   ├── ClimaSite.E2E/              # Playwright E2E tests
│   │   ├── fixtures/               # Test data factory
│   │   ├── helpers/                # Utility functions
│   │   └── tests/                  # Test files by feature
│   └── ClimaSite.Web.Tests/        # Angular unit tests (karma.conf.js)
│
├── docs/
│   ├── plans/                      # Implementation plans (00-18)
│   └── skills.md                   # Required skills documentation
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
Accept-Language: en|bg|de          # For translated content
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

**After EVERY implementation session, you MUST:**

1. **Run ALL tests** (backend, frontend, AND E2E):
   ```bash
   # Run from project root
   dotnet test && \
   cd src/ClimaSite.Web && ng test --watch=false --browsers=ChromeHeadless && \
   cd ../../tests/ClimaSite.E2E && dotnet test
   ```

2. **Ensure ALL tests pass** - Do not proceed if any tests fail. Fix failures first.

3. **Update CLAUDE.md** if:
   - New features were added (update status table)
   - New patterns or conventions were established
   - New commands or workflows were introduced

4. **Commit and push to main**:
   ```bash
   git add -A
   git commit -m "feat: <description of changes>"
   git push origin main
   ```

**This workflow is NON-NEGOTIABLE. Never skip these steps.**

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
# Backend
dotnet build                                   # Build all projects
dotnet test                                    # Run all backend tests
dotnet run --project src/ClimaSite.Api         # Start API server (http://localhost:5000)

# Frontend
cd src/ClimaSite.Web
npm install                                    # Install dependencies
ng serve                                       # Start dev server (http://localhost:4200)
ng test                                        # Run unit tests (watch mode)
ng test --watch=false --browsers=ChromeHeadless  # CI mode
ng build --configuration=production            # Production build

# E2E Tests (Playwright)
cd tests/ClimaSite.E2E
npx playwright install                         # Install browsers (first time)
npx playwright test                            # Run all E2E tests
npx playwright test --ui                       # Interactive UI mode
npx playwright test tests/auth/                # Run specific folder
npx playwright test -g "checkout"              # Run tests matching pattern
npx playwright test --debug                    # Debug mode

# Database
cd src/ClimaSite.Infrastructure
dotnet ef migrations add <Name>                # Create migration
dotnet ef database update                      # Apply migrations
dotnet ef migrations remove                    # Remove last migration

# Full Test Suite (run before any PR)
dotnet test && \
cd src/ClimaSite.Web && ng test --watch=false --browsers=ChromeHeadless && \
cd ../../tests/ClimaSite.E2E && npx playwright test

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
var lang = Request.Headers["Accept-Language"].FirstOrDefault();

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

| Plan | Task IDs | Description | Status |
|------|----------|-------------|--------|
| 00-master-overview.md | - | Project overview and phases | Reference |
| 01-design-system-theming.md | DST-001 to DST-015 | Colors, themes, components | Complete |
| 02-internationalization-i18n.md | I18N-001 to I18N-015 | Multi-language support | Complete |
| 03-authentication-user-management.md | AUTH-001 to AUTH-027 | JWT auth, users, roles | Complete |
| 04-product-catalog.md | CAT-001 to CAT-030 | Products, categories, variants | Complete |
| 05-shopping-cart.md | CART-001 to CART-020 | Cart, guest carts, merging | Complete |
| 06-checkout-orders.md | CHK-001 to CHK-035 | Checkout, Stripe, orders | Complete |
| 07-admin-panel.md | ADM-001 to ADM-030 | Admin dashboard, CRUD | Complete |
| 08-testing-infrastructure.md | TEST-001 to TEST-020 | Test setup, patterns | Complete |
| 09-inventory-management.md | INV-001 to INV-020 | Stock, reservations | Complete |
| 10-search-navigation.md | SRCH-001 to SRCH-024 | Search, facets, filters | Complete |
| 11-reviews-ratings.md | REV-001 to REV-022 | Reviews, ratings, moderation | Complete |
| 12-notifications-system.md | NOT-001 to NOT-020 | Email, in-app notifications | Partial |
| 13-wishlist.md | WISH-001 to WISH-019 | Wishlist, sharing | Not Started |
| 17-future-enhancements.md | Various | Related products, translations | Complete |
| **18-bug-fixes-and-enhancements.md** | NAV, AUTH, I18N, THEME, HOME, FILTER | **Bug fixes and improvements** | **Pending** |

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
| Test Factory | `tests/ClimaSite.E2E/fixtures/test-data-factory.ts` |
| Plans | `docs/plans/` |
| Animation Services | `src/ClimaSite.Web/src/app/core/services/animation.service.ts` |
| Flying Cart | `src/ClimaSite.Web/src/app/core/services/flying-cart.service.ts` |
| Confetti | `src/ClimaSite.Web/src/app/core/services/confetti.service.ts` |
| Performance Audit | `docs/performance/performance-audit.md` |
| Animation Directives | `src/ClimaSite.Web/src/app/shared/directives/` |

### Important URLs (Development)

| Service | URL |
|---------|-----|
| Angular Frontend | http://localhost:4200 |
| API Backend | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Playwright Report | http://localhost:9323 |
