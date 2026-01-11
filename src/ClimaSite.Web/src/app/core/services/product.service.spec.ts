import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ProductService } from './product.service';
import { LanguageService } from './language.service';
import { environment } from '../../../environments/environment';
import { signal } from '@angular/core';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;
  let languageServiceMock: jasmine.SpyObj<LanguageService>;

  beforeEach(() => {
    languageServiceMock = jasmine.createSpyObj('LanguageService', [], {
      currentLanguage: signal('en')
    });

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        ProductService,
        { provide: LanguageService, useValue: languageServiceMock }
      ]
    });

    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getProducts', () => {
    it('should fetch products without lang param when language is English', () => {
      const mockProducts = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0, hasPreviousPage: false, hasNextPage: false };

      service.getProducts().subscribe(result => {
        expect(result).toEqual(mockProducts);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/products`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush(mockProducts);
    });

    it('should include lang param when language is not English', () => {
      Object.defineProperty(languageServiceMock, 'currentLanguage', {
        value: signal('bg')
      });

      const mockProducts = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0, hasPreviousPage: false, hasNextPage: false };

      service.getProducts().subscribe(result => {
        expect(result).toEqual(mockProducts);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('lang')).toBe('bg');
      req.flush(mockProducts);
    });

    it('should include filter parameters', () => {
      const mockProducts = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0, hasPreviousPage: false, hasNextPage: false };

      service.getProducts({
        pageNumber: 2,
        pageSize: 24,
        brand: 'TestBrand',
        minPrice: 100,
        maxPrice: 500
      }).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products`);
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('24');
      expect(req.request.params.get('brand')).toBe('TestBrand');
      expect(req.request.params.get('minPrice')).toBe('100');
      expect(req.request.params.get('maxPrice')).toBe('500');
      req.flush(mockProducts);
    });
  });

  describe('getProductBySlug', () => {
    it('should fetch product by slug without lang param when language is English', () => {
      const mockProduct = { id: '1', name: 'Test Product', slug: 'test-product' };

      service.getProductBySlug('test-product').subscribe(result => {
        expect(result).toEqual(mockProduct as any);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/products/test-product`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.has('lang')).toBeFalse();
      req.flush(mockProduct);
    });

    it('should include lang param when language is not English', () => {
      Object.defineProperty(languageServiceMock, 'currentLanguage', {
        value: signal('de')
      });

      const mockProduct = { id: '1', name: 'Test Produkt', slug: 'test-product' };

      service.getProductBySlug('test-product').subscribe(result => {
        expect(result).toEqual(mockProduct as any);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/test-product`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('lang')).toBe('de');
      req.flush(mockProduct);
    });
  });

  describe('getFeaturedProducts', () => {
    it('should fetch featured products with count', () => {
      const mockProducts = [{ id: '1', name: 'Featured Product' }];

      service.getFeaturedProducts(4).subscribe(result => {
        expect(result).toEqual(mockProducts as any);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/featured`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('count')).toBe('4');
      req.flush(mockProducts);
    });

    it('should include lang param when language is not English', () => {
      Object.defineProperty(languageServiceMock, 'currentLanguage', {
        value: signal('bg')
      });

      const mockProducts = [{ id: '1', name: 'Featured Product BG' }];

      service.getFeaturedProducts().subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/featured`);
      expect(req.request.params.get('lang')).toBe('bg');
      req.flush(mockProducts);
    });
  });

  describe('searchProducts', () => {
    it('should search products with query', () => {
      const mockProducts = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0, hasPreviousPage: false, hasNextPage: false };

      service.searchProducts({ q: 'air conditioner' }).subscribe(result => {
        expect(result).toEqual(mockProducts);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/search`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('q')).toBe('air conditioner');
      req.flush(mockProducts);
    });

    it('should include lang param when language is not English', () => {
      Object.defineProperty(languageServiceMock, 'currentLanguage', {
        value: signal('de')
      });

      const mockProducts = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0, hasPreviousPage: false, hasNextPage: false };

      service.searchProducts({ q: 'klimaanlage' }).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/search`);
      expect(req.request.params.get('lang')).toBe('de');
      req.flush(mockProducts);
    });
  });

  describe('getRelatedProducts', () => {
    it('should fetch related products', () => {
      const mockProducts = [{ id: '2', name: 'Related Product' }];

      service.getRelatedProducts('product-123', 4).subscribe(result => {
        expect(result).toEqual(mockProducts as any);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/product-123/related`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('count')).toBe('4');
      req.flush(mockProducts);
    });

    it('should include lang param when language is not English', () => {
      Object.defineProperty(languageServiceMock, 'currentLanguage', {
        value: signal('bg')
      });

      const mockProducts = [{ id: '2', name: 'Related Product BG' }];

      service.getRelatedProducts('product-123').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/product-123/related`);
      expect(req.request.params.get('lang')).toBe('bg');
      req.flush(mockProducts);
    });
  });

  describe('getFilterOptions', () => {
    it('should fetch filter options', () => {
      const mockOptions = { brands: [], priceRange: { min: 0, max: 1000 }, specifications: {}, tags: [] };

      service.getFilterOptions().subscribe(result => {
        expect(result).toEqual(mockOptions);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/products/filters`);
      expect(req.request.method).toBe('GET');
      req.flush(mockOptions);
    });

    it('should include category slug when provided', () => {
      const mockOptions = { brands: [], priceRange: { min: 0, max: 1000 }, specifications: {}, tags: [] };

      service.getFilterOptions('air-conditioners').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/api/products/filters`);
      expect(req.request.params.get('categorySlug')).toBe('air-conditioners');
      req.flush(mockOptions);
    });
  });
});
