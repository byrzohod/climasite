import { Directive, ElementRef, inject, input, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Directive({
  selector: '[appParallax]',
  standalone: true
})
export class ParallaxDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);

  speed = input<number>(0.5);
  direction = input<'up' | 'down'>('up');

  private rafId: number | null = null;
  private lastScrollY = 0;

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.lastScrollY = window.scrollY;
    this.updatePosition();
    window.addEventListener('scroll', this.onScroll, { passive: true });
  }

  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    window.removeEventListener('scroll', this.onScroll);
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
    }
  }

  private onScroll = (): void => {
    if (this.rafId) return;

    this.rafId = requestAnimationFrame(() => {
      this.updatePosition();
      this.rafId = null;
    });
  };

  private updatePosition(): void {
    const scrollY = window.scrollY;
    const element = this.el.nativeElement as HTMLElement;
    const rect = element.getBoundingClientRect();

    // Only apply parallax when element is in view
    if (rect.bottom > 0 && rect.top < window.innerHeight) {
      const multiplier = this.direction() === 'up' ? -1 : 1;
      const offset = scrollY * this.speed() * multiplier;
      element.style.transform = `translateY(${offset}px)`;
    }
  }
}
