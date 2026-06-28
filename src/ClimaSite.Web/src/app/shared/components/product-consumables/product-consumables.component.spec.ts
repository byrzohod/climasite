import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { ProductConsumablesComponent } from './product-consumables.component';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductBrief } from '../../../core/models/product.model';

describe('ProductConsumablesComponent', () => {
  let component: ProductConsumablesComponent;
  let fixture: ComponentFixture<ProductConsumablesComponent>;
  let productService: jasmine.SpyObj<ProductService>;
  let cartService: jasmine.SpyObj<CartService>;

  const makeProduct = (over: Partial<ProductBrief> = {}): ProductBrief => ({
    id: 'p-1',
    name: 'Replacement Filter',
    slug: 'replacement-filter',
    basePrice: 29.99,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 4,
    reviewCount: 3,
    primaryImageUrl: 'https://example.com/filter.jpg',
    inStock: true,
    ...over
  });

  const consumables = [
    makeProduct({ id: 'c-1', slug: 'filter-a', name: 'Filter A' }),
    makeProduct({ id: 'c-2', slug: 'filter-b', name: 'Filter B', inStock: false })
  ];

  beforeEach(async () => {
    productService = jasmine.createSpyObj('ProductService', ['getProductConsumables']);
    cartService = jasmine.createSpyObj('CartService', ['addToCart']);
    productService.getProductConsumables.and.returnValue(of([]));
    cartService.addToCart.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [ProductConsumablesComponent, RouterTestingModule, TranslateModule.forRoot()],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: CartService, useValue: cartService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductConsumablesComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('productId', 'parent-1');
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('Loading consumables (effect)', () => {
    it('should request consumables for the provided productId', () => {
      productService.getProductConsumables.and.returnValue(of(consumables));
      fixture.componentRef.setInput('productId', 'parent-1');
      fixture.detectChanges();
      expect(productService.getProductConsumables).toHaveBeenCalledWith('parent-1');
    });

    it('should populate the consumables signal and render a card per item', fakeAsync(() => {
      productService.getProductConsumables.and.returnValue(of(consumables));
      fixture.componentRef.setInput('productId', 'parent-1');
      fixture.detectChanges();
      tick(100); // flush the post-load updateScrollButtons timer
      fixture.detectChanges();

      expect(component.consumables().length).toBe(2);
      const cards = fixture.nativeElement.querySelectorAll('[data-testid="consumable-card"]');
      expect(cards.length).toBe(2);
      expect(fixture.nativeElement.querySelector('[data-testid="consumables-section"]')).toBeTruthy();
    }));

    it('should NOT render the section when there are no consumables', () => {
      productService.getProductConsumables.and.returnValue(of([]));
      fixture.componentRef.setInput('productId', 'parent-1');
      fixture.detectChanges();

      expect(component.consumables().length).toBe(0);
      expect(fixture.nativeElement.querySelector('[data-testid="consumables-section"]')).toBeNull();
    });

    it('should leave consumables empty (and not throw) when the load errors', () => {
      productService.getProductConsumables.and.returnValue(throwError(() => new Error('boom')));
      fixture.componentRef.setInput('productId', 'parent-1');
      expect(() => fixture.detectChanges()).not.toThrow();
      expect(component.consumables().length).toBe(0);
    });
  });

  describe('addToCart()', () => {
    function fakeEvent(): { event: Event; preventDefault: jasmine.Spy; stopPropagation: jasmine.Spy } {
      const preventDefault = jasmine.createSpy('preventDefault');
      const stopPropagation = jasmine.createSpy('stopPropagation');
      return { event: { preventDefault, stopPropagation } as unknown as Event, preventDefault, stopPropagation };
    }

    beforeEach(() => {
      fixture.componentRef.setInput('productId', 'parent-1');
      fixture.detectChanges();
    });

    it('should prevent default navigation and stop propagation', () => {
      const e = fakeEvent();
      component.addToCart(makeProduct({ id: 'c-1' }), e.event);
      expect(e.preventDefault).toHaveBeenCalled();
      expect(e.stopPropagation).toHaveBeenCalled();
    });

    it('should call CartService.addToCart with the product id and quantity 1', () => {
      component.addToCart(makeProduct({ id: 'c-9' }), fakeEvent().event);
      expect(cartService.addToCart).toHaveBeenCalledWith('c-9', 1);
    });

    it('should flag the item as added on success, then clear it after the timeout', fakeAsync(() => {
      cartService.addToCart.and.returnValue(of({} as any));
      component.addToCart(makeProduct({ id: 'c-1' }), fakeEvent().event);

      // synchronous success: no longer "adding", now "added"
      expect(component.addingItems()['c-1']).toBeFalse();
      expect(component.addedItems()['c-1']).toBeTrue();

      tick(3000);
      expect(component.addedItems()['c-1']).toBeFalse();
    }));

    it('should clear the adding flag on error without marking added', () => {
      cartService.addToCart.and.returnValue(throwError(() => new Error('fail')));
      component.addToCart(makeProduct({ id: 'c-1' }), fakeEvent().event);
      expect(component.addingItems()['c-1']).toBeFalse();
      expect(component.addedItems()['c-1']).toBeFalsy();
    });

    it('should ignore a second click while an add is already in flight', () => {
      // never-completing observable keeps the item in the "adding" state
      cartService.addToCart.and.returnValue(of({} as any));
      const product = makeProduct({ id: 'c-1' });

      // Force the in-flight state manually then attempt a re-entry
      component.addingItems.set({ 'c-1': true });
      component.addToCart(product, fakeEvent().event);
      expect(cartService.addToCart).not.toHaveBeenCalled();
    });
  });

  describe('Scroll controls', () => {
    function mockCarousel(props: Partial<{ scrollLeft: number; scrollWidth: number; clientWidth: number }>) {
      const scrollBy = jasmine.createSpy('scrollBy');
      component.carousel = {
        nativeElement: {
          scrollLeft: 0,
          scrollWidth: 1000,
          clientWidth: 300,
          scrollBy,
          ...props
        }
      } as any;
      return scrollBy;
    }

    it('scrollLeft() should scroll the carousel left by 240px', () => {
      const scrollBy = mockCarousel({});
      component.scrollLeft();
      expect(scrollBy).toHaveBeenCalledWith({ left: -240, behavior: 'smooth' });
    });

    it('scrollRight() should scroll the carousel right by 240px', () => {
      const scrollBy = mockCarousel({});
      component.scrollRight();
      expect(scrollBy).toHaveBeenCalledWith({ left: 240, behavior: 'smooth' });
    });

    it('onScroll() should disable left when at the start', () => {
      mockCarousel({ scrollLeft: 0, scrollWidth: 1000, clientWidth: 300 });
      component.onScroll();
      expect(component.canScrollLeft()).toBeFalse();
      expect(component.canScrollRight()).toBeTrue();
    });

    it('onScroll() should enable left and disable right when scrolled to the end', () => {
      mockCarousel({ scrollLeft: 700, scrollWidth: 1000, clientWidth: 300 });
      component.onScroll();
      expect(component.canScrollLeft()).toBeTrue();
      expect(component.canScrollRight()).toBeFalse();
    });
  });

  describe('ngOnDestroy()', () => {
    it('should complete the destroy subject', () => {
      fixture.componentRef.setInput('productId', 'parent-1');
      fixture.detectChanges();
      const destroy$ = (component as unknown as { destroy$: { complete: () => void } }).destroy$;
      spyOn(destroy$, 'complete').and.callThrough();
      component.ngOnDestroy();
      expect(destroy$.complete).toHaveBeenCalled();
    });
  });
});
