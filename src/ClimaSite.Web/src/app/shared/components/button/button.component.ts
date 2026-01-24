import { Component, input, output, inject, PLATFORM_ID, computed } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';

/**
 * Button variant types following Nordic Tech Design System
 */
export type ButtonVariant = 
  | 'primary' 
  | 'secondary' 
  | 'outline' 
  | 'ghost' 
  | 'destructive' 
  | 'success' 
  | 'link'
  | 'glass'
  // Legacy aliases
  | 'danger'
  | 'warm';

/**
 * Button size options
 */
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Button type attribute
 */
export type ButtonType = 'button' | 'submit' | 'reset';

/**
 * Icon position in button
 */
export type IconPosition = 'left' | 'right';

/**
 * Modern, accessible button component following Nordic Tech Design System.
 * 
 * @example
 * ```html
 * <app-button variant="primary" size="lg" [loading]="isSubmitting()">
 *   Submit Form
 * </app-button>
 * 
 * <app-button variant="outline" [iconLeft]="true">
 *   <lucide-icon icon-left name="plus" [size]="16" />
 *   Add Item
 * </app-button>
 * ```
 */
@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type()"
      [disabled]="isDisabled()"
      [class]="buttonClasses()"
      [attr.data-testid]="testId()"
      [attr.aria-disabled]="disabled() || loading() ? 'true' : null"
      [attr.aria-busy]="loading() ? 'true' : null"
      (click)="handleClick($event)"
      (mouseenter)="onMouseEnter($event)"
      (mousemove)="onMouseMove($event)"
      (mouseleave)="onMouseLeave()"
    >
      <!-- Loading spinner -->
      @if (loading()) {
        <span class="btn-spinner" aria-hidden="true">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              class="spinner-track"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              stroke-width="3"
            ></circle>
            <path
              class="spinner-head"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
        </span>
      }
      
      <!-- Left icon slot -->
      @if (iconLeft() && !loading()) {
        <span class="btn-icon btn-icon-left" aria-hidden="true">
          <ng-content select="[icon-left]"></ng-content>
        </span>
      }
      
      <!-- Main content -->
      <span class="btn-content" [class.btn-content-hidden]="loading()">
        <ng-content></ng-content>
      </span>
      
      <!-- Right icon slot -->
      @if (iconRight() && !loading()) {
        <span class="btn-icon btn-icon-right" aria-hidden="true">
          <ng-content select="[icon-right]"></ng-content>
        </span>
      }
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }

    /* Base Button */
    .btn {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2, 0.5rem);
      font-family: var(--font-body);
      font-weight: var(--font-medium, 500);
      line-height: 1;
      white-space: nowrap;
      cursor: pointer;
      user-select: none;
      text-decoration: none;
      border: none;
      border-radius: var(--radius-lg, 0.5rem);
      transition: 
        background-color var(--duration-fast, 150ms) var(--ease-smooth, ease),
        color var(--duration-fast, 150ms) var(--ease-smooth, ease),
        border-color var(--duration-fast, 150ms) var(--ease-smooth, ease),
        box-shadow var(--duration-fast, 150ms) var(--ease-smooth, ease),
        transform var(--duration-fast, 150ms) var(--ease-spring, ease),
        opacity var(--duration-fast, 150ms) var(--ease-smooth, ease);
    }

    .btn:focus-visible {
      outline: var(--focus-ring-width, 2px) solid var(--focus-ring-color, var(--color-primary));
      outline-offset: var(--focus-ring-offset, 2px);
    }

    .btn:focus:not(:focus-visible) {
      outline: none;
    }

    .btn:disabled,
    .btn[aria-disabled="true"] {
      opacity: 0.5;
      cursor: not-allowed;
      pointer-events: none;
    }

    .btn:active:not(:disabled) {
      transform: scale(0.98);
    }

    /* Sizes */
    .btn-xs {
      min-height: var(--btn-height-xs, 1.75rem);
      padding: var(--space-1-5, 0.375rem) var(--space-3, 0.75rem);
      font-size: 0.75rem;
      border-radius: var(--radius-md, 0.25rem);
      gap: var(--space-1, 0.25rem);
    }
    .btn-xs .btn-spinner svg { width: 0.875rem; height: 0.875rem; }
    .btn-xs .btn-icon { font-size: 0.875rem; }

    .btn-sm {
      min-height: var(--btn-height-sm, 2rem);
      padding: var(--space-2, 0.5rem) var(--space-4, 1rem);
      font-size: 0.75rem;
      gap: var(--space-1-5, 0.375rem);
    }
    .btn-sm .btn-spinner svg { width: 0.875rem; height: 0.875rem; }
    .btn-sm .btn-icon { font-size: 1rem; }

    .btn-md {
      min-height: var(--btn-height-md, 2.5rem);
      padding: var(--space-2-5, 0.625rem) var(--space-5, 1.25rem);
      font-size: 0.875rem;
    }
    .btn-md .btn-spinner svg { width: 1rem; height: 1rem; }
    .btn-md .btn-icon { font-size: 1.25rem; }

    .btn-lg {
      min-height: var(--btn-height-lg, 3rem);
      padding: var(--space-3-5, 0.875rem) var(--space-7, 1.75rem);
      font-size: 1rem;
      border-radius: var(--radius-xl, 0.75rem);
      gap: var(--space-2-5, 0.625rem);
    }
    .btn-lg .btn-spinner svg { width: 1.125rem; height: 1.125rem; }
    .btn-lg .btn-icon { font-size: 1.5rem; }

    .btn-xl {
      min-height: var(--btn-height-xl, 3.5rem);
      padding: var(--space-4, 1rem) var(--space-8, 2rem);
      font-size: 1.125rem;
      border-radius: var(--radius-xl, 0.75rem);
      gap: var(--space-3, 0.75rem);
    }
    .btn-xl .btn-spinner svg { width: 1.25rem; height: 1.25rem; }
    .btn-xl .btn-icon { font-size: 1.75rem; }

    /* Variants */
    .btn-primary {
      background: var(--gradient-primary-btn, var(--color-primary));
      color: var(--color-text-inverse, #fff);
      box-shadow: var(--shadow-btn);
    }
    .btn-primary:hover:not(:disabled) {
      box-shadow: var(--shadow-btn-hover), var(--glow-sm-primary);
      transform: translateY(-1px);
    }
    .btn-primary:active:not(:disabled) {
      box-shadow: var(--shadow-btn-active);
      transform: translateY(0) scale(0.98);
    }

    .btn-secondary {
      background-color: var(--color-bg-tertiary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border-primary);
      box-shadow: var(--shadow-xs);
    }
    .btn-secondary:hover:not(:disabled) {
      background-color: var(--color-bg-hover);
      border-color: var(--color-border-secondary);
      box-shadow: var(--shadow-sm);
    }
    .btn-secondary:active:not(:disabled) {
      box-shadow: none;
    }

    .btn-outline {
      background-color: transparent;
      color: var(--color-primary);
      border: 2px solid var(--color-primary);
    }
    .btn-outline:hover:not(:disabled) {
      background-color: var(--color-primary-light);
      box-shadow: var(--glow-sm-primary);
    }
    .btn-outline:active:not(:disabled) {
      box-shadow: none;
    }

    .btn-ghost {
      background-color: transparent;
      color: var(--color-text-secondary);
      border: none;
    }
    .btn-ghost:hover:not(:disabled) {
      background-color: var(--color-bg-hover);
      color: var(--color-text-primary);
    }

    .btn-destructive {
      background-color: var(--color-error);
      color: var(--color-text-inverse, #fff);
      border: none;
    }
    .btn-destructive:hover:not(:disabled) {
      background-color: var(--color-error-dark);
      box-shadow: var(--shadow-error);
    }
    .btn-destructive:active:not(:disabled) {
      box-shadow: none;
    }

    .btn-success {
      background-color: var(--color-success);
      color: var(--color-text-inverse, #fff);
      border: none;
    }
    .btn-success:hover:not(:disabled) {
      background-color: var(--color-success-dark);
      box-shadow: var(--shadow-success);
    }
    .btn-success:active:not(:disabled) {
      box-shadow: none;
    }

    .btn-link {
      background-color: transparent;
      color: var(--color-primary);
      border: none;
      padding-left: 0;
      padding-right: 0;
      min-height: auto;
      text-decoration: none;
    }
    .btn-link:hover:not(:disabled) {
      text-decoration: underline;
      color: var(--color-primary-hover);
    }
    .btn-link:focus-visible {
      outline-offset: 4px;
    }

    .btn-glass {
      background: var(--glass-bg);
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      color: var(--color-text-primary);
      border: 1px solid var(--glass-border);
    }
    .btn-glass:hover:not(:disabled) {
      background: var(--glass-bg-heavy);
      border-color: rgba(255, 255, 255, 0.3);
    }

    .btn-warm {
      background: var(--gradient-warm);
      color: var(--color-text-inverse, #fff);
      border: none;
      box-shadow: var(--shadow-btn);
    }
    .btn-warm:hover:not(:disabled) {
      box-shadow: var(--shadow-btn-hover), var(--shadow-warning);
      transform: translateY(-1px);
    }

    /* Modifiers */
    .btn-full-width {
      width: 100%;
    }

    .btn-icon-only {
      aspect-ratio: 1;
      padding: 0;
      justify-content: center;
    }
    .btn-icon-only.btn-xs { width: var(--btn-height-xs, 1.75rem); }
    .btn-icon-only.btn-sm { width: var(--btn-height-sm, 2rem); }
    .btn-icon-only.btn-md { width: var(--btn-height-md, 2.5rem); }
    .btn-icon-only.btn-lg { width: var(--btn-height-lg, 3rem); }
    .btn-icon-only.btn-xl { width: var(--btn-height-xl, 3.5rem); }
    .btn-icon-only .btn-content:empty { display: none; }

    .btn-magnetic {
      will-change: transform;
    }

    .btn-loading {
      pointer-events: none;
    }

    /* Inner Elements */
    .btn-spinner {
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .btn-spinner svg {
      animation: spin 0.8s linear infinite;
    }
    .spinner-track {
      opacity: 0.25;
    }
    .spinner-head {
      opacity: 0.75;
    }

    .btn-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      line-height: 0;
    }
    .btn-icon ::ng-deep svg,
    .btn-icon ::ng-deep lucide-icon {
      width: 1em;
      height: 1em;
    }

    .btn-icon-left {
      margin-right: var(--space-0-5, 0.125rem);
    }

    .btn-icon-right {
      margin-left: var(--space-0-5, 0.125rem);
    }

    .btn-content {
      display: inline-flex;
      align-items: center;
      transition: opacity var(--duration-fast, 150ms) var(--ease-smooth, ease);
    }
    .btn-content-hidden {
      opacity: 0;
      position: absolute;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }

    @media (prefers-reduced-motion: reduce) {
      .btn {
        transition: none;
      }
      .btn:hover:not(:disabled),
      .btn:active:not(:disabled) {
        transform: none;
      }
      .btn-spinner svg {
        animation: none;
      }
      .btn-magnetic {
        will-change: auto;
      }
    }
  `]
})
export class ButtonComponent {
  private readonly platformId = inject(PLATFORM_ID);

  // ----- Inputs -----
  
  /** Visual style variant */
  readonly variant = input<ButtonVariant>('primary');
  
  /** Size of the button */
  readonly size = input<ButtonSize>('md');
  
  /** HTML button type attribute */
  readonly type = input<ButtonType>('button');
  
  /** Whether the button is disabled */
  readonly disabled = input<boolean>(false);
  
  /** Whether the button is in loading state */
  readonly loading = input<boolean>(false);
  
  /** Make button full width of container */
  readonly fullWidth = input<boolean>(false);
  
  /** Icon-only button (square aspect ratio) */
  readonly iconOnly = input<boolean>(false);
  
  /** Whether button has left icon */
  readonly iconLeft = input<boolean>(false);
  
  /** Whether button has right icon */
  readonly iconRight = input<boolean>(false);
  
  /** Enable magnetic hover effect */
  readonly magnetic = input<boolean>(false);
  
  /** Test ID for E2E testing */
  readonly testId = input<string>('button');

  // ----- Outputs -----
  
  /** Emitted when button is clicked */
  readonly clicked = output<MouseEvent>();

  // ----- Private state -----
  
  private buttonElement: HTMLButtonElement | null = null;
  private isMagnetic = false;

  // ----- Computed -----
  
  /** Whether button should be disabled (disabled or loading) */
  protected readonly isDisabled = computed(() => this.disabled() || this.loading());

  /** 
   * Normalize variant - map legacy names to new ones 
   */
  protected readonly normalizedVariant = computed(() => {
    const v = this.variant();
    // Map legacy 'danger' to 'destructive'
    if (v === 'danger') return 'destructive';
    return v;
  });

  /** Build CSS class string for button */
  protected readonly buttonClasses = computed(() => {
    const classes = [
      'btn',
      `btn-${this.normalizedVariant()}`,
      `btn-${this.size()}`,
    ];

    if (this.fullWidth()) {
      classes.push('btn-full-width');
    }

    if (this.iconOnly()) {
      classes.push('btn-icon-only');
    }

    if (this.magnetic()) {
      classes.push('btn-magnetic');
    }

    if (this.loading()) {
      classes.push('btn-loading');
    }

    return classes.join(' ');
  });

  // ----- Event handlers -----

  protected handleClick(event: MouseEvent): void {
    if (!this.isDisabled()) {
      this.clicked.emit(event);
    }
  }

  protected onMouseEnter(event: MouseEvent): void {
    if (!this.magnetic() || !isPlatformBrowser(this.platformId)) return;
    
    this.buttonElement = event.target as HTMLButtonElement;
    this.isMagnetic = true;
  }

  protected onMouseMove(event: MouseEvent): void {
    if (!this.isMagnetic || !this.buttonElement) return;

    const rect = this.buttonElement.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const deltaX = (event.clientX - centerX) * 0.15;
    const deltaY = (event.clientY - centerY) * 0.15;

    this.buttonElement.style.transform = `translate(${deltaX}px, ${deltaY}px)`;
  }

  protected onMouseLeave(): void {
    if (!this.buttonElement) return;

    this.buttonElement.style.transform = '';
    this.isMagnetic = false;
    this.buttonElement = null;
  }
}
