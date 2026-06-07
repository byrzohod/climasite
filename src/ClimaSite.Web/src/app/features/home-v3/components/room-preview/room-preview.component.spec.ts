import { ComponentFixture, TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RoomPreviewComponent } from './room-preview.component';

describe('RoomPreviewComponent', () => {
  let fixture: ComponentFixture<RoomPreviewComponent>;
  let requestAnimationFrameSpy: jasmine.Spy;

  const canvasTokens: Record<string, string> = {
    '--home-v3-canvas-bg': '#0f172a',
    '--home-v3-canvas-glow': 'rgba(14, 165, 233, 0.25)',
    '--home-v3-canvas-glow-transparent': 'rgba(14, 165, 233, 0)',
    '--home-v3-canvas-stroke': '#1f2937',
    '--home-v3-canvas-stroke-strong': '#334155',
    '--home-v3-floor': '#e2e8f0',
    '--home-v3-wall-back': '#f8fafc',
    '--home-v3-wall-side': '#cbd5e1',
    '--home-v3-ac-top': '#f8fafc',
    '--home-v3-ac-side': '#cbd5e1',
    '--home-v3-ac-front': '#e2e8f0',
    '--home-v3-led-cool': '#38bdf8',
    '--home-v3-led-warm': '#fb923c',
    '--home-v3-flow-cool': '#7dd3fc',
    '--home-v3-flow-warm': '#fdba74',
    '--home-v3-furniture-wood-top': '#92400e',
    '--home-v3-furniture-wood-side': '#78350f',
    '--home-v3-furniture-wood-front': '#a16207',
    '--home-v3-furniture-fabric-top': '#64748b',
    '--home-v3-furniture-fabric-side': '#475569',
    '--home-v3-furniture-fabric-front': '#94a3b8',
    '--home-v3-furniture-light-top': '#f1f5f9',
    '--home-v3-furniture-light-side': '#cbd5e1',
    '--home-v3-furniture-light-front': '#e2e8f0',
    '--home-v3-furniture-dark-top': '#334155',
    '--home-v3-furniture-dark-side': '#1e293b',
    '--home-v3-furniture-dark-front': '#475569'
  };

  beforeEach(async () => {
    spyOn(window, 'matchMedia').and.returnValue({
      matches: true,
      media: '(prefers-reduced-motion: reduce)',
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false
    });
    requestAnimationFrameSpy = spyOn(window, 'requestAnimationFrame').and.callThrough();

    await TestBed.configureTestingModule({
      imports: [
        RoomPreviewComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      'homeV3.preview.ariaLabel': 'Room preview for {{area}} m² {{roomType}} in {{zone}}',
      'homeV3.roomType.living': 'living room',
      'homeV3.zone.B.name': 'moderate zone',
      'homeV3.preview.outside': 'Outside',
      'homeV3.preview.inside': 'Inside',
      'homeV3.preview.power': 'Power'
    });
    translate.use('en');

    fixture = TestBed.createComponent(RoomPreviewComponent);
    Object.entries(canvasTokens).forEach(([name, value]) => fixture.nativeElement.style.setProperty(name, value));
    fixture.componentRef.setInput('area', 24);
    fixture.componentRef.setInput('roomType', 'living');
    fixture.componentRef.setInput('zone', 'B');
    fixture.componentRef.setInput('outsideC', 28);
    fixture.componentRef.setInput('insideC', 23);
    fixture.componentRef.setInput('watts', 238);
  });

  it('renders the canvas fallback and stat chips', fakeAsync(() => {
    requestAnimationFrameSpy.calls.reset();
    fixture.detectChanges();
    flushMicrotasks();

    const frame = fixture.debugElement.query(By.css('.preview-frame')).nativeElement as HTMLElement;
    const canvas = fixture.debugElement.query(By.css('canvas')).nativeElement as HTMLCanvasElement;

    expect(frame.getAttribute('role')).toBe('img');
    expect(frame.getAttribute('aria-label')).toContain('24');
    expect(canvas.width).toBeGreaterThan(0);
    expect(fixture.nativeElement.textContent).toContain('28°C');
    expect(fixture.nativeElement.textContent).toContain('23°C');
    expect(fixture.nativeElement.textContent).toContain('238 W');
  }));

  it('does not schedule animation frames when reduced motion is enabled', fakeAsync(() => {
    requestAnimationFrameSpy.calls.reset();
    fixture.detectChanges();
    flushMicrotasks();

    expect(requestAnimationFrameSpy).not.toHaveBeenCalled();
  }));
});
