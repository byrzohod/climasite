import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export interface AddressData {
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  phone?: string;
}

@Component({
  selector: 'app-address-card',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="address-card" [class.compact]="compact()">
      @if (title()) {
        <div class="address-card__header">
          <h4 class="address-card__title">{{ title() }}</h4>
          @if (showCopyButton()) {
            <button
              type="button"
              class="address-card__copy-btn"
              (click)="copyToClipboard()"
              [title]="'common.copyToClipboard' | translate"
              data-testid="copy-address-btn"
            >
              @if (copied()) {
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
              } @else {
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                </svg>
              }
            </button>
          }
        </div>
      }

      @if (address()) {
        <div class="address-card__content" data-testid="address-content">
          <p class="address-card__name">
            {{ address()!.firstName }} {{ address()!.lastName }}
          </p>
          <p class="address-card__line">{{ address()!.addressLine1 }}</p>
          @if (address()!.addressLine2) {
            <p class="address-card__line">{{ address()!.addressLine2 }}</p>
          }
          <p class="address-card__line">
            {{ address()!.city }}@if (address()!.state) {, {{ address()!.state }}} {{ address()!.postalCode }}
          </p>
          <p class="address-card__line">{{ address()!.country }}</p>
          @if (address()!.phone && showPhone()) {
            <p class="address-card__phone">
              <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
              </svg>
              {{ address()!.phone }}
            </p>
          }
        </div>
      } @else {
        <p class="address-card__empty">{{ 'common.noAddress' | translate }}</p>
      }
    </div>
  `,
  styles: [`
    .address-card {
      background-color: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      padding: 1rem;

      &.compact {
        padding: 0.75rem;

        .address-card__title {
          font-size: 0.75rem;
        }

        .address-card__content {
          font-size: 0.8125rem;
        }
      }
    }

    .address-card__header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .address-card__title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.025em;
      margin: 0;
    }

    .address-card__copy-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      padding: 0;
      border: none;
      background-color: transparent;
      color: var(--color-text-secondary);
      border-radius: 4px;
      cursor: pointer;
      transition: all 0.2s ease;

      &:hover {
        background-color: var(--color-background-hover);
        color: var(--color-primary);
      }
    }

    .address-card__content {
      font-size: 0.875rem;
      line-height: 1.5;
    }

    .address-card__name {
      font-weight: 600;
      color: var(--color-text);
      margin: 0 0 0.25rem 0;
    }

    .address-card__line {
      color: var(--color-text-secondary);
      margin: 0;
    }

    .address-card__phone {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: var(--color-text-secondary);
      margin: 0.5rem 0 0 0;

      svg {
        flex-shrink: 0;
      }
    }

    .address-card__empty {
      color: var(--color-text-muted);
      font-style: italic;
      margin: 0;
    }
  `]
})
export class AddressCardComponent {
  address = input<AddressData | null>(null);
  title = input<string>('');
  compact = input<boolean>(false);
  showPhone = input<boolean>(true);
  showCopyButton = input<boolean>(true);

  copied = signal(false);
  addressCopied = output<void>();

  copyToClipboard(): void {
    const addr = this.address();
    if (!addr) return;

    const lines = [
      `${addr.firstName} ${addr.lastName}`,
      addr.addressLine1,
      addr.addressLine2,
      `${addr.city}${addr.state ? ', ' + addr.state : ''} ${addr.postalCode}`,
      addr.country,
      addr.phone
    ].filter(Boolean);

    const text = lines.join('\n');

    navigator.clipboard.writeText(text).then(() => {
      this.copied.set(true);
      this.addressCopied.emit();

      setTimeout(() => {
        this.copied.set(false);
      }, 2000);
    });
  }
}
