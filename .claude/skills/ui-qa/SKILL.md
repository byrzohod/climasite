# /ui-qa - Comprehensive UI QA via Playwright automation

## When to use

Invoke this skill:
- **After every UI change** -- before PR submission
- **As part of CI on every PR** -- automated runs
- **Before every release** -- full manual + automated pass

Every UI change must be verified against the categories below. These are the most common, annoying, and user-facing issues that slip through. Use Playwright to automate as many as possible.

## How to run this skill

1. Launch the app locally (or point to a test deployment)
2. Navigate to every page/route in the app
3. Run the console error, network error, axe-core, translation key, horizontal scroll, CLS, alt text, and placeholder content checks on each page
4. Take snapshots across all configured viewports
5. Generate a report listing every issue found, grouped by page and category
6. Return a clear pass/fail verdict

## UI QA Checklist

### Internationalization / Translations
- [ ] No raw translation keys visible (`user.login.button`, `errors.404.title`)
- [ ] No untranslated English strings in non-English locales
- [ ] No hardcoded strings bypassing the i18n framework
- [ ] No interpolation placeholders leaking (`{count}`, `%s`, `{{name}}`)
- [ ] Text does not overflow when translated (German +30-40%, French/Spanish +15-30%)
- [ ] Plural rules handled (no "1 items" or "0 item")
- [ ] RTL layout tested if supported (icons mirrored, text-align correct, direction flipped)
- [ ] Date/number/currency formats match locale

### Visual / Contrast / Accessibility
- [ ] No black text on black background, no white text on white
- [ ] Text contrast meets WCAG AA: 4.5:1 normal text, 3:1 large text and UI components
- [ ] Text readable over images/gradients (scrim or text-shadow present)
- [ ] Focus indicators visible on all interactive elements (no `outline: none` without replacement)
- [ ] Color is not the only way to convey meaning (icons + text accompany red/green)
- [ ] Heading hierarchy correct (no h1 -> h3 jumps, only one h1 per page)
- [ ] All form inputs have associated labels
- [ ] Icon-only buttons have accessible names (`aria-label` or visually hidden text)

### Layout & Responsive
- [ ] No horizontal scrollbar at any breakpoint
- [ ] No overflowing containers or cut-off text
- [ ] No overlapping elements
- [ ] Images load correctly and have proper aspect ratios
- [ ] Fixed headers don't cover anchor-linked content
- [ ] Z-index stacking correct: modals above overlays above tooltips above page content
- [ ] Modal backdrop blocks scroll of underlying page
- [ ] Scroll lock restored after modal close
- [ ] Tables responsive on mobile (horizontal scroll or stacked layout)
- [ ] Long unbreakable strings (URLs, tokens) don't blow out the layout
- [ ] Tested at: 320, 375, 414, 768, 1024, 1280, 1440, 1920 widths

### Forms
- [ ] Validation fires inline, not only on submit
- [ ] Error messages clear and actionable ("Email is required" not "Invalid input")
- [ ] Errors announced to screen readers (`aria-invalid`, `aria-describedby`)
- [ ] Required fields indicated visually AND via `aria-required`
- [ ] Labels connected to inputs (clicking label focuses input)
- [ ] Tab order logical (no `tabindex` > 0)
- [ ] Form state preserved on validation errors
- [ ] Submit button shows loading state and prevents double-submission
- [ ] Autocomplete attributes set (`email`, `current-password`, `new-password`)
- [ ] `inputmode` set for numeric/tel fields on mobile
- [ ] Error messages appear near the field causing them
- [ ] Success confirmation shown after submission
- [ ] Destructive actions require confirmation
- [ ] Placeholder text is NOT used as the only label

### Interactive / Behavior
- [ ] Hover states work and are not stuck on touch devices
- [ ] Click/tap targets are at least 44x44 px on mobile
- [ ] Double-click does not cause duplicate API calls or navigation
- [ ] Scroll position preserved on route change / modal close
- [ ] Back button works correctly in SPAs
- [ ] Loading states present for all async operations
- [ ] Empty states present ("No results found")
- [ ] Error states present (network failure handled gracefully)
- [ ] Dropdowns/menus close on outside click and Escape key
- [ ] Modals trap focus and restore focus on close
- [ ] Debounced search works correctly (not firing on every keystroke, not missing updates)

### Content & Text
- [ ] No typos or grammar errors
- [ ] No lorem ipsum / placeholder text / "test123"
- [ ] No TODO, FIXME, XXX, or HACK comments visible
- [ ] No `console.log` or debug statements left in production code
- [ ] No `undefined`, `null`, `NaN`, `[object Object]`, `Invalid Date` rendered
- [ ] No email merge tag leakage ("Hello {{firstName}}")
- [ ] Consistent capitalization (Title Case vs Sentence case)
- [ ] Consistent terminology throughout (Sign in vs Log in, Delete vs Remove)
- [ ] Markdown renders correctly (no raw `**bold**` or `[text](url)`)
- [ ] No HTML entities visible (`&amp;`, `&nbsp;`)

### Images & Media
- [ ] All images have meaningful `alt` attributes (or empty for decorative)
- [ ] No broken image URLs (404s)
- [ ] Images have `width` and `height` attributes (prevents CLS)
- [ ] Images below the fold use `loading="lazy"` (but NOT the LCP hero image)
- [ ] Videos have captions or subtitles for accessibility
- [ ] Videos do not autoplay with sound
- [ ] SVG icons have `role="img"` and accessible title if meaningful

### Performance
- [ ] Cumulative Layout Shift (CLS) < 0.1
- [ ] Largest Contentful Paint (LCP) < 2.5s
- [ ] Interaction to Next Paint (INP) < 200ms
- [ ] No Flash of Unstyled Content (FOUC)
- [ ] Fonts use `font-display: swap` or `optional`
- [ ] Skeleton/placeholder shown for slow-loading content

### Navigation
- [ ] No broken internal links (404s)
- [ ] External links open in new tab with `rel="noopener noreferrer"`
- [ ] Active nav item highlighted on current route
- [ ] Breadcrumbs show correct hierarchy
- [ ] Logo links to home page
- [ ] 404 page exists and provides navigation back
- [ ] 500 page exists

### State & Data
- [ ] Loading spinners always resolve (no infinite loading)
- [ ] Data refreshes correctly after mutations (no stale cache)
- [ ] Race conditions on rapid clicks handled (debounce, disable button)
- [ ] Pagination shows correct page numbers and totals
- [ ] Filters and sort indicators reflect actual applied state
- [ ] "Clear all" actually clears all
- [ ] Tokens refresh correctly (no 401 loops)

### Browser / Device
- [ ] Tested on Safari, Chrome, Firefox at minimum
- [ ] Mobile touch targets at least 44x44 px
- [ ] iOS safe area insets respected (notch, home indicator)
- [ ] iOS keyboard does not permanently cover inputs
- [ ] Input `font-size` >= 16px on iOS (prevents zoom on focus)
- [ ] Pinch-zoom not disabled
- [ ] Use `100dvh` instead of `100vh` on mobile where appropriate

### UX / Mental Model
- [ ] Destructive actions (delete, unsubscribe) require confirmation OR provide undo
- [ ] Success feedback is clear and visible
- [ ] Error recovery path is clear (not a dead end)
- [ ] Progress indicator for multi-step flows
- [ ] Session expiration warns the user (does not just dump them)
- [ ] No dark patterns (pre-checked opt-ins, confirmshaming, roach-motel subscriptions)

## Playwright Automation Snippets

### Console & Network Error Detection
```js
const consoleErrors = [];
const failedRequests = [];
page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
page.on('pageerror', err => consoleErrors.push(err.message));
page.on('response', r => { if (r.status() >= 400) failedRequests.push(r.url()); });
page.on('requestfailed', r => failedRequests.push(r.url()));
// ... run test ...
expect(consoleErrors).toEqual([]);
expect(failedRequests).toEqual([]);
```

### Accessibility & Contrast (axe-core)
```js
import AxeBuilder from '@axe-core/playwright';
const results = await new AxeBuilder({ page })
  .withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa'])
  .analyze();
expect(results.violations).toEqual([]);
```

### Raw Translation Key Detection
```js
const bodyText = await page.locator('body').innerText();
const rawKeys = bodyText.match(/\b[a-z]+(\.[a-z_]+){2,}\b/gi) || [];
const interpolations = bodyText.match(/\{\{?\s*\w+\s*\}?\}/g) || [];
expect(rawKeys).toEqual([]);
expect(interpolations).toEqual([]);
```

### Horizontal Scroll Detection
```js
const hasHorizontalScroll = await page.evaluate(
  () => document.documentElement.scrollWidth > window.innerWidth
);
expect(hasHorizontalScroll).toBe(false);
```

### Layout Shift (CLS) Measurement
```js
const cls = await page.evaluate(() => new Promise(resolve => {
  let total = 0;
  new PerformanceObserver(list => {
    for (const e of list.getEntries()) if (!e.hadRecentInput) total += e.value;
  }).observe({ type: 'layout-shift', buffered: true });
  setTimeout(() => resolve(total), 3000);
}));
expect(cls).toBeLessThan(0.1);
```

### Missing Alt Text Detection
```js
const missing = await page.$$eval('img', imgs =>
  imgs.filter(i => !i.alt && i.getAttribute('role') !== 'presentation').map(i => i.src)
);
expect(missing).toEqual([]);
```

### Broken Link Checking
```js
const links = await page.$$eval('a[href]', as => as.map(a => a.href));
for (const url of links) {
  const response = await page.request.head(url);
  expect.soft(response.status(), `Broken link: ${url}`).toBeLessThan(400);
}
```

### Placeholder Content Detection
```js
const bodyText = await page.locator('body').innerText();
expect(bodyText).not.toMatch(/lorem ipsum|TODO|FIXME|console\.log|undefined|\[object Object\]/i);
```

### Visual Regression (Snapshots)
```js
await expect(page).toHaveScreenshot('login-page.png', { maxDiffPixelRatio: 0.01 });
```

### Multi-Viewport Testing (playwright.config.ts)
```js
export default defineConfig({
  projects: [
    { name: 'mobile', use: devices['iPhone 14'] },
    { name: 'tablet', use: devices['iPad Pro'] },
    { name: 'desktop-1280', use: { viewport: { width: 1280, height: 720 } } },
    { name: 'desktop-1920', use: { viewport: { width: 1920, height: 1080 } } },
    { name: 'dark-mode', use: { colorScheme: 'dark', viewport: { width: 1280, height: 720 } } },
  ],
});
```

## What Playwright CANNOT catch automatically

These still need manual review:
- Copy quality and tone
- Alt text meaningfulness (only presence)
- Cognitive accessibility (reading level)
- Subjective design judgment
- Some race conditions
- About 60% of WCAG issues require manual testing

Don't let automation give you false confidence -- still do manual spot checks on critical flows, especially around content, copy, and subjective design quality.
