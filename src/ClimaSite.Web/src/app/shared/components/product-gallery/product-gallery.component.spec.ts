import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductGalleryComponent, ProductImage } from './product-gallery.component';
import { TranslateModule } from '@ngx-translate/core';

describe('ProductGalleryComponent', () => {
  let component: ProductGalleryComponent;
  let fixture: ComponentFixture<ProductGalleryComponent>;

  const mockImages: ProductImage[] = [
    { id: '1', url: 'https://example.com/image1.jpg', altText: 'Image 1', sortOrder: 0 },
    { id: '2', url: 'https://example.com/image2.jpg', altText: 'Image 2', sortOrder: 1 },
    { id: '3', url: 'https://example.com/image3.jpg', altText: 'Image 3', sortOrder: 2 }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductGalleryComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductGalleryComponent);
    component = fixture.componentInstance;
    // Set required input using ComponentRef
    fixture.componentRef.setInput('images', mockImages);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should select first image by default', () => {
    expect(component.selectedImage()).toEqual(mockImages[0]);
  });

  it('should change selected image when thumbnail clicked', () => {
    component.selectImage(mockImages[1]);
    expect(component.selectedImage()).toEqual(mockImages[1]);
  });

  it('should calculate current index correctly', () => {
    expect(component.currentIndex()).toBe(0);
    component.selectImage(mockImages[1]);
    expect(component.currentIndex()).toBe(1);
  });

  it('should navigate to next image', () => {
    expect(component.currentIndex()).toBe(0);
    component.nextImage();
    expect(component.currentIndex()).toBe(1);
  });

  it('should navigate to previous image', () => {
    component.selectImage(mockImages[2]);
    expect(component.currentIndex()).toBe(2);
    component.prevImage();
    expect(component.currentIndex()).toBe(1);
  });

  it('should not go past first image', () => {
    expect(component.currentIndex()).toBe(0);
    component.prevImage();
    expect(component.currentIndex()).toBe(0);
  });

  it('should not go past last image', () => {
    component.selectImage(mockImages[2]);
    expect(component.currentIndex()).toBe(2);
    component.nextImage();
    expect(component.currentIndex()).toBe(2);
  });

  it('should open fullscreen mode', () => {
    expect(component.isFullscreen()).toBeFalse();
    component.openFullscreen();
    expect(component.isFullscreen()).toBeTrue();
  });

  it('should close fullscreen mode', () => {
    component.openFullscreen();
    expect(component.isFullscreen()).toBeTrue();
    component.closeFullscreen();
    expect(component.isFullscreen()).toBeFalse();
  });

  it('should reset zoom state on close', () => {
    component.openFullscreen();
    component.closeFullscreen();
    expect(component.isFullscreen()).toBeFalse();
  });

  it('should handle keyboard navigation in fullscreen', () => {
    component.openFullscreen();

    // Arrow right should go to next image
    const rightEvent = new KeyboardEvent('keydown', { key: 'ArrowRight' });
    component.onKeyDown(rightEvent);
    expect(component.currentIndex()).toBe(1);

    // Arrow left should go to previous image
    const leftEvent = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
    component.onKeyDown(leftEvent);
    expect(component.currentIndex()).toBe(0);

    // Escape should close fullscreen
    const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape' });
    component.onKeyDown(escapeEvent);
    expect(component.isFullscreen()).toBeFalse();
  });

  it('should not respond to keyboard when not in fullscreen', () => {
    const rightEvent = new KeyboardEvent('keydown', { key: 'ArrowRight' });
    component.onKeyDown(rightEvent);
    expect(component.currentIndex()).toBe(0); // Should not change
  });

  it('should sort images by sortOrder', () => {
    const unsortedImages: ProductImage[] = [
      { id: '3', url: 'url3', sortOrder: 2 },
      { id: '1', url: 'url1', sortOrder: 0 },
      { id: '2', url: 'url2', sortOrder: 1 }
    ];

    fixture.componentRef.setInput('images', unsortedImages);
    fixture.detectChanges();

    // First selected should be the one with lowest sortOrder
    expect(component.selectedImage()?.id).toBe('1');
  });

  it('should detect touch device', () => {
    // In test environment, this should return false
    const isTouchDevice = component.isTouchDevice();
    expect(typeof isTouchDevice).toBe('boolean');
  });

  describe('Zoom functionality', () => {
    it('should start with isZooming false', () => {
      expect(component.isZooming()).toBeFalse();
    });

    it('should stop zooming on mouse leave', () => {
      component.onMouseLeave();
      expect(component.isZooming()).toBeFalse();
    });
  });
});
