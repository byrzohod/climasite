# Skill: UI Auditor (SKILL-UI)

## Purpose

The UI Auditor identifies visual inconsistencies, design system violations, and aesthetic issues that make the interface feel unpolished or unprofessional.

## What You Look For

### 1. Spacing & Layout
- [ ] Consistent padding/margins (8px grid system)
- [ ] Proper alignment (left, center, right consistency)
- [ ] Adequate whitespace (not cramped, not too sparse)
- [ ] Grid consistency (columns align, gutters consistent)
- [ ] Content width consistency across pages
- [ ] Vertical rhythm (consistent spacing between sections)

### 2. Typography
- [ ] Correct font families used (Space Grotesk, Inter, JetBrains Mono)
- [ ] Consistent font sizes (use defined scale)
- [ ] Proper font weights (not mixing weights randomly)
- [ ] Line heights appropriate for text size
- [ ] Letter spacing where needed
- [ ] Text truncation handled properly (ellipsis, no overflow)
- [ ] No orphaned words in headings

### 3. Colors
- [ ] Only design system colors used (no hardcoded hex)
- [ ] Proper contrast ratios (text readable)
- [ ] Consistent use of primary/secondary/accent
- [ ] Hover states have appropriate color changes
- [ ] Active/selected states clearly visible
- [ ] Error/success/warning colors consistent
- [ ] Dark mode colors work properly

### 4. Borders & Shadows
- [ ] Consistent border radius (use tokens)
- [ ] Consistent border widths
- [ ] Border colors from design system
- [ ] Shadow depths appropriate (not too heavy/light)
- [ ] Shadow consistency across similar elements
- [ ] Focus rings visible and consistent

### 5. Icons & Images
- [ ] Icons consistent style (all outline or all filled)
- [ ] Icons proper size relative to context
- [ ] Icons aligned with text properly
- [ ] Images have proper aspect ratios
- [ ] Images don't stretch/distort
- [ ] Placeholder images look intentional
- [ ] No broken image links

### 6. Buttons & Interactive Elements
- [ ] Button sizes consistent
- [ ] Button styles match their importance
- [ ] Disabled states look disabled
- [ ] Loading states look polished
- [ ] Hover effects smooth
- [ ] Click/active states visible

### 7. Forms
- [ ] Input heights consistent
- [ ] Label styles consistent
- [ ] Placeholder text styled appropriately
- [ ] Focus states visible
- [ ] Error styling consistent
- [ ] Required field indicators consistent

### 8. Cards & Containers
- [ ] Card styles consistent
- [ ] Padding inside cards consistent
- [ ] Card shadows/borders match design system
- [ ] Hover effects (if any) consistent
- [ ] Content layout inside cards consistent

### 9. Animations & Transitions
- [ ] Transitions smooth (not janky)
- [ ] Animation duration appropriate
- [ ] Easing feels natural
- [ ] No animation artifacts
- [ ] Animations don't distract

### 10. Responsive Behavior
- [ ] Elements resize gracefully
- [ ] No horizontal overflow
- [ ] Text remains readable at all sizes
- [ ] Touch targets adequate on mobile

## Tools & Techniques

1. **Visual Inspection**: Slowly scroll through each page, zoom in on details
2. **Browser DevTools**: Inspect computed styles, check for hardcoded values
3. **Pixel Comparison**: Compare similar elements for inconsistencies
4. **Resize Testing**: Check at various viewport widths
5. **Theme Toggle**: Verify both light and dark modes

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | Breaks visual hierarchy, unreadable | White text on white background |
| HIGH | Clearly noticeable, looks unprofessional | Misaligned columns, wrong font |
| MEDIUM | Noticeable on inspection | Inconsistent padding, slightly off colors |
| LOW | Minor polish | 1px misalignment, subtle spacing |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: UI
- **Location**: [Page URL or component file path]
- **Element**: [CSS selector or description]
- **Description**: [What's wrong]
- **Expected**: [What it should look like]
- **Actual**: [What it looks like now]
- **Screenshot**: [If helpful]
- **Suggested Fix**: [CSS/code changes needed]
- **Status**: Open
```

## Common Issues to Watch For

1. **Inconsistent button padding** - Primary vs secondary buttons have different padding
2. **Mixed icon styles** - Some icons are outline, some are filled
3. **Hardcoded colors** - `#3b82f6` instead of `var(--color-primary)`
4. **Font inconsistency** - Wrong font family on some elements
5. **Shadow mismatch** - Different shadow depths on similar cards
6. **Border radius variance** - Some buttons 8px, some 12px
7. **Spacing inconsistency** - 16px gap here, 20px gap there
8. **Text overflow** - Long product names break layout
9. **Image aspect ratio** - Product images stretched
10. **Hover state missing** - Clickable element has no hover feedback
