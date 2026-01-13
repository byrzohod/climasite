import { Injectable, signal, computed, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ProductBrief } from '../models/product.model';

export interface RecentlyViewedProduct {
  id: string;
  name: string;
  slug: string;
  primaryImageUrl?: string;
  basePrice: number;
  salePrice?: number;
  isOnSale: boolean;
  brand?: string;
  viewedAt: number;
}

const STORAGE_KEY = 'climasite_recently_viewed';
const MAX_ITEMS = 12;

@Injectable({
  providedIn: 'root'
})
export class RecentlyViewedService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly products = signal<RecentlyViewedProduct[]>([]);

  readonly recentlyViewed = computed(() => this.products());
  readonly count = computed(() => this.products().length);

  constructor() {
    this.loadFromStorage();
  }

  addProduct(product: ProductBrief | { id: string; name: string; slug: string; primaryImageUrl?: string; basePrice: number; salePrice?: number; isOnSale: boolean; brand?: string }): void {
    const current = this.products();

    // Remove if already exists (to re-add at front)
    const filtered = current.filter(p => p.id !== product.id);

    const newProduct: RecentlyViewedProduct = {
      id: product.id,
      name: product.name,
      slug: product.slug,
      primaryImageUrl: product.primaryImageUrl,
      basePrice: product.basePrice,
      salePrice: product.salePrice,
      isOnSale: product.isOnSale,
      brand: product.brand,
      viewedAt: Date.now()
    };

    // Add to front and limit to MAX_ITEMS
    const updated = [newProduct, ...filtered].slice(0, MAX_ITEMS);

    this.products.set(updated);
    this.saveToStorage();
  }

  removeProduct(productId: string): void {
    const updated = this.products().filter(p => p.id !== productId);
    this.products.set(updated);
    this.saveToStorage();
  }

  clearAll(): void {
    this.products.set([]);
    this.saveToStorage();
  }

  getProductsExcluding(excludeId: string, limit: number = 8): RecentlyViewedProduct[] {
    return this.products()
      .filter(p => p.id !== excludeId)
      .slice(0, limit);
  }

  private loadFromStorage(): void {
    if (!this.isBrowser) return;

    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as RecentlyViewedProduct[];
        // Filter out old items (older than 30 days)
        const thirtyDaysAgo = Date.now() - 30 * 24 * 60 * 60 * 1000;
        const valid = parsed.filter(p => p.viewedAt > thirtyDaysAgo);
        this.products.set(valid);
      }
    } catch {
      this.products.set([]);
    }
  }

  private saveToStorage(): void {
    if (!this.isBrowser) return;

    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.products()));
    } catch {
      // Storage full or unavailable, silently fail
    }
  }
}
