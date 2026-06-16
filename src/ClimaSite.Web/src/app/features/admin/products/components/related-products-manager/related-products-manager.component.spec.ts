import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component } from '@angular/core';
import { RelatedProductsManagerComponent } from './related-products-manager.component';
import { AdminRelatedProductsService, ProductRelationsDto } from '../../services/admin-related-products.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({
      'admin.products.relatedProducts': 'Related Products',
      'admin.products.noRelations': 'No related products',
      'admin.products.addRelation': 'Add Related Product',
      'admin.products.searchProducts': 'Search products...',
      'admin.products.relations.Similar': 'Similar Products',
      'admin.products.relations.Accessory': 'Accessories',
      'admin.products.relations.Upgrade': 'Upgrades',
      'admin.products.relations.Bundle': 'Bundles',
      'admin.products.relations.FrequentlyBoughtTogether': 'Frequently Bought Together',
      'common.loading': 'Loading...'
    });
  }
}

@Component({
  standalone: true,
  imports: [RelatedProductsManagerComponent],
  template: '<app-related-products-manager [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('RelatedProductsManagerComponent', () => {
  let component: RelatedProductsManagerComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const mockRelations: ProductRelationsDto = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    productName: 'Test Product',
    relationGroups: [
      {
        relationType: 'Similar',
        relations: [
          {
            id: 'rel-1',
            relatedProductId: 'prod-2',
            name: 'Similar Product 1',
            sku: 'SKU-001',
            price: 199.99,
            sortOrder: 0
          },
          {
            id: 'rel-2',
            relatedProductId: 'prod-3',
            name: 'Similar Product 2',
            sku: 'SKU-002',
            price: 299.99,
            sortOrder: 1
          }
        ]
      },
      {
        relationType: 'Accessory',
        relations: []
      },
      {
        relationType: 'Upgrade',
        relations: []
      },
      {
        relationType: 'Bundle',
        relations: []
      },
      {
        relationType: 'FrequentlyBoughtTogether',
        relations: []
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        RelatedProductsManagerComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        AdminRelatedProductsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    expect(component).toBeTruthy();
  }));

  it('should load relations on init', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    expect(component.relations()).toEqual(mockRelations);
    expect(component.loading()).toBeFalsy();
  }));

  it('should display relation type tabs', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    const tabs = fixture.nativeElement.querySelectorAll('.relation-tabs button');
    expect(tabs.length).toBe(5);
  }));

  it('should show relations for active type', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    // Default active type is 'Similar'
    const currentRelations = component.currentRelations();
    expect(currentRelations.length).toBe(2);
  }));

  it('should change active type', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    expect(component.activeType()).toBe('Similar');

    component.setActiveType('Accessory');
    expect(component.activeType()).toBe('Accessory');
    expect(component.currentRelations().length).toBe(0);
  }));

  it('should return correct count for relation type', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    expect(component.getCount('Similar')).toBe(2);
    expect(component.getCount('Accessory')).toBe(0);
  }));

  it('should remove relation when remove button clicked', fakeAsync(() => {
    fixture.detectChanges();
    const req1 = httpMock.expectOne(req => req.url.includes('/relations') && req.method === 'GET');
    req1.flush(mockRelations);
    tick();
    fixture.detectChanges();

    component.removeRelation('rel-1');

    const deleteReq = httpMock.expectOne(req => req.method === 'DELETE');
    expect(deleteReq.request.url).toContain('/relations/rel-1');
    deleteReq.flush({ success: true });
    tick();

    // Should reload relations after deletion
    const req2 = httpMock.expectOne(req => req.url.includes('/relations') && req.method === 'GET');
    req2.flush(mockRelations);
    tick();
  }));

  it('should show loading state initially', fakeAsync(() => {
    fixture.detectChanges();

    expect(component.loading()).toBeTruthy();
    const loadingElement = fixture.nativeElement.querySelector('.loading-state');
    expect(loadingElement).toBeTruthy();

    const req = httpMock.expectOne(req => req.url.includes('/relations'));
    req.flush(mockRelations);
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBeFalsy();
  }));

  it('should not search when the term is shorter than two characters (BUG-14)', fakeAsync(() => {
    fixture.detectChanges();
    httpMock.expectOne(req => req.url.includes('/relations')).flush(mockRelations);
    tick();

    component.searchTerm = 'a';
    component.searchProducts();
    tick();

    expect(component.searchResults().length).toBe(0);
    // No search request should have been issued.
    httpMock.expectNone(req => req.params.has('search'));
  }));

  it('should call the products search endpoint and exclude self/existing relations (BUG-14)', fakeAsync(() => {
    fixture.detectChanges();
    httpMock.expectOne(req => req.url.includes('/relations')).flush(mockRelations);
    tick();

    component.searchTerm = 'cooler';
    component.searchProducts();

    const searchReq = httpMock.expectOne(req => req.params.get('search') === 'cooler');
    expect(searchReq.request.method).toBe('GET');
    expect(searchReq.request.params.get('pageSize')).toBe('10');
    expect(searchReq.request.params.get('status')).toBe('Active');

    searchReq.flush({
      items: [
        // Self — should be excluded.
        {
          id: '123e4567-e89b-12d3-a456-426614174000',
          name: 'Test Product',
          sku: 'SELF',
          slug: 'test-product',
          price: 100,
          salePrice: null,
          stockQuantity: 5,
          status: 'Active',
          primaryImageUrl: null,
          categoryName: null,
          createdAt: '2026-06-15T00:00:00Z',
          updatedAt: '2026-06-15T00:00:00Z'
        },
        // Already related (relatedProductId prod-2) — should be excluded.
        {
          id: 'prod-2',
          name: 'Similar Product 1',
          sku: 'SKU-001',
          slug: 'similar-1',
          price: 199.99,
          salePrice: null,
          stockQuantity: 5,
          status: 'Active',
          primaryImageUrl: null,
          categoryName: null,
          createdAt: '2026-06-15T00:00:00Z',
          updatedAt: '2026-06-15T00:00:00Z'
        },
        // New candidate — should be kept.
        {
          id: 'prod-9',
          name: 'Brand New Cooler',
          sku: 'SKU-009',
          slug: 'brand-new-cooler',
          price: 399.99,
          salePrice: null,
          stockQuantity: 5,
          status: 'Active',
          primaryImageUrl: 'https://img/9.jpg',
          categoryName: null,
          createdAt: '2026-06-15T00:00:00Z',
          updatedAt: '2026-06-15T00:00:00Z'
        }
      ],
      totalCount: 3,
      pageNumber: 1,
      pageSize: 10,
      totalPages: 1
    });
    tick();

    const results = component.searchResults();
    expect(results.length).toBe(1);
    expect(results[0].id).toBe('prod-9');
    expect(results[0].sku).toBe('SKU-009');
    expect(results[0].primaryImageUrl).toBe('https://img/9.jpg');
  }));
});
