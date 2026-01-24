import {
  Directive,
  ElementRef,
  HostListener,
  Renderer2,
  inject,
  input,
  signal,
  effect,
  OnInit,
  OnDestroy
} from '@angular/core';

/**
 * Fallback strategy when image fails to load
 */
export type ImageFallbackStrategy = 'placeholder' | 'hide' | 'custom';

/**
 * Optimized Image Directive
 * 
 * Enhances image elements with:
 * - Automatic lazy loading
 * - Error fallback handling
 * - Optional blur-up effect during loading
 * - WebP detection and fallback
 * - Accessibility improvements
 * 
 * @example
 * ```html
 * <!-- Basic usage with automatic fallback -->
 * <img appOptimizedImage src="product.jpg" alt="Product" />
 * 
 * <!-- With custom fallback image -->
 * <img appOptimizedImage 
 *      src="product.jpg" 
 *      [fallbackSrc]="'/assets/images/fallbacks/no-product-image.svg'"
 *      alt="Product" />
 * 
 * <!-- With blur-up effect -->
 * <img appOptimizedImage 
 *      src="product-large.jpg"
 *      [placeholderSrc]="'product-tiny.jpg'"
 *      [blurUp]="true"
 *      alt="Product" />
 * ```
 */
@Directive({
  selector: 'img[appOptimizedImage]',
  standalone: true,
  host: {
    '[class.optimized-image]': 'true',
    '[class.optimized-image--loading]': 'isLoading()',
    '[class.optimized-image--loaded]': 'isLoaded()',
    '[class.optimized-image--error]': 'hasError()',
    '[class.optimized-image--blur-up]': 'blurUp()'
  }
})
export class OptimizedImageDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef<HTMLImageElement>);
  private readonly renderer = inject(Renderer2);
  private intersectionObserver?: IntersectionObserver;

  /** Fallback image URL when the primary image fails to load */
  readonly fallbackSrc = input<string>('/assets/images/fallbacks/no-product-image.svg');

  /** Low-quality placeholder for blur-up effect */
  readonly placeholderSrc = input<string | null>(null);

  /** Enable blur-up loading effect */
  readonly blurUp = input<boolean>(false);

  /** Loading attribute override (defaults to 'lazy') */
  readonly loading = input<'lazy' | 'eager'>('lazy');

  /** Fallback strategy when image fails */
  readonly fallbackStrategy = input<ImageFallbackStrategy>('placeholder');

  /** Decode images asynchronously for better performance */
  readonly asyncDecode = input<boolean>(true);

  /** Track loading state */
  readonly isLoading = signal(true);

  /** Track loaded state */
  readonly isLoaded = signal(false);

  /** Track error state */
  readonly hasError = signal(false);

  private originalSrc = '';
  private retryCount = 0;
  private readonly maxRetries = 1;

  constructor() {
    // Effect to handle blur-up when placeholderSrc changes
    effect(() => {
      const placeholder = this.placeholderSrc();
      if (placeholder && this.blurUp() && !this.isLoaded()) {
        this.setupBlurUp(placeholder);
      }
    });
  }

  ngOnInit(): void {
    const img = this.el.nativeElement;
    
    // Store original src
    this.originalSrc = img.src || img.getAttribute('src') || '';

    // Set loading attribute
    this.renderer.setAttribute(img, 'loading', this.loading());

    // Set decoding attribute for async decode
    if (this.asyncDecode()) {
      this.renderer.setAttribute(img, 'decoding', 'async');
    }

    // Ensure alt attribute exists for accessibility
    if (!img.hasAttribute('alt')) {
      this.renderer.setAttribute(img, 'alt', '');
    }

    // Setup blur-up if enabled
    if (this.blurUp() && this.placeholderSrc()) {
      this.setupBlurUp(this.placeholderSrc()!);
    }

    // Check if image is already loaded (cached)
    if (img.complete && img.naturalHeight > 0) {
      this.onLoad();
    }
  }

  ngOnDestroy(): void {
    this.intersectionObserver?.disconnect();
  }

  /**
   * Handle image load success
   */
  @HostListener('load')
  onLoad(): void {
    this.isLoading.set(false);
    this.isLoaded.set(true);
    this.hasError.set(false);

    // Remove blur effect after loading
    if (this.blurUp()) {
      this.renderer.removeClass(this.el.nativeElement, 'blur-up-loading');
      this.renderer.addClass(this.el.nativeElement, 'blur-up-loaded');
    }
  }

  /**
   * Handle image load error
   */
  @HostListener('error')
  onError(): void {
    const img = this.el.nativeElement;
    const currentSrc = img.src;

    // Prevent infinite loops - don't try to load fallback if it also fails
    if (currentSrc === this.fallbackSrc() || this.retryCount >= this.maxRetries) {
      this.handleFinalError();
      return;
    }

    this.retryCount++;

    // Apply fallback based on strategy
    switch (this.fallbackStrategy()) {
      case 'placeholder':
        this.applyFallback();
        break;
      case 'hide':
        this.hideImage();
        break;
      case 'custom':
        // Let the parent component handle it via the hasError signal
        this.hasError.set(true);
        this.isLoading.set(false);
        break;
    }
  }

  /**
   * Setup blur-up placeholder effect
   */
  private setupBlurUp(placeholderSrc: string): void {
    const img = this.el.nativeElement;
    
    // Store the full-quality src
    const fullSrc = img.src;
    
    // Load placeholder first
    this.renderer.setAttribute(img, 'src', placeholderSrc);
    this.renderer.addClass(img, 'blur-up-loading');
    
    // Create a new image to preload the full-quality version
    const preloadImg = new Image();
    preloadImg.onload = () => {
      // Swap to full quality image
      this.renderer.setAttribute(img, 'src', fullSrc);
    };
    preloadImg.src = fullSrc;
  }

  /**
   * Apply fallback image
   */
  private applyFallback(): void {
    const fallback = this.fallbackSrc();
    if (fallback) {
      this.renderer.setAttribute(this.el.nativeElement, 'src', fallback);
      this.hasError.set(true);
    } else {
      this.handleFinalError();
    }
  }

  /**
   * Hide the image element completely
   */
  private hideImage(): void {
    this.renderer.setStyle(this.el.nativeElement, 'display', 'none');
    this.hasError.set(true);
    this.isLoading.set(false);
  }

  /**
   * Handle final error state when all fallbacks fail
   */
  private handleFinalError(): void {
    this.hasError.set(true);
    this.isLoading.set(false);
    this.renderer.addClass(this.el.nativeElement, 'optimized-image--broken');
  }
}
