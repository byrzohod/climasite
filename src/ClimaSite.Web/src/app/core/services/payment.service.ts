import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, of, switchMap, tap, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { loadStripe, Stripe, StripeElements, StripeCardElement } from '@stripe/stripe-js';

export interface PaymentConfig {
  publishableKey: string;
}

export interface PaymentIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
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

  // Public readonly signals
  readonly isInitialized = this._isInitialized.asReadonly();
  readonly isProcessing = this._isProcessing.asReadonly();
  readonly error = this._error.asReadonly();

  async initialize(): Promise<boolean> {
    if (this.stripe) {
      this._isInitialized.set(true);
      return true;
    }

    try {
      const config = await this.http.get<PaymentConfig>(`${this.apiUrl}/config`).toPromise();

      if (!config?.publishableKey) {
        this._error.set('Payment configuration not available');
        return false;
      }

      this.stripe = await loadStripe(config.publishableKey);

      if (!this.stripe) {
        this._error.set('Failed to initialize Stripe');
        return false;
      }

      this._isInitialized.set(true);
      this._error.set(null);
      return true;
    } catch (error) {
      console.error('Failed to initialize payment service:', error);
      this._error.set('Failed to initialize payment service');
      return false;
    }
  }

  createElements(containerId: string): StripeCardElement | null {
    if (!this.stripe) {
      this._error.set('Stripe not initialized');
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

  createPaymentIntent(amount: number, currency: string = 'bgn', orderReference?: string): Observable<PaymentIntentResponse> {
    return this.http.post<PaymentIntentResponse>(`${this.apiUrl}/create-intent`, {
      amount,
      currency,
      orderReference
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
  }): Promise<{ success: boolean; paymentIntentId?: string; error?: string }> {
    if (!this.stripe || !this.cardElement) {
      return { success: false, error: 'Payment not initialized' };
    }

    this._isProcessing.set(true);
    this._error.set(null);

    try {
      const { error, paymentIntent } = await this.stripe.confirmCardPayment(clientSecret, {
        payment_method: {
          card: this.cardElement,
          billing_details: billingDetails
        }
      });

      this._isProcessing.set(false);

      if (error) {
        this._error.set(error.message || 'Payment failed');
        return { success: false, error: error.message };
      }

      if (paymentIntent?.status === 'succeeded') {
        return { success: true, paymentIntentId: paymentIntent.id };
      }

      return { success: false, error: 'Payment was not completed' };
    } catch (err: any) {
      this._isProcessing.set(false);
      this._error.set(err.message || 'Payment failed');
      return { success: false, error: err.message };
    }
  }

  getPaymentIntentStatus(paymentIntentId: string): Observable<PaymentIntentStatus> {
    return this.http.get<PaymentIntentStatus>(`${this.apiUrl}/intent/${paymentIntentId}`);
  }

  clearError(): void {
    this._error.set(null);
  }
}
