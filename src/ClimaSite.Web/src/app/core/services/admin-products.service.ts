import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * A single product feature (mirrors the backend ProductFeatureDto).
 */
export interface AdminProductFeature {
  title: string;
  description: string;
  icon?: string;
}

/**
 * A row in the admin products list (mirrors AdminProductListItemDto).
 */
export interface AdminProductListItem {
  id: string;
  name: string;
  sku: string;
  slug: string;
  price: number;
  salePrice?: number | null;
  stockQuantity: number;
  status: string;
  primaryImageUrl?: string | null;
  categoryName?: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Paginated envelope returned by the admin products list endpoint.
 */
export interface AdminProductsList {
  items: AdminProductListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Full product detail (mirrors AdminProductDetailDto). Optional collections are
 * loosely typed because the admin form only edits the scalar fields.
 */
export interface AdminProductDetail {
  id: string;
  name: string;
  sku: string;
  slug: string;
  shortDescription?: string | null;
  description?: string | null;
  basePrice: number;
  compareAtPrice?: number | null;
  costPrice?: number | null;
  categoryId?: string | null;
  categoryName?: string | null;
  brand?: string | null;
  model?: string | null;
  isActive: boolean;
  isFeatured: boolean;
  requiresInstallation: boolean;
  warrantyMonths: number;
  weightKg?: number | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  specifications?: Record<string, unknown>;
  features?: AdminProductFeature[];
  tags?: string[];
  images?: unknown[];
  variants?: unknown[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Query parameters accepted by the admin products list endpoint
 * (mirrors GetAdminProductsQuery).
 */
export interface AdminProductsQuery {
  pageNumber?: number;
  pageSize?: number;
  categoryId?: string;
  search?: string;
  /** "Active" | "Inactive" | "LowStock" | "OutOfStock" (case-insensitive on the server). */
  status?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  /** "asc" | "desc" */
  sortOrder?: string;
}

/**
 * Payload for POST /api/admin/products (mirrors CreateProductCommand).
 */
export interface CreateProductPayload {
  name: string;
  sku: string;
  shortDescription?: string;
  description?: string;
  basePrice: number;
  compareAtPrice?: number;
  costPrice?: number;
  categoryId?: string;
  brand?: string;
  model?: string;
  isActive?: boolean;
  isFeatured?: boolean;
  requiresInstallation?: boolean;
  warrantyMonths?: number;
  weightKg?: number;
  metaTitle?: string;
  metaDescription?: string;
  specifications?: Record<string, unknown>;
  features?: AdminProductFeature[];
  tags?: string[];
}

/**
 * Payload for PUT /api/admin/products/{id} (mirrors UpdateProductCommand).
 * Requires the id and slug in addition to the create fields.
 */
export interface UpdateProductPayload extends CreateProductPayload {
  id: string;
  slug: string;
}

/**
 * Response returned by create.
 */
export interface CreateProductResponse {
  id: string;
  slug: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin/products`;

  getProducts(query: AdminProductsQuery = {}): Observable<AdminProductsList> {
    let params = new HttpParams()
      .set('pageNumber', (query.pageNumber ?? 1).toString())
      .set('pageSize', (query.pageSize ?? 20).toString());

    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.minPrice !== undefined && query.minPrice !== null) {
      params = params.set('minPrice', query.minPrice.toString());
    }
    if (query.maxPrice !== undefined && query.maxPrice !== null) {
      params = params.set('maxPrice', query.maxPrice.toString());
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortOrder) {
      params = params.set('sortOrder', query.sortOrder);
    }

    return this.http.get<AdminProductsList>(this.baseUrl, { params });
  }

  /** Fetches the full editable product detail by id (admin detail endpoint). */
  getProduct(id: string): Observable<AdminProductDetail> {
    return this.http.get<AdminProductDetail>(`${this.baseUrl}/${id}`);
  }

  createProduct(payload: CreateProductPayload): Observable<CreateProductResponse> {
    return this.http.post<CreateProductResponse>(this.baseUrl, payload);
  }

  updateProduct(id: string, payload: UpdateProductPayload): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.baseUrl}/${id}`, payload);
  }

  deleteProduct(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.baseUrl}/${id}`);
  }

  toggleStatus(id: string, isActive: boolean): Observable<{ success: boolean }> {
    return this.http.patch<{ success: boolean }>(`${this.baseUrl}/${id}/status`, { isActive });
  }

  toggleFeatured(id: string, isFeatured: boolean): Observable<{ success: boolean }> {
    return this.http.patch<{ success: boolean }>(`${this.baseUrl}/${id}/featured`, { isFeatured });
  }

  /**
   * Lightweight active-product search used by relation pickers (BUG-14 fix).
   * Returns the standard list items so callers can map id/name/sku/image/price.
   */
  searchProducts(term: string): Observable<AdminProductsList> {
    const params = new HttpParams()
      .set('search', term)
      .set('pageSize', '10')
      .set('status', 'Active');

    return this.http.get<AdminProductsList>(this.baseUrl, { params });
  }
}

/**
 * The product statuses mirrored from the backend status string.
 */
export const PRODUCT_STATUSES: readonly string[] = [
  'Active',
  'Inactive',
  'OutOfStock',
  'LowStock'
] as const;
