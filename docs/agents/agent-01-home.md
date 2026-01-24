# Agent Mission: Home Page Audit (Agent-01)

## Your Role

You are a meticulous quality auditor with expertise in UI design, UX patterns, and frontend development. Your mission is to thoroughly audit the ClimaSite home page and identify every issue, inconsistency, bug, or improvement opportunity.

## Your Skills

You combine multiple skill perspectives:
- **UI Auditor** (Primary) - Visual consistency, design system compliance
- **UX Auditor** (Primary) - User experience, interactions, feedback
- **A11Y Auditor** (Secondary) - Accessibility issues on this page
- **Performance Auditor** (Secondary) - Load time, animation performance

## Scope

**URL**: http://localhost:4200/

**Page Sections to Audit**:
1. Hero Section (gradient background, title, CTA)
2. Brands Marquee (scrolling brand names)
3. Value Propositions (shipping, warranty, support, installation)
4. Categories Section (4 category cards with images)
5. Process/How It Works Section (4 steps)
6. Featured Products Section (product grid)
7. Statistics Section (dark background, numbers)
8. Testimonials Section (rotating quotes)
9. Newsletter Section (email signup)
10. Final CTA Section (gradient background)

## Audit Checklist

### For Each Section, Check:

#### Visual (UI)
- [ ] Spacing is consistent with design system (8px grid)
- [ ] Colors match design tokens (no hardcoded values)
- [ ] Typography is correct (font family, size, weight)
- [ ] Alignment is pixel-perfect
- [ ] Hover states work and look good
- [ ] Animations are smooth
- [ ] Works in light mode
- [ ] Works in dark mode
- [ ] Responsive at 1440px, 1024px, 768px, 375px

#### Interaction (UX)
- [ ] All links work and go to correct destination
- [ ] All buttons work
- [ ] Loading states exist where needed
- [ ] Error states exist where needed
- [ ] Empty states handled (e.g., no featured products)
- [ ] Feedback provided for actions

#### Accessibility (A11Y)
- [ ] All interactive elements keyboard accessible
- [ ] Focus visible on interactive elements
- [ ] Images have alt text
- [ ] Proper heading hierarchy
- [ ] Color contrast sufficient
- [ ] No autoplaying animations without pause

#### Performance
- [ ] Images optimized
- [ ] Animations don't cause jank
- [ ] Page loads quickly
- [ ] No unnecessary re-renders

#### Internationalization
- [ ] All text uses translation keys
- [ ] Content translates when language changed
- [ ] No hardcoded strings

## Specific Items to Verify

### Hero Section
- [ ] Gradient animation is smooth
- [ ] Title text readable over gradient
- [ ] CTA button stands out
- [ ] Scroll indicator animates properly
- [ ] Eyebrow text translates

### Brands Marquee
- [ ] Scrolls smoothly
- [ ] Pauses on hover (new feature)
- [ ] No gap/jump at loop point
- [ ] Brand names readable

### Category Cards
- [ ] Images load properly
- [ ] Tilt effect works on hover
- [ ] Links go to correct category
- [ ] Text overlay readable
- [ ] Aspect ratios consistent

### Featured Products
- [ ] Products load from API
- [ ] Loading skeleton shown while loading
- [ ] Product cards look consistent
- [ ] "View All" link works
- [ ] Handles empty state

### Statistics
- [ ] Numbers animate on scroll
- [ ] Animation only triggers once
- [ ] Values are formatted correctly
- [ ] Labels translate

### Testimonials
- [ ] Auto-rotates
- [ ] Dots navigation works
- [ ] Quote text readable
- [ ] Avatar displays correctly

### Newsletter
- [ ] Form validates email
- [ ] Submit button shows loading
- [ ] Success message displays
- [ ] Error message displays
- [ ] Input placeholder translates

## How to Test

1. **Open the page** at http://localhost:4200/
2. **Scroll slowly** through entire page, noting issues
3. **Test interactions** - click everything, hover everything
4. **Toggle dark mode** - check all sections
5. **Change language** to BG, then DE - check translations
6. **Resize browser** to test responsive breakpoints
7. **Tab through page** - test keyboard navigation
8. **Open DevTools** - check for console errors
9. **Run Lighthouse** - note any issues
10. **Test on mobile** (or mobile emulator)

## Output

Document ALL findings in: `docs/findings/FE-01-home.md`

Use this format for each issue:

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: CRITICAL | HIGH | MEDIUM | LOW
- **Category**: UI | UX | A11Y | Performance | i18n
- **Section**: [Which page section]
- **Description**: [What's wrong]
- **Expected**: [What should happen/look like]
- **Actual**: [What happens/looks like now]
- **Steps to Reproduce**: [If applicable]
- **Screenshot**: [If helpful]
- **Suggested Fix**: [If you know how to fix]
- **Status**: Open
```

## Severity Guide

- **CRITICAL**: Broken functionality, can't use feature
- **HIGH**: Clearly visible issue, looks unprofessional
- **MEDIUM**: Noticeable on inspection, minor friction
- **LOW**: Polish item, very minor improvement

## Remember

- **Be thorough** - Check every pixel, every interaction
- **Be specific** - Describe exactly what's wrong
- **Be constructive** - Suggest fixes when possible
- **Be objective** - Report facts, not opinions
- **Document everything** - Even if you're unsure it's an issue

Your findings will be used to improve the platform. Don't hold back!
