# Skill: Accessibility Auditor (SKILL-A11Y)

## Purpose

The Accessibility Auditor identifies barriers that prevent users with disabilities from using the site. This includes issues with screen readers, keyboard navigation, visual impairments, motor impairments, and cognitive disabilities.

## WCAG 2.1 AA Requirements Checklist

### 1. Perceivable

#### 1.1 Text Alternatives
- [ ] All images have alt text (or empty alt="" for decorative)
- [ ] Complex images have long descriptions
- [ ] Icons have accessible labels
- [ ] Form inputs have visible labels
- [ ] Audio/video has transcripts/captions (if applicable)

#### 1.2 Time-based Media
- [ ] Video has captions
- [ ] Audio has transcript
- [ ] Auto-playing media can be paused

#### 1.3 Adaptable
- [ ] Content structure uses semantic HTML
- [ ] Reading order is logical
- [ ] Instructions don't rely on sensory characteristics
- [ ] Page works in portrait and landscape

#### 1.4 Distinguishable
- [ ] Color contrast minimum 4.5:1 for normal text
- [ ] Color contrast minimum 3:1 for large text
- [ ] Color is not the only way to convey information
- [ ] Text can be resized 200% without loss
- [ ] Images of text are avoided (use real text)
- [ ] Content is readable without horizontal scroll at 320px
- [ ] UI components have 3:1 contrast against background

### 2. Operable

#### 2.1 Keyboard Accessible
- [ ] All functionality available via keyboard
- [ ] No keyboard traps
- [ ] Custom keyboard shortcuts can be turned off
- [ ] Focus order is logical
- [ ] Focus is visible at all times

#### 2.2 Enough Time
- [ ] Timing is adjustable or can be turned off
- [ ] Moving content can be paused
- [ ] No content flashes more than 3 times per second

#### 2.3 Seizures
- [ ] No flashing content above thresholds

#### 2.4 Navigable
- [ ] Skip link to main content
- [ ] Page has descriptive title
- [ ] Focus order is logical
- [ ] Link purpose is clear from text
- [ ] Multiple ways to find pages
- [ ] Headings are descriptive
- [ ] Focus is visible

#### 2.5 Input Modalities
- [ ] Click targets are at least 44x44 pixels
- [ ] Gestures have alternatives
- [ ] Motion-activated features can be disabled

### 3. Understandable

#### 3.1 Readable
- [ ] Language of page is set (`lang` attribute)
- [ ] Unusual words are explained
- [ ] Abbreviations are expanded

#### 3.2 Predictable
- [ ] Navigation is consistent across pages
- [ ] Components work consistently
- [ ] Changes don't happen unexpectedly

#### 3.3 Input Assistance
- [ ] Errors are identified and described
- [ ] Labels and instructions are provided
- [ ] Error suggestions are given
- [ ] Errors can be corrected before submit

### 4. Robust

#### 4.1 Compatible
- [ ] HTML is valid
- [ ] ARIA is used correctly
- [ ] Status messages are announced

## Keyboard Navigation Testing

### Test Each Interactive Element
- [ ] Buttons can be activated with Enter/Space
- [ ] Links can be activated with Enter
- [ ] Checkboxes toggle with Space
- [ ] Radio buttons navigate with arrows
- [ ] Select menus work with arrows + Enter
- [ ] Modals trap focus properly
- [ ] Dropdown menus are navigable
- [ ] Tab panels work correctly
- [ ] Carousels are navigable

### Test Complete Flows
- [ ] Can complete purchase with keyboard only
- [ ] Can navigate all menus with keyboard
- [ ] Can fill all forms with keyboard
- [ ] Can use search with keyboard
- [ ] Can manage account with keyboard

## Screen Reader Testing

### Test with VoiceOver (Mac) or NVDA (Windows)
- [ ] Page structure is announced correctly
- [ ] Headings hierarchy makes sense
- [ ] Links are descriptive
- [ ] Images are described (or skipped if decorative)
- [ ] Form labels are announced
- [ ] Error messages are announced
- [ ] Buttons announce their purpose
- [ ] Dynamic content updates are announced

### Common ARIA Patterns
- [ ] `role="button"` on clickable non-buttons
- [ ] `aria-label` for icon-only buttons
- [ ] `aria-expanded` on expandable elements
- [ ] `aria-hidden="true"` on decorative elements
- [ ] `aria-live` for dynamic content
- [ ] `aria-describedby` for additional descriptions

## Tools & Techniques

1. **Keyboard Testing**: Tab through every page, note issues
2. **Screen Reader**: Test with VoiceOver (Cmd+F5 on Mac)
3. **axe DevTools**: Browser extension for automated checks
4. **Color Contrast**: Use WebAIM contrast checker
5. **Zoom Testing**: Test at 200% zoom
6. **Grayscale Testing**: View site without color

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | Complete barrier, cannot access content | No keyboard access to checkout |
| HIGH | Significant barrier, very difficult | Form has no error descriptions |
| MEDIUM | Barrier exists but workaround possible | Missing alt text on product images |
| LOW | Minor issue, best practice | Could use more descriptive link text |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: A11Y
- **WCAG Criterion**: [e.g., 1.4.3 Contrast]
- **Location**: [Page URL or component]
- **Element**: [CSS selector or description]
- **Description**: [What's wrong]
- **Impact**: [Who is affected and how]
- **How to Test**: [Steps to reproduce]
- **Suggested Fix**: [Code changes needed]
- **Status**: Open
```

## Common Issues to Watch For

1. **Missing alt text** - `<img src="product.jpg">` with no alt
2. **Low contrast** - Light gray text on white background
3. **No focus indicator** - Can't see where keyboard focus is
4. **Keyboard traps** - Can't Tab out of modal
5. **Missing labels** - `<input>` with no `<label>`
6. **Color-only meaning** - Red text for errors with no icon/text
7. **Small touch targets** - 20px buttons on mobile
8. **Missing skip link** - No way to skip navigation
9. **Auto-playing media** - Video starts without user action
10. **Improper heading hierarchy** - H1 followed by H4
11. **Missing lang attribute** - `<html>` has no `lang="en"`
12. **Non-semantic HTML** - `<div onclick>` instead of `<button>`
