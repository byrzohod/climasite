import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import {
  AdminDashboardService,
  DashboardKpiDto,
  RevenueChartDto,
  OrderStatusChartDto,
  RecentOrderDto,
  LowStockProductDto,
  TopSellingProductDto
} from './admin-dashboard.service';

describe('AdminDashboardService', () => {
  let service: AdminDashboardService;
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

  const revenueChart: RevenueChartDto = {
    dataPoints: [
      { date: '2026-06-10', value: 1200, label: 'Jun 10' },
      { date: '2026-06-11', value: 1800, label: 'Jun 11' }
    ],
    period: '7d'
  };

  const orderStatusChart: OrderStatusChartDto = {
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminDashboardService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminDashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches dashboard KPIs from the base endpoint', () => {
    service.getKpis().subscribe(result => {
      expect(result.pendingOrders).toBe(7);
      expect(result.totalOrders.thisMonth).toBe(80);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    req.flush(kpis);
  });

  it('requests the revenue chart with the default period', () => {
    service.getRevenueChart().subscribe(result => {
      expect(result.dataPoints.length).toBe(2);
    });

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/revenue`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('period')).toBe('7d');
    req.flush(revenueChart);
  });

  it('forwards a custom revenue chart period', () => {
    service.getRevenueChart('30d').subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/revenue`);
    expect(req.request.params.get('period')).toBe('30d');
    req.flush(revenueChart);
  });

  it('fetches the order-status chart', () => {
    service.getOrderStatusChart().subscribe(result => {
      expect(result.delivered).toBe(12);
    });

    const req = httpMock.expectOne(`${baseUrl}/orders-chart`);
    expect(req.request.method).toBe('GET');
    req.flush(orderStatusChart);
  });

  it('requests recent orders with the default count', () => {
    service.getRecentOrders().subscribe(result => {
      expect(result[0].orderNumber).toBe('ORD-1001');
    });

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent-orders`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('count')).toBe('10');
    req.flush(recentOrders);
  });

  it('forwards a custom recent-orders count', () => {
    service.getRecentOrders(5).subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent-orders`);
    expect(req.request.params.get('count')).toBe('5');
    req.flush(recentOrders);
  });

  it('requests low-stock products with the default count', () => {
    service.getLowStockProducts().subscribe(result => {
      expect(result[0].sku).toBe('AC-001');
    });

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/low-stock`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('count')).toBe('10');
    req.flush(lowStock);
  });

  it('requests top-selling products with default count and period', () => {
    service.getTopSellingProducts().subscribe(result => {
      expect(result[0].quantitySold).toBe(42);
    });

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/top-products`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('count')).toBe('10');
    expect(req.request.params.get('period')).toBe('30d');
    req.flush(topProducts);
  });

  it('forwards custom top-product count and period', () => {
    service.getTopSellingProducts(5, '7d').subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/top-products`);
    expect(req.request.params.get('count')).toBe('5');
    expect(req.request.params.get('period')).toBe('7d');
    req.flush(topProducts);
  });
});
