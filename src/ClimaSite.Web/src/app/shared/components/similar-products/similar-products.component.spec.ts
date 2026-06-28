import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ElementRef } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';

import { SimilarProductsComponent } from './similar-products.component';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ProductBrief } from '../../../core/models/product.model';

function makeProduct(overrides: Partial<ProductBrief> = {}): ProductBrief {
  return {
    id: 'prod-1',
    name: 'Test AC Unit',
    slug: 'test-ac-unit',
    basePrice: 599.99,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 4.5,
    reviewCount: 10,
    inStock: true,
    primaryImageUrl: 'http://example.com/ac.jpg',
    brand: 'CoolBrand',
    ...overrides
  };
}

describe('SimilarProductsComponent', () => {
  let fixture: ComponentFixture<SimilarProductsComponent>;
  let component: SimilarProductsComponent;
  let productService: jasmine.SpyObj<ProductService>;
  let cartService: jasmine.SpyObj<CartService>;

  beforeEach(async () => {
    productService = jasmine.createSpyObj<ProductService>('ProductService', ['getSimilarProducts']);
    cartService = jasmine.createSpyObj<CartService>('CartService', ['addToCart']);

    // Sensible defaults; individual tests override return values before detectChanges.
    productService.getSimilarProducts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [SimilarProductsComponent, TranslateModule.forRoot(), RouterTestingModule],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: CartService, useValue: cartService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SimilarProductsComponent);
    component = fixture.componentInstance;
  });

  describe('Loading similar products', () => {
    it('should call the service with the bound productId via the effect', fakeAsync(() => {
      fixture.componentRef.setInput('productId', 'abc-123');
      fixture.detectChanges();
      tick(100);

      expect(productService.getSimilarProducts).toHaveBeenCalledWith('abc-123');
    }));

    it('should not render the section when no similar products are returned', fakeAsync(() => {
      productService.getSimilarProducts.and.returnValue(of([]));
      fixture.componentRef.setInput('productId', 'abc-123');
      fixture.detectChanges();
      tick(100);

      expect(component.products().length).toBe(0);
      expect(fixture.nativeElement.querySelector('[data-testid="similar-products-section"]')).toBeNull();
    }));

    it('should render a card per returned product', fakeAsync(() => {
      productService.getSimilarProducts.and.returnValue(of([
        makeProduct({ id: 'p1', name: 'Unit One', slug: 'unit-one' }),
        makeProduct({ id: 'p2', name: 'Unit Two', slug: 'unit-two' })
      ]));
      fixture.componentRef.setInput('productId', 'abc-123');
      fixture.detectChanges();
      tick(100);
      fixture.detectChanges();

      const section = fixture.nativeElement.querySelector('[data-testid="similar-products-section"]');
      expect(section).toBeTruthy();

      const cards = fixture.nativeElement.querySelectorAll('[data-testid="similar-product-card"]');
      expect(cards.length).toBe(2);
      expect(fixture.nativeElement.textContent).toContain('Unit One');
      expect(fixture.nativeElement.textContent).toContain('Unit Two');
    }));

    it('should render a sale badge for discounted products', fakeAsync(() => {
      productService.getSimilarProducts.and.returnValue(of([
        makeProduct({ id: 'p1', isOnSale: true, discountPercentage: 25, salePrice: 449.99 })
      ]));
      fixture.componentRef.setInput('productId', 'abc-123');
      fixture.detectChanges();
      tick(100);
      fixture.detectChanges();

      const badge: HTMLElement = fixture.nativeElement.querySelector('.sale-badge');
      expect(badge).toBeTruthy();
      expect(badge.textContent).toContain('25');
    }));
  });

  describe('addToCart', () => {
    it('should prevent default, stop propagation and delegate to CartService', () => {
      const product = makeProduct({ id: 'p1' });
      cartService.addToCart.and.returnValue(of({} as any));
      const event = jasmine.createSpyObj<Event>('Event', ['preventDefault', 'stopPropagation']);

      component.addToCart(product, event);

      expect(event.preventDefault).toHaveBeenCalled();
      expect(event.stopPropagation).toHaveBeenCalled();
      expect(cartService.addToCart).toHaveBeenCalledWith('p1', 1);
    });

    it('should mark the item as added on success and reset after 3s', fakeAsync(() => {
      const product = makeProduct({ id: 'p1' });
      cartService.addToCart.and.returnValue(of({} as any));
      const event = jasmine.createSpyObj<Event>('Event', ['preventDefault', 'stopPropagation']);

      component.addToCart(product, event);

      expect(component.addingItems()['p1']).toBeFalse();
      expect(component.addedItems()['p1']).toBeTrue();

      tick(3000);
      expect(component.addedItems()['p1']).toBeFalse();
    }));

    it('should clear the adding flag when the cart call errors', () => {
      const product = makeProduct({ id: 'p1' });
      cartService.addToCart.and.returnValue(throwError(() => new Error('boom')));
      const event = jasmine.createSpyObj<Event>('Event', ['preventDefault', 'stopPropagation']);

      component.addToCart(product, event);

      expect(component.addingItems()['p1']).toBeFalse();
      expect(component.addedItems()['p1']).toBeFalsy();
    });

    it('should ignore a second add while the first is still in flight', () => {
      const product = makeProduct({ id: 'p1' });
      component.addingItems.set({ p1: true });

      component.addToCart(product, jasmine.createSpyObj<Event>('Event', ['preventDefault', 'stopPropagation']));

      expect(cartService.addToCart).not.toHaveBeenCalled();
    });
  });

  describe('Carousel scrolling', () => {
    it('should scroll left by a fixed offset', () => {
      const nativeElement = jasmine.createSpyObj<HTMLDivElement>('carousel', ['scrollBy']);
      component.carousel = new ElementRef(nativeElement);

      component.scrollLeft();

      expect(nativeElement.scrollBy as jasmine.Spy).toHaveBeenCalledWith({ left: -240, behavior: 'smooth' });
    });

    it('should scroll right by a fixed offset', () => {
      const nativeElement = jasmine.createSpyObj<HTMLDivElement>('carousel', ['scrollBy']);
      component.carousel = new ElementRef(nativeElement);

      component.scrollRight();

      expect(nativeElement.scrollBy as jasmine.Spy).toHaveBeenCalledWith({ left: 240, behavior: 'smooth' });
    });

    it('should compute scroll-button availability on scroll', () => {
      const stub = (scrollLeft: number): ElementRef<HTMLDivElement> =>
        new ElementRef({ scrollLeft, scrollWidth: 1000, clientWidth: 200 } as unknown as HTMLDivElement);

      // At the start: cannot scroll left, can scroll right.
      component.carousel = stub(0);
      component.onScroll();
      expect(component.canScrollLeft()).toBeFalse();
      expect(component.canScrollRight()).toBeTrue();

      // Mid-scroll: can scroll both directions.
      component.carousel = stub(100);
      component.onScroll();
      expect(component.canScrollLeft()).toBeTrue();
      expect(component.canScrollRight()).toBeTrue();

      // At the end (within the 10px threshold): cannot scroll right.
      component.carousel = stub(800);
      component.onScroll();
      expect(component.canScrollRight()).toBeFalse();
    });

    it('should not throw when the carousel ref is not yet available', () => {
      component.carousel = undefined as unknown as ElementRef<HTMLDivElement>;
      expect(() => component.onScroll()).not.toThrow();
      expect(() => component.scrollLeft()).not.toThrow();
      expect(() => component.scrollRight()).not.toThrow();
    });
  });

  it('should complete cleanup on destroy without throwing', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
