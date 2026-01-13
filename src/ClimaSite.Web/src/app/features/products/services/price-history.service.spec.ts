import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PriceHistoryService, ProductPriceHistory } from './price-history.service';
import { environment } from '../../../../environments/environment';

describe('PriceHistoryService', () => {
  let service: PriceHistoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PriceHistoryService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(PriceHistoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getPriceHistory', () => {
    it('should fetch price history for a product', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse: ProductPriceHistory = {
        productId,
        productName: 'Test AC Unit',
        currentPrice: 599.99,
        currentCompareAtPrice: 699.99,
        lowestPrice: 549.99,
        highestPrice: 699.99,
        averagePrice: 599.99,
        pricePoints: [
          {
            date: '2024-01-01T00:00:00Z',
            price: 699.99,
            reason: 'Initial'
          },
          {
            date: '2024-01-15T00:00:00Z',
            price: 549.99,
            reason: 'Promotion'
          },
          {
            date: '2024-02-01T00:00:00Z',
            price: 599.99,
            reason: 'PromotionEnd'
          }
        ]
      };

      service.getPriceHistory(productId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/price-history/${productId}?daysBack=90`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should use custom daysBack parameter', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';
      const daysBack = 30;

      service.getPriceHistory(productId, daysBack).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/price-history/${productId}?daysBack=30`);
      expect(req.request.method).toBe('GET');
      req.flush({});
    });

    it('should default to 90 days if no daysBack specified', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';

      service.getPriceHistory(productId).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/price-history/${productId}?daysBack=90`);
      expect(req.request.method).toBe('GET');
      req.flush({});
    });
  });
});
