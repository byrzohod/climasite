import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { PaymentTrustStripComponent } from './payment-trust-strip.component';
import { PaymentIconComponent, PaymentBrand, PaymentIconSize } from './payment-icon.component';

// Test host component
@Component({
  standalone: true,
  imports: [PaymentTrustStripComponent],
  template: `
    <app-payment-trust-strip
      [brands]="brands"
      [size]="size"
      [showLabel]="showLabel"
      [showSecureText]="showSecureText"
      [grayscale]="grayscale"
      [layout]="layout"
    />
  `
})
class TestHostComponent {
  brands: PaymentBrand[] = ['visa', 'mastercard', 'amex', 'paypal'];
  size: PaymentIconSize = 'md';
  showLabel = false;
  showSecureText = false;
  grayscale = false;
  layout: 'horizontal' | 'vertical' = 'horizontal';
}

describe('PaymentTrustStripComponent', () => {
  let component: PaymentTrustStripComponent;
  let fixture: ComponentFixture<PaymentTrustStripComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PaymentTrustStripComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentTrustStripComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should render default payment icons', () => {
      fixture.detectChanges();

      const paymentIcons = fixture.debugElement.queryAll(By.directive(PaymentIconComponent));
      expect(paymentIcons.length).toBe(4); // visa, mastercard, amex, paypal
    });

    it('should have correct test id', () => {
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.attributes['data-testid']).toBe('payment-trust-strip');
    });
  });

  describe('Custom Brands', () => {
    it('should render only specified brands', () => {
      fixture.componentRef.setInput('brands', ['visa', 'mastercard']);
      fixture.detectChanges();

      const paymentIcons = fixture.debugElement.queryAll(By.directive(PaymentIconComponent));
      expect(paymentIcons.length).toBe(2);
    });

    it('should render all available brands when specified', () => {
      const allBrands: PaymentBrand[] = [
        'visa',
        'mastercard',
        'amex',
        'paypal',
        'apple-pay',
        'google-pay',
        'bank'
      ];
      fixture.componentRef.setInput('brands', allBrands);
      fixture.detectChanges();

      const paymentIcons = fixture.debugElement.queryAll(By.directive(PaymentIconComponent));
      expect(paymentIcons.length).toBe(7);
    });
  });

  describe('Label', () => {
    it('should not show label by default', () => {
      fixture.detectChanges();

      const label = fixture.debugElement.query(By.css('.trust-label'));
      expect(label).toBeFalsy();
    });

    it('should show label when showLabel is true', () => {
      fixture.componentRef.setInput('showLabel', true);
      fixture.detectChanges();

      const label = fixture.debugElement.query(By.css('.trust-label'));
      expect(label).toBeTruthy();
    });
  });

  describe('Secure Text', () => {
    it('should not show secure text by default', () => {
      fixture.detectChanges();

      const secureText = fixture.debugElement.query(By.css('.secure-text'));
      expect(secureText).toBeFalsy();
    });

    it('should show secure text when showSecureText is true', () => {
      fixture.componentRef.setInput('showSecureText', true);
      fixture.detectChanges();

      const secureText = fixture.debugElement.query(By.css('.secure-text'));
      expect(secureText).toBeTruthy();
    });

    it('should display lock icon in secure text', () => {
      fixture.componentRef.setInput('showSecureText', true);
      fixture.detectChanges();

      const lockIcon = fixture.debugElement.query(By.css('.secure-text .lock-icon'));
      expect(lockIcon).toBeTruthy();
    });
  });

  describe('Size', () => {
    it('should pass size to child icons', () => {
      fixture.componentRef.setInput('size', 'lg');
      fixture.detectChanges();

      const paymentIcons = fixture.debugElement.queryAll(By.directive(PaymentIconComponent));
      paymentIcons.forEach(icon => {
        expect(icon.componentInstance.size()).toBe('lg');
      });
    });

    it('should add compact class for small size', () => {
      fixture.componentRef.setInput('size', 'sm');
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.classes['compact']).toBeTruthy();
    });
  });

  describe('Grayscale', () => {
    it('should pass grayscale to child icons', () => {
      fixture.componentRef.setInput('grayscale', true);
      fixture.detectChanges();

      const paymentIcons = fixture.debugElement.queryAll(By.directive(PaymentIconComponent));
      paymentIcons.forEach(icon => {
        expect(icon.componentInstance.grayscale()).toBe(true);
      });
    });
  });

  describe('Layout', () => {
    it('should default to horizontal layout', () => {
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.classes['vertical']).toBeFalsy();
    });

    it('should apply vertical class when layout is vertical', () => {
      fixture.componentRef.setInput('layout', 'vertical');
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.classes['vertical']).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    it('should have role="group"', () => {
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.attributes['role']).toBe('group');
    });

    it('should have aria-label', () => {
      fixture.detectChanges();

      const strip = fixture.debugElement.query(By.css('.payment-trust-strip'));
      expect(strip.attributes['aria-label']).toBeTruthy();
    });
  });
});

describe('PaymentTrustStripComponent with TestHost', () => {
  let hostFixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    hostFixture = TestBed.createComponent(TestHostComponent);
    hostComponent = hostFixture.componentInstance;
    hostFixture.detectChanges();
  });

  it('should create with host component', () => {
    expect(hostComponent).toBeTruthy();
  });

  it('should update when brands change', () => {
    hostComponent.brands = ['visa', 'paypal'];
    hostFixture.detectChanges();

    const paymentIcons = hostFixture.debugElement.queryAll(By.directive(PaymentIconComponent));
    expect(paymentIcons.length).toBe(2);
  });

  it('should toggle label visibility', () => {
    expect(hostFixture.debugElement.query(By.css('.trust-label'))).toBeFalsy();

    hostComponent.showLabel = true;
    hostFixture.detectChanges();

    expect(hostFixture.debugElement.query(By.css('.trust-label'))).toBeTruthy();
  });

  it('should toggle secure text visibility', () => {
    expect(hostFixture.debugElement.query(By.css('.secure-text'))).toBeFalsy();

    hostComponent.showSecureText = true;
    hostFixture.detectChanges();

    expect(hostFixture.debugElement.query(By.css('.secure-text'))).toBeTruthy();
  });

  it('should change layout', () => {
    const strip = hostFixture.debugElement.query(By.css('.payment-trust-strip'));
    expect(strip.classes['vertical']).toBeFalsy();

    hostComponent.layout = 'vertical';
    hostFixture.detectChanges();

    expect(strip.classes['vertical']).toBeTruthy();
  });
});
