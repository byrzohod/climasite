import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';
import { toTranslationKey } from '../utils/translation-key.util';

export interface BankTransferConfig {
  iban: string;
  accountName: string;
  bankName: string;
}

export interface PaymentConfig {
  publishableKey: string;
  bankTransfer?: BankTransferConfig;
}

export interface PaymentIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  amount: number;
  currency: string;
}

export interface PaymentIntentStatus {
  paymentIntentId: string;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/payments`;

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;
  private cardElement: StripeCardElement | null = null;

  // Signal-based state
  private readonly _isInitialized = signal(false);
  private readonly _isProcessing = signal(false);
  private readonly _error = signal<string | null>(null);
  // GAP-06: bank-transfer account details from the public payment config, shown in the bank
  // instructions panels. Cached after the first fetch.
  private readonly _bankTransfer = signal<BankTransferConfig | null>(null);

  // Public readonly signals
  readonly isInitialized = this._isInitialized.asReadonly();
  readonly isProcessing = this._isProcessing.asReadonly();
  readonly error = this._error.asReadonly();
  readonly bankTransfer = this._bankTransfer.asReadonly();

  /**
   * Loads the public payment config (Stripe key + bank-transfer details) and caches the bank
   * details. Safe to call regardless of the selected payment method (GAP-06).
   */
  async loadConfig(): Promise<PaymentConfig | null> {
    try {
      const config = await this.http.get<PaymentConfig>(`${this.apiUrl}/config`).toPromise();
      if (config?.bankTransfer) {
        this._bankTransfer.set(config.bankTransfer);
      }
      return config ?? null;
    } catch (error) {
      console.error('Failed to load payment config:', error);
      return null;
    }
  }

  async initialize(): Promise<boolean> {
    if (this.stripe) {
      this._isInitialized.set(true);
      return true;
    }

    try {
      const config = await this.loadConfig();

      if (!config?.publishableKey) {
        this._error.set('checkout.payment.errors.configUnavailable');
        return false;
      }

      this.stripe = await loadStripe(config.publishableKey);

      if (!this.stripe) {
        this._error.set('checkout.payment.errors.stripeInitFailed');
        return false;
      }

      this._isInitialized.set(true);
      this._error.set(null);
      return true;
    } catch (error) {
      console.error('Failed to initialize payment service:', error);
      this._error.set('checkout.payment.errors.serviceInitFailed');
      return false;
    }
  }

  createElements(containerId: string): StripeCardElement | null {
    if (!this.stripe) {
      this._error.set('checkout.payment.errors.stripeNotInitialized');
      return null;
    }

    this.elements = this.stripe.elements();
    this.cardElement = this.elements.create('card', {
      style: {
        base: {
          fontSize: '16px',
          color: 'var(--color-text-primary, #1a1a1a)',
          '::placeholder': {
            color: 'var(--color-text-secondary, #6b7280)'
          }
        },
        invalid: {
          color: 'var(--color-error, #dc2626)'
        }
      }
    });

    const container = document.getElementById(containerId);
    if (container) {
      this.cardElement.mount(container);
    }

    return this.cardElement;
  }

  destroyElements(): void {
    if (this.cardElement) {
      this.cardElement.unmount();
      this.cardElement = null;
    }
    this.elements = null;
  }

  createPaymentIntent(shippingMethod: string, guestSessionId?: string): Observable<PaymentIntentResponse> {
    // Amount and currency are computed server-side from the cart; the client
    // only supplies the shipping method and (for guests) the session id.
    return this.http.post<PaymentIntentResponse>(`${this.apiUrl}/create-intent`, {
      shippingMethod,
      guestSessionId
    });
  }

  async confirmPayment(clientSecret: string, billingDetails?: {
    name?: string;
    email?: string;
    phone?: string;
    address?: {
      line1?: string;
      line2?: string;
      city?: string;
      state?: string;
      postal_code?: string;
      country?: string;
    };
  }, paymentMethodId?: string): Promise<{ success: boolean; paymentIntentId?: string; error?: string }> {
    // A pre-created PaymentMethod id is used when the live card element is no longer mounted: the
    // card form lives on the payment step but the order is confirmed on the review step, where the
    // element has been removed from the DOM. Fall back to the live element only when no id is given.
    if (!this.stripe || (!paymentMethodId && !this.cardElement)) {
      return { success: false, error: 'checkout.payment.errors.notInitialized' };
    }

    this._isProcessing.set(true);
    this._error.set(null);

    try {
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(
        clientSecret,
        paymentMethodId
          ? { payment_method: paymentMethodId }
          : { payment_method: { card: this.cardElement!, billing_details: billingDetails } }
      );

      this._isProcessing.set(false);

      if (error) {
        const errorKey = toTranslationKey(error.message, 'checkout.payment.errors.failed');
        this._error.set(errorKey);
        return { success: false, error: errorKey };
      }

      if (paymentIntent?.status === 'succeeded') {
        return { success: true, paymentIntentId: paymentIntent.id };
      }

      return { success: false, error: 'checkout.payment.errors.notCompleted' };
    } catch (err: unknown) {
      const message = toTranslationKey(err instanceof Error ? err.message : null, 'checkout.payment.errors.failed');
      this._isProcessing.set(false);
      this._error.set(message);
      return { success: false, error: message };
    }
  }

  /**
   * Creates a Stripe PaymentMethod from the live card element while it is still mounted. Call this
   * before leaving the payment step (the element is destroyed on the review step). The returned id
   * is then passed to confirmPayment(), which can confirm without the element in the DOM.
   */
  async createCardPaymentMethod(billingDetails?: {
    name?: string;
    email?: string;
    phone?: string;
    address?: {
      line1?: string;
      line2?: string;
      city?: string;
      state?: string;
      postal_code?: string;
      country?: string;
    };
  }): Promise<{ success: boolean; paymentMethodId?: string; error?: string }> {
    if (!this.stripe || !this.cardElement) {
      return { success: false, error: 'checkout.payment.errors.notInitialized' };
    }

    this._error.set(null);

    try {
      const { error, paymentMethod } = await this.stripe.createPaymentMethod({
        type: 'card',
        card: this.cardElement,
        billing_details: billingDetails
      });

      if (error) {
        const errorKey = toTranslationKey(error.message, 'checkout.payment.errors.failed');
        this._error.set(errorKey);
        return { success: false, error: errorKey };
      }

      return { success: true, paymentMethodId: paymentMethod?.id };
    } catch (err: unknown) {
      const message = toTranslationKey(err instanceof Error ? err.message : null, 'checkout.payment.errors.failed');
      this._error.set(message);
      return { success: false, error: message };
    }
  }

  getPaymentIntentStatus(paymentIntentId: string): Observable<PaymentIntentStatus> {
    return this.http.get<PaymentIntentStatus>(`${this.apiUrl}/intent/${paymentIntentId}`);
  }

  clearError(): void {
    this._error.set(null);
  }
}
