import { Component, input, signal, inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export interface ShareOption {
  name: string;
  icon: string;
  url: string;
  color: string;
}

@Component({
  selector: 'app-share-product',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="share-product" data-testid="share-product">
      <button
        class="share-trigger"
        [class.open]="isOpen()"
        (click)="toggleDropdown()"
        [attr.aria-expanded]="isOpen()"
        aria-haspopup="true">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="18" cy="5" r="3"/>
          <circle cx="6" cy="12" r="3"/>
          <circle cx="18" cy="19" r="3"/>
          <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
          <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
        </svg>
        <span>{{ 'products.share.title' | translate }}</span>
      </button>

      @if (isOpen()) {
        <div class="share-dropdown" role="menu">
          <button
            class="share-option copy-link"
            (click)="copyToClipboard()"
            [class.copied]="copied()"
            data-testid="copy-link">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              @if (copied()) {
                <polyline points="20 6 9 17 4 12"/>
              } @else {
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
              }
            </svg>
            <span>{{ copied() ? ('products.share.copied' | translate) : ('products.share.copyLink' | translate) }}</span>
          </button>

          <div class="share-divider"></div>
          <span class="share-via">{{ 'products.share.shareVia' | translate }}</span>

          @for (option of shareOptions(); track option.name) {
            <a
              class="share-option"
              [href]="option.url"
              target="_blank"
              rel="noopener noreferrer"
              [style.--share-color]="option.color"
              [attr.data-testid]="'share-' + option.name.toLowerCase()"
              (click)="closeDropdown()">
              <span class="icon" [innerHTML]="option.icon"></span>
              <span>{{ option.name }}</span>
            </a>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .share-product {
      position: relative;
      display: inline-block;
    }

    .share-trigger {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border);
      border-radius: 8px;
      background: var(--color-bg-primary);
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;

      svg {
        width: 18px;
        height: 18px;
      }

      &:hover, &.open {
        border-color: var(--color-primary);
        color: var(--color-primary);
      }
    }

    .share-dropdown {
      position: absolute;
      top: calc(100% + 8px);
      right: 0;
      min-width: 200px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
      padding: 0.5rem;
      z-index: 100;
      animation: dropdownFade 0.2s ease-out;
    }

    @keyframes dropdownFade {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .share-option {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      width: 100%;
      padding: 0.625rem 0.75rem;
      border: none;
      border-radius: 6px;
      background: transparent;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;

      svg, .icon {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      &:hover {
        background: var(--color-bg-secondary);
        color: var(--share-color, var(--color-primary));
      }

      &.copy-link {
        color: var(--color-text-primary);

        &.copied {
          color: #22c55e;
        }
      }
    }

    .share-divider {
      height: 1px;
      background: var(--color-border);
      margin: 0.5rem 0;
    }

    .share-via {
      display: block;
      padding: 0.25rem 0.75rem;
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .icon {
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .icon :global(svg) {
      width: 20px;
      height: 20px;
    }
  `]
})
export class ShareProductComponent {
  private readonly platformId = inject(PLATFORM_ID);

  productName = input.required<string>();
  productUrl = input<string>('');

  isOpen = signal(false);
  copied = signal(false);

  shareOptions = signal<ShareOption[]>([
    {
      name: 'Facebook',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>',
      url: '',
      color: '#1877F2'
    },
    {
      name: 'Twitter',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>',
      url: '',
      color: '#000000'
    },
    {
      name: 'WhatsApp',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/></svg>',
      url: '',
      color: '#25D366'
    },
    {
      name: 'Email',
      icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>',
      url: '',
      color: '#EA4335'
    }
  ]);

  constructor() {
    this.updateShareUrls();
  }

  private updateShareUrls(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    // Will be called when component initializes
    setTimeout(() => {
      const url = this.productUrl() || window.location.href;
      const text = encodeURIComponent(this.productName());
      const encodedUrl = encodeURIComponent(url);

      this.shareOptions.update(options =>
        options.map(option => {
          switch (option.name) {
            case 'Facebook':
              return { ...option, url: `https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}` };
            case 'Twitter':
              return { ...option, url: `https://twitter.com/intent/tweet?text=${text}&url=${encodedUrl}` };
            case 'WhatsApp':
              return { ...option, url: `https://wa.me/?text=${text}%20${encodedUrl}` };
            case 'Email':
              return { ...option, url: `mailto:?subject=${text}&body=${text}%20${encodedUrl}` };
            default:
              return option;
          }
        })
      );
    });
  }

  toggleDropdown(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      this.updateShareUrls();
    }
  }

  closeDropdown(): void {
    this.isOpen.set(false);
  }

  async copyToClipboard(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;

    const url = this.productUrl() || window.location.href;

    try {
      await navigator.clipboard.writeText(url);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch {
      // Fallback for older browsers
      const textArea = document.createElement('textarea');
      textArea.value = url;
      textArea.style.position = 'fixed';
      textArea.style.left = '-9999px';
      document.body.appendChild(textArea);
      textArea.select();
      document.execCommand('copy');
      document.body.removeChild(textArea);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    }
  }
}
