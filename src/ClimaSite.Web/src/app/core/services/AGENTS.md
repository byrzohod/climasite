# ANGULAR SERVICES

28 singleton services. Signal-based state, RxJS for HTTP only.

## STATE PATTERN

```typescript
// Private signal + readonly computed (REQUIRED)
private readonly _cart = signal<Cart | null>(null);
readonly cart = this._cart.asReadonly();
readonly items = computed(() => this._cart()?.items ?? []);
```

## KEY SERVICES

| Service | Purpose | State |
|---------|---------|-------|
| CartService | Shopping cart, session mgmt | Signal |
| WishlistService | Dual-mode (guest/auth) | Signal |
| AnimationService | Scroll tracking, reduced motion | Signal |
| FlyingCartService | Product-to-cart animation | Stateless |
| ConfettiService | Order celebration canvas | Stateless |
| ThemeService | Light/dark toggle | Signal |
| LanguageService | EN/BG/DE switching | Signal |
| ComparisonService | Product compare | Signal |
| RecentlyViewedService | localStorage history | Signal |

## API SERVICES

ProductService, CategoryService, CheckoutService, ReviewService, PaymentService - return Observables, update Signals via `tap()`.

## CONVENTIONS

- Use `inject()` function, NOT constructor injection
- Private `_signal`, public `readonly computed`
- RxJS ONLY for HTTP, Signals for UI state
- All services `providedIn: 'root'`

## COMPLEXITY HOTSPOTS

| File | Lines | Notes |
|------|-------|-------|
| confetti.service.ts | 318 | Canvas physics simulation |
| animation.service.ts | 271 | Scroll velocity, RAF throttling |
| cart.service.ts | 261 | TODO: SVC-001 (error msgs not i18n) |

## ANIMATION ARCHITECTURE

AnimationService provides scroll state to directives. FlyingCartService/ConfettiService are standalone canvas animations. All respect `prefers-reduced-motion`.
