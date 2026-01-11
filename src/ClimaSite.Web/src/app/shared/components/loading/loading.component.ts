import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type LoadingSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      [class]="containerClasses()"
      [attr.data-testid]="testId()"
      role="status"
      [attr.aria-label]="ariaLabel()"
    >
      <svg
        [class]="spinnerClasses()"
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
      @if (text()) {
        <span class="loading-text">{{ text() }}</span>
      }
    </div>
  `,
  styles: [`
    .loading-container {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }

    .loading-container-fullscreen {
      position: fixed;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background-color: var(--color-bg-overlay);
      z-index: 9999;
    }

    .loading-container-overlay {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background-color: var(--color-bg-overlay);
    }

    .loading-container-centered {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }

    .loading-spinner {
      animation: spin 1s linear infinite;
      color: var(--color-primary);
    }

    @keyframes spin {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }

    /* Sizes */
    .loading-sm {
      width: 1rem;
      height: 1rem;
    }

    .loading-md {
      width: 1.5rem;
      height: 1.5rem;
    }

    .loading-lg {
      width: 2rem;
      height: 2rem;
    }

    .loading-xl {
      width: 3rem;
      height: 3rem;
    }

    .loading-text {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
    }

    .loading-container-fullscreen .loading-text,
    .loading-container-overlay .loading-text {
      color: var(--color-text-inverse);
      margin-top: 0.75rem;
    }
  `]
})
export class LoadingComponent {
  readonly size = input<LoadingSize>('md');
  readonly text = input<string>('');
  readonly mode = input<'inline' | 'centered' | 'overlay' | 'fullscreen'>('inline');
  readonly testId = input<string>('loading');
  readonly ariaLabel = input<string>('Loading');

  protected containerClasses = () => {
    const mode = this.mode();
    if (mode === 'fullscreen') return 'loading-container-fullscreen';
    if (mode === 'overlay') return 'loading-container-overlay';
    if (mode === 'centered') return 'loading-container-centered';
    return 'loading-container';
  };

  protected spinnerClasses = () => {
    return `loading-spinner loading-${this.size()}`;
  };
}
