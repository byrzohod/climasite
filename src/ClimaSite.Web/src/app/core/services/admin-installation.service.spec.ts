import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import {
  AdminInstallationService,
  AdminInstallationRequestsList
} from './admin-installation.service';

describe('AdminInstallationService', () => {
  let service: AdminInstallationService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/installation-requests`;

  const list: AdminInstallationRequestsList = {
    items: [
      {
        id: 'req-1',
        productName: 'DualZone Pro 12000',
        installationType: 'Premium',
        status: 'Pending',
        customerName: 'Jane Buyer',
        customerEmail: 'jane@test.com',
        customerPhone: '+359888123456',
        city: 'Sofia',
        country: 'Bulgaria',
        preferredDate: '2026-07-01T00:00:00Z',
        scheduledDate: null,
        estimatedPrice: 250,
        createdAt: '2026-06-15T00:00:00Z'
      }
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminInstallationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminInstallationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests installation requests with default pagination params', () => {
    service.getRequests().subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.has('status')).toBeFalse();
    req.flush(list);
  });

  it('forwards the status filter and pagination when provided', () => {
    service.getRequests({ pageNumber: 2, pageSize: 10, status: 'Confirmed' }).subscribe();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('status')).toBe('Confirmed');
    req.flush(list);
  });

  it('updates status with the expected verb and body', () => {
    service.updateStatus('req-1', { status: 'Confirmed' }).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/req-1/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ status: 'Confirmed' });
    req.flush({ success: true });
  });

  it('sends a scheduled date when scheduling', () => {
    service.updateStatus('req-2', { status: 'Scheduled', scheduledDate: '2026-08-01T00:00:00.000Z' }).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/req-2/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ status: 'Scheduled', scheduledDate: '2026-08-01T00:00:00.000Z' });
    req.flush({ success: true });
  });
});
