import { Component, inject, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { RecentlyViewedService, RecentlyViewedProduct } from '../../../core/services/recently-viewed.service';

@Component({
  selector: 'app-recently-viewed',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    @if (products().length > 0) {
      <section class="recently-viewed" data-testid="recently-viewed">
        <div class="section-header">
          <h3 class="section-title">{{ 'products.recentlyViewed.title' | translate }}</h3>
          @if (showClearButton() && products().length > 0) {
            <button
              class="btn-clear"
              (click)="clearHistory()"
              data-testid="clear-history">
              {{ 'products.recentlyViewed.clearHistory' | translate }}
            </button>
          }
        </div>

        <div class="products-scroll" [class.compact]="compact()">
          @for (product of products(); track product.id) {
            <a
              [routerLink]="['/products', product.slug]"
              class="product-card"
              [attr.data-testid]="'recently-viewed-' + product.id">
              <div class="product-image">
                @if (product.primaryImageUrl) {
                  <img [src]="product.primaryImageUrl" [alt]="product.name" loading="lazy" />
                } @else {
                  <div class="no-image">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                      <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                      <circle cx="8.5" cy="8.5" r="1.5"/>
                      <polyline points="21 15 16 10 5 21"/>
                    </svg>
                  </div>
                }
                @if (product.isOnSale) {
                  <span class="sale-badge">{{ 'common.sale' | translate }}</span>
                }
              </div>
              <div class="product-info">
                @if (product.brand) {
                  <span class="brand">{{ product.brand }}</span>
                }
                <span class="name">{{ product.name }}</span>
                <div class="price">
                  @if (product.isOnSale && product.salePrice) {
                    <span class="original">{{ product.basePrice | currency:'EUR' }}</span>
                    <span class="sale">{{ product.salePrice | currency:'EUR' }}</span>
                  } @else {
                    <span class="current">{{ product.basePrice | currency:'EUR' }}</span>
                  }
                </div>
              </div>
            </a>
          }
        </div>
      </section>
    }
  `,
  styles: [`
    .recently-viewed {
      padding: 1.5rem 0;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .section-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0;
    }

    .btn-clear {
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: transparent;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-danger);
        color: var(--color-danger);
      }
    }

    .products-scroll {
      display: flex;
      gap: 1rem;
      overflow-x: auto;
      padding-bottom: 0.5rem;
      scroll-snap-type: x mandatory;
      -webkit-overflow-scrolling: touch;

      &::-webkit-scrollbar {
        height: 6px;
      }

      &::-webkit-scrollbar-track {
        background: var(--color-bg-secondary);
        border-radius: 3px;
      }

      &::-webkit-scrollbar-thumb {
        background: var(--color-border);
        border-radius: 3px;

        &:hover {
          background: var(--color-text-secondary);
        }
      }

      &.compact {
        .product-card {
          min-width: 140px;
          max-width: 140px;
        }

        .product-image {
          height: 100px;
        }

        .name {
          font-size: 0.75rem;
          -webkit-line-clamp: 2;
        }

        .price {
          font-size: 0.8rem;
        }
      }
    }

    .product-card {
      flex: 0 0 auto;
      min-width: 180px;
      max-width: 180px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      overflow: hidden;
      text-decoration: none;
      scroll-snap-align: start;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-primary);
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }
    }

    .product-image {
      position: relative;
      height: 140px;
      background: var(--color-bg-secondary);
      display: flex;
      align-items: center;
      justify-content: center;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        padding: 0.5rem;
      }

      .no-image {
        color: var(--color-text-secondary);

        svg {
          width: 48px;
          height: 48px;
        }
      }
    }

    .sale-badge {
      position: absolute;
      top: 0.5rem;
      left: 0.5rem;
      padding: 0.25rem 0.5rem;
      background: var(--color-danger);
      color: white;
      font-size: 0.625rem;
      font-weight: 700;
      text-transform: uppercase;
      border-radius: 4px;
    }

    .product-info {
      padding: 0.75rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .brand {
      font-size: 0.625rem;
      text-transform: uppercase;
      color: var(--color-text-secondary);
      letter-spacing: 0.5px;
    }

    .name {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
      line-height: 1.3;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .price {
      font-size: 0.9rem;
      font-weight: 600;
      margin-top: 0.25rem;

      .current {
        color: var(--color-text-primary);
      }

      .original {
        text-decoration: line-through;
        color: var(--color-text-secondary);
        font-size: 0.75rem;
        margin-right: 0.25rem;
      }

      .sale {
        color: var(--color-danger);
      }
    }

    @media (max-width: 768px) {
      .section-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }

      .product-card {
        min-width: 150px;
        max-width: 150px;
      }

      .product-image {
        height: 120px;
      }
    }
  `]
})
export class RecentlyViewedComponent {
  private readonly recentlyViewedService = inject(RecentlyViewedService);

  excludeProductId = input<string>('');
  maxItems = input<number>(8);
  showClearButton = input<boolean>(true);
  compact = input<boolean>(false);

  products = computed<RecentlyViewedProduct[]>(() => {
    const excludeId = this.excludeProductId();
    if (excludeId) {
      return this.recentlyViewedService.getProductsExcluding(excludeId, this.maxItems());
    }
    return this.recentlyViewedService.recentlyViewed().slice(0, this.maxItems());
  });

  clearHistory(): void {
    this.recentlyViewedService.clearAll();
  }
}
