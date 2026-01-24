# User Account & Profile - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Profile View/Edit** - User personal information management (first name, last name, phone)
- **Password Change** - Authenticated password update with current password verification
- **Order History Access** - Paginated order list with filtering, sorting, search, and order details
- **Saved Addresses Management** - Full CRUD for shipping/billing addresses with default address support
- **Preferences** - User language (EN/BG/DE) and currency (EUR/BGN/USD) preferences

### API Endpoints
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/auth/me` | Get current user profile | Yes |
| PUT | `/api/auth/me` | Update user profile (name, phone, preferences) | Yes |
| PUT | `/api/auth/change-password` | Change user password | Yes |
| GET | `/api/addresses` | Get user's saved addresses | Yes |
| GET | `/api/addresses/{id}` | Get specific address | Yes |
| POST | `/api/addresses` | Create new address | Yes |
| PUT | `/api/addresses/{id}` | Update existing address | Yes |
| DELETE | `/api/addresses/{id}` | Delete address | Yes |
| PUT | `/api/addresses/{id}/default` | Set address as default | Yes |
| GET | `/api/orders` | Get user's order history (paginated) | Yes |
| GET | `/api/orders/{id}` | Get order details | Yes |
| POST | `/api/orders/{id}/cancel` | Cancel order | Yes |
| POST | `/api/orders/{id}/reorder` | Re-add order items to cart | Yes |
| GET | `/api/orders/{id}/invoice` | Download order invoice PDF | Yes |

### Frontend Routes
| Route | Component | Guard |
|-------|-----------|-------|
| `/account` | AccountDashboardComponent | authGuard |
| `/account/profile` | ProfileComponent | authGuard |
| `/account/orders` | OrdersComponent | authGuard |
| `/account/orders/:id` | OrderDetailsComponent | authGuard |
| `/account/addresses` | AddressesComponent | authGuard |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Controllers** | `src/ClimaSite.Api/Controllers/AuthController.cs` |
| | `src/ClimaSite.Api/Controllers/AddressesController.cs` |
| | `src/ClimaSite.Api/Controllers/OrdersController.cs` |
| **Address Commands** | `src/ClimaSite.Application/Features/Addresses/Commands/CreateAddressCommand.cs` |
| | `src/ClimaSite.Application/Features/Addresses/Commands/UpdateAddressCommand.cs` |
| | `src/ClimaSite.Application/Features/Addresses/Commands/DeleteAddressCommand.cs` |
| | `src/ClimaSite.Application/Features/Addresses/Commands/SetDefaultAddressCommand.cs` |
| **Address Queries** | `src/ClimaSite.Application/Features/Addresses/Queries/GetUserAddressesQuery.cs` |
| | `src/ClimaSite.Application/Features/Addresses/Queries/GetAddressByIdQuery.cs` |
| **Auth Commands** | `src/ClimaSite.Application/Auth/Commands/UpdateProfileCommand.cs` |
| | `src/ClimaSite.Application/Auth/Commands/ChangePasswordCommand.cs` |
| **Auth Queries** | `src/ClimaSite.Application/Auth/Queries/GetUserByIdQuery.cs` |
| **DTOs** | `src/ClimaSite.Application/Common/DTOs/AddressDto.cs` |
| | `src/ClimaSite.Application/Features/Auth/DTOs/UserDto.cs` |
| **Entities** | `src/ClimaSite.Core/Entities/SavedAddress.cs` |
| | `src/ClimaSite.Core/Entities/ApplicationUser.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| **Components** | `src/ClimaSite.Web/src/app/features/account/account-dashboard/account-dashboard.component.ts` |
| | `src/ClimaSite.Web/src/app/features/account/profile/profile.component.ts` |
| | `src/ClimaSite.Web/src/app/features/account/addresses/addresses.component.ts` |
| | `src/ClimaSite.Web/src/app/features/account/orders/orders.component.ts` |
| | `src/ClimaSite.Web/src/app/features/account/order-details/order-details.component.ts` |
| **Services** | `src/ClimaSite.Web/src/app/auth/services/auth.service.ts` |
| | `src/ClimaSite.Web/src/app/core/services/address.service.ts` |
| | `src/ClimaSite.Web/src/app/core/services/checkout.service.ts` |
| **Models** | `src/ClimaSite.Web/src/app/auth/services/auth.service.ts` (User, UpdateProfileRequest) |
| | `src/ClimaSite.Web/src/app/core/models/address.model.ts` |
| | `src/ClimaSite.Web/src/app/core/models/order.model.ts` |
| **Routes** | `src/ClimaSite.Web/src/app/features/account/account.routes.ts` |

---

## 3. Test Coverage Audit

### Unit Tests (Frontend)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `profile.component.spec.ts` | `should create` | Component creation |
| | `should initialize profile form with user data` | Form initialization |
| | `should initialize preferences form with user preferences` | Preferences form |
| | `should have empty password form initially` | Password form init |
| | `should call updateProfile on valid form submission` | Profile update |
| | `should show success message on successful update` | Success feedback |
| | `should show error message on failed update` | Error handling |
| | `should not call updateProfile if form is invalid` | Validation |
| | `should call updateProfile with preferences on submission` | Preferences update |
| | `should apply language change after successful update` | Language switching |
| | `should call changePassword on valid form submission` | Password change |
| | `should show success message on successful password change` | Password success |
| | `should reset password form after successful change` | Form reset |
| | `should show error message on failed password change` | Password error |
| | `should not call changePassword if form is invalid` | Password validation |
| | `should show validation error for password mismatch` | Mismatch validation |
| | `should show validation error for short password` | Length validation |
| | `should have isUpdatingProfile signal initialized to false` | Loading state |
| | `should have isChangingPassword signal initialized to false` | Loading state |
| | `should reset isUpdatingProfile after profile update completes` | Loading reset |
| | `should reset isChangingPassword after password change completes` | Loading reset |

| Test File | Coverage Status |
|-----------|-----------------|
| `orders.component.spec.ts` | Exists - Basic tests |
| `order-details.component.spec.ts` | Exists - Basic tests |
| `addresses.component.spec.ts` | **MISSING** |
| `address.service.spec.ts` | **MISSING** |

### Integration Tests (Backend)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | AddressesController has no integration tests |
| **NONE FOUND** | - | Profile update endpoints have no integration tests |

**Note:** Only `ProductsControllerTests.cs` exists in `tests/ClimaSite.Api.Tests/Controllers/`.

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `ProfileTests.cs` | `ProfilePage_WhenAuthenticated_ShowsAllSections` | Profile page structure |
| | `ProfilePage_WhenNotAuthenticated_RedirectsToLogin` | Auth guard protection |
| | `ProfilePage_ShowsUserData` | Profile data display |
| | `ProfilePage_UpdatePersonalInfo_ShowsSuccess` | Profile update flow |
| | `ProfilePage_PreferencesSection_ShowsLanguageAndCurrency` | Preferences UI |
| | `ProfilePage_ChangeLanguage_UpdatesPreference` | Language switching |
| | `ProfilePage_PasswordSection_ShowsAllFields` | Password change UI |
| | `ProfilePage_PasswordMismatch_ShowsError` | Password validation |
| | `ProfilePage_PasswordTooShort_ShowsError` | Password strength |
| | `ProfilePage_EmailFieldIsDisabled` | Email protection |
| | `ProfilePage_PhoneField_AcceptsInput` | Phone input |

| Test File | Coverage Status |
|-----------|-----------------|
| `AddressesTests.cs` | **MISSING** - No E2E tests for addresses management |
| `OrdersTests.cs` | **MISSING** - No E2E tests for order history |
| `OrderDetailsTests.cs` | **MISSING** - No E2E tests for order details |

---

## 4. Manual Verification Steps

### Profile View/Edit
1. Navigate to `/account/profile` (requires login)
2. Verify personal info section shows current user data (first name, last name, phone)
3. Verify email field is displayed but disabled (read-only)
4. Edit first name and last name
5. Click "Save Changes"
6. Verify success message appears
7. Refresh page and verify changes persisted
8. Verify changes reflect in user menu dropdown

### Password Change
1. Navigate to `/account/profile`
2. Scroll to password section
3. Enter current password (correct)
4. Enter new password (min 8 chars)
5. Enter confirm password (must match)
6. Click "Change Password"
7. Verify success message
8. Logout and login with new password
9. Test error case: enter incorrect current password
10. Verify error message appears

### Preferences (Language/Theme)
1. Navigate to `/account/profile`
2. In preferences section, select language "Български"
3. Click "Save"
4. Verify UI immediately switches to Bulgarian
5. Refresh page, verify language persists
6. Change currency to BGN
7. Verify currency symbol updates in product prices
8. Repeat for German (DE) and English (EN)

### Order History Access
1. Navigate to `/account/orders`
2. Verify order list displays with pagination
3. Test search by order number
4. Test filter by status (Pending, Paid, Processing, etc.)
5. Test filter by date range
6. Test sort by date/total/status
7. Test sort direction toggle
8. Click "View Details" on an order
9. Verify order details page shows:
   - Order number and date
   - Status with color coding
   - Item list with images, quantities, prices
   - Order timeline/events
   - Shipping address
   - Payment method
   - Order summary (subtotal, shipping, tax, total)
10. Test reorder functionality (add items to cart)
11. Test download invoice (if available)
12. Test cancel order (only for Pending/Paid status)

### Saved Addresses Management
1. Navigate to `/account/addresses`
2. Click "Add New Address"
3. Fill address form:
   - Full name (required)
   - Address line 1 (required)
   - Address line 2 (optional)
   - City (required)
   - State/Province (optional)
   - Postal code (required)
   - Country (required - dropdown)
   - Phone (optional)
   - Type (Shipping/Billing/Both)
   - Set as default checkbox
4. Save address
5. Verify address appears in list
6. Click "Edit" on address
7. Modify fields and save
8. Verify changes persisted
9. Click "Set as Default"
10. Verify default badge appears
11. Verify only one address can be default
12. Click "Delete" on address
13. Confirm deletion in modal
14. Verify address removed from list
15. Test address selection in checkout flow

### Account Dashboard
1. Navigate to `/account`
2. Verify welcome message with user name
3. Verify navigation links to Profile, Orders, Addresses
4. Click each link and verify navigation

---

## 5. Gaps & Risks

### Critical Gaps

- [ ] **No addresses E2E tests** - Full CRUD flow for addresses is untested end-to-end
- [ ] **No addresses unit tests** - AddressesComponent and AddressService have no `.spec.ts` files
- [ ] **No orders E2E tests** - Order history, filtering, pagination untested
- [ ] **No order details E2E tests** - Order details view, reorder, cancel, invoice untested
- [ ] **No backend integration tests** - AddressesController and profile endpoints have no API tests

### Medium Gaps

- [ ] **No password change E2E test with actual credentials** - Current test only validates form, not API call
- [ ] **No concurrent session tests** - Behavior when changing password on multiple devices untested
- [ ] **No address validation tests** - Country-specific postal code formats not validated
- [ ] **No rate limiting tests** - Profile/password update brute force protection untested
- [ ] **Theme preference not persisted** - Only language/currency stored, theme is browser-local only

### Security Risks

- [ ] **No IDOR protection tests** - Verify users can't access/modify other users' addresses
- [ ] **Password complexity on change** - Backend may not enforce same complexity as registration
- [ ] **Session invalidation on password change** - Other active sessions should be terminated

### UX/Accessibility Gaps

- [ ] **No keyboard navigation tests for address modal** - Focus trap and Escape key handling
- [ ] **No screen reader tests** - ARIA labels for address actions not verified
- [ ] **No error message i18n verification** - Backend error messages may not be localized

### Technical Debt

- [ ] **Profile component has TODO** - Phone validation pattern consideration noted but not implemented
- [ ] **Address country dropdown hardcoded** - Country list should come from API or config
- [ ] **Order status colors inline** - Status styling should use CSS variables for theming

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **Critical** | No addresses E2E tests | Create `tests/ClimaSite.E2E/Tests/Account/AddressesTests.cs` with tests for: add, edit, delete, set default, use in checkout |
| **Critical** | No orders E2E tests | Create `tests/ClimaSite.E2E/Tests/Account/OrdersTests.cs` with tests for: list, filter, sort, pagination, search |
| **Critical** | No order details E2E tests | Create `tests/ClimaSite.E2E/Tests/Account/OrderDetailsTests.cs` with tests for: view, reorder, cancel, invoice |
| **Critical** | No backend integration tests | Create `tests/ClimaSite.Api.Tests/Controllers/AddressesControllerTests.cs` |
| **High** | No addresses component unit tests | Create `addresses.component.spec.ts` with form validation, CRUD operations tests |
| **High** | No address service unit tests | Create `address.service.spec.ts` with signal state management tests |
| **High** | IDOR protection | Add integration tests verifying users can only access own addresses/orders |
| **Medium** | Theme persistence | Store theme preference in user profile (add to UpdateProfileRequest) |
| **Medium** | Password change session invalidation | Implement and test logout of other sessions on password change |
| **Medium** | Address postal code validation | Add country-specific postal code format validators |
| **Low** | Country list from API | Create `/api/countries` endpoint or use i18n config |
| **Low** | Phone validation pattern | Implement international phone format validation |

### Recommended E2E Test Cases

**AddressesTests.cs:**
```csharp
// Add new address
AddAddressButton_OpensModal_WithEmptyForm
AddAddress_WithValidData_ShowsInList
AddAddress_WithMissingRequired_ShowsValidation
AddAddress_AsDefault_UpdatesDefaultBadge

// Edit address
EditAddress_LoadsExistingData
EditAddress_SavesChanges
EditAddress_Cancel_DiscardsChanges

// Delete address
DeleteAddress_ShowsConfirmation
DeleteAddress_Confirmed_RemovesFromList
DeleteAddress_Cancelled_KeepsAddress

// Default address
SetDefault_UpdatesBadge_RemovesPreviousDefault
DefaultAddress_PreselectedInCheckout
```

**OrdersTests.cs:**
```csharp
// Order list
OrdersList_ShowsUserOrders
OrdersList_EmptyState_WhenNoOrders
OrdersList_Pagination_WorksCorrectly

// Filtering
FilterByStatus_ShowsMatchingOrders
FilterByDateRange_ShowsMatchingOrders
SearchByOrderNumber_FindsOrder
ClearFilters_ShowsAllOrders

// Sorting
SortByDate_OrdersCorrectly
SortByTotal_OrdersCorrectly
SortDirection_TogglesCorrectly
```

**OrderDetailsTests.cs:**
```csharp
// View
OrderDetails_ShowsAllSections
OrderDetails_Timeline_ShowsEvents
OrderDetails_ItemsList_ShowsProducts

// Actions
Reorder_AddsItemsToCart
Reorder_PartialStock_ShowsWarning
CancelOrder_PendingStatus_ShowsModal
CancelOrder_Confirmed_UpdatesStatus
DownloadInvoice_GeneratesPDF
```

---

## 7. Evidence & Notes

### Profile Component Implementation

The ProfileComponent uses Angular Signals for state management and has three separate forms:

```typescript
// Form initialization
this.profileForm = this.fb.group({
  firstName: ['', Validators.required],
  lastName: ['', Validators.required],
  phone: ['']
});

this.preferencesForm = this.fb.group({
  preferredLanguage: ['en'],
  preferredCurrency: ['EUR']
});

this.passwordForm = this.fb.group({
  currentPassword: ['', Validators.required],
  newPassword: ['', [Validators.required, Validators.minLength(8)]],
  confirmPassword: ['', Validators.required]
}, { validators: this.passwordMatchValidator });
```

### Address Service Signal-Based State

The AddressService uses Angular Signals for reactive state management:

```typescript
// Signal-based state
private readonly _addresses = signal<SavedAddress[]>([]);
private readonly _isLoading = signal(false);
private readonly _error = signal<string | null>(null);

// Computed signals
readonly defaultAddress = computed(() => this._addresses().find(a => a.isDefault) ?? null);
readonly shippingAddresses = computed(() => this._addresses().filter(a => a.type === 'Shipping' || a.type === 'Both'));
readonly billingAddresses = computed(() => this._addresses().filter(a => a.type === 'Billing' || a.type === 'Both'));
```

### Order History Features

The OrdersComponent provides comprehensive filtering:

- **Search**: Debounced search by order number
- **Status filter**: Dropdown with all available order statuses
- **Date range**: From/To date pickers
- **Sorting**: By date, total, status with direction toggle
- **Pagination**: Configurable page size (5, 10, 20, 50)

### User Model Properties

```typescript
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  emailConfirmed: boolean;
  role: string;
  preferredLanguage: string;  // 'en' | 'bg' | 'de'
  preferredCurrency: string;  // 'EUR' | 'BGN' | 'USD'
  createdAt: string;
  lastLoginAt?: string;
}
```

### Address Model Properties

```typescript
export interface SavedAddress {
  id: string;
  fullName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  countryCode: string;
  phone?: string;
  isDefault: boolean;
  type: 'Shipping' | 'Billing' | 'Both';
  createdAt: string;
  updatedAt?: string;
}
```

### Test Data Factory Usage

E2E tests use TestDataFactory to create real users and data:

```csharp
// Create user and navigate to profile
var user = await _dataFactory.CreateUserAsync();
var loginPage = new LoginPage(_page);
await loginPage.NavigateAsync();
await loginPage.LoginAsync(user.Email, user.Password);
await _page.GotoAsync("/account/profile");
```

### API Authorization

All account endpoints require authentication via JWT:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Requires valid JWT token
public class AddressesController : ControllerBase
```

### Data Test IDs

Key test IDs for account features:

| Feature | Test IDs |
|---------|----------|
| Profile | `profile-page`, `profile-form`, `profile-firstName`, `profile-lastName`, `profile-phone`, `profile-submit` |
| Preferences | `profile-language`, `profile-currency`, `preferences-submit` |
| Password | `password-form`, `current-password`, `new-password`, `confirm-password`, `password-submit` |
| Addresses | `address-card`, `add-address-btn`, `address-modal`, `save-address-btn`, `edit-address-btn`, `delete-address-btn` |
| Orders | `orders-page`, `orders-list`, `order-card`, `orders-search`, `orders-status-filter`, `view-order-details` |
| Order Details | `order-details-page`, `order-number`, `order-status`, `order-total`, `reorder-btn`, `cancel-order-btn` |
