import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { AnimationService, MOTION_PREFERENCE_STORAGE_KEY } from './animation.service';

describe('AnimationService (reduced-motion preference)', () => {
  const REDUCE_MOTION_CLASS = 'reduce-motion';

  function createService(): AnimationService {
    TestBed.configureTestingModule({
      providers: [
        AnimationService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    return TestBed.inject(AnimationService);
  }

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove(REDUCE_MOTION_CLASS);
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove(REDUCE_MOTION_CLASS);
  });

  it('should be created', () => {
    expect(createService()).toBeTruthy();
  });

  it('should default to the system motion preference when nothing is stored', () => {
    const service = createService();
    expect(service.motionPreference()).toBe('system');
  });

  it('should load the stored motion preference on construction', () => {
    localStorage.setItem(MOTION_PREFERENCE_STORAGE_KEY, 'on');
    const service = createService();
    expect(service.motionPreference()).toBe('on');
  });

  it('should ignore an invalid stored value and fall back to system', () => {
    localStorage.setItem(MOTION_PREFERENCE_STORAGE_KEY, 'bogus');
    const service = createService();
    expect(service.motionPreference()).toBe('system');
  });

  it('should persist the motion preference to localStorage', () => {
    const service = createService();
    service.setMotionPreference('off');
    expect(localStorage.getItem(MOTION_PREFERENCE_STORAGE_KEY)).toBe('off');
  });

  it('should force reduced motion when the override is "on"', () => {
    const service = createService();
    service.setMotionPreference('on');
    expect(service.prefersReducedMotion()).toBeTrue();
  });

  it('should opt out of reduced motion when the override is "off"', () => {
    const service = createService();
    service.setMotionPreference('off');
    expect(service.prefersReducedMotion()).toBeFalse();
  });

  it('should reflect the OS preference when the override is "system"', () => {
    const service = createService();
    service.setMotionPreference('system');
    expect(service.prefersReducedMotion()).toBe(service.systemPrefersReducedMotion());
  });

  it('should add the reduce-motion class on <html> when the override is "on"', () => {
    const service = createService();
    service.setMotionPreference('on');
    TestBed.flushEffects();

    expect(document.documentElement.classList.contains(REDUCE_MOTION_CLASS)).toBeTrue();
  });

  it('should remove the reduce-motion class on <html> when switching to "off"', () => {
    const service = createService();

    service.setMotionPreference('on');
    TestBed.flushEffects();
    expect(document.documentElement.classList.contains(REDUCE_MOTION_CLASS)).toBeTrue();

    service.setMotionPreference('off');
    TestBed.flushEffects();
    expect(document.documentElement.classList.contains(REDUCE_MOTION_CLASS)).toBeFalse();
  });

  it('should expose prefersReducedMotion as a reactive signal that follows the override', () => {
    const service = createService();

    service.setMotionPreference('off');
    expect(service.prefersReducedMotion()).toBeFalse();

    service.setMotionPreference('on');
    expect(service.prefersReducedMotion()).toBeTrue();
  });

  it('should zero stagger delay and animation duration when reduced motion is forced on', () => {
    const service = createService();
    service.setMotionPreference('on');

    expect(service.getStaggerDelay(5)).toBe(0);
    expect(service.getDuration(400)).toBe(0);
  });
});
