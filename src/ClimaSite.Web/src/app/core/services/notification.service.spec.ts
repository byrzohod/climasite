import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { NotificationService, NotificationSummary, NotificationsList } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/notifications`;

  const summary: NotificationSummary = {
    totalCount: 3,
    unreadCount: 2,
    recentItems: [
      {
        id: 'n-1',
        type: 'order_shipped',
        title: 'Order shipped',
        message: 'Your order ORD-1 has shipped.',
        link: '/account/orders/order-1',
        data: {},
        isRead: false,
        readAt: null,
        createdAt: '2026-06-16T00:00:00Z'
      },
      {
        id: 'n-2',
        type: 'payment_received',
        title: 'Payment received',
        message: 'We received payment for ORD-1.',
        link: '/account/orders/order-1',
        data: {},
        isRead: true,
        readAt: '2026-06-16T01:00:00Z',
        createdAt: '2026-06-16T00:30:00Z'
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        NotificationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('loadSummary fetches /summary and updates signals', () => {
    service.loadSummary().subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/summary`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('recentCount')).toBe('5');
    req.flush(summary);

    expect(service.unreadCount()).toBe(2);
    expect(service.recent().length).toBe(2);
    expect(service.recent()[0].title).toBe('Order shipped');
  });

  it('loadSummary swallows errors and leaves signals at default', () => {
    let result: NotificationSummary | null | undefined;
    service.loadSummary().subscribe(r => (result = r));

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/summary`);
    req.flush('boom', { status: 500, statusText: 'Server Error' });

    expect(result).toBeNull();
    expect(service.unreadCount()).toBe(0);
    expect(service.recent().length).toBe(0);
  });

  it('getNotifications passes query params', () => {
    const list: NotificationsList = {
      items: [],
      totalCount: 0,
      unreadCount: 0,
      pageNumber: 2,
      pageSize: 10
    };
    service.getNotifications({ pageNumber: 2, pageSize: 10, isRead: false }).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('isRead')).toBe('false');
    req.flush(list);
  });

  it('markAsRead PUTs /{id}/read and refreshes the summary', () => {
    service.markAsRead('n-1').subscribe();

    const readReq = httpMock.expectOne(`${baseUrl}/n-1/read`);
    expect(readReq.request.method).toBe('PUT');
    readReq.flush({});

    // After mark-read, a fresh summary is loaded.
    const summaryReq = httpMock.expectOne(r => r.url === `${baseUrl}/summary`);
    summaryReq.flush({ ...summary, unreadCount: 1 });
    expect(service.unreadCount()).toBe(1);
  });

  it('markAllAsRead PUTs /read-all and refreshes the summary (unread -> 0)', () => {
    service.markAllAsRead().subscribe();

    const allReq = httpMock.expectOne(`${baseUrl}/read-all`);
    expect(allReq.request.method).toBe('PUT');
    allReq.flush({ markedCount: 2 });

    const summaryReq = httpMock.expectOne(r => r.url === `${baseUrl}/summary`);
    summaryReq.flush({ ...summary, unreadCount: 0 });
    expect(service.unreadCount()).toBe(0);
  });

  it('delete DELETEs /{id} and refreshes the summary', () => {
    service.delete('n-1').subscribe();

    const delReq = httpMock.expectOne(`${baseUrl}/n-1`);
    expect(delReq.request.method).toBe('DELETE');
    delReq.flush({});

    const summaryReq = httpMock.expectOne(r => r.url === `${baseUrl}/summary`);
    summaryReq.flush({ ...summary, totalCount: 2 });
  });
});
