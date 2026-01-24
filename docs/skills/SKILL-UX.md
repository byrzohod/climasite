# Skill: UX Auditor (SKILL-UX)

## Purpose

The UX Auditor identifies usability problems, confusing interactions, missing feedback, poor affordances, and anything that makes the user experience frustrating or unclear.

## What You Look For

### 1. User Feedback
- [ ] Loading states shown for all async operations
- [ ] Success messages after actions (add to cart, save, etc.)
- [ ] Error messages are clear and actionable
- [ ] Progress indication for multi-step processes
- [ ] Confirmation dialogs for destructive actions
- [ ] Visual feedback on button clicks
- [ ] Hover states indicate interactivity

### 2. Navigation & Wayfinding
- [ ] User always knows where they are (breadcrumbs, active nav)
- [ ] Clear way to go back/home
- [ ] Consistent navigation patterns
- [ ] Links are distinguishable from plain text
- [ ] Mobile menu is intuitive
- [ ] Search is easily accessible
- [ ] Cart/account always reachable

### 3. Forms & Input
- [ ] Labels are clear and helpful
- [ ] Required fields are marked
- [ ] Validation happens at right time (on blur, on submit)
- [ ] Error messages explain how to fix
- [ ] Form doesn't lose data on error
- [ ] Autocomplete works where expected
- [ ] Tab order is logical
- [ ] Enter key submits forms

### 4. Content Clarity
- [ ] Headlines are descriptive
- [ ] Button labels are action-oriented ("Add to Cart" not "Submit")
- [ ] No jargon or unclear terminology
- [ ] Empty states explain what to do
- [ ] Help text where needed
- [ ] Prices and totals are clear

### 5. Affordances & Discoverability
- [ ] Clickable things look clickable
- [ ] Non-clickable things don't look clickable
- [ ] Drag/swipe actions are discoverable
- [ ] Hidden menus have clear triggers
- [ ] Search filters are easy to find
- [ ] Sort options are intuitive

### 6. Flow & Task Completion
- [ ] Tasks can be completed in reasonable steps
- [ ] No dead ends
- [ ] Progress can be saved
- [ ] Easy to recover from mistakes
- [ ] Confirmation before irreversible actions
- [ ] Success state is clear

### 7. Error Handling
- [ ] Errors don't crash the page
- [ ] Error messages are human-readable
- [ ] User can recover from errors
- [ ] Network errors handled gracefully
- [ ] Session timeout handled well
- [ ] 404 pages are helpful

### 8. Empty & Edge States
- [ ] Empty cart has helpful message
- [ ] No results search has suggestions
- [ ] Empty order history encourages action
- [ ] Zero quantity handling
- [ ] Out of stock behavior
- [ ] Max quantity limits

### 9. Performance Perception
- [ ] Page feels fast
- [ ] No perceived lag on interactions
- [ ] Skeleton loaders while loading
- [ ] Optimistic UI updates where appropriate
- [ ] No jarring content shifts

### 10. Mobile Experience
- [ ] Touch targets are large enough (44x44 min)
- [ ] No accidental taps
- [ ] Swipe gestures work
- [ ] Keyboard doesn't cover inputs
- [ ] Forms are easy to fill on mobile

## Common User Flows to Test

### Flow A: Browse and Purchase
1. Land on home page
2. Find a product (via category or search)
3. View product details
4. Add to cart
5. View cart
6. Proceed to checkout
7. Complete purchase
8. View order confirmation

### Flow B: Account Management
1. Register new account
2. Verify email (if applicable)
3. Log in
4. Update profile
5. Add shipping address
6. View order history
7. Log out

### Flow C: Product Discovery
1. Use search
2. Apply filters
3. Sort results
4. View product
5. Check reviews
6. Add to wishlist
7. Compare products (if feature exists)

## Tools & Techniques

1. **Task Analysis**: Try to complete common tasks, note friction
2. **Think Aloud**: Verbalize thoughts while using ("I'm not sure what this does...")
3. **Error Injection**: Intentionally make mistakes, see what happens
4. **Edge Cases**: Try unusual inputs, empty states, max values
5. **Speed Testing**: Use the site quickly, see what breaks

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | User cannot complete essential task | Checkout button doesn't work |
| HIGH | Task is difficult/confusing | No error message on form failure |
| MEDIUM | Task has friction but is possible | Unclear button label |
| LOW | Minor inconvenience | Could use better feedback |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: UX
- **Location**: [Page URL]
- **User Task**: [What user is trying to do]
- **Description**: [What's wrong/confusing]
- **Expected Behavior**: [What should happen]
- **Actual Behavior**: [What happens now]
- **User Impact**: [How this affects the user]
- **Steps to Reproduce**: [If applicable]
- **Suggested Fix**: [How to improve]
- **Status**: Open
```

## Common Issues to Watch For

1. **Missing loading states** - Button clicked, nothing happens for 2 seconds
2. **Silent failures** - Action fails with no error message
3. **Unclear CTAs** - "Submit" instead of "Place Order"
4. **No confirmation** - Item added to cart with no feedback
5. **Dead ends** - Empty state with no action to take
6. **Lost form data** - Error clears all input fields
7. **Hidden information** - Price only shown at checkout
8. **Confusing navigation** - User can't find their orders
9. **Unexpected behavior** - Clicking logo doesn't go home
10. **Mobile issues** - Can't scroll, can't close modal
11. **Broken flows** - Next step not obvious
12. **No undo** - Deleted item can't be recovered
