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

export type RevealAnimation = 
  | 'fade'
  | 'fade-up'
  | 'fade-down'
  | 'fade-left'
  | 'fade-right'
  | 'scale'
  | 'scale-up'
  | 'blur'
  | 'blur-up'
  | 'slide-up'
  | 'slide-down'
  | 'slide-left'
  | 'slide-right'
  | 'flip-up'
  | 'flip-down'
  | 'zoom-in'
  | 'zoom-out'
  | 'rotate-left'
  | 'rotate-right'
  | 'clip-left'
  | 'clip-right'
  | 'clip-top'
  | 'clip-bottom';

/**
 * RevealDirective - Enhanced scroll-triggered reveal animations
 * 
 * Replaces the basic AnimateOnScrollDirective with more animation options
 * and better performance optimizations.
 * 
 * Usage:
 * <div appReveal>Fade up (default)</div>
 * <div appReveal="fade-left" [delay]="200">Fade from left with delay</div>
 * <div appReveal="blur-up" [duration]="600" [threshold]="0.3">Blur reveal</div>
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
  
  /** Animation duration (ms) */
  duration = input<number>(600);
  
  /** Intersection observer threshold (0-1) */
  threshold = input<number>(0.15);
  
  /** Distance for translate animations (px) */
  distance = input<number>(40);
  
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
      
      case 'fade-left':
        return { opacity: '0', transform: `translateX(-${dist}px)` };
      
      case 'fade-right':
        return { opacity: '0', transform: `translateX(${dist}px)` };
      
      case 'scale':
        return { opacity: '0', transform: 'scale(0.9)' };
      
      case 'scale-up':
        return { opacity: '0', transform: `scale(0.9) translateY(${dist}px)` };
      
      case 'blur':
        return { opacity: '0', filter: 'blur(10px)' };
      
      case 'blur-up':
        return { opacity: '0', filter: 'blur(10px)', transform: `translateY(${dist}px)` };
      
      case 'slide-up':
        return { opacity: '0', transform: `translateY(${dist * 2}px)` };
      
      case 'slide-down':
        return { opacity: '0', transform: `translateY(-${dist * 2}px)` };
      
      case 'slide-left':
        return { opacity: '0', transform: `translateX(-${dist * 2}px)` };
      
      case 'slide-right':
        return { opacity: '0', transform: `translateX(${dist * 2}px)` };
      
      case 'flip-up':
        return { opacity: '0', transform: 'perspective(1000px) rotateX(90deg)' };
      
      case 'flip-down':
        return { opacity: '0', transform: 'perspective(1000px) rotateX(-90deg)' };
      
      case 'zoom-in':
        return { opacity: '0', transform: 'scale(0.5)' };
      
      case 'zoom-out':
        return { opacity: '0', transform: 'scale(1.5)' };
      
      case 'rotate-left':
        return { opacity: '0', transform: 'rotate(-15deg) scale(0.9)' };
      
      case 'rotate-right':
        return { opacity: '0', transform: 'rotate(15deg) scale(0.9)' };
      
      case 'clip-left':
        return { clipPath: 'inset(0 100% 0 0)' };
      
      case 'clip-right':
        return { clipPath: 'inset(0 0 0 100%)' };
      
      case 'clip-top':
        return { clipPath: 'inset(0 0 100% 0)' };
      
      case 'clip-bottom':
        return { clipPath: 'inset(100% 0 0 0)' };
      
      default:
        return { opacity: '0', transform: `translateY(${dist}px)` };
    }
  }
  
  private getRevealedStyles(): Record<string, string> {
    const anim = this.animation();
    
    if (anim.startsWith('clip-')) {
      return { clipPath: 'inset(0 0 0 0)' };
    }
    
    const styles: Record<string, string> = {
      opacity: '1',
      transform: 'none'
    };
    
    if (anim === 'blur' || anim === 'blur-up') {
      styles['filter'] = 'blur(0)';
    }
    
    return styles;
  }
  
  private getTransitionProperties(): string[] {
    const anim = this.animation();
    
    if (anim.startsWith('clip-')) {
      return ['clip-path'];
    }
    
    const props = ['opacity', 'transform'];
    
    if (anim === 'blur' || anim === 'blur-up') {
      props.push('filter');
    }
    
    return props;
  }
  
  private getWillChangeProperties(): string {
    const anim = this.animation();
    
    if (anim.startsWith('clip-')) {
      return 'clip-path';
    }
    
    if (anim === 'blur' || anim === 'blur-up') {
      return 'opacity, transform, filter';
    }
    
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
