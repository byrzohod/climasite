import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ThemeToggleComponent } from './theme-toggle.component';
import { ThemeService } from '../../../core/services/theme.service';
import { PLATFORM_ID } from '@angular/core';

describe('ThemeToggleComponent', () => {
  let component: ThemeToggleComponent;
  let fixture: ComponentFixture<ThemeToggleComponent>;
  let themeService: ThemeService;

  beforeEach(async () => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.classList.remove('dark');

    await TestBed.configureTestingModule({
      imports: [ThemeToggleComponent],
      providers: [
        ThemeService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ThemeToggleComponent);
    component = fixture.componentInstance;
    themeService = TestBed.inject(ThemeService);
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.classList.remove('dark');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have data-testid attribute', () => {
    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('data-testid')).toBe('theme-toggle');
  });

  it('should toggle theme when clicked in toggle mode', fakeAsync(() => {
    themeService.setThemeMode('light');
    tick();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    button.click();
    tick();
    fixture.detectChanges();

    expect(themeService.effectiveTheme()).toBe('dark');
  }));

  it('should show sun icon when in light mode', fakeAsync(() => {
    themeService.setThemeMode('light');
    tick();
    fixture.detectChanges();

    const sunIcon = fixture.nativeElement.querySelector('.theme-toggle__icon--sun');
    expect(sunIcon.classList.contains('theme-toggle__icon--active')).toBeTrue();
  }));

  it('should show moon icon when in dark mode', fakeAsync(() => {
    themeService.setThemeMode('dark');
    tick();
    fixture.detectChanges();

    const moonIcon = fixture.nativeElement.querySelector('.theme-toggle__icon--moon');
    expect(moonIcon.classList.contains('theme-toggle__icon--active')).toBeTrue();
  }));

  it('should have accessible aria-label', () => {
    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toContain('Switch to');
  });

  it('should be keyboard accessible', () => {
    const button = fixture.nativeElement.querySelector('button');
    expect(button.tagName).toBe('BUTTON');
    expect(button.getAttribute('type')).toBe('button');
  });
});
