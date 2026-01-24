import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { BrandService } from '../../../core/services/brand.service';
import { CartService } from '../../../core/services/cart.service';
import { Brand } from '../../../core/models/brand.model';
import { ProductBrief } from '../../../core/models/product.model';
import { ParallaxDirective } from '../../../shared/directives/parallax.directive';

@Component({
  selector: 'app-brand-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, ParallaxDirective],
  template: `
    <div class="brand-detail-page">
      @if (isLoading()) {
        <div class="loading">{{ 'common.loading' | translate }}</div>
      } @else if (error()) {
        <div class="error">{{ error() }}</div>
      } @else if (brand()) {
<!-- Hero Banner with Parallax -->
        <div class="brand-hero">
          <div 
            class="hero-bg" 
            [style.background-image]="brand()?.bannerImageUrl ? 'url(' + brand()?.bannerImageUrl + ')' : ''"
            appParallax 
            [speed]="0.2" 
            [direction]="'down'" 
            [scaleOnScroll]="1.1"
          ></div>
          <div class="hero-overlay">
            <div class="hero-content">
              @if (brand()?.logoUrl) {
                <img [src]="brand()?.logoUrl" [alt]="brand()?.name" class="brand-logo" loading="eager" fetchpriority="high" />
              } @else {
                <div class="logo-placeholder">{{ brand()?.name?.charAt(0) }}</div>
              }
              <h1>{{ brand()?.name }}</h1>
              @if (brand()?.description) {
                <p class="description">{{ brand()?.description }}</p>
              }
              <div class="brand-meta">
                @if (brand()?.countryOfOrigin) {
                  <span class="meta-item">
                    <span class="label">{{ 'brands.countryOfOrigin' | translate }}:</span>
                    {{ brand()?.countryOfOrigin }}
                  </span>
                }
                @if (brand()?.foundedYear && brand()!.foundedYear > 0) {
                  <span class="meta-item">
                    <span class="label">{{ 'brands.foundedYear' | translate }}:</span>
                    {{ brand()?.foundedYear }}
                  </span>
                }
                @if (brand()?.websiteUrl) {
                  <a [href]="brand()?.websiteUrl" target="_blank" rel="noopener" class="website-link">
                    {{ 'brands.visitWebsite' | translate }}
                  </a>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Breadcrumb -->
        <nav class="breadcrumb">
          <a routerLink="/">{{ 'nav.home' | translate }}</a>
          <span>/</span>
          <a routerLink="/brands">{{ 'nav.brands' | translate }}</a>
          <span>/</span>
          <span>{{ brand()?.name }}</span>
        </nav>

        <!-- Products -->
        <div class="products-section">
          <h2>{{ 'brands.products' | translate }} ({{ brand()?.productCount }})</h2>

          @if (brand()?.products?.length === 0) {
            <div class="no-products">
              <p>{{ 'products.noProducts' | translate }}</p>
            </div>
          } @else {
            <div class="products-grid">
              @for (product of brand()?.products; track product.id) {
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
                      {{ 'common.added' | translate }}
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
      }
    </div>
  `,
  styles: [`
    .brand-detail-page {
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

.brand-hero {
      position: relative;
      min-height: 300px;
      border-radius: 0 0 24px 24px;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      inset: -20%;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      background-size: cover;
      background-position: center;
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

      .brand-logo {
        max-width: 150px;
        max-height: 80px;
        object-fit: contain;
        margin-bottom: 1.5rem;
        background: white;
        padding: 0.5rem 1rem;
        border-radius: 8px;
      }

      .logo-placeholder {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: white;
        color: var(--color-primary);
        font-size: 2.5rem;
        font-weight: 700;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 1.5rem;
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

    .brand-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 1.5rem;
      align-items: center;

      .meta-item {
        font-size: 0.875rem;
        opacity: 0.9;

        .label {
          opacity: 0.7;
          margin-right: 0.25rem;
        }
      }

      .website-link {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        background: white;
        color: var(--color-primary);
        border-radius: 6px;
        font-size: 0.875rem;
        font-weight: 600;
        text-decoration: none;
        transition: all 0.2s;

        &:hover {
          background: var(--color-bg-secondary);
        }
      }
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
export class BrandDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly brandService = inject(BrandService);
  private readonly cartService = inject(CartService);

  brand = signal<Brand | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  addingItems = signal<Record<string, boolean>>({});
  addedItems = signal<Record<string, boolean>>({});

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      this.loadBrand(slug);
    } else {
      this.error.set('Brand not found');
      this.isLoading.set(false);
    }
  }

  private loadBrand(slug: string): void {
    this.isLoading.set(true);
    this.brandService.getBrandBySlug(slug).subscribe({
      next: (brand) => {
        this.brand.set(brand);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load brand:', err);
        this.error.set('Brand not found');
        this.isLoading.set(false);
      }
    });
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
