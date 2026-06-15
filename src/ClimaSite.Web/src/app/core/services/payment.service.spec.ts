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
