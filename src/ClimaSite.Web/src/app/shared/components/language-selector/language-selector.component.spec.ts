import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LanguageSelectorComponent } from './language-selector.component';
import { TranslateModule } from '@ngx-translate/core';
import { PLATFORM_ID } from '@angular/core';
import { LanguageService } from '../../../core/services/language.service';

describe('LanguageSelectorComponent', () => {
  let component: LanguageSelectorComponent;
  let fixture: ComponentFixture<LanguageSelectorComponent>;
  let languageService: LanguageService;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LanguageSelectorComponent, TranslateModule.forRoot()],
      providers: [
        LanguageService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LanguageSelectorComponent);
    component = fixture.componentInstance;
    languageService = TestBed.inject(LanguageService);
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have data-testid attribute', () => {
    const selector = fixture.nativeElement.querySelector('[data-testid="language-selector"]');
    expect(selector).toBeTruthy();
  });

  it('should toggle dropdown when clicked', () => {
    const toggle = fixture.nativeElement.querySelector('[data-testid="language-toggle"]');

    // Initially closed
    let dropdown = fixture.nativeElement.querySelector('[data-testid="language-dropdown"]');
    expect(dropdown).toBeFalsy();

    // Open dropdown
    toggle.click();
    fixture.detectChanges();
    dropdown = fixture.nativeElement.querySelector('[data-testid="language-dropdown"]');
    expect(dropdown).toBeTruthy();

    // Close dropdown
    toggle.click();
    fixture.detectChanges();
    dropdown = fixture.nativeElement.querySelector('[data-testid="language-dropdown"]');
    expect(dropdown).toBeFalsy();
  });

  it('should show all language options when open', () => {
    const toggle = fixture.nativeElement.querySelector('[data-testid="language-toggle"]');
    toggle.click();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll('[role="option"]');
    expect(options.length).toBe(3);
  });

  it('should change language when option is selected', () => {
    const toggle = fixture.nativeElement.querySelector('[data-testid="language-toggle"]');
    toggle.click();
    fixture.detectChanges();

    const germanOption = fixture.nativeElement.querySelector('[data-testid="language-de"]');
    germanOption.click();
    fixture.detectChanges();

    expect(languageService.currentLanguage()).toBe('de');
  });

  it('should close dropdown after selecting language', () => {
    const toggle = fixture.nativeElement.querySelector('[data-testid="language-toggle"]');
    toggle.click();
    fixture.detectChanges();

    const germanOption = fixture.nativeElement.querySelector('[data-testid="language-de"]');
    germanOption.click();
    fixture.detectChanges();

    const dropdown = fixture.nativeElement.querySelector('[data-testid="language-dropdown"]');
    expect(dropdown).toBeFalsy();
  });

  it('should display current language code', () => {
    const codeSpan = fixture.nativeElement.querySelector('.language-code');
    expect(codeSpan.textContent.trim()).toBe('EN');
  });
});
