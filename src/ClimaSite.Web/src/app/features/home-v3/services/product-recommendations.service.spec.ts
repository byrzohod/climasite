import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { LanguageService } from '../../../core/services/language.service';
import { environment } from '../../../../environments/environment';
import { ProductRecommendationsService } from './product-recommendations.service';

describe('ProductRecommendationsService', () => {
  let service: ProductRecommendationsService;
  let httpMock: HttpTestingController;
  let languageServiceMock: jasmine.SpyObj<LanguageService>;
  const apiUrl = `${environment.apiUrl}/api/products/recommendations`;

  beforeEach(() => {
    languageServiceMock = jasmine.createSpyObj('LanguageService', [], {
      currentLanguage: signal('en')
    });

    TestBed.configureTestingModule({
      providers: [
        ProductRecommendationsService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LanguageService, useValue: languageServiceMock }
      ]
    });

    service = TestBed.inject(ProductRecommendationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests recommendations with area, room type, and zone parameters', () => {
    service.getRecommendations(36, 'office', 'C').subscribe(result => {
      expect(result).toEqual([]);
    });

    const req = httpMock.expectOne(request => request.url === apiUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('area')).toBe('36');
    expect(req.request.params.get('type')).toBe('office');
    expect(req.request.params.get('zone')).toBe('C');
    expect(req.request.params.has('lang')).toBeFalse();
    req.flush([]);
  });

  it('includes lang for non-English recommendation requests', () => {
    Object.defineProperty(languageServiceMock, 'currentLanguage', {
      value: signal('bg')
    });

    service.getRecommendations(24, 'living', 'B').subscribe();

    const req = httpMock.expectOne(request => request.url === apiUrl);
    expect(req.request.params.get('lang')).toBe('bg');
    req.flush([]);
  });
});
