import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { CartService } from '../../core/services/cart.service';
import { CartItem } from '../../core/models/cart.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule],
  template: `
    <div class="cart-container" data-testid="cart-page">
      <h1>{{ 'cart.title' | translate }}</h1>

      @if (cartService.isLoading()) {
        <div class="loading" data-testid="cart-loading">
          {{ 'common.loading' | translate }}
        </div>
      } @else if (cartService.isEmpty()) {
        <div class="empty-cart" data-testid="empty-cart">
          <div class="empty-icon">🛒</div>
          <p>{{ 'cart.empty' | translate }}</p>
          <a routerLink="/products" class="continue-shopping-btn" data-testid="continue-shopping">
            {{ 'cart.continueShopping' | translate }}
          </a>
        </div>
      } @else {
        <div class="cart-content">
          <div class="cart-items">
            @for (item of cartService.items(); track item.id) {
              <div class="cart-item" data-testid="cart-item">
                <div class="item-image">
                  @if (item.imageUrl) {
                    <img [src]="item.imageUrl" [alt]="item.productName" />
                  } @else {
                    <div class="no-image">📦</div>
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

                <div class="item-quantity" data-testid="item-quantity">
                  <button
                    class="qty-btn"
                    (click)="decreaseQuantity(item)"
                    [disabled]="item.quantity <= 1"
                    data-testid="decrease-quantity"
                  >−</button>
                  <input
                    type="number"
                    [value]="item.quantity"
                    (change)="updateQuantity(item, $event)"
                    min="1"
                    [max]="item.maxQuantity"
                  />
                  <button
                    class="qty-btn"
                    (click)="increaseQuantity(item)"
                    [disabled]="item.quantity >= item.maxQuantity"
                    data-testid="increase-quantity"
                  >+</button>
                </div>

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

          <div class="cart-summary">
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

    .empty-cart {
      text-align: center;
      padding: 4rem 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;

      .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
        opacity: 0.5;
      }

      p {
        font-size: 1.25rem;
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }
    }

    .continue-shopping-btn {
      display: inline-block;
      background: var(--color-primary);
      color: white;
      padding: 1rem 2rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      transition: background-color 0.2s;

      &:hover {
        background: var(--color-primary-dark);
      }
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
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
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

      .qty-btn {
        width: 32px;
        height: 32px;
        border: 1px solid var(--color-border);
        background: var(--color-bg-secondary);
        border-radius: 6px;
        cursor: pointer;
        font-size: 1rem;
        color: var(--color-text-primary);
        transition: background-color 0.2s;

        &:hover:not(:disabled) {
          background: var(--color-border);
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }

      input {
        width: 50px;
        height: 32px;
        text-align: center;
        border: 1px solid var(--color-border);
        border-radius: 6px;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        font-size: 1rem;

        &::-webkit-inner-spin-button,
        &::-webkit-outer-spin-button {
          -webkit-appearance: none;
          margin: 0;
        }
      }
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
        background: var(--color-error-bg, #fee2e2);
        color: var(--color-error);
      }
    }

    .cart-summary {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;
      height: fit-content;
      position: sticky;
      top: 2rem;

      h2 {
        font-size: 1.25rem;
        margin-bottom: 1.5rem;
        color: var(--color-text-primary);
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
      background: var(--color-primary);
      color: white;
      padding: 1rem;
      border-radius: 8px;
      text-align: center;
      text-decoration: none;
      font-weight: 600;
      margin-top: 1.5rem;
      transition: background-color 0.2s;

      &:hover {
        background: var(--color-primary-dark);
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
export class CartComponent {
  readonly cartService = inject(CartService);

  updateQuantity(item: CartItem, event: Event): void {
    const input = event.target as HTMLInputElement;
    const quantity = parseInt(input.value, 10);

    if (quantity > 0 && quantity <= item.maxQuantity) {
      this.cartService.updateQuantity(item.id, quantity).subscribe();
    } else {
      input.value = item.quantity.toString();
    }
  }

  increaseQuantity(item: CartItem): void {
    if (item.quantity < item.maxQuantity) {
      this.cartService.updateQuantity(item.id, item.quantity + 1).subscribe();
    }
  }

  decreaseQuantity(item: CartItem): void {
    if (item.quantity > 1) {
      this.cartService.updateQuantity(item.id, item.quantity - 1).subscribe();
    }
  }

  removeItem(item: CartItem): void {
    this.cartService.removeItem(item.id).subscribe();
  }
}
