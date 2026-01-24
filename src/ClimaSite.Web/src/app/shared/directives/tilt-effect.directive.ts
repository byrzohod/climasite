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
 * TiltEffectDirective - Creates a 3D tilt effect on hover
 * 
 * The element tilts toward the cursor position, creating
 * a dynamic 3D perspective effect.
 * 
 * Usage:
 * <div appTiltEffect>Tilt me</div>
 * <div appTiltEffect [maxTilt]="20" [perspective]="1000" [glare]="true">With glare</div>
 */
@Directive({
  selector: '[appTiltEffect]',
  standalone: true
})
export class TiltEffectDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Maximum tilt angle in degrees */
  maxTilt = input<number>(15);
  
  /** Perspective value in px */
  perspective = input<number>(1000);
  
  /** Scale on hover */
  scale = input<number>(1.02);
  
  /** Enable glare effect */
  glare = input<boolean>(false);
  
  /** Glare max opacity */
  glareOpacity = input<number>(0.3);
  
  /** Transition speed in ms */
  speed = input<number>(300);
  
  /** Reset on leave */
  reset = input<boolean>(true);
  
  private element!: HTMLElement;
  private glareElement: HTMLElement | null = null;
  private isHovered = false;
  private rafId: number | null = null;
  
  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.element = this.el.nativeElement;
    this.setupStyles();
    this.setupGlare();
    this.bindEvents();
  }
  
  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.unbindEvents();
    this.removeGlare();
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
  }
  
  private setupStyles(): void {
    // Add perspective to parent or create wrapper
    this.renderer.setStyle(this.element, 'transform-style', 'preserve-3d');
    this.renderer.setStyle(
      this.element, 
      'transition', 
      `transform ${this.speed()}ms cubic-bezier(0.03, 0.98, 0.52, 0.99)`
    );
    this.renderer.setStyle(this.element, 'will-change', 'transform');
    
    // Ensure relative positioning for glare
    const position = getComputedStyle(this.element).position;
    if (position === 'static') {
      this.renderer.setStyle(this.element, 'position', 'relative');
    }
  }
  
  private setupGlare(): void {
    if (!this.glare()) return;
    
    // Create glare overlay
    this.glareElement = this.renderer.createElement('div');
    this.renderer.setStyle(this.glareElement, 'position', 'absolute');
    this.renderer.setStyle(this.glareElement, 'top', '0');
    this.renderer.setStyle(this.glareElement, 'left', '0');
    this.renderer.setStyle(this.glareElement, 'right', '0');
    this.renderer.setStyle(this.glareElement, 'bottom', '0');
    this.renderer.setStyle(this.glareElement, 'pointer-events', 'none');
    this.renderer.setStyle(this.glareElement, 'border-radius', 'inherit');
    this.renderer.setStyle(this.glareElement, 'overflow', 'hidden');
    this.renderer.setStyle(this.glareElement, 'opacity', '0');
    this.renderer.setStyle(
      this.glareElement, 
      'transition', 
      `opacity ${this.speed()}ms ease`
    );
    this.renderer.setStyle(
      this.glareElement,
      'background',
      'linear-gradient(135deg, rgba(255,255,255,0.5) 0%, rgba(255,255,255,0) 60%)'
    );
    this.renderer.setStyle(this.glareElement, 'transform', 'translateZ(1px)');
    
    this.renderer.appendChild(this.element, this.glareElement);
  }
  
  private removeGlare(): void {
    if (this.glareElement) {
      this.renderer.removeChild(this.element, this.glareElement);
      this.glareElement = null;
    }
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
    if (this.animationService.prefersReducedMotion()) return;
    
    this.isHovered = true;
    
    // Show glare
    if (this.glareElement) {
      this.renderer.setStyle(this.glareElement, 'opacity', String(this.glareOpacity()));
    }
  };
  
  private onMouseMove = (event: MouseEvent): void => {
    if (!this.isHovered) return;
    if (this.animationService.prefersReducedMotion()) return;
    
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
    
    this.rafId = requestAnimationFrame(() => {
      this.updateTilt(event);
    });
  };
  
  private onMouseLeave = (): void => {
    this.isHovered = false;
    
    // Hide glare
    if (this.glareElement) {
      this.renderer.setStyle(this.glareElement, 'opacity', '0');
    }
    
    if (this.reset()) {
      this.resetTilt();
    }
  };
  
  private updateTilt(event: MouseEvent): void {
    const rect = this.element.getBoundingClientRect();
    
    // Calculate position relative to center (0 = center, -1 to 1 = edges)
    const x = (event.clientX - rect.left) / rect.width;
    const y = (event.clientY - rect.top) / rect.height;
    
    // Calculate tilt angles (inverted for natural feel)
    const tiltX = (0.5 - y) * this.maxTilt() * 2; // Vertical tilt
    const tiltY = (x - 0.5) * this.maxTilt() * 2; // Horizontal tilt
    
    // Apply transform
    const transform = `
      perspective(${this.perspective()}px) 
      rotateX(${tiltX}deg) 
      rotateY(${tiltY}deg) 
      scale(${this.scale()})
    `;
    this.renderer.setStyle(this.element, 'transform', transform);
    
    // Update glare position
    if (this.glareElement) {
      const glareX = x * 100;
      const glareY = y * 100;
      this.renderer.setStyle(
        this.glareElement,
        'background',
        `radial-gradient(circle at ${glareX}% ${glareY}%, rgba(255,255,255,0.5) 0%, rgba(255,255,255,0) 60%)`
      );
    }
  }
  
  private resetTilt(): void {
    const transform = `
      perspective(${this.perspective()}px) 
      rotateX(0deg) 
      rotateY(0deg) 
      scale(1)
    `;
    this.renderer.setStyle(this.element, 'transform', transform);
  }
}
