import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * A single KPI metric with today/week/month rollups and a trend percentage.
 * Mirrors the backend KpiMetric record.
 */
export interface KpiMetric {
  today: number;
  thisWeek: number;
  thisMonth: number;
  trendPercentage: number;
}

/**
 * Top-level dashboard KPIs. Mirrors the backend DashboardKpiDto.
 */
export interface DashboardKpiDto {
  totalOrders: KpiMetric;
  revenue: KpiMetric;
  newCustomers: KpiMetric;
  averageOrderValue: KpiMetric;
  pendingOrders: number;
  lowStockCount: number;
}

/**
 * A single point on the revenue chart.
 */
export interface RevenueDataPoint {
  date: string;
  value: number;
  label: string;
}

/**
 * Revenue time series for a given period. Mirrors the backend RevenueChartDto.
 */
export interface RevenueChartDto {
  dataPoints: RevenueDataPoint[];
  period: string;
}

/**
 * Order-count breakdown by status. Mirrors the backend OrderStatusChartDto.
 */
export interface OrderStatusChartDto {
  pending: number;
  processing: number;
  shipped: number;
  delivered: number;
  cancelled: number;
}

/**
 * A compact recent-order row. Mirrors the backend RecentOrderDto.
 */
export interface RecentOrderDto {
  id: string;
  orderNumber: string;
  customerName: string;
  totalAmount: number;
  status: string;
  createdAt: string;
}

/**
 * A product that has fallen at or below its low-stock threshold.
 * Mirrors the backend LowStockProductDto.
 */
export interface LowStockProductDto {
  id: string;
  name: string;
  sku: string;
  currentStock: number;
  threshold: number;
  imageUrl: string | null;
}

/**
 * A best-selling product over a period. Mirrors the backend TopSellingProductDto.
 */
export interface TopSellingProductDto {
  id: string;
  name: string;
  sku: string;
  quantitySold: number;
  revenue: number;
  imageUrl: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin/dashboard`;

  getKpis(): Observable<DashboardKpiDto> {
    return this.http.get<DashboardKpiDto>(this.baseUrl);
  }

  getRevenueChart(period = '7d'): Observable<RevenueChartDto> {
    const params = new HttpParams().set('period', period);
    return this.http.get<RevenueChartDto>(`${this.baseUrl}/revenue`, { params });
  }

  getOrderStatusChart(): Observable<OrderStatusChartDto> {
    return this.http.get<OrderStatusChartDto>(`${this.baseUrl}/orders-chart`);
  }

  getRecentOrders(count = 10): Observable<RecentOrderDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<RecentOrderDto[]>(`${this.baseUrl}/recent-orders`, { params });
  }

  getLowStockProducts(count = 10): Observable<LowStockProductDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<LowStockProductDto[]>(`${this.baseUrl}/low-stock`, { params });
  }

  getTopSellingProducts(count = 10, period = '30d'): Observable<TopSellingProductDto[]> {
    const params = new HttpParams()
      .set('count', count.toString())
      .set('period', period);
    return this.http.get<TopSellingProductDto[]>(`${this.baseUrl}/top-products`, { params });
  }
}
