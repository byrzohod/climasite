import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CartService } from '../../core/services/cart.service';
import { CartItem } from '../../core/models/cart.model';
import { RevealDirective } from '../../shared/directives/reveal.directive';
import { ToastService } from '../../shared/components/toast/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule, RevealDirective, EmptyStateComponent],
  template: `
    <div class="cart-container" data-testid="cart-page">
      <h1>{{ 'cart.title' | translate }}</h1>

      @if (cartService.isLoading()) {
        <div class="loading" data-testid="cart-loading">
          {{ 'common.loading' | translate }}
        </div>
      } @else if (cartService.error()) {
        <div class="error-message" data-testid="cart-error">
          {{ cartService.error() }}
        </div>
      } @else if (cartService.isEmpty()) {
        <app-empty-state
          variant="cart"
          [title]="'emptyState.cart.title' | translate"
          [description]="'emptyState.cart.description' | translate"
          [actionLabel]="'emptyState.cart.action' | translate"
          actionRoute="/products"
          data-testid="empty-cart"
        />
      } @else {
        <div class="cart-content">
          <div class="cart-items">
            @for (item of cartService.items(); track item.id; let i = $index) {
              <div class="cart-item" 
                   [class.removing]="removingItems().has(item.id)"
                   data-testid="cart-item" 
                   appReveal="fade-up" 
                   [delay]="i * 75">
                <div class="item-image">
                  @if (item.imageUrl) {
                    <img [src]="item.imageUrl" [alt]="item.productName" loading="lazy" />
                  } @else {
                    <div class="no-image">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21 8v13H3V8"/>
                        <path d="M1 3h22v5H1z"/>
                        <path d="M10 12h4"/>
                      </svg>
                    </div>
                  }
                </div>

                <div class="item-details">
                  <a [routerLink]="['/products', item.productSlug]" class="item-name" data-testid="cart-item-name">
                    {{ item.productName }}
                  </a>
                  @if (item.variantName) {
                    <span class="item-variant">{{ item.variantName }}</span>
                  }
                  <span class="item-sku">{{ item.sku }}</span>
                </div>

                <div class="item-price" data-testid="cart-item-price">
                  @if (item.salePrice && item.salePrice < item.unitPrice) {
                    <span class="original-price">{{ item.unitPrice | currency }}</span>
                    <span class="sale-price">{{ item.salePrice | currency }}</span>
                  } @else {
                    <span>{{ item.unitPrice | currency }}</span>
                  }
                </div>

                <div class="item-quantity" [class.updating]="updatingItemId() === item.id" data-testid="item-quantity">
                  <button
                    class="qty-btn"
                    (click)="decreaseQuantity(item)"
                    [disabled]="item.quantity <= 1 || updatingItemId() === item.id"
                    [attr.aria-label]="('cart.decrease_quantity' | translate) + ' ' + item.productName"
                    data-testid="decrease-quantity"
                  >−</button>
                  <input
                    type="number"
                    [value]="item.quantity"
                    (change)="updateQuantity(item, $event)"
                    min="1"
                    [max]="item.maxQuantity"
                    [disabled]="updatingItemId() === item.id"
                    [attr.aria-label]="('cart.quantity_for' | translate) + ' ' + item.productName"
                    data-testid="quantity-input"
                  />
                  <button
                    class="qty-btn"
                    (click)="increaseQuantity(item)"
                    [disabled]="item.quantity >= item.maxQuantity || updatingItemId() === item.id"
                    [attr.aria-label]="('cart.increase_quantity' | translate) + ' ' + item.productName"
                    data-testid="increase-quantity"
                  >+</button>
                  @if (updatingItemId() === item.id) {
                    <div class="quantity-spinner" data-testid="quantity-spinner"></div>
                  }
                </div>
                @if (itemError() === item.id) {
                  <div class="item-error" data-testid="quantity-error">
                    {{ 'cart.update_failed' | translate }}
                  </div>
                }

                <div class="item-subtotal" data-testid="cart-item-subtotal">
                  {{ item.subtotal | currency }}
                </div>

                <button
                  class="remove-btn"
                  (click)="removeItem(item)"
                  [attr.aria-label]="'cart.item.remove' | translate"
                  data-testid="remove-item"
                >
                  ✕
                </button>
              </div>
            }
          </div>

          <div class="cart-summary" appReveal="fade-up" [delay]="150">
            <h2>{{ 'cart.summary.title' | translate }}</h2>

            <div class="summary-row">
              <span>{{ 'cart.summary.subtotal' | translate }}</span>
              <span data-testid="cart-subtotal">{{ cartService.subtotal() | currency }}</span>
            </div>

            <div class="summary-row">
              <span>{{ 'cart.summary.shipping' | translate }}</span>
              <span data-testid="cart-shipping">
                @if (cartService.cart()?.shipping === 0) {
                  {{ 'cart.summary.freeShipping' | translate }}
                } @else {
                  {{ cartService.cart()?.shipping | currency }}
                }
              </span>
            </div>

            <div class="summary-row">
              <span>{{ 'cart.summary.tax' | translate }}</span>
              <span data-testid="cart-tax">{{ cartService.cart()?.tax | currency }}</span>
            </div>

            <div class="summary-row total">
              <span>{{ 'cart.summary.total' | translate }}</span>
              <span data-testid="cart-total">{{ cartService.total() | currency }}</span>
            </div>

            <a routerLink="/checkout" class="checkout-btn" data-testid="proceed-to-checkout">
              {{ 'cart.checkout' | translate }}
            </a>

            <a routerLink="/products" class="continue-link" data-testid="continue-shopping">
              {{ 'cart.continueShopping' | translate }}
            </a>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .cart-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .loading {
      text-align: center;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .error-message {
      padding: 1rem;
      background: var(--color-error-bg);
      color: var(--color-error);
      border-radius: 8px;
      margin-bottom: 1rem;
      text-align: center;
    }

    .cart-content {
      display: grid;
      grid-template-columns: 1fr 350px;
      gap: 2rem;
    }

    .cart-items {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .cart-item {
      display: grid;
      grid-template-columns: 100px 1fr auto auto auto auto;
      gap: 1rem;
      align-items: center;
      padding: 1.5rem;
      background: var(--glass-bg);
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      border: 1px solid var(--glass-border);
      border-radius: var(--radius-lg, 16px);
      box-shadow: var(--shadow-sm);
      transition: opacity 0.3s ease-out, transform 0.3s ease-out, 
                  max-height 0.3s ease-out, margin 0.3s ease-out, 
                  padding 0.3s ease-out, box-shadow 0.3s ease-out;
      max-height: 200px;
      overflow: hidden;

      &:hover:not(.removing) {
        box-shadow: var(--shadow-md);
        transform: translateY(-2px);
      }

      &.removing {
        opacity: 0;
        transform: translateX(100px);
        max-height: 0;
        margin: 0;
        padding-top: 0;
        padding-bottom: 0;
        border-width: 0;
        pointer-events: none;
      }
    }

    /* Respect reduced motion preferences */
                    @media (prefers-reduced-motion: reduce) {
                      .cart-item {
                        transition: opacity 0.15s ease-out;
                        
                        &.removing {
                          transform: none;
                          max-height: unset;
                        }
                      }
                      
                      .qty-btn,
                      .item-quantity input {
                        transition: none !important;
                        animation: none !important;
                        transform: none !important;
                      }
                    }

    .item-image {
      width: 100px;
      height: 100px;
      border-radius: 8px;
      overflow: hidden;
      background: var(--color-bg-secondary);

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      .no-image {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 2rem;
      }
    }

    .item-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;

      .item-name {
        font-weight: 600;
        color: var(--color-text-primary);
        text-decoration: none;

        &:hover {
          color: var(--color-primary);
        }
      }

      .item-variant {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      .item-sku {
        font-size: 0.75rem;
        color: var(--color-text-tertiary);
      }
    }

    .item-price {
      text-align: right;

      .original-price {
        text-decoration: line-through;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        display: block;
      }

      .sale-price {
        color: var(--color-error);
        font-weight: 600;
      }
    }

    .item-quantity {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      position: relative;

      &.updating {
        opacity: 0.7;
        pointer-events: none;
      }

      .qty-btn {
                        /* WCAG touch target: minimum 44x44px on mobile */
                        min-width: 44px;
                        min-height: 44px;
                        width: 44px;
                        height: 44px;
                        border: 1px solid var(--color-border);
                        background: var(--color-bg-secondary);
                        border-radius: 8px;
                        cursor: pointer;
                        font-size: 1.25rem;
                        font-weight: 500;
                        color: var(--color-text-primary);
                        transition: transform 0.1s ease-out, background-color 0.2s ease-out;
                        display: flex;
                        align-items: center;
                        justify-content: center;

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

                        /* Smaller on desktop where mouse precision is better */
                        @media (min-width: 768px) {
                          min-width: 36px;
                          min-height: 36px;
                          width: 36px;
                          height: 36px;
                          font-size: 1rem;
                        }
                      }

      input {
                        width: 56px;
                        min-height: 44px;
                        height: 44px;
                        text-align: center;
                        border: 1px solid var(--color-border);
                        border-radius: 8px;
                        background: var(--color-bg-secondary);
                        color: var(--color-text-primary);
                        font-size: 1rem;
                        transition: transform 0.15s ease-out, background-color 0.3s ease, border-color 0.3s ease;

                        &::-webkit-inner-spin-button,
                        &::-webkit-outer-spin-button {
                          -webkit-appearance: none;
                          margin: 0;
                        }

                        &.quantity-updated {
                          background-color: var(--color-success-light);
                          border-color: var(--color-success);
                        }
                        
                        &.incrementing {
                          animation: numberUp 0.15s ease-out;
                        }
                        
                        &.decrementing {
                          animation: numberDown 0.15s ease-out;
                        }

                        &:disabled {
                          opacity: 0.6;
                          cursor: not-allowed;
                        }

                        /* Smaller on desktop */
                        @media (min-width: 768px) {
                          width: 50px;
                          min-height: 36px;
                          height: 36px;
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

      .quantity-spinner {
        position: absolute;
        right: -28px;
        width: 18px;
        height: 18px;
        border: 2px solid var(--color-border);
        border-top-color: var(--color-primary);
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
      }
    }

    .item-error {
      grid-column: 2 / -1;
      color: var(--color-error);
      font-size: 0.75rem;
      padding: 0.25rem 0;
      animation: fadeIn 0.2s ease;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .item-subtotal {
      font-weight: 600;
      color: var(--color-text-primary);
      min-width: 80px;
      text-align: right;
    }

    .remove-btn {
      width: 32px;
      height: 32px;
      border: none;
      background: transparent;
      color: var(--color-text-secondary);
      cursor: pointer;
      font-size: 1rem;
      border-radius: 6px;
      transition: background-color 0.2s, color 0.2s;

      &:hover {
        background: var(--color-error-bg);
        color: var(--color-error);
      }
    }

    .cart-summary {
      background: var(--glass-bg-heavy);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid var(--glass-border);
      border-radius: var(--radius-xl, 20px);
      padding: 2rem;
      height: fit-content;
      position: sticky;
      top: 2rem;
      box-shadow: var(--shadow-lg);

      h2 {
        font-size: 1.25rem;
        margin-bottom: 1.5rem;
        color: var(--color-text-primary);
        font-weight: 600;
      }
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      padding: 0.75rem 0;
      border-bottom: 1px solid var(--color-border);
      color: var(--color-text-secondary);

      &.total {
        border-bottom: none;
        padding-top: 1rem;
        margin-top: 0.5rem;
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }
    }

    .checkout-btn {
      display: block;
      width: 100%;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      color: var(--color-text-inverse);
      padding: 1.125rem;
      border-radius: var(--radius-lg, 12px);
      text-align: center;
      text-decoration: none;
      font-weight: 600;
      font-size: 1.0625rem;
      margin-top: 1.5rem;
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

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 20px var(--glow-primary);

        &::before {
          opacity: 1;
        }
      }
    }

    .continue-link {
      display: block;
      text-align: center;
      margin-top: 1rem;
      color: var(--color-primary);
      text-decoration: none;

      &:hover {
        text-decoration: underline;
      }
    }

    @media (max-width: 1024px) {
      .cart-content {
        grid-template-columns: 1fr;
      }

      .cart-item {
        grid-template-columns: 80px 1fr;
        grid-template-rows: auto auto auto;
        gap: 0.75rem;
      }

      .item-image {
        width: 80px;
        height: 80px;
        grid-row: span 3;
      }

      .item-details {
        grid-column: 2;
      }

      .item-price {
        text-align: left;
      }

      .item-quantity {
        justify-self: start;
      }

      .item-subtotal {
        text-align: left;
      }

      .remove-btn {
        position: absolute;
        top: 1rem;
        right: 1rem;
      }

      .cart-item {
        position: relative;
      }
    }
  `]
})
export class CartComponent implements OnInit {
  readonly cartService = inject(CartService);
  private readonly translate = inject(TranslateService);
  private readonly toastService = inject(ToastService);

  // CART-U01 & CART-U02: Track loading and error state per item
  readonly updatingItemId = signal<string | null>(null);
  readonly itemError = signal<string | null>(null);
  
  // Track items being removed (for exit animation)
  readonly removingItems = signal<Set<string>>(new Set());

  // Store previous quantities for reverting on error
  private previousQuantities = new Map<string, number>();
  
  // Store removed items for undo functionality
  private removedItemsCache = new Map<string, CartItem>();
  
  // Animation duration in ms
  private readonly ANIMATION_DURATION = 300;

  ngOnInit(): void {
    // Always reload cart data when navigating to cart page
    // This ensures fresh data after login, reorder, or other cart modifications
    this.cartService.loadCart();
  }

  updateQuantity(item: CartItem, event: Event): void {
    const input = event.target as HTMLInputElement;
    const quantity = parseInt(input.value, 10);

    if (quantity > 0 && quantity <= item.maxQuantity) {
      this.performQuantityUpdate(item, quantity, input);
    } else {
      input.value = item.quantity.toString();
    }
  }

  increaseQuantity(item: CartItem): void {
    if (item.quantity < item.maxQuantity) {
      this.performQuantityUpdate(item, item.quantity + 1);
    }
  }

  decreaseQuantity(item: CartItem): void {
    if (item.quantity > 1) {
      this.performQuantityUpdate(item, item.quantity - 1);
    }
  }

  private performQuantityUpdate(item: CartItem, newQuantity: number, input?: HTMLInputElement): void {
    // Clear any previous error for this item
    this.itemError.set(null);
    
    // Store previous quantity for potential rollback
    this.previousQuantities.set(item.id, item.quantity);
    
    // Set loading state
    this.updatingItemId.set(item.id);

    this.cartService.updateQuantity(item.id, newQuantity).subscribe({
      next: () => {
        this.updatingItemId.set(null);
        if (input) {
          this.showQuantityFeedback(input);
        }
        this.previousQuantities.delete(item.id);
      },
      error: () => {
        this.updatingItemId.set(null);
        this.itemError.set(item.id);
        
        // Revert quantity in UI by reloading cart
        this.cartService.loadCart();
        
        // Clear error after 5 seconds
        setTimeout(() => {
          if (this.itemError() === item.id) {
            this.itemError.set(null);
          }
        }, 5000);
      }
    });
  }

  private showQuantityFeedback(input: HTMLInputElement): void {
    // Add visual feedback by briefly highlighting the quantity input
    input.classList.add('quantity-updated');
    setTimeout(() => input.classList.remove('quantity-updated'), 300);
  }

  async removeItem(item: CartItem): Promise<void> {
    // Cache item for potential undo
    this.removedItemsCache.set(item.id, { ...item });
    
    // Mark as removing (triggers exit animation)
    this.removingItems.update(set => new Set(set).add(item.id));
    
    // Wait for animation to complete
    await this.waitForAnimation();
    
    // Actually remove from backend
    this.cartService.removeItem(item.id).subscribe({
      next: () => {
        // Clean up removing state
        this.removingItems.update(set => {
          const newSet = new Set(set);
          newSet.delete(item.id);
          return newSet;
        });
        
        // Show undo toast
        this.showUndoToast(item);
      },
      error: () => {
        // Remove from removing state on error
        this.removingItems.update(set => {
          const newSet = new Set(set);
          newSet.delete(item.id);
          return newSet;
        });
        
        // Reload cart to restore item
        this.cartService.loadCart();
        
        // Show error toast
        this.toastService.error(
          this.translate.instant('cart.remove_failed')
        );
      }
    });
  }
  
  private waitForAnimation(): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, this.ANIMATION_DURATION));
  }
  
  private showUndoToast(item: CartItem): void {
    const message = this.translate.instant('cart.item_removed', { name: item.productName });
    const undoText = this.translate.instant('common.undo');
    
    // Show toast with longer duration for undo action
    this.toastService.success(
      `${message} - ${undoText}`,
      { duration: 6000, dismissible: true }
    );
    
    // Note: For a full undo implementation, we would need to add 
    // an action callback to the toast service. For now, the user
    // can re-add the item from the product page.
  }
  
  /**
   * Undo removal of an item (re-add to cart)
   * This can be called programmatically if undo action is triggered
   */
  undoRemoval(itemId: string): void {
    const cachedItem = this.removedItemsCache.get(itemId);
    if (cachedItem) {
      this.cartService.addToCart(
        cachedItem.productId,
        cachedItem.quantity,
        cachedItem.variantId
      ).subscribe({
        next: () => {
          this.removedItemsCache.delete(itemId);
          this.toastService.success(
            this.translate.instant('cart.item_restored')
          );
        },
        error: () => {
          this.toastService.error(
            this.translate.instant('cart.restore_failed')
          );
        }
      });
    }
  }
}
