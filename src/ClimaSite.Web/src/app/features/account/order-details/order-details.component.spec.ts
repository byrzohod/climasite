import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OrderDetailsComponent } from './order-details.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { CheckoutService } from '../../../core/services/checkout.service';
import { of, throwError } from 'rxjs';
import { Order } from '../../../core/models/order.model';

describe('OrderDetailsComponent', () => {
  let component: OrderDetailsComponent;
  let fixture: ComponentFixture<OrderDetailsComponent>;
  let checkoutServiceMock: jasmine.SpyObj<CheckoutService>;

  const mockOrder: Order = {
    id: '123',
    orderNumber: 'ORD-001',
    customerEmail: 'test@example.com',
    customerPhone: '+359888123456',
    status: 'Processing',
    paymentMethod: 'card',
    subtotal: 500,
    shippingCost: 20,
    taxAmount: 100,
    discountAmount: 0,
    total: 620,
    currency: 'EUR',
    items: [
      {
        id: 'item-1',
        productId: 'prod-1',
        productName: 'Test AC Unit',
        productSlug: 'test-ac-unit',
        variantId: 'var-1',
        variantName: 'Standard',
        sku: 'AC-001',
        quantity: 1,
        unitPrice: 500,
        lineTotal: 500,
        imageUrl: '/images/ac.jpg'
      }
    ],
    events: [
      {
        id: 'event-1',
        orderId: '123',
        status: 'Pending',
        description: 'Order placed',
        createdAt: new Date().toISOString()
      },
      {
        id: 'event-2',
        orderId: '123',
        status: 'Processing',
        description: 'Order is being processed',
        createdAt: new Date().toISOString()
      }
    ],
    shippingAddress: {
      firstName: 'John',
      lastName: 'Doe',
      addressLine1: '123 Main St',
      addressLine2: 'Apt 4',
      city: 'Sofia',
      state: 'Sofia',
      postalCode: '1000',
      country: 'Bulgaria',
      phone: '+359888123456'
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  beforeEach(async () => {
    checkoutServiceMock = jasmine.createSpyObj('CheckoutService', ['getOrder', 'cancelOrder']);
    checkoutServiceMock.getOrder.and.returnValue(of(mockOrder));
    checkoutServiceMock.cancelOrder.and.returnValue(of({ ...mockOrder, status: 'Cancelled' }));

    await TestBed.configureTestingModule({
      imports: [
        OrderDetailsComponent,
        HttpClientTestingModule,
        RouterTestingModule,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: CheckoutService, useValue: checkoutServiceMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => '123'
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load order on init', () => {
    expect(checkoutServiceMock.getOrder).toHaveBeenCalledWith('123');
    expect(component.order()).toEqual(mockOrder);
  });

  it('should display order details', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="order-details-page"]')).toBeTruthy();
  });

  it('should display order items', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    const items = compiled.querySelectorAll('[data-testid="order-item-row"]');
    expect(items.length).toBe(1);
  });

  it('should display shipping address', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('John');
    expect(compiled.textContent).toContain('Doe');
    expect(compiled.textContent).toContain('123 Main St');
  });

  it('should display order totals', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('620');
  });

  it('should handle error when order not found', () => {
    checkoutServiceMock.getOrder.and.returnValue(throwError(() => new Error('Not found')));

    const newFixture = TestBed.createComponent(OrderDetailsComponent);
    newFixture.detectChanges();

    const newComponent = newFixture.componentInstance;
    expect(newComponent.error()).toBeTruthy();
  });

  it('should show loading state while fetching', () => {
    expect(component.isLoading()).toBeFalse();
  });

  it('should show error if no order id in route', async () => {
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [
        OrderDetailsComponent,
        HttpClientTestingModule,
        RouterTestingModule,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: CheckoutService, useValue: checkoutServiceMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => null
              }
            }
          }
        }
      ]
    }).compileComponents();

    const noIdFixture = TestBed.createComponent(OrderDetailsComponent);
    noIdFixture.detectChanges();

    const noIdComponent = noIdFixture.componentInstance;
    expect(noIdComponent.error()).toBe('Order not found');
  });

  it('should determine if order can be cancelled', () => {
    // Processing status - cannot be cancelled
    expect(component.canCancel()).toBeFalse();

    // Pending status - can be cancelled
    component.order.set({ ...mockOrder, status: 'Pending' });
    expect(component.canCancel()).toBeTrue();

    // Paid status - can be cancelled
    component.order.set({ ...mockOrder, status: 'Paid' });
    expect(component.canCancel()).toBeTrue();

    // Delivered status - cannot be cancelled
    component.order.set({ ...mockOrder, status: 'Delivered' });
    expect(component.canCancel()).toBeFalse();
  });

  it('should display timeline when events exist', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('.timeline')).toBeTruthy();
  });

  it('should show cancel button for cancellable orders', () => {
    component.order.set({ ...mockOrder, status: 'Pending' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="cancel-order-btn"]')).toBeTruthy();
  });

  it('should not show cancel button for non-cancellable orders', () => {
    component.order.set({ ...mockOrder, status: 'Delivered' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="cancel-order-btn"]')).toBeFalsy();
  });
});
