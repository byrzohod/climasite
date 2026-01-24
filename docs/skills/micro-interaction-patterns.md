# Micro-Interaction Patterns for E-Commerce

> A comprehensive guide to button, form, card, toggle, cart, navigation, and notification micro-interactions with implementation details, timing specifications, and accessibility considerations.

## Table of Contents

1. [Button Interactions](#1-button-interactions)
2. [Form Interactions](#2-form-interactions)
3. [Card Interactions](#3-card-interactions)
4. [Toggle & Switch Interactions](#4-toggle--switch-interactions)
5. [Cart & Purchase Interactions](#5-cart--purchase-interactions)
6. [Navigation Interactions](#6-navigation-interactions)
7. [Notification Interactions](#7-notification-interactions)
8. [Timing Reference Guide](#8-timing-reference-guide)
9. [Accessibility Guidelines](#9-accessibility-guidelines)

---

## 1. Button Interactions

### 1.1 Hover States

#### Lift Effect

**Animation Properties:**
- `transform: translateY(-2px)` - Vertical lift
- `box-shadow` - Enhanced shadow depth
- `background-color` - Optional color shift

**Timing:**
- Duration: `150ms`
- Easing: `ease-out`
- Delay: `0ms`

**Implementation:**

```scss
.btn {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  transition: 
    transform 150ms ease-out,
    box-shadow 150ms ease-out,
    background-color 150ms ease-out;
  
  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
  }
}
```

**Reduced Motion Fallback:**

```scss
@media (prefers-reduced-motion: reduce) {
  .btn {
    transition: none;
    
    &:hover {
      transform: none;
      // Keep color change only
      opacity: 0.9;
    }
  }
}
```

---

#### Glow Effect

**Animation Properties:**
- `box-shadow` with spread - Outer glow
- `background-color` - Slight brightness increase

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`
- Delay: `0ms`

**Implementation:**

```scss
.btn-glow {
  position: relative;
  transition: box-shadow 200ms ease-out;
  
  &:hover {
    box-shadow: 
      0 0 20px rgba(var(--color-primary-rgb), 0.4),
      0 0 40px rgba(var(--color-primary-rgb), 0.2);
  }
}

// Alternative: Pseudo-element glow (better performance)
.btn-glow-pseudo {
  position: relative;
  
  &::before {
    content: '';
    position: absolute;
    inset: -4px;
    border-radius: inherit;
    background: var(--color-primary);
    opacity: 0;
    filter: blur(8px);
    transition: opacity 200ms ease-out;
    z-index: -1;
  }
  
  &:hover::before {
    opacity: 0.4;
  }
}
```

---

#### Color Shift Effect

**Animation Properties:**
- `background-color` - Primary to darker/lighter variant
- `border-color` - If bordered button

**Timing:**
- Duration: `150ms`
- Easing: `ease-out`
- Delay: `0ms`

**Implementation:**

```scss
.btn-primary {
  background-color: var(--color-primary);
  transition: background-color 150ms ease-out;
  
  &:hover {
    background-color: var(--color-primary-dark);
  }
}

// Outline button variant
.btn-outline {
  background-color: transparent;
  border: 2px solid var(--color-primary);
  color: var(--color-primary);
  transition: 
    background-color 150ms ease-out,
    color 150ms ease-out;
  
  &:hover {
    background-color: var(--color-primary);
    color: var(--color-text-inverse);
  }
}
```

---

### 1.2 Press/Active States

#### Scale Down Effect

**Animation Properties:**
- `transform: scale(0.96-0.98)` - Slight shrink
- `box-shadow` - Reduced shadow

**Timing:**
- Duration: `100ms`
- Easing: `ease-in`
- Delay: `0ms`

**Implementation:**

```scss
.btn {
  transition: 
    transform 150ms ease-out,
    box-shadow 150ms ease-out;
  
  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
  }
  
  &:active {
    transform: scale(0.97) translateY(0);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition-duration: 100ms;
  }
}
```

---

#### Darken Effect

**Animation Properties:**
- `background-color` - Darker shade
- `filter: brightness()` - Alternative approach

**Timing:**
- Duration: `50ms`
- Easing: `ease-in`
- Delay: `0ms`

**Implementation:**

```scss
.btn-primary {
  background-color: var(--color-primary);
  
  &:active {
    background-color: var(--color-primary-darker);
    // OR use filter for any color
    filter: brightness(0.9);
    transition-duration: 50ms;
  }
}
```

---

### 1.3 Loading States

#### Spinner Loading

**Animation Properties:**
- Button text: `opacity: 0` - Hide text
- Spinner: `rotate` animation - Continuous spin
- Button: `pointer-events: none` - Prevent interaction

**Timing:**
- Spinner rotation: `800ms` (infinite)
- Text fade: `150ms`
- Easing: `linear` (rotation), `ease-out` (fade)

**Implementation:**

```scss
.btn {
  position: relative;
  
  .btn-text {
    transition: opacity 150ms ease-out;
  }
  
  .btn-spinner {
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    opacity: 0;
    transition: opacity 150ms ease-out;
    
    // Spinner animation
    width: 20px;
    height: 20px;
    border: 2px solid transparent;
    border-top-color: currentColor;
    border-radius: 50%;
    animation: spin 800ms linear infinite;
    animation-play-state: paused;
  }
  
  &.loading {
    pointer-events: none;
    
    .btn-text {
      opacity: 0;
    }
    
    .btn-spinner {
      opacity: 1;
      animation-play-state: running;
    }
  }
}

@keyframes spin {
  to { transform: translate(-50%, -50%) rotate(360deg); }
}
```

**HTML Structure:**

```html
<button class="btn btn-primary" data-testid="submit-btn">
  <span class="btn-text">Add to Cart</span>
  <span class="btn-spinner" aria-hidden="true"></span>
</button>
```

**JavaScript:**

```typescript
function setButtonLoading(button: HTMLButtonElement, loading: boolean): void {
  button.classList.toggle('loading', loading);
  button.setAttribute('aria-busy', String(loading));
  button.disabled = loading;
}
```

---

#### Progress Loading

**Animation Properties:**
- Progress bar: `width` from 0% to 100%
- Optional pulse effect

**Timing:**
- Progress update: `200ms` per step
- Easing: `ease-out`

**Implementation:**

```scss
.btn-progress {
  position: relative;
  overflow: hidden;
  
  &::before {
    content: '';
    position: absolute;
    left: 0;
    bottom: 0;
    width: 0;
    height: 3px;
    background: var(--color-success);
    transition: width 200ms ease-out;
  }
  
  &[data-progress="25"]::before { width: 25%; }
  &[data-progress="50"]::before { width: 50%; }
  &[data-progress="75"]::before { width: 75%; }
  &[data-progress="100"]::before { width: 100%; }
}
```

---

### 1.4 Success States

#### Checkmark Reveal

**Animation Properties:**
- Button text: Cross-fade to checkmark
- Background: Color change to success
- Checkmark: Scale/draw animation

**Timing:**
- Color transition: `200ms`
- Checkmark reveal: `300ms`
- Easing: `ease-out`, `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.btn-success-state {
  position: relative;
  
  .btn-text,
  .btn-success {
    transition: opacity 200ms ease-out, transform 200ms ease-out;
  }
  
  .btn-success {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transform: scale(0.8);
  }
  
  &.success {
    background-color: var(--color-success);
    pointer-events: none;
    
    .btn-text {
      opacity: 0;
      transform: scale(0.8);
    }
    
    .btn-success {
      opacity: 1;
      transform: scale(1);
    }
    
    .checkmark {
      animation: checkmark-draw 300ms ease-out forwards;
    }
  }
}

@keyframes checkmark-draw {
  0% {
    stroke-dashoffset: 24;
    opacity: 0;
  }
  50% {
    opacity: 1;
  }
  100% {
    stroke-dashoffset: 0;
    opacity: 1;
  }
}
```

**SVG Checkmark:**

```html
<svg class="checkmark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
  <polyline points="4 12 9 17 20 6" stroke-dasharray="24" stroke-dashoffset="24"/>
</svg>
```

---

#### Color Flash

**Animation Properties:**
- `background-color` - Flash to success then settle
- Optional: Scale pulse

**Timing:**
- Flash: `150ms`
- Hold: `300ms`
- Return: `300ms`

**Implementation:**

```scss
.btn {
  &.flash-success {
    animation: success-flash 600ms ease-out;
  }
}

@keyframes success-flash {
  0% {
    background-color: var(--color-primary);
    transform: scale(1);
  }
  25% {
    background-color: var(--color-success);
    transform: scale(1.02);
  }
  50% {
    background-color: var(--color-success);
    transform: scale(1);
  }
  100% {
    background-color: var(--color-primary);
  }
}
```

---

### 1.5 Disabled States

**Animation Properties:**
- `opacity: 0.5-0.6` - Visual dimming
- `cursor: not-allowed`
- No hover/active transitions

**Timing:**
- Transition to disabled: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.btn {
  transition: 
    opacity 200ms ease-out,
    background-color 150ms ease-out,
    transform 150ms ease-out;
  
  &:disabled,
  &[aria-disabled="true"] {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
    
    // Remove all hover/active effects
    &:hover,
    &:active {
      transform: none;
      box-shadow: none;
    }
  }
}
```

---

## 2. Form Interactions

### 2.1 Input Focus Animations

#### Border Highlight

**Animation Properties:**
- `border-color` - Neutral to primary
- `box-shadow` - Focus ring
- `outline` - For accessibility

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`
- Delay: `0ms`

**Implementation:**

```scss
.form-input {
  border: 2px solid var(--color-border);
  border-radius: 8px;
  padding: 12px 16px;
  transition: 
    border-color 200ms ease-out,
    box-shadow 200ms ease-out;
  
  &:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.15);
  }
  
  &:focus-visible {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
  }
}
```

---

#### Background Shift

**Animation Properties:**
- `background-color` - Subtle lightening/darkening

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.form-input {
  background-color: var(--color-input-bg);
  transition: background-color 200ms ease-out;
  
  &:focus {
    background-color: var(--color-input-bg-focus);
  }
}
```

---

### 2.2 Floating Label Patterns

**Animation Properties:**
- Label `transform: translateY()` - Vertical movement
- Label `font-size` - Scale down
- Label `color` - Highlight on focus

**Timing:**
- Duration: `200ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.form-field {
  position: relative;
  
  .form-input {
    padding: 20px 16px 8px;
    
    &::placeholder {
      color: transparent;
    }
  }
  
  .form-label {
    position: absolute;
    left: 16px;
    top: 50%;
    transform: translateY(-50%);
    color: var(--color-text-muted);
    font-size: 1rem;
    pointer-events: none;
    transition: 
      transform 200ms cubic-bezier(0.4, 0, 0.2, 1),
      font-size 200ms cubic-bezier(0.4, 0, 0.2, 1),
      color 200ms ease-out;
    transform-origin: left center;
  }
  
  // Float label when focused or has value
  .form-input:focus ~ .form-label,
  .form-input:not(:placeholder-shown) ~ .form-label {
    transform: translateY(-130%);
    font-size: 0.75rem;
  }
  
  .form-input:focus ~ .form-label {
    color: var(--color-primary);
  }
}
```

**HTML Structure:**

```html
<div class="form-field">
  <input 
    type="email" 
    id="email" 
    class="form-input" 
    placeholder=" "
    required
  />
  <label for="email" class="form-label">Email Address</label>
</div>
```

---

### 2.3 Validation Feedback

#### Shake Animation (Error)

**Animation Properties:**
- `transform: translateX()` - Horizontal shake
- `border-color` - Error color
- Icon appearance

**Timing:**
- Shake duration: `400ms`
- Shake count: 3-4 oscillations
- Easing: `ease-out`

**Implementation:**

```scss
.form-input {
  &.error,
  &:invalid:not(:placeholder-shown) {
    border-color: var(--color-error);
    animation: shake 400ms ease-out;
  }
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  10%, 50%, 90% { transform: translateX(-4px); }
  30%, 70% { transform: translateX(4px); }
}

// Error message entrance
.error-message {
  opacity: 0;
  transform: translateY(-8px);
  transition: 
    opacity 200ms ease-out,
    transform 200ms ease-out;
  
  &.visible {
    opacity: 1;
    transform: translateY(0);
  }
}
```

---

#### Border Color Transition

**Animation Properties:**
- `border-color` - Neutral to error/success
- Icon fade-in

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.form-input {
  transition: border-color 200ms ease-out;
  
  &.valid {
    border-color: var(--color-success);
  }
  
  &.error {
    border-color: var(--color-error);
  }
}

.validation-icon {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  opacity: 0;
  transition: opacity 200ms ease-out;
  
  &.visible {
    opacity: 1;
  }
}
```

---

### 2.4 Success Indicators

**Animation Properties:**
- Checkmark icon: Scale + fade in
- Border/background: Success color
- Optional: Input glow

**Timing:**
- Duration: `250ms`
- Easing: `cubic-bezier(0.175, 0.885, 0.32, 1.275)` (bouncy)

**Implementation:**

```scss
.form-field {
  .success-icon {
    position: absolute;
    right: 12px;
    top: 50%;
    transform: translateY(-50%) scale(0);
    opacity: 0;
    color: var(--color-success);
    transition: 
      opacity 250ms ease-out,
      transform 250ms cubic-bezier(0.175, 0.885, 0.32, 1.275);
  }
  
  &.valid .success-icon {
    opacity: 1;
    transform: translateY(-50%) scale(1);
  }
}
```

---

### 2.5 Error Recovery

**Animation Properties:**
- Error state removal: Smooth fade
- Border return to neutral
- Error message exit

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.form-input {
  transition: border-color 200ms ease-out;
  
  // Error to valid transition
  &.error {
    border-color: var(--color-error);
  }
  
  &.recovering {
    border-color: var(--color-border);
  }
}

.error-message {
  transition: 
    opacity 200ms ease-out,
    transform 200ms ease-out,
    max-height 200ms ease-out;
  max-height: 100px;
  
  &.hiding {
    opacity: 0;
    transform: translateY(-8px);
    max-height: 0;
  }
}
```

---

## 3. Card Interactions

### 3.1 Hover Lift Effect

**Animation Properties:**
- `transform: translateY(-8px)` - Vertical lift
- `box-shadow` - Enhanced depth

**Timing:**
- Duration: `250ms`
- Easing: `ease-out`
- Delay: `0ms`

**Implementation:**

```scss
.product-card {
  transform: translateY(0);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: 
    transform 250ms ease-out,
    box-shadow 250ms ease-out;
  
  &:hover {
    transform: translateY(-8px);
    box-shadow: 
      0 12px 24px rgba(0, 0, 0, 0.1),
      0 4px 8px rgba(0, 0, 0, 0.05);
  }
}

@media (prefers-reduced-motion: reduce) {
  .product-card {
    &:hover {
      transform: none;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
    }
  }
}
```

---

### 3.2 Content Reveal on Hover

**Animation Properties:**
- Hidden content: `opacity` + `transform`
- Stagger delay for multiple elements

**Timing:**
- Duration: `200ms`
- Stagger: `50ms` between elements
- Easing: `ease-out`

**Implementation:**

```scss
.product-card {
  .quick-actions {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    padding: 16px;
    opacity: 0;
    transform: translateY(16px);
    transition: 
      opacity 200ms ease-out,
      transform 200ms ease-out;
  }
  
  .quick-action-btn {
    opacity: 0;
    transform: translateY(8px);
    transition: 
      opacity 200ms ease-out,
      transform 200ms ease-out;
    
    &:nth-child(1) { transition-delay: 0ms; }
    &:nth-child(2) { transition-delay: 50ms; }
    &:nth-child(3) { transition-delay: 100ms; }
  }
  
  &:hover,
  &:focus-within {
    .quick-actions {
      opacity: 1;
      transform: translateY(0);
    }
    
    .quick-action-btn {
      opacity: 1;
      transform: translateY(0);
    }
  }
}
```

---

### 3.3 Selection States

**Animation Properties:**
- Border/outline: Color change
- Background: Subtle tint
- Checkmark: Scale reveal

**Timing:**
- Duration: `200ms`
- Checkmark pop: `250ms` with overshoot easing

**Implementation:**

```scss
.selectable-card {
  border: 2px solid transparent;
  transition: 
    border-color 200ms ease-out,
    background-color 200ms ease-out;
  
  .selection-indicator {
    position: absolute;
    top: 12px;
    right: 12px;
    width: 24px;
    height: 24px;
    border: 2px solid var(--color-border);
    border-radius: 50%;
    transform: scale(1);
    transition: 
      transform 250ms cubic-bezier(0.175, 0.885, 0.32, 1.275),
      background-color 200ms ease-out,
      border-color 200ms ease-out;
    
    &::after {
      content: '\\2713';
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 14px;
      opacity: 0;
      transform: scale(0);
      transition: 
        opacity 200ms ease-out,
        transform 200ms ease-out;
    }
  }
  
  &.selected {
    border-color: var(--color-primary);
    background-color: rgba(var(--color-primary-rgb), 0.05);
    
    .selection-indicator {
      background-color: var(--color-primary);
      border-color: var(--color-primary);
      transform: scale(1.1);
      
      &::after {
        opacity: 1;
        transform: scale(1);
      }
    }
  }
}
```

---

### 3.4 Drag and Drop Feedback

**Animation Properties:**
- Dragging element: Scale, shadow, opacity
- Drop zone: Border pulse, background highlight
- Placeholder: Dashed border

**Timing:**
- Drag start: `200ms`
- Drop zone feedback: `150ms`
- Reorder animation: `300ms`

**Implementation:**

```scss
// Dragging element
.card {
  transition: 
    transform 200ms ease-out,
    box-shadow 200ms ease-out,
    opacity 200ms ease-out;
  
  &.dragging {
    transform: scale(1.02) rotate(2deg);
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
    opacity: 0.9;
    z-index: 1000;
    cursor: grabbing;
  }
}

// Drop zone
.drop-zone {
  transition: 
    border-color 150ms ease-out,
    background-color 150ms ease-out;
  
  &.drag-over {
    border: 2px dashed var(--color-primary);
    background-color: rgba(var(--color-primary-rgb), 0.05);
  }
}

// Placeholder for reordering
.card-placeholder {
  border: 2px dashed var(--color-border);
  background-color: var(--color-surface-subtle);
  animation: placeholder-pulse 1s ease-in-out infinite;
}

@keyframes placeholder-pulse {
  0%, 100% { opacity: 0.5; }
  50% { opacity: 0.8; }
}

// Reorder animation
.card-list {
  .card {
    transition: transform 300ms ease-out;
  }
}
```

**JavaScript for Drag Feedback:**

```typescript
function handleDragStart(e: DragEvent, element: HTMLElement): void {
  element.classList.add('dragging');
  // Use requestAnimationFrame for smooth animation start
  requestAnimationFrame(() => {
    element.style.cursor = 'grabbing';
  });
}

function handleDragEnd(e: DragEvent, element: HTMLElement): void {
  element.classList.remove('dragging');
}

function handleDragOver(e: DragEvent, dropZone: HTMLElement): void {
  e.preventDefault();
  dropZone.classList.add('drag-over');
}

function handleDragLeave(e: DragEvent, dropZone: HTMLElement): void {
  dropZone.classList.remove('drag-over');
}
```

---

## 4. Toggle & Switch Interactions

### 4.1 Checkbox Animations

**Animation Properties:**
- Check mark: Draw/scale animation
- Background: Color fill
- Border: Color transition

**Timing:**
- Check draw: `200ms`
- Background: `150ms`
- Easing: `ease-out`, `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.checkbox-wrapper {
  display: flex;
  align-items: center;
  gap: 12px;
  
  input[type="checkbox"] {
    appearance: none;
    width: 20px;
    height: 20px;
    border: 2px solid var(--color-border);
    border-radius: 4px;
    background-color: transparent;
    cursor: pointer;
    position: relative;
    transition: 
      border-color 150ms ease-out,
      background-color 150ms ease-out;
    
    &::after {
      content: '';
      position: absolute;
      left: 6px;
      top: 2px;
      width: 5px;
      height: 10px;
      border: 2px solid white;
      border-top: 0;
      border-left: 0;
      transform: rotate(45deg) scale(0);
      opacity: 0;
      transition: 
        transform 200ms cubic-bezier(0.4, 0, 0.2, 1),
        opacity 150ms ease-out;
    }
    
    &:checked {
      background-color: var(--color-primary);
      border-color: var(--color-primary);
      
      &::after {
        transform: rotate(45deg) scale(1);
        opacity: 1;
      }
    }
    
    &:focus-visible {
      outline: 2px solid var(--color-primary);
      outline-offset: 2px;
    }
  }
}
```

---

### 4.2 Radio Button Feedback

**Animation Properties:**
- Inner dot: Scale from center
- Outer ring: Border color
- Optional: Ripple effect

**Timing:**
- Duration: `200ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.radio-wrapper {
  input[type="radio"] {
    appearance: none;
    width: 20px;
    height: 20px;
    border: 2px solid var(--color-border);
    border-radius: 50%;
    position: relative;
    cursor: pointer;
    transition: border-color 200ms ease-out;
    
    &::after {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      width: 10px;
      height: 10px;
      background-color: var(--color-primary);
      border-radius: 50%;
      transform: translate(-50%, -50%) scale(0);
      transition: transform 200ms cubic-bezier(0.4, 0, 0.2, 1);
    }
    
    &:checked {
      border-color: var(--color-primary);
      
      &::after {
        transform: translate(-50%, -50%) scale(1);
      }
    }
  }
}
```

---

### 4.3 Toggle Switches

**Animation Properties:**
- Thumb: `transform: translateX()` slide
- Track: `background-color` change
- Optional: Thumb scale on drag

**Timing:**
- Slide duration: `200ms`
- Background: `150ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.toggle-switch {
  width: 48px;
  height: 26px;
  background-color: var(--color-surface-muted);
  border-radius: 13px;
  padding: 2px;
  cursor: pointer;
  transition: background-color 150ms ease-out;
  
  &:focus-within {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
  }
  
  input {
    position: absolute;
    opacity: 0;
    width: 0;
    height: 0;
  }
  
  .toggle-thumb {
    width: 22px;
    height: 22px;
    background-color: white;
    border-radius: 50%;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    transform: translateX(0);
    transition: transform 200ms cubic-bezier(0.4, 0, 0.2, 1);
  }
  
  input:checked ~ .toggle-thumb {
    transform: translateX(22px);
  }
  
  input:checked + & {
    background-color: var(--color-primary);
  }
  
  // Alternative: Use :has() for modern browsers
  &:has(input:checked) {
    background-color: var(--color-primary);
    
    .toggle-thumb {
      transform: translateX(22px);
    }
  }
}
```

---

### 4.4 Filter Chip Selection

**Animation Properties:**
- Background: Color fill
- Border: Color change
- Check icon: Scale reveal
- Optional: Scale pulse

**Timing:**
- Duration: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.filter-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border: 1px solid var(--color-border);
  border-radius: 20px;
  background-color: transparent;
  cursor: pointer;
  transition: 
    background-color 200ms ease-out,
    border-color 200ms ease-out,
    transform 150ms ease-out;
  
  .chip-icon {
    width: 16px;
    height: 16px;
    opacity: 0;
    transform: scale(0);
    transition: 
      opacity 200ms ease-out,
      transform 200ms cubic-bezier(0.175, 0.885, 0.32, 1.275);
  }
  
  &:hover {
    border-color: var(--color-primary);
  }
  
  &:active {
    transform: scale(0.97);
  }
  
  &.selected {
    background-color: var(--color-primary);
    border-color: var(--color-primary);
    color: white;
    
    .chip-icon {
      opacity: 1;
      transform: scale(1);
    }
  }
}
```

---

## 5. Cart & Purchase Interactions

### 5.1 Add to Cart Animation

#### Item Flight Effect

**Animation Properties:**
- Clone element: Position, size, opacity
- Flight path: Bezier curve to cart icon
- Cart icon: Pulse/bounce

**Timing:**
- Flight duration: `500-600ms`
- Cart pulse: `300ms`
- Easing: `cubic-bezier(0.2, 1, 0.3, 1)`

**Implementation:**

```typescript
async function animateAddToCart(
  sourceEl: HTMLElement, 
  cartIconEl: HTMLElement
): Promise<void> {
  // Check for reduced motion preference
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    pulseCartIcon(cartIconEl);
    return;
  }

  const clone = sourceEl.cloneNode(true) as HTMLElement;
  const sourceRect = sourceEl.getBoundingClientRect();
  const cartRect = cartIconEl.getBoundingClientRect();
  
  // Style the clone
  Object.assign(clone.style, {
    position: 'fixed',
    top: `${sourceRect.top}px`,
    left: `${sourceRect.left}px`,
    width: `${sourceRect.width}px`,
    height: `${sourceRect.height}px`,
    margin: '0',
    zIndex: '10000',
    pointerEvents: 'none',
    transition: 'none'
  });
  
  document.body.appendChild(clone);
  
  // Force reflow
  clone.offsetHeight;
  
  // Animate using Web Animations API
  const animation = clone.animate([
    {
      top: `${sourceRect.top}px`,
      left: `${sourceRect.left}px`,
      width: `${sourceRect.width}px`,
      height: `${sourceRect.height}px`,
      opacity: 1,
      borderRadius: '8px'
    },
    {
      top: `${cartRect.top + cartRect.height / 2}px`,
      left: `${cartRect.left + cartRect.width / 2}px`,
      width: '20px',
      height: '20px',
      opacity: 0.5,
      borderRadius: '50%'
    }
  ], {
    duration: 500,
    easing: 'cubic-bezier(0.2, 1, 0.3, 1)'
  });
  
  animation.onfinish = () => {
    clone.remove();
    pulseCartIcon(cartIconEl);
  };
}

function pulseCartIcon(cartIconEl: HTMLElement): void {
  cartIconEl.animate([
    { transform: 'scale(1)' },
    { transform: 'scale(1.3)' },
    { transform: 'scale(1)' }
  ], {
    duration: 300,
    easing: 'ease-out'
  });
}
```

---

#### Badge Bounce

**Animation Properties:**
- Badge: Scale bounce
- Number: Fade/slide transition

**Timing:**
- Bounce duration: `400ms`
- Easing: `cubic-bezier(0.175, 0.885, 0.32, 1.275)`

**Implementation:**

```scss
.cart-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 20px;
  height: 20px;
  padding: 0 6px;
  border-radius: 10px;
  background-color: var(--color-primary);
  color: white;
  font-size: 12px;
  font-weight: 600;
  
  &.updated {
    animation: badge-bounce 400ms cubic-bezier(0.175, 0.885, 0.32, 1.275);
  }
}

@keyframes badge-bounce {
  0% { transform: scale(1); }
  30% { transform: scale(1.4); }
  50% { transform: scale(0.9); }
  70% { transform: scale(1.15); }
  100% { transform: scale(1); }
}
```

**JavaScript for badge update:**

```typescript
function updateCartBadge(badgeEl: HTMLElement, count: number): void {
  badgeEl.textContent = count.toString();
  badgeEl.classList.remove('updated');
  
  // Force reflow to restart animation
  void badgeEl.offsetHeight;
  
  badgeEl.classList.add('updated');
  
  // Announce to screen readers
  announceToScreenReader(`Cart updated: ${count} items`);
}
```

---

### 5.2 Quantity Increment/Decrement

**Animation Properties:**
- Number: Slide up/down with fade
- Button: Subtle press feedback
- Optional: Input highlight

**Timing:**
- Number transition: `200ms`
- Button feedback: `100ms`
- Easing: `ease-out`

**Implementation:**

```scss
.quantity-control {
  display: flex;
  align-items: center;
  gap: 8px;
  
  .quantity-btn {
    width: 32px;
    height: 32px;
    border: 1px solid var(--color-border);
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: 
      background-color 100ms ease-out,
      transform 100ms ease-out;
    
    &:hover {
      background-color: var(--color-surface-hover);
    }
    
    &:active {
      transform: scale(0.95);
    }
    
    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
  
  .quantity-display {
    position: relative;
    width: 40px;
    height: 32px;
    overflow: hidden;
    
    .quantity-value {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      transition: 
        transform 200ms ease-out,
        opacity 200ms ease-out;
      
      &.slide-up {
        transform: translateY(-100%);
        opacity: 0;
      }
      
      &.slide-down {
        transform: translateY(100%);
        opacity: 0;
      }
    }
  }
}
```

**JavaScript:**

```typescript
function updateQuantity(
  displayEl: HTMLElement, 
  newValue: number, 
  direction: 'up' | 'down'
): void {
  const currentValue = displayEl.querySelector('.quantity-value') as HTMLElement;
  const slideClass = direction === 'up' ? 'slide-up' : 'slide-down';
  
  // Create new value element
  const newValueEl = document.createElement('span');
  newValueEl.className = `quantity-value ${direction === 'up' ? 'slide-down' : 'slide-up'}`;
  newValueEl.textContent = newValue.toString();
  displayEl.appendChild(newValueEl);
  
  // Trigger animation
  requestAnimationFrame(() => {
    currentValue.classList.add(slideClass);
    newValueEl.classList.remove('slide-up', 'slide-down');
  });
  
  // Clean up old element
  setTimeout(() => {
    currentValue.remove();
  }, 200);
}
```

---

### 5.3 Remove Item Animation

#### Swipe to Delete

**Animation Properties:**
- Item: `transform: translateX()` + `opacity`
- Delete reveal: Background slide
- Collapse: Height animation

**Timing:**
- Swipe threshold: `100px`
- Delete animation: `300ms`
- Collapse: `200ms`
- Easing: `ease-out`

**Implementation:**

```scss
.cart-item {
  position: relative;
  overflow: hidden;
  transition: 
    transform 200ms ease-out,
    opacity 200ms ease-out;
  
  &.swiping {
    transition: none;
  }
  
  &.removing {
    transform: translateX(-100%);
    opacity: 0;
  }
  
  .delete-reveal {
    position: absolute;
    right: 0;
    top: 0;
    bottom: 0;
    width: 80px;
    background-color: var(--color-error);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    transform: translateX(100%);
    transition: transform 200ms ease-out;
  }
  
  &.swiped .delete-reveal {
    transform: translateX(0);
  }
}

// Collapse animation wrapper
.cart-item-wrapper {
  transition: 
    max-height 200ms ease-out,
    margin 200ms ease-out,
    padding 200ms ease-out;
  
  &.collapsing {
    max-height: 0 !important;
    margin: 0;
    padding: 0;
    overflow: hidden;
  }
}
```

---

#### Fade Out

**Animation Properties:**
- `opacity: 0`
- Optional: `transform: scale(0.95)`

**Timing:**
- Duration: `200ms`
- Collapse: `150ms` after fade
- Easing: `ease-out`

**Implementation:**

```scss
.cart-item {
  transition: 
    opacity 200ms ease-out,
    transform 200ms ease-out;
  
  &.removing {
    opacity: 0;
    transform: scale(0.95);
  }
}
```

---

### 5.4 Cart Total Update

**Animation Properties:**
- Number: Count up/down animation
- Highlight: Brief color flash
- Currency formatting maintained

**Timing:**
- Count duration: `300-500ms` (based on difference)
- Highlight: `200ms`
- Easing: `ease-out`

**Implementation:**

```typescript
function animateCartTotal(
  element: HTMLElement, 
  fromValue: number, 
  toValue: number,
  currency: string = '$'
): void {
  const duration = Math.min(500, Math.abs(toValue - fromValue) * 10 + 200);
  const startTime = performance.now();
  
  // Highlight effect
  element.classList.add('updating');
  
  function update(currentTime: number): void {
    const elapsed = currentTime - startTime;
    const progress = Math.min(elapsed / duration, 1);
    
    // Ease out
    const easeOut = 1 - Math.pow(1 - progress, 3);
    const currentValue = fromValue + (toValue - fromValue) * easeOut;
    
    element.textContent = `${currency}${currentValue.toFixed(2)}`;
    
    if (progress < 1) {
      requestAnimationFrame(update);
    } else {
      element.classList.remove('updating');
    }
  }
  
  requestAnimationFrame(update);
}
```

```scss
.cart-total {
  transition: color 200ms ease-out;
  
  &.updating {
    color: var(--color-primary);
  }
}
```

---

### 5.5 Checkout Button States

**Animation Properties:**
- Idle: Standard button styles
- Loading: Spinner + text change
- Processing: Progress indicator
- Success: Checkmark + redirect

**Timing:**
- State transitions: `200ms`
- Loading spinner: `800ms` cycle
- Success hold: `1000ms` before redirect

**Implementation:**

```scss
.checkout-btn {
  position: relative;
  min-width: 200px;
  
  .btn-content {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    transition: opacity 200ms ease-out;
  }
  
  .btn-spinner,
  .btn-success {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transition: opacity 200ms ease-out;
  }
  
  // States
  &.loading {
    pointer-events: none;
    
    .btn-content { opacity: 0; }
    .btn-spinner { opacity: 1; }
  }
  
  &.success {
    background-color: var(--color-success);
    pointer-events: none;
    
    .btn-content { opacity: 0; }
    .btn-success { opacity: 1; }
  }
  
  &.error {
    animation: shake 400ms ease-out;
    background-color: var(--color-error);
  }
}
```

---

## 6. Navigation Interactions

### 6.1 Menu Open/Close

#### Slide Animation

**Animation Properties:**
- Menu panel: `transform: translateX()` or `translateY()`
- Backdrop: `opacity`
- Body scroll lock

**Timing:**
- Open: `300ms`
- Close: `250ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
// Mobile slide-out menu
.mobile-menu {
  position: fixed;
  top: 0;
  left: 0;
  width: 300px;
  height: 100vh;
  background: var(--color-surface);
  transform: translateX(-100%);
  transition: transform 300ms cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 1000;
  
  &.open {
    transform: translateX(0);
  }
}

.menu-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  opacity: 0;
  visibility: hidden;
  transition: 
    opacity 300ms ease-out,
    visibility 300ms ease-out;
  z-index: 999;
  
  &.visible {
    opacity: 1;
    visibility: visible;
  }
}
```

---

#### Hamburger to X Transform

**Animation Properties:**
- Top line: Rotate + translate
- Middle line: Opacity fade
- Bottom line: Rotate + translate

**Timing:**
- Duration: `300ms`
- Easing: `ease-out`

**Implementation:**

```scss
.hamburger {
  width: 24px;
  height: 20px;
  position: relative;
  cursor: pointer;
  
  .line {
    position: absolute;
    left: 0;
    width: 100%;
    height: 2px;
    background: currentColor;
    transition: 
      transform 300ms ease-out,
      opacity 300ms ease-out;
    
    &:nth-child(1) {
      top: 0;
    }
    
    &:nth-child(2) {
      top: 50%;
      transform: translateY(-50%);
    }
    
    &:nth-child(3) {
      bottom: 0;
    }
  }
  
  &.open {
    .line:nth-child(1) {
      transform: translateY(9px) rotate(45deg);
    }
    
    .line:nth-child(2) {
      opacity: 0;
    }
    
    .line:nth-child(3) {
      transform: translateY(-9px) rotate(-45deg);
    }
  }
}
```

---

### 6.2 Dropdown Animations

#### Fade + Scale

**Animation Properties:**
- `opacity`: 0 to 1
- `transform`: Scale from 0.95 to 1
- `transform-origin`: Based on position

**Timing:**
- Open: `200ms`
- Close: `150ms`
- Easing: `ease-out`

**Implementation:**

```scss
.dropdown {
  position: relative;
  
  .dropdown-menu {
    position: absolute;
    top: 100%;
    left: 0;
    min-width: 200px;
    background: var(--color-surface);
    border-radius: 8px;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
    opacity: 0;
    visibility: hidden;
    transform: scale(0.95);
    transform-origin: top left;
    transition: 
      opacity 200ms ease-out,
      transform 200ms ease-out,
      visibility 200ms ease-out;
    
    // Position variations
    &.align-right {
      left: auto;
      right: 0;
      transform-origin: top right;
    }
    
    &.drop-up {
      top: auto;
      bottom: 100%;
      transform-origin: bottom left;
    }
  }
  
  &.open .dropdown-menu {
    opacity: 1;
    visibility: visible;
    transform: scale(1);
  }
}
```

---

#### Slide Down

**Animation Properties:**
- `max-height`: 0 to content height
- `opacity`: 0 to 1
- Clip overflow during animation

**Timing:**
- Duration: `250ms`
- Easing: `ease-out`

**Implementation:**

```scss
.dropdown-menu {
  max-height: 0;
  opacity: 0;
  overflow: hidden;
  transition: 
    max-height 250ms ease-out,
    opacity 200ms ease-out;
  
  &.open {
    max-height: 500px; // Or use JS for precise height
    opacity: 1;
  }
}
```

**JavaScript for dynamic height:**

```typescript
function toggleDropdown(menu: HTMLElement, isOpen: boolean): void {
  if (isOpen) {
    // Get actual content height
    menu.style.maxHeight = 'none';
    const height = menu.scrollHeight;
    menu.style.maxHeight = '0';
    
    // Force reflow
    void menu.offsetHeight;
    
    menu.style.maxHeight = `${height}px`;
    menu.classList.add('open');
  } else {
    menu.style.maxHeight = '0';
    menu.classList.remove('open');
  }
}
```

---

### 6.3 Tab Switching

#### Underline Slide

**Animation Properties:**
- Underline indicator: `transform: translateX()` + `width`
- Tab content: Fade or slide

**Timing:**
- Underline: `300ms`
- Content: `200ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.tabs {
  position: relative;
  
  .tab-list {
    display: flex;
    border-bottom: 1px solid var(--color-border);
  }
  
  .tab {
    padding: 12px 24px;
    background: none;
    border: none;
    cursor: pointer;
    position: relative;
    color: var(--color-text-muted);
    transition: color 200ms ease-out;
    
    &.active,
    &:hover {
      color: var(--color-text);
    }
  }
  
  .tab-indicator {
    position: absolute;
    bottom: 0;
    height: 2px;
    background: var(--color-primary);
    transition: 
      transform 300ms cubic-bezier(0.4, 0, 0.2, 1),
      width 300ms cubic-bezier(0.4, 0, 0.2, 1);
  }
  
  .tab-content {
    opacity: 0;
    transform: translateX(20px);
    transition: 
      opacity 200ms ease-out,
      transform 200ms ease-out;
    
    &.active {
      opacity: 1;
      transform: translateX(0);
    }
  }
}
```

**JavaScript:**

```typescript
function switchTab(tabs: NodeListOf<HTMLElement>, index: number): void {
  const indicator = document.querySelector('.tab-indicator') as HTMLElement;
  const activeTab = tabs[index];
  
  // Update indicator position
  indicator.style.width = `${activeTab.offsetWidth}px`;
  indicator.style.transform = `translateX(${activeTab.offsetLeft}px)`;
  
  // Update active states
  tabs.forEach((tab, i) => {
    tab.classList.toggle('active', i === index);
    tab.setAttribute('aria-selected', String(i === index));
  });
}
```

---

### 6.4 Breadcrumb Updates

**Animation Properties:**
- New item: Fade in + slide from right
- Separator: Scale appearance

**Timing:**
- Duration: `200ms`
- Stagger: `50ms` between items
- Easing: `ease-out`

**Implementation:**

```scss
.breadcrumb-item {
  opacity: 0;
  transform: translateX(10px);
  animation: breadcrumb-enter 200ms ease-out forwards;
  
  @for $i from 1 through 5 {
    &:nth-child(#{$i}) {
      animation-delay: #{($i - 1) * 50}ms;
    }
  }
}

@keyframes breadcrumb-enter {
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.breadcrumb-separator {
  opacity: 0;
  transform: scale(0);
  animation: separator-enter 150ms ease-out forwards;
}

@keyframes separator-enter {
  to {
    opacity: 1;
    transform: scale(1);
  }
}
```

---

## 7. Notification Interactions

### 7.1 Toast Entrance/Exit

#### Slide + Fade

**Animation Properties:**
- Enter: `transform: translateY()` + `opacity`
- Exit: Reverse animation
- Position: Top, bottom, or corner

**Timing:**
- Enter: `300ms`
- Exit: `200ms`
- Auto-dismiss: `4000-6000ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.toast-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  z-index: 9999;
}

.toast {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
  background: var(--color-surface);
  border-radius: 8px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
  transform: translateX(100%);
  opacity: 0;
  animation: toast-enter 300ms cubic-bezier(0.4, 0, 0.2, 1) forwards;
  
  &.exiting {
    animation: toast-exit 200ms ease-out forwards;
  }
  
  // Variants
  &.success {
    border-left: 4px solid var(--color-success);
  }
  
  &.error {
    border-left: 4px solid var(--color-error);
  }
  
  &.warning {
    border-left: 4px solid var(--color-warning);
  }
  
  // Progress bar for auto-dismiss
  .toast-progress {
    position: absolute;
    bottom: 0;
    left: 0;
    height: 3px;
    background: var(--color-primary);
    animation: toast-progress 5000ms linear forwards;
  }
}

@keyframes toast-enter {
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

@keyframes toast-exit {
  to {
    transform: translateX(100%);
    opacity: 0;
  }
}

@keyframes toast-progress {
  from { width: 100%; }
  to { width: 0%; }
}
```

**JavaScript Toast Service:**

```typescript
interface ToastOptions {
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
}

function showToast(options: ToastOptions): void {
  const { message, type, duration = 5000 } = options;
  
  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.innerHTML = `
    <span class="toast-icon">${getIcon(type)}</span>
    <span class="toast-message">${message}</span>
    <button class="toast-close" aria-label="Close">&times;</button>
    <div class="toast-progress" style="animation-duration: ${duration}ms"></div>
  `;
  
  const container = document.querySelector('.toast-container');
  container?.appendChild(toast);
  
  // Announce to screen readers
  announceToScreenReader(message);
  
  // Auto-dismiss
  const dismissTimeout = setTimeout(() => dismissToast(toast), duration);
  
  // Manual dismiss
  toast.querySelector('.toast-close')?.addEventListener('click', () => {
    clearTimeout(dismissTimeout);
    dismissToast(toast);
  });
}

function dismissToast(toast: HTMLElement): void {
  toast.classList.add('exiting');
  
  toast.addEventListener('animationend', () => {
    toast.remove();
  }, { once: true });
}
```

---

### 7.2 Badge Count Update

**Animation Properties:**
- Number change: Scale bounce
- New badge: Pop in
- Zero state: Fade out

**Timing:**
- Bounce: `300ms`
- Pop in: `200ms`
- Fade out: `150ms`
- Easing: `cubic-bezier(0.175, 0.885, 0.32, 1.275)` (bouncy)

**Implementation:**

```scss
.notification-badge {
  position: absolute;
  top: -8px;
  right: -8px;
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  background: var(--color-error);
  color: white;
  border-radius: 9px;
  font-size: 11px;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  transform: scale(0);
  opacity: 0;
  transition: 
    transform 200ms cubic-bezier(0.175, 0.885, 0.32, 1.275),
    opacity 150ms ease-out;
  
  &.visible {
    transform: scale(1);
    opacity: 1;
  }
  
  &.updated {
    animation: badge-pulse 300ms ease-out;
  }
}

@keyframes badge-pulse {
  0% { transform: scale(1); }
  50% { transform: scale(1.3); }
  100% { transform: scale(1); }
}
```

---

### 7.3 Alert Appearance

**Animation Properties:**
- Slide down from top or expand in place
- Icon animation (shake for error, bounce for success)
- Optional: Backdrop for modal alerts

**Timing:**
- Appear: `300ms`
- Icon animation: `500ms`
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`

**Implementation:**

```scss
.alert {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 16px;
  border-radius: 8px;
  transform: translateY(-20px);
  opacity: 0;
  animation: alert-enter 300ms cubic-bezier(0.4, 0, 0.2, 1) forwards;
  
  &.success {
    background: var(--color-success-bg);
    border: 1px solid var(--color-success);
    
    .alert-icon {
      animation: success-bounce 500ms ease-out;
    }
  }
  
  &.error {
    background: var(--color-error-bg);
    border: 1px solid var(--color-error);
    
    .alert-icon {
      animation: error-shake 500ms ease-out;
    }
  }
  
  &.dismissing {
    animation: alert-exit 200ms ease-out forwards;
  }
}

@keyframes alert-enter {
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

@keyframes alert-exit {
  to {
    transform: translateY(-20px);
    opacity: 0;
  }
}

@keyframes success-bounce {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.2); }
}

@keyframes error-shake {
  0%, 100% { transform: translateX(0); }
  20%, 60% { transform: translateX(-5px); }
  40%, 80% { transform: translateX(5px); }
}
```

---

### 7.4 Success/Error Messages

**Animation Properties:**
- Container: Height expand/collapse
- Content: Fade + slide
- Icon: Type-specific animation

**Timing:**
- Expand: `250ms`
- Content: `200ms` (staggered 50ms after container)
- Easing: `ease-out`

**Implementation:**

```scss
.message-container {
  overflow: hidden;
  max-height: 0;
  opacity: 0;
  transition: 
    max-height 250ms ease-out,
    opacity 200ms ease-out 50ms;
  
  &.visible {
    max-height: 200px;
    opacity: 1;
  }
  
  .message-content {
    padding: 16px;
    transform: translateY(-10px);
    opacity: 0;
    transition: 
      transform 200ms ease-out,
      opacity 200ms ease-out;
  }
  
  &.visible .message-content {
    transform: translateY(0);
    opacity: 1;
    transition-delay: 100ms;
  }
}

// Inline form message
.form-message {
  font-size: 14px;
  padding: 8px 12px;
  border-radius: 4px;
  transform: translateY(-8px);
  opacity: 0;
  transition: 
    transform 200ms ease-out,
    opacity 200ms ease-out;
  
  &.visible {
    transform: translateY(0);
    opacity: 1;
  }
  
  &.success {
    background: var(--color-success-subtle);
    color: var(--color-success-dark);
  }
  
  &.error {
    background: var(--color-error-subtle);
    color: var(--color-error-dark);
  }
}
```

---

## 8. Timing Reference Guide

### Quick Reference Table

| Interaction Type | Duration | Easing | Notes |
|-----------------|----------|--------|-------|
| **Micro feedback** | 50-100ms | ease-out | Button press, instant feedback |
| **Standard hover** | 150ms | ease-out | Color changes, subtle transforms |
| **Content reveal** | 200ms | ease-out | Dropdowns, tooltips |
| **Card hover** | 250ms | ease-out | Lift effects, shadows |
| **Tab/page transitions** | 300ms | cubic-bezier | Content switches |
| **Complex animations** | 400-500ms | custom bezier | Multi-step sequences |
| **Loading spinners** | 800-1000ms | linear | Continuous rotation |
| **Auto-dismiss toasts** | 4000-6000ms | - | User reading time |

### Easing Functions Reference

```scss
// Standard easings
$ease-out: cubic-bezier(0, 0, 0.2, 1);
$ease-in: cubic-bezier(0.4, 0, 1, 1);
$ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);

// Special purpose
$ease-bounce: cubic-bezier(0.175, 0.885, 0.32, 1.275);  // Overshoot
$ease-snappy: cubic-bezier(0.7, 0, 0.3, 1);            // Quick response
$ease-smooth: cubic-bezier(0.25, 0.1, 0.25, 1);        // Natural movement

// CSS custom properties
:root {
  --ease-out: cubic-bezier(0, 0, 0.2, 1);
  --ease-in: cubic-bezier(0.4, 0, 1, 1);
  --ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-bounce: cubic-bezier(0.175, 0.885, 0.32, 1.275);
  --ease-snappy: cubic-bezier(0.7, 0, 0.3, 1);
}
```

### When to Use Which Duration

| Duration | Use Cases |
|----------|-----------|
| **50-100ms** | Button active states, immediate feedback |
| **150ms** | Hover states, color transitions |
| **200ms** | Input focus, small reveals, form validation |
| **250-300ms** | Card hover, dropdown menus, tab switches |
| **300-400ms** | Modal open/close, slide animations |
| **400-600ms** | Complex sequences, add-to-cart flights |

---

## 9. Accessibility Guidelines

### 9.1 Reduced Motion Support

**Always provide reduced motion alternatives:**

```scss
// Global reduced motion handling
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}

// Component-specific alternatives
.animated-element {
  transition: transform 300ms ease-out, opacity 300ms ease-out;
  
  @media (prefers-reduced-motion: reduce) {
    transition: opacity 0.01ms;
    transform: none !important;
  }
}
```

**JavaScript Detection:**

```typescript
const prefersReducedMotion = window.matchMedia(
  '(prefers-reduced-motion: reduce)'
).matches;

function animate(element: HTMLElement): void {
  if (prefersReducedMotion) {
    // Instant state change
    element.classList.add('final-state');
    return;
  }
  
  // Full animation
  element.animate(keyframes, options);
}
```

### 9.2 Focus Management

**Rules for animated focus:**

1. Never hide focused elements during animation
2. Keep focus visible at all times
3. Animate focus indicators smoothly

```scss
// Animated focus ring
.interactive-element {
  outline: 2px solid transparent;
  outline-offset: 2px;
  transition: 
    outline-color 150ms ease-out,
    outline-offset 150ms ease-out;
  
  &:focus-visible {
    outline-color: var(--color-primary);
    outline-offset: 4px;
  }
}

// Skip animation for focus changes
.modal {
  &.opening {
    // Don't trap focus during animation
    pointer-events: none;
  }
  
  &.open {
    pointer-events: auto;
    // Now safe to move focus
  }
}
```

### 9.3 Screen Reader Announcements

**Announce important state changes:**

```typescript
// Live region for announcements
const announcer = document.createElement('div');
announcer.setAttribute('aria-live', 'polite');
announcer.setAttribute('aria-atomic', 'true');
announcer.className = 'sr-only';
document.body.appendChild(announcer);

function announceToScreenReader(message: string): void {
  announcer.textContent = '';
  
  // Small delay ensures announcement is made
  setTimeout(() => {
    announcer.textContent = message;
  }, 100);
}

// Usage examples
addToCart(product).then(() => {
  announceToScreenReader(`${product.name} added to cart. Cart now has ${cartCount} items.`);
});

showToast({ type: 'success', message: 'Order placed successfully' });
announceToScreenReader('Order placed successfully');
```

### 9.4 Animation Timing for Accessibility

| Concern | Guideline |
|---------|-----------|
| **Cognitive load** | Don't animate multiple elements simultaneously in different directions |
| **Reading time** | Toast messages: minimum 4 seconds visible |
| **Seizure risk** | Never flash content more than 3 times per second |
| **Focus tracking** | Keep animated content near current focus |
| **Progress indication** | Always show loading state for actions > 1 second |

### 9.5 ARIA Attributes for Animated Elements

```html
<!-- Loading state -->
<button aria-busy="true" aria-disabled="true">
  <span class="loading-spinner"></span>
  Loading...
</button>

<!-- Expandable content -->
<button aria-expanded="false" aria-controls="dropdown-menu">
  Menu
</button>
<div id="dropdown-menu" aria-hidden="true">
  <!-- Dropdown content -->
</div>

<!-- Toast notification -->
<div role="status" aria-live="polite">
  Item added to cart
</div>

<!-- Error alert -->
<div role="alert" aria-live="assertive">
  Payment failed. Please try again.
</div>

<!-- Progress indicator -->
<div role="progressbar" aria-valuenow="50" aria-valuemin="0" aria-valuemax="100">
  50% complete
</div>
```

---

## Implementation Checklist

Before implementing micro-interactions:

- [ ] Define purpose: Does this animation aid understanding or provide feedback?
- [ ] Set timing: Use appropriate duration from reference table
- [ ] Choose easing: Match the feel to the interaction type
- [ ] Add reduced motion fallback
- [ ] Test with keyboard navigation
- [ ] Add ARIA attributes where needed
- [ ] Test screen reader announcements
- [ ] Verify no layout shift during animation
- [ ] Check performance (use transform/opacity only)
- [ ] Test on low-powered devices

---

## Resources

- [MDN: prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion)
- [Web Animations API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Animations_API)
- [WCAG 2.1 Animation Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/animation-from-interactions)
- [Cubic Bezier Generator](https://cubic-bezier.com/)
- [Material Design Motion](https://m3.material.io/styles/motion/overview)
