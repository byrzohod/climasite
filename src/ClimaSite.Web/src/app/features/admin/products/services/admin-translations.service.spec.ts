import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import {
  AdminTranslationsService,
  ProductTranslationsDto,
  AddTranslationRequest,
  UpdateTranslationRequest
} from './admin-translations.service';
import { environment } from '../../../../../environments/environment';

describe('AdminTranslationsService', () => {
  let service: AdminTranslationsService;
  let httpMock: HttpTestingController;

  const productId = '123e4567-e89b-12d3-a456-426614174000';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminTranslationsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AdminTranslationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProductTranslations', () => {
    it('should fetch product translations', () => {
      const mockResponse: ProductTranslationsDto = {
        productId,
        productName: 'Test Product',
        defaultLanguage: 'en',
        translations: [
          {
            id: 'trans-1',
            languageCode: 'bg',
            name: 'Тестов продукт',
            shortDescription: 'Кратко описание',
            createdAt: '2024-01-01T00:00:00Z',
            updatedAt: '2024-01-01T00:00:00Z'
          }
        ],
        availableLanguages: ['de']
      };

      service.getProductTranslations(productId).subscribe(result => {
        expect(result).toEqual(mockResponse);
        expect(result.translations.length).toBe(1);
        expect(result.translations[0].languageCode).toBe('bg');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/translations`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('addTranslation', () => {
    it('should add a new translation', () => {
      const request: AddTranslationRequest = {
        languageCode: 'de',
        name: 'German Product Name',
        shortDescription: 'Short description in German',
        description: 'Full description in German'
      };
      const mockResponse = { translationId: 'new-trans-id' };

      service.addTranslation(productId, request).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/translations`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });
  });

  describe('updateTranslation', () => {
    it('should update an existing translation', () => {
      const languageCode = 'bg';
      const request: UpdateTranslationRequest = {
        name: 'Updated Bulgarian Name',
        shortDescription: 'Updated short description',
        description: 'Updated full description'
      };

      service.updateTranslation(productId, languageCode, request).subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/translations/${languageCode}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush({ success: true });
    });
  });

  describe('deleteTranslation', () => {
    it('should delete a translation', () => {
      const languageCode = 'de';

      service.deleteTranslation(productId, languageCode).subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/translations/${languageCode}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ success: true });
    });
  });
});
