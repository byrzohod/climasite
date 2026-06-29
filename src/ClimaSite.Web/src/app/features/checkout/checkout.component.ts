import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { shippingCostFor as computeShippingCost } from '../../core/pricing/shipping';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService, CheckoutStep } from '../../core/services/checkout.service';
import { AddressService } from '../../core/services/address.service';
import { AuthService } from '../../auth/services/auth.service';
import { PaymentService } from '../../core/services/payment.service';
import { ConfettiService } from '../../core/services/confetti.service';
import { Address } from '../../core/models/order.model';
import { SavedAddress } from '../../core/models/address.model';
import { IconComponent } from '../../shared/components/icon';
import { apiErrorToTranslationKey, toTranslationKey } from '../../core/utils/translation-key.util';
import { DualPricePipe } from '../../shared/pipes/dual-price.pipe';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, TranslateModule, IconComponent, DualPricePipe],
  template: `
    <div class="checkout-container" data-testid="checkout-page">
      <h1>{{ 'checkout.title' | translate }}</h1>

      @if (orderPlaced()) {
        <!-- Order Confirmation - show this even if cart is empty (because we just cleared it) -->
        <div class="checkout-section" data-testid="order-confirmation">
          <div class="confirmation-content">
            <div class="confirmation-icon">✓</div>
            <h2>{{ 'checkout.orderComplete.title' | translate }}</h2>
            <p>{{ 'checkout.orderComplete.message' | translate }}</p>
            <p class="order-number-label">{{ 'checkout.orderComplete.orderNumber' | translate }}:</p>
            <p class="order-number" data-testid="order-number">{{ checkoutService.lastOrderId() }}</p>
            <div class="confirmation-actions">
              <a [routerLink]="['/account/orders', checkoutService.lastOrderId()]" class="btn-primary" data-testid="view-order-btn">
                {{ 'checkout.orderComplete.viewOrder' | translate }}
              </a>
              <a routerLink="/products" class="btn-secondary">
                {{ 'checkout.orderComplete.continueShopping' | translate }}
              </a>
            </div>
          </div>
        </div>
      } @else if (cartService.loadFailed()) {
        <!-- B-020: a cart load failure must not masquerade as an empty cart here either. -->
        <div class="checkout-cart-error" role="alert" data-testid="checkout-cart-error">
          <p>{{ cartService.error() | translate }}</p>
          <button class="btn-primary" (click)="cartService.loadCart()" data-testid="checkout-cart-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      } @else if (cartService.isEmpty()) {
        <div class="empty-cart" data-testid="checkout-empty">
          <p>{{ 'cart.empty' | translate }}</p>
          <a routerLink="/products" class="btn-primary">{{ 'cart.continueShopping' | translate }}</a>
        </div>
      } @else {
        <!-- Progress Steps -->
        <div class="checkout-steps" data-testid="checkout-steps">
          <div class="step" [class.active]="checkoutService.currentStep() === 'shipping'" [class.completed]="isStepCompleted('shipping')">
            <span class="step-number">1</span>
            <span class="step-label">{{ 'checkout.steps.shipping' | translate }}</span>
          </div>
          <div class="step-line"></div>
          <div class="step" [class.active]="checkoutService.currentStep() === 'payment'" [class.completed]="isStepCompleted('payment')">
            <span class="step-number">2</span>
            <span class="step-label">{{ 'checkout.steps.payment' | translate }}</span>
          </div>
          <div class="step-line"></div>
          <div class="step" [class.active]="checkoutService.currentStep() === 'review'" [class.completed]="orderPlaced()">
            <span class="step-number">3</span>
            <span class="step-label">{{ 'checkout.steps.review' | translate }}</span>
          </div>
        </div>

        <div class="checkout-content">
          <div class="checkout-main">
            <!-- Shipping Step -->
            @if (checkoutService.currentStep() === 'shipping') {
              <div class="checkout-section" data-testid="shipping-section">
                <h2>{{ 'checkout.shipping.title' | translate }}</h2>

                <!-- Saved Addresses for Authenticated Users -->
                @if (authService.isAuthenticated() && addressService.hasAddresses()) {
                  <div class="saved-addresses-section" data-testid="saved-addresses-section">
                    <h3>{{ 'checkout.shipping.useSavedAddress' | translate }}</h3>
                    <div class="saved-addresses-list">
                      @for (address of addressService.addresses(); track address.id) {
                        <div
                          class="saved-address-card"
                          [class.selected]="selectedAddressId() === address.id"
                          (click)="selectSavedAddress(address)"
                          data-testid="saved-address-card"
                        >
                          @if (address.isDefault) {
                            <span class="default-badge">{{ 'account.addresses.default' | translate }}</span>
                          }
                          <p class="address-name">{{ address.fullName }}</p>
                          <p>{{ address.addressLine1 }}</p>
                          <p>{{ address.city }}, {{ address.postalCode }}</p>
                          <p>{{ address.country }}</p>
                        </div>
                      }
                    </div>
                    <div class="separator">
                      <span>{{ 'checkout.shipping.orEnterNew' | translate }}</span>
                    </div>
                  </div>
                }

                <form [formGroup]="shippingForm" (ngSubmit)="submitShipping()" data-testid="checkout-form">
                  <div class="form-row">
                    <div class="form-group" data-testid="shipping-firstname">
                      <label for="firstName">{{ 'checkout.shipping.firstName' | translate }}</label>
                      <input type="text" id="firstName" formControlName="firstName" />
                    </div>
                    <div class="form-group" data-testid="shipping-lastname">
                      <label for="lastName">{{ 'checkout.shipping.lastName' | translate }}</label>
                      <input type="text" id="lastName" formControlName="lastName" />
                    </div>
                  </div>

                  <div class="form-group" data-testid="shipping-email">
                    <label for="email">{{ 'contact.form.email' | translate }}</label>
                    <input type="email" id="email" formControlName="email" />
                  </div>

                  <div class="form-group" data-testid="shipping-street">
                    <label for="addressLine1">{{ 'checkout.shipping.address' | translate }}</label>
                    <input type="text" id="addressLine1" formControlName="addressLine1" />
                  </div>

                  <div class="form-group" data-testid="shipping-apartment">
                    <label for="addressLine2">{{ 'checkout.shipping.apartment' | translate }}</label>
                    <input type="text" id="addressLine2" formControlName="addressLine2" />
                  </div>

                  <div class="form-row">
                    <div class="form-group" data-testid="shipping-city">
                      <label for="city">{{ 'checkout.shipping.city' | translate }}</label>
                      <input type="text" id="city" formControlName="city" />
                    </div>
                    <div class="form-group" data-testid="shipping-state">
                      <label for="state">{{ 'checkout.shipping.state' | translate }}</label>
                      <input type="text" id="state" formControlName="state" />
                    </div>
                  </div>

                  <div class="form-row">
                    <div class="form-group" data-testid="shipping-postal-code">
                      <label for="postalCode">{{ 'checkout.shipping.postalCode' | translate }}</label>
                      <input type="text" id="postalCode" formControlName="postalCode" />
                    </div>
                    <div class="form-group" data-testid="shipping-country">
                      <label for="country">{{ 'checkout.shipping.country' | translate }}</label>
                      <select id="country" formControlName="country">
                        <option value="Bulgaria">{{ 'countries.bulgaria' | translate }}</option>
                        <option value="Germany">{{ 'countries.germany' | translate }}</option>
                        <option value="Austria">{{ 'countries.austria' | translate }}</option>
                        <option value="Romania">{{ 'countries.romania' | translate }}</option>
                        <option value="Greece">{{ 'countries.greece' | translate }}</option>
                      </select>
                    </div>
                  </div>

                  <div class="form-group" data-testid="shipping-phone">
                    <label for="phone">{{ 'checkout.shipping.phone' | translate }}</label>
                    <input type="tel" id="phone" formControlName="phone" />
                  </div>

                  <button type="submit" class="btn-primary" [disabled]="shippingForm.invalid" data-testid="next-step">
                    {{ 'common.next' | translate }}: {{ 'checkout.steps.payment' | translate }}
                  </button>
                </form>
              </div>
            }

            <!-- Payment Step -->
            @if (checkoutService.currentStep() === 'payment') {
              <div class="checkout-section" data-testid="payment-section">
                <h2>{{ 'checkout.shipping.method' | translate }}</h2>

                <div class="shipping-methods" data-testid="shipping-methods">
                  <label class="payment-option" [class.selected]="checkoutService.shippingMethod() === 'standard'">
                    <input type="radio" name="shippingMethod" value="standard" [checked]="checkoutService.shippingMethod() === 'standard'" (change)="selectShippingMethod('standard')" data-testid="shipping-standard" />
                    <span class="payment-icon"><app-icon name="package" size="lg" /></span>
                    <span>{{ 'checkout.shipping.standard' | translate }} ({{ 'checkout.shipping.standardTime' | translate }}) -
                      @if (shippingCost.standard === 0) {
                        {{ 'cart.summary.freeShipping' | translate }}
                      } @else {
                        {{ shippingCost.standard | dualPrice }}
                      }
                    </span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.shippingMethod() === 'express'">
                    <input type="radio" name="shippingMethod" value="express" [checked]="checkoutService.shippingMethod() === 'express'" (change)="selectShippingMethod('express')" data-testid="shipping-express" />
                    <span class="payment-icon"><app-icon name="truck" size="lg" /></span>
                    <span>{{ 'checkout.shipping.express' | translate }} ({{ 'checkout.shipping.expressTime' | translate }}) - {{ shippingCost.express | dualPrice }}</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.shippingMethod() === 'overnight'">
                    <input type="radio" name="shippingMethod" value="overnight" [checked]="checkoutService.shippingMethod() === 'overnight'" (change)="selectShippingMethod('overnight')" data-testid="shipping-overnight" />
                    <span class="payment-icon"><app-icon name="zap" size="lg" /></span>
                    <span>{{ 'checkout.shipping.overnight' | translate }} ({{ 'checkout.shipping.overnightTime' | translate }}) - {{ shippingCost.overnight | dualPrice }}</span>
                  </label>
                </div>

                <h2 style="margin-top: 2rem;">{{ 'checkout.payment.title' | translate }}</h2>

                <div class="payment-methods">
                  <label class="payment-option" [class.selected]="checkoutService.paymentMethod() === 'card'">
                    <input type="radio" name="paymentMethod" value="card" [checked]="checkoutService.paymentMethod() === 'card'" (change)="selectPaymentMethod('card')" data-testid="payment-card" />
                    <span class="payment-icon"><app-icon name="credit-card" size="lg" /></span>
                    <span>{{ 'checkout.payment.card' | translate }}</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.paymentMethod() === 'bank'">
                    <input type="radio" name="paymentMethod" value="bank" [checked]="checkoutService.paymentMethod() === 'bank'" (change)="selectPaymentMethod('bank')" data-testid="payment-bank" />
                    <span class="payment-icon"><app-icon name="building-2" size="lg" /></span>
                    <span>{{ 'checkout.payment.bank' | translate }}</span>
                  </label>
                </div>

                @if (checkoutService.paymentMethod() === 'card') {
                  <div class="card-form" data-testid="card-form">
                    @if (!paymentService.isInitialized()) {
                      <div class="loading-stripe">
                        <p>{{ 'checkout.payment.loadingStripe' | translate }}</p>
                      </div>
                    } @else {
                      <div class="form-group">
                        <label>{{ 'checkout.payment.cardDetails' | translate }}</label>
                        <div id="stripe-card-element" class="stripe-element" data-testid="stripe-card-element"></div>
                        @if (paymentService.error()) {
                          <p class="stripe-error">{{ paymentService.error() | translate }}</p>
                        }
                      </div>
                    }
                  </div>
                }

                @if (checkoutService.paymentMethod() === 'bank') {
                  <div class="bank-info-panel" data-testid="bank-info-panel">
                    <h3>{{ 'checkout.payment.bankInstructions.title' | translate }}</h3>
                    @if (paymentService.bankTransfer(); as bank) {
                      <dl class="bank-details">
                        <div class="bank-detail-row">
                          <dt>{{ 'checkout.payment.bankInstructions.accountName' | translate }}</dt>
                          <dd>{{ bank.accountName }}</dd>
                        </div>
                        <div class="bank-detail-row">
                          <dt>{{ 'checkout.payment.bankInstructions.iban' | translate }}</dt>
                          <dd>{{ bank.iban }}</dd>
                        </div>
                        <div class="bank-detail-row">
                          <dt>{{ 'checkout.payment.bankInstructions.bankName' | translate }}</dt>
                          <dd>{{ bank.bankName }}</dd>
                        </div>
                      </dl>
                    }
                    <p class="bank-note">{{ 'checkout.payment.bankInstructions.beforeOrderNote' | translate }}</p>
                  </div>
                }

                <div class="step-actions">
                  <button type="button" class="btn-secondary" (click)="goToStep('shipping')" data-testid="previous-step">
                    {{ 'common.back' | translate }}
                  </button>
                  <button type="button" class="btn-primary" (click)="proceedFromPayment()" data-testid="next-step">
                    {{ 'common.next' | translate }}: {{ 'checkout.steps.review' | translate }}
                  </button>
                </div>
              </div>
            }

            <!-- Review Step -->
            @if (checkoutService.currentStep() === 'review') {
              <div class="checkout-section" data-testid="review-section">
                <h2>{{ 'checkout.review.title' | translate }}</h2>

                @if (checkoutService.error()) {
                  <div class="error-message" data-testid="checkout-error">
                    {{ checkoutService.error() | translate }}
                  </div>
                }

                <div class="review-section">
                  <h3>{{ 'checkout.review.shippingAddress' | translate }}</h3>
                  @if (checkoutService.shippingAddress(); as addr) {
                    <div class="address-display">
                      <p>{{ addr.firstName }} {{ addr.lastName }}</p>
                      <p>{{ addr.addressLine1 }}</p>
                      @if (addr.addressLine2) {
                        <p>{{ addr.addressLine2 }}</p>
                      }
                      <p>{{ addr.city }}, {{ addr.state }} {{ addr.postalCode }}</p>
                      <p>{{ addr.country }}</p>
                      <p>{{ addr.phone }}</p>
                    </div>
                  }
                  <button type="button" class="btn-link" (click)="goToStep('shipping')">{{ 'common.edit' | translate }}</button>
                </div>

                <div class="review-section">
                  <h3>{{ 'checkout.review.paymentMethod' | translate }}</h3>
                  <p class="payment-display">
                    @switch (checkoutService.paymentMethod()) {
                      @case ('card') { <app-icon name="credit-card" size="sm" /> {{ 'checkout.payment.card' | translate }} }
                      @case ('bank') { <app-icon name="building-2" size="sm" /> {{ 'checkout.payment.bank' | translate }} }
                    }
                  </p>
                  <button type="button" class="btn-link" (click)="goToStep('payment')">{{ 'common.edit' | translate }}</button>
                </div>

                <div class="review-section">
                  <h3>{{ 'checkout.review.items' | translate }}</h3>
                  <div class="order-items">
                    @for (item of cartService.items(); track item.id) {
                      <div class="order-item">
                        <span class="item-qty">{{ item.quantity }}x</span>
                        <span class="item-name">{{ item.productName }}</span>
                        <span class="item-price">{{ item.subtotal | dualPrice }}</span>
                      </div>
                    }
                  </div>
                </div>

                <div class="step-actions">
                  <button type="button" class="btn-secondary" (click)="goToStep('payment')" data-testid="previous-step">
                    {{ 'common.back' | translate }}
                  </button>
                  <button type="button" class="btn-primary btn-place-order" [disabled]="checkoutService.isProcessing() || placingOrder()" (click)="placeOrder()" data-testid="place-order">
                    @if (checkoutService.isProcessing() || placingOrder()) {
                      {{ 'common.loading' | translate }}
                    } @else {
                      {{ 'checkout.placeOrder' | translate }}
                    }
                  </button>
                </div>
              </div>
            }

          </div>

          <!-- Order Summary Sidebar -->
          <div class="checkout-sidebar">
            <div class="order-summary" data-testid="order-summary">
              <h3>{{ 'cart.summary.title' | translate }}</h3>

              <div class="summary-items">
                @for (item of cartService.items(); track item.id) {
                  <div class="summary-item">
                    <span class="item-info">{{ item.productName }} × {{ item.quantity }}</span>
                    <span class="item-price">{{ item.subtotal | dualPrice }}</span>
                  </div>
                }
              </div>

              <div class="summary-totals">
                <div class="summary-row">
                  <span>{{ 'cart.summary.subtotal' | translate }}</span>
                  <span>{{ cartService.subtotal() | dualPrice }}</span>
                </div>
                <div class="summary-row">
                  <span>{{ 'cart.summary.shipping' | translate }}</span>
                  <span data-testid="summary-shipping">
                    @if (selectedShippingCost() === 0) {
                      {{ 'cart.summary.freeShipping' | translate }}
                    } @else {
                      {{ selectedShippingCost() | dualPrice }}
                    }
                  </span>
                </div>
                <div class="summary-row">
                  <span>{{ 'cart.summary.tax' | translate }}</span>
                  <span>{{ cartService.cart()?.tax | dualPrice }}</span>
                </div>
                <div class="summary-row total">
                  <span>{{ 'cart.summary.total' | translate }}</span>
                  <span data-testid="order-total">{{ summaryTotal() | dualPrice }}</span>
                </div>
              </div>

              <a routerLink="/cart" class="back-to-cart-link" data-testid="back-to-cart">
                ← {{ 'checkout.backToCart' | translate }}
              </a>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .checkout-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .empty-cart {
      text-align: center;
      padding: 3rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;

      p {
        margin-bottom: 1.5rem;
        color: var(--color-text-secondary);
      }
    }

    .checkout-cart-error {
      text-align: center;
      padding: 3rem;
      background: var(--color-error-bg);
      border-radius: 12px;

      p {
        margin-bottom: 1.5rem;
        color: var(--color-error);
      }

      .btn-primary {
        padding: 0.75rem 1.5rem;
        background: var(--color-primary);
        color: var(--color-text-inverse);
        border: none;
        border-radius: 8px;
        cursor: pointer;
        font-weight: 500;
        transition: background 0.2s ease;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    .checkout-steps {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin-bottom: 2rem;
      padding: 1.5rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
    }

    .step {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      border-radius: 8px;
      color: var(--color-text-secondary);

      &.active {
        background: var(--color-primary-light);
        color: var(--color-primary);
        font-weight: 600;
      }

      &.completed {
        color: var(--color-success);
      }
    }

    .step-number {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border: 2px solid currentColor;
      border-radius: 50%;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .step-line {
      flex: 1;
      max-width: 60px;
      height: 2px;
      background: var(--color-border);
    }

    .checkout-content {
      display: grid;
      grid-template-columns: 1fr 350px;
      gap: 2rem;
    }

    .checkout-section {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 2rem;

      h2 {
        font-size: 1.25rem;
        color: var(--color-text-primary);
        margin-bottom: 1.5rem;
      }
    }

    .saved-addresses-section {
      margin-bottom: 1.5rem;

      h3 {
        font-size: 1rem;
        color: var(--color-text-secondary);
        margin-bottom: 1rem;
      }
    }

    .saved-addresses-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .saved-address-card {
      padding: 1rem;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      cursor: pointer;
      transition: border-color 0.2s, background-color 0.2s;
      position: relative;

      &:hover {
        border-color: var(--color-primary);
      }

      &.selected {
        border-color: var(--color-primary);
        background-color: var(--color-primary-light);
      }

      .default-badge {
        position: absolute;
        top: -8px;
        right: 8px;
        background: var(--color-primary);
        color: white;
        font-size: 0.65rem;
        padding: 0.15rem 0.5rem;
        border-radius: 4px;
      }

      p {
        margin: 0;
        font-size: 0.875rem;
        color: var(--color-text-primary);
        line-height: 1.4;

        &.address-name {
          font-weight: 600;
          margin-bottom: 0.25rem;
        }
      }
    }

    .separator {
      display: flex;
      align-items: center;
      text-align: center;
      margin-bottom: 1rem;

      &::before,
      &::after {
        content: '';
        flex: 1;
        border-bottom: 1px solid var(--color-border);
      }

      span {
        padding: 0 1rem;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .form-group {
      margin-bottom: 1rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      input, select {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        font-size: 1rem;
        transition: border-color 0.2s ease-out, box-shadow 0.2s ease-out, background-color 0.2s ease-out;

        &:hover:not(:disabled):not(:focus) {
          border-color: var(--color-border-secondary);
        }

        &:focus {
          outline: none;
          border-color: var(--color-primary);
          box-shadow: 0 0 0 3px var(--color-primary-light);
        }

        &.invalid {
          border-color: var(--color-error);
          animation: inputShake 0.4s ease-out;

          &:focus {
            box-shadow: 0 0 0 3px var(--color-error-light);
          }
        }
      }

      select {
        cursor: pointer;
        appearance: none;
        background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
        background-position: right 0.75rem center;
        background-repeat: no-repeat;
        background-size: 1.25em 1.25em;
        padding-right: 2.5rem;
      }

      .field-error {
        display: block;
        color: var(--color-error);
        font-size: 0.75rem;
        margin-top: 0.25rem;
        animation: errorSlideIn 0.2s ease-out forwards;
      }
    }

    @keyframes inputShake {
      0%, 100% { transform: translateX(0); }
      20% { transform: translateX(-6px); }
      40% { transform: translateX(6px); }
      60% { transform: translateX(-4px); }
      80% { transform: translateX(4px); }
    }

    @keyframes errorSlideIn {
      from {
        opacity: 0;
        transform: translateY(-5px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .payment-methods {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .payment-option {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      cursor: pointer;
      transition: border-color 0.2s ease-out, background-color 0.2s ease-out, transform 0.15s ease-out, box-shadow 0.2s ease-out;

      &:hover {
        border-color: var(--color-primary);
        transform: translateY(-1px);
      }

      &:active {
        transform: translateY(0) scale(0.99);
      }

      &.selected {
        border-color: var(--color-primary);
        background: var(--color-primary-light);
        box-shadow: 0 0 0 1px var(--color-primary);
      }

      .payment-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-text-secondary);
        transition: transform 0.2s ease-out, color 0.2s ease-out;
      }

      &:hover .payment-icon {
        transform: scale(1.1);
        color: var(--color-primary);
      }

      &.selected .payment-icon {
        color: var(--color-primary);
      }

      input {
        display: none;
      }
    }

    .card-form {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }

    .bank-info-panel {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      margin-bottom: 1.5rem;

      h3 {
        font-size: 1rem;
        color: var(--color-text-primary);
        margin: 0 0 1rem;
      }

      .bank-details {
        margin: 0 0 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .bank-detail-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;

        dt {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
        }

        dd {
          margin: 0;
          color: var(--color-text-primary);
          font-weight: 500;
          font-family: monospace;
          text-align: right;
          word-break: break-all;
        }
      }

      .bank-note {
        margin: 0;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .stripe-element {
      padding: 0.75rem;
      border: 1px solid var(--color-border);
      border-radius: 8px;
      background-color: var(--color-bg-primary);
      min-height: 40px;
    }

    .stripe-error {
      color: var(--color-error);
      font-size: 0.875rem;
      margin-top: 0.5rem;
    }

    .loading-stripe {
      text-align: center;
      padding: 1rem;
      color: var(--color-text-secondary);
    }

    .step-actions {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      margin-top: 2rem;
    }

    .btn-primary {
      padding: 0.875rem 1.5rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .btn-secondary {
      padding: 0.875rem 1.5rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover {
        background: var(--color-border);
      }
    }

    .btn-link {
      background: none;
      border: none;
      color: var(--color-primary);
      cursor: pointer;
      font-size: 0.875rem;

      &:hover {
        text-decoration: underline;
      }
    }

    .btn-place-order {
      flex: 1;
      padding: 1rem;
      font-size: 1.125rem;
    }

    .review-section {
      padding: 1rem 0;
      border-bottom: 1px solid var(--color-border);

      &:last-of-type {
        border-bottom: none;
      }

      h3 {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        margin-bottom: 0.5rem;
      }
    }

    .address-display {
      p {
        margin: 0.25rem 0;
        color: var(--color-text-primary);
      }
    }

    .payment-display {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: var(--color-text-primary);
    }

    .order-items {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .order-item {
      display: flex;
      gap: 1rem;

      .item-qty {
        color: var(--color-text-secondary);
        min-width: 30px;
      }

      .item-name {
        flex: 1;
        color: var(--color-text-primary);
      }

      .item-price {
        color: var(--color-text-primary);
        font-weight: 500;
      }
    }

    .error-message {
      padding: 1rem;
      background: var(--color-error-bg);
      color: var(--color-error);
      border-radius: 8px;
      margin-bottom: 1rem;
    }

    .checkout-sidebar {
      position: sticky;
      top: 2rem;
      height: fit-content;
    }

    .order-summary {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;

      h3 {
        font-size: 1.125rem;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
      }
    }

    .summary-items {
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: 1rem;
    }

    .summary-item {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      font-size: 0.875rem;

      .item-info {
        color: var(--color-text-secondary);
      }

      .item-price {
        color: var(--color-text-primary);
      }
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      color: var(--color-text-secondary);

      &.total {
        padding-top: 1rem;
        margin-top: 0.5rem;
        border-top: 1px solid var(--color-border);
        font-size: 1.125rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }
    }

    .back-to-cart-link {
      display: block;
      text-align: center;
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border);
      color: var(--color-primary);
      text-decoration: none;
      font-size: 0.875rem;

      &:hover {
        text-decoration: underline;
      }
    }

    .confirmation-content {
      text-align: center;
      padding: 2rem;

      .confirmation-icon {
        width: 80px;
        height: 80px;
        margin: 0 auto 1.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-success);
        color: var(--color-text-inverse);
        font-size: 2.5rem;
        border-radius: 50%;
      }

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 0.5rem;
      }

      .order-number-label {
        font-size: 0.875rem;
        margin-top: 1.5rem;
      }

      .order-number {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-primary);
        margin-bottom: 2rem;
      }

      .confirmation-actions {
        display: flex;
        gap: 1rem;
        justify-content: center;
        flex-wrap: wrap;
      }

      .btn-primary, .btn-secondary {
        display: inline-block;
        padding: 0.75rem 1.5rem;
        border-radius: 8px;
        font-weight: 600;
        text-decoration: none;
      }

      .btn-secondary {
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border);

        &:hover {
          background: var(--color-border);
        }
      }
    }

    @media (max-width: 1024px) {
      .checkout-content {
        grid-template-columns: 1fr;
      }

      .checkout-sidebar {
        position: static;
      }
    }

    @media (max-width: 640px) {
      .checkout-steps {
        flex-wrap: wrap;
      }

      .step-line {
        display: none;
      }

      .form-row {
        grid-template-columns: 1fr;
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .form-group input,
      .form-group select,
      .form-group .field-error,
      .payment-option,
      .payment-option .payment-icon,
      .payment-option .payment-icon app-icon {
        transition: none !important;
        animation: none !important;
      }

      .form-group input.invalid {
        border-color: var(--color-error);
      }

      .form-group .field-error {
        opacity: 1;
        transform: translateY(0);
      }

      .payment-option:hover,
      .payment-option:active {
        transform: none !important;
      }
    }
  `]
})
export class CheckoutComponent implements OnInit, OnDestroy {
  readonly cartService = inject(CartService);
  readonly checkoutService = inject(CheckoutService);
  readonly addressService = inject(AddressService);
  readonly authService = inject(AuthService);
  readonly paymentService = inject(PaymentService);
  private readonly confettiService = inject(ConfettiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  orderPlaced = signal(false);
  selectedAddressId = signal<string | null>(null);
  private clientSecret = signal<string | null>(null);
  // BUG-04: double-submit guard. Set at the very top of placeOrder() — before the
  // create-intent call — so a rapid double-click can't fire two intent/charge sequences.
  // checkoutService.isProcessing() only flips inside createOrder(), which runs too late.
  readonly placingOrder = signal(false);
  // Stripe PaymentMethod id captured from the card element on the payment step (the element is
  // removed from the DOM on the review step, so we cannot confirm with the live element there).
  private cardPaymentMethodId: string | null = null;

ngOnInit(): void {
    // Load saved addresses if user is authenticated
    if (this.authService.isAuthenticated()) {
      this.addressService.loadAddresses();
    }
  }

  ngOnDestroy(): void {
    // Stop confetti animation if component is destroyed
    this.confettiService.stop();
  }

  private async initializeStripe(): Promise<void> {
    const initialized = await this.paymentService.initialize();
    if (initialized) {
      // Small delay to ensure DOM is ready
      setTimeout(() => {
        this.paymentService.createElements('stripe-card-element');
      }, 100);
    }
  }

  shippingForm: FormGroup = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    addressLine1: ['', Validators.required],
    addressLine2: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['Bulgaria', Validators.required],
    phone: ['', Validators.required]
  });

  isStepCompleted(step: CheckoutStep): boolean {
    if (step === 'shipping') {
      return this.checkoutService.shippingAddress() !== null;
    }
    if (step === 'payment') {
      return this.checkoutService.paymentMethod() !== '';
    }
    return false;
  }

  goToStep(step: CheckoutStep): void {
    this.checkoutService.setStep(step);

    // Initialize Stripe when entering payment step
    if (step === 'payment' && this.checkoutService.paymentMethod() === 'card') {
      this.initializeStripe();
    }
  }

  selectSavedAddress(address: SavedAddress): void {
    this.selectedAddressId.set(address.id);

    // Parse fullName into firstName and lastName
    const nameParts = address.fullName.split(' ');
    const firstName = nameParts[0] || '';
    const lastName = nameParts.slice(1).join(' ') || '';

    // Fill the form with the saved address data
    this.shippingForm.patchValue({
      firstName,
      lastName,
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2 || '',
      city: address.city,
      state: address.state || '',
      postalCode: address.postalCode,
      country: address.country,
      phone: address.phone || ''
    });
  }

  submitShipping(): void {
    if (this.shippingForm.invalid) return;

    const formValue = this.shippingForm.value;
    const address: Address = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      addressLine1: formValue.addressLine1,
      addressLine2: formValue.addressLine2 || undefined,
      city: formValue.city,
      state: formValue.state,
      postalCode: formValue.postalCode,
      country: formValue.country,
      phone: formValue.phone
    };

    this.checkoutService.setShippingAddress(address);
    // Route through goToStep (not setStep) so Stripe Elements initialize when the payment step is
    // entered via the shipping "Next" button. Card is the default method, so its (change) event
    // never fires on this path — without this the card field stays stuck on "Loading Stripe…".
    this.goToStep('payment');
  }

  selectPaymentMethod(method: string): void {
    this.checkoutService.setPaymentMethod(method);

    // Initialize Stripe when card payment is selected
    if (method === 'card') {
      this.initializeStripe();
    } else {
      this.paymentService.destroyElements();
    }

    // GAP-06: load bank-transfer details so the instructions panel can render.
    if (method === 'bank') {
      this.paymentService.loadConfig();
    }
  }

  /** Current cart subtotal as the server computes it (sum of line totals). */
  private cartSubtotal(): number {
    return this.cartService.subtotal?.() ?? 0;
  }

  /**
   * DEC-SHIPPING — threshold-aware shipping cost for a single method. Delegates to the shared client
   * helper (`core/pricing/shipping.ts`), which mirrors the server's
   * CheckoutPricing.GetShippingCost(method, subtotal), so the displayed option amount == the
   * order-summary shipping line == the cart page == the actual Stripe charge (displayed == charged).
   * Both the option labels and the order summary read through this — never a second divergent formula.
   */
  shippingCostFor(method: string): number {
    return computeShippingCost(method, this.cartSubtotal());
  }

  /** Per-tier shipping costs shown in the option labels (standard is threshold-aware). */
  get shippingCost(): { standard: number; express: number; overnight: number } {
    return {
      standard: this.shippingCostFor('standard'),
      express: this.shippingCostFor('express'),
      overnight: this.shippingCostFor('overnight')
    };
  }

  /** Cost of the currently selected shipping method — drives the order-summary line + total. */
  selectedShippingCost(): number {
    return this.shippingCostFor(this.checkoutService.shippingMethod());
  }

  /**
   * Grand total shown in the order summary, matching the server's
   * CheckoutPricing.CalculateTotal = subtotal + selectedShipping + tax. The cart's tax is the
   * authoritative 20% VAT the API already returns; we add the threshold-aware selected shipping so
   * the displayed total equals what Stripe charges.
   */
  summaryTotal(): number {
    const tax = this.cartService.cart?.()?.tax ?? 0;
    return this.cartSubtotal() + this.selectedShippingCost() + tax;
  }

  selectShippingMethod(method: string): void {
    this.checkoutService.setShippingMethod(method);
  }

  /**
   * Advance from the payment step to the review step. For card payments this captures a Stripe
   * PaymentMethod from the card element *now*, while it is still mounted — the element is removed
   * from the DOM on the review step, so confirming there with the live element fails. If capture
   * fails (e.g. an incomplete card), we surface the error and stay on the payment step.
   */
  async proceedFromPayment(): Promise<void> {
    if (this.checkoutService.paymentMethod() === 'card') {
      const result = await this.paymentService.createCardPaymentMethod(this.buildCardBillingDetails());
      if (!result.success) {
        this.checkoutService.setError(toTranslationKey(result.error, 'checkout.payment.errors.failed'));
        return;
      }
      this.cardPaymentMethodId = result.paymentMethodId ?? null;
      this.checkoutService.setError(null);
    }
    this.goToStep('review');
  }

  /** Billing details for Stripe, derived from the shipping form + saved address. */
  private buildCardBillingDetails() {
    const address = this.checkoutService.shippingAddress();
    return {
      name: `${this.shippingForm.get('firstName')?.value} ${this.shippingForm.get('lastName')?.value}`,
      email: this.shippingForm.get('email')?.value,
      phone: this.shippingForm.get('phone')?.value,
      address: address ? {
        line1: address.addressLine1,
        line2: address.addressLine2,
        city: address.city,
        state: address.state,
        postal_code: address.postalCode,
        country: address.country === 'Bulgaria' ? 'BG' : address.country
      } : undefined
    };
  }

  /**
   * A fresh per-attempt idempotency token that satisfies the server's [A-Za-z0-9_-]{8,200}
   * contract. Prefers crypto.randomUUID(), but degrades gracefully so card checkout (a revenue
   * path) never hard-fails on a non-secure-context / older browser where randomUUID is absent:
   * falls back to getRandomValues() hex, then to a time+random token as a last resort.
   */
  private newAttemptKey(): string {
    const c: Crypto | undefined = globalThis.crypto;
    if (typeof c?.randomUUID === 'function') {
      return c.randomUUID();
    }
    if (typeof c?.getRandomValues === 'function') {
      const bytes = c.getRandomValues(new Uint8Array(16));
      return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
    }
    return Date.now().toString(36) + Math.random().toString(36).slice(2);
  }

  async placeOrder(): Promise<void> {
    // BUG-04: double-submit guard. Bail out if a placement is already in flight, otherwise
    // claim the lock immediately — before any create-intent/charge call — so a double-click
    // can never fire two intent/charge/createOrder sequences. Reset on every exit path.
    if (this.placingOrder()) {
      return;
    }
    this.placingOrder.set(true);

    const email = this.shippingForm.get('email')?.value;
    const phone = this.shippingForm.get('phone')?.value;

    // Handle card payment with Stripe
    if (this.checkoutService.paymentMethod() === 'card') {
      try {
        // Per-attempt idempotency key: a network retry of this one create-intent POST resends
        // the identical key so Stripe dedupes it; a user retry after a failure is a new click
        // here -> new key -> a fresh intent (so a refunded charge is never replayed).
        const attemptKey = this.newAttemptKey();

        // Create payment intent. The amount and currency are computed
        // server-side from the cart and chosen shipping method.
        const intentResponse = await this.paymentService.createPaymentIntent(
          this.checkoutService.shippingMethod(),
          this.checkoutService.getSessionId() || undefined,
          attemptKey
        ).toPromise();

        if (!intentResponse?.clientSecret) {
          // Create-intent succeeded over HTTP but returned no usable intent (e.g. a
          // misconfigured/placeholder Stripe key). Card is unavailable — steer the buyer
          // to bank transfer rather than showing a generic failure.
          this.checkoutService.setError('checkout.cardUnavailable');
          this.placingOrder.set(false);
          return;
        }

        // Confirm payment with Stripe. Prefer the PaymentMethod captured on the payment step
        // (cardPaymentMethodId) — the card element is no longer mounted on this review step.
        // Fall back to the live element + billing details if no id was captured.
        const paymentResult = await this.paymentService.confirmPayment(
          intentResponse.clientSecret,
          this.buildCardBillingDetails(),
          this.cardPaymentMethodId ?? undefined
        );

        if (!paymentResult.success) {
          this.checkoutService.setError(toTranslationKey(paymentResult.error, 'checkout.payment.errors.failed'));
          this.cardPaymentMethodId = null;
          this.placingOrder.set(false);
          return;
        }
        this.cardPaymentMethodId = null;

// Payment succeeded, create order with payment intent ID
        this.checkoutService.createOrder(email, phone, paymentResult.paymentIntentId).subscribe({
          next: () => {
            this.cartService.clearCart().subscribe();
            this.paymentService.destroyElements();
            this.placingOrder.set(false);
            // Navigate to dedicated confirmation page
            const orderId = this.checkoutService.lastOrderId();
            if (orderId) {
              this.goToConfirmation(orderId);
            } else {
              // Fallback to inline confirmation
              this.orderPlaced.set(true);
              this.confettiService.burst();
            }
          },
          error: (err) => {
            this.placingOrder.set(false);
            this.checkoutService.setError(apiErrorToTranslationKey(err, 'checkout.errors.placeOrderFailed'));
          }
        });
      } catch (error: unknown) {
        // The only call awaited inside this try that can throw is createPaymentIntent
        // (confirmPayment resolves with {success,error} and is handled above). A rejection
        // here therefore means create-intent failed — typically a payment/config problem
        // such as a 400 'Invalid API Key' from a placeholder Stripe key. Surface the
        // clearer "card unavailable" message that steers the buyer to bank transfer,
        // unless the error already carries a specific translation key to preserve.
        this.placingOrder.set(false);
        this.checkoutService.setError(apiErrorToTranslationKey(error, 'checkout.cardUnavailable'));
      }
    } else {
      // Offline payment (bank transfer): no Stripe charge — the order is created Pending and the
      // buyer receives wiring instructions (GAP-06).
      this.checkoutService.createOrder(email, phone).subscribe({
        next: () => {
          this.cartService.clearCart().subscribe();
          this.placingOrder.set(false);
          // Navigate to dedicated confirmation page
          const orderId = this.checkoutService.lastOrderId();
          if (orderId) {
            this.goToConfirmation(orderId);
          } else {
            // Fallback to inline confirmation
            this.orderPlaced.set(true);
            this.confettiService.burst();
          }
        },
        error: (err) => {
          this.placingOrder.set(false);
          console.error('Order failed:', err);
          this.checkoutService.setError(apiErrorToTranslationKey(err, 'checkout.errors.placeOrderFailed'));
        }
      });
    }
  }

  /**
   * Navigates to the order confirmation. For guest orders the opaque access token is carried as a
   * query param so the unauthenticated confirmation page can fetch the order (GAP-07).
   */
  private goToConfirmation(orderId: string): void {
    const token = this.checkoutService.lastGuestToken();
    this.router.navigate(
      ['/checkout/confirmation', orderId],
      token ? { queryParams: { token } } : {});
  }
}
