import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, input } from '@angular/core';
import { FooterComponent } from './footer.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { TrustBadgeStripComponent } from '../../../shared/components/trust-badge';
import { PaymentTrustStripComponent } from '../../../shared/components/payment-icons';

// Mock TrustBadgeStripComponent to avoid Lucide icon registration issues
@Component({
  selector: 'app-trust-badge-strip',
  template: '<div data-testid="mock-trust-badge-strip"></div>',
  standalone: true
})
class MockTrustBadgeStripComponent {
  readonly badges = input<unknown[]>();
  readonly compact = input<boolean>(false);
  readonly centered = input<boolean>(false);
}

// Mock PaymentTrustStripComponent to avoid icon registration issues
@Component({
  selector: 'app-payment-trust-strip',
  template: '<div data-testid="mock-payment-trust-strip"></div>',
  standalone: true
})
class MockPaymentTrustStripComponent {
  readonly brands = input<string[]>();
  readonly size = input<string>('md');
  readonly grayscale = input<boolean>(false);
}

describe('FooterComponent', () => {
  let component: FooterComponent;
  let fixture: ComponentFixture<FooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FooterComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    })
    .overrideComponent(FooterComponent, {
      remove: { imports: [TrustBadgeStripComponent, PaymentTrustStripComponent] },
      add: { imports: [MockTrustBadgeStripComponent, MockPaymentTrustStripComponent] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(FooterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have footer element', () => {
    const footer = fixture.nativeElement.querySelector('[data-testid="footer"]');
    expect(footer).toBeTruthy();
  });

  it('should have newsletter form', () => {
    const input = fixture.nativeElement.querySelector('[data-testid="newsletter-input"]');
    const submit = fixture.nativeElement.querySelector('[data-testid="newsletter-submit"]');
    expect(input).toBeTruthy();
    expect(submit).toBeTruthy();
  });

  it('should have current year in copyright', () => {
    const currentYear = new Date().getFullYear();
    expect(component.currentYear).toBe(currentYear);
  });

  it('should have social links', () => {
    const socialLinks = fixture.nativeElement.querySelectorAll('.social-link');
    expect(socialLinks.length).toBeGreaterThan(0);
  });
});
