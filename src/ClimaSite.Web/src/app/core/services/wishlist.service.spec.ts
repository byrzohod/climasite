import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../../auth/services/auth.service';
import { ProductBrief } from '../models/product.model';
import { environment } from '../../../environments/environment';
import { WishlistDto, WishlistItem, WishlistService } from './wishlist.service';

describe('WishlistService', () => {
  let service: WishlistService;
  let httpMock: HttpTestingController;
  let authenticated = false;
  let authLoading = false;

  const apiUrl = `${environment.apiUrl}/api/wishlist`;
  const product: ProductBrief = {
    id: 'product-1',
    name: 'Wishlist AC',
    slug: 'wishlist-ac',
    shortDescription: 'Efficient AC',
    basePrice: 899,
    salePrice: 999,
    isOnSale: true,
    discountPercentage: 10,
    brand: 'CDL',
    averageRating: 0,
    reviewCount: 0,
    primaryImageUrl: '/images/wishlist-ac.webp',
    inStock: true
  };

  const emptyWishlist: WishlistDto = {
    id: 'wishlist-1',
    userId: 'user-1',
    isPublic: false,
    shareToken: null,
    items: [],
    itemCount: 0,
    updatedAt: '2026-06-07T00:00:00Z'
  };

  const wishlistWithItem: WishlistDto = {
    ...emptyWishlist,
    items: [{
      id: 'item-1',
      productId: product.id,
      productName: product.name,
      productSlug: product.slug,
      shortDescription: product.shortDescription,
      brand: product.brand,
      imageUrl: product.primaryImageUrl,
      primaryImageUrl: product.primaryImageUrl,
      price: product.basePrice,
      salePrice: product.salePrice,
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
    }],
    itemCount: 1
  };

  const configure = (isAuthenticated = false, storedItems: WishlistItem[] = [], isLoading = false): void => {
    authenticated = isAuthenticated;
    authLoading = isLoading;
    localStorage.clear();
    if (storedItems.length > 0) {
      localStorage.setItem('climasite_wishlist', JSON.stringify(storedItems));
    }

    TestBed.configureTestingModule({
      providers: [
        WishlistService,
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => authenticated,
            isLoading: () => authLoading
          }
        }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    service = TestBed.inject(WishlistService);
  };

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  it('hydrates authenticated wishlist from backend DTO items', () => {
    configure(true);

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.flush(wishlistWithItem);

    expect(service.itemCount()).toBe(1);
    expect(service.items()[0].product?.name).toBe('Wishlist AC');
    expect(service.items()[0].product?.primaryImageUrl).toBe('/images/wishlist-ac.webp');
  });

  it('does not auto-fetch while auth login merge is already loading', () => {
    configure(true, [], true);

    httpMock.expectNone(req => req.url.includes('/api/wishlist'));
    expect(service.isLoading()).toBeFalse();
  });

  it('stores guest wishlist items in localStorage without API calls', () => {
    configure(false);

    service.addToWishlist(product.id, product);

    const stored = JSON.parse(localStorage.getItem('climasite_wishlist') ?? '[]') as WishlistItem[];
    expect(stored).toHaveSize(1);
    expect(stored[0].productId).toBe(product.id);
    httpMock.expectNone(req => req.url.includes('/api/wishlist'));
  });

  it('clears authenticated wishlist on the server', () => {
    configure(true);
    httpMock.expectOne(apiUrl).flush(wishlistWithItem);

    service.clearWishlist();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('DELETE');
    req.flush(emptyWishlist);

    expect(service.itemCount()).toBe(0);
  });

  it('enables sharing and stores the returned share token', () => {
    configure(true);
    httpMock.expectOne(apiUrl).flush(emptyWishlist);

    service.setSharing(true).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/share`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isPublic: true });
    req.flush({
      ...emptyWishlist,
      isPublic: true,
      shareToken: 'share-token'
    });

    expect(service.isPublic()).toBeTrue();
    expect(service.shareToken()).toBe('share-token');
  });

  it('merges guest items into authenticated wishlist after login', () => {
    const guestItem: WishlistItem = {
      productId: product.id,
      addedAt: '2026-06-07T00:00:00Z',
      product
    };
    configure(false, [guestItem]);
    authenticated = true;

    let merged: WishlistDto | undefined;
    service.mergeWithUserWishlist().subscribe(result => {
      merged = result ?? undefined;
    });

    const getReq = httpMock.expectOne(apiUrl);
    expect(getReq.request.method).toBe('GET');
    getReq.flush(emptyWishlist);

    const addReq = httpMock.expectOne(`${apiUrl}/items/${product.id}`);
    expect(addReq.request.method).toBe('POST');
    addReq.flush(wishlistWithItem);

    expect(merged!.itemCount).toBe(1);
    expect(service.itemCount()).toBe(1);
  });

  it('reuses the in-flight backend sync for concurrent login merges', () => {
    const guestItem: WishlistItem = {
      productId: product.id,
      addedAt: '2026-06-07T00:00:00Z',
      product
    };
    configure(false, [guestItem]);
    authenticated = true;

    let first: WishlistDto | undefined;
    let second: WishlistDto | undefined;
    service.mergeWithUserWishlist().subscribe(result => {
      first = result ?? undefined;
    });
    service.mergeWithUserWishlist().subscribe(result => {
      second = result ?? undefined;
    });

    const getRequests = httpMock.match(apiUrl);
    expect(getRequests).toHaveSize(1);
    getRequests[0].flush(emptyWishlist);

    const addRequests = httpMock.match(`${apiUrl}/items/${product.id}`);
    expect(addRequests).toHaveSize(1);
    addRequests[0].flush(wishlistWithItem);

    expect(first!.itemCount).toBe(1);
    expect(second!.itemCount).toBe(1);
    expect(service.itemCount()).toBe(1);
  });

  it('loads shared wishlist without mutating local wishlist state', () => {
    configure(false);

    let shared: WishlistDto | undefined;
    service.getSharedWishlist('share-token').subscribe(result => {
      shared = result;
    });

    const req = httpMock.expectOne(`${apiUrl}/shared/share-token`);
    expect(req.request.method).toBe('GET');
    req.flush(wishlistWithItem);

    expect(shared!.itemCount).toBe(1);
    expect(service.itemCount()).toBe(0);
  });
});
