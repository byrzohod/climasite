import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import {
  AdminProductsService,
  AdminProductsList,
  AdminProductDetail,
  CreateProductPayload,
  UpdateProductPayload,
  PRODUCT_STATUSES
} from './admin-products.service';

describe('AdminProductsService', () => {
  let service: AdminProductsService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/products`;

  const productsList: AdminProductsList = {
    items: [
      {
        id: 'prod-1',
        name: 'Test AC Unit',
        sku: 'AC-001',
        slug: 'test-ac-unit',
        price: 599.99,
        salePrice: 499.99,
        stockQuantity: 10,
        status: 'Active',
        primaryImageUrl: 'https://img/ac.jpg',
        categoryName: 'Air Conditioners',
        createdAt: '2026-06-15T00:00:00Z',
        updatedAt: '2026-06-15T00:00:00Z'
      }
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1
  };

  const productDetail: AdminProductDetail = {
    id: 'prod-1',
    name: 'Test AC Unit',
    sku: 'AC-001',
    slug: 'test-ac-unit',
    shortDescription: 'Short',
    description: 'Long',
    basePrice: 599.99,
    compareAtPrice: 699.99,
    costPrice: 300,
    categoryId: 'cat-1',
    categoryName: 'Air Conditioners',
    brand: 'Daikin',
    model: 'X1',
    isActive: true,
    isFeatured: false,
    requiresInstallation: true,
    warrantyMonths: 24,
    weightKg: 12.5,
    metaTitle: 'AC',
    metaDescription: 'AC desc',
    specifications: {},
    features: [],
    tags: [],
    images: [],
    variants: [],
    createdAt: '2026-06-15T00:00:00Z',
    updatedAt: '2026-06-15T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminProductsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminProductsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests products with default pagination params', () => {
    service.getProducts().subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.has('search')).toBeFalse();
    expect(req.request.params.has('status')).toBeFalse();
    req.flush(productsList);
  });

  it('forwards search, status, price range, sortBy, and sortOrder params when provided', () => {
    service.getProducts({
      pageNumber: 2,
      pageSize: 10,
      search: 'AC',
      categoryId: 'cat-1',
      status: 'Inactive',
      minPrice: 100,
      maxPrice: 900,
      sortBy: 'price',
      sortOrder: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('search')).toBe('AC');
    expect(req.request.params.get('categoryId')).toBe('cat-1');
    expect(req.request.params.get('status')).toBe('Inactive');
    expect(req.request.params.get('minPrice')).toBe('100');
    expect(req.request.params.get('maxPrice')).toBe('900');
    expect(req.request.params.get('sortBy')).toBe('price');
    expect(req.request.params.get('sortOrder')).toBe('desc');
    req.flush(productsList);
  });

  it('fetches a single product by id', () => {
    service.getProduct('prod-1').subscribe(result => {
      expect(result.sku).toBe('AC-001');
    });

    const req = httpMock.expectOne(`${baseUrl}/prod-1`);
    expect(req.request.method).toBe('GET');
    req.flush(productDetail);
  });

  it('creates a product with the expected body and POST verb', () => {
    const payload: CreateProductPayload = {
      name: 'New AC',
      sku: 'AC-NEW',
      basePrice: 799.99,
      isActive: true,
      isFeatured: false
    };

    service.createProduct(payload).subscribe(result => {
      expect(result.id).toBe('prod-2');
      expect(result.slug).toBe('new-ac');
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 'prod-2', slug: 'new-ac' });
  });

  it('updates a product with the expected body and PUT verb (no sku in payload)', () => {
    const payload: UpdateProductPayload = {
      id: 'prod-1',
      slug: 'test-ac-unit',
      name: 'Updated AC',
      basePrice: 649.99,
      isActive: true,
      isFeatured: true
    } as UpdateProductPayload;

    service.updateProduct('prod-1', payload).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/prod-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    expect((req.request.body as Record<string, unknown>)['sku']).toBeUndefined();
    req.flush({ success: true });
  });

  it('deletes a product by id', () => {
    service.deleteProduct('prod-1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/prod-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });

  it('toggles status with a PATCH and isActive body', () => {
    service.toggleStatus('prod-1', false).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/prod-1/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({ success: true });
  });

  it('toggles featured with a PATCH and isFeatured body', () => {
    service.toggleFeatured('prod-1', true).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/prod-1/featured`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isFeatured: true });
    req.flush({ success: true });
  });

  it('searches products with search, pageSize=10 and status=Active', () => {
    service.searchProducts('cool').subscribe(result => {
      expect(result.items.length).toBe(1);
    });

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('cool');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('status')).toBe('Active');
    req.flush(productsList);
  });

  it('exposes the four product statuses', () => {
    expect(PRODUCT_STATUSES.length).toBe(4);
    expect(PRODUCT_STATUSES).toContain('OutOfStock');
    expect(PRODUCT_STATUSES).toContain('LowStock');
  });
});
