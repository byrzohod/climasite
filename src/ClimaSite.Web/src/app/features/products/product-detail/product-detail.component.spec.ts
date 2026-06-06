import { signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { CartService } from '../../../core/services/cart.service';
import { FlyingCartService } from '../../../core/services/flying-cart.service';
import { LanguageService } from '../../../core/services/language.service';
import { ProductService } from '../../../core/services/product.service';
import { WishlistService } from '../../../core/services/wishlist.service';
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

describe('ProductDetailComponent', () => {
  let fixture: ComponentFixture<ProductDetailComponent>;
  let component: ProductDetailComponent;
  let routeSlug: string | null;
  let productService: jasmine.SpyObj<ProductService>;

  beforeEach(async () => {
    routeSlug = null;
    productService = jasmine.createSpyObj<ProductService>('ProductService', ['getProductBySlug']);

    await TestBed.configureTestingModule({
      imports: [
        ProductDetailComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: CartService, useValue: { addToCart: () => of(null) } },
        { provide: LanguageService, useValue: { currentLanguage: signal('en') } },
        { provide: WishlistService, useValue: { isInWishlist: () => false, toggleWishlist: () => undefined } },
        { provide: FlyingCartService, useValue: { fly: () => undefined } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: () => routeSlug
              }
            }
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
    expect(error.textContent).not.toContain('Product not found');
  }));

  it('renders a translated load-error message when product loading fails', fakeAsync(() => {
    routeSlug = 'missing-product';
    productService.getProductBySlug.and.returnValue(throwError(() => new Error('not found')));

    createComponent();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="error"]') as HTMLElement;
    expect(productService.getProductBySlug).toHaveBeenCalledWith('missing-product');
    expect(component.error()).toBe('products.details.loadError');
    expect(error.textContent).toContain('Localized product load error');
    expect(error.textContent).not.toContain('Failed to load product. Please try again.');
  }));
});
