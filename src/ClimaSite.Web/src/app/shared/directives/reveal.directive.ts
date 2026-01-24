import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID,
  Renderer2,
  output
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

/**
 * Simplified animation types following Nordic Tech design philosophy.
 * Only essential, subtle animations are supported.
 */
export type RevealAnimation = 
  | 'fade'
  | 'fade-up'
  | 'fade-down';

/**
 * RevealDirective - Subtle scroll-triggered reveal animations
 * 
 * Following the "Nordic Tech" design philosophy: Animation should communicate, not decorate.
 * Only essential, subtle animations are supported: fade, fade-up, fade-down.
 * 
 * Usage:
 * <div appReveal>Fade up (default)</div>
 * <div appReveal="fade" [delay]="100">Simple fade</div>
 * <div appReveal="fade-down" [delay]="200" [stagger]="true">Staggered items</div>
 */
@Directive({
  selector: '[appReveal]',
  standalone: true
})
export class RevealDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Animation type */
  animation = input<RevealAnimation>('fade-up', { alias: 'appReveal' });
  
  /** Delay before animation starts (ms) */
  delay = input<number>(0);
  
  /** Animation duration (ms) - reduced for snappier feel */
  duration = input<number>(300);
  
  /** Intersection observer threshold (0-1) */
  threshold = input<number>(0.15);
  
  /** Distance for translate animations (px) - reduced for subtlety */
  distance = input<number>(16);
  
  /** Enable stagger for list items */
  stagger = input<boolean>(false);
  
  /** Stagger delay between items (ms) */
  staggerDelay = input<number>(50);
  
  /** Only animate once */
  once = input<boolean>(true);
  
  /** Easing function */
  easing = input<string>('cubic-bezier(0.16, 1, 0.3, 1)');
  
  /** Root margin for intersection observer */
  rootMargin = input<string>('0px 0px -50px 0px');
  
  /** Emits when animation starts */
  revealed = output<void>();
  
  /** Emits when animation resets (if once=false) */
  hidden = output<void>();
  
  private element!: HTMLElement;
  private observer: IntersectionObserver | null = null;
  private hasAnimated = false;
  
  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.element = this.el.nativeElement;
    this.setInitialState();
    this.setupObserver();
  }
  
  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
      this.observer = null;
    }
  }
  
  private setInitialState(): void {
    // Skip animations if reduced motion preferred
    if (this.animationService.prefersReducedMotion()) {
      return;
    }
    
    const styles = this.getInitialStyles();
    
    Object.entries(styles).forEach(([property, value]) => {
      this.renderer.setStyle(this.element, property, value);
    });
    
    // Set transition
    const transition = this.getTransitionProperties()
      .map(prop => `${prop} ${this.duration()}ms ${this.easing()} ${this.delay()}ms`)
      .join(', ');
    
    this.renderer.setStyle(this.element, 'transition', transition);
    this.renderer.setStyle(this.element, 'will-change', this.getWillChangeProperties());
  }
  
  private getInitialStyles(): Record<string, string> {
    const dist = this.distance();
    
    switch (this.animation()) {
      case 'fade':
        return { opacity: '0' };
      
      case 'fade-up':
        return { opacity: '0', transform: `translateY(${dist}px)` };
      
      case 'fade-down':
        return { opacity: '0', transform: `translateY(-${dist}px)` };
      
      default:
        // Default to fade-up for any unrecognized animation
        return { opacity: '0', transform: `translateY(${dist}px)` };
    }
  }
  
  private getRevealedStyles(): Record<string, string> {
    return {
      opacity: '1',
      transform: 'none'
    };
  }
  
  private getTransitionProperties(): string[] {
    return ['opacity', 'transform'];
  }
  
  private getWillChangeProperties(): string {
    return 'opacity, transform';
  }
  
  private setupObserver(): void {
    if (this.animationService.prefersReducedMotion()) {
      // Immediately show element without animation
      this.reveal();
      return;
    }
    
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !this.hasAnimated) {
            this.reveal();
            this.hasAnimated = true;
            
            if (this.once()) {
              this.observer?.unobserve(this.element);
            }
          } else if (!entry.isIntersecting && !this.once() && this.hasAnimated) {
            this.hide();
            this.hasAnimated = false;
          }
        });
      },
      {
        threshold: this.threshold(),
        rootMargin: this.rootMargin()
      }
    );
    
    this.observer.observe(this.element);
  }
  
  private reveal(): void {
    const styles = this.getRevealedStyles();
    
    Object.entries(styles).forEach(([property, value]) => {
      this.renderer.setStyle(this.element, property, value);
    });
    
    // Clean up will-change after animation
    setTimeout(() => {
      this.renderer.setStyle(this.element, 'will-change', 'auto');
    }, this.duration() + this.delay() + 100);
    
    this.revealed.emit();
  }
  
  private hide(): void {
    const styles = this.getInitialStyles();
    
    // Re-enable will-change
    this.renderer.setStyle(this.element, 'will-change', this.getWillChangeProperties());
    
    Object.entries(styles).forEach(([property, value]) => {
      this.renderer.setStyle(this.element, property, value);
    });
    
    this.hidden.emit();
  }
}
