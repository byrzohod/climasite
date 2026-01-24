# Plan 21D: Cart & Checkout Optimization

## Overview

This plan focuses on transforming ClimaSite's cart and checkout experience from functional but plain to a trust-building, streamlined, professional flow that maximizes conversion. The "Nordic Tech" design direction emphasizes clarity, professionalism, and user confidence throughout the purchase journey.

### Current State Summary

| Area | Current | Issues |
|------|---------|--------|
| Cart Page | Glass morphism cards, functional | Plain, no savings display, no upsells |
| Mini-Cart | None | Users must navigate to full cart page |
| Checkout Flow | 3-step wizard (Shipping → Payment → Review) | Fragmented, multiple page loads |
| Payment Icons | Emoji icons (💳, 🅿️, 🏦) | **Unprofessional**, erodes trust |
| Shipping Options | Radio buttons with emojis | No visual cards, no delivery estimates |
| Order Confirmation | Inline in checkout component | No dedicated route, limited next steps |
| Express Checkout | None | Returning customers repeat full flow |
| Installation Services | None | Missing upsell opportunity |

### Goals

1. **Increase checkout completion rate** by reducing friction and building trust
2. **Improve cart engagement** with quick edit capability via mini-cart
3. **Professionalize payment UI** with real brand icons (Visa, Mastercard, PayPal)
4. **Create dedicated confirmation page** with order tracking and next steps
5. **Enable express checkout** for authenticated users with saved addresses
6. **Add installation upsell** to increase average order value
7. **Maintain accessibility** (WCAG 2.1 AA compliance)

### Success Metrics

| Metric | Current Baseline | Target | Measurement |
|--------|-----------------|--------|-------------|
| Cart-to-Checkout Rate | TBD | +15% | Analytics |
| Checkout Completion Rate | TBD | +20% | Analytics |
| Average Order Value | TBD | +10% (with upsells) | Analytics |
| Checkout Time (seconds) | TBD | -25% | Session recording |
| Mobile Checkout Abandonment | TBD | -30% | Analytics |

### Estimated Effort

| Component | Effort | Complexity |
|-----------|--------|------------|
| Cart Page Enhancement | 4-6 hours | Medium |
| Mini-Cart Drawer | 6-8 hours | Medium-High |
| Checkout Flow Redesign | 8-12 hours | High |
| Payment Icons Library | 2-3 hours | Low |
| Shipping Options Redesign | 3-4 hours | Medium |
| Order Confirmation Page | 4-6 hours | Medium |
| Installation Upsell UI | 4-6 hours | Medium |
| Express Checkout | 6-8 hours | High |
| Testing & QA | 8-10 hours | - |
| **Total** | **45-63 hours** | **2-3 days** |

---

## Component Redesigns

### 1. Cart Page Enhancement

**Current:** `src/ClimaSite.Web/src/app/features/cart/cart.component.ts` (838 lines)

The cart page uses glass morphism styling and has good UX patterns (flying cart animation, undo removal), but lacks:
- Savings summary (how much user is saving)
- Recommended products section
- Trust signals
- Express checkout option

#### Design Specifications

```
┌──────────────────────────────────────────────────────────────────────┐
│ Your Cart (3 items)                                    [Continue Shopping] │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ [IMG] Product Name                    $599.00   [-][2][+]  $1,198 │ │
│  │       Variant: 12,000 BTU                                  [×]   │ │
│  │       ✓ In Stock · Ships in 1-2 days                            │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ 🔧 Add Professional Installation                    +$149.00    │ │
│  │    Includes mounting, electrical, and 1-year warranty           │ │
│  │                                          [Add to Order]          │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│ ORDER SUMMARY                                                        │
│ ─────────────────────────                                           │
│ Subtotal (3 items)                                         $1,797.00 │
│ Shipping                                                       FREE  │
│ Tax (20% VAT)                                               $359.40 │
│ ─────────────────────────                                           │
│ 💰 You're saving $200.00                                           │
│ ─────────────────────────                                           │
│ TOTAL                                                     $2,156.40 │
│                                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │         ⚡ Express Checkout (saved address)                      │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │              Proceed to Checkout →                               │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ 🔒 Secure Checkout · 30-Day Returns · Free Support                  │
├──────────────────────────────────────────────────────────────────────┤
│ RECOMMENDED FOR YOU                                                  │
│ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                     │
│ │ Product │ │ Product │ │ Product │ │ Product │                     │
│ │  Card   │ │  Card   │ │  Card   │ │  Card   │                     │
│ └─────────┘ └─────────┘ └─────────┘ └─────────┘                     │
└──────────────────────────────────────────────────────────────────────┘
```

#### New Features

| Feature | Description | Priority |
|---------|-------------|----------|
| Savings Summary | Display total savings from sale prices | High |
| Stock Status | Per-item stock and delivery estimate | High |
| Installation Upsell | Add professional installation service | Medium |
| Express Checkout | One-click for returning customers | Medium |
| Recommended Products | Cross-sell product carousel | Low |
| Trust Strip | Security badges below CTA | High |

---

### 2. Mini-Cart Drawer

**New Component:** `src/ClimaSite.Web/src/app/shared/components/mini-cart/`

A slide-out drawer that appears when adding items to cart, providing quick access without leaving the current page.

#### Design Specifications

```
                                        ┌──────────────────────────────┐
                                        │ YOUR CART (3)          [×]  │
                                        ├──────────────────────────────┤
                                        │                              │
                                        │ ┌──────────────────────────┐ │
                                        │ │ [IMG] Product Name       │ │
                                        │ │       $599 × 2 = $1,198  │ │
                                        │ │       [-] [2] [+]   [🗑] │ │
                                        │ └──────────────────────────┘ │
                                        │                              │
                                        │ ┌──────────────────────────┐ │
                                        │ │ [IMG] Product Name       │ │
                                        │ │       $399 × 1 = $399    │ │
                                        │ │       [-] [1] [+]   [🗑] │ │
                                        │ └──────────────────────────┘ │
                                        │                              │
                                        │ ─────────────────────────── │
                                        │ Subtotal           $1,597.00 │
                                        │ Shipping               FREE  │
                                        │ ─────────────────────────── │
                                        │                              │
                                        │ ┌──────────────────────────┐ │
                                        │ │    Checkout  →           │ │
                                        │ └──────────────────────────┘ │
                                        │ ┌──────────────────────────┐ │
                                        │ │    View Full Cart        │ │
                                        │ └──────────────────────────┘ │
                                        │                              │
                                        │ 🔒 Secure checkout           │
                                        └──────────────────────────────┘
```

#### Component Structure

```typescript
// mini-cart-drawer.component.ts
@Component({
  selector: 'app-mini-cart-drawer',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule]
})
export class MiniCartDrawerComponent {
  private readonly cartService = inject(CartService);
  private readonly miniCartService = inject(MiniCartService);
  
  isOpen = this.miniCartService.isOpen;
  items = this.cartService.items;
  subtotal = this.cartService.subtotal;
  itemCount = this.cartService.itemCount;
  
  close(): void { ... }
  updateQuantity(itemId: string, quantity: number): void { ... }
  removeItem(itemId: string): void { ... }
  proceedToCheckout(): void { ... }
}
```

```typescript
// mini-cart.service.ts
@Injectable({ providedIn: 'root' })
export class MiniCartService {
  private readonly _isOpen = signal(false);
  readonly isOpen = this._isOpen.asReadonly();
  
  open(): void { this._isOpen.set(true); }
  close(): void { this._isOpen.set(false); }
  toggle(): void { this._isOpen.update(v => !v); }
  
  // Auto-open when item added
  openOnAdd(): void {
    this.open();
    setTimeout(() => this.close(), 5000); // Auto-close after 5s
  }
}
```

#### Interaction Patterns

| Trigger | Behavior |
|---------|----------|
| Add to Cart (any page) | Drawer slides in from right, auto-closes after 5s |
| Click Cart Icon (header) | Toggle drawer open/close |
| Click outside drawer | Close drawer |
| Press Escape | Close drawer |
| Click "Checkout" | Close drawer, navigate to /checkout |
| Click "View Full Cart" | Close drawer, navigate to /cart |

#### Accessibility Requirements

- Focus trap when open
- Escape key closes drawer
- Announce changes to screen readers
- Backdrop with aria-hidden for page content

---

### 3. Checkout Flow Redesign

**Current:** `src/ClimaSite.Web/src/app/features/checkout/checkout.component.ts` (1206 lines)

Currently a 3-step wizard with separate views for Shipping, Payment, and Review.

#### Options Considered

| Approach | Pros | Cons |
|----------|------|------|
| **Keep 3-Step** | Familiar, less overwhelming | Extra navigation, slower |
| **Single Page Progressive** | Faster, less abandonment | Can feel long on mobile |
| **Accordion Style** | Best of both, visible progress | More complex implementation |

**Recommendation:** **Accordion-Style Single Page** with collapsible sections

#### Design Specifications

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Secure Checkout                                                    🔒        │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ 1. SHIPPING                                              [✓ Complete]  │  │
│  │ ────────────────────────────────────────────────────────────────────── │  │
│  │ John Doe · 123 Main St, Sofia 1000, Bulgaria                  [Edit]  │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ 2. DELIVERY METHOD                                       [✓ Complete]  │  │
│  │ ────────────────────────────────────────────────────────────────────── │  │
│  │ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐              │  │
│  │ │ 📦 Standard    │ │ 🚀 Express     │ │ ⚡ Next Day    │              │  │
│  │ │ 5-7 days       │ │ 2-3 days       │ │ 1 day          │              │  │
│  │ │ FREE           │ │ $9.99          │ │ $19.99         │              │  │
│  │ │ ✓ Selected     │ │                │ │                │              │  │
│  │ └────────────────┘ └────────────────┘ └────────────────┘              │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ 3. PAYMENT                                                    [Active] │  │
│  │ ────────────────────────────────────────────────────────────────────── │  │
│  │                                                                        │  │
│  │ Select payment method:                                                 │  │
│  │                                                                        │  │
│  │ ┌─────────────────────────────────────────────────────────────────┐   │  │
│  │ │ [VISA] [MC] [AMEX] Credit/Debit Card               ✓ Selected   │   │  │
│  │ └─────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌─────────────────────────────────────────────────────────────────┐   │  │
│  │ │ [PayPal Logo]   PayPal                                          │   │  │
│  │ └─────────────────────────────────────────────────────────────────┘   │  │
│  │ ┌─────────────────────────────────────────────────────────────────┐   │  │
│  │ │ [Bank Icon]     Bank Transfer                                   │   │  │
│  │ └─────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                        │  │
│  │ ┌─────────────────────────────────────────────────────────────────┐   │  │
│  │ │            [Stripe Card Element]                                │   │  │
│  │ └─────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                        │  │
│  │                                       [Continue to Review →]           │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ 4. REVIEW & PAY                                             [Locked]   │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ ORDER SUMMARY (sticky sidebar on desktop)                                    │
│ ──────────────────────────────────────                                      │
│ [IMG] Product 1 × 2                                              $1,198.00  │
│ [IMG] Product 2 × 1                                                $399.00  │
│ ──────────────────────────────────────                                      │
│ Subtotal                                                         $1,597.00  │
│ Shipping (Standard)                                                   FREE  │
│ Tax                                                                $319.40  │
│ ──────────────────────────────────────                                      │
│ TOTAL                                                            $1,916.40  │
│                                                                              │
│ 🔒 Your payment is secure                                                   │
│ [Visa] [MC] [AMEX] [PayPal] [Stripe]                                       │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

### 4. Payment UI - Brand Icons

**Problem:** Current payment method selection uses emoji icons (💳, 🅿️, 🏦) which appear unprofessional and can render differently across devices.

**Solution:** Create a payment icons component library with official brand SVGs.

#### Payment Icons Component

```typescript
// payment-icon.component.ts
@Component({
  selector: 'app-payment-icon',
  standalone: true,
  template: `
    @switch (brand()) {
      @case ('visa') { <svg>...</svg> }
      @case ('mastercard') { <svg>...</svg> }
      @case ('amex') { <svg>...</svg> }
      @case ('paypal') { <svg>...</svg> }
      @case ('apple-pay') { <svg>...</svg> }
      @case ('google-pay') { <svg>...</svg> }
      @case ('bank') { <svg>...</svg> }
      @case ('card-generic') { <svg>...</svg> }
    }
  `,
  styles: [`
    :host { display: inline-flex; }
    svg { height: 24px; width: auto; }
    :host(.lg) svg { height: 32px; }
    :host(.sm) svg { height: 16px; }
  `]
})
export class PaymentIconComponent {
  brand = input.required<PaymentBrand>();
}

type PaymentBrand = 
  | 'visa' 
  | 'mastercard' 
  | 'amex' 
  | 'paypal' 
  | 'apple-pay' 
  | 'google-pay' 
  | 'bank' 
  | 'card-generic';
```

#### Icon Sources

| Brand | Source | License |
|-------|--------|---------|
| Visa | [Visa Brand Resources](https://www.visa.com/brandresources/) | Trademark usage |
| Mastercard | [Mastercard Brand Center](https://brand.mastercard.com/) | Trademark usage |
| American Express | [Amex Merchant Guide](https://www.americanexpress.com/us/merchant/) | Trademark usage |
| PayPal | [PayPal Developer](https://developer.paypal.com/) | Trademark usage |
| Apple Pay | [Apple Pay Identity Guidelines](https://developer.apple.com/apple-pay/) | Apple guidelines |
| Google Pay | [Google Pay Brand Guidelines](https://developers.google.com/pay/api/) | Google guidelines |

---

### 5. Shipping Options Redesign

**Current:** Radio buttons with emoji icons (📦, 🚀, ⚡)

**Target:** Visual cards with delivery dates and pricing

#### Design Specifications

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Choose Delivery Speed                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ ┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐ │
│ │ ┌───┐               │ │ ┌───┐               │ │ ┌───┐               │ │
│ │ │📦 │  Standard     │ │ │🚀 │  Express      │ │ │⚡ │  Next Day     │ │
│ │ └───┘               │ │ └───┘               │ │ └───┘               │ │
│ │                     │ │                     │ │                     │ │
│ │ Arrives: Jan 29-31  │ │ Arrives: Jan 26-27  │ │ Arrives: Tomorrow   │ │
│ │                     │ │                     │ │                     │ │
│ │ ─────────────────── │ │ ─────────────────── │ │ ─────────────────── │ │
│ │                     │ │                     │ │                     │ │
│ │ FREE                │ │ $9.99               │ │ $19.99              │ │
│ │ Orders over $100    │ │                     │ │                     │ │
│ │                     │ │                     │ │                     │ │
│ │ ✓ Selected          │ │                     │ │ ⚡ Fastest          │ │
│ └─────────────────────┘ └─────────────────────┘ └─────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Component Structure

```typescript
@Component({
  selector: 'app-shipping-option-card',
  standalone: true,
  template: `
    <button 
      class="shipping-option"
      [class.selected]="selected()"
      (click)="select.emit()"
      [attr.aria-pressed]="selected()">
      <div class="icon">
        <app-shipping-icon [type]="option().type" />
      </div>
      <div class="details">
        <h4>{{ option().name | translate }}</h4>
        <p class="delivery-date">
          {{ 'checkout.shipping.arrives' | translate }}: 
          {{ option().estimatedDelivery | date:'mediumDate' }}
        </p>
      </div>
      <div class="price">
        @if (option().price === 0) {
          <span class="free">{{ 'checkout.shipping.free' | translate }}</span>
        } @else {
          <span>{{ option().price | currency }}</span>
        }
      </div>
      @if (selected()) {
        <div class="check-mark">✓</div>
      }
    </button>
  `
})
export class ShippingOptionCardComponent {
  option = input.required<ShippingOption>();
  selected = input<boolean>(false);
  select = output<void>();
}
```

---

### 6. Order Confirmation Page

**Current:** Inline confirmation within checkout component (not a separate route)

**Target:** Dedicated `/checkout/confirmation/:orderId` route with rich content

#### Route Configuration

```typescript
// app.routes.ts
{
  path: 'checkout',
  children: [
    { path: '', component: CheckoutComponent },
    { path: 'confirmation/:orderId', component: OrderConfirmationComponent }
  ]
}
```

#### Design Specifications

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                         ┌─────────────────────────┐                          │
│                         │          ✓             │                          │
│                         │     Order Placed!       │                          │
│                         └─────────────────────────┘                          │
│                                                                              │
│                    Thank you for your order, John!                           │
│                                                                              │
│              A confirmation email has been sent to john@example.com          │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ORDER #CLM-2026-001234                         Placed: Jan 24, 2026   │  │
│  │                                                                        │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │                                                                        │  │
│  │  📦 SHIPPING TO                           💳 PAYMENT                   │  │
│  │  John Doe                                 Visa ending in 4242          │  │
│  │  123 Main Street                          $1,916.40 charged            │  │
│  │  Sofia 1000, Bulgaria                                                  │  │
│  │                                                                        │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │                                                                        │  │
│  │  🚚 ESTIMATED DELIVERY                                                 │  │
│  │  Standard Shipping: January 29-31, 2026                                │  │
│  │                                                                        │  │
│  │  ───────────────────────────────────────────────────────────────────  │  │
│  │                                                                        │  │
│  │  ITEMS ORDERED (3)                                                     │  │
│  │  ┌────────────────────────────────────────────────────────────────┐   │  │
│  │  │ [IMG] Arctic Pro 12000 BTU × 2                      $1,198.00  │   │  │
│  │  │ [IMG] Premium Installation Kit × 1                    $399.00  │   │  │
│  │  └────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                        │  │
│  │  Subtotal                                               $1,597.00     │  │
│  │  Shipping                                                    FREE     │  │
│  │  Tax                                                      $319.40     │  │
│  │  ─────────────────────────────────────────────────────────────────   │  │
│  │  TOTAL                                                  $1,916.40     │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  📋 WHAT'S NEXT?                                                       │  │
│  │                                                                        │  │
│  │  1. You'll receive a confirmation email shortly                        │  │
│  │  2. We'll email you when your order ships with tracking info           │  │
│  │  3. Estimated delivery: January 29-31, 2026                            │  │
│  │                                                                        │  │
│  │  Need professional installation? Contact us at +359 888 123 456        │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│     ┌──────────────────────┐    ┌──────────────────────┐                    │
│     │   Track Your Order   │    │  Continue Shopping   │                    │
│     └──────────────────────┘    └──────────────────────┘                    │
│                                                                              │
│  ───────────────────────────────────────────────────────────────────────────│
│                                                                              │
│  📞 NEED HELP?                                                               │
│  Call us: +359 888 123 456 · Email: support@climasite.bg                    │
│  Mon-Fri 9:00-18:00                                                         │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### Component Structure

```typescript
// order-confirmation.component.ts
@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, PaymentIconComponent]
})
export class OrderConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);
  private readonly confettiService = inject(ConfettiService);
  
  order = signal<Order | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  
  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');
    if (orderId) {
      this.loadOrder(orderId);
      this.confettiService.burst(); // Celebrate!
    }
  }
  
  private loadOrder(orderId: string): void { ... }
}
```

---

### 7. Installation Upsell Component

A dedicated component to upsell professional installation services during cart/checkout.

#### Design Specifications

```
┌──────────────────────────────────────────────────────────────────────────┐
│ 🔧 Add Professional Installation                                         │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  [Illustration of technician installing AC unit]                         │
│                                                                          │
│  Expert Installation by Certified Technicians                            │
│                                                                          │
│  ✓ Professional mounting and electrical connection                       │
│  ✓ Vacuum and refrigerant charging                                       │
│  ✓ System testing and calibration                                        │
│  ✓ 1-year installation warranty                                          │
│  ✓ Cleanup of work area                                                   │
│                                                                          │
│  ─────────────────────────────────────────────────────────────────────── │
│                                                                          │
│  Starting at $149.00 per unit                                            │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │                    Add Installation ($149)                          │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Or call +359 888 123 456 for a custom quote                            │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

#### Component Structure

```typescript
// installation-upsell.component.ts
@Component({
  selector: 'app-installation-upsell',
  standalone: true,
  imports: [CommonModule, TranslateModule]
})
export class InstallationUpsellComponent {
  private readonly cartService = inject(CartService);
  
  basePrice = input<number>(149);
  isAdding = signal(false);
  
  eligibleProducts = computed(() => 
    this.cartService.items().filter(item => 
      item.requiresInstallation || item.categorySlug === 'air-conditioners'
    )
  );
  
  totalInstallationCost = computed(() => 
    this.eligibleProducts().length * this.basePrice()
  );
  
  async addInstallation(): Promise<void> {
    this.isAdding.set(true);
    // Add installation service to cart
    await this.cartService.addInstallationService(
      this.eligibleProducts().map(p => p.id)
    );
    this.isAdding.set(false);
  }
}
```

---

## Task List

### Phase 1: Foundation (Priority: Critical)

- [x] TASK-21D-001: Create PaymentIconComponent with SVG icons for Visa, Mastercard, Amex, PayPal
- [ ] TASK-21D-002: Create ShippingIconComponent with SVG icons for delivery types
- [ ] TASK-21D-003: Add payment brand detection utility (card number → brand)
- [ ] TASK-21D-004: Replace all emoji icons in checkout.component.ts with new icon components
- [x] TASK-21D-005: Add translation keys for all new checkout UI strings

### Phase 2: Mini-Cart (Priority: High)

- [ ] TASK-21D-006: Create MiniCartService with open/close/toggle signals
- [ ] TASK-21D-007: Create MiniCartDrawerComponent with slide-in animation
- [ ] TASK-21D-008: Implement quantity controls in mini-cart
- [ ] TASK-21D-009: Implement remove item functionality with confirmation
- [ ] TASK-21D-010: Add keyboard navigation and focus trap
- [ ] TASK-21D-011: Add screen reader announcements for cart changes
- [ ] TASK-21D-012: Integrate mini-cart with header cart icon
- [ ] TASK-21D-013: Auto-open mini-cart when FlyingCartService completes animation
- [ ] TASK-21D-014: Add backdrop overlay with click-to-close

### Phase 3: Cart Page Enhancement (Priority: High)

- [ ] TASK-21D-015: Add savings summary calculation and display
- [ ] TASK-21D-016: Add stock status and delivery estimate per item
- [ ] TASK-21D-017: Create trust badge strip component
- [ ] TASK-21D-018: Add express checkout button for authenticated users
- [ ] TASK-21D-019: Create recommended products section (carousel)
- [ ] TASK-21D-020: Add installation upsell card to cart summary

### Phase 4: Checkout Flow (Priority: High)

- [ ] TASK-21D-021: Refactor checkout to accordion-style single page
- [ ] TASK-21D-022: Create collapsible section component for checkout steps
- [ ] TASK-21D-023: Create ShippingOptionCardComponent with visual design
- [ ] TASK-21D-024: Implement delivery date estimation logic
- [ ] TASK-21D-025: Update payment method selection with icon components
- [ ] TASK-21D-026: Add order summary sticky sidebar on desktop
- [ ] TASK-21D-027: Add progress indicator showing completed steps
- [ ] TASK-21D-028: Implement form validation with inline error messages

### Phase 5: Order Confirmation (Priority: Medium)

- [x] TASK-21D-029: Create /checkout/confirmation/:orderId route
- [x] TASK-21D-030: Create OrderConfirmationComponent
- [x] TASK-21D-031: Design and implement confirmation page layout
- [x] TASK-21D-032: Add "What's Next" section with timeline
- [x] TASK-21D-033: Add order tracking CTA button
- [x] TASK-21D-034: Integrate confetti animation on page load
- [x] TASK-21D-035: Update checkout flow to redirect to confirmation page

### Phase 6: Installation Upsell (Priority: Medium)

- [ ] TASK-21D-036: Create InstallationUpsellComponent
- [ ] TASK-21D-037: Add installation service product type to backend
- [ ] TASK-21D-038: Implement addInstallationService in CartService
- [ ] TASK-21D-039: Add installation eligibility check per product
- [ ] TASK-21D-040: Add installation upsell to cart page
- [ ] TASK-21D-041: Add installation upsell to checkout review step

### Phase 7: Express Checkout (Priority: Medium)

- [ ] TASK-21D-042: Create ExpressCheckoutService
- [ ] TASK-21D-043: Implement one-click checkout for saved addresses
- [ ] TASK-21D-044: Add express checkout button to cart page
- [ ] TASK-21D-045: Add express checkout confirmation modal
- [ ] TASK-21D-046: Handle express checkout API endpoint

### Phase 8: Testing & Polish (Priority: Critical)

- [ ] TASK-21D-047: Write unit tests for MiniCartService
- [x] TASK-21D-048: Write unit tests for PaymentIconComponent
- [ ] TASK-21D-049: Write E2E tests for mini-cart interactions
- [ ] TASK-21D-050: Write E2E tests for checkout flow
- [ ] TASK-21D-051: Write E2E tests for order confirmation page
- [ ] TASK-21D-052: Test all flows on mobile viewport
- [ ] TASK-21D-053: Accessibility audit with axe-core
- [ ] TASK-21D-054: Performance audit with Lighthouse
- [ ] TASK-21D-055: Cross-browser testing (Chrome, Firefox, Safari)

---

## Technical Specifications

### Mini-Cart Component Architecture

```
src/ClimaSite.Web/src/app/
├── shared/
│   └── components/
│       └── mini-cart/
│           ├── mini-cart-drawer.component.ts    # Main drawer component
│           ├── mini-cart-drawer.component.scss  # Styles
│           ├── mini-cart-item.component.ts      # Individual item row
│           └── mini-cart.service.ts             # State management
```

### Payment Icons Library

```
src/ClimaSite.Web/src/app/
├── shared/
│   └── components/
│       └── payment-icons/
│           ├── payment-icon.component.ts        # Icon renderer
│           ├── icons/                           # SVG icon files
│           │   ├── visa.svg
│           │   ├── mastercard.svg
│           │   ├── amex.svg
│           │   ├── paypal.svg
│           │   ├── apple-pay.svg
│           │   ├── google-pay.svg
│           │   └── bank.svg
│           └── payment-brand.util.ts            # Card number → brand
```

### Confirmation Page Routing

```typescript
// Update app.routes.ts
export const routes: Routes = [
  // ... existing routes
  {
    path: 'checkout',
    children: [
      { 
        path: '', 
        component: CheckoutComponent,
        canActivate: [CartNotEmptyGuard]
      },
      { 
        path: 'confirmation/:orderId', 
        component: OrderConfirmationComponent 
      }
    ]
  }
];
```

### Upsell Component Design

```typescript
// installation-upsell.component.ts
interface InstallationService {
  id: string;
  name: string;
  description: string;
  price: number;
  features: string[];
  eligibleCategories: string[];
}

// Backend endpoint
POST /api/cart/installation
{
  "productIds": ["product-1", "product-2"],
  "serviceType": "standard" | "premium"
}
```

---

## Checkout Psychology

### Progress Indication

| Principle | Implementation |
|-----------|----------------|
| **Show Progress** | Numbered steps with completion checkmarks |
| **Reduce Perceived Length** | Accordion collapses completed sections |
| **Encourage Completion** | "Almost done!" messaging at final step |
| **Allow Recovery** | Edit buttons on completed sections |

### Trust Signals During Checkout

| Signal | Placement | Purpose |
|--------|-----------|---------|
| Lock icon in header | Top of checkout | Security assurance |
| Payment brand icons | Payment section, sidebar | Familiar, trusted brands |
| "Secure checkout" text | Multiple locations | Reinforce security |
| SSL badge | Footer of checkout | Technical trust |
| Money-back guarantee | Order summary | Reduce purchase anxiety |
| Customer service contact | Confirmation page | Post-purchase support |

### Error Recovery Patterns

| Error Type | Pattern |
|------------|---------|
| Invalid card | Inline error with suggestion, card element shake |
| Network failure | Retry button with countdown, preserve form state |
| Stock unavailable | Alert with alternatives, don't clear cart |
| Session timeout | Warning before timeout, auto-save progress |
| Payment declined | Specific error message, suggest alternative payment |

### Guest vs Authenticated Flow

| Feature | Guest | Authenticated |
|---------|-------|---------------|
| Express checkout | Not available | Available (with saved address) |
| Saved addresses | Not shown | Selectable list |
| Payment methods | Manual entry | Can use saved cards (future) |
| Order history | Email-based lookup | Account dashboard |
| Cart persistence | Session-based | Account-based |

---

## Dependencies

### On Other Plans

| Plan | Dependency | Blocker? |
|------|------------|----------|
| 21C (Navigation) | Mini-cart integration with header | No |
| 21E (Component Library) | Button, card, icon components | Partial |
| 21G (Trust System) | Trust badge component | No |
| 21B (Product Experience) | Recommended products component | No |

### External Dependencies

| Dependency | Purpose | Status |
|------------|---------|--------|
| Stripe Elements | Payment card input | Already integrated |
| Payment brand SVGs | Professional icons | Need to source |
| ngx-translate | i18n for new strings | Already integrated |

---

## Testing Checklist

### Unit Tests

- [ ] MiniCartService: open, close, toggle, autoClose
- [ ] PaymentIconComponent: renders correct SVG for each brand
- [ ] ShippingOptionCardComponent: selection, accessibility
- [ ] InstallationUpsellComponent: eligibility calculation, add action
- [ ] ExpressCheckoutService: flow validation

### Integration Tests

- [ ] Cart → Mini-cart sync
- [ ] Checkout form → Order creation
- [ ] Payment processing → Confirmation redirect
- [ ] Express checkout flow

### E2E Tests (Playwright)

```typescript
// Example E2E test structure
test.describe('Checkout Flow', () => {
  test('guest user completes checkout with card payment', async ({ page }) => {
    // Add product to cart
    // Open mini-cart, verify item
    // Proceed to checkout
    // Fill shipping form
    // Select shipping method
    // Enter card details
    // Place order
    // Verify confirmation page
  });

  test('authenticated user uses express checkout', async ({ page }) => {
    // Login with saved address
    // Add product to cart
    // Click express checkout
    // Confirm in modal
    // Verify confirmation page
  });
});
```

### Accessibility Tests

- [ ] Mini-cart drawer: focus trap, escape key, ARIA
- [ ] Shipping cards: keyboard selection, aria-pressed
- [ ] Payment options: radio group semantics
- [ ] Confirmation page: heading hierarchy, landmarks

### Cross-Browser Testing

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] iOS Safari
- [ ] Chrome Android

### Theme Testing

- [ ] All components render correctly in light mode
- [ ] All components render correctly in dark mode
- [ ] Contrast ratios meet WCAG AA

### i18n Testing

- [ ] All new strings translated to EN, BG, DE
- [ ] RTL considerations (future)
- [ ] Currency formatting per locale

---

## Migration Notes

### Breaking Changes

1. **Order confirmation route change**: Old `/checkout` inline confirmation → new `/checkout/confirmation/:orderId`
2. **Checkout state management**: Refactored to support accordion flow

### Backward Compatibility

- Existing `/cart` route unchanged
- Existing cart API endpoints unchanged
- Existing order API endpoints unchanged

### Rollback Plan

1. Feature flag for new checkout flow
2. Keep old checkout component until stable
3. A/B test new vs old if needed

---

## File Changes Summary

### New Files

| File | Purpose |
|------|---------|
| `shared/components/mini-cart/mini-cart-drawer.component.ts` | Mini-cart drawer |
| `shared/components/mini-cart/mini-cart-item.component.ts` | Cart item row |
| `shared/components/mini-cart/mini-cart.service.ts` | Drawer state |
| `shared/components/payment-icons/payment-icon.component.ts` | Payment brand icons |
| `shared/components/payment-icons/icons/*.svg` | SVG icon files |
| `shared/components/shipping-option-card/shipping-option-card.component.ts` | Shipping UI |
| `shared/components/installation-upsell/installation-upsell.component.ts` | Upsell UI |
| `features/checkout/confirmation/order-confirmation.component.ts` | Confirmation page |
| `core/services/express-checkout.service.ts` | Express checkout logic |

### Modified Files

| File | Changes |
|------|---------|
| `features/cart/cart.component.ts` | Add savings, upsell, trust badges |
| `features/checkout/checkout.component.ts` | Refactor to accordion, use icon components |
| `core/layout/header/header.component.ts` | Integrate mini-cart |
| `app.routes.ts` | Add confirmation route |
| `assets/i18n/en.json` | New translation keys |
| `assets/i18n/bg.json` | New translation keys |
| `assets/i18n/de.json` | New translation keys |

---

*Document created: January 24, 2026*
*Status: Ready for implementation*
*Estimated effort: 2-3 days*
*Priority: High*
