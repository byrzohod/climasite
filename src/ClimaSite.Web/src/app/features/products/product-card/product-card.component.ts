import { Component, Input, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ProductBrief } from '../../../core/models/product.model';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <article
      class="product-card"
      [class.list-view]="listView"
      data-testid="product-card"
    >
      <a [routerLink]="['/products', product.slug]" class="product-image-link">
        <div class="product-image">
          @if (product.primaryImageUrl) {
            <img
              [src]="product.primaryImageUrl"
              [alt]="product.name"
              loading="lazy"
            />
          } @else {
            <div class="placeholder-image">
              <span>No Image</span>
            </div>
          }

          @if (product.isOnSale && product.discountPercentage > 0) {
            <span class="sale-badge" data-testid="sale-badge">
              -{{ product.discountPercentage }}%
            </span>
          }

          @if (!product.inStock) {
            <span class="out-of-stock-badge">
              {{ 'products.details.outOfStock' | translate }}
            </span>
          }
        </div>
      </a>

      <div class="product-info">
        @if (product.brand) {
          <span class="product-brand">{{ product.brand }}</span>
        }

        <a [routerLink]="['/products', product.slug]" class="product-name-link">
          <h3 class="product-name" [title]="product.name" data-testid="product-name">{{ product.name }}</h3>
        </a>

        @if (listView && product.shortDescription) {
          <p class="product-description">{{ product.shortDescription }}</p>
        }

        <div class="product-rating" *ngIf="product.reviewCount > 0">
          <div class="stars">
            @for (star of stars; track $index) {
              <span
                class="star"
                [class.filled]="star <= Math.floor(product.averageRating)"
                [class.half]="star === Math.ceil(product.averageRating) && product.averageRating % 1 >= 0.5"
              >&#9733;</span>
            }
          </div>
          <span class="review-count">({{ product.reviewCount }})</span>
        </div>

        <div class="product-price" data-testid="product-price">
          @if (product.isOnSale && product.salePrice) {
            <span class="original-price">{{ product.salePrice | currency:'EUR' }}</span>
            <span class="sale-price">{{ product.basePrice | currency:'EUR' }}</span>
          } @else {
            <span class="current-price">{{ product.basePrice | currency:'EUR' }}</span>
          }
        </div>

        <div class="product-actions">
          <button
            class="btn-add-to-cart"
            [class.added]="addedToCart()"
            [disabled]="!product.inStock || isAddingToCart()"
            (click)="addToCart($event)"
            data-testid="add-to-cart"
          >
            @if (isAddingToCart()) {
              {{ 'common.loading' | translate }}
            } @else if (addedToCart()) {
              ✓ Added
            } @else if (!product.inStock) {
              {{ 'products.details.outOfStock' | translate }}
            } @else {
              {{ 'products.details.addToCart' | translate }}
            }
          </button>

          <!-- NAV-002: Wishlist button with state management -->
          <button
            class="btn-wishlist"
            [class.wishlisted]="isWishlisted()"
            (click)="toggleWishlist($event)"
            [attr.aria-label]="(isWishlisted() ? 'products.details.removeFromWishlist' : 'products.details.addToWishlist') | translate"
            data-testid="wishlist-button"
          >
            @if (isWishlisted()) {
              <span class="heart-icon filled">&#9829;</span>
            } @else {
              <span class="heart-icon">&#9825;</span>
            }
          </button>
        </div>
      </div>
    </article>
  `,
  styles: [`
    .product-card {
      display: flex;
      flex-direction: column;
      background: var(--color-bg-primary);
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      transition: transform 0.2s ease, box-shadow 0.2s ease;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
      }

      &.list-view {
        flex-direction: row;
        gap: 1.5rem;

        .product-image {
          width: 200px;
          min-width: 200px;
          height: 200px;
        }

        .product-info {
          flex: 1;
          padding: 1rem 1rem 1rem 0;
        }

        .product-actions {
          margin-top: auto;
        }
      }
    }

    .product-image-link {
      display: block;
      text-decoration: none;
    }

    .product-image {
      position: relative;
      height: 220px;
      background: var(--color-bg-secondary);
      overflow: hidden;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        transition: transform 0.3s ease;
      }

      &:hover img {
        transform: scale(1.05);
      }

      .placeholder-image {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 100%;
        height: 100%;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .sale-badge {
      position: absolute;
      top: 0.75rem;
      left: 0.75rem;
      padding: 0.25rem 0.5rem;
      background: var(--color-error);
      color: white;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: 4px;
    }

    .out-of-stock-badge {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      padding: 0.25rem 0.5rem;
      background: var(--color-text-secondary);
      color: white;
      font-size: 0.75rem;
      font-weight: 500;
      border-radius: 4px;
    }

    .product-info {
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex: 1;
    }

    .product-brand {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .product-name-link {
      text-decoration: none;
      color: inherit;
    }

    .product-name {
      font-size: 1rem;
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      line-height: 1.3;

      &:hover {
        color: var(--color-primary);
      }
    }

    .product-description {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .product-rating {
      display: flex;
      align-items: center;
      gap: 0.25rem;

      .stars {
        display: flex;
        gap: 2px;
      }

      .star {
        color: var(--color-border);
        font-size: 0.875rem;

        &.filled, &.half {
          color: #ffc107;
        }
      }

      .review-count {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
      }
    }

    .product-price {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;
      margin-top: auto;

      .current-price, .sale-price {
        font-size: 1.25rem;
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

    .product-actions {
      display: flex;
      gap: 0.5rem;
      margin-top: 0.75rem;
    }

    .btn-add-to-cart {
      flex: 1;
      padding: 0.625rem 1rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s ease;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        background: var(--color-bg-secondary);
        color: var(--color-text-secondary);
        cursor: not-allowed;
      }

      &.added {
        background: var(--color-success, #22c55e);
        color: white;
        cursor: default;
      }
    }

    .btn-wishlist {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s ease;

      .heart-icon {
        font-size: 1.25rem;
        color: var(--color-text-secondary);

        &.filled {
          color: var(--color-error);
        }
      }

      &:hover {
        background: var(--color-error-bg);
        border-color: var(--color-error);

        .heart-icon {
          color: var(--color-error);
        }
      }

      /* NAV-002: Wishlisted state */
      &.wishlisted {
        background: var(--color-error-bg);
        border-color: var(--color-error);

        .heart-icon {
          color: var(--color-error);
        }
      }
    }
  `]
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductBrief;
  @Input() listView = false;

  private readonly cartService = inject(CartService);
  private readonly wishlistService = inject(WishlistService);

  Math = Math;
  stars = [1, 2, 3, 4, 5];
  isAddingToCart = signal(false);
  addedToCart = signal(false);

  // NAV-002: Check if product is in wishlist - must access items() signal for reactivity
  isWishlisted = computed(() => {
    const items = this.wishlistService.items();
    return items.some(item => item.productId === this.product?.id);
  });

  addToCart(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.product.inStock || this.isAddingToCart()) {
      return;
    }

    this.isAddingToCart.set(true);
    this.addedToCart.set(false);

    this.cartService.addToCart(this.product.id, 1).subscribe({
      next: () => {
        this.isAddingToCart.set(false);
        this.addedToCart.set(true);
        // Reset the added state after 2 seconds
        setTimeout(() => this.addedToCart.set(false), 2000);
      },
      error: () => {
        this.isAddingToCart.set(false);
      }
    });
  }

  // NAV-002: Toggle wishlist functionality
  toggleWishlist(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.wishlistService.toggleWishlist(this.product.id, this.product);
  }
}
