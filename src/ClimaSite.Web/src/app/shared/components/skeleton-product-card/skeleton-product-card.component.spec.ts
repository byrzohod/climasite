import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonProductCardComponent } from './skeleton-product-card.component';

describe('SkeletonProductCardComponent', () => {
  let component: SkeletonProductCardComponent;
  let fixture: ComponentFixture<SkeletonProductCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonProductCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SkeletonProductCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Structure', () => {
    it('should render a skeleton-card wrapper with role="status"', () => {
      const card: HTMLElement = fixture.nativeElement.querySelector('.skeleton-card');
      expect(card).toBeTruthy();
      expect(card.getAttribute('role')).toBe('status');
    });

    it('should render an image placeholder and a content area', () => {
      expect(fixture.nativeElement.querySelector('.skeleton-image')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.skeleton-content')).toBeTruthy();
    });

    it('should render six nested skeleton elements (1 image + 5 content lines)', () => {
      const skeletons = fixture.nativeElement.querySelectorAll('app-skeleton');
      expect(skeletons.length).toBe(6);
    });

    it('should render the image skeleton as a rectangular variant', () => {
      const rectangular = fixture.nativeElement.querySelector('.skeleton--rectangular');
      expect(rectangular).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    it('should use the default aria-label and expose it to screen readers', () => {
      const card: HTMLElement = fixture.nativeElement.querySelector('.skeleton-card');
      expect(card.getAttribute('aria-label')).toBe('Loading product...');

      const srOnly: HTMLElement = fixture.nativeElement.querySelector('.sr-only');
      expect(srOnly.textContent?.trim()).toBe('Loading product...');
    });

    it('should reflect a custom aria-label on the wrapper and sr-only text', () => {
      fixture.componentRef.setInput('ariaLabel', 'Loading AC unit');
      fixture.detectChanges();

      const card: HTMLElement = fixture.nativeElement.querySelector('.skeleton-card');
      expect(card.getAttribute('aria-label')).toBe('Loading AC unit');
      const srOnly: HTMLElement = fixture.nativeElement.querySelector('.sr-only');
      expect(srOnly.textContent?.trim()).toBe('Loading AC unit');
    });

    it('should use the default test id', () => {
      const card: HTMLElement = fixture.nativeElement.querySelector('.skeleton-card');
      expect(card.getAttribute('data-testid')).toBe('skeleton-product-card');
    });

    it('should reflect a custom test id', () => {
      fixture.componentRef.setInput('testId', 'card-loader');
      fixture.detectChanges();

      const card: HTMLElement = fixture.nativeElement.querySelector('.skeleton-card');
      expect(card.getAttribute('data-testid')).toBe('card-loader');
    });
  });

  describe('Animation passthrough', () => {
    it('should default to the pulse animation on nested skeletons', () => {
      const pulsing = fixture.nativeElement.querySelectorAll('.skeleton--pulse');
      expect(pulsing.length).toBeGreaterThan(0);
    });

    it('should propagate animation="none" so no animation classes are applied', () => {
      fixture.componentRef.setInput('animation', 'none');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll('.skeleton--pulse').length).toBe(0);
      expect(fixture.nativeElement.querySelectorAll('.skeleton--wave').length).toBe(0);
    });

    it('should propagate animation="wave" to nested skeletons', () => {
      fixture.componentRef.setInput('animation', 'wave');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelectorAll('.skeleton--wave').length).toBeGreaterThan(0);
    });
  });
});
