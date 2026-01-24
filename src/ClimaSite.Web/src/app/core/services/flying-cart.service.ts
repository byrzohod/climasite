import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from './animation.service';
import { CartService } from './cart.service';

export interface FlyingCartOptions {
  imageUrl: string;
  sourceElement: HTMLElement;
  imageSize?: number;
  /** If true, auto-opens the mini-cart after animation completes. Default: true */
  openMiniCart?: boolean;
}

/**
 * FlyingCartService - Animates product images flying to the cart icon
 * 
 * Features:
 * - Clones the product image
 * - Animates from product position to cart icon with arc trajectory
 * - Scales down and fades out during flight
 * - Triggers bump animation on cart icon
 * - Respects reduced motion preference
 */
@Injectable({ providedIn: 'root' })
export class FlyingCartService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  private readonly cartService = inject(CartService);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  
  private readonly ANIMATION_DURATION = 600; // ms
  private readonly IMAGE_SIZE = 60; // px
  private readonly MINI_CART_OPEN_DELAY = 400; // ms after animation completes
  
  /**
   * Trigger the flying cart animation
   * @param options - Animation options including image URL and source element
   */
  fly(options: FlyingCartOptions): void {
    if (!this.isBrowser) return;
    
    const shouldOpenMiniCart = options.openMiniCart !== false; // Default to true
    
    // Skip animation if reduced motion is preferred
    if (this.animationService.prefersReducedMotion()) {
      this.triggerCartBump();
      if (shouldOpenMiniCart) {
        this.openMiniCartWithDelay();
      }
      return;
    }
    
    const cartIcon = document.querySelector('[data-testid="cart-icon"]') as HTMLElement;
    if (!cartIcon) {
      console.warn('FlyingCartService: Cart icon not found');
      return;
    }
    
    // Get positions
    const sourceRect = options.sourceElement.getBoundingClientRect();
    const cartRect = cartIcon.getBoundingClientRect();
    
    // Create flying element
    const flyingEl = this.createFlyingElement(options.imageUrl, options.imageSize || this.IMAGE_SIZE);
    
    // Set initial position (center of source element)
    const startX = sourceRect.left + sourceRect.width / 2 - (options.imageSize || this.IMAGE_SIZE) / 2;
    const startY = sourceRect.top + sourceRect.height / 2 - (options.imageSize || this.IMAGE_SIZE) / 2;
    
    flyingEl.style.left = `${startX}px`;
    flyingEl.style.top = `${startY}px`;
    
    // Add to DOM
    document.body.appendChild(flyingEl);
    
    // Calculate end position (center of cart icon)
    const endX = cartRect.left + cartRect.width / 2 - (options.imageSize || this.IMAGE_SIZE) / 2;
    const endY = cartRect.top + cartRect.height / 2 - (options.imageSize || this.IMAGE_SIZE) / 2;
    
    // Calculate arc control point (above the midpoint)
    const midX = (startX + endX) / 2;
    const midY = Math.min(startY, endY) - 100; // Arc height
    
    // Animate using requestAnimationFrame for smooth performance
    this.animateArc(flyingEl, startX, startY, midX, midY, endX, endY, () => {
      // Cleanup
      flyingEl.remove();
      
      // Trigger cart bump animation
      this.triggerCartBump();
      
      // Auto-open mini-cart after a short delay
      if (shouldOpenMiniCart) {
        this.openMiniCartWithDelay();
      }
    });
  }
  
  /**
   * Create the flying image element
   */
  private createFlyingElement(imageUrl: string, size: number): HTMLElement {
    const el = document.createElement('div');
    el.className = 'flying-cart-item';
    el.setAttribute('aria-hidden', 'true');
    
    el.style.cssText = `
      position: fixed;
      width: ${size}px;
      height: ${size}px;
      border-radius: 8px;
      overflow: hidden;
      z-index: 10000;
      pointer-events: none;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      will-change: transform, opacity;
      background-color: var(--color-bg-card, #fff);
    `;
    
    const img = document.createElement('img');
    img.src = imageUrl;
    img.alt = '';
    img.style.cssText = `
      width: 100%;
      height: 100%;
      object-fit: contain;
    `;
    
    el.appendChild(img);
    return el;
  }
  
  /**
   * Animate element along a quadratic bezier curve (arc trajectory)
   */
  private animateArc(
    element: HTMLElement,
    startX: number,
    startY: number,
    controlX: number,
    controlY: number,
    endX: number,
    endY: number,
    onComplete: () => void
  ): void {
    const startTime = performance.now();
    const duration = this.ANIMATION_DURATION;
    
    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      
      // Ease out cubic for smooth deceleration
      const eased = 1 - Math.pow(1 - progress, 3);
      
      // Quadratic bezier curve calculation
      const t = eased;
      const x = Math.pow(1 - t, 2) * startX + 2 * (1 - t) * t * controlX + Math.pow(t, 2) * endX;
      const y = Math.pow(1 - t, 2) * startY + 2 * (1 - t) * t * controlY + Math.pow(t, 2) * endY;
      
      // Scale down (1 -> 0.3)
      const scale = 1 - (eased * 0.7);
      
      // Fade out in last 30%
      const opacity = progress > 0.7 ? 1 - ((progress - 0.7) / 0.3) : 1;
      
      // Apply transform (GPU accelerated)
      element.style.transform = `translate(${x - parseFloat(element.style.left)}px, ${y - parseFloat(element.style.top)}px) scale(${scale})`;
      element.style.opacity = String(opacity);
      
      if (progress < 1) {
        requestAnimationFrame(animate);
      } else {
        onComplete();
      }
    };
    
    requestAnimationFrame(animate);
  }
  
  /**
   * Trigger the bump animation on the cart icon
   */
  private triggerCartBump(): void {
    const cartIcon = document.querySelector('[data-testid="cart-icon"]') as HTMLElement;
    if (!cartIcon) return;
    
    // Add bump class
    cartIcon.classList.add('cart-bump');
    
    // Remove class after animation
    setTimeout(() => {
      cartIcon.classList.remove('cart-bump');
    }, 300);
  }
  
  /**
   * Open the mini-cart drawer after a delay
   * This gives the user a moment to see the cart bump animation
   */
  private openMiniCartWithDelay(): void {
    setTimeout(() => {
      this.cartService.openMiniCart();
    }, this.MINI_CART_OPEN_DELAY);
  }
}
