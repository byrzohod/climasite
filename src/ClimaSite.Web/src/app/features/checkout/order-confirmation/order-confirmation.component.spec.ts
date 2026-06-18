import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { LucideAngularModule } from 'lucide-angular';
import { of, throwError } from 'rxjs';

import { signal } from '@angular/core';

import { OrderConfirmationComponent } from './order-confirmation.component';
import { CheckoutService } from '../../../core/services/checkout.service';
import { ConfettiService } from '../../../core/services/confetti.service';
import { PaymentService, BankTransferConfig } from '../../../core/services/payment.service';
import { Order, OrderStatus } from '../../../core/models/order.model';
import { ICON_REGISTRY } from '../../../shared/components/icon';

describe('OrderConfirmationComponent', () => {
  let component: OrderConfirmationComponent;
  let fixture: ComponentFixture<OrderConfirmationComponent>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;
  let confettiService: jasmine.SpyObj<ConfettiService>;
  let paymentService: jasmine.SpyObj<PaymentService>;
  const bankTransferSignal = signal<BankTransferConfig | null>(null);

  const mockOrder: Order = {
    id: 'test-order-123',
    orderNumber: 'CLM-2026-001234',
    customerEmail: 'john@example.com',
    customerPhone: '+359888123456',
    status: 'Paid' as OrderStatus,
    shippingAddress: {
      firstName: 'John',
      lastName: 'Doe',
      addressLine1: '123 Main St',
      addressLine2: 'Apt 4B',
      city: 'Sofia',
      state: 'Sofia',
      postalCode: '1000',
      country: 'Bulgaria',
      phone: '+359888123456'
    },
    items: [
      {
        id: 'item-1',
        productId: 'prod-1',
        productName: 'Arctic Pro 12000 BTU',
        productSlug: 'arctic-pro-12000-btu',
        sku: 'AC-12K-001',
        imageUrl: '/images/products/ac-1.jpg',
        unitPrice: 599,
        quantity: 2,
        lineTotal: 1198
      },
      {
        id: 'item-2',
        productId: 'prod-2',
        productName: 'Premium Installation Kit',
        productSlug: 'premium-installation-kit',
        sku: 'KIT-001',
        unitPrice: 399,
        quantity: 1,
        lineTotal: 399
      }
    ],
    subtotal: 1597,
    shippingCost: 0,
    taxAmount: 319.4,
    discountAmount: 0,
    total: 1916.4,
    currency: 'USD',
    paymentMethod: 'card',
    shippingMethod: 'standard',
    events: [],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  beforeEach(async () => {
    const checkoutSpy = jasmine.createSpyObj('CheckoutService', ['getOrder']);
    const confettiSpy = jasmine.createSpyObj('ConfettiService', ['burst', 'stop']);
    bankTransferSignal.set(null);
    const paymentSpy = jasmine.createSpyObj<PaymentService>('PaymentService', ['loadConfig']);
    paymentSpy.loadConfig.and.returnValue(Promise.resolve(null));
    // Expose the bankTransfer readonly signal on the spy.
    Object.defineProperty(paymentSpy, 'bankTransfer', { value: bankTransferSignal.asReadonly() });

    await TestBed.configureTestingModule({
      imports: [
        OrderConfirmationComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick(ICON_REGISTRY)
      ],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => 'test-order-123'
              },
              queryParamMap: {
                get: () => null
              }
            }
          }
        },
        { provide: CheckoutService, useValue: checkoutSpy },
        { provide: ConfettiService, useValue: confettiSpy },
        { provide: PaymentService, useValue: paymentSpy }
      ]
    }).compileComponents();

    checkoutService = TestBed.inject(CheckoutService) as jasmine.SpyObj<CheckoutService>;
    confettiService = TestBed.inject(ConfettiService) as jasmine.SpyObj<ConfettiService>;
    paymentService = TestBed.inject(PaymentService) as jasmine.SpyObj<PaymentService>;
  });

  describe('when order loads successfully', () => {
    beforeEach(() => {
      checkoutService.getOrder.and.returnValue(of(mockOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should load order on init', () => {
      expect(checkoutService.getOrder).toHaveBeenCalledWith('test-order-123');
    });

    it('should display order number', () => {
      const orderNumber = fixture.nativeElement.querySelector('[data-testid="order-number"]');
      expect(orderNumber.textContent).toContain('CLM-2026-001234');
    });

    it('should display shipping address', () => {
      const shippingInfo = fixture.nativeElement.querySelector('[data-testid="shipping-info"]');
      expect(shippingInfo.textContent).toContain('John');
      expect(shippingInfo.textContent).toContain('Doe');
      expect(shippingInfo.textContent).toContain('123 Main St');
      expect(shippingInfo.textContent).toContain('Sofia');
    });

    it('should display order items', () => {
      const items = fixture.nativeElement.querySelectorAll('.order-item');
      expect(items.length).toBe(2);
      expect(items[0].textContent).toContain('Arctic Pro 12000 BTU');
      expect(items[1].textContent).toContain('Premium Installation Kit');
    });

    it('should display order total', () => {
      const total = fixture.nativeElement.querySelector('[data-testid="order-total"]');
      expect(total.textContent).toContain('1,916.40');
    });

    it('should trigger confetti for recent orders', () => {
      expect(confettiService.burst).toHaveBeenCalled();
    });

    it('should display Continue Shopping button', () => {
      const continueBtn = fixture.nativeElement.querySelector('[data-testid="continue-shopping-btn"]');
      expect(continueBtn).toBeTruthy();
      expect(continueBtn.getAttribute('href')).toBe('/products');
    });

    it('should display View Order button', () => {
      const viewBtn = fixture.nativeElement.querySelector('[data-testid="view-order-btn"]');
      expect(viewBtn).toBeTruthy();
    });
  });

  describe('when order has tracking number', () => {
    beforeEach(() => {
      const orderWithTracking = { ...mockOrder, trackingNumber: 'TRK123456' };
      checkoutService.getOrder.and.returnValue(of(orderWithTracking));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should display Track Order button instead of View Order', () => {
      const trackBtn = fixture.nativeElement.querySelector('[data-testid="track-order-btn"]');
      expect(trackBtn).toBeTruthy();
      
      const viewBtn = fixture.nativeElement.querySelector('[data-testid="view-order-btn"]');
      expect(viewBtn).toBeFalsy();
    });
  });

  describe('when order has discount', () => {
    beforeEach(() => {
      const orderWithDiscount = { ...mockOrder, discountAmount: 50 };
      checkoutService.getOrder.and.returnValue(of(orderWithDiscount));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should display discount row', () => {
      const discountRow = fixture.nativeElement.querySelector('.totals-row.discount');
      expect(discountRow).toBeTruthy();
      expect(discountRow.textContent).toContain('50');
    });
  });

  describe('payment method (GAP-06)', () => {
    it('does not show bank instructions or paypal for a card order', () => {
      checkoutService.getOrder.and.returnValue(of(mockOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const bankPanel = fixture.nativeElement.querySelector('[data-testid="bank-transfer-instructions"]');
      expect(bankPanel).toBeFalsy();
      expect(fixture.nativeElement.textContent).not.toContain('PayPal');
      expect(paymentService.loadConfig).not.toHaveBeenCalled();
    });

    it('shows bank transfer instructions with amount and reference for a bank order', () => {
      bankTransferSignal.set({
        iban: 'BG80BNBG96611020345678',
        accountName: 'ClimaSite EOOD',
        bankName: 'placeholder Bank'
      });
      const bankOrder = { ...mockOrder, paymentMethod: 'bank' };
      checkoutService.getOrder.and.returnValue(of(bankOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const bankPanel = fixture.nativeElement.querySelector('[data-testid="bank-transfer-instructions"]');
      expect(bankPanel).toBeTruthy();

      const reference = fixture.nativeElement.querySelector('[data-testid="bank-reference"]');
      expect(reference.textContent).toContain('CLM-2026-001234');

      const amount = fixture.nativeElement.querySelector('[data-testid="bank-amount"]');
      expect(amount.textContent).toContain('1,916.40');

      expect(bankPanel.textContent).toContain('BG80BNBG96611020345678');
      expect(bankPanel.textContent).toContain('ClimaSite EOOD');
      expect(paymentService.loadConfig).toHaveBeenCalled();
    });
  });

  describe('when order fails to load', () => {
    beforeEach(() => {
      checkoutService.getOrder.and.returnValue(throwError(() => ({ 
        error: { message: 'Raw order load failure' }
      })));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should display error state', () => {
      const errorState = fixture.nativeElement.querySelector('[data-testid="error-state"]');
      expect(errorState).toBeTruthy();
    });

    it('should display error message', () => {
      expect(component.error()).toBe('checkout.orderConfirmation.errors.loadFailed');
      expect(component.error()).not.toBe('Raw order load failure');
    });

    it('should not trigger confetti', () => {
      expect(confettiService.burst).not.toHaveBeenCalled();
    });

    it('should display retry button', () => {
      const retryBtn = fixture.nativeElement.querySelector('[data-testid="retry-btn"]');
      expect(retryBtn).toBeTruthy();
    });
  });

  describe('loading state', () => {
    beforeEach(() => {
      // Don't return anything yet - keep loading
      checkoutService.getOrder.and.returnValue(of(mockOrder));
    });

    it('should show loading state initially', () => {
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      // Don't call detectChanges to keep it in loading state
      expect(component.loading()).toBe(true);
    });
  });

  describe('estimated delivery calculation', () => {
    it('should calculate correct delivery for standard shipping', () => {
      checkoutService.getOrder.and.returnValue(of(mockOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const delivery = component.estimatedDelivery();
      expect(delivery).toBeTruthy();
      // Standard is 5-7 days, so should contain a date range
      expect(delivery).toMatch(/\w+ \d+ - \w+ \d+/);
    });

    it('should calculate correct delivery for express shipping', () => {
      const expressOrder = { ...mockOrder, shippingMethod: 'express' };
      checkoutService.getOrder.and.returnValue(of(expressOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const delivery = component.estimatedDelivery();
      expect(delivery).toBeTruthy();
    });

    it('should calculate correct delivery for overnight shipping', () => {
      const overnightOrder = { ...mockOrder, shippingMethod: 'overnight' };
      checkoutService.getOrder.and.returnValue(of(overnightOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      const delivery = component.estimatedDelivery();
      expect(delivery).toBeTruthy();
      // Overnight is 1 day, so should be a single date
      expect(delivery).toMatch(/\w+ \d+$/);
    });
  });

  describe('copy order number', () => {
    beforeEach(() => {
      checkoutService.getOrder.and.returnValue(of(mockOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      // Mock clipboard API
      spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());
    });

    it('should copy order number to clipboard', fakeAsync(() => {
      component.copyOrderNumber();
      tick();

      expect(navigator.clipboard.writeText).toHaveBeenCalledWith('CLM-2026-001234');
    }));

    it('should set copied flag temporarily', fakeAsync(() => {
      component.copyOrderNumber();
      tick();

      expect(component.copied()).toBe(true);

      tick(2000);
      expect(component.copied()).toBe(false);
    }));
  });

  describe('guest token handling (GAP-07)', () => {
    let guestToken: string | null;
    let locationReplaceSpy: jasmine.Spy;

    function configureWithToken(token: string | null): void {
      guestToken = token;
      TestBed.resetTestingModule();
      const checkoutSpy = jasmine.createSpyObj('CheckoutService', ['getOrder', 'getGuestOrder']);
      const confettiSpy = jasmine.createSpyObj('ConfettiService', ['burst', 'stop']);
      const paymentSpy = jasmine.createSpyObj<PaymentService>('PaymentService', ['loadConfig']);
      paymentSpy.loadConfig.and.returnValue(Promise.resolve(null));
      Object.defineProperty(paymentSpy, 'bankTransfer', { value: signal<BankTransferConfig | null>(null).asReadonly() });
      checkoutSpy.getOrder.and.returnValue(of(mockOrder));
      checkoutSpy.getGuestOrder.and.returnValue(of(mockOrder));

      TestBed.configureTestingModule({
        imports: [
          OrderConfirmationComponent,
          TranslateModule.forRoot(),
          LucideAngularModule.pick(ICON_REGISTRY)
        ],
        providers: [
          provideRouter([]),
          provideHttpClient(),
          provideHttpClientTesting(),
          {
            provide: ActivatedRoute,
            useValue: {
              snapshot: {
                paramMap: { get: () => 'test-order-123' },
                queryParamMap: { get: () => guestToken }
              }
            }
          },
          { provide: CheckoutService, useValue: checkoutSpy },
          { provide: ConfettiService, useValue: confettiSpy },
          { provide: PaymentService, useValue: paymentSpy }
        ]
      });

      checkoutService = TestBed.inject(CheckoutService) as jasmine.SpyObj<CheckoutService>;
      const location = TestBed.inject(Location);
      locationReplaceSpy = spyOn(location, 'replaceState').and.callThrough();
    }

    it('loads the guest order with the token and strips it from the URL', () => {
      configureWithToken('guest-token-abc');
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      expect(checkoutService.getGuestOrder).toHaveBeenCalledWith('test-order-123', 'guest-token-abc');
      expect(locationReplaceSpy).toHaveBeenCalledWith('/checkout/confirmation/test-order-123');
    });

    it('does not call replaceState when there is no token', () => {
      configureWithToken(null);
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      expect(checkoutService.getOrder).toHaveBeenCalledWith('test-order-123');
      expect(checkoutService.getGuestOrder).not.toHaveBeenCalled();
      expect(locationReplaceSpy).not.toHaveBeenCalled();
    });

    it('retryLoad reuses the captured token after the URL has been stripped', () => {
      configureWithToken('guest-token-abc');
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      checkoutService.getGuestOrder.calls.reset();
      component.retryLoad();

      expect(checkoutService.getGuestOrder).toHaveBeenCalledWith('test-order-123', 'guest-token-abc');
    });
  });

  describe('cleanup', () => {
    beforeEach(() => {
      checkoutService.getOrder.and.returnValue(of(mockOrder));
      fixture = TestBed.createComponent(OrderConfirmationComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should stop confetti on destroy', () => {
      component.ngOnDestroy();
      expect(confettiService.stop).toHaveBeenCalled();
    });
  });
});
