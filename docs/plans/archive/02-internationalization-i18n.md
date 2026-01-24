# Internationalization (i18n) Plan

## 1. Overview

This document outlines the comprehensive internationalization strategy for the ClimaSite HVAC e-commerce platform. The implementation enables multi-language support with runtime language switching capability, allowing users to change languages without page reload.

### 1.1 Supported Languages

| Code | Language   | Status  | Direction |
|------|------------|---------|-----------|
| EN   | English    | Default | LTR       |
| BG   | Bulgarian  | Initial | LTR       |
| DE   | German     | Initial | LTR       |

### 1.2 Key Requirements

- **Runtime language switching** - No page reload required
- **All user-facing text translatable** - Including dynamic content
- **Language persistence** - User preference saved in localStorage
- **SEO support** - Proper lang attributes and meta tags
- **E2E testing** - Real language switching tests with Playwright

---

## 2. Architecture

### 2.1 File Structure

```
src/ClimaSite.Web/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   └── services/
│   │   │       └── translation.service.ts
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   └── language-switcher/
│   │   │   │       ├── language-switcher.component.ts
│   │   │   │       ├── language-switcher.component.html
│   │   │   │       └── language-switcher.component.scss
│   │   │   ├── pipes/
│   │   │   │   └── translate-params.pipe.ts
│   │   │   └── directives/
│   │   │       └── translate-attr.directive.ts
│   │   └── app.config.ts
│   └── assets/
│       └── i18n/
│           ├── en.json
│           ├── bg.json
│           └── de.json
├── e2e/
│   └── tests/
│       └── i18n/
│           ├── language-switching.spec.ts
│           ├── translation-coverage.spec.ts
│           └── persistence.spec.ts
```

### 2.2 Translation File Structure

```json
{
  "common": {
    "buttons": {
      "add_to_cart": "Add to Cart",
      "remove_from_cart": "Remove from Cart",
      "checkout": "Checkout",
      "continue_shopping": "Continue Shopping",
      "cancel": "Cancel",
      "save": "Save",
      "delete": "Delete",
      "edit": "Edit",
      "submit": "Submit",
      "back": "Back",
      "next": "Next",
      "previous": "Previous",
      "apply": "Apply",
      "clear": "Clear",
      "search": "Search",
      "filter": "Filter",
      "sort": "Sort",
      "view_details": "View Details",
      "close": "Close"
    },
    "labels": {
      "price": "Price",
      "quantity": "Quantity",
      "total": "Total",
      "subtotal": "Subtotal",
      "tax": "Tax",
      "shipping": "Shipping",
      "discount": "Discount",
      "in_stock": "In Stock",
      "out_of_stock": "Out of Stock",
      "limited_stock": "Limited Stock",
      "free_shipping": "Free Shipping",
      "sale": "Sale",
      "new": "New",
      "featured": "Featured"
    },
    "messages": {
      "loading": "Loading...",
      "error": "An error occurred",
      "success": "Success!",
      "no_results": "No results found",
      "confirm_delete": "Are you sure you want to delete this item?",
      "item_added": "Item added to cart",
      "item_removed": "Item removed from cart"
    },
    "validation": {
      "required": "This field is required",
      "email_invalid": "Please enter a valid email address",
      "min_length": "Minimum {{min}} characters required",
      "max_length": "Maximum {{max}} characters allowed",
      "password_mismatch": "Passwords do not match",
      "invalid_phone": "Please enter a valid phone number"
    }
  },
  "header": {
    "search_placeholder": "Search products...",
    "cart": "Cart",
    "account": "Account",
    "login": "Login",
    "logout": "Logout",
    "register": "Register",
    "my_orders": "My Orders",
    "wishlist": "Wishlist",
    "language": "Language"
  },
  "navigation": {
    "home": "Home",
    "products": "Products",
    "categories": "Categories",
    "air_conditioners": "Air Conditioners",
    "heating_systems": "Heating Systems",
    "ventilation": "Ventilation",
    "accessories": "Accessories",
    "about": "About Us",
    "contact": "Contact",
    "support": "Support",
    "faq": "FAQ"
  },
  "footer": {
    "company_info": "Company Information",
    "customer_service": "Customer Service",
    "follow_us": "Follow Us",
    "newsletter": "Newsletter",
    "newsletter_placeholder": "Enter your email",
    "subscribe": "Subscribe",
    "privacy_policy": "Privacy Policy",
    "terms_of_service": "Terms of Service",
    "shipping_info": "Shipping Information",
    "returns": "Returns & Refunds",
    "copyright": "© {{year}} ClimaSite. All rights reserved."
  },
  "products": {
    "title": "Products",
    "all_products": "All Products",
    "no_results": "No products found",
    "showing_results": "Showing {{count}} products",
    "sort_by": "Sort by",
    "sort_options": {
      "price_low": "Price: Low to High",
      "price_high": "Price: High to Low",
      "name_asc": "Name: A to Z",
      "name_desc": "Name: Z to A",
      "newest": "Newest First",
      "best_selling": "Best Selling",
      "rating": "Highest Rated"
    },
    "filters": {
      "title": "Filters",
      "price_range": "Price Range",
      "brand": "Brand",
      "category": "Category",
      "rating": "Rating",
      "availability": "Availability",
      "clear_all": "Clear All Filters"
    },
    "details": {
      "description": "Description",
      "specifications": "Specifications",
      "reviews": "Reviews",
      "related_products": "Related Products",
      "energy_rating": "Energy Rating",
      "warranty": "Warranty",
      "sku": "SKU",
      "brand": "Brand",
      "model": "Model"
    }
  },
  "cart": {
    "title": "Shopping Cart",
    "empty": "Your cart is empty",
    "items": "{{count}} item(s)",
    "update_quantity": "Update Quantity",
    "remove_item": "Remove Item",
    "clear_cart": "Clear Cart",
    "continue_shopping": "Continue Shopping",
    "proceed_to_checkout": "Proceed to Checkout",
    "order_summary": "Order Summary",
    "promo_code": "Promo Code",
    "apply_code": "Apply Code",
    "estimated_total": "Estimated Total"
  },
  "checkout": {
    "title": "Checkout",
    "steps": {
      "shipping": "Shipping",
      "payment": "Payment",
      "review": "Review",
      "confirmation": "Confirmation"
    },
    "shipping_address": "Shipping Address",
    "billing_address": "Billing Address",
    "same_as_shipping": "Same as shipping address",
    "payment_method": "Payment Method",
    "card_number": "Card Number",
    "expiry_date": "Expiry Date",
    "cvv": "CVV",
    "place_order": "Place Order",
    "order_placed": "Order Placed Successfully!",
    "order_number": "Order Number: {{number}}",
    "confirmation_email": "A confirmation email has been sent to {{email}}"
  },
  "account": {
    "title": "My Account",
    "profile": "Profile",
    "orders": "Orders",
    "addresses": "Addresses",
    "payment_methods": "Payment Methods",
    "preferences": "Preferences",
    "security": "Security",
    "personal_info": {
      "title": "Personal Information",
      "first_name": "First Name",
      "last_name": "Last Name",
      "email": "Email",
      "phone": "Phone Number",
      "date_of_birth": "Date of Birth"
    },
    "address": {
      "title": "Address",
      "street": "Street Address",
      "city": "City",
      "state": "State/Province",
      "postal_code": "Postal Code",
      "country": "Country",
      "add_new": "Add New Address",
      "set_default": "Set as Default"
    }
  },
  "auth": {
    "login": {
      "title": "Login",
      "email": "Email",
      "password": "Password",
      "remember_me": "Remember Me",
      "forgot_password": "Forgot Password?",
      "no_account": "Don't have an account?",
      "sign_up": "Sign Up"
    },
    "register": {
      "title": "Create Account",
      "confirm_password": "Confirm Password",
      "terms_agreement": "I agree to the Terms of Service and Privacy Policy",
      "already_have_account": "Already have an account?",
      "sign_in": "Sign In"
    },
    "forgot_password": {
      "title": "Forgot Password",
      "instructions": "Enter your email address and we'll send you a link to reset your password.",
      "send_link": "Send Reset Link",
      "back_to_login": "Back to Login"
    },
    "reset_password": {
      "title": "Reset Password",
      "new_password": "New Password",
      "confirm_new_password": "Confirm New Password",
      "reset": "Reset Password"
    }
  },
  "errors": {
    "page_not_found": "Page Not Found",
    "page_not_found_message": "The page you're looking for doesn't exist.",
    "server_error": "Server Error",
    "server_error_message": "Something went wrong. Please try again later.",
    "unauthorized": "Unauthorized",
    "unauthorized_message": "You need to log in to access this page.",
    "forbidden": "Access Denied",
    "forbidden_message": "You don't have permission to access this resource.",
    "network_error": "Network Error",
    "network_error_message": "Please check your internet connection."
  },
  "hvac": {
    "cooling_capacity": "Cooling Capacity",
    "heating_capacity": "Heating Capacity",
    "energy_efficiency": "Energy Efficiency",
    "noise_level": "Noise Level",
    "room_size": "Suitable Room Size",
    "refrigerant": "Refrigerant Type",
    "btu": "BTU",
    "seer": "SEER Rating",
    "inverter": "Inverter Technology",
    "wifi_enabled": "WiFi Enabled",
    "smart_control": "Smart Control",
    "installation": "Installation",
    "installation_included": "Installation Included",
    "installation_not_included": "Installation Not Included"
  }
}
```

---

## 3. Tasks

### Task I18N-001: Setup ngx-translate

**Priority:** High
**Estimated Effort:** 2 hours
**Dependencies:** None

**Description:**
Install and configure ngx-translate library for Angular 19+ with standalone components.

**Acceptance Criteria:**
- [ ] Install `@ngx-translate/core` and `@ngx-translate/http-loader`
- [ ] Configure TranslateModule in `app.config.ts`
- [ ] Set up HttpLoaderFactory for lazy loading translations
- [ ] Verify translations load correctly on app startup
- [ ] Default language set to English

**Implementation:**

```bash
# Install packages
npm install @ngx-translate/core @ngx-translate/http-loader
```

```typescript
// src/app/app.config.ts
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, HttpClient } from '@angular/common/http';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';

import { routes } from './app.routes';

export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      })
    )
  ]
};
```

---

### Task I18N-002: Create TranslationService Wrapper

**Priority:** High
**Estimated Effort:** 3 hours
**Dependencies:** I18N-001

**Description:**
Create a centralized translation service that wraps ngx-translate with additional functionality including signals for reactive language state.

**Acceptance Criteria:**
- [ ] Service provides reactive `currentLang` signal
- [ ] Language changes persist to localStorage
- [ ] Service restores saved language on initialization
- [ ] Provides available languages configuration
- [ ] Handles language change events

**Implementation:**

```typescript
// src/app/core/services/translation.service.ts
import { Injectable, signal, computed, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface Language {
  code: string;
  name: string;
  nativeName: string;
  flag: string;
}

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly STORAGE_KEY = 'climasite_language';

  readonly availableLanguages: Language[] = [
    { code: 'en', name: 'English', nativeName: 'English', flag: '🇬🇧' },
    { code: 'bg', name: 'Bulgarian', nativeName: 'Български', flag: '🇧🇬' },
    { code: 'de', name: 'German', nativeName: 'Deutsch', flag: '🇩🇪' }
  ];

  readonly currentLang = signal<string>('en');

  readonly currentLanguage = computed(() =>
    this.availableLanguages.find(l => l.code === this.currentLang())
    ?? this.availableLanguages[0]
  );

  constructor(private translate: TranslateService) {
    this.initializeLanguage();

    // Sync signal with localStorage
    effect(() => {
      const lang = this.currentLang();
      localStorage.setItem(this.STORAGE_KEY, lang);
      document.documentElement.lang = lang;
    });
  }

  private initializeLanguage(): void {
    this.translate.setDefaultLang('en');
    this.translate.addLangs(this.availableLanguages.map(l => l.code));

    const savedLang = localStorage.getItem(this.STORAGE_KEY);
    const browserLang = this.translate.getBrowserLang();

    let initialLang = 'en';

    if (savedLang && this.isValidLanguage(savedLang)) {
      initialLang = savedLang;
    } else if (browserLang && this.isValidLanguage(browserLang)) {
      initialLang = browserLang;
    }

    this.setLanguage(initialLang);
  }

  setLanguage(lang: string): void {
    if (!this.isValidLanguage(lang)) {
      console.warn(`Invalid language code: ${lang}. Falling back to English.`);
      lang = 'en';
    }

    this.translate.use(lang);
    this.currentLang.set(lang);
  }

  isValidLanguage(lang: string): boolean {
    return this.availableLanguages.some(l => l.code === lang);
  }

  instant(key: string, params?: object): string {
    return this.translate.instant(key, params);
  }

  get(key: string, params?: object) {
    return this.translate.get(key, params);
  }

  onLangChange() {
    return this.translate.onLangChange;
  }
}
```

---

### Task I18N-003: Language Switcher Component

**Priority:** High
**Estimated Effort:** 4 hours
**Dependencies:** I18N-002

**Description:**
Create a standalone language switcher component with dropdown menu displaying available languages with flags.

**Acceptance Criteria:**
- [ ] Standalone component with dropdown menu
- [ ] Display current language flag and code
- [ ] Show all available languages in dropdown
- [ ] Language change triggers runtime switch (no reload)
- [ ] Proper accessibility attributes (ARIA)
- [ ] Data-testid attributes for E2E testing
- [ ] Responsive design (works on mobile)

**Implementation:**

```typescript
// src/app/shared/components/language-switcher/language-switcher.component.ts
import { Component, signal, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslationService, Language } from '../../../core/services/translation.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-switcher.component.html',
  styleUrls: ['./language-switcher.component.scss']
})
export class LanguageSwitcherComponent {
  isOpen = signal(false);

  constructor(
    public translationService: TranslationService,
    private elementRef: ElementRef
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.isOpen.set(false);
  }

  toggleDropdown(): void {
    this.isOpen.update(v => !v);
  }

  selectLanguage(lang: Language): void {
    this.translationService.setLanguage(lang.code);
    this.isOpen.set(false);
  }

  isSelected(lang: Language): boolean {
    return lang.code === this.translationService.currentLang();
  }
}
```

```html
<!-- src/app/shared/components/language-switcher/language-switcher.component.html -->
<div class="language-switcher" data-testid="language-switcher">
  <button
    type="button"
    class="language-switcher__toggle"
    [attr.aria-expanded]="isOpen()"
    aria-haspopup="listbox"
    aria-label="Select language"
    data-testid="language-switcher-toggle"
    (click)="toggleDropdown()"
  >
    <span class="language-switcher__flag">
      {{ translationService.currentLanguage().flag }}
    </span>
    <span class="language-switcher__code">
      {{ translationService.currentLang() | uppercase }}
    </span>
    <svg
      class="language-switcher__arrow"
      [class.language-switcher__arrow--open]="isOpen()"
      viewBox="0 0 24 24"
      width="16"
      height="16"
    >
      <path d="M7 10l5 5 5-5z" fill="currentColor"/>
    </svg>
  </button>

  @if (isOpen()) {
    <ul
      class="language-switcher__dropdown"
      role="listbox"
      aria-label="Available languages"
      data-testid="language-dropdown"
    >
      @for (lang of translationService.availableLanguages; track lang.code) {
        <li
          role="option"
          class="language-switcher__option"
          [class.language-switcher__option--selected]="isSelected(lang)"
          [attr.aria-selected]="isSelected(lang)"
          [attr.data-testid]="'lang-' + lang.code"
          (click)="selectLanguage(lang)"
          (keydown.enter)="selectLanguage(lang)"
          (keydown.space)="selectLanguage(lang)"
          tabindex="0"
        >
          <span class="language-switcher__option-flag">{{ lang.flag }}</span>
          <span class="language-switcher__option-name">{{ lang.nativeName }}</span>
          @if (isSelected(lang)) {
            <svg class="language-switcher__check" viewBox="0 0 24 24" width="16" height="16">
              <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" fill="currentColor"/>
            </svg>
          }
        </li>
      }
    </ul>
  }
</div>
```

```scss
// src/app/shared/components/language-switcher/language-switcher.component.scss
.language-switcher {
  position: relative;
  display: inline-block;

  &__toggle {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--border-color, #e0e0e0);
    border-radius: 0.375rem;
    background: var(--bg-color, #ffffff);
    color: var(--text-color, #333333);
    cursor: pointer;
    transition: all 0.2s ease;

    &:hover {
      border-color: var(--primary-color, #0066cc);
    }

    &:focus {
      outline: 2px solid var(--primary-color, #0066cc);
      outline-offset: 2px;
    }
  }

  &__flag {
    font-size: 1.25rem;
    line-height: 1;
  }

  &__code {
    font-size: 0.875rem;
    font-weight: 500;
  }

  &__arrow {
    transition: transform 0.2s ease;

    &--open {
      transform: rotate(180deg);
    }
  }

  &__dropdown {
    position: absolute;
    top: calc(100% + 0.25rem);
    right: 0;
    min-width: 180px;
    margin: 0;
    padding: 0.25rem 0;
    list-style: none;
    background: var(--bg-color, #ffffff);
    border: 1px solid var(--border-color, #e0e0e0);
    border-radius: 0.375rem;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    z-index: 1000;
  }

  &__option {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.625rem 1rem;
    cursor: pointer;
    transition: background-color 0.15s ease;

    &:hover {
      background-color: var(--hover-bg, #f5f5f5);
    }

    &--selected {
      background-color: var(--selected-bg, #e3f2fd);
      color: var(--primary-color, #0066cc);
    }

    &:focus {
      outline: none;
      background-color: var(--hover-bg, #f5f5f5);
    }
  }

  &__option-flag {
    font-size: 1.25rem;
    line-height: 1;
  }

  &__option-name {
    flex: 1;
    font-size: 0.875rem;
  }

  &__check {
    color: var(--primary-color, #0066cc);
  }
}

// Mobile responsive
@media (max-width: 480px) {
  .language-switcher {
    &__code {
      display: none;
    }

    &__dropdown {
      right: -0.5rem;
    }
  }
}
```

---

### Task I18N-004: Create Translation Files

**Priority:** High
**Estimated Effort:** 8 hours
**Dependencies:** I18N-001

**Description:**
Create complete translation files for all supported languages (EN, BG, DE).

**Acceptance Criteria:**
- [ ] English (en.json) - Complete base translations
- [ ] Bulgarian (bg.json) - Complete translations
- [ ] German (de.json) - Complete translations
- [ ] All keys match across all language files
- [ ] No missing translations
- [ ] Proper handling of plurals and interpolation

**English (en.json):** See Section 2.2 for full structure.

**Bulgarian (bg.json):**

```json
{
  "common": {
    "buttons": {
      "add_to_cart": "Добави в кошницата",
      "remove_from_cart": "Премахни от кошницата",
      "checkout": "Поръчай",
      "continue_shopping": "Продължи пазаруването",
      "cancel": "Отказ",
      "save": "Запази",
      "delete": "Изтрий",
      "edit": "Редактирай",
      "submit": "Изпрати",
      "back": "Назад",
      "next": "Напред",
      "previous": "Предишен",
      "apply": "Приложи",
      "clear": "Изчисти",
      "search": "Търси",
      "filter": "Филтрирай",
      "sort": "Сортирай",
      "view_details": "Виж детайли",
      "close": "Затвори"
    },
    "labels": {
      "price": "Цена",
      "quantity": "Количество",
      "total": "Общо",
      "subtotal": "Междинна сума",
      "tax": "ДДС",
      "shipping": "Доставка",
      "discount": "Отстъпка",
      "in_stock": "В наличност",
      "out_of_stock": "Изчерпано",
      "limited_stock": "Ограничено количество",
      "free_shipping": "Безплатна доставка",
      "sale": "Разпродажба",
      "new": "Ново",
      "featured": "Препоръчано"
    },
    "messages": {
      "loading": "Зареждане...",
      "error": "Възникна грешка",
      "success": "Успешно!",
      "no_results": "Няма намерени резултати",
      "confirm_delete": "Сигурни ли сте, че искате да изтриете този елемент?",
      "item_added": "Артикулът е добавен в кошницата",
      "item_removed": "Артикулът е премахнат от кошницата"
    },
    "validation": {
      "required": "Това поле е задължително",
      "email_invalid": "Моля, въведете валиден имейл адрес",
      "min_length": "Минимум {{min}} символа",
      "max_length": "Максимум {{max}} символа",
      "password_mismatch": "Паролите не съвпадат",
      "invalid_phone": "Моля, въведете валиден телефонен номер"
    }
  },
  "header": {
    "search_placeholder": "Търси продукти...",
    "cart": "Кошница",
    "account": "Акаунт",
    "login": "Вход",
    "logout": "Изход",
    "register": "Регистрация",
    "my_orders": "Моите поръчки",
    "wishlist": "Любими",
    "language": "Език"
  },
  "navigation": {
    "home": "Начало",
    "products": "Продукти",
    "categories": "Категории",
    "air_conditioners": "Климатици",
    "heating_systems": "Отоплителни системи",
    "ventilation": "Вентилация",
    "accessories": "Аксесоари",
    "about": "За нас",
    "contact": "Контакти",
    "support": "Поддръжка",
    "faq": "Често задавани въпроси"
  },
  "footer": {
    "company_info": "Информация за фирмата",
    "customer_service": "Обслужване на клиенти",
    "follow_us": "Последвайте ни",
    "newsletter": "Бюлетин",
    "newsletter_placeholder": "Въведете вашия имейл",
    "subscribe": "Абонирай се",
    "privacy_policy": "Политика за поверителност",
    "terms_of_service": "Условия за ползване",
    "shipping_info": "Информация за доставка",
    "returns": "Връщане и възстановяване",
    "copyright": "© {{year}} ClimaSite. Всички права запазени."
  },
  "products": {
    "title": "Продукти",
    "all_products": "Всички продукти",
    "no_results": "Няма намерени продукти",
    "showing_results": "Показани {{count}} продукта",
    "sort_by": "Сортирай по",
    "sort_options": {
      "price_low": "Цена: ниска към висока",
      "price_high": "Цена: висока към ниска",
      "name_asc": "Име: А до Я",
      "name_desc": "Име: Я до А",
      "newest": "Най-нови",
      "best_selling": "Най-продавани",
      "rating": "Най-високо оценени"
    },
    "filters": {
      "title": "Филтри",
      "price_range": "Ценови диапазон",
      "brand": "Марка",
      "category": "Категория",
      "rating": "Оценка",
      "availability": "Наличност",
      "clear_all": "Изчисти всички филтри"
    },
    "details": {
      "description": "Описание",
      "specifications": "Спецификации",
      "reviews": "Отзиви",
      "related_products": "Подобни продукти",
      "energy_rating": "Енергиен клас",
      "warranty": "Гаранция",
      "sku": "Код",
      "brand": "Марка",
      "model": "Модел"
    }
  },
  "cart": {
    "title": "Количка",
    "empty": "Вашата количка е празна",
    "items": "{{count}} артикул(а)",
    "update_quantity": "Промени количество",
    "remove_item": "Премахни артикул",
    "clear_cart": "Изчисти количката",
    "continue_shopping": "Продължи пазаруването",
    "proceed_to_checkout": "Към поръчка",
    "order_summary": "Обобщение на поръчката",
    "promo_code": "Промо код",
    "apply_code": "Приложи код",
    "estimated_total": "Очаквана сума"
  },
  "checkout": {
    "title": "Поръчка",
    "steps": {
      "shipping": "Доставка",
      "payment": "Плащане",
      "review": "Преглед",
      "confirmation": "Потвърждение"
    },
    "shipping_address": "Адрес за доставка",
    "billing_address": "Адрес за фактуриране",
    "same_as_shipping": "Същият като адреса за доставка",
    "payment_method": "Начин на плащане",
    "card_number": "Номер на карта",
    "expiry_date": "Валидна до",
    "cvv": "CVV",
    "place_order": "Потвърди поръчка",
    "order_placed": "Поръчката е успешна!",
    "order_number": "Номер на поръчка: {{number}}",
    "confirmation_email": "Имейл за потвърждение е изпратен на {{email}}"
  },
  "account": {
    "title": "Моят акаунт",
    "profile": "Профил",
    "orders": "Поръчки",
    "addresses": "Адреси",
    "payment_methods": "Методи на плащане",
    "preferences": "Предпочитания",
    "security": "Сигурност",
    "personal_info": {
      "title": "Лична информация",
      "first_name": "Име",
      "last_name": "Фамилия",
      "email": "Имейл",
      "phone": "Телефон",
      "date_of_birth": "Дата на раждане"
    },
    "address": {
      "title": "Адрес",
      "street": "Улица",
      "city": "Град",
      "state": "Област",
      "postal_code": "Пощенски код",
      "country": "Държава",
      "add_new": "Добави нов адрес",
      "set_default": "Задай като основен"
    }
  },
  "auth": {
    "login": {
      "title": "Вход",
      "email": "Имейл",
      "password": "Парола",
      "remember_me": "Запомни ме",
      "forgot_password": "Забравена парола?",
      "no_account": "Нямате акаунт?",
      "sign_up": "Регистрация"
    },
    "register": {
      "title": "Създаване на акаунт",
      "confirm_password": "Потвърди парола",
      "terms_agreement": "Съгласен съм с Условията за ползване и Политиката за поверителност",
      "already_have_account": "Вече имате акаунт?",
      "sign_in": "Влезте"
    },
    "forgot_password": {
      "title": "Забравена парола",
      "instructions": "Въведете вашия имейл адрес и ще ви изпратим линк за възстановяване.",
      "send_link": "Изпрати линк",
      "back_to_login": "Обратно към вход"
    },
    "reset_password": {
      "title": "Нова парола",
      "new_password": "Нова парола",
      "confirm_new_password": "Потвърди новата парола",
      "reset": "Смени паролата"
    }
  },
  "errors": {
    "page_not_found": "Страницата не е намерена",
    "page_not_found_message": "Страницата, която търсите, не съществува.",
    "server_error": "Сървърна грешка",
    "server_error_message": "Нещо се обърка. Моля, опитайте отново по-късно.",
    "unauthorized": "Неоторизиран",
    "unauthorized_message": "Трябва да влезете, за да достъпите тази страница.",
    "forbidden": "Достъпът е отказан",
    "forbidden_message": "Нямате права за достъп до този ресурс.",
    "network_error": "Мрежова грешка",
    "network_error_message": "Моля, проверете интернет връзката си."
  },
  "hvac": {
    "cooling_capacity": "Охлаждащ капацитет",
    "heating_capacity": "Отоплителен капацитет",
    "energy_efficiency": "Енергийна ефективност",
    "noise_level": "Ниво на шум",
    "room_size": "Подходящ размер на стая",
    "refrigerant": "Вид хладилен агент",
    "btu": "BTU",
    "seer": "SEER рейтинг",
    "inverter": "Инверторна технология",
    "wifi_enabled": "WiFi функция",
    "smart_control": "Смарт управление",
    "installation": "Монтаж",
    "installation_included": "Монтаж включен",
    "installation_not_included": "Монтаж не е включен"
  }
}
```

**German (de.json):**

```json
{
  "common": {
    "buttons": {
      "add_to_cart": "In den Warenkorb",
      "remove_from_cart": "Aus dem Warenkorb entfernen",
      "checkout": "Zur Kasse",
      "continue_shopping": "Weiter einkaufen",
      "cancel": "Abbrechen",
      "save": "Speichern",
      "delete": "Löschen",
      "edit": "Bearbeiten",
      "submit": "Absenden",
      "back": "Zurück",
      "next": "Weiter",
      "previous": "Zurück",
      "apply": "Anwenden",
      "clear": "Löschen",
      "search": "Suchen",
      "filter": "Filtern",
      "sort": "Sortieren",
      "view_details": "Details anzeigen",
      "close": "Schließen"
    },
    "labels": {
      "price": "Preis",
      "quantity": "Menge",
      "total": "Gesamt",
      "subtotal": "Zwischensumme",
      "tax": "MwSt.",
      "shipping": "Versand",
      "discount": "Rabatt",
      "in_stock": "Auf Lager",
      "out_of_stock": "Nicht vorrätig",
      "limited_stock": "Begrenzte Verfügbarkeit",
      "free_shipping": "Kostenloser Versand",
      "sale": "Angebot",
      "new": "Neu",
      "featured": "Empfohlen"
    },
    "messages": {
      "loading": "Wird geladen...",
      "error": "Ein Fehler ist aufgetreten",
      "success": "Erfolgreich!",
      "no_results": "Keine Ergebnisse gefunden",
      "confirm_delete": "Sind Sie sicher, dass Sie diesen Artikel löschen möchten?",
      "item_added": "Artikel zum Warenkorb hinzugefügt",
      "item_removed": "Artikel aus dem Warenkorb entfernt"
    },
    "validation": {
      "required": "Dieses Feld ist erforderlich",
      "email_invalid": "Bitte geben Sie eine gültige E-Mail-Adresse ein",
      "min_length": "Mindestens {{min}} Zeichen erforderlich",
      "max_length": "Maximal {{max}} Zeichen erlaubt",
      "password_mismatch": "Passwörter stimmen nicht überein",
      "invalid_phone": "Bitte geben Sie eine gültige Telefonnummer ein"
    }
  },
  "header": {
    "search_placeholder": "Produkte suchen...",
    "cart": "Warenkorb",
    "account": "Konto",
    "login": "Anmelden",
    "logout": "Abmelden",
    "register": "Registrieren",
    "my_orders": "Meine Bestellungen",
    "wishlist": "Wunschliste",
    "language": "Sprache"
  },
  "navigation": {
    "home": "Startseite",
    "products": "Produkte",
    "categories": "Kategorien",
    "air_conditioners": "Klimaanlagen",
    "heating_systems": "Heizsysteme",
    "ventilation": "Lüftung",
    "accessories": "Zubehör",
    "about": "Über uns",
    "contact": "Kontakt",
    "support": "Support",
    "faq": "FAQ"
  },
  "footer": {
    "company_info": "Unternehmensinformationen",
    "customer_service": "Kundenservice",
    "follow_us": "Folgen Sie uns",
    "newsletter": "Newsletter",
    "newsletter_placeholder": "E-Mail-Adresse eingeben",
    "subscribe": "Abonnieren",
    "privacy_policy": "Datenschutz",
    "terms_of_service": "AGB",
    "shipping_info": "Versandinformationen",
    "returns": "Rückgabe & Erstattung",
    "copyright": "© {{year}} ClimaSite. Alle Rechte vorbehalten."
  },
  "products": {
    "title": "Produkte",
    "all_products": "Alle Produkte",
    "no_results": "Keine Produkte gefunden",
    "showing_results": "{{count}} Produkte angezeigt",
    "sort_by": "Sortieren nach",
    "sort_options": {
      "price_low": "Preis: Niedrig bis Hoch",
      "price_high": "Preis: Hoch bis Niedrig",
      "name_asc": "Name: A bis Z",
      "name_desc": "Name: Z bis A",
      "newest": "Neueste zuerst",
      "best_selling": "Meistverkauft",
      "rating": "Bestbewertet"
    },
    "filters": {
      "title": "Filter",
      "price_range": "Preisbereich",
      "brand": "Marke",
      "category": "Kategorie",
      "rating": "Bewertung",
      "availability": "Verfügbarkeit",
      "clear_all": "Alle Filter löschen"
    },
    "details": {
      "description": "Beschreibung",
      "specifications": "Spezifikationen",
      "reviews": "Bewertungen",
      "related_products": "Ähnliche Produkte",
      "energy_rating": "Energieeffizienzklasse",
      "warranty": "Garantie",
      "sku": "Artikelnummer",
      "brand": "Marke",
      "model": "Modell"
    }
  },
  "cart": {
    "title": "Warenkorb",
    "empty": "Ihr Warenkorb ist leer",
    "items": "{{count}} Artikel",
    "update_quantity": "Menge aktualisieren",
    "remove_item": "Artikel entfernen",
    "clear_cart": "Warenkorb leeren",
    "continue_shopping": "Weiter einkaufen",
    "proceed_to_checkout": "Zur Kasse gehen",
    "order_summary": "Bestellübersicht",
    "promo_code": "Gutscheincode",
    "apply_code": "Code anwenden",
    "estimated_total": "Geschätzte Summe"
  },
  "checkout": {
    "title": "Kasse",
    "steps": {
      "shipping": "Versand",
      "payment": "Zahlung",
      "review": "Überprüfung",
      "confirmation": "Bestätigung"
    },
    "shipping_address": "Lieferadresse",
    "billing_address": "Rechnungsadresse",
    "same_as_shipping": "Gleich wie Lieferadresse",
    "payment_method": "Zahlungsmethode",
    "card_number": "Kartennummer",
    "expiry_date": "Ablaufdatum",
    "cvv": "CVV",
    "place_order": "Bestellung aufgeben",
    "order_placed": "Bestellung erfolgreich aufgegeben!",
    "order_number": "Bestellnummer: {{number}}",
    "confirmation_email": "Eine Bestätigungs-E-Mail wurde an {{email}} gesendet"
  },
  "account": {
    "title": "Mein Konto",
    "profile": "Profil",
    "orders": "Bestellungen",
    "addresses": "Adressen",
    "payment_methods": "Zahlungsmethoden",
    "preferences": "Einstellungen",
    "security": "Sicherheit",
    "personal_info": {
      "title": "Persönliche Informationen",
      "first_name": "Vorname",
      "last_name": "Nachname",
      "email": "E-Mail",
      "phone": "Telefonnummer",
      "date_of_birth": "Geburtsdatum"
    },
    "address": {
      "title": "Adresse",
      "street": "Straße",
      "city": "Stadt",
      "state": "Bundesland",
      "postal_code": "Postleitzahl",
      "country": "Land",
      "add_new": "Neue Adresse hinzufügen",
      "set_default": "Als Standard festlegen"
    }
  },
  "auth": {
    "login": {
      "title": "Anmelden",
      "email": "E-Mail",
      "password": "Passwort",
      "remember_me": "Angemeldet bleiben",
      "forgot_password": "Passwort vergessen?",
      "no_account": "Noch kein Konto?",
      "sign_up": "Registrieren"
    },
    "register": {
      "title": "Konto erstellen",
      "confirm_password": "Passwort bestätigen",
      "terms_agreement": "Ich stimme den AGB und der Datenschutzerklärung zu",
      "already_have_account": "Bereits ein Konto?",
      "sign_in": "Anmelden"
    },
    "forgot_password": {
      "title": "Passwort vergessen",
      "instructions": "Geben Sie Ihre E-Mail-Adresse ein und wir senden Ihnen einen Link zum Zurücksetzen.",
      "send_link": "Link senden",
      "back_to_login": "Zurück zur Anmeldung"
    },
    "reset_password": {
      "title": "Passwort zurücksetzen",
      "new_password": "Neues Passwort",
      "confirm_new_password": "Neues Passwort bestätigen",
      "reset": "Passwort zurücksetzen"
    }
  },
  "errors": {
    "page_not_found": "Seite nicht gefunden",
    "page_not_found_message": "Die gesuchte Seite existiert nicht.",
    "server_error": "Serverfehler",
    "server_error_message": "Etwas ist schief gelaufen. Bitte versuchen Sie es später erneut.",
    "unauthorized": "Nicht autorisiert",
    "unauthorized_message": "Sie müssen sich anmelden, um auf diese Seite zuzugreifen.",
    "forbidden": "Zugriff verweigert",
    "forbidden_message": "Sie haben keine Berechtigung für diese Ressource.",
    "network_error": "Netzwerkfehler",
    "network_error_message": "Bitte überprüfen Sie Ihre Internetverbindung."
  },
  "hvac": {
    "cooling_capacity": "Kühlleistung",
    "heating_capacity": "Heizleistung",
    "energy_efficiency": "Energieeffizienz",
    "noise_level": "Geräuschpegel",
    "room_size": "Geeignete Raumgröße",
    "refrigerant": "Kältemittel",
    "btu": "BTU",
    "seer": "SEER-Wert",
    "inverter": "Inverter-Technologie",
    "wifi_enabled": "WLAN-fähig",
    "smart_control": "Smart-Steuerung",
    "installation": "Installation",
    "installation_included": "Installation inklusive",
    "installation_not_included": "Installation nicht inklusive"
  }
}
```

---

### Task I18N-005: Translate Pipe with Parameters

**Priority:** Medium
**Estimated Effort:** 2 hours
**Dependencies:** I18N-001

**Description:**
Create a custom pipe for translations with dynamic parameters for complex interpolations.

**Acceptance Criteria:**
- [ ] Pipe handles string interpolation
- [ ] Works with multiple parameters
- [ ] Supports nested translation keys
- [ ] Pure pipe for performance

**Implementation:**

```typescript
// src/app/shared/pipes/translate-params.pipe.ts
import { Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({
  name: 'translateParams',
  standalone: true,
  pure: false // Required for language change detection
})
export class TranslateParamsPipe implements PipeTransform {
  constructor(private translate: TranslateService) {}

  transform(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
```

**Usage Example:**

```html
<!-- Simple usage -->
<p>{{ 'common.messages.loading' | translate }}</p>

<!-- With parameters -->
<p>{{ 'footer.copyright' | translateParams: { year: currentYear } }}</p>

<!-- With count -->
<p>{{ 'cart.items' | translateParams: { count: cartItemCount } }}</p>
```

---

### Task I18N-006: Translate Attribute Directive

**Priority:** Medium
**Estimated Effort:** 2 hours
**Dependencies:** I18N-001

**Description:**
Create a directive for translating HTML attributes like placeholder, title, aria-label.

**Acceptance Criteria:**
- [ ] Supports placeholder, title, aria-label, alt
- [ ] Updates on language change
- [ ] Works with interpolation parameters

**Implementation:**

```typescript
// src/app/shared/directives/translate-attr.directive.ts
import { Directive, ElementRef, Input, OnInit, OnDestroy } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';

@Directive({
  selector: '[translateAttr]',
  standalone: true
})
export class TranslateAttrDirective implements OnInit, OnDestroy {
  @Input('translateAttr') config!: {
    placeholder?: string;
    title?: string;
    ariaLabel?: string;
    alt?: string;
    params?: Record<string, unknown>;
  };

  private destroy$ = new Subject<void>();

  constructor(
    private el: ElementRef,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.updateAttributes();

    this.translate.onLangChange
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.updateAttributes());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateAttributes(): void {
    const element = this.el.nativeElement;
    const params = this.config.params || {};

    if (this.config.placeholder) {
      element.placeholder = this.translate.instant(this.config.placeholder, params);
    }
    if (this.config.title) {
      element.title = this.translate.instant(this.config.title, params);
    }
    if (this.config.ariaLabel) {
      element.setAttribute('aria-label', this.translate.instant(this.config.ariaLabel, params));
    }
    if (this.config.alt) {
      element.alt = this.translate.instant(this.config.alt, params);
    }
  }
}
```

**Usage Example:**

```html
<input
  type="text"
  [translateAttr]="{ placeholder: 'header.search_placeholder' }"
/>

<img
  src="product.jpg"
  [translateAttr]="{ alt: 'products.details.image_alt', params: { name: product.name } }"
/>

<button
  [translateAttr]="{ ariaLabel: 'common.buttons.close', title: 'common.buttons.close' }"
>
  <svg>...</svg>
</button>
```

---

### Task I18N-007: Integrate Translations in Components

**Priority:** High
**Estimated Effort:** 6 hours
**Dependencies:** I18N-001, I18N-004

**Description:**
Update all existing components to use translation keys instead of hardcoded text.

**Acceptance Criteria:**
- [ ] All hardcoded strings replaced with translation keys
- [ ] TranslateModule imported in standalone components
- [ ] Proper pipe usage in templates
- [ ] Service injection where needed

**Implementation Example:**

```typescript
// src/app/features/products/product-card/product-card.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <article class="product-card">
      <img [src]="product.imageUrl" [alt]="product.name" />
      <h3>{{ product.name }}</h3>
      <p class="price">{{ 'common.labels.price' | translate }}: {{ product.price | currency }}</p>

      @if (product.inStock) {
        <span class="badge badge--success">{{ 'common.labels.in_stock' | translate }}</span>
      } @else {
        <span class="badge badge--danger">{{ 'common.labels.out_of_stock' | translate }}</span>
      }

      <button
        class="btn btn--primary"
        [disabled]="!product.inStock"
      >
        {{ 'common.buttons.add_to_cart' | translate }}
      </button>
    </article>
  `
})
export class ProductCardComponent {
  @Input() product!: Product;
}
```

---

### Task I18N-008: Date and Number Formatting

**Priority:** Medium
**Estimated Effort:** 3 hours
**Dependencies:** I18N-002

**Description:**
Implement locale-aware date and number formatting that updates with language changes.

**Acceptance Criteria:**
- [ ] Dates formatted according to current locale
- [ ] Numbers and currencies use locale formatting
- [ ] Updates automatically on language change
- [ ] Custom pipes for common formats

**Implementation:**

```typescript
// src/app/shared/pipes/locale-date.pipe.ts
import { Pipe, PipeTransform } from '@angular/core';
import { TranslationService } from '../../core/services/translation.service';

@Pipe({
  name: 'localeDate',
  standalone: true,
  pure: false
})
export class LocaleDatePipe implements PipeTransform {
  private localeMap: Record<string, string> = {
    'en': 'en-GB',
    'bg': 'bg-BG',
    'de': 'de-DE'
  };

  constructor(private translationService: TranslationService) {}

  transform(
    value: Date | string | number,
    format: 'short' | 'medium' | 'long' | 'full' = 'medium'
  ): string {
    if (!value) return '';

    const date = new Date(value);
    const locale = this.localeMap[this.translationService.currentLang()] || 'en-GB';

    const options: Intl.DateTimeFormatOptions = this.getFormatOptions(format);

    return new Intl.DateTimeFormat(locale, options).format(date);
  }

  private getFormatOptions(format: string): Intl.DateTimeFormatOptions {
    switch (format) {
      case 'short':
        return { day: '2-digit', month: '2-digit', year: '2-digit' };
      case 'medium':
        return { day: 'numeric', month: 'short', year: 'numeric' };
      case 'long':
        return { day: 'numeric', month: 'long', year: 'numeric' };
      case 'full':
        return { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' };
      default:
        return { day: 'numeric', month: 'short', year: 'numeric' };
    }
  }
}

// src/app/shared/pipes/locale-currency.pipe.ts
import { Pipe, PipeTransform } from '@angular/core';
import { TranslationService } from '../../core/services/translation.service';

@Pipe({
  name: 'localeCurrency',
  standalone: true,
  pure: false
})
export class LocaleCurrencyPipe implements PipeTransform {
  private localeMap: Record<string, string> = {
    'en': 'en-GB',
    'bg': 'bg-BG',
    'de': 'de-DE'
  };

  constructor(private translationService: TranslationService) {}

  transform(
    value: number,
    currencyCode: string = 'EUR',
    display: 'symbol' | 'code' | 'name' = 'symbol'
  ): string {
    if (value === null || value === undefined) return '';

    const locale = this.localeMap[this.translationService.currentLang()] || 'en-GB';

    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currencyCode,
      currencyDisplay: display
    }).format(value);
  }
}
```

---

### Task I18N-009: Backend API Localization

**Priority:** Medium
**Estimated Effort:** 4 hours
**Dependencies:** None

**Description:**
Implement backend support for localized API responses and error messages.

**Acceptance Criteria:**
- [ ] Accept-Language header processing
- [ ] Localized error messages in ProblemDetails
- [ ] Resource files for backend translations
- [ ] Middleware for culture setting

**Implementation:**

```csharp
// src/ClimaSite.Api/Middleware/LocalizationMiddleware.cs
using System.Globalization;

namespace ClimaSite.Api.Middleware;

public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _supportedCultures = { "en", "bg", "de" };

    public LocalizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        var culture = GetPreferredCulture(acceptLanguage);

        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        context.Items["Culture"] = culture;

        await _next(context);
    }

    private string GetPreferredCulture(string acceptLanguage)
    {
        if (string.IsNullOrEmpty(acceptLanguage))
            return "en";

        var languages = acceptLanguage
            .Split(',')
            .Select(l => l.Split(';')[0].Trim().ToLowerInvariant())
            .Select(l => l.Length > 2 ? l[..2] : l);

        foreach (var lang in languages)
        {
            if (_supportedCultures.Contains(lang))
                return lang;
        }

        return "en";
    }
}

// Program.cs registration
app.UseMiddleware<LocalizationMiddleware>();
```

```csharp
// src/ClimaSite.Api/Resources/ErrorMessages.cs
namespace ClimaSite.Api.Resources;

public static class ErrorMessages
{
    private static readonly Dictionary<string, Dictionary<string, string>> Messages = new()
    {
        ["product_not_found"] = new()
        {
            ["en"] = "Product not found",
            ["bg"] = "Продуктът не е намерен",
            ["de"] = "Produkt nicht gefunden"
        },
        ["invalid_quantity"] = new()
        {
            ["en"] = "Invalid quantity specified",
            ["bg"] = "Невалидно количество",
            ["de"] = "Ungültige Menge angegeben"
        },
        ["out_of_stock"] = new()
        {
            ["en"] = "Product is out of stock",
            ["bg"] = "Продуктът е изчерпан",
            ["de"] = "Produkt ist nicht vorrätig"
        },
        ["order_failed"] = new()
        {
            ["en"] = "Failed to process order",
            ["bg"] = "Грешка при обработка на поръчката",
            ["de"] = "Bestellung konnte nicht verarbeitet werden"
        }
    };

    public static string Get(string key, string culture = "en")
    {
        if (Messages.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(culture, out var message))
                return message;

            return translations.GetValueOrDefault("en", key);
        }

        return key;
    }
}
```

---

### Task I18N-010: Product Content Translation

**Priority:** Medium
**Estimated Effort:** 4 hours
**Dependencies:** I18N-009

**Description:**
Implement database support for multilingual product content (names, descriptions).

**Acceptance Criteria:**
- [ ] Translation table schema
- [ ] EF Core entity configuration
- [ ] Repository methods for fetching translated content
- [ ] Fallback to default language

**Implementation:**

```csharp
// src/ClimaSite.Core/Entities/ProductTranslation.cs
namespace ClimaSite.Core.Entities;

public class ProductTranslation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }

    public virtual Product Product { get; set; } = null!;
}

// src/ClimaSite.Core/Entities/Product.cs (updated)
public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    // ... other properties

    public virtual ICollection<ProductTranslation> Translations { get; set; }
        = new List<ProductTranslation>();

    public string GetName(string languageCode)
    {
        var translation = Translations.FirstOrDefault(t => t.LanguageCode == languageCode)
            ?? Translations.FirstOrDefault(t => t.LanguageCode == "en");
        return translation?.Name ?? Sku;
    }

    public string GetDescription(string languageCode)
    {
        var translation = Translations.FirstOrDefault(t => t.LanguageCode == languageCode)
            ?? Translations.FirstOrDefault(t => t.LanguageCode == "en");
        return translation?.Description ?? string.Empty;
    }
}
```

```csharp
// src/ClimaSite.Infrastructure/Data/Configurations/ProductTranslationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClimaSite.Core.Entities;

namespace ClimaSite.Infrastructure.Data.Configurations;

public class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.ToTable("product_translations");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.LanguageCode)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(pt => pt.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pt => pt.Description)
            .HasMaxLength(4000);

        builder.Property(pt => pt.ShortDescription)
            .HasMaxLength(500);

        builder.HasIndex(pt => new { pt.ProductId, pt.LanguageCode })
            .IsUnique();

        builder.HasOne(pt => pt.Product)
            .WithMany(p => p.Translations)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### Task I18N-011: SEO and Meta Tags

**Priority:** Low
**Estimated Effort:** 2 hours
**Dependencies:** I18N-002

**Description:**
Implement localized meta tags and SEO attributes for better search engine indexing.

**Acceptance Criteria:**
- [ ] Dynamic document language attribute
- [ ] Localized page titles
- [ ] Hreflang tags for language alternatives
- [ ] Localized meta descriptions

**Implementation:**

```typescript
// src/app/core/services/seo.service.ts
import { Injectable, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';
import { TranslationService } from './translation.service';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class SeoService {
  constructor(
    @Inject(DOCUMENT) private document: Document,
    private titleService: Title,
    private meta: Meta,
    private translationService: TranslationService,
    private translate: TranslateService
  ) {
    // Update HTML lang attribute on language change
    this.translate.onLangChange.subscribe(event => {
      this.document.documentElement.lang = event.lang;
    });
  }

  updatePageMeta(titleKey: string, descriptionKey?: string): void {
    // Set title
    this.translate.get(titleKey).subscribe(title => {
      this.titleService.setTitle(`${title} | ClimaSite`);
    });

    // Set description
    if (descriptionKey) {
      this.translate.get(descriptionKey).subscribe(description => {
        this.meta.updateTag({ name: 'description', content: description });
      });
    }

    // Set hreflang tags
    this.setHreflangTags();
  }

  private setHreflangTags(): void {
    // Remove existing hreflang tags
    const existingTags = this.document.querySelectorAll('link[hreflang]');
    existingTags.forEach(tag => tag.remove());

    const baseUrl = window.location.origin;
    const path = window.location.pathname;

    this.translationService.availableLanguages.forEach(lang => {
      const link = this.document.createElement('link');
      link.rel = 'alternate';
      link.hreflang = lang.code;
      link.href = `${baseUrl}/${lang.code}${path}`;
      this.document.head.appendChild(link);
    });

    // Add x-default
    const defaultLink = this.document.createElement('link');
    defaultLink.rel = 'alternate';
    defaultLink.hreflang = 'x-default';
    defaultLink.href = `${baseUrl}/en${path}`;
    this.document.head.appendChild(defaultLink);
  }
}
```

---

### Task I18N-012: Missing Translation Handler

**Priority:** Low
**Estimated Effort:** 2 hours
**Dependencies:** I18N-001

**Description:**
Implement a handler for missing translations to aid development and debugging.

**Acceptance Criteria:**
- [ ] Log missing translations in development
- [ ] Fallback to key or default language
- [ ] Optional reporting endpoint
- [ ] Visual indicator in development mode

**Implementation:**

```typescript
// src/app/core/handlers/missing-translation.handler.ts
import { MissingTranslationHandler, MissingTranslationHandlerParams } from '@ngx-translate/core';
import { Injectable, isDevMode } from '@angular/core';

@Injectable()
export class CustomMissingTranslationHandler implements MissingTranslationHandler {
  private missingKeys = new Set<string>();

  handle(params: MissingTranslationHandlerParams): string {
    const key = params.key;

    if (isDevMode() && !this.missingKeys.has(key)) {
      this.missingKeys.add(key);
      console.warn(`Missing translation for key: "${key}" in language: "${params.translateService.currentLang}"`);
    }

    // Return the key itself as fallback, wrapped in brackets for visibility
    return isDevMode() ? `[${key}]` : key;
  }

  getMissingKeys(): string[] {
    return Array.from(this.missingKeys);
  }

  clearMissingKeys(): void {
    this.missingKeys.clear();
  }
}

// Update app.config.ts
import { MissingTranslationHandler } from '@ngx-translate/core';
import { CustomMissingTranslationHandler } from './core/handlers/missing-translation.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    // ... existing providers
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        missingTranslationHandler: {
          provide: MissingTranslationHandler,
          useClass: CustomMissingTranslationHandler
        },
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      })
    )
  ]
};
```

---

### Task I18N-013: Translation Key Validator

**Priority:** Low
**Estimated Effort:** 3 hours
**Dependencies:** I18N-004

**Description:**
Create a build-time script to validate translation files consistency.

**Acceptance Criteria:**
- [ ] Verify all keys exist in all language files
- [ ] Check for unused translation keys
- [ ] Validate JSON structure
- [ ] Run as npm script and CI step

**Implementation:**

```typescript
// scripts/validate-translations.ts
import * as fs from 'fs';
import * as path from 'path';

interface TranslationFile {
  [key: string]: string | TranslationFile;
}

const I18N_PATH = path.join(__dirname, '../src/ClimaSite.Web/src/assets/i18n');
const SUPPORTED_LANGUAGES = ['en', 'bg', 'de'];
const BASE_LANGUAGE = 'en';

function loadTranslationFile(lang: string): TranslationFile {
  const filePath = path.join(I18N_PATH, `${lang}.json`);
  const content = fs.readFileSync(filePath, 'utf-8');
  return JSON.parse(content);
}

function extractKeys(obj: TranslationFile, prefix = ''): string[] {
  const keys: string[] = [];

  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;

    if (typeof value === 'object' && value !== null) {
      keys.push(...extractKeys(value as TranslationFile, fullKey));
    } else {
      keys.push(fullKey);
    }
  }

  return keys;
}

function validateTranslations(): void {
  console.log('Validating translation files...\n');

  const translations: Record<string, TranslationFile> = {};
  const allKeys: Record<string, Set<string>> = {};

  // Load all translation files
  for (const lang of SUPPORTED_LANGUAGES) {
    try {
      translations[lang] = loadTranslationFile(lang);
      allKeys[lang] = new Set(extractKeys(translations[lang]));
      console.log(`✓ Loaded ${lang}.json (${allKeys[lang].size} keys)`);
    } catch (error) {
      console.error(`✗ Failed to load ${lang}.json:`, error);
      process.exit(1);
    }
  }

  console.log('');

  // Get base language keys
  const baseKeys = allKeys[BASE_LANGUAGE];
  let hasErrors = false;

  // Check for missing keys in each language
  for (const lang of SUPPORTED_LANGUAGES) {
    if (lang === BASE_LANGUAGE) continue;

    const langKeys = allKeys[lang];
    const missingKeys = [...baseKeys].filter(k => !langKeys.has(k));
    const extraKeys = [...langKeys].filter(k => !baseKeys.has(k));

    if (missingKeys.length > 0) {
      console.error(`✗ ${lang}.json is missing ${missingKeys.length} keys:`);
      missingKeys.forEach(k => console.error(`    - ${k}`));
      hasErrors = true;
    }

    if (extraKeys.length > 0) {
      console.warn(`⚠ ${lang}.json has ${extraKeys.length} extra keys:`);
      extraKeys.forEach(k => console.warn(`    - ${k}`));
    }

    if (missingKeys.length === 0 && extraKeys.length === 0) {
      console.log(`✓ ${lang}.json is in sync with ${BASE_LANGUAGE}.json`);
    }
  }

  console.log('');

  if (hasErrors) {
    console.error('Translation validation failed!');
    process.exit(1);
  } else {
    console.log('Translation validation passed!');
  }
}

validateTranslations();
```

```json
// Add to package.json scripts
{
  "scripts": {
    "validate:i18n": "ts-node scripts/validate-translations.ts"
  }
}
```

---

### Task I18N-014: Lazy Loading Translations

**Priority:** Low
**Estimated Effort:** 2 hours
**Dependencies:** I18N-001

**Description:**
Implement lazy loading for translation files to improve initial load performance.

**Acceptance Criteria:**
- [ ] Only load active language on startup
- [ ] Preload translations for smoother switching
- [ ] Cache loaded translations
- [ ] Loading indicator during language switch

**Implementation:**

```typescript
// src/app/core/services/translation-loader.service.ts
import { Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TranslationLoaderService {
  private loadedLanguages = new Set<string>();
  readonly isLoading = signal(false);

  constructor(private translate: TranslateService) {}

  async preloadLanguage(lang: string): Promise<void> {
    if (this.loadedLanguages.has(lang)) {
      return;
    }

    this.isLoading.set(true);

    try {
      await firstValueFrom(this.translate.getTranslation(lang));
      this.loadedLanguages.add(lang);
    } finally {
      this.isLoading.set(false);
    }
  }

  async preloadAllLanguages(): Promise<void> {
    const languages = this.translate.getLangs();
    await Promise.all(languages.map(lang => this.preloadLanguage(lang)));
  }

  isLanguageLoaded(lang: string): boolean {
    return this.loadedLanguages.has(lang);
  }
}
```

---

### Task I18N-015: Form Validation Messages

**Priority:** Medium
**Estimated Effort:** 3 hours
**Dependencies:** I18N-001, I18N-004

**Description:**
Implement localized form validation error messages.

**Acceptance Criteria:**
- [ ] All validation errors use translation keys
- [ ] Dynamic parameter interpolation (min, max values)
- [ ] Custom validation message directive
- [ ] Consistent error display component

**Implementation:**

```typescript
// src/app/shared/components/form-error/form-error.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ValidationErrors } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-form-error',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    @if (control && control.invalid && (control.dirty || control.touched)) {
      <div class="form-error" role="alert">
        @for (error of errorMessages; track error.key) {
          <p class="form-error__message">
            {{ error.key | translate: error.params }}
          </p>
        }
      </div>
    }
  `,
  styles: [`
    .form-error {
      margin-top: 0.25rem;

      &__message {
        font-size: 0.875rem;
        color: var(--error-color, #dc3545);
        margin: 0;
      }
    }
  `]
})
export class FormErrorComponent {
  @Input() control: AbstractControl | null = null;

  private errorKeyMap: Record<string, string> = {
    required: 'common.validation.required',
    email: 'common.validation.email_invalid',
    minlength: 'common.validation.min_length',
    maxlength: 'common.validation.max_length',
    pattern: 'common.validation.invalid_format',
    passwordMismatch: 'common.validation.password_mismatch',
    invalidPhone: 'common.validation.invalid_phone'
  };

  get errorMessages(): Array<{ key: string; params: Record<string, unknown> }> {
    if (!this.control || !this.control.errors) {
      return [];
    }

    return Object.entries(this.control.errors).map(([errorKey, errorValue]) => ({
      key: this.errorKeyMap[errorKey] || `common.validation.${errorKey}`,
      params: this.getErrorParams(errorKey, errorValue)
    }));
  }

  private getErrorParams(errorKey: string, errorValue: unknown): Record<string, unknown> {
    if (typeof errorValue !== 'object' || errorValue === null) {
      return {};
    }

    const value = errorValue as ValidationErrors;

    switch (errorKey) {
      case 'minlength':
        return { min: value['requiredLength'] };
      case 'maxlength':
        return { max: value['requiredLength'] };
      case 'min':
        return { min: value['min'] };
      case 'max':
        return { max: value['max'] };
      default:
        return value;
    }
  }
}
```

**Usage Example:**

```html
<form [formGroup]="loginForm">
  <div class="form-group">
    <label for="email">{{ 'auth.login.email' | translate }}</label>
    <input
      type="email"
      id="email"
      formControlName="email"
      [class.is-invalid]="loginForm.get('email')?.invalid && loginForm.get('email')?.touched"
    />
    <app-form-error [control]="loginForm.get('email')" />
  </div>

  <div class="form-group">
    <label for="password">{{ 'auth.login.password' | translate }}</label>
    <input
      type="password"
      id="password"
      formControlName="password"
      [class.is-invalid]="loginForm.get('password')?.invalid && loginForm.get('password')?.touched"
    />
    <app-form-error [control]="loginForm.get('password')" />
  </div>
</form>
```

---

## 4. E2E Tests (Playwright - NO MOCKING)

### 4.1 Test Configuration

```typescript
// e2e/playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
  },
});
```

### 4.2 Test I18N-E2E-001: Language Switching

```typescript
// e2e/tests/i18n/language-switching.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Language Switching', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage to ensure clean state
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  test('should display default language as English', async ({ page }) => {
    await page.goto('/');

    // Verify English content is displayed
    await expect(page.getByText('Add to Cart')).toBeVisible();
    await expect(page.getByText('Products')).toBeVisible();
    await expect(page.getByPlaceholder('Search products...')).toBeVisible();

    // Verify language switcher shows EN
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('EN');
  });

  test('user can switch language from English to Bulgarian', async ({ page }) => {
    await page.goto('/');

    // Verify default English
    await expect(page.getByText('Add to Cart')).toBeVisible();

    // Open language switcher
    await page.getByTestId('language-switcher-toggle').click();

    // Verify dropdown is visible
    await expect(page.getByTestId('language-dropdown')).toBeVisible();

    // Select Bulgarian
    await page.getByTestId('lang-bg').click();

    // Verify Bulgarian text (without page reload)
    await expect(page.getByText('Добави в кошницата')).toBeVisible();
    await expect(page.getByText('Продукти')).toBeVisible();
    await expect(page.getByPlaceholder('Търси продукти...')).toBeVisible();

    // Verify language switcher shows BG
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('BG');

    // Verify dropdown closes after selection
    await expect(page.getByTestId('language-dropdown')).not.toBeVisible();
  });

  test('user can switch language from English to German', async ({ page }) => {
    await page.goto('/');

    // Open language switcher
    await page.getByTestId('language-switcher-toggle').click();

    // Select German
    await page.getByTestId('lang-de').click();

    // Verify German text
    await expect(page.getByText('In den Warenkorb')).toBeVisible();
    await expect(page.getByText('Produkte')).toBeVisible();
    await expect(page.getByPlaceholder('Produkte suchen...')).toBeVisible();

    // Verify language switcher shows DE
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('DE');
  });

  test('user can switch through all languages without reload', async ({ page }) => {
    await page.goto('/');

    // Start with English
    await expect(page.getByText('Products')).toBeVisible();

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();
    await expect(page.getByText('Продукти')).toBeVisible();

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();
    await expect(page.getByText('Produkte')).toBeVisible();

    // Switch back to English
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-en').click();
    await expect(page.getByText('Products')).toBeVisible();

    // Verify no page reloads occurred (check navigation count)
    const navigationCount = await page.evaluate(() =>
      (window as any).navigationCount || 0
    );
    expect(navigationCount).toBe(0);
  });
});
```

### 4.3 Test I18N-E2E-002: Language Persistence

```typescript
// e2e/tests/i18n/persistence.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Language Persistence', () => {
  test('selected language persists after page reload', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Verify Bulgarian is active
    await expect(page.getByText('Добави в кошницата')).toBeVisible();

    // Reload page
    await page.reload();

    // Verify Bulgarian is still active after reload
    await expect(page.getByText('Добави в кошницата')).toBeVisible();
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('BG');
  });

  test('selected language persists across different pages', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // Navigate to products page
    await page.getByRole('link', { name: 'Produkte' }).click();

    // Verify German is still active
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('DE');
    await expect(page.getByText('Alle Produkte')).toBeVisible();

    // Navigate to cart
    await page.getByRole('link', { name: 'Warenkorb' }).click();

    // Verify German is still active
    await expect(page.getByText('Warenkorb')).toBeVisible();
  });

  test('language is stored in localStorage', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Verify localStorage contains the language
    const storedLang = await page.evaluate(() =>
      localStorage.getItem('climasite_language')
    );
    expect(storedLang).toBe('bg');
  });

  test('clears language preference when localStorage is cleared', async ({ page }) => {
    await page.goto('/');

    // Set Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Clear localStorage
    await page.evaluate(() => localStorage.clear());

    // Reload
    await page.reload();

    // Should default to English
    await expect(page.getByText('Add to Cart')).toBeVisible();
    await expect(page.getByTestId('language-switcher-toggle')).toContainText('EN');
  });
});
```

### 4.4 Test I18N-E2E-003: Translation Coverage

```typescript
// e2e/tests/i18n/translation-coverage.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Translation Coverage', () => {
  const languages = [
    { code: 'en', name: 'English', testId: 'lang-en' },
    { code: 'bg', name: 'Bulgarian', testId: 'lang-bg' },
    { code: 'de', name: 'German', testId: 'lang-de' }
  ];

  for (const lang of languages) {
    test.describe(`${lang.name} translations`, () => {
      test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await page.evaluate(() => localStorage.clear());
        await page.reload();

        if (lang.code !== 'en') {
          await page.getByTestId('language-switcher-toggle').click();
          await page.getByTestId(lang.testId).click();
        }
      });

      test('header elements are translated', async ({ page }) => {
        // No missing translation indicators (keys in brackets)
        const missingTranslations = await page.locator('text=/\\[.*\\]/').count();
        expect(missingTranslations).toBe(0);

        // Header elements exist
        await expect(page.getByTestId('header-cart')).toBeVisible();
        await expect(page.getByTestId('header-account')).toBeVisible();
        await expect(page.getByTestId('header-search')).toBeVisible();
      });

      test('navigation elements are translated', async ({ page }) => {
        await expect(page.getByRole('navigation')).toBeVisible();

        // Check for missing translations
        const navText = await page.getByRole('navigation').textContent();
        expect(navText).not.toMatch(/\[.*\]/);
      });

      test('product listing page is translated', async ({ page }) => {
        await page.goto('/products');

        // Check page content
        await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

        // No missing translations
        const pageContent = await page.locator('body').textContent();
        const missingMatches = pageContent?.match(/\[[\w.]+\]/g) || [];
        expect(missingMatches.length).toBe(0);
      });

      test('cart page is translated', async ({ page }) => {
        await page.goto('/cart');

        // Check cart elements
        await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

        // No missing translations
        const pageContent = await page.locator('body').textContent();
        expect(pageContent).not.toMatch(/\[[\w.]+\]/);
      });

      test('footer elements are translated', async ({ page }) => {
        await expect(page.getByRole('contentinfo')).toBeVisible();

        const footerText = await page.getByRole('contentinfo').textContent();
        expect(footerText).not.toMatch(/\[.*\]/);
      });
    });
  }

  test('no untranslated keys visible on any page', async ({ page }) => {
    const pagesToCheck = ['/', '/products', '/cart', '/account', '/contact'];

    for (const pagePath of pagesToCheck) {
      await page.goto(pagePath);

      // Check for translation key patterns (keys in brackets indicate missing)
      const missingTranslations = await page.locator('text=/\\[\\w+(\\.\\w+)*\\]/').count();

      expect(
        missingTranslations,
        `Found ${missingTranslations} missing translations on ${pagePath}`
      ).toBe(0);
    }
  });
});
```

### 4.5 Test I18N-E2E-004: Form Validation Messages

```typescript
// e2e/tests/i18n/form-validation.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Form Validation Messages', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  test('validation messages display in English', async ({ page }) => {
    // Submit empty form
    await page.getByRole('button', { name: 'Login' }).click();

    // Check validation messages
    await expect(page.getByText('This field is required')).toBeVisible();
  });

  test('validation messages display in Bulgarian', async ({ page }) => {
    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Submit empty form
    await page.getByRole('button', { name: 'Вход' }).click();

    // Check validation messages in Bulgarian
    await expect(page.getByText('Това поле е задължително')).toBeVisible();
  });

  test('validation messages display in German', async ({ page }) => {
    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // Submit empty form
    await page.getByRole('button', { name: 'Anmelden' }).click();

    // Check validation messages in German
    await expect(page.getByText('Dieses Feld ist erforderlich')).toBeVisible();
  });

  test('email validation message updates on language change', async ({ page }) => {
    // Enter invalid email
    await page.getByLabel('Email').fill('invalid-email');
    await page.getByLabel('Email').blur();

    // Check English message
    await expect(page.getByText('Please enter a valid email address')).toBeVisible();

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Check Bulgarian message (should update without re-validation)
    await expect(page.getByText('Моля, въведете валиден имейл адрес')).toBeVisible();
  });
});
```

### 4.6 Test I18N-E2E-005: Dynamic Content Translation

```typescript
// e2e/tests/i18n/dynamic-content.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Dynamic Content Translation', () => {
  test('product count updates with correct pluralization', async ({ page }) => {
    await page.goto('/products');

    // Verify English count format
    await expect(page.getByText(/Showing \d+ products/)).toBeVisible();

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Verify Bulgarian count format
    await expect(page.getByText(/Показани \d+ продукта/)).toBeVisible();

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // Verify German count format
    await expect(page.getByText(/\d+ Produkte angezeigt/)).toBeVisible();
  });

  test('cart item count updates language dynamically', async ({ page }) => {
    // Add item to cart first (assuming product page)
    await page.goto('/products/1');
    await page.getByRole('button', { name: 'Add to Cart' }).click();

    // Go to cart
    await page.goto('/cart');

    // Verify English
    await expect(page.getByText(/1 item\(s\)/)).toBeVisible();

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // Verify German
    await expect(page.getByText(/1 Artikel/)).toBeVisible();
  });

  test('date formats change based on language', async ({ page }) => {
    await page.goto('/account/orders');

    // Assuming there's an order with a date
    // English format: DD/MM/YYYY or similar
    const englishDateFormat = /\d{1,2}\/\d{1,2}\/\d{2,4}/;

    // Switch to German (expects DD.MM.YYYY)
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    const germanDateFormat = /\d{1,2}\.\d{1,2}\.\d{2,4}/;

    // Verify date format changed
    const dateElements = await page.locator('[data-testid="order-date"]').all();
    if (dateElements.length > 0) {
      const dateText = await dateElements[0].textContent();
      expect(dateText).toMatch(germanDateFormat);
    }
  });

  test('currency formats change based on language', async ({ page }) => {
    await page.goto('/products');

    // Get price element
    const priceLocator = page.locator('[data-testid="product-price"]').first();

    // English/Default format
    const englishPrice = await priceLocator.textContent();

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // German format uses comma as decimal separator
    const germanPrice = await priceLocator.textContent();

    // Prices should be formatted differently (e.g., "€1,234.56" vs "1.234,56 €")
    expect(germanPrice).not.toBe(englishPrice);
  });
});
```

### 4.7 Test I18N-E2E-006: Accessibility

```typescript
// e2e/tests/i18n/accessibility.spec.ts
import { test, expect } from '@playwright/test';

test.describe('i18n Accessibility', () => {
  test('html lang attribute updates on language change', async ({ page }) => {
    await page.goto('/');

    // Default should be English
    const htmlLang = await page.locator('html').getAttribute('lang');
    expect(htmlLang).toBe('en');

    // Switch to Bulgarian
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-bg').click();

    // Verify lang attribute updated
    const bgLang = await page.locator('html').getAttribute('lang');
    expect(bgLang).toBe('bg');

    // Switch to German
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    const deLang = await page.locator('html').getAttribute('lang');
    expect(deLang).toBe('de');
  });

  test('language switcher is keyboard accessible', async ({ page }) => {
    await page.goto('/');

    // Tab to language switcher
    await page.keyboard.press('Tab');
    // Continue tabbing until we reach the language switcher
    let attempts = 0;
    while (attempts < 20) {
      const focused = await page.evaluate(() => document.activeElement?.getAttribute('data-testid'));
      if (focused === 'language-switcher-toggle') break;
      await page.keyboard.press('Tab');
      attempts++;
    }

    // Open with Enter
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('language-dropdown')).toBeVisible();

    // Navigate with arrow keys
    await page.keyboard.press('ArrowDown');

    // Select with Enter
    await page.keyboard.press('Enter');

    // Dropdown should close
    await expect(page.getByTestId('language-dropdown')).not.toBeVisible();
  });

  test('language switcher has proper ARIA attributes', async ({ page }) => {
    await page.goto('/');

    const toggle = page.getByTestId('language-switcher-toggle');

    // Check ARIA attributes when closed
    await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    await expect(toggle).toHaveAttribute('aria-haspopup', 'listbox');

    // Open dropdown
    await toggle.click();

    // Check ARIA attributes when open
    await expect(toggle).toHaveAttribute('aria-expanded', 'true');

    // Check dropdown attributes
    const dropdown = page.getByTestId('language-dropdown');
    await expect(dropdown).toHaveAttribute('role', 'listbox');

    // Check option attributes
    const options = await page.locator('[role="option"]').all();
    expect(options.length).toBe(3);

    for (const option of options) {
      await expect(option).toHaveAttribute('aria-selected');
    }
  });

  test('escape key closes language dropdown', async ({ page }) => {
    await page.goto('/');

    // Open dropdown
    await page.getByTestId('language-switcher-toggle').click();
    await expect(page.getByTestId('language-dropdown')).toBeVisible();

    // Press Escape
    await page.keyboard.press('Escape');

    // Dropdown should close
    await expect(page.getByTestId('language-dropdown')).not.toBeVisible();
  });

  test('clicking outside closes language dropdown', async ({ page }) => {
    await page.goto('/');

    // Open dropdown
    await page.getByTestId('language-switcher-toggle').click();
    await expect(page.getByTestId('language-dropdown')).toBeVisible();

    // Click outside
    await page.locator('body').click({ position: { x: 10, y: 10 } });

    // Dropdown should close
    await expect(page.getByTestId('language-dropdown')).not.toBeVisible();
  });
});
```

### 4.8 Test I18N-E2E-007: Mobile Responsiveness

```typescript
// e2e/tests/i18n/mobile.spec.ts
import { test, expect, devices } from '@playwright/test';

test.describe('i18n Mobile Responsiveness', () => {
  test.use({ ...devices['iPhone 12'] });

  test('language switcher works on mobile', async ({ page }) => {
    await page.goto('/');

    // Language switcher should be visible
    await expect(page.getByTestId('language-switcher-toggle')).toBeVisible();

    // Open dropdown
    await page.getByTestId('language-switcher-toggle').click();

    // Dropdown should be visible and properly positioned
    await expect(page.getByTestId('language-dropdown')).toBeVisible();

    // Select Bulgarian
    await page.getByTestId('lang-bg').click();

    // Verify language changed
    await expect(page.getByText('Продукти')).toBeVisible();
  });

  test('language switcher shows only flag on mobile', async ({ page }) => {
    await page.goto('/');

    const toggle = page.getByTestId('language-switcher-toggle');

    // On mobile, language code might be hidden
    // Check that toggle is compact
    const toggleBox = await toggle.boundingBox();
    expect(toggleBox?.width).toBeLessThan(80); // Compact width
  });

  test('translated content fits mobile viewport', async ({ page }) => {
    await page.goto('/');

    // Switch to German (typically longer words)
    await page.getByTestId('language-switcher-toggle').click();
    await page.getByTestId('lang-de').click();

    // Check no horizontal scroll
    const hasHorizontalScroll = await page.evaluate(() => {
      return document.documentElement.scrollWidth > document.documentElement.clientWidth;
    });

    expect(hasHorizontalScroll).toBe(false);
  });
});
```

---

## 5. Implementation Timeline

| Phase | Tasks | Duration | Dependencies |
|-------|-------|----------|--------------|
| Phase 1 | I18N-001, I18N-002, I18N-003 | 1 week | None |
| Phase 2 | I18N-004, I18N-007 | 1 week | Phase 1 |
| Phase 3 | I18N-005, I18N-006, I18N-015 | 1 week | Phase 2 |
| Phase 4 | I18N-008, I18N-009, I18N-010 | 1 week | Phase 2 |
| Phase 5 | I18N-011, I18N-012, I18N-013, I18N-014 | 1 week | Phase 4 |
| Phase 6 | E2E Tests & QA | 1 week | All Phases |

**Total Estimated Duration:** 6 weeks

---

## 6. Definition of Done

- [ ] All translation files complete for EN, BG, DE
- [ ] Language switcher component functional
- [ ] Runtime language switching works without reload
- [ ] Language preference persists in localStorage
- [ ] All E2E tests passing
- [ ] No missing translation keys in production
- [ ] HTML lang attribute updates correctly
- [ ] Form validation messages translated
- [ ] Backend API responses localized
- [ ] Date and number formatting locale-aware
- [ ] Accessibility requirements met (ARIA, keyboard navigation)
- [ ] Mobile responsive design verified
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## 7. Future Considerations

### 7.1 Additional Languages
- Russian (RU)
- Romanian (RO)
- Greek (EL)
- Turkish (TR)

### 7.2 Advanced Features
- Right-to-left (RTL) language support (Arabic, Hebrew)
- Pluralization rules for complex languages
- Gender-specific translations
- Regional variants (en-US vs en-GB)
- Translation management system (TMS) integration
- Machine translation fallback for missing translations

### 7.3 Performance Optimizations
- Service worker caching for translation files
- Preloading of likely language switches
- Compression of translation JSON files
- Tree-shaking unused translations in production

---

## 8. References

- [ngx-translate Documentation](https://github.com/ngx-translate/core)
- [Angular i18n Guide](https://angular.io/guide/i18n)
- [Playwright Documentation](https://playwright.dev/docs/intro)
- [WCAG 2.1 Language Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/language-of-page.html)
