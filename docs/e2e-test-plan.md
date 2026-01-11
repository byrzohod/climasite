# ClimaSite E2E Test Plan

## Overview

This document outlines a comprehensive End-to-End (E2E) test plan for the ClimaSite HVAC E-Commerce platform. All tests use **REAL data** and the **actual backend/database** - no mocking.

## Testing Principles

### Core Principles
1. **No Mocking** - All tests hit the real API and database
2. **Test Isolation** - Each test creates its own data via TestDataFactory
3. **Self-Cleanup** - Tests clean up after themselves using correlation IDs
4. **Real User Flows** - Tests simulate actual user behavior
5. **Data-Driven** - Use the seeded HVAC products for realistic scenarios

### Test Environment
- **Backend**: http://localhost:5029
- **Frontend**: http://localhost:4200
- **Database**: PostgreSQL on port 5433
- **Framework**: Playwright (.NET)

---

## Test Categories

### 1. Authentication & User Management (AUTH)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| AUTH-001 | Guest can view products | Unauthenticated users can browse products | High |
| AUTH-002 | Guest can access cart | Unauthenticated users can use cart | High |
| AUTH-003 | User registration | Complete registration flow with valid data | High |
| AUTH-004 | Registration validation | Form validation for email, password requirements | High |
| AUTH-005 | Duplicate email registration | Shows error when email already exists | Medium |
| AUTH-006 | User login | Login with valid credentials | High |
| AUTH-007 | Invalid login | Shows error for wrong credentials | High |
| AUTH-008 | Remember me functionality | Persistent session with remember me option | Low |
| AUTH-009 | Logout | User can log out successfully | High |
| AUTH-010 | Forgot password flow | Request password reset email | Medium |
| AUTH-011 | Reset password | Reset password with valid token | Medium |
| AUTH-012 | Session expiry | Redirects to login after session expires | Medium |
| AUTH-013 | Protected routes | Unauthenticated access redirects to login | High |

### 2. Product Browsing & Discovery (PROD)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| PROD-001 | Homepage loads products | Featured products visible on homepage | High |
| PROD-002 | Product list page | All products displayed with pagination | High |
| PROD-003 | Product detail page | Full product info, images, specifications | High |
| PROD-004 | Category navigation | Navigate to category-filtered products | High |
| PROD-005 | Subcategory filtering | Filter products by subcategory | Medium |
| PROD-006 | Price range filter | Filter products by min/max price | High |
| PROD-007 | Sort by price ascending | Sort products low to high | High |
| PROD-008 | Sort by price descending | Sort products high to low | High |
| PROD-009 | Sort by name | Alphabetical product sorting | Medium |
| PROD-010 | Sort by newest | Most recent products first | Medium |
| PROD-011 | In-stock filter | Only show available products | High |
| PROD-012 | Brand filter | Filter by product brand | Medium |
| PROD-013 | Combined filters | Multiple filters applied together | High |
| PROD-014 | Clear filters | Reset all filters to default | Medium |
| PROD-015 | Product search | Search by product name/description | High |
| PROD-016 | Search with no results | Empty state when no matches | Medium |
| PROD-017 | Product specifications | View detailed HVAC specs | Medium |
| PROD-018 | Related products | Show similar products | Low |
| PROD-019 | Product reviews display | Show customer reviews | Medium |
| PROD-020 | Sale/discount badges | Products on sale show discount | Medium |
| PROD-021 | Out of stock indicator | Clear indication when unavailable | High |
| PROD-022 | Product image gallery | Multiple images with zoom | Low |

### 3. Shopping Cart (CART)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| CART-001 | Add product to cart | Single product addition | High |
| CART-002 | Add multiple products | Add different products to cart | High |
| CART-003 | Update quantity | Change product quantity in cart | High |
| CART-004 | Remove item | Remove product from cart | High |
| CART-005 | Cart persistence (guest) | Cart survives page refresh | High |
| CART-006 | Cart persistence (user) | Cart persists after login | High |
| CART-007 | Cart merge on login | Guest cart merges with user cart | Medium |
| CART-008 | Empty cart state | Proper UI when cart is empty | Medium |
| CART-009 | Cart totals calculation | Subtotal, tax, total accurate | High |
| CART-010 | Max quantity validation | Cannot exceed stock quantity | High |
| CART-011 | Out of stock handling | Product goes out of stock in cart | Medium |
| CART-012 | Cart icon badge | Shows item count in header | Medium |
| CART-013 | Continue shopping link | Navigate back to products | Low |
| CART-014 | Apply promo code | Valid promo code applies discount | Medium |
| CART-015 | Invalid promo code | Error message for invalid code | Medium |

### 4. Checkout Process (CHK)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| CHK-001 | Checkout as guest | Complete checkout without account | High |
| CHK-002 | Checkout as user | Complete checkout with logged-in user | High |
| CHK-003 | Shipping form validation | All required fields validated | High |
| CHK-004 | Save shipping address | Save address for future orders | Medium |
| CHK-005 | Use saved address | Select previously saved address | Medium |
| CHK-006 | Multiple addresses | Manage multiple shipping addresses | Low |
| CHK-007 | Shipping method selection | Choose shipping speed/cost | Medium |
| CHK-008 | Payment form display | Card payment form renders correctly | High |
| CHK-009 | Payment validation | Card details validation | High |
| CHK-010 | Order review | Review order before confirmation | High |
| CHK-011 | Edit cart from checkout | Go back and modify cart | Medium |
| CHK-012 | Order placement | Successfully place order | High |
| CHK-013 | Order confirmation page | Show order details after purchase | High |
| CHK-014 | Order confirmation email | Email sent to customer (mock SMTP) | Medium |
| CHK-015 | Failed payment handling | Graceful error when payment fails | High |
| CHK-016 | Stock validation at checkout | Prevent overselling | High |
| CHK-017 | Price lock | Price doesn't change during checkout | Medium |

### 5. User Account Management (ACC)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| ACC-001 | Account dashboard | View account overview | High |
| ACC-002 | View order history | List of previous orders | High |
| ACC-003 | Order details | View specific order details | High |
| ACC-004 | Order status tracking | Track order progress | Medium |
| ACC-005 | Update profile | Edit name, email, phone | High |
| ACC-006 | Change password | Update account password | High |
| ACC-007 | Password requirements | Validation for password strength | Medium |
| ACC-008 | Manage addresses | CRUD for saved addresses | Medium |
| ACC-009 | Default address | Set default shipping address | Low |
| ACC-010 | Language preference | Change interface language | Medium |
| ACC-011 | Account deletion | Request account deletion | Low |

### 6. Wishlist (WISH)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| WISH-001 | Add to wishlist | Add product from detail page | High |
| WISH-002 | Add to wishlist from list | Add product from product list | Medium |
| WISH-003 | Remove from wishlist | Remove product from wishlist | High |
| WISH-004 | View wishlist | See all wishlist items | High |
| WISH-005 | Move to cart | Add wishlist item to cart | High |
| WISH-006 | Wishlist persistence | Wishlist survives logout/login | Medium |
| WISH-007 | Guest wishlist | Wishlist for unauthenticated users | Low |
| WISH-008 | Share wishlist | Generate shareable wishlist link | Low |

### 7. Product Reviews (REV)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| REV-001 | View product reviews | Display reviews on product page | High |
| REV-002 | Submit review | Write and submit a review | High |
| REV-003 | Star rating | Select 1-5 star rating | High |
| REV-004 | Review validation | Title and content required | Medium |
| REV-005 | Verified purchase badge | Show badge for actual buyers | Medium |
| REV-006 | Review pagination | Navigate through many reviews | Low |
| REV-007 | Sort reviews | Sort by date, rating, helpful | Medium |
| REV-008 | Edit own review | Modify submitted review | Low |
| REV-009 | Delete own review | Remove submitted review | Low |
| REV-010 | Report review | Flag inappropriate content | Low |

### 8. Navigation & UI (NAV)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| NAV-001 | Header navigation | All main nav links work | High |
| NAV-002 | Footer links | Footer links navigate correctly | Medium |
| NAV-003 | Breadcrumb navigation | Breadcrumbs on product pages | Medium |
| NAV-004 | Mobile menu | Hamburger menu on mobile | High |
| NAV-005 | Responsive design | Layout adapts to screen size | High |
| NAV-006 | Theme toggle | Switch between light/dark mode | Medium |
| NAV-007 | Language selector | Change language (EN/BG/DE) | High |
| NAV-008 | 404 page | Proper not found page | Medium |
| NAV-009 | Loading states | Spinners during data fetch | Medium |
| NAV-010 | Error states | Friendly error messages | High |

### 9. Search (SRCH)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| SRCH-001 | Basic search | Search products by keyword | High |
| SRCH-002 | Search suggestions | Autocomplete as user types | Medium |
| SRCH-003 | Search results page | Display matching products | High |
| SRCH-004 | No results handling | Helpful message when empty | Medium |
| SRCH-005 | Search with filters | Apply filters to search results | Medium |
| SRCH-006 | Search persistence | Search term persists in URL | Low |
| SRCH-007 | Recent searches | Show previous search terms | Low |

### 10. Internationalization (I18N)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| I18N-001 | English translations | All text in English by default | High |
| I18N-002 | Bulgarian translations | Switch to Bulgarian | High |
| I18N-003 | German translations | Switch to German | High |
| I18N-004 | Language persistence | Language choice saved | Medium |
| I18N-005 | Currency display | Prices formatted correctly | Medium |
| I18N-006 | Date/time formatting | Local format for dates | Low |

---

## Complete User Journey Tests

These tests simulate full end-to-end user journeys through the application.

### Journey 1: First-Time Buyer (JNY-001)
**Priority: High**

Steps:
1. Land on homepage
2. Browse featured products
3. Navigate to Air Conditioners category
4. Filter by price range ($500-$1000)
5. View product details for "DualZone Pro 12000"
6. Add to cart
7. View cart
8. Proceed to checkout
9. Register new account
10. Enter shipping address
11. Select shipping method
12. Enter payment details
13. Review order
14. Place order
15. View confirmation page

### Journey 2: Returning Customer (JNY-002)
**Priority: High**

Steps:
1. Log in to existing account
2. View order history
3. Search for "heat pump"
4. Add product to wishlist
5. Continue browsing
6. Add wishlist item to cart
7. Add another product to cart
8. Apply promo code
9. Use saved address at checkout
10. Complete purchase
11. View new order in history

### Journey 3: Comparison Shopper (JNY-003)
**Priority: Medium**

Steps:
1. Browse as guest
2. Filter by Heat Pumps category
3. Sort by price (low to high)
4. View multiple product details
5. Add 3 products to wishlist
6. Compare specifications
7. Add preferred product to cart
8. Modify quantity to 2
9. Checkout as guest

### Journey 4: Mobile Shopper (JNY-004)
**Priority: High**

Steps (mobile viewport):
1. Open site on mobile
2. Use hamburger menu navigation
3. Browse products
4. Filter using mobile filters UI
5. Add product to cart
6. View cart in mobile layout
7. Complete mobile checkout flow

### Journey 5: International Buyer (JNY-005)
**Priority: Medium**

Steps:
1. Change language to Bulgarian
2. Verify all UI translated
3. Browse products
4. Add to cart
5. Check prices in local format
6. Change language to German
7. Complete checkout

---

## Test Data Strategy

### Seeded Data (Available)
- **Categories**: Air Conditioners, Heating Systems, Ventilation, Accessories + subcategories
- **Products**: 14 HVAC products with variants, images, specifications
- **Admin User**: admin@climasite.local / Admin123!

### Test-Generated Data
- **Users**: Created per-test via TestDataFactory
- **Orders**: Created during checkout tests
- **Reviews**: Created during review tests
- **Cart Items**: Created during cart tests

### Data Cleanup
- Each test uses a correlation ID
- Cleanup endpoint removes test data after each test
- Database state restored for isolation

---

## Test Execution Strategy

### CI/CD Pipeline
```bash
# Full E2E test suite
npx playwright test

# Specific test file
npx playwright test tests/authentication/

# With UI for debugging
npx playwright test --ui

# Generate HTML report
npx playwright test --reporter=html
```

### Test Categories by Priority

**Priority 1 - Critical Path (Run on every PR)**
- AUTH-001, AUTH-002, AUTH-003, AUTH-006
- PROD-001, PROD-002, PROD-003, PROD-004
- CART-001, CART-003, CART-004, CART-009
- CHK-001, CHK-002, CHK-012
- JNY-001 (First-Time Buyer)

**Priority 2 - Important (Run nightly)**
- All Medium priority tests
- JNY-002, JNY-003, JNY-004

**Priority 3 - Comprehensive (Run weekly)**
- All Low priority tests
- JNY-005
- Edge cases and error scenarios

---

## Browser Matrix

| Browser | Versions | Priority |
|---------|----------|----------|
| Chrome | Latest | High |
| Firefox | Latest | High |
| Safari | Latest | Medium |
| Edge | Latest | Medium |
| Mobile Chrome | Latest | High |
| Mobile Safari | Latest | High |

---

## Accessibility Testing

Each test should verify:
- Keyboard navigation works
- Focus management is correct
- ARIA labels present
- Color contrast sufficient
- Screen reader compatibility

---

## Performance Baselines

| Metric | Target | Critical |
|--------|--------|----------|
| Homepage Load | < 2s | < 4s |
| Product List Load | < 1.5s | < 3s |
| Product Detail Load | < 1s | < 2s |
| Add to Cart | < 500ms | < 1s |
| Checkout Complete | < 3s | < 5s |

---

## Test Maintenance

### Weekly Review
- Check for flaky tests
- Update selectors if UI changes
- Review test coverage gaps

### Monthly Review
- Update test data if products change
- Review new features for test coverage
- Performance baseline updates

### Quarterly Review
- Browser version updates
- Playwright version updates
- Full test suite audit

---

## Reporting

### Daily Reports
- Test pass/fail counts
- Execution time trends
- Flaky test identification

### Weekly Reports
- Coverage metrics
- New tests added
- Issues found

### Release Reports
- Full regression results
- Performance comparison
- Accessibility audit results
