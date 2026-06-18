import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, input } from '@angular/core';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { NotificationsComponent } from './notifications.component';
import {
  NotificationService,
  NotificationItem,
  NotificationsList
} from '../../../core/services/notification.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state';

// Mock EmptyStateComponent to avoid Lucide icon registration in unit tests.
@Component({
  selector: 'app-empty-state',
  template: '<div class="empty-state" data-testid="mock-empty-state"></div>',
  standalone: true
})
class MockEmptyStateComponent {
  readonly variant = input<string>('generic');
  readonly title = input<string>('');
  readonly description = input<string>('');
  readonly actionLabel = input<string>('');
  readonly actionRoute = input<string>('');
  readonly actionTestId = input<string>('empty-state-action');
  readonly showIcon = input<boolean>(true);
}

describe('NotificationsComponent', () => {
  let component: NotificationsComponent;
  let fixture: ComponentFixture<NotificationsComponent>;
  let serviceMock: jasmine.SpyObj<NotificationService>;

  const recentIso = new Date().toISOString();

  const items: NotificationItem[] = [
    {
      id: 'n1',
      type: 'orderPlaced',
      title: 'Order placed',
      message: 'Your order CLM-1 was placed',
      link: '/account/orders/1',
      data: {},
      isRead: false,
      createdAt: recentIso
    },
    {
      id: 'n2',
      type: 'orderShipped',
      title: 'Order shipped',
      message: 'Your order CLM-1 has shipped',
      link: null,
      data: {},
      isRead: true,
      createdAt: recentIso
    }
  ];

  const list: NotificationsList = {
    items,
    totalCount: 2,
    unreadCount: 1,
    pageNumber: 1,
    pageSize: 50
  };

  async function setup(): Promise<void> {
    serviceMock = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'getNotifications',
      'markAsRead',
      'markAllAsRead'
    ]);
    serviceMock.getNotifications.and.returnValue(of(list));
    serviceMock.markAsRead.and.returnValue(of({}));
    serviceMock.markAllAsRead.and.returnValue(of({}));

    await TestBed.configureTestingModule({
      imports: [NotificationsComponent, RouterTestingModule, TranslateModule.forRoot()],
      providers: [{ provide: NotificationService, useValue: serviceMock }]
    })
      .overrideComponent(NotificationsComponent, {
        remove: { imports: [EmptyStateComponent] },
        add: { imports: [MockEmptyStateComponent] }
      })
      .compileComponents();

    fixture = TestBed.createComponent(NotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await setup();
  });

  it('should create and load notifications on init', () => {
    expect(component).toBeTruthy();
    expect(serviceMock.getNotifications).toHaveBeenCalledWith({ pageNumber: 1, pageSize: 50 });
    expect(component.notifications().length).toBe(2);
    expect(component.unreadCount()).toBe(1);
  });

  it('should render a row per notification', () => {
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="notification-row"]');
    expect(rows.length).toBe(2);
  });

  it('should mark a single notification as read and decrement unread count', () => {
    component.markRead(items[0]);
    expect(serviceMock.markAsRead).toHaveBeenCalledWith('n1');
    expect(component.notifications().find(n => n.id === 'n1')?.isRead).toBeTrue();
    expect(component.unreadCount()).toBe(0);
  });

  it('should not call markAsRead for an already-read notification', () => {
    component.markRead(items[1]);
    expect(serviceMock.markAsRead).not.toHaveBeenCalled();
  });

  it('should mark all as read', () => {
    component.markAllRead();
    expect(serviceMock.markAllAsRead).toHaveBeenCalled();
    expect(component.notifications().every(n => n.isRead)).toBeTrue();
    expect(component.unreadCount()).toBe(0);
  });

  it('should mark unread notification read when viewed', () => {
    component.onView(items[0]);
    expect(serviceMock.markAsRead).toHaveBeenCalledWith('n1');
  });

  it('should show empty state when there are no notifications', async () => {
    serviceMock.getNotifications.and.returnValue(
      of({ items: [], totalCount: 0, unreadCount: 0, pageNumber: 1, pageSize: 50 })
    );
    component.load();
    fixture.detectChanges();

    expect(component.notifications().length).toBe(0);
    const empty = fixture.nativeElement.querySelector('[data-testid="notifications-empty"]');
    expect(empty).toBeTruthy();
  });

  it('should show error state when loading fails', () => {
    serviceMock.getNotifications.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    fixture.detectChanges();

    expect(component.error()).toBeTrue();
    expect(component.loading()).toBeFalse();
    const errorEl = fixture.nativeElement.querySelector('[data-testid="notifications-error"]');
    expect(errorEl).toBeTruthy();
  });

  it('relativeTime returns a localized just-now for invalid dates', () => {
    expect(component.relativeTime('not-a-date')).toBe('notifications.time.justNow');
  });
});
