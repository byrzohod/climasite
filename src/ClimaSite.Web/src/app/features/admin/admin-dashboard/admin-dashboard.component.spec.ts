import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AdminDashboardComponent } from './admin-dashboard.component';
import {
  DashboardKpiDto,
  OrderStatusChartDto,
  RecentOrderDto,
  LowStockProductDto,
  TopSellingProductDto
} from '../../../core/services/admin-dashboard.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AdminDashboardComponent', () => {
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let component: AdminDashboardComponent;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin/dashboard`;

  const kpis: DashboardKpiDto = {
    totalOrders: { today: 5, thisWeek: 20, thisMonth: 80, trendPercentage: 12.5 },
    revenue: { today: 1000, thisWeek: 5000, thisMonth: 20000, trendPercentage: -3.2 },
    newCustomers: { today: 2, thisWeek: 10, thisMonth: 40, trendPercentage: 0 },
    averageOrderValue: { today: 200, thisWeek: 250, thisMonth: 250, trendPercentage: 5 },
    pendingOrders: 7,
    lowStockCount: 3
  };

  const orderStatus: OrderStatusChartDto = {
    pending: 4,
    processing: 3,
    shipped: 6,
    delivered: 12,
    cancelled: 1
  };

  const recentOrders: RecentOrderDto[] = [
    {
      id: 'order-1',
      orderNumber: 'ORD-1001',
      customerName: 'Jane Doe',
      totalAmount: 599.99,
      status: 'Pending',
      createdAt: '2026-06-15T00:00:00Z'
    }
  ];

  const lowStock: LowStockProductDto[] = [
    {
      id: 'product-1',
      name: 'Test AC Unit',
      sku: 'AC-001',
      currentStock: 2,
      threshold: 10,
      imageUrl: null
    }
  ];

  const topProducts: TopSellingProductDto[] = [
    {
      id: 'product-2',
      name: 'Top Seller Heater',
      sku: 'HT-002',
      quantitySold: 42,
      revenue: 12000,
      imageUrl: null
    }
  ];

  /** Flush all five parallel dashboard requests with the given (optionally overridden) payloads. */
  function flushAll(overrides: {
    kpis?: DashboardKpiDto;
    orderStatus?: OrderStatusChartDto;
    recentOrders?: RecentOrderDto[];
    lowStock?: LowStockProductDto[];
    topProducts?: TopSellingProductDto[];
  } = {}): void {
    httpMock.expectOne(baseUrl).flush(overrides.kpis ?? kpis);
    httpMock.expectOne(r => r.url === `${baseUrl}/orders-chart`).flush(overrides.orderStatus ?? orderStatus);
    httpMock.expectOne(r => r.url === `${baseUrl}/recent-orders`).flush(overrides.recentOrders ?? recentOrders);
    httpMock.expectOne(r => r.url === `${baseUrl}/low-stock`).flush(overrides.lowStock ?? lowStock);
    httpMock.expectOne(r => r.url === `${baseUrl}/top-products`).flush(overrides.topProducts ?? topProducts);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AdminDashboardComponent,
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

    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders the dashboard container and keeps the existing nav tiles', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="admin-dashboard"]')).toBeTruthy();

    // Original navigation tiles must still be present.
    const navLinks = fixture.nativeElement.querySelectorAll('.admin-link');
    expect(navLinks.length).toBe(4);
  });

  it('shows the loading state before data resolves', () => {
    fixture.detectChanges();

    expect(component.loading()).toBeTrue();
    expect(fixture.nativeElement.querySelector('[data-testid="dashboard-loading"]')).toBeTruthy();

    flushAll();
    fixture.detectChanges();

    expect(component.loading()).toBeFalse();
    expect(fixture.nativeElement.querySelector('[data-testid="dashboard-loading"]')).toBeNull();
  });

  it('renders the KPI cards with month values and trend styling', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    const totalOrders = fixture.nativeElement.querySelector('[data-testid="kpi-total-orders"]');
    expect(totalOrders).toBeTruthy();
    expect(totalOrders.textContent).toContain('80');

    const revenue = fixture.nativeElement.querySelector('[data-testid="kpi-revenue"]');
    expect(revenue.textContent).toContain('20,000');

    expect(fixture.nativeElement.querySelector('[data-testid="kpi-new-customers"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="kpi-avg-order-value"]')).toBeTruthy();

    // Positive trend -> up, negative -> down, zero -> flat.
    expect(totalOrders.querySelector('.kpi-trend.trend-up')).toBeTruthy();
    expect(revenue.querySelector('.kpi-trend.trend-down')).toBeTruthy();
    const newCustomers = fixture.nativeElement.querySelector('[data-testid="kpi-new-customers"]');
    expect(newCustomers.querySelector('.kpi-trend.trend-flat')).toBeTruthy();
  });

  it('renders the pending and low-stock badges', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    const pending = fixture.nativeElement.querySelector('[data-testid="kpi-pending-orders"]');
    expect(pending.textContent).toContain('7');

    const lowStockBadge = fixture.nativeElement.querySelector('[data-testid="kpi-low-stock"]');
    expect(lowStockBadge.textContent).toContain('3');
  });

  it('renders recent-orders, low-stock and top-product lists', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    const recent = fixture.nativeElement.querySelector('[data-testid="dashboard-recent-orders"]');
    expect(recent.textContent).toContain('ORD-1001');

    const low = fixture.nativeElement.querySelector('[data-testid="dashboard-low-stock"]');
    expect(low.textContent).toContain('AC-001');

    const top = fixture.nativeElement.querySelector('[data-testid="dashboard-top-products"]');
    expect(top.textContent).toContain('HT-002');
  });

  it('links each recent order to its admin detail page', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="recent-order-link"]');
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain('/admin/orders/order-1');
  });

  it('scales order-status bars relative to the largest count', () => {
    fixture.detectChanges();
    flushAll();
    fixture.detectChanges();

    expect(component.orderStatusBars().length).toBe(5);
    // delivered (12) is the max -> 100%.
    expect(component.barWidth(12)).toBe(100);
    // cancelled (1) -> small but visible (>= 2%).
    expect(component.barWidth(1)).toBeGreaterThanOrEqual(2);
    // zero -> no fill.
    expect(component.barWidth(0)).toBe(0);
  });

  it('shows the error state with a retry button when loading fails', () => {
    fixture.detectChanges();

    // First request errors; forkJoin fails fast, so flushing one is enough.
    httpMock.expectOne(baseUrl).flush('boom', { status: 500, statusText: 'Server Error' });
    // Drain any other in-flight requests so verify() stays clean.
    httpMock.match(() => true).forEach(r => {
      if (!r.cancelled) {
        r.flush(null, { status: 500, statusText: 'Server Error' });
      }
    });
    fixture.detectChanges();

    expect(component.error()).toBe('admin.dashboard.error');
    expect(fixture.nativeElement.querySelector('[data-testid="dashboard-error"]')).toBeTruthy();

    const retry = fixture.nativeElement.querySelector('[data-testid="dashboard-retry"]');
    expect(retry).toBeTruthy();
  });

  it('retries the load when the retry button is clicked', () => {
    fixture.detectChanges();

    httpMock.expectOne(baseUrl).flush('boom', { status: 500, statusText: 'Server Error' });
    httpMock.match(() => true).forEach(r => {
      if (!r.cancelled) {
        r.flush(null, { status: 500, statusText: 'Server Error' });
      }
    });
    fixture.detectChanges();

    component.loadDashboard();
    fixture.detectChanges();

    expect(component.loading()).toBeTrue();
    flushAll();
    fixture.detectChanges();

    expect(component.error()).toBeNull();
    expect(component.kpis()).not.toBeNull();
  });
});
