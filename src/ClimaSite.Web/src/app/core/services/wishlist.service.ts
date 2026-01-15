import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../auth/services/auth.service';
import { ProductBrief } from '../models/product.model';
import { environment } from '../../../environments/environment';
import { tap, catchError, of } from 'rxjs';

export interface WishlistItem {
  productId: string;
  addedAt: string;
  product?: ProductBrief;
}

/**
 * NAV-002: WishlistService
 * Manages wishlist state with localStorage persistence for guests
 * and API sync for authenticated users.
 */
@Injectable({
  providedIn: 'root'
})
export class WishlistService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly apiUrl = environment.apiUrl;
  private readonly STORAGE_KEY = 'climasite_wishlist';

  private readonly _items = signal<WishlistItem[]>([]);
  private readonly _isLoading = signal(false);

  readonly items = this._items.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly itemCount = computed(() => this._items().length);

  constructor() {
    this.loadWishlist();
  }

  /**
   * Check if a product is in the wishlist
   */
  isInWishlist(productId: string): boolean {
    return this._items().some(item => item.productId === productId);
  }

  /**
   * Add a product to the wishlist
   */
  addToWishlist(productId: string, product?: ProductBrief): void {
    if (this.isInWishlist(productId)) {
      return;
    }

    const newItem: WishlistItem = {
      productId,
      addedAt: new Date().toISOString(),
      product
    };

    this._items.update(items => [...items, newItem]);
    this.saveToStorage();

    // Sync with API if authenticated
    if (this.authService.isAuthenticated()) {
      this.syncAddToApi(productId);
    }
  }

  /**
   * Remove a product from the wishlist
   */
  removeFromWishlist(productId: string): void {
    this._items.update(items => items.filter(item => item.productId !== productId));
    this.saveToStorage();

    // Sync with API if authenticated
    if (this.authService.isAuthenticated()) {
      this.syncRemoveFromApi(productId);
    }
  }

  /**
   * Toggle a product in/out of the wishlist
   */
  toggleWishlist(productId: string, product?: ProductBrief): boolean {
    if (this.isInWishlist(productId)) {
      this.removeFromWishlist(productId);
      return false;
    } else {
      this.addToWishlist(productId, product);
      return true;
    }
  }

  /**
   * Clear the wishlist
   */
  clearWishlist(): void {
    this._items.set([]);
    this.saveToStorage();
  }

  /**
   * Load wishlist from storage or API
   */
  private loadWishlist(): void {
    if (!this.isBrowser) return;

    // Load from localStorage first (for immediate display)
    const stored = localStorage.getItem(this.STORAGE_KEY);
    if (stored) {
      try {
        const items = JSON.parse(stored) as WishlistItem[];
        this._items.set(items);
      } catch {
        this._items.set([]);
      }
    }

    // If authenticated, fetch from API and merge
    if (this.authService.isAuthenticated()) {
      this.fetchFromApi();
    }
  }

  /**
   * Save wishlist to localStorage
   */
  private saveToStorage(): void {
    if (!this.isBrowser) return;
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this._items()));
  }

  /**
   * Fetch wishlist from API
   */
  private fetchFromApi(): void {
    this._isLoading.set(true);

    this.http.get<WishlistItem[]>(`${this.apiUrl}/api/wishlist`).pipe(
      tap(apiItems => {
        // Merge API items with local items
        const localItems = this._items();
        const mergedMap = new Map<string, WishlistItem>();

        // Add API items
        apiItems.forEach(item => mergedMap.set(item.productId, item));

        // Add local items that aren't in API
        localItems.forEach(item => {
          if (!mergedMap.has(item.productId)) {
            mergedMap.set(item.productId, item);
            // Sync new local items to API
            this.syncAddToApi(item.productId);
          }
        });

        this._items.set(Array.from(mergedMap.values()));
        this.saveToStorage();
        this._isLoading.set(false);
      }),
      catchError(() => {
        this._isLoading.set(false);
        return of([]);
      })
    ).subscribe();
  }

  /**
   * Sync add operation to API
   */
  private syncAddToApi(productId: string): void {
    this.http.post(`${this.apiUrl}/api/wishlist/items`, { productId }).pipe(
      catchError(() => of(null))
    ).subscribe();
  }

  /**
   * Sync remove operation to API
   */
  private syncRemoveFromApi(productId: string): void {
    this.http.delete(`${this.apiUrl}/api/wishlist/items/${productId}`).pipe(
      catchError(() => of(null))
    ).subscribe();
  }

  /**
   * Merge guest wishlist with user wishlist after login
   */
  mergeWithUserWishlist(): void {
    if (!this.authService.isAuthenticated()) return;

    const guestItems = this._items();
    if (guestItems.length > 0) {
      // Fetch user wishlist and merge
      this.fetchFromApi();
    }
  }
}
