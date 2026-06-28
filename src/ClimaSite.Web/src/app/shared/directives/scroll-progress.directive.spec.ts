import { Component, PLATFORM_ID, WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ScrollProgressDirective, ProgressStyle } from './scroll-progress.directive';
import { AnimationService } from '../../core/services/animation.service';

@Component({
  standalone: true,
  imports: [ScrollProgressDirective],
  template: `
    <div
      [appScrollProgress]="progressStyle"
      [min]="min"
      [max]="max"
      [invert]="invert"
      [gradientColors]="gradientColors"
    ></div>
  `
})
class HostComponent {
  progressStyle: ProgressStyle = 'width';
  min = 0;
  max = 1;
  invert = false;
  gradientColors = ['var(--color-primary)', 'var(--color-accent)'];
}

describe('ScrollProgressDirective', () => {
  let scrollProgress: WritableSignal<number>;

  function configure(platform: 'browser' | 'server' = 'browser'): void {
    scrollProgress = signal(0);
    const animMock = jasmine.createSpyObj('AnimationService', [], {
      scrollProgress,
      prefersReducedMotion: signal(false)
    });
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        { provide: AnimationService, useValue: animMock },
        { provide: PLATFORM_ID, useValue: platform }
      ]
    });
  }

  function createHost(): ComponentFixture<HostComponent> {
    return TestBed.createComponent(HostComponent);
  }

  function el(fixture: ComponentFixture<HostComponent>): HTMLElement {
    return fixture.nativeElement.querySelector('div');
  }

  // -------------------------------------------------------------------------
  // SSR — the directive returns early in ngOnInit before touching the DOM or
  // creating the scroll effect, so it is a pure no-op on the server.
  // -------------------------------------------------------------------------
  describe('SSR (non-browser)', () => {
    it('is a no-op: no will-change / inline sizing styles applied', () => {
      configure('server');
      const fixture = createHost();
      fixture.detectChanges();

      expect(el(fixture).style.willChange).toBe('');
      expect(el(fixture).style.width).toBe('');
      expect(el(fixture).style.transform).toBe('');
    });

    it('instantiates the directive on the host element', () => {
      configure('server');
      const fixture = createHost();
      fixture.detectChanges();

      const directive = fixture.debugElement
        .query(By.directive(ScrollProgressDirective))
        .injector.get(ScrollProgressDirective);
      expect(directive).toBeInstanceOf(ScrollProgressDirective);
    });

    it('does not read scrollProgress on the server (no effect wired up)', () => {
      configure('server');
      const fixture = createHost();
      fixture.detectChanges();

      // Pushing a new scroll value must not change the element on the server.
      scrollProgress.set(0.75);
      fixture.detectChanges();
      expect(el(fixture).style.width).toBe('');
    });
  });

  // -------------------------------------------------------------------------
  // KNOWN LATENT BUG (documented, not fixed here — tests-only task):
  //
  // In the browser, ngOnInit calls `effect()` (setupScrollEffect). `effect()`
  // must run inside an injection context (constructor / field initializer /
  // runInInjectionContext), but ngOnInit is NOT one, so Angular throws
  // NG0203. The directive has no current usages, so the bug is dormant in
  // production. These tests pin the *actual* current behaviour; if the
  // directive is fixed (e.g. by moving `effect()` into the constructor or
  // passing an explicit injector), update them to assert the real
  // scroll-driven width/scale/opacity/background mapping instead.
  // -------------------------------------------------------------------------
  describe('browser initialization (current behaviour)', () => {
    it('throws an injection-context error because effect() runs in ngOnInit', () => {
      configure('browser');
      const fixture = createHost();

      expect(() => fixture.detectChanges()).toThrowError(/injection context/i);
    });
  });
});
