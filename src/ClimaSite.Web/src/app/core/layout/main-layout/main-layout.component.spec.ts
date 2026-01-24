import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, input } from '@angular/core';
import { MainLayoutComponent } from './main-layout.component';
import { FooterComponent } from '../footer/footer.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { PLATFORM_ID } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TrustBadgeStripComponent } from '../../../shared/components/trust-badge';
import { PaymentTrustStripComponent } from '../../../shared/components/payment-icons';
import { BottomNavComponent } from '../../../shared/components/bottom-nav';

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

// Mock BottomNavComponent to avoid Lucide icon registration issues
@Component({
  selector: 'app-bottom-nav',
  template: '<nav data-testid="mock-bottom-nav"></nav>',
  standalone: true
})
class MockBottomNavComponent {}

describe('MainLayoutComponent', () => {
  let component: MainLayoutComponent;
  let fixture: ComponentFixture<MainLayoutComponent>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [
        MainLayoutComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations()
      ]
    })
    // Override FooterComponent's imports to use mocks (FooterComponent is imported by MainLayoutComponent)
    .overrideComponent(FooterComponent, {
      remove: { imports: [TrustBadgeStripComponent, PaymentTrustStripComponent] },
      add: { imports: [MockTrustBadgeStripComponent, MockPaymentTrustStripComponent] }
    })
    // Override MainLayoutComponent to use mock BottomNavComponent
    .overrideComponent(MainLayoutComponent, {
      remove: { imports: [BottomNavComponent] },
      add: { imports: [MockBottomNavComponent] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have main layout element', () => {
    const layout = fixture.nativeElement.querySelector('[data-testid="main-layout"]');
    expect(layout).toBeTruthy();
  });

  it('should have header', () => {
    const header = fixture.nativeElement.querySelector('[data-testid="header"]');
    expect(header).toBeTruthy();
  });

  it('should have main content', () => {
    const main = fixture.nativeElement.querySelector('[data-testid="main-content"]');
    expect(main).toBeTruthy();
  });

  it('should have footer', () => {
    const footer = fixture.nativeElement.querySelector('[data-testid="footer"]');
    expect(footer).toBeTruthy();
  });
});
