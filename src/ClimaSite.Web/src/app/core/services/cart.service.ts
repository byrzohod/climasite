import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap, catchError, of, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart, CartItem, AddToCartRequest, UpdateCartItemRequest, CartSummary } from '../models/cart.model';

// TODO: SVC-001 - Error messages in this service are hardcoded (e.g., 'Failed to add item to cart').
// These should be replaced with translation keys and use TranslateService for i18n support.
// Example: this._error.set(this.translate.instant('cart.errors.addFailed'));

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/cart`;
  private readonly SESSION_KEY = 'climasite_session_id';

  // Signal-based state
  private readonly _cart = signal<Cart | null>(null);
  private readonly _isLoading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _miniCartOpen = signal(false);

  // Public readonly signals
  readonly cart = this._cart.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly miniCartOpen = this._miniCartOpen.asReadonly();

  // Computed signals
  readonly items = computed(() => this._cart()?.items ?? []);
  readonly itemCount = computed(() => this._cart()?.itemCount ?? 0);
  readonly subtotal = computed(() => this._cart()?.subtotal ?? 0);
  readonly total = computed(() => this._cart()?.total ?? 0);
  readonly isEmpty = computed(() => this.itemCount() === 0);

  constructor() {
    this.loadCart();
  }

  private getSessionId(): string {
    let sessionId = localStorage.getItem(this.SESSION_KEY);
    if (!sessionId) {
      sessionId = this.generateSessionId();
      localStorage.setItem(this.SESSION_KEY, sessionId);
    }
    return sessionId;
  }

  private generateSessionId(): string {
    return 'sess_' + Math.random().toString(36).substring(2) + Date.now().toString(36);
  }

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'X-Session-Id': this.getSessionId()
    });
  }

  loadCart(): void {
    this._isLoading.set(true);
    this._error.set(null);

    const sessionId = this.getSessionId();
    this.http.get<Cart>(`${this.apiUrl}?guestSessionId=${sessionId}`)
      .pipe(
        tap(cart => {
          this._cart.set(cart);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to load cart:', error);
          // Initialize with empty cart on error
          this._cart.set(this.createEmptyCart());
          this._isLoading.set(false);
          return of(null);
        })
      )
      .subscribe();
  }

  private createEmptyCart(): Cart {
    return {
      id: '',
      sessionId: this.getSessionId(),
      items: [],
      subtotal: 0,
      shipping: 0,
      tax: 0,
      total: 0,
      itemCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
  }

  addToCart(productId: string, quantity: number = 1, variantId?: string): Observable<Cart> {
    this._isLoading.set(true);
    this._error.set(null);

    const request: AddToCartRequest = {
      productId,
      quantity,
      variantId,
      guestSessionId: this.getSessionId()
    };

    return this.http.post<Cart>(`${this.apiUrl}/items`, request)
      .pipe(
        tap(cart => {
          this._cart.set(cart);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to add to cart:', error);
          this._error.set('Failed to add item to cart');
          this._isLoading.set(false);
          throw error;
        })
      );
  }

  updateQuantity(itemId: string, quantity: number): Observable<Cart> {
    this._isLoading.set(true);
    this._error.set(null);

    const request = { quantity, guestSessionId: this.getSessionId() };

    return this.http.put<Cart>(`${this.apiUrl}/items/${itemId}`, request)
      .pipe(
        tap(cart => {
          this._cart.set(cart);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to update cart item:', error);
          this._error.set('Failed to update item quantity');
          this._isLoading.set(false);
          throw error;
        })
      );
  }

  removeItem(itemId: string): Observable<Cart> {
    this._isLoading.set(true);
    this._error.set(null);

    const sessionId = this.getSessionId();
    return this.http.delete<Cart>(`${this.apiUrl}/items/${itemId}?guestSessionId=${sessionId}`)
      .pipe(
        tap(cart => {
          this._cart.set(cart);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to remove cart item:', error);
          this._error.set('Failed to remove item from cart');
          this._isLoading.set(false);
          throw error;
        })
      );
  }

  clearCart(): Observable<void> {
    this._isLoading.set(true);
    this._error.set(null);

    const sessionId = this.getSessionId();
    return this.http.delete<void>(`${this.apiUrl}?guestSessionId=${sessionId}`)
      .pipe(
        tap(() => {
          this._cart.set(this.createEmptyCart());
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to clear cart:', error);
          this._error.set('Failed to clear cart');
          this._isLoading.set(false);
          throw error;
        })
      );
  }

  getSummary(): CartSummary {
    return {
      itemCount: this.itemCount(),
      subtotal: this.subtotal(),
      total: this.total()
    };
  }

  // Merge guest cart with user cart after login
  mergeCart(userId: string): Observable<Cart> {
    this._isLoading.set(true);

    return this.http.post<Cart>(`${this.apiUrl}/merge`, { userId }, { headers: this.getHeaders() })
      .pipe(
        tap(cart => {
          this._cart.set(cart);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to merge cart:', error);
          this._isLoading.set(false);
          throw error;
        })
      );
  }

  // Check if a product is in the cart
  isInCart(productId: string, variantId?: string): boolean {
    const items = this.items();
    return items.some(item =>
      item.productId === productId &&
      (!variantId || item.variantId === variantId)
    );
  }

  // Get quantity of a specific product in cart
  getItemQuantity(productId: string, variantId?: string): number {
    const items = this.items();
    const item = items.find(i =>
      i.productId === productId &&
      (!variantId || i.variantId === variantId)
    );
    return item?.quantity ?? 0;
  }

  // Mini-cart drawer state management
  /**
   * Open the mini-cart drawer
   */
  openMiniCart(): void {
    this._miniCartOpen.set(true);
    if (typeof document !== 'undefined') {
      document.body.style.overflow = 'hidden';
    }
  }

  /**
   * Close the mini-cart drawer
   */
  closeMiniCart(): void {
    this._miniCartOpen.set(false);
    if (typeof document !== 'undefined') {
      document.body.style.overflow = '';
    }
  }

  /**
   * Toggle the mini-cart drawer open/closed state
   */
  toggleMiniCart(): void {
    if (this._miniCartOpen()) {
      this.closeMiniCart();
    } else {
      this.openMiniCart();
    }
  }
}
