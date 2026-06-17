import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HeaderComponent } from './header.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { PLATFORM_ID, signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../auth/services/auth.service';
import { NotificationService, NotificationItem } from '../../services/notification.service';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [
        HeaderComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' },
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have header element', () => {
    const header = fixture.nativeElement.querySelector('[data-testid="header"]');
    expect(header).toBeTruthy();
  });

  it('should have logo', () => {
    const logo = fixture.nativeElement.querySelector('[data-testid="header-logo"]');
    expect(logo).toBeTruthy();
  });

  it('should have navigation', () => {
    const nav = fixture.nativeElement.querySelector('[data-testid="header-nav"]');
    expect(nav).toBeTruthy();
  });

  it('should have cart button', () => {
    const cart = fixture.nativeElement.querySelector('[data-testid="cart-icon"]');
    expect(cart).toBeTruthy();
  });

  it('should have theme toggle', () => {
    const themeToggle = fixture.nativeElement.querySelector('[data-testid="theme-toggle"]');
    expect(themeToggle).toBeTruthy();
  });

  it('should have language selector', () => {
    const langSelector = fixture.nativeElement.querySelector('[data-testid="language-selector"]');
    expect(langSelector).toBeTruthy();
  });

  it('should have mobile menu toggle', () => {
    const mobileToggle = fixture.nativeElement.querySelector('[data-testid="mobile-menu-toggle"]');
    expect(mobileToggle).toBeTruthy();
  });

  it('should not render the notification bell when unauthenticated', () => {
    const bell = fixture.nativeElement.querySelector('[data-testid="notification-bell"]');
    expect(bell).toBeFalsy();
  });
});

describe('HeaderComponent — notification bell (authenticated)', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;

  const unreadCount = signal(0);
  const recent = signal<NotificationItem[]>([]);

  const sampleNotifications: NotificationItem[] = [
    {
      id: 'n-1',
      type: 'order_shipped',
      title: 'Order shipped',
      message: 'Your order ORD-1 has shipped.',
      link: '/account/orders/order-1',
      data: {},
      isRead: false,
      readAt: null,
      createdAt: new Date().toISOString()
    }
  ];

  const notificationServiceStub: Partial<NotificationService> = {
    unreadCount: unreadCount.asReadonly(),
    recent: recent.asReadonly(),
    loadSummary: jasmine.createSpy('loadSummary').and.returnValue(of(null)),
    markAllAsRead: jasmine.createSpy('markAllAsRead').and.returnValue(of({})),
    markAsRead: jasmine.createSpy('markAsRead').and.returnValue(of({}))
  };

  const authServiceStub: Partial<AuthService> = {
    isAuthenticated: signal(true).asReadonly(),
    isAdmin: signal(false).asReadonly(),
    isLoading: signal(false).asReadonly(),
    user: signal({
      id: 'user-1',
      email: 'buyer@test.com',
      firstName: 'Jane',
      lastName: 'Doe',
      role: 'Customer'
    } as never).asReadonly()
  };

  beforeEach(async () => {
    localStorage.clear();
    unreadCount.set(0);
    recent.set([]);

    await TestBed.configureTestingModule({
      imports: [
        HeaderComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' },
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceStub },
        { provide: NotificationService, useValue: notificationServiceStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('renders the bell for authenticated users', () => {
    const bell = fixture.nativeElement.querySelector('[data-testid="notification-bell"]');
    expect(bell).toBeTruthy();
  });

  it('shows the badge with the unread count when there are unread notifications', () => {
    unreadCount.set(3);
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="notification-badge"]');
    expect(badge).toBeTruthy();
    expect(badge.textContent.trim()).toBe('3');
  });

  it('hides the badge when there are no unread notifications', () => {
    unreadCount.set(0);
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="notification-badge"]');
    expect(badge).toBeFalsy();
  });

  it('opens the dropdown and lists recent notifications', () => {
    recent.set(sampleNotifications);
    fixture.detectChanges();

    component.toggleNotificationMenu();
    fixture.detectChanges();

    const dropdown = fixture.nativeElement.querySelector('[data-testid="notification-dropdown"]');
    expect(dropdown).toBeTruthy();
    const items = fixture.nativeElement.querySelectorAll('[data-testid="notification-item"]');
    expect(items.length).toBe(1);
    expect(items[0].textContent).toContain('Order shipped');
  });

  it('shows the empty state when there are no recent notifications', () => {
    recent.set([]);
    fixture.detectChanges();

    component.toggleNotificationMenu();
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('[data-testid="notification-empty"]');
    expect(empty).toBeTruthy();
  });

  it('mark-all calls the service and the badge clears when the count goes to zero', () => {
    unreadCount.set(2);
    fixture.detectChanges();

    component.toggleNotificationMenu();
    fixture.detectChanges();

    const markAll = fixture.nativeElement.querySelector('[data-testid="notification-mark-all"]');
    expect(markAll).toBeTruthy();
    markAll.click();

    expect(notificationServiceStub.markAllAsRead).toHaveBeenCalled();

    // Simulate the summary refresh result clearing the unread count.
    unreadCount.set(0);
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('[data-testid="notification-badge"]');
    expect(badge).toBeFalsy();
  });

  it('clicking an unread notification marks it read', () => {
    recent.set(sampleNotifications);
    fixture.detectChanges();

    component.onNotificationClick(sampleNotifications[0]);
    expect(notificationServiceStub.markAsRead).toHaveBeenCalledWith('n-1');
  });
});
