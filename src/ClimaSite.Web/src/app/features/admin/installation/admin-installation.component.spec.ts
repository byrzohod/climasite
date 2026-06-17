import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminInstallationComponent } from './admin-installation.component';
import { AdminInstallationRequestsList } from '../../../core/services/admin-installation.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AdminInstallationComponent', () => {
  let fixture: ComponentFixture<AdminInstallationComponent>;
  let component: AdminInstallationComponent;
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
      },
      {
        id: 'req-2',
        productName: 'CoolBreeze 9000',
        installationType: 'Standard',
        status: 'Confirmed',
        customerName: 'John Smith',
        customerEmail: 'john@test.com',
        customerPhone: '+359888999000',
        city: 'Plovdiv',
        country: 'Bulgaria',
        preferredDate: null,
        scheduledDate: null,
        estimatedPrice: 150,
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
        AdminInstallationComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminInstallationComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads and renders installation rows', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(list);
    fixture.detectChanges();

    expect(component.requests().length).toBe(2);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="installation-row"]');
    expect(rows.length).toBe(2);

    const page = fixture.nativeElement.querySelector('[data-testid="admin-installation-page"]');
    expect(page).toBeTruthy();

    const badges = fixture.nativeElement.querySelectorAll('[data-testid="installation-status-badge"]');
    expect(badges.length).toBe(2);
  });

  it('shows the error state with a retry button when loading fails', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne(r => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(component.error()).toBe('admin.installation.error');
    expect(fixture.nativeElement.querySelector('[data-testid="installation-error"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="installation-retry"]')).toBeTruthy();
  });

  it('shows the empty state when there are no requests', () => {
    fixture.detectChanges();

    httpMock.expectOne(r => r.url === baseUrl).flush({
      ...list,
      items: [],
      totalCount: 0,
      totalPages: 0
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="installation-empty"]')).toBeTruthy();
  });

  it('reloads with a status filter applied', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(list);

    component.applyStatusFilter('Confirmed');

    const req = httpMock.expectOne(r => r.url === baseUrl);
    expect(req.request.params.get('status')).toBe('Confirmed');
    expect(component.page()).toBe(1);
    req.flush(list);
  });

  it('applies a status change and refreshes the list', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(list);
    fixture.detectChanges();

    component.setTargetStatus('req-1', 'Confirmed');
    component.applyStatus(component.requests()[0]);

    const statusReq = httpMock.expectOne(`${baseUrl}/req-1/status`);
    expect(statusReq.request.method).toBe('PUT');
    expect(statusReq.request.body).toEqual({ status: 'Confirmed', scheduledDate: undefined });
    statusReq.flush({ success: true });

    // After success it refreshes the list.
    httpMock.expectOne(r => r.url === baseUrl).flush(list);

    expect(component.actionSuccess()).toBe('admin.installation.toasts.updated');
    expect(component.targetStatus('req-1')).toBe('');
  });

  it('blocks scheduling without a date and surfaces an error', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(list);
    fixture.detectChanges();

    component.setTargetStatus('req-1', 'Scheduled');
    component.applyStatus(component.requests()[0]);

    // No HTTP request is made because the date is missing.
    httpMock.expectNone(`${baseUrl}/req-1/status`);
    expect(component.actionError()).toBe('admin.installation.toasts.scheduledDateRequired');
  });

  it('sends an ISO scheduled date when scheduling with a date', () => {
    fixture.detectChanges();
    httpMock.expectOne(r => r.url === baseUrl).flush(list);
    fixture.detectChanges();

    component.setTargetStatus('req-1', 'Scheduled');
    component.setScheduledDate('req-1', '2026-08-01');
    component.applyStatus(component.requests()[0]);

    const statusReq = httpMock.expectOne(`${baseUrl}/req-1/status`);
    expect(statusReq.request.body.status).toBe('Scheduled');
    expect(statusReq.request.body.scheduledDate).toContain('2026-08-01');
    statusReq.flush({ success: true });

    httpMock.expectOne(r => r.url === baseUrl).flush(list);
  });
});
