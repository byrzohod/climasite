import { Component, input, output, inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'success' | 'warm' | 'glass';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [class]="buttonClasses()"
      [attr.data-testid]="testId()"
      (click)="handleClick($event)"
      (mouseenter)="onMouseEnter($event)"
      (mousemove)="onMouseMove($event)"
      (mouseleave)="onMouseLeave()"
    >
      @if (loading()) {
        <svg
          class="btn-spinner"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          ></path>
        </svg>
      }
      @if (iconLeft()) {
        <span class="btn-icon-left">
          <ng-content select="[icon-left]"></ng-content>
        </span>
      }
      <span class="btn-content">
        <ng-content></ng-content>
      </span>
      @if (iconRight()) {
        <span class="btn-icon-right">
          <ng-content select="[icon-right]"></ng-content>
        </span>
      }
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }

    button {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      font-family: var(--font-body);
      font-weight: var(--font-medium);
      border-radius: var(--radius-lg);
      cursor: pointer;
      overflow: hidden;
      white-space: nowrap;
      transition: 
        background-color var(--duration-fast) var(--ease-smooth),
        color var(--duration-fast) var(--ease-smooth),
        border-color var(--duration-fast) var(--ease-smooth),
        box-shadow var(--duration-fast) var(--ease-smooth),
        transform var(--duration-fast) var(--ease-spring);

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
        transform: none !important;
      }

      &:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }

      &:active:not(:disabled) {
        transform: scale(0.98);
      }
    }

    /* Spinner */
    .btn-spinner {
      width: 1em;
      height: 1em;
      animation: spin 0.8s linear infinite;
      margin-right: 0.25rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    /* Icon slots */
    .btn-icon-left,
    .btn-icon-right {
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .btn-icon-left {
      margin-right: 0.25rem;
    }

    .btn-icon-right {
      margin-left: 0.25rem;
    }

    /* Sizes */
    .btn-xs {
      padding: 0.375rem 0.75rem;
      font-size: var(--text-body-xs);
      border-radius: var(--radius-md);

      .btn-spinner { width: 0.875rem; height: 0.875rem; }
    }

    .btn-sm {
      padding: 0.5rem 1rem;
      font-size: var(--text-body-xs);

      .btn-spinner { width: 0.875rem; height: 0.875rem; }
    }

    .btn-md {
      padding: 0.625rem 1.25rem;
      font-size: var(--text-body-sm);
    }

    .btn-lg {
      padding: 0.875rem 1.75rem;
      font-size: var(--text-body);
      border-radius: var(--radius-xl);
    }

    .btn-xl {
      padding: 1rem 2rem;
      font-size: var(--text-body-lg);
      border-radius: var(--radius-xl);

      .btn-spinner { width: 1.25rem; height: 1.25rem; }
    }

    /* Variants */
    .btn-primary {
      background: var(--gradient-primary-btn, var(--color-primary));
      color: var(--color-text-inverse);
      border: none;
      box-shadow: var(--shadow-btn);

      &:hover:not(:disabled) {
        box-shadow: var(--shadow-btn-hover), var(--glow-sm-primary);
        transform: translateY(-1px);
      }

      &:active:not(:disabled) {
        box-shadow: var(--shadow-btn-active);
        transform: translateY(0) scale(0.98);
      }
    }

    .btn-secondary {
      background-color: var(--color-bg-tertiary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border-primary);
      box-shadow: var(--shadow-xs);

      &:hover:not(:disabled) {
        background-color: var(--color-bg-hover);
        border-color: var(--color-border-secondary);
        box-shadow: var(--shadow-sm);
      }
    }

    .btn-outline {
      background-color: transparent;
      color: var(--color-primary);
      border: 2px solid var(--color-primary);

      &:hover:not(:disabled) {
        background-color: var(--color-primary-light);
        box-shadow: var(--glow-sm-primary);
      }
    }

    .btn-ghost {
      background-color: transparent;
      color: var(--color-text-secondary);
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-bg-hover);
        color: var(--color-text-primary);
      }
    }

    .btn-danger {
      background-color: var(--color-error);
      color: var(--color-text-inverse);
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-error-dark);
        box-shadow: var(--glow-error);
      }
    }

    .btn-success {
      background-color: var(--color-success);
      color: var(--color-text-inverse);
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-success-dark);
        box-shadow: var(--glow-success);
      }
    }

    .btn-warm {
      background: var(--gradient-warm);
      color: var(--color-text-inverse);
      border: none;
      box-shadow: var(--shadow-btn);

      &:hover:not(:disabled) {
        box-shadow: var(--shadow-btn-hover), var(--glow-warm);
        transform: translateY(-1px);
      }
    }

    .btn-glass {
      background: var(--glass-bg);
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      color: var(--color-text-primary);
      border: 1px solid var(--glass-border);

      &:hover:not(:disabled) {
        background: var(--glass-bg-heavy);
        border-color: rgba(255, 255, 255, 0.3);
      }
    }

    .btn-full-width {
      width: 100%;
    }

    .btn-icon-only {
      padding: 0.625rem;
      aspect-ratio: 1;

      .btn-content:empty {
        display: none;
      }
    }

    /* Magnetic hover effect (applied via JS) */
    .btn-magnetic {
      will-change: transform;
    }
  `]
})
export class ButtonComponent {
  private readonly platformId = inject(PLATFORM_ID);

  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
  readonly fullWidth = input<boolean>(false);
  readonly iconOnly = input<boolean>(false);
  readonly iconLeft = input<boolean>(false);
  readonly iconRight = input<boolean>(false);
  readonly magnetic = input<boolean>(false);
  readonly testId = input<string>('button');

  readonly clicked = output<MouseEvent>();

  private buttonElement: HTMLButtonElement | null = null;
  private isMagnetic = false;

  protected buttonClasses = () => {
    const classes = [
      `btn-${this.variant()}`,
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

    return classes.join(' ');
  };

  protected handleClick(event: MouseEvent): void {
    if (!this.disabled() && !this.loading()) {
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
