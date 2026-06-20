import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { Router } from '@angular/router';

import { CheckoutComponent } from './checkout.component';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService } from '../../core/services/checkout.service';
import { AddressService } from '../../core/services/address.service';
import { AuthService } from '../../auth/services/auth.service';
import { PaymentService, PaymentIntentResponse } from '../../core/services/payment.service';
import { ConfettiService } from '../../core/services/confetti.service';

/**
 * BUG-04: a rapid double-click on "Place order" must not fire two create-intent
 * (and therefore two charge) sequences. The component claims an in-flight guard at
 * the very top of placeOrder(), before any create-intent call.
 */
describe('CheckoutComponent - double-submit guard', () => {
  let component: CheckoutComponent;
  let paymentService: jasmine.SpyObj<PaymentService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  beforeEach(() => {
    paymentService = jasmine.createSpyObj<PaymentService>(
      'PaymentService',
      ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements', 'loadConfig']
    );
    paymentService.loadConfig.and.returnValue(Promise.resolve(null));
    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'isProcessing']
    );

    // Card flow: a pending create-intent keeps the guard held across both clicks.
    paymentService.createPaymentIntent.and.returnValue(new Subject<PaymentIntentResponse>().asObservable());

    checkoutService.paymentMethod.and.returnValue('card');
    checkoutService.shippingMethod.and.returnValue('standard');
    checkoutService.shippingAddress.and.returnValue(null);
    checkoutService.getSessionId.and.returnValue('session-1');

    const cartService = jasmine.createSpyObj<CartService>('CartService', ['clearCart']);
    const addressService = jasmine.createSpyObj<AddressService>('AddressService', ['loadAddresses', 'hasAddresses', 'addresses']);
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['isAuthenticated']);
    authService.isAuthenticated.and.returnValue(false);
    const confettiService = jasmine.createSpyObj<ConfettiService>('ConfettiService', ['burst', 'stop']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: CheckoutService, useValue: checkoutService },
        { provide: AddressService, useValue: addressService },
        { provide: AuthService, useValue: authService },
        { provide: PaymentService, useValue: paymentService },
        { provide: ConfettiService, useValue: confettiService },
        { provide: Router, useValue: router }
      ]
    });

    // Instantiate the component in its injection context WITHOUT rendering the template
    // (the template needs Stripe DOM / translations we don't want to set up here).
    component = TestBed.runInInjectionContext(() => new CheckoutComponent());
    component.shippingForm.patchValue({ email: 'buyer@test.com', phone: '+359888000000' });
  });

  it('fires only one create-intent when placeOrder() is double-clicked', () => {
    // Both calls return pending promises (the create-intent Subject never emits), so we don't
    // await them. The guard is set synchronously before the first await, so the synchronous
    // entry of the second call short-circuits and never reaches createPaymentIntent again.
    void component.placeOrder();
    void component.placeOrder();

    expect(paymentService.createPaymentIntent).toHaveBeenCalledTimes(1);
  });

  it('sets the in-flight guard immediately on the first placeOrder()', () => {
    expect(component.placingOrder()).toBeFalse();
    void component.placeOrder();
    expect(component.placingOrder()).toBeTrue();
  });
});

/**
 * GAP-06: bank transfer is a real, supported payment method; the fake PayPal option is gone.
 * Selecting "bank" loads the bank-transfer config so the instructions panel can render.
 */
describe('CheckoutComponent - bank transfer payment (GAP-06)', () => {
  let component: CheckoutComponent;
  let paymentService: jasmine.SpyObj<PaymentService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  beforeEach(() => {
    paymentService = jasmine.createSpyObj<PaymentService>(
      'PaymentService',
      ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements', 'loadConfig']
    );
    paymentService.loadConfig.and.returnValue(Promise.resolve(null));
    paymentService.initialize.and.returnValue(Promise.resolve(true));

    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'isProcessing']
    );
    checkoutService.paymentMethod.and.returnValue('bank');

    const cartService = jasmine.createSpyObj<CartService>('CartService', ['clearCart']);
    const addressService = jasmine.createSpyObj<AddressService>('AddressService', ['loadAddresses', 'hasAddresses', 'addresses']);
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['isAuthenticated']);
    authService.isAuthenticated.and.returnValue(false);
    const confettiService = jasmine.createSpyObj<ConfettiService>('ConfettiService', ['burst', 'stop']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: CheckoutService, useValue: checkoutService },
        { provide: AddressService, useValue: addressService },
        { provide: AuthService, useValue: authService },
        { provide: PaymentService, useValue: paymentService },
        { provide: ConfettiService, useValue: confettiService },
        { provide: Router, useValue: router }
      ]
    });

    component = TestBed.runInInjectionContext(() => new CheckoutComponent());
  });

  it('loads bank-transfer config when bank is selected', () => {
    component.selectPaymentMethod('bank');
    expect(checkoutService.setPaymentMethod).toHaveBeenCalledWith('bank');
    expect(paymentService.loadConfig).toHaveBeenCalled();
    // Bank is offline: no Stripe card elements are created.
    expect(paymentService.destroyElements).toHaveBeenCalled();
  });

  it('does not load bank config when card is selected', () => {
    component.selectPaymentMethod('card');
    expect(checkoutService.setPaymentMethod).toHaveBeenCalledWith('card');
    expect(paymentService.loadConfig).not.toHaveBeenCalled();
    expect(paymentService.initialize).toHaveBeenCalled();
  });
});

/**
 * SLICE C: when the card create-intent fails (the configured Stripe key is a placeholder,
 * so POST /api/payments/create-intent returns 400 'Invalid API Key'), the checkout surfaces
 * the clearer `checkout.cardUnavailable` message — which steers the buyer to bank transfer —
 * instead of the generic `checkout.payment.errors.failed` text. Card remains the default.
 */
describe('CheckoutComponent - card payment failure messaging (SLICE C)', () => {
  let component: CheckoutComponent;
  let paymentService: jasmine.SpyObj<PaymentService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  beforeEach(() => {
    paymentService = jasmine.createSpyObj<PaymentService>(
      'PaymentService',
      ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements', 'loadConfig']
    );
    paymentService.loadConfig.and.returnValue(Promise.resolve(null));

    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'isProcessing']
    );
    // Card is the default payment method (unchanged by this slice).
    checkoutService.paymentMethod.and.returnValue('card');
    checkoutService.shippingMethod.and.returnValue('standard');
    checkoutService.shippingAddress.and.returnValue(null);
    checkoutService.getSessionId.and.returnValue('session-1');

    const cartService = jasmine.createSpyObj<CartService>('CartService', ['clearCart']);
    const addressService = jasmine.createSpyObj<AddressService>('AddressService', ['loadAddresses', 'hasAddresses', 'addresses']);
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['isAuthenticated']);
    authService.isAuthenticated.and.returnValue(false);
    const confettiService = jasmine.createSpyObj<ConfettiService>('ConfettiService', ['burst', 'stop']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: CheckoutService, useValue: checkoutService },
        { provide: AddressService, useValue: addressService },
        { provide: AuthService, useValue: authService },
        { provide: PaymentService, useValue: paymentService },
        { provide: ConfettiService, useValue: confettiService },
        { provide: Router, useValue: router }
      ]
    });

    component = TestBed.runInInjectionContext(() => new CheckoutComponent());
    component.shippingForm.patchValue({ email: 'buyer@test.com', phone: '+359888000000' });
  });

  it('surfaces checkout.cardUnavailable when create-intent fails with a 400 Invalid API Key', async () => {
    // Mirrors the placeholder-Stripe-key case: create-intent rejects with a 400.
    paymentService.createPaymentIntent.and.returnValue(
      throwError(() => ({ status: 400, error: { message: 'Invalid API Key provided' } }))
    );

    await component.placeOrder();

    expect(checkoutService.setError).toHaveBeenCalledWith('checkout.cardUnavailable');
    expect(checkoutService.setError).not.toHaveBeenCalledWith('checkout.payment.errors.failed');
    // The guard is released so the buyer can retry / switch to bank transfer.
    expect(component.placingOrder()).toBeFalse();
    // Card failures must never silently place an order.
    expect(checkoutService.createOrder).not.toHaveBeenCalled();
  });

  it('surfaces checkout.cardUnavailable when create-intent returns no client secret', async () => {
    // Misconfigured key path where the request succeeds over HTTP but yields no usable intent.
    paymentService.createPaymentIntent.and.returnValue(of({ clientSecret: '' } as PaymentIntentResponse));

    await component.placeOrder();

    expect(checkoutService.setError).toHaveBeenCalledWith('checkout.cardUnavailable');
    expect(component.placingOrder()).toBeFalse();
    expect(checkoutService.createOrder).not.toHaveBeenCalled();
  });

  it('preserves a specific field-validation key from confirmPayment rather than cardUnavailable', async () => {
    // Create-intent succeeds; the failure is in Stripe.js card confirmation (entered card details).
    // Those validation errors must be kept as-is, NOT replaced by the cardUnavailable message.
    paymentService.createPaymentIntent.and.returnValue(
      of({ clientSecret: 'pi_secret_123' } as PaymentIntentResponse)
    );
    paymentService.confirmPayment.and.returnValue(
      Promise.resolve({ success: false, error: 'checkout.payment.errors.cardDeclined' })
    );

    await component.placeOrder();

    expect(checkoutService.setError).toHaveBeenCalledWith('checkout.payment.errors.cardDeclined');
    expect(checkoutService.setError).not.toHaveBeenCalledWith('checkout.cardUnavailable');
  });
});
