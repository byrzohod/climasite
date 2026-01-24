# Plan 21C: Navigation & Header Redesign

## Overview

### Goals

Transform ClimaSite's navigation from a complex three-row header into a streamlined, modern "Nordic Tech" experience that prioritizes usability, reduces cognitive load, and introduces key missing features like a mini-cart drawer and enhanced search.

### Current State Summary

| Element | Current | Lines of Code | Issues |
|---------|---------|---------------|--------|
| Header Component | 3-row structure (top bar, main header, nav bar) | ~1,332 lines | Overly complex, hard to maintain |
| Top Bar | Contact info, language, theme toggle | Hidden on mobile | Useful but takes vertical space |
| Main Header | Logo, search, wishlist, cart, user menu | Responsive | Search lacks suggestions |
| Navigation Bar | Links + MegaMenu component | Desktop only | Cluttered, no visual hierarchy |
| Mobile Menu | Slide-in panel from right | Separate overlay | No bottom nav option |
| Mini-Cart | **Does not exist** | N/A | Cart icon links to full page |
| Search | Basic input with button | No autocomplete | No suggestions, recent searches |

### Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Header Height (desktop) | ~140px (3 rows) | ~80px (2 rows) |
| Time to access cart preview | Click + page load | Instant drawer |
| Search engagement | Low (basic input) | +50% with suggestions |
| Mobile nav accessibility | Hamburger only | Bottom nav + hamburger |
| Component complexity | 1,332 lines | <600 lines (split into sub-components) |
| Lighthouse Performance | 70-80 | 90+ (no layout shift) |

### Estimated Effort

| Phase | Effort | Complexity |
|-------|--------|------------|
| Header Simplification | 1 day | Medium |
| Search Enhancement | 1-1.5 days | High |
| Mini-Cart Drawer | 1 day | Medium |
| Mega Menu Redesign | 0.5-1 day | Medium |
| Mobile Navigation | 1 day | Medium |
| Testing & Polish | 0.5 day | Low |
| **Total** | **5-6 days** | |

---

## Component Redesigns

### 1. Header Simplification

**Goal:** Merge three rows into two, reduce visual complexity, improve maintainability.

#### Current Structure (3 Rows)
```
[Top Bar: Contact | Language/Theme]
[Main Header: Logo | Search | Actions]
[Nav Bar: Home | Promotions | Products | Brands | About | Resources | Contact]
```

#### Target Structure (2 Rows)
```
[Unified Header: Logo | Nav Links | Search | Language/Theme | Cart/User]
[Contextual Bar: Promotions/Seasonal Message (optional, dismissible)]
```

#### Design Specifications

| Element | Specification |
|---------|---------------|
| Header Height | 64px fixed |
| Max Width | 80rem (1280px) centered |
| Logo | 32px height, gradient text effect |
| Nav Links | Inline with logo, 14px medium weight |
| Search | Expandable icon -> full search on click/focus |
| Actions | Language (dropdown), Theme (icon), Cart (with count badge), User |
| Sticky Behavior | Becomes translucent glass on scroll |

#### Component Split Strategy

Current monolithic `header.component.ts` should be split:

```
app/core/layout/header/
├── header.component.ts           # Main container (~200 lines)
├── header-logo.component.ts      # Logo + branding (~50 lines)
├── header-nav.component.ts       # Navigation links (~80 lines)
├── header-search.component.ts    # Expandable search (~150 lines)
├── header-actions.component.ts   # Cart, user, theme (~100 lines)
└── mobile-header.component.ts    # Mobile-specific header (~150 lines)
```

---

### 2. Search Enhancement

**Goal:** Transform basic search input into a powerful discovery tool with autocomplete, recent searches, and popular products.

#### Current Search
- Basic text input with submit button
- Navigates to `/products?search={query}`
- No suggestions, no history

#### Target Search Features

| Feature | Description | Priority |
|---------|-------------|----------|
| Autocomplete | Product name suggestions as user types | Critical |
| Recent Searches | Show last 5 searches (localStorage) | High |
| Popular Products | Display trending/featured products | High |
| Category Suggestions | Suggest matching categories | Medium |
| Voice Search | Microphone icon for voice input | Low |
| Keyboard Navigation | Arrow keys, Enter to select | Critical |

#### Search Service Updates

**New file:** `search.service.ts`

```typescript
interface SearchSuggestion {
  type: 'product' | 'category' | 'recent' | 'popular';
  id: string;
  name: string;
  slug: string;
  image?: string;
  price?: number;
  categorySlug?: string;
}

interface SearchState {
  query: string;
  suggestions: SearchSuggestion[];
  recentSearches: string[];
  isLoading: boolean;
  isOpen: boolean;
}
```

#### API Endpoint Required

```
GET /api/search/suggestions?q={query}&limit=10
Response: {
  products: [{ id, name, slug, image, price }],
  categories: [{ id, name, slug }],
  popular: [{ id, name, slug, image, price }]
}
```

#### Search Dropdown UI

```
+------------------------------------------+
| [x] Recent Searches                       |
| - "daikin inverter"                      |
| - "mitsubishi heat pump"                 |
+------------------------------------------+
| Popular Products                          |
| [img] Daikin Sensira 12K BTU    $599     |
| [img] Mitsubishi MSZ-AP25VG     $749     |
+------------------------------------------+
| Categories                                |
| > Air Conditioners                        |
| > Heat Pumps                              |
+------------------------------------------+
```

---

### 3. Mega Menu Redesign

**Goal:** Transform text-heavy menu into visual category navigation with featured products.

#### Current State
- 692 lines in `mega-menu.component.ts`
- Text-only category list on left
- Subcategory grid on right
- No product previews

#### Target Design

```
+------------------------------------------------------------------+
| [Category Icons Column]  |  [Subcategory Grid]  |  [Featured]     |
|                          |                      |                 |
| [icon] Air Conditioning  |  Wall-Mounted        |  [Product Card] |
| [icon] Heating Systems   |  Multi-Split         |  "Top Pick"     |
| [icon] Ventilation       |  Floor Units         |  Daikin Sensira |
| [icon] Accessories       |  Cassette            |  $599           |
|                          |  Ducted              |  [Add to Cart]  |
|                          |                      |                 |
|                          |  [View All AC ->]    |  [See More ->]  |
+------------------------------------------------------------------+
```

#### Visual Category Cards

Each category should have:
- Custom icon (SVG, not emoji)
- Hover state with subtle background
- Active state with accent color
- Product count badge

#### Featured Products Section

- Show 1-2 featured products from active category
- Include image, name, price, rating
- Quick "Add to Cart" button
- "See More" link to category

---

### 4. Mini-Cart Drawer

**Goal:** Allow users to preview and edit cart without leaving current page.

#### Feature Specifications

| Feature | Description |
|---------|-------------|
| Trigger | Click cart icon opens drawer |
| Position | Right side slide-out, 400px width |
| Content | Cart items, subtotal, checkout button |
| Actions | Update quantity, remove item |
| Upsells | "You might also like" section |
| Animation | 300ms slide with backdrop fade |
| Close | Click outside, X button, Escape key |

#### Mini-Cart Component Structure

**New file:** `mini-cart-drawer.component.ts`

```typescript
@Component({
  selector: 'app-mini-cart-drawer',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    @if (isOpen()) {
      <div class="drawer-backdrop" (click)="close()"></div>
      <aside class="mini-cart-drawer" role="dialog" aria-modal="true">
        <header class="drawer-header">
          <h2>{{ 'cart.yourCart' | translate }}</h2>
          <button (click)="close()" aria-label="Close cart">
            <svg>...</svg>
          </button>
        </header>
        
        <div class="drawer-content">
          @if (cartService.isEmpty()) {
            <div class="empty-cart">
              <svg>...</svg>
              <p>{{ 'cart.empty' | translate }}</p>
              <a routerLink="/products">{{ 'cart.startShopping' | translate }}</a>
            </div>
          } @else {
            <ul class="cart-items">
              @for (item of cartService.items(); track item.id) {
                <app-mini-cart-item [item]="item" />
              }
            </ul>
          }
        </div>
        
        @if (!cartService.isEmpty()) {
          <footer class="drawer-footer">
            <div class="subtotal">
              <span>{{ 'cart.subtotal' | translate }}</span>
              <span>{{ cartService.subtotal() | currency }}</span>
            </div>
            <a routerLink="/cart" class="btn-secondary">
              {{ 'cart.viewCart' | translate }}
            </a>
            <a routerLink="/checkout" class="btn-primary">
              {{ 'cart.checkout' | translate }}
            </a>
          </footer>
        }
      </aside>
    }
  `
})
```

#### Mini-Cart Service

**Update:** `cart.service.ts`

```typescript
// Add to CartService
readonly miniCartOpen = signal(false);

openMiniCart(): void {
  this.miniCartOpen.set(true);
  document.body.style.overflow = 'hidden';
}

closeMiniCart(): void {
  this.miniCartOpen.set(false);
  document.body.style.overflow = '';
}

toggleMiniCart(): void {
  if (this.miniCartOpen()) {
    this.closeMiniCart();
  } else {
    this.openMiniCart();
  }
}
```

#### Upsell Products

Show 2-3 products:
- From same category as cart items
- Frequently bought together
- Accessories for cart items

---

### 5. Mobile Navigation

**Goal:** Improve mobile UX with bottom navigation option and gesture support.

#### Current Mobile Experience
- Hamburger menu in header
- Slide-in panel from right
- No persistent navigation

#### Target Mobile Experience

##### Option A: Bottom Navigation Bar (Recommended)

```
+------------------------------------------+
|           [Page Content]                  |
+------------------------------------------+
| [Home] [Products] [Search] [Cart] [Menu] |
+------------------------------------------+
```

| Tab | Icon | Action |
|-----|------|--------|
| Home | House | Navigate to / |
| Products | Grid | Open categories sheet |
| Search | Magnifier | Open search overlay |
| Cart | Shopping bag + badge | Open mini-cart drawer |
| Menu | Hamburger | Open full menu panel |

##### Mobile Menu Panel

Keep existing slide-in panel but enhance:
- Full-height sheet
- Gesture support (swipe down to close)
- Category accordion
- Account section at bottom

##### Gesture Support

| Gesture | Action |
|---------|--------|
| Swipe right on edge | Open menu |
| Swipe left on menu | Close menu |
| Swipe right on cart drawer | Close drawer |
| Pull down on sheet | Close sheet |

#### Implementation Notes

```typescript
// New directive for swipe gestures
@Directive({
  selector: '[appSwipeGesture]',
  standalone: true
})
export class SwipeGestureDirective {
  @Output() swipeLeft = new EventEmitter<void>();
  @Output() swipeRight = new EventEmitter<void>();
  @Output() swipeDown = new EventEmitter<void>();
  
  private touchStartX = 0;
  private touchStartY = 0;
  
  @HostListener('touchstart', ['$event'])
  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0].clientX;
    this.touchStartY = event.touches[0].clientY;
  }
  
  @HostListener('touchend', ['$event'])
  onTouchEnd(event: TouchEvent): void {
    const deltaX = event.changedTouches[0].clientX - this.touchStartX;
    const deltaY = event.changedTouches[0].clientY - this.touchStartY;
    
    if (Math.abs(deltaX) > 50 && Math.abs(deltaX) > Math.abs(deltaY)) {
      if (deltaX > 0) this.swipeRight.emit();
      else this.swipeLeft.emit();
    } else if (deltaY > 50 && Math.abs(deltaY) > Math.abs(deltaX)) {
      this.swipeDown.emit();
    }
  }
}
```

---

### 6. Language/Theme Controls

**Goal:** Streamline placement, reduce visual clutter.

#### Current State
- Language selector: Dropdown in top bar
- Theme toggle: Sun/moon icon in top bar
- Both hidden on mobile

#### Target Placement

##### Desktop
- Language: Text dropdown in header actions (e.g., "EN" with chevron)
- Theme: Icon button next to language

##### Mobile
- Language: In full menu panel footer
- Theme: In full menu panel footer
- Quick toggle via long-press on logo (optional Easter egg)

#### Component Updates

```typescript
// Simplified language selector
@Component({
  selector: 'app-language-selector',
  template: `
    <button 
      class="language-btn"
      (click)="toggleDropdown()"
      [attr.aria-expanded]="isOpen()"
    >
      {{ currentLang().toUpperCase() }}
      <svg class="chevron">...</svg>
    </button>
    
    @if (isOpen()) {
      <div class="language-dropdown">
        @for (lang of languages; track lang.code) {
          <button (click)="selectLanguage(lang.code)">
            {{ lang.name }}
          </button>
        }
      </div>
    }
  `
})
```

---

## Task List

### Phase 1: Header Simplification
- [ ] TASK-21C-001: Create new header component structure (split monolith)
- [ ] TASK-21C-002: Design and implement two-row header layout
- [ ] TASK-21C-003: Create HeaderLogoComponent
- [ ] TASK-21C-004: Create HeaderNavComponent with inline links
- [ ] TASK-21C-005: Create HeaderActionsComponent
- [ ] TASK-21C-006: Implement sticky header with glass effect on scroll
- [ ] TASK-21C-007: Add responsive breakpoint handling
- [ ] TASK-21C-008: Update header styles for Nordic Tech aesthetic

### Phase 2: Search Enhancement
- [ ] TASK-21C-009: Create SearchService with suggestion state management
- [ ] TASK-21C-010: Design search API endpoint (backend)
- [ ] TASK-21C-011: Implement autocomplete API call with debounce
- [ ] TASK-21C-012: Create expandable search input component
- [ ] TASK-21C-013: Build search dropdown UI (suggestions, recent, popular)
- [ ] TASK-21C-014: Implement keyboard navigation (arrow keys, Enter, Escape)
- [ ] TASK-21C-015: Add recent searches to localStorage
- [ ] TASK-21C-016: Style search dropdown for light/dark themes
- [ ] TASK-21C-017: Add loading and empty states to search

### Phase 3: Mini-Cart Drawer
- [x] TASK-21C-018: Create MiniCartDrawerComponent
- [x] TASK-21C-019: Create MiniCartItemComponent
- [x] TASK-21C-020: Add mini-cart state to CartService
- [x] TASK-21C-021: Implement drawer slide animation
- [x] TASK-21C-022: Add backdrop with click-to-close
- [x] TASK-21C-023: Implement quantity update inline
- [x] TASK-21C-024: Implement remove item with animation
- [ ] TASK-21C-025: Add upsell products section
- [x] TASK-21C-026: Handle empty cart state
- [x] TASK-21C-027: Add keyboard accessibility (focus trap, Escape)
- [x] TASK-21C-028: Connect cart icon click to open drawer

### Phase 4: Mega Menu Redesign
- [ ] TASK-21C-029: Redesign mega menu with visual category cards
- [ ] TASK-21C-030: Add featured products section to mega menu
- [ ] TASK-21C-031: Implement product count badges on categories
- [ ] TASK-21C-032: Add quick "Add to Cart" on featured products
- [ ] TASK-21C-033: Update mega menu animations (reduce motion)
- [ ] TASK-21C-034: Ensure mega menu accessibility (ARIA, keyboard)

### Phase 5: Mobile Navigation
- [ ] TASK-21C-035: Create BottomNavComponent
- [ ] TASK-21C-036: Implement tab items (Home, Products, Search, Cart, Menu)
- [ ] TASK-21C-037: Add cart badge to bottom nav
- [ ] TASK-21C-038: Create mobile search overlay
- [ ] TASK-21C-039: Create mobile categories bottom sheet
- [ ] TASK-21C-040: Add SwipeGestureDirective for gestures
- [ ] TASK-21C-041: Enhance existing mobile menu panel
- [ ] TASK-21C-042: Add gesture support to menu and cart drawer
- [ ] TASK-21C-043: Test touch targets (44px minimum)

### Phase 6: Language/Theme Controls
- [ ] TASK-21C-044: Simplify LanguageSelectorComponent
- [ ] TASK-21C-045: Move language/theme to header actions
- [ ] TASK-21C-046: Add language/theme to mobile menu footer
- [ ] TASK-21C-047: Update styling for streamlined controls

### Phase 7: Testing & Polish
- [ ] TASK-21C-048: Write unit tests for new header components
- [ ] TASK-21C-049: Write unit tests for SearchService
- [x] TASK-21C-050: Write unit tests for MiniCartDrawerComponent
- [ ] TASK-21C-051: Write E2E tests for search flow
- [ ] TASK-21C-052: Write E2E tests for mini-cart flow
- [ ] TASK-21C-053: Write E2E tests for mobile navigation
- [ ] TASK-21C-054: Cross-browser testing (Chrome, Firefox, Safari)
- [ ] TASK-21C-055: Accessibility audit (screen reader, keyboard)
- [ ] TASK-21C-056: Performance audit (no layout shift, fast interactions)
- [x] TASK-21C-057: Update i18n translations for new strings

---

## Technical Specifications

### Header Component Refactoring Plan

#### File Structure

```
src/app/core/layout/
├── header/
│   ├── header.component.ts              # Container (~150 lines)
│   ├── header.component.spec.ts
│   ├── components/
│   │   ├── header-logo.component.ts     # Logo
│   │   ├── header-nav.component.ts      # Desktop nav links
│   │   ├── header-search.component.ts   # Expandable search
│   │   ├── header-actions.component.ts  # Cart, user, theme, lang
│   │   └── mobile-header.component.ts   # Mobile-specific header
│   └── index.ts                         # Barrel exports
├── mini-cart/
│   ├── mini-cart-drawer.component.ts
│   ├── mini-cart-item.component.ts
│   └── mini-cart-drawer.component.spec.ts
├── mobile-nav/
│   ├── bottom-nav.component.ts
│   ├── mobile-search-overlay.component.ts
│   ├── mobile-categories-sheet.component.ts
│   └── mobile-nav.component.spec.ts
└── mega-menu/                           # Moved from shared
    └── mega-menu.component.ts
```

#### Header Container Component

```typescript
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    HeaderLogoComponent,
    HeaderNavComponent,
    HeaderSearchComponent,
    HeaderActionsComponent,
    MobileHeaderComponent,
    MegaMenuComponent
  ],
  template: `
    <header 
      class="header" 
      [class.header--sticky]="isSticky()"
      [class.header--scrolled]="hasScrolled()"
      data-testid="header"
    >
      <!-- Desktop Header -->
      <div class="header-desktop">
        <div class="header-container">
          <app-header-logo />
          <app-header-nav />
          <app-mega-menu />
          <app-header-search />
          <app-header-actions />
        </div>
      </div>
      
      <!-- Mobile Header -->
      <app-mobile-header class="header-mobile" />
    </header>
  `
})
export class HeaderComponent {
  readonly isSticky = signal(false);
  readonly hasScrolled = signal(false);
  
  @HostListener('window:scroll')
  onScroll(): void {
    const scrollY = window.scrollY;
    this.isSticky.set(scrollY > 0);
    this.hasScrolled.set(scrollY > 100);
  }
}
```

### New Mini-Cart Component

```typescript
// mini-cart-drawer.component.ts
@Component({
  selector: 'app-mini-cart-drawer',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    TranslateModule,
    MiniCartItemComponent
  ],
  template: `
    @if (cartService.miniCartOpen()) {
      <!-- Backdrop -->
      <div 
        class="drawer-backdrop"
        (click)="close()"
        [@fadeIn]
        data-testid="mini-cart-backdrop"
      ></div>
      
      <!-- Drawer -->
      <aside 
        class="mini-cart-drawer"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="'cart.miniCart' | translate"
        [@slideIn]
        (keydown.escape)="close()"
        appSwipeGesture
        (swipeRight)="close()"
        data-testid="mini-cart-drawer"
      >
        <!-- Header -->
        <header class="drawer-header">
          <h2 class="drawer-title">
            {{ 'cart.yourCart' | translate }}
            <span class="item-count">({{ cartService.itemCount() }})</span>
          </h2>
          <button 
            type="button"
            class="close-btn"
            (click)="close()"
            [attr.aria-label]="'common.close' | translate"
            data-testid="mini-cart-close"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd"/>
            </svg>
          </button>
        </header>
        
        <!-- Content -->
        <div class="drawer-content">
          @if (cartService.isEmpty()) {
            <div class="empty-cart">
              <svg class="empty-icon"><!-- cart icon --></svg>
              <p class="empty-text">{{ 'cart.empty' | translate }}</p>
              <a 
                routerLink="/products" 
                class="shop-link"
                (click)="close()"
              >
                {{ 'cart.startShopping' | translate }}
              </a>
            </div>
          } @else {
            <ul class="cart-items" role="list">
              @for (item of cartService.items(); track item.id) {
                <app-mini-cart-item 
                  [item]="item"
                  (quantityChange)="updateQuantity(item.id, $event)"
                  (remove)="removeItem(item.id)"
                />
              }
            </ul>
            
            <!-- Upsells -->
            @if (upsellProducts().length > 0) {
              <section class="upsells">
                <h3>{{ 'cart.youMightLike' | translate }}</h3>
                <div class="upsell-products">
                  @for (product of upsellProducts(); track product.id) {
                    <div class="upsell-product">
                      <img [src]="product.image" [alt]="product.name" />
                      <div class="upsell-info">
                        <span class="upsell-name">{{ product.name }}</span>
                        <span class="upsell-price">{{ product.price | currency }}</span>
                      </div>
                      <button 
                        type="button"
                        class="add-btn"
                        (click)="addToCart(product.id)"
                      >
                        +
                      </button>
                    </div>
                  }
                </div>
              </section>
            }
          }
        </div>
        
        <!-- Footer -->
        @if (!cartService.isEmpty()) {
          <footer class="drawer-footer">
            <div class="subtotal-row">
              <span>{{ 'cart.subtotal' | translate }}</span>
              <span class="subtotal-amount">{{ cartService.subtotal() | currency }}</span>
            </div>
            <p class="shipping-note">{{ 'cart.shippingNote' | translate }}</p>
            <div class="action-buttons">
              <a 
                routerLink="/cart" 
                class="btn btn-secondary"
                (click)="close()"
              >
                {{ 'cart.viewCart' | translate }}
              </a>
              <a 
                routerLink="/checkout" 
                class="btn btn-primary"
                (click)="close()"
              >
                {{ 'cart.checkout' | translate }}
              </a>
            </div>
          </footer>
        }
      </aside>
    }
  `,
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('slideIn', [
      transition(':enter', [
        style({ transform: 'translateX(100%)' }),
        animate('300ms ease-out', style({ transform: 'translateX(0)' }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ transform: 'translateX(100%)' }))
      ])
    ])
  ]
})
export class MiniCartDrawerComponent implements OnInit {
  readonly cartService = inject(CartService);
  private readonly productService = inject(ProductService);
  
  readonly upsellProducts = signal<Product[]>([]);
  
  ngOnInit(): void {
    this.loadUpsellProducts();
  }
  
  close(): void {
    this.cartService.closeMiniCart();
  }
  
  updateQuantity(itemId: string, quantity: number): void {
    this.cartService.updateQuantity(itemId, quantity).subscribe();
  }
  
  removeItem(itemId: string): void {
    this.cartService.removeItem(itemId).subscribe();
  }
  
  addToCart(productId: string): void {
    this.cartService.addToCart(productId, 1).subscribe();
  }
  
  private loadUpsellProducts(): void {
    // Load related/recommended products based on cart contents
    const categorySlug = this.cartService.items()[0]?.categorySlug;
    if (categorySlug) {
      this.productService.getRelated(categorySlug, 3).subscribe(products => {
        this.upsellProducts.set(products);
      });
    }
  }
}
```

### Search Service Updates

```typescript
// search.service.ts
@Injectable({
  providedIn: 'root'
})
export class SearchService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/search`;
  private readonly RECENT_KEY = 'climasite_recent_searches';
  private readonly MAX_RECENT = 5;
  
  // State
  readonly query = signal('');
  readonly suggestions = signal<SearchSuggestion[]>([]);
  readonly recentSearches = signal<string[]>([]);
  readonly popularProducts = signal<SearchSuggestion[]>([]);
  readonly isLoading = signal(false);
  readonly isOpen = signal(false);
  
  // Computed
  readonly hasResults = computed(() => 
    this.suggestions().length > 0 || 
    this.recentSearches().length > 0 ||
    this.popularProducts().length > 0
  );
  
  constructor() {
    this.loadRecentSearches();
    this.loadPopularProducts();
  }
  
  search(query: string): void {
    this.query.set(query);
    
    if (query.length < 2) {
      this.suggestions.set([]);
      return;
    }
    
    this.isLoading.set(true);
    this.fetchSuggestions(query);
  }
  
  private fetchSuggestions = debounce((query: string) => {
    this.http.get<SearchSuggestionsResponse>(`${this.apiUrl}/suggestions`, {
      params: { q: query, limit: '10' }
    }).subscribe({
      next: (response) => {
        const suggestions: SearchSuggestion[] = [
          ...response.products.map(p => ({ ...p, type: 'product' as const })),
          ...response.categories.map(c => ({ ...c, type: 'category' as const }))
        ];
        this.suggestions.set(suggestions);
        this.isLoading.set(false);
      },
      error: () => {
        this.suggestions.set([]);
        this.isLoading.set(false);
      }
    });
  }, 300);
  
  addToRecentSearches(query: string): void {
    const recent = this.recentSearches();
    const filtered = recent.filter(q => q !== query);
    const updated = [query, ...filtered].slice(0, this.MAX_RECENT);
    this.recentSearches.set(updated);
    localStorage.setItem(this.RECENT_KEY, JSON.stringify(updated));
  }
  
  clearRecentSearches(): void {
    this.recentSearches.set([]);
    localStorage.removeItem(this.RECENT_KEY);
  }
  
  private loadRecentSearches(): void {
    const stored = localStorage.getItem(this.RECENT_KEY);
    if (stored) {
      this.recentSearches.set(JSON.parse(stored));
    }
  }
  
  private loadPopularProducts(): void {
    this.http.get<Product[]>(`${this.apiUrl}/popular`, {
      params: { limit: '4' }
    }).subscribe({
      next: (products) => {
        this.popularProducts.set(products.map(p => ({
          ...p,
          type: 'popular' as const
        })));
      }
    });
  }
  
  open(): void {
    this.isOpen.set(true);
  }
  
  close(): void {
    this.isOpen.set(false);
    this.query.set('');
    this.suggestions.set([]);
  }
}
```

### Mobile Navigation Component

```typescript
// bottom-nav.component.ts
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    <nav class="bottom-nav" data-testid="bottom-nav">
      <a 
        routerLink="/" 
        routerLinkActive="active"
        [routerLinkActiveOptions]="{ exact: true }"
        class="nav-item"
        data-testid="bottom-nav-home"
      >
        <svg class="nav-icon"><!-- home icon --></svg>
        <span class="nav-label">{{ 'nav.home' | translate }}</span>
      </a>
      
      <button 
        type="button"
        class="nav-item"
        (click)="openCategories()"
        data-testid="bottom-nav-products"
      >
        <svg class="nav-icon"><!-- grid icon --></svg>
        <span class="nav-label">{{ 'nav.products' | translate }}</span>
      </button>
      
      <button 
        type="button"
        class="nav-item nav-item--search"
        (click)="openSearch()"
        data-testid="bottom-nav-search"
      >
        <div class="search-btn-inner">
          <svg class="nav-icon"><!-- search icon --></svg>
        </div>
      </button>
      
      <button 
        type="button"
        class="nav-item"
        (click)="openCart()"
        data-testid="bottom-nav-cart"
      >
        <div class="cart-icon-wrapper">
          <svg class="nav-icon"><!-- cart icon --></svg>
          @if (cartService.itemCount() > 0) {
            <span class="cart-badge">{{ cartService.itemCount() }}</span>
          }
        </div>
        <span class="nav-label">{{ 'nav.cart' | translate }}</span>
      </button>
      
      <button 
        type="button"
        class="nav-item"
        (click)="openMenu()"
        data-testid="bottom-nav-menu"
      >
        <svg class="nav-icon"><!-- menu icon --></svg>
        <span class="nav-label">{{ 'nav.menu' | translate }}</span>
      </button>
    </nav>
  `,
  styles: [`
    .bottom-nav {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      height: 64px;
      background: var(--color-bg-primary);
      border-top: 1px solid var(--color-border-primary);
      display: flex;
      align-items: center;
      justify-content: space-around;
      padding: 0 8px;
      padding-bottom: env(safe-area-inset-bottom);
      z-index: var(--z-sticky);
      
      /* Only show on mobile */
      @media (min-width: 768px) {
        display: none;
      }
    }
    
    .nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      padding: 8px 12px;
      background: none;
      border: none;
      color: var(--color-text-secondary);
      text-decoration: none;
      cursor: pointer;
      transition: color 0.2s;
      
      &.active,
      &:hover {
        color: var(--color-primary);
      }
    }
    
    .nav-icon {
      width: 24px;
      height: 24px;
    }
    
    .nav-label {
      font-size: 10px;
      font-weight: 500;
    }
    
    .nav-item--search {
      .search-btn-inner {
        width: 48px;
        height: 48px;
        background: var(--gradient-primary-btn);
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-text-inverse);
        margin-top: -16px;
        box-shadow: var(--shadow-lg);
      }
    }
    
    .cart-icon-wrapper {
      position: relative;
    }
    
    .cart-badge {
      position: absolute;
      top: -4px;
      right: -8px;
      min-width: 16px;
      height: 16px;
      padding: 0 4px;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      font-size: 10px;
      font-weight: 600;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `]
})
export class BottomNavComponent {
  readonly cartService = inject(CartService);
  readonly mobileNavService = inject(MobileNavService);
  readonly searchService = inject(SearchService);
  
  openCategories(): void {
    this.mobileNavService.openCategoriesSheet();
  }
  
  openSearch(): void {
    this.searchService.open();
  }
  
  openCart(): void {
    this.cartService.openMiniCart();
  }
  
  openMenu(): void {
    this.mobileNavService.openMenu();
  }
}
```

---

## UX Patterns

### Sticky Header Behavior

| Scroll Position | Header State |
|-----------------|--------------|
| 0px | Normal, full height (64px) |
| 1-50px | Add subtle shadow |
| 50px+ | Glass effect, slight opacity |
| Scroll up | Show header (if hidden) |
| Scroll down fast | Hide header (optional) |

#### CSS Implementation

```scss
.header {
  position: sticky;
  top: 0;
  height: 64px;
  background: var(--color-bg-primary);
  transition: 
    background 200ms ease,
    box-shadow 200ms ease,
    backdrop-filter 200ms ease;
  z-index: var(--z-sticky);
  
  &--sticky {
    box-shadow: 0 1px 0 var(--color-border-primary);
  }
  
  &--scrolled {
    background: var(--glass-bg);
    backdrop-filter: blur(20px) saturate(180%);
    box-shadow: 0 4px 30px var(--shadow-color);
  }
}

// Hide header on scroll down (optional)
.header--hidden {
  transform: translateY(-100%);
}
```

### Scroll-Triggered Changes

| Trigger | Action |
|---------|--------|
| Page load | Header full opacity |
| Scroll > 50px | Glass effect activates |
| Scroll > 200px (optional) | Compact mode (smaller logo) |
| Scroll direction change | Recalculate visibility |

### Mobile Gestures

| Gesture | Element | Action |
|---------|---------|--------|
| Swipe right from left edge | Page | Open menu |
| Swipe left | Open menu | Close menu |
| Swipe right | Mini-cart drawer | Close drawer |
| Pull down | Bottom sheet | Close sheet |
| Tap outside | Overlay | Close all modals |

#### Gesture Implementation Notes

```typescript
// Threshold values
const SWIPE_THRESHOLD = 50;    // Minimum distance for swipe
const EDGE_THRESHOLD = 20;      // Max distance from edge for edge swipe
const VELOCITY_THRESHOLD = 0.3; // Minimum velocity for fast swipe

// Edge detection
const isEdgeSwipe = (touchStartX: number) => touchStartX < EDGE_THRESHOLD;
```

---

## Dependencies

### On Other Plans

| Plan | Dependency | Description |
|------|------------|-------------|
| 21E: Component Library | Icons | Need Lucide icon integration for nav icons |
| 21E: Component Library | Buttons | Unified button styles for header actions |
| 21B: Product Experience | Product Cards | Mini-cart item component styling |
| 21D: Cart & Checkout | Cart Service | Mini-cart state management additions |
| 21F: Animation Audit | Reduced Motion | Honor prefers-reduced-motion |

### External Dependencies

| Dependency | Purpose | Status |
|------------|---------|--------|
| Lucide Icons | Navigation icons | To be added |
| ngx-translate | i18n for new strings | Existing |
| Angular Animations | Drawer/overlay animations | Existing |

### Backend Requirements

| Endpoint | Purpose | Priority |
|----------|---------|----------|
| `GET /api/search/suggestions` | Search autocomplete | Critical |
| `GET /api/search/popular` | Popular products | High |
| `GET /api/products/related` | Upsell products | Medium |

---

## Testing Checklist

### Unit Tests

- [ ] HeaderComponent renders correctly
- [ ] HeaderLogoComponent links to home
- [ ] HeaderNavComponent shows correct links
- [ ] HeaderSearchComponent expands on focus
- [ ] SearchService debounces API calls
- [ ] SearchService manages recent searches
- [ ] MiniCartDrawerComponent opens/closes
- [ ] MiniCartItemComponent updates quantity
- [ ] BottomNavComponent shows cart badge
- [ ] SwipeGestureDirective emits events

### Integration Tests

- [ ] Search suggestions appear on type
- [ ] Clicking suggestion navigates correctly
- [ ] Mini-cart opens when cart icon clicked
- [ ] Mini-cart reflects cart state changes
- [ ] Mega menu shows correct categories
- [ ] Language selector changes app language
- [ ] Theme toggle switches theme

### E2E Tests

```typescript
// tests/ClimaSite.E2E/Tests/navigation/
// header.spec.ts
test.describe('Header', () => {
  test('search shows suggestions', async ({ page }) => {
    await page.goto('/');
    await page.click('[data-testid="header-search"]');
    await page.fill('[data-testid="search-input"]', 'daikin');
    await expect(page.locator('[data-testid="search-suggestions"]')).toBeVisible();
    await expect(page.locator('[data-testid="suggestion-item"]')).toHaveCount.above(0);
  });
  
  test('mini-cart opens on cart click', async ({ page }) => {
    await page.goto('/');
    await page.click('[data-testid="cart-icon"]');
    await expect(page.locator('[data-testid="mini-cart-drawer"]')).toBeVisible();
  });
  
  test('mini-cart closes on backdrop click', async ({ page }) => {
    await page.goto('/');
    await page.click('[data-testid="cart-icon"]');
    await page.click('[data-testid="mini-cart-backdrop"]');
    await expect(page.locator('[data-testid="mini-cart-drawer"]')).not.toBeVisible();
  });
});

// mobile-nav.spec.ts
test.describe('Mobile Navigation', () => {
  test.use({ viewport: { width: 375, height: 667 } });
  
  test('bottom nav is visible on mobile', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('[data-testid="bottom-nav"]')).toBeVisible();
  });
  
  test('bottom nav cart shows badge', async ({ page, request }) => {
    // Add item to cart via API
    const factory = new TestDataFactory(request);
    const product = await factory.createProduct();
    await factory.addToCart(product.id);
    
    await page.goto('/');
    await expect(page.locator('[data-testid="bottom-nav-cart"] .cart-badge')).toContainText('1');
  });
  
  test('tapping search opens search overlay', async ({ page }) => {
    await page.goto('/');
    await page.tap('[data-testid="bottom-nav-search"]');
    await expect(page.locator('[data-testid="mobile-search-overlay"]')).toBeVisible();
  });
});
```

### Accessibility Tests

- [ ] All interactive elements have aria-labels
- [ ] Mini-cart drawer has focus trap
- [ ] Escape key closes all modals/drawers
- [ ] Screen reader announces cart count changes
- [ ] Keyboard navigation works in search dropdown
- [ ] Color contrast meets WCAG AA
- [ ] Touch targets are 44px minimum

### Performance Tests

- [ ] No Cumulative Layout Shift (CLS) on header
- [ ] First Input Delay < 100ms
- [ ] Search debounce prevents excessive API calls
- [ ] Animations respect prefers-reduced-motion
- [ ] Lazy load mega menu content

### Cross-Browser Tests

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] iOS Safari
- [ ] Chrome Android

### Theme Tests

- [ ] Light mode: all components render correctly
- [ ] Dark mode: all components render correctly
- [ ] Theme switch doesn't cause flicker
- [ ] Glass effects work in both themes

### i18n Tests

- [ ] All new strings have EN translations
- [ ] All new strings have BG translations
- [ ] All new strings have DE translations
- [ ] RTL consideration (if applicable)

---

## New i18n Keys Required

```json
{
  "nav": {
    "menu": "Menu"
  },
  "search": {
    "placeholder": "Search products...",
    "recentSearches": "Recent Searches",
    "clearRecent": "Clear",
    "popularProducts": "Popular Products",
    "categories": "Categories",
    "noResults": "No results found",
    "searchFor": "Search for \"{query}\""
  },
  "cart": {
    "yourCart": "Your Cart",
    "miniCart": "Shopping Cart Preview",
    "empty": "Your cart is empty",
    "startShopping": "Start Shopping",
    "subtotal": "Subtotal",
    "viewCart": "View Cart",
    "checkout": "Checkout",
    "shippingNote": "Shipping calculated at checkout",
    "youMightLike": "You Might Also Like",
    "remove": "Remove",
    "itemAdded": "Item added to cart"
  }
}
```

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Search API latency | Medium | Medium | Debounce, loading states, fallback to local filtering |
| Complex header refactor causes regressions | High | Medium | Incremental refactor, comprehensive tests |
| Mobile gestures conflict with browser | Medium | Low | Test on real devices, provide fallbacks |
| Mini-cart performance with many items | Low | Low | Virtual scrolling if >20 items |
| Accessibility issues with new components | High | Low | WCAG audit, screen reader testing |

---

## Success Criteria

| Criteria | Measurement |
|----------|-------------|
| Header height reduced | From ~140px to ~64px (desktop) |
| Mini-cart adoption | >30% of users interact with mini-cart |
| Search engagement | +50% searches with suggestions |
| Mobile UX | Task completion rate maintained or improved |
| Performance | Lighthouse score 90+ |
| Accessibility | Zero WCAG AA violations |
| Code quality | All tests pass, <600 lines per component |

---

*Document created: January 24, 2026*
*Status: Ready for implementation*
*Estimated effort: 5-6 days*
*Priority: High*
