import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminCustomerListItem {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  orderCount: number;
  totalSpent: number;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface AdminCustomersList {
  items: AdminCustomerListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface CustomerAddress {
  id: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
}

export interface CustomerStats {
  totalOrders: number;
  totalSpent: number;
  averageOrderValue: number;
  reviewsWritten: number;
  wishlistItems: number;
}

export interface CustomerOrderSummary {
  id: string;
  orderNumber: string;
  total: number;
  status: string;
  createdAt: string;
}

export interface AdminCustomerDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  preferredLanguage: string;
  preferredCurrency: string;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string;
  addresses: CustomerAddress[];
  stats: CustomerStats;
  recentOrders: CustomerOrderSummary[];
}

export interface AdminCustomersQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  registeredFrom?: string;
  registeredTo?: string;
  sortBy?: string;
  sortOrder?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminCustomersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin/customers`;

  getCustomers(query: AdminCustomersQuery = {}): Observable<AdminCustomersList> {
    let params = new HttpParams()
      .set('pageNumber', (query.pageNumber ?? 1).toString())
      .set('pageSize', (query.pageSize ?? 20).toString());

    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.registeredFrom) {
      params = params.set('registeredFrom', query.registeredFrom);
    }
    if (query.registeredTo) {
      params = params.set('registeredTo', query.registeredTo);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortOrder) {
      params = params.set('sortOrder', query.sortOrder);
    }

    return this.http.get<AdminCustomersList>(this.baseUrl, { params });
  }

  getCustomer(id: string): Observable<AdminCustomerDetail> {
    return this.http.get<AdminCustomerDetail>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}

/**
 * Customer status filter values, mirrored from the backend GetAdminCustomersQuery switch.
 * Only "all" (empty), "active", and "inactive" are surfaced in the customers UI.
 */
export const CUSTOMER_STATUS_FILTERS: readonly string[] = ['active', 'inactive'] as const;
