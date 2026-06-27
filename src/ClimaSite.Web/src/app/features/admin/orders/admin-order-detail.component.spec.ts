import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AdminOrderDetailComponent } from './admin-order-detail.component';
import {
  AdminOrdersService,
  AdminOrderDetail
} from '../../../core/services/admin-orders.service';

/**
 * Plan-19 B2 (batch 2): unit coverage for the admin order-detail page.
 *
 * The component owns its display + form state in signals and plain fields, and drives the
 * backend through AdminOrdersService. We provide a jasmine spy double for that service plus a
 * minimal ActivatedRoute snapshot, and instantiate the component WITHOUT rendering the template
 * (the template imports RouterLink + ngx-translate and a currency pipe we don't want to set up),
 * mirroring checkout.component.spec.ts / cart.component.spec.ts.
 *
 * Each `next`/`error` callback resolves synchronously off the spy observables, so no fakeAsync
 * is needed — the subscribe completes within the method call.
 */

function makeOrder(overrides: Partial<AdminOrderDetail> = {}): AdminOrderDetail {
  return {
    id: 'order-1',
    orderNumber: 'CS-1001',
    userId: 'user-1',
    customerName: 'Jane Buyer',
    customerEmail: 'jane@test.com',
    customerPhone: '+359888000000',
    status: 'Paid',
    subtotal: 100,
    shippingCost: 5.99,
    taxAmount: 20,
    discountAmount: 0,
    totalAmount: 125.99,
    currency: 'EUR',
    shippingAddress: null,
    billingAddress: null,
    shippingMethod: 'standard',
    trackingNumber: null,
    paymentMethod: 'card',
    paidAt: '2026-06-01T10:00:00Z',
    shippedAt: null,
    deliveredAt: null,
    cancelledAt: null,
    cancellationReason: null,
    notes: null,
    items: [],
    createdAt: '2026-06-01T09:00:00Z',
    updatedAt: '2026-06-01T10:00:00Z',
    ...overrides
  };
}

interface SetupOptions {
  routeId?: string | null;
}

function setup(opts: SetupOptions = {}): {
  component: AdminOrderDetailComponent;
  ordersService: jasmine.SpyObj<AdminOrdersService>;
} {
  const routeId = 'routeId' in opts ? opts.routeId : 'order-1';

  const ordersService = jasmine.createSpyObj<AdminOrdersService>('AdminOrdersService', [
    'getOrder',
    'updateStatus',
    'updateShipping',
    'addNote'
  ]);
  ordersService.getOrder.and.returnValue(of(makeOrder()));
  ordersService.updateStatus.and.returnValue(of(void 0));
  ordersService.updateShipping.and.returnValue(of(void 0));
  ordersService.addNote.and.returnValue(of({ message: 'ok' }));

  TestBed.configureTestingModule({
    providers: [
      { provide: AdminOrdersService, useValue: ordersService },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            paramMap: {
              get: () => routeId
            }
          }
        }
      }
    ]
  });

  const component = TestBed.runInInjectionContext(() => new AdminOrderDetailComponent());
  return { component, ordersService };
}

describe('AdminOrderDetailComponent', () => {
  it('creates', () => {
    const { component } = setup();
    expect(component).toBeTruthy();
  });

  describe('ngOnInit / loadOrder', () => {
    it('reads the id from the route and loads the order, hydrating the shipping form', () => {
      const { component, ordersService } = setup();
      ordersService.getOrder.and.returnValue(
        of(makeOrder({ trackingNumber: 'TRK-9', shippingMethod: 'express' }))
      );

      component.ngOnInit();

      expect(ordersService.getOrder).toHaveBeenCalledOnceWith('order-1');
      expect(component.order()?.orderNumber).toBe('CS-1001');
      expect(component.loading()).toBeFalse();
      expect(component.loadError()).toBeNull();
      // Shipping form is seeded from the loaded order.
      expect(component.trackingNumber).toBe('TRK-9');
      expect(component.shippingMethod).toBe('express');
      // The status select is reset to the empty placeholder.
      expect(component.selectedStatus).toBe('');
    });

    it('seeds tracking/shipping fields to empty strings when the order has none', () => {
      const { component, ordersService } = setup();
      ordersService.getOrder.and.returnValue(
        of(makeOrder({ trackingNumber: null, shippingMethod: null }))
      );

      component.ngOnInit();

      expect(component.trackingNumber).toBe('');
      expect(component.shippingMethod).toBe('');
    });

    it('sets a load-error key (and never calls the API) when the route has no id', () => {
      const { component, ordersService } = setup({ routeId: null });

      component.ngOnInit();

      expect(ordersService.getOrder).not.toHaveBeenCalled();
      expect(component.loadError()).toBe('admin.orders.errors.loadFailed');
      // loading() was never flipped on for the no-id short-circuit.
      expect(component.loading()).toBeFalse();
    });

    it('surfaces a load-error key and clears loading when the API errors', () => {
      const { component, ordersService } = setup();
      ordersService.getOrder.and.returnValue(throwError(() => new Error('500')));

      component.ngOnInit();

      expect(component.loadError()).toBe('admin.orders.errors.loadFailed');
      expect(component.loading()).toBeFalse();
      expect(component.order()).toBeNull();
    });

    it('clears a prior load error on a successful retry', () => {
      const { component, ordersService } = setup();
      ordersService.getOrder.and.returnValue(throwError(() => new Error('500')));
      component.ngOnInit();
      expect(component.loadError()).toBe('admin.orders.errors.loadFailed');

      // Retry succeeds.
      ordersService.getOrder.and.returnValue(of(makeOrder()));
      component.loadOrder();

      expect(component.loadError()).toBeNull();
      expect(component.order()).not.toBeNull();
    });
  });

  describe('validNextStatuses (computed transition guard)', () => {
    it('exposes the valid transitions for the current status', () => {
      const { component } = setup();
      component.order.set(makeOrder({ status: 'Paid' }));
      // Mirrors ORDER_STATUS_TRANSITIONS.Paid.
      expect(component.validNextStatuses()).toEqual(['Processing', 'Refunded', 'Cancelled']);
    });

    it('returns an empty list for terminal statuses (Cancelled)', () => {
      const { component } = setup();
      component.order.set(makeOrder({ status: 'Cancelled' }));
      expect(component.validNextStatuses()).toEqual([]);
    });

    it('returns an empty list when there is no loaded order', () => {
      const { component } = setup();
      component.order.set(null);
      expect(component.validNextStatuses()).toEqual([]);
    });
  });

  describe('applyStatus', () => {
    it('sends the trimmed note + notify flag, then refetches and shows success', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();

      component.selectedStatus = 'Processing';
      component.statusNote = '  packed and ready  ';
      component.notifyCustomer = true;

      component.applyStatus();

      expect(ordersService.updateStatus).toHaveBeenCalledOnceWith('order-1', {
        status: 'Processing',
        note: 'packed and ready',
        notifyCustomer: true
      });
      // On success the note is cleared, success surfaced, and the order refetched.
      expect(component.statusNote).toBe('');
      expect(component.actionSuccess()).toBe('admin.orders.toasts.statusUpdated');
      expect(component.actionError()).toBeNull();
      expect(component.saving()).toBeFalse();
      expect(ordersService.getOrder).toHaveBeenCalledTimes(1);
    });

    it('omits the note (sends undefined) when it is blank/whitespace', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      component.selectedStatus = 'Processing';
      component.statusNote = '   ';
      component.notifyCustomer = false;

      component.applyStatus();

      expect(ordersService.updateStatus).toHaveBeenCalledOnceWith('order-1', {
        status: 'Processing',
        note: undefined,
        notifyCustomer: false
      });
    });

    it('does nothing when no status is selected', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      component.selectedStatus = '';

      component.applyStatus();

      expect(ordersService.updateStatus).not.toHaveBeenCalled();
    });

    it('surfaces a status-failed error, keeps the note, and does NOT refetch on failure', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();
      ordersService.updateStatus.and.returnValue(throwError(() => new Error('500')));

      component.selectedStatus = 'Processing';
      component.statusNote = 'keep me';
      component.applyStatus();

      expect(component.actionError()).toBe('admin.orders.errors.statusFailed');
      expect(component.actionSuccess()).toBeNull();
      expect(component.saving()).toBeFalse();
      // Note is preserved so the admin can retry, and no refetch happened.
      expect(component.statusNote).toBe('keep me');
      expect(ordersService.getOrder).not.toHaveBeenCalled();
    });

    it('clears any prior action error/success when a new action starts (startAction)', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      component.actionError.set('admin.orders.errors.statusFailed');
      component.actionSuccess.set('admin.orders.toasts.statusUpdated');

      // Hold the observable open so we can assert the in-flight (post-startAction) state.
      ordersService.updateStatus.and.returnValue(throwError(() => new Error('x')));
      component.selectedStatus = 'Processing';
      component.applyStatus();

      // After a failure, error is set fresh and the stale success was cleared.
      expect(component.actionSuccess()).toBeNull();
      expect(component.actionError()).toBe('admin.orders.errors.statusFailed');
    });
  });

  describe('saveShipping', () => {
    it('sends trimmed tracking/method + markAsShipped, resets the flag, and refetches on success', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();

      component.trackingNumber = '  TRK-123  ';
      component.shippingMethod = '  DHL  ';
      component.markAsShipped = true;

      component.saveShipping();

      expect(ordersService.updateShipping).toHaveBeenCalledOnceWith('order-1', {
        trackingNumber: 'TRK-123',
        shippingMethod: 'DHL',
        markAsShipped: true
      });
      expect(component.markAsShipped).toBeFalse();
      expect(component.actionSuccess()).toBe('admin.orders.toasts.shippingUpdated');
      expect(component.saving()).toBeFalse();
      expect(ordersService.getOrder).toHaveBeenCalledTimes(1);
    });

    it('sends undefined for empty tracking/method fields', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      component.trackingNumber = '';
      component.shippingMethod = '   ';
      component.markAsShipped = false;

      component.saveShipping();

      expect(ordersService.updateShipping).toHaveBeenCalledOnceWith('order-1', {
        trackingNumber: undefined,
        shippingMethod: undefined,
        markAsShipped: false
      });
    });

    it('surfaces a shipping-failed error and does NOT refetch on failure', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();
      ordersService.updateShipping.and.returnValue(throwError(() => new Error('500')));

      component.saveShipping();

      expect(component.actionError()).toBe('admin.orders.errors.shippingFailed');
      expect(component.actionSuccess()).toBeNull();
      expect(component.saving()).toBeFalse();
      expect(ordersService.getOrder).not.toHaveBeenCalled();
    });
  });

  describe('addNote', () => {
    it('sends the trimmed note, clears the field, and refetches on success', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();

      component.newNote = '  please call before delivery  ';
      component.addNote();

      expect(ordersService.addNote).toHaveBeenCalledOnceWith('order-1', 'please call before delivery');
      expect(component.newNote).toBe('');
      expect(component.actionSuccess()).toBe('admin.orders.toasts.noteAdded');
      expect(component.saving()).toBeFalse();
      expect(ordersService.getOrder).toHaveBeenCalledTimes(1);
    });

    it('does nothing for a blank/whitespace note', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      component.newNote = '   ';

      component.addNote();

      expect(ordersService.addNote).not.toHaveBeenCalled();
    });

    it('surfaces a note-failed error and preserves the typed note on failure', () => {
      const { component, ordersService } = setup();
      component.ngOnInit();
      ordersService.getOrder.calls.reset();
      ordersService.addNote.and.returnValue(throwError(() => new Error('500')));

      component.newNote = 'keep this';
      component.addNote();

      expect(component.actionError()).toBe('admin.orders.errors.noteFailed');
      expect(component.newNote).toBe('keep this');
      expect(component.saving()).toBeFalse();
      expect(ordersService.getOrder).not.toHaveBeenCalled();
    });
  });

  describe('address formatting (shippingAddressLines / billingAddressLines)', () => {
    it('returns no lines for a null address', () => {
      const { component } = setup();
      component.order.set(makeOrder({ shippingAddress: null }));
      expect(component.shippingAddressLines()).toEqual([]);
    });

    it('joins first+last into a single name line, then appends present fields in order', () => {
      const { component } = setup();
      component.order.set(
        makeOrder({
          shippingAddress: {
            firstName: 'Jane',
            lastName: 'Buyer',
            addressLine1: '1 Cooling St',
            addressLine2: 'Apt 4',
            city: 'Sofia',
            state: 'Sofia-grad',
            postalCode: '1000',
            country: 'BG',
            phone: '+359888'
          }
        })
      );

      expect(component.shippingAddressLines()).toEqual([
        'Jane Buyer',
        '1 Cooling St',
        'Apt 4',
        'Sofia',
        'Sofia-grad',
        '1000',
        'BG',
        '+359888'
      ]);
    });

    it('skips empty/whitespace and non-string null fields', () => {
      const { component } = setup();
      component.order.set(
        makeOrder({
          billingAddress: {
            firstName: 'Solo',
            lastName: '',
            addressLine1: '  ',
            city: 'Plovdiv',
            postalCode: null,
            country: undefined
          }
        })
      );

      // Only the first name (no last name) and the non-empty city survive.
      expect(component.billingAddressLines()).toEqual(['Solo', 'Plovdiv']);
    });

    it('coerces non-string values to trimmed strings', () => {
      const { component } = setup();
      component.order.set(
        makeOrder({
          shippingAddress: {
            postalCode: 1000,
            city: 'Varna'
          }
        })
      );

      expect(component.shippingAddressLines()).toEqual(['Varna', '1000']);
    });
  });
});
