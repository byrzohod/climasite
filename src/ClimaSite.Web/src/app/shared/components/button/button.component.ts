import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

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
    >
      @if (loading()) {
        <svg
          class="animate-spin -ml-1 mr-2 h-4 w-4"
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
      <ng-content></ng-content>
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }

    button {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-weight: 500;
      border-radius: 0.5rem;
      transition: all 0.2s ease;
      cursor: pointer;

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      &:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
    }

    /* Sizes */
    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.875rem;
      line-height: 1.25rem;
    }

    .btn-md {
      padding: 0.5rem 1rem;
      font-size: 0.875rem;
      line-height: 1.25rem;
    }

    .btn-lg {
      padding: 0.75rem 1.5rem;
      font-size: 1rem;
      line-height: 1.5rem;
    }

    /* Variants */
    .btn-primary {
      background-color: var(--color-primary);
      color: white;
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-primary-hover);
      }

      &:active:not(:disabled) {
        background-color: var(--color-primary-active);
      }
    }

    .btn-secondary {
      background-color: var(--color-bg-tertiary);
      color: var(--color-text-primary);
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-bg-active);
      }
    }

    .btn-outline {
      background-color: transparent;
      color: var(--color-primary);
      border: 2px solid var(--color-primary);

      &:hover:not(:disabled) {
        background-color: var(--color-primary-light);
      }
    }

    .btn-ghost {
      background-color: transparent;
      color: var(--color-text-primary);
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-bg-hover);
      }
    }

    .btn-danger {
      background-color: var(--color-error);
      color: white;
      border: none;

      &:hover:not(:disabled) {
        background-color: var(--color-error-dark);
      }
    }

    .btn-full-width {
      width: 100%;
    }
  `]
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
  readonly fullWidth = input<boolean>(false);
  readonly testId = input<string>('button');

  readonly clicked = output<MouseEvent>();

  protected buttonClasses = () => {
    const classes = [
      `btn-${this.variant()}`,
      `btn-${this.size()}`,
    ];

    if (this.fullWidth()) {
      classes.push('btn-full-width');
    }

    return classes.join(' ');
  };

  protected handleClick(event: MouseEvent): void {
    if (!this.disabled() && !this.loading()) {
      this.clicked.emit(event);
    }
  }
}
