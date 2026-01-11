import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CartService } from './cart.service';
import { Cart, CartItem } from '../models/cart.model';
import { environment } from '../../../environments/environment';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/cart`;

  const mockCartItem: CartItem = {
    id: 'item-1',
    productId: 'product-1',
    productName: 'Test Product',
    productSlug: 'test-product',
    sku: 'TEST-001',
    unitPrice: 99.99,
    quantity: 2,
    subtotal: 199.98,
    maxQuantity: 10
  };

  const mockCart: Cart = {
    id: 'cart-1',
    sessionId: 'sess_test123',
    items: [mockCartItem],
    subtotal: 199.98,
    shipping: 10,
    tax: 20,
    total: 229.98,
    itemCount: 2,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z'
  };

  const emptyCart: Cart = {
    id: '',
    sessionId: 'sess_test123',
    items: [],
    subtotal: 0,
    shipping: 0,
    tax: 0,
    total: 0,
    itemCount: 0,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z'
  };

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('climasite_session_id', 'sess_test123');

    TestBed.configureTestingModule({
      providers: [
        CartService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    service = TestBed.inject(CartService);

    // Handle initial loadCart call
    const req = httpMock.expectOne(req =>
      req.url.includes('/api/cart') && req.method === 'GET'
    );
    req.flush(emptyCart);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('initialization', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should have default empty state', () => {
      expect(service.cart()).toEqual(emptyCart);
      expect(service.items()).toEqual([]);
      expect(service.itemCount()).toBe(0);
      expect(service.isEmpty()).toBeTrue();
    });

    it('should use existing session ID from localStorage', () => {
      expect(localStorage.getItem('climasite_session_id')).toBe('sess_test123');
    });
  });

  describe('loadCart', () => {
    it('should load cart from API', fakeAsync(() => {
      service.loadCart();
      tick();

      const req = httpMock.expectOne(req =>
        req.url.includes('/api/cart') &&
        req.url.includes('guestSessionId=sess_test123')
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockCart);
      tick();

      expect(service.cart()).toEqual(mockCart);
      expect(service.items().length).toBe(1);
      expect(service.itemCount()).toBe(2);
      expect(service.subtotal()).toBe(199.98);
      expect(service.total()).toBe(229.98);
      expect(service.isEmpty()).toBeFalse();
    }));

    it('should create empty cart on error', fakeAsync(() => {
      service.loadCart();
      tick();

      const req = httpMock.expectOne(req =>
        req.url.includes('/api/cart')
      );
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(service.cart()).toBeTruthy();
      expect(service.items()).toEqual([]);
      expect(service.isLoading()).toBeFalse();
    }));
  });

  describe('addToCart', () => {
    it('should add item to cart', fakeAsync(() => {
      service.addToCart('product-1', 2).subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/items`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        productId: 'product-1',
        quantity: 2,
        variantId: undefined,
        guestSessionId: 'sess_test123'
      });
      req.flush(mockCart);
      tick();

      expect(service.cart()).toEqual(mockCart);
      expect(service.itemCount()).toBe(2);
    }));

    it('should add item with variant', fakeAsync(() => {
      service.addToCart('product-1', 1, 'variant-1').subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/items`);
      expect(req.request.body.variantId).toBe('variant-1');
      req.flush(mockCart);
    }));

    it('should set error on failure', fakeAsync(() => {
      let errorOccurred = false;
      service.addToCart('product-1', 1).subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(`${apiUrl}/items`);
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBe('Failed to add item to cart');
      expect(service.isLoading()).toBeFalse();
    }));

    it('should set loading state during request', fakeAsync(() => {
      service.addToCart('product-1', 1).subscribe();
      // Don't tick yet - check loading state

      const req = httpMock.expectOne(`${apiUrl}/items`);
      expect(service.isLoading()).toBeTrue();

      req.flush(mockCart);
      tick();

      expect(service.isLoading()).toBeFalse();
    }));
  });

  describe('updateQuantity', () => {
    it('should update item quantity', fakeAsync(() => {
      const updatedCart = { ...mockCart, itemCount: 5 };

      service.updateQuantity('item-1', 5).subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/items/item-1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({
        quantity: 5,
        guestSessionId: 'sess_test123'
      });
      req.flush(updatedCart);
      tick();

      expect(service.itemCount()).toBe(5);
    }));

    it('should set error on failure', fakeAsync(() => {
      let errorOccurred = false;
      service.updateQuantity('item-1', 5).subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(`${apiUrl}/items/item-1`);
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBe('Failed to update item quantity');
    }));
  });

  describe('removeItem', () => {
    it('should remove item from cart', fakeAsync(() => {
      service.removeItem('item-1').subscribe();
      tick();

      const req = httpMock.expectOne(req =>
        req.url.includes(`${apiUrl}/items/item-1`) &&
        req.url.includes('guestSessionId=sess_test123')
      );
      expect(req.request.method).toBe('DELETE');
      req.flush(emptyCart);
      tick();

      expect(service.items()).toEqual([]);
      expect(service.itemCount()).toBe(0);
    }));

    it('should set error on failure', fakeAsync(() => {
      let errorOccurred = false;
      service.removeItem('item-1').subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(req => req.url.includes(`${apiUrl}/items/item-1`));
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBe('Failed to remove item from cart');
    }));
  });

  describe('clearCart', () => {
    it('should clear all items from cart', fakeAsync(() => {
      service.clearCart().subscribe();
      tick();

      const req = httpMock.expectOne(req =>
        req.url.includes(apiUrl) &&
        req.url.includes('guestSessionId=sess_test123') &&
        !req.url.includes('/items')
      );
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      tick();

      expect(service.items()).toEqual([]);
      expect(service.itemCount()).toBe(0);
      expect(service.isEmpty()).toBeTrue();
    }));

    it('should set error on failure', fakeAsync(() => {
      let errorOccurred = false;
      service.clearCart().subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(req =>
        req.url.includes(apiUrl) &&
        !req.url.includes('/items')
      );
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(errorOccurred).toBeTrue();
      expect(service.error()).toBe('Failed to clear cart');
    }));
  });

  describe('getSummary', () => {
    it('should return cart summary', fakeAsync(() => {
      service.loadCart();
      tick();

      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(mockCart);
      tick();

      const summary = service.getSummary();
      expect(summary).toEqual({
        itemCount: 2,
        subtotal: 199.98,
        total: 229.98
      });
    }));
  });

  describe('mergeCart', () => {
    it('should merge guest cart with user cart', fakeAsync(() => {
      service.mergeCart('user-1').subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/merge`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ userId: 'user-1' });
      expect(req.request.headers.get('X-Session-Id')).toBe('sess_test123');
      req.flush(mockCart);
      tick();

      expect(service.cart()).toEqual(mockCart);
    }));

    it('should throw error on failure', fakeAsync(() => {
      let errorOccurred = false;
      service.mergeCart('user-1').subscribe({
        error: () => { errorOccurred = true; }
      });
      tick();

      const req = httpMock.expectOne(`${apiUrl}/merge`);
      req.error(new ErrorEvent('Network error'));
      tick();

      expect(errorOccurred).toBeTrue();
    }));
  });

  describe('isInCart', () => {
    it('should return true if product is in cart', fakeAsync(() => {
      service.loadCart();
      tick();

      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(mockCart);
      tick();

      expect(service.isInCart('product-1')).toBeTrue();
      expect(service.isInCart('product-2')).toBeFalse();
    }));

    it('should check variant ID if provided', fakeAsync(() => {
      const cartWithVariant: Cart = {
        ...mockCart,
        items: [{ ...mockCartItem, variantId: 'variant-1' }]
      };

      service.loadCart();
      tick();

      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(cartWithVariant);
      tick();

      expect(service.isInCart('product-1', 'variant-1')).toBeTrue();
      expect(service.isInCart('product-1', 'variant-2')).toBeFalse();
    }));
  });

  describe('getItemQuantity', () => {
    it('should return quantity of product in cart', fakeAsync(() => {
      service.loadCart();
      tick();

      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(mockCart);
      tick();

      expect(service.getItemQuantity('product-1')).toBe(2);
      expect(service.getItemQuantity('product-2')).toBe(0);
    }));

    it('should check variant ID if provided', fakeAsync(() => {
      const cartWithVariant: Cart = {
        ...mockCart,
        items: [{ ...mockCartItem, variantId: 'variant-1', quantity: 3 }]
      };

      service.loadCart();
      tick();

      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(cartWithVariant);
      tick();

      expect(service.getItemQuantity('product-1', 'variant-1')).toBe(3);
      expect(service.getItemQuantity('product-1', 'variant-2')).toBe(0);
    }));
  });

  describe('computed signals', () => {
    beforeEach(fakeAsync(() => {
      service.loadCart();
      tick();
      const req = httpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(mockCart);
      tick();
    }));

    it('should compute items from cart', () => {
      expect(service.items()).toEqual(mockCart.items);
    });

    it('should compute itemCount from cart', () => {
      expect(service.itemCount()).toBe(mockCart.itemCount);
    });

    it('should compute subtotal from cart', () => {
      expect(service.subtotal()).toBe(mockCart.subtotal);
    });

    it('should compute total from cart', () => {
      expect(service.total()).toBe(mockCart.total);
    });

    it('should compute isEmpty correctly', () => {
      expect(service.isEmpty()).toBeFalse();
    });
  });

  describe('session management', () => {
    it('should generate new session ID if none exists', () => {
      localStorage.removeItem('climasite_session_id');

      // Create new service instance
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          CartService,
          provideHttpClient(),
          provideHttpClientTesting()
        ]
      });

      const newHttpMock = TestBed.inject(HttpTestingController);
      const newService = TestBed.inject(CartService);

      const req = newHttpMock.expectOne(req => req.url.includes('/api/cart'));
      req.flush(emptyCart);

      const storedSessionId = localStorage.getItem('climasite_session_id');
      expect(storedSessionId).toBeTruthy();
      expect(storedSessionId).toMatch(/^sess_/);

      newHttpMock.verify();
    });
  });
});
