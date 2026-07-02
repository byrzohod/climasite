import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable, Subject, of, throwError } from 'rxjs';

import { CartService } from '../../../core/services/cart.service';
import { FlyingCartService } from '../../../core/services/flying-cart.service';
import { LanguageService, SupportedLanguage } from '../../../core/services/language.service';
import { ProductService } from '../../../core/services/product.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { SeoService } from '../../../core/services/seo.service';
import { StructuredDataService } from '../../../core/services/structured-data.service';
import { Product, ProductVariant } from '../../../core/models/product.model';
import { ProductDetailComponent } from './product-detail.component';

const translations: Record<string, string> = {
  'products.details.notFound': 'Localized product not found',
  'products.details.loadError': 'Localized product load error'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

function makeVariant(overrides: Partial<ProductVariant> = {}): ProductVariant {
  return {
    id: 'var-1',
    sku: 'AC-001-STD',
    name: 'Standard',
    price: 599,
    stockQuantity: 10,
    reservedQuantity: 0,
    availableQuantity: 10,
    isActive: true,
    ...overrides
  };
}

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 'prod-1',
    sku: 'AC-001',
    name: 'Test Air Conditioner',
    slug: 'test-air-conditioner',
    shortDescription: 'A great AC unit',
    description: 'Full description',
    brand: 'TestBrand',
    basePrice: 599,
    salePrice: 699,
    isOnSale: true,
    discountPercentage: 14,
    isActive: true,
    isFeatured: false,
    averageRating: 4.5,
    reviewCount: 10,
    images: [{ id: 'img-1', url: '/assets/p.jpg', isPrimary: true, sortOrder: 0 }],
    variants: [],
    warrantyMonths: 24,
    requiresInstallation: false,
    createdAt: '2024-01-01',
    ...overrides
  };
}

describe('ProductDetailComponent', () => {
  let fixture: ComponentFixture<ProductDetailComponent>;
  let component: ProductDetailComponent;
  let productService: jasmine.SpyObj<ProductService>;
  let seo: jasmine.SpyObj<SeoService>;
  let structuredData: jasmine.SpyObj<StructuredDataService>;
  let langSignal: WritableSignal<SupportedLanguage>;
  let paramMap$: BehaviorSubject<ParamMap>;

  function setSlug(slug: string | null): void {
    paramMap$.next(convertToParamMap(slug ? { slug } : {}));
  }

  beforeEach(async () => {
    productService = jasmine.createSpyObj<ProductService>('ProductService', ['getProductBySlug']);
    seo = jasmine.createSpyObj<SeoService>('SeoService', ['setMeta']);
    structuredData = jasmine.createSpyObj<StructuredDataService>('StructuredDataService', ['setProductData', 'setBreadcrumbData']);
    langSignal = signal<SupportedLanguage>('en');
    paramMap$ = new BehaviorSubject<ParamMap>(convertToParamMap({}));

    await TestBed.configureTestingModule({
      imports: [
        ProductDetailComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: SeoService, useValue: seo },
        { provide: StructuredDataService, useValue: structuredData },
        { provide: CartService, useValue: { addToCart: () => of(null) } },
        { provide: LanguageService, useValue: { currentLanguage: langSignal, instant: (k: string) => k } },
        { provide: WishlistService, useValue: { isInWishlist: () => false, toggleWishlist: () => undefined } },
        { provide: FlyingCartService, useValue: { fly: () => undefined } },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: paramMap$.asObservable(),
            snapshot: { paramMap: convertToParamMap({}) }
          }
        }
      ]
    })
      .overrideComponent(ProductDetailComponent, {
        set: {
          template: `
            @if (error()) {
              <div data-testid="error">{{ error() | translate }}</div>
            }
          `
        }
      })
      .compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('renders a translated not-found message when the route has no slug', fakeAsync(() => {
    createComponent();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="error"]') as HTMLElement;
    expect(component.error()).toBe('products.details.notFound');
    expect(error.textContent).toContain('Localized product not found');
    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({ robots: 'noindex,follow' }));
  }));

  it('renders a translated load-error message and marks noindex when loading fails', fakeAsync(() => {
    productService.getProductBySlug.and.returnValue(throwError(() => new Error('not found')));
    setSlug('missing-product');

    createComponent();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="error"]') as HTMLElement;
    expect(productService.getProductBySlug).toHaveBeenCalledWith('missing-product');
    expect(component.error()).toBe('products.details.loadError');
    expect(error.textContent).toContain('Localized product load error');
    // H5: a 404'd slug must emit noindex,follow even though the route matched.
    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({ robots: 'noindex,follow' }));
  }));

  it('sets product meta + Product JSON-LD on successful load', fakeAsync(() => {
    const product = makeProduct({ metaTitle: undefined, name: 'Cool AC' });
    productService.getProductBySlug.and.returnValue(of(product));
    setSlug('cool-ac');

    createComponent();
    tick();

    expect(structuredData.setProductData).toHaveBeenCalledWith(product, jasmine.any(String));
    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({
      title: 'Cool AC',
      type: 'product',
      robots: 'index,follow'
    }));
  }));

  it('prefers curated metaTitle/metaDescription when present', fakeAsync(() => {
    const product = makeProduct({ metaTitle: 'Curated SEO Title', metaDescription: 'Curated SEO desc' });
    productService.getProductBySlug.and.returnValue(of(product));
    setSlug('test-air-conditioner');

    createComponent();
    tick();

    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({
      title: 'Curated SEO Title',
      description: 'Curated SEO desc'
    }));
  }));

  it('fetches exactly once per (slug, lang) change and dedupes repeats', fakeAsync(() => {
    productService.getProductBySlug.and.returnValue(of(makeProduct()));
    setSlug('p1');

    createComponent();
    tick();
    expect(productService.getProductBySlug).toHaveBeenCalledTimes(1);

    // Language change with the same slug -> one more fetch.
    langSignal.set('bg');
    fixture.detectChanges();
    tick();
    expect(productService.getProductBySlug).toHaveBeenCalledTimes(2);

    // New slug -> one more fetch.
    setSlug('p2');
    fixture.detectChanges();
    tick();
    expect(productService.getProductBySlug).toHaveBeenCalledTimes(3);

    // Re-emitting the same slug must NOT trigger a duplicate fetch.
    setSlug('p2');
    fixture.detectChanges();
    tick();
    expect(productService.getProductBySlug).toHaveBeenCalledTimes(3);
  }));

  it('rejects a stale out-of-order detail response (loadSeq guard)', fakeAsync(() => {
    const subjectA = new Subject<Product>();
    const subjectB = new Subject<Product>();
    // First load (slug pa) subscribes to A; the second (slug pb) subscribes to B.
    productService.getProductBySlug.and.returnValues(subjectA, subjectB);

    setSlug('pa');
    createComponent();
    tick();

    setSlug('pb');
    fixture.detectChanges();
    tick();

    // The LATER load (B) completes first, then the earlier (A) lands late.
    subjectB.next(makeProduct({ name: 'Product B', slug: 'pb' }));
    subjectA.next(makeProduct({ name: 'Product A', slug: 'pa' }));

    // Applied product + meta must reflect B; the stale A is rejected by the seq guard.
    expect(component.product()?.name).toBe('Product B');
    const lastTitle = (seo.setMeta.calls.mostRecent().args[0] as { title?: string }).title;
    expect(lastTitle).toBe('Product B');
  }));

  // INV-01 A3: the PDP availability indicator is derived from the reservation-adjusted
  // `availableQuantity` (stock − reserved), NOT raw stock.
  function loadProductWithVariants(variants: ProductVariant[]): void {
    productService.getProductBySlug.and.returnValue(of(makeProduct({ variants })));
    setSlug('stock-test');
    createComponent();
    tick();
  }

  it('reports out-of-stock when a variant is fully reserved despite raw stock remaining', fakeAsync(() => {
    // availableQuantity 0 while stockQuantity 10 -> shoppers must see out-of-stock, not "in stock".
    loadProductWithVariants([makeVariant({ stockQuantity: 10, reservedQuantity: 10, availableQuantity: 0 })]);

    expect(component.availableQuantity()).toBe(0);
    expect(component.stockState()).toBe('out-of-stock');
  }));

  it('reports low-stock when reservation-adjusted availability is at/below the threshold', fakeAsync(() => {
    loadProductWithVariants([makeVariant({ stockQuantity: 12, reservedQuantity: 9, availableQuantity: 3 })]);

    expect(component.availableQuantity()).toBe(3);
    expect(component.stockState()).toBe('low-stock');
  }));

  it('reports in-stock when ample reservation-adjusted availability remains', fakeAsync(() => {
    loadProductWithVariants([makeVariant({ stockQuantity: 20, reservedQuantity: 4, availableQuantity: 16 })]);

    expect(component.availableQuantity()).toBe(16);
    expect(component.stockState()).toBe('in-stock');
  }));

  it('uses the DEFAULT variant availability, not the sum across variants', fakeAsync(() => {
    // Aggregate would be 2 + 8 = 10 (plenty), but add-to-cart uses the first available variant (v1, 2 units).
    // The PDP must advertise v1's lower availability so it never promises a qty a single variant can't fill.
    loadProductWithVariants([
      makeVariant({ id: 'v1', availableQuantity: 2 }),
      makeVariant({ id: 'v2', availableQuantity: 8 })
    ]);

    expect(component.availableQuantity()).toBe(2, 'default (first available) variant, not the 10-unit sum');
    expect(component.stockState()).toBe('low-stock');
  }));

  it('skips a fully-reserved first variant to the first available one (mirrors add-to-cart)', fakeAsync(() => {
    loadProductWithVariants([
      makeVariant({ id: 'v1', stockQuantity: 5, reservedQuantity: 5, availableQuantity: 0 }),
      makeVariant({ id: 'v2', stockQuantity: 8, reservedQuantity: 2, availableQuantity: 6 })
    ]);

    expect(component.availableQuantity()).toBe(6, 'first active variant WITH availability is the default');
    expect(component.stockState()).toBe('in-stock');
  }));

  it('falls back to the first active variant (out-of-stock) when none has availability', fakeAsync(() => {
    loadProductWithVariants([
      makeVariant({ id: 'v1', stockQuantity: 5, reservedQuantity: 5, availableQuantity: 0 }),
      makeVariant({ id: 'v2', stockQuantity: 3, reservedQuantity: 3, availableQuantity: 0 })
    ]);

    expect(component.availableQuantity()).toBe(0);
    expect(component.stockState()).toBe('out-of-stock');
  }));

  it('hides the indicator (state "none") when the product exposes no active variant', fakeAsync(() => {
    loadProductWithVariants([]);

    expect(component.stockState()).toBe('none');
  }));
});
