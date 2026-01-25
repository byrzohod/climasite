# SHARED COMPONENTS & DIRECTIVES

45+ components, 9 directives. All standalone, Signal-based.

## KEY COMPONENTS

| Component | Purpose | Signals |
|-----------|---------|---------|
| AlertComponent | Info/success/error alerts | type, dismissible inputs |
| ModalComponent | Dialog overlay | isOpen, closed output |
| ToastComponent | Notifications | Progress bar, hover pause |
| ButtonComponent | Styled buttons | variant, size, loading |
| BreadcrumbComponent | Navigation trail | items input |
| SkeletonComponent | Loading placeholders | type, count inputs |

## SIGNAL INPUT PATTERN

```typescript
@Component({ standalone: true })
export class AlertComponent {
  type = input<AlertType>('info');      // Signal input
  dismissible = input<boolean>(false);
  dismissed = output<void>();           // Signal output
  
  protected visible = signal(true);     // Local state
}
```

## DIRECTIVES

| Directive | Purpose |
|-----------|---------|
| RevealDirective | Scroll-triggered fade animations |
| CountUpDirective | Number animation on scroll |
| OptimizedImageDirective | Lazy loading, srcset |
| MagneticHoverDirective | Cursor-following effect |
| ScrollProgressDirective | Reading progress bar |

## DEPRECATED

`skeleton-product-card/` - Use `skeleton/` system instead.

## CONVENTIONS

- Standalone with explicit imports
- Signal inputs: `input<T>(defaultValue)`
- Signal outputs: `output<T>()`
- `data-testid` on ALL interactive elements
- CSS variables for colors (no hardcoding)
- TranslateModule for all text
