import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, catchError } from 'rxjs';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * A single in-app notification, mirroring the API's NotificationDto.
 */
export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  message: string;
  link?: string | null;
  data: Record<string, unknown>;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

/**
 * Paged notifications list, mirroring the API's NotificationsListDto.
 */
export interface NotificationsList {
  items: NotificationItem[];
  totalCount: number;
  unreadCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Summary payload (unread count + recent items), mirroring the API's NotificationSummaryDto.
 */
export interface NotificationSummary {
  totalCount: number;
  unreadCount: number;
  recentItems: NotificationItem[];
}

export interface NotificationQuery {
  pageNumber?: number;
  pageSize?: number;
  isRead?: boolean;
  type?: string;
}

/**
 * GAP-09: Consumes the in-app notifications API (`/api/notifications`).
 *
 * Exposes `unreadCount` and `recent` signals for the header bell, and thin wrappers around the
 * list/mark-read/mark-all/delete endpoints. Summary is refreshed after any mutation so the badge
 * stays in sync. No aggressive polling — callers load the summary on demand (header init / route
 * changes).
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/notifications`;

  private readonly _unreadCount = signal(0);
  private readonly _recent = signal<NotificationItem[]>([]);

  /** Unread notification count for the header badge. */
  readonly unreadCount = this._unreadCount.asReadonly();
  /** Most-recent notifications for the header dropdown. */
  readonly recent = this._recent.asReadonly();

  /**
   * Fetch the summary (unread count + recent items) and update the exposed signals.
   * Failures are swallowed (e.g. unauthenticated / offline) so the header never breaks.
   */
  loadSummary(recentCount = 5): Observable<NotificationSummary | null> {
    return this.http
      .get<NotificationSummary>(`${this.apiUrl}/summary`, { params: { recentCount } })
      .pipe(
        tap(summary => this.applySummary(summary)),
        catchError(() => of(null))
      );
  }

  /** Fetch a paged list of notifications. */
  getNotifications(query: NotificationQuery = {}): Observable<NotificationsList> {
    const params: Record<string, string> = {};
    if (query.pageNumber != null) params['pageNumber'] = String(query.pageNumber);
    if (query.pageSize != null) params['pageSize'] = String(query.pageSize);
    if (query.isRead != null) params['isRead'] = String(query.isRead);
    if (query.type) params['type'] = query.type;
    return this.http.get<NotificationsList>(this.apiUrl, { params });
  }

  /** Mark a single notification as read, then refresh the summary. */
  markAsRead(id: string): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => this.loadSummary().subscribe())
    );
  }

  /** Mark all notifications as read, then refresh the summary. */
  markAllAsRead(): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => this.loadSummary().subscribe())
    );
  }

  /** Delete a notification, then refresh the summary. */
  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.loadSummary().subscribe())
    );
  }

  private applySummary(summary: NotificationSummary): void {
    this._unreadCount.set(summary?.unreadCount ?? 0);
    this._recent.set(summary?.recentItems ?? []);
  }
}
