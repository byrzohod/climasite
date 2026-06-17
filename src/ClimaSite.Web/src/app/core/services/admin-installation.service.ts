import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminInstallationRequest {
  id: string;
  productName: string;
  installationType: string;
  status: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  city: string;
  country: string;
  preferredDate: string | null;
  scheduledDate: string | null;
  estimatedPrice: number;
  createdAt: string;
}

export interface AdminInstallationRequestsList {
  items: AdminInstallationRequest[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminInstallationRequestsQuery {
  pageNumber?: number;
  pageSize?: number;
  status?: string;
}

export interface UpdateInstallationStatusPayload {
  status: string;
  scheduledDate?: string;
  finalPrice?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminInstallationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin/installation-requests`;

  getRequests(query: AdminInstallationRequestsQuery = {}): Observable<AdminInstallationRequestsList> {
    let params = new HttpParams()
      .set('pageNumber', (query.pageNumber ?? 1).toString())
      .set('pageSize', (query.pageSize ?? 20).toString());

    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http.get<AdminInstallationRequestsList>(this.baseUrl, { params });
  }

  updateStatus(id: string, payload: UpdateInstallationStatusPayload): Observable<{ success: boolean }> {
    return this.http.put<{ success: boolean }>(`${this.baseUrl}/${id}/status`, payload);
  }
}

/**
 * Target statuses an admin can transition an installation request into.
 * Mirrors the backend UpdateInstallationRequestStatusCommand switch (Pending is the initial
 * state only, so it is not an actionable target).
 */
export const INSTALLATION_STATUS_TARGETS: readonly string[] = [
  'Confirmed',
  'Scheduled',
  'InProgress',
  'Completed',
  'Cancelled'
] as const;

/** Status values surfaced in the list filter (includes Pending, which list rows may show). */
export const INSTALLATION_STATUS_FILTERS: readonly string[] = [
  'Pending',
  'Confirmed',
  'Scheduled',
  'InProgress',
  'Completed',
  'Cancelled'
] as const;
