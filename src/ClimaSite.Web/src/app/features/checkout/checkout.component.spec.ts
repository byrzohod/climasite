import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { Subject, Observable, of, throwError } from 'rxjs';
import { Router, provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';

import { CheckoutComponent } from './checkout.component';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService, CheckoutStep } from '../../core/services/checkout.service';
import { AddressService } from '../../core/services/address.service';
import { AuthService } from '../../auth/services/auth.service';
import { PaymentService, PaymentIntentResponse } from '../../core/services/payment.service';
import { ConfettiService } from '../../core/services/confetti.service';
import { Order } from '../../core/models/order.model';
import { SavedAddress } from '../../core/models/address.model';

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

  // BUG-11 / DEC-CURRENCY: the shipping-option labels must mirror the server's CheckoutPricing.cs in EUR
  // (express 15.99 / overnight 19.99 flat; standard 5.99 below the €50 free-shipping threshold) so
  // displayed shipping == charged shipping. This block's CartService spy has no subtotal signal
  // (defaults to 0 < 50), so standard is the paid 5.99 rate.
  it('shows shipping costs that match the server (CheckoutPricing) in EUR', () => {
    expect(component.shippingCost).toEqual({ standard: 5.99, express: 15.99, overnight: 19.99 });
  });
});

/**
 * DEC-SHIPPING: standard shipping is FREE when the cart subtotal is at/above €50, otherwise €5.99.
 * The UI must mirror the server's CheckoutPricing.GetShippingCost(method, subtotal) so the displayed
 * shipping == the charged shipping, and the order-summary shipping line + grand total reflect the
 * SELECTED method (not the always-zero cart.shipping). Express €15.99 / overnight €19.99 are flat.
 */
describe('CheckoutComponent - DEC-SHIPPING free standard shipping over €50', () => {
  let component: CheckoutComponent;
  let cartService: jasmine.SpyObj<CartService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  function buildComponent(subtotal: number, tax: number, selectedMethod = 'standard'): void {
    cartService = jasmine.createSpyObj<CartService>('CartService', ['clearCart', 'subtotal', 'cart', 'total']);
    cartService.subtotal.and.returnValue(subtotal);
    cartService.total.and.returnValue(subtotal + tax);
    cartService.cart.and.returnValue({
      id: 'c1', items: [], subtotal, shipping: 0, tax, total: subtotal + tax,
      itemCount: 0, createdAt: '', updatedAt: ''
    });

    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'isProcessing']
    );
    checkoutService.paymentMethod.and.returnValue('card');
    checkoutService.shippingMethod.and.returnValue(selectedMethod);
    checkoutService.shippingAddress.and.returnValue(null);
    checkoutService.getSessionId.and.returnValue('session-1');

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
        { provide: PaymentService, useValue: jasmine.createSpyObj<PaymentService>('PaymentService', ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements', 'loadConfig']) },
        { provide: ConfettiService, useValue: confettiService },
        { provide: Router, useValue: router }
      ]
    });

    component = TestBed.runInInjectionContext(() => new CheckoutComponent());
  }

  it('charges €5.99 standard shipping just below the €50 threshold (49.99)', () => {
    buildComponent(49.99, 10.0, 'standard');
    expect(component.shippingCostFor('standard')).toBe(5.99);
    expect(component.shippingCost.standard).toBe(5.99);
    expect(component.selectedShippingCost()).toBe(5.99);
  });

  it('gives FREE standard shipping exactly at the €50 threshold', () => {
    buildComponent(50, 10.0, 'standard');
    expect(component.shippingCostFor('standard')).toBe(0);
    expect(component.shippingCost.standard).toBe(0);
    expect(component.selectedShippingCost()).toBe(0);
  });

  it('gives FREE standard shipping above the €50 threshold (50.01)', () => {
    buildComponent(50.01, 10.0, 'standard');
    expect(component.shippingCostFor('standard')).toBe(0);
    expect(component.selectedShippingCost()).toBe(0);
  });

  it('keeps express €15.99 and overnight €19.99 flat regardless of subtotal', () => {
    buildComponent(500, 100.0, 'express');
    // Flat tiers are unaffected by the free-shipping threshold even on a large order.
    expect(component.shippingCostFor('express')).toBe(15.99);
    expect(component.shippingCostFor('overnight')).toBe(19.99);
  });

  it('order-summary total = subtotal + selected shipping + tax for paid standard (<€50)', () => {
    // subtotal 49.99, tax 10.00, standard selected → +5.99 shipping.
    buildComponent(49.99, 10.0, 'standard');
    expect(component.selectedShippingCost()).toBe(5.99);
    expect(component.summaryTotal()).toBeCloseTo(49.99 + 5.99 + 10.0, 2);
  });

  it('order-summary total omits shipping when standard is free (≥€50)', () => {
    // subtotal 60.00, tax 12.00, standard selected → free shipping, total = subtotal + tax.
    buildComponent(60, 12.0, 'standard');
    expect(component.selectedShippingCost()).toBe(0);
    expect(component.summaryTotal()).toBeCloseTo(60 + 0 + 12.0, 2);
  });

  it('order-summary reflects the SELECTED express method even on a ≥€50 order (no longer always-free)', () => {
    // The latent bug: summary used cart.shipping (always 0) and omitted express/overnight from the
    // total. With express selected on a €60 order, shipping must be €15.99 and the total must include it.
    buildComponent(60, 12.0, 'express');
    expect(component.selectedShippingCost()).toBe(15.99);
    expect(component.summaryTotal()).toBeCloseTo(60 + 15.99 + 12.0, 2);
  });

  it('order-summary reflects the SELECTED overnight method in the total', () => {
    buildComponent(60, 12.0, 'overnight');
    expect(component.selectedShippingCost()).toBe(19.99);
    expect(component.summaryTotal()).toBeCloseTo(60 + 19.99 + 12.0, 2);
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

/**
 * Stripe CARD fix: the card form lives on the payment step but the order is confirmed on the
 * review step (where the live card element is gone). proceedFromPayment() captures a Stripe
 * PaymentMethod *before* leaving the payment step. On success it stores the id and advances to
 * review; on failure it surfaces the error and stays on payment. The captured id is later passed
 * to confirmPayment() as the 3rd argument inside placeOrder().
 */
describe('CheckoutComponent - proceedFromPayment (Stripe card capture)', () => {
  let component: CheckoutComponent;
  let paymentService: jasmine.SpyObj<PaymentService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  function buildComponent(paymentMethod: 'card' | 'bank'): void {
    paymentService = jasmine.createSpyObj<PaymentService>(
      'PaymentService',
      ['createPaymentIntent', 'confirmPayment', 'createCardPaymentMethod', 'destroyElements', 'initialize', 'createElements', 'loadConfig']
    );
    paymentService.loadConfig.and.returnValue(Promise.resolve(null));
    paymentService.initialize.and.returnValue(Promise.resolve(true));

    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'lastGuestToken', 'isProcessing']
    );
    checkoutService.paymentMethod.and.returnValue(paymentMethod);
    checkoutService.shippingMethod.and.returnValue('standard');
    checkoutService.shippingAddress.and.returnValue(null);
    checkoutService.getSessionId.and.returnValue('session-1');
    checkoutService.lastOrderId.and.returnValue(null);
    checkoutService.lastGuestToken.and.returnValue(null);

    const cartService = jasmine.createSpyObj<CartService>('CartService', ['clearCart']);
    cartService.clearCart.and.returnValue(of(void 0));
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
  }

  it('captures the card payment method and advances to review on success (card)', async () => {
    buildComponent('card');
    paymentService.createCardPaymentMethod.and.returnValue(
      Promise.resolve({ success: true, paymentMethodId: 'pm_captured_1' })
    );

    await component.proceedFromPayment();

    expect(paymentService.createCardPaymentMethod).toHaveBeenCalledTimes(1);
    // Advances to the review step (goToStep delegates to checkoutService.setStep).
    expect(checkoutService.setStep).toHaveBeenCalledWith('review');
    // Any prior error is cleared on success.
    expect(checkoutService.setError).toHaveBeenCalledWith(null);
  });

  it('passes the captured payment method id to confirmPayment as the 3rd arg in placeOrder', async () => {
    buildComponent('card');
    paymentService.createCardPaymentMethod.and.returnValue(
      Promise.resolve({ success: true, paymentMethodId: 'pm_captured_1' })
    );
    paymentService.createPaymentIntent.and.returnValue(
      of({ clientSecret: 'cs_review_1' } as PaymentIntentResponse)
    );
    paymentService.confirmPayment.and.returnValue(
      Promise.resolve({ success: true, paymentIntentId: 'pi_ok_1' })
    );
    // The component's success callback ignores the emitted order, so an empty cast suffices.
    checkoutService.createOrder.and.returnValue(of({} as unknown as Order));

    await component.proceedFromPayment();
    await component.placeOrder();

    expect(paymentService.confirmPayment).toHaveBeenCalledTimes(1);
    const args = paymentService.confirmPayment.calls.mostRecent().args;
    expect(args[0]).toBe('cs_review_1');
    expect(args[2]).toBe('pm_captured_1');
  });

  it('surfaces the error and does NOT advance to review when capture fails (card)', async () => {
    buildComponent('card');
    paymentService.createCardPaymentMethod.and.returnValue(
      Promise.resolve({ success: false, error: 'checkout.payment.errors.cardDeclined' })
    );

    await component.proceedFromPayment();

    expect(checkoutService.setError).toHaveBeenCalledWith('checkout.payment.errors.cardDeclined');
    // Must stay on the payment step: never advance to review.
    expect(checkoutService.setStep).not.toHaveBeenCalledWith('review');
  });

  it('falls back to checkout.payment.errors.failed when capture fails without a translation key', async () => {
    buildComponent('card');
    paymentService.createCardPaymentMethod.and.returnValue(
      Promise.resolve({ success: false, error: 'Your card number is incomplete.' })
    );

    await component.proceedFromPayment();

    expect(checkoutService.setError).toHaveBeenCalledWith('checkout.payment.errors.failed');
    expect(checkoutService.setStep).not.toHaveBeenCalledWith('review');
  });

  it('goes straight to review without capturing a card method when bank is selected', async () => {
    buildComponent('bank');

    await component.proceedFromPayment();

    expect(paymentService.createCardPaymentMethod).not.toHaveBeenCalled();
    expect(checkoutService.setStep).toHaveBeenCalledWith('review');
  });
});

/**
 * PAY-IDEM: each place-order ATTEMPT must forward a FRESH idempotency key (the 3rd arg of
 * createPaymentIntent). A network retry of one POST reuses the same key (Stripe dedupes); a *user*
 * retry after a failure is a new click -> a new key -> a fresh intent, so a refunded charge is never
 * replayed (the [High]). This guards against a refactor that hoists/caches the attempt key.
 */
describe('CheckoutComponent - per-attempt idempotency key rotation (PAY-IDEM)', () => {
  let component: CheckoutComponent;
  let paymentService: jasmine.SpyObj<PaymentService>;
  let checkoutService: jasmine.SpyObj<CheckoutService>;

  beforeEach(() => {
    paymentService = jasmine.createSpyObj<PaymentService>(
      'PaymentService',
      ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements', 'loadConfig']
    );
    paymentService.loadConfig.and.returnValue(Promise.resolve(null));
    // Resolve create-intent with NO client secret so placeOrder() bails early (no Stripe.js needed),
    // releasing the in-flight guard so the next attempt re-enters and calls create-intent again.
    paymentService.createPaymentIntent.and.returnValue(of({ clientSecret: '' } as PaymentIntentResponse));

    checkoutService = jasmine.createSpyObj<CheckoutService>(
      'CheckoutService',
      ['createOrder', 'setError', 'setStep', 'setShippingAddress', 'setPaymentMethod', 'setShippingMethod', 'getSessionId', 'paymentMethod', 'shippingMethod', 'shippingAddress', 'lastOrderId', 'isProcessing']
    );
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

  it('forwards a fresh, non-empty idempotency key on each place-order attempt', async () => {
    // Two separate user attempts (each releases the guard via the early bail-out above).
    await component.placeOrder();
    await component.placeOrder();

    const calls = paymentService.createPaymentIntent.calls.all();
    expect(calls.length).toBe(2);

    const firstKey = calls[0].args[2] as string;
    const secondKey = calls[1].args[2] as string;

    expect(typeof firstKey).toBe('string');
    expect(firstKey.length).toBeGreaterThan(0);
    expect(secondKey.length).toBeGreaterThan(0);
    // Per-attempt rotation: the two attempts must NOT share a key (closes the refund-replay [High]).
    expect(firstKey).not.toBe(secondKey);
  });
});

/**
 * B-014 / B-042: accessible radiogroups on checkout. This block renders the REAL template (unlike the
 * logic-only blocks above) to assert the DOM contract:
 *  - B-014: the shipping-method and payment-method sets are role=radiogroup with an accessible name,
 *    and their native <input type=radio> are focusable (sr-only, NOT display:none) so keyboard/SR
 *    users can change the method; selecting via the radio updates the model.
 *  - B-042: the saved-address cards are a role=radiogroup of role=radio with aria-checked + roving
 *    tabindex, and a keydown handler that selects on Enter/Space and moves+selects on arrow keys.
 */
describe('CheckoutComponent - accessible radiogroups (B-014 / B-042)', () => {
  let fixture: ComponentFixture<CheckoutComponent>;
  let component: CheckoutComponent;
  let checkoutService: {
    currentStep: ReturnType<typeof signal<CheckoutStep>>;
    shippingMethod: ReturnType<typeof signal<string>>;
    paymentMethod: ReturnType<typeof signal<string>>;
    shippingAddress: ReturnType<typeof signal<null>>;
    error: ReturnType<typeof signal<string | null>>;
    isProcessing: ReturnType<typeof signal<boolean>>;
    lastOrderId: ReturnType<typeof signal<string | null>>;
    setShippingMethod: jasmine.Spy;
    setPaymentMethod: jasmine.Spy;
    setStep: jasmine.Spy;
    setError: jasmine.Spy;
    getSessionId: () => string;
  };

  const translations: Record<string, string> = {
    'checkout.shipping.method': 'Shipping Method',
    'checkout.payment.title': 'Payment Method',
    'checkout.shipping.useSavedAddress': 'Use a saved address'
  };

  class FakeTranslateLoader implements TranslateLoader {
    getTranslation(_lang: string): Observable<Record<string, string>> {
      return of(translations);
    }
  }

  const addr1: SavedAddress = {
    id: 'addr-1', fullName: 'Alice Example', addressLine1: '1 First St', city: 'Sofia',
    postalCode: '1000', country: 'Bulgaria', countryCode: 'BG', isDefault: true, type: 'Shipping',
    createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z'
  };
  const addr2: SavedAddress = {
    id: 'addr-2', fullName: 'Bob Example', addressLine1: '2 Second St', city: 'Plovdiv',
    postalCode: '4000', country: 'Bulgaria', countryCode: 'BG', isDefault: false, type: 'Shipping',
    createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z'
  };

  beforeEach(async () => {
    checkoutService = {
      currentStep: signal<CheckoutStep>('payment'),
      shippingMethod: signal('standard'),
      paymentMethod: signal('bank'),
      shippingAddress: signal(null),
      error: signal<string | null>(null),
      isProcessing: signal(false),
      lastOrderId: signal<string | null>(null),
      setShippingMethod: jasmine.createSpy('setShippingMethod'),
      setPaymentMethod: jasmine.createSpy('setPaymentMethod'),
      setStep: jasmine.createSpy('setStep'),
      setError: jasmine.createSpy('setError'),
      getSessionId: () => 'session-1'
    };

    const cartService = {
      loadFailed: signal(false),
      error: signal<string | null>(null),
      isEmpty: signal(false),
      items: signal<unknown[]>([]),
      subtotal: signal(0),
      cart: signal({ tax: 0 }),
      loadCart: jasmine.createSpy('loadCart'),
      clearCart: jasmine.createSpy('clearCart').and.returnValue(of(void 0))
    };

    const addressService = {
      hasAddresses: signal(true),
      addresses: signal<SavedAddress[]>([addr1, addr2]),
      loadAddresses: jasmine.createSpy('loadAddresses')
    };

    const authService = { isAuthenticated: signal(true) };

    const paymentService = {
      isInitialized: signal(false),
      error: signal<string | null>(null),
      bankTransfer: signal(null),
      initialize: jasmine.createSpy('initialize').and.returnValue(Promise.resolve(false)),
      createElements: jasmine.createSpy('createElements'),
      destroyElements: jasmine.createSpy('destroyElements'),
      loadConfig: jasmine.createSpy('loadConfig').and.returnValue(Promise.resolve(null))
    };

    const confettiService = jasmine.createSpyObj<ConfettiService>('ConfettiService', ['burst', 'stop']);

    await TestBed.configureTestingModule({
      imports: [
        CheckoutComponent,
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeTranslateLoader } })
      ],
      providers: [
        provideRouter([]),
        { provide: CartService, useValue: cartService },
        { provide: CheckoutService, useValue: checkoutService },
        { provide: AddressService, useValue: addressService },
        { provide: AuthService, useValue: authService },
        { provide: PaymentService, useValue: paymentService },
        { provide: ConfettiService, useValue: confettiService }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(CheckoutComponent);
    component = fixture.componentInstance;
  });

  // ---- B-014: method radiogroups ----
  describe('B-014: shipping + payment method radiogroups', () => {
    beforeEach(() => {
      checkoutService.currentStep.set('payment');
      fixture.detectChanges();
    });

    it('renders both method sets as role=radiogroup with an accessible name', () => {
      const shippingGroup = fixture.debugElement.query(By.css('[data-testid="shipping-methods"]'));
      const paymentGroup = fixture.debugElement.query(By.css('[data-testid="payment-methods"]'));

      expect(shippingGroup.nativeElement.getAttribute('role')).toBe('radiogroup');
      expect(paymentGroup.nativeElement.getAttribute('role')).toBe('radiogroup');

      // Each group is named by its visible heading (aria-labelledby → an element that exists).
      const shippingLabelId = shippingGroup.nativeElement.getAttribute('aria-labelledby');
      const paymentLabelId = paymentGroup.nativeElement.getAttribute('aria-labelledby');
      expect(shippingLabelId).toBeTruthy();
      expect(paymentLabelId).toBeTruthy();
      expect(document.getElementById(shippingLabelId!)?.textContent).toContain('Shipping Method');
      expect(fixture.nativeElement.querySelector('#' + paymentLabelId)?.textContent).toContain('Payment Method');
    });

    it('keeps the native radios focusable (sr-only, NOT display:none)', () => {
      const radio = fixture.debugElement.query(By.css('input[data-testid="shipping-standard"]')).nativeElement as HTMLInputElement;
      expect(radio.type).toBe('radio');
      // The regression: display:none removes the radio from the tab order + a11y tree.
      expect(getComputedStyle(radio).display).not.toBe('none');
      expect(getComputedStyle(radio).visibility).not.toBe('hidden');
    });

    it('updates the model when a method radio is selected', () => {
      const express = fixture.debugElement.query(By.css('input[data-testid="shipping-express"]')).nativeElement as HTMLInputElement;
      express.click();
      expect(checkoutService.setShippingMethod).toHaveBeenCalledWith('express');

      // 'bank' is the preselected method here, so toggle to the currently-unchecked 'card' radio —
      // clicking an already-checked radio fires no change event.
      const card = fixture.debugElement.query(By.css('input[data-testid="payment-card"]')).nativeElement as HTMLInputElement;
      card.click();
      expect(checkoutService.setPaymentMethod).toHaveBeenCalledWith('card');
    });
  });

  // ---- B-042: saved-address radiogroup ----
  describe('B-042: saved-address cards as a keyboard radiogroup', () => {
    beforeEach(() => {
      checkoutService.currentStep.set('shipping');
      fixture.detectChanges();
    });

    function cards(): HTMLElement[] {
      return fixture.debugElement.queryAll(By.css('[data-testid="saved-address-card"]')).map(d => d.nativeElement);
    }

    it('renders a role=radiogroup of role=radio named by its visible heading (WCAG 2.5.3)', () => {
      const group = fixture.debugElement.query(By.css('.saved-addresses-list')).nativeElement as HTMLElement;
      expect(group.getAttribute('role')).toBe('radiogroup');
      // Accessible name comes from the visible <h3> (aria-labelledby), so name == visible label.
      const labelId = group.getAttribute('aria-labelledby');
      expect(labelId).toBeTruthy();
      expect(document.getElementById(labelId!)?.textContent).toContain('Use a saved address');

      const all = cards();
      expect(all.length).toBe(2);
      for (const card of all) {
        expect(card.getAttribute('role')).toBe('radio');
        expect(card.getAttribute('aria-checked')).not.toBeNull();
      }
    });

    it('uses roving tabindex — the first card is reachable when nothing is selected', () => {
      const all = cards();
      expect(all[0].getAttribute('tabindex')).toBe('0');
      expect(all[1].getAttribute('tabindex')).toBe('-1');
    });

    it('selects the focused card on Enter and reflects aria-checked', () => {
      const all = cards();
      all[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      fixture.detectChanges();

      expect(component.selectedAddressId()).toBe('addr-1');
      expect(cards()[0].getAttribute('aria-checked')).toBe('true');
      // Roving tabindex now follows the selection.
      expect(cards()[0].getAttribute('tabindex')).toBe('0');
      expect(cards()[1].getAttribute('tabindex')).toBe('-1');
    });

    it('selects the focused card on Space', () => {
      const all = cards();
      all[1].dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
      fixture.detectChanges();

      expect(component.selectedAddressId()).toBe('addr-2');
      expect(cards()[1].getAttribute('aria-checked')).toBe('true');
    });

    it('moves + selects the next card on ArrowDown (wrap-around)', () => {
      const all = cards();
      all[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
      fixture.detectChanges();
      expect(component.selectedAddressId()).toBe('addr-2');

      // Wrap from the last back to the first.
      cards()[1].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
      fixture.detectChanges();
      expect(component.selectedAddressId()).toBe('addr-1');
    });

    it('moves + selects the previous card on ArrowUp (wrap-around)', () => {
      const all = cards();
      all[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
      fixture.detectChanges();
      // From the first, ArrowUp wraps to the last.
      expect(component.selectedAddressId()).toBe('addr-2');
    });
  });
});
