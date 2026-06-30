import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { PromotionDetailComponent } from './promotion-detail.component';
import { PromotionService } from '../../../core/services/promotion.service';
import { CartService } from '../../../core/services/cart.service';
import { Promotion, PromotionType } from '../../../core/models/promotion.model';
import { ProductBrief } from '../../../core/models/product.model';
import { Cart } from '../../../core/models/cart.model';

/**
 * Plan-19 B3: unit coverage for PromotionDetailComponent — slug resolution, loading/error/
 * loaded branches, getDiscountText() formatting, copyCode() clipboard + copied-flag reset,
 * the terms section, and the add-to-cart adding/added/reset lifecycle + re-entrancy guard.
 */

function brief(overrides: Partial<ProductBrief> = {}): ProductBrief {
  return {
    id: 'p1',
    name: 'Test AC Unit',
    slug: 'test-ac-unit',
    basePrice: 599.99,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 4,
    reviewCount: 2,
    inStock: true,
    ...overrides
  };
}

function mockPromotion(overrides: Partial<Promotion> = {}): Promotion {
  return {
    id: 'promo1',
    name: 'Summer Sale',
    slug: 'summer-sale',
    description: 'Hot deals',
    code: 'SUMMER25',
    type: PromotionType.Percentage,
    discountValue: 25,
    minimumOrderAmount: 100,
    startDate: '2026-06-01',
    endDate: '2026-07-01',
    isActive: true,
    isFeatured: true,
    termsAndConditions: 'Terms apply',
    products: [brief()],
    ...overrides
  };
}

const EMPTY_CART: Cart = {
  id: '', sessionId: 's', items: [], subtotal: 0, shipping: 0,
  tax: 0, total: 0, itemCount: 0, createdAt: '', updatedAt: ''
};

describe('PromotionDetailComponent', () => {
  let promotionService: jasmine.SpyObj<PromotionService>;
  let cartService: jasmine.SpyObj<CartService>;
  let currentSlug: string | null;

  const activatedRouteStub = {
    // `paramMap` is read once at construction by toSignal; the getter reflects the
    // per-test `currentSlug` set before create().
    get paramMap() {
      return of(convertToParamMap(currentSlug ? { slug: currentSlug } : {}));
    },
    snapshot: { paramMap: { get: (key: string) => (key === 'slug' ? currentSlug : null) } }
  };

  beforeEach(async () => {
    currentSlug = 'summer-sale';
    promotionService = jasmine.createSpyObj<PromotionService>('PromotionService', ['getPromotionBySlug']);
    cartService = jasmine.createSpyObj<CartService>('CartService', ['addToCart']);
    promotionService.getPromotionBySlug.and.returnValue(of(mockPromotion()));
    cartService.addToCart.and.returnValue(of(EMPTY_CART));

    // Ensure a spy-able clipboard exists even in environments that lack one.
    if (!('clipboard' in navigator)) {
      Object.defineProperty(navigator, 'clipboard', {
        value: { writeText: () => Promise.resolve() },
        configurable: true
      });
    }

    await TestBed.configureTestingModule({
      imports: [PromotionDetailComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: PromotionService, useValue: promotionService },
        { provide: CartService, useValue: cartService }
      ]
    }).compileComponents();
  });

  function create(): ComponentFixture<PromotionDetailComponent> {
    const fixture = TestBed.createComponent(PromotionDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('should create', () => {
    expect(create().componentInstance).toBeTruthy();
  });

  describe('ngOnInit slug resolution', () => {
    it('loads the promotion for the route slug', () => {
      const fixture = create();
      expect(promotionService.getPromotionBySlug).toHaveBeenCalledWith('summer-sale');
      expect(fixture.componentInstance.promotion()?.name).toBe('Summer Sale');
      expect(fixture.componentInstance.isLoading()).toBeFalse();
    });

    it('sets a not-found error and skips the service when slug is missing', () => {
      currentSlug = null;
      const fixture = create();
      expect(promotionService.getPromotionBySlug).not.toHaveBeenCalled();
      expect(fixture.componentInstance.error()).toBe('promotions.errors.notFound');
      expect(fixture.componentInstance.isLoading()).toBeFalse();
    });
  });

  describe('template branches', () => {
    it('shows the loading indicator while the request is in flight', () => {
      promotionService.getPromotionBySlug.and.returnValue(new Subject<Promotion>());
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.promotion-hero')).toBeNull();
    });

    it('renders the hero, promo code, products and terms on success', () => {
      const fixture = create();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.promotion-hero h1')?.textContent).toContain('Summer Sale');
      expect(el.querySelector('.discount-badge')?.textContent).toContain('-25%');
      expect(el.querySelector('.promo-code .code')?.textContent).toContain('SUMMER25');
      expect(el.querySelectorAll('.product-card').length).toBe(1);
      expect(el.querySelector('.terms-section')?.textContent).toContain('Terms apply');
    });

    it('omits the promo code block when the promotion has no code', () => {
      promotionService.getPromotionBySlug.and.returnValue(of(mockPromotion({ code: undefined })));
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.promo-code')).toBeNull();
    });

    it('shows the error branch when the service fails', () => {
      spyOn(console, 'error');
      promotionService.getPromotionBySlug.and.returnValue(throwError(() => new Error('boom')));
      const fixture = create();
      expect(fixture.componentInstance.error()).toBe('promotions.errors.notFound');
      expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    });

    it('shows the no-products message when there are no products', () => {
      promotionService.getPromotionBySlug.and.returnValue(of(mockPromotion({ products: [] })));
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.no-products')).toBeTruthy();
    });
  });

  describe('getDiscountText', () => {
    it('returns empty string when no promotion is loaded', () => {
      currentSlug = null;
      const component = create().componentInstance;
      expect(component.getDiscountText()).toBe('');
    });

    it('formats each promotion type', () => {
      const component = create().componentInstance;

      component.promotion.set(mockPromotion({ type: PromotionType.Percentage, discountValue: 40 }));
      expect(component.getDiscountText()).toBe('-40%');

      component.promotion.set(mockPromotion({ type: PromotionType.FixedAmount, discountValue: 75 }));
      expect(component.getDiscountText()).toBe('-€75');

      component.promotion.set(mockPromotion({ type: PromotionType.FreeShipping }));
      expect(component.getDiscountText()).toBe('Free Shipping');

      component.promotion.set(mockPromotion({ type: PromotionType.BuyOneGetOne }));
      expect(component.getDiscountText()).toBe('BOGO');

      component.promotion.set(mockPromotion({ type: 'Mystery' as unknown as PromotionType }));
      expect(component.getDiscountText()).toBe('SALE');
    });
  });

  describe('copyCode', () => {
    it('writes the code to the clipboard and toggles copied off after 2s', fakeAsync(() => {
      const writeText = spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());
      const component = create().componentInstance;

      component.copyCode();
      tick(); // resolve the clipboard promise microtask
      expect(writeText).toHaveBeenCalledWith('SUMMER25');
      expect(component.copied()).toBeTrue();

      tick(2000);
      expect(component.copied()).toBeFalse();
    }));

    it('does nothing when there is no code', () => {
      const writeText = spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());
      promotionService.getPromotionBySlug.and.returnValue(of(mockPromotion({ code: undefined })));
      const component = create().componentInstance;

      component.copyCode();
      expect(writeText).not.toHaveBeenCalled();
      expect(component.copied()).toBeFalse();
    });
  });

  describe('addToCart', () => {
    it('calls CartService with the product id and quantity 1', () => {
      const component = create().componentInstance;
      component.addToCart(brief({ id: 'p9' }));
      expect(cartService.addToCart).toHaveBeenCalledWith('p9', 1);
    });

    it('marks the item added on success then clears it after 3s', fakeAsync(() => {
      const component = create().componentInstance;
      component.addToCart(brief({ id: 'p9' }));

      expect(component.addingItems()['p9']).toBeFalse();
      expect(component.addedItems()['p9']).toBeTrue();

      tick(3000);
      expect(component.addedItems()['p9']).toBeFalse();
    }));

    it('clears the adding flag when CartService errors', () => {
      spyOn(console, 'error');
      cartService.addToCart.and.returnValue(throwError(() => new Error('nope')));
      const component = create().componentInstance;
      component.addToCart(brief({ id: 'p9' }));
      expect(component.addingItems()['p9']).toBeFalse();
      expect(component.addedItems()['p9']).toBeFalsy();
    });

    it('ignores a second call while the first is still in flight', () => {
      cartService.addToCart.and.returnValue(new Subject<Cart>());
      const component = create().componentInstance;
      const product = brief({ id: 'p9' });
      component.addToCart(product);
      component.addToCart(product);
      expect(cartService.addToCart).toHaveBeenCalledTimes(1);
      expect(component.addingItems()['p9']).toBeTrue();
    });
  });
});
