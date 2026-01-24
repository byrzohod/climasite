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

/**
 * MagneticHoverDirective - Creates a magnetic hover effect
 * 
 * The element subtly follows the cursor when hovered, creating
 * an engaging and premium interaction feel.
 * 
 * Usage:
 * <button appMagneticHover>Hover me</button>
 * <button appMagneticHover [strength]="0.3" [scale]="1.05">Strong effect</button>
 */
@Directive({
  selector: '[appMagneticHover]',
  standalone: true
})
export class MagneticHoverDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Strength of the magnetic pull (0-1) */
  strength = input<number>(0.2);
  
  /** Scale factor on hover (1 = no scale) */
  scale = input<number>(1.02);
  
  /** Enable subtle rotation effect */
  rotate = input<boolean>(false);
  
  /** Transition duration in ms */
  duration = input<number>(150);
  
  private element!: HTMLElement;
  private boundingRect: DOMRect | null = null;
  private isHovered = false;
  private rafId: number | null = null;
  
  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.element = this.el.nativeElement;
    this.setupStyles();
    this.bindEvents();
  }
  
  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.unbindEvents();
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
  }
  
  private setupStyles(): void {
    // Set initial transition
    this.renderer.setStyle(
      this.element, 
      'transition', 
      `transform ${this.duration()}ms cubic-bezier(0.34, 1.56, 0.64, 1)`
    );
    
    // Ensure transform origin is centered
    this.renderer.setStyle(this.element, 'transform-origin', 'center center');
    
    // Add will-change for performance
    this.renderer.setStyle(this.element, 'will-change', 'transform');
  }
  
  private bindEvents(): void {
    this.element.addEventListener('mouseenter', this.onMouseEnter);
    this.element.addEventListener('mousemove', this.onMouseMove);
    this.element.addEventListener('mouseleave', this.onMouseLeave);
  }
  
  private unbindEvents(): void {
    this.element.removeEventListener('mouseenter', this.onMouseEnter);
    this.element.removeEventListener('mousemove', this.onMouseMove);
    this.element.removeEventListener('mouseleave', this.onMouseLeave);
  }
  
  private onMouseEnter = (): void => {
    // Skip animation if reduced motion is preferred
    if (this.animationService.prefersReducedMotion()) return;
    
    this.isHovered = true;
    this.boundingRect = this.element.getBoundingClientRect();
    
    // Faster transition on enter
    this.renderer.setStyle(
      this.element, 
      'transition', 
      `transform ${this.duration() * 0.5}ms cubic-bezier(0.34, 1.56, 0.64, 1)`
    );
  };
  
  private onMouseMove = (event: MouseEvent): void => {
    if (!this.isHovered || !this.boundingRect) return;
    if (this.animationService.prefersReducedMotion()) return;
    
    // Cancel previous animation frame
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
    
    this.rafId = requestAnimationFrame(() => {
      this.updatePosition(event);
    });
  };
  
  private onMouseLeave = (): void => {
    this.isHovered = false;
    this.boundingRect = null;
    
    // Slower transition on leave for smooth return
    this.renderer.setStyle(
      this.element, 
      'transition', 
      `transform ${this.duration()}ms cubic-bezier(0.34, 1.56, 0.64, 1)`
    );
    
    // Reset transform
    this.renderer.setStyle(this.element, 'transform', 'translate(0, 0) scale(1) rotate(0deg)');
  };
  
  private updatePosition(event: MouseEvent): void {
    if (!this.boundingRect) return;
    
    const rect = this.boundingRect;
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;
    
    // Calculate distance from center
    const deltaX = event.clientX - centerX;
    const deltaY = event.clientY - centerY;
    
    // Apply strength modifier
    const moveX = deltaX * this.strength();
    const moveY = deltaY * this.strength();
    
    // Calculate rotation (subtle)
    let rotateAmount = 0;
    if (this.rotate()) {
      rotateAmount = (deltaX / rect.width) * 5; // Max 5 degrees
    }
    
    // Apply transform
    const transform = `translate(${moveX}px, ${moveY}px) scale(${this.scale()}) rotate(${rotateAmount}deg)`;
    this.renderer.setStyle(this.element, 'transform', transform);
  }
}
