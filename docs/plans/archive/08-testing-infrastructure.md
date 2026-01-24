# Testing Infrastructure Plan

## Document Information
- **Document ID**: PLAN-008
- **Version**: 1.0
- **Last Updated**: 2026-01-10
- **Status**: Draft

---

## 1. Overview

This document outlines the comprehensive testing strategy for the ClimaSite HVAC e-commerce platform. The strategy encompasses unit tests, integration tests, and end-to-end (E2E) tests to ensure code quality, reliability, and business requirements compliance.

### 1.1 Testing Goals

- **Code Quality**: Catch bugs early in the development cycle
- **Regression Prevention**: Ensure new changes don't break existing functionality
- **Documentation**: Tests serve as living documentation of system behavior
- **Confidence**: Enable confident deployments to production
- **Business Validation**: Verify critical business flows work correctly

### 1.2 Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| Backend Unit/Integration | xUnit, Moq, FluentAssertions | .NET testing |
| Frontend Unit | Jasmine, Karma | Angular component testing |
| E2E Testing | Playwright | Cross-browser E2E tests |
| Test Database | PostgreSQL | Isolated test data |
| CI/CD | GitHub Actions | Automated test execution |

---

## 2. Testing Philosophy

### 2.1 Core Principles

#### NO MOCKING in E2E Tests
E2E tests must interact with real services, real databases, and real API endpoints. This ensures tests validate actual system behavior.

```
E2E Tests = Real Browser + Real API + Real Database + Real Services
```

#### Self-Contained Tests
Each E2E test creates its own complete test data set:
- Creates its own users
- Creates its own products
- Creates its own orders
- Cleans up after itself

#### Test Isolation
Tests must not depend on:
- Data from other tests
- Execution order
- Shared mutable state

### 2.2 Test Pyramid

```
                    /\
                   /  \
                  / E2E \        <- Few, comprehensive
                 /  Tests \         (10-15% of tests)
                /----------\
               / Integration \   <- More coverage
              /    Tests      \     (20-30% of tests)
             /----------------\
            /    Unit Tests    \ <- Most tests
           /                    \   (55-70% of tests)
          /____________________\
```

| Level | Count Target | Execution Time | Scope |
|-------|--------------|----------------|-------|
| Unit | 500+ tests | < 30 seconds | Single class/method |
| Integration | 150+ tests | < 3 minutes | API endpoints, DB |
| E2E | 50+ tests | < 15 minutes | Full user journeys |

### 2.3 What to Test

| Test Level | What to Test | What NOT to Test |
|------------|--------------|------------------|
| Unit | Business logic, calculations, validations | Framework code, trivial getters/setters |
| Integration | API contracts, database operations, auth | External services (mock these) |
| E2E | Critical user journeys, checkout flow | Edge cases (use unit tests) |

---

## 3. Project Structure

```
tests/
├── ClimaSite.Core.Tests/              # Domain/business logic unit tests
│   ├── Entities/
│   │   ├── ProductTests.cs
│   │   ├── OrderTests.cs
│   │   └── UserTests.cs
│   ├── Services/
│   │   ├── PricingServiceTests.cs
│   │   ├── InventoryServiceTests.cs
│   │   └── OrderServiceTests.cs
│   ├── Validators/
│   │   └── ProductValidatorTests.cs
│   └── ClimaSite.Core.Tests.csproj
│
├── ClimaSite.Api.Tests/               # API integration tests
│   ├── Controllers/
│   │   ├── ProductsControllerTests.cs
│   │   ├── OrdersControllerTests.cs
│   │   ├── AuthControllerTests.cs
│   │   └── CartControllerTests.cs
│   ├── Infrastructure/
│   │   ├── TestWebApplicationFactory.cs
│   │   ├── DatabaseFixture.cs
│   │   └── AuthenticationHelper.cs
│   ├── Fixtures/
│   │   └── IntegrationTestFixture.cs
│   └── ClimaSite.Api.Tests.csproj
│
├── ClimaSite.E2E/                     # Playwright E2E tests
│   ├── Tests/
│   │   ├── Authentication/
│   │   │   ├── LoginTests.cs
│   │   │   └── RegistrationTests.cs
│   │   ├── Products/
│   │   │   ├── ProductBrowsingTests.cs
│   │   │   └── ProductSearchTests.cs
│   │   ├── Cart/
│   │   │   └── ShoppingCartTests.cs
│   │   └── Checkout/
│   │       └── CheckoutFlowTests.cs
│   ├── Infrastructure/
│   │   ├── TestDataFactory.cs
│   │   ├── ApiClient.cs
│   │   ├── DatabaseCleaner.cs
│   │   └── PlaywrightFixture.cs
│   ├── PageObjects/
│   │   ├── HomePage.cs
│   │   ├── ProductPage.cs
│   │   ├── CartPage.cs
│   │   └── CheckoutPage.cs
│   ├── playwright.config.ts
│   └── ClimaSite.E2E.csproj
│
└── ClimaSite.Web.Tests/               # Angular frontend tests
    ├── components/
    │   ├── product-card.component.spec.ts
    │   ├── cart.component.spec.ts
    │   └── checkout-form.component.spec.ts
    ├── services/
    │   ├── product.service.spec.ts
    │   ├── cart.service.spec.ts
    │   └── auth.service.spec.ts
    ├── pipes/
    │   └── currency-format.pipe.spec.ts
    └── karma.conf.js
```

---

## 4. Implementation Tasks

### TEST-001: xUnit Test Project Setup

**Objective**: Set up xUnit test projects with proper dependencies and configuration.

**Tasks**:
1. Create test project for Core library
2. Create test project for API
3. Configure test settings and dependencies
4. Set up code coverage tooling

**Implementation**:

```xml
<!-- ClimaSite.Core.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Bogus" Version="35.*" />
    <PackageReference Include="coverlet.collector" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ClimaSite.Core\ClimaSite.Core.csproj" />
  </ItemGroup>
</Project>
```

**Acceptance Criteria**:
- [ ] All test projects compile successfully
- [ ] `dotnet test` discovers and runs tests
- [ ] Code coverage reports are generated
- [ ] Tests run in parallel by default

---

### TEST-002: TestWebApplicationFactory Implementation

**Objective**: Create a custom WebApplicationFactory for integration testing with real PostgreSQL.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Infrastructure/TestWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ClimaSite.Api.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("climasite_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ClimaSiteDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<ClimaSiteDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClimaSiteDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
```

**Acceptance Criteria**:
- [ ] PostgreSQL container starts automatically for tests
- [ ] Database migrations run on test database
- [ ] Factory properly cleans up resources
- [ ] Multiple test classes can share the factory

---

### TEST-003: Database Cleanup Strategy

**Objective**: Implement reliable database cleanup between tests to ensure isolation.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Infrastructure/DatabaseCleaner.cs
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClimaSite.Api.Tests.Infrastructure;

public class DatabaseCleaner
{
    private readonly ClimaSiteDbContext _context;

    public DatabaseCleaner(ClimaSiteDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cleans all data from tables while preserving schema.
    /// Uses TRUNCATE with CASCADE for efficiency.
    /// </summary>
    public async Task CleanAsync()
    {
        var tables = new[]
        {
            "order_items",
            "orders",
            "cart_items",
            "carts",
            "product_images",
            "product_specifications",
            "products",
            "categories",
            "user_addresses",
            "users",
            "audit_logs"
        };

        await using var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        await connection.OpenAsync();

        foreach (var table in tables)
        {
            await using var cmd = new NpgsqlCommand(
                $"TRUNCATE TABLE {table} CASCADE", connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Reset sequences
        await using var resetCmd = new NpgsqlCommand(@"
            DO $$
            DECLARE
                seq RECORD;
            BEGIN
                FOR seq IN SELECT sequencename FROM pg_sequences WHERE schemaname = 'public'
                LOOP
                    EXECUTE 'ALTER SEQUENCE ' || seq.sequencename || ' RESTART WITH 1';
                END LOOP;
            END $$;", connection);
        await resetCmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Cleans specific data created by a test using correlation ID.
    /// </summary>
    public async Task CleanByCorrelationIdAsync(Guid correlationId)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM orders WHERE correlation_id = {0}", correlationId);
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM users WHERE correlation_id = {0}", correlationId);
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM products WHERE correlation_id = {0}", correlationId);
    }
}
```

**Acceptance Criteria**:
- [ ] Database is clean before each test
- [ ] Cleanup is fast (< 500ms)
- [ ] Foreign key constraints are handled
- [ ] Sequences are reset

---

### TEST-004: Test Data Factory (NO MOCKING)

**Objective**: Create factories that generate REAL data in the database for E2E tests.

**Implementation**:

```csharp
// tests/ClimaSite.E2E/Infrastructure/TestDataFactory.cs
using System.Net.Http.Json;
using Bogus;

namespace ClimaSite.E2E.Infrastructure;

/// <summary>
/// Creates REAL data through the API - NO MOCKING.
/// Each test creates its own isolated data set.
/// </summary>
public class TestDataFactory
{
    private readonly HttpClient _apiClient;
    private readonly Faker _faker = new();
    private readonly Guid _correlationId = Guid.NewGuid();

    public Guid CorrelationId => _correlationId;

    public TestDataFactory(HttpClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Creates a real user account through the registration API.
    /// </summary>
    public async Task<TestUser> CreateUserAsync(string? email = null, string? password = null)
    {
        var user = new TestUser
        {
            Email = email ?? $"test_{_correlationId:N}_{_faker.Random.AlphaNumeric(8)}@test.com",
            Password = password ?? "TestPassword123!",
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName()
        };

        var response = await _apiClient.PostAsJsonAsync("/api/auth/register", new
        {
            email = user.Email,
            password = user.Password,
            firstName = user.FirstName,
            lastName = user.LastName,
            correlationId = _correlationId
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        user.Id = result!.UserId;
        user.Token = result.Token;

        return user;
    }

    /// <summary>
    /// Creates a real product through the admin API.
    /// </summary>
    public async Task<TestProduct> CreateProductAsync(
        string? name = null,
        decimal? price = null,
        int? stock = null,
        Guid? categoryId = null)
    {
        var product = new TestProduct
        {
            Name = name ?? $"Test AC Unit {_faker.Commerce.ProductAdjective()}",
            Description = _faker.Commerce.ProductDescription(),
            Price = price ?? _faker.Random.Decimal(500, 5000),
            Stock = stock ?? _faker.Random.Int(10, 100),
            Sku = $"TEST-{_correlationId:N}-{_faker.Random.AlphaNumeric(6)}".ToUpper(),
            CategoryId = categoryId ?? await GetOrCreateCategoryAsync()
        };

        var response = await _apiClient.PostAsJsonAsync("/api/admin/products", new
        {
            name = product.Name,
            description = product.Description,
            price = product.Price,
            stockQuantity = product.Stock,
            sku = product.Sku,
            categoryId = product.CategoryId,
            correlationId = _correlationId,
            specifications = new
            {
                btuRating = _faker.Random.Int(9000, 24000),
                energyRating = _faker.PickRandom("A++", "A+", "A", "B"),
                refrigerantType = "R32"
            }
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Id = result!.Id;

        return product;
    }

    /// <summary>
    /// Creates a complete order with products and user.
    /// </summary>
    public async Task<TestOrder> CreateOrderAsync(TestUser? user = null, int productCount = 2)
    {
        user ??= await CreateUserAsync();

        var products = new List<TestProduct>();
        for (int i = 0; i < productCount; i++)
        {
            products.Add(await CreateProductAsync());
        }

        // Add products to cart via API
        _apiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

        foreach (var product in products)
        {
            await _apiClient.PostAsJsonAsync("/api/cart/items", new
            {
                productId = product.Id,
                quantity = _faker.Random.Int(1, 3)
            });
        }

        // Create order from cart
        var orderResponse = await _apiClient.PostAsJsonAsync("/api/orders", new
        {
            shippingAddress = new
            {
                street = _faker.Address.StreetAddress(),
                city = _faker.Address.City(),
                postalCode = _faker.Address.ZipCode(),
                country = "Bulgaria"
            },
            paymentMethod = "card",
            correlationId = _correlationId
        });

        orderResponse.EnsureSuccessStatusCode();

        var result = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();

        return new TestOrder
        {
            Id = result!.Id,
            OrderNumber = result.OrderNumber,
            User = user,
            Products = products,
            TotalAmount = result.TotalAmount
        };
    }

    /// <summary>
    /// Creates a category if one doesn't exist.
    /// </summary>
    public async Task<Guid> GetOrCreateCategoryAsync(string? name = null)
    {
        name ??= "Test Air Conditioners";

        var response = await _apiClient.GetAsync($"/api/categories?name={name}");
        if (response.IsSuccessStatusCode)
        {
            var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
            if (categories?.Any() == true)
                return categories.First().Id;
        }

        var createResponse = await _apiClient.PostAsJsonAsync("/api/admin/categories", new
        {
            name = name,
            description = $"Test category created for E2E tests",
            correlationId = _correlationId
        });

        createResponse.EnsureSuccessStatusCode();
        var result = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        return result!.Id;
    }

    /// <summary>
    /// Creates an admin user for tests requiring elevated privileges.
    /// </summary>
    public async Task<TestUser> CreateAdminUserAsync()
    {
        var user = await CreateUserAsync();

        // Elevate to admin via direct database or seeded admin endpoint
        var response = await _apiClient.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = user.Id,
            testSecret = Environment.GetEnvironmentVariable("TEST_ADMIN_SECRET")
        });

        response.EnsureSuccessStatusCode();

        // Re-authenticate to get admin token
        var loginResponse = await _apiClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = user.Password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        user.Token = result!.Token;
        user.IsAdmin = true;

        return user;
    }
}

// Supporting classes
public class TestUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Token { get; set; } = default!;
    public bool IsAdmin { get; set; }
}

public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Sku { get; set; } = default!;
    public Guid CategoryId { get; set; }
}

public class TestOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public TestUser User { get; set; } = default!;
    public List<TestProduct> Products { get; set; } = new();
    public decimal TotalAmount { get; set; }
}
```

**Acceptance Criteria**:
- [ ] Factory creates real data via API calls
- [ ] Each factory instance has unique correlation ID
- [ ] Data can be traced back for cleanup
- [ ] Factory supports all major entities

---

### TEST-005: Playwright Configuration

**Objective**: Configure Playwright for cross-browser E2E testing.

**Implementation**:

```csharp
// tests/ClimaSite.E2E/Infrastructure/PlaywrightFixture.cs
using Microsoft.Playwright;

namespace ClimaSite.E2E.Infrastructure;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;
    public HttpClient ApiClient { get; private set; } = default!;

    private readonly string _baseUrl;
    private readonly string _apiUrl;

    public PlaywrightFixture()
    {
        _baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:4200";
        _apiUrl = Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5000";
    }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("E2E_HEADLESS") != "false",
            SlowMo = int.TryParse(Environment.GetEnvironmentVariable("E2E_SLOW_MO"), out var slowMo)
                ? slowMo : 0
        });

        ApiClient = new HttpClient
        {
            BaseAddress = new Uri(_apiUrl)
        };
    }

    public async Task<IPage> CreatePageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = Environment.GetEnvironmentVariable("E2E_RECORD_VIDEO") == "true"
                ? "videos/" : null
        });

        var page = await context.NewPageAsync();

        // Set default timeouts
        page.SetDefaultTimeout(30000);
        page.SetDefaultNavigationTimeout(30000);

        return page;
    }

    public TestDataFactory CreateDataFactory() => new(ApiClient);

    public async Task DisposeAsync()
    {
        ApiClient.Dispose();
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}
```

```json
// tests/ClimaSite.E2E/playwright.config.json
{
  "testDir": "./Tests",
  "timeout": 60000,
  "retries": 2,
  "workers": 4,
  "reporter": [
    ["html", { "outputFolder": "test-results/html" }],
    ["junit", { "outputFile": "test-results/junit.xml" }]
  ],
  "use": {
    "baseURL": "http://localhost:4200",
    "trace": "retain-on-failure",
    "screenshot": "only-on-failure",
    "video": "retain-on-failure"
  },
  "projects": [
    {
      "name": "chromium",
      "use": { "browserName": "chromium" }
    },
    {
      "name": "firefox",
      "use": { "browserName": "firefox" }
    },
    {
      "name": "webkit",
      "use": { "browserName": "webkit" }
    }
  ]
}
```

**Acceptance Criteria**:
- [ ] Playwright installs browsers automatically
- [ ] Tests run in headless mode by default
- [ ] Video recording works for failures
- [ ] Cross-browser testing supported

---

### TEST-006: Page Object Model Implementation

**Objective**: Implement Page Objects for maintainable E2E tests.

**Implementation**:

```csharp
// tests/ClimaSite.E2E/PageObjects/BasePage.cs
using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    public async Task WaitForLoadAsync()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task ClickAsync(string selector)
    {
        await Page.ClickAsync(selector);
    }

    protected async Task FillAsync(string selector, string value)
    {
        await Page.FillAsync(selector, value);
    }

    protected async Task<string> GetTextAsync(string selector)
    {
        return await Page.TextContentAsync(selector) ?? string.Empty;
    }
}

// tests/ClimaSite.E2E/PageObjects/LoginPage.cs
namespace ClimaSite.E2E.PageObjects;

public class LoginPage : BasePage
{
    private const string EmailInput = "[data-testid='login-email']";
    private const string PasswordInput = "[data-testid='login-password']";
    private const string SubmitButton = "[data-testid='login-submit']";
    private const string ErrorMessage = "[data-testid='login-error']";

    public LoginPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/login");
        await WaitForLoadAsync();
    }

    public async Task LoginAsync(string email, string password)
    {
        await FillAsync(EmailInput, email);
        await FillAsync(PasswordInput, password);
        await ClickAsync(SubmitButton);
    }

    public async Task<string> GetErrorMessageAsync()
    {
        return await GetTextAsync(ErrorMessage);
    }

    public async Task<bool> IsLoggedInAsync()
    {
        await Page.WaitForURLAsync("**/dashboard", new() { Timeout = 5000 });
        return Page.Url.Contains("dashboard");
    }
}

// tests/ClimaSite.E2E/PageObjects/ProductPage.cs
namespace ClimaSite.E2E.PageObjects;

public class ProductPage : BasePage
{
    private const string ProductTitle = "[data-testid='product-title']";
    private const string ProductPrice = "[data-testid='product-price']";
    private const string AddToCartButton = "[data-testid='add-to-cart']";
    private const string QuantityInput = "[data-testid='quantity-input']";
    private const string CartNotification = "[data-testid='cart-notification']";

    public ProductPage(IPage page) : base(page) { }

    public async Task NavigateAsync(Guid productId)
    {
        await Page.GotoAsync($"/products/{productId}");
        await WaitForLoadAsync();
    }

    public async Task<string> GetProductTitleAsync()
    {
        return await GetTextAsync(ProductTitle);
    }

    public async Task<decimal> GetProductPriceAsync()
    {
        var priceText = await GetTextAsync(ProductPrice);
        return decimal.Parse(priceText.Replace("$", "").Replace(",", ""));
    }

    public async Task AddToCartAsync(int quantity = 1)
    {
        if (quantity > 1)
        {
            await FillAsync(QuantityInput, quantity.ToString());
        }
        await ClickAsync(AddToCartButton);
        await Page.WaitForSelectorAsync(CartNotification);
    }
}

// tests/ClimaSite.E2E/PageObjects/CheckoutPage.cs
namespace ClimaSite.E2E.PageObjects;

public class CheckoutPage : BasePage
{
    private const string ShippingAddressForm = "[data-testid='shipping-form']";
    private const string StreetInput = "[data-testid='street-input']";
    private const string CityInput = "[data-testid='city-input']";
    private const string PostalCodeInput = "[data-testid='postal-code-input']";
    private const string PaymentSection = "[data-testid='payment-section']";
    private const string PlaceOrderButton = "[data-testid='place-order']";
    private const string OrderConfirmation = "[data-testid='order-confirmation']";
    private const string OrderNumber = "[data-testid='order-number']";

    public CheckoutPage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/checkout");
        await WaitForLoadAsync();
    }

    public async Task FillShippingAddressAsync(string street, string city, string postalCode)
    {
        await FillAsync(StreetInput, street);
        await FillAsync(CityInput, city);
        await FillAsync(PostalCodeInput, postalCode);
    }

    public async Task PlaceOrderAsync()
    {
        await ClickAsync(PlaceOrderButton);
        await Page.WaitForSelectorAsync(OrderConfirmation, new() { Timeout = 10000 });
    }

    public async Task<string> GetOrderNumberAsync()
    {
        return await GetTextAsync(OrderNumber);
    }
}
```

**Acceptance Criteria**:
- [ ] Page objects encapsulate UI interactions
- [ ] Selectors use data-testid attributes
- [ ] Common operations are abstracted
- [ ] Page objects are reusable across tests

---

### TEST-007: Integration Test Base Class

**Objective**: Create base class for API integration tests with shared setup.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Infrastructure/IntegrationTestBase.cs
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Infrastructure;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly ClimaSiteDbContext DbContext;
    protected readonly DatabaseCleaner Cleaner;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ClimaSiteDbContext>();
        Cleaner = new DatabaseCleaner(DbContext);
    }

    public virtual async Task InitializeAsync()
    {
        await Cleaner.CleanAsync();
    }

    public virtual Task DisposeAsync()
    {
        Scope.Dispose();
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<string> AuthenticateAsync(string email = "test@test.com", string password = "Password123!")
    {
        // Register user
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        // Login and get token
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result!.Token);

        return result.Token;
    }

    protected async Task<Guid> CreateTestProductAsync(decimal price = 999.99m)
    {
        var product = new Product
        {
            Name = "Test AC Unit",
            Description = "Test Description",
            Price = price,
            StockQuantity = 100,
            Sku = $"TEST-{Guid.NewGuid():N}"[..20]
        };

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return product.Id;
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
```

**Acceptance Criteria**:
- [ ] Base class handles common setup/teardown
- [ ] Database is cleaned before each test
- [ ] Authentication helper methods work
- [ ] Test data creation helpers available

---

### TEST-008: Unit Test Patterns

**Objective**: Establish unit testing patterns with Arrange-Act-Assert.

**Implementation**:

```csharp
// tests/ClimaSite.Core.Tests/Services/PricingServiceTests.cs
using FluentAssertions;
using Moq;

namespace ClimaSite.Core.Tests.Services;

public class PricingServiceTests
{
    private readonly Mock<IDiscountRepository> _discountRepoMock;
    private readonly Mock<ITaxCalculator> _taxCalculatorMock;
    private readonly PricingService _sut; // System Under Test

    public PricingServiceTests()
    {
        _discountRepoMock = new Mock<IDiscountRepository>();
        _taxCalculatorMock = new Mock<ITaxCalculator>();
        _sut = new PricingService(_discountRepoMock.Object, _taxCalculatorMock.Object);
    }

    [Fact]
    public async Task CalculateTotal_WithNoDiscount_ReturnsBasePrice()
    {
        // Arrange
        var items = new List<CartItem>
        {
            new() { ProductId = Guid.NewGuid(), UnitPrice = 100m, Quantity = 2 },
            new() { ProductId = Guid.NewGuid(), UnitPrice = 50m, Quantity = 1 }
        };
        _taxCalculatorMock.Setup(x => x.CalculateTax(It.IsAny<decimal>())).Returns(0m);

        // Act
        var result = await _sut.CalculateTotalAsync(items);

        // Assert
        result.Should().Be(250m);
    }

    [Fact]
    public async Task CalculateTotal_WithPercentageDiscount_AppliesDiscount()
    {
        // Arrange
        var items = new List<CartItem>
        {
            new() { ProductId = Guid.NewGuid(), UnitPrice = 100m, Quantity = 1 }
        };
        var discount = new Discount { Type = DiscountType.Percentage, Value = 10 };
        _discountRepoMock.Setup(x => x.GetActiveDiscountAsync(It.IsAny<string>()))
            .ReturnsAsync(discount);
        _taxCalculatorMock.Setup(x => x.CalculateTax(It.IsAny<decimal>())).Returns(0m);

        // Act
        var result = await _sut.CalculateTotalAsync(items, "SUMMER10");

        // Assert
        result.Should().Be(90m);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 20)]
    [InlineData(500, 100)]
    public async Task CalculateTax_WithDifferentAmounts_ReturnsCorrectTax(decimal amount, decimal expectedTax)
    {
        // Arrange
        _taxCalculatorMock.Setup(x => x.CalculateTax(amount)).Returns(expectedTax);

        // Act
        var result = await _sut.CalculateTaxAsync(amount);

        // Assert
        result.Should().Be(expectedTax);
    }

    [Fact]
    public async Task CalculateTotal_WithNullItems_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = async () => await _sut.CalculateTotalAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*items*");
    }
}

// tests/ClimaSite.Core.Tests/Entities/OrderTests.cs
namespace ClimaSite.Core.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void AddItem_WhenCalled_IncreasesItemCount()
    {
        // Arrange
        var order = new Order();
        var item = new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100 };

        // Act
        order.AddItem(item);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items.Should().Contain(item);
    }

    [Fact]
    public void CalculateTotal_WithMultipleItems_SumsCorrectly()
    {
        // Arrange
        var order = new Order();
        order.AddItem(new OrderItem { Quantity = 2, UnitPrice = 100 });
        order.AddItem(new OrderItem { Quantity = 1, UnitPrice = 50 });

        // Act
        var total = order.CalculateTotal();

        // Assert
        total.Should().Be(250m);
    }

    [Fact]
    public void SetStatus_ToShipped_SetsShippedDate()
    {
        // Arrange
        var order = new Order { Status = OrderStatus.Processing };

        // Act
        order.SetStatus(OrderStatus.Shipped);

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
```

**Acceptance Criteria**:
- [ ] Tests follow AAA pattern
- [ ] FluentAssertions used for readability
- [ ] Moq used appropriately for dependencies
- [ ] Theory tests cover multiple scenarios

---

### TEST-009: Integration Test Patterns

**Objective**: Establish integration testing patterns for API endpoints.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Controllers/ProductsControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class ProductsControllerTests : IntegrationTestBase
{
    public ProductsControllerTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProducts_ReturnsOk_WithProductList()
    {
        // Arrange
        await CreateTestProductAsync();
        await CreateTestProductAsync();

        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetProduct_WithValidId_ReturnsProduct()
    {
        // Arrange
        var productId = await CreateTestProductAsync(price: 1299.99m);

        // Act
        var response = await Client.GetAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Price.Should().Be(1299.99m);
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var product = new CreateProductRequest
        {
            Name = "New AC Unit",
            Description = "High efficiency cooling",
            Price = 1499.99m,
            StockQuantity = 50,
            Sku = "NEW-AC-001"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/products", product);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var product = new CreateProductRequest { Name = "Test" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/products", product);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchProducts_WithQuery_ReturnsFilteredResults()
    {
        // Arrange
        await DbContext.Products.AddRangeAsync(
            new Product { Name = "Split AC 12000 BTU", Price = 999, Sku = "SPLIT-12" },
            new Product { Name = "Window AC 9000 BTU", Price = 599, Sku = "WIN-9" },
            new Product { Name = "Portable Heater", Price = 299, Sku = "HEAT-1" }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products?search=AC");

        // Assert
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().HaveCount(2);
        products.Should().OnlyContain(p => p.Name.Contains("AC"));
    }

    private async Task AuthenticateAsAdminAsync()
    {
        // Create admin user directly in DB for testing
        var adminUser = new User
        {
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin
        };
        DbContext.Users.Add(adminUser);
        await DbContext.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@test.com",
            password = "Admin123!"
        });

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result!.Token);
    }
}
```

**Acceptance Criteria**:
- [ ] Tests cover all HTTP methods
- [ ] Authentication scenarios tested
- [ ] Error responses validated
- [ ] Database state verified

---

### TEST-010: E2E Test Patterns (NO MOCKING)

**Objective**: Establish E2E testing patterns with real data creation.

**Implementation**:

```csharp
// tests/ClimaSite.E2E/Tests/Checkout/CheckoutFlowTests.cs
using Microsoft.Playwright;
using FluentAssertions;

namespace ClimaSite.E2E.Tests.Checkout;

[Collection("Playwright")]
public class CheckoutFlowTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public CheckoutFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        // Cleanup test data using correlation ID
        await CleanupTestDataAsync();
        await _page.Context.CloseAsync();
    }

    /// <summary>
    /// Complete checkout flow with REAL user, REAL products, REAL order.
    /// NO MOCKING - all data is created through API calls.
    /// </summary>
    [Fact]
    public async Task CompleteCheckout_WithValidData_CreatesOrder()
    {
        // Arrange - Create REAL test data
        var user = await _dataFactory.CreateUserAsync();
        var product1 = await _dataFactory.CreateProductAsync(name: "Test Split AC", price: 999.99m);
        var product2 = await _dataFactory.CreateProductAsync(name: "Test Window AC", price: 599.99m);

        // Act - Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);
        (await loginPage.IsLoggedInAsync()).Should().BeTrue();

        // Add products to cart
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product1.Id);
        await productPage.AddToCartAsync(quantity: 1);

        await productPage.NavigateAsync(product2.Id);
        await productPage.AddToCartAsync(quantity: 2);

        // Proceed to checkout
        var cartPage = new CartPage(_page);
        await cartPage.NavigateAsync();
        var cartTotal = await cartPage.GetTotalAsync();
        cartTotal.Should().Be(999.99m + (599.99m * 2));

        await cartPage.ProceedToCheckoutAsync();

        // Complete checkout
        var checkoutPage = new CheckoutPage(_page);
        await checkoutPage.FillShippingAddressAsync(
            street: "123 Test Street",
            city: "Sofia",
            postalCode: "1000"
        );
        await checkoutPage.PlaceOrderAsync();

        // Assert - Verify order was created
        var orderNumber = await checkoutPage.GetOrderNumberAsync();
        orderNumber.Should().NotBeNullOrEmpty();
        orderNumber.Should().StartWith("ORD-");

        // Verify order exists in database through API
        _fixture.ApiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Token);

        var orderResponse = await _fixture.ApiClient.GetAsync($"/api/orders/{orderNumber}");
        orderResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Checkout_WithInsufficientStock_ShowsError()
    {
        // Arrange - Create product with limited stock
        var user = await _dataFactory.CreateUserAsync();
        var product = await _dataFactory.CreateProductAsync(
            name: "Limited Stock AC",
            price: 1299.99m,
            stock: 1  // Only 1 in stock
        );

        // Login
        var loginPage = new LoginPage(_page);
        await loginPage.NavigateAsync();
        await loginPage.LoginAsync(user.Email, user.Password);

        // Try to add more than available
        var productPage = new ProductPage(_page);
        await productPage.NavigateAsync(product.Id);
        await productPage.AddToCartAsync(quantity: 5);

        // Assert - Should show stock warning
        var errorMessage = await _page.TextContentAsync("[data-testid='stock-warning']");
        errorMessage.Should().Contain("Only 1 available");
    }

    [Fact]
    public async Task Checkout_AsGuest_RequiresLogin()
    {
        // Arrange - Create product without logging in
        var adminUser = await _dataFactory.CreateAdminUserAsync();
        _fixture.ApiClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminUser.Token);

        var product = await _dataFactory.CreateProductAsync();

        // Act - Try to checkout without login
        await _page.GotoAsync($"/products/{product.Id}");
        await _page.ClickAsync("[data-testid='add-to-cart']");
        await _page.GotoAsync("/checkout");

        // Assert - Should redirect to login
        await _page.WaitForURLAsync("**/login?returnUrl=*checkout*");
        _page.Url.Should().Contain("/login");
    }

    private async Task CleanupTestDataAsync()
    {
        try
        {
            var response = await _fixture.ApiClient.DeleteAsync(
                $"/api/test/cleanup/{_dataFactory.CorrelationId}");
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

// tests/ClimaSite.E2E/Tests/Products/ProductBrowsingTests.cs
namespace ClimaSite.E2E.Tests.Products;

[Collection("Playwright")]
public class ProductBrowsingTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = default!;
    private TestDataFactory _dataFactory = default!;

    public ProductBrowsingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _dataFactory = _fixture.CreateDataFactory();
    }

    public async Task DisposeAsync()
    {
        await _page.Context.CloseAsync();
    }

    [Fact]
    public async Task ProductList_DisplaysCreatedProducts()
    {
        // Arrange - Create REAL products
        var product1 = await _dataFactory.CreateProductAsync(name: "Premium AC Unit", price: 1999.99m);
        var product2 = await _dataFactory.CreateProductAsync(name: "Budget AC Unit", price: 499.99m);

        // Act
        await _page.GotoAsync("/products");
        await _page.WaitForSelectorAsync("[data-testid='product-card']");

        // Assert
        var productCards = await _page.QuerySelectorAllAsync("[data-testid='product-card']");
        productCards.Should().HaveCountGreaterOrEqualTo(2);

        var pageContent = await _page.ContentAsync();
        pageContent.Should().Contain("Premium AC Unit");
        pageContent.Should().Contain("Budget AC Unit");
    }

    [Fact]
    public async Task ProductFilter_ByPriceRange_FiltersCorrectly()
    {
        // Arrange
        await _dataFactory.CreateProductAsync(name: "Cheap AC", price: 299.99m);
        await _dataFactory.CreateProductAsync(name: "Mid AC", price: 799.99m);
        await _dataFactory.CreateProductAsync(name: "Premium AC", price: 1999.99m);

        // Act - Filter by price range
        await _page.GotoAsync("/products");
        await _page.FillAsync("[data-testid='min-price']", "500");
        await _page.FillAsync("[data-testid='max-price']", "1000");
        await _page.ClickAsync("[data-testid='apply-filter']");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var products = await _page.QuerySelectorAllAsync("[data-testid='product-card']");
        products.Should().HaveCount(1);

        var productName = await _page.TextContentAsync("[data-testid='product-card'] [data-testid='product-name']");
        productName.Should().Contain("Mid AC");
    }

    [Fact]
    public async Task ProductSearch_FindsMatchingProducts()
    {
        // Arrange
        await _dataFactory.CreateProductAsync(name: "Inverter Split AC 12000 BTU");
        await _dataFactory.CreateProductAsync(name: "Window AC 9000 BTU");
        await _dataFactory.CreateProductAsync(name: "Portable Heater");

        // Act
        await _page.GotoAsync("/products");
        await _page.FillAsync("[data-testid='search-input']", "Inverter");
        await _page.PressAsync("[data-testid='search-input']", "Enter");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var products = await _page.QuerySelectorAllAsync("[data-testid='product-card']");
        products.Should().HaveCount(1);
    }
}
```

**Acceptance Criteria**:
- [ ] Tests create real data via TestDataFactory
- [ ] No mocking of API or database
- [ ] Tests are self-contained
- [ ] Cleanup runs after each test

---

### TEST-011: Frontend Unit Tests (Angular)

**Objective**: Set up Jasmine/Karma tests for Angular components.

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/components/product-card/product-card.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductCardComponent } from './product-card.component';
import { By } from '@angular/platform-browser';

describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;

  const mockProduct = {
    id: '123',
    name: 'Test AC Unit',
    description: 'High efficiency cooling',
    price: 999.99,
    imageUrl: '/images/test-ac.jpg',
    stockQuantity: 10
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;
    component.product = mockProduct;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display product name', () => {
    const nameElement = fixture.debugElement.query(By.css('[data-testid="product-name"]'));
    expect(nameElement.nativeElement.textContent).toContain('Test AC Unit');
  });

  it('should display formatted price', () => {
    const priceElement = fixture.debugElement.query(By.css('[data-testid="product-price"]'));
    expect(priceElement.nativeElement.textContent).toContain('$999.99');
  });

  it('should emit addToCart event when button clicked', () => {
    spyOn(component.addToCart, 'emit');

    const button = fixture.debugElement.query(By.css('[data-testid="add-to-cart-btn"]'));
    button.nativeElement.click();

    expect(component.addToCart.emit).toHaveBeenCalledWith(mockProduct);
  });

  it('should show out of stock badge when stockQuantity is 0', () => {
    component.product = { ...mockProduct, stockQuantity: 0 };
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('[data-testid="out-of-stock"]'));
    expect(badge).toBeTruthy();
  });

  it('should disable add to cart button when out of stock', () => {
    component.product = { ...mockProduct, stockQuantity: 0 };
    fixture.detectChanges();

    const button = fixture.debugElement.query(By.css('[data-testid="add-to-cart-btn"]'));
    expect(button.nativeElement.disabled).toBeTrue();
  });
});

// src/ClimaSite.Web/src/app/services/cart.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CartService } from './cart.service';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CartService]
    });
    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should add item to cart', () => {
    const productId = '123';
    const quantity = 2;

    service.addItem(productId, quantity).subscribe(response => {
      expect(response.success).toBeTrue();
    });

    const req = httpMock.expectOne('/api/cart/items');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId, quantity });
    req.flush({ success: true });
  });

  it('should get cart items', () => {
    const mockItems = [
      { productId: '1', quantity: 2, unitPrice: 100 },
      { productId: '2', quantity: 1, unitPrice: 50 }
    ];

    service.getItems().subscribe(items => {
      expect(items.length).toBe(2);
      expect(items[0].productId).toBe('1');
    });

    const req = httpMock.expectOne('/api/cart');
    expect(req.request.method).toBe('GET');
    req.flush(mockItems);
  });

  it('should calculate cart total', () => {
    const mockCart = {
      items: [
        { productId: '1', quantity: 2, unitPrice: 100 },
        { productId: '2', quantity: 1, unitPrice: 50 }
      ],
      total: 250
    };

    service.getCart().subscribe(cart => {
      expect(cart.total).toBe(250);
    });

    const req = httpMock.expectOne('/api/cart');
    req.flush(mockCart);
  });

  it('should remove item from cart', () => {
    const productId = '123';

    service.removeItem(productId).subscribe();

    const req = httpMock.expectOne(`/api/cart/items/${productId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  it('should update item quantity', () => {
    const productId = '123';
    const newQuantity = 5;

    service.updateQuantity(productId, newQuantity).subscribe();

    const req = httpMock.expectOne(`/api/cart/items/${productId}`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ quantity: newQuantity });
    req.flush({});
  });
});

// src/ClimaSite.Web/karma.conf.js
module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: {
      jasmine: {},
      clearContext: false
    },
    jasmineHtmlReporter: {
      suppressAll: true
    },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage'),
      subdir: '.',
      reporters: [
        { type: 'html' },
        { type: 'lcovonly' },
        { type: 'text-summary' }
      ],
      check: {
        global: {
          statements: 80,
          branches: 75,
          functions: 80,
          lines: 80
        }
      }
    },
    reporters: ['progress', 'kjhtml', 'coverage'],
    browsers: ['ChromeHeadless'],
    restartOnFileChange: true
  });
};
```

**Acceptance Criteria**:
- [ ] Component tests pass
- [ ] Service tests with HttpTestingController
- [ ] Code coverage thresholds enforced
- [ ] Tests run in headless Chrome

---

### TEST-012: Test Data Seeding

**Objective**: Create database seeding utilities for test environments.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Infrastructure/TestDataSeeder.cs
using Bogus;

namespace ClimaSite.Api.Tests.Infrastructure;

public class TestDataSeeder
{
    private readonly ClimaSiteDbContext _context;
    private readonly Faker _faker = new();

    public TestDataSeeder(ClimaSiteDbContext context)
    {
        _context = context;
    }

    public async Task SeedCategoriesAsync()
    {
        var categories = new List<Category>
        {
            new() { Name = "Air Conditioners", Slug = "air-conditioners" },
            new() { Name = "Heating Systems", Slug = "heating-systems" },
            new() { Name = "Ventilation", Slug = "ventilation" },
            new() { Name = "Accessories", Slug = "accessories" }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Product>> SeedProductsAsync(int count = 10, Guid? categoryId = null)
    {
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => f.Random.Decimal(100, 5000))
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 100))
            .RuleFor(p => p.Sku, f => f.Commerce.Ean13())
            .RuleFor(p => p.CategoryId, categoryId ?? Guid.NewGuid());

        var products = productFaker.Generate(count);
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        return products;
    }

    public async Task<User> SeedUserAsync(string email = "test@test.com", UserRole role = UserRole.Customer)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task SeedCompleteScenarioAsync()
    {
        await SeedCategoriesAsync();

        var categories = await _context.Categories.ToListAsync();
        foreach (var category in categories)
        {
            await SeedProductsAsync(5, category.Id);
        }

        await SeedUserAsync("customer@test.com", UserRole.Customer);
        await SeedUserAsync("admin@test.com", UserRole.Admin);
    }
}
```

**Acceptance Criteria**:
- [ ] Seeder creates realistic test data
- [ ] Categories and products are linked
- [ ] Users with different roles created
- [ ] Bogus library used for variety

---

### TEST-013: Continuous Integration Setup

**Objective**: Configure GitHub Actions for automated testing.

**Implementation**:

```yaml
# .github/workflows/test.yml
name: Test Suite

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '10.0.x'
  NODE_VERSION: '20'

jobs:
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run Core Unit Tests
        run: dotnet test tests/ClimaSite.Core.Tests --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: ./coverage/**/coverage.cobertura.xml
          flags: unit-tests

  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
          POSTGRES_DB: climasite_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run Integration Tests
        run: dotnet test tests/ClimaSite.Api.Tests --no-build --verbosity normal
        env:
          DATABASE_URL: "Host=localhost;Database=climasite_test;Username=test;Password=test"

  frontend-tests:
    name: Frontend Unit Tests
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: src/ClimaSite.Web/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: src/ClimaSite.Web

      - name: Run tests
        run: npm run test:ci
        working-directory: src/ClimaSite.Web

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: src/ClimaSite.Web/coverage/lcov.info
          flags: frontend-tests

  e2e-tests:
    name: E2E Tests
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
          POSTGRES_DB: climasite_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: src/ClimaSite.Web/package-lock.json

      - name: Install Playwright
        run: |
          dotnet tool install --global Microsoft.Playwright.CLI
          playwright install chromium

      - name: Build Backend
        run: dotnet build src/ClimaSite.Api

      - name: Build Frontend
        run: |
          cd src/ClimaSite.Web
          npm ci
          npm run build

      - name: Start Backend
        run: |
          dotnet run --project src/ClimaSite.Api &
          sleep 10
        env:
          DATABASE_URL: "Host=localhost;Database=climasite_test;Username=test;Password=test"
          ASPNETCORE_ENVIRONMENT: Testing

      - name: Start Frontend
        run: |
          cd src/ClimaSite.Web
          npm run serve:test &
          sleep 10

      - name: Run E2E Tests
        run: dotnet test tests/ClimaSite.E2E --verbosity normal
        env:
          E2E_BASE_URL: http://localhost:4200
          E2E_API_URL: http://localhost:5000
          E2E_HEADLESS: true

      - name: Upload test artifacts
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: e2e-artifacts
          path: |
            tests/ClimaSite.E2E/test-results/
            tests/ClimaSite.E2E/videos/

  test-summary:
    name: Test Summary
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests, frontend-tests, e2e-tests]
    if: always()

    steps:
      - name: Check test results
        run: |
          if [ "${{ needs.unit-tests.result }}" != "success" ] || \
             [ "${{ needs.integration-tests.result }}" != "success" ] || \
             [ "${{ needs.frontend-tests.result }}" != "success" ] || \
             [ "${{ needs.e2e-tests.result }}" != "success" ]; then
            echo "One or more test jobs failed"
            exit 1
          fi
          echo "All tests passed!"
```

**Acceptance Criteria**:
- [ ] All test types run in CI
- [ ] PostgreSQL service available for tests
- [ ] Code coverage uploaded to Codecov
- [ ] E2E test artifacts saved on failure

---

### TEST-014: Test Environment Configuration

**Objective**: Configure test-specific application settings.

**Implementation**:

```json
// src/ClimaSite.Api/appsettings.Testing.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Database": {
    "CommandTimeout": 30,
    "EnableSensitiveDataLogging": true
  },
  "Authentication": {
    "JwtSecret": "test-secret-key-for-testing-only-minimum-32-characters",
    "TokenExpirationMinutes": 60
  },
  "Features": {
    "EnableTestEndpoints": true,
    "EmailSending": false,
    "PaymentProcessing": false
  },
  "TestSettings": {
    "AdminElevationSecret": "test-admin-secret"
  }
}
```

```csharp
// src/ClimaSite.Api/Controllers/TestController.cs
#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly ClimaSiteDbContext _context;
    private readonly IConfiguration _configuration;

    public TestController(ClimaSiteDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Cleanup test data by correlation ID.
    /// Only available in Testing environment.
    /// </summary>
    [HttpDelete("cleanup/{correlationId:guid}")]
    public async Task<IActionResult> Cleanup(Guid correlationId)
    {
        if (!IsTestEnvironment())
            return NotFound();

        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM orders WHERE correlation_id = {0}", correlationId);
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM products WHERE correlation_id = {0}", correlationId);
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM users WHERE correlation_id = {0}", correlationId);

        return NoContent();
    }

    /// <summary>
    /// Elevate user to admin for testing.
    /// Only available in Testing environment.
    /// </summary>
    [HttpPost("elevate-admin")]
    public async Task<IActionResult> ElevateToAdmin([FromBody] ElevateRequest request)
    {
        if (!IsTestEnvironment())
            return NotFound();

        var expectedSecret = _configuration["TestSettings:AdminElevationSecret"];
        if (request.TestSecret != expectedSecret)
            return Unauthorized();

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return NotFound();

        user.Role = UserRole.Admin;
        await _context.SaveChangesAsync();

        return Ok();
    }

    private bool IsTestEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env == "Testing" || env == "Development";
    }
}

public record ElevateRequest(Guid UserId, string TestSecret);
#endif
```

**Acceptance Criteria**:
- [ ] Test endpoints only available in test environment
- [ ] Sensitive features disabled in tests
- [ ] Test secrets properly configured
- [ ] Cleanup endpoints work correctly

---

### TEST-015: Performance Test Setup

**Objective**: Add basic performance testing capabilities.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Performance/PerformanceTests.cs
using System.Diagnostics;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Performance;

public class PerformanceTests : IntegrationTestBase
{
    public PerformanceTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProducts_Under100Items_CompletesWithin500ms()
    {
        // Arrange
        var seeder = new TestDataSeeder(DbContext);
        await seeder.SeedProductsAsync(100);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await Client.GetAsync("/api/products");
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task CreateOrder_CompletesWithin1000ms()
    {
        // Arrange
        await AuthenticateAsync();
        var productId = await CreateTestProductAsync();

        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId,
            quantity = 1
        });

        var orderRequest = new
        {
            shippingAddress = new { street = "123 Test", city = "Sofia", postalCode = "1000" }
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);
        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task ConcurrentProductRequests_AllSucceed()
    {
        // Arrange
        await new TestDataSeeder(DbContext).SeedProductsAsync(10);
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - 50 concurrent requests
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Client.GetAsync("/api/products"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
    }
}
```

**Acceptance Criteria**:
- [ ] Response time thresholds defined
- [ ] Concurrent request tests pass
- [ ] Performance regressions detectable

---

### TEST-016: Snapshot Testing

**Objective**: Implement snapshot testing for API responses.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Snapshots/ProductSnapshotTests.cs
using Verify;
using VerifyXunit;

namespace ClimaSite.Api.Tests.Snapshots;

[UsesVerify]
public class ProductSnapshotTests : IntegrationTestBase
{
    public ProductSnapshotTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProduct_ReturnsExpectedStructure()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Name = "Test AC Unit",
            Description = "High efficiency cooling",
            Price = 999.99m,
            StockQuantity = 50,
            Sku = "TEST-AC-001",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/products/{product.Id}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Compare against snapshot
        await Verify(content);
    }
}
```

**Acceptance Criteria**:
- [ ] Snapshot files generated
- [ ] Changes detected on API modifications
- [ ] Snapshots version controlled

---

### TEST-017: Test Reporting

**Objective**: Configure detailed test reporting and metrics.

**Implementation**:

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <CoverletOutput>./coverage/</CoverletOutput>
    <Threshold>80</Threshold>
    <ThresholdType>line,branch,method</ThresholdType>
  </PropertyGroup>
</Project>
```

```yaml
# Additional CI step for test reporting
- name: Generate Test Report
  if: always()
  run: |
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator \
      -reports:"**/coverage.cobertura.xml" \
      -targetdir:"coverage-report" \
      -reporttypes:"Html;Badges"

- name: Publish Test Report
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: test-report
    path: coverage-report/
```

**Acceptance Criteria**:
- [ ] HTML coverage reports generated
- [ ] Coverage badges available
- [ ] Test results visible in PR

---

### TEST-018: Contract Testing

**Objective**: Implement API contract testing.

**Implementation**:

```csharp
// tests/ClimaSite.Api.Tests/Contracts/ProductContractTests.cs
using System.Text.Json;
using JsonSchema.Net;

namespace ClimaSite.Api.Tests.Contracts;

public class ProductContractTests : IntegrationTestBase
{
    public ProductContractTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProducts_MatchesContract()
    {
        // Arrange
        await CreateTestProductAsync();
        var schema = JsonSchema.FromFile("Contracts/product-list.schema.json");

        // Act
        var response = await Client.GetAsync("/api/products");
        var json = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(json);

        // Assert
        var result = schema.Evaluate(document.RootElement);
        result.IsValid.Should().BeTrue(result.Message);
    }
}
```

```json
// tests/ClimaSite.Api.Tests/Contracts/product-list.schema.json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["id", "name", "price"],
        "properties": {
          "id": { "type": "string", "format": "uuid" },
          "name": { "type": "string" },
          "description": { "type": "string" },
          "price": { "type": "number", "minimum": 0 },
          "stockQuantity": { "type": "integer", "minimum": 0 }
        }
      }
    },
    "totalCount": { "type": "integer" },
    "page": { "type": "integer" },
    "pageSize": { "type": "integer" }
  },
  "required": ["items", "totalCount"]
}
```

**Acceptance Criteria**:
- [ ] JSON schemas defined for all endpoints
- [ ] Contract tests validate responses
- [ ] Breaking changes detected

---

### TEST-019: Test Documentation

**Objective**: Document testing conventions and best practices.

**Key Guidelines**:

1. **Naming Conventions**
   - Unit tests: `MethodName_StateUnderTest_ExpectedBehavior`
   - Integration tests: `Endpoint_Action_ExpectedResult`
   - E2E tests: `Feature_Scenario_ExpectedOutcome`

2. **Test Organization**
   - One test class per production class (unit tests)
   - One test class per controller (integration tests)
   - One test class per feature (E2E tests)

3. **Assertions**
   - Use FluentAssertions for readable assertions
   - One logical assertion per test
   - Use descriptive failure messages

4. **Test Data**
   - Use Bogus for random realistic data
   - Use constants for expected values
   - Clean up test data after E2E tests

**Acceptance Criteria**:
- [ ] Testing conventions documented
- [ ] Examples provided for each test type
- [ ] Review checklist created

---

### TEST-020: Test Maintenance Automation

**Objective**: Set up automation for test maintenance.

**Implementation**:

```yaml
# .github/workflows/test-maintenance.yml
name: Test Maintenance

on:
  schedule:
    - cron: '0 6 * * 1'  # Weekly on Monday
  workflow_dispatch:

jobs:
  update-snapshots:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Update Playwright browsers
        run: |
          dotnet tool install --global Microsoft.Playwright.CLI
          playwright install

      - name: Check for outdated test dependencies
        run: dotnet list tests/ClimaSite.Api.Tests package --outdated

  flaky-test-detection:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Run tests multiple times
        run: |
          for i in {1..5}; do
            dotnet test --no-build || echo "Run $i failed"
          done
```

**Acceptance Criteria**:
- [ ] Weekly test maintenance runs
- [ ] Flaky tests detected
- [ ] Outdated dependencies flagged

---

## 5. E2E Test Patterns Summary

### Pattern: Self-Contained Test with Real Data

```csharp
[Fact]
public async Task CompleteUserJourney_PurchaseProduct()
{
    // 1. CREATE REAL DATA - No mocking
    var user = await _dataFactory.CreateUserAsync();
    var product = await _dataFactory.CreateProductAsync();

    // 2. PERFORM REAL ACTIONS via UI
    await LoginAsync(user);
    await AddToCartAsync(product);
    await CompleteCheckoutAsync();

    // 3. VERIFY REAL RESULTS
    var order = await GetOrderFromApiAsync();
    order.Should().NotBeNull();
    order.Items.Should().Contain(i => i.ProductId == product.Id);
}
```

### Pattern: Database Cleanup

```csharp
public async Task DisposeAsync()
{
    // Clean by correlation ID - only removes THIS test's data
    await _apiClient.DeleteAsync($"/api/test/cleanup/{_correlationId}");
}
```

### Pattern: Test Isolation

Each test:
- Gets its own user account
- Creates its own products
- Has unique correlation ID for cleanup
- Cannot see or affect other tests' data

---

## 6. CI/CD Integration Summary

| Stage | Tests | Trigger | Duration |
|-------|-------|---------|----------|
| Unit Tests | ClimaSite.Core.Tests | Every push | ~30s |
| Integration Tests | ClimaSite.Api.Tests | Every push | ~2min |
| Frontend Tests | ClimaSite.Web.Tests | Every push | ~1min |
| E2E Tests | ClimaSite.E2E | After unit/integration pass | ~10min |

### Test Gates

- **PR Merge**: All tests must pass
- **Deploy to Staging**: Unit + Integration tests pass
- **Deploy to Production**: All tests including E2E pass

---

## 7. Test Examples Summary

### Unit Test Example

```csharp
[Fact]
public void CalculateDiscount_WithValidCode_AppliesDiscount()
{
    // Arrange
    var service = new PricingService(_mockRepo.Object);
    var amount = 100m;

    // Act
    var result = service.CalculateDiscount(amount, "SAVE10");

    // Assert
    result.Should().Be(90m);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task CreateProduct_AsAdmin_ReturnsCreated()
{
    // Arrange
    await AuthenticateAsAdminAsync();
    var product = new { name = "Test AC", price = 999.99m };

    // Act
    var response = await Client.PostAsJsonAsync("/api/admin/products", product);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### E2E Test Example

```csharp
[Fact]
public async Task UserCanPurchaseProduct()
{
    // Create REAL data
    var user = await _dataFactory.CreateUserAsync();
    var product = await _dataFactory.CreateProductAsync();

    // Perform REAL actions
    await _page.GotoAsync("/login");
    await _page.FillAsync("#email", user.Email);
    await _page.FillAsync("#password", user.Password);
    await _page.ClickAsync("#submit");

    await _page.GotoAsync($"/products/{product.Id}");
    await _page.ClickAsync("#add-to-cart");
    await _page.ClickAsync("#checkout");

    // Verify REAL result
    await _page.WaitForURLAsync("**/order-confirmation");
    var orderNumber = await _page.TextContentAsync("#order-number");
    orderNumber.Should().NotBeNullOrEmpty();
}
```

---

## 8. Task Summary

| Task ID | Description | Priority | Estimated Effort |
|---------|-------------|----------|------------------|
| TEST-001 | xUnit Test Project Setup | High | 4 hours |
| TEST-002 | TestWebApplicationFactory | High | 8 hours |
| TEST-003 | Database Cleanup Strategy | High | 4 hours |
| TEST-004 | Test Data Factory | High | 8 hours |
| TEST-005 | Playwright Configuration | High | 4 hours |
| TEST-006 | Page Object Model | Medium | 8 hours |
| TEST-007 | Integration Test Base Class | High | 4 hours |
| TEST-008 | Unit Test Patterns | Medium | 4 hours |
| TEST-009 | Integration Test Patterns | Medium | 8 hours |
| TEST-010 | E2E Test Patterns | High | 16 hours |
| TEST-011 | Frontend Unit Tests | Medium | 8 hours |
| TEST-012 | Test Data Seeding | Medium | 4 hours |
| TEST-013 | CI/CD Setup | High | 8 hours |
| TEST-014 | Test Environment Config | Medium | 4 hours |
| TEST-015 | Performance Tests | Low | 8 hours |
| TEST-016 | Snapshot Testing | Low | 4 hours |
| TEST-017 | Test Reporting | Medium | 4 hours |
| TEST-018 | Contract Testing | Low | 8 hours |
| TEST-019 | Test Documentation | Medium | 4 hours |
| TEST-020 | Test Maintenance | Low | 4 hours |

**Total Estimated Effort**: 124 hours

---

## Appendix A: Required NuGet Packages

```xml
<!-- Test Projects -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Bogus" Version="35.*" />
<PackageReference Include="coverlet.collector" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
<PackageReference Include="Microsoft.Playwright" Version="1.*" />
<PackageReference Include="Verify.Xunit" Version="24.*" />
```

## Appendix B: npm Packages (Frontend)

```json
{
  "devDependencies": {
    "@angular-devkit/build-angular": "^18.0.0",
    "jasmine-core": "~5.1.0",
    "karma": "~6.4.0",
    "karma-chrome-launcher": "~3.2.0",
    "karma-coverage": "~2.2.0",
    "karma-jasmine": "~5.1.0",
    "karma-jasmine-html-reporter": "~2.1.0"
  }
}
```
