import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PromotionService } from './promotion.service';
import { LanguageService, SupportedLanguage } from './language.service';
import { Promotion, PromotionBrief, PromotionType } from '../models/promotion.model';
import { PaginatedResult } from '../models/product.model';
import { environment } from '../../../environments/environment';

describe('PromotionService', () => {
  let service: PromotionService;
  let httpMock: HttpTestingController;
  let langSignal: WritableSignal<SupportedLanguage>;
  const apiUrl = `${environment.apiUrl}/api/promotions`;

  const mockBrief: PromotionBrief = {
    id: 'promo-1',
    name: 'Summer Sale',
    slug: 'summer-sale',
    description: 'Cool deals',
    code: 'SUMMER',
    type: PromotionType.Percentage,
    discountValue: 20,
    startDate: '2025-06-01T00:00:00Z',
    endDate: '2025-08-31T00:00:00Z',
    bannerImageUrl: 'https://example.com/banner.png',
    thumbnailImageUrl: 'https://example.com/thumb.png',
    productCount: 8
  };

  const mockPaginated: PaginatedResult<PromotionBrief> = {
    items: [mockBrief],
    pageNumber: 1,
    pageSize: 12,
    totalCount: 1,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  const mockPromotion: Promotion = {
    id: 'promo-1',
    name: 'Summer Sale',
    slug: 'summer-sale',
    description: 'Cool deals',
    code: 'SUMMER',
    type: PromotionType.Percentage,
    discountValue: 20,
    minimumOrderAmount: 100,
    startDate: '2025-06-01T00:00:00Z',
    endDate: '2025-08-31T00:00:00Z',
    bannerImageUrl: 'https://example.com/banner.png',
    thumbnailImageUrl: 'https://example.com/thumb.png',
    isActive: true,
    isFeatured: true,
    termsAndConditions: 'See store',
    products: []
  };

  beforeEach(() => {
    langSignal = signal<SupportedLanguage>('en');

    TestBed.configureTestingModule({
      providers: [
        PromotionService,
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: LanguageService,
          useValue: { currentLanguage: langSignal }
        }
      ]
    });

    service = TestBed.inject(PromotionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getActivePromotions', () => {
    it('should GET active promotions with default paging and no lang for en', () => {
      let result: PaginatedResult<PromotionBrief> | undefined;
      service.getActivePromotions().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('12');
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush(mockPaginated);

      expect(result).toEqual(mockPaginated);
    });

    it('should pass explicit paging parameters', () => {
      service.getActivePromotions(3, 24).subscribe();

      const req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.params.get('pageNumber')).toBe('3');
      expect(req.request.params.get('pageSize')).toBe('24');
      req.flush(mockPaginated);
    });

    it('should append the lang param for a non-en language', () => {
      langSignal.set('bg');
      service.getActivePromotions().subscribe();

      const req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.params.get('lang')).toBe('bg');
      req.flush(mockPaginated);
    });

    it('should not append lang for de when reset to en', () => {
      langSignal.set('de');
      service.getActivePromotions().subscribe();
      let req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.params.get('lang')).toBe('de');
      req.flush(mockPaginated);

      langSignal.set('en');
      service.getActivePromotions().subscribe();
      req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush(mockPaginated);
    });

    it('should map an empty result set', () => {
      const empty: PaginatedResult<PromotionBrief> = {
        items: [],
        pageNumber: 1,
        pageSize: 12,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false
      };
      let result: PaginatedResult<PromotionBrief> | undefined;
      service.getActivePromotions().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === apiUrl);
      req.flush(empty);

      expect(result?.items).toEqual([]);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.getActivePromotions().subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(r => r.url === apiUrl);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });

  describe('getPromotionBySlug', () => {
    it('should GET a promotion by slug without lang for en', () => {
      let result: Promotion | undefined;
      service.getPromotionBySlug('summer-sale').subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/summer-sale`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush(mockPromotion);

      expect(result).toEqual(mockPromotion);
    });

    it('should append the lang param for a non-en language', () => {
      langSignal.set('de');
      service.getPromotionBySlug('summer-sale').subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/summer-sale`);
      expect(req.request.params.get('lang')).toBe('de');
      req.flush(mockPromotion);
    });

    it('should propagate a 404 for an unknown slug', () => {
      let status = 0;
      service.getPromotionBySlug('missing').subscribe({ error: (e) => (status = e.status) });

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/missing`);
      req.flush('not found', { status: 404, statusText: 'Not Found' });

      expect(status).toBe(404);
    });
  });

  describe('getFeaturedPromotions', () => {
    it('should GET featured promotions with the default count and no lang for en', () => {
      let result: PromotionBrief[] | undefined;
      service.getFeaturedPromotions().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/featured`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('count')).toBe('4');
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush([mockBrief]);

      expect(result).toEqual([mockBrief]);
    });

    it('should pass an explicit count', () => {
      service.getFeaturedPromotions(8).subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/featured`);
      expect(req.request.params.get('count')).toBe('8');
      req.flush([mockBrief]);
    });

    it('should append the lang param for a non-en language', () => {
      langSignal.set('bg');
      service.getFeaturedPromotions(2).subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/featured`);
      expect(req.request.params.get('count')).toBe('2');
      expect(req.request.params.get('lang')).toBe('bg');
      req.flush([mockBrief]);
    });

    it('should map an empty featured list', () => {
      let result: PromotionBrief[] | undefined;
      service.getFeaturedPromotions().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/featured`);
      req.flush([]);

      expect(result).toEqual([]);
    });
  });
});
