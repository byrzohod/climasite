import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import {
  AdminOrdersService,
  AdminOrdersList,
  AdminOrderDetail,
  getValidNextStatuses,
  ORDER_STATUSES
} from './admin-orders.service';

describe('AdminOrdersService', () => {
  let service: AdminOrdersService;
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
      }
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1
  };

  const orderDetail: AdminOrderDetail = {
    id: 'order-1',
    orderNumber: 'ORD-1001',
    userId: 'user-1',
    customerName: 'Jane Doe',
    customerEmail: 'jane@test.com',
    customerPhone: '+359888123456',
    status: 'Pending',
    subtotal: 500,
    shippingCost: 20,
    taxAmount: 79.99,
    discountAmount: 0,
    totalAmount: 599.99,
    currency: 'EUR',
    shippingAddress: { city: 'Sofia' },
    billingAddress: { city: 'Sofia' },
    shippingMethod: 'standard',
    trackingNumber: null,
    paymentMethod: null,
    paidAt: null,
    shippedAt: null,
    deliveredAt: null,
    cancelledAt: null,
    cancellationReason: null,
    notes: null,
    items: [],
    createdAt: '2026-06-15T00:00:00Z',
    updatedAt: '2026-06-15T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminOrdersService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminOrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests orders with default pagination params', () => {
    service.getOrders().subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.has('search')).toBeFalse();
    expect(req.request.params.has('status')).toBeFalse();
    req.flush(ordersList);
  });

  it('forwards search, status, and sort params when provided', () => {
    service.getOrders({
      pageNumber: 2,
      pageSize: 10,
      search: 'ORD-1001',
      status: 'Pending',
      sortBy: 'total',
      sortOrder: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('search')).toBe('ORD-1001');
    expect(req.request.params.get('status')).toBe('Pending');
    expect(req.request.params.get('sortBy')).toBe('total');
    expect(req.request.params.get('sortOrder')).toBe('asc');
    req.flush(ordersList);
  });

  it('fetches a single order by id', () => {
    service.getOrder('order-1').subscribe(result => {
      expect(result.orderNumber).toBe('ORD-1001');
    });

    const req = httpMock.expectOne(`${baseUrl}/order-1`);
    expect(req.request.method).toBe('GET');
    req.flush(orderDetail);
  });

  it('updates order status with the expected body', () => {
    service.updateStatus('order-1', {
      status: 'Paid',
      note: 'Confirmed payment',
      notifyCustomer: true
    }).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/order-1/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      status: 'Paid',
      note: 'Confirmed payment',
      notifyCustomer: true
    });
    req.flush(null);
  });

  it('updates shipping info with the expected body', () => {
    service.updateShipping('order-1', {
      trackingNumber: 'TRACK-123',
      shippingMethod: 'express',
      markAsShipped: true
    }).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/order-1/shipping`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      trackingNumber: 'TRACK-123',
      shippingMethod: 'express',
      markAsShipped: true
    });
    req.flush(null);
  });

  it('adds a note with the expected body', () => {
    service.addNote('order-1', 'Called the customer').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/order-1/notes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ note: 'Called the customer' });
    req.flush({ message: 'Note added' });
  });

  it('exposes the nine order statuses', () => {
    expect(ORDER_STATUSES.length).toBe(9);
    expect(ORDER_STATUSES).toContain('PaymentFailed');
  });

  it('returns valid next statuses for transitions', () => {
    expect(getValidNextStatuses('Pending')).toEqual(['Paid', 'Cancelled', 'PaymentFailed']);
    expect(getValidNextStatuses('Shipped')).toEqual(['Delivered', 'Returned']);
    expect(getValidNextStatuses('Cancelled')).toEqual([]);
    expect(getValidNextStatuses('Unknown')).toEqual([]);
  });
});
