import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import {
  AdminCustomersService,
  AdminCustomersList,
  AdminCustomerDetail
} from './admin-customers.service';

describe('AdminCustomersService', () => {
  let service: AdminCustomersService;
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
      }
    ],
    totalCount: 1,
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
    addresses: [
      {
        id: 'addr-1',
        addressLine1: '12 Vitosha Blvd',
        addressLine2: null,
        city: 'Sofia',
        state: 'Sofia-City',
        postalCode: '1000',
        country: 'Bulgaria',
        isDefault: true
      }
    ],
    stats: {
      totalOrders: 3,
      totalSpent: 1799.97,
      averageOrderValue: 599.99,
      reviewsWritten: 2,
      wishlistItems: 5
    },
    recentOrders: [
      {
        id: 'order-1',
        orderNumber: 'ORD-1001',
        total: 599.99,
        status: 'Delivered',
        createdAt: '2026-05-01T00:00:00Z'
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminCustomersService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminCustomersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests customers with default pagination params', () => {
    service.getCustomers().subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.has('search')).toBeFalse();
    expect(req.request.params.has('status')).toBeFalse();
    req.flush(customersList);
  });

  it('forwards search, status, date, and sort params when provided', () => {
    service.getCustomers({
      pageNumber: 2,
      pageSize: 10,
      search: 'jane',
      status: 'active',
      registeredFrom: '2026-01-01',
      registeredTo: '2026-06-15',
      sortBy: 'email',
      sortOrder: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('search')).toBe('jane');
    expect(req.request.params.get('status')).toBe('active');
    expect(req.request.params.get('registeredFrom')).toBe('2026-01-01');
    expect(req.request.params.get('registeredTo')).toBe('2026-06-15');
    expect(req.request.params.get('sortBy')).toBe('email');
    expect(req.request.params.get('sortOrder')).toBe('asc');
    req.flush(customersList);
  });

  it('fetches a single customer by id', () => {
    service.getCustomer('cust-1').subscribe(result => {
      expect(result.email).toBe('jane@test.com');
      expect(result.stats.totalOrders).toBe(3);
      expect(result.addresses.length).toBe(1);
      expect(result.recentOrders.length).toBe(1);
    });

    const req = httpMock.expectOne(`${baseUrl}/cust-1`);
    expect(req.request.method).toBe('GET');
    req.flush(customerDetail);
  });

  it('updates customer status with the expected verb and body', () => {
    service.updateStatus('cust-1', false).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/cust-1/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush(null);
  });

  it('sends isActive true when activating a customer', () => {
    service.updateStatus('cust-2', true).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/cust-2/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isActive: true });
    req.flush(null);
  });
});
