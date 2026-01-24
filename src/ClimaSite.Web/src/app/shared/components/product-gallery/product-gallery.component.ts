import { Component, ElementRef, HostListener, ViewChild, computed, effect, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  sortOrder: number;
}

@Component({
  selector: 'app-product-gallery',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="product-gallery" data-testid="product-gallery">
      <!-- Main Image Container -->
      <div class="main-image-container"
           #mainImageContainer
           [class.zooming]="isZooming() && !isTouchDevice()"
           (mousemove)="onMouseMove($event)"
           (mouseleave)="onMouseLeave()"
           (click)="openFullscreen()">
        @if (isImageLoading()) {
          <div class="image-loading" data-testid="image-loading"></div>
        }
        @if (selectedImage()) {
          <img [src]="selectedImage()!.url"
               [alt]="selectedImage()!.altText || 'Product image'"
               class="main-image"
               [class.changing]="isTransitioning()"
               [class.loaded]="!isImageLoading()"
               data-testid="gallery-main-image"
               loading="eager"
               fetchpriority="high"
               (load)="onImageLoad()" />

          <!-- Zoom Lens (desktop only) -->
          @if (isZooming() && !isTouchDevice()) {
            <div class="zoom-lens"
                 [style.left.px]="lensPosition().x"
                 [style.top.px]="lensPosition().y"
                 [style.width.px]="lensSize"
                 [style.height.px]="lensSize">
            </div>
          }

          <!-- Zoom magnifier icon -->
          <button class="zoom-hint" [attr.aria-label]="'common.aria.zoomImage' | translate">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"></circle>
              <path d="m21 21-4.35-4.35"></path>
              <path d="M11 8v6"></path>
              <path d="M8 11h6"></path>
            </svg>
          </button>
        } @else {
          <div class="no-image">{{ 'products.noImage' | translate }}</div>
        }
      </div>

      <!-- Zoom Result Panel (desktop hover zoom) -->
      @if (isZooming() && !isTouchDevice()) {
        <div class="zoom-result"
             [style.background-image]="'url(' + selectedImage()!.url + ')'"
             [style.background-position]="zoomPosition()"
             [style.background-size]="zoomBackgroundSize()">
        </div>
      }

      <!-- Thumbnail Strip -->
      @if (images() && images().length > 1) {
        <div class="thumbnails" data-testid="gallery-thumbnails">
          @for (image of images(); track image.id) {
            <button class="thumbnail"
                    [class.active]="selectedImage()?.id === image.id"
                    (click)="selectImage(image)"
                    data-testid="gallery-thumbnail">
              <img [src]="image.url"
                   [alt]="image.altText || 'Product thumbnail'"
                   loading="lazy" />
            </button>
          }
        </div>
      }

      <!-- Fullscreen Modal / Lightbox -->
      @if (isFullscreen()) {
        <div class="fullscreen-overlay"
             [class.closing]="isClosingFullscreen()"
             data-testid="gallery-fullscreen"
             (click)="closeFullscreen()"
             role="dialog"
             aria-modal="true">
          <div class="fullscreen-content" (click)="$event.stopPropagation()">
            <!-- Navigation -->
            <button class="nav-btn prev"
                    [disabled]="currentIndex() === 0"
                    (click)="prevImage($event)"
                    [attr.aria-label]="'common.aria.previousImage' | translate">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="m15 18-6-6 6-6"/>
              </svg>
            </button>

            <!-- Image with pinch zoom on mobile -->
            <div class="fullscreen-image-wrapper"
                 #fullscreenWrapper
                 (touchstart)="onTouchStart($event)"
                 (touchmove)="onTouchMove($event)"
                 (touchend)="onTouchEnd()">
              <img [src]="selectedImage()!.url"
                   [alt]="selectedImage()!.altText || 'Product image'"
                   class="fullscreen-image"
                   [class.slide-in-left]="slideDirection() === 'left'"
                   [class.slide-in-right]="slideDirection() === 'right'"
                   [style.transform]="fullscreenTransform()" />
            </div>

            <button class="nav-btn next"
                    [disabled]="currentIndex() === images().length - 1"
                    (click)="nextImage($event)"
                    [attr.aria-label]="'common.aria.nextImage' | translate">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="m9 18 6-6-6-6"/>
              </svg>
            </button>

            <!-- Close button -->
            <button class="close-btn"
                    (click)="closeFullscreen()"
                    [attr.aria-label]="'common.aria.closeFullscreen' | translate">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M18 6 6 18"/>
                <path d="m6 6 12 12"/>
              </svg>
            </button>

            <!-- Image counter -->
            <div class="image-counter">
              {{ currentIndex() + 1 }} / {{ images().length }}
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .product-gallery {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      position: relative;
    }

    /* Main Image Container with zoom effect on hover */
    .main-image-container {
      position: relative;
      aspect-ratio: 1;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      overflow: hidden;
      cursor: zoom-in;

      .main-image {
        width: 100%;
        height: 100%;
        object-fit: contain;
        opacity: 0;
        transition: opacity 0.3s ease-out, transform 0.4s ease-out;

        &.loaded {
          opacity: 1;
        }

        /* Crossfade transition when changing images */
        &.changing {
          opacity: 0;
        }
      }

      /* Image zoom effect on hover */
      &.zooming .main-image {
        transform: scale(1.1);
      }

      .no-image {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-text-secondary);
        font-size: 1rem;
      }
    }

    /* Loading shimmer animation */
    .image-loading {
      position: absolute;
      inset: 0;
      background: linear-gradient(90deg,
        var(--color-bg-secondary) 25%,
        var(--color-bg-tertiary, var(--color-bg-primary)) 50%,
        var(--color-bg-secondary) 75%
      );
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 12px;
    }

    @keyframes shimmer {
      0% {
        background-position: 200% 0;
      }
      100% {
        background-position: -200% 0;
      }
    }

    .zoom-lens {
      position: absolute;
      border: 2px solid var(--color-primary);
      background: rgba(255, 255, 255, 0.3);
      pointer-events: none;
      border-radius: 4px;
      z-index: 1;
      transition: opacity 0.15s ease-out;
    }

    .zoom-hint {
      position: absolute;
      bottom: 1rem;
      right: 1rem;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border: none;
      background: var(--color-bg-primary);
      color: var(--color-text-secondary);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      transition: all 0.2s ease-out;

      svg {
        width: 20px;
        height: 20px;
        transition: transform 0.2s ease-out;
      }

      &:hover {
        background: var(--color-primary);
        color: white;
        transform: scale(1.1);

        svg {
          transform: scale(1.1);
        }
      }
    }

    .zoom-result {
      position: absolute;
      top: 0;
      left: calc(100% + 1rem);
      width: 400px;
      height: 400px;
      border: 1px solid var(--color-border);
      border-radius: 12px;
      background-repeat: no-repeat;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      z-index: 100;
      pointer-events: none;
      animation: zoomResultFadeIn 0.2s ease-out;
    }

    @keyframes zoomResultFadeIn {
      from {
        opacity: 0;
        transform: translateX(-10px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    .thumbnails {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    /* Thumbnail with enhanced animations */
    .thumbnail {
      width: 80px;
      height: 80px;
      padding: 0;
      border: 2px solid transparent;
      border-radius: 8px;
      overflow: hidden;
      cursor: pointer;
      background: var(--color-bg-secondary);
      transition: transform 0.2s ease-out, box-shadow 0.2s ease-out, border-color 0.2s ease-out;

      &:hover {
        transform: scale(1.05);
        border-color: var(--color-primary-light);
      }

      &.active {
        border-color: var(--color-primary);
        box-shadow: 0 0 0 3px var(--color-primary-light, rgba(59, 130, 246, 0.3));
        transform: scale(1.02);
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        transition: transform 0.2s ease-out;
      }

      &:hover img {
        transform: scale(1.05);
      }
    }

    /* Fullscreen Modal / Lightbox with scale animation */
    .fullscreen-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.95);
      z-index: 1000;
      display: flex;
      align-items: center;
      justify-content: center;
      animation: lightboxOpen 0.3s ease-out forwards;

      &.closing {
        animation: lightboxClose 0.2s ease-in forwards;
      }
    }

    @keyframes lightboxOpen {
      from {
        opacity: 0;
        transform: scale(0.9);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }

    @keyframes lightboxClose {
      from {
        opacity: 1;
        transform: scale(1);
      }
      to {
        opacity: 0;
        transform: scale(0.9);
      }
    }

    .fullscreen-content {
      position: relative;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .fullscreen-image-wrapper {
      max-width: 90vw;
      max-height: 90vh;
      display: flex;
      align-items: center;
      justify-content: center;
      touch-action: none;
      overflow: hidden;
    }

    .fullscreen-image {
      max-width: 100%;
      max-height: 90vh;
      object-fit: contain;
      transition: transform 0.1s ease-out;

      /* Slide animations for image navigation */
      &.slide-in-left {
        animation: slideInLeft 0.3s ease-out forwards;
      }

      &.slide-in-right {
        animation: slideInRight 0.3s ease-out forwards;
      }
    }

    @keyframes slideInLeft {
      from {
        opacity: 0;
        transform: translateX(-50px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    @keyframes slideInRight {
      from {
        opacity: 0;
        transform: translateX(50px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    .nav-btn {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      width: 50px;
      height: 50px;
      border-radius: 50%;
      border: none;
      background: rgba(255, 255, 255, 0.1);
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease-out;
      z-index: 10;

      svg {
        width: 24px;
        height: 24px;
        transition: transform 0.2s ease-out;
      }

      &:hover:not(:disabled) {
        background: rgba(255, 255, 255, 0.2);
        transform: translateY(-50%) scale(1.1);

        svg {
          transform: scale(1.1);
        }
      }

      &:active:not(:disabled) {
        transform: translateY(-50%) scale(0.95);
      }

      &:disabled {
        opacity: 0.3;
        cursor: not-allowed;
      }

      &.prev {
        left: 1rem;

        &:hover:not(:disabled) svg {
          transform: translateX(-3px);
        }
      }

      &.next {
        right: 1rem;

        &:hover:not(:disabled) svg {
          transform: translateX(3px);
        }
      }
    }

    .close-btn {
      position: absolute;
      top: 1rem;
      right: 1rem;
      width: 44px;
      height: 44px;
      border-radius: 50%;
      border: none;
      background: rgba(255, 255, 255, 0.1);
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease-out;
      z-index: 10;

      svg {
        width: 24px;
        height: 24px;
        transition: transform 0.2s ease-out;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.2);
        transform: scale(1.1) rotate(90deg);
      }

      &:active {
        transform: scale(0.95);
      }
    }

    .image-counter {
      position: absolute;
      bottom: 1rem;
      left: 50%;
      transform: translateX(-50%);
      padding: 0.5rem 1rem;
      background: rgba(0, 0, 0, 0.6);
      color: white;
      border-radius: 20px;
      font-size: 0.875rem;
      animation: fadeInUp 0.3s ease-out 0.1s backwards;
    }

    @keyframes fadeInUp {
      from {
        opacity: 0;
        transform: translateX(-50%) translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateX(-50%) translateY(0);
      }
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .zoom-result {
        display: none;
      }
    }

    @media (max-width: 768px) {
      .thumbnails {
        justify-content: center;
      }

      .thumbnail {
        width: 60px;
        height: 60px;
      }

      .nav-btn {
        width: 40px;
        height: 40px;

        svg {
          width: 20px;
          height: 20px;
        }
      }

      .zoom-hint {
        width: 36px;
        height: 36px;

        svg {
          width: 18px;
          height: 18px;
        }
      }
    }

    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .main-image,
      .thumbnail,
      .fullscreen-overlay,
      .fullscreen-image,
      .zoom-hint,
      .zoom-lens,
      .zoom-result,
      .nav-btn,
      .close-btn,
      .image-counter {
        transition: none !important;
        animation: none !important;
      }

      .image-loading {
        animation: none !important;
        background: var(--color-bg-secondary);
      }

      /* For reduced motion, just show/hide without animations */
      .main-image.changing {
        opacity: 1;
      }
    }
  `]
})
export class ProductGalleryComponent {
  @ViewChild('mainImageContainer') mainImageContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('fullscreenWrapper') fullscreenWrapper!: ElementRef<HTMLDivElement>;

  images = input.required<ProductImage[]>();
  productName = input<string>('');

  // State
  selectedImage = signal<ProductImage | null>(null);
  isZooming = signal(false);
  isFullscreen = signal(false);
  isClosingFullscreen = signal(false);
  isTransitioning = signal(false);
  isImageLoading = signal(true);
  slideDirection = signal<'left' | 'right' | null>(null);

  // Zoom calculations
  lensSize = 150;
  zoomLevel = 2.5;
  lensPosition = signal({ x: 0, y: 0 });
  mousePosition = signal({ x: 0, y: 0 });
  imageSize = signal({ width: 0, height: 0 });

  // Pinch zoom state
  private initialPinchDistance = 0;
  private pinchScale = signal(1);
  private pinchTranslate = signal({ x: 0, y: 0 });

  // Animation timeouts
  private transitionTimeout: ReturnType<typeof setTimeout> | null = null;
  private slideTimeout: ReturnType<typeof setTimeout> | null = null;

  // Computed
  currentIndex = computed(() => {
    const selected = this.selectedImage();
    if (!selected) return 0;
    return this.images().findIndex(img => img.id === selected.id);
  });

  zoomPosition = computed(() => {
    const mouse = this.mousePosition();
    const imgSize = this.imageSize();
    if (imgSize.width === 0 || imgSize.height === 0) return '0% 0%';

    const xPercent = (mouse.x / imgSize.width) * 100;
    const yPercent = (mouse.y / imgSize.height) * 100;
    return `${xPercent}% ${yPercent}%`;
  });

  zoomBackgroundSize = computed(() => {
    const imgSize = this.imageSize();
    return `${imgSize.width * this.zoomLevel}px ${imgSize.height * this.zoomLevel}px`;
  });

  fullscreenTransform = computed(() => {
    const scale = this.pinchScale();
    const translate = this.pinchTranslate();
    return `scale(${scale}) translate(${translate.x}px, ${translate.y}px)`;
  });

  constructor() {
    effect(() => {
      const imgs = this.images();
      if (imgs && imgs.length > 0 && !this.selectedImage()) {
        const sorted = [...imgs].sort((a, b) => a.sortOrder - b.sortOrder);
        this.selectedImage.set(sorted[0]);
      }
    });
  }

  @HostListener('document:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.isFullscreen()) return;

    switch (event.key) {
      case 'Escape':
        this.closeFullscreen();
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.prevImage();
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.nextImage();
        break;
    }
  }

  isTouchDevice(): boolean {
    return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  }

  selectImage(image: ProductImage): void {
    // Skip if already selected
    if (this.selectedImage()?.id === image.id) return;

    // Start crossfade transition
    this.isTransitioning.set(true);
    this.isImageLoading.set(true);

    // Clear any existing timeout
    if (this.transitionTimeout) {
      clearTimeout(this.transitionTimeout);
    }

    // After fade out, change the image
    this.transitionTimeout = setTimeout(() => {
      this.selectedImage.set(image);
      this.isTransitioning.set(false);
    }, 150); // Half of the 0.3s transition
  }

  onImageLoad(): void {
    this.isImageLoading.set(false);

    if (this.mainImageContainer?.nativeElement) {
      const img = this.mainImageContainer.nativeElement.querySelector('img');
      if (img) {
        this.imageSize.set({
          width: img.clientWidth,
          height: img.clientHeight
        });
      }
    }
  }

  onMouseMove(event: MouseEvent): void {
    if (this.isTouchDevice()) return;

    const container = this.mainImageContainer?.nativeElement;
    if (!container) return;

    const rect = container.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // Update mouse position for zoom
    this.mousePosition.set({ x, y });

    // Calculate lens position (centered on mouse)
    const lensX = Math.max(0, Math.min(x - this.lensSize / 2, rect.width - this.lensSize));
    const lensY = Math.max(0, Math.min(y - this.lensSize / 2, rect.height - this.lensSize));
    this.lensPosition.set({ x: lensX, y: lensY });

    this.isZooming.set(true);
  }

  onMouseLeave(): void {
    this.isZooming.set(false);
  }

  openFullscreen(): void {
    this.isFullscreen.set(true);
    this.pinchScale.set(1);
    this.pinchTranslate.set({ x: 0, y: 0 });
    document.body.style.overflow = 'hidden';
  }

  closeFullscreen(): void {
    // Trigger closing animation
    this.isClosingFullscreen.set(true);

    // Wait for animation to complete before hiding
    setTimeout(() => {
      this.isFullscreen.set(false);
      this.isClosingFullscreen.set(false);
      this.pinchScale.set(1);
      this.pinchTranslate.set({ x: 0, y: 0 });
      document.body.style.overflow = '';
    }, 200); // Match lightboxClose animation duration
  }

  prevImage(event?: Event): void {
    event?.stopPropagation();
    const idx = this.currentIndex();
    if (idx > 0) {
      this.triggerSlideAnimation('right');
      this.selectedImage.set(this.images()[idx - 1]);
    }
  }

  nextImage(event?: Event): void {
    event?.stopPropagation();
    const idx = this.currentIndex();
    if (idx < this.images().length - 1) {
      this.triggerSlideAnimation('left');
      this.selectedImage.set(this.images()[idx + 1]);
    }
  }

  private triggerSlideAnimation(direction: 'left' | 'right'): void {
    // Clear any existing slide timeout
    if (this.slideTimeout) {
      clearTimeout(this.slideTimeout);
    }

    this.slideDirection.set(direction);

    // Reset after animation completes
    this.slideTimeout = setTimeout(() => {
      this.slideDirection.set(null);
    }, 300); // Match slide animation duration
  }

  // Touch handlers for pinch zoom
  onTouchStart(event: TouchEvent): void {
    if (event.touches.length === 2) {
      this.initialPinchDistance = this.getPinchDistance(event);
    }
  }

  onTouchMove(event: TouchEvent): void {
    if (event.touches.length === 2 && this.initialPinchDistance > 0) {
      event.preventDefault();
      const currentDistance = this.getPinchDistance(event);
      const scale = currentDistance / this.initialPinchDistance;
      const newScale = Math.max(1, Math.min(this.pinchScale() * scale, 4));
      this.pinchScale.set(newScale);
      this.initialPinchDistance = currentDistance;
    }
  }

  onTouchEnd(): void {
    this.initialPinchDistance = 0;
    // Reset to 1 if scale is close to 1
    if (this.pinchScale() < 1.1) {
      this.pinchScale.set(1);
      this.pinchTranslate.set({ x: 0, y: 0 });
    }
  }

  private getPinchDistance(event: TouchEvent): number {
    const touch1 = event.touches[0];
    const touch2 = event.touches[1];
    return Math.hypot(
      touch2.clientX - touch1.clientX,
      touch2.clientY - touch1.clientY
    );
  }
}
