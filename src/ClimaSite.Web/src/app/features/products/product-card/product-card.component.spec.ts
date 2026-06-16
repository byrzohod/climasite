import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { ProductCardComponent } from './product-card.component';
import { ProductBrief } from '../../../core/models/product.model';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { FlyingCartService } from '../../../core/services/flying-cart.service';

describe('ProductCardComponent', () => {
  let component: ProductCardComponent;
  let fixture: ComponentFixture<ProductCardComponent>;

  // BUG-06 contract: basePrice is the CURRENT selling price (active/emphasized),
  // salePrice is the ORIGINAL compare-at price (struck-through) when on sale.
  const onSaleProduct: ProductBrief = {
    id: 'prod-1',
    name: 'DualZone Pro 12000',
    slug: 'dualzone-pro-12000',
    basePrice: 899.99,
    salePrice: 1099.99,
    isOnSale: true,
    discountPercentage: 18,
    brand: 'TestBrand',
    averageRating: 4.5,
    reviewCount: 12,
    primaryImageUrl: 'https://example.com/img.jpg',
    inStock: true
  };

  const regularProduct: ProductBrief = {
    ...onSaleProduct,
    id: 'prod-2',
    slug: 'regular-unit',
    salePrice: undefined,
    isOnSale: false,
    discountPercentage: 0
  };

  beforeEach(async () => {
    const cartSpy = jasmine.createSpyObj('CartService', ['addToCart']);
    cartSpy.addToCart.and.returnValue(of({}));
    const wishlistSpy = jasmine.createSpyObj('WishlistService', ['toggleWishlist'], {
      items: signal([])
    });
    const flyingCartSpy = jasmine.createSpyObj('FlyingCartService', ['fly']);

    await TestBed.configureTestingModule({
      imports: [
        ProductCardComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: CartService, useValue: cartSpy },
        { provide: WishlistService, useValue: wishlistSpy },
        { provide: FlyingCartService, useValue: flyingCartSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductCardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    component.product = regularProduct;
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('renders the current price (basePrice) as the active price and the original (salePrice) struck-through when on sale', () => {
    component.product = onSaleProduct;
    fixture.detectChanges();

    const priceBlock = fixture.nativeElement.querySelector('[data-testid="product-price"]');
    const activePrice = priceBlock.querySelector('.sale-price');
    const struckPrice = priceBlock.querySelector('.original-price');

    expect(activePrice).toBeTruthy();
    expect(struckPrice).toBeTruthy();
    // Active (emphasized) price reflects the current selling price (basePrice).
    expect(activePrice.textContent).toContain('899.99');
    // Struck-through price reflects the original compare-at price (salePrice).
    expect(struckPrice.textContent).toContain('1,099.99');
  });

  it('shows the discount badge derived from discountPercentage when on sale', () => {
    component.product = onSaleProduct;
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="sale-badge"]');
    expect(badge).toBeTruthy();
    expect(badge.textContent).toContain('18');
  });

  it('shows only the current price and no struck-through original when not on sale', () => {
    component.product = regularProduct;
    fixture.detectChanges();

    const priceBlock = fixture.nativeElement.querySelector('[data-testid="product-price"]');
    const currentPrice = priceBlock.querySelector('.current-price');
    const struckPrice = priceBlock.querySelector('.original-price');

    expect(currentPrice).toBeTruthy();
    expect(currentPrice.textContent).toContain('899.99');
    expect(struckPrice).toBeFalsy();
  });
});
