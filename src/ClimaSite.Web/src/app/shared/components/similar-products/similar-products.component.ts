import { Component, inject, input, signal, effect, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductBrief } from '../../../core/models/product.model';

@Component({
  selector: 'app-similar-products',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    @if (products().length > 0) {
      <section class="similar-products-section" data-testid="similar-products-section">
        <div class="section-header">
          <h3>{{ 'products.similar.title' | translate }}</h3>
        </div>

        <div class="carousel-container">
          <button
            class="nav-btn prev"
            [disabled]="!canScrollLeft()"
            (click)="scrollLeft()"
            aria-label="Previous">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd" />
            </svg>
          </button>

          <div class="products-carousel" #carousel (scroll)="onScroll()">
            @for (product of products(); track product.id) {
              <div class="product-card" data-testid="similar-product-card">
                <a [routerLink]="['/products', product.slug]" class="product-link">
                  <div class="product-image">
                    @if (product.isOnSale) {
                      <span class="sale-badge">-{{ product.discountPercentage }}%</span>
                    }
                    @if (product.primaryImageUrl) {
                      <img [src]="product.primaryImageUrl" [alt]="product.name" loading="lazy" />
                    } @else {
                      <div class="no-image">{{ 'products.noImage' | translate }}</div>
                    }
                  </div>
                  <div class="product-info">
                    @if (product.brand) {
                      <span class="product-brand">{{ product.brand }}</span>
                    }
                    <h4 class="product-name">{{ product.name }}</h4>
                    <div class="product-price">
                      @if (product.isOnSale && product.salePrice) {
                        <span class="original-price">{{ product.salePrice | currency:'EUR' }}</span>
                        <span class="sale-price">{{ product.basePrice | currency:'EUR' }}</span>
                      } @else {
                        <span class="current-price">{{ product.basePrice | currency:'EUR' }}</span>
                      }
                    </div>
                  </div>
                </a>
                <button
                  class="add-to-cart-btn"
                  [class.added]="addedItems()[product.id]"
                  [disabled]="!product.inStock || addingItems()[product.id]"
                  (click)="addToCart(product, $event)">
                  @if (addingItems()[product.id]) {
                    <span class="spinner"></span>
                  } @else if (addedItems()[product.id]) {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                    </svg>
                  } @else if (!product.inStock) {
                    <span>{{ 'products.outOfStock' | translate }}</span>
                  } @else {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M1 1.75A.75.75 0 011.75 1h1.628a1.75 1.75 0 011.734 1.51L5.18 3a65.25 65.25 0 0113.36 1.412.75.75 0 01.58.875 48.645 48.645 0 01-1.618 6.2.75.75 0 01-.712.513H6a2.503 2.503 0 00-2.292 1.5H17.25a.75.75 0 010 1.5H2.76a.75.75 0 01-.748-.807 4.002 4.002 0 012.716-3.486L3.626 2.716a.25.25 0 00-.248-.216H1.75A.75.75 0 011 1.75zM6 17.5a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM15.5 19a1.5 1.5 0 100-3 1.5 1.5 0 000 3z" />
                    </svg>
                  }
                </button>
              </div>
            }
          </div>

          <button
            class="nav-btn next"
            [disabled]="!canScrollRight()"
            (click)="scrollRight()"
            aria-label="Next">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </section>
    }
  `,
  styles: [`
    .similar-products-section {
      margin-top: 3rem;
    }

    .section-header {
      margin-bottom: 1.5rem;

      h3 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0;
      }
    }

    .carousel-container {
      position: relative;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .nav-btn {
      flex-shrink: 0;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border: 1px solid var(--color-border);
      background: var(--color-bg-primary);
      color: var(--color-text-primary);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;

      svg {
        width: 20px;
        height: 20px;
      }

      &:hover:not(:disabled) {
        background: var(--color-bg-secondary);
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }
    }

    .products-carousel {
      display: flex;
      gap: 1rem;
      overflow-x: auto;
      scroll-behavior: smooth;
      scrollbar-width: none;
      -ms-overflow-style: none;
      flex: 1;
      padding: 0.5rem 0;

      &::-webkit-scrollbar {
        display: none;
      }
    }

    .product-card {
      flex-shrink: 0;
      width: 220px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;
      transition: all 0.2s;
      position: relative;

      &:hover {
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        border-color: var(--color-primary);
      }
    }

    .product-link {
      display: block;
      text-decoration: none;
      color: inherit;
    }

    .product-image {
      position: relative;
      aspect-ratio: 1;
      background: var(--color-bg-secondary);
      overflow: hidden;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        transition: transform 0.3s;
      }

      .product-card:hover & img {
        transform: scale(1.05);
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

      .sale-badge {
        position: absolute;
        top: 0.5rem;
        left: 0.5rem;
        padding: 0.25rem 0.5rem;
        background: var(--color-error);
        color: white;
        font-size: 0.75rem;
        font-weight: 600;
        border-radius: 4px;
      }
    }

    .product-info {
      padding: 1rem;
    }

    .product-brand {
      display: block;
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0.25rem;
    }

    .product-name {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
      margin: 0 0 0.5rem;
      line-height: 1.4;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .product-price {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;

      .current-price, .sale-price {
        font-weight: 700;
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

    .add-to-cart-btn {
      position: absolute;
      bottom: 1rem;
      right: 1rem;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      border: none;
      background: var(--color-primary);
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;
      opacity: 0;

      svg {
        width: 18px;
        height: 18px;
      }

      .product-card:hover & {
        opacity: 1;
      }

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
        transform: scale(1.1);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
        background: var(--color-text-secondary);
      }

      &.added {
        background: var(--color-success, #22c55e);
        opacity: 1;
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

    @media (max-width: 768px) {
      .nav-btn {
        display: none;
      }

      .products-carousel {
        scroll-snap-type: x mandatory;
        -webkit-overflow-scrolling: touch;
      }

      .product-card {
        width: 160px;
        scroll-snap-align: start;

        .add-to-cart-btn {
          opacity: 1;
        }
      }
    }
  `]
})
export class SimilarProductsComponent implements OnDestroy {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly destroy$ = new Subject<void>();

  @ViewChild('carousel') carousel!: ElementRef<HTMLDivElement>;

  productId = input.required<string>();

  products = signal<ProductBrief[]>([]);
  addingItems = signal<Record<string, boolean>>({});
  addedItems = signal<Record<string, boolean>>({});
  canScrollLeft = signal(false);
  canScrollRight = signal(true);

  constructor() {
    effect(() => {
      const id = this.productId();
      if (id) {
        this.loadSimilarProducts(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadSimilarProducts(productId: string): void {
    this.productService.getSimilarProducts(productId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (products) => {
          this.products.set(products);
          setTimeout(() => this.updateScrollButtons(), 100);
        },
        error: (err) => console.error('Failed to load similar products:', err)
      });
  }

  onScroll(): void {
    this.updateScrollButtons();
  }

  private updateScrollButtons(): void {
    if (!this.carousel?.nativeElement) return;

    const el = this.carousel.nativeElement;
    this.canScrollLeft.set(el.scrollLeft > 0);
    this.canScrollRight.set(el.scrollLeft < el.scrollWidth - el.clientWidth - 10);
  }

  scrollLeft(): void {
    if (this.carousel?.nativeElement) {
      this.carousel.nativeElement.scrollBy({ left: -240, behavior: 'smooth' });
    }
  }

  scrollRight(): void {
    if (this.carousel?.nativeElement) {
      this.carousel.nativeElement.scrollBy({ left: 240, behavior: 'smooth' });
    }
  }

  addToCart(product: ProductBrief, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

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
}
