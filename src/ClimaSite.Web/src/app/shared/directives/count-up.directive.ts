import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID, 
  output 
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

export type EasingFunction = 'linear' | 'ease-out' | 'ease-in-out' | 'ease-out-quart' | 'ease-out-expo';

/**
 * CountUpDirective - Animated number counting with enhanced features
 * 
 * Smoothly counts from a start value to a target value when scrolled into view.
 * 
 * Usage:
 * <span [appCountUp]="5000">0</span>
 * <span [appCountUp]="99.9" [decimals]="1" suffix="%">0</span>
 * <span [appCountUp]="50000" prefix="$" [separator]="true">0</span>
 */
@Directive({
  selector: '[appCountUp]',
  standalone: true
})
export class CountUpDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Target value to count to */
  targetValue = input.required<number>({ alias: 'appCountUp' });
  
  /** Starting value */
  startValue = input<number>(0);
  
  /** Animation duration in milliseconds */
  duration = input<number>(2000);
  
  /** Prefix (e.g., '$', '€') */
  prefix = input<string>('');
  
  /** Suffix (e.g., '%', '+', 'K') */
  suffix = input<string>('');
  
  /** Number of decimal places */
  decimals = input<number>(0);
  
  /** Use thousand separator */
  separator = input<boolean>(true);
  
  /** Separator character */
  separatorChar = input<string>(',');
  
  /** Decimal character */
  decimalChar = input<string>('.');
  
  /** Easing function */
  easing = input<EasingFunction>('ease-out-quart');
  
  /** Intersection observer threshold */
  threshold = input<number>(0.3);
  
  /** Delay before starting (ms) */
  delay = input<number>(0);
  
  /** Emit when counting completes */
  countComplete = output<void>();
  
  /** Emit current value during animation */
  countUpdate = output<number>();
  
  private observer: IntersectionObserver | null = null;
  private animationId: number | null = null;
  private hasAnimated = false;
  private element!: HTMLElement;
  
  ngOnInit(): void {
    this.element = this.el.nativeElement;
    
    if (!isPlatformBrowser(this.platformId)) {
      // SSR: show final value immediately
      this.updateDisplay(this.targetValue());
      return;
    }
    
    // Set initial display
    this.updateDisplay(this.startValue());
    
    // Skip animation if reduced motion preferred
    if (this.animationService.prefersReducedMotion()) {
      this.updateDisplay(this.targetValue());
      this.countComplete.emit();
      return;
    }
    
    this.setupObserver();
  }
  
  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
      this.observer = null;
    }
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }
  }
  
  private setupObserver(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !this.hasAnimated) {
            this.hasAnimated = true;
            
            // Apply delay if specified
            if (this.delay() > 0) {
              setTimeout(() => this.startCounting(), this.delay());
            } else {
              this.startCounting();
            }
            
            this.observer?.unobserve(this.element);
          }
        });
      },
      { threshold: this.threshold() }
    );
    
    this.observer.observe(this.element);
  }
  
  private startCounting(): void {
    const startTime = performance.now();
    const start = this.startValue();
    const target = this.targetValue();
    const durationMs = this.duration();
    const easingFn = this.getEasingFunction();
    
    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / durationMs, 1);
      const easedProgress = easingFn(progress);
      
      const currentValue = start + (target - start) * easedProgress;
      
      this.updateDisplay(currentValue);
      this.countUpdate.emit(currentValue);
      
      if (progress < 1) {
        this.animationId = requestAnimationFrame(animate);
      } else {
        this.updateDisplay(target);
        this.countComplete.emit();
        this.animationId = null;
      }
    };
    
    this.animationId = requestAnimationFrame(animate);
  }
  
  private getEasingFunction(): (t: number) => number {
    switch (this.easing()) {
      case 'linear':
        return (t) => t;
      
      case 'ease-out':
        return (t) => 1 - Math.pow(1 - t, 3);
      
      case 'ease-in-out':
        return (t) => t < 0.5 
          ? 4 * t * t * t 
          : 1 - Math.pow(-2 * t + 2, 3) / 2;
      
      case 'ease-out-quart':
        return (t) => 1 - Math.pow(1 - t, 4);
      
      case 'ease-out-expo':
        return (t) => t === 1 ? 1 : 1 - Math.pow(2, -10 * t);
      
      default:
        return (t) => 1 - Math.pow(1 - t, 4);
    }
  }
  
  private updateDisplay(value: number): void {
    const formattedValue = this.formatNumber(value);
    this.element.textContent = `${this.prefix()}${formattedValue}${this.suffix()}`;
  }
  
  private formatNumber(value: number): string {
    const decimalsCount = this.decimals();
    
    // Round to specified decimals
    const rounded = decimalsCount > 0 
      ? value.toFixed(decimalsCount) 
      : Math.floor(value).toString();
    
    if (!this.separator()) {
      return rounded;
    }
    
    // Add thousand separators
    const parts = rounded.split('.');
    const integerPart = parts[0].replace(
      /\B(?=(\d{3})+(?!\d))/g, 
      this.separatorChar()
    );
    
    if (parts.length === 2) {
      return `${integerPart}${this.decimalChar()}${parts[1]}`;
    }
    
    return integerPart;
  }
  
  /**
   * Manually trigger the count animation
   */
  triggerCount(): void {
    if (!this.hasAnimated) {
      this.hasAnimated = true;
      this.startCounting();
    }
  }
  
  /**
   * Reset and re-trigger the animation
   */
  reset(): void {
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }
    this.hasAnimated = false;
    this.updateDisplay(this.startValue());
  }
}
