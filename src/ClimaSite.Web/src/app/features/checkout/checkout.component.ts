import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService, CheckoutStep } from '../../core/services/checkout.service';
import { Address } from '../../core/models/order.model';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, TranslateModule],
  template: `
    <div class="checkout-container" data-testid="checkout-page">
      <h1>{{ 'checkout.title' | translate }}</h1>

      @if (cartService.isEmpty()) {
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
                        <option value="Bulgaria">Bulgaria</option>
                        <option value="Germany">Germany</option>
                        <option value="Austria">Austria</option>
                        <option value="Romania">Romania</option>
                        <option value="Greece">Greece</option>
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
                    <span class="payment-icon">📦</span>
                    <span>{{ 'checkout.shipping.standard' | translate }} ({{ 'checkout.shipping.standardTime' | translate }}) - {{ 'checkout.shipping.free' | translate }}</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.shippingMethod() === 'express'">
                    <input type="radio" name="shippingMethod" value="express" [checked]="checkoutService.shippingMethod() === 'express'" (change)="selectShippingMethod('express')" data-testid="shipping-express" />
                    <span class="payment-icon">🚀</span>
                    <span>{{ 'checkout.shipping.express' | translate }} ({{ 'checkout.shipping.expressTime' | translate }}) - $9.99</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.shippingMethod() === 'overnight'">
                    <input type="radio" name="shippingMethod" value="overnight" [checked]="checkoutService.shippingMethod() === 'overnight'" (change)="selectShippingMethod('overnight')" data-testid="shipping-overnight" />
                    <span class="payment-icon">⚡</span>
                    <span>{{ 'checkout.shipping.overnight' | translate }} ({{ 'checkout.shipping.overnightTime' | translate }}) - $19.99</span>
                  </label>
                </div>

                <h2 style="margin-top: 2rem;">{{ 'checkout.payment.title' | translate }}</h2>

                <div class="payment-methods">
                  <label class="payment-option" [class.selected]="checkoutService.paymentMethod() === 'card'">
                    <input type="radio" name="paymentMethod" value="card" [checked]="checkoutService.paymentMethod() === 'card'" (change)="selectPaymentMethod('card')" data-testid="payment-card" />
                    <span class="payment-icon">💳</span>
                    <span>{{ 'checkout.payment.card' | translate }}</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.paymentMethod() === 'paypal'">
                    <input type="radio" name="paymentMethod" value="paypal" [checked]="checkoutService.paymentMethod() === 'paypal'" (change)="selectPaymentMethod('paypal')" data-testid="payment-paypal" />
                    <span class="payment-icon">🅿️</span>
                    <span>{{ 'checkout.payment.paypal' | translate }}</span>
                  </label>

                  <label class="payment-option" [class.selected]="checkoutService.paymentMethod() === 'bank'">
                    <input type="radio" name="paymentMethod" value="bank" [checked]="checkoutService.paymentMethod() === 'bank'" (change)="selectPaymentMethod('bank')" data-testid="payment-bank" />
                    <span class="payment-icon">🏦</span>
                    <span>{{ 'checkout.payment.bank' | translate }}</span>
                  </label>
                </div>

                @if (checkoutService.paymentMethod() === 'card') {
                  <div class="card-form" data-testid="card-form">
                    <div class="form-group" data-testid="card-number">
                      <label>{{ 'checkout.payment.cardNumber' | translate }}</label>
                      <input type="text" placeholder="1234 5678 9012 3456" />
                    </div>
                    <div class="form-row">
                      <div class="form-group" data-testid="card-expiry">
                        <label>{{ 'checkout.payment.expiry' | translate }}</label>
                        <input type="text" placeholder="MM/YY" />
                      </div>
                      <div class="form-group" data-testid="card-cvv">
                        <label>{{ 'checkout.payment.cvv' | translate }}</label>
                        <input type="text" placeholder="123" />
                      </div>
                    </div>
                    <div class="form-group" data-testid="card-name">
                      <label>{{ 'checkout.payment.nameOnCard' | translate }}</label>
                      <input type="text" placeholder="John Doe" />
                    </div>
                  </div>
                }

                <div class="step-actions">
                  <button type="button" class="btn-secondary" (click)="goToStep('shipping')" data-testid="previous-step">
                    {{ 'common.back' | translate }}
                  </button>
                  <button type="button" class="btn-primary" (click)="goToStep('review')" data-testid="next-step">
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
                    {{ checkoutService.error() }}
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
                      @case ('card') { 💳 {{ 'checkout.payment.card' | translate }} }
                      @case ('paypal') { 🅿️ {{ 'checkout.payment.paypal' | translate }} }
                      @case ('bank') { 🏦 {{ 'checkout.payment.bank' | translate }} }
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
                        <span class="item-price">{{ item.subtotal | currency }}</span>
                      </div>
                    }
                  </div>
                </div>

                <div class="step-actions">
                  <button type="button" class="btn-secondary" (click)="goToStep('payment')" data-testid="previous-step">
                    {{ 'common.back' | translate }}
                  </button>
                  <button type="button" class="btn-primary btn-place-order" [disabled]="checkoutService.isProcessing()" (click)="placeOrder()" data-testid="place-order">
                    @if (checkoutService.isProcessing()) {
                      {{ 'common.loading' | translate }}
                    } @else {
                      {{ 'checkout.placeOrder' | translate }}
                    }
                  </button>
                </div>
              </div>
            }

            <!-- Order Confirmation -->
            @if (orderPlaced()) {
              <div class="checkout-section" data-testid="order-confirmation">
                <div class="confirmation-content">
                  <div class="confirmation-icon">✓</div>
                  <h2>{{ 'checkout.orderComplete.title' | translate }}</h2>
                  <p>{{ 'checkout.orderComplete.message' | translate }}</p>
                  <p class="order-number-label">{{ 'checkout.orderComplete.orderNumber' | translate }}:</p>
                  <p class="order-number" data-testid="order-number">{{ checkoutService.lastOrderId() }}</p>
                  <div class="confirmation-actions">
                    <a [routerLink]="['/account/orders', checkoutService.lastOrderId()]" class="btn-secondary" data-testid="view-order">
                      {{ 'checkout.orderComplete.viewOrder' | translate }}
                    </a>
                    <a routerLink="/products" class="btn-primary" data-testid="continue-shopping">
                      {{ 'checkout.orderComplete.continueShopping' | translate }}
                    </a>
                  </div>
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
                    <span class="item-price">{{ item.subtotal | currency }}</span>
                  </div>
                }
              </div>

              <div class="summary-totals">
                <div class="summary-row">
                  <span>{{ 'cart.summary.subtotal' | translate }}</span>
                  <span>{{ cartService.subtotal() | currency }}</span>
                </div>
                <div class="summary-row">
                  <span>{{ 'cart.summary.shipping' | translate }}</span>
                  <span>
                    @if ((cartService.cart()?.shipping ?? 0) === 0) {
                      {{ 'cart.summary.freeShipping' | translate }}
                    } @else {
                      {{ cartService.cart()?.shipping | currency }}
                    }
                  </span>
                </div>
                <div class="summary-row">
                  <span>{{ 'cart.summary.tax' | translate }}</span>
                  <span>{{ cartService.cart()?.tax | currency }}</span>
                </div>
                <div class="summary-row total">
                  <span>{{ 'cart.summary.total' | translate }}</span>
                  <span data-testid="order-total">{{ cartService.total() | currency }}</span>
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
        color: var(--color-success, #22c55e);
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

        &:focus {
          outline: none;
          border-color: var(--color-primary);
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
      transition: border-color 0.2s;

      &:hover {
        border-color: var(--color-primary);
      }

      &.selected {
        border-color: var(--color-primary);
        background: var(--color-primary-light);
      }

      input {
        display: none;
      }

      .payment-icon {
        font-size: 1.5rem;
      }
    }

    .card-form {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      margin-bottom: 1.5rem;
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
      background: var(--color-error-bg, #fee2e2);
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
        background: var(--color-success, #22c55e);
        color: white;
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
  `]
})
export class CheckoutComponent {
  readonly cartService = inject(CartService);
  readonly checkoutService = inject(CheckoutService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  orderPlaced = signal(false);

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
    this.checkoutService.setStep('payment');
  }

  selectPaymentMethod(method: string): void {
    this.checkoutService.setPaymentMethod(method);
  }

  selectShippingMethod(method: string): void {
    this.checkoutService.setShippingMethod(method);
  }

  placeOrder(): void {
    const email = this.shippingForm.get('email')?.value;
    const phone = this.shippingForm.get('phone')?.value;

    this.checkoutService.createOrder(email, phone).subscribe({
      next: () => {
        this.orderPlaced.set(true);
        this.cartService.clearCart().subscribe();
        // Stay on the page and show confirmation inline
      },
      error: (err) => {
        console.error('Order failed:', err);
        // Error is already set by checkoutService in catchError
      }
    });
  }
}
