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
           (mousemove)="onMouseMove($event)"
           (mouseleave)="onMouseLeave()"
           (click)="openFullscreen()">
        @if (selectedImage()) {
          <img [src]="selectedImage()!.url"
               [alt]="selectedImage()!.altText || 'Product image'"
               class="main-image"
               data-testid="gallery-main-image"
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
          <button class="zoom-hint" aria-label="Zoom image">
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

      <!-- Fullscreen Modal -->
      @if (isFullscreen()) {
        <div class="fullscreen-overlay"
             data-testid="gallery-fullscreen"
             (click)="closeFullscreen()"
             role="dialog"
             aria-modal="true">
          <div class="fullscreen-content" (click)="$event.stopPropagation()">
            <!-- Navigation -->
            <button class="nav-btn prev"
                    [disabled]="currentIndex() === 0"
                    (click)="prevImage($event)"
                    aria-label="Previous image">
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
                   [style.transform]="fullscreenTransform()" />
            </div>

            <button class="nav-btn next"
                    [disabled]="currentIndex() === images().length - 1"
                    (click)="nextImage($event)"
                    aria-label="Next image">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="m9 18 6-6-6-6"/>
              </svg>
            </button>

            <!-- Close button -->
            <button class="close-btn"
                    (click)="closeFullscreen()"
                    aria-label="Close fullscreen">
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
        transition: opacity 0.2s;
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

    .zoom-lens {
      position: absolute;
      border: 2px solid var(--color-primary);
      background: rgba(255, 255, 255, 0.3);
      pointer-events: none;
      border-radius: 4px;
      z-index: 1;
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
      transition: all 0.2s;

      svg {
        width: 20px;
        height: 20px;
      }

      &:hover {
        background: var(--color-primary);
        color: white;
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
    }

    .thumbnails {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .thumbnail {
      width: 80px;
      height: 80px;
      padding: 0;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      overflow: hidden;
      cursor: pointer;
      background: var(--color-bg-secondary);
      transition: border-color 0.2s;

      &:hover {
        border-color: var(--color-primary-light);
      }

      &.active {
        border-color: var(--color-primary);
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
    }

    /* Fullscreen Modal */
    .fullscreen-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.95);
      z-index: 1000;
      display: flex;
      align-items: center;
      justify-content: center;
      animation: fadeIn 0.2s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
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
    }

    .fullscreen-image {
      max-width: 100%;
      max-height: 90vh;
      object-fit: contain;
      transition: transform 0.1s ease-out;
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
      transition: all 0.2s;
      z-index: 10;

      svg {
        width: 24px;
        height: 24px;
      }

      &:hover:not(:disabled) {
        background: rgba(255, 255, 255, 0.2);
      }

      &:disabled {
        opacity: 0.3;
        cursor: not-allowed;
      }

      &.prev {
        left: 1rem;
      }

      &.next {
        right: 1rem;
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
      transition: all 0.2s;
      z-index: 10;

      svg {
        width: 24px;
        height: 24px;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.2);
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
    this.selectedImage.set(image);
  }

  onImageLoad(): void {
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
    this.isFullscreen.set(false);
    this.pinchScale.set(1);
    this.pinchTranslate.set({ x: 0, y: 0 });
    document.body.style.overflow = '';
  }

  prevImage(event?: Event): void {
    event?.stopPropagation();
    const idx = this.currentIndex();
    if (idx > 0) {
      this.selectedImage.set(this.images()[idx - 1]);
    }
  }

  nextImage(event?: Event): void {
    event?.stopPropagation();
    const idx = this.currentIndex();
    if (idx < this.images().length - 1) {
      this.selectedImage.set(this.images()[idx + 1]);
    }
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
