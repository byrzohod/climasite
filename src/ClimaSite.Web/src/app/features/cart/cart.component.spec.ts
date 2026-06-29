import { WritableSignal, signal } from '@angular/core';
import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { TranslateService, TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { CartComponent } from './cart.component';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { Cart, CartItem } from '../../core/models/cart.model';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

/**
 * Plan-19 B2: unit coverage for the cart page's quantity / removal / totals logic.
 *
 * The component reads its display state straight off CartService signals (items,
 * subtotal, total, cart, isEmpty, isLoading, error) and mutates the backend through
 * CartService methods, so we provide a CartService double built from writable signals
 * + jasmine method spies. The component is instantiated without rendering the template
 * (it imports app-empty-state / RevealDirective and uses ngx-translate), matching the
 * approach in checkout.component.spec.ts. removeItem() waits 300ms for an exit
 * animation, so those paths use fakeAsync + tick.
 */

interface CartServiceStub {
  items: WritableSignal<CartItem[]>;
  cart: WritableSignal<Cart | null>;
  subtotal: WritableSignal<number>;
  total: WritableSignal<number>;
  itemCount: WritableSignal<number>;
  isEmpty: WritableSignal<boolean>;
  isLoading: WritableSignal<boolean>;
  error: WritableSignal<string | null>;
  loadFailed: WritableSignal<boolean>;
  loadCart: jasmine.Spy;
  updateQuantity: jasmine.Spy;
  removeItem: jasmine.Spy;
  addToCart: jasmine.Spy;
}

function makeItem(overrides: Partial<CartItem> = {}): CartItem {
  return {
    id: 'item-1',
    productId: 'product-1',
    productName: 'Test AC Unit',
    productSlug: 'test-ac-unit',
    sku: 'TEST-001',
    unitPrice: 99.99,
    quantity: 2,
    subtotal: 199.98,
    maxQuantity: 10,
    ...overrides
  };
}

describe('CartComponent', () => {
  let component: CartComponent;
  let cartService: CartServiceStub;
  let toastService: jasmine.SpyObj<Pick<ToastService, 'success' | 'error'>>;
  let translate: jasmine.SpyObj<Pick<TranslateService, 'instant'>>;

  beforeEach(() => {
    cartService = {
      items: signal<CartItem[]>([]),
      cart: signal<Cart | null>(null),
      subtotal: signal(0),
      total: signal(0),
      itemCount: signal(0),
      isEmpty: signal(true),
      isLoading: signal(false),
      error: signal<string | null>(null),
      loadFailed: signal(false),
      loadCart: jasmine.createSpy('loadCart'),
      updateQuantity: jasmine.createSpy('updateQuantity').and.returnValue(of(undefined)),
      removeItem: jasmine.createSpy('removeItem').and.returnValue(of(undefined)),
      addToCart: jasmine.createSpy('addToCart').and.returnValue(of(undefined))
    };

    toastService = jasmine.createSpyObj<Pick<ToastService, 'success' | 'error'>>('ToastService', ['success', 'error']);
    translate = jasmine.createSpyObj<Pick<TranslateService, 'instant'>>('TranslateService', ['instant']);
    translate.instant.and.callFake((key: string | string[]) => key as string);

    TestBed.configureTestingModule({
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: ToastService, useValue: toastService },
        { provide: TranslateService, useValue: translate }
      ]
    });

    component = TestBed.runInInjectionContext(() => new CartComponent());
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('always reloads the cart so the page shows fresh data after login/reorder', () => {
      component.ngOnInit();
      expect(cartService.loadCart).toHaveBeenCalledTimes(1);
    });
  });

  describe('state branches exposed to the template', () => {
    it('reports an empty cart by default', () => {
      expect(component.cartService.isEmpty()).toBeTrue();
      expect(component.cartService.items()).toEqual([]);
    });

    it('reflects a populated cart with totals from the service', () => {
      cartService.items.set([makeItem()]);
      cartService.isEmpty.set(false);
      cartService.subtotal.set(199.98);
      cartService.total.set(229.98);
      cartService.cart.set({ shipping: 10, tax: 20 } as Cart);

      expect(component.cartService.isEmpty()).toBeFalse();
      expect(component.cartService.items().length).toBe(1);
      expect(component.cartService.subtotal()).toBe(199.98);
      expect(component.cartService.total()).toBe(229.98);
      expect(component.cartService.cart()?.shipping).toBe(10);
    });

    it('surfaces a loading flag and an error key straight from the service', () => {
      cartService.isLoading.set(true);
      expect(component.cartService.isLoading()).toBeTrue();

      cartService.isLoading.set(false);
      cartService.error.set('cart.errors.loadFailed');
      expect(component.cartService.error()).toBe('cart.errors.loadFailed');
    });
  });

  describe('increaseQuantity', () => {
    it('calls updateQuantity with quantity+1 and clears the spinner on success', () => {
      const item = makeItem({ quantity: 2, maxQuantity: 10 });

      component.increaseQuantity(item);

      expect(cartService.updateQuantity).toHaveBeenCalledOnceWith('item-1', 3);
      expect(component.updatingItemId()).toBeNull();
      expect(component.itemError()).toBeNull();
    });

    it('does nothing once quantity has reached maxQuantity', () => {
      const item = makeItem({ quantity: 10, maxQuantity: 10 });
      component.increaseQuantity(item);
      expect(cartService.updateQuantity).not.toHaveBeenCalled();
    });
  });

  describe('decreaseQuantity', () => {
    it('calls updateQuantity with quantity-1', () => {
      const item = makeItem({ quantity: 3 });
      component.decreaseQuantity(item);
      expect(cartService.updateQuantity).toHaveBeenCalledOnceWith('item-1', 2);
    });

    it('refuses to go below 1 (never reaches zero)', () => {
      const item = makeItem({ quantity: 1 });
      component.decreaseQuantity(item);
      expect(cartService.updateQuantity).not.toHaveBeenCalled();
    });
  });

  describe('updateQuantity (manual input)', () => {
    function inputEvent(value: string): { event: Event; input: HTMLInputElement } {
      const input = document.createElement('input');
      input.value = value;
      const event = { target: input } as unknown as Event;
      return { event, input };
    }

    it('applies a valid in-range typed quantity', () => {
      const item = makeItem({ quantity: 2, maxQuantity: 10 });
      const { event } = inputEvent('5');

      component.updateQuantity(item, event);

      expect(cartService.updateQuantity).toHaveBeenCalledOnceWith('item-1', 5);
    });

    it('rejects a zero quantity and resets the input back to the current value', () => {
      const item = makeItem({ quantity: 4, maxQuantity: 10 });
      const { event, input } = inputEvent('0');

      component.updateQuantity(item, event);

      expect(cartService.updateQuantity).not.toHaveBeenCalled();
      expect(input.value).toBe('4');
    });

    it('rejects a quantity above maxQuantity and resets the input', () => {
      const item = makeItem({ quantity: 4, maxQuantity: 10 });
      const { event, input } = inputEvent('11');

      component.updateQuantity(item, event);

      expect(cartService.updateQuantity).not.toHaveBeenCalled();
      expect(input.value).toBe('4');
    });
  });

  describe('quantity update error handling', () => {
    it('records the failing item id, reloads to revert, and auto-clears the error after 5s', fakeAsync(() => {
      const item = makeItem({ quantity: 2, maxQuantity: 10 });
      cartService.updateQuantity.and.returnValue(throwError(() => new Error('500')));

      component.increaseQuantity(item);

      expect(component.itemError()).toBe('item-1');
      expect(component.updatingItemId()).toBeNull();
      // Revert is done by reloading the cart from the server.
      expect(cartService.loadCart).toHaveBeenCalledTimes(1);

      tick(5000);
      expect(component.itemError()).toBeNull();
    }));
  });

  describe('removeItem', () => {
    it('marks the item removing, waits for the animation, then removes it and toasts undo', fakeAsync(() => {
      const item = makeItem();

      const promise = component.removeItem(item);
      // Exit animation in flight: item is flagged removing, backend not yet called.
      expect(component.removingItems().has('item-1')).toBeTrue();
      expect(cartService.removeItem).not.toHaveBeenCalled();

      tick(300); // ANIMATION_DURATION
      flushMicrotasks(promise);

      expect(cartService.removeItem).toHaveBeenCalledOnceWith('item-1');
      // removing flag is cleared once the backend confirms.
      expect(component.removingItems().has('item-1')).toBeFalse();
      expect(toastService.success).toHaveBeenCalled();
    }));

    it('reloads the cart and shows an error toast when the backend removal fails', fakeAsync(() => {
      const item = makeItem();
      cartService.removeItem.and.returnValue(throwError(() => new Error('boom')));

      const promise = component.removeItem(item);
      tick(300);
      flushMicrotasks(promise);

      expect(component.removingItems().has('item-1')).toBeFalse();
      expect(cartService.loadCart).toHaveBeenCalledTimes(1);
      expect(toastService.error).toHaveBeenCalledWith('cart.remove_failed');
      expect(toastService.success).not.toHaveBeenCalled();
    }));
  });

  describe('undoRemoval', () => {
    it('re-adds a cached removed item and toasts restoration on success', fakeAsync(() => {
      const item = makeItem({ productId: 'product-9', quantity: 3, variantId: 'variant-1' });

      // Removing then resolving the removal caches the item under its id.
      const removal = component.removeItem(item);
      tick(300);
      flushMicrotasks(removal);
      toastService.success.calls.reset();

      component.undoRemoval('item-1');

      expect(cartService.addToCart).toHaveBeenCalledOnceWith('product-9', 3, 'variant-1');
      expect(toastService.success).toHaveBeenCalledWith('cart.item_restored');
    }));

    it('does nothing for an item id that was never removed', () => {
      component.undoRemoval('never-existed');
      expect(cartService.addToCart).not.toHaveBeenCalled();
    });

    it('shows a restore-failed toast when re-adding the cached item errors', fakeAsync(() => {
      const item = makeItem();
      const removal = component.removeItem(item);
      tick(300);
      flushMicrotasks(removal);

      cartService.addToCart.and.returnValue(throwError(() => new Error('boom')));
      component.undoRemoval('item-1');

      expect(toastService.error).toHaveBeenCalledWith('cart.restore_failed');
    }));
  });

  describe('DEC-SHIPPING — cart-page shipping estimate (displayed == charged)', () => {
    it('shows the €5.99 standard estimate and includes it in the total for a sub-€50 cart', () => {
      cartService.subtotal.set(40);
      cartService.cart.set({ tax: 8 } as Cart);

      expect(component.standardShipping()).toBe(5.99);
      expect(component.cartTotal()).toBeCloseTo(40 + 5.99 + 8, 2);
    });

    it('shows free standard shipping (no shipping in the total) at/above €50', () => {
      cartService.subtotal.set(50);
      cartService.cart.set({ tax: 10 } as Cart);

      expect(component.standardShipping()).toBe(0);
      expect(component.cartTotal()).toBeCloseTo(50 + 10, 2);
    });
  });
});

// helper: drain the microtask queue for the resolved removeItem promise inside fakeAsync.
function flushMicrotasks(p: Promise<void>): void {
  void p;
  tick();
}

/**
 * B-020: render-level coverage for the cart-error branch + Retry. Uses a real fixture (the main suite is
 * logic-only) so we can assert the template shows the error+retry state — not a fake empty cart — on a load
 * failure, and that Retry re-invokes loadCart.
 */
describe('CartComponent error branch (B-020 render)', () => {
  let fixture: ComponentFixture<CartComponent>;
  let cartService: CartServiceStub;

  beforeEach(() => {
    cartService = {
      items: signal<CartItem[]>([]),
      cart: signal<Cart | null>(null),
      subtotal: signal(0),
      total: signal(0),
      itemCount: signal(0),
      isEmpty: signal(true),
      isLoading: signal(false),
      error: signal<string | null>(null),
      loadFailed: signal(false),
      loadCart: jasmine.createSpy('loadCart'),
      updateQuantity: jasmine.createSpy('updateQuantity').and.returnValue(of(undefined)),
      removeItem: jasmine.createSpy('removeItem').and.returnValue(of(undefined)),
      addToCart: jasmine.createSpy('addToCart').and.returnValue(of(undefined))
    };

    TestBed.configureTestingModule({
      imports: [
        CartComponent,
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeTranslateLoader } })
      ],
      providers: [
        provideRouter([]),
        { provide: CartService, useValue: cartService },
        { provide: ToastService, useValue: jasmine.createSpyObj('ToastService', ['success', 'error']) }
      ]
    });

    fixture = TestBed.createComponent(CartComponent);
  });

  it('renders the error + Retry state (not the empty state) when loadFailed() is set', () => {
    cartService.error.set('cart.errors.loadFailed');
    cartService.loadFailed.set(true);
    fixture.detectChanges();

    const el = fixture.nativeElement;
    expect(el.querySelector('[data-testid="cart-error"]')).toBeTruthy();
    expect(el.querySelector('[data-testid="cart-retry"]')).toBeTruthy();
    // A load failure must NOT render as an empty cart.
    expect(el.querySelector('[data-testid="empty-cart"]')).toBeNull();
  });

  it('does NOT show the page-level error branch for a transient mutation error', () => {
    // A failed quantity update sets error() but not loadFailed() — the cart must stay visible.
    cartService.items.set([makeItem()]);
    cartService.isEmpty.set(false);
    cartService.error.set('cart.errors.updateQuantityFailed');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="cart-error"]')).toBeNull();
  });

  it('Retry re-invokes loadCart', () => {
    cartService.error.set('cart.errors.loadFailed');
    cartService.loadFailed.set(true);
    fixture.detectChanges();
    cartService.loadCart.calls.reset();

    fixture.nativeElement.querySelector('[data-testid="cart-retry"]').click();

    expect(cartService.loadCart).toHaveBeenCalledTimes(1);
  });
});
