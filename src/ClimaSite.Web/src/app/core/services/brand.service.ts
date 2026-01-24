import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Brand, BrandBrief, BrandListResponse } from '../models/brand.model';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BrandService {
  private readonly http = inject(HttpClient);
  private readonly translate = inject(TranslateService);
  private readonly apiUrl = `${environment.apiUrl}/api/brands`;

  getBrands(pageNumber = 1, pageSize = 24, featured?: boolean): Observable<BrandListResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('lang', this.translate.currentLang || 'en');

    if (featured !== undefined) {
      params = params.set('featured', featured.toString());
    }

    return this.http.get<BrandListResponse>(this.apiUrl, { params });
  }

  getBrandBySlug(slug: string, productPage = 1, productPageSize = 12): Observable<Brand> {
    const params = new HttpParams()
      .set('productPage', productPage.toString())
      .set('productPageSize', productPageSize.toString())
      .set('lang', this.translate.currentLang || 'en');

    return this.http.get<Brand>(`${this.apiUrl}/${slug}`, { params });
  }

  getFeaturedBrands(limit = 8): Observable<BrandBrief[]> {
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('lang', this.translate.currentLang || 'en');

    return this.http.get<BrandBrief[]>(`${this.apiUrl}/featured`, { params });
  }
}
