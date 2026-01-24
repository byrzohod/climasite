import { Component, input, output } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CartItem } from '../../../core/models/cart.model';

/**
 * Mini Cart Item Component
 * 
 * Displays a single cart item in the mini-cart drawer.
 * Features:
 * - Product image, name, variant
 * - Price display (shows sale price if applicable)
 * - Quantity controls (+/-)
 * - Remove button
 * - Loading state
 */
@Component({
  selector: 'app-mini-cart-item',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule, CurrencyPipe],
  template: `
    <article 
      class="mini-cart-item" 
      [class.is-loading]="isLoading()"
      data-testid="cart-item"
    >
      <!-- Product Image -->
      <a 
        [routerLink]="['/products', item().productSlug]"
        class="item-image-link"
        tabindex="-1"
      >
        <div class="item-image">
          @if (item().imageUrl) {
            <img 
              [src]="item().imageUrl" 
              [alt]="item().productName"
              loading="lazy"
            />
          } @else {
            <div class="no-image">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                <circle cx="8.5" cy="8.5" r="1.5"></circle>
                <polyline points="21 15 16 10 5 21"></polyline>
              </svg>
            </div>
          }
        </div>
      </a>
      
      <!-- Item Details -->
      <div class="item-details">
        <!-- Name & Variant -->
        <div class="item-info">
          <a 
            [routerLink]="['/products', item().productSlug]"
            class="item-name"
          >
            {{ item().productName }}
          </a>
          @if (item().variantName) {
            <span class="item-variant">{{ item().variantName }}</span>
          }
        </div>
        
        <!-- Price -->
        <div class="item-price">
          @if (item().salePrice && item().salePrice! < item().unitPrice) {
            <span class="price-sale">{{ item().salePrice | currency }}</span>
            <span class="price-original">{{ item().unitPrice | currency }}</span>
          } @else {
            <span class="price-regular">{{ item().unitPrice | currency }}</span>
          }
        </div>
        
        <!-- Quantity Controls & Remove -->
        <div class="item-actions">
          <div class="quantity-controls">
            <button
              type="button"
              class="qty-btn"
              (click)="decreaseQuantity()"
              [disabled]="item().quantity <= 1 || isLoading()"
              [attr.aria-label]="'cart.decrease_quantity' | translate"
              data-testid="qty-decrease"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
            </button>
            
            <span class="qty-value" [attr.aria-label]="'cart.item.quantity' | translate">
              {{ item().quantity }}
            </span>
            
            <button
              type="button"
              class="qty-btn"
              (click)="increaseQuantity()"
              [disabled]="item().quantity >= item().maxQuantity || isLoading()"
              [attr.aria-label]="'cart.increase_quantity' | translate"
              data-testid="qty-increase"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
            </button>
          </div>
          
          <button
            type="button"
            class="remove-btn"
            (click)="onRemove()"
            [disabled]="isLoading()"
            [attr.aria-label]="'cart.item.remove' | translate"
            data-testid="item-remove"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </button>
        </div>
      </div>
      
      <!-- Loading Overlay -->
      @if (isLoading()) {
        <div class="loading-overlay">
          <div class="spinner"></div>
        </div>
      }
    </article>
  `,
  styles: [`
    .mini-cart-item {
      position: relative;
      display: flex;
      gap: var(--space-3);
      padding: var(--space-3);
      background-color: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: var(--radius-xl);
      transition: opacity var(--duration-fast) var(--ease-out);
      
      &.is-loading {
        opacity: 0.6;
        pointer-events: none;
      }
    }
    
    // Image
    .item-image-link {
      flex-shrink: 0;
    }
    
    .item-image {
      width: 72px;
      height: 72px;
      border-radius: var(--radius-lg);
      overflow: hidden;
      background-color: var(--color-bg-secondary);
      
      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
    }
    
    .no-image {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-text-tertiary);
      
      svg {
        width: 32px;
        height: 32px;
      }
    }
    
    // Details
    .item-details {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }
    
    .item-info {
      display: flex;
      flex-direction: column;
      gap: var(--space-0-5);
    }
    
    .item-name {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
      text-decoration: none;
      line-height: 1.3;
      overflow: hidden;
      text-overflow: ellipsis;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      
      &:hover {
        color: var(--color-primary);
      }
    }
    
    .item-variant {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
    }
    
    // Price
    .item-price {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      margin-top: var(--space-0-5);
    }
    
    .price-regular,
    .price-sale {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-primary);
    }
    
    .price-sale {
      color: var(--color-error);
    }
    
    .price-original {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
      text-decoration: line-through;
    }
    
    // Actions
    .item-actions {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-2);
      margin-top: auto;
      padding-top: var(--space-2);
    }
    
    // Quantity Controls
    .quantity-controls {
      display: flex;
      align-items: center;
      gap: var(--space-1);
      background-color: var(--color-bg-secondary);
      border-radius: var(--radius-lg);
      padding: var(--space-0-5);
    }
    
    .qty-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      padding: 0;
      border: none;
      border-radius: var(--radius-md);
      background-color: transparent;
      color: var(--color-text-secondary);
      cursor: pointer;
      transition: 
        background-color var(--duration-fast) var(--ease-out),
        color var(--duration-fast) var(--ease-out);
      
      svg {
        width: 14px;
        height: 14px;
      }
      
      &:hover:not(:disabled) {
        background-color: var(--color-bg-hover);
        color: var(--color-text-primary);
      }
      
      &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }
      
      &:focus-visible {
        outline: 2px solid var(--color-primary);
        outline-offset: 1px;
      }
    }
    
    .qty-value {
      min-width: 24px;
      text-align: center;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
    }
    
    // Remove Button
    .remove-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      padding: 0;
      border: none;
      border-radius: var(--radius-md);
      background-color: transparent;
      color: var(--color-text-tertiary);
      cursor: pointer;
      transition: 
        background-color var(--duration-fast) var(--ease-out),
        color var(--duration-fast) var(--ease-out);
      
      svg {
        width: 16px;
        height: 16px;
      }
      
      &:hover:not(:disabled) {
        background-color: var(--color-error-light);
        color: var(--color-error);
      }
      
      &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }
      
      &:focus-visible {
        outline: 2px solid var(--color-primary);
        outline-offset: 1px;
      }
    }
    
    // Loading Overlay
    .loading-overlay {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: rgba(255, 255, 255, 0.5);
      border-radius: var(--radius-xl);
      
      [data-theme="dark"] & {
        background-color: rgba(0, 0, 0, 0.3);
      }
    }
    
    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--color-border-secondary);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    
    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }
    
    // Reduced motion
    @media (prefers-reduced-motion: reduce) {
      .mini-cart-item,
      .qty-btn,
      .remove-btn {
        transition: none;
      }
      
      .spinner {
        animation: none;
        border-color: var(--color-primary);
      }
    }
  `]
})
export class MiniCartItemComponent {
  // Inputs
  item = input.required<CartItem>();
  isLoading = input<boolean>(false);
  
  // Outputs
  quantityChange = output<number>();
  remove = output<void>();
  
  /**
   * Decrease quantity by 1
   */
  decreaseQuantity(): void {
    const currentQty = this.item().quantity;
    if (currentQty > 1) {
      this.quantityChange.emit(currentQty - 1);
    }
  }
  
  /**
   * Increase quantity by 1
   */
  increaseQuantity(): void {
    const currentQty = this.item().quantity;
    const maxQty = this.item().maxQuantity;
    if (currentQty < maxQty) {
      this.quantityChange.emit(currentQty + 1);
    }
  }
  
  /**
   * Remove item from cart
   */
  onRemove(): void {
    this.remove.emit();
  }
}
