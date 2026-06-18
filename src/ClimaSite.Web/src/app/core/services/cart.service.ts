import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart, AddToCartRequest, CartSummary } from '../models/cart.model';
import { LanguageService } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly languageService = inject(LanguageService);
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

  // Guest cart/order/payment-intent access is keyed on this id (it is reachable via the
  // anonymous create-intent endpoint), so it MUST be unguessable. Use the CSPRNG-backed
  // crypto.randomUUID(); fall back to crypto.getRandomValues() on older runtimes that
  // lack randomUUID. Existing stored ids keep working — only generation changes here.
  private generateSessionId(): string {
    return 'sess_' + this.secureUuid();
  }

  private secureUuid(): string {
    const cryptoObj = typeof crypto !== 'undefined' ? crypto : undefined;

    if (cryptoObj?.randomUUID) {
      return cryptoObj.randomUUID();
    }

    // Fallback: build an RFC-4122 v4 UUID from CSPRNG bytes.
    if (cryptoObj?.getRandomValues) {
      const bytes = cryptoObj.getRandomValues(new Uint8Array(16));
      bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
      bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant 10xx
      const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0'));
      return (
        hex.slice(0, 4).join('') +
        '-' + hex.slice(4, 6).join('') +
        '-' + hex.slice(6, 8).join('') +
        '-' + hex.slice(8, 10).join('') +
        '-' + hex.slice(10, 16).join('')
      );
    }

    // Last-resort fallback (non-CSPRNG) — only reached if Web Crypto is entirely unavailable.
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
  }

  // Returns a `&lang=xx` suffix for the current language, or '' for the default (en),
  // mirroring how the catalog services thread language through to the API.
  private langSuffix(): string {
    const lang = this.languageService.currentLanguage();
    return lang && lang !== 'en' ? `&lang=${lang}` : '';
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
    this.http.get<Cart>(`${this.apiUrl}?guestSessionId=${sessionId}${this.langSuffix()}`)
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
          this._error.set('cart.errors.addFailed');
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
          this._error.set('cart.errors.updateQuantityFailed');
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
          this._error.set('cart.errors.removeFailed');
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
          this._error.set('cart.errors.clearFailed');
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

  // Merge guest cart with user cart after login.
  // The backend reads the guest session from the `guestSessionId` query parameter
  // (CartController.MergeGuestCart uses [FromQuery]); sending it only in a header or
  // body previously produced a deterministic 400 that silently dropped the guest cart.
  mergeCart(userId: string): Observable<Cart> {
    this._isLoading.set(true);

    const guestSessionId = encodeURIComponent(this.getSessionId());
    return this.http.post<Cart>(`${this.apiUrl}/merge?guestSessionId=${guestSessionId}`, { userId }, { headers: this.getHeaders() })
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
