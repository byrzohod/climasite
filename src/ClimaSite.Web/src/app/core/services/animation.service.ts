import { Injectable, PLATFORM_ID, inject, signal, computed } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface ScrollState {
  scrollY: number;
  scrollProgress: number;
  direction: 'up' | 'down';
  velocity: number;
}

/**
 * AnimationService - Centralized animation control and utilities
 * 
 * Provides:
 * - Scroll state tracking (position, direction, velocity)
 * - Reduced motion preference detection
 * - Stagger delay calculations
 * - Viewport intersection helpers
 */
@Injectable({ providedIn: 'root' })
export class AnimationService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  
  // Scroll state
  private _scrollY = signal(0);
  private _scrollProgress = signal(0);
  private _scrollDirection = signal<'up' | 'down'>('down');
  private _scrollVelocity = signal(0);
  
  // Last scroll position for direction/velocity calculation
  private lastScrollY = 0;
  private lastScrollTime = 0;
  private rafId: number | null = null;
  
  // Reduced motion preference
  private _prefersReducedMotion = signal(false);
  
  // Public readonly signals
  readonly scrollY = this._scrollY.asReadonly();
  readonly scrollProgress = this._scrollProgress.asReadonly();
  readonly scrollDirection = this._scrollDirection.asReadonly();
  readonly scrollVelocity = this._scrollVelocity.asReadonly();
  readonly prefersReducedMotion = this._prefersReducedMotion.asReadonly();
  
  // Computed scroll state
  readonly scrollState = computed<ScrollState>(() => ({
    scrollY: this._scrollY(),
    scrollProgress: this._scrollProgress(),
    direction: this._scrollDirection(),
    velocity: this._scrollVelocity()
  }));
  
  // Check if user is scrolling
  readonly isScrolling = computed(() => Math.abs(this._scrollVelocity()) > 0.1);
  
  constructor() {
    if (this.isBrowser) {
      this.initScrollTracking();
      this.initReducedMotionDetection();
    }
  }
  
  /**
   * Initialize scroll position tracking
   */
  private initScrollTracking(): void {
    // Initial values
    this.lastScrollY = window.scrollY;
    this.lastScrollTime = performance.now();
    this.updateScrollState();
    
    // Throttled scroll handler
    window.addEventListener('scroll', this.onScroll, { passive: true });
    
    // Update on resize (document height may change)
    window.addEventListener('resize', this.updateScrollProgress, { passive: true });
  }
  
  /**
   * Initialize reduced motion preference detection
   */
  private initReducedMotionDetection(): void {
    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    this._prefersReducedMotion.set(mediaQuery.matches);
    
    mediaQuery.addEventListener('change', (e) => {
      this._prefersReducedMotion.set(e.matches);
    });
  }
  
  /**
   * Scroll event handler with RAF throttling
   */
  private onScroll = (): void => {
    if (this.rafId !== null) return;
    
    this.rafId = requestAnimationFrame(() => {
      this.updateScrollState();
      this.rafId = null;
    });
  };
  
  /**
   * Update all scroll state values
   */
  private updateScrollState(): void {
    const currentScrollY = window.scrollY;
    const currentTime = performance.now();
    const timeDelta = currentTime - this.lastScrollTime;
    
    // Calculate velocity (pixels per millisecond)
    const velocity = timeDelta > 0 
      ? (currentScrollY - this.lastScrollY) / timeDelta 
      : 0;
    
    // Update direction
    if (currentScrollY > this.lastScrollY + 1) {
      this._scrollDirection.set('down');
    } else if (currentScrollY < this.lastScrollY - 1) {
      this._scrollDirection.set('up');
    }
    
    // Update values
    this._scrollY.set(currentScrollY);
    this._scrollVelocity.set(velocity);
    this.updateScrollProgress();
    
    // Store for next calculation
    this.lastScrollY = currentScrollY;
    this.lastScrollTime = currentTime;
  }
  
  /**
   * Update scroll progress (0-1)
   */
  private updateScrollProgress = (): void => {
    const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
    const progress = scrollHeight > 0 ? window.scrollY / scrollHeight : 0;
    this._scrollProgress.set(Math.min(1, Math.max(0, progress)));
  };
  
  /**
   * Calculate stagger delay for sequential animations
   * @param index - Item index in the sequence
   * @param baseDelay - Base delay between items (default: 50ms)
   * @param maxDelay - Maximum total delay (default: 500ms)
   * @returns Delay in milliseconds
   */
  getStaggerDelay(index: number, baseDelay = 50, maxDelay = 500): number {
    if (this._prefersReducedMotion()) return 0;
    return Math.min(index * baseDelay, maxDelay);
  }
  
  /**
   * Get animation duration based on reduced motion preference
   * @param normalDuration - Normal duration in ms
   * @param reducedDuration - Duration when reduced motion is preferred (default: 0)
   */
  getDuration(normalDuration: number, reducedDuration = 0): number {
    return this._prefersReducedMotion() ? reducedDuration : normalDuration;
  }
  
  /**
   * Check if an element is in the viewport
   * @param element - Element to check
   * @param threshold - Visibility threshold (0-1)
   */
  isInViewport(element: Element, threshold = 0): boolean {
    if (!this.isBrowser) return false;
    
    const rect = element.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    const windowWidth = window.innerWidth;
    
    const verticalVisible = 
      (rect.top <= windowHeight * (1 - threshold)) && 
      (rect.bottom >= windowHeight * threshold);
    
    const horizontalVisible = 
      (rect.left <= windowWidth * (1 - threshold)) && 
      (rect.right >= windowWidth * threshold);
    
    return verticalVisible && horizontalVisible;
  }
  
  /**
   * Get element's visibility percentage in viewport
   * @param element - Element to check
   * @returns Visibility percentage (0-1)
   */
  getVisibilityPercentage(element: Element): number {
    if (!this.isBrowser) return 0;
    
    const rect = element.getBoundingClientRect();
    const windowHeight = window.innerHeight;
    
    if (rect.bottom < 0 || rect.top > windowHeight) return 0;
    
    const visibleHeight = Math.min(rect.bottom, windowHeight) - Math.max(rect.top, 0);
    return Math.max(0, Math.min(1, visibleHeight / rect.height));
  }
  
  /**
   * Smooth scroll to an element
   * @param element - Target element or selector
   * @param offset - Offset from top (default: 0)
   */
  scrollToElement(element: Element | string, offset = 0): void {
    if (!this.isBrowser) return;
    
    const targetElement = typeof element === 'string' 
      ? document.querySelector(element) 
      : element;
    
    if (!targetElement) return;
    
    const targetPosition = targetElement.getBoundingClientRect().top + window.scrollY - offset;
    
    window.scrollTo({
      top: targetPosition,
      behavior: this._prefersReducedMotion() ? 'auto' : 'smooth'
    });
  }
  
  /**
   * Scroll to top of page
   */
  scrollToTop(): void {
    if (!this.isBrowser) return;
    
    window.scrollTo({
      top: 0,
      behavior: this._prefersReducedMotion() ? 'auto' : 'smooth'
    });
  }
  
  /**
   * Create an intersection observer with common defaults
   * @param callback - Callback function
   * @param options - Observer options
   */
  createIntersectionObserver(
    callback: IntersectionObserverCallback,
    options: IntersectionObserverInit = {}
  ): IntersectionObserver | null {
    if (!this.isBrowser) return null;
    
    const defaultOptions: IntersectionObserverInit = {
      threshold: 0.1,
      rootMargin: '0px 0px -50px 0px',
      ...options
    };
    
    return new IntersectionObserver(callback, defaultOptions);
  }
  
  /**
   * Cleanup method (call in component ngOnDestroy if needed)
   */
  destroy(): void {
    if (this.isBrowser) {
      window.removeEventListener('scroll', this.onScroll);
      window.removeEventListener('resize', this.updateScrollProgress);
      
      if (this.rafId !== null) {
        cancelAnimationFrame(this.rafId);
      }
    }
  }
}
