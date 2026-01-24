# API Contracts & Security - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **REST API Design** - Endpoint structure, HTTP methods, status codes
- **OpenAPI/Swagger** - API documentation accuracy and completeness
- **Request Validation** - FluentValidation pipeline for input validation
- **Error Response Format** - Consistent error handling and response structure
- **Authentication Security** - JWT implementation, token management
- **Authorization** - Role-based access control (RBAC)
- **GDPR/PII Handling** - Personal data protection compliance
- **Rate Limiting** - API abuse prevention
- **SQL Injection Protection** - Database query security
- **XSS Protection** - Cross-site scripting prevention
- **Dependency Security** - Third-party vulnerability management

### API Controllers Inventory

| Controller | Route | Auth Required | Role |
|-----------|-------|---------------|------|
| **AuthController** | `/api/auth/*` | Mixed | Public/User |
| **ProductsController** | `/api/products/*` | No | Public |
| **CategoriesController** | `/api/categories/*` | No | Public |
| **BrandsController** | `/api/brands/*` | No | Public |
| **CartController** | `/api/cart/*` | Mixed | Public/User |
| **OrdersController** | `/api/orders/*` | Mixed | Public/User |
| **PaymentsController** | `/api/payments/*` | Yes | User |
| **AddressesController** | `/api/addresses/*` | Yes | User |
| **WishlistController** | `/api/wishlist/*` | Yes | User |
| **ReviewsController** | `/api/reviews/*` | Mixed | Public/User |
| **QuestionsController** | `/api/questions/*` | Mixed | Public/User |
| **NotificationsController** | `/api/notifications/*` | Yes | User |
| **PromotionsController** | `/api/promotions/*` | No | Public |
| **PriceHistoryController** | `/api/price-history/*` | No | Public |
| **InstallationController** | `/api/installation/*` | No | Public |
| **AdminProductsController** | `/api/admin/products/*` | Yes | Admin |
| **AdminCategoriesController** | `/api/admin/categories/*` | Yes | Admin |
| **AdminOrdersController** | `/api/admin/orders/*` | Yes | Admin |
| **AdminCustomersController** | `/api/admin/customers/*` | Yes | Admin |
| **AdminDashboardController** | `/api/admin/dashboard/*` | Yes | Admin |
| **AdminReviewsController** | `/api/admin/reviews/*` | Yes | Admin |
| **AdminQuestionsController** | `/api/admin/questions/*` | Yes | Admin |
| **InventoryController** | `/api/admin/inventory/*` | Yes | Admin |
| **TestController** | `/api/test/*` | No | Debug Only |

---

## 2. Code Path Map

### Backend Security Infrastructure

| Layer | Files | Purpose |
|-------|-------|---------|
| **Middleware** | `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs` | Global error handling |
| **Program.cs** | `src/ClimaSite.Api/Program.cs` | Auth configuration, CORS, Swagger |
| **Validation Pipeline** | `src/ClimaSite.Application/Common/Behaviors/ValidationBehavior.cs` | MediatR validation |
| **Validation Exception** | `src/ClimaSite.Application/Common/Exceptions/ValidationException.cs` | Validation error handling |
| **DI Registration** | `src/ClimaSite.Application/DependencyInjection.cs` | FluentValidation registration |

### Controller Endpoints Detail

#### AuthController (`/api/auth`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/register` | No | User registration |
| POST | `/login` | No | User authentication |
| POST | `/logout` | Yes | Token revocation |
| POST | `/refresh` | No* | Refresh access token (*uses cookie) |
| POST | `/forgot-password` | No | Password reset request |
| POST | `/reset-password` | No | Reset password with token |
| POST | `/confirm-email` | No | Email verification |
| GET | `/me` | Yes | Get current user |
| PUT | `/me` | Yes | Update profile |
| PUT | `/change-password` | Yes | Change password |

#### ProductsController (`/api/products`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | No | List products (paginated) |
| GET | `/{slug}` | No | Get product by slug |
| GET | `/featured` | No | Featured products |
| GET | `/search` | No | Search products |
| GET | `/{id}/related` | No | Related products |
| GET | `/{id}/similar` | No | Similar products |
| GET | `/{id}/consumables` | No | Product consumables |
| GET | `/{id}/frequently-bought-together` | No | Frequently bought together |
| GET | `/filters` | No | Filter options |

#### OrdersController (`/api/orders`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/` | No* | Create order (*guest checkout allowed) |
| GET | `/` | Yes | List user orders |
| GET | `/statuses` | No | Get order statuses |
| GET | `/{id}` | No* | Get order by ID (*ownership check needed) |
| GET | `/by-number/{orderNumber}` | No* | Get order by number |
| POST | `/{id}/cancel` | Yes | Cancel order |
| POST | `/{id}/reorder` | Yes | Reorder previous order |
| GET | `/{id}/invoice` | Yes | Download invoice |

#### CartController (`/api/cart`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | No | Get cart |
| POST | `/items` | No | Add to cart |
| PUT | `/items/{itemId}` | No | Update quantity |
| DELETE | `/items/{itemId}` | No | Remove item |
| DELETE | `/` | No | Clear cart |
| POST | `/merge` | Yes | Merge guest cart |

#### Admin Controllers (`/api/admin/*`)
All admin endpoints require `[Authorize(Roles = "Admin")]`:
- Products CRUD, translations, relations
- Categories CRUD, reordering
- Orders management, status updates
- Customers management
- Dashboard KPIs, charts
- Reviews/Questions moderation
- Inventory management

### FluentValidation Validators

| Validator | Location | Validates |
|-----------|----------|-----------|
| `LoginCommandValidator` | `Features/Auth/Commands/LoginCommand.cs` | Email, password required |
| `RegisterCommandValidator` | `Features/Auth/Commands/RegisterCommand.cs` | Email, password strength, name |
| `RefreshTokenCommandValidator` | `Features/Auth/Commands/RefreshTokenCommand.cs` | Token required |
| `CreateOrderCommandValidator` | `Features/Orders/Commands/CreateOrderCommand.cs` | Shipping info, items |
| `AddToCartCommandValidator` | `Features/Cart/Commands/AddToCartCommand.cs` | ProductId, quantity |
| `CreateReviewCommandValidator` | `Features/Reviews/Commands/CreateReviewCommand.cs` | ProductId, rating, title, content |
| `AskQuestionCommandValidator` | `Features/Questions/Commands/AskQuestionCommand.cs` | ProductId, question text |
| `CreateProductCommandValidator` | `Features/Products/Commands/CreateProductCommand.cs` | Name, SKU, price |
| `UpdateProductCommandValidator` | `Features/Products/Commands/UpdateProductCommand.cs` | Id, optional fields |
| `CreateCategoryCommandValidator` | `Features/Categories/Commands/CreateCategoryCommand.cs` | Name, slug |
| `CreateInstallationRequestCommandValidator` | `Features/Installation/Commands/...` | Customer info, address |
| `AddProductTranslationCommandValidator` | `Features/Admin/Translations/Commands/...` | LanguageCode, name |
| *(30+ more validators)* | Various | See grep results |

---

## 3. Test Coverage Audit

### API Integration Tests

| Test File | Tests | Coverage |
|-----------|-------|----------|
| `tests/ClimaSite.Api.Tests/Controllers/ProductsControllerTests.cs` | 7 tests | Products GET, translations |

**Missing Integration Tests:**
- [ ] AuthController - No integration tests
- [ ] OrdersController - No integration tests
- [ ] CartController - No integration tests
- [ ] PaymentsController - No integration tests
- [ ] AddressesController - No integration tests
- [ ] All Admin Controllers - No integration tests

### E2E Security Tests

| Test File | Coverage |
|-----------|----------|
| `tests/ClimaSite.E2E/Tests/Authentication/LoginTests.cs` | Login validation, error handling |
| `tests/ClimaSite.E2E/Tests/Authentication/UserMenuTests.cs` | Auth state, logout |
| `tests/ClimaSite.E2E/Tests/Account/ProfileTests.cs` | Auth guard, profile access |

**Missing E2E Security Tests:**
- [ ] Registration with weak passwords
- [ ] Rate limiting verification
- [ ] CSRF protection
- [ ] Token expiration handling
- [ ] Admin access control
- [ ] Order ownership verification

---

## 4. Manual Verification Steps

### API Contract Verification

1. **Swagger Documentation**
   - Navigate to `http://localhost:5000/swagger`
   - Verify all endpoints are documented
   - Check request/response schemas match implementation
   - Verify auth requirements are accurate
   - Test "Try it out" functionality

2. **Status Code Verification**
   - `200 OK` - Successful GET, PUT, DELETE
   - `201 Created` - Successful POST with resource creation
   - `204 No Content` - Successful action without response body
   - `400 Bad Request` - Validation errors
   - `401 Unauthorized` - Missing/invalid token
   - `403 Forbidden` - Insufficient permissions
   - `404 Not Found` - Resource doesn't exist
   - `500 Internal Server Error` - Unhandled exceptions

3. **Error Response Format**
   ```bash
   # Test validation error
   curl -X POST http://localhost:5000/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "", "password": ""}'
   # Expected: {"status":400,"message":"...","detail":"..."}
   ```

### Authentication Security

1. **JWT Token Validation**
   ```bash
   # Test with expired token
   curl http://localhost:5000/api/auth/me \
     -H "Authorization: Bearer <expired_token>"
   # Expected: 401 Unauthorized
   
   # Test with tampered token
   curl http://localhost:5000/api/auth/me \
     -H "Authorization: Bearer <tampered_token>"
   # Expected: 401 Unauthorized
   ```

2. **Refresh Token Cookie**
   - Verify `HttpOnly` flag is set
   - Verify `Secure` flag in production
   - Verify `SameSite` attribute
   - Check 7-day expiration

3. **Password Reset Security**
   - Verify same response for valid/invalid emails (prevents enumeration)
   - Verify token expiration
   - Verify token single-use

### Authorization Verification

1. **Admin Endpoints**
   ```bash
   # Test admin endpoint as regular user
   curl http://localhost:5000/api/admin/products \
     -H "Authorization: Bearer <customer_token>"
   # Expected: 403 Forbidden
   ```

2. **Resource Ownership**
   - Verify users can only access their own orders
   - Verify users can only access their own addresses
   - Verify users can only access their own wishlist

### Input Validation

1. **SQL Injection Test**
   ```bash
   curl "http://localhost:5000/api/products?searchTerm='; DROP TABLE products;--"
   # Expected: No SQL error, safe handling
   ```

2. **XSS Test**
   ```bash
   curl -X POST http://localhost:5000/api/reviews \
     -H "Authorization: Bearer <token>" \
     -H "Content-Type: application/json" \
     -d '{"productId":"...","rating":5,"title":"<script>alert(1)</script>","content":"test"}'
   # Expected: Stored as escaped text, not executed
   ```

---

## 5. Gaps & Risks

### Critical Security Gaps

| ID | Issue | Risk Level | Description |
|----|-------|------------|-------------|
| SEC-001 | **No Rate Limiting** | Critical | No protection against brute force or DDoS |
| SEC-002 | **Order Access Control** | Critical | `/api/orders/{id}` lacks ownership verification |
| SEC-003 | **No GDPR Endpoints** | High | No data export or account deletion |
| SEC-004 | **ProblemDetails Not Used** | Medium | Error format not RFC 7807 compliant |
| SEC-005 | **No CSRF Protection** | Medium | State-changing operations lack CSRF tokens |

### Authentication Gaps

| ID | Issue | Risk Level | Description |
|----|-------|------------|-------------|
| AUTH-SEC-001 | **Access Token in localStorage** | Medium | XSS vulnerability for token theft |
| AUTH-SEC-002 | **No Account Lockout Tests** | Medium | Lockout after failed attempts not verified |
| AUTH-SEC-003 | **No Session Revocation** | Low | Cannot invalidate all sessions on password change |

### API Contract Gaps

| ID | Issue | Risk Level | Description |
|----|-------|------------|-------------|
| API-001 | **Missing Integration Tests** | High | 90% of endpoints lack integration tests |
| API-002 | **Inconsistent Error Messages** | Low | Some use `message`, others use `error` |
| API-003 | **Missing Response Types** | Low | Some endpoints lack `[ProducesResponseType]` |

### Input Validation Gaps

| ID | Issue | Risk Level | Description |
|----|-------|------------|-------------|
| VAL-001 | **No XSS Sanitization** | Medium | Review/question content not sanitized |
| VAL-002 | **No File Upload Validation** | N/A | File uploads not implemented yet |
| VAL-003 | **Inline Validators** | Low | Validators not in separate files |

### Dependency Security

| ID | Issue | Risk Level | Description |
|----|-------|------------|-------------|
| DEP-001 | **No Vulnerability Scanning** | Medium | No automated dependency checks |
| DEP-002 | **No Security Headers** | Medium | Missing CSP, X-Frame-Options, etc. |

---

## 6. Recommended Fixes & Tests

### Critical Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **SEC-001: Rate Limiting** | Implement `app.UseRateLimiter()` with sliding window policy | Medium |
```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("auth", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 6;
        config.PermitLimit = 10;
    });
    options.AddSlidingWindowLimiter("api", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 6;
        config.PermitLimit = 100;
    });
});
app.UseRateLimiter();
```

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **SEC-002: Order Access** | Add ownership check in GetOrderByIdQuery handler | Low |
```csharp
// In GetOrderByIdQueryHandler
if (order.UserId != null && order.UserId != currentUserId && !isAdmin)
{
    return Result<OrderDto>.Failure("Order not found");
}
```

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **SEC-003: GDPR Compliance** | Add account endpoints for data export/deletion | High |
```csharp
// New endpoints needed:
// GET /api/account/export-data - Export all user data as JSON/ZIP
// DELETE /api/account - Delete account and all associated data
```

### High Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **SEC-004: ProblemDetails** | Migrate to RFC 7807 format | Medium |
```csharp
services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
services.AddProblemDetails();
```

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **API-001: Integration Tests** | Create comprehensive test suite | High |
```
tests/ClimaSite.Api.Tests/Controllers/
  ├── AuthControllerTests.cs
  ├── OrdersControllerTests.cs
  ├── CartControllerTests.cs
  ├── PaymentsControllerTests.cs
  ├── AddressesControllerTests.cs
  └── Admin/
      ├── AdminProductsControllerTests.cs
      └── AdminOrdersControllerTests.cs
```

### Medium Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **DEP-002: Security Headers** | Add security headers middleware | Low |
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **VAL-001: XSS Sanitization** | Use HtmlSanitizer for user content | Medium |
```csharp
// Add HtmlSanitizer package
// In CreateReviewCommandHandler:
var sanitizer = new HtmlSanitizer();
review.SetContent(sanitizer.Sanitize(command.Content));
```

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **DEP-001: Vulnerability Scanning** | Add GitHub Dependabot or Snyk | Low |
```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
```

### Low Priority

| Issue | Recommendation | Effort |
|-------|----------------|--------|
| **AUTH-SEC-003: Session Revocation** | Invalidate tokens on password change | Medium |
| **API-002: Error Consistency** | Standardize all errors to `message` key | Low |
| **API-003: Response Types** | Add `[ProducesResponseType]` to all endpoints | Low |

---

## 7. Evidence & Notes

### Current Security Implementation

**JWT Configuration** (from `Program.cs:63-81`):
```csharp
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero  // No clock skew tolerance
    };
});
```

**Refresh Token Cookie Security** (from `AuthController.cs:158-172`):
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = isProduction,  // HTTPS only in production
    SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
    Expires = DateTimeOffset.UtcNow.AddDays(7)
};
```

**Exception Handling** (from `ExceptionHandlingMiddleware.cs`):
```csharp
var (statusCode, message) = exception switch
{
    NotFoundException => (HttpStatusCode.NotFound, ...),
    ValidationException => (HttpStatusCode.BadRequest, ...),
    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ...),
    ArgumentException => (HttpStatusCode.BadRequest, ...),
    _ => (HttpStatusCode.InternalServerError, "An error occurred...")
};
```

**CORS Configuration** (from `Program.cs:94-104`):
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

### SQL Injection Protection

The application uses Entity Framework Core exclusively with LINQ queries. No `FromSqlRaw` or `ExecuteSqlRaw` calls exist in production code. The only raw SQL is in test database cleanup (`DatabaseCleaner.cs`), which is acceptable.

### Password Security

ASP.NET Core Identity handles password hashing using PBKDF2 by default with:
- 100,000 iterations
- SHA256
- 128-bit salt
- 256-bit subkey

### Swagger Security

Swagger is configured with JWT Bearer authentication (from `Program.cs:120-145`):
- Security scheme defined for Bearer token
- Security requirement added globally
- Available at `/swagger` endpoint

### Test Controller Safety

The `TestController` is only compiled in DEBUG builds (`#if DEBUG`) and all endpoints check `_environment.IsDevelopment()` before executing. This prevents accidental exposure in production.

### Authorization Attribute Coverage

| Controller | Class-Level Auth | Notes |
|-----------|------------------|-------|
| AdminProductsController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminCategoriesController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminOrdersController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminCustomersController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminDashboardController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminReviewsController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| AdminQuestionsController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| InventoryController | `[Authorize(Roles = "Admin")]` | All endpoints protected |
| PaymentsController | `[Authorize]` | `/config` has `[AllowAnonymous]` |
| AddressesController | `[Authorize]` | All endpoints protected |
| WishlistController | `[Authorize]` | All endpoints protected |
| NotificationsController | `[Authorize]` | All endpoints protected |
| AuthController | None | Per-endpoint `[Authorize]` |
| CartController | None | `/merge` has `[Authorize]` |
| OrdersController | None | Mixed auth per endpoint |
| ReviewsController | None | Write ops have `[Authorize]` |
| QuestionsController | None | Official answers require Admin |

### FluentValidation Pipeline

Validation is automatically applied via MediatR pipeline behavior:
```csharp
// ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();
        
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
            
        if (failures.Any())
            throw new ValidationException(failures);
            
        return await next();
    }
}
```

---

## Summary

The ClimaSite API has a solid foundation with proper JWT authentication, role-based authorization, and input validation through FluentValidation. However, several critical security gaps need immediate attention:

1. **Rate limiting is not implemented** - This is the highest priority security risk
2. **Order access control is incomplete** - Users may access others' orders
3. **No GDPR compliance endpoints** - Required for EU customers
4. **Integration test coverage is minimal** - Only ProductsController has tests

The recommended action plan:
1. Implement rate limiting immediately (SEC-001)
2. Add order ownership verification (SEC-002)
3. Add security headers (DEP-002)
4. Create comprehensive integration tests (API-001)
5. Implement GDPR endpoints (SEC-003)
