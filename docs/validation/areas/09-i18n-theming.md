# Internationalization & Theming - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Multi-Language Support** - EN (English), BG (Bulgarian), DE (German)
- **Language Switcher** - Header dropdown with flag icons and native names
- **Translation Fallback** - English as default when translations missing
- **Translation Management** - Admin UI for product translations
- **Light/Dark Theme** - System preference detection and manual toggle
- **Theme Persistence** - LocalStorage for cross-session persistence
- **CSS Variable System** - Centralized color definitions in `_colors.scss`
- **Shared UI Components** - Themed reusable components (Alert, Modal, Toast, Breadcrumb, etc.)

### Supported Languages
| Code | Language | Native Name | Flag |
|------|----------|-------------|------|
| en | English | English | GB |
| bg | Bulgarian | Български | BG |
| de | German | Deutsch | DE |

### Theme Modes
| Mode | Description |
|------|-------------|
| `light` | Light theme with white backgrounds |
| `dark` | Dark theme with gray-900 backgrounds |
| `system` | Follows OS preference via `prefers-color-scheme` |

---

## 2. Code Path Map

### Frontend - Internationalization

| Layer | Files |
|-------|-------|
| **Service** | `src/ClimaSite.Web/src/app/core/services/language.service.ts` |
| **Component** | `src/ClimaSite.Web/src/app/shared/components/language-selector/language-selector.component.ts` |
| **Translations** | `src/ClimaSite.Web/src/assets/i18n/en.json` |
| | `src/ClimaSite.Web/src/assets/i18n/bg.json` |
| | `src/ClimaSite.Web/src/assets/i18n/de.json` |
| **Module Config** | `src/ClimaSite.Web/src/app/app.config.ts` (ngx-translate setup) |
| **Pipe** | `src/ClimaSite.Web/src/app/shared/pipes/spec-key.pipe.ts` |

### Frontend - Theming

| Layer | Files |
|-------|-------|
| **Service** | `src/ClimaSite.Web/src/app/core/services/theme.service.ts` |
| **Component** | `src/ClimaSite.Web/src/app/shared/components/theme-toggle/theme-toggle.component.ts` |
| **Styles** | `src/ClimaSite.Web/src/styles/_colors.scss` (SINGLE SOURCE OF TRUTH) |
| | `src/ClimaSite.Web/src/styles/styles.scss` (global imports) |

### Backend - Translation Management

| Layer | Files |
|-------|-------|
| **Controller** | `src/ClimaSite.Api/Controllers/Admin/ProductTranslationsController.cs` |
| **Entity** | `src/ClimaSite.Core/Entities/ProductTranslation.cs` |
| **Service** | Product queries accept `lang` parameter via Accept-Language header |

### Shared Components (Theme-Aware)

| Component | File |
|-----------|------|
| Alert | `src/ClimaSite.Web/src/app/shared/components/alert/alert.component.ts` |
| Modal | `src/ClimaSite.Web/src/app/shared/components/modal/modal.component.ts` |
| Toast | `src/ClimaSite.Web/src/app/shared/components/toast/toast.component.ts` |
| Breadcrumb | `src/ClimaSite.Web/src/app/shared/components/breadcrumb/breadcrumb.component.ts` |
| Button | `src/ClimaSite.Web/src/app/shared/components/button/button.component.ts` |
| Card | `src/ClimaSite.Web/src/app/shared/components/card/card.component.ts` |
| Input | `src/ClimaSite.Web/src/app/shared/components/input/input.component.ts` |
| Badge | `src/ClimaSite.Web/src/app/shared/components/badge/badge.component.ts` |
| Loading | `src/ClimaSite.Web/src/app/shared/components/loading/loading.component.ts` |

---

## 3. Test Coverage Audit

### Unit Tests - LanguageService

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `language.service.spec.ts` | `should be created` | Service instantiation |
| | `should have supported languages` | Languages array includes en, bg, de |
| | `should default to English when no stored preference` | Default behavior |
| | `should set language and persist to localStorage` | Persistence |
| | `should use stored language on initialization` | Load from storage |
| | `should return current language info` | `getCurrentLanguageInfo()` |
| | `should not set invalid language` | Validation |
| | `should update translate service when language changes` | Integration with ngx-translate |

### Unit Tests - ThemeService

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `theme.service.spec.ts` | `should be created` | Service instantiation |
| | `should default to system preference when no stored theme` | Default behavior |
| | `should apply light theme by default when system preference is light` | System preference |
| | `should toggle between light and dark mode` | `toggleTheme()` |
| | `should cycle through light, dark, and system modes` | `cycleTheme()` |
| | `should persist theme preference to localStorage` | Persistence |
| | `should load theme preference from localStorage` | Load from storage |
| | `should return correct isDarkMode computed value` | Computed signal |
| | `should set dark mode correctly` | Signal values verification |

### Unit Tests - LanguageSelectorComponent

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `language-selector.component.spec.ts` | `should create` | Component instantiation |
| | Other tests exist | Basic functionality verified |

### Unit Tests - ThemeToggleComponent

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `theme-toggle.component.spec.ts` | `should create` | Component instantiation |
| | `should have data-testid attribute` | Test attribute present |
| | `should toggle theme when clicked in toggle mode` | Toggle functionality |
| | `should show sun icon when in light mode` | Icon visibility |
| | `should show moon icon when in dark mode` | Icon visibility |
| | `should have accessible aria-label` | Accessibility |
| | `should be keyboard accessible` | Keyboard support |

### E2E Tests - Theme & Settings

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `ThemeAndSettingsTests.cs` | `ThemeToggle_SwitchesBetweenLightAndDark` (E2E-070) | Theme toggle |
| | `ThemePersistence_AcrossNavigation` (E2E-071) | Navigation persistence |
| | `ThemePersistence_AcrossRefresh` (E2E-072) | Refresh persistence |
| | `LanguageSelector_ShowsAvailableLanguages` (E2E-073) | Language dropdown |
| | `LanguageChange_UpdatesUIText` (E2E-074) | UI translation update |
| | `LanguagePersistence_AcrossNavigation` (E2E-075) | Language persistence |
| | `Accessibility_KeyboardNavigation` | Keyboard navigation |
| | `ResponsiveDesign_MobileViewport` | Mobile responsiveness |
| | `ResponsiveDesign_TabletViewport` | Tablet responsiveness |

### E2E Tests - Language/Internationalization

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `LanguageTests.cs` | `HomePage_LoadsWithDefaultLanguage` | Default language loading |
| | `ProductPage_LoadsWithLanguageParameter` | Product page i18n |
| | `ProductList_LoadsProductsWithCurrentLanguage` | Product list i18n |
| | `ProductDetail_DisplaysTranslatedContent_WhenAvailable` | Translations display |
| | `APIEndpoint_AcceptsLanguageParameter` | API lang parameter |
| | `ProductSearch_WorksWithCurrentLanguage` | Search with language |

### E2E Tests - User Journey (Theme/Language)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `UserJourneyTests.cs` | `User changes language and completes purchase in different language` | Full language journey |
| | `User switches between light and dark themes` | Theme switching journey |

---

## 4. Manual Verification Steps

### Language Switching
1. Navigate to home page
2. Verify English content loads by default
3. Click/hover on language selector in header
4. Verify dropdown shows EN, BG, DE with flags and native names
5. Select Bulgarian (BG)
6. Verify all UI text changes to Bulgarian
7. Navigate to products page - verify language persists
8. Refresh page - verify language persists
9. Select German (DE) - verify translation
10. Select English (EN) - verify return to English

### Theme Switching
1. Navigate to home page
2. Click theme toggle button in header
3. Verify background changes from light to dark (or vice versa)
4. Verify text colors adjust appropriately
5. Navigate to product detail page - verify theme persists
6. Refresh page - verify theme persists
7. Toggle again - verify switch works
8. Test in system mode by changing OS dark mode setting

### Translation Coverage Check
1. Navigate through all major pages:
   - Home page (hero, categories, benefits, testimonials)
   - Product list (filters, sorting, pagination)
   - Product detail (description, specs, reviews, Q&A)
   - Cart page (summary, checkout button)
   - Checkout (shipping, payment, review steps)
   - Account pages (profile, orders, addresses)
   - Admin panel (if admin user)
2. For each page, switch to Bulgarian and German
3. Verify no English text leaks through (except brand names)
4. Verify no raw translation keys shown (e.g., `common.save`)

### Dark Theme Parity Check
1. Set dark theme
2. Navigate through all pages
3. Verify:
   - No white text on white background
   - No black text on dark background
   - Form inputs are readable
   - Buttons have proper contrast
   - Cards/modals have appropriate backgrounds
   - Icons are visible
   - Shadows work correctly
   - Focus rings are visible

### Shared Components Theme Check
1. Trigger an Alert component (success, error, warning, info variants)
2. Open a Modal (verify backdrop and content)
3. Trigger a Toast notification
4. Check Breadcrumb navigation
5. Verify Button variants (primary, secondary, outline)
6. Check form Inputs (focus states, validation states)
7. All above in both light and dark themes

---

## 5. Gaps & Risks

### Critical Gaps

- [ ] **No E2E test for all 3 languages** - Tests verify language selector works but don't fully validate all UI text in each language
- [ ] **No automated translation completeness check** - No test verifies all keys exist in all 3 JSON files
- [ ] **No RTL language support** - Only LTR languages (EN, BG, DE) supported

### Medium Gaps

- [ ] **No translation key validation at build time** - Missing keys only discovered at runtime
- [ ] **No i18n for error messages from API** - Backend errors returned in English only
- [ ] **No pluralization tests** - ngx-translate pluralization not explicitly tested
- [ ] **No date/number format localization tests** - Currency/date formatting not verified per locale
- [ ] **No accessibility tests for language/theme controls** - Screen reader compatibility not tested

### Low Gaps

- [ ] **No visual regression tests for themes** - No screenshot comparison between themes
- [ ] **Profile language selector not synced with header** - Profile page has separate language dropdown
- [ ] **No keyboard shortcut for theme toggle** - Must click button, no Ctrl+Shift+T style shortcut

### Technical Risks

- [ ] **Large translation files** - Each JSON is ~1200 lines, may impact initial load
- [ ] **No lazy loading of translations** - All translations loaded upfront
- [ ] **CSS variable fallback** - Some browsers may not support CSS custom properties (very old browsers)

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **Critical** | No translation completeness check | Create build script to compare keys across en.json, bg.json, de.json |
| **High** | No full E2E for each language | Add `LanguageTests` that verify specific translated strings appear on page |
| **High** | API errors in English only | Implement `Accept-Language` header handling in API for error messages |
| **Medium** | No visual regression for themes | Add Playwright screenshot tests comparing light/dark for key pages |
| **Medium** | Large translation files | Implement lazy loading of translations per route module |
| **Medium** | No pluralization tests | Add unit tests for translation keys with `count` parameter |
| **Low** | No keyboard shortcut for theme | Add Ctrl+Shift+D keyboard shortcut in ThemeService |
| **Low** | Profile language not synced | Sync profile language dropdown with header selector |

### Suggested New Tests

```typescript
// Unit test: Translation completeness
describe('Translation Files', () => {
  it('should have same keys in all languages', () => {
    const enKeys = getAllKeys(en);
    const bgKeys = getAllKeys(bg);
    const deKeys = getAllKeys(de);
    
    expect(bgKeys).toEqual(enKeys);
    expect(deKeys).toEqual(enKeys);
  });
});

// E2E test: Verify Bulgarian text
[Fact]
public async Task HomePage_DisplaysBulgarianContent_WhenBGSelected()
{
    await homePage.SelectLanguageAsync("bg");
    var heroTitle = await _page.TextContentAsync("[data-testid='hero-title']");
    heroTitle.Should().Contain("Перфектен климат");
}

// E2E test: Theme visual comparison
[Fact]
public async Task ProductPage_LooksCorrect_InDarkMode()
{
    await themeToggle.ClickAsync();
    await Expect(_page).ToHaveScreenshotAsync("product-page-dark.png");
}
```

---

## 7. Evidence & Notes

### Translation File Structure

All translation files follow the same nested key structure:

```json
{
  "common": { "appName": "...", "loading": "...", "aria": { ... } },
  "nav": { "home": "...", "products": "...", ... },
  "auth": { "login": { ... }, "register": { ... }, ... },
  "products": { "title": "...", "filters": { ... }, "details": { ... }, ... },
  "cart": { ... },
  "checkout": { ... },
  "account": { ... },
  "footer": { ... },
  "errors": { ... },
  "home": { "hero": { ... }, "categories": { ... }, ... },
  "admin": { ... },
  ...
}
```

Key counts per file:
- `en.json`: ~1230 lines
- `bg.json`: ~1230 lines  
- `de.json`: ~1230 lines

### CSS Variable System

From `_colors.scss`:

```scss
// Light theme (default in :root)
--color-bg-primary: #ffffff;
--color-text-primary: #0f172a;
--color-primary: #0ea5e9;

// Dark theme ([data-theme="dark"])
--color-bg-primary: #0f172a;
--color-text-primary: #f8fafc;
--color-primary: #38bdf8;
```

The file defines:
- 12 gray scale colors
- 10 primary palette colors (Arctic Blue)
- 10 secondary, accent, warm, ember, aurora palettes
- Success, warning, error, info semantic colors
- Glass effects and gradients
- Light/dark variants for all semantic variables

### Theme Transition

Smooth transitions via CSS:
```scss
--theme-transition: background-color 0.3s ease, color 0.3s ease, 
                    border-color 0.3s ease, box-shadow 0.3s ease;
```

### Language Detection Priority

From `LanguageService`:
1. LocalStorage (`climasite-language`)
2. Browser language (`navigator.language`)
3. Default to English (`en`)

### Theme Detection Priority

From `ThemeService`:
1. LocalStorage (`climasite-theme-preference`)
2. System preference (`prefers-color-scheme`)
3. Default to `system` mode

### Components Using TranslateModule

Major components verified to use `TranslateModule`:
- `ContactComponent`
- `WishlistComponent`
- `FooterComponent`
- `ProfileComponent`
- `ProductListComponent`
- `CheckoutComponent`
- `CartComponent`
- `HomeComponent`
- `LoginComponent`
- `RegisterComponent`
- `ForgotPasswordComponent`
- `ResetPasswordComponent`
- `OrderDetailsComponent`
- `BreadcrumbComponent`
- `ProductReviewsComponent`
- All admin components

### data-testid Attributes

Key selectors for E2E testing:
- `[data-testid='theme-toggle']` - Theme toggle button
- `[data-testid='language-selector']` - Language dropdown container
- `[data-testid='language-dropdown']` - Language options dropdown
- `[data-testid='language-en']` - English option
- `[data-testid='language-bg']` - Bulgarian option
- `[data-testid='language-de']` - German option

### Accessibility

- Theme toggle has `aria-label` ("Switch to dark/light mode")
- Language selector has `aria-expanded`, `aria-haspopup`
- Language options use `role="listbox"` and `role="option"`
- Escape key closes language dropdown
- Focus visible states styled with `--color-ring`
