import { Component, PLATFORM_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AnimateOnScrollDirective, AnimationType } from './animate-on-scroll.directive';

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
  imports: [AnimateOnScrollDirective],
  template: `
    <div
      appAnimateOnScroll
      [animation]="animation"
      [delay]="delay"
      [threshold]="threshold"
      [once]="once"
    >Content</div>
  `
})
class HostComponent {
  animation: AnimationType = 'fade-in-up';
  delay = 0;
  threshold = 0.1;
  once = true;
}

describe('AnimateOnScrollDirective', () => {
  let originalIO: typeof IntersectionObserver;

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

  function configure(platform: 'browser' | 'server' = 'browser'): void {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: PLATFORM_ID, useValue: platform }]
    });
  }

  function createHost(): ComponentFixture<HostComponent> {
    return TestBed.createComponent(HostComponent);
  }

  function el(fixture: ComponentFixture<HostComponent>): HTMLElement {
    return fixture.nativeElement.querySelector('div');
  }

  // -------------------------------------------------------------------------
  // SSR — directive is a no-op (no observer, no inline styles).
  // -------------------------------------------------------------------------
  it('does nothing on the server', () => {
    configure('server');
    const fixture = createHost();
    fixture.detectChanges();

    expect(MockIntersectionObserver.instances.length).toBe(0);
    expect(el(fixture).style.opacity).toBe('');
    expect(el(fixture).style.transition).toBe('');
  });

  // -------------------------------------------------------------------------
  // Initial hidden state per animation type.
  // -------------------------------------------------------------------------
  describe('initial hidden state', () => {
    const cases: Array<{ animation: AnimationType; transform: string }> = [
      { animation: 'fade-in', transform: 'none' },
      { animation: 'fade-in-up', transform: 'translateY(30px)' },
      { animation: 'fade-in-left', transform: 'translateX(-30px)' },
      { animation: 'fade-in-right', transform: 'translateX(30px)' },
      { animation: 'scale-in', transform: 'scale(0.9)' },
      { animation: 'slide-up', transform: 'translateY(50px)' }
    ];

    cases.forEach(({ animation, transform }) => {
      it(`sets opacity 0 and transform "${transform}" for ${animation}`, () => {
        configure('browser');
        const fixture = createHost();
        fixture.componentInstance.animation = animation;
        fixture.detectChanges();

        expect(el(fixture).style.opacity).toBe('0');
        expect(el(fixture).style.transform).toBe(transform);
      });
    });

    it('sets an opacity/transform transition with ease-out', () => {
      configure('browser');
      const fixture = createHost();
      fixture.detectChanges();

      const transition = el(fixture).style.transition;
      expect(transition).toContain('opacity');
      expect(transition).toContain('transform');
      expect(transition).toContain('ease-out');
    });
  });

  // -------------------------------------------------------------------------
  // Observer wiring + animate / reset.
  // -------------------------------------------------------------------------
  describe('observer behaviour', () => {
    it('creates an observer with the configured threshold and a bottom rootMargin', () => {
      configure('browser');
      const fixture = createHost();
      fixture.componentInstance.threshold = 0.1;
      fixture.detectChanges();

      expect(MockIntersectionObserver.instances.length).toBe(1);
      expect(MockIntersectionObserver.last.options?.threshold).toBe(0.1);
      expect(MockIntersectionObserver.last.options?.rootMargin).toBe('0px 0px -50px 0px');
      expect(MockIntersectionObserver.last.observe).toHaveBeenCalledWith(el(fixture));
    });

    it('animates to visible on intersection and unobserves when once is true', () => {
      configure('browser');
      const fixture = createHost();
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);

      expect(el(fixture).style.opacity).toBe('1');
      expect(el(fixture).style.transform).toBe('none');
      expect(MockIntersectionObserver.last.unobserve).toHaveBeenCalledWith(el(fixture));
    });

    it('does not unobserve when once is false, and resets when leaving view', () => {
      configure('browser');
      const fixture = createHost();
      fixture.componentInstance.once = false;
      fixture.componentInstance.animation = 'fade-in-up';
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);
      expect(el(fixture).style.opacity).toBe('1');
      expect(MockIntersectionObserver.last.unobserve).not.toHaveBeenCalled();

      MockIntersectionObserver.last.trigger(false);
      expect(el(fixture).style.opacity).toBe('0');
      expect(el(fixture).style.transform).toBe('translateY(30px)');
    });

    it('disconnects the observer on destroy', () => {
      configure('browser');
      const fixture = createHost();
      fixture.detectChanges();
      const observer = MockIntersectionObserver.last;

      fixture.destroy();

      expect(observer.disconnect).toHaveBeenCalled();
    });
  });
});
