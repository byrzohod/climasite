import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export type AlertType = 'success' | 'warning' | 'error' | 'info';

@Component({
  selector: 'app-alert',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    @if (visible()) {
      <div
        role="alert"
        [class]="alertClasses()"
        [attr.data-testid]="testId()"
      >
        <div class="alert-content">
          @if (showIcon()) {
            <span class="alert-icon" aria-hidden="true">
              @switch (type()) {
                @case ('success') {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
                  </svg>
                }
                @case ('warning') {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
                  </svg>
                }
                @case ('error') {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
                  </svg>
                }
                @case ('info') {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
                  </svg>
                }
              }
            </span>
          }
          <div class="alert-message">
            <ng-content></ng-content>
          </div>
        </div>
        @if (dismissible()) {
          <button
            type="button"
            class="alert-dismiss"
            (click)="dismiss()"
            [attr.aria-label]="'common.close' | translate"
            data-testid="alert-dismiss-button"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
            </svg>
          </button>
        }
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .alert {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 1rem;
      border-radius: var(--radius-lg);
      border: 1px solid transparent;
      transition: opacity 0.2s ease, transform 0.2s ease;
    }

    .alert-content {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      flex: 1;
      min-width: 0;
    }

    .alert-icon {
      flex-shrink: 0;
      width: 1.25rem;
      height: 1.25rem;
      margin-top: 0.125rem;
    }

    .alert-icon svg {
      width: 100%;
      height: 100%;
    }

    .alert-message {
      flex: 1;
      font-size: 0.875rem;
      line-height: 1.5;
      color: inherit;
    }

    .alert-dismiss {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 1.5rem;
      height: 1.5rem;
      padding: 0;
      border: none;
      background: transparent;
      cursor: pointer;
      border-radius: var(--radius-md);
      color: currentColor;
      opacity: 0.7;
      transition: opacity 0.15s ease, background-color 0.15s ease;
    }

    .alert-dismiss:hover {
      opacity: 1;
      background-color: rgba(0, 0, 0, 0.1);
    }

    .alert-dismiss:focus-visible {
      outline: 2px solid currentColor;
      outline-offset: 2px;
    }

    .alert-dismiss svg {
      width: 1rem;
      height: 1rem;
    }

    /* Variants */
    .alert-success {
      background-color: var(--color-success-light);
      border-color: var(--color-success);
      color: var(--color-success-dark);
    }

    .alert-warning {
      background-color: var(--color-warning-light);
      border-color: var(--color-warning);
      color: var(--color-warning-dark);
    }

    .alert-error {
      background-color: var(--color-error-light);
      border-color: var(--color-error);
      color: var(--color-error-dark);
    }

    .alert-info {
      background-color: var(--color-info-light);
      border-color: var(--color-info);
      color: var(--color-info-dark);
    }
  `]
})
export class AlertComponent {
  readonly type = input<AlertType>('info');
  readonly dismissible = input<boolean>(false);
  readonly showIcon = input<boolean>(true);
  readonly testId = input<string>('alert');

  readonly dismissed = output<void>();

  protected visible = signal(true);

  protected alertClasses = () => {
    return `alert alert-${this.type()}`;
  };

  protected dismiss(): void {
    this.visible.set(false);
    this.dismissed.emit();
  }

  /** Public method to reset visibility (useful when reusing the component) */
  show(): void {
    this.visible.set(true);
  }
}
