import { Injectable, signal, computed, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ProductBrief } from '../models/product.model';

export interface CompareProduct {
  id: string;
  name: string;
  slug: string;
  primaryImageUrl?: string;
  basePrice: number;
  salePrice?: number;
  isOnSale: boolean;
  brand?: string;
  specifications?: Record<string, unknown>;
}

const STORAGE_KEY = 'climasite_compare';
const MAX_COMPARE_ITEMS = 4;

@Injectable({
  providedIn: 'root'
})
export class ComparisonService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly products = signal<CompareProduct[]>([]);

  readonly compareList = computed(() => this.products());
  readonly count = computed(() => this.products().length);
  readonly isFull = computed(() => this.products().length >= MAX_COMPARE_ITEMS);
  readonly isEmpty = computed(() => this.products().length === 0);
  readonly canCompare = computed(() => this.products().length >= 2);

  constructor() {
    this.loadFromStorage();
  }

  isInCompare(productId: string): boolean {
    return this.products().some(p => p.id === productId);
  }

  addToCompare(product: ProductBrief | CompareProduct): boolean {
    if (this.isInCompare(product.id)) {
      return false;
    }

    if (this.isFull()) {
      return false;
    }

    const compareProduct: CompareProduct = {
      id: product.id,
      name: product.name,
      slug: product.slug,
      primaryImageUrl: product.primaryImageUrl,
      basePrice: product.basePrice,
      salePrice: product.salePrice,
      isOnSale: product.isOnSale,
      brand: product.brand,
      specifications: (product as CompareProduct).specifications
    };

    this.products.update(list => [...list, compareProduct]);
    this.saveToStorage();
    return true;
  }

  removeFromCompare(productId: string): void {
    this.products.update(list => list.filter(p => p.id !== productId));
    this.saveToStorage();
  }

  toggleCompare(product: ProductBrief | CompareProduct): boolean {
    if (this.isInCompare(product.id)) {
      this.removeFromCompare(product.id);
      return false;
    } else {
      return this.addToCompare(product);
    }
  }

  clearAll(): void {
    this.products.set([]);
    this.saveToStorage();
  }

  getSpecificationKeys(): string[] {
    const allKeys = new Set<string>();
    this.products().forEach(product => {
      if (product.specifications) {
        Object.keys(product.specifications).forEach(key => allKeys.add(key));
      }
    });
    return Array.from(allKeys).sort();
  }

  private loadFromStorage(): void {
    if (!this.isBrowser) return;

    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as CompareProduct[];
        this.products.set(parsed.slice(0, MAX_COMPARE_ITEMS));
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
      // Storage full or unavailable
    }
  }
}
