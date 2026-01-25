# E2E TESTING

Playwright tests with REAL data. NO MOCKING.

## CRITICAL RULES

1. **NO MOCKING** - Tests use real API calls
2. **Self-contained** - Each test creates its own data
3. **Cleanup** - Use correlation IDs for isolation
4. **data-testid** - Only selector strategy allowed

## TEST STRUCTURE

```
Tests/
├── Authentication/    # Login, register, password
├── Cart/             # Add, update, remove items
├── Checkout/         # Shipping, payment, confirm
├── Products/         # Browse, filter, search
├── Orders/           # History, details
├── Accessibility/    # WCAG compliance (axe-core)
├── Admin/            # Dashboard, CRUD
└── Journeys/         # Multi-step user flows
```

## TEST DATA FACTORY

```csharp
var factory = new TestDataFactory(apiClient);

// Creates REAL user via /api/auth/register
var user = await factory.CreateUserAsync();

// Creates REAL product via /api/admin/products
var product = await factory.CreateProductAsync(
    name: "Test AC",
    price: 599.99m
);

// Cleanup all data created by THIS factory
await factory.CleanupAsync();
```

## PAGE OBJECT MODEL

```csharp
public class CartPage : BasePage
{
    public async Task AddItemAsync(string productId) { }
    public async Task<decimal> GetTotalAsync() { }
    public async Task ProceedToCheckoutAsync() { }
}
```

## TEST LIFECYCLE

```csharp
[Collection("Playwright")]
public class CheckoutTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _factory = _fixture.CreateDataFactory();
    }
    
    public async Task DisposeAsync()
    {
        await _factory.CleanupAsync();  // ALWAYS cleanup
    }
}
```

## COMMANDS

```bash
npx playwright test                    # Run all
npx playwright test --ui               # Interactive mode
npx playwright test -g "checkout"      # Pattern match
```
