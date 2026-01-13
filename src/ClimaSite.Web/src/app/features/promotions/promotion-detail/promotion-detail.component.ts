import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { PromotionService } from '../../../core/services/promotion.service';
import { CartService } from '../../../core/services/cart.service';
import { Promotion, PromotionType } from '../../../core/models/promotion.model';
import { ProductBrief } from '../../../core/models/product.model';

@Component({
  selector: 'app-promotion-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="promotion-detail-page">
      @if (isLoading()) {
        <div class="loading">{{ 'common.loading' | translate }}</div>
      } @else if (error()) {
        <div class="error">{{ error() }}</div>
      } @else if (promotion()) {
        <!-- Hero Banner -->
        <div class="promotion-hero" [style.background-image]="promotion()?.bannerImageUrl ? 'url(' + promotion()?.bannerImageUrl + ')' : ''">
          <div class="hero-overlay">
            <div class="hero-content">
              <span class="discount-badge">{{ getDiscountText() }}</span>
              <h1>{{ promotion()?.name }}</h1>
              @if (promotion()?.description) {
                <p class="description">{{ promotion()?.description }}</p>
              }
              <div class="promo-meta">
                @if (promotion()?.code) {
                  <div class="promo-code">
                    <span class="label">{{ 'promotions.useCode' | translate }}:</span>
                    <span class="code" (click)="copyCode()">{{ promotion()?.code }}</span>
                    @if (copied()) {
                      <span class="copied-badge">✓</span>
                    }
                  </div>
                }
                <div class="validity">
                  {{ 'promotions.activeUntil' | translate }}: {{ promotion()?.endDate | date:'longDate' }}
                </div>
                @if (promotion()?.minimumOrderAmount) {
                  <div class="min-order">
                    {{ 'promotions.minOrder' | translate:{ amount: promotion()?.minimumOrderAmount | currency:'EUR' } }}
                  </div>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Breadcrumb -->
        <nav class="breadcrumb">
          <a routerLink="/">{{ 'nav.home' | translate }}</a>
          <span>/</span>
          <a routerLink="/promotions">{{ 'nav.promotions' | translate }}</a>
          <span>/</span>
          <span>{{ promotion()?.name }}</span>
        </nav>

        <!-- Products -->
        <div class="products-section">
          <h2>{{ 'promotions.viewProducts' | translate }} ({{ promotion()?.products?.length }})</h2>

          @if (promotion()?.products?.length === 0) {
            <div class="no-products">
              <p>{{ 'products.noProducts' | translate }}</p>
            </div>
          } @else {
            <div class="products-grid">
              @for (product of promotion()?.products; track product.id) {
                <div class="product-card">
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
                      <h3 class="product-name">{{ product.name }}</h3>
                      <div class="product-price">
                        @if (product.isOnSale && product.salePrice) {
                          <span class="sale-price">{{ product.salePrice | currency:'EUR' }}</span>
                          <span class="original-price">{{ product.basePrice | currency:'EUR' }}</span>
                        } @else {
                          <span class="current-price">{{ product.basePrice | currency:'EUR' }}</span>
                        }
                      </div>
                    </div>
                  </a>
                  <button
                    class="btn-add-to-cart"
                    [class.added]="addedItems()[product.id]"
                    [disabled]="!product.inStock || addingItems()[product.id]"
                    (click)="addToCart(product)">
                    @if (addingItems()[product.id]) {
                      <span class="spinner"></span>
                    } @else if (addedItems()[product.id]) {
                      ✓ {{ 'common.added' | translate }}
                    } @else if (!product.inStock) {
                      {{ 'products.outOfStock' | translate }}
                    } @else {
                      {{ 'products.details.addToCart' | translate }}
                    }
                  </button>
                </div>
              }
            </div>
          }
        </div>

        <!-- Terms -->
        @if (promotion()?.termsAndConditions) {
          <div class="terms-section">
            <h3>{{ 'promotions.termsApply' | translate }}</h3>
            <p>{{ promotion()?.termsAndConditions }}</p>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .promotion-detail-page {
      max-width: 1200px;
      margin: 0 auto;
    }

    .loading, .error {
      text-align: center;
      padding: 4rem 2rem;
      color: var(--color-text-secondary);
    }

    .error {
      color: var(--color-error);
    }

    .promotion-hero {
      position: relative;
      min-height: 300px;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      background-size: cover;
      background-position: center;
      border-radius: 0 0 24px 24px;
      overflow: hidden;
    }

    .hero-overlay {
      position: absolute;
      inset: 0;
      background: linear-gradient(to right, rgba(0,0,0,0.8), rgba(0,0,0,0.3));
      display: flex;
      align-items: center;
      padding: 3rem;
    }

    .hero-content {
      max-width: 600px;
      color: white;

      .discount-badge {
        display: inline-block;
        padding: 0.5rem 1rem;
        background: var(--color-error);
        border-radius: 20px;
        font-weight: 700;
        font-size: 1.25rem;
        margin-bottom: 1rem;
      }

      h1 {
        font-size: 2.5rem;
        font-weight: 700;
        margin: 0 0 1rem;
      }

      .description {
        font-size: 1.125rem;
        opacity: 0.9;
        line-height: 1.6;
        margin: 0 0 1.5rem;
      }
    }

    .promo-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;
    }

    .promo-code {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      .label {
        opacity: 0.8;
      }

      .code {
        background: white;
        color: var(--color-primary);
        padding: 0.5rem 1rem;
        border-radius: 6px;
        font-family: monospace;
        font-weight: 700;
        cursor: pointer;

        &:hover {
          background: var(--color-bg-secondary);
        }
      }

      .copied-badge {
        color: #22c55e;
        font-weight: 700;
      }
    }

    .validity, .min-order {
      opacity: 0.8;
      font-size: 0.875rem;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1.5rem 2rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);

      a {
        color: var(--color-text-secondary);
        text-decoration: none;

        &:hover {
          color: var(--color-primary);
        }
      }
    }

    .products-section {
      padding: 2rem;

      h2 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 2rem;
      }
    }

    .no-products {
      text-align: center;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .products-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .product-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;
      transition: all 0.2s;

      &:hover {
        box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
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
        font-size: 0.875rem;
      }

      .sale-badge {
        position: absolute;
        top: 0.75rem;
        left: 0.75rem;
        padding: 0.25rem 0.75rem;
        background: var(--color-error);
        color: white;
        font-size: 0.875rem;
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
      font-size: 1rem;
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
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        text-decoration: line-through;
      }
    }

    .btn-add-to-cart {
      width: calc(100% - 2rem);
      margin: 0 1rem 1rem;
      padding: 0.75rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
        background: var(--color-text-secondary);
      }

      &.added {
        background: var(--color-success, #22c55e);
      }

      .spinner {
        display: inline-block;
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

    .terms-section {
      padding: 2rem;
      margin: 2rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;

      h3 {
        font-size: 1rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 1rem;
      }

      p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        line-height: 1.6;
        margin: 0;
      }
    }

    @media (max-width: 768px) {
      .hero-content h1 {
        font-size: 1.75rem;
      }

      .hero-overlay {
        padding: 2rem;
      }

      .products-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }
    }
  `]
})
export class PromotionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly promotionService = inject(PromotionService);
  private readonly cartService = inject(CartService);

  promotion = signal<Promotion | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  copied = signal(false);
  addingItems = signal<Record<string, boolean>>({});
  addedItems = signal<Record<string, boolean>>({});

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      this.loadPromotion(slug);
    } else {
      this.error.set('Promotion not found');
      this.isLoading.set(false);
    }
  }

  private loadPromotion(slug: string): void {
    this.isLoading.set(true);
    this.promotionService.getPromotionBySlug(slug).subscribe({
      next: (promotion) => {
        this.promotion.set(promotion);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load promotion:', err);
        this.error.set('Promotion not found');
        this.isLoading.set(false);
      }
    });
  }

  getDiscountText(): string {
    const promo = this.promotion();
    if (!promo) return '';

    switch (promo.type) {
      case PromotionType.Percentage:
        return `-${promo.discountValue}%`;
      case PromotionType.FixedAmount:
        return `-€${promo.discountValue}`;
      case PromotionType.FreeShipping:
        return 'Free Shipping';
      case PromotionType.BuyOneGetOne:
        return 'BOGO';
      default:
        return 'SALE';
    }
  }

  copyCode(): void {
    const code = this.promotion()?.code;
    if (code) {
      navigator.clipboard.writeText(code).then(() => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 2000);
      });
    }
  }

  addToCart(product: ProductBrief): void {
    if (this.addingItems()[product.id]) return;

    this.addingItems.update(items => ({ ...items, [product.id]: true }));

    this.cartService.addToCart(product.id, 1).subscribe({
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
