import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PaymentService, PaymentIntentResponse } from './payment.service';
import { environment } from '../../../environments/environment';

describe('PaymentService', () => {
  let service: PaymentService;
  let httpMock: HttpTestingController;
  const createIntentUrl = `${environment.apiUrl}/api/payments/create-intent`;

  const mockIntentResponse: PaymentIntentResponse = {
    paymentIntentId: 'pi_test_123',
    clientSecret: 'pi_test_123_secret',
    amount: 1234.56,
    currency: 'EUR'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PaymentService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(PaymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('createCardPaymentMethod', () => {
    it('creates a card PaymentMethod and returns its id on success', async () => {
      const createPaymentMethod = jasmine.createSpy('createPaymentMethod')
        .and.returnValue(Promise.resolve({ paymentMethod: { id: 'pm_test_456' } }));
      const mockCardElement = {} as unknown;
      (service as unknown as { stripe: unknown }).stripe = { createPaymentMethod };
      (service as unknown as { cardElement: unknown }).cardElement = mockCardElement;

      const result = await service.createCardPaymentMethod({ name: 'Jane Buyer' });

      expect(result.success).toBeTrue();
      expect(result.paymentMethodId).toBe('pm_test_456');
      expect(createPaymentMethod).toHaveBeenCalledTimes(1);
      const arg = createPaymentMethod.calls.mostRecent().args[0];
      expect(arg.type).toBe('card');
      expect(arg.card).toBe(mockCardElement);
      expect(arg.billing_details).toEqual({ name: 'Jane Buyer' });
    });

    it('returns a mapped translation key and success=false when Stripe returns an error', async () => {
      const createPaymentMethod = jasmine.createSpy('createPaymentMethod')
        .and.returnValue(Promise.resolve({ error: { message: 'checkout.payment.errors.cardDeclined' } }));
      (service as unknown as { stripe: unknown }).stripe = { createPaymentMethod };
      (service as unknown as { cardElement: unknown }).cardElement = {};

      const result = await service.createCardPaymentMethod();

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.cardDeclined');
      expect(result.paymentMethodId).toBeUndefined();
    });

    it('falls back to checkout.payment.errors.failed when the Stripe error is not a translation key', async () => {
      const createPaymentMethod = jasmine.createSpy('createPaymentMethod')
        .and.returnValue(Promise.resolve({ error: { message: 'Your card number is incomplete.' } }));
      (service as unknown as { stripe: unknown }).stripe = { createPaymentMethod };
      (service as unknown as { cardElement: unknown }).cardElement = {};

      const result = await service.createCardPaymentMethod();

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.failed');
    });

    it('returns notInitialized when stripe is null', async () => {
      (service as unknown as { stripe: unknown }).stripe = null;
      (service as unknown as { cardElement: unknown }).cardElement = {};

      const result = await service.createCardPaymentMethod();

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.notInitialized');
    });

    it('returns notInitialized when the card element is null', async () => {
      (service as unknown as { stripe: unknown }).stripe = { createPaymentMethod: jasmine.createSpy() };
      (service as unknown as { cardElement: unknown }).cardElement = null;

      const result = await service.createCardPaymentMethod();

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.notInitialized');
    });
  });

  describe('confirmPayment', () => {
    it('confirms with a pre-created payment method id even when the card element is null', async () => {
      const confirmCardPayment = jasmine.createSpy('confirmCardPayment')
        .and.returnValue(Promise.resolve({ paymentIntent: { id: 'pi_done_1', status: 'succeeded' } }));
      (service as unknown as { stripe: unknown }).stripe = { confirmCardPayment };
      // The live element is gone on the review step - confirming by id must still work.
      (service as unknown as { cardElement: unknown }).cardElement = null;

      const result = await service.confirmPayment('cs_test_1', { name: 'Jane' }, 'pm_test_456');

      expect(result.success).toBeTrue();
      expect(result.paymentIntentId).toBe('pi_done_1');
      expect(confirmCardPayment).toHaveBeenCalledWith('cs_test_1', { payment_method: 'pm_test_456' });
    });

    it('confirms with the live card element when no payment method id is given', async () => {
      const confirmCardPayment = jasmine.createSpy('confirmCardPayment')
        .and.returnValue(Promise.resolve({ paymentIntent: { id: 'pi_done_2', status: 'succeeded' } }));
      const mockCardElement = {} as unknown;
      (service as unknown as { stripe: unknown }).stripe = { confirmCardPayment };
      (service as unknown as { cardElement: unknown }).cardElement = mockCardElement;

      const billing = { name: 'Jane' };
      const result = await service.confirmPayment('cs_test_2', billing);

      expect(result.success).toBeTrue();
      expect(result.paymentIntentId).toBe('pi_done_2');
      expect(confirmCardPayment).toHaveBeenCalledWith('cs_test_2', {
        payment_method: { card: mockCardElement, billing_details: billing }
      });
    });

    it('returns notInitialized when stripe is null', async () => {
      (service as unknown as { stripe: unknown }).stripe = null;
      (service as unknown as { cardElement: unknown }).cardElement = {};

      const result = await service.confirmPayment('cs_test_3', undefined, 'pm_test_456');

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.notInitialized');
    });

    it('returns notInitialized when no id is given and the card element is null', async () => {
      (service as unknown as { stripe: unknown }).stripe = { confirmCardPayment: jasmine.createSpy() };
      (service as unknown as { cardElement: unknown }).cardElement = null;

      const result = await service.confirmPayment('cs_test_4');

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.notInitialized');
    });

    it('returns a mapped error when Stripe rejects the confirmation', async () => {
      const confirmCardPayment = jasmine.createSpy('confirmCardPayment')
        .and.returnValue(Promise.resolve({ error: { message: 'checkout.payment.errors.cardDeclined' } }));
      (service as unknown as { stripe: unknown }).stripe = { confirmCardPayment };
      (service as unknown as { cardElement: unknown }).cardElement = null;

      const result = await service.confirmPayment('cs_test_5', undefined, 'pm_test_456');

      expect(result.success).toBeFalse();
      expect(result.error).toBe('checkout.payment.errors.cardDeclined');
    });
  });

  describe('createPaymentIntent', () => {
    it('should POST only the shipping method and guest session id (server computes the amount)', () => {
      service.createPaymentIntent('express', 'sess_guest_1').subscribe(res => {
        expect(res).toEqual(mockIntentResponse);
      });

      const req = httpMock.expectOne(createIntentUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        shippingMethod: 'express',
        guestSessionId: 'sess_guest_1'
      });
      // The client must NOT send an amount or currency anymore (BUG-02).
      expect(req.request.body.amount).toBeUndefined();
      expect(req.request.body.currency).toBeUndefined();
      req.flush(mockIntentResponse);
    });

    it('should omit the guest session id when not provided', () => {
      service.createPaymentIntent('standard').subscribe();

      const req = httpMock.expectOne(createIntentUrl);
      expect(req.request.body.shippingMethod).toBe('standard');
      expect(req.request.body.guestSessionId).toBeUndefined();
      req.flush(mockIntentResponse);
    });

    it('should include the per-attempt idempotency key in the POST body when provided', () => {
      service.createPaymentIntent('express', 'sess_guest_2', 'attempt-key-123').subscribe();

      const req = httpMock.expectOne(createIntentUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.idempotencyKey).toBe('attempt-key-123');
      expect(req.request.body.shippingMethod).toBe('express');
      expect(req.request.body.guestSessionId).toBe('sess_guest_2');
      req.flush(mockIntentResponse);
    });

    it('should not include an idempotency key field when none is provided', () => {
      service.createPaymentIntent('standard', 'sess_guest_3').subscribe();

      const req = httpMock.expectOne(createIntentUrl);
      expect('idempotencyKey' in req.request.body).toBeFalse();
      req.flush(mockIntentResponse);
    });

    it('should expose the server-computed amount and currency on the response', () => {
      let result: PaymentIntentResponse | undefined;
      service.createPaymentIntent('standard').subscribe(res => { result = res; });

      const req = httpMock.expectOne(createIntentUrl);
      req.flush(mockIntentResponse);

      expect(result?.amount).toBe(1234.56);
      expect(result?.currency).toBe('EUR');
    });
  });
});
