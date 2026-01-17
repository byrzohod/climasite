import { Directive, ElementRef, inject, input, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type AnimationType = 'fade-in' | 'fade-in-up' | 'fade-in-left' | 'fade-in-right' | 'scale-in' | 'slide-up';

@Directive({
  selector: '[appAnimateOnScroll]',
  standalone: true
})
export class AnimateOnScrollDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private observer: IntersectionObserver | null = null;

  animation = input<AnimationType>('fade-in-up');
  delay = input<number>(0);
  threshold = input<number>(0.1);
  once = input<boolean>(true);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const element = this.el.nativeElement as HTMLElement;

    // Set initial hidden state
    element.style.opacity = '0';
    element.style.transform = this.getInitialTransform();
    element.style.transition = `opacity 0.6s ease-out ${this.delay()}ms, transform 0.6s ease-out ${this.delay()}ms`;

    // Create intersection observer
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            this.animate(element);
            if (this.once()) {
              this.observer?.unobserve(element);
            }
          } else if (!this.once()) {
            this.reset(element);
          }
        });
      },
      {
        threshold: this.threshold(),
        rootMargin: '0px 0px -50px 0px'
      }
    );

    this.observer.observe(element);
  }

  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
      this.observer = null;
    }
  }

  private getInitialTransform(): string {
    switch (this.animation()) {
      case 'fade-in':
        return 'none';
      case 'fade-in-up':
        return 'translateY(30px)';
      case 'fade-in-left':
        return 'translateX(-30px)';
      case 'fade-in-right':
        return 'translateX(30px)';
      case 'scale-in':
        return 'scale(0.9)';
      case 'slide-up':
        return 'translateY(50px)';
      default:
        return 'translateY(30px)';
    }
  }

  private animate(element: HTMLElement): void {
    element.style.opacity = '1';
    element.style.transform = 'none';
  }

  private reset(element: HTMLElement): void {
    element.style.opacity = '0';
    element.style.transform = this.getInitialTransform();
  }
}
