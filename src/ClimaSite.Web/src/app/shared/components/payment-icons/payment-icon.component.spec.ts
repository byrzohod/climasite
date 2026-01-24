import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import {
  PaymentIconComponent,
  PaymentBrand,
  PaymentIconSize,
  PAYMENT_ICON_SIZE_MAP
} from './payment-icon.component';

// Test host component to test input bindings
@Component({
  standalone: true,
  imports: [PaymentIconComponent],
  template: `
    <app-payment-icon
      [brand]="brand"
      [size]="size"
      [grayscale]="grayscale"
      [label]="label"
    />
  `
})
class TestHostComponent {
  brand: PaymentBrand = 'visa';
  size: PaymentIconSize = 'md';
  grayscale = false;
  label = '';
}

describe('PaymentIconComponent', () => {
  let component: PaymentIconComponent;
  let fixture: ComponentFixture<PaymentIconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentIconComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentIconComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.componentRef.setInput('brand', 'visa');
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    const brands: PaymentBrand[] = [
      'visa',
      'mastercard',
      'amex',
      'paypal',
      'apple-pay',
      'google-pay',
      'bank',
      'card-generic'
    ];

    brands.forEach(brand => {
      it(`should render SVG for ${brand}`, () => {
        fixture.componentRef.setInput('brand', brand);
        fixture.detectChanges();

        const svg = fixture.debugElement.query(By.css('svg'));
        expect(svg).toBeTruthy();
      });

      it(`should have correct test id for ${brand}`, () => {
        fixture.componentRef.setInput('brand', brand);
        fixture.detectChanges();

        const iconSpan = fixture.debugElement.query(By.css('.payment-icon'));
        expect(iconSpan.attributes['data-testid']).toBe(`payment-icon-${brand}`);
      });
    });
  });

  describe('Size Mapping', () => {
    const sizeTests: { size: PaymentIconSize; expectedPx: number }[] = [
      { size: 'sm', expectedPx: 20 },
      { size: 'md', expectedPx: 28 },
      { size: 'lg', expectedPx: 36 }
    ];

    sizeTests.forEach(({ size, expectedPx }) => {
      it(`should map size '${size}' to ${expectedPx}px`, () => {
        fixture.componentRef.setInput('brand', 'visa');
        fixture.componentRef.setInput('size', size);
        fixture.detectChanges();

        expect(component.computedSize()).toBe(expectedPx);
        expect(PAYMENT_ICON_SIZE_MAP[size]).toBe(expectedPx);
      });
    });

    it('should default to md size (28px)', () => {
      fixture.componentRef.setInput('brand', 'mastercard');
      fixture.detectChanges();

      expect(component.size()).toBe('md');
      expect(component.computedSize()).toBe(28);
    });
  });

  describe('Grayscale', () => {
    it('should not have grayscale class by default', () => {
      fixture.componentRef.setInput('brand', 'visa');
      fixture.detectChanges();

      const iconSpan = fixture.debugElement.query(By.css('.payment-icon'));
      expect(iconSpan.classes['grayscale']).toBeFalsy();
    });

    it('should have grayscale class when grayscale is true', () => {
      fixture.componentRef.setInput('brand', 'visa');
      fixture.componentRef.setInput('grayscale', true);
      fixture.detectChanges();

      const iconSpan = fixture.debugElement.query(By.css('.payment-icon'));
      expect(iconSpan.classes['grayscale']).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    it('should have role="img"', () => {
      fixture.componentRef.setInput('brand', 'visa');
      fixture.detectChanges();

      const iconSpan = fixture.debugElement.query(By.css('.payment-icon'));
      expect(iconSpan.attributes['role']).toBe('img');
    });

    it('should have default aria-label for each brand', () => {
      const brandLabels: Record<PaymentBrand, string> = {
        'visa': 'Visa',
        'mastercard': 'Mastercard',
        'amex': 'American Express',
        'paypal': 'PayPal',
        'apple-pay': 'Apple Pay',
        'google-pay': 'Google Pay',
        'bank': 'Bank Transfer',
        'card-generic': 'Credit Card'
      };

      Object.entries(brandLabels).forEach(([brand, expectedLabel]) => {
        fixture.componentRef.setInput('brand', brand as PaymentBrand);
        fixture.detectChanges();

        expect(component.ariaLabel()).toBe(expectedLabel);
      });
    });

    it('should use custom label when provided', () => {
      fixture.componentRef.setInput('brand', 'visa');
      fixture.componentRef.setInput('label', 'Custom Visa Label');
      fixture.detectChanges();

      expect(component.ariaLabel()).toBe('Custom Visa Label');
    });

    it('should have aria-hidden on SVG', () => {
      fixture.componentRef.setInput('brand', 'mastercard');
      fixture.detectChanges();

      const svg = fixture.debugElement.query(By.css('svg'));
      expect(svg.attributes['aria-hidden']).toBe('true');
    });
  });
});

describe('PaymentIconComponent with TestHost', () => {
  let hostFixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent]
    }).compileComponents();

    hostFixture = TestBed.createComponent(TestHostComponent);
    hostComponent = hostFixture.componentInstance;
    hostFixture.detectChanges();
  });

  it('should create with host component', () => {
    expect(hostComponent).toBeTruthy();
  });

  it('should update when host inputs change', () => {
    hostComponent.size = 'lg';
    hostFixture.detectChanges();

    const iconComponent = hostFixture.debugElement.query(By.directive(PaymentIconComponent));
    expect(iconComponent.componentInstance.size()).toBe('lg');
    expect(iconComponent.componentInstance.computedSize()).toBe(36);
  });

  it('should update brand', () => {
    hostComponent.brand = 'paypal';
    hostFixture.detectChanges();

    const iconComponent = hostFixture.debugElement.query(By.directive(PaymentIconComponent));
    expect(iconComponent.componentInstance.brand()).toBe('paypal');
  });

  it('should toggle grayscale', () => {
    hostComponent.grayscale = true;
    hostFixture.detectChanges();

    const iconSpan = hostFixture.debugElement.query(By.css('.payment-icon'));
    expect(iconSpan.classes['grayscale']).toBeTruthy();
  });
});

describe('PAYMENT_ICON_SIZE_MAP', () => {
  it('should have correct size mappings', () => {
    expect(PAYMENT_ICON_SIZE_MAP.sm).toBe(20);
    expect(PAYMENT_ICON_SIZE_MAP.md).toBe(28);
    expect(PAYMENT_ICON_SIZE_MAP.lg).toBe(36);
  });

  it('should have entries for all PaymentIconSize values', () => {
    const sizes: PaymentIconSize[] = ['sm', 'md', 'lg'];
    sizes.forEach(size => {
      expect(PAYMENT_ICON_SIZE_MAP[size]).toBeDefined();
      expect(typeof PAYMENT_ICON_SIZE_MAP[size]).toBe('number');
    });
  });
});
