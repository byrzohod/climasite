import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CheckoutService, CheckoutStep } from './checkout.service';
import { Order, Address } from '../models/order.model';
import { environment } from '../../../environments/environment';

describe('CheckoutService', () => {
  let service: CheckoutService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/orders`;

  const mockAddress: Address = {
    firstName: 'John',
    lastName: 'Doe',
    addressLine1: '123 Main St',
    addressLine2: 'Apt 4',
    city: 'Sofia',
    state: 'Sofia City',
    postalCode: '1000',
    country: 'Bulgaria',
    phone: '+359888123456'
  };

  const mockOrder: Order = {
    id: 'order-1',
    orderNumber: 'ORD-2025-001',
    userId: 'user-1',
    customerEmail: 'test@example.com',
    customerPhone: '+359888123456',
    status: 'Pending',
    shippingAddress: mockAddress,
    items: [
      {
        id: 'item-1',
        productId: 'product-1',
        productName: 'Test Product',
        productSlug: 'test-product',
        sku: 'TEST-001',
        unitPrice: 99.99,
        quantity: 2,
        lineTotal: 199.98
      }
    ],
    events: [],
    subtotal: 199.98,
    shippingCost: 10,
    taxAmount: 20,
    discountAmount: 0,
    total: 229.98,
    currency: 'EUR',
    paymentMethod: 'card',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z'
  };

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('climasite_session_id', 'sess_test123');

    TestBed.configureTestingModule({
      providers: [
        CheckoutService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(CheckoutService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('initialization', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should have default shipping step', () => {
      expect(service.currentStep()).toBe('shipping');
    });

    it('should have null addresses by default', () => {
      expect(service.shippingAddress()).toBeNull();
      expect(service.billingAddress()).toBeNull();
    });

    it('should have default payment method as card', () => {
      expect(service.paymentMethod()).toBe('card');
    });

    it('should not be processing by default', () => {
      expect(service.isProcessing()).toBeFalse();
    });

    it('should have no error by default', () => {
      expect(service.error()).toBeNull();
    });

    it('should have no lastOrderId by default', () => {
      expect(service.lastOrderId()).toBeNull();
    });
  });

  describe('step management', () => {
    it('should set step to shipping', () => {
      service.setStep('shipping');
      expect(service.currentStep()).toBe('shipping');
    });

    it('should set step to payment', () => {
      service.setStep('payment');
      expect(service.currentStep()).toBe('payment');
    });

    it('should set step to review', () => {
      service.setStep('review');
      expect(service.currentStep()).toBe('review');
    });
  });

  describe('address management', () => {
    it('should set shipping address', () => {
      service.setShippingAddress(mockAddress);
      expect(service.shippingAddress()).toEqual(mockAddress);
    });

    it('should set billing address', () => {
      service.setBillingAddress(mockAddress);
      expect(service.billingAddress()).toEqual(mockAddress);
    });

    it('should allow null billing address', () => {
      service.setBillingAddress(mockAddress);
      expect(service.billingAddress()).toEqual(mockAddress);

      service.setBillingAddress(null);
      expect(service.billingAddress()).toBeNull();
    });
  });

  describe('payment method', () => {
    it('should set payment method', () => {
      service.setPaymentMethod('paypal');
      expect(service.paymentMethod()).toBe('paypal');
    });
  });

  describe('step validation', () => {
    it('should not allow proceed to payment without shipping address', () => {
      expect(service.canProceedToPayment()).toBeFalse();
    });

    it('should allow proceed to payment with shipping address', () => {
      service.setShippingAddress(mockAddress);
      expect(service.canProceedToPayment()).toBeTrue();
    });

    it('should not allow proceed to review without shipping and payment', () => {
      expect(service.canProceedToReview()).toBeFalse();
    });

    it('should allow proceed to review with shipping and payment method', () => {
      service.setShippingAddress(mockAddress);
      service.setPaymentMethod('card');
      expect(service.canProceedToReview()).toBeTrue();
    });

    it('should not allow proceed to review with empty shipping method', () => {
      service.setShippingAddress(mockAddress);
      service.setShippingMethod('');
      expect(service.canProceedToReview()).toBeFalse();
    });
  });

  describe('createOrder', () => {
    beforeEach(() => {
      service.setShippingAddress(mockAddress);
      service.setPaymentMethod('card');
    });

    it('should create order with email', fakeAsync(() => {
      service.createOrder('test@example.com').subscribe();
      tick();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.customerEmail).toBe('test@example.com');
      expect(req.request.body.shippingAddress).toEqual(mockAddress);
      expect(req.request.body.shippingMethod).toBe('standard');
      expect(req.request.body.paymentMethod).toBe('card');
      expect(req.request.headers.get('X-Session-Id')).toBe('sess_test123');
      req.flush(mockOrder);
      tick();

      expect(service.lastOrderId()).toBe('order-1');
      expect(service.isProcessing()).toBeFalse();
    }));

    it('should include billing address if set', fakeAsync(() => {
      const billingAddress: Address = { ...mockAddress, addressLine1: '456 Billing St' };
      service.setBillingAddress(billingAddress);

      service.createOrder('test@example.com').subscribe();
      tick();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.billingAddress).toEqual(billingAddress);
      req.flush(mockOrder);
    }));

    it('should set processing state during request', fakeAsync(() => {
      service.createOrder('test@example.com').subscribe();
      // Don't tick yet

      const req = httpMock.expectOne(apiUrl);
      expect(service.isProcessing()).toBeTrue();

      req.flush(mockOrder);
      tick();

      expect(service.isProcessing()).toBeFalse();
    }));

    it('should throw error without shipping address', () => {
      service.setShippingAddress(null as any);

      expect(() => service.createOrder('test@example.com')).toThrow();
      expect(service.error()).toBe('Shipping address is required');
      expect(service.isProcessing()).toBeFalse();
    });

    it('should set error on API failure', fakeAsync(() => {
      let errorOccurred = false;
      service.createOrder('test@example.com').subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(apiUrl);
      req.error(new ErrorEvent('Network error'), { status: 500 });
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBeTruthy();
      expect(service.isProcessing()).toBeFalse();
    }));

    it('should set error message from API response', fakeAsync(() => {
      let errorOccurred = false;
      service.createOrder('test@example.com').subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(apiUrl);
      req.flush({ message: 'Insufficient stock' }, { status: 400, statusText: 'Bad Request' });
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBe('Insufficient stock');
    }));
  });

  describe('getOrder', () => {
    it('should get order by ID', fakeAsync(() => {
      let result: Order | undefined;
      service.getOrder('order-1').subscribe(order => { result = order; });
      tick();

      const req = httpMock.expectOne(`${apiUrl}/order-1`);
      expect(req.request.method).toBe('GET');
      expect(req.request.headers.get('X-Session-Id')).toBe('sess_test123');
      req.flush(mockOrder);
      tick();

      expect(result).toEqual(mockOrder);
    }));
  });

  describe('resetCheckout', () => {
    beforeEach(() => {
      service.setStep('review');
      service.setShippingAddress(mockAddress);
      service.setBillingAddress(mockAddress);
      service.setPaymentMethod('paypal');
    });

    it('should reset step to shipping', () => {
      service.resetCheckout();
      expect(service.currentStep()).toBe('shipping');
    });

    it('should reset shipping address to null', () => {
      service.resetCheckout();
      expect(service.shippingAddress()).toBeNull();
    });

    it('should reset billing address to null', () => {
      service.resetCheckout();
      expect(service.billingAddress()).toBeNull();
    });

    it('should reset payment method to card', () => {
      service.resetCheckout();
      expect(service.paymentMethod()).toBe('card');
    });

    it('should reset error to null', () => {
      service.resetCheckout();
      expect(service.error()).toBeNull();
    });
  });

  describe('validateShippingAddress', () => {
    it('should return empty array for valid address', () => {
      const errors = service.validateShippingAddress(mockAddress);
      expect(errors).toEqual([]);
    });

    it('should return error for missing firstName', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, firstName: '' });
      expect(errors).toContain('First name is required');
    });

    it('should return error for missing lastName', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, lastName: '' });
      expect(errors).toContain('Last name is required');
    });

    it('should return error for missing addressLine1', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, addressLine1: '' });
      expect(errors).toContain('Street address is required');
    });

    it('should return error for missing city', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, city: '' });
      expect(errors).toContain('City is required');
    });

    it('should return error for missing postalCode', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, postalCode: '' });
      expect(errors).toContain('Postal code is required');
    });

    it('should return error for missing country', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, country: '' });
      expect(errors).toContain('Country is required');
    });

    it('should return error for missing phone', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, phone: '' });
      expect(errors).toContain('Phone number is required');
    });

    it('should return multiple errors for multiple missing fields', () => {
      const errors = service.validateShippingAddress({
        firstName: '',
        lastName: '',
        addressLine1: '123 Main St',
        city: '',
        postalCode: '1000',
        country: 'Bulgaria',
        phone: '',
        state: 'Sofia'
      });

      expect(errors).toContain('First name is required');
      expect(errors).toContain('Last name is required');
      expect(errors).toContain('City is required');
      expect(errors).toContain('Phone number is required');
      expect(errors.length).toBe(4);
    });

    it('should trim whitespace from fields', () => {
      const errors = service.validateShippingAddress({ ...mockAddress, firstName: '   ' });
      expect(errors).toContain('First name is required');
    });

    it('should not require addressLine2 field', () => {
      const addressWithoutLine2 = { ...mockAddress };
      delete addressWithoutLine2.addressLine2;

      const errors = service.validateShippingAddress(addressWithoutLine2);
      expect(errors).toEqual([]);
    });
  });

  describe('session management', () => {
    it('should use session ID from localStorage', () => {
      service.setShippingAddress(mockAddress);
      service.createOrder('test@example.com').subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.headers.get('X-Session-Id')).toBe('sess_test123');
      req.flush(mockOrder);
    });

    it('should handle missing session ID gracefully', () => {
      localStorage.removeItem('climasite_session_id');

      service.setShippingAddress(mockAddress);
      service.createOrder('test@example.com').subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.headers.get('X-Session-Id')).toBe('');
      req.flush(mockOrder);
    });
  });
});
