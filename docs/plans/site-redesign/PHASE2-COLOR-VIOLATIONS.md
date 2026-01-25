# Phase 2: Color Violation Cleanup

**Status**: Not Started  
**Identified**: 2026-01-25 (Design System Phase 1)  
**Total Violations**: 44 hardcoded hex colors across 3 files

## Summary

This document catalogs all hardcoded color values found in component files that violate the design system's CSS variable convention. These should be refactored in Phase 2 to use design tokens from `_colors.scss`.

## MEDIUM PRIORITY

### confetti.service.ts (9 violations)
**Location**: `src/ClimaSite.Web/src/app/core/services/confetti.service.ts`  
**Lines**: 153-161  
**Type**: Fallback colors for confetti animation

These are intentional fallbacks when CSS variables fail to load, but should be updated to match the new Terra Luxe palette:

```typescript
// Lines 153-161 - Update fallback values to match new palette
'--color-primary-400' fallback: '#38bdf8' → '#E88A5E' (terracotta)
'--color-primary-500' fallback: '#0ea5e9' → '#C4785A' (terracotta)
'--color-accent-400' fallback: '#22d3ee' → '#4D9999' (deep teal)
'--color-accent-500' fallback: '#06b6d4' → '#2A5A5A' (deep teal)
'--color-warm-400' fallback: '#fbbf24' → (keep, still valid)
'--color-warm-500' fallback: '#f59e0b' → (keep, still valid)
'--color-aurora-400' fallback: '#2dd4bf' → (keep, still valid)
'--color-success-400' fallback: '#34d399' → '#6B8E6B' (sage green)
'--color-ember-400' fallback: '#fb923c' → (keep, still valid)
```

**Recommendation**: Update fallback values to match new palette, but keep as fallbacks (don't remove).

---

### product-card.component.ts (5 violations)
**Location**: `src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts`  
**Lines**: 336, 341, 346, 351, 356  
**Type**: Status badge gradients

Hardcoded gradients for product status badges:

```typescript
// Line 336 - Success gradient
background: linear-gradient(135deg, #10b981 0%, #059669 100%);
// Should use: var(--color-success) or CSS variable-based gradient

// Line 341 - Available gradient  
background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);

// Line 346 - Warning gradient
background: linear-gradient(135deg, #eab308 0%, #ca8a04 100%);
// Should use: var(--color-warning)

// Line 351 - Hot gradient
background: linear-gradient(135deg, #f97316 0%, #ea580c 100%);

// Line 356 - Error gradient
background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
// Should use: var(--color-error)
```

**Recommendation**: Extract to CSS custom properties in `_colors.scss`:
```scss
--gradient-status-success: linear-gradient(135deg, var(--color-success) 0%, var(--color-success-dark) 100%);
--gradient-status-warning: linear-gradient(135deg, var(--color-warning) 0%, var(--color-warning-dark) 100%);
--gradient-status-error: linear-gradient(135deg, var(--color-error) 0%, var(--color-error-dark) 100%);
```

---

## LOW PRIORITY

### payment-icon.component.ts (30 violations)
**Location**: `src/ClimaSite.Web/src/app/shared/components/payment-icons/payment-icon.component.ts`  
**Lines**: 68-139  
**Type**: Payment brand colors (SVG fills)

These are **brand-mandated colors** for payment provider logos (Visa, Mastercard, PayPal, etc.). They should NOT be changed to match the design system, but should be documented and potentially extracted to constants.

**Examples**:
- Visa: `#1A1F71` (brand blue)
- Mastercard: `#EB001B`, `#F79E1B`, `#FF5F00` (brand red/orange)
- PayPal: `#003087`, `#009CDE` (brand blue)
- Apple Pay: `#000000` (brand black)
- Google Pay: `#4285F4`, `#34A853`, `#EA4335` (Google brand colors)

**Recommendation**: 
1. Extract to named constants at top of file:
   ```typescript
   const BRAND_COLORS = {
     visa: '#1A1F71',
     mastercard: { red: '#EB001B', orange: '#F79E1B', overlap: '#FF5F00' },
     // ... etc
   };
   ```
2. Add comment explaining these are brand requirements
3. Do NOT change these colors - they are legally required by payment providers

---

## Recommendations for Phase 2

1. **Confetti Service**: Update fallback values to match Terra Luxe palette
2. **Product Card**: Extract status gradients to CSS custom properties
3. **Payment Icons**: Extract to named constants with documentation
4. **General Pattern**: All future components must use CSS variables from `_colors.scss`

---

## Verification Commands

```bash
# Find remaining hardcoded hex colors
grep -r '#[0-9a-fA-F]\{6\}' src/ClimaSite.Web/src/app --include="*.ts"

# Find remaining rgba values
grep -r 'rgba(' src/ClimaSite.Web/src/app --include="*.ts"
```

---

**Last Updated**: 2026-01-25  
**Next Review**: Phase 2 kickoff
