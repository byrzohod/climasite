# ClimaSite - Comprehensive E2E Testing Plan

## Overview

This plan covers all E2E testing scenarios for ClimaSite using Playwright with C#. All tests follow the project's critical rules:
- **NO MOCKING** - Tests use REAL data, REAL API calls, REAL database
- **Self-contained** - Each test creates its own test data
- **Database cleanup** - Tests clean up after themselves using correlation IDs
- **Test isolation** - Tests do not depend on other tests or pre-existing data

---

## Test Infrastructure

### TestDataFactory Methods Required
```csharp
- CreateUserAsync() - Creates a real user via API
- CreateProductAsync() - Creates a real product via API
- CreateCategoryAsync() - Creates a category via API
- CreateOrderAsync() - Creates an order via API
- CleanupAsync() - Cleans up all test data created
```

### Page Objects Required
```csharp
- HomePage
- LoginPage
- RegisterPage
- ProductListPage
- ProductDetailPage
- CartPage
- CheckoutPage
- AccountPage
- OrdersPage
- HeaderComponent (for mega menu, user menu)
```

---

## Task Index

| Task ID | Description | Priority | Status |
|---------|-------------|----------|--------|
| **Authentication & User Menu** |
| E2E-001 | User can register new account | Critical | Pending |
| E2E-002 | User can login with valid credentials | Critical | Pending |
| E2E-003 | User sees dropdown menu when logged in | Critical | Pending |
| E2E-004 | User can access account from dropdown | Critical | Pending |
| E2E-005 | User can access orders from dropdown | Critical | Pending |
| E2E-006 | User can logout from dropdown | Critical | Pending |
| E2E-007 | Login link shows when not authenticated | Critical | Pending |
| **Navigation & Mega Menu** |
| E2E-010 | Main navigation links work correctly | High | Pending |
| E2E-011 | Mega menu opens on hover/click | High | Pending |
| E2E-012 | Category navigation works in mega menu | High | Pending |
| E2E-013 | Subcategory links navigate correctly | High | Pending |
| E2E-014 | Mobile menu opens and closes | High | Pending |
| E2E-015 | Mobile menu navigation works | High | Pending |
| **Product Browsing** |
| E2E-020 | Product list page loads products | Critical | Pending |
| E2E-021 | Product cards display correct info | High | Pending |
| E2E-022 | Product detail page loads correctly | Critical | Pending |
| E2E-023 | Product images display properly | High | Pending |
| E2E-024 | Product specifications show | High | Pending |
| **Product Filtering & Search** |
| E2E-030 | Category filter works | High | Pending |
| E2E-031 | Price range filter works | High | Pending |
| E2E-032 | Brand filter works | High | Pending |
| E2E-033 | Multiple filters combine correctly | High | Pending |
| E2E-034 | Filter reset clears all filters | Medium | Pending |
| E2E-035 | Search returns relevant results | High | Pending |
| E2E-036 | Empty search shows no results message | Medium | Pending |
| **Shopping Cart** |
| E2E-040 | Add product to cart from list | Critical | Pending |
| E2E-041 | Add product to cart from detail page | Critical | Pending |
| E2E-042 | Cart badge updates on add | High | Pending |
| E2E-043 | Cart page shows added items | Critical | Pending |
| E2E-044 | Update quantity in cart | High | Pending |
| E2E-045 | Remove item from cart | High | Pending |
| E2E-046 | Cart persists after page reload | High | Pending |
| E2E-047 | Cart totals calculate correctly | Critical | Pending |
| **Checkout Flow** |
| E2E-050 | Guest can proceed to checkout | High | Pending |
| E2E-051 | Logged in user sees saved info | High | Pending |
| E2E-052 | Shipping form validation works | Critical | Pending |
| E2E-053 | Payment form validation works | Critical | Pending |
| E2E-054 | Order summary shows correct items | Critical | Pending |
| E2E-055 | Order can be placed successfully | Critical | Pending |
| E2E-056 | Order confirmation page shows | Critical | Pending |
| E2E-057 | Order appears in user's order history | Critical | Pending |
| **Complete User Journeys** |
| E2E-060 | New user: Register → Browse → Add to Cart → Checkout | Critical | Pending |
| E2E-061 | Returning user: Login → View Orders → Reorder | High | Pending |
| E2E-062 | Guest user: Browse → Add to Cart → Checkout | Critical | Pending |
| E2E-063 | User: Browse by Category → Filter → Purchase | High | Pending |
| E2E-064 | User: Search → Find Product → Purchase | High | Pending |
| **Theme & Accessibility** |
| E2E-070 | Theme toggle switches between light/dark | Medium | Pending |
| E2E-071 | Language selector changes language | Medium | Pending |
| E2E-072 | Keyboard navigation works for menus | Medium | Pending |
| **Error Handling** |
| E2E-080 | 404 page shows for invalid routes | Medium | Pending |
| E2E-081 | Network error shows appropriate message | Medium | Pending |
| E2E-082 | Form errors display correctly | High | Pending |

---

## Section 1: Authentication & User Menu Tests

### E2E-001: User Can Register New Account

**File**: `Tests/Authentication/RegistrationTests.cs`

```csharp
[Fact]
public async Task Register_WithValidData_CreatesAccountAndLogsIn()
{
    // Arrange
    var email = $"test-{Guid.NewGuid()}@example.com";

    // Act - Register via UI
    await _page.GotoAsync("/register");
    await _page.FillAsync("[data-testid='firstName']", "Test");
    await _page.FillAsync("[data-testid='lastName']", "User");
    await _page.FillAsync("[data-testid='email']", email);
    await _page.FillAsync("[data-testid='password']", "TestPass123!");
    await _page.FillAsync("[data-testid='confirmPassword']", "TestPass123!");
    await _page.CheckAsync("[data-testid='terms']");
    await _page.ClickAsync("[data-testid='submit']");

    // Assert - User is logged in and redirected
    await Expect(_page).ToHaveURLAsync(new Regex("/account|/$"));
    await Expect(_page.Locator("[data-testid='user-menu']")).ToBeVisibleAsync();
}
```

### E2E-003: User Sees Dropdown Menu When Logged In

**File**: `Tests/Authentication/UserMenuTests.cs`

```csharp
[Fact]
public async Task LoggedInUser_ClicksUserIcon_SeesDropdownMenu()
{
    // Arrange - Create and login user
    var user = await _dataFactory.CreateUserAsync();
    await LoginAsUserAsync(user);

    // Act - Click user menu trigger
    await _page.ClickAsync("[data-testid='user-menu-trigger']");

    // Assert - Dropdown is visible with all links
    var dropdown = _page.Locator("[data-testid='user-dropdown']");
    await Expect(dropdown).ToBeVisibleAsync();
    await Expect(dropdown.Locator("[data-testid='account-link']")).ToBeVisibleAsync();
    await Expect(dropdown.Locator("[data-testid='orders-link']")).ToBeVisibleAsync();
    await Expect(dropdown.Locator("[data-testid='logout-button']")).ToBeVisibleAsync();
}
```

### E2E-006: User Can Logout From Dropdown

```csharp
[Fact]
public async Task LoggedInUser_ClicksLogout_IsLoggedOut()
{
    // Arrange
    var user = await _dataFactory.CreateUserAsync();
    await LoginAsUserAsync(user);

    // Act - Open dropdown and click logout
    await _page.ClickAsync("[data-testid='user-menu-trigger']");
    await _page.ClickAsync("[data-testid='logout-button']");

    // Assert - User is logged out, login button visible
    await Expect(_page.Locator("[data-testid='login-button']")).ToBeVisibleAsync();
    await Expect(_page.Locator("[data-testid='user-menu']")).Not.ToBeVisibleAsync();
}
```

---

## Section 2: Navigation & Mega Menu Tests

### E2E-011: Mega Menu Opens on Hover/Click

**File**: `Tests/Navigation/MegaMenuTests.cs`

```csharp
[Fact]
public async Task MegaMenu_OnHover_ShowsCategoriesAndSubcategories()
{
    // Arrange
    await _page.GotoAsync("/");

    // Act - Hover over Products in nav
    await _page.HoverAsync("[data-testid='mega-menu-trigger']");

    // Assert - Mega menu dropdown is visible
    var megaMenu = _page.Locator("[data-testid='mega-menu-dropdown']");
    await Expect(megaMenu).ToBeVisibleAsync();

    // Assert - Categories are shown
    await Expect(megaMenu.Locator("[data-testid='category-item']").First).ToBeVisibleAsync();
}

[Fact]
public async Task MegaMenu_HoverCategory_ShowsSubcategories()
{
    // Arrange
    await _page.GotoAsync("/");
    await _page.HoverAsync("[data-testid='mega-menu-trigger']");

    // Act - Hover over first category
    await _page.HoverAsync("[data-testid='category-item'] >> nth=0");

    // Assert - Subcategories panel is visible
    await Expect(_page.Locator("[data-testid='subcategories-panel']")).ToBeVisibleAsync();
    await Expect(_page.Locator("[data-testid='subcategory-link']").First).ToBeVisibleAsync();
}
```

### E2E-013: Subcategory Links Navigate Correctly

```csharp
[Fact]
public async Task MegaMenu_ClickSubcategory_NavigatesToFilteredProducts()
{
    // Arrange
    await _page.GotoAsync("/");
    await _page.HoverAsync("[data-testid='mega-menu-trigger']");
    await _page.HoverAsync("[data-testid='category-item'] >> nth=0");

    // Act - Click a subcategory
    await _page.ClickAsync("[data-testid='subcategory-link'] >> nth=0");

    // Assert - Navigated to products with category filter
    await Expect(_page).ToHaveURLAsync(new Regex("/products\\?category="));
}
```

---

## Section 3: Shopping Cart Tests

### E2E-040: Add Product to Cart From List

**File**: `Tests/Cart/AddToCartTests.cs`

```csharp
[Fact]
public async Task ProductList_ClickAddToCart_AddsProductToCart()
{
    // Arrange - Create a product
    var product = await _dataFactory.CreateProductAsync();
    await _page.GotoAsync("/products");

    // Act - Click add to cart on product card
    var productCard = _page.Locator($"[data-testid='product-card-{product.Id}']");
    await productCard.Locator("[data-testid='add-to-cart']").ClickAsync();

    // Assert - Cart badge shows 1
    await Expect(_page.Locator("[data-testid='cart-count']")).ToHaveTextAsync("1");
}
```

### E2E-047: Cart Totals Calculate Correctly

```csharp
[Fact]
public async Task Cart_WithMultipleItems_ShowsCorrectTotal()
{
    // Arrange - Create products and add to cart
    var product1 = await _dataFactory.CreateProductAsync(price: 100.00m);
    var product2 = await _dataFactory.CreateProductAsync(price: 250.00m);

    await AddProductToCartAsync(product1.Id);
    await AddProductToCartAsync(product2.Id);

    // Act - Go to cart
    await _page.GotoAsync("/cart");

    // Assert - Subtotal is correct
    var subtotal = await _page.Locator("[data-testid='cart-subtotal']").TextContentAsync();
    subtotal.Should().Contain("350"); // $350.00
}
```

---

## Section 4: Complete Purchase Flow Tests

### E2E-060: Complete User Journey - Register to Purchase

**File**: `Tests/Journeys/CompletePurchaseTests.cs`

```csharp
[Fact]
public async Task NewUser_RegisterBrowseAndPurchase_CompletesSuccessfully()
{
    // Step 1: Register new user
    var email = $"journey-{Guid.NewGuid()}@example.com";
    await _page.GotoAsync("/register");
    await _page.FillAsync("[data-testid='firstName']", "Journey");
    await _page.FillAsync("[data-testid='lastName']", "Test");
    await _page.FillAsync("[data-testid='email']", email);
    await _page.FillAsync("[data-testid='password']", "TestPass123!");
    await _page.FillAsync("[data-testid='confirmPassword']", "TestPass123!");
    await _page.CheckAsync("[data-testid='terms']");
    await _page.ClickAsync("[data-testid='submit']");

    // Step 2: Browse products
    await _page.GotoAsync("/products");
    await Expect(_page.Locator("[data-testid='product-card']").First).ToBeVisibleAsync();

    // Step 3: View product detail
    await _page.ClickAsync("[data-testid='product-card'] >> nth=0");
    await Expect(_page).ToHaveURLAsync(new Regex("/products/"));

    // Step 4: Add to cart
    await _page.ClickAsync("[data-testid='add-to-cart']");
    await Expect(_page.Locator("[data-testid='cart-count']")).ToHaveTextAsync("1");

    // Step 5: Go to cart
    await _page.ClickAsync("[data-testid='cart-icon']");
    await Expect(_page).ToHaveURLAsync("/cart");

    // Step 6: Proceed to checkout
    await _page.ClickAsync("[data-testid='checkout-button']");
    await Expect(_page).ToHaveURLAsync("/checkout");

    // Step 7: Fill shipping info
    await _page.FillAsync("[data-testid='shipping-address']", "123 Test Street");
    await _page.FillAsync("[data-testid='shipping-city']", "Test City");
    await _page.FillAsync("[data-testid='shipping-postalCode']", "12345");
    await _page.FillAsync("[data-testid='shipping-phone']", "+1234567890");
    await _page.ClickAsync("[data-testid='continue-to-payment']");

    // Step 8: Fill payment info (test mode)
    await _page.FillAsync("[data-testid='card-number']", "4242424242424242");
    await _page.FillAsync("[data-testid='card-expiry']", "12/30");
    await _page.FillAsync("[data-testid='card-cvv']", "123");
    await _page.ClickAsync("[data-testid='continue-to-review']");

    // Step 9: Review and place order
    await _page.ClickAsync("[data-testid='place-order']");

    // Step 10: Verify order confirmation
    await Expect(_page).ToHaveURLAsync(new Regex("/checkout/confirmation"));
    await Expect(_page.Locator("[data-testid='order-number']")).ToBeVisibleAsync();

    // Step 11: Verify order in history
    await _page.ClickAsync("[data-testid='user-menu-trigger']");
    await _page.ClickAsync("[data-testid='orders-link']");
    await Expect(_page.Locator("[data-testid='order-item']").First).ToBeVisibleAsync();
}
```

### E2E-062: Guest User Purchase Flow

```csharp
[Fact]
public async Task GuestUser_BrowseAndPurchase_CompletesSuccessfully()
{
    // Step 1: Browse as guest
    await _page.GotoAsync("/products");

    // Step 2: Add product to cart
    await _page.ClickAsync("[data-testid='add-to-cart'] >> nth=0");

    // Step 3: Go to cart
    await _page.GotoAsync("/cart");

    // Step 4: Proceed to checkout
    await _page.ClickAsync("[data-testid='checkout-button']");

    // Step 5: Fill guest email and shipping
    await _page.FillAsync("[data-testid='guest-email']", "guest@example.com");
    await _page.FillAsync("[data-testid='shipping-firstName']", "Guest");
    await _page.FillAsync("[data-testid='shipping-lastName']", "User");
    await _page.FillAsync("[data-testid='shipping-address']", "456 Guest Ave");
    await _page.FillAsync("[data-testid='shipping-city']", "Guest City");
    await _page.FillAsync("[data-testid='shipping-postalCode']", "54321");
    await _page.FillAsync("[data-testid='shipping-phone']", "+0987654321");
    await _page.ClickAsync("[data-testid='continue-to-payment']");

    // Step 6: Fill payment and complete
    await _page.FillAsync("[data-testid='card-number']", "4242424242424242");
    await _page.FillAsync("[data-testid='card-expiry']", "12/30");
    await _page.FillAsync("[data-testid='card-cvv']", "123");
    await _page.ClickAsync("[data-testid='continue-to-review']");
    await _page.ClickAsync("[data-testid='place-order']");

    // Assert - Order confirmation shown
    await Expect(_page).ToHaveURLAsync(new Regex("/checkout/confirmation"));
}
```

### E2E-063: Browse by Category and Filter

```csharp
[Fact]
public async Task User_BrowseByCategoryAndFilter_FindsProducts()
{
    // Step 1: Open mega menu and select category
    await _page.GotoAsync("/");
    await _page.HoverAsync("[data-testid='mega-menu-trigger']");
    await _page.ClickAsync("[data-testid='category-item'] >> nth=0");

    // Assert - Products page with category filter
    await Expect(_page).ToHaveURLAsync(new Regex("/products\\?category="));

    // Step 2: Apply price filter
    await _page.FillAsync("[data-testid='price-min']", "100");
    await _page.FillAsync("[data-testid='price-max']", "500");
    await _page.ClickAsync("[data-testid='apply-filters']");

    // Assert - URL updated with filters
    await Expect(_page).ToHaveURLAsync(new Regex("priceMin=100"));

    // Step 3: Select a product
    await _page.ClickAsync("[data-testid='product-card'] >> nth=0");

    // Assert - Product detail page
    await Expect(_page).ToHaveURLAsync(new Regex("/products/"));
    await Expect(_page.Locator("[data-testid='product-title']")).ToBeVisibleAsync();
}
```

---

## Section 5: Theme & Language Tests

### E2E-070: Theme Toggle

**File**: `Tests/Settings/ThemeTests.cs`

```csharp
[Fact]
public async Task ThemeToggle_Click_SwitchesBetweenLightAndDark()
{
    // Arrange
    await _page.GotoAsync("/");

    // Get initial theme
    var initialTheme = await _page.EvaluateAsync<string>("document.documentElement.dataset.theme");

    // Act - Click theme toggle
    await _page.ClickAsync("[data-testid='theme-toggle']");

    // Assert - Theme changed
    var newTheme = await _page.EvaluateAsync<string>("document.documentElement.dataset.theme");
    newTheme.Should().NotBe(initialTheme);
}
```

### E2E-071: Language Selector

```csharp
[Fact]
public async Task LanguageSelector_SelectBulgarian_ChangesLanguage()
{
    // Arrange
    await _page.GotoAsync("/");

    // Act - Select Bulgarian
    await _page.ClickAsync("[data-testid='language-selector']");
    await _page.ClickAsync("[data-testid='language-bg']");

    // Assert - UI text changed to Bulgarian
    var homeLink = await _page.Locator("[data-testid='nav-home']").TextContentAsync();
    homeLink.Should().Contain("Начало");
}
```

---

## Execution Order

1. **Phase 1: Core Authentication** (E2E-001 to E2E-007)
2. **Phase 2: Navigation & Menu** (E2E-010 to E2E-015)
3. **Phase 3: Product Browsing** (E2E-020 to E2E-024)
4. **Phase 4: Shopping Cart** (E2E-040 to E2E-047)
5. **Phase 5: Checkout** (E2E-050 to E2E-057)
6. **Phase 6: Complete Journeys** (E2E-060 to E2E-064)
7. **Phase 7: Settings & Error Handling** (E2E-070 to E2E-082)

---

## Test Data Requirements

Each test creates its own data. The TestDataFactory must support:

```csharp
public class TestDataFactory
{
    // User creation
    public Task<TestUser> CreateUserAsync(string? email = null, string? password = null);
    public Task<TestUser> CreateAdminUserAsync();

    // Product creation
    public Task<TestProduct> CreateProductAsync(
        string? name = null,
        decimal? price = null,
        string? categorySlug = null);

    // Category creation
    public Task<TestCategory> CreateCategoryAsync(string? name = null);

    // Order creation (for testing order history)
    public Task<TestOrder> CreateOrderAsync(string userId, List<string> productIds);

    // Cleanup
    public Task CleanupAsync(); // Deletes all created test data
}
```

---

## Running Tests

```bash
# Run all E2E tests
cd tests/ClimaSite.E2E
dotnet test

# Run specific test category
dotnet test --filter "Category=Authentication"

# Run with headed browser (for debugging)
HEADED=1 dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~NewUser_RegisterBrowseAndPurchase"
```
