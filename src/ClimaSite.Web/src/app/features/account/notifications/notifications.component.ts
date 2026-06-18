import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  NotificationService,
  NotificationItem
} from '../../../core/services/notification.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state';

/**
 * GAP-09: Full notifications list page at `/account/notifications`.
 *
 * Closes the "View all" loop from the header dropdown. Lists every notification via
 * `NotificationService.getNotifications()`, supports per-item and bulk mark-as-read, and
 * lets the user navigate to a notification's target link. Mirrors the other account
 * sub-pages (CSS variables, data-testids, i18n) and reuses the header's relative-time /
 * type-label conventions.
 */
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, EmptyStateComponent],
  template: `
    <div class="notifications-container" data-testid="notifications-page">
      <div class="notifications-header">
        <h1>{{ 'notifications.title' | translate }}</h1>
        @if (unreadCount() > 0) {
          <button
            type="button"
            class="mark-all-btn"
            (click)="markAllRead()"
            data-testid="notifications-mark-all"
          >
            {{ 'notifications.markAllRead' | translate }}
          </button>
        }
      </div>

      @if (loading()) {
        <div class="loading-state" data-testid="notifications-loading">
          <div class="loading-spinner"></div>
          <p>{{ 'common.loading' | translate }}</p>
        </div>
      } @else if (error()) {
        <div class="error-state" data-testid="notifications-error">
          <p>{{ 'notifications.loadError' | translate }}</p>
          <button type="button" class="retry-btn" (click)="load()" data-testid="notifications-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      } @else if (notifications().length === 0) {
        <app-empty-state
          variant="generic"
          [title]="'notifications.empty' | translate"
          data-testid="notifications-empty"
        />
      } @else {
        <ul class="notifications-list" data-testid="notifications-list">
          @for (notification of notifications(); track notification.id) {
            <li
              class="notification-row"
              [class.notification-row--unread]="!notification.isRead"
              data-testid="notification-row"
            >
              <div class="notification-row__main">
                @if (!notification.isRead) {
                  <span class="notification-row__dot" aria-hidden="true"></span>
                }
                <div class="notification-row__body">
                  <p class="notification-row__title">{{ notification.title }}</p>
                  <p class="notification-row__message">{{ notification.message }}</p>
                  <span class="notification-row__time">{{ relativeTime(notification.createdAt) }}</span>
                </div>
              </div>
              <div class="notification-row__actions">
                @if (notification.link) {
                  <a
                    [routerLink]="notification.link"
                    class="notification-row__link"
                    (click)="onView(notification)"
                    data-testid="notification-view"
                  >
                    {{ 'notifications.view' | translate }}
                  </a>
                }
                @if (!notification.isRead) {
                  <button
                    type="button"
                    class="notification-row__read"
                    (click)="markRead(notification)"
                    data-testid="notification-mark-read"
                  >
                    {{ 'notifications.markRead' | translate }}
                  </button>
                }
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .notifications-container {
      max-width: 720px;
      margin: 0 auto;
      padding: 2rem 1rem;
    }

    .notifications-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;

      h1 {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0;
      }
    }

    .mark-all-btn {
      background: transparent;
      border: 1px solid var(--color-border);
      color: var(--color-primary);
      border-radius: 6px;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s ease, border-color 0.2s ease;

      &:hover {
        background: var(--color-bg-tertiary);
        border-color: var(--color-primary);
      }
    }

    .loading-state,
    .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      min-height: 200px;
      text-align: center;
      color: var(--color-text-secondary);
    }

    .loading-spinner {
      width: 36px;
      height: 36px;
      border: 4px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .retry-btn {
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: 6px;
      padding: 0.5rem 1rem;
      font-weight: 600;
      cursor: pointer;
    }

    .notifications-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .notification-row {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 10px;
    }

    .notification-row--unread {
      border-color: var(--color-primary);
      background: var(--color-bg-primary);
    }

    .notification-row__main {
      display: flex;
      align-items: flex-start;
      gap: 0.625rem;
      flex: 1;
      min-width: 0;
    }

    .notification-row__dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--color-primary);
      margin-top: 0.4rem;
      flex-shrink: 0;
    }

    .notification-row__body {
      min-width: 0;
    }

    .notification-row__title {
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0 0 0.25rem;
    }

    .notification-row__message {
      color: var(--color-text-secondary);
      font-size: 0.9375rem;
      margin: 0 0 0.25rem;
    }

    .notification-row__time {
      color: var(--color-text-tertiary);
      font-size: 0.8125rem;
    }

    .notification-row__actions {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 0.5rem;
      flex-shrink: 0;
    }

    .notification-row__link {
      color: var(--color-primary);
      font-size: 0.875rem;
      font-weight: 600;
      text-decoration: none;

      &:hover {
        text-decoration: underline;
      }
    }

    .notification-row__read {
      background: transparent;
      border: none;
      color: var(--color-text-secondary);
      font-size: 0.8125rem;
      cursor: pointer;
      padding: 0;

      &:hover {
        color: var(--color-primary);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .loading-spinner {
        animation: none;
      }
    }

    @media (max-width: 600px) {
      .notification-row {
        flex-direction: column;
      }

      .notification-row__actions {
        flex-direction: row;
        align-items: center;
      }
    }
  `]
})
export class NotificationsComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly translate = inject(TranslateService);

  readonly notifications = signal<NotificationItem[]>([]);
  readonly unreadCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit(): void {
    this.load();
  }

  /** Load the full notifications list (most recent first, large page so all are shown). */
  load(): void {
    this.loading.set(true);
    this.error.set(false);

    this.notificationService.getNotifications({ pageNumber: 1, pageSize: 50 }).subscribe({
      next: list => {
        this.notifications.set(list.items ?? []);
        this.unreadCount.set(list.unreadCount ?? 0);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }

  /** Mark a single notification as read and reflect it locally. */
  markRead(notification: NotificationItem): void {
    if (notification.isRead) {
      return;
    }
    this.notificationService.markAsRead(notification.id).subscribe({
      next: () => this.applyRead(notification.id)
    });
  }

  /** Mark every notification as read. */
  markAllRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.update(items => items.map(item => ({ ...item, isRead: true })));
        this.unreadCount.set(0);
      }
    });
  }

  /** Follow a notification's link; mark it read on the way out. */
  onView(notification: NotificationItem): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe({
        next: () => this.applyRead(notification.id)
      });
    }
  }

  private applyRead(id: string): void {
    this.notifications.update(items =>
      items.map(item => (item.id === id ? { ...item, isRead: true } : item))
    );
    this.unreadCount.update(count => Math.max(0, count - 1));
  }

  /**
   * Coarse relative time ("just now", "5m", "3h", "2d"), mirroring the header dropdown,
   * with a localized "just now" fallback for invalid/future timestamps.
   */
  relativeTime(isoDate: string): string {
    const then = new Date(isoDate).getTime();
    if (Number.isNaN(then)) {
      return this.translate.instant('notifications.time.justNow');
    }

    const diffSeconds = Math.floor((Date.now() - then) / 1000);
    if (diffSeconds < 60) {
      return this.translate.instant('notifications.time.justNow');
    }

    const minutes = Math.floor(diffSeconds / 60);
    if (minutes < 60) {
      return this.translate.instant('notifications.time.minutes', { count: minutes });
    }

    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return this.translate.instant('notifications.time.hours', { count: hours });
    }

    const days = Math.floor(hours / 24);
    return this.translate.instant('notifications.time.days', { count: days });
  }
}
