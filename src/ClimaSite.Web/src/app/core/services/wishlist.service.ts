import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../auth/services/auth.service';
import { ProductBrief } from '../models/product.model';
import { environment } from '../../../environments/environment';
import { Observable, from, of } from 'rxjs';
import { tap, catchError, switchMap, concatMap, reduce, finalize, shareReplay } from 'rxjs';

export interface WishlistDto {
  id: string;
  userId: string;
  isPublic: boolean;
  shareToken?: string | null;
  items: WishlistApiItem[];
  itemCount: number;
  updatedAt: string;
}

export interface WishlistApiItem {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  shortDescription?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
  primaryImageUrl?: string | null;
  price: number;
  salePrice?: number | null;
  isOnSale: boolean;
  discountPercentage: number;
  averageRating: number;
  reviewCount: number;
  inStock: boolean;
  note?: string | null;
  priority: number;
  priceWhenAdded?: number | null;
  notifyOnSale: boolean;
  addedAt: string;
}

export interface WishlistItem {
  id?: string;
  productId: string;
  addedAt: string;
  product?: ProductBrief;
  note?: string | null;
  priority?: number;
  priceWhenAdded?: number | null;
  notifyOnSale?: boolean;
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

  private readonly apiUrl = `${environment.apiUrl}/api/wishlist`;
  private readonly STORAGE_KEY = 'climasite_wishlist';

  private readonly _items = signal<WishlistItem[]>([]);
  private readonly _wishlist = signal<WishlistDto | null>(null);
  private readonly _isLoading = signal(false);
  private fetchInFlight$: Observable<WishlistDto | null> | null = null;

  readonly items = this._items.asReadonly();
  readonly wishlist = this._wishlist.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly itemCount = computed(() => this._items().length);
  readonly isPublic = computed(() => this._wishlist()?.isPublic ?? false);
  readonly shareToken = computed(() => this._wishlist()?.shareToken ?? null);

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
    this._wishlist.update(wishlist => wishlist
      ? {
          ...wishlist,
          items: [],
          itemCount: 0,
          updatedAt: new Date().toISOString()
        }
      : wishlist);
    this.saveToStorage();

    if (this.authService.isAuthenticated()) {
      this.http.delete<WishlistDto>(this.apiUrl).pipe(
        tap(wishlist => this.applyWishlistDto(wishlist)),
        catchError(() => of(null))
      ).subscribe();
    }
  }

  refreshWishlist(): Observable<WishlistDto | null> {
    if (!this.authService.isAuthenticated()) {
      return of(null);
    }

    return this.fetchFromApi();
  }

  setSharing(isPublic: boolean): Observable<WishlistDto> {
    return this.http.put<WishlistDto>(`${this.apiUrl}/share`, { isPublic }).pipe(
      tap(wishlist => this.applyWishlistDto(wishlist))
    );
  }

  regenerateShareToken(): Observable<WishlistDto> {
    return this.http.post<WishlistDto>(`${this.apiUrl}/share-token`, {}).pipe(
      tap(wishlist => this.applyWishlistDto(wishlist))
    );
  }

  getSharedWishlist(shareToken: string): Observable<WishlistDto> {
    return this.http.get<WishlistDto>(`${this.apiUrl}/shared/${encodeURIComponent(shareToken)}`);
  }

  toProductBrief(item: WishlistApiItem): ProductBrief {
    return {
      id: item.productId,
      name: item.productName,
      slug: item.productSlug,
      shortDescription: item.shortDescription ?? undefined,
      basePrice: item.price,
      salePrice: item.salePrice ?? undefined,
      isOnSale: item.isOnSale,
      discountPercentage: item.discountPercentage,
      brand: item.brand ?? undefined,
      averageRating: item.averageRating,
      reviewCount: item.reviewCount,
      primaryImageUrl: item.primaryImageUrl ?? item.imageUrl ?? undefined,
      inStock: item.inStock
    };
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
    if (this.authService.isAuthenticated() && !this.authService.isLoading()) {
      this.fetchFromApi().subscribe();
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
  private fetchFromApi(): Observable<WishlistDto | null> {
    if (this.fetchInFlight$) {
      return this.fetchInFlight$;
    }

    this._isLoading.set(true);

    this.fetchInFlight$ = this.http.get<WishlistDto>(this.apiUrl).pipe(
      switchMap(apiWishlist => {
        const localItems = this._items();
        const apiProductIds = new Set(apiWishlist.items.map(item => item.productId));
        const localOnlyItems = localItems.filter(item => !apiProductIds.has(item.productId));

        if (localOnlyItems.length === 0) {
          return of(apiWishlist);
        }

        return from(localOnlyItems).pipe(
          concatMap(item => this.http.post<WishlistDto>(`${this.apiUrl}/items/${item.productId}`, {})),
          reduce((_, wishlist) => wishlist, apiWishlist)
        );
      }),
      tap(wishlist => {
        this.applyWishlistDto(wishlist);
      }),
      catchError(() => {
        return of(null);
      }),
      finalize(() => {
        this._isLoading.set(false);
        this.fetchInFlight$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    return this.fetchInFlight$;
  }

  /**
   * Sync add operation to API
   */
  private syncAddToApi(productId: string): void {
    this.http.post<WishlistDto>(`${this.apiUrl}/items/${productId}`, {}).pipe(
      tap(wishlist => this.applyWishlistDto(wishlist)),
      catchError(() => of(null))
    ).subscribe();
  }

  /**
   * Sync remove operation to API
   */
  private syncRemoveFromApi(productId: string): void {
    this.http.delete<WishlistDto>(`${this.apiUrl}/items/${productId}`).pipe(
      tap(wishlist => this.applyWishlistDto(wishlist)),
      catchError(() => of(null))
    ).subscribe();
  }

  /**
   * Merge guest wishlist with user wishlist after login
   */
  mergeWithUserWishlist(): Observable<WishlistDto | null> {
    if (!this.authService.isAuthenticated()) {
      return of(null);
    }

    return this.fetchFromApi();
  }

  private applyWishlistDto(wishlist: WishlistDto): void {
    this._wishlist.set(wishlist);
    this._items.set(wishlist.items.map(item => ({
      id: item.id,
      productId: item.productId,
      addedAt: item.addedAt,
      product: this.toProductBrief(item),
      note: item.note,
      priority: item.priority,
      priceWhenAdded: item.priceWhenAdded,
      notifyOnSale: item.notifyOnSale
    })));
    this.saveToStorage();
    this._isLoading.set(false);
  }
}
