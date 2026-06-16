import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
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
      ['createPaymentIntent', 'confirmPayment', 'destroyElements', 'initialize', 'createElements']
    );
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
