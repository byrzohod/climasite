import { Component, input, output, computed, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

export interface BundleProduct {
  id: string;
  name: string;
  slug: string;
  imageUrl: string;
  price: number;
  salePrice?: number;
  selected?: boolean;
}

@Component({
  selector: 'app-frequently-bought',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="frequently-bought" data-testid="frequently-bought">
      <h3 class="section-title">{{ 'products.frequentlyBought.title' | translate }}</h3>
      <p class="section-subtitle">{{ 'products.frequentlyBought.subtitle' | translate }}</p>

      <div class="bundle-container">
        <!-- Main Product (always included) -->
        <div class="bundle-product main-product">
          <div class="product-image">
            <img [src]="mainProduct().imageUrl" [alt]="mainProduct().name" loading="lazy" />
          </div>
          <div class="product-info">
            <span class="product-name">{{ mainProduct().name }}</span>
            <span class="product-price">
              @if (mainProduct().salePrice && mainProduct().salePrice! < mainProduct().price) {
                <span class="original">{{ mainProduct().price | currency:'EUR' }}</span>
                <span class="sale">{{ mainProduct().salePrice | currency:'EUR' }}</span>
              } @else {
                {{ mainProduct().price | currency:'EUR' }}
              }
            </span>
          </div>
          <span class="included-badge">{{ 'products.frequentlyBought.included' | translate }}</span>
        </div>

        <!-- Plus sign -->
        <div class="plus-sign">+</div>

        <!-- Bundle Products -->
        @for (product of bundleProducts(); track product.id; let idx = $index) {
          <div class="bundle-product"
               [class.selected]="isSelected(product.id)"
               (click)="toggleProduct(product.id)"
               [attr.data-testid]="'bundle-product-' + product.id">
            <div class="checkbox">
              @if (isSelected(product.id)) {
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
                  <polyline points="20 6 9 17 4 12"/>
                </svg>
              }
            </div>
            <div class="product-image">
              <img [src]="product.imageUrl" [alt]="product.name" loading="lazy" />
            </div>
            <div class="product-info">
              <a [routerLink]="['/products', product.slug]" class="product-name">{{ product.name }}</a>
              <span class="product-price">
                @if (product.salePrice && product.salePrice < product.price) {
                  <span class="original">{{ product.price | currency:'EUR' }}</span>
                  <span class="sale">{{ product.salePrice | currency:'EUR' }}</span>
                } @else {
                  {{ product.price | currency:'EUR' }}
                }
              </span>
            </div>

            @if (idx < bundleProducts().length - 1) {
              <div class="plus-sign-small">+</div>
            }
          </div>
        }
      </div>

      <!-- Bundle Summary -->
      <div class="bundle-summary">
        <div class="summary-row">
          <span class="label">{{ 'products.frequentlyBought.totalItems' | translate }}:</span>
          <span class="value">{{ selectedCount() + 1 }} {{ 'common.items' | translate }}</span>
        </div>

        @if (bundleSavings() > 0) {
          <div class="summary-row savings">
            <span class="label">{{ 'products.frequentlyBought.bundleSavings' | translate }}:</span>
            <span class="value">-{{ bundleSavings() | currency:'EUR' }}</span>
          </div>
        }

        <div class="summary-row total">
          <span class="label">{{ 'products.frequentlyBought.bundlePrice' | translate }}:</span>
          <span class="value">{{ bundleTotal() | currency:'EUR' }}</span>
        </div>

        <button
          class="btn-add-bundle"
          [disabled]="isAddingToCart()"
          (click)="addBundleToCart()"
          data-testid="add-bundle-to-cart">
          @if (isAddingToCart()) {
            {{ 'common.loading' | translate }}
          } @else {
            {{ 'products.frequentlyBought.addBundle' | translate }}
          }
        </button>
      </div>
    </div>
  `,
  styles: [`
    .frequently-bought {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      border: 1px solid var(--color-border);
    }

    .section-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0 0 0.25rem;
    }

    .section-subtitle {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin: 0 0 1.5rem;
    }

    .bundle-container {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin-bottom: 1.5rem;
    }

    .bundle-product {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 1rem;
      background: var(--color-bg-primary);
      border: 2px solid var(--color-border);
      border-radius: 8px;
      min-width: 140px;
      max-width: 160px;
      cursor: pointer;
      transition: all 0.2s;
      position: relative;

      &:hover:not(.main-product) {
        border-color: var(--color-primary-light);
      }

      &.selected {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.05);
      }

      &.main-product {
        cursor: default;
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.05);
      }
    }

    .checkbox {
      position: absolute;
      top: 0.5rem;
      right: 0.5rem;
      width: 24px;
      height: 24px;
      border: 2px solid var(--color-border);
      border-radius: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--color-bg-primary);
      transition: all 0.2s;

      svg {
        width: 16px;
        height: 16px;
        color: var(--color-primary);
      }

      .selected & {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.1);
      }
    }

    .included-badge {
      position: absolute;
      top: 0.5rem;
      right: 0.5rem;
      font-size: 0.625rem;
      padding: 0.25rem 0.5rem;
      background: var(--color-primary);
      color: white;
      border-radius: 4px;
      font-weight: 600;
      text-transform: uppercase;
    }

    .product-image {
      width: 80px;
      height: 80px;
      margin-bottom: 0.5rem;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
    }

    .product-info {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 0.25rem;
    }

    .product-name {
      font-size: 0.75rem;
      font-weight: 500;
      color: var(--color-text-primary);
      text-decoration: none;
      line-height: 1.3;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;

      &:hover {
        color: var(--color-primary);
      }
    }

    .product-price {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-primary);

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

    .plus-sign {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--color-text-secondary);
      padding: 0 0.5rem;
    }

    .plus-sign-small {
      position: absolute;
      right: -18px;
      top: 50%;
      transform: translateY(-50%);
      font-size: 1rem;
      font-weight: 700;
      color: var(--color-text-secondary);
      z-index: 1;
    }

    .bundle-summary {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border);
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.875rem;

      .label {
        color: var(--color-text-secondary);
      }

      .value {
        font-weight: 600;
        color: var(--color-text-primary);
      }

      &.savings {
        .value {
          color: var(--color-success);
        }
      }

      &.total {
        font-size: 1rem;
        padding-top: 0.5rem;
        border-top: 1px dashed var(--color-border);

        .value {
          font-size: 1.25rem;
          color: var(--color-primary);
        }
      }
    }

    .btn-add-bundle {
      width: 100%;
      padding: 1rem;
      margin-top: 0.75rem;
      border: none;
      border-radius: 8px;
      background: var(--color-primary);
      color: white;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    @media (max-width: 768px) {
      .bundle-container {
        flex-direction: column;
      }

      .bundle-product {
        width: 100%;
        max-width: none;
        flex-direction: row;
        gap: 1rem;
        padding: 0.75rem;
      }

      .product-image {
        width: 60px;
        height: 60px;
        margin-bottom: 0;
      }

      .product-info {
        align-items: flex-start;
        text-align: left;
        flex: 1;
      }

      .plus-sign {
        display: none;
      }

      .plus-sign-small {
        display: none;
      }

      .checkbox {
        position: static;
        order: -1;
      }

      .included-badge {
        position: static;
        order: -1;
        margin-right: auto;
      }
    }
  `]
})
export class FrequentlyBoughtComponent {
  mainProduct = input.required<BundleProduct>();
  bundleProducts = input.required<BundleProduct[]>();
  bundleDiscount = input<number>(10); // Percentage discount for bundle

  addToCart = output<string[]>();

  private selectedProductIds = signal<Set<string>>(new Set());
  isAddingToCart = signal(false);
  private initialized = false;

  constructor() {
    // Initialize with all products selected by default using effect
    effect(() => {
      const products = this.bundleProducts();
      if (products.length > 0 && !this.initialized) {
        this.initialized = true;
        const ids = new Set(products.map(p => p.id));
        this.selectedProductIds.set(ids);
      }
    });
  }

  isSelected(productId: string): boolean {
    return this.selectedProductIds().has(productId);
  }

  toggleProduct(productId: string): void {
    const current = this.selectedProductIds();
    const updated = new Set(current);

    if (updated.has(productId)) {
      updated.delete(productId);
    } else {
      updated.add(productId);
    }

    this.selectedProductIds.set(updated);
  }

  selectedCount = computed(() => this.selectedProductIds().size);

  bundleTotal = computed(() => {
    const main = this.mainProduct();
    const mainPrice = main.salePrice && main.salePrice < main.price ? main.salePrice : main.price;

    const bundlePrice = this.bundleProducts()
      .filter(p => this.isSelected(p.id))
      .reduce((sum, p) => {
        const price = p.salePrice && p.salePrice < p.price ? p.salePrice : p.price;
        return sum + price;
      }, 0);

    const subtotal = mainPrice + bundlePrice;

    // Apply bundle discount only if there are selected bundle products
    if (this.selectedCount() > 0) {
      return subtotal * (1 - this.bundleDiscount() / 100);
    }

    return mainPrice;
  });

  bundleSavings = computed(() => {
    if (this.selectedCount() === 0) return 0;

    const main = this.mainProduct();
    const mainPrice = main.salePrice && main.salePrice < main.price ? main.salePrice : main.price;

    const bundlePrice = this.bundleProducts()
      .filter(p => this.isSelected(p.id))
      .reduce((sum, p) => {
        const price = p.salePrice && p.salePrice < p.price ? p.salePrice : p.price;
        return sum + price;
      }, 0);

    const subtotal = mainPrice + bundlePrice;
    return subtotal * (this.bundleDiscount() / 100);
  });

  addBundleToCart(): void {
    this.isAddingToCart.set(true);

    const productIds = [
      this.mainProduct().id,
      ...this.bundleProducts()
        .filter(p => this.isSelected(p.id))
        .map(p => p.id)
    ];

    this.addToCart.emit(productIds);

    // Reset after a delay (parent should handle actual cart logic)
    setTimeout(() => {
      this.isAddingToCart.set(false);
    }, 1000);
  }
}
