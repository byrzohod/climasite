import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminUsersComponent } from './admin-users.component';
import {
  AdminCustomersList,
  AdminCustomerDetail
} from '../../../core/services/admin-customers.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AdminUsersComponent', () => {
  let fixture: ComponentFixture<AdminUsersComponent>;
  let component: AdminUsersComponent;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/customers`;

  const customersList: AdminCustomersList = {
    items: [
      {
        id: 'cust-1',
        email: 'jane@test.com',
        fullName: 'Jane Doe',
        phone: '+359888123456',
        isActive: true,
        emailConfirmed: true,
        orderCount: 3,
        totalSpent: 1799.97,
        lastLoginAt: '2026-06-15T00:00:00Z',
        createdAt: '2026-01-01T00:00:00Z'
      },
      {
        id: 'cust-2',
        email: 'john@test.com',
        fullName: 'John Smith',
        phone: null,
        isActive: false,
        emailConfirmed: false,
        orderCount: 0,
        totalSpent: 0,
        lastLoginAt: null,
        createdAt: '2026-02-01T00:00:00Z'
      }
    ],
    totalCount: 2,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1
  };

  const customerDetail: AdminCustomerDetail = {
    id: 'cust-1',
    email: 'jane@test.com',
    firstName: 'Jane',
    lastName: 'Doe',
    phoneNumber: '+359888123456',
    isActive: true,
    emailConfirmed: true,
    preferredLanguage: 'en',
    preferredCurrency: 'EUR',
    lastLoginAt: '2026-06-15T00:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-06-15T00:00:00Z',
    addresses: [],
    stats: {
      totalOrders: 3,
      totalSpent: 1799.97,
      averageOrderValue: 599.99,
      reviewsWritten: 2,
      wishlistItems: 5
    },
    recentOrders: []
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AdminUsersComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads and renders customer rows', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(customersList);
    fixture.detectChanges();

    expect(component.customers().length).toBe(2);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="customer-row"]');
    expect(rows.length).toBe(2);

    const page = fixture.nativeElement.querySelector('[data-testid="admin-customers-page"]');
    expect(page).toBeTruthy();
  });

  it('shows the error state with a retry button when loading fails', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.error()).toBe('admin.users.error');
    const errorBox = fixture.nativeElement.querySelector('[data-testid="customers-error"]');
    expect(errorBox).toBeTruthy();
    const retry = fixture.nativeElement.querySelector('[data-testid="customers-retry"]');
    expect(retry).toBeTruthy();
  });

  it('shows the empty state when there are no customers', () => {
    fixture.detectChanges();

    httpMock.expectOne(r => r.url === baseUrl).flush({
      ...customersList,
      items: [],
      totalCount: 0,
      totalPages: 0
    });
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('[data-testid="customers-empty"]');
    expect(empty).toBeTruthy();
  });

  it('reloads with a status filter applied', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(customersList);

    component.applyStatusFilter('inactive');

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('status')).toBe('inactive');
    expect(component.page()).toBe(1);
    req.flush(customersList);
  });

  it('opens the detail panel and fetches the selected customer', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(customersList);
    fixture.detectChanges();

    component.openDetail('cust-1');

    const detailReq = httpMock.expectOne(`${baseUrl}/cust-1`);
    expect(detailReq.request.method).toBe('GET');
    detailReq.flush(customerDetail);
    fixture.detectChanges();

    expect(component.detailOpen()).toBeTrue();
    expect(component.detail()?.email).toBe('jane@test.com');
    const panel = fixture.nativeElement.querySelector('[data-testid="customer-detail"]');
    expect(panel).toBeTruthy();
    const badge = fixture.nativeElement.querySelector('[data-testid="customer-active-badge"]');
    expect(badge).toBeTruthy();
  });

  it('toggles customer status then refreshes detail and list', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(customersList);
    fixture.detectChanges();

    component.openDetail('cust-1');
    httpMock.expectOne(`${baseUrl}/cust-1`).flush(customerDetail);
    fixture.detectChanges();

    component.toggleStatus(component.detail()!);

    const statusReq = httpMock.expectOne(`${baseUrl}/cust-1/status`);
    expect(statusReq.request.method).toBe('PUT');
    expect(statusReq.request.body).toEqual({ isActive: false });
    statusReq.flush(null);

    // After success it refreshes both the detail panel and the list.
    httpMock.expectOne(`${baseUrl}/cust-1`).flush(customerDetail);
    httpMock.expectOne(r => r.url === baseUrl).flush(customersList);

    expect(component.actionSuccess()).toBe('admin.users.toasts.deactivated');
  });

  it('closes the detail panel and clears state', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(customersList);
    fixture.detectChanges();

    component.openDetail('cust-1');
    httpMock.expectOne(`${baseUrl}/cust-1`).flush(customerDetail);
    fixture.detectChanges();

    component.closeDetail();
    fixture.detectChanges();

    expect(component.detailOpen()).toBeFalse();
    expect(component.detail()).toBeNull();
    const panel = fixture.nativeElement.querySelector('[data-testid="customer-detail"]');
    expect(panel).toBeFalsy();
  });
});
