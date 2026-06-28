import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { MiniCartItemComponent } from './mini-cart-item.component';
import { CartItem } from '../../../core/models/cart.model';

describe('MiniCartItemComponent', () => {
  let component: MiniCartItemComponent;
  let fixture: ComponentFixture<MiniCartItemComponent>;

  const baseItem: CartItem = {
    id: 'ci-1',
    productId: 'prod-1',
    productName: 'Arctic Pro 12000 BTU',
    productSlug: 'arctic-pro-12000',
    sku: 'AP-12000',
    imageUrl: 'https://example.com/ac.jpg',
    unitPrice: 599.99,
    quantity: 2,
    subtotal: 1199.98,
    maxQuantity: 5
  };

  function setItem(overrides: Partial<CartItem> = {}, isLoading = false): void {
    fixture.componentRef.setInput('item', { ...baseItem, ...overrides });
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.detectChanges();
  }

  const q = (sel: string) => fixture.nativeElement.querySelector(sel) as HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MiniCartItemComponent, RouterTestingModule, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(MiniCartItemComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    setItem();
    expect(component).toBeTruthy();
  });

  describe('Rendering', () => {
    it('should render the product name', () => {
      setItem();
      expect(q('.item-name').textContent).toContain('Arctic Pro 12000 BTU');
    });

    it('should render the current quantity', () => {
      setItem({ quantity: 3 });
      expect(q('.qty-value').textContent).toContain('3');
    });

    it('should render the product image when imageUrl is present', () => {
      setItem();
      const img = q('.item-image img') as HTMLImageElement;
      expect(img).toBeTruthy();
      expect(img.getAttribute('src')).toBe('https://example.com/ac.jpg');
    });

    it('should render the no-image placeholder when imageUrl is absent', () => {
      setItem({ imageUrl: undefined });
      expect(q('.item-image img')).toBeNull();
      expect(q('.no-image')).toBeTruthy();
    });

    it('should render the variant name only when present', () => {
      setItem();
      expect(q('.item-variant')).toBeNull();

      setItem({ variantName: 'White / 3.5kW' });
      expect(q('.item-variant').textContent).toContain('White / 3.5kW');
    });
  });

  describe('Price display', () => {
    it('should show only the regular price when there is no sale price', () => {
      setItem({ salePrice: undefined });
      expect(q('.price-regular')).toBeTruthy();
      expect(q('.price-sale')).toBeNull();
      expect(q('.price-original')).toBeNull();
    });

    it('should show sale + struck-through original when salePrice exceeds unitPrice', () => {
      // Contract in template: salePrice is the (higher) compare-at price, unitPrice the current price.
      setItem({ unitPrice: 499.99, salePrice: 599.99 });
      expect(q('.price-sale')).toBeTruthy();
      expect(q('.price-original')).toBeTruthy();
      expect(q('.price-regular')).toBeNull();
    });

    it('should NOT show the sale layout when salePrice is not greater than unitPrice', () => {
      setItem({ unitPrice: 599.99, salePrice: 599.99 });
      expect(q('.price-regular')).toBeTruthy();
      expect(q('.price-sale')).toBeNull();
    });
  });

  describe('Quantity controls', () => {
    it('should disable decrease button when quantity is 1', () => {
      setItem({ quantity: 1 });
      const dec = q('[data-testid="qty-decrease"]') as HTMLButtonElement;
      expect(dec.disabled).toBeTrue();
    });

    it('should enable decrease button when quantity is above 1', () => {
      setItem({ quantity: 2 });
      const dec = q('[data-testid="qty-decrease"]') as HTMLButtonElement;
      expect(dec.disabled).toBeFalse();
    });

    it('should disable increase button when quantity equals maxQuantity', () => {
      setItem({ quantity: 5, maxQuantity: 5 });
      const inc = q('[data-testid="qty-increase"]') as HTMLButtonElement;
      expect(inc.disabled).toBeTrue();
    });

    it('should enable increase button below maxQuantity', () => {
      setItem({ quantity: 2, maxQuantity: 5 });
      const inc = q('[data-testid="qty-increase"]') as HTMLButtonElement;
      expect(inc.disabled).toBeFalse();
    });
  });

  describe('decreaseQuantity()', () => {
    it('should emit quantityChange with quantity - 1 when above 1', () => {
      setItem({ quantity: 3 });
      spyOn(component.quantityChange, 'emit');
      component.decreaseQuantity();
      expect(component.quantityChange.emit).toHaveBeenCalledOnceWith(2);
    });

    it('should NOT emit when quantity is already 1', () => {
      setItem({ quantity: 1 });
      spyOn(component.quantityChange, 'emit');
      component.decreaseQuantity();
      expect(component.quantityChange.emit).not.toHaveBeenCalled();
    });

    it('should emit when the decrease button is clicked', () => {
      setItem({ quantity: 4 });
      spyOn(component.quantityChange, 'emit');
      (q('[data-testid="qty-decrease"]') as HTMLButtonElement).click();
      expect(component.quantityChange.emit).toHaveBeenCalledOnceWith(3);
    });
  });

  describe('increaseQuantity()', () => {
    it('should emit quantityChange with quantity + 1 when below max', () => {
      setItem({ quantity: 2, maxQuantity: 5 });
      spyOn(component.quantityChange, 'emit');
      component.increaseQuantity();
      expect(component.quantityChange.emit).toHaveBeenCalledOnceWith(3);
    });

    it('should NOT emit when quantity is at max', () => {
      setItem({ quantity: 5, maxQuantity: 5 });
      spyOn(component.quantityChange, 'emit');
      component.increaseQuantity();
      expect(component.quantityChange.emit).not.toHaveBeenCalled();
    });
  });

  describe('onRemove()', () => {
    it('should emit the remove output', () => {
      setItem();
      spyOn(component.remove, 'emit');
      component.onRemove();
      expect(component.remove.emit).toHaveBeenCalledTimes(1);
    });

    it('should emit remove when the remove button is clicked', () => {
      setItem();
      spyOn(component.remove, 'emit');
      (q('[data-testid="item-remove"]') as HTMLButtonElement).click();
      expect(component.remove.emit).toHaveBeenCalledTimes(1);
    });
  });

  describe('Loading state', () => {
    it('should add is-loading class and show the overlay when loading', () => {
      setItem({}, true);
      expect(q('.mini-cart-item').classList.contains('is-loading')).toBeTrue();
      expect(q('.loading-overlay')).toBeTruthy();
    });

    it('should disable all action buttons while loading', () => {
      setItem({ quantity: 3, maxQuantity: 5 }, true);
      expect((q('[data-testid="qty-decrease"]') as HTMLButtonElement).disabled).toBeTrue();
      expect((q('[data-testid="qty-increase"]') as HTMLButtonElement).disabled).toBeTrue();
      expect((q('[data-testid="item-remove"]') as HTMLButtonElement).disabled).toBeTrue();
    });

    it('should not show the overlay when not loading', () => {
      setItem({}, false);
      expect(q('.loading-overlay')).toBeNull();
    });
  });
});
