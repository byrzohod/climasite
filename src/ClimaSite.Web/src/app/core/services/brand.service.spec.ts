import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { BrandService } from './brand.service';
import { BrandListResponse, BrandBrief, Brand } from '../models/brand.model';
import { environment } from '../../../environments/environment';

describe('BrandService', () => {
  let service: BrandService;
  let httpMock: HttpTestingController;
  let translateService: TranslateService;

  const mockBrandBrief: BrandBrief = {
    id: 'brand-1',
    name: 'TestBrand',
    slug: 'testbrand',
    description: 'A test brand',
    logoUrl: 'https://example.com/logo.png',
    countryOfOrigin: 'USA',
    isFeatured: true,
    productCount: 5
  };

  const mockBrandListResponse: BrandListResponse = {
    items: [mockBrandBrief],
    pageNumber: 1,
    totalPages: 1,
    totalCount: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  const mockBrand: Brand = {
    ...mockBrandBrief,
    websiteUrl: 'https://testbrand.com',
    foundedYear: 2010,
    products: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        TranslateModule.forRoot()
      ],
      providers: [BrandService]
    });

    service = TestBed.inject(BrandService);
    httpMock = TestBed.inject(HttpTestingController);
    translateService = TestBed.inject(TranslateService);
    translateService.use('en');
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getBrands', () => {
    it('should call correct API endpoint with environment URL', () => {
      service.getBrands().subscribe(response => {
        expect(response).toEqual(mockBrandListResponse);
      });

      const req = httpMock.expectOne(req => 
        req.url.startsWith(`${environment.apiUrl}/api/brands`)
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.url).toContain(environment.apiUrl);
      req.flush(mockBrandListResponse);
    });

    it('should include pagination parameters', () => {
      service.getBrands(2, 12).subscribe();

      const req = httpMock.expectOne(req => 
        req.url.includes('/api/brands') &&
        req.params.get('pageNumber') === '2' &&
        req.params.get('pageSize') === '12'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockBrandListResponse);
    });

    it('should include featured filter when specified', () => {
      service.getBrands(1, 24, true).subscribe();

      const req = httpMock.expectOne(req => 
        req.url.includes('/api/brands') &&
        req.params.get('featured') === 'true'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockBrandListResponse);
    });

    it('should include language parameter', () => {
      translateService.use('bg');
      
      service.getBrands().subscribe();

      const req = httpMock.expectOne(req => 
        req.url.includes('/api/brands') &&
        req.params.get('lang') === 'bg'
      );
      req.flush(mockBrandListResponse);
    });
  });

  describe('getBrandBySlug', () => {
    it('should call correct API endpoint with slug', () => {
      const slug = 'testbrand';
      
      service.getBrandBySlug(slug).subscribe(response => {
        expect(response).toEqual(mockBrand);
      });

      const req = httpMock.expectOne(req => 
        req.url.includes(`${environment.apiUrl}/api/brands/${slug}`)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockBrand);
    });

    it('should include product pagination parameters', () => {
      service.getBrandBySlug('testbrand', 2, 24).subscribe();

      const req = httpMock.expectOne(req => 
        req.url.includes('/api/brands/testbrand') &&
        req.params.get('productPage') === '2' &&
        req.params.get('productPageSize') === '24'
      );
      req.flush(mockBrand);
    });
  });

  describe('getFeaturedBrands', () => {
    it('should call correct API endpoint', () => {
      const featuredBrands = [mockBrandBrief];
      
      service.getFeaturedBrands().subscribe(response => {
        expect(response).toEqual(featuredBrands);
      });

      const req = httpMock.expectOne(req => 
        req.url.includes(`${environment.apiUrl}/api/brands/featured`)
      );
      expect(req.request.method).toBe('GET');
      req.flush(featuredBrands);
    });

    it('should include limit parameter', () => {
      service.getFeaturedBrands(4).subscribe();

      const req = httpMock.expectOne(req => 
        req.url.includes('/api/brands/featured') &&
        req.params.get('limit') === '4'
      );
      req.flush([mockBrandBrief]);
    });
  });
});
