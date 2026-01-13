import { Component, inject, input, signal, effect, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductBrief } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-consumables',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    @if (consumables().length > 0) {
      <section class="consumables-section" data-testid="consumables-section">
        <div class="section-header">
          <h3>{{ 'products.consumables.title' | translate }}</h3>
          <p class="subtitle">{{ 'products.consumables.subtitle' | translate }}</p>
        </div>

        <div class="consumables-grid">
          @for (product of consumables(); track product.id) {
            <div class="consumable-card" data-testid="consumable-card">
              <div class="product-image">
                <a [routerLink]="['/products', product.slug]">
                  @if (product.primaryImageUrl) {
                    <img [src]="product.primaryImageUrl" [alt]="product.name" loading="lazy" />
                  } @else {
                    <div class="no-image">{{ 'products.noImage' | translate }}</div>
                  }
                </a>
              </div>
              <div class="product-info">
                <a [routerLink]="['/products', product.slug]" class="product-name">
                  {{ product.name }}
                </a>
                <div class="product-price">
                  @if (product.isOnSale && product.salePrice) {
                    <span class="sale-price">{{ product.salePrice | currency:'EUR' }}</span>
                    <span class="original-price">{{ product.basePrice | currency:'EUR' }}</span>
                  } @else {
                    <span class="current-price">{{ product.basePrice | currency:'EUR' }}</span>
                  }
                </div>
              </div>
              <button
                class="add-btn"
                [class.added]="addedItems()[product.id]"
                [disabled]="!product.inStock || addingItems()[product.id]"
                (click)="addToCart(product)"
                data-testid="consumable-add-btn">
                @if (addingItems()[product.id]) {
                  <span class="spinner"></span>
                } @else if (addedItems()[product.id]) {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                  </svg>
                } @else {
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon">
                    <path d="M10 5a.75.75 0 01.75.75v4.5h4.5a.75.75 0 010 1.5h-4.5v4.5a.75.75 0 01-1.5 0v-4.5h-4.5a.75.75 0 010-1.5h4.5v-4.5A.75.75 0 0110 5z" />
                  </svg>
                }
                <span>{{ addedItems()[product.id] ? ('common.added' | translate) : ('common.add' | translate) }}</span>
              </button>
            </div>
          }
        </div>

        @if (consumables().length > 1) {
          <div class="add-all-section">
            <div class="total-info">
              <span class="label">{{ 'products.consumables.totalPrice' | translate }}:</span>
              <span class="total-price">{{ totalPrice() | currency:'EUR' }}</span>
            </div>
            <button
              class="btn-add-all"
              [disabled]="isAddingAll()"
              (click)="addAllToCart()"
              data-testid="add-all-consumables">
              @if (isAddingAll()) {
                {{ 'common.loading' | translate }}
              } @else {
                {{ 'products.consumables.addAll' | translate }}
              }
            </button>
          </div>
        }
      </section>
    }
  `,
  styles: [`
    .consumables-section {
      margin-top: 3rem;
      padding: 2rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
    }

    .section-header {
      margin-bottom: 1.5rem;

      h3 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin: 0;
      }
    }

    .consumables-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem;
    }

    .consumable-card {
      background: var(--color-bg-primary);
      border-radius: 8px;
      padding: 1rem;
      display: flex;
      flex-direction: column;
      transition: box-shadow 0.2s;

      &:hover {
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }
    }

    .product-image {
      aspect-ratio: 1;
      background: var(--color-bg-secondary);
      border-radius: 6px;
      overflow: hidden;
      margin-bottom: 0.75rem;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }

      .no-image {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-text-secondary);
        font-size: 0.75rem;
      }
    }

    .product-info {
      flex: 1;
      margin-bottom: 0.75rem;
    }

    .product-name {
      display: block;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
      text-decoration: none;
      line-height: 1.4;
      margin-bottom: 0.5rem;

      &:hover {
        color: var(--color-primary);
      }
    }

    .product-price {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;

      .current-price, .sale-price {
        font-weight: 600;
        color: var(--color-text-primary);
      }

      .sale-price {
        color: var(--color-error);
      }

      .original-price {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
        text-decoration: line-through;
      }
    }

    .add-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      .icon {
        width: 16px;
        height: 16px;
      }

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      &.added {
        background: var(--color-success, #22c55e);
      }

      .spinner {
        width: 16px;
        height: 16px;
        border: 2px solid transparent;
        border-top-color: currentColor;
        border-radius: 50%;
        animation: spin 0.6s linear infinite;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .add-all-section {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border);
    }

    .total-info {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;

      .label {
        color: var(--color-text-secondary);
      }

      .total-price {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }
    }

    .btn-add-all {
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    @media (max-width: 640px) {
      .consumables-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .add-all-section {
        flex-direction: column;
        gap: 1rem;
        text-align: center;
      }

      .btn-add-all {
        width: 100%;
      }
    }
  `]
})
export class ProductConsumablesComponent implements OnDestroy {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly destroy$ = new Subject<void>();

  productId = input.required<string>();

  consumables = signal<ProductBrief[]>([]);
  addingItems = signal<Record<string, boolean>>({});
  addedItems = signal<Record<string, boolean>>({});
  isAddingAll = signal(false);

  totalPrice = signal(0);

  constructor() {
    effect(() => {
      const id = this.productId();
      if (id) {
        this.loadConsumables(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadConsumables(productId: string): void {
    this.productService.getProductConsumables(productId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (products) => {
          this.consumables.set(products);
          this.calculateTotal(products);
        },
        error: (err) => console.error('Failed to load consumables:', err)
      });
  }

  private calculateTotal(products: ProductBrief[]): void {
    const total = products.reduce((sum, p) => {
      const price = p.isOnSale && p.salePrice ? p.salePrice : p.basePrice;
      return sum + price;
    }, 0);
    this.totalPrice.set(total);
  }

  addToCart(product: ProductBrief): void {
    if (this.addingItems()[product.id]) return;

    this.addingItems.update(items => ({ ...items, [product.id]: true }));

    this.cartService.addToCart(product.id, 1)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.addingItems.update(items => ({ ...items, [product.id]: false }));
          this.addedItems.update(items => ({ ...items, [product.id]: true }));

          setTimeout(() => {
            this.addedItems.update(items => ({ ...items, [product.id]: false }));
          }, 3000);
        },
        error: () => {
          this.addingItems.update(items => ({ ...items, [product.id]: false }));
        }
      });
  }

  addAllToCart(): void {
    if (this.isAddingAll()) return;

    this.isAddingAll.set(true);
    const products = this.consumables().filter(p => p.inStock);
    let completed = 0;

    products.forEach(product => {
      this.cartService.addToCart(product.id, 1)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.addedItems.update(items => ({ ...items, [product.id]: true }));
            completed++;
            if (completed === products.length) {
              this.isAddingAll.set(false);
            }
          },
          error: () => {
            completed++;
            if (completed === products.length) {
              this.isAddingAll.set(false);
            }
          }
        });
    });
  }
}
