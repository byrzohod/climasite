import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CookieConsentComponent } from './cookie-consent.component';
import { ConsentService } from '../../../core/services/consent.service';

const STORAGE_KEY = 'climasite_cookie_consent';

describe('CookieConsentComponent', () => {
  let fixture: ComponentFixture<CookieConsentComponent>;
  let component: CookieConsentComponent;

  beforeEach(async () => {
    localStorage.removeItem(STORAGE_KEY);

    await TestBed.configureTestingModule({
      imports: [CookieConsentComponent, TranslateModule.forRoot()],
      providers: [provideRouter([]), ConsentService]
    }).compileComponents();

    fixture = TestBed.createComponent(CookieConsentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.removeItem(STORAGE_KEY);
  });

  function banner() {
    return fixture.debugElement.query(By.css('[data-testid="cookie-consent"]'));
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show the banner when no decision has been made', () => {
    expect(banner()).toBeTruthy();
  });

  it('should hide the banner and persist after accept', () => {
    fixture.debugElement
      .query(By.css('[data-testid="cookie-consent-accept"]'))
      .nativeElement.click();
    fixture.detectChanges();

    expect(banner()).toBeNull();
    expect(component.consent.accepted()).toBeTrue();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('accepted');
  });

  it('should hide the banner and persist after reject', () => {
    fixture.debugElement
      .query(By.css('[data-testid="cookie-consent-reject"]'))
      .nativeElement.click();
    fixture.detectChanges();

    expect(banner()).toBeNull();
    expect(component.consent.hasDecided()).toBeTrue();
    expect(component.consent.accepted()).toBeFalse();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('rejected');
  });

  it('should not render the banner when a decision already exists', async () => {
    localStorage.setItem(STORAGE_KEY, 'accepted');

    // Rebuild with the pre-existing decision so the service hydrates from storage.
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [CookieConsentComponent, TranslateModule.forRoot()],
      providers: [provideRouter([]), ConsentService]
    }).compileComponents();

    const freshFixture = TestBed.createComponent(CookieConsentComponent);
    freshFixture.detectChanges();

    expect(freshFixture.debugElement.query(By.css('[data-testid="cookie-consent"]'))).toBeNull();
  });

  describe('Z-Index Layering (overlay stacking)', () => {
    // The banner must sit on the canonical "banner" token layer (300), below the
    // overlay (400) / modal (500) layers, so an open drawer or modal is never
    // blocked. On mobile it must also clear the 56px fixed bottom-nav. Guards
    // against regressing to the previous hardcoded z-index: 1000 + bottom: 0.
    const componentCss = (): string =>
      ((CookieConsentComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles || []).join('\n');

    it('pins the banner to the --z-banner token, not a hardcoded value', () => {
      const css = componentCss();
      expect(css).toContain('z-index: var(--z-banner, 300)');
      expect(css).not.toContain('z-index: 1000');
    });

    it('offsets the banner above the 56px bottom-nav on mobile', () => {
      const css = componentCss().replace(/\s+/g, ' ');
      expect(css).toContain('@media (max-width: 767px)');
      expect(css).toContain('bottom: calc(56px + env(safe-area-inset-bottom, 0px))');
    });
  });
});
