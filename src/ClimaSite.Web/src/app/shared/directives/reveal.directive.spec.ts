import { Component, PLATFORM_ID, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';

import { RevealDirective, RevealAnimation } from './reveal.directive';
import { AnimationService } from '../../core/services/animation.service';

class MockIntersectionObserver {
  static instances: MockIntersectionObserver[] = [];
  observe = jasmine.createSpy('observe');
  unobserve = jasmine.createSpy('unobserve');
  disconnect = jasmine.createSpy('disconnect');
  callback: IntersectionObserverCallback;
  options?: IntersectionObserverInit;

  constructor(cb: IntersectionObserverCallback, options?: IntersectionObserverInit) {
    this.callback = cb;
    this.options = options;
    MockIntersectionObserver.instances.push(this);
  }

  trigger(isIntersecting: boolean): void {
    this.callback(
      [{ isIntersecting } as IntersectionObserverEntry],
      this as unknown as IntersectionObserver
    );
  }

  static get last(): MockIntersectionObserver {
    return MockIntersectionObserver.instances[MockIntersectionObserver.instances.length - 1];
  }
}

@Component({
  standalone: true,
  imports: [RevealDirective],
  template: `
    <div
      [appReveal]="animation"
      [delay]="delay"
      [duration]="duration"
      [threshold]="threshold"
      [distance]="distance"
      [once]="once"
      (revealed)="onRevealed()"
      (hidden)="onHidden()"
    >Content</div>
  `
})
class HostComponent {
  animation: RevealAnimation = 'fade-up';
  delay = 0;
  duration = 300;
  threshold = 0.15;
  distance = 16;
  once = true;

  revealedCount = 0;
  hiddenCount = 0;
  onRevealed(): void { this.revealedCount++; }
  onHidden(): void { this.hiddenCount++; }
}

describe('RevealDirective', () => {
  let originalIO: typeof IntersectionObserver;
  let reducedMotion: ReturnType<typeof signal<boolean>>;

  beforeEach(() => {
    originalIO = window.IntersectionObserver;
    MockIntersectionObserver.instances = [];
    (window as unknown as { IntersectionObserver: unknown }).IntersectionObserver =
      MockIntersectionObserver;
  });

  afterEach(() => {
    (window as unknown as { IntersectionObserver: typeof IntersectionObserver }).IntersectionObserver =
      originalIO;
  });

  function configure(opts: { platform?: 'browser' | 'server'; reducedMotion?: boolean } = {}): void {
    reducedMotion = signal(opts.reducedMotion ?? false);
    const animMock = jasmine.createSpyObj('AnimationService', [], {
      prefersReducedMotion: reducedMotion
    });
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        { provide: AnimationService, useValue: animMock },
        { provide: PLATFORM_ID, useValue: opts.platform ?? 'browser' }
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
  // SSR — no-op.
  // -------------------------------------------------------------------------
  it('does nothing on the server', () => {
    configure({ platform: 'server' });
    const fixture = createHost();
    fixture.detectChanges();

    expect(MockIntersectionObserver.instances.length).toBe(0);
    expect(el(fixture).style.opacity).toBe('');
  });

  // -------------------------------------------------------------------------
  // Initial hidden state per animation type.
  // -------------------------------------------------------------------------
  describe('initial hidden state', () => {
    it('fade: only opacity is hidden, no transform', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.animation = 'fade';
      fixture.detectChanges();

      expect(el(fixture).style.opacity).toBe('0');
      expect(el(fixture).style.transform).toBe('');
    });

    it('fade-up: translates down by the distance', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.animation = 'fade-up';
      fixture.componentInstance.distance = 16;
      fixture.detectChanges();

      expect(el(fixture).style.opacity).toBe('0');
      expect(el(fixture).style.transform).toBe('translateY(16px)');
    });

    it('fade-down: translates up by the distance', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.animation = 'fade-down';
      fixture.componentInstance.distance = 40;
      fixture.detectChanges();

      expect(el(fixture).style.transform).toBe('translateY(-40px)');
    });

    it('sets a transition and will-change on opacity/transform', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.detectChanges();

      const transition = el(fixture).style.transition;
      expect(transition).toContain('opacity');
      expect(transition).toContain('transform');
      expect(transition).toContain('cubic-bezier(0.16, 1, 0.3, 1)');
      expect(el(fixture).style.willChange).toBe('opacity, transform');
    });
  });

  // -------------------------------------------------------------------------
  // Observer wiring + reveal / hide.
  // -------------------------------------------------------------------------
  describe('observer behaviour', () => {
    it('creates an observer with the configured threshold and rootMargin', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.threshold = 0.15;
      fixture.detectChanges();

      expect(MockIntersectionObserver.instances.length).toBe(1);
      expect(MockIntersectionObserver.last.options?.threshold).toBe(0.15);
      expect(MockIntersectionObserver.last.options?.rootMargin).toBe('0px 0px -50px 0px');
      expect(MockIntersectionObserver.last.observe).toHaveBeenCalledWith(el(fixture));
    });

    it('reveals on intersection, emits revealed, and clears will-change after the timeout', fakeAsync(() => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);

      expect(el(fixture).style.opacity).toBe('1');
      expect(el(fixture).style.transform).toBe('none');
      expect(fixture.componentInstance.revealedCount).toBe(1);
      expect(MockIntersectionObserver.last.unobserve).toHaveBeenCalledWith(el(fixture));

      tick(300 + 0 + 100); // duration + delay + buffer
      expect(el(fixture).style.willChange).toBe('auto');
    }));

    it('with once=false, hides again when leaving the viewport', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.once = false;
      fixture.componentInstance.animation = 'fade-up';
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);
      expect(el(fixture).style.opacity).toBe('1');
      expect(MockIntersectionObserver.last.unobserve).not.toHaveBeenCalled();

      MockIntersectionObserver.last.trigger(false);
      expect(el(fixture).style.opacity).toBe('0');
      expect(el(fixture).style.transform).toBe('translateY(16px)');
      expect(fixture.componentInstance.hiddenCount).toBe(1);
    });

    it('disconnects the observer on destroy', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.detectChanges();
      const observer = MockIntersectionObserver.last;

      fixture.destroy();

      expect(observer.disconnect).toHaveBeenCalled();
    });
  });

  // -------------------------------------------------------------------------
  // Reduced motion — reveal immediately, no observer, no initial hide.
  // -------------------------------------------------------------------------
  it('reveals immediately and skips the observer when reduced motion is preferred', () => {
    configure({ platform: 'browser', reducedMotion: true });
    const fixture = createHost();
    fixture.detectChanges();

    expect(el(fixture).style.opacity).toBe('1');
    expect(el(fixture).style.transform).toBe('none');
    expect(fixture.componentInstance.revealedCount).toBe(1);
    expect(MockIntersectionObserver.instances.length).toBe(0);
  });
});
