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
import { Product } from '../../../core/models/product.model';
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
});
