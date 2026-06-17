import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID, signal } from '@angular/core';
import { FlyingCartService } from './flying-cart.service';
import { AnimationService } from './animation.service';
import { CartService } from './cart.service';

/**
 * Unit specs for FlyingCartService (Plan 21F Phase 3 — TASK-21F-009).
 *
 * Asserts the refined animation constants (subtle, faster flight) and the
 * reduced-motion behavior. These tests guard the values changed in Phase 3:
 *   - ANIMATION_DURATION 600 -> 400ms
 *   - IMAGE_SIZE 60 -> 48px
 *   - arc apex offset -100 -> -50
 */
describe('FlyingCartService', () => {
  let service: FlyingCartService;
  let reducedMotion: ReturnType<typeof signal<boolean>>;
  let openMiniCartSpy: jasmine.Spy;

  beforeEach(() => {
    reducedMotion = signal(false);
    openMiniCartSpy = jasmine.createSpy('openMiniCart');

    const animationStub: Partial<AnimationService> = {
      // prefersReducedMotion is a readonly signal — expose the toggleable one
      prefersReducedMotion: reducedMotion.asReadonly()
    };
    const cartStub: Partial<CartService> = {
      openMiniCart: openMiniCartSpy
    };

    TestBed.configureTestingModule({
      providers: [
        FlyingCartService,
        { provide: PLATFORM_ID, useValue: 'browser' },
        { provide: AnimationService, useValue: animationStub },
        { provide: CartService, useValue: cartStub }
      ]
    });

    service = TestBed.inject(FlyingCartService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('refined animation constants (Phase 3)', () => {
    it('uses a 400ms flight duration (was 600ms)', () => {
      expect((service as unknown as { ANIMATION_DURATION: number }).ANIMATION_DURATION).toBe(400);
    });

    it('uses a 48px flying image size (was 60px)', () => {
      expect((service as unknown as { IMAGE_SIZE: number }).IMAGE_SIZE).toBe(48);
    });
  });

  describe('arc apex offset', () => {
    it('uses a subtle -50px apex offset (was -100px)', () => {
      // Stub geometry so we can capture the arc control point passed to animateArc.
      const sourceEl = document.createElement('div');
      sourceEl.getBoundingClientRect = () =>
        ({ left: 100, top: 400, width: 40, height: 40 } as DOMRect);

      const cartIcon = document.createElement('div');
      cartIcon.setAttribute('data-testid', 'cart-icon');
      cartIcon.getBoundingClientRect = () =>
        ({ left: 800, top: 50, width: 40, height: 40 } as DOMRect);
      document.body.appendChild(cartIcon);

      const animateSpy = spyOn(
        service as unknown as { animateArc: (...args: number[]) => void },
        'animateArc'
      ).and.stub();

      service.fly({ imageUrl: 'x.png', sourceElement: sourceEl, openMiniCart: false });

      expect(animateSpy).toHaveBeenCalled();
      const callArgs = animateSpy.calls.mostRecent().args as number[];
      // animateArc(el, startX, startY, controlX, controlY, endX, endY, cb)
      const startY = callArgs[2];
      const controlY = callArgs[4];
      const endY = callArgs[6];
      // controlY === min(startY, endY) - 50
      expect(controlY).toBe(Math.min(startY, endY) - 50);

      cartIcon.remove();
      // Clean up any flying element appended to body
      document.querySelectorAll('.flying-cart-item').forEach((n) => n.remove());
    });
  });

  describe('reduced-motion behavior', () => {
    it('skips the flying animation but still bumps the cart and opens the mini-cart', () => {
      reducedMotion.set(true);

      const cartIcon = document.createElement('div');
      cartIcon.setAttribute('data-testid', 'cart-icon');
      document.body.appendChild(cartIcon);

      const sourceEl = document.createElement('div');
      const animateSpy = spyOn(
        service as unknown as { animateArc: (...args: unknown[]) => void },
        'animateArc'
      ).and.stub();

      service.fly({ imageUrl: 'x.png', sourceElement: sourceEl, openMiniCart: true });

      // No flying clone should be created
      expect(animateSpy).not.toHaveBeenCalled();
      expect(document.querySelector('.flying-cart-item')).toBeNull();
      // Cart bump applied to the icon
      expect(cartIcon.classList.contains('cart-bump')).toBeTrue();

      cartIcon.remove();
    });
  });
});
