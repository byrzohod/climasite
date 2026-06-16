import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminOrdersComponent } from './admin-orders.component';
import { AdminOrdersList } from '../../../core/services/admin-orders.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AdminOrdersComponent', () => {
  let fixture: ComponentFixture<AdminOrdersComponent>;
  let component: AdminOrdersComponent;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/orders`;

  const ordersList: AdminOrdersList = {
    items: [
      {
        id: 'order-1',
        orderNumber: 'ORD-1001',
        customerName: 'Jane Doe',
        customerEmail: 'jane@test.com',
        totalAmount: 599.99,
        status: 'Pending',
        paymentStatus: 'Pending',
        itemCount: 2,
        createdAt: '2026-06-15T00:00:00Z'
      },
      {
        id: 'order-2',
        orderNumber: 'ORD-1002',
        customerName: 'John Smith',
        customerEmail: 'john@test.com',
        totalAmount: 1299,
        status: 'Shipped',
        paymentStatus: 'Paid',
        itemCount: 1,
        createdAt: '2026-06-14T00:00:00Z'
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
        AdminOrdersComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminOrdersComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads and renders order rows', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(ordersList);
    fixture.detectChanges();

    expect(component.orders().length).toBe(2);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="order-row"]');
    expect(rows.length).toBe(2);

    const page = fixture.nativeElement.querySelector('[data-testid="admin-orders-page"]');
    expect(page).toBeTruthy();
  });

  it('shows the error state with a retry button when loading fails', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.error()).toBe('admin.orders.errors.loadFailed');
    const errorBox = fixture.nativeElement.querySelector('[data-testid="orders-error"]');
    expect(errorBox).toBeTruthy();
    const retry = fixture.nativeElement.querySelector('[data-testid="orders-retry"]');
    expect(retry).toBeTruthy();
  });

  it('shows the empty state when there are no orders', () => {
    fixture.detectChanges();

    httpMock.expectOne(r => r.url === baseUrl).flush({
      ...ordersList,
      items: [],
      totalCount: 0,
      totalPages: 0
    });
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('[data-testid="orders-empty"]');
    expect(empty).toBeTruthy();
  });

  it('reloads with a status filter applied', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(ordersList);

    component.applyStatusFilter('Shipped');

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('status')).toBe('Shipped');
    expect(component.page()).toBe(1);
    req.flush(ordersList);
  });
});
