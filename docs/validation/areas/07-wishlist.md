# Wishlist - Validation Report

> Updated: 2026-06-07

## 1. Scope Summary

### Features Covered
- **Add to Wishlist**: authenticated users can add active products from product detail/card flows and receive the hydrated server DTO.
- **Remove from Wishlist**: individual removal syncs to the API and updates frontend signal state.
- **Clear Wishlist**: authenticated users can clear all server wishlist items.
- **View Wishlist**: `/wishlist` renders backend-hydrated product cards, empty state, share controls, and owner-only actions.
- **Guest/Local Storage**: guests keep local wishlist items in `localStorage`.
- **Guest-to-Login Merge**: local guest items are merged into the authenticated server wishlist after login.
- **API Sync**: authenticated wishlist state is loaded from and written to the backend, with in-flight fetch reuse during login merge.
- **Public Sharing**: owners can enable/disable sharing, copy the public link, regenerate the token, and anonymous users can view `/wishlist/shared/:shareToken` read-only.
- **Unavailable Products**: inactive/deleted products are omitted from returned wishlist DTO item lists instead of rendering incomplete cards.

### API Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/wishlist` | Get the current user's hydrated wishlist | Required |
| POST | `/api/wishlist/items/{productId}` | Add an active product and return the hydrated wishlist | Required |
| DELETE | `/api/wishlist/items/{productId}` | Remove one product and return the hydrated wishlist | Required |
| DELETE | `/api/wishlist` | Clear all items and return the hydrated wishlist | Required |
| PUT | `/api/wishlist/share` | Enable or disable public sharing | Required |
| POST | `/api/wishlist/share-token` | Regenerate the public share token | Required |
| GET | `/api/wishlist/shared/{shareToken}` | Load a public shared wishlist | Anonymous |

### Frontend Routes & Components

| Route | Component | Description |
|-------|-----------|-------------|
| `/wishlist` | `WishlistComponent` | Owner wishlist page with product grid and share controls |
| `/wishlist/shared/:shareToken` | `WishlistComponent` | Anonymous read-only shared wishlist view |
| N/A | `WishlistService` | Signal-based wishlist state and backend sync |
| N/A | Header integration | Wishlist link and badge count |
| N/A | Product Card integration | Quick wishlist toggle button |
| N/A | Product Detail integration | Wishlist toggle button |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| Controller | `src/ClimaSite.Api/Controllers/WishlistController.cs` |
| Commands | `src/ClimaSite.Application/Features/Wishlist/Commands/AddToWishlistCommand.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Commands/RemoveFromWishlistCommand.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Commands/ClearWishlistCommand.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Commands/SetWishlistSharingCommand.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Commands/RegenerateWishlistShareTokenCommand.cs` |
| Queries | `src/ClimaSite.Application/Features/Wishlist/Queries/GetWishlistQuery.cs` |
| | `src/ClimaSite.Application/Features/Wishlist/Queries/GetSharedWishlistQuery.cs` |
| Services | `src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs` |
| DTOs | `src/ClimaSite.Application/Features/Wishlist/DTOs/WishlistDto.cs` |
| Entities | `src/ClimaSite.Core/Entities/Wishlist.cs` |
| | `src/ClimaSite.Core/Entities/WishlistItem.cs` |
| Repository | `src/ClimaSite.Infrastructure/Repositories/WishlistRepository.cs` |
| Configuration | `src/ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| Component | `src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.ts` |
| Service | `src/ClimaSite.Web/src/app/core/services/wishlist.service.ts` |
| Login merge | `src/ClimaSite.Web/src/app/auth/services/auth.service.ts` |
| Integrations | `src/ClimaSite.Web/src/app/core/layout/header/header.component.ts` |
| | `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts` |
| | `src/ClimaSite.Web/src/app/features/products/product-detail/product-detail.component.ts` |
| Routes | `src/ClimaSite.Web/src/app/app.routes.ts` |
| Translations | `src/ClimaSite.Web/src/assets/i18n/{en,bg,de}.json` |

---

## 3. Test Coverage Audit

### Backend Unit Tests

| Test File | Coverage |
|-----------|----------|
| `tests/ClimaSite.Core.Tests/Entities/WishlistTests.cs` | Wishlist and WishlistItem domain behavior: add/remove/clear, duplicate prevention, share tokens, notes, priority, notify-on-sale |
| `tests/ClimaSite.Application.Tests/Features/Wishlist/WishlistHandlersTests.cs` | Add/remove/clear, sharing enable, token regeneration, authenticated get, shared get, inactive product failure, unauthenticated mutations |

### API Integration Tests

| Test File | Coverage |
|-----------|----------|
| `tests/ClimaSite.Api.Tests/Controllers/WishlistControllerTests.cs` | Auth guard, add/get/remove persistence, concurrent add idempotence, clear, anonymous shared view, disabled sharing 404, token regeneration |

### Frontend Unit Tests

| Test File | Coverage |
|-----------|----------|
| `src/ClimaSite.Web/src/app/core/services/wishlist.service.spec.ts` | Backend DTO hydration, auth-loading guard, guest storage, server clear, sharing token state, guest merge, concurrent login merge fetch reuse, shared read-only load |
| `src/ClimaSite.Web/src/app/features/wishlist/wishlist.component.spec.ts` | Owner/shared rendering, translated controls, share actions, read-only shared state |
| `src/ClimaSite.Web/src/app/app.routes.spec.ts` | Shared wishlist route registration |

### E2E Tests

| Test File | Coverage |
|-----------|----------|
| `tests/ClimaSite.E2E/Tests/Wishlist/WishlistTests.cs` | Add from product detail with backend hydration, remove server item, clear server items, anonymous shared link, guest-to-login merge |

---

## 4. Manual / Browser Verification

Completed on 2026-06-07 against the real local API and Angular app:
- Owner wishlist desktop view.
- Anonymous shared wishlist desktop view.
- Mobile dark-theme owner view.
- No visible `wishlist.*`, `undefined`, `null`, TODO/debug text, or missing shared-view translations.
- Shared view is read-only.
- Mobile fixed bottom navigation no longer overlaps the wishlist product card CTA.

---

## 5. Fixed Gaps

- Backend wishlist handlers now return hydrated `WishlistDto` objects instead of write-only success responses.
- Frontend service now consumes the backend DTO shape instead of treating the API response as a raw item array.
- Clear wishlist syncs through `DELETE /api/wishlist`.
- Public sharing is exposed through API endpoints and `/wishlist/shared/:shareToken`.
- Guest-to-login merge syncs local-only items into the server wishlist and applies the final backend DTO.
- Login-time wishlist sync reuses in-flight API fetches to avoid duplicate creation races.
- Backend user mutations are serialized per user and wrapped in EF transactions; API integration coverage verifies concurrent add idempotence.
- Owner and shared wishlist UI strings use EN/BG/DE translation keys.

---

## 6. Remaining Product Scope

The wishlist feature is complete for the Plan 18 WISH-* scope. The following wishlist-adjacent capabilities remain future enhancements, not merge blockers for this slice:
- Move-to-cart endpoint and UI action.
- Item note/priority editing UI.
- Notify-on-sale behavior and notification delivery.
- Per-endpoint rate limits, which are part of the broader Plan 18 security hardening phase.
- Large wishlist performance profiling.

---

## 7. Latest Validation

Final local validation completed on 2026-06-07:
- `dotnet build --no-restore -m:1` passed.
- Core tests: 199/199 passed.
- Application tests: 172/172 passed.
- API tests: 77/77 passed.
- Frontend lint passed.
- Frontend production build passed.
- Frontend i18n extraction check passed: 712/712.
- Frontend Karma suite passed: 983/983.
- E2E suite passed: 210/210 against real local API/data.
