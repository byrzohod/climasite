import { ComponentFixture, TestBed, fakeAsync, tick, flush } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { signal, WritableSignal } from '@angular/core';
import { of, Subject } from 'rxjs';

import { MiniCartDrawerComponent } from './mini-cart-drawer.component';
import { MiniCartItemComponent } from './mini-cart-item.component';
import { CartService } from '../../../core/services/cart.service';
import { Cart, CartItem } from '../../../core/models/cart.model';

describe('MiniCartDrawerComponent', () => {
  let component: MiniCartDrawerComponent;
  let fixture: ComponentFixture<MiniCartDrawerComponent>;
  
  // Writable signals for the mock
  let mockItems: WritableSignal<CartItem[]>;
  let mockItemCount: WritableSignal<number>;
  let mockSubtotal: WritableSignal<number>;
  let mockIsEmpty: WritableSignal<boolean>;
  let mockIsLoading: WritableSignal<boolean>;

  const mockCartItem: CartItem = {
    id: 'item-1',
    productId: 'prod-1',
    productName: 'Test Air Conditioner',
    productSlug: 'test-air-conditioner',
    sku: 'AC-001',
    imageUrl: 'https://example.com/image.jpg',
    unitPrice: 599.99,
    quantity: 2,
    subtotal: 1199.98,
    maxQuantity: 10
  };

  const mockCart: Cart = {
    id: 'cart-1',
    sessionId: 'session-1',
    items: [mockCartItem],
    subtotal: 1199.98,
    shipping: 0,
    tax: 0,
    total: 1199.98,
    itemCount: 2,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };
  
  function createCartServiceMock(itemsValue: CartItem[] = [mockCartItem]) {
    mockItems = signal(itemsValue);
    mockItemCount = signal(itemsValue.reduce((sum, i) => sum + i.quantity, 0));
    mockSubtotal = signal(itemsValue.reduce((sum, i) => sum + i.subtotal, 0));
    mockIsEmpty = signal(itemsValue.length === 0);
    mockIsLoading = signal(false);
    
    return {
      items: mockItems.asReadonly(),
      itemCount: mockItemCount.asReadonly(),
      subtotal: mockSubtotal.asReadonly(),
      isEmpty: mockIsEmpty.asReadonly(),
      isLoading: mockIsLoading.asReadonly(),
      updateQuantity: jasmine.createSpy('updateQuantity').and.returnValue(of(mockCart)),
      removeItem: jasmine.createSpy('removeItem').and.returnValue(of({ ...mockCart, items: [], itemCount: 0 }))
    };
  }

  describe('with cart items', () => {
    let cartServiceMock: ReturnType<typeof createCartServiceMock>;

    beforeEach(async () => {
      cartServiceMock = createCartServiceMock([mockCartItem]);
      
      await TestBed.configureTestingModule({
        imports: [
          MiniCartDrawerComponent,
          MiniCartItemComponent,
          NoopAnimationsModule,
          RouterTestingModule,
          TranslateModule.forRoot()
        ],
        providers: [
          { provide: CartService, useValue: cartServiceMock }
        ]
      }).compileComponents();

      fixture = TestBed.createComponent(MiniCartDrawerComponent);
      component = fixture.componentInstance;
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    describe('when closed', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', false);
        fixture.detectChanges();
      });

      it('should not render the drawer', () => {
        const drawer = fixture.nativeElement.querySelector('[data-testid="mini-cart-drawer"]');
        expect(drawer).toBeNull();
      });

      it('should not render the backdrop', () => {
        const backdrop = fixture.nativeElement.querySelector('[data-testid="mini-cart-backdrop"]');
        expect(backdrop).toBeNull();
      });
    });

    describe('when open', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should render the drawer', () => {
        const drawer = fixture.nativeElement.querySelector('[data-testid="mini-cart-drawer"]');
        expect(drawer).toBeTruthy();
      });

      it('should render the backdrop', () => {
        const backdrop = fixture.nativeElement.querySelector('[data-testid="mini-cart-backdrop"]');
        expect(backdrop).toBeTruthy();
      });

      it('should display cart item count in header', () => {
        const itemCount = fixture.nativeElement.querySelector('.item-count');
        expect(itemCount.textContent).toContain('(2)');
      });

      it('should display cart items', () => {
        const items = fixture.nativeElement.querySelectorAll('[data-testid="mini-cart-item"]');
        expect(items.length).toBe(1);
      });

      it('should display subtotal', () => {
        const subtotal = fixture.nativeElement.querySelector('.subtotal-amount');
        expect(subtotal.textContent).toContain('1,199.98');
      });

      it('should have view cart button', () => {
        const viewCartBtn = fixture.nativeElement.querySelector('[data-testid="mini-cart-view-cart"]');
        expect(viewCartBtn).toBeTruthy();
      });

      it('should have checkout button', () => {
        const checkoutBtn = fixture.nativeElement.querySelector('[data-testid="mini-cart-checkout"]');
        expect(checkoutBtn).toBeTruthy();
      });
    });

    describe('closing behavior', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should emit closed event when close button clicked', () => {
        spyOn(component.closed, 'emit');
        
        const closeBtn = fixture.nativeElement.querySelector('[data-testid="mini-cart-close"]');
        closeBtn.click();
        
        expect(component.closed.emit).toHaveBeenCalled();
      });

      it('should emit closed event when backdrop clicked', () => {
        spyOn(component.closed, 'emit');
        
        const backdrop = fixture.nativeElement.querySelector('[data-testid="mini-cart-backdrop"]');
        backdrop.click();
        
        expect(component.closed.emit).toHaveBeenCalled();
      });
    });

    describe('quantity updates', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should call updateQuantity when quantity changes', () => {
        component.updateQuantity('item-1', 3);
        
        expect(cartServiceMock.updateQuantity).toHaveBeenCalledWith('item-1', 3);
      });

      it('should not update quantity if less than 1', () => {
        component.updateQuantity('item-1', 0);
        
        expect(cartServiceMock.updateQuantity).not.toHaveBeenCalled();
      });
    });

    describe('item removal', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should call removeItem when remove is triggered', () => {
        component.removeItem('item-1');
        
        expect(cartServiceMock.removeItem).toHaveBeenCalledWith('item-1');
      });
    });

    describe('accessibility', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should have role="dialog" on drawer', () => {
        const drawer = fixture.nativeElement.querySelector('[data-testid="mini-cart-drawer"]');
        expect(drawer.getAttribute('role')).toBe('dialog');
      });

      it('should have aria-modal="true" on drawer', () => {
        const drawer = fixture.nativeElement.querySelector('[data-testid="mini-cart-drawer"]');
        expect(drawer.getAttribute('aria-modal')).toBe('true');
      });

      it('should have aria-label on close button', () => {
        const closeBtn = fixture.nativeElement.querySelector('[data-testid="mini-cart-close"]');
        expect(closeBtn.hasAttribute('aria-label')).toBe(true);
      });

      it('should have aria-hidden="true" on backdrop', () => {
        const backdrop = fixture.nativeElement.querySelector('[data-testid="mini-cart-backdrop"]');
        expect(backdrop.getAttribute('aria-hidden')).toBe('true');
      });
    });

    describe('loading states', () => {
      beforeEach(() => {
        fixture.componentRef.setInput('isOpen', true);
        fixture.detectChanges();
      });

      it('should start with no items loading', () => {
        expect(component.isItemLoading('item-1')).toBeFalse();
      });

      it('should track loading state correctly', () => {
        expect(component.loadingItems().size).toBe(0);
      });
    });
  });

  describe('with empty cart', () => {
    let cartServiceMock: ReturnType<typeof createCartServiceMock>;

    beforeEach(async () => {
      // Create mock with empty items
      cartServiceMock = createCartServiceMock([]);
      
      await TestBed.configureTestingModule({
        imports: [
          MiniCartDrawerComponent,
          MiniCartItemComponent,
          NoopAnimationsModule,
          RouterTestingModule,
          TranslateModule.forRoot()
        ],
        providers: [
          { provide: CartService, useValue: cartServiceMock }
        ]
      }).compileComponents();

      fixture = TestBed.createComponent(MiniCartDrawerComponent);
      component = fixture.componentInstance;
      fixture.componentRef.setInput('isOpen', true);
      fixture.detectChanges();
    });

    it('should display empty cart message', () => {
      const emptyCart = fixture.nativeElement.querySelector('[data-testid="mini-cart-empty"]');
      expect(emptyCart).toBeTruthy();
    });

    it('should display shop link', () => {
      const shopLink = fixture.nativeElement.querySelector('[data-testid="mini-cart-shop-link"]');
      expect(shopLink).toBeTruthy();
    });

    it('should not display footer', () => {
      const footer = fixture.nativeElement.querySelector('[data-testid="mini-cart-footer"]');
      expect(footer).toBeNull();
    });

    it('should not display cart items', () => {
      const items = fixture.nativeElement.querySelector('[data-testid="mini-cart-items"]');
      expect(items).toBeNull();
    });
  });
});

describe('MiniCartItemComponent', () => {
  let component: MiniCartItemComponent;
  let fixture: ComponentFixture<MiniCartItemComponent>;

  const mockItem: CartItem = {
    id: 'item-1',
    productId: 'prod-1',
    productName: 'Test Air Conditioner',
    productSlug: 'test-air-conditioner',
    sku: 'AC-001',
    imageUrl: 'https://example.com/image.jpg',
    unitPrice: 599.99,
    salePrice: 499.99,
    quantity: 2,
    subtotal: 999.98,
    maxQuantity: 10
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        MiniCartItemComponent,
        RouterTestingModule,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MiniCartItemComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('item', mockItem);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display product name', () => {
    const name = fixture.nativeElement.querySelector('.item-name');
    expect(name.textContent).toContain('Test Air Conditioner');
  });

  it('should display product image', () => {
    const img = fixture.nativeElement.querySelector('.item-image img');
    expect(img).toBeTruthy();
    expect(img.getAttribute('src')).toBe('https://example.com/image.jpg');
  });

  it('should display sale price when available', () => {
    const salePrice = fixture.nativeElement.querySelector('.price-sale');
    const originalPrice = fixture.nativeElement.querySelector('.price-original');
    
    expect(salePrice.textContent).toContain('499.99');
    expect(originalPrice.textContent).toContain('599.99');
  });

  it('should display quantity', () => {
    const qty = fixture.nativeElement.querySelector('.qty-value');
    expect(qty.textContent.trim()).toBe('2');
  });

  describe('quantity controls', () => {
    it('should emit quantityChange when increase clicked', () => {
      spyOn(component.quantityChange, 'emit');
      
      const increaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-increase"]');
      increaseBtn.click();
      
      expect(component.quantityChange.emit).toHaveBeenCalledWith(3);
    });

    it('should emit quantityChange when decrease clicked', () => {
      spyOn(component.quantityChange, 'emit');
      
      const decreaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-decrease"]');
      decreaseBtn.click();
      
      expect(component.quantityChange.emit).toHaveBeenCalledWith(1);
    });

    it('should disable decrease button when quantity is 1', () => {
      fixture.componentRef.setInput('item', { ...mockItem, quantity: 1 });
      fixture.detectChanges();
      
      const decreaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-decrease"]');
      expect(decreaseBtn.disabled).toBeTrue();
    });

    it('should disable increase button when at max quantity', () => {
      fixture.componentRef.setInput('item', { ...mockItem, quantity: 10 });
      fixture.detectChanges();
      
      const increaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-increase"]');
      expect(increaseBtn.disabled).toBeTrue();
    });
  });

  describe('remove item', () => {
    it('should emit remove event when remove button clicked', () => {
      spyOn(component.remove, 'emit');
      
      const removeBtn = fixture.nativeElement.querySelector('[data-testid="item-remove"]');
      removeBtn.click();
      
      expect(component.remove.emit).toHaveBeenCalled();
    });
  });

  describe('loading state', () => {
    it('should show loading overlay when isLoading is true', () => {
      fixture.componentRef.setInput('isLoading', true);
      fixture.detectChanges();
      
      const overlay = fixture.nativeElement.querySelector('.loading-overlay');
      expect(overlay).toBeTruthy();
    });

    it('should disable buttons when loading', () => {
      fixture.componentRef.setInput('isLoading', true);
      fixture.detectChanges();
      
      const increaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-increase"]');
      const decreaseBtn = fixture.nativeElement.querySelector('[data-testid="qty-decrease"]');
      const removeBtn = fixture.nativeElement.querySelector('[data-testid="item-remove"]');
      
      expect(increaseBtn.disabled).toBeTrue();
      expect(decreaseBtn.disabled).toBeTrue();
      expect(removeBtn.disabled).toBeTrue();
    });
  });

  describe('variant display', () => {
    it('should display variant name when present', () => {
      fixture.componentRef.setInput('item', { ...mockItem, variantName: 'White / 12000 BTU' });
      fixture.detectChanges();
      
      const variant = fixture.nativeElement.querySelector('.item-variant');
      expect(variant.textContent).toContain('White / 12000 BTU');
    });

    it('should not display variant element when not present', () => {
      fixture.componentRef.setInput('item', { ...mockItem, variantName: undefined });
      fixture.detectChanges();
      
      const variant = fixture.nativeElement.querySelector('.item-variant');
      expect(variant).toBeNull();
    });
  });

  describe('regular price display', () => {
    it('should display only regular price when no sale price', () => {
      fixture.componentRef.setInput('item', { ...mockItem, salePrice: undefined });
      fixture.detectChanges();
      
      const regularPrice = fixture.nativeElement.querySelector('.price-regular');
      const salePrice = fixture.nativeElement.querySelector('.price-sale');
      
      expect(regularPrice).toBeTruthy();
      expect(salePrice).toBeNull();
    });

    it('should display only regular price when sale price equals unit price', () => {
      fixture.componentRef.setInput('item', { ...mockItem, salePrice: 599.99 });
      fixture.detectChanges();
      
      const regularPrice = fixture.nativeElement.querySelector('.price-regular');
      const salePrice = fixture.nativeElement.querySelector('.price-sale');
      
      expect(regularPrice).toBeTruthy();
      expect(salePrice).toBeNull();
    });
  });

  describe('no image state', () => {
    it('should display placeholder when no image URL', () => {
      fixture.componentRef.setInput('item', { ...mockItem, imageUrl: undefined });
      fixture.detectChanges();
      
      const noImage = fixture.nativeElement.querySelector('.no-image');
      expect(noImage).toBeTruthy();
    });
  });
});
