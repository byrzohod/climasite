# ClimaSite - UI/UX Enhancements Phase 2

## Overview

This plan addresses critical UI/UX issues and implements new features to bring ClimaSite to professional e-commerce standards, modeled after successful HVAC retailers like cdl.bg.

### Key Objectives

1. **Fix User Account Navigation** - Correct user menu behavior when logged in
2. **Improve Theme & Visual Design** - Fix contrast issues, enhance light theme
3. **Implement Mega Menu Navigation** - Categories with subcategories on hover
4. **Enhance Product Filtering** - Advanced filtering capabilities
5. **Upgrade Product Details Page** - Add missing sections and features
6. **Add Professional Navigation** - Complete site navigation structure
7. **Full Testing Coverage** - E2E, unit, and integration tests for all features

---

## Task Index

| Task ID | Description | Priority | Effort |
|---------|-------------|----------|--------|
| **User Account & Navigation** |
| UX2-001 | Fix user icon behavior when logged in | Critical | S |
| UX2-002 | Implement user dropdown menu | Critical | M |
| UX2-003 | Add order history page access | Critical | M |
| UX2-004 | Create account dashboard page | High | L |
| **Theme & Visual Design** |
| UX2-010 | Audit and fix white-on-white text issues | Critical | M |
| UX2-011 | Improve light theme contrast ratios | Critical | M |
| UX2-012 | Enhance visual hierarchy and spacing | High | L |
| UX2-013 | Add subtle shadows and depth | Medium | S |
| UX2-014 | Implement professional color accents | Medium | M |
| **Mega Menu & Categories** |
| UX2-020 | Design category data structure with subcategories | High | M |
| UX2-021 | Seed HVAC category hierarchy (backend) | High | L |
| UX2-022 | Create mega menu component | High | L |
| UX2-023 | Implement hover interactions and animations | High | M |
| UX2-024 | Add category icons and imagery | Medium | M |
| UX2-025 | Mobile-responsive category navigation | High | M |
| **Product Filtering** |
| UX2-030 | Implement price range filter | High | M |
| UX2-031 | Add brand/manufacturer filter | High | M |
| UX2-032 | Create specification-based filters | High | L |
| UX2-033 | Implement filter persistence (URL params) | Medium | M |
| UX2-034 | Add active filters display with remove | Medium | S |
| UX2-035 | Create mobile filter drawer | High | M |
| **Product Details Page** |
| UX2-040 | Implement image gallery with thumbnails | High | M |
| UX2-041 | Add specifications table section | High | M |
| UX2-042 | Create warranty information section | High | S |
| UX2-043 | Add delivery and installation info | High | S |
| UX2-044 | Implement stock status indicator | High | S |
| UX2-045 | Add related products carousel | High | M |
| UX2-046 | Create "Recently Viewed" section | Medium | M |
| UX2-047 | Add product comparison feature | Medium | L |
| UX2-048 | Implement financing/payment options display | Medium | M |
| UX2-049 | Add "Ask a Question" feature | Low | M |
| **Site Navigation** |
| UX2-050 | Implement main navigation menu | High | M |
| UX2-051 | Create Promotions page | High | L |
| UX2-052 | Create Brands page | High | M |
| UX2-053 | Enhance About Us page | Medium | M |
| UX2-054 | Create "Useful/Resources" section | Medium | L |
| UX2-055 | Enhance Contact page | Medium | M |
| UX2-056 | Implement sticky header on scroll | Medium | S |
| UX2-057 | Add search bar to header | High | M |
| **Testing** |
| UX2-060 | E2E tests for user menu flows | Critical | M |
| UX2-061 | E2E tests for category navigation | Critical | M |
| UX2-062 | E2E tests for product filtering | Critical | L |
| UX2-063 | E2E tests for product details page | Critical | M |
| UX2-064 | Unit tests for new components | High | L |
| UX2-065 | Visual regression tests | Medium | M |

---

## Section 1: User Account & Navigation

### UX2-001: Fix User Icon Behavior When Logged In

**Problem**: When logged in, clicking the user icon shows nothing and links to login page instead of account menu.

**Current Behavior**:
- User icon always links to `/login`
- No indication of logged-in state
- No access to account features

**Required Behavior**:
- When NOT logged in: Show login/register options
- When logged in: Show user dropdown menu with:
  - User name/email display
  - My Account link
  - My Orders link
  - Wishlist link
  - Logout button

**Implementation**:

```typescript
// header.component.ts
@Component({
  selector: 'app-header',
  template: `
    <div class="user-menu" (mouseenter)="showDropdown = true" (mouseleave)="showDropdown = false">
      @if (authService.isAuthenticated()) {
        <button class="user-button" [attr.aria-expanded]="showDropdown">
          <span class="user-icon">
            <svg><!-- user icon --></svg>
          </span>
          <span class="user-name">{{ authService.user()?.firstName }}</span>
          <svg class="chevron"><!-- chevron --></svg>
        </button>

        @if (showDropdown) {
          <div class="user-dropdown" data-testid="user-dropdown">
            <div class="dropdown-header">
              <span class="user-email">{{ authService.user()?.email }}</span>
            </div>
            <nav class="dropdown-nav">
              <a routerLink="/account" data-testid="account-link">
                {{ 'nav.account' | translate }}
              </a>
              <a routerLink="/account/orders" data-testid="orders-link">
                {{ 'nav.orders' | translate }}
              </a>
              <a routerLink="/account/wishlist" data-testid="wishlist-link">
                {{ 'nav.wishlist' | translate }}
              </a>
            </nav>
            <div class="dropdown-footer">
              <button (click)="logout()" data-testid="logout-button">
                {{ 'auth.logout' | translate }}
              </button>
            </div>
          </div>
        }
      } @else {
        <a routerLink="/login" class="login-link" data-testid="login-link">
          <svg><!-- user icon --></svg>
          <span>{{ 'auth.login.title' | translate }}</span>
        </a>
      }
    </div>
  `
})
```

**Acceptance Criteria**:
- [ ] User icon shows login link when not authenticated
- [ ] User icon shows dropdown menu when authenticated
- [ ] Dropdown displays user's name and email
- [ ] All navigation links work correctly
- [ ] Logout button clears session and redirects to home
- [ ] Menu closes on click outside
- [ ] Keyboard accessible (Tab, Enter, Escape)

**Tests Required**:
- E2E: User can access orders from dropdown
- E2E: User can logout from dropdown
- Unit: Dropdown visibility toggle logic

---

### UX2-002: Implement User Dropdown Menu

**Design Specifications**:

```scss
.user-dropdown {
  position: absolute;
  top: 100%;
  right: 0;
  min-width: 240px;
  background: var(--color-bg-card);
  border-radius: 8px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
  border: 1px solid var(--color-border-primary);
  z-index: 1000;

  .dropdown-header {
    padding: 1rem;
    border-bottom: 1px solid var(--color-border-primary);

    .user-email {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }
  }

  .dropdown-nav {
    padding: 0.5rem 0;

    a {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      color: var(--color-text-primary);
      transition: background-color 0.15s;

      &:hover {
        background-color: var(--color-bg-hover);
      }

      svg {
        width: 1.25rem;
        height: 1.25rem;
        color: var(--color-text-tertiary);
      }
    }
  }

  .dropdown-footer {
    padding: 0.5rem 1rem 1rem;
    border-top: 1px solid var(--color-border-primary);

    button {
      width: 100%;
      padding: 0.625rem 1rem;
      background: transparent;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      color: var(--color-text-primary);
      cursor: pointer;
      transition: all 0.15s;

      &:hover {
        background-color: var(--color-error-light);
        border-color: var(--color-error);
        color: var(--color-error);
      }
    }
  }
}
```

---

### UX2-003: Add Order History Page Access

**Route Structure**:
```
/account              → Account Dashboard
/account/orders       → Order History List
/account/orders/:id   → Order Details
/account/profile      → Profile Settings
/account/addresses    → Saved Addresses
/account/wishlist     → Wishlist
```

**Order History Page Features**:
- List of all orders with status badges
- Order date, total, item count
- Filter by status (All, Pending, Shipped, Delivered, Cancelled)
- Search by order number
- Quick reorder button
- Pagination

**Order Details Page Features**:
- Order summary with all items
- Shipping tracking (if available)
- Invoice download
- Return/cancel request (where applicable)

---

### UX2-004: Create Account Dashboard Page

**Layout**:
```
┌─────────────────────────────────────────────────────────────┐
│  Account Dashboard                                          │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  Recent     │  │  Wishlist   │  │  Profile    │         │
│  │  Orders (3) │  │  Items (5)  │  │  Settings   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                             │
│  Recent Orders                           [View All →]       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ #ORD-12345  │  Jan 10, 2026  │  $1,299  │  Shipped   │  │
│  │ #ORD-12344  │  Jan 5, 2026   │  $599    │  Delivered │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  Saved Addresses                         [Manage →]         │
│  ┌────────────────────────┐  ┌────────────────────────┐    │
│  │ Home                   │  │ Office                 │    │
│  │ 123 Main St...         │  │ 456 Work Ave...        │    │
│  └────────────────────────┘  └────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

---

## Section 2: Theme & Visual Design

### UX2-010: Audit and Fix White-on-White Text Issues

**Problem**: Some text elements are white/light colored on white backgrounds, making them invisible.

**Audit Checklist**:
- [ ] Form input placeholder text
- [ ] Disabled button text
- [ ] Card headers on light backgrounds
- [ ] Badge text
- [ ] Breadcrumb text
- [ ] Tab labels
- [ ] Dropdown menu items
- [ ] Error messages on light backgrounds

**Color Variable Fixes**:

```scss
// _colors.scss - Light Theme Fixes

:root {
  // Text colors - ensure sufficient contrast
  --color-text-primary: #111827;      // gray-900 - main text
  --color-text-secondary: #4b5563;    // gray-600 - secondary text
  --color-text-tertiary: #6b7280;     // gray-500 - muted text
  --color-text-placeholder: #9ca3af;  // gray-400 - placeholders (4.5:1 contrast)

  // Background colors - add subtle differentiation
  --color-bg-primary: #ffffff;
  --color-bg-secondary: #f9fafb;      // gray-50 - cards, sections
  --color-bg-tertiary: #f3f4f6;       // gray-100 - inputs, hover

  // Border colors - more visible
  --color-border-primary: #e5e7eb;    // gray-200
  --color-border-secondary: #d1d5db;  // gray-300 - active states
  --color-border-focus: #3b82f6;      // blue-500
}
```

---

### UX2-011: Improve Light Theme Contrast Ratios

**WCAG 2.1 Requirements**:
- Normal text: 4.5:1 minimum contrast ratio
- Large text (18pt+): 3:1 minimum contrast ratio
- UI components: 3:1 minimum contrast ratio

**Current Issues**:
1. Too many near-white backgrounds create visual flatness
2. Text lacks sufficient contrast on light gray backgrounds
3. Buttons and interactive elements don't stand out

**Solutions**:

```scss
// Enhanced Light Theme

:root {
  // Primary action color - vibrant blue
  --color-primary: #2563eb;           // blue-600
  --color-primary-hover: #1d4ed8;     // blue-700
  --color-primary-light: #dbeafe;     // blue-100

  // Secondary accent - warm orange for CTAs
  --color-accent: #ea580c;            // orange-600
  --color-accent-hover: #c2410c;      // orange-700

  // Success states - clear green
  --color-success: #16a34a;           // green-600
  --color-success-bg: #dcfce7;        // green-100

  // Enhanced shadows for depth
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
  --shadow-card: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
}

// Card component enhancement
.card {
  background: var(--color-bg-primary);
  border: 1px solid var(--color-border-primary);
  border-radius: 12px;
  box-shadow: var(--shadow-card);

  &:hover {
    box-shadow: var(--shadow-md);
    border-color: var(--color-border-secondary);
  }
}

// Button enhancements
.btn-primary {
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
  box-shadow: 0 2px 4px rgba(37, 99, 235, 0.3);

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(37, 99, 235, 0.4);
  }
}
```

---

### UX2-012: Enhance Visual Hierarchy and Spacing

**Typography Scale**:

```scss
// Typography system
:root {
  // Font sizes
  --text-xs: 0.75rem;      // 12px
  --text-sm: 0.875rem;     // 14px
  --text-base: 1rem;       // 16px
  --text-lg: 1.125rem;     // 18px
  --text-xl: 1.25rem;      // 20px
  --text-2xl: 1.5rem;      // 24px
  --text-3xl: 1.875rem;    // 30px
  --text-4xl: 2.25rem;     // 36px

  // Font weights
  --font-normal: 400;
  --font-medium: 500;
  --font-semibold: 600;
  --font-bold: 700;

  // Line heights
  --leading-tight: 1.25;
  --leading-normal: 1.5;
  --leading-relaxed: 1.625;
}

// Heading styles
h1, .h1 {
  font-size: var(--text-3xl);
  font-weight: var(--font-bold);
  line-height: var(--leading-tight);
  color: var(--color-text-primary);
  letter-spacing: -0.02em;
}

h2, .h2 {
  font-size: var(--text-2xl);
  font-weight: var(--font-semibold);
  line-height: var(--leading-tight);
}

h3, .h3 {
  font-size: var(--text-xl);
  font-weight: var(--font-semibold);
}
```

**Spacing Scale**:

```scss
// Consistent spacing
:root {
  --space-1: 0.25rem;   // 4px
  --space-2: 0.5rem;    // 8px
  --space-3: 0.75rem;   // 12px
  --space-4: 1rem;      // 16px
  --space-5: 1.25rem;   // 20px
  --space-6: 1.5rem;    // 24px
  --space-8: 2rem;      // 32px
  --space-10: 2.5rem;   // 40px
  --space-12: 3rem;     // 48px
  --space-16: 4rem;     // 64px
}

// Section spacing
.section {
  padding: var(--space-12) 0;

  @media (max-width: 768px) {
    padding: var(--space-8) 0;
  }
}

// Card content spacing
.card-body {
  padding: var(--space-6);
}
```

---

## Section 3: Mega Menu & Categories

### UX2-020: Category Data Structure

**Database Schema**:

```sql
-- Categories table with hierarchical support
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    name_bg VARCHAR(255),
    name_de VARCHAR(255),
    slug VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    icon VARCHAR(100),
    image_url VARCHAR(500),
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index for tree queries
CREATE INDEX idx_categories_parent ON categories(parent_id);
CREATE INDEX idx_categories_slug ON categories(slug);
```

**Category Interface**:

```typescript
export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentId?: string;
  icon?: string;
  imageUrl?: string;
  sortOrder: number;
  isActive: boolean;
  children?: Category[];
  productCount?: number;
}

export interface CategoryTree extends Category {
  children: CategoryTree[];
}
```

---

### UX2-021: Seed HVAC Category Hierarchy

**Complete Category Structure** (based on cdl.bg):

```typescript
const categories: CategorySeed[] = [
  {
    name: 'Water Purification Systems',
    name_bg: 'Системи за пречистване на вода',
    slug: 'water-purification',
    icon: 'water-drop',
    children: [
      { name: 'Water Filters', name_bg: 'Филтри за пречистване на вода', slug: 'water-filters' },
      { name: 'Home Water Systems', name_bg: 'Домашна система за пречистване на вода', slug: 'home-water-systems' },
      { name: 'Reverse Osmosis', name_bg: 'Обратна осмоза', slug: 'reverse-osmosis' },
      { name: 'UV Lamps', name_bg: 'UV лампи', slug: 'uv-lamps' },
      { name: 'Filter Columns', name_bg: 'Филтърни колони', slug: 'filter-columns' },
      { name: 'Ionizers', name_bg: 'Йонизатори', slug: 'ionizers' },
      { name: 'Nanofiltration', name_bg: 'Нанофилтрация', slug: 'nanofiltration' },
      { name: 'Consumables', name_bg: 'Консумативи за пречистване на вода', slug: 'water-consumables' },
    ]
  },
  {
    name: 'Water Softening',
    name_bg: 'Омекотяване на вода',
    slug: 'water-softening',
    icon: 'water-soft',
    children: [
      { name: 'Industrial Softeners', name_bg: 'Промишлени омекотители', slug: 'industrial-softeners' },
      { name: 'Household Softeners', name_bg: 'Битови омекотители', slug: 'household-softeners' },
      { name: 'Scale Prevention', name_bg: 'Системи срещу котлен камък', slug: 'scale-prevention' },
    ]
  },
  {
    name: 'Air Conditioning',
    name_bg: 'Климатизация',
    slug: 'air-conditioning',
    icon: 'snowflake',
    children: [
      { name: 'Wall-Mounted AC', name_bg: 'Високостенни климатици', slug: 'wall-mounted-ac' },
      { name: 'Pool Heat Pumps', name_bg: 'Термопомпи за басейн', slug: 'pool-heat-pumps' },
      { name: 'Multi-Split Systems', name_bg: 'Мултисплит системи', slug: 'multi-split' },
      { name: 'Floor AC', name_bg: 'Подови климатици', slug: 'floor-ac' },
      { name: 'Floor-Ceiling AC', name_bg: 'Подово таванни климатици', slug: 'floor-ceiling-ac' },
      { name: 'Cassette AC', name_bg: 'Касетъчни климатици', slug: 'cassette-ac' },
      { name: 'Ducted AC', name_bg: 'Канални климатици', slug: 'ducted-ac' },
      { name: 'Column AC', name_bg: 'Колонни климатици', slug: 'column-ac' },
      { name: 'Heat Pumps', name_bg: 'Термопомпи', slug: 'heat-pumps' },
      { name: 'Air Purifiers', name_bg: 'Въздухопречистватели', slug: 'air-purifiers' },
      { name: 'Dehumidifiers', name_bg: 'Изсушители за въздух', slug: 'dehumidifiers' },
      { name: 'VRV/VRF Systems', name_bg: 'VRV/VRF', slug: 'vrv-vrf' },
      { name: 'Chillers', name_bg: 'Чилъри', slug: 'chillers' },
      { name: 'Accessories', name_bg: 'Аксесоари', slug: 'ac-accessories' },
    ]
  },
  {
    name: 'Ventilation',
    name_bg: 'Вентилация',
    slug: 'ventilation',
    icon: 'fan',
    children: [
      { name: 'JET Fans', name_bg: 'JET Вентилатори', slug: 'jet-fans' },
      { name: 'Hot Air Units', name_bg: 'Топловъздушни апарати', slug: 'hot-air-units' },
      { name: 'Ventilation Elements', name_bg: 'Вентилационни елементи', slug: 'ventilation-elements' },
      { name: 'Heat Recovery Units', name_bg: 'Рекуперативни блокове', slug: 'heat-recovery' },
      { name: 'Valves & Sensors', name_bg: 'Клапи и датчици', slug: 'valves-sensors' },
      { name: 'Axial Fans', name_bg: 'Осови вентилатори', slug: 'axial-fans' },
      { name: 'Roof Fans', name_bg: 'Покривни Вентилатори', slug: 'roof-fans' },
      { name: 'Controls', name_bg: 'Управления', slug: 'controls' },
      { name: 'Explosion-Proof Fans', name_bg: 'Взривозащитени вентилатори', slug: 'explosion-proof-fans' },
      { name: 'Air Curtains', name_bg: 'Въздушни завеси', slug: 'air-curtains' },
      { name: 'Household Fans', name_bg: 'Битови вентилатори', slug: 'household-fans' },
      { name: 'Air Ducts', name_bg: 'Въздуховоди', slug: 'air-ducts' },
      { name: 'Centrifugal Fans', name_bg: 'Центробежни вентилатори', slug: 'centrifugal-fans' },
      { name: 'Duct Fans', name_bg: 'Канални вентилатори', slug: 'duct-fans' },
    ]
  },
  {
    name: 'Heating',
    name_bg: 'Отопление',
    slug: 'heating',
    icon: 'flame',
    children: [
      { name: 'Water Convectors', name_bg: 'Водни конвектори', slug: 'water-convectors' },
      { name: 'Storage Heaters', name_bg: 'Акумулиращи радиатори', slug: 'storage-heaters' },
      { name: 'Electric Towel Rails', name_bg: 'Електрически лири', slug: 'electric-towel-rails' },
      { name: 'Pellet Boilers', name_bg: 'Пелетни котли', slug: 'pellet-boilers' },
      { name: 'Electric Convectors', name_bg: 'Електрически конвектори', slug: 'electric-convectors' },
      { name: 'Pellet Stoves', name_bg: 'Пелетни камини', slug: 'pellet-stoves' },
      { name: 'Combined Boilers', name_bg: 'Комбинирани котли', slug: 'combined-boilers' },
      { name: 'Underfloor Heating', name_bg: 'Подово електрическо отопление', slug: 'underfloor-heating' },
      { name: 'Patio Heaters', name_bg: 'Градинско отопление', slug: 'patio-heaters' },
      { name: 'Wood & Coal Stoves', name_bg: 'Печки на дърва и въглища', slug: 'wood-coal-stoves' },
      { name: 'Panel Radiators', name_bg: 'Панелни радиатори', slug: 'panel-radiators' },
      { name: 'Bathroom Heating', name_bg: 'Електрическо отопление за баня', slug: 'bathroom-heating' },
      { name: 'Radiator Accessories', name_bg: 'Аксесоари за радиатори', slug: 'radiator-accessories' },
      { name: 'Towel Rail Heaters', name_bg: 'Електрически нагреватели за лири', slug: 'towel-rail-heaters' },
      { name: 'Dual Heater Radiators', name_bg: 'Електрически радиатори с два нагревателя', slug: 'dual-heater-radiators' },
      { name: 'Solid Fuel Boilers', name_bg: 'Котли на твърдо гориво', slug: 'solid-fuel-boilers' },
      { name: 'Aluminum Towel Rails', name_bg: 'Алуминиеви лири', slug: 'aluminum-towel-rails' },
      { name: 'Aluminum Radiators', name_bg: 'Алуминиеви радиатори', slug: 'aluminum-radiators' },
      { name: 'Chrome Towel Rails', name_bg: 'Лири хром-никел', slug: 'chrome-towel-rails' },
      { name: 'Gas Fireplaces', name_bg: 'Камини на газ', slug: 'gas-fireplaces' },
      { name: 'Steel Towel Rails', name_bg: 'Стоманени лири', slug: 'steel-towel-rails' },
      { name: 'Infrared Radiators', name_bg: 'Електрически лъчисти радиатори', slug: 'infrared-radiators' },
      { name: 'Gas Boilers', name_bg: 'Газови котли', slug: 'gas-boilers' },
      { name: 'Infrared Heating Film', name_bg: 'Инфрачервено отоплително фолио', slug: 'infrared-film' },
      { name: 'Wood Fireplaces', name_bg: 'Камини на дърва', slug: 'wood-fireplaces' },
    ]
  },
  {
    name: 'Water Heaters',
    name_bg: 'Бойлери',
    slug: 'water-heaters',
    icon: 'water-heater',
    children: [
      { name: 'Vertical Heaters', name_bg: 'Вертикални бойлери', slug: 'vertical-heaters' },
      { name: 'Gas Water Heaters', name_bg: 'Газови бойлери', slug: 'gas-water-heaters' },
      { name: 'Compact Heaters', name_bg: 'Малолитражни бойлери', slug: 'compact-heaters' },
      { name: 'Multi-Position Heaters', name_bg: 'Мултипозицонни бойлери', slug: 'multi-position-heaters' },
      { name: 'Horizontal Heaters', name_bg: 'Хоризонтални бойлери', slug: 'horizontal-heaters' },
      { name: 'Thermodynamic Heaters', name_bg: 'Термодинамични бойлери', slug: 'thermodynamic-heaters' },
      { name: 'Coil Heaters', name_bg: 'Бойлери със серпентина', slug: 'coil-heaters' },
      { name: 'Single Coil Heaters', name_bg: 'Бойлери с 1 серпентина', slug: 'single-coil-heaters' },
      { name: 'Double Coil Heaters', name_bg: 'Бойлери с 2 серпентини', slug: 'double-coil-heaters' },
      { name: 'Buffer Tanks', name_bg: 'Буферни съдове', slug: 'buffer-tanks' },
      { name: 'Instant Heaters', name_bg: 'Проточни бойлери', slug: 'instant-heaters' },
    ]
  },
  {
    name: 'Consumables & Accessories',
    name_bg: 'Консумативи и аксесоари',
    slug: 'consumables-accessories',
    icon: 'tools',
    children: [
      { name: 'Bathroom Accessories', name_bg: 'Аксесоари за баня', slug: 'bathroom-accessories' },
      { name: 'Consumables & Chemicals', name_bg: 'Консумативи и препарати', slug: 'consumables-chemicals' },
    ]
  },
  {
    name: 'Solar Systems',
    name_bg: 'Слънчеви инсталации',
    slug: 'solar-systems',
    icon: 'sun',
    children: [
      { name: 'Selective Collectors', name_bg: 'Колектори селективни', slug: 'selective-collectors' },
      { name: 'Vacuum Tube Collectors', name_bg: 'Колектори вакумнотръбни', slug: 'vacuum-tube-collectors' },
    ]
  },
];
```

---

### UX2-022: Create Mega Menu Component

**Component Structure**:

```typescript
// mega-menu.component.ts
@Component({
  selector: 'app-mega-menu',
  template: `
    <nav class="mega-menu" data-testid="mega-menu">
      <ul class="menu-list">
        @for (category of categories(); track category.id) {
          <li
            class="menu-item"
            (mouseenter)="openCategory(category)"
            (mouseleave)="scheduleClose()"
          >
            <a [routerLink]="['/products']" [queryParams]="{category: category.slug}">
              <span class="category-icon" [innerHTML]="category.icon | safeHtml"></span>
              <span class="category-name">{{ category.name }}</span>
              @if (category.children?.length) {
                <svg class="chevron"><!-- chevron right --></svg>
              }
            </a>

            @if (activeCategory()?.id === category.id && category.children?.length) {
              <div class="submenu-panel" data-testid="submenu-panel">
                <div class="submenu-header">
                  <h3>{{ category.name }}</h3>
                  <a [routerLink]="['/products']" [queryParams]="{category: category.slug}">
                    {{ 'common.viewAll' | translate }} →
                  </a>
                </div>
                <div class="submenu-grid">
                  @for (child of category.children; track child.id) {
                    <a
                      class="submenu-item"
                      [routerLink]="['/products']"
                      [queryParams]="{category: child.slug}"
                      data-testid="submenu-item"
                    >
                      {{ child.name }}
                    </a>
                  }
                </div>
                @if (category.imageUrl) {
                  <div class="submenu-promo">
                    <img [src]="category.imageUrl" [alt]="category.name">
                  </div>
                }
              </div>
            }
          </li>
        }
      </ul>
    </nav>
  `,
  styles: [`
    .mega-menu {
      background: var(--color-bg-primary);
      border-bottom: 1px solid var(--color-border-primary);
    }

    .menu-list {
      display: flex;
      list-style: none;
      margin: 0;
      padding: 0;
      max-width: 1280px;
      margin: 0 auto;
    }

    .menu-item {
      position: relative;

      > a {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 1rem 1.25rem;
        color: var(--color-text-primary);
        font-weight: 500;
        transition: background-color 0.15s;

        &:hover {
          background: var(--color-bg-hover);
        }
      }
    }

    .submenu-panel {
      position: absolute;
      top: 100%;
      left: 0;
      min-width: 600px;
      max-width: 800px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: 0 0 12px 12px;
      box-shadow: var(--shadow-lg);
      padding: 1.5rem;
      z-index: 1000;

      display: grid;
      grid-template-columns: 1fr auto;
      gap: 1.5rem;
    }

    .submenu-header {
      grid-column: 1 / -1;
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--color-border-primary);

      h3 {
        font-size: 1.125rem;
        font-weight: 600;
        margin: 0;
      }
    }

    .submenu-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.5rem;
    }

    .submenu-item {
      padding: 0.5rem 0.75rem;
      border-radius: 6px;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      transition: all 0.15s;

      &:hover {
        background: var(--color-bg-hover);
        color: var(--color-primary);
      }
    }

    .submenu-promo {
      width: 200px;
      border-radius: 8px;
      overflow: hidden;

      img {
        width: 100%;
        height: auto;
      }
    }
  `]
})
export class MegaMenuComponent {
  private readonly categoryService = inject(CategoryService);

  readonly categories = this.categoryService.categoryTree;
  readonly activeCategory = signal<Category | null>(null);

  private closeTimeout?: ReturnType<typeof setTimeout>;

  openCategory(category: Category): void {
    if (this.closeTimeout) {
      clearTimeout(this.closeTimeout);
    }
    this.activeCategory.set(category);
  }

  scheduleClose(): void {
    this.closeTimeout = setTimeout(() => {
      this.activeCategory.set(null);
    }, 150);
  }
}
```

---

## Section 4: Product Filtering

### UX2-030-035: Advanced Filtering System

**Filter Types**:

| Filter | Type | Implementation |
|--------|------|----------------|
| Price Range | Range slider | Min/max input with slider |
| Category | Multi-select | Checkbox list |
| Brand | Multi-select | Searchable checkbox list |
| In Stock | Toggle | Boolean switch |
| Rating | Star selection | 4+ stars, 3+ stars, etc. |
| Specifications | Dynamic | Based on category (BTU, Energy Rating, etc.) |

**Filter Component**:

```typescript
// product-filters.component.ts
@Component({
  selector: 'app-product-filters',
  template: `
    <aside class="filters-sidebar" data-testid="product-filters">
      <!-- Active Filters -->
      @if (hasActiveFilters()) {
        <div class="active-filters">
          <div class="active-filters-header">
            <span>{{ 'filters.active' | translate }}</span>
            <button (click)="clearAllFilters()" data-testid="clear-filters">
              {{ 'filters.clearAll' | translate }}
            </button>
          </div>
          <div class="active-filters-list">
            @for (filter of activeFilters(); track filter.key) {
              <span class="filter-tag">
                {{ filter.label }}
                <button (click)="removeFilter(filter.key)">×</button>
              </span>
            }
          </div>
        </div>
      }

      <!-- Category Filter -->
      <div class="filter-section">
        <h3 class="filter-title">{{ 'filters.category' | translate }}</h3>
        <div class="filter-options">
          @for (category of categories(); track category.id) {
            <label class="filter-checkbox">
              <input
                type="checkbox"
                [checked]="isSelected('category', category.slug)"
                (change)="toggleFilter('category', category.slug)"
              >
              <span>{{ category.name }}</span>
              <span class="count">({{ category.productCount }})</span>
            </label>
          }
        </div>
      </div>

      <!-- Price Range Filter -->
      <div class="filter-section">
        <h3 class="filter-title">{{ 'filters.price' | translate }}</h3>
        <div class="price-range">
          <input
            type="number"
            [value]="priceMin()"
            (input)="updatePriceMin($event)"
            placeholder="Min"
            data-testid="price-min"
          >
          <span>-</span>
          <input
            type="number"
            [value]="priceMax()"
            (input)="updatePriceMax($event)"
            placeholder="Max"
            data-testid="price-max"
          >
        </div>
        <app-range-slider
          [min]="0"
          [max]="maxProductPrice()"
          [values]="[priceMin(), priceMax()]"
          (valuesChange)="updatePriceRange($event)"
        />
      </div>

      <!-- Brand Filter -->
      <div class="filter-section">
        <h3 class="filter-title">{{ 'filters.brand' | translate }}</h3>
        <input
          type="search"
          placeholder="{{ 'filters.searchBrands' | translate }}"
          (input)="filterBrands($event)"
          class="brand-search"
        >
        <div class="filter-options scrollable">
          @for (brand of filteredBrands(); track brand.id) {
            <label class="filter-checkbox">
              <input
                type="checkbox"
                [checked]="isSelected('brand', brand.slug)"
                (change)="toggleFilter('brand', brand.slug)"
              >
              <span>{{ brand.name }}</span>
              <span class="count">({{ brand.productCount }})</span>
            </label>
          }
        </div>
      </div>

      <!-- In Stock Toggle -->
      <div class="filter-section">
        <label class="filter-toggle">
          <input
            type="checkbox"
            [checked]="inStockOnly()"
            (change)="toggleInStock()"
            data-testid="in-stock-filter"
          >
          <span>{{ 'filters.inStock' | translate }}</span>
        </label>
      </div>

      <!-- Dynamic Specification Filters -->
      @for (spec of specificationFilters(); track spec.key) {
        <div class="filter-section">
          <h3 class="filter-title">{{ spec.label }}</h3>
          <div class="filter-options">
            @for (option of spec.options; track option.value) {
              <label class="filter-checkbox">
                <input
                  type="checkbox"
                  [checked]="isSelected(spec.key, option.value)"
                  (change)="toggleFilter(spec.key, option.value)"
                >
                <span>{{ option.label }}</span>
                <span class="count">({{ option.count }})</span>
              </label>
            }
          </div>
        </div>
      }
    </aside>
  `
})
```

**URL Parameter Persistence**:

```typescript
// Filter state synced with URL
// /products?category=wall-mounted-ac&brand=panasonic,daikin&price=500-2000&inStock=true

@Injectable({ providedIn: 'root' })
export class FilterService {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly filters = signal<FilterState>({});

  constructor() {
    // Initialize from URL
    this.route.queryParams.pipe(
      map(params => this.parseFilters(params))
    ).subscribe(filters => this.filters.set(filters));
  }

  updateFilter(key: string, value: string | string[] | null): void {
    const current = this.filters();
    const updated = { ...current };

    if (value === null || (Array.isArray(value) && value.length === 0)) {
      delete updated[key];
    } else {
      updated[key] = value;
    }

    this.filters.set(updated);
    this.syncToUrl(updated);
  }

  private syncToUrl(filters: FilterState): void {
    const queryParams: Record<string, string> = {};

    for (const [key, value] of Object.entries(filters)) {
      if (Array.isArray(value)) {
        queryParams[key] = value.join(',');
      } else {
        queryParams[key] = value;
      }
    }

    this.router.navigate([], {
      queryParams,
      queryParamsHandling: 'merge'
    });
  }
}
```

---

## Section 5: Product Details Page

### UX2-040-049: Enhanced Product Details

**Page Layout** (based on cdl.bg):

```
┌─────────────────────────────────────────────────────────────────────┐
│ Breadcrumb: Home > Air Conditioning > Wall-Mounted AC > Product    │
├─────────────────────────────────────────────────────────────────────┤
│ ┌───────────────────────┐  ┌────────────────────────────────────┐  │
│ │                       │  │ Brand: PANASONIC                   │  │
│ │    Main Image         │  │                                    │  │
│ │                       │  │ Panasonic CS/CU-BZ60ZKE White      │  │
│ │                       │  │                                    │  │
│ │                       │  │ ★★★★☆ (4.5) · 23 Reviews           │  │
│ └───────────────────────┘  │                                    │  │
│ [thumb][thumb][thumb][+3]  │ Price: €1,529.79                   │  │
│                            │ (2,992 лв / BGN)                   │  │
│                            │                                    │  │
│                            │ ✓ In Stock (5 available)           │  │
│                            │ ✓ Standard Installation Included   │  │
│                            │                                    │  │
│                            │ Quantity: [−] 1 [+]                │  │
│                            │                                    │  │
│                            │ [  Add to Cart  ] [♡ Wishlist]     │  │
│                            │                                    │  │
│                            │ ┌──────────────────────────────┐  │  │
│                            │ │ 🚚 Free Delivery over €100   │  │  │
│                            │ │ 🔧 Installation Available    │  │  │
│                            │ │ 🔄 14-Day Returns            │  │  │
│                            │ └──────────────────────────────┘  │  │
│                            └────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│ [Description] [Specifications] [Reviews (23)] [Warranty]           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ Product Description                                                 │
│ ─────────────────                                                   │
│ The Panasonic BZ60ZKE is a high-efficiency wall-mounted air        │
│ conditioner designed for rooms between 35-40 sq.m...               │
│                                                                     │
│ Key Features:                                                       │
│ • 21,000 BTU cooling capacity                                       │
│ • A++ energy rating (cooling)                                       │
│ • Aerowings technology for optimal airflow                         │
│ • Panasonic Comfort Cloud app control                              │
│ • Ultra-quiet 20dB operation                                        │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Technical Specifications                                            │
│ ─────────────────────────                                           │
│ ┌─────────────────────────┬─────────────────────────────────────┐  │
│ │ Cooling Capacity        │ 21,000 BTU                          │  │
│ │ Heating Capacity        │ 22,000 BTU                          │  │
│ │ Energy Class (Cooling)  │ A++                                 │  │
│ │ Energy Class (Heating)  │ A+                                  │  │
│ │ SEER                    │ 6.8                                 │  │
│ │ SCOP                    │ 4.0                                 │  │
│ │ Noise Level (Indoor)    │ 20 dB(A)                           │  │
│ │ Room Size               │ 35-40 m²                            │  │
│ │ Dimensions (Indoor)     │ 779 x 290 x 209 mm                  │  │
│ │ Weight (Indoor)         │ 10 kg                               │  │
│ │ Warranty                │ 2 years + 5 years compressor        │  │
│ └─────────────────────────┴─────────────────────────────────────┘  │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Warranty & Service                                                  │
│ ──────────────────                                                  │
│ • 2 Years Full Warranty                                             │
│ • 5 Years Compressor Warranty                                       │
│ • Authorized Service Centers Nationwide                             │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Financing Options                                                   │
│ ─────────────────                                                   │
│ Split your purchase into 12 monthly payments of €127.48            │
│ [Learn More About Financing]                                        │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Related Products                                                    │
│ ────────────────                                                    │
│ [Product Card] [Product Card] [Product Card] [Product Card] →      │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Recently Viewed                                                     │
│ ───────────────                                                     │
│ [Product Card] [Product Card] [Product Card]                       │
└─────────────────────────────────────────────────────────────────────┘
```

**Image Gallery Component**:

```typescript
@Component({
  selector: 'app-product-gallery',
  template: `
    <div class="product-gallery" data-testid="product-gallery">
      <div class="main-image-container">
        <img
          [src]="activeImage()"
          [alt]="product().name"
          class="main-image"
          (click)="openLightbox()"
        >
        <button class="zoom-btn" (click)="openLightbox()">
          <svg><!-- zoom icon --></svg>
        </button>
      </div>

      <div class="thumbnail-strip">
        @for (image of product().images; track image.id; let i = $index) {
          <button
            class="thumbnail"
            [class.active]="activeIndex() === i"
            (click)="setActiveImage(i)"
          >
            <img [src]="image.thumbnailUrl" [alt]="'Image ' + (i + 1)">
          </button>
        }
      </div>
    </div>

    @if (lightboxOpen()) {
      <app-lightbox
        [images]="product().images"
        [startIndex]="activeIndex()"
        (close)="closeLightbox()"
      />
    }
  `
})
```

**Specifications Table Component**:

```typescript
@Component({
  selector: 'app-product-specs',
  template: `
    <div class="specifications" data-testid="product-specifications">
      <h2>{{ 'product.specifications' | translate }}</h2>

      <table class="specs-table">
        @for (group of specificationGroups(); track group.name) {
          <tbody>
            <tr class="group-header">
              <th colspan="2">{{ group.name }}</th>
            </tr>
            @for (spec of group.specs; track spec.key) {
              <tr>
                <td class="spec-label">{{ spec.label }}</td>
                <td class="spec-value">{{ spec.value }} {{ spec.unit }}</td>
              </tr>
            }
          </tbody>
        }
      </table>
    </div>
  `,
  styles: [`
    .specs-table {
      width: 100%;
      border-collapse: collapse;

      .group-header th {
        background: var(--color-bg-tertiary);
        padding: 0.75rem 1rem;
        font-weight: 600;
        text-align: left;
        border-bottom: 2px solid var(--color-border-primary);
      }

      tr:nth-child(even) {
        background: var(--color-bg-secondary);
      }

      td {
        padding: 0.75rem 1rem;
        border-bottom: 1px solid var(--color-border-primary);
      }

      .spec-label {
        color: var(--color-text-secondary);
        width: 40%;
      }

      .spec-value {
        font-weight: 500;
      }
    }
  `]
})
```

---

## Section 6: Site Navigation

### UX2-050-057: Navigation Structure

**Header Navigation**:

```
┌─────────────────────────────────────────────────────────────────────┐
│ 📞 0898 567 504  |  📧 info@climasite.com  |  🌐 EN ▼  |  🌙 Theme  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [LOGO]     [──────── Search ────────] [🔍]    [♡] [🛒 Cart (3)]   │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ [Home] [Promotions 🔥] [Products ▼] [Brands] [About] [Blog] [Contact]│
└─────────────────────────────────────────────────────────────────────┘
```

**Navigation Items**:

| Menu Item | Route | Description |
|-----------|-------|-------------|
| Home (Начало) | `/` | Homepage |
| Promotions (Промоции) | `/promotions` | Current deals and discounts |
| Products (Продукти) | `/products` | Opens mega menu on hover |
| Brands (Марки) | `/brands` | Brand listing page |
| About Us (За нас) | `/about` | Company information |
| Useful (Полезно) | `/resources` | Guides, FAQs, blog |
| Contact (Контакти) | `/contact` | Contact form and info |

**Sticky Header Implementation**:

```typescript
@Component({
  selector: 'app-header',
  template: `
    <header
      class="site-header"
      [class.sticky]="isSticky()"
      [class.hidden]="isHidden()"
    >
      <!-- Top bar -->
      <div class="top-bar">
        <div class="contact-info">
          <a href="tel:+359898567504">📞 0898 567 504</a>
          <a href="mailto:info@climasite.com">📧 info@climasite.com</a>
        </div>
        <div class="top-actions">
          <app-language-switcher />
          <app-theme-toggle />
        </div>
      </div>

      <!-- Main header -->
      <div class="main-header">
        <a routerLink="/" class="logo">
          <img src="/assets/logo.svg" alt="ClimaSite">
        </a>

        <app-search-bar class="search" />

        <div class="header-actions">
          <app-user-menu />
          <a routerLink="/wishlist" class="icon-link">
            <svg><!-- heart --></svg>
            @if (wishlistCount() > 0) {
              <span class="badge">{{ wishlistCount() }}</span>
            }
          </a>
          <a routerLink="/cart" class="cart-link">
            <svg><!-- cart --></svg>
            @if (cartCount() > 0) {
              <span class="badge">{{ cartCount() }}</span>
            }
            <span class="cart-total">{{ cartTotal() | currency }}</span>
          </a>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="main-nav">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">
          {{ 'nav.home' | translate }}
        </a>
        <a routerLink="/promotions" routerLinkActive="active" class="promo-link">
          {{ 'nav.promotions' | translate }}
          <span class="hot-badge">🔥</span>
        </a>
        <div class="nav-item has-mega-menu" (mouseenter)="showMegaMenu()" (mouseleave)="hideMegaMenu()">
          <a routerLink="/products" routerLinkActive="active">
            {{ 'nav.products' | translate }}
            <svg class="chevron"><!-- chevron --></svg>
          </a>
          @if (megaMenuVisible()) {
            <app-mega-menu />
          }
        </div>
        <a routerLink="/brands" routerLinkActive="active">
          {{ 'nav.brands' | translate }}
        </a>
        <a routerLink="/about" routerLinkActive="active">
          {{ 'nav.about' | translate }}
        </a>
        <a routerLink="/resources" routerLinkActive="active">
          {{ 'nav.resources' | translate }}
        </a>
        <a routerLink="/contact" routerLinkActive="active">
          {{ 'nav.contact' | translate }}
        </a>
      </nav>
    </header>
  `,
  host: {
    '(window:scroll)': 'onScroll()'
  }
})
export class HeaderComponent {
  private lastScrollY = 0;

  readonly isSticky = signal(false);
  readonly isHidden = signal(false);

  onScroll(): void {
    const currentScrollY = window.scrollY;

    // Make sticky after scrolling 100px
    this.isSticky.set(currentScrollY > 100);

    // Hide on scroll down, show on scroll up
    if (currentScrollY > this.lastScrollY && currentScrollY > 200) {
      this.isHidden.set(true);
    } else {
      this.isHidden.set(false);
    }

    this.lastScrollY = currentScrollY;
  }
}
```

---

## Section 7: Testing Requirements

### Testing Coverage Requirements

As per project guidelines, ALL features must have comprehensive test coverage:

| Test Type | Target | Framework |
|-----------|--------|-----------|
| Unit Tests (Backend) | 80%+ | xUnit |
| Unit Tests (Frontend) | 70%+ | Jasmine/Karma |
| Integration Tests | All endpoints | xUnit + Testcontainers |
| E2E Tests | All user flows | Playwright (NO MOCKING) |

### UX2-060: E2E Tests for User Menu Flows

```typescript
// tests/ClimaSite.E2E/tests/user-menu.spec.ts

import { test, expect } from '@playwright/test';
import { TestDataFactory } from '../fixtures/factories';

test.describe('User Menu', () => {
  let factory: TestDataFactory;

  test.beforeAll(async ({ request }) => {
    factory = new TestDataFactory(request);
  });

  test('shows login link when not authenticated', async ({ page }) => {
    await page.goto('/');

    const loginLink = page.getByTestId('login-link');
    await expect(loginLink).toBeVisible();
    await expect(loginLink).toHaveAttribute('href', '/login');
  });

  test('shows user dropdown when authenticated', async ({ page }) => {
    // Create real user and login
    const user = await factory.createUser({
      email: `test-${Date.now()}@climasite.test`,
      password: 'TestPass123@'
    });

    await page.goto('/login');
    await page.getByTestId('email-input').fill(user.email);
    await page.getByTestId('password-input').fill('TestPass123@');
    await page.getByTestId('login-submit').click();

    await page.waitForURL('/');

    // Hover on user menu
    await page.getByTestId('user-menu-trigger').hover();

    // Verify dropdown is visible
    const dropdown = page.getByTestId('user-dropdown');
    await expect(dropdown).toBeVisible();

    // Verify dropdown links
    await expect(page.getByTestId('account-link')).toBeVisible();
    await expect(page.getByTestId('orders-link')).toBeVisible();
    await expect(page.getByTestId('logout-button')).toBeVisible();
  });

  test('can navigate to orders from dropdown', async ({ page }) => {
    // Login as user with orders
    const user = await factory.createUserWithOrders();
    await factory.loginAs(page, user);

    await page.goto('/');
    await page.getByTestId('user-menu-trigger').hover();
    await page.getByTestId('orders-link').click();

    await expect(page).toHaveURL('/account/orders');
    await expect(page.getByTestId('orders-list')).toBeVisible();
  });

  test('logout clears session and redirects', async ({ page }) => {
    const user = await factory.createUser();
    await factory.loginAs(page, user);

    await page.goto('/');
    await page.getByTestId('user-menu-trigger').hover();
    await page.getByTestId('logout-button').click();

    // Should redirect to home
    await expect(page).toHaveURL('/');

    // Should show login link again
    await expect(page.getByTestId('login-link')).toBeVisible();

    // Protected route should redirect to login
    await page.goto('/account');
    await expect(page).toHaveURL('/login');
  });
});
```

### UX2-061: E2E Tests for Category Navigation

```typescript
// tests/ClimaSite.E2E/tests/category-navigation.spec.ts

test.describe('Category Navigation', () => {
  test('mega menu opens on hover', async ({ page }) => {
    await page.goto('/');

    const productsNav = page.getByRole('link', { name: /products/i });
    await productsNav.hover();

    const megaMenu = page.getByTestId('mega-menu');
    await expect(megaMenu).toBeVisible();
  });

  test('shows subcategories on category hover', async ({ page }) => {
    await page.goto('/');

    const productsNav = page.getByRole('link', { name: /products/i });
    await productsNav.hover();

    const acCategory = page.getByRole('link', { name: /air conditioning/i });
    await acCategory.hover();

    const submenu = page.getByTestId('submenu-panel');
    await expect(submenu).toBeVisible();

    // Check for subcategories
    await expect(page.getByRole('link', { name: /wall-mounted ac/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /heat pumps/i })).toBeVisible();
  });

  test('clicking subcategory navigates to filtered products', async ({ page }) => {
    await page.goto('/');

    await page.getByRole('link', { name: /products/i }).hover();
    await page.getByRole('link', { name: /air conditioning/i }).hover();
    await page.getByTestId('submenu-item').filter({ hasText: 'Wall-Mounted AC' }).click();

    await expect(page).toHaveURL(/\/products\?category=wall-mounted-ac/);

    // Verify filter is active
    await expect(page.getByTestId('active-filter-category')).toContainText('Wall-Mounted AC');
  });
});
```

### UX2-062: E2E Tests for Product Filtering

```typescript
// tests/ClimaSite.E2E/tests/product-filtering.spec.ts

test.describe('Product Filtering', () => {
  test.beforeAll(async ({ request }) => {
    const factory = new TestDataFactory(request);

    // Create test products with different attributes
    await factory.createProducts([
      { name: 'Cheap AC', price: 300, brand: 'Generic', category: 'wall-mounted-ac' },
      { name: 'Mid AC', price: 800, brand: 'Panasonic', category: 'wall-mounted-ac' },
      { name: 'Premium AC', price: 1500, brand: 'Daikin', category: 'wall-mounted-ac' },
    ]);
  });

  test('can filter by price range', async ({ page }) => {
    await page.goto('/products?category=wall-mounted-ac');

    await page.getByTestId('price-min').fill('500');
    await page.getByTestId('price-max').fill('1000');
    await page.keyboard.press('Enter');

    // Wait for filter to apply
    await page.waitForURL(/price=500-1000/);

    // Should only show mid-range product
    const products = page.getByTestId('product-card');
    await expect(products).toHaveCount(1);
    await expect(products.first()).toContainText('Mid AC');
  });

  test('can filter by brand', async ({ page }) => {
    await page.goto('/products?category=wall-mounted-ac');

    await page.getByLabel('Panasonic').check();

    await page.waitForURL(/brand=panasonic/);

    const products = page.getByTestId('product-card');
    await expect(products).toHaveCount(1);
    await expect(products.first()).toContainText('Mid AC');
  });

  test('can combine multiple filters', async ({ page }) => {
    await page.goto('/products');

    // Select category
    await page.getByLabel('Air Conditioning').check();

    // Set price range
    await page.getByTestId('price-min').fill('200');
    await page.getByTestId('price-max').fill('900');

    // Filter to in-stock only
    await page.getByTestId('in-stock-filter').check();

    // Verify URL has all params
    await expect(page).toHaveURL(/category=.*&price=.*&inStock=true/);
  });

  test('can clear all filters', async ({ page }) => {
    await page.goto('/products?category=wall-mounted-ac&brand=panasonic&price=500-1000');

    await page.getByTestId('clear-filters').click();

    await expect(page).toHaveURL('/products');
    await expect(page.getByTestId('active-filters')).not.toBeVisible();
  });

  test('filters persist in URL for sharing', async ({ page }) => {
    // Navigate with filters
    await page.goto('/products?category=heating&brand=daikin&price=1000-2000');

    // Verify filters are applied
    await expect(page.getByTestId('active-filter-category')).toContainText('Heating');
    await expect(page.getByTestId('active-filter-brand')).toContainText('Daikin');

    // Reload page
    await page.reload();

    // Filters should still be active
    await expect(page.getByTestId('active-filter-category')).toContainText('Heating');
  });
});
```

### UX2-063: E2E Tests for Product Details Page

```typescript
// tests/ClimaSite.E2E/tests/product-details.spec.ts

test.describe('Product Details Page', () => {
  let testProduct: Product;

  test.beforeAll(async ({ request }) => {
    const factory = new TestDataFactory(request);

    testProduct = await factory.createProduct({
      name: 'Panasonic BZ60ZKE',
      price: 1529.79,
      images: [
        { url: '/images/product1.jpg', isPrimary: true },
        { url: '/images/product2.jpg' },
        { url: '/images/product3.jpg' },
      ],
      specifications: {
        'Cooling Capacity': '21,000 BTU',
        'Energy Rating': 'A++',
        'Noise Level': '20 dB(A)',
      },
      warranty: '2 years + 5 years compressor',
      stockQuantity: 5,
    });
  });

  test('displays product information correctly', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    await expect(page.getByRole('heading', { level: 1 })).toContainText('Panasonic BZ60ZKE');
    await expect(page.getByTestId('product-price')).toContainText('€1,529.79');
    await expect(page.getByTestId('stock-status')).toContainText('In Stock');
  });

  test('image gallery works correctly', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    const gallery = page.getByTestId('product-gallery');
    const mainImage = gallery.locator('.main-image');
    const thumbnails = gallery.locator('.thumbnail');

    // Should have 3 thumbnails
    await expect(thumbnails).toHaveCount(3);

    // Click second thumbnail
    await thumbnails.nth(1).click();

    // Main image should update
    await expect(mainImage).toHaveAttribute('src', /product2/);

    // Second thumbnail should be active
    await expect(thumbnails.nth(1)).toHaveClass(/active/);
  });

  test('can switch between tabs', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Click specifications tab
    await page.getByRole('tab', { name: /specifications/i }).click();
    await expect(page.getByTestId('product-specifications')).toBeVisible();

    // Click reviews tab
    await page.getByRole('tab', { name: /reviews/i }).click();
    await expect(page.getByTestId('product-reviews')).toBeVisible();

    // Click warranty tab
    await page.getByRole('tab', { name: /warranty/i }).click();
    await expect(page.getByTestId('product-warranty')).toBeVisible();
  });

  test('can add product to cart', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Set quantity
    await page.getByTestId('quantity-increase').click();
    await expect(page.getByTestId('quantity-input')).toHaveValue('2');

    // Add to cart
    await page.getByTestId('add-to-cart').click();

    // Should show success message
    await expect(page.getByTestId('cart-notification')).toBeVisible();

    // Cart badge should update
    await expect(page.getByTestId('cart-count')).toContainText('2');
  });

  test('can add product to wishlist', async ({ page }) => {
    // Login first
    const factory = new TestDataFactory(page.request);
    const user = await factory.createUser();
    await factory.loginAs(page, user);

    await page.goto(`/products/${testProduct.slug}`);

    await page.getByTestId('add-to-wishlist').click();

    // Button should change state
    await expect(page.getByTestId('add-to-wishlist')).toHaveClass(/active/);

    // Wishlist count should update
    await expect(page.getByTestId('wishlist-count')).toContainText('1');
  });

  test('displays related products', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    const relatedSection = page.getByTestId('related-products');
    await expect(relatedSection).toBeVisible();

    const relatedProducts = relatedSection.getByTestId('product-card');
    await expect(relatedProducts.first()).toBeVisible();
  });
});
```

---

## Implementation Priority

### Phase 1: Critical Fixes (Week 1)
| Task | Priority | Description |
|------|----------|-------------|
| UX2-001 | Critical | Fix user icon behavior |
| UX2-002 | Critical | User dropdown menu |
| UX2-003 | Critical | Order history access |
| UX2-010 | Critical | Fix white-on-white text |
| UX2-011 | Critical | Improve contrast ratios |

### Phase 2: Navigation & Categories (Week 2)
| Task | Priority | Description |
|------|----------|-------------|
| UX2-020 | High | Category data structure |
| UX2-021 | High | Seed HVAC categories |
| UX2-022 | High | Mega menu component |
| UX2-023 | High | Hover interactions |
| UX2-050 | High | Main navigation |
| UX2-057 | High | Header search bar |

### Phase 3: Filtering & Product Page (Week 3)
| Task | Priority | Description |
|------|----------|-------------|
| UX2-030-035 | High | Product filtering |
| UX2-040-044 | High | Product page core features |
| UX2-045-046 | High | Related/Recently viewed |

### Phase 4: Polish & Testing (Week 4)
| Task | Priority | Description |
|------|----------|-------------|
| UX2-012-014 | Medium | Visual hierarchy |
| UX2-047-049 | Medium | Compare, financing, Q&A |
| UX2-051-056 | Medium | Additional pages |
| UX2-060-065 | Critical | All testing |

---

## Translation Keys Required

Add to `assets/i18n/*.json`:

```json
{
  "nav": {
    "home": "Home",
    "promotions": "Promotions",
    "products": "Products",
    "brands": "Brands",
    "about": "About Us",
    "resources": "Useful",
    "contact": "Contact",
    "account": "My Account",
    "orders": "My Orders",
    "wishlist": "Wishlist"
  },
  "filters": {
    "active": "Active Filters",
    "clearAll": "Clear All",
    "category": "Category",
    "price": "Price",
    "brand": "Brand",
    "searchBrands": "Search brands...",
    "inStock": "In Stock Only"
  },
  "product": {
    "specifications": "Specifications",
    "description": "Description",
    "reviews": "Reviews",
    "warranty": "Warranty",
    "delivery": "Delivery",
    "relatedProducts": "Related Products",
    "recentlyViewed": "Recently Viewed",
    "addToCart": "Add to Cart",
    "addToWishlist": "Add to Wishlist",
    "inStock": "In Stock",
    "outOfStock": "Out of Stock",
    "freeDelivery": "Free delivery over €100",
    "installationAvailable": "Installation available"
  },
  "common": {
    "viewAll": "View All"
  }
}
```

---

## Definition of Done

Each task is complete when:

- [ ] Feature implemented and working
- [ ] Responsive design verified (mobile, tablet, desktop)
- [ ] Light and dark themes tested
- [ ] All 3 languages working (EN, BG, DE)
- [ ] Unit tests written (80%+ coverage)
- [ ] E2E tests passing
- [ ] No console errors
- [ ] Accessibility verified (keyboard nav, screen reader)
- [ ] Code reviewed and approved

---

*Last Updated: January 11, 2026*
*Version: 1.0.0*
