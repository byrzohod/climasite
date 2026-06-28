import { Component, PLATFORM_ID, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MagneticHoverDirective } from './magnetic-hover.directive';
import { AnimationService } from '../../core/services/animation.service';

@Component({
  standalone: true,
  imports: [MagneticHoverDirective],
  template: `
    <button
      appMagneticHover
      [strength]="strength"
      [scale]="scale"
      [rotate]="rotate"
      [duration]="duration"
    >Hover me</button>
  `
})
class HostComponent {
  strength = 0.2;
  scale = 1.02;
  rotate = false;
  duration = 150;
}

const RECT: DOMRect = {
  left: 0,
  top: 0,
  right: 100,
  bottom: 100,
  width: 100,
  height: 100,
  x: 0,
  y: 0,
  toJSON: () => ({})
} as DOMRect;

describe('MagneticHoverDirective', () => {
  let reducedMotion: ReturnType<typeof signal<boolean>>;

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

  function btn(fixture: ComponentFixture<HostComponent>): HTMLElement {
    return fixture.nativeElement.querySelector('button');
  }

  // -------------------------------------------------------------------------
  // SSR — no styles, no listeners.
  // -------------------------------------------------------------------------
  it('does nothing on the server', () => {
    configure({ platform: 'server' });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);

    expect(element.style.transition).toBe('');
    expect(element.style.willChange).toBe('');

    element.dispatchEvent(new MouseEvent('mouseenter'));
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));
    expect(element.style.transform).toBe('');
  });

  // -------------------------------------------------------------------------
  // Setup styles (browser).
  // -------------------------------------------------------------------------
  it('applies baseline styles on init', () => {
    configure({ platform: 'browser' });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);

    expect(element.style.transition).toContain('cubic-bezier');
    expect(element.style.transitionProperty).toContain('transform');
    expect(element.style.transformOrigin).toContain('center');
    expect(element.style.willChange).toBe('transform');
  });

  // -------------------------------------------------------------------------
  // Magnetic follow on mousemove.
  // -------------------------------------------------------------------------
  it('translates toward the cursor by strength on mousemove', () => {
    spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
      cb(0);
      return 1;
    });

    configure({ platform: 'browser' });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);
    spyOn(element, 'getBoundingClientRect').and.returnValue(RECT);

    element.dispatchEvent(new MouseEvent('mouseenter'));
    // cursor at (100,100); center (50,50); delta (50,50); strength 0.2 => (10,10)
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));

    expect(element.style.transform).toContain('translate(10px, 10px)');
    expect(element.style.transform).toContain('scale(1.02)');
    expect(element.style.transform).toContain('rotate(0deg)');
  });

  it('applies a rotation when rotate is enabled', () => {
    spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
      cb(0);
      return 1;
    });

    configure({ platform: 'browser' });
    const fixture = createHost();
    fixture.componentInstance.rotate = true;
    fixture.detectChanges();
    const element = btn(fixture);
    spyOn(element, 'getBoundingClientRect').and.returnValue(RECT);

    element.dispatchEvent(new MouseEvent('mouseenter'));
    // deltaX 50, width 100 => (50/100)*5 = 2.5deg
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));

    expect(element.style.transform).toContain('rotate(2.5deg)');
  });

  it('resets the transform on mouseleave', () => {
    spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
      cb(0);
      return 1;
    });

    configure({ platform: 'browser' });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);
    spyOn(element, 'getBoundingClientRect').and.returnValue(RECT);

    element.dispatchEvent(new MouseEvent('mouseenter'));
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));
    expect(element.style.transform).toContain('translate(10px, 10px)');

    element.dispatchEvent(new MouseEvent('mouseleave'));
    expect(element.style.transform).toContain('scale(1)');
    expect(element.style.transform).toContain('rotate(0deg)');
    expect(element.style.transform).not.toContain('translate(10px, 10px)');
  });

  // -------------------------------------------------------------------------
  // Reduced motion — no magnetic movement.
  // -------------------------------------------------------------------------
  it('does not move when reduced motion is preferred', () => {
    spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
      cb(0);
      return 1;
    });

    configure({ platform: 'browser', reducedMotion: true });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);
    spyOn(element, 'getBoundingClientRect').and.returnValue(RECT);

    element.dispatchEvent(new MouseEvent('mouseenter'));
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));

    expect(element.style.transform).not.toContain('scale(1.02)');
  });

  // -------------------------------------------------------------------------
  // Cleanup — listeners removed on destroy.
  // -------------------------------------------------------------------------
  it('removes listeners on destroy so further moves have no effect', () => {
    spyOn(window, 'requestAnimationFrame').and.callFake((cb: FrameRequestCallback) => {
      cb(0);
      return 1;
    });

    configure({ platform: 'browser' });
    const fixture = createHost();
    fixture.detectChanges();
    const element = btn(fixture);
    spyOn(element, 'getBoundingClientRect').and.returnValue(RECT);

    fixture.destroy();

    element.dispatchEvent(new MouseEvent('mouseenter'));
    element.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }));

    expect(element.style.transform).not.toContain('translate(10px, 10px)');
  });
});
