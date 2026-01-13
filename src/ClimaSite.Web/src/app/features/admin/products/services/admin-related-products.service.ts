import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface RelatedProductDto {
  id: string;
  relatedProductId: string;
  name: string;
  sku: string;
  primaryImageUrl?: string;
  price: number;
  sortOrder: number;
}

export interface RelationGroupDto {
  relationType: string;
  relations: RelatedProductDto[];
}

export interface ProductRelationsDto {
  productId: string;
  productName: string;
  relationGroups: RelationGroupDto[];
}

export interface AddRelationRequest {
  relatedProductId: string;
  relationType: string;
}

export interface ReorderRelationsRequest {
  relationType: string;
  relationIds: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminRelatedProductsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/admin/products`;

  getProductRelations(productId: string): Observable<ProductRelationsDto> {
    return this.http.get<ProductRelationsDto>(`${this.apiUrl}/${productId}/relations`);
  }

  addRelation(productId: string, request: AddRelationRequest): Observable<{ relationId: string }> {
    return this.http.post<{ relationId: string }>(`${this.apiUrl}/${productId}/relations`, request);
  }

  removeRelation(productId: string, relationId: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${productId}/relations/${relationId}`);
  }

  reorderRelations(productId: string, request: ReorderRelationsRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.apiUrl}/${productId}/relations/reorder`, request);
  }
}
