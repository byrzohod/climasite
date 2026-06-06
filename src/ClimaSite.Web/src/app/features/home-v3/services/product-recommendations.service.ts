import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { LanguageService } from '../../../core/services/language.service';
import type { ClimateZone, RecommendedProduct, RoomType } from '../models/home-v3.models';

@Injectable({ providedIn: 'root' })
export class ProductRecommendationsService {
  private readonly http = inject(HttpClient);
  private readonly languageService = inject(LanguageService);
  private readonly url = `${environment.apiUrl}/api/products/recommendations`;

  getRecommendations(area: number, type: RoomType, zone: ClimateZone): Observable<RecommendedProduct[]> {
    let params = new HttpParams()
      .set('area', area.toString())
      .set('type', type)
      .set('zone', zone);

    const lang = this.languageService.currentLanguage();
    if (lang && lang !== 'en') {
      params = params.set('lang', lang);
    }

    return this.http.get<RecommendedProduct[]>(this.url, { params });
  }
}
