import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface PricePoint {
  date: string;
  price: number;
  compareAtPrice?: number;
  reason: string;
}

export interface ProductPriceHistory {
  productId: string;
  productName: string;
  currentPrice: number;
  currentCompareAtPrice?: number;
  lowestPrice: number;
  highestPrice: number;
  averagePrice: number;
  pricePoints: PricePoint[];
}

@Injectable({
  providedIn: 'root'
})
export class PriceHistoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/price-history`;

  getPriceHistory(productId: string, daysBack: number = 90): Observable<ProductPriceHistory> {
    return this.http.get<ProductPriceHistory>(`${this.apiUrl}/${productId}`, {
      params: { daysBack: daysBack.toString() }
    });
  }
}
