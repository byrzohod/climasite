import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface ProductTranslationsDto {
  productId: string;
  productName: string;
  defaultLanguage: string;
  translations: ProductTranslationDto[];
  availableLanguages: string[];
}

export interface ProductTranslationDto {
  id: string;
  languageCode: string;
  name: string;
  shortDescription?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AddTranslationRequest {
  languageCode: string;
  name: string;
  shortDescription?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
}

export interface UpdateTranslationRequest {
  name: string;
  shortDescription?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminTranslationsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/admin/products`;

  getProductTranslations(productId: string): Observable<ProductTranslationsDto> {
    return this.http.get<ProductTranslationsDto>(`${this.baseUrl}/${productId}/translations`);
  }

  addTranslation(productId: string, request: AddTranslationRequest): Observable<{ translationId: string }> {
    return this.http.post<{ translationId: string }>(`${this.baseUrl}/${productId}/translations`, request);
  }

  updateTranslation(productId: string, languageCode: string, request: UpdateTranslationRequest): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.baseUrl}/${productId}/translations/${languageCode}`, request);
  }

  deleteTranslation(productId: string, languageCode: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.baseUrl}/${productId}/translations/${languageCode}`);
  }
}
