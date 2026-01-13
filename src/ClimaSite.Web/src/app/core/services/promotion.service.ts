import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Promotion, PromotionBrief } from '../models/promotion.model';
import { PaginatedResult } from '../models/product.model';
import { LanguageService } from './language.service';

@Injectable({
  providedIn: 'root'
})
export class PromotionService {
  private readonly http = inject(HttpClient);
  private readonly languageService = inject(LanguageService);
  private readonly apiUrl = `${environment.apiUrl}/api/promotions`;

  getActivePromotions(pageNumber: number = 1, pageSize: number = 12): Observable<PaginatedResult<PromotionBrief>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }

    return this.http.get<PaginatedResult<PromotionBrief>>(this.apiUrl, { params });
  }

  getPromotionBySlug(slug: string): Observable<Promotion> {
    let params = new HttpParams();
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<Promotion>(`${this.apiUrl}/${slug}`, { params });
  }

  getFeaturedPromotions(count: number = 4): Observable<PromotionBrief[]> {
    let params = new HttpParams().set('count', count.toString());
    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }
    return this.http.get<PromotionBrief[]>(`${this.apiUrl}/featured`, { params });
  }
}
