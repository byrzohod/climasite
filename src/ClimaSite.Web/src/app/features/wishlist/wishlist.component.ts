import { Component, inject, signal, computed, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { WishlistService } from '../../core/services/wishlist.service';
import { AuthService } from '../../auth/services/auth.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { EmptyStateComponent } from '../../shared/components/empty-state';

/**
 * NAV-002: WishlistComponent
 * Displays the user's wishlist with product details
 */
@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    TranslateModule,
    ProductCardComponent,
    LoadingComponent,
    EmptyStateComponent
  ],
  template: `
    <div class="wishlist-page">
      <div class="wishlist-container">
        <div class="breadcrumb">
          <a routerLink="/" data-testid="wishlist-breadcrumb-home">{{ 'nav.home' | translate }}</a>
          <span class="separator">/</span>
          <span class="current">
            {{ (isSharedView() ? 'wishlist.share.sharedTitle' : 'nav.wishlist') | translate }}
          </span>
        </div>

        <div class="page-header">
          <h1>{{ (isSharedView() ? 'wishlist.share.sharedTitle' : 'nav.wishlist') | translate }}</h1>
          <p class="item-count">{{ 'wishlist.itemCount' | translate:{ count: products().length } }}</p>
        </div>

        @if (loading()) {
          <app-loading />
        } @else if (notFound()) {
          <app-empty-state
            variant="wishlist"
            [title]="'wishlist.share.notFoundTitle' | translate"
            [description]="'wishlist.share.notFoundDescription' | translate"
            [actionLabel]="'emptyState.wishlist.action' | translate"
            actionRoute="/products"
            data-testid="wishlist-shared-not-found"
          />
        } @else if (products().length === 0) {
          <app-empty-state
            variant="wishlist"
            [title]="'emptyState.wishlist.title' | translate"
            [description]="'emptyState.wishlist.description' | translate"
            [actionLabel]="'emptyState.wishlist.action' | translate"
            actionRoute="/products"
            data-testid="wishlist-empty"
          />
        } @else {
          @if (!isSharedView()) {
            <div class="wishlist-actions">
              @if (authService.isAuthenticated()) {
                <div class="share-panel" data-testid="wishlist-share-panel">
                  <button
                    type="button"
                    class="btn-share"
                    (click)="toggleSharing()"
                    [disabled]="shareLoading()"
                    data-testid="wishlist-share-toggle"
                  >
                    {{ (wishlistService.isPublic() ? 'wishlist.share.disable' : 'wishlist.share.enable') | translate }}
                  </button>

                  @if (shareUrl()) {
                    <div class="share-link" data-testid="wishlist-share-url">{{ shareUrl() }}</div>
                    <button
                      type="button"
                      class="btn-share-secondary"
                      (click)="copyShareUrl()"
                      data-testid="wishlist-copy-share"
                    >
                      {{ 'wishlist.share.copy' | translate }}
                    </button>
                    <button
                      type="button"
                      class="btn-share-secondary"
                      (click)="regenerateShareToken()"
                      [disabled]="shareLoading()"
                      data-testid="wishlist-regenerate-share"
                    >
                      {{ 'wishlist.share.regenerate' | translate }}
                    </button>
                  }

                  @if (shareCopied()) {
                    <span class="share-status" aria-live="polite" data-testid="wishlist-share-copied">
                      {{ 'wishlist.share.copied' | translate }}
                    </span>
                  }
                </div>
              }

            @if (confirmingClear()) {
              <div class="clear-confirm" role="group" data-testid="wishlist-clear-confirm">
                <span class="clear-confirm-text">{{ 'wishlist.clearConfirm.message' | translate }}</span>
                <button
                  type="button"
                  class="btn-clear btn-clear-danger"
                  (click)="clearWishlist()"
                  [disabled]="clearLoading()"
                  data-testid="confirm-clear-wishlist"
                >
                  {{ 'wishlist.clearConfirm.confirm' | translate }}
                </button>
                <button
                  type="button"
                  class="btn-share-secondary"
                  (click)="cancelClear()"
                  [disabled]="clearLoading()"
                  data-testid="cancel-clear-wishlist"
                >
                  {{ 'wishlist.clearConfirm.cancel' | translate }}
                </button>
              </div>
            } @else {
              <button
                type="button"
                class="btn-clear"
                (click)="requestClear()"
                data-testid="clear-wishlist"
              >
                {{ 'wishlist.clearAll' | translate }}
              </button>
            }
            </div>
          }

          <div class="product-grid" data-testid="wishlist-items">
            @for (product of products(); track product.id) {
              <div class="wishlist-item" data-testid="wishlist-item">
                @if (!isSharedView()) {
                  <button
                    type="button"
                    class="remove-btn"
                    (click)="removeItem(product.id)"
                    [attr.aria-label]="'wishlist.remove' | translate"
                    data-testid="remove-from-wishlist"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.519.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd"/>
                    </svg>
                  </button>
                }
                <app-product-card [product]="product" />
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .wishlist-page {
      min-height: 60vh;
      background: var(--color-bg-secondary);
    }

    .wishlist-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 1rem;
    }

    .breadcrumb {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
      padding: 1rem 0;
      font-size: 0.875rem;
      color: var(--color-text-secondary);

      a {
        color: var(--color-primary);
        text-decoration: none;
        &:hover { text-decoration: underline; }
      }

      .separator { color: var(--color-text-secondary); }
      .current { color: var(--color-text-primary); }
    }

    .page-header {
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      .item-count {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }
    }

    .wishlist-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1rem;
    }

    .share-panel {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
      min-width: 0;
    }

    .share-link {
      max-width: min(42rem, 100%);
      padding: 0.5rem 0.75rem;
      overflow: hidden;
      color: var(--color-text-secondary);
      text-overflow: ellipsis;
      white-space: nowrap;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 6px;
      font-size: 0.8125rem;
    }

    .share-status {
      color: var(--color-success);
      font-size: 0.8125rem;
      font-weight: 600;
    }

    .btn-share,
    .btn-share-secondary {
      padding: 0.5rem 1rem;
      background: var(--color-primary);
      border: 1px solid var(--color-primary);
      border-radius: 6px;
      color: var(--color-text-inverse);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s ease;

      &:disabled {
        cursor: not-allowed;
        opacity: 0.65;
      }
    }

    .btn-share-secondary {
      background: var(--color-bg-primary);
      border-color: var(--color-border);
      color: var(--color-text-secondary);
    }

    .btn-clear {
      padding: 0.5rem 1rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 6px;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s ease;

      &:hover {
        background: var(--color-error-bg);
        color: var(--color-error);
        border-color: var(--color-error);
      }

      &:disabled {
        cursor: not-allowed;
        opacity: 0.65;
      }
    }

    .clear-confirm {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
    }

    .clear-confirm-text {
      font-size: 0.8125rem;
      color: var(--color-text-secondary);
    }

    .btn-clear-danger {
      background: var(--color-error-bg);
      color: var(--color-error);
      border-color: var(--color-error);
    }

    .product-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
    }

    .wishlist-item {
      position: relative;

      .remove-btn {
        position: absolute;
        top: 0.5rem;
        right: 0.5rem;
        z-index: 10;
        display: flex;
        align-items: center;
        justify-content: center;
        width: 2rem;
        height: 2rem;
        background: var(--color-bg-primary);
        border: 1px solid var(--color-border);
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s ease;

        svg {
          width: 1rem;
          height: 1rem;
          color: var(--color-text-secondary);
        }

        &:hover {
          background: var(--color-error-bg);
          border-color: var(--color-error);

          svg {
            color: var(--color-error);
          }
        }
      }
    }

    @media (max-width: 768px) {
      .wishlist-page {
        padding-bottom: calc(5rem + env(safe-area-inset-bottom, 0));
      }

      .page-header h1 {
        font-size: 1.5rem;
      }
      .product-grid {
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
      }

      :host ::ng-deep app-product-card .product-image {
        height: 170px;
        min-height: 170px;
      }
    }
  `]
})
export class WishlistComponent implements OnInit {
  readonly wishlistService = inject(WishlistService);
  private readonly route = inject(ActivatedRoute);
  readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly translate = inject(TranslateService);

  readonly products = signal<ProductBrief[]>([]);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly isSharedView = signal(false);
  readonly shareLoading = signal(false);
  readonly shareCopied = signal(false);
  readonly confirmingClear = signal(false);
  readonly clearLoading = signal(false);
  readonly shareUrl = computed(() => {
    const token = this.wishlistService.shareToken();
    if (!token || typeof window === 'undefined') {
      return null;
    }

    return `${window.location.origin}/wishlist/shared/${token}`;
  });

  constructor() {
    // Update products when wishlist changes
    effect(() => {
      if (this.isSharedView()) {
        return;
      }

      const items = this.wishlistService.items();
      const cachedProducts = items
        .filter(item => item.product)
        .map(item => item.product as ProductBrief);
      this.products.set(cachedProducts);
      this.loading.set(this.wishlistService.isLoading());
    });
  }

  ngOnInit(): void {
    const shareToken = this.route.snapshot.paramMap.get('shareToken');
    if (shareToken) {
      this.isSharedView.set(true);
      this.loadSharedWishlist(shareToken);
      return;
    }

    this.wishlistService.refreshWishlist().subscribe({
      next: () => this.loadWishlistProducts(),
      error: () => this.loading.set(false)
    });
    this.loadWishlistProducts();
  }

  loadWishlistProducts(): void {
    this.loading.set(true);

    const wishlistItems = this.wishlistService.items();

    if (wishlistItems.length === 0) {
      this.products.set([]);
      this.loading.set(false);
      return;
    }

    // Use cached products from wishlist items
    const cachedProducts = wishlistItems
      .filter(item => item.product)
      .map(item => item.product as ProductBrief);

    this.products.set(cachedProducts);
    this.loading.set(false);
  }

  removeItem(productId: string): void {
    this.wishlistService.removeFromWishlist(productId);
    this.products.update(products => products.filter(p => p.id !== productId));
  }

  /** Show the inline confirmation before clearing the whole wishlist. */
  requestClear(): void {
    this.confirmingClear.set(true);
  }

  /** Dismiss the inline clear confirmation without changing anything. */
  cancelClear(): void {
    this.confirmingClear.set(false);
  }

  clearWishlist(): void {
    const snapshot = this.products();
    this.clearLoading.set(true);
    this.products.set([]);

    this.wishlistService.clearWishlist().subscribe({
      next: () => {
        this.clearLoading.set(false);
        this.confirmingClear.set(false);
      },
      error: () => {
        // Restore the UI snapshot so a failed clear does not leave the page out of sync
        // with the server (the items reappear on refresh otherwise).
        this.products.set(snapshot);
        this.clearLoading.set(false);
        this.confirmingClear.set(false);
        this.toastService.error(this.translate.instant('wishlist.errors.clearFailed'));
      }
    });
  }

  toggleSharing(): void {
    this.shareLoading.set(true);
    const enabling = !this.wishlistService.isPublic();
    this.wishlistService.setSharing(enabling).subscribe({
      next: () => this.shareLoading.set(false),
      error: () => {
        this.shareLoading.set(false);
        this.toastService.error(this.translate.instant('wishlist.errors.shareFailed'));
      }
    });
  }

  regenerateShareToken(): void {
    this.shareLoading.set(true);
    this.wishlistService.regenerateShareToken().subscribe({
      next: () => this.shareLoading.set(false),
      error: () => {
        this.shareLoading.set(false);
        this.toastService.error(this.translate.instant('wishlist.errors.regenerateFailed'));
      }
    });
  }

  copyShareUrl(): void {
    const url = this.shareUrl();
    if (!url || typeof navigator === 'undefined' || !navigator.clipboard) {
      return;
    }

    navigator.clipboard.writeText(url).then(() => {
      this.shareCopied.set(true);
      window.setTimeout(() => this.shareCopied.set(false), 2500);
    });
  }

  private loadSharedWishlist(shareToken: string): void {
    this.loading.set(true);
    this.notFound.set(false);

    this.wishlistService.getSharedWishlist(shareToken).subscribe({
      next: wishlist => {
        this.products.set(wishlist.items.map(item => this.wishlistService.toProductBrief(item)));
        this.loading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }
}
