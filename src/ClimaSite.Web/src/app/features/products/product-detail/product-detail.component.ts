import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule],
  template: `
    <div class="product-detail-container" data-testid="product-detail">
      @if (isLoading()) {
        <div class="loading" data-testid="loading">
          {{ 'common.loading' | translate }}
        </div>
      } @else if (error()) {
        <div class="error" data-testid="error">
          {{ error() }}
        </div>
      } @else if (product()) {
        <div class="product-content">
          <!-- Breadcrumb -->
          <nav class="breadcrumb" data-testid="breadcrumb">
            <a routerLink="/">{{ 'nav.home' | translate }}</a>
            <span>/</span>
            <a routerLink="/products">{{ 'nav.products' | translate }}</a>
            @if (product()?.category) {
              <span>/</span>
              <a [routerLink]="['/products']" [queryParams]="{category: product()?.category?.slug}">
                {{ product()?.category?.name }}
              </a>
            }
            <span>/</span>
            <span>{{ product()?.name }}</span>
          </nav>

          <div class="product-main">
            <!-- Image Gallery -->
            <div class="product-gallery">
              <div class="main-image">
                @if (product()?.images?.length) {
                  <img [src]="selectedImage() || product()?.images?.[0]?.url" [alt]="product()?.name" />
                } @else {
                  <div class="no-image">{{ 'products.noImage' | translate }}</div>
                }
              </div>
              @if (product()?.images && product()!.images.length > 1) {
                <div class="thumbnail-list">
                  @for (image of product()?.images; track image.id) {
                    <button
                      class="thumbnail"
                      [class.active]="selectedImage() === image.url"
                      (click)="selectImage(image.url)"
                    >
                      <img [src]="image.url" [alt]="image.altText || product()?.name" />
                    </button>
                  }
                </div>
              }
            </div>

            <!-- Product Info -->
            <div class="product-info">
              @if (product()?.brand) {
                <span class="product-brand">{{ product()?.brand }}</span>
              }

              <h1 class="product-title" data-testid="product-title">{{ product()?.name }}</h1>

              @if (product()?.reviewCount && product()!.reviewCount > 0) {
                <div class="product-rating">
                  <div class="stars">
                    @for (star of [1,2,3,4,5]; track star) {
                      <span class="star" [class.filled]="star <= Math.floor(product()?.averageRating || 0)">★</span>
                    }
                  </div>
                  <span class="rating-text">
                    {{ product()?.averageRating | number:'1.1-1' }} ({{ product()?.reviewCount }} {{ 'products.details.reviews' | translate }})
                  </span>
                </div>
              }

              <div class="product-price" data-testid="product-price">
                @if (product()?.isOnSale && product()?.salePrice) {
                  <span class="original-price">{{ product()?.basePrice | currency:'EUR' }}</span>
                  <span class="sale-price">{{ product()?.salePrice | currency:'EUR' }}</span>
                  <span class="discount-badge">-{{ product()?.discountPercentage }}%</span>
                } @else {
                  <span class="current-price">{{ product()?.basePrice | currency:'EUR' }}</span>
                }
              </div>

              @if (product()?.shortDescription) {
                <p class="product-description">{{ product()?.shortDescription }}</p>
              }

              <div class="product-meta">
                @if (product()?.sku) {
                  <div class="meta-item">
                    <span class="meta-label">{{ 'products.details.sku' | translate }}:</span>
                    <span class="meta-value">{{ product()?.sku }}</span>
                  </div>
                }
                @if (product()?.model) {
                  <div class="meta-item">
                    <span class="meta-label">{{ 'products.details.model' | translate }}:</span>
                    <span class="meta-value">{{ product()?.model }}</span>
                  </div>
                }
                @if (product()?.warrantyMonths) {
                  <div class="meta-item">
                    <span class="meta-label">{{ 'products.details.warranty' | translate }}:</span>
                    <span class="meta-value">{{ product()?.warrantyMonths }} {{ 'products.details.months' | translate }}</span>
                  </div>
                }
              </div>

              <!-- Add to Cart Section -->
              <div class="add-to-cart-section">
                <div class="quantity-wrapper" data-testid="quantity-input">
                  <label>{{ 'products.details.quantity' | translate }}:</label>
                  <div class="quantity-controls">
                    <button type="button" (click)="decreaseQuantity()" [disabled]="quantity() <= 1">−</button>
                    <input
                      type="number"
                      [value]="quantity()"
                      (change)="onQuantityChange($event)"
                      min="1"
                      max="99"
                    />
                    <button type="button" (click)="increaseQuantity()">+</button>
                  </div>
                </div>

                <button
                  class="btn-add-to-cart"
                  [class.added]="addedToCart()"
                  [disabled]="isAddingToCart()"
                  (click)="addToCart()"
                  data-testid="add-to-cart"
                >
                  @if (isAddingToCart()) {
                    {{ 'common.loading' | translate }}
                  } @else if (addedToCart()) {
                    ✓ {{ 'cart.item.added' | translate }}
                  } @else {
                    {{ 'products.details.addToCart' | translate }}
                  }
                </button>

                <button class="btn-wishlist" data-testid="add-to-wishlist">
                  ♡ {{ 'products.details.addToWishlist' | translate }}
                </button>
              </div>

              <!-- Cart Notification -->
              @if (showNotification()) {
                <div class="cart-notification" data-testid="cart-notification">
                  ✓ {{ 'cart.item.added' | translate }}
                </div>
              }
            </div>
          </div>

          <!-- Product Details Tabs -->
          <div class="product-tabs">
            <div class="tab-headers">
              <button
                class="tab-header"
                [class.active]="activeTab() === 'description'"
                (click)="setActiveTab('description')"
              >
                {{ 'products.details.description' | translate }}
              </button>
              <button
                class="tab-header"
                [class.active]="activeTab() === 'specifications'"
                (click)="setActiveTab('specifications')"
              >
                {{ 'products.details.specifications' | translate }}
              </button>
              <button
                class="tab-header"
                [class.active]="activeTab() === 'reviews'"
                (click)="setActiveTab('reviews')"
              >
                {{ 'products.details.reviews' | translate }} ({{ product()?.reviewCount || 0 }})
              </button>
            </div>

            <div class="tab-content">
              @if (activeTab() === 'description') {
                <div class="tab-panel">
                  @if (product()?.description) {
                    <p>{{ product()?.description }}</p>
                  } @else {
                    <p>{{ product()?.shortDescription || ('products.details.noDescription' | translate) }}</p>
                  }

                  @if (product()?.features) {
                    <h4>{{ 'products.details.features' | translate }}</h4>
                    <ul class="features-list">
                      @for (feature of getFeatures(); track $index) {
                        <li>{{ feature }}</li>
                      }
                    </ul>
                  }
                </div>
              }

              @if (activeTab() === 'specifications') {
                <div class="tab-panel">
                  @if (product()?.specifications) {
                    <table class="specs-table">
                      <tbody>
                        @for (spec of getSpecifications(); track spec.key) {
                          <tr>
                            <th>{{ spec.key }}</th>
                            <td>{{ spec.value }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  } @else {
                    <p>{{ 'products.details.noSpecifications' | translate }}</p>
                  }
                </div>
              }

              @if (activeTab() === 'reviews') {
                <div class="tab-panel">
                  <p>{{ 'products.details.reviewsComingSoon' | translate }}</p>
                </div>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .product-detail-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .loading, .error {
      text-align: center;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .error {
      color: var(--color-error);
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 2rem;
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

    .product-main {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
      margin-bottom: 3rem;
    }

    .product-gallery {
      .main-image {
        aspect-ratio: 1;
        background: var(--color-bg-secondary);
        border-radius: 12px;
        overflow: hidden;
        margin-bottom: 1rem;

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
        }
      }

      .thumbnail-list {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
      }

      .thumbnail {
        width: 80px;
        height: 80px;
        padding: 0;
        border: 2px solid var(--color-border);
        border-radius: 8px;
        overflow: hidden;
        cursor: pointer;
        background: var(--color-bg-secondary);

        &.active {
          border-color: var(--color-primary);
        }

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }
    }

    .product-info {
      .product-brand {
        display: block;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-bottom: 0.5rem;
      }

      .product-title {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
      }

      .product-rating {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 1rem;

        .stars {
          display: flex;
          gap: 2px;
        }

        .star {
          color: var(--color-border);
          font-size: 1rem;

          &.filled {
            color: #ffc107;
          }
        }

        .rating-text {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
        }
      }

      .product-price {
        display: flex;
        align-items: baseline;
        gap: 0.75rem;
        margin-bottom: 1.5rem;

        .current-price, .sale-price {
          font-size: 2rem;
          font-weight: 700;
          color: var(--color-text-primary);
        }

        .sale-price {
          color: var(--color-error);
        }

        .original-price {
          font-size: 1.25rem;
          color: var(--color-text-secondary);
          text-decoration: line-through;
        }

        .discount-badge {
          padding: 0.25rem 0.5rem;
          background: var(--color-error);
          color: white;
          font-size: 0.875rem;
          font-weight: 600;
          border-radius: 4px;
        }
      }

      .product-description {
        color: var(--color-text-secondary);
        line-height: 1.6;
        margin-bottom: 1.5rem;
      }

      .product-meta {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        margin-bottom: 2rem;
        padding: 1rem;
        background: var(--color-bg-secondary);
        border-radius: 8px;

        .meta-item {
          display: flex;
          gap: 0.5rem;
        }

        .meta-label {
          font-weight: 500;
          color: var(--color-text-secondary);
        }

        .meta-value {
          color: var(--color-text-primary);
        }
      }
    }

    .add-to-cart-section {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .quantity-wrapper {
      display: flex;
      align-items: center;
      gap: 1rem;

      label {
        font-weight: 500;
        color: var(--color-text-secondary);
      }

      .quantity-controls {
        display: flex;
        align-items: center;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        overflow: hidden;

        button {
          width: 40px;
          height: 40px;
          border: none;
          background: var(--color-bg-secondary);
          color: var(--color-text-primary);
          font-size: 1.25rem;
          cursor: pointer;

          &:hover:not(:disabled) {
            background: var(--color-border);
          }

          &:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }
        }

        input {
          width: 60px;
          height: 40px;
          border: none;
          border-left: 1px solid var(--color-border);
          border-right: 1px solid var(--color-border);
          text-align: center;
          font-size: 1rem;
          background: var(--color-bg-primary);
          color: var(--color-text-primary);

          &::-webkit-inner-spin-button,
          &::-webkit-outer-spin-button {
            -webkit-appearance: none;
            margin: 0;
          }
        }
      }
    }

    .btn-add-to-cart {
      padding: 1rem 2rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1.125rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.7;
        cursor: not-allowed;
      }

      &.added {
        background: var(--color-success, #22c55e);
      }
    }

    .btn-wishlist {
      padding: 1rem 2rem;
      background: transparent;
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      font-size: 1rem;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-error);
        color: var(--color-error);
      }
    }

    .cart-notification {
      padding: 1rem;
      background: var(--color-success-bg, #dcfce7);
      color: var(--color-success, #22c55e);
      border-radius: 8px;
      font-weight: 500;
      text-align: center;
    }

    .product-tabs {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;

      .tab-headers {
        display: flex;
        border-bottom: 1px solid var(--color-border);
      }

      .tab-header {
        flex: 1;
        padding: 1rem;
        background: transparent;
        border: none;
        color: var(--color-text-secondary);
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;

        &:hover {
          color: var(--color-text-primary);
        }

        &.active {
          color: var(--color-primary);
          border-bottom: 2px solid var(--color-primary);
          margin-bottom: -1px;
        }
      }

      .tab-content {
        padding: 2rem;
      }

      .tab-panel {
        color: var(--color-text-secondary);
        line-height: 1.6;

        h4 {
          color: var(--color-text-primary);
          margin: 1.5rem 0 1rem;
        }

        .features-list {
          padding-left: 1.5rem;

          li {
            margin-bottom: 0.5rem;
          }
        }
      }
    }

    .specs-table {
      width: 100%;
      border-collapse: collapse;

      tr {
        border-bottom: 1px solid var(--color-border);

        &:last-child {
          border-bottom: none;
        }
      }

      th {
        text-align: left;
        padding: 0.75rem;
        background: var(--color-bg-secondary);
        font-weight: 500;
        color: var(--color-text-secondary);
        width: 40%;
      }

      td {
        padding: 0.75rem;
        color: var(--color-text-primary);
      }
    }

    @media (max-width: 768px) {
      .product-main {
        grid-template-columns: 1fr;
        gap: 2rem;
      }

      .product-info .product-title {
        font-size: 1.5rem;
      }

      .product-info .product-price {
        .current-price, .sale-price {
          font-size: 1.5rem;
        }
      }

      .tab-headers {
        flex-direction: column;
      }
    }
  `]
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);

  Math = Math;

  product = signal<Product | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  quantity = signal(1);
  selectedImage = signal<string | null>(null);
  activeTab = signal<'description' | 'specifications' | 'reviews'>('description');
  isAddingToCart = signal(false);
  addedToCart = signal(false);
  showNotification = signal(false);

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      this.loadProduct(slug);
    } else {
      this.error.set('Product not found');
      this.isLoading.set(false);
    }
  }

  private loadProduct(slug: string): void {
    this.isLoading.set(true);
    this.productService.getProductBySlug(slug).subscribe({
      next: (product) => {
        this.product.set(product);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load product:', err);
        this.error.set('Failed to load product. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  selectImage(url: string): void {
    this.selectedImage.set(url);
  }

  setActiveTab(tab: 'description' | 'specifications' | 'reviews'): void {
    this.activeTab.set(tab);
  }

  increaseQuantity(): void {
    if (this.quantity() < 99) {
      this.quantity.update(q => q + 1);
    }
  }

  decreaseQuantity(): void {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  onQuantityChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = parseInt(input.value, 10);
    if (value >= 1 && value <= 99) {
      this.quantity.set(value);
    } else {
      input.value = this.quantity().toString();
    }
  }

  addToCart(): void {
    const prod = this.product();
    if (!prod || this.isAddingToCart()) return;

    this.isAddingToCart.set(true);
    this.addedToCart.set(false);

    this.cartService.addToCart(prod.id, this.quantity()).subscribe({
      next: () => {
        this.isAddingToCart.set(false);
        this.addedToCart.set(true);
        this.showNotification.set(true);

        setTimeout(() => {
          this.addedToCart.set(false);
          this.showNotification.set(false);
        }, 3000);
      },
      error: () => {
        this.isAddingToCart.set(false);
      }
    });
  }

  getFeatures(): string[] {
    const prod = this.product();
    if (!prod?.features) return [];
    return Object.values(prod.features);
  }

  getSpecifications(): { key: string; value: string }[] {
    const prod = this.product();
    if (!prod?.specifications) return [];
    return Object.entries(prod.specifications).map(([key, value]) => ({
      key,
      value: String(value)
    }));
  }
}
