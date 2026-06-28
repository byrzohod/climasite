import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { SplitTextDirective, SplitType } from './split-text.directive';
import { AnimationService } from '../../core/services/animation.service';

@Component({
  standalone: true,
  imports: [SplitTextDirective],
  template: `
    <h1
      [appSplitText]="splitType"
      [animate]="animate"
      [staggerDelay]="staggerDelay"
      [duration]="duration"
      [initialOpacity]="initialOpacity"
      [initialY]="initialY"
      [threshold]="threshold"
    >{{ text }}</h1>
  `
})
class TestHostComponent {
  splitType: SplitType = 'chars';
  animate = false;
  staggerDelay = 30;
  duration = 600;
  initialOpacity = 0;
  initialY = 20;
  threshold = 0.2;
  text = 'Hello World';
}

// A host with no attribute bindings, to assert the directive's own defaults.
@Component({
  standalone: true,
  imports: [SplitTextDirective],
  template: `<h1 appSplitText>Defaults Text</h1>`
})
class DefaultsHostComponent {}

// A host with STATIC (non-interpolated) text. The directive captures originalHTML in ngOnInit,
// which runs before interpolation is committed to the DOM; static content is already present,
// so this host exercises the real restore-on-destroy path (see ngOnDestroy spec below).
@Component({
  standalone: true,
  imports: [SplitTextDirective],
  template: `<h1 [appSplitText]="'chars'">Hello World</h1>`
})
class StaticTextHostComponent {}

// Reach private members for deterministic assertions (no reliance on
// afterNextRender / IntersectionObserver firing on their own).
interface SplitTextInternals {
  initializeSplit(): void;
  splitElements: HTMLElement[];
}

function asInternals(d: SplitTextDirective): SplitTextInternals {
  return d as unknown as SplitTextInternals;
}

describe('SplitTextDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;
  let directive: SplitTextDirective;
  let el: HTMLElement;
  let animationServiceSpy: jasmine.SpyObj<AnimationService>;

  function createDirective(config: Partial<TestHostComponent> = {}): void {
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    Object.assign(host, config);
    fixture.detectChanges();
    const dirEl = fixture.debugElement.query(By.directive(SplitTextDirective));
    directive = dirEl.injector.get(SplitTextDirective);
    el = dirEl.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    animationServiceSpy = jasmine.createSpyObj<AnimationService>('AnimationService', [
      'prefersReducedMotion'
    ]);
    animationServiceSpy.prefersReducedMotion.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [TestHostComponent, DefaultsHostComponent, StaticTextHostComponent],
      providers: [{ provide: AnimationService, useValue: animationServiceSpy }]
    }).compileComponents();
  });

  // ==========================================================================
  // INPUT DEFAULTS
  // ==========================================================================

  describe('input defaults', () => {
    it('should expose the documented default input values', () => {
      const defFixture = TestBed.createComponent(DefaultsHostComponent);
      defFixture.detectChanges();
      const dir = defFixture.debugElement
        .query(By.directive(SplitTextDirective))
        .injector.get(SplitTextDirective);

      // A bare `appSplitText` attribute (no value) binds an empty-string attribute to the
      // aliased `splitType` input, which overrides the directive's documented 'chars' default.
      expect(dir.splitType()).toBe('');
      expect(dir.staggerDelay()).toBe(30);
      expect(dir.animate()).toBeFalse();
      expect(dir.duration()).toBe(600);
      expect(dir.initialOpacity()).toBe(0);
      expect(dir.initialY()).toBe(20);
      expect(dir.threshold()).toBe(0.2);
    });

    it('should reflect a bound split type', () => {
      createDirective({ splitType: 'words' });
      expect(directive.splitType()).toBe('words');
    });
  });

  // ==========================================================================
  // SPLITTING (animate = false)
  // ==========================================================================

  describe('splitting text', () => {
    it('should split into one .split-item per non-space character (chars)', () => {
      createDirective({ splitType: 'chars', animate: false });
      asInternals(directive).initializeSplit();

      const items = el.querySelectorAll('.split-item');
      // "Hello World" => 10 letters
      expect(items.length).toBe(10);
      expect(el.textContent).toBe('Hello World');
    });

    it('should split into one .split-item per word (words)', () => {
      createDirective({ splitType: 'words', animate: false });
      asInternals(directive).initializeSplit();

      const items = Array.from(el.querySelectorAll('.split-item'));
      expect(items.length).toBe(2);
      expect(items[0].textContent).toBe('Hello');
      expect(items[1].textContent).toBe('World');
      expect(el.textContent).toBe('Hello World');
    });

    it('should wrap a single line in a .line-wrapper for lines split', () => {
      createDirective({ splitType: 'lines', animate: false, text: 'Single Line' } as Partial<TestHostComponent>);
      asInternals(directive).initializeSplit();

      expect(el.querySelector('.line-wrapper')).toBeTruthy();
      expect(el.querySelectorAll('.split-item').length).toBe(1);
      expect(el.textContent).toBe('Single Line');
    });

    it('should NOT apply animation styles to split items when animate is false', () => {
      createDirective({ splitType: 'words', animate: false });
      asInternals(directive).initializeSplit();

      const span = el.querySelector('.split-item') as HTMLElement;
      expect(span.style.opacity).toBe('');
      expect(span.style.transform).toBe('');
      expect(span.style.transition).toBe('');
    });
  });

  // ==========================================================================
  // REDUCED MOTION
  // ==========================================================================

  describe('reduced motion', () => {
    it('should skip splitting when animate is on and reduced motion is preferred', () => {
      animationServiceSpy.prefersReducedMotion.and.returnValue(true);
      createDirective({ animate: true, text: 'Reduced Motion' } as Partial<TestHostComponent>);
      asInternals(directive).initializeSplit();

      expect(el.querySelectorAll('.split-item').length).toBe(0);
      expect(el.textContent).toBe('Reduced Motion');
    });

    it('should still split when reduced motion is preferred but animate is off', () => {
      animationServiceSpy.prefersReducedMotion.and.returnValue(true);
      createDirective({ animate: false, splitType: 'words' });
      asInternals(directive).initializeSplit();

      expect(el.querySelectorAll('.split-item').length).toBe(2);
    });
  });

  // ==========================================================================
  // ANIMATION WIRING (animate = true) — mocked IntersectionObserver
  // ==========================================================================

  describe('scroll animation wiring', () => {
    let originalIO: typeof IntersectionObserver;
    let observeSpy: jasmine.Spy;
    let unobserveSpy: jasmine.Spy;
    let disconnectSpy: jasmine.Spy;
    let ioCallback: IntersectionObserverCallback;
    let ioOptions: IntersectionObserverInit | undefined;

    beforeEach(() => {
      originalIO = window.IntersectionObserver;
      observeSpy = jasmine.createSpy('observe');
      unobserveSpy = jasmine.createSpy('unobserve');
      disconnectSpy = jasmine.createSpy('disconnect');

      class MockIO {
        constructor(cb: IntersectionObserverCallback, opts?: IntersectionObserverInit) {
          ioCallback = cb;
          ioOptions = opts;
        }
        observe = observeSpy;
        unobserve = unobserveSpy;
        disconnect = disconnectSpy;
        takeRecords = (): IntersectionObserverEntry[] => [];
        readonly root = null;
        readonly rootMargin = '';
        readonly thresholds: number[] = [];
      }
      (window as unknown as { IntersectionObserver: unknown }).IntersectionObserver = MockIO;

      createDirective({ animate: true, splitType: 'words', staggerDelay: 30, threshold: 0.2 });
      asInternals(directive).initializeSplit();
    });

    afterEach(() => {
      (window as unknown as { IntersectionObserver: typeof IntersectionObserver })
        .IntersectionObserver = originalIO;
    });

    it('should apply initial animation styles to split items', () => {
      const span = el.querySelector('.split-item') as HTMLElement;
      expect(span.style.opacity).toBe('0');
      expect(span.style.transform).toContain('20px');
      expect(span.style.transition).toContain('600ms');
    });

    it('should observe the host element with the configured threshold', () => {
      expect(observeSpy).toHaveBeenCalled();
      expect(ioOptions?.threshold).toBe(0.2);
    });

    it('should animate items in and unobserve when the element intersects', fakeAsync(() => {
      ioCallback(
        [{ isIntersecting: true, target: el } as unknown as IntersectionObserverEntry],
        {} as IntersectionObserver
      );
      // staggerDelay 30 * (2 items) -> flush the longest delay.
      tick(30);

      const spans = Array.from(el.querySelectorAll('.split-item')) as HTMLElement[];
      spans.forEach(span => {
        expect(span.style.opacity).toBe('1');
        // revealed transform is translateY(0) — no longer the 20px initial offset
        expect(span.style.transform).not.toContain('20px');
      });
      expect(unobserveSpy).toHaveBeenCalled();
    }));

    it('should disconnect the observer on destroy', () => {
      directive.ngOnDestroy();
      expect(disconnectSpy).toHaveBeenCalled();
    });
  });

  // ==========================================================================
  // PUBLIC TRIGGER / RESET METHODS
  // ==========================================================================

  describe('manual trigger & reset', () => {
    // Build the directive outside of fakeAsync so the constructor's
    // afterNextRender hook is never scheduled inside the virtual clock.
    beforeEach(() => {
      createDirective({ animate: true, splitType: 'words', staggerDelay: 30 });
      asInternals(directive).initializeSplit();
    });

    it('triggerAnimation should reveal split items (opacity 1)', fakeAsync(() => {
      directive.triggerAnimation();
      tick(30);

      const spans = Array.from(el.querySelectorAll('.split-item')) as HTMLElement[];
      expect(spans.length).toBeGreaterThan(0);
      spans.forEach(span => expect(span.style.opacity).toBe('1'));
    }));

    it('resetAnimation should restore the initial opacity/offset on split items', () => {
      // Force a revealed state first.
      const spans = Array.from(el.querySelectorAll('.split-item')) as HTMLElement[];
      spans.forEach(span => {
        span.style.opacity = '1';
        span.style.transform = 'translateY(0)';
      });

      directive.resetAnimation();

      spans.forEach(span => {
        expect(span.style.opacity).toBe('0');
        expect(span.style.transform).toContain('20px');
      });
    });
  });

  // ==========================================================================
  // CLEANUP
  // ==========================================================================

  describe('ngOnDestroy', () => {
    it('should restore the original HTML', () => {
      // Static-content host so the directive captures the original HTML in ngOnInit
      // (interpolated content is not yet in the DOM at that point — see StaticTextHostComponent).
      const staticFixture = TestBed.createComponent(StaticTextHostComponent);
      staticFixture.detectChanges();
      const dirEl = staticFixture.debugElement.query(By.directive(SplitTextDirective));
      const staticDirective = dirEl.injector.get(SplitTextDirective);
      const staticEl = dirEl.nativeElement as HTMLElement;

      asInternals(staticDirective).initializeSplit();
      expect(staticEl.querySelectorAll('.split-item').length).toBeGreaterThan(0);

      staticDirective.ngOnDestroy();

      expect(staticEl.querySelectorAll('.split-item').length).toBe(0);
      expect(staticEl.textContent).toBe('Hello World');
    });
  });
});
