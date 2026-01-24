import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID,
  Renderer2,
  effect
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

export type ProgressStyle = 'width' | 'height' | 'scale-x' | 'scale-y' | 'opacity' | 'background';

/**
 * ScrollProgressDirective - Animates element based on scroll progress
 * 
 * Useful for progress bars, scroll indicators, and scroll-linked animations.
 * 
 * Usage:
 * <div appScrollProgress>Width-based progress bar</div>
 * <div appScrollProgress="scale-x" [min]="0" [max]="100">Scale progress</div>
 * <div appScrollProgress="opacity">Fade on scroll</div>
 */
@Directive({
  selector: '[appScrollProgress]',
  standalone: true
})
export class ScrollProgressDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Progress style type */
  style = input<ProgressStyle>('width', { alias: 'appScrollProgress' });
  
  /** Minimum value (for scale/opacity) */
  min = input<number>(0);
  
  /** Maximum value (for scale/opacity) */
  max = input<number>(1);
  
  /** Invert the progress (1 to 0 instead of 0 to 1) */
  invert = input<boolean>(false);
  
  /** Background gradient colors (for background style) */
  gradientColors = input<string[]>(['var(--color-primary)', 'var(--color-accent)']);
  
  private element!: HTMLElement;
  private effectRef: ReturnType<typeof effect> | null = null;
  
  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.element = this.el.nativeElement;
    this.setupStyles();
    this.setupScrollEffect();
  }
  
  ngOnDestroy(): void {
    // Effect is automatically cleaned up by Angular
  }
  
  private setupStyles(): void {
    // Add will-change for performance
    switch (this.style()) {
      case 'width':
      case 'height':
        this.renderer.setStyle(this.element, 'will-change', this.style());
        break;
      case 'scale-x':
      case 'scale-y':
        this.renderer.setStyle(this.element, 'will-change', 'transform');
        this.renderer.setStyle(this.element, 'transform-origin', 'left center');
        break;
      case 'opacity':
        this.renderer.setStyle(this.element, 'will-change', 'opacity');
        break;
      case 'background':
        this.renderer.setStyle(this.element, 'will-change', 'background-size');
        const colors = this.gradientColors();
        this.renderer.setStyle(
          this.element, 
          'background', 
          `linear-gradient(90deg, ${colors.join(', ')})`
        );
        this.renderer.setStyle(this.element, 'background-size', '200% 100%');
        break;
    }
  }
  
  private setupScrollEffect(): void {
    this.effectRef = effect(() => {
      let progress = this.animationService.scrollProgress();
      
      if (this.invert()) {
        progress = 1 - progress;
      }
      
      this.updateElement(progress);
    });
  }
  
  private updateElement(progress: number): void {
    const min = this.min();
    const max = this.max();
    const value = min + (max - min) * progress;
    
    switch (this.style()) {
      case 'width':
        this.renderer.setStyle(this.element, 'width', `${value * 100}%`);
        break;
      
      case 'height':
        this.renderer.setStyle(this.element, 'height', `${value * 100}%`);
        break;
      
      case 'scale-x':
        this.renderer.setStyle(this.element, 'transform', `scaleX(${value})`);
        break;
      
      case 'scale-y':
        this.renderer.setStyle(this.element, 'transform', `scaleY(${value})`);
        break;
      
      case 'opacity':
        this.renderer.setStyle(this.element, 'opacity', String(value));
        break;
      
      case 'background':
        const position = progress * 100;
        this.renderer.setStyle(this.element, 'background-position', `${position}% 0`);
        break;
    }
  }
}
