import { Component, PLATFORM_ID, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { CountUpDirective, EasingFunction } from './count-up.directive';
import { AnimationService } from '../../core/services/animation.service';

// ---------------------------------------------------------------------------
// Deterministic IntersectionObserver mock — lets us trigger intersection
// synchronously and assert observe/unobserve/disconnect wiring.
// ---------------------------------------------------------------------------
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
  imports: [CountUpDirective],
  template: `
    <span
      [appCountUp]="target"
      [startValue]="startValue"
      [duration]="duration"
      [prefix]="prefix"
      [suffix]="suffix"
      [decimals]="decimals"
      [separator]="separator"
      [separatorChar]="separatorChar"
      [decimalChar]="decimalChar"
      [easing]="easing"
      [threshold]="threshold"
      [delay]="delay"
      (countComplete)="onComplete()"
      (countUpdate)="onUpdate($event)"
    >0</span>
  `
})
class CountUpHostComponent {
  target = 5000;
  startValue = 0;
  duration = 2000;
  prefix = '';
  suffix = '';
  decimals = 0;
  separator = true;
  separatorChar = ',';
  decimalChar = '.';
  easing: EasingFunction = 'ease-out-quart';
  threshold = 0.3;
  delay = 0;

  completeCount = 0;
  updates: number[] = [];
  onComplete(): void { this.completeCount++; }
  onUpdate(value: number): void { this.updates.push(value); }
}

describe('CountUpDirective', () => {
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

  /** Configure TestBed for the given platform / reduced-motion combination. */
  function configure(opts: { platform?: 'browser' | 'server'; reducedMotion?: boolean } = {}): void {
    reducedMotion = signal(opts.reducedMotion ?? false);
    const animMock = jasmine.createSpyObj('AnimationService', [], {
      prefersReducedMotion: reducedMotion
    });
    TestBed.configureTestingModule({
      imports: [CountUpHostComponent],
      providers: [
        { provide: AnimationService, useValue: animMock },
        { provide: PLATFORM_ID, useValue: opts.platform ?? 'browser' }
      ]
    });
  }

  function createHost(): ComponentFixture<CountUpHostComponent> {
    return TestBed.createComponent(CountUpHostComponent);
  }

  function span(fixture: ComponentFixture<CountUpHostComponent>): HTMLElement {
    return fixture.nativeElement.querySelector('span');
  }

  function directiveOf(fixture: ComponentFixture<CountUpHostComponent>): CountUpDirective {
    return fixture.debugElement
      .query(By.directive(CountUpDirective))
      .injector.get(CountUpDirective);
  }

  // -------------------------------------------------------------------------
  // SSR — number formatting is exercised through the deterministic
  // "show final value immediately" path.
  // -------------------------------------------------------------------------
  describe('SSR (non-browser) formatting', () => {
    it('shows the final target value immediately and does not observe', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 5000;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('5,000');
      expect(MockIntersectionObserver.instances.length).toBe(0);
    });

    it('omits the thousands separator when separator is false', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 5000;
      fixture.componentInstance.separator = false;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('5000');
    });

    it('inserts separators for large integers', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 1234567;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('1,234,567');
    });

    it('applies prefix and suffix around the formatted value', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 5000;
      fixture.componentInstance.prefix = '$';
      fixture.componentInstance.suffix = '+';
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('$5,000+');
    });

    it('renders the requested number of decimals', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 99.9;
      fixture.componentInstance.decimals = 1;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('99.9');
    });

    it('floors to an integer when decimals is 0', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 99.9;
      fixture.componentInstance.decimals = 0;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('99');
    });

    it('honours custom separator and decimal characters', () => {
      configure({ platform: 'server' });
      const fixture = createHost();
      fixture.componentInstance.target = 1234.5;
      fixture.componentInstance.decimals = 1;
      fixture.componentInstance.separatorChar = '.';
      fixture.componentInstance.decimalChar = ',';
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('1.234,5');
    });
  });

  // -------------------------------------------------------------------------
  // Reduced motion (browser) — jump straight to the target, emit complete,
  // never wire up an observer.
  // -------------------------------------------------------------------------
  describe('reduced motion', () => {
    it('shows the final value, emits countComplete and skips the observer', () => {
      configure({ platform: 'browser', reducedMotion: true });
      const fixture = createHost();
      fixture.componentInstance.target = 250;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('250');
      expect(fixture.componentInstance.completeCount).toBe(1);
      expect(MockIntersectionObserver.instances.length).toBe(0);
    });
  });

  // -------------------------------------------------------------------------
  // Browser (animated) — observer wiring + completion via a stubbed RAF.
  // -------------------------------------------------------------------------
  describe('browser (animated)', () => {
    it('initially displays the start value', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.startValue = 10;
      fixture.detectChanges();

      expect(span(fixture).textContent).toBe('10');
    });

    it('creates an IntersectionObserver with the configured threshold and observes the element', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.threshold = 0.3;
      fixture.detectChanges();

      expect(MockIntersectionObserver.instances.length).toBe(1);
      expect(MockIntersectionObserver.last.options?.threshold).toBe(0.3);
      expect(MockIntersectionObserver.last.observe).toHaveBeenCalledWith(span(fixture));
    });

    it('counts to the target and emits completion when intersecting', () => {
      spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
        cb(1e15); // huge timestamp => progress clamps to 1 => completes immediately
        return 1;
      });

      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.target = 1000;
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);

      expect(span(fixture).textContent).toBe('1,000');
      expect(fixture.componentInstance.completeCount).toBe(1);
      expect(fixture.componentInstance.updates.length).toBeGreaterThan(0);
      expect(fixture.componentInstance.updates[fixture.componentInstance.updates.length - 1]).toBe(1000);
      expect(MockIntersectionObserver.last.unobserve).toHaveBeenCalledWith(span(fixture));
    });

    it('does not re-trigger on a second intersection', () => {
      spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
        cb(1e15);
        return 1;
      });

      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);
      MockIntersectionObserver.last.trigger(true);

      expect(fixture.componentInstance.completeCount).toBe(1);
    });

    it('respects the delay before starting to count', fakeAsync(() => {
      spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
        cb(1e15);
        return 1;
      });

      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.target = 42;
      fixture.componentInstance.delay = 100;
      fixture.detectChanges();

      MockIntersectionObserver.last.trigger(true);
      // Before the delay elapses, counting has not completed.
      expect(fixture.componentInstance.completeCount).toBe(0);

      tick(100);
      expect(fixture.componentInstance.completeCount).toBe(1);
      expect(span(fixture).textContent).toBe('42');
    }));

    it('triggerCount() manually runs the animation only once', () => {
      spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
        cb(1e15);
        return 1;
      });

      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.target = 77;
      fixture.detectChanges();

      const directive = directiveOf(fixture);
      directive.triggerCount();
      directive.triggerCount(); // hasAnimated guard => no second run

      expect(span(fixture).textContent).toBe('77');
      expect(fixture.componentInstance.completeCount).toBe(1);
    });

    it('reset() restores the start-value display', () => {
      configure({ platform: 'browser' });
      const fixture = createHost();
      fixture.componentInstance.startValue = 5;
      fixture.detectChanges();

      const directive = directiveOf(fixture);
      span(fixture).textContent = 'mutated';
      directive.reset();

      expect(span(fixture).textContent).toBe('5');
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
});
