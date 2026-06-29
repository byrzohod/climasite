import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminProductsComponent } from './admin-products.component';
import { AdminProductsList, AdminProductDetail } from '../../../core/services/admin-products.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AdminProductsComponent', () => {
  let fixture: ComponentFixture<AdminProductsComponent>;
  let component: AdminProductsComponent;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/products`;

  const productsList: AdminProductsList = {
    items: [
      {
        id: 'prod-1',
        name: 'Test AC Unit',
        sku: 'AC-001',
        slug: 'test-ac-unit',
        // On sale: price is the current selling price (499.99); salePrice carries the
        // higher original/compare-at (599.99) that renders struck-through.
        price: 499.99,
        salePrice: 599.99,
        stockQuantity: 10,
        status: 'Active',
        primaryImageUrl: 'https://img/ac.jpg',
        categoryName: 'Air Conditioners',
        createdAt: '2026-06-15T00:00:00Z',
        updatedAt: '2026-06-15T00:00:00Z'
      },
      {
        id: 'prod-2',
        name: 'Inactive Heater',
        sku: 'HT-002',
        slug: 'inactive-heater',
        price: 299,
        salePrice: null,
        stockQuantity: 0,
        status: 'Inactive',
        primaryImageUrl: null,
        categoryName: 'Heating',
        createdAt: '2026-06-14T00:00:00Z',
        updatedAt: '2026-06-14T00:00:00Z'
      }
    ],
    totalCount: 2,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AdminProductsComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminProductsComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads and renders product rows', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(productsList);
    fixture.detectChanges();

    expect(component.products().length).toBe(2);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="product-row"]');
    expect(rows.length).toBe(2);

    const page = fixture.nativeElement.querySelector('[data-testid="admin-products-page"]');
    expect(page).toBeTruthy();

    const badges = fixture.nativeElement.querySelectorAll('[data-testid="product-status-badge"]');
    expect(badges.length).toBe(2);
  });

  it('renders the current price prominently and the original compare-at struck-through (B-002)', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('[data-testid="product-row"]');

    // On-sale row: prominent .sale-price = current selling price (499.99),
    // struck .old-price = original/compare-at (599.99). Must NOT be inverted.
    const onSale = rows[0];
    const current = onSale.querySelector('.sale-price');
    const original = onSale.querySelector('.old-price');
    expect(current).toBeTruthy();
    expect(original).toBeTruthy();
    expect(current.textContent).toContain('499.99');
    expect(original.textContent).toContain('599.99');

    // Not-on-sale row: single price, no struck-through element.
    const plain = rows[1];
    expect(plain.querySelector('.old-price')).toBeNull();
    expect(plain.querySelector('.sale-price')).toBeNull();
    expect(plain.textContent).toContain('299');
  });

  it('shows the error state with a retry button when loading fails', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.error()).toBe('admin.products.error');
    const errorBox = fixture.nativeElement.querySelector('[data-testid="products-error"]');
    expect(errorBox).toBeTruthy();
    const retry = fixture.nativeElement.querySelector('[data-testid="products-retry"]');
    expect(retry).toBeTruthy();
  });

  it('shows the empty state when there are no products', () => {
    fixture.detectChanges();

    httpMock.expectOne(r => r.url === baseUrl).flush({
      ...productsList,
      items: [],
      totalCount: 0,
      totalPages: 0
    });
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('[data-testid="products-empty"]');
    expect(empty).toBeTruthy();
  });

  it('reloads with status=Active when the Active status filter is applied', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);

    component.applyStatusFilter('Active');

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('status')).toBe('Active');
    expect(component.page()).toBe(1);
    req.flush(productsList);
  });

  it('reloads with the search param when a search is applied', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);

    component.applySearch('AC');

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('search')).toBe('AC');
    req.flush(productsList);
  });

  it('opens and closes the create form without extra requests', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);
    fixture.detectChanges();

    component.openCreate();
    fixture.detectChanges();
    expect(component.showForm()).toBeTrue();
    expect(component.formMode()).toBe('create');
    const form = fixture.nativeElement.querySelector('[data-testid="product-form"]');
    expect(form).toBeTruthy();

    component.cancelForm();
    fixture.detectChanges();
    expect(component.showForm()).toBeFalse();
  });

  it('opens edit and fetches the product detail by id', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);
    fixture.detectChanges();

    component.openEdit(productsList.items[0]);

    // Detail is fetched by the row's GUID id, NOT the slug.
    const detailReq = httpMock.expectOne(`${baseUrl}/prod-1`);
    expect(detailReq.request.method).toBe('GET');

    const detail: AdminProductDetail = {
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
    detailReq.flush(detail);
    fixture.detectChanges();

    expect(component.formMode()).toBe('edit');
    expect(component.editingId()).toBe('prod-1');
    expect(component.form.brand).toBe('Daikin');
    expect(component.form.compareAtPrice).toBe(699.99);

    // Child editors (translations + relations) load on the productId input;
    // flush them so httpMock.verify() stays clean.
    httpMock.match(r => r.url.includes('/translations')).forEach(req =>
      req.flush({
        productId: 'prod-1',
        productName: 'Test AC Unit',
        defaultLanguage: 'en',
        translations: [],
        availableLanguages: ['en', 'bg', 'de']
      })
    );
    httpMock.match(r => r.url.includes('/relations')).forEach(req =>
      req.flush({ productId: 'prod-1', productName: 'Test AC Unit', relationGroups: [] })
    );
  });

  it('toggles featured via PATCH and reloads', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);

    component.toggleFeatured(productsList.items[0]);

    const patch = httpMock.expectOne(`${baseUrl}/prod-1/featured`);
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toEqual({ isFeatured: true });
    patch.flush({ success: true });

    // Reloads the list after a successful toggle.
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);
  });

  it('activates an inactive product via PATCH status=true', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);

    component.activate(productsList.items[1]);

    const patch = httpMock.expectOne(`${baseUrl}/prod-2/status`);
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toEqual({ isActive: true });
    patch.flush({ success: true });

    httpMock.expectOne(r => r.url === baseUrl).flush(productsList);
  });
});
