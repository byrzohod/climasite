import { ComponentFixture, TestBed, fakeAsync, tick, flush } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FrequentlyBoughtComponent, BundleProduct } from './frequently-bought.component';
import { TranslateModule } from '@ngx-translate/core';

describe('FrequentlyBoughtComponent', () => {
  let component: FrequentlyBoughtComponent;
  let fixture: ComponentFixture<FrequentlyBoughtComponent>;

  const mockMainProduct: BundleProduct = {
    id: 'main-1',
    name: 'Main Air Conditioner',
    slug: 'main-ac',
    imageUrl: 'https://example.com/main.jpg',
    price: 599
  };

  const mockBundleProducts: BundleProduct[] = [
    {
      id: 'bundle-1',
      name: 'Installation Kit',
      slug: 'install-kit',
      imageUrl: 'https://example.com/kit.jpg',
      price: 49
    },
    {
      id: 'bundle-2',
      name: 'Remote Control',
      slug: 'remote',
      imageUrl: 'https://example.com/remote.jpg',
      price: 29,
      salePrice: 19
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FrequentlyBoughtComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FrequentlyBoughtComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('mainProduct', mockMainProduct);
    fixture.componentRef.setInput('bundleProducts', mockBundleProducts);
    fixture.detectChanges();
    // Wait for effect to run
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display main product', () => {
    const compiled = fixture.nativeElement;
    const mainProduct = compiled.querySelector('.main-product');
    expect(mainProduct).toBeTruthy();
    expect(mainProduct.textContent).toContain('Main Air Conditioner');
  });

  it('should display bundle products', () => {
    const compiled = fixture.nativeElement;
    const bundleProducts = compiled.querySelectorAll('.bundle-product:not(.main-product)');
    expect(bundleProducts.length).toBe(2);
  });

  it('should select all bundle products by default', () => {
    expect(component.selectedCount()).toBe(2);
  });

  it('should toggle product selection', () => {
    expect(component.isSelected('bundle-1')).toBeTrue();

    component.toggleProduct('bundle-1');
    fixture.detectChanges();

    expect(component.isSelected('bundle-1')).toBeFalse();
    expect(component.selectedCount()).toBe(1);
  });

  it('should calculate bundle total with discount', () => {
    // Main: 599, Bundle1: 49, Bundle2: 19 (sale) = 667
    // With 10% discount: 667 * 0.9 = 600.3
    const total = component.bundleTotal();
    expect(total).toBeCloseTo(600.3, 1);
  });

  it('should calculate bundle savings', () => {
    // Subtotal: 667, 10% savings = 66.7
    const savings = component.bundleSavings();
    expect(savings).toBeCloseTo(66.7, 1);
  });

  it('should use sale price when available', () => {
    // Bundle2 has salePrice of 19 instead of 29
    const total = component.bundleTotal();
    // Main: 599 + Bundle1: 49 + Bundle2: 19 = 667 * 0.9 = 600.3
    expect(total).toBeCloseTo(600.3, 1);
  });

  it('should return only main product price when no bundle selected', () => {
    // Deselect all bundle products
    component.toggleProduct('bundle-1');
    component.toggleProduct('bundle-2');
    fixture.detectChanges();

    expect(component.bundleTotal()).toBe(599); // Just main product, no discount
    expect(component.bundleSavings()).toBe(0);
  });

  it('should emit addToCart with selected product IDs', fakeAsync(() => {
    const emitSpy = spyOn(component.addToCart, 'emit');

    component.addBundleToCart();
    tick(1000);

    expect(emitSpy).toHaveBeenCalledWith(['main-1', 'bundle-1', 'bundle-2']);
  }));

  it('should emit addToCart with only main and selected products', fakeAsync(() => {
    component.toggleProduct('bundle-1'); // Deselect bundle-1
    fixture.detectChanges();

    const emitSpy = spyOn(component.addToCart, 'emit');

    component.addBundleToCart();
    tick(1000);

    expect(emitSpy).toHaveBeenCalledWith(['main-1', 'bundle-2']);
  }));

  it('should apply custom bundle discount', () => {
    fixture.componentRef.setInput('bundleDiscount', 15);
    fixture.detectChanges();
    fixture.detectChanges(); // For effect

    // Main: 599, Bundle1: 49, Bundle2: 19 = 667
    // With 15% discount: 667 * 0.85 = 566.95
    const total = component.bundleTotal();
    expect(total).toBeCloseTo(566.95, 1);
  });

  it('should show loading state when adding to cart', fakeAsync(() => {
    expect(component.isAddingToCart()).toBeFalse();

    component.addBundleToCart();
    expect(component.isAddingToCart()).toBeTrue();

    tick(1000);
    expect(component.isAddingToCart()).toBeFalse();
  }));

  it('should render add bundle button', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('[data-testid="add-bundle-to-cart"]');
    expect(button).toBeTruthy();
  });
});
