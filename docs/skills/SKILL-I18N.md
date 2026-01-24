# Skill: Internationalization Auditor (SKILL-I18N)

## Purpose

The i18n Auditor identifies translation issues, hardcoded strings, locale problems, and anything that prevents the site from working well in all supported languages (EN, BG, DE).

## Supported Languages

| Code | Language | Status |
|------|----------|--------|
| en | English | Primary |
| bg | Bulgarian | Full support |
| de | German | Full support |

## What You Look For

### 1. Hardcoded Strings
- [ ] All user-visible text uses translation keys
- [ ] No text in templates that should be translated
- [ ] No text in component files that should be translated
- [ ] Error messages use translation keys
- [ ] Validation messages use translation keys
- [ ] Toast/notification messages translated
- [ ] Modal titles and content translated
- [ ] Button labels translated
- [ ] Placeholder text translated
- [ ] Alt text translated (where appropriate)

### 2. Missing Translations
- [ ] All keys exist in en.json
- [ ] All keys exist in bg.json
- [ ] All keys exist in de.json
- [ ] No empty translation values
- [ ] No placeholder text like "TODO" or "TRANSLATE ME"

### 3. Translation Quality
- [ ] Translations make sense in context
- [ ] Grammar is correct
- [ ] No truncated text due to length
- [ ] Technical terms translated appropriately
- [ ] Consistent terminology throughout

### 4. Date/Time Formatting
- [ ] Dates formatted for locale (DD/MM/YYYY vs MM/DD/YYYY)
- [ ] Times formatted for locale (24h vs 12h AM/PM)
- [ ] Relative dates work ("2 days ago")
- [ ] Time zones handled correctly

### 5. Number Formatting
- [ ] Decimal separator correct (. vs ,)
- [ ] Thousands separator correct (, vs . vs space)
- [ ] Currency symbol position correct
- [ ] Currency code correct (EUR, BGN, etc.)
- [ ] Percentages formatted correctly

### 6. Currency
- [ ] Currency matches locale expectations
- [ ] Currency conversion (if applicable)
- [ ] Price display consistent

### 7. Text Direction
- [ ] LTR languages display correctly
- [ ] RTL support (if needed for future)

### 8. Locale-Specific Content
- [ ] Contact information locale-aware
- [ ] Address format locale-aware
- [ ] Phone number format locale-aware

### 9. Language Switching
- [ ] Language switcher accessible
- [ ] Language persists across pages
- [ ] Language persists across sessions
- [ ] URL/route doesn't break on switch
- [ ] Content updates immediately on switch

### 10. Dynamic Content
- [ ] Product names/descriptions translated (if applicable)
- [ ] Category names translated
- [ ] System-generated content (emails, etc.) translated

## Testing Methodology

### 1. Language Switch Test
For each page:
1. Load in English
2. Switch to Bulgarian - verify all text changes
3. Switch to German - verify all text changes
4. Look for any text that didn't change (hardcoded)

### 2. Console Check
```javascript
// In browser console, look for missing translation warnings
// ngx-translate logs warnings for missing keys
```

### 3. Translation File Audit
```bash
# Compare translation files
diff <(jq -S 'keys' en.json) <(jq -S 'keys' bg.json)
diff <(jq -S 'keys' en.json) <(jq -S 'keys' de.json)
```

### 4. Search for Hardcoded Strings
```bash
# Look for hardcoded strings in templates
grep -r ">" --include="*.html" src/app | grep -v "{{"
# Look for hardcoded strings in components
grep -r "'" --include="*.ts" src/app | grep -v "import\|from\|translate"
```

### 5. Text Overflow Test
Switch to German (usually longest) and check:
- Buttons don't overflow
- Labels fit in their containers
- Table headers fit
- Navigation items fit

## Translation File Structure Check

### Required Key Categories
```json
{
  "common": { /* Shared strings */ },
  "nav": { /* Navigation items */ },
  "home": { /* Home page */ },
  "products": { /* Product catalog */ },
  "cart": { /* Shopping cart */ },
  "checkout": { /* Checkout flow */ },
  "auth": { /* Authentication */ },
  "account": { /* Account pages */ },
  "admin": { /* Admin panel */ },
  "errors": { /* Error messages */ },
  "validation": { /* Form validation */ }
}
```

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | Text unreadable/missing in production language | Empty button, no error message |
| HIGH | Hardcoded English in non-English locale | "Add to Cart" shown in Bulgarian |
| MEDIUM | Translation exists but is poor quality | Grammatically incorrect |
| LOW | Minor translation improvement | Could use better wording |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: i18n
- **Type**: [Hardcoded | Missing | Quality | Format]
- **Location**: [File path or URL]
- **Language(s) Affected**: [en/bg/de/all]
- **Current Text**: [What's there now]
- **Expected Text**: [What should be there]
- **Translation Key**: [If applicable]
- **Screenshot**: [If helpful]
- **Status**: Open
```

## Common Issues to Watch For

1. **Hardcoded strings** - English text that never translates
2. **Missing keys** - Translation key doesn't exist in .json
3. **Empty translations** - Key exists but value is empty
4. **Untranslated English** - bg.json has English text
5. **Text overflow** - German text too long for container
6. **Wrong date format** - American dates in European locale
7. **Wrong number format** - 1,234.56 vs 1.234,56
8. **Inconsistent terminology** - "Cart" vs "Basket" in same language
9. **Error messages hardcoded** - API errors in English only
10. **Placeholder hardcoded** - "Search..." not translated
11. **Dynamic content not translated** - Product names always English
12. **Language doesn't persist** - Resets on page refresh

## Translation Files Location

```
src/ClimaSite.Web/src/assets/i18n/
├── en.json
├── bg.json
└── de.json
```

## Quick Commands

```bash
# Check for missing keys
cd src/ClimaSite.Web/src/assets/i18n
node -e "
const en = require('./en.json');
const bg = require('./bg.json');
const de = require('./de.json');
const enKeys = Object.keys(JSON.stringify(en).match(/\"[^\"]+\":/g));
const bgKeys = Object.keys(JSON.stringify(bg).match(/\"[^\"]+\":/g));
const deKeys = Object.keys(JSON.stringify(de).match(/\"[^\"]+\":/g));
console.log('EN keys:', enKeys.length);
console.log('BG keys:', bgKeys.length);
console.log('DE keys:', deKeys.length);
"
```
