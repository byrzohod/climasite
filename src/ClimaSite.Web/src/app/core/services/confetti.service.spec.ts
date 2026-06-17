import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID, signal } from '@angular/core';
import { ConfettiService } from './confetti.service';
import { AnimationService } from './animation.service';

/**
 * Unit specs for ConfettiService (Plan 21F Phase 3 — TASK-21F-010).
 *
 * Asserts the refined celebration constants and the squares-only shape policy:
 *   - PARTICLE_COUNT 75 -> 50
 *   - ANIMATION_DURATION 3000 -> 2000ms
 *   - FADE_START lowered below the new total (so particles still fade)
 *   - particle shapes = squares only (no circle / ribbon)
 *   - reduced-motion sparkle fallback retained
 */
describe('ConfettiService', () => {
  let service: ConfettiService;
  let reducedMotion: ReturnType<typeof signal<boolean>>;

  type ConfettiInternals = {
    PARTICLE_COUNT: number;
    ANIMATION_DURATION: number;
    FADE_START: number;
    particles: { shape: string }[];
    createParticles: () => void;
    drawParticle: (p: unknown) => void;
    ctx: CanvasRenderingContext2D | null;
  };

  const internals = () => service as unknown as ConfettiInternals;

  beforeEach(() => {
    reducedMotion = signal(false);

    const animationStub: Partial<AnimationService> = {
      prefersReducedMotion: reducedMotion.asReadonly()
    };

    TestBed.configureTestingModule({
      providers: [
        ConfettiService,
        { provide: PLATFORM_ID, useValue: 'browser' },
        { provide: AnimationService, useValue: animationStub }
      ]
    });

    service = TestBed.inject(ConfettiService);
  });

  afterEach(() => {
    service.stop();
    document.querySelectorAll('#confetti-canvas, .confetti-sparkle').forEach((n) => n.remove());
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('refined celebration constants (Phase 3)', () => {
    it('emits 50 particles (was 75)', () => {
      expect(internals().PARTICLE_COUNT).toBe(50);
    });

    it('runs for 2000ms (was 3000ms)', () => {
      expect(internals().ANIMATION_DURATION).toBe(2000);
    });

    it('starts fading before the total duration so particles still fade out', () => {
      expect(internals().FADE_START).toBeLessThan(internals().ANIMATION_DURATION);
    });
  });

  describe('squares-only shapes', () => {
    it('creates the configured count of particles, all squares', () => {
      internals().createParticles();
      const particles = internals().particles;

      expect(particles.length).toBe(50);
      expect(particles.every((p) => p.shape === 'square')).toBeTrue();
      // No legacy shapes remain
      expect(particles.some((p) => p.shape === 'circle' || p.shape === 'ribbon')).toBeFalse();
    });

    it('draws particles with fillRect only (no arc / circle drawing)', () => {
      const fakeCtx = jasmine.createSpyObj<CanvasRenderingContext2D>('ctx', [
        'save',
        'restore',
        'translate',
        'rotate',
        'fillRect',
        'beginPath',
        'arc',
        'fill'
      ]);
      internals().ctx = fakeCtx;

      internals().drawParticle({
        x: 10,
        y: 10,
        vx: 0,
        vy: 0,
        color: '#fff',
        size: 8,
        rotation: 0,
        rotationSpeed: 0,
        opacity: 1,
        shape: 'square'
      });

      expect(fakeCtx.fillRect).toHaveBeenCalledTimes(1);
      expect(fakeCtx.arc).not.toHaveBeenCalled();
      expect(fakeCtx.beginPath).not.toHaveBeenCalled();
    });
  });

  describe('reduced-motion fallback', () => {
    it('shows the sparkle fallback and does not create a confetti canvas', () => {
      reducedMotion.set(true);

      service.burst();

      expect(document.querySelector('.confetti-sparkle')).not.toBeNull();
      expect(document.querySelector('#confetti-canvas')).toBeNull();
    });
  });
});
