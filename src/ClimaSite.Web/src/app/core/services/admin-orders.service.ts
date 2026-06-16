import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminOrderListItem {
  id: string;
  orderNumber: string;
  customerName: string;
  customerEmail: string;
  totalAmount: number;
  status: string;
  paymentStatus: string | null;
  itemCount: number;
  createdAt: string;
}

export interface AdminOrdersList {
  items: AdminOrderListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminOrderItem {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  imageUrl: string | null;
}

export interface AdminOrderDetail {
  id: string;
  orderNumber: string;
  userId: string | null;
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  status: string;
  subtotal: number;
  shippingCost: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  currency: string;
  shippingAddress: Record<string, unknown> | null;
  billingAddress: Record<string, unknown> | null;
  shippingMethod: string | null;
  trackingNumber: string | null;
  paymentMethod: string | null;
  paidAt: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  notes: string | null;
  items: AdminOrderItem[];
  createdAt: string;
  updatedAt: string;
}

export interface AdminOrdersQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortOrder?: string;
}

export interface UpdateOrderStatusRequest {
  status: string;
  note?: string;
  notifyCustomer: boolean;
}

export interface UpdateShippingRequest {
  trackingNumber?: string;
  shippingMethod?: string;
  markAsShipped: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminOrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin/orders`;

  getOrders(query: AdminOrdersQuery = {}): Observable<AdminOrdersList> {
    let params = new HttpParams()
      .set('pageNumber', (query.pageNumber ?? 1).toString())
      .set('pageSize', (query.pageSize ?? 20).toString());

    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.dateFrom) {
      params = params.set('dateFrom', query.dateFrom);
    }
    if (query.dateTo) {
      params = params.set('dateTo', query.dateTo);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortOrder) {
      params = params.set('sortOrder', query.sortOrder);
    }

    return this.http.get<AdminOrdersList>(this.baseUrl, { params });
  }

  getOrder(id: string): Observable<AdminOrderDetail> {
    return this.http.get<AdminOrderDetail>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: string, request: UpdateOrderStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, request);
  }

  updateShipping(id: string, request: UpdateShippingRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/shipping`, request);
  }

  addNote(id: string, note: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/notes`, { note });
  }
}

/**
 * The nine order statuses, mirrored from the backend OrderStatus enum.
 */
export const ORDER_STATUSES: readonly string[] = [
  'Pending',
  'Paid',
  'Processing',
  'Shipped',
  'Delivered',
  'Cancelled',
  'Refunded',
  'Returned',
  'PaymentFailed'
] as const;

/**
 * Valid status transitions, mirrored from the backend state machine.
 * Maps each current status to the set of statuses it may transition to.
 */
export const ORDER_STATUS_TRANSITIONS: Readonly<Record<string, readonly string[]>> = {
  Pending: ['Paid', 'Cancelled', 'PaymentFailed'],
  Paid: ['Processing', 'Refunded', 'Cancelled'],
  Processing: ['Shipped', 'Refunded', 'Cancelled'],
  Shipped: ['Delivered', 'Returned'],
  Delivered: ['Returned'],
  Returned: ['Refunded'],
  PaymentFailed: ['Paid', 'Cancelled'],
  Cancelled: [],
  Refunded: []
};

export function getValidNextStatuses(current: string): readonly string[] {
  return ORDER_STATUS_TRANSITIONS[current] ?? [];
}
