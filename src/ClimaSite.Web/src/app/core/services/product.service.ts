import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Product,
  ProductBrief,
  PaginatedResult,
  FilterOptions,
  ProductFilter,
  ProductSearchParams
} from '../models/product.model';
import { LanguageService } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly languageService = inject(LanguageService);
  private readonly apiUrl = `${environment.apiUrl}/api/products`;

  getProducts(filter: ProductFilter = {}): Observable<PaginatedResult<ProductBrief>> {
    let params = new HttpParams();

    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.brand) params = params.set('brand', filter.brand);
    if (filter.minPrice !== undefined) params = params.set('minPrice', filter.minPrice.toString());
    if (filter.maxPrice !== undefined) params = params.set('maxPrice', filter.maxPrice.toString());
    if (filter.inStock !== undefined) params = params.set('inStock', filter.inStock.toString());
    if (filter.onSale !== undefined) params = params.set('onSale', filter.onSale.toString());
    if (filter.isFeatured !== undefined) params = params.set('isFeatured', filter.isFeatured.toString());
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined) params = params.set('sortDescending', filter.sortDescending.toString());

    // Add current language to request
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }

    return this.http.get<PaginatedResult<ProductBrief>>(this.apiUrl, { params });
  }

  getProductBySlug(slug: string): Observable<Product> {
    let params = new HttpParams();
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<Product>(`${this.apiUrl}/${slug}`, { params });
  }

  getFeaturedProducts(count: number = 8): Observable<ProductBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<ProductBrief[]>(`${this.apiUrl}/featured`, { params });
  }

  searchProducts(searchParams: ProductSearchParams): Observable<PaginatedResult<ProductBrief>> {
    let httpParams = new HttpParams().set('q', searchParams.q);

    if (searchParams.pageNumber) httpParams = httpParams.set('pageNumber', searchParams.pageNumber.toString());
    if (searchParams.pageSize) httpParams = httpParams.set('pageSize', searchParams.pageSize.toString());
    if (searchParams.categorySlug) httpParams = httpParams.set('categorySlug', searchParams.categorySlug);
    if (searchParams.brands && searchParams.brands.length > 0) {
      searchParams.brands.forEach(brand => {
        httpParams = httpParams.append('brands', brand);
      });
    }
    if (searchParams.minPrice !== undefined) httpParams = httpParams.set('minPrice', searchParams.minPrice.toString());
    if (searchParams.maxPrice !== undefined) httpParams = httpParams.set('maxPrice', searchParams.maxPrice.toString());

    // Add current language to request
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      httpParams = httpParams.set('lang', lang);
    }

    return this.http.get<PaginatedResult<ProductBrief>>(`${this.apiUrl}/search`, { params: httpParams });
  }

  getRelatedProducts(productId: string, count: number = 8): Observable<ProductBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<ProductBrief[]>(`${this.apiUrl}/${productId}/related`, { params });
  }

  getSimilarProducts(productId: string, count: number = 8): Observable<ProductBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<ProductBrief[]>(`${this.apiUrl}/${productId}/similar`, { params });
  }

  getProductConsumables(productId: string, count: number = 6): Observable<ProductBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<ProductBrief[]>(`${this.apiUrl}/${productId}/consumables`, { params });
  }

  getFrequentlyBoughtTogether(productId: string, count: number = 4): Observable<ProductBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<ProductBrief[]>(`${this.apiUrl}/${productId}/frequently-bought-together`, { params });
  }

  getFilterOptions(categorySlug?: string): Observable<FilterOptions> {
    let params = new HttpParams();
    if (categorySlug) params = params.set('categorySlug', categorySlug);

    return this.http.get<FilterOptions>(`${this.apiUrl}/filters`, { params });
  }
}
