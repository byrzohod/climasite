import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID,
  Renderer2,
  NgZone
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

export type ParallaxMode = 'scroll' | 'mouse' | 'both';

/**
 * ParallaxDirective - Creates subtle parallax effects on elements
 * 
 * Supports scroll-based and mouse-move based parallax with
 * GPU-accelerated transforms and reduced motion preference.
 * 
 * Usage:
 * <div appParallax>Default scroll parallax</div>
 * <div appParallax [speed]="0.3" [direction]="'down'">Slower, moves down</div>
 * <div appParallax [mode]="'mouse'" [intensity]="20">Mouse-based parallax</div>
 * <div appParallax [mode]="'both'" [speed]="0.2" [intensity]="15">Combined</div>
 */
@Directive({
  selector: '[appParallax]',
  standalone: true
})
export class ParallaxDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  private readonly ngZone = inject(NgZone);

  /** Parallax speed factor (0-1, higher = more movement) */
  speed = input<number>(0.5);
  
  /** Direction of movement relative to scroll */
  direction = input<'up' | 'down'>('up');
  
  /** Parallax mode: scroll, mouse, or both */
  mode = input<ParallaxMode>('scroll');
  
  /** Mouse parallax intensity (pixels of max offset) */
  intensity = input<number>(30);
  
  /** Axis for scroll parallax */
  axis = input<'x' | 'y' | 'both'>('y');
  
  /** Smoothing factor for mouse movement (higher = smoother/slower) */
  smoothing = input<number>(0.1);
  
  /** Scale factor on scroll (1 = no scale) */
  scaleOnScroll = input<number>(1);
  
  /** Rotation on scroll in degrees */
  rotateOnScroll = input<number>(0);
  
  /** Whether to contain parallax within element bounds */
  containWithinBounds = input<boolean>(true);

  private element!: HTMLElement;
  private rafId: number | null = null;
  private scrollRafId: number | null = null;
  private isInitialized = false;
  
  // Current transform state
  private currentX = 0;
  private currentY = 0;
  private targetX = 0;
  private targetY = 0;
  
  // Bound event handlers
  private boundOnScroll: (() => void) | null = null;
  private boundOnMouseMove: ((e: MouseEvent) => void) | null = null;
  private boundOnMouseLeave: (() => void) | null = null;

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    // Respect reduced motion preference
    if (this.animationService.prefersReducedMotion()) return;
    
    this.element = this.el.nativeElement;
    this.setupStyles();
    this.bindEvents();
    this.isInitialized = true;
    
    // Initial update
    if (this.mode() === 'scroll' || this.mode() === 'both') {
      this.updateScrollParallax();
    }
  }

  ngOnDestroy(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.unbindEvents();
    
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
    }
    if (this.scrollRafId !== null) {
      cancelAnimationFrame(this.scrollRafId);
    }
  }

  /**
   * Setup initial styles for GPU acceleration
   */
  private setupStyles(): void {
    this.renderer.setStyle(this.element, 'will-change', 'transform');
    this.renderer.setStyle(this.element, 'backface-visibility', 'hidden');
    this.renderer.setStyle(this.element, 'perspective', '1000px');
  }

  /**
   * Bind appropriate event listeners based on mode
   */
  private bindEvents(): void {
    this.ngZone.runOutsideAngular(() => {
      const currentMode = this.mode();
      
      if (currentMode === 'scroll' || currentMode === 'both') {
        this.boundOnScroll = this.onScroll.bind(this);
        window.addEventListener('scroll', this.boundOnScroll, { passive: true });
      }
      
      if (currentMode === 'mouse' || currentMode === 'both') {
        this.boundOnMouseMove = this.onMouseMove.bind(this);
        this.boundOnMouseLeave = this.onMouseLeave.bind(this);
        
        // Listen on window for smoother tracking
        window.addEventListener('mousemove', this.boundOnMouseMove, { passive: true });
        this.element.addEventListener('mouseleave', this.boundOnMouseLeave, { passive: true });
        
        // Start the animation loop for smooth interpolation
        this.startAnimationLoop();
      }
    });
  }

  /**
   * Unbind event listeners
   */
  private unbindEvents(): void {
    if (this.boundOnScroll) {
      window.removeEventListener('scroll', this.boundOnScroll);
    }
    if (this.boundOnMouseMove) {
      window.removeEventListener('mousemove', this.boundOnMouseMove);
    }
    if (this.boundOnMouseLeave) {
      this.element.removeEventListener('mouseleave', this.boundOnMouseLeave);
    }
  }

  /**
   * Handle scroll events with RAF throttling
   */
  private onScroll(): void {
    if (this.scrollRafId !== null) return;
    
    this.scrollRafId = requestAnimationFrame(() => {
      this.updateScrollParallax();
      this.scrollRafId = null;
    });
  }

  /**
   * Handle mouse move events
   */
  private onMouseMove(event: MouseEvent): void {
    const rect = this.element.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    
    // Check if element is visible
    if (rect.bottom < 0 || rect.top > viewportHeight) return;
    
    // Calculate mouse position relative to viewport center
    const mouseX = (event.clientX / viewportWidth - 0.5) * 2; // -1 to 1
    const mouseY = (event.clientY / viewportHeight - 0.5) * 2; // -1 to 1
    
    // Set target position
    this.targetX = mouseX * this.intensity();
    this.targetY = mouseY * this.intensity();
  }

  /**
   * Handle mouse leave - smoothly return to center
   */
  private onMouseLeave(): void {
    this.targetX = 0;
    this.targetY = 0;
  }

  /**
   * Animation loop for smooth mouse parallax interpolation
   */
  private startAnimationLoop(): void {
    const animate = () => {
      // Smooth interpolation towards target
      const smoothingFactor = this.smoothing();
      this.currentX += (this.targetX - this.currentX) * smoothingFactor;
      this.currentY += (this.targetY - this.currentY) * smoothingFactor;
      
      // Apply combined transform if using both modes
      if (this.mode() === 'both') {
        this.applyMouseTransform();
      } else {
        this.applyTransform(this.currentX, this.currentY);
      }
      
      this.rafId = requestAnimationFrame(animate);
    };
    
    this.rafId = requestAnimationFrame(animate);
  }

  /**
   * Update parallax based on scroll position
   */
  private updateScrollParallax(): void {
    const rect = this.element.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    
    // Check if element is in viewport
    if (rect.bottom < 0 || rect.top > windowHeight) return;
    
    // Calculate how far through the viewport the element has scrolled
    // 0 = element just entering from bottom, 1 = element leaving at top
    const scrollProgress = (windowHeight - rect.top) / (windowHeight + rect.height);
    
    // Calculate offset based on speed and direction
    const speedFactor = this.speed();
    const directionMultiplier = this.direction() === 'up' ? -1 : 1;
    
    // Calculate parallax offset
    const maxOffset = windowHeight * speedFactor;
    const offset = (scrollProgress - 0.5) * maxOffset * directionMultiplier;
    
    // Calculate scale if enabled
    const scale = this.scaleOnScroll() !== 1 
      ? 1 + (scrollProgress - 0.5) * (this.scaleOnScroll() - 1)
      : 1;
    
    // Calculate rotation if enabled
    const rotation = this.rotateOnScroll() !== 0
      ? (scrollProgress - 0.5) * this.rotateOnScroll()
      : 0;
    
    // Build transform based on axis
    let translateX = 0;
    let translateY = 0;
    
    const currentAxis = this.axis();
    if (currentAxis === 'y' || currentAxis === 'both') {
      translateY = offset;
    }
    if (currentAxis === 'x' || currentAxis === 'both') {
      translateX = offset;
    }
    
    // Contain within bounds if enabled
    if (this.containWithinBounds()) {
      translateX = Math.max(-maxOffset, Math.min(maxOffset, translateX));
      translateY = Math.max(-maxOffset, Math.min(maxOffset, translateY));
    }
    
    // Apply transform (only scroll component)
    if (this.mode() === 'scroll') {
      this.applyScrollTransform(translateX, translateY, scale, rotation);
    }
  }

  /**
   * Apply mouse-based transform
   */
  private applyMouseTransform(): void {
    // Combine with scroll if in 'both' mode
    const rect = this.element.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    const scrollProgress = (windowHeight - rect.top) / (windowHeight + rect.height);
    
    const speedFactor = this.speed();
    const directionMultiplier = this.direction() === 'up' ? -1 : 1;
    const maxOffset = windowHeight * speedFactor;
    const scrollOffset = (scrollProgress - 0.5) * maxOffset * directionMultiplier;
    
    let translateY = scrollOffset;
    const currentAxis = this.axis();
    if (currentAxis === 'x') {
      translateY = 0;
    }
    
    // Combine scroll and mouse offsets
    const totalX = this.currentX;
    const totalY = translateY + this.currentY;
    
    const transform = `translate3d(${totalX.toFixed(2)}px, ${totalY.toFixed(2)}px, 0)`;
    this.renderer.setStyle(this.element, 'transform', transform);
  }

  /**
   * Apply scroll-based transform
   */
  private applyScrollTransform(x: number, y: number, scale: number, rotation: number): void {
    let transform = `translate3d(${x.toFixed(2)}px, ${y.toFixed(2)}px, 0)`;
    
    if (scale !== 1) {
      transform += ` scale(${scale.toFixed(3)})`;
    }
    if (rotation !== 0) {
      transform += ` rotate(${rotation.toFixed(2)}deg)`;
    }
    
    this.renderer.setStyle(this.element, 'transform', transform);
  }

  /**
   * Apply basic transform
   */
  private applyTransform(x: number, y: number): void {
    const transform = `translate3d(${x.toFixed(2)}px, ${y.toFixed(2)}px, 0)`;
    this.renderer.setStyle(this.element, 'transform', transform);
  }
}
