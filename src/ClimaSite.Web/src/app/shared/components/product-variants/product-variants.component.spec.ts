import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ProductVariantsComponent, VariantGroup, ProductVariantOption } from './product-variants.component';
import { TranslateModule } from '@ngx-translate/core';

describe('ProductVariantsComponent', () => {
  let component: ProductVariantsComponent;
  let fixture: ComponentFixture<ProductVariantsComponent>;

  const mockVariantGroups: VariantGroup[] = [
    {
      name: 'Capacity',
      type: 'buttons',
      options: [
        { id: '1', name: '9000 BTU', slug: 'ac-9000', value: '9000', unit: 'BTU', price: 499, inStock: true, isSelected: true },
        { id: '2', name: '12000 BTU', slug: 'ac-12000', value: '12000', unit: 'BTU', price: 699, inStock: true },
        { id: '3', name: '18000 BTU', slug: 'ac-18000', value: '18000', unit: 'BTU', price: 899, inStock: false }
      ]
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ProductVariantsComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductVariantsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('variantGroups', mockVariantGroups);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display variant groups', () => {
    const compiled = fixture.nativeElement;
    const groups = compiled.querySelectorAll('.variant-group');
    expect(groups.length).toBe(1);
  });

  it('should display variant options as buttons', () => {
    const compiled = fixture.nativeElement;
    const buttons = compiled.querySelectorAll('.variant-btn');
    expect(buttons.length).toBe(3);
  });

  it('should mark selected variant', () => {
    const compiled = fixture.nativeElement;
    const selectedBtn = compiled.querySelector('.variant-btn.selected');
    expect(selectedBtn).toBeTruthy();
    expect(selectedBtn.textContent).toContain('9000');
  });

  it('should mark out-of-stock variants', () => {
    const compiled = fixture.nativeElement;
    const outOfStockBtn = compiled.querySelector('.variant-btn.out-of-stock');
    expect(outOfStockBtn).toBeTruthy();
    expect(outOfStockBtn.textContent).toContain('18000');
  });

  it('should calculate price range correctly', () => {
    const range = component.priceRange();
    expect(range).toBeTruthy();
    expect(range!.min).toBe(499);
    expect(range!.max).toBe(899);
  });

  it('should emit variantSelected when option is clicked', () => {
    const emitSpy = spyOn(component.variantSelected, 'emit');
    const option = mockVariantGroups[0].options[1]; // 12000 BTU (in stock)

    const mockEvent = new Event('click');
    spyOn(mockEvent, 'preventDefault');

    component.onVariantSelect(option, mockEvent);

    expect(emitSpy).toHaveBeenCalledWith(option);
  });

  it('should not emit variantSelected for out-of-stock option', () => {
    const emitSpy = spyOn(component.variantSelected, 'emit');
    const option = mockVariantGroups[0].options[2]; // 18000 BTU (out of stock)

    const mockEvent = new Event('click');
    spyOn(mockEvent, 'preventDefault');

    component.onVariantSelect(option, mockEvent);

    expect(mockEvent.preventDefault).toHaveBeenCalled();
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should show price comparison when enabled', () => {
    fixture.componentRef.setInput('showPriceComparison', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const priceComparison = compiled.querySelector('.price-comparison');
    expect(priceComparison).toBeTruthy();
  });

  it('should display dropdown type variants', () => {
    const dropdownGroups: VariantGroup[] = [
      {
        name: 'Size',
        type: 'dropdown',
        options: [
          { id: '1', name: 'Small', slug: 'small', value: 'S', price: 99, inStock: true },
          { id: '2', name: 'Medium', slug: 'medium', value: 'M', price: 99, inStock: true }
        ]
      }
    ];

    fixture.componentRef.setInput('variantGroups', dropdownGroups);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const select = compiled.querySelector('.variant-select');
    expect(select).toBeTruthy();
  });

  it('should display swatch type variants', () => {
    const swatchGroups: VariantGroup[] = [
      {
        name: 'Color',
        type: 'swatches',
        options: [
          { id: '1', name: 'White', slug: 'white', value: '#ffffff', price: 499, inStock: true, isSelected: true },
          { id: '2', name: 'Black', slug: 'black', value: '#000000', price: 499, inStock: true }
        ]
      }
    ];

    fixture.componentRef.setInput('variantGroups', swatchGroups);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const swatches = compiled.querySelectorAll('.swatch');
    expect(swatches.length).toBe(2);
  });

  it('should use sale price in price range when available', () => {
    const groupsWithSale: VariantGroup[] = [
      {
        name: 'Capacity',
        type: 'buttons',
        options: [
          { id: '1', name: '9000 BTU', slug: 'ac-9000', value: '9000', price: 599, salePrice: 499, inStock: true },
          { id: '2', name: '12000 BTU', slug: 'ac-12000', value: '12000', price: 799, inStock: true }
        ]
      }
    ];

    fixture.componentRef.setInput('variantGroups', groupsWithSale);
    fixture.detectChanges();

    const range = component.priceRange();
    expect(range!.min).toBe(499); // Sale price
    expect(range!.max).toBe(799);
  });

  it('should return null for price range when no groups', () => {
    fixture.componentRef.setInput('variantGroups', []);
    fixture.detectChanges();

    expect(component.priceRange()).toBeNull();
  });
});
