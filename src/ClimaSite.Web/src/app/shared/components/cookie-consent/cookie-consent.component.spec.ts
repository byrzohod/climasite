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
});
