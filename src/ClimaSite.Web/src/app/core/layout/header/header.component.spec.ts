import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HeaderComponent } from './header.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { PLATFORM_ID } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [
        HeaderComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' },
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have header element', () => {
    const header = fixture.nativeElement.querySelector('[data-testid="header"]');
    expect(header).toBeTruthy();
  });

  it('should have logo', () => {
    const logo = fixture.nativeElement.querySelector('[data-testid="header-logo"]');
    expect(logo).toBeTruthy();
  });

  it('should have navigation', () => {
    const nav = fixture.nativeElement.querySelector('[data-testid="header-nav"]');
    expect(nav).toBeTruthy();
  });

  it('should have cart button', () => {
    const cart = fixture.nativeElement.querySelector('[data-testid="cart-icon"]');
    expect(cart).toBeTruthy();
  });

  it('should have theme toggle', () => {
    const themeToggle = fixture.nativeElement.querySelector('[data-testid="theme-toggle"]');
    expect(themeToggle).toBeTruthy();
  });

  it('should have language selector', () => {
    const langSelector = fixture.nativeElement.querySelector('[data-testid="language-selector"]');
    expect(langSelector).toBeTruthy();
  });

  it('should have mobile menu toggle', () => {
    const mobileToggle = fixture.nativeElement.querySelector('[data-testid="mobile-menu-toggle"]');
    expect(mobileToggle).toBeTruthy();
  });
});
