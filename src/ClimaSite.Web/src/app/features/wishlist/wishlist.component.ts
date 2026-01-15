import { Component, inject, signal, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { WishlistService } from '../../core/services/wishlist.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';
import { LoadingComponent } from '../../shared/components/loading/loading.component';

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
    LoadingComponent
  ],
  template: `
    <div class="wishlist-page">
      <div class="wishlist-container">
        <div class="breadcrumb">
          <a routerLink="/">{{ 'nav.home' | translate }}</a>
          <span class="separator">/</span>
          <span class="current">{{ 'nav.wishlist' | translate }}</span>
        </div>

        <div class="page-header">
          <h1>{{ 'nav.wishlist' | translate }}</h1>
          <p class="item-count">{{ products().length }} {{ 'products.title' | translate | lowercase }}</p>
        </div>

        @if (loading()) {
          <app-loading />
        } @else if (products().length === 0) {
          <div class="empty-state" data-testid="wishlist-empty">
            <div class="empty-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 01-.383-.218 25.18 25.18 0 01-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0112 5.052 5.5 5.5 0 0116.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 01-4.244 3.17 15.247 15.247 0 01-.383.219l-.022.012-.007.004-.003.001a.752.752 0 01-.704 0l-.003-.001z"/>
              </svg>
            </div>
            <h2>{{ 'wishlist.empty.title' | translate }}</h2>
            <p>{{ 'wishlist.empty.description' | translate }}</p>
            <a routerLink="/products" class="btn-primary">
              {{ 'wishlist.empty.browseProducts' | translate }}
            </a>
          </div>
        } @else {
          <div class="wishlist-actions">
            <button
              type="button"
              class="btn-clear"
              (click)="clearWishlist()"
              data-testid="clear-wishlist"
            >
              {{ 'wishlist.clearAll' | translate }}
            </button>
          </div>

          <div class="product-grid" data-testid="wishlist-items">
            @for (product of products(); track product.id) {
              <div class="wishlist-item" data-testid="wishlist-item">
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

    .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      background: var(--color-bg-primary);
      border-radius: 12px;

      .empty-icon {
        margin-bottom: 1.5rem;

        svg {
          width: 4rem;
          height: 4rem;
          color: var(--color-text-secondary);
        }
      }

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }
    }

    .btn-primary {
      display: inline-block;
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s ease;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    .wishlist-actions {
      display: flex;
      justify-content: flex-end;
      margin-bottom: 1rem;
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
      .page-header h1 {
        font-size: 1.5rem;
      }

      .product-grid {
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
      }
    }
  `]
})
export class WishlistComponent implements OnInit {
  private readonly wishlistService = inject(WishlistService);

  readonly products = signal<ProductBrief[]>([]);
  readonly loading = signal(true);

  constructor() {
    // Update products when wishlist changes
    effect(() => {
      const items = this.wishlistService.items();
      const cachedProducts = items
        .filter(item => item.product)
        .map(item => item.product as ProductBrief);
      this.products.set(cachedProducts);
    });
  }

  ngOnInit(): void {
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

  clearWishlist(): void {
    this.wishlistService.clearWishlist();
    this.products.set([]);
  }
}
