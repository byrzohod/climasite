import { Component, inject, signal, computed, effect, ElementRef, ViewChild, DestroyRef } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { LanguageService } from '../../../core/services/language.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { FlyingCartService } from '../../../core/services/flying-cart.service';
import { SeoService } from '../../../core/services/seo.service';
import { StructuredDataService } from '../../../core/services/structured-data.service';
import { Product } from '../../../core/models/product.model';
import { ProductConsumablesComponent } from '../../../shared/components/product-consumables/product-consumables.component';
import { SimilarProductsComponent } from '../../../shared/components/similar-products/similar-products.component';
import { ProductGalleryComponent, ProductImage as GalleryImage } from '../../../shared/components/product-gallery/product-gallery.component';
import { EnergyRatingComponent, EnergyRatingLevel } from '../../../shared/components/energy-rating/energy-rating.component';
import { WarrantyBadgeComponent } from '../../../shared/components/warranty-badge/warranty-badge.component';
import { ProductReviewsComponent } from '../../../shared/components/product-reviews/product-reviews.component';
import { ProductQaComponent } from '../components/product-qa/product-qa.component';
import { InstallationServiceComponent } from '../components/installation-service/installation-service.component';
import { SpecKeyPipe } from '../../../shared/pipes/spec-key.pipe';
import { RevealDirective } from '../../../shared/directives/reveal.directive';
import { DualPricePipe } from '../../../shared/pipes/dual-price.pipe';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule, ProductConsumablesComponent, SimilarProductsComponent, ProductGalleryComponent, EnergyRatingComponent, WarrantyBadgeComponent, ProductReviewsComponent, ProductQaComponent, InstallationServiceComponent, SpecKeyPipe, RevealDirective, DualPricePipe],
  template: `
    <div class="product-detail-container" data-testid="product-detail">
      @if (isLoading()) {
        <div class="loading" data-testid="loading">
          {{ 'common.loading' | translate }}
        </div>
      } @else if (error()) {
        <div class="error" data-testid="error">
          {{ error() | translate }}
        </div>
      } @else if (product()) {
        <div class="product-content">
          <!-- Breadcrumb -->
          <nav class="breadcrumb" [attr.aria-label]="'common.aria.breadcrumb' | translate" data-testid="breadcrumb">
            <a routerLink="/" data-testid="breadcrumb-home">{{ 'nav.home' | translate }}</a>
            <span class="separator">/</span>
            <a routerLink="/products" data-testid="breadcrumb-products">{{ 'nav.products' | translate }}</a>
            @if (product()?.category) {
              <span class="separator">/</span>
              <a [routerLink]="['/products/category', product()!.category!.slug]"
                 class="breadcrumb-category"
                 data-testid="breadcrumb-category">
                {{ product()?.category?.name }}
              </a>
            }
            <span class="separator">/</span>
            <span class="current" data-testid="breadcrumb-current">{{ product()?.name }}</span>
          </nav>

          <div class="product-main">
            <!-- Image Gallery with Zoom -->
            <div class="product-gallery-wrapper" appReveal="fade-up" [duration]="300" #galleryWrapper>
              <app-product-gallery
                [images]="galleryImages()"
                [productName]="product()?.name || ''" />
            </div>

            <!-- Product Info -->
            <div class="product-info" appReveal="fade-up" [delay]="100" [duration]="300">
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
                  <span class="sale-price">{{ product()?.basePrice | dualPrice }}</span>
                  <span class="original-price">{{ product()?.salePrice | dualPrice }}</span>
                  <span class="discount-badge">-{{ product()?.discountPercentage }}%</span>
                } @else {
                  <span class="current-price">{{ product()?.basePrice | dualPrice }}</span>
                }
              </div>

              <!-- INV-01 A3: reservation-adjusted availability (stock − held-by-checkout) -->
              @if (stockState() !== 'none') {
                <div
                  class="stock-indicator"
                  [class.in-stock]="stockState() === 'in-stock'"
                  [class.low-stock]="stockState() === 'low-stock'"
                  [class.out-of-stock]="stockState() === 'out-of-stock'"
                  role="status"
                  data-testid="stock-indicator"
                >
                  <span class="stock-dot" aria-hidden="true"></span>
                  @switch (stockState()) {
                    @case ('out-of-stock') {
                      <span>{{ 'products.stock.outOfStock' | translate }}</span>
                    }
                    @case ('low-stock') {
                      <span>{{ 'products.stock.lowStock' | translate:{ count: availableQuantity() } }}</span>
                    }
                    @default {
                      <span>{{ 'products.stock.inStock' | translate }}</span>
                    }
                  }
                </div>
              }

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
              </div>

              <!-- Warranty & Trust Badges -->
              <app-warranty-badge
                [warrantyMonths]="product()?.warrantyMonths || 0"
                [returnDays]="30"
                [freeShipping]="(product()?.basePrice || 0) >= 500"
                [inStock]="!hasActiveVariants() || availableQuantity() > 0"
                [installationAvailable]="product()?.requiresInstallation || false" />

              <!-- Energy Rating (if available) -->
              @if (energyClass()) {
                <div class="energy-ratings-section">
                  <app-energy-rating
                    [rating]="energyClass()!"
                    [label]="'products.energyRating'" />
                </div>
              }

              <!-- Add to Cart Section -->
              <div class="add-to-cart-section">
              <div class="quantity-wrapper">
                  <label id="quantity-label">{{ 'products.details.quantity' | translate }}:</label>
                  <div class="quantity-controls">
                    <button type="button" (click)="decreaseQuantity()" [disabled]="quantity() <= 1" [attr.aria-label]="'products.details.decreaseQuantity' | translate">−</button>
                    <input
                      type="number"
                      [value]="quantity()"
                      (change)="onQuantityChange($event)"
                      min="1"
                      max="99"
                      role="spinbutton"
                      aria-valuemin="1"
                      aria-valuemax="99"
                      [attr.aria-valuenow]="quantity()"
                      aria-labelledby="quantity-label"
                      data-testid="quantity-input"
                    />
                    <button type="button" (click)="increaseQuantity()" [attr.aria-label]="'products.details.increaseQuantity' | translate">+</button>
                  </div>
                </div>

                <button
                  class="btn-add-to-cart"
                  [class.added]="addedToCart()"
                  [disabled]="isAddingToCart() || stockState() === 'out-of-stock'"
                  (click)="addToCart()"
                  data-testid="add-to-cart"
                >
                  @if (isAddingToCart()) {
                    {{ 'common.loading' | translate }}
                  } @else if (addedToCart()) {
                    ✓ {{ 'cart.item.added' | translate }}
                  } @else if (stockState() === 'out-of-stock') {
                    {{ 'products.stock.outOfStock' | translate }}
                  } @else {
                    {{ 'products.details.addToCart' | translate }}
                  }
                </button>

                <button
                  class="btn-wishlist"
                  [class.active]="isInWishlist()"
                  [disabled]="wishlistLoading()"
                  (click)="toggleWishlist()"
                  data-testid="add-to-wishlist"
                >
                  @if (wishlistLoading()) {
                    <span class="wishlist-spinner"></span>
                  } @else if (isInWishlist()) {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="heart-icon">
                      <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                    </svg>
                    {{ 'products.details.removeFromWishlist' | translate }}
                  } @else {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="heart-icon">
                      <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                    </svg>
                    {{ 'products.details.addToWishlist' | translate }}
                  }
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
          <div class="product-tabs" appReveal="fade-up" [delay]="200">
            <div class="tab-headers">
              <button
                class="tab-header"
                [class.active]="activeTab() === 'description'"
                (click)="setActiveTab('description')"
                data-testid="tab-description"
              >
                {{ 'products.details.description' | translate }}
              </button>
              <button
                class="tab-header"
                [class.active]="activeTab() === 'specifications'"
                (click)="setActiveTab('specifications')"
                data-testid="tab-specifications"
              >
                {{ 'products.details.specifications' | translate }}
              </button>
              <button
                class="tab-header"
                [class.active]="activeTab() === 'reviews'"
                (click)="setActiveTab('reviews')"
                data-testid="tab-reviews"
              >
                {{ 'products.details.reviews' | translate }} ({{ product()?.reviewCount || 0 }})
              </button>
              <button
                class="tab-header"
                [class.active]="activeTab() === 'qa'"
                (click)="setActiveTab('qa')"
                data-testid="tab-qa"
              >
                {{ 'products.qa.title' | translate }}
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
                    <h2>{{ 'products.details.features' | translate }}</h2>
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
                            <th>{{ spec.key | specKey }}</th>
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
                  <app-product-reviews [productId]="product()!.id" />
                </div>
              }

              @if (activeTab() === 'qa') {
                <div class="tab-panel">
                  <app-product-qa [productId]="product()!.id" />
                </div>
              }
            </div>
          </div>

          <!-- Installation Service -->
          <app-installation-service [productId]="product()!.id" />

          <!-- Recommended Accessories / Consumables -->
          <app-product-consumables [productId]="product()!.id" />

          <!-- Similar Products -->
          <app-similar-products [productId]="product()!.id" />
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
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 2rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);

      a {
        color: var(--color-primary);
        text-decoration: none;
        transition: color 0.2s;

        &:hover {
          text-decoration: underline;
        }
      }

      .separator {
        color: var(--color-text-secondary);
      }

      .current {
        color: var(--color-text-primary);
        font-weight: 500;
      }
    }

    .product-main {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
      margin-bottom: 3rem;
    }

    .product-gallery-wrapper {
      position: relative;
    }

    .energy-ratings-section {
      margin: 1.5rem 0;
    }

    app-warranty-badge {
      display: block;
      margin: 1.5rem 0;
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
            color: var(--color-warning-400);
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
          color: var(--color-text-inverse);
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

    .stock-indicator {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      font-size: 0.875rem;
      font-weight: 600;

      .stock-dot {
        width: 0.5rem;
        height: 0.5rem;
        border-radius: 50%;
        background: currentColor;
        flex-shrink: 0;
      }

      &.in-stock {
        color: var(--color-success);
      }

      &.low-stock {
        color: var(--color-warning);
      }

      &.out-of-stock {
        color: var(--color-error);
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
                          transition: transform 0.1s ease-out, background-color 0.2s ease-out;

                          &:hover:not(:disabled) {
                            background: var(--color-bg-tertiary);
                          }

                          &:active:not(:disabled) {
                            transform: scale(0.9);
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
                          transition: transform 0.15s ease-out;

                          &::-webkit-inner-spin-button,
                          &::-webkit-outer-spin-button {
                            -webkit-appearance: none;
                            margin: 0;
                          }
                          
                          &.incrementing {
                            animation: numberUp 0.15s ease-out;
                          }
                          
                          &.decrementing {
                            animation: numberDown 0.15s ease-out;
                          }
                        }
                      }
                      
                      @keyframes numberUp {
                        0% { transform: translateY(0); }
                        50% { transform: translateY(-5px); opacity: 0.5; }
                        100% { transform: translateY(0); }
                      }
                      
                      @keyframes numberDown {
                        0% { transform: translateY(0); }
                        50% { transform: translateY(5px); opacity: 0.5; }
                        100% { transform: translateY(0); }
                      }
                    }

    .btn-add-to-cart {
      padding: 1rem 2rem;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      color: var(--color-text-inverse);
      border: none;
      border-radius: var(--radius-lg, 12px);
      font-size: 1.125rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        inset: 0;
        background: var(--gradient-glass);
        opacity: 0;
        transition: opacity 0.3s;
      }

      &:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 8px 20px var(--glow-primary);

        &::before {
          opacity: 1;
        }
      }

      &:active:not(:disabled) {
        transform: translateY(0);
      }

      &:disabled {
        opacity: 0.7;
        cursor: not-allowed;
      }

      &.added {
        background: linear-gradient(135deg, var(--color-success) 0%, var(--color-success-dark) 100%);
      }
    }

    .btn-wishlist {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 1rem 2rem;
      background: transparent;
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      font-size: 1rem;
      cursor: pointer;
      transition: all 0.2s;

      .heart-icon {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      &:hover:not(:disabled) {
        border-color: var(--color-error);
        color: var(--color-error);
      }

      &.active {
        border-color: var(--color-error);
        color: var(--color-error);
        background: rgba(239, 68, 68, 0.1);
      }

      &:disabled {
        opacity: 0.7;
        cursor: not-allowed;
      }

      .wishlist-spinner {
        width: 20px;
        height: 20px;
        border: 2px solid var(--color-border);
        border-top-color: var(--color-primary);
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .cart-notification {
      padding: 1rem;
      background: var(--color-success-bg);
      color: var(--color-success);
      border-radius: 8px;
      font-weight: 500;
      text-align: center;
    }

    .product-tabs {
      background: var(--glass-bg);
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      border: 1px solid var(--glass-border);
      border-radius: var(--radius-xl, 20px);
      overflow: hidden;
      box-shadow: var(--shadow-lg);

      .tab-headers {
                        display: flex;
                        border-bottom: 1px solid var(--color-border);
                        background: var(--color-bg-secondary);
                        position: relative;
                      }

                      .tab-header {
                        flex: 1;
                        padding: 1.25rem 1rem;
                        background: transparent;
                        border: none;
                        color: var(--color-text-secondary);
                        font-weight: 500;
                        cursor: pointer;
                        transition: color 0.2s ease-out, background-color 0.2s ease-out;
                        position: relative;

                        &::after {
                          content: '';
                          position: absolute;
                          bottom: 0;
                          left: 50%;
                          width: 0;
                          height: 3px;
                          background: linear-gradient(90deg, var(--color-primary), var(--color-accent));
                          border-radius: 3px 3px 0 0;
                          transition: width 0.3s ease-out, left 0.3s ease-out;
                          transform: translateX(-50%);
                        }

                        &:hover {
                          color: var(--color-text-primary);
                          background: var(--color-primary-light);
                        }

                        &.active {
                          color: var(--color-primary-active);
                          background: var(--color-bg-primary);
                          font-weight: 700;

                          &::after {
                            width: 60%;
                          }
                        }
                      }

                      .tab-content {
                        padding: 2rem;
                      }
                      
                      .tab-panel {
                        animation: tabContentEnter 0.3s ease-out forwards;
                      }
                      
                      @keyframes tabContentEnter {
                        from { opacity: 0; transform: translateY(10px); }
                        to { opacity: 1; transform: translateY(0); }
                      }

      .tab-panel {
        color: var(--color-text-secondary);
        line-height: 1.7;

        h2 {
          font-size: 1.25rem;
          color: var(--color-text-primary);
          margin: 1.5rem 0 1rem;
          font-weight: 600;
        }

        .features-list {
          padding-left: 1.5rem;

          li {
            margin-bottom: 0.75rem;
            position: relative;

            &::marker {
              color: var(--color-primary);
            }
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
                    
                    /* Reduced motion support */
                    @media (prefers-reduced-motion: reduce) {
                      .quantity-controls button,
                      .quantity-controls input,
                      .tab-header,
                      .tab-panel {
                        transition: none !important;
                        animation: none !important;
                        transform: none !important;
                      }
                    }
                  `]
})
export class ProductDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly languageService = inject(LanguageService);
  private readonly wishlistService = inject(WishlistService);
  private readonly flyingCartService = inject(FlyingCartService);
  private readonly seo = inject(SeoService);
  private readonly structuredData = inject(StructuredDataService);
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  /** Slug as a signal so same-route param changes (`/products/a` -> `/products/b`) refresh. */
  private readonly slug = toSignal(
    this.route.paramMap.pipe(map(p => p.get('slug'))),
    { initialValue: this.route.snapshot.paramMap.get('slug') }
  );
  /** Last `(slug, lang)` actually loaded — dedupes the unified reactive trigger. */
  private lastLoadedKey: string | null = null;
  /**
   * Monotonic load token (#91 FOUND-loaderr-race pattern). Navigating A->B reuses this
   * component (no destroy), so `takeUntilDestroyed` alone can't stop A's slower response
   * from landing after B's. Each load captures `seq`; callbacks apply state/meta/JSON-LD
   * only when their `seq` is still the latest.
   */
  private loadSeq = 0;

  @ViewChild('galleryWrapper') galleryWrapper?: ElementRef<HTMLElement>;

  Math = Math;

  product = signal<Product | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  quantity = signal(1);
  selectedImage = signal<string | null>(null);
  activeTab = signal<'description' | 'specifications' | 'reviews' | 'qa'>('description');
  isAddingToCart = signal(false);
  addedToCart = signal(false);
  showNotification = signal(false);
  wishlistLoading = signal(false);

  // Computed: Check if current product is in wishlist
  isInWishlist = computed(() => {
    const prod = this.product();
    if (!prod) return false;
    return this.wishlistService.isInWishlist(prod.id);
  });

  /**
   * INV-01 A3: low-stock display threshold. At/below this many (reservation-adjusted) units the PDP
   * shows "Only N left". Mirrors the variant default LowStockThreshold on the backend.
   */
  readonly lowStockThreshold = 5;

  /** True once the loaded product exposes any active variant to derive availability from. */
  hasActiveVariants = computed<boolean>(() =>
    (this.product()?.variants ?? []).some(v => v.isActive));

  /**
   * INV-01 A3: the variant no-variant add-to-cart will actually use. Mirrors AddToCartCommand's server
   * selection exactly — the first active variant with reservation-adjusted availability, else the first
   * active variant (fallback). The PDP has no variant selector, so availability must reflect THIS variant,
   * not a sum across variants (a sum could advertise a qty no single default variant can satisfy).
   */
  private defaultVariant = computed(() => {
    const active = (this.product()?.variants ?? []).filter(v => v.isActive);
    return active.find(v => (v.availableQuantity ?? 0) > 0) ?? active[0] ?? null;
  });

  /**
   * INV-01 A3: units available to buy right now on the default variant, using the DTO's reservation-adjusted
   * `availableQuantity` (stock − reserved) — NOT raw stock — so units held by another in-flight checkout are
   * not offered. Matches what add-to-cart will accept for qty > 1.
   */
  availableQuantity = computed<number>(() =>
    Math.max(this.defaultVariant()?.availableQuantity ?? 0, 0));

  /**
   * Stock badge state for the PDP indicator. 'none' hides the indicator (product has no active
   * variant data, e.g. an accessory with no variants) and preserves the pre-A3 trust-badge display.
   */
  stockState = computed<'in-stock' | 'low-stock' | 'out-of-stock' | 'none'>(() => {
    if (!this.hasActiveVariants()) return 'none';
    const available = this.availableQuantity();
    if (available <= 0) return 'out-of-stock';
    if (available <= this.lowStockThreshold) return 'low-stock';
    return 'in-stock';
  });

  constructor() {
    // H7/H3/H5: ONE reactive load trigger keyed on (slug, lang). Combining the route
    // param signal with the language signal in a single effect — deduped against the
    // last-loaded key — refreshes content + head on either change without the old
    // double-fetch race (no second subscription alongside this effect).
    effect(() => {
      const slug = this.slug();
      const lang = this.languageService.currentLanguage();
      if (!slug) {
        this.lastLoadedKey = null;
        this.loadSeq++; // invalidate any in-flight load
        this.error.set('products.details.notFound');
        this.isLoading.set(false);
        this.seo.setMeta({
          titleKey: 'seo.product.notFoundTitle',
          descriptionKey: 'seo.product.notFoundDescription',
          robots: 'noindex,follow'
        });
        return;
      }
      const key = `${slug}::${lang}`;
      if (key === this.lastLoadedKey) return;
      this.lastLoadedKey = key;
      this.loadProduct(slug);
    });
  }

  // Computed properties for new components
  galleryImages = computed<GalleryImage[]>(() => {
    const prod = this.product();
    if (!prod?.images?.length) return [];
    return prod.images.map(img => ({
      id: img.id,
      url: img.url,
      altText: img.altText,
      sortOrder: img.sortOrder
    }));
  });

  energyClass = computed<EnergyRatingLevel | null>(() => {
    const prod = this.product();
    if (!prod?.specifications) return null;
    const energySpec = prod.specifications['energy_class'] ||
                       prod.specifications['energyClass'] ||
                       prod.specifications['energy_efficiency'];
    if (!energySpec) return null;
    const validRatings: EnergyRatingLevel[] = ['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D', 'E', 'F', 'G'];
    const rating = String(energySpec).toUpperCase();
    return validRatings.includes(rating as EnergyRatingLevel) ? rating as EnergyRatingLevel : null;
  });

  private loadProduct(slug: string): void {
    const seq = ++this.loadSeq;
    this.isLoading.set(true);
    this.error.set(null);
    // takeUntilDestroyed cancels in-flight on destroy; the `seq` guard additionally rejects
    // a stale response from a previous slug/lang that lands out-of-order on the SAME instance.
    this.productService.getProductBySlug(slug)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          if (seq !== this.loadSeq) return;
          this.product.set(product);
          this.isLoading.set(false);
          this.applyProductSeo(product);
        },
        error: () => {
          if (seq !== this.loadSeq) return;
          // Error details are intentionally not logged in production
          // Consider implementing a logging service for production error tracking
          this.error.set('products.details.loadError');
          this.isLoading.set(false);
          // H5: a matched /products/:slug whose slug 404s in-component is not covered by
          // the `**` route's noindex — mark this error state noindex,follow explicitly.
          this.seo.setMeta({
            titleKey: 'seo.product.notFoundTitle',
            descriptionKey: 'seo.product.notFoundDescription',
            robots: 'noindex,follow'
          });
        }
      });
  }

  /** Apply curated-or-fallback meta + Product/Breadcrumb JSON-LD for the loaded product. */
  private applyProductSeo(product: Product): void {
    const origin = this.document.location.origin;
    this.seo.setMeta({
      title: product.metaTitle ?? product.name,
      description: product.metaDescription ?? product.shortDescription ?? product.description,
      image: product.images?.[0]?.url,
      type: 'product',
      robots: 'index,follow'
    });
    this.structuredData.setProductData(product, origin);
    // B-048/M1: the visible breadcrumb here is inline markup, so this component is the
    // breadcrumb JSON-LD emitter for the product page (Home > Products > [Category] > name).
    const trail = [
      { name: this.languageService.instant('nav.home'), url: `${origin}/` },
      { name: this.languageService.instant('nav.products'), url: `${origin}/products` }
    ];
    if (product.category) {
      trail.push({ name: product.category.name, url: `${origin}/products/category/${product.category.slug}` });
    }
    trail.push({ name: product.name, url: `${origin}/products/${product.slug}` });
    this.structuredData.setBreadcrumbData(trail);
  }

  selectImage(url: string): void {
    this.selectedImage.set(url);
  }

  setActiveTab(tab: 'description' | 'specifications' | 'reviews' | 'qa'): void {
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
    if (!prod || this.isAddingToCart() || this.stockState() === 'out-of-stock') return;

    this.isAddingToCart.set(true);
    this.addedToCart.set(false);

this.cartService.addToCart(prod.id, this.quantity()).subscribe({
      next: () => {
        this.isAddingToCart.set(false);
        this.addedToCart.set(true);
        this.showNotification.set(true);

        // Trigger flying cart animation
        const primaryImageUrl = prod.images?.[0]?.url;
        if (this.galleryWrapper?.nativeElement && primaryImageUrl) {
          // Find the main image within the gallery
          const mainImage = this.galleryWrapper.nativeElement.querySelector('img') as HTMLElement;
          if (mainImage) {
            this.flyingCartService.fly({
              imageUrl: primaryImageUrl,
              sourceElement: mainImage
            });
          }
        }

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

  toggleWishlist(): void {
    const prod = this.product();
    if (!prod || this.wishlistLoading()) return;

    this.wishlistLoading.set(true);

    // Create a ProductBrief for the wishlist service
    const productBrief = {
      id: prod.id,
      name: prod.name,
      slug: prod.slug,
      basePrice: prod.basePrice,
      salePrice: prod.salePrice,
      primaryImageUrl: prod.images?.[0]?.url || '',
      category: prod.category?.name || '',
      brand: prod.brand || '',
      averageRating: prod.averageRating || 0,
      reviewCount: prod.reviewCount || 0,
      isOnSale: prod.isOnSale || false,
      discountPercentage: prod.discountPercentage || 0,
      inStock: true
    };

    this.wishlistService.toggleWishlist(prod.id, productBrief);

    // Brief loading state for visual feedback
    setTimeout(() => {
      this.wishlistLoading.set(false);
    }, 300);
  }
}
