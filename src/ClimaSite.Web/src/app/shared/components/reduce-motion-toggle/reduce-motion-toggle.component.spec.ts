import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ReduceMotionToggleComponent } from './reduce-motion-toggle.component';
import { AnimationService, MOTION_PREFERENCE_STORAGE_KEY } from '../../../core/services/animation.service';

describe('ReduceMotionToggleComponent', () => {
  let fixture: ComponentFixture<ReduceMotionToggleComponent>;
  let service: AnimationService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('reduce-motion');

    TestBed.configureTestingModule({
      imports: [ReduceMotionToggleComponent, TranslateModule.forRoot()],
      providers: [
        AnimationService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    fixture = TestBed.createComponent(ReduceMotionToggleComponent);
    service = TestBed.inject(AnimationService);
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('reduce-motion');
  });

  function queryOption(value: string): HTMLInputElement {
    return fixture.nativeElement.querySelector(
      `[data-testid="reduce-motion-option-${value}"]`
    ) as HTMLInputElement;
  }

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the toggle with all three options', () => {
    expect(fixture.nativeElement.querySelector('[data-testid="reduce-motion-toggle"]')).toBeTruthy();
    expect(queryOption('system')).toBeTruthy();
    expect(queryOption('on')).toBeTruthy();
    expect(queryOption('off')).toBeTruthy();
  });

  it('should mark the system option as checked by default', () => {
    expect(queryOption('system').checked).toBeTrue();
    expect(queryOption('on').checked).toBeFalse();
    expect(queryOption('off').checked).toBeFalse();
  });

  it('should update the service preference when an option is selected', () => {
    queryOption('on').dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(service.motionPreference()).toBe('on');
    expect(queryOption('on').checked).toBeTrue();
  });

  it('should persist the selection to localStorage', () => {
    queryOption('off').dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(localStorage.getItem(MOTION_PREFERENCE_STORAGE_KEY)).toBe('off');
  });

  it('should expose a radiogroup role for accessibility', () => {
    const group = fixture.nativeElement.querySelector('[data-testid="reduce-motion-toggle"]');
    expect(group.getAttribute('role')).toBe('radiogroup');
  });
});
