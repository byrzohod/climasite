import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { BrandDetailComponent } from './brand-detail.component';
import { BrandService } from '../../../core/services/brand.service';
import { CartService } from '../../../core/services/cart.service';
import { Brand } from '../../../core/models/brand.model';
import { ProductBrief } from '../../../core/models/product.model';
import { Cart } from '../../../core/models/cart.model';

/**
 * Plan-19 B3: unit coverage for BrandDetailComponent — slug resolution in ngOnInit,
 * the loading / error / loaded template branches, hero + product rendering, and the
 * add-to-cart adding/added/reset lifecycle (incl. the re-entrancy guard).
 *
 * BrandService / CartService are mocked as spies; ActivatedRoute exposes a mutable slug
 * so each test controls what ngOnInit reads before the first detectChanges().
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

function mockBrand(overrides: Partial<Brand> = {}): Brand {
  return {
    id: 'b1',
    name: 'Daikin',
    slug: 'daikin',
    description: 'Japanese HVAC leader',
    logoUrl: 'https://cdn/logo.png',
    bannerImageUrl: 'https://cdn/banner.png',
    websiteUrl: 'https://daikin.com',
    countryOfOrigin: 'Japan',
    foundedYear: 1924,
    isFeatured: true,
    productCount: 1,
    products: [brief()],
    ...overrides
  };
}

const EMPTY_CART: Cart = {
  id: '', sessionId: 's', items: [], subtotal: 0, shipping: 0,
  tax: 0, total: 0, itemCount: 0, createdAt: '', updatedAt: ''
};

describe('BrandDetailComponent', () => {
  let brandService: jasmine.SpyObj<BrandService>;
  let cartService: jasmine.SpyObj<CartService>;
  let currentSlug: string | null;

  const activatedRouteStub = {
    snapshot: { paramMap: { get: (key: string) => (key === 'slug' ? currentSlug : null) } }
  };

  beforeEach(async () => {
    currentSlug = 'daikin';
    brandService = jasmine.createSpyObj<BrandService>('BrandService', ['getBrandBySlug']);
    cartService = jasmine.createSpyObj<CartService>('CartService', ['addToCart']);
    brandService.getBrandBySlug.and.returnValue(of(mockBrand()));
    cartService.addToCart.and.returnValue(of(EMPTY_CART));

    await TestBed.configureTestingModule({
      imports: [BrandDetailComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: activatedRouteStub },
        { provide: BrandService, useValue: brandService },
        { provide: CartService, useValue: cartService }
      ]
    }).compileComponents();
  });

  function create(): ComponentFixture<BrandDetailComponent> {
    const fixture = TestBed.createComponent(BrandDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('should create', () => {
    expect(create().componentInstance).toBeTruthy();
  });

  describe('ngOnInit slug resolution', () => {
    it('loads the brand for the route slug and clears loading', () => {
      const fixture = create();
      expect(brandService.getBrandBySlug).toHaveBeenCalledWith('daikin');
      expect(fixture.componentInstance.isLoading()).toBeFalse();
      expect(fixture.componentInstance.brand()?.name).toBe('Daikin');
    });

    it('sets a not-found error and does not call the service when slug is missing', () => {
      currentSlug = null;
      const fixture = create();
      expect(brandService.getBrandBySlug).not.toHaveBeenCalled();
      expect(fixture.componentInstance.error()).toBe('brands.errors.notFound');
      expect(fixture.componentInstance.isLoading()).toBeFalse();
    });
  });

  describe('template branches', () => {
    it('shows the loading indicator while the request is in flight', () => {
      brandService.getBrandBySlug.and.returnValue(new Subject<Brand>());
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.brand-hero')).toBeNull();
    });

    it('renders the hero with brand name, description and product grid on success', () => {
      const fixture = create();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.brand-hero h1')?.textContent).toContain('Daikin');
      expect(el.querySelector('.description')?.textContent).toContain('Japanese HVAC leader');
      expect(el.querySelectorAll('.product-card').length).toBe(1);
      expect(el.querySelector('.product-name')?.textContent).toContain('Test AC Unit');
    });

    it('renders a logo placeholder (first letter) when no logoUrl', () => {
      brandService.getBrandBySlug.and.returnValue(of(mockBrand({ logoUrl: undefined })));
      const fixture = create();
      const placeholder = fixture.nativeElement.querySelector('.logo-placeholder');
      expect(placeholder?.textContent?.trim()).toBe('D');
    });

    it('shows the error branch when the service fails', () => {
      spyOn(console, 'error');
      brandService.getBrandBySlug.and.returnValue(throwError(() => new Error('boom')));
      const fixture = create();
      expect(fixture.componentInstance.error()).toBe('brands.errors.notFound');
      expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    });

    it('shows the no-products message when the brand has no products', () => {
      brandService.getBrandBySlug.and.returnValue(of(mockBrand({ products: [], productCount: 0 })));
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.no-products')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.product-card')).toBeNull();
    });

    it('renders a sale badge for products on sale', () => {
      brandService.getBrandBySlug.and.returnValue(
        of(mockBrand({ products: [brief({ isOnSale: true, discountPercentage: 20, salePrice: 480 })] }))
      );
      const fixture = create();
      expect(fixture.nativeElement.querySelector('.sale-badge')?.textContent).toContain('20');
    });
  });

  describe('addToCart', () => {
    it('calls CartService with the product id and quantity 1', () => {
      const fixture = create();
      fixture.componentInstance.addToCart(brief({ id: 'p9' }));
      expect(cartService.addToCart).toHaveBeenCalledWith('p9', 1);
    });

    it('marks the item added on success then clears the flag after 3s', fakeAsync(() => {
      const fixture = create();
      const product = brief({ id: 'p9' });
      fixture.componentInstance.addToCart(product);

      expect(fixture.componentInstance.addingItems()['p9']).toBeFalse();
      expect(fixture.componentInstance.addedItems()['p9']).toBeTrue();

      tick(3000);
      expect(fixture.componentInstance.addedItems()['p9']).toBeFalse();
    }));

    it('clears the adding flag when CartService errors', () => {
      spyOn(console, 'error');
      cartService.addToCart.and.returnValue(throwError(() => new Error('nope')));
      const fixture = create();
      fixture.componentInstance.addToCart(brief({ id: 'p9' }));
      expect(fixture.componentInstance.addingItems()['p9']).toBeFalse();
      expect(fixture.componentInstance.addedItems()['p9']).toBeFalsy();
    });

    it('ignores a second call while the first is still in flight (re-entrancy guard)', () => {
      cartService.addToCart.and.returnValue(new Subject<Cart>());
      const fixture = create();
      const product = brief({ id: 'p9' });
      fixture.componentInstance.addToCart(product);
      fixture.componentInstance.addToCart(product);
      expect(cartService.addToCart).toHaveBeenCalledTimes(1);
      expect(fixture.componentInstance.addingItems()['p9']).toBeTrue();
    });
  });
});
