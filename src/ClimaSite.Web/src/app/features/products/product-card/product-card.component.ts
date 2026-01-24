import { Component, Input, inject, signal, computed, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ProductBrief, EnergyRatingLevel } from '../../../core/models/product.model';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { FlyingCartService } from '../../../core/services/flying-cart.service';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <article
      class="product-card"
      [class.list-view]="listView"
      [class.is-hovered]="isHovered"
      data-testid="product-card"
      (mouseenter)="isHovered = true"
      (mouseleave)="isHovered = false"
    >
      <a [routerLink]="['/products', product.slug]" class="product-image-link">
        <div class="product-image">
          @if (product.primaryImageUrl) {
            <img
              #productImage
              [src]="product.primaryImageUrl"
              [alt]="product.name"
              loading="lazy"
              data-testid="product-image"
            />
          } @else {
            <div class="placeholder-image">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 15.75l5.159-5.159a2.25 2.25 0 013.182 0l5.159 5.159m-1.5-1.5l1.409-1.409a2.25 2.25 0 013.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 001.5-1.5V6a1.5 1.5 0 00-1.5-1.5H3.75A1.5 1.5 0 002.25 6v12a1.5 1.5 0 001.5 1.5zm10.5-11.25h.008v.008h-.008V8.25zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
              </svg>
            </div>
          }

          <!-- Badges -->
          <div class="badge-container">
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

          <!-- Energy Rating Badge -->
          @if (product.energyRating) {
            <div 
              class="energy-badge" 
              [class]="getEnergyRatingClass(product.energyRating)"
              [attr.aria-label]="('specs.energy_class' | translate) + ': ' + product.energyRating"
              data-testid="energy-badge">
              <span class="energy-label">{{ product.energyRating }}</span>
            </div>
          }

          <!-- Quick actions overlay -->
          <div class="quick-actions">
            <button
              class="quick-action-btn wishlist"
              [class.active]="isWishlisted()"
              (click)="toggleWishlist($event)"
              [attr.aria-label]="(isWishlisted() ? 'products.details.removeFromWishlist' : 'products.details.addToWishlist') | translate"
              data-testid="wishlist-button"
            >
              @if (isWishlisted()) {
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 01-.383-.218 25.18 25.18 0 01-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0112 5.052 5.5 5.5 0 0116.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 01-4.244 3.17 15.247 15.247 0 01-.383.219l-.022.012-.007.004-.003.001a.752.752 0 01-.704 0l-.003-.001z" />
                </svg>
              } @else {
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
                </svg>
              }
            </button>
          </div>
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

        <!-- Quick Specs Row (HVAC-specific) -->
        @if (hasQuickSpecs()) {
          <div class="quick-specs" data-testid="quick-specs">
            @if (product.btuCapacity) {
              <span class="spec-item" [attr.aria-label]="'specs.btu' | translate">
                <svg class="spec-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                  <path d="M8 16a6 6 0 0 0 6-6c0-1.655-1.122-2.904-2.432-4.362C10.254 4.176 8.75 2.503 8 0c0 0-6 5.686-6 10a6 6 0 0 0 6 6zM6.5 12.5a1 1 0 0 1-1-1 3.5 3.5 0 0 1 3.5-3.5.5.5 0 0 1 0 1A2.5 2.5 0 0 0 6.5 11.5a.5.5 0 0 1-.5.5.5.5 0 0 1-.5.5z"/>
                </svg>
                {{ formatBtu(product.btuCapacity) }}
              </span>
            }
            @if (product.noiseLevel) {
              <span class="spec-item" [attr.aria-label]="'specs.noise_level' | translate">
                <svg class="spec-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                  <path d="M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z"/>
                  <path d="M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z"/>
                  <path d="M8.707 11.182A4.486 4.486 0 0 0 10.025 8a4.486 4.486 0 0 0-1.318-3.182L8 5.525A3.489 3.489 0 0 1 9.025 8 3.49 3.49 0 0 1 8 10.475l.707.707zM6.717 3.55A.5.5 0 0 1 7 4v8a.5.5 0 0 1-.812.39L3.825 10.5H1.5A.5.5 0 0 1 1 10V6a.5.5 0 0 1 .5-.5h2.325l2.363-1.89a.5.5 0 0 1 .529-.06z"/>
                </svg>
                {{ product.noiseLevel }}dB
              </span>
            }
            @if (product.roomSizeMin && product.roomSizeMax) {
              <span class="spec-item" [attr.aria-label]="'specs.room_size' | translate">
                <svg class="spec-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                  <path d="M14.763.075A.5.5 0 0 1 15 .5v15a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5V14h-1v1.5a.5.5 0 0 1-.5.5h-9a.5.5 0 0 1-.5-.5V10a.5.5 0 0 1 .342-.474L6 7.64V4.5a.5.5 0 0 1 .276-.447l8-4a.5.5 0 0 1 .487.022zM6 8.694L1 10.36V15h5V8.694zM7 15h2v-1.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 .5.5V15h2V1.309l-7 3.5V15z"/>
                </svg>
                {{ product.roomSizeMin }}-{{ product.roomSizeMax }}m²
              </span>
            }
          </div>
        }

        <div class="product-rating" *ngIf="product.reviewCount > 0">
          <div class="stars">
            @for (star of stars; track $index) {
              <svg 
                class="star-icon" 
                [class.filled]="star <= Math.floor(product.averageRating)"
                [class.half]="star === Math.ceil(product.averageRating) && product.averageRating % 1 >= 0.5"
                xmlns="http://www.w3.org/2000/svg" 
                viewBox="0 0 24 24"
              >
                <path d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z"/>
              </svg>
            }
          </div>
          <span class="review-count">({{ product.reviewCount }})</span>
        </div>

        <div class="product-price" data-testid="product-price">
          @if (product.isOnSale && product.salePrice) {
            <span class="sale-price">{{ product.salePrice | currency:'EUR' }}</span>
            <span class="original-price">{{ product.basePrice | currency:'EUR' }}</span>
          } @else {
            <span class="current-price">{{ product.basePrice | currency:'EUR' }}</span>
          }
        </div>

        <div class="product-actions">
          <button
            class="btn-add-to-cart"
            [class.loading]="isAddingToCart()"
            [class.added]="addedToCart()"
            [disabled]="!product.inStock || isAddingToCart()"
            (click)="addToCart($event)"
            data-testid="add-to-cart"
          >
            @if (isAddingToCart()) {
              <svg class="spinner" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>{{ 'common.loading' | translate }}</span>
            } @else if (addedToCart()) {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M19.916 4.626a.75.75 0 01.208 1.04l-9 13.5a.75.75 0 01-1.154.114l-6-6a.75.75 0 011.06-1.06l5.353 5.353 8.493-12.739a.75.75 0 011.04-.208z" clip-rule="evenodd" />
              </svg>
              <span>{{ 'cart.added' | translate }}</span>
            } @else if (!product.inStock) {
              <span>{{ 'products.details.outOfStock' | translate }}</span>
            } @else {
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M7.5 14.25a3 3 0 00-3 3h15.75m-12.75-3h11.218c1.121-2.3 2.1-4.684 2.924-7.138a60.114 60.114 0 00-16.536-1.84M7.5 14.25L5.106 5.272M6 20.25a.75.75 0 11-1.5 0 .75.75 0 011.5 0zm12.75 0a.75.75 0 11-1.5 0 .75.75 0 011.5 0z" />
              </svg>
              <span>{{ 'products.details.addToCart' | translate }}</span>
            }
          </button>
        </div>
      </div>
    </article>
  `,
  styles: [`
    .product-card {
      --card-radius: var(--radius-xl);
      --card-padding: 1rem;
      
      display: flex;
      flex-direction: column;
      background: var(--color-bg-card);
      border-radius: var(--card-radius);
      border: 1px solid var(--color-border-primary);
      overflow: hidden;
      transition: 
        transform var(--duration-normal) var(--ease-out-quart),
        box-shadow var(--duration-normal) var(--ease-smooth),
        border-color var(--duration-fast) var(--ease-smooth);

      &:hover,
      &.is-hovered {
        transform: translateY(-4px);
        box-shadow: var(--shadow-xl);
        border-color: var(--color-border-secondary);

        .product-image img {
          transform: scale(1.08);
        }

        .quick-actions {
          opacity: 1;
          transform: translateY(0);
        }
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
          padding: var(--card-padding) var(--card-padding) var(--card-padding) 0;
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
      min-height: 220px;
      aspect-ratio: 1 / 1;
      background: var(--color-bg-secondary);
      overflow: hidden;

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        transition: transform var(--duration-slow) var(--ease-out-quart);
      }

      .placeholder-image {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 100%;
        height: 100%;
        color: var(--color-text-tertiary);

        svg {
          width: 3rem;
          height: 3rem;
        }
      }
    }

    .badge-container {
      position: absolute;
      top: 0.75rem;
      left: 0.75rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      z-index: 2;
    }

    .sale-badge {
      padding: 0.25rem 0.625rem;
      background: linear-gradient(135deg, var(--color-error) 0%, var(--color-error-dark) 100%);
      color: var(--color-text-inverse);
      font-size: var(--text-body-xs);
      font-weight: var(--font-semibold);
      border-radius: var(--radius-md);
      box-shadow: 0 2px 8px rgba(239, 68, 68, 0.3);
    }

    .out-of-stock-badge {
      padding: 0.25rem 0.625rem;
      background: var(--color-secondary-600);
      color: var(--color-text-inverse);
      font-size: var(--text-body-xs);
      font-weight: var(--font-medium);
      border-radius: var(--radius-md);
    }

    /* Energy Rating Badge */
    .energy-badge {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 42px;
      height: 28px;
      padding: 0 0.5rem;
      border-radius: var(--radius-md);
      font-family: var(--font-mono);
      font-size: var(--text-body-sm);
      font-weight: var(--font-bold);
      color: var(--color-text-inverse);
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      z-index: 3;
      transition: transform var(--duration-fast) var(--ease-smooth);

      &:hover {
        transform: scale(1.05);
      }

      /* Energy rating color classes */
      &.energy-excellent {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
        box-shadow: 0 2px 8px rgba(16, 185, 129, 0.4);
      }

      &.energy-good {
        background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
        box-shadow: 0 2px 8px rgba(34, 197, 94, 0.4);
      }

      &.energy-average {
        background: linear-gradient(135deg, #eab308 0%, #ca8a04 100%);
        box-shadow: 0 2px 8px rgba(234, 179, 8, 0.4);
      }

      &.energy-poor {
        background: linear-gradient(135deg, #f97316 0%, #ea580c 100%);
        box-shadow: 0 2px 8px rgba(249, 115, 22, 0.4);
      }

      &.energy-bad {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        box-shadow: 0 2px 8px rgba(239, 68, 68, 0.4);
      }
    }

    .energy-label {
      letter-spacing: 0.5px;
    }

    .quick-actions {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      opacity: 0;
      transform: translateY(-8px);
      transition: 
        opacity var(--duration-normal) var(--ease-smooth),
        transform var(--duration-normal) var(--ease-out-quart);
      z-index: 2;
    }

    .quick-action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: var(--glass-bg-heavy);
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
      border: 1px solid var(--glass-border);
      border-radius: var(--radius-full);
      cursor: pointer;
      transition: all var(--duration-fast) var(--ease-smooth);

      svg {
        width: 18px;
        height: 18px;
        color: var(--color-text-secondary);
        transition: color var(--duration-fast) var(--ease-smooth);
      }

      &:hover {
        background: var(--color-bg-card);
        border-color: var(--color-error);
        transform: scale(1.1);

        svg {
          color: var(--color-error);
        }
      }

      &.active {
        background: var(--color-error-light);
        border-color: var(--color-error);

        svg {
          color: var(--color-error);
        }
      }
    }

    .product-info {
      padding: var(--card-padding);
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex: 1;
    }

    .product-brand {
      font-family: var(--font-body);
      font-size: var(--text-body-xs);
      font-weight: var(--font-medium);
      color: var(--color-text-tertiary);
      text-transform: uppercase;
      letter-spacing: var(--tracking-wide);
    }

    .product-name-link {
      text-decoration: none;
      color: inherit;
    }

    .product-name {
      font-family: var(--font-display);
      font-size: var(--text-body);
      font-weight: var(--font-semibold);
      color: var(--color-text-primary);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      line-height: var(--leading-snug);
      transition: color var(--duration-fast) var(--ease-smooth);

      &:hover {
        color: var(--color-primary);
      }
    }

    .product-description {
      font-size: var(--text-body-sm);
      color: var(--color-text-secondary);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      line-height: var(--leading-normal);
    }

    /* Quick Specs Row */
    .quick-specs {
      display: flex;
      flex-wrap: wrap;
      gap: 0.625rem;
      padding: 0.5rem 0;
      margin-top: 0.25rem;
      border-top: 1px solid var(--color-border-primary);
    }

    .spec-item {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      font-family: var(--font-mono);
      font-size: var(--text-body-xs);
      color: var(--color-text-secondary);
      white-space: nowrap;

      .spec-icon {
        width: 12px;
        height: 12px;
        flex-shrink: 0;
        color: var(--color-primary);
      }
    }

    .product-rating {
      display: flex;
      align-items: center;
      gap: 0.375rem;

      .stars {
        display: flex;
        gap: 2px;
      }

      .star-icon {
        width: 14px;
        height: 14px;
        fill: var(--color-border-secondary);
        stroke: none;

        &.filled {
          fill: var(--color-warning-400);
        }

        &.half {
          fill: url(#half-star-gradient);
        }
      }

      .review-count {
        font-size: var(--text-body-xs);
        color: var(--color-text-tertiary);
      }
    }

    .product-price {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;
      margin-top: auto;
      padding-top: 0.5rem;

      .current-price {
        font-family: var(--font-mono);
        font-size: var(--text-h4);
        font-weight: var(--font-bold);
        color: var(--color-text-primary);
      }

      .sale-price {
        font-family: var(--font-mono);
        font-size: var(--text-h4);
        font-weight: var(--font-bold);
        color: var(--color-error);
      }

      .original-price {
        font-family: var(--font-mono);
        font-size: var(--text-body-sm);
        color: var(--color-text-tertiary);
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
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: var(--radius-lg);
      font-family: var(--font-body);
      font-size: var(--text-body-sm);
      font-weight: var(--font-medium);
      cursor: pointer;
      transition: all var(--duration-fast) var(--ease-smooth);

      svg {
        width: 18px;
        height: 18px;
      }

      .spinner {
        width: 16px;
        height: 16px;
        animation: spin 0.8s linear infinite;
      }

      @keyframes spin {
        to { transform: rotate(360deg); }
      }

      &:hover:not(:disabled) {
        background: var(--color-primary-hover);
        box-shadow: var(--glow-sm-primary);
        transform: translateY(-1px);
      }

      &:active:not(:disabled) {
        transform: translateY(0) scale(0.98);
      }

      &:disabled {
        background: var(--color-bg-tertiary);
        color: var(--color-text-tertiary);
        cursor: not-allowed;
        transform: none;
        box-shadow: none;
      }

      &.added {
        background: var(--color-success);
        color: var(--color-text-inverse);

        &:hover:not(:disabled) {
          background: var(--color-success);
          box-shadow: var(--glow-success);
        }
      }

      &.loading {
        pointer-events: none;
      }
    }
    
    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .product-card,
      .product-image img,
      .quick-actions,
      .quick-action-btn,
      .btn-add-to-cart,
      .energy-badge {
        transition: none !important;
        animation: none !important;
        transform: none !important;
      }
      
      .product-card:hover,
      .product-card.is-hovered {
        transform: none;
      }
      
      .product-image img {
        transform: none;
      }
      
      .quick-actions {
        opacity: 1;
        transform: none;
      }

      .energy-badge:hover {
        transform: none;
      }
    }
  `]
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductBrief;
  @Input() listView = false;

  @ViewChild('productImage') productImage?: ElementRef<HTMLImageElement>;

  private readonly cartService = inject(CartService);
  private readonly wishlistService = inject(WishlistService);
  private readonly flyingCartService = inject(FlyingCartService);

  Math = Math;
  stars = [1, 2, 3, 4, 5];
  isAddingToCart = signal(false);
  addedToCart = signal(false);
  isHovered = false;

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
        
        // Trigger flying cart animation
        if (this.productImage?.nativeElement && this.product.primaryImageUrl) {
          this.flyingCartService.fly({
            imageUrl: this.product.primaryImageUrl,
            sourceElement: this.productImage.nativeElement
          });
        }
        
        setTimeout(() => this.addedToCart.set(false), 2000);
      },
      error: () => {
        this.isAddingToCart.set(false);
      }
    });
  }

  toggleWishlist(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.wishlistService.toggleWishlist(this.product.id, this.product);
  }

  /**
   * Check if product has any quick specs to display
   */
  hasQuickSpecs(): boolean {
    return !!(
      this.product.btuCapacity ||
      this.product.noiseLevel ||
      (this.product.roomSizeMin && this.product.roomSizeMax)
    );
  }

  /**
   * Get the CSS class for energy rating badge based on efficiency level
   * A+++, A++, A+ = Excellent (dark green)
   * A = Good (green)
   * B = Average (yellow)
   * C, D = Poor (orange)
   * E, F, G = Bad (red)
   */
  getEnergyRatingClass(rating: EnergyRatingLevel): string {
    switch (rating) {
      case 'A+++':
      case 'A++':
      case 'A+':
        return 'energy-excellent';
      case 'A':
        return 'energy-good';
      case 'B':
        return 'energy-average';
      case 'C':
      case 'D':
        return 'energy-poor';
      default:
        return 'energy-bad';
    }
  }

  /**
   * Format BTU capacity for display
   * e.g., 12000 -> "12K BTU"
   */
  formatBtu(btu: number): string {
    if (btu >= 1000) {
      const formatted = (btu / 1000).toFixed(btu % 1000 === 0 ? 0 : 1);
      return `${formatted}K BTU`;
    }
    return `${btu} BTU`;
  }
}
