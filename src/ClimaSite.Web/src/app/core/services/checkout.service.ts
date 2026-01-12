import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, tap, catchError, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Order,
  CreateOrderRequest,
  Address,
  OrderBrief,
  PaginatedOrders,
  OrdersFilterParams,
  OrderStatus,
  ReorderResult
} from '../models/order.model';

export type CheckoutStep = 'shipping' | 'payment' | 'review';

@Injectable({
  providedIn: 'root'
})
export class CheckoutService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/orders`;
  private readonly SESSION_KEY = 'climasite_session_id';

  // State signals
  private readonly _currentStep = signal<CheckoutStep>('shipping');
  private readonly _shippingAddress = signal<Address | null>(null);
  private readonly _billingAddress = signal<Address | null>(null);
  private readonly _paymentMethod = signal<string>('card');
  private readonly _shippingMethod = signal<string>('standard');
  private readonly _isProcessing = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastOrderId = signal<string | null>(null);

  // Public readonly signals
  readonly currentStep = this._currentStep.asReadonly();
  readonly shippingAddress = this._shippingAddress.asReadonly();
  readonly billingAddress = this._billingAddress.asReadonly();
  readonly paymentMethod = this._paymentMethod.asReadonly();
  readonly shippingMethod = this._shippingMethod.asReadonly();
  readonly isProcessing = this._isProcessing.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastOrderId = this._lastOrderId.asReadonly();

  private getSessionId(): string {
    return localStorage.getItem(this.SESSION_KEY) || '';
  }

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'X-Session-Id': this.getSessionId()
    });
  }

  setStep(step: CheckoutStep): void {
    this._currentStep.set(step);
  }

  setShippingAddress(address: Address): void {
    this._shippingAddress.set(address);
  }

  setBillingAddress(address: Address | null): void {
    this._billingAddress.set(address);
  }

  setPaymentMethod(method: string): void {
    this._paymentMethod.set(method);
  }

  setShippingMethod(method: string): void {
    this._shippingMethod.set(method);
  }

  canProceedToPayment(): boolean {
    return this._shippingAddress() !== null;
  }

  canProceedToReview(): boolean {
    return this.canProceedToPayment() && this._shippingMethod() !== '';
  }

  createOrder(email: string, phone?: string): Observable<Order> {
    this._isProcessing.set(true);
    this._error.set(null);

    const shippingAddress = this._shippingAddress();
    if (!shippingAddress) {
      this._error.set('Shipping address is required');
      this._isProcessing.set(false);
      throw new Error('Shipping address is required');
    }

    const request: CreateOrderRequest = {
      customerEmail: email,
      customerPhone: phone || shippingAddress.phone,
      shippingAddress,
      billingAddress: this._billingAddress() || undefined,
      shippingMethod: this._shippingMethod(),
      paymentMethod: this._paymentMethod(),
      guestSessionId: this.getSessionId() || undefined
    };

    return this.http.post<Order>(this.apiUrl, request, { headers: this.getHeaders() })
      .pipe(
        tap((order) => {
          this._isProcessing.set(false);
          this._lastOrderId.set(order.id);
        }),
        catchError(error => {
          console.error('Failed to create order:', error);
          this._error.set(error.error?.message || 'Failed to place order');
          this._isProcessing.set(false);
          throw error;
        })
      );
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${orderId}`, { headers: this.getHeaders() });
  }

  getOrders(params: OrdersFilterParams = {}): Observable<PaginatedOrders> {
    let httpParams = new HttpParams();

    if (params.pageNumber) httpParams = httpParams.set('pageNumber', params.pageNumber.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDirection) httpParams = httpParams.set('sortDirection', params.sortDirection);

    return this.http.get<PaginatedOrders>(this.apiUrl, {
      headers: this.getHeaders(),
      params: httpParams
    });
  }

  getOrderStatuses(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/statuses`, { headers: this.getHeaders() });
  }

  cancelOrder(orderId: string, reason?: string): Observable<Order> {
    return this.http.post<Order>(`${this.apiUrl}/${orderId}/cancel`, { reason }, { headers: this.getHeaders() });
  }

  reorder(orderId: string): Observable<ReorderResult> {
    return this.http.post<ReorderResult>(`${this.apiUrl}/${orderId}/reorder`, {}, { headers: this.getHeaders() });
  }

  downloadInvoice(orderId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${orderId}/invoice`, {
      headers: this.getHeaders(),
      responseType: 'blob'
    });
  }

  resetCheckout(): void {
    this._currentStep.set('shipping');
    this._shippingAddress.set(null);
    this._billingAddress.set(null);
    this._paymentMethod.set('card');
    this._shippingMethod.set('standard');
    this._error.set(null);
  }

  // For validation
  validateShippingAddress(address: Partial<Address>): string[] {
    const errors: string[] = [];

    if (!address.firstName?.trim()) errors.push('First name is required');
    if (!address.lastName?.trim()) errors.push('Last name is required');
    if (!address.addressLine1?.trim()) errors.push('Street address is required');
    if (!address.city?.trim()) errors.push('City is required');
    if (!address.postalCode?.trim()) errors.push('Postal code is required');
    if (!address.country?.trim()) errors.push('Country is required');
    if (!address.phone?.trim()) errors.push('Phone number is required');

    return errors;
  }
}
