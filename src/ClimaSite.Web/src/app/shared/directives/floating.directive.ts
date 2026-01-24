import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID,
  Renderer2
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

export type FloatingVariant = 'gentle' | 'medium' | 'pronounced' | 'custom';

/**
 * FloatingDirective - Creates subtle floating animations
 * 
 * Adds a gentle floating effect to elements, perfect for
 * hero sections and decorative elements.
 * 
 * Usage:
 * <div appFloating>Gentle float</div>
 * <div appFloating [variant]="'pronounced'" [duration]="6000">Larger motion</div>
 * <div appFloating [delay]="500">With stagger delay</div>
 */
@Directive({
  selector: '[appFloating]',
  standalone: true
})
export class FloatingDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);

  /** Animation variant preset */
  variant = input<FloatingVariant>('gentle');
  
  /** Custom duration in milliseconds */
  duration = input<number>(4000);
  
  /** Animation delay in milliseconds */
  delay = input<number>(0);
  
  /** Custom X amplitude in pixels (for custom variant) */
  amplitudeX = input<number>(0);
  
  /** Custom Y amplitude in pixels (for custom variant) */
  amplitudeY = input<number>(10);
  
  /** Include rotation in animation */
  rotate = input<boolean>(false);
  
  /** Rotation amplitude in degrees */
  rotateAmplitude = input<number>(2);
  
  /** Include scale pulse */
  pulse = input<boolean>(false);
  
  /** Scale pulse amplitude */
  pulseAmplitude = input<number>(0.02);
  
  /** Pause animation on hover */
  pauseOnHover = input<boolean>(false);

  private element!: HTMLElement;
  private animationName = '';
  private styleElement: HTMLStyleElement | null = null;

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    // Respect reduced motion preference
    if (this.animationService.prefersReducedMotion()) return;
    
    this.element = this.el.nativeElement;
    this.createAnimation();
    this.applyStyles();
  }

  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.removeAnimation();
  }

  /**
   * Create unique keyframe animation
   */
  private createAnimation(): void {
    // Generate unique animation name
    this.animationName = `floating-${Math.random().toString(36).substr(2, 9)}`;
    
    // Get animation parameters based on variant
    const params = this.getVariantParams();
    
    // Build keyframes
    const keyframes = this.buildKeyframes(params);
    
    // Create style element with keyframes
    this.styleElement = document.createElement('style');
    this.styleElement.textContent = `
      @keyframes ${this.animationName} {
        ${keyframes}
      }
    `;
    document.head.appendChild(this.styleElement);
  }

  /**
   * Get animation parameters based on variant
   */
  private getVariantParams(): {
    amplitudeX: number;
    amplitudeY: number;
    rotateAmp: number;
    pulseAmp: number;
  } {
    const currentVariant = this.variant();
    
    switch (currentVariant) {
      case 'gentle':
        return {
          amplitudeX: 0,
          amplitudeY: 8,
          rotateAmp: this.rotate() ? 1 : 0,
          pulseAmp: this.pulse() ? 0.01 : 0
        };
      case 'medium':
        return {
          amplitudeX: 3,
          amplitudeY: 12,
          rotateAmp: this.rotate() ? 2 : 0,
          pulseAmp: this.pulse() ? 0.02 : 0
        };
      case 'pronounced':
        return {
          amplitudeX: 5,
          amplitudeY: 20,
          rotateAmp: this.rotate() ? 3 : 0,
          pulseAmp: this.pulse() ? 0.03 : 0
        };
      case 'custom':
        return {
          amplitudeX: this.amplitudeX(),
          amplitudeY: this.amplitudeY(),
          rotateAmp: this.rotate() ? this.rotateAmplitude() : 0,
          pulseAmp: this.pulse() ? this.pulseAmplitude() : 0
        };
      default:
        return {
          amplitudeX: 0,
          amplitudeY: 8,
          rotateAmp: 0,
          pulseAmp: 0
        };
    }
  }

  /**
   * Build keyframe CSS string
   */
  private buildKeyframes(params: {
    amplitudeX: number;
    amplitudeY: number;
    rotateAmp: number;
    pulseAmp: number;
  }): string {
    const { amplitudeX, amplitudeY, rotateAmp, pulseAmp } = params;
    
    // Create smooth wave motion using multiple keyframes
    const frames = [
      { percent: 0, x: 0, y: 0, rotate: 0, scale: 1 },
      { percent: 25, x: amplitudeX, y: -amplitudeY * 0.5, rotate: rotateAmp * 0.5, scale: 1 + pulseAmp * 0.5 },
      { percent: 50, x: 0, y: -amplitudeY, rotate: 0, scale: 1 + pulseAmp },
      { percent: 75, x: -amplitudeX, y: -amplitudeY * 0.5, rotate: -rotateAmp * 0.5, scale: 1 + pulseAmp * 0.5 },
      { percent: 100, x: 0, y: 0, rotate: 0, scale: 1 }
    ];
    
    return frames.map(frame => {
      const transforms = [`translate3d(${frame.x}px, ${frame.y}px, 0)`];
      
      if (rotateAmp !== 0) {
        transforms.push(`rotate(${frame.rotate}deg)`);
      }
      if (pulseAmp !== 0) {
        transforms.push(`scale(${frame.scale})`);
      }
      
      return `${frame.percent}% { transform: ${transforms.join(' ')}; }`;
    }).join('\n        ');
  }

  /**
   * Apply animation styles to element
   */
  private applyStyles(): void {
    const duration = this.duration();
    const delay = this.delay();
    
    this.renderer.setStyle(this.element, 'will-change', 'transform');
    this.renderer.setStyle(
      this.element, 
      'animation', 
      `${this.animationName} ${duration}ms ease-in-out ${delay}ms infinite`
    );
    
    // Add pause on hover functionality
    if (this.pauseOnHover()) {
      this.renderer.setStyle(this.element, 'transition', 'animation-play-state 0.3s');
      
      this.element.addEventListener('mouseenter', () => {
        this.renderer.setStyle(this.element, 'animation-play-state', 'paused');
      });
      
      this.element.addEventListener('mouseleave', () => {
        this.renderer.setStyle(this.element, 'animation-play-state', 'running');
      });
    }
  }

  /**
   * Remove animation styles
   */
  private removeAnimation(): void {
    if (this.styleElement) {
      document.head.removeChild(this.styleElement);
      this.styleElement = null;
    }
    
    if (this.element) {
      this.renderer.removeStyle(this.element, 'animation');
      this.renderer.removeStyle(this.element, 'will-change');
    }
  }
}
