import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateNoOpLoader } from '@ngx-translate/core';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../auth/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { FlyingCartService } from '../../core/services/flying-cart.service';
import { ProductBrief } from '../../core/models/product.model';
import { WishlistApiItem, WishlistDto, WishlistItem, WishlistService } from '../../core/services/wishlist.service';
import { WishlistComponent } from './wishlist.component';

describe('WishlistComponent', () => {
  let fixture: ComponentFixture<WishlistComponent>;
  let itemsSignal: WritableSignal<WishlistItem[]>;
  let wishlistPublic = false;
  let wishlistToken: string | null = null;

  type WishlistServiceStub = jasmine.SpyObj<Pick<
    WishlistService,
    | 'refreshWishlist'
    | 'removeFromWishlist'
    | 'clearWishlist'
    | 'setSharing'
    | 'regenerateShareToken'
    | 'getSharedWishlist'
    | 'toProductBrief'
    | 'toggleWishlist'
  >> & {
    items: () => WishlistItem[];
    isLoading: () => boolean;
    isPublic: () => boolean;
    shareToken: () => string | null;
    toProductBrief: (item: WishlistApiItem) => ProductBrief;
  };
  let wishlistService: WishlistServiceStub;

  const product: ProductBrief = {
    id: 'product-1',
    name: 'Wishlist Product',
    slug: 'wishlist-product',
    basePrice: 999,
    salePrice: undefined,
    isOnSale: false,
    discountPercentage: 0,
    brand: 'CDL',
    averageRating: 0,
    reviewCount: 0,
    primaryImageUrl: '/images/wishlist-product.webp',
    inStock: true
  };

  const sharedItem: WishlistApiItem = {
    id: 'item-1',
    productId: product.id,
    productName: product.name,
    productSlug: product.slug,
    shortDescription: null,
    brand: product.brand,
    imageUrl: product.primaryImageUrl,
    primaryImageUrl: product.primaryImageUrl,
    price: product.basePrice,
    salePrice: null,
    isOnSale: product.isOnSale,
    discountPercentage: product.discountPercentage,
    averageRating: product.averageRating,
    reviewCount: product.reviewCount,
    inStock: product.inStock,
    note: null,
    priority: 0,
    priceWhenAdded: null,
    notifyOnSale: false,
    addedAt: '2026-06-07T00:00:00Z'
  };

  const sharedWishlist: WishlistDto = {
    id: 'wishlist-1',
    userId: 'user-1',
    isPublic: true,
    shareToken: 'share-token',
    items: [sharedItem],
    itemCount: 1,
    updatedAt: '2026-06-07T00:00:00Z'
  };

  const configure = async (shareToken: string | null = null, authenticated = true): Promise<void> => {
    itemsSignal = signal<WishlistItem[]>([
      {
        id: 'item-1',
        productId: product.id,
        addedAt: '2026-06-07T00:00:00Z',
        product
      }
    ]);
    wishlistPublic = false;
    wishlistToken = null;

    wishlistService = jasmine.createSpyObj('WishlistService', [
      'refreshWishlist',
      'removeFromWishlist',
      'clearWishlist',
      'setSharing',
      'regenerateShareToken',
      'getSharedWishlist',
      'toProductBrief',
      'toggleWishlist'
    ]) as WishlistServiceStub;

    wishlistService.items = itemsSignal.asReadonly();
    wishlistService.isLoading = () => false;
    wishlistService.isPublic = () => wishlistPublic;
    wishlistService.shareToken = () => wishlistToken;
    wishlistService.refreshWishlist.and.returnValue(of(null));
    wishlistService.setSharing.and.callFake(isPublic => {
      wishlistPublic = isPublic;
      wishlistToken = isPublic ? 'share-token' : null;
      return of({ ...sharedWishlist, isPublic, shareToken: wishlistToken });
    });
    wishlistService.regenerateShareToken.and.returnValue(of(sharedWishlist));
    wishlistService.getSharedWishlist.and.returnValue(of(sharedWishlist));
    wishlistService.toProductBrief.and.callFake(item => ({
      id: item.productId,
      name: item.productName,
      slug: item.productSlug,
      basePrice: item.price,
      salePrice: item.salePrice ?? undefined,
      isOnSale: item.isOnSale,
      discountPercentage: item.discountPercentage,
      brand: item.brand ?? undefined,
      averageRating: item.averageRating,
      reviewCount: item.reviewCount,
      primaryImageUrl: item.primaryImageUrl ?? item.imageUrl ?? undefined,
      inStock: item.inStock
    }));

    await TestBed.configureTestingModule({
      imports: [
        WishlistComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateNoOpLoader }
        })
      ],
      providers: [
        provideRouter([]),
        { provide: WishlistService, useValue: wishlistService },
        { provide: AuthService, useValue: { isAuthenticated: () => authenticated } },
        { provide: CartService, useValue: { addToCart: () => of(null) } },
        { provide: FlyingCartService, useValue: { fly: jasmine.createSpy('fly') } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => key === 'shareToken' ? shareToken : null
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WishlistComponent);
    fixture.detectChanges();
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders products from wishlist service items', async () => {
    await configure();

    expect(fixture.componentInstance.products()).toEqual([product]);
    expect(fixture.nativeElement.textContent).toContain('Wishlist Product');
  });

  it('clears owner wishlist through the service', async () => {
    await configure();

    fixture.debugElement.query(By.css('[data-testid="clear-wishlist"]')).nativeElement.click();
    fixture.detectChanges();

    expect(wishlistService.clearWishlist).toHaveBeenCalled();
    expect(fixture.componentInstance.products()).toEqual([]);
  });

  it('enables sharing from owner actions', async () => {
    await configure();

    fixture.debugElement.query(By.css('[data-testid="wishlist-share-toggle"]')).nativeElement.click();
    fixture.detectChanges();

    expect(wishlistService.setSharing).toHaveBeenCalledWith(true);
  });

  it('loads shared wishlist as read-only view', async () => {
    await configure('share-token', false);

    expect(wishlistService.getSharedWishlist).toHaveBeenCalledWith('share-token');
    expect(fixture.componentInstance.products()[0].name).toBe('Wishlist Product');
    expect(fixture.debugElement.query(By.css('[data-testid="remove-from-wishlist"]'))).toBeNull();
    expect(fixture.debugElement.query(By.css('[data-testid="clear-wishlist"]'))).toBeNull();
  });
});
