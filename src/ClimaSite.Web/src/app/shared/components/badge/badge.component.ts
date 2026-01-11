import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
export type BadgeSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span
      [class]="badgeClasses()"
      [attr.data-testid]="testId()"
    >
      <ng-content></ng-content>
    </span>
  `,
  styles: [`
    span {
      display: inline-flex;
      align-items: center;
      font-weight: 500;
      border-radius: 9999px;
      white-space: nowrap;
    }

    /* Sizes */
    .badge-sm {
      padding: 0.125rem 0.5rem;
      font-size: 0.75rem;
      line-height: 1rem;
    }

    .badge-md {
      padding: 0.25rem 0.625rem;
      font-size: 0.75rem;
      line-height: 1rem;
    }

    .badge-lg {
      padding: 0.375rem 0.75rem;
      font-size: 0.875rem;
      line-height: 1.25rem;
    }

    /* Variants */
    .badge-default {
      background-color: var(--color-bg-tertiary);
      color: var(--color-text-primary);
    }

    .badge-primary {
      background-color: var(--color-primary-light);
      color: var(--color-primary);
    }

    .badge-secondary {
      background-color: var(--color-bg-tertiary);
      color: var(--color-text-secondary);
    }

    .badge-success {
      background-color: var(--color-success-light);
      color: var(--color-success-dark);
    }

    .badge-warning {
      background-color: var(--color-warning-light);
      color: var(--color-warning-dark);
    }

    .badge-error {
      background-color: var(--color-error-light);
      color: var(--color-error-dark);
    }

    .badge-info {
      background-color: var(--color-info-light);
      color: var(--color-info-dark);
    }
  `]
})
export class BadgeComponent {
  readonly variant = input<BadgeVariant>('default');
  readonly size = input<BadgeSize>('md');
  readonly testId = input<string>('badge');

  protected badgeClasses = () => {
    return `badge-${this.variant()} badge-${this.size()}`;
  };
}
