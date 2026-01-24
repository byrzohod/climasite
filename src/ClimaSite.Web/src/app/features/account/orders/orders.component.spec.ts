import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, input } from '@angular/core';
import { OrdersComponent } from './orders.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { CheckoutService } from '../../../core/services/checkout.service';
import { of, throwError } from 'rxjs';
import { OrderBrief, PaginatedOrders } from '../../../core/models/order.model';
import { EmptyStateComponent } from '../../../shared/components/empty-state';

// Mock EmptyStateComponent to avoid Lucide icon registration issues
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
  readonly showIcon = input<boolean>(true);
}

describe('OrdersComponent', () => {
  let component: OrdersComponent;
  let fixture: ComponentFixture<OrdersComponent>;
  let checkoutServiceMock: jasmine.SpyObj<CheckoutService>;

  const mockOrders: OrderBrief[] = [
    {
      id: '1',
      orderNumber: 'ORD-001',
      status: 'Pending',
      total: 130,
      itemCount: 2,
      createdAt: new Date().toISOString(),
      items: [
        {
          id: 'item-1',
          productName: 'Test Product',
          imageUrl: 'https://example.com/image.jpg',
          quantity: 1
        }
      ]
    }
  ];

  const mockPaginatedResponse: PaginatedOrders = {
    items: mockOrders,
    pageNumber: 1,
    totalPages: 1,
    totalCount: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  const emptyPaginatedResponse: PaginatedOrders = {
    items: [],
    pageNumber: 1,
    totalPages: 0,
    totalCount: 0,
    hasPreviousPage: false,
    hasNextPage: false
  };

  beforeEach(async () => {
    checkoutServiceMock = jasmine.createSpyObj('CheckoutService', ['getOrders', 'getOrderStatuses']);
    checkoutServiceMock.getOrders.and.returnValue(of(mockPaginatedResponse));
    checkoutServiceMock.getOrderStatuses.and.returnValue(of(['Pending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled']));

    await TestBed.configureTestingModule({
      imports: [
        OrdersComponent,
        HttpClientTestingModule,
        RouterTestingModule,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: CheckoutService, useValue: checkoutServiceMock }
      ]
    })
    // Override OrdersComponent to use mock EmptyStateComponent
    .overrideComponent(OrdersComponent, {
      remove: { imports: [EmptyStateComponent] },
      add: { imports: [MockEmptyStateComponent] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(OrdersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load orders on init', () => {
    expect(checkoutServiceMock.getOrders).toHaveBeenCalled();
    expect(component.orders()).toEqual(mockOrders);
  });

  it('should show loading state initially', () => {
    // Create a new component without calling detectChanges
    const newFixture = TestBed.createComponent(OrdersComponent);
    const newComponent = newFixture.componentInstance;

    expect(newComponent.isLoading()).toBeTrue();
  });

  it('should handle error when loading orders fails', () => {
    checkoutServiceMock.getOrders.and.returnValue(throwError(() => new Error('Failed to load')));

    const newFixture = TestBed.createComponent(OrdersComponent);
    const newComponent = newFixture.componentInstance;
    newFixture.detectChanges();

    expect(newComponent.isLoading()).toBeFalse();
    expect(newComponent.orders()).toEqual([]);
  });

  it('should display orders when loaded', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="order-card"]')).toBeTruthy();
  });

  it('should display empty state when no orders', () => {
    checkoutServiceMock.getOrders.and.returnValue(of(emptyPaginatedResponse));
    const newFixture = TestBed.createComponent(OrdersComponent);
    newFixture.detectChanges();

    const compiled = newFixture.nativeElement;
    expect(compiled.querySelector('.empty-state')).toBeTruthy();
  });

  it('should filter orders by status', () => {
    component.selectedStatus = 'Pending';
    component.onFilterChange();

    expect(checkoutServiceMock.getOrders).toHaveBeenCalledWith(
      jasmine.objectContaining({ status: 'Pending' })
    );
  });

  it('should handle pagination', () => {
    const multiPageResponse: PaginatedOrders = {
      ...mockPaginatedResponse,
      totalPages: 3,
      hasNextPage: true
    };
    // Reset spy calls and set new response
    checkoutServiceMock.getOrders.calls.reset();
    checkoutServiceMock.getOrders.and.returnValue(of(multiPageResponse));

    // Set up paginated response so goToPage allows page 2
    component.paginatedOrders.set(multiPageResponse);
    component.goToPage(2);

    expect(checkoutServiceMock.getOrders).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 2 })
    );
  });

  it('should clear filters', () => {
    component.searchQuery = 'test';
    component.selectedStatus = 'Pending';
    component.dateFrom = '2024-01-01';

    component.clearFilters();

    expect(component.searchQuery).toBe('');
    expect(component.selectedStatus).toBe('');
    expect(component.dateFrom).toBe('');
  });

  it('should toggle sort direction', () => {
    expect(component.sortDirection).toBe('desc');

    component.toggleSortDirection();

    expect(component.sortDirection).toBe('asc');
  });

  it('should correctly identify active filters', () => {
    expect(component.hasActiveFilters()).toBeFalse();

    component.selectedStatus = 'Pending';
    expect(component.hasActiveFilters()).toBeTrue();

    component.selectedStatus = '';
    component.searchQuery = 'test';
    expect(component.hasActiveFilters()).toBeTrue();
  });
});
