import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import {
  AdminRelatedProductsService,
  ProductRelationsDto,
  AddRelationRequest,
  ReorderRelationsRequest
} from './admin-related-products.service';
import { environment } from '../../../../../environments/environment';

describe('AdminRelatedProductsService', () => {
  let service: AdminRelatedProductsService;
  let httpMock: HttpTestingController;

  const productId = '123e4567-e89b-12d3-a456-426614174000';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminRelatedProductsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AdminRelatedProductsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProductRelations', () => {
    it('should fetch product relations', () => {
      const mockResponse: ProductRelationsDto = {
        productId,
        productName: 'Test Product',
        relationGroups: [
          {
            relationType: 'Similar',
            relations: [
              {
                id: 'rel-1',
                relatedProductId: 'prod-2',
                name: 'Related Product',
                sku: 'SKU-002',
                price: 299.99,
                sortOrder: 0
              }
            ]
          }
        ]
      };

      service.getProductRelations(productId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/relations`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('addRelation', () => {
    it('should add a new relation', () => {
      const request: AddRelationRequest = {
        relatedProductId: 'prod-2',
        relationType: 'Similar'
      };
      const mockResponse = { relationId: 'new-rel-id' };

      service.addRelation(productId, request).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/relations`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });
  });

  describe('removeRelation', () => {
    it('should remove a relation', () => {
      const relationId = 'rel-1';

      service.removeRelation(productId, relationId).subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/relations/${relationId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ success: true });
    });
  });

  describe('reorderRelations', () => {
    it('should reorder relations', () => {
      const request: ReorderRelationsRequest = {
        relationType: 'Similar',
        relationIds: ['rel-2', 'rel-1', 'rel-3']
      };

      service.reorderRelations(productId, request).subscribe(result => {
        expect(result.success).toBe(true);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/admin/products/${productId}/relations/reorder`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush({ success: true });
    });
  });
});
