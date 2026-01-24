# Plan 21G: Trust & Credibility System

## 1. Overview

### Purpose

The Trust & Credibility System establishes visual and functional elements that build customer confidence throughout the ClimaSite shopping experience. In e-commerce, trust signals directly impact conversion rates - studies show that displaying trust badges can increase conversions by 42%. For HVAC products (high-value purchases), trust is even more critical.

### Current State Analysis

| Element | Current State | Impact |
|---------|---------------|--------|
| Payment Trust Badges | None | Customers unsure about payment security |
| Security Badges | None | No visual confirmation of secure checkout |
| Warranty Display | Text only, buried in description | Warranty benefits not prominent |
| Certifications | None | No Energy Star, HVAC industry credibility |
| Review Badges | Basic stars | No verified purchase, no photo reviews |
| Testimonials | Initials only | Low credibility, feels fake |
| Brand Logos | Text ticker | No visual brand partnership proof |
| Support Visibility | Footer link only | Customers can't easily find help |
| Guarantees | None visible | No satisfaction/money-back guarantee |

### Goals

1. **Increase checkout conversion** by 15-25% through trust reinforcement
2. **Reduce cart abandonment** by displaying payment/security badges
3. **Build brand credibility** with certifications and partnerships
4. **Enhance social proof** with verified reviews and real testimonials
5. **Improve support accessibility** with prominent contact options

### Success Metrics

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Checkout Conversion | Baseline | +20% | Analytics |
| Cart Abandonment | Baseline | -15% | Analytics |
| Time on Product Page | Baseline | +10% | Analytics |
| Support Ticket "Trust" Issues | Baseline | -30% | Support Tickets |
| Review Submission Rate | Baseline | +25% | Database |

### Estimated Effort

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 1: Core Trust Badges | 6 tasks | 2-3 days |
| Phase 2: Review Enhancements | 4 tasks | 1-2 days |
| Phase 3: Testimonials & Social Proof | 3 tasks | 1 day |
| Phase 4: Support & Guarantees | 3 tasks | 1 day |
| **Total** | **16 tasks** | **5-7 days** |

---

## 2. Trust Elements

### 2.1 Payment Trust Badges

Display recognized payment processor logos to reassure customers their payment information is secure.

**Badges to Include:**
- Visa
- Mastercard
- American Express
- PayPal
- Apple Pay
- Google Pay
- Stripe Secure Checkout
- PCI DSS Compliant

**Placement:**
- Footer (always visible)
- Checkout payment section
- Cart summary sidebar
- Product page (near Add to Cart)

**Design Guidelines:**
- Use official brand logos (grayscale for subtlety, color on hover)
- Consistent height (24px default, 32px in checkout)
- Horizontal arrangement with equal spacing
- "Secure Payment" label above badges

---

### 2.2 Security Badges

Visual indicators of website and transaction security.

**Badges to Include:**
- SSL/TLS Encrypted (lock icon)
- "Secure Checkout" badge
- "Your Data is Protected" badge
- GDPR Compliant (for EU customers)
- Norton Secured / McAfee SECURE (if applicable)

**Placement:**
- Checkout header (persistent)
- Footer security section
- Form inputs (mini lock icons)
- Payment form (prominent)

**Design Guidelines:**
- Green color (#10b981) for trust association
- Lock icon as universal security symbol
- Subtle animation on checkout entry
- Tooltip with security details on hover

---

### 2.3 Warranty Display

Transform warranty from hidden text to prominent visual feature.

**Warranty Types:**
- Manufacturer Warranty (varies by product)
- Extended Warranty Options
- Installation Warranty
- Parts & Labor Coverage

**Visual Elements:**
- Warranty shield icon
- Duration prominently displayed
- Coverage highlights (bulleted)
- "Warranty Included" badge on product cards
- Expandable warranty details

**Placement:**
- Product card badges
- Product detail page (dedicated section)
- Cart item summary
- Order confirmation
- Post-purchase emails

---

### 2.4 Certification Badges

Industry-specific certifications that establish product quality and compliance.

**Certifications to Display:**

| Certification | Description | Products |
|---------------|-------------|----------|
| Energy Star | Energy efficiency | AC, Heat Pumps |
| AHRI Certified | Performance verified | All HVAC |
| UL Listed | Safety certified | All electrical |
| EPA Certified | Environmental compliance | Refrigerants |
| SEER Rating | Efficiency rating | AC units |
| HSPF Rating | Heating efficiency | Heat pumps |
| ISO 9001 | Quality management | Brand-level |

**Design:**
- Official certification logos
- Rating values displayed (e.g., "SEER 21")
- "What this means" tooltip
- Link to certification verification

**Placement:**
- Product specifications section
- Filter sidebar (filter by certification)
- Product cards (Energy Star badge)
- Brand pages

---

### 2.5 Review Enhancements

Upgrade basic star ratings to comprehensive social proof system.

**New Features:**

| Feature | Description |
|---------|-------------|
| Verified Purchase Badge | Green checkmark for buyers |
| Photo Reviews | Customer-uploaded product images |
| Video Reviews | Short video testimonials |
| Helpful Votes | "Was this helpful?" functionality |
| Review Filters | By rating, verified, with photos |
| Review Highlights | AI-extracted pros/cons summary |
| Reviewer Profile | Purchase history, expertise level |
| Response from ClimaSite | Official responses to reviews |

**Visual Design:**
- Verified badge: Green checkmark + "Verified Purchase"
- Photo thumbnails in review card
- Expand to view full-size photos
- Helpful count: "45 people found this helpful"

---

### 2.6 Testimonial Upgrades

Transform anonymous testimonials into credible customer stories.

**Current → Target:**

| Aspect | Current | Target |
|--------|---------|--------|
| Photo | Initials only | Real customer photos |
| Name | "J.S." | "John Smith, Chicago IL" |
| Context | None | "Purchased: AC Pro 3000, 6 months ago" |
| Format | Text only | Text + optional video |
| Verification | None | Linked to actual order |
| Company | None | Company logo (B2B) |

**New Testimonial Structure:**
```
[Customer Photo] [Video Play Button]
"Quote text here..."
- Full Name, City, State
- Product Purchased | Time Since Purchase
- [Star Rating]
```

**Sources:**
- Post-purchase email requests
- Incentivized review program (10% off next order)
- Video testimonial contests
- B2B customer spotlights

---

### 2.7 Brand Partnerships

Transform text brand ticker into credible partnership display.

**Current:** Text names scrolling
**Target:** Logo carousel with partnership context

**Elements:**
- High-resolution brand logos
- "Authorized Dealer" badges
- Partnership tier indicators
- Link to brand landing pages
- "Trusted by Industry Leaders" headline

**Brands to Feature:**
- Daikin
- Carrier
- Trane
- Lennox
- Mitsubishi Electric
- LG
- Samsung
- Bosch
- Fujitsu
- Rheem

**Design:**
- Logo carousel (auto-scroll, pause on hover)
- Grayscale → color on hover
- Grid layout alternative for desktop
- "Official Partner" badge overlay

---

### 2.8 Support Visibility

Make customer support prominent and accessible.

**Support Channels:**

| Channel | Current | Target |
|---------|---------|--------|
| Phone | Footer only | Header + floating |
| Email | Footer only | Contact form modal |
| Live Chat | None | Chat widget |
| Help Center | Basic FAQ | Searchable knowledge base |
| Response Time | Not shown | "Usually responds in 2 hours" |

**Implementation:**

1. **Floating Support Button**
   - Fixed position (bottom-right)
   - Expands to show options
   - Chat, phone, email icons
   - "Need Help?" label

2. **Header Support Info**
   - Phone number visible
   - Hours of operation
   - "Expert Support" badge

3. **Checkout Support**
   - Prominent "Questions?" link
   - Phone number in checkout header
   - Live chat available

---

### 2.9 Guarantee Badges

Explicit guarantees that reduce purchase anxiety.

**Guarantees to Display:**

| Guarantee | Description | Conditions |
|-----------|-------------|------------|
| 30-Day Money Back | Full refund if not satisfied | Unused, original packaging |
| Price Match | Match competitor prices | Same product, authorized dealer |
| Free Shipping | Orders over $99 | Continental US |
| Expert Installation | Certified installer network | Select products |
| Satisfaction Guarantee | 100% satisfaction or replacement | Within 90 days |

**Visual Design:**
- Shield icon with checkmark
- Bold guarantee text
- "Learn More" link
- Color: Trust green (#10b981)

**Placement:**
- Product pages (above Add to Cart)
- Cart page
- Checkout summary
- Footer guarantee strip

---

## 3. Task List

### Phase 1: Core Trust Badges & Components

- [ ] **TASK-21G-001**: Create TrustBadgeComponent with payment, security, and warranty variants
- [ ] **TASK-21G-002**: Create PaymentBadgesComponent with all payment method logos
- [ ] **TASK-21G-003**: Create SecurityBadgesComponent for SSL, encryption, GDPR indicators
- [ ] **TASK-21G-004**: Create CertificationBadgeComponent for Energy Star, AHRI, UL badges
- [ ] **TASK-21G-005**: Create WarrantyCardComponent with visual warranty display
- [ ] **TASK-21G-006**: Create GuaranteeBadgeComponent for money-back, price match, etc.

### Phase 2: Review System Enhancements

- [ ] **TASK-21G-007**: Add VerifiedPurchaseBadgeComponent to review system
- [ ] **TASK-21G-008**: Implement photo upload for reviews with gallery display
- [ ] **TASK-21G-009**: Add "Helpful" voting system with count display
- [ ] **TASK-21G-010**: Create ReviewFiltersComponent (by rating, verified, with photos)

### Phase 3: Testimonials & Social Proof

- [ ] **TASK-21G-011**: Redesign TestimonialCardComponent with real photos and context
- [ ] **TASK-21G-012**: Create BrandLogosCarouselComponent with partner logos
- [ ] **TASK-21G-013**: Add video testimonial support with inline player

### Phase 4: Support & Footer Trust

- [ ] **TASK-21G-014**: Create FloatingSupportButtonComponent with chat/phone/email
- [ ] **TASK-21G-015**: Create FooterTrustSectionComponent consolidating all trust elements
- [ ] **TASK-21G-016**: Add checkout trust reinforcement strip

---

## 4. Component Specifications

### 4.1 TrustBadgeComponent

**Purpose:** Reusable badge component for various trust indicators.

**File:** `src/ClimaSite.Web/src/app/shared/components/trust-badge/trust-badge.component.ts`

```typescript
@Component({
  selector: 'app-trust-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './trust-badge.component.html',
  styleUrl: './trust-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrustBadgeComponent {
  // Badge type determines icon and styling
  type = input.required<'payment' | 'security' | 'warranty' | 'certification' | 'guarantee'>();
  
  // Badge content
  label = input.required<string>();
  sublabel = input<string>();
  
  // Optional icon override (default based on type)
  icon = input<string>();
  
  // Size variant
  size = input<'sm' | 'md' | 'lg'>('md');
  
  // Show tooltip with more info
  tooltip = input<string>();
  
  // Link for "Learn More"
  link = input<string>();
  
  // Computed icon based on type
  displayIcon = computed(() => {
    if (this.icon()) return this.icon();
    switch (this.type()) {
      case 'payment': return 'credit-card';
      case 'security': return 'shield-check';
      case 'warranty': return 'shield';
      case 'certification': return 'badge-check';
      case 'guarantee': return 'check-circle';
      default: return 'check';
    }
  });
}
```

**Template Structure:**
```html
<div class="trust-badge" [class]="'trust-badge--' + type() + ' trust-badge--' + size()">
  <div class="trust-badge__icon">
    <app-icon [name]="displayIcon()" />
  </div>
  <div class="trust-badge__content">
    <span class="trust-badge__label">{{ label() | translate }}</span>
    @if (sublabel()) {
      <span class="trust-badge__sublabel">{{ sublabel() | translate }}</span>
    }
  </div>
  @if (tooltip()) {
    <app-tooltip [content]="tooltip() | translate" />
  }
</div>
```

**Styling:**
```scss
.trust-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-radius: 0.375rem;
  background: var(--color-surface-variant);
  border: 1px solid var(--color-border);
  
  &--security {
    --badge-color: var(--color-success);
  }
  
  &--warranty {
    --badge-color: var(--color-primary);
  }
  
  &--certification {
    --badge-color: var(--color-warning);
  }
  
  &--guarantee {
    --badge-color: var(--color-success);
  }
  
  &__icon {
    color: var(--badge-color, var(--color-text-secondary));
  }
  
  &__label {
    font-weight: 500;
    color: var(--color-text-primary);
  }
  
  &__sublabel {
    font-size: 0.75rem;
    color: var(--color-text-secondary);
  }
  
  // Size variants
  &--sm {
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
  }
  
  &--lg {
    padding: 0.75rem 1rem;
    font-size: 1rem;
  }
}
```

---

### 4.2 PaymentBadgesComponent

**Purpose:** Display accepted payment methods with official logos.

**File:** `src/ClimaSite.Web/src/app/shared/components/payment-badges/payment-badges.component.ts`

```typescript
@Component({
  selector: 'app-payment-badges',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="payment-badges" [class.payment-badges--compact]="compact()">
      @if (showLabel()) {
        <span class="payment-badges__label">{{ 'trust.securePayment' | translate }}</span>
      }
      <div class="payment-badges__logos">
        @for (method of paymentMethods; track method.id) {
          <img 
            [src]="'/assets/payment/' + method.id + '.svg'"
            [alt]="method.name"
            [title]="method.name"
            class="payment-badges__logo"
            [class.payment-badges__logo--grayscale]="grayscale()"
            loading="lazy"
          />
        }
      </div>
    </div>
  `,
  styleUrl: './payment-badges.component.scss'
})
export class PaymentBadgesComponent {
  compact = input(false);
  showLabel = input(true);
  grayscale = input(false);
  
  paymentMethods = [
    { id: 'visa', name: 'Visa' },
    { id: 'mastercard', name: 'Mastercard' },
    { id: 'amex', name: 'American Express' },
    { id: 'paypal', name: 'PayPal' },
    { id: 'apple-pay', name: 'Apple Pay' },
    { id: 'google-pay', name: 'Google Pay' },
    { id: 'stripe', name: 'Stripe' }
  ];
}
```

**Assets Required:**
- `/assets/payment/visa.svg`
- `/assets/payment/mastercard.svg`
- `/assets/payment/amex.svg`
- `/assets/payment/paypal.svg`
- `/assets/payment/apple-pay.svg`
- `/assets/payment/google-pay.svg`
- `/assets/payment/stripe.svg`

---

### 4.3 CertificationBadgeComponent

**Purpose:** Display product certifications (Energy Star, AHRI, etc.).

**File:** `src/ClimaSite.Web/src/app/shared/components/certification-badge/certification-badge.component.ts`

```typescript
export type CertificationType = 
  | 'energy-star'
  | 'ahri'
  | 'ul-listed'
  | 'epa'
  | 'iso-9001';

export interface CertificationInfo {
  type: CertificationType;
  label: string;
  value?: string; // e.g., "SEER 21" for energy ratings
  description: string;
  verificationUrl?: string;
}

@Component({
  selector: 'app-certification-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div 
      class="certification-badge"
      [class.certification-badge--compact]="compact()"
      [attr.aria-label]="certification().label"
    >
      <img 
        [src]="'/assets/certifications/' + certification().type + '.svg'"
        [alt]="certification().label"
        class="certification-badge__logo"
      />
      @if (!compact()) {
        <div class="certification-badge__content">
          <span class="certification-badge__label">{{ certification().label }}</span>
          @if (certification().value) {
            <span class="certification-badge__value">{{ certification().value }}</span>
          }
        </div>
      }
      @if (showTooltip()) {
        <app-tooltip [content]="certification().description" />
      }
    </div>
  `,
  styleUrl: './certification-badge.component.scss'
})
export class CertificationBadgeComponent {
  certification = input.required<CertificationInfo>();
  compact = input(false);
  showTooltip = input(true);
}
```

---

### 4.4 VerifiedPurchaseBadgeComponent

**Purpose:** Display verified purchase indicator on reviews.

**File:** `src/ClimaSite.Web/src/app/shared/components/verified-purchase-badge/verified-purchase-badge.component.ts`

```typescript
@Component({
  selector: 'app-verified-purchase-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <span 
      class="verified-badge"
      [attr.aria-label]="'reviews.verifiedPurchase' | translate"
    >
      <svg class="verified-badge__icon" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
      </svg>
      <span class="verified-badge__text">{{ 'reviews.verifiedPurchase' | translate }}</span>
    </span>
  `,
  styles: [`
    .verified-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      color: var(--color-success);
      font-size: 0.75rem;
      font-weight: 500;
      
      &__icon {
        width: 1rem;
        height: 1rem;
      }
    }
  `]
})
export class VerifiedPurchaseBadgeComponent {}
```

---

### 4.5 WarrantyCardComponent

**Purpose:** Display warranty information prominently.

**File:** `src/ClimaSite.Web/src/app/shared/components/warranty-card/warranty-card.component.ts`

```typescript
export interface WarrantyInfo {
  type: 'manufacturer' | 'extended' | 'installation';
  duration: string; // e.g., "5 years"
  coverage: string[]; // e.g., ["Parts", "Labor", "Compressor"]
  details?: string;
}

@Component({
  selector: 'app-warranty-card',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="warranty-card" [class.warranty-card--expanded]="expanded()">
      <div class="warranty-card__header" (click)="toggleExpanded()">
        <div class="warranty-card__icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            <path d="M9 12l2 2 4-4"/>
          </svg>
        </div>
        <div class="warranty-card__info">
          <span class="warranty-card__type">{{ getTypeLabel() | translate }}</span>
          <span class="warranty-card__duration">{{ warranty().duration }}</span>
        </div>
        <button class="warranty-card__toggle" [attr.aria-expanded]="expanded()">
          <svg [class.rotate]="expanded()" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
          </svg>
        </button>
      </div>
      
      @if (expanded()) {
        <div class="warranty-card__details" [@expandCollapse]>
          <h4>{{ 'warranty.coverage' | translate }}:</h4>
          <ul class="warranty-card__coverage">
            @for (item of warranty().coverage; track item) {
              <li>
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                </svg>
                {{ item }}
              </li>
            }
          </ul>
          @if (warranty().details) {
            <p class="warranty-card__description">{{ warranty().details }}</p>
          }
          <a class="warranty-card__link" routerLink="/warranty-terms">
            {{ 'warranty.viewTerms' | translate }}
          </a>
        </div>
      }
    </div>
  `,
  styleUrl: './warranty-card.component.scss',
  animations: [
    trigger('expandCollapse', [
      state('void', style({ height: '0', opacity: 0 })),
      state('*', style({ height: '*', opacity: 1 })),
      transition('void <=> *', animate('200ms ease-in-out'))
    ])
  ]
})
export class WarrantyCardComponent {
  warranty = input.required<WarrantyInfo>();
  
  expanded = signal(false);
  
  toggleExpanded(): void {
    this.expanded.update(v => !v);
  }
  
  getTypeLabel(): string {
    const labels: Record<string, string> = {
      'manufacturer': 'warranty.manufacturer',
      'extended': 'warranty.extended',
      'installation': 'warranty.installation'
    };
    return labels[this.warranty().type] || 'warranty.standard';
  }
}
```

---

### 4.6 TestimonialCardComponent (Enhanced)

**Purpose:** Display customer testimonials with photos and context.

**File:** `src/ClimaSite.Web/src/app/shared/components/testimonials/testimonial-card.component.ts`

```typescript
export interface Testimonial {
  id: string;
  quote: string;
  customerName: string;
  customerLocation: string;
  customerPhoto?: string;
  customerCompany?: string;
  companyLogo?: string;
  productPurchased?: string;
  purchaseDate?: Date;
  rating: number;
  videoUrl?: string;
  verified: boolean;
}

@Component({
  selector: 'app-testimonial-card',
  standalone: true,
  imports: [CommonModule, TranslateModule, VerifiedPurchaseBadgeComponent],
  template: `
    <article class="testimonial-card" [class.testimonial-card--featured]="featured()">
      <!-- Video thumbnail if available -->
      @if (testimonial().videoUrl) {
        <button class="testimonial-card__video" (click)="playVideo()">
          <img 
            [src]="testimonial().customerPhoto || '/assets/placeholder-avatar.svg'"
            [alt]="testimonial().customerName"
            class="testimonial-card__video-thumb"
          />
          <div class="testimonial-card__play-button">
            <svg viewBox="0 0 24 24" fill="currentColor">
              <path d="M8 5v14l11-7z"/>
            </svg>
          </div>
        </button>
      }
      
      <blockquote class="testimonial-card__quote">
        <svg class="testimonial-card__quote-icon" viewBox="0 0 24 24" fill="currentColor">
          <path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z"/>
        </svg>
        "{{ testimonial().quote }}"
      </blockquote>
      
      <footer class="testimonial-card__footer">
        <div class="testimonial-card__customer">
          @if (testimonial().customerPhoto && !testimonial().videoUrl) {
            <img 
              [src]="testimonial().customerPhoto"
              [alt]="testimonial().customerName"
              class="testimonial-card__avatar"
            />
          } @else if (!testimonial().videoUrl) {
            <div class="testimonial-card__avatar-placeholder">
              {{ getInitials() }}
            </div>
          }
          
          <div class="testimonial-card__info">
            <cite class="testimonial-card__name">{{ testimonial().customerName }}</cite>
            <span class="testimonial-card__location">{{ testimonial().customerLocation }}</span>
            
            @if (testimonial().customerCompany) {
              <span class="testimonial-card__company">
                @if (testimonial().companyLogo) {
                  <img [src]="testimonial().companyLogo" [alt]="testimonial().customerCompany" />
                } @else {
                  {{ testimonial().customerCompany }}
                }
              </span>
            }
          </div>
        </div>
        
        <div class="testimonial-card__meta">
          @if (testimonial().rating) {
            <div class="testimonial-card__rating">
              @for (star of [1,2,3,4,5]; track star) {
                <svg 
                  class="star" 
                  [class.filled]="star <= testimonial().rating"
                  viewBox="0 0 20 20" 
                  fill="currentColor"
                >
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                </svg>
              }
            </div>
          }
          
          @if (testimonial().verified) {
            <app-verified-purchase-badge />
          }
          
          @if (testimonial().productPurchased) {
            <span class="testimonial-card__product">
              {{ 'testimonials.purchased' | translate }}: {{ testimonial().productPurchased }}
            </span>
          }
        </div>
      </footer>
    </article>
  `,
  styleUrl: './testimonial-card.component.scss'
})
export class TestimonialCardComponent {
  testimonial = input.required<Testimonial>();
  featured = input(false);
  
  videoPlaying = output<string>();
  
  getInitials(): string {
    return this.testimonial().customerName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase();
  }
  
  playVideo(): void {
    this.videoPlaying.emit(this.testimonial().videoUrl!);
  }
}
```

---

### 4.7 FloatingSupportButtonComponent

**Purpose:** Provide always-accessible support options.

**File:** `src/ClimaSite.Web/src/app/shared/components/floating-support/floating-support.component.ts`

```typescript
@Component({
  selector: 'app-floating-support',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div 
      class="floating-support"
      [class.floating-support--expanded]="expanded()"
      [class.floating-support--minimized]="minimized()"
    >
      <!-- Expanded Menu -->
      @if (expanded()) {
        <div class="floating-support__menu" [@slideUp]>
          <button class="floating-support__option" (click)="startChat()">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
            </svg>
            <span>{{ 'support.liveChat' | translate }}</span>
            <span class="floating-support__status floating-support__status--online">
              {{ 'support.online' | translate }}
            </span>
          </button>
          
          <a class="floating-support__option" href="tel:+1-800-CLIMA-00">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
            </svg>
            <span>1-800-CLIMA-00</span>
            <span class="floating-support__hours">{{ 'support.hours' | translate }}</span>
          </a>
          
          <button class="floating-support__option" (click)="openContactForm()">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
              <polyline points="22,6 12,13 2,6"/>
            </svg>
            <span>{{ 'support.email' | translate }}</span>
            <span class="floating-support__response">{{ 'support.responseTime' | translate }}</span>
          </button>
          
          <a class="floating-support__option" routerLink="/help">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
            <span>{{ 'support.helpCenter' | translate }}</span>
          </a>
        </div>
      }
      
      <!-- Main Toggle Button -->
      <button 
        class="floating-support__toggle"
        (click)="toggle()"
        [attr.aria-expanded]="expanded()"
        [attr.aria-label]="'support.needHelp' | translate"
      >
        @if (expanded()) {
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"/>
            <line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        } @else {
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
          <span class="floating-support__label">{{ 'support.help' | translate }}</span>
        }
      </button>
    </div>
  `,
  styleUrl: './floating-support.component.scss',
  animations: [
    trigger('slideUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'translateY(20px)' }))
      ])
    ])
  ]
})
export class FloatingSupportComponent {
  expanded = signal(false);
  minimized = signal(false);
  
  chatOpened = output<void>();
  contactFormOpened = output<void>();
  
  toggle(): void {
    this.expanded.update(v => !v);
  }
  
  startChat(): void {
    this.chatOpened.emit();
    this.expanded.set(false);
  }
  
  openContactForm(): void {
    this.contactFormOpened.emit();
    this.expanded.set(false);
  }
}
```

---

### 4.8 FooterTrustSectionComponent

**Purpose:** Consolidated trust section for footer.

**File:** `src/ClimaSite.Web/src/app/shared/components/footer-trust-section/footer-trust-section.component.ts`

```typescript
@Component({
  selector: 'app-footer-trust-section',
  standalone: true,
  imports: [
    CommonModule, 
    TranslateModule,
    PaymentBadgesComponent,
    SecurityBadgesComponent,
    GuaranteeBadgeComponent
  ],
  template: `
    <section class="footer-trust" aria-labelledby="footer-trust-title">
      <h3 id="footer-trust-title" class="sr-only">{{ 'trust.sectionTitle' | translate }}</h3>
      
      <div class="footer-trust__grid">
        <!-- Guarantees -->
        <div class="footer-trust__group">
          <h4 class="footer-trust__heading">{{ 'trust.ourPromise' | translate }}</h4>
          <div class="footer-trust__guarantees">
            <app-guarantee-badge type="money-back" duration="30" />
            <app-guarantee-badge type="price-match" />
            <app-guarantee-badge type="free-shipping" threshold="99" />
          </div>
        </div>
        
        <!-- Payment Methods -->
        <div class="footer-trust__group">
          <h4 class="footer-trust__heading">{{ 'trust.securePayment' | translate }}</h4>
          <app-payment-badges [compact]="true" [showLabel]="false" />
        </div>
        
        <!-- Security -->
        <div class="footer-trust__group">
          <h4 class="footer-trust__heading">{{ 'trust.security' | translate }}</h4>
          <app-security-badges />
        </div>
        
        <!-- Support -->
        <div class="footer-trust__group footer-trust__group--support">
          <h4 class="footer-trust__heading">{{ 'trust.support' | translate }}</h4>
          <div class="footer-trust__support-info">
            <a href="tel:+1-800-CLIMA-00" class="footer-trust__phone">
              <svg viewBox="0 0 24 24" fill="currentColor">
                <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/>
              </svg>
              1-800-CLIMA-00
            </a>
            <span class="footer-trust__hours">
              {{ 'support.availableHours' | translate }}
            </span>
          </div>
        </div>
      </div>
    </section>
  `,
  styleUrl: './footer-trust-section.component.scss'
})
export class FooterTrustSectionComponent {}
```

---

## 5. Placement Strategy

### 5.1 Home Page

| Location | Components | Purpose |
|----------|------------|---------|
| Below Hero | Trust badge strip (guarantees) | Immediate trust |
| Testimonials Section | Enhanced testimonial cards | Social proof |
| Brand Section | Logo carousel | Partnership credibility |
| Footer | Full trust section | Reinforcement |

### 5.2 Product Detail Page

| Location | Components | Purpose |
|----------|------------|---------|
| Near Price | Warranty card, certifications | Value reinforcement |
| Above Add to Cart | Guarantee badges | Reduce hesitation |
| Reviews Section | Verified badges, photo reviews | Social proof |
| Sidebar | Support contact | Assistance |
| Sticky Footer (mobile) | Trust badges + CTA | Conversion |

### 5.3 Cart Page

| Location | Components | Purpose |
|----------|------------|---------|
| Order Summary | Payment badges | Payment security |
| Below Total | Guarantee strip | Final reassurance |
| Sidebar | Support contact | Pre-checkout help |

### 5.4 Checkout Flow

| Step | Components | Purpose |
|------|------------|---------|
| Header (all steps) | Security badge, support phone | Persistent trust |
| Shipping Step | Free shipping badge (if applicable) | Value reminder |
| Payment Step | Payment badges, SSL badge | Payment security |
| Review Step | All guarantees | Final confirmation |
| Confirmation | Warranty info, support contact | Post-purchase trust |

### 5.5 Footer (Global)

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Trust & Security Section                         │
├──────────────────┬─────────────────┬─────────────────┬─────────────────┤
│   Our Promise    │  Secure Payment │    Security     │     Support     │
├──────────────────┼─────────────────┼─────────────────┼─────────────────┤
│ ✓ 30-Day Returns │ [Visa] [MC]     │ 🔒 SSL Secured  │ 📞 1-800-XXX    │
│ ✓ Price Match    │ [Amex] [PayPal] │ ✓ GDPR Compliant│ Mon-Fri 8am-8pm │
│ ✓ Free Shipping  │ [Apple] [Google]│ ✓ PCI Compliant │ Sat-Sun 9am-5pm │
└──────────────────┴─────────────────┴─────────────────┴─────────────────┘
```

---

## 6. Content Requirements

### 6.1 Badge Images/SVGs Needed

**Payment Methods:**
```
/assets/payment/
├── visa.svg
├── mastercard.svg
├── amex.svg
├── paypal.svg
├── apple-pay.svg
├── google-pay.svg
├── stripe.svg
└── discover.svg
```

**Certifications:**
```
/assets/certifications/
├── energy-star.svg
├── ahri.svg
├── ul-listed.svg
├── epa.svg
├── iso-9001.svg
└── seer-rating.svg
```

**Brand Logos:**
```
/assets/brands/
├── daikin.svg
├── carrier.svg
├── trane.svg
├── lennox.svg
├── mitsubishi.svg
├── lg.svg
├── samsung.svg
├── bosch.svg
├── fujitsu.svg
└── rheem.svg
```

**Security/Trust Icons:**
```
/assets/trust/
├── ssl-secure.svg
├── gdpr.svg
├── pci-dss.svg
├── shield-check.svg
├── money-back.svg
├── price-match.svg
└── free-shipping.svg
```

### 6.2 Translation Keys

```json
{
  "trust": {
    "sectionTitle": "Trust & Security",
    "securePayment": "Secure Payment",
    "security": "Your Security",
    "ourPromise": "Our Promise",
    "support": "Need Help?",
    "moneyBack": "30-Day Money Back",
    "moneyBackDesc": "Not satisfied? Full refund within 30 days",
    "priceMatch": "Price Match Guarantee",
    "priceMatchDesc": "Found it cheaper? We'll match it",
    "freeShipping": "Free Shipping",
    "freeShippingDesc": "On orders over $99",
    "sslSecure": "SSL Encrypted",
    "gdprCompliant": "GDPR Compliant",
    "pciCompliant": "PCI DSS Compliant",
    "dataProtected": "Your Data is Protected"
  },
  "warranty": {
    "manufacturer": "Manufacturer Warranty",
    "extended": "Extended Warranty",
    "installation": "Installation Warranty",
    "coverage": "Coverage Includes",
    "viewTerms": "View Full Terms",
    "included": "Warranty Included"
  },
  "reviews": {
    "verifiedPurchase": "Verified Purchase",
    "helpful": "Helpful",
    "helpfulCount": "{{count}} people found this helpful",
    "withPhotos": "With Photos",
    "filterByRating": "Filter by Rating"
  },
  "testimonials": {
    "purchased": "Purchased",
    "monthsAgo": "{{count}} months ago",
    "watchVideo": "Watch Video"
  },
  "support": {
    "needHelp": "Need Help?",
    "help": "Help",
    "liveChat": "Live Chat",
    "online": "Online",
    "offline": "Offline",
    "email": "Email Us",
    "responseTime": "Usually responds in 2 hours",
    "helpCenter": "Help Center",
    "hours": "Mon-Fri 8am-8pm EST",
    "availableHours": "Available Mon-Fri 8am-8pm, Sat-Sun 9am-5pm EST"
  },
  "certifications": {
    "energyStar": "ENERGY STAR Certified",
    "energyStarDesc": "Meets strict energy efficiency guidelines set by the EPA",
    "ahri": "AHRI Certified",
    "ahriDesc": "Performance verified by the Air-Conditioning, Heating, and Refrigeration Institute",
    "ulListed": "UL Listed",
    "ulListedDesc": "Tested for safety by Underwriters Laboratories"
  }
}
```

### 6.3 Warranty Terms Display

**Standard Warranty Template:**
```
┌──────────────────────────────────────────┐
│ 🛡️ 5-Year Manufacturer Warranty         │
├──────────────────────────────────────────┤
│ Coverage Includes:                       │
│ ✓ Compressor                            │
│ ✓ All Parts                             │
│ ✓ Labor (Year 1 only)                   │
├──────────────────────────────────────────┤
│ Registration required within 60 days     │
│ [Register Product] [View Full Terms]     │
└──────────────────────────────────────────┘
```

---

## 7. Dependencies

### 7.1 Plan Dependencies

| This Plan | Depends On | Reason |
|-----------|------------|--------|
| 21G | 21E (Component Library) | Base components (Button, Card, Tooltip) |
| 21G | 11 (Reviews & Ratings) | Review system exists |
| 21G | Existing Footer | Footer structure |

### 7.2 Technical Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Lucide Icons | Plan 21E | Or use inline SVGs |
| Angular CDK (Overlay) | Installed | For tooltip, dropdown |
| ngx-translate | Installed | For i18n |
| Animation Service | Exists | For micro-interactions |

### 7.3 Asset Dependencies

| Asset Type | Source | Notes |
|------------|--------|-------|
| Payment Logos | Official SVGs | Download from brand guidelines |
| Certification Logos | Official Sources | Energy Star, AHRI websites |
| Brand Logos | Brand Partners | Request from partners |
| Testimonial Photos | Customers | With permission |

---

## 8. Testing Checklist

### 8.1 Unit Tests

- [ ] TrustBadgeComponent renders correct variant styles
- [ ] PaymentBadgesComponent displays all payment methods
- [ ] CertificationBadgeComponent shows tooltip on hover
- [ ] WarrantyCardComponent expands/collapses correctly
- [ ] VerifiedPurchaseBadgeComponent displays for verified reviews
- [ ] TestimonialCardComponent handles missing photos gracefully
- [ ] FloatingSupportComponent toggles menu correctly
- [ ] FooterTrustSectionComponent renders all sub-components

### 8.2 Integration Tests

- [ ] Trust badges load with correct images
- [ ] Payment badges visible on checkout pages
- [ ] Warranty info pulls from product data
- [ ] Verified badge shows only for verified purchases
- [ ] Support button opens chat widget
- [ ] All components work in both light/dark themes
- [ ] All text uses translation keys

### 8.3 E2E Tests

- [ ] User sees trust badges on home page
- [ ] User sees payment badges during checkout
- [ ] User can expand warranty details on product page
- [ ] User can filter reviews by verified purchase
- [ ] User can access support via floating button
- [ ] Trust section displays correctly on mobile

### 8.4 Accessibility Tests

- [ ] All badges have appropriate alt text
- [ ] Tooltips accessible via keyboard
- [ ] Support button has aria-label
- [ ] Color contrast meets WCAG AA
- [ ] Focus indicators visible
- [ ] Screen reader announces badge content

### 8.5 Visual Regression Tests

- [ ] Trust badges consistent across pages
- [ ] Payment logos aligned correctly
- [ ] Dark mode colors appropriate
- [ ] Mobile responsive layouts
- [ ] RTL support (if applicable)

---

## 9. Implementation Notes

### 9.1 Performance Considerations

1. **Lazy load images**: All badge images should use `loading="lazy"`
2. **SVG sprites**: Consider combining small icons into sprite sheet
3. **Preload critical badges**: Payment badges on checkout should preload
4. **Cache testimonial data**: Testimonials don't change often

### 9.2 A/B Testing Opportunities

| Test | Variants | Hypothesis |
|------|----------|------------|
| Badge placement | Above fold vs. sidebar | Above fold increases conversion |
| Guarantee prominence | Text vs. visual badges | Visual badges more effective |
| Support button | Bottom-right vs. bottom-left | Right position more discoverable |
| Testimonial format | Quote only vs. with photo | Photos increase credibility |

### 9.3 Analytics Events

```typescript
// Track trust element interactions
analytics.track('trust_badge_viewed', {
  badge_type: 'payment' | 'security' | 'warranty' | 'guarantee',
  location: 'checkout' | 'product' | 'footer',
  page: window.location.pathname
});

analytics.track('support_button_clicked', {
  action: 'chat' | 'phone' | 'email' | 'help_center',
  page: window.location.pathname
});

analytics.track('warranty_expanded', {
  product_id: string,
  warranty_type: string
});
```

---

## 10. Rollout Plan

### Phase 1: Foundation (Days 1-2)
1. Create base TrustBadgeComponent
2. Create PaymentBadgesComponent
3. Create SecurityBadgesComponent
4. Add to checkout flow

### Phase 2: Product Trust (Days 3-4)
1. Create CertificationBadgeComponent
2. Create WarrantyCardComponent
3. Enhance existing WarrantyBadgeComponent
4. Add to product pages

### Phase 3: Social Proof (Days 5-6)
1. Create VerifiedPurchaseBadgeComponent
2. Enhance TestimonialCardComponent
3. Create BrandLogosCarouselComponent
4. Add photo review support

### Phase 4: Support & Polish (Day 7)
1. Create FloatingSupportButtonComponent
2. Create FooterTrustSectionComponent
3. Final integration and testing
4. Performance optimization

---

*Document created: January 24, 2026*
*Last updated: January 24, 2026*
*Status: Ready for Implementation*
*Priority: High*
*Estimated Total Effort: 5-7 days*
