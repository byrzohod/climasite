import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import { SkeletonComponent, type SkeletonVariant, type SkeletonAnimation } from './skeleton.component';

// Test host component for testing inputs
@Component({
  standalone: true,
  imports: [SkeletonComponent],
  template: `
    <app-skeleton
      [variant]="variant"
      [width]="width"
      [height]="height"
      [animation]="animation"
      [lines]="lines"
      [lastLineWidth]="lastLineWidth"
      [ariaLabel]="ariaLabel"
      [testId]="testId"
    />
  `
})
class TestHostComponent {
  variant: SkeletonVariant = 'text';
  width = '100%';
  height = '1rem';
  animation: SkeletonAnimation = 'pulse';
  lines = 1;
  lastLineWidth = '70%';
  ariaLabel = 'Loading...';
  testId = 'skeleton';
}

describe('SkeletonComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, SkeletonComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      const skeleton = fixture.debugElement.query(By.css('[data-testid="skeleton"]'));
      expect(skeleton).toBeTruthy();
    });

    it('should render with default props', () => {
      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton).toBeTruthy();
      expect(skeleton.nativeElement.classList).toContain('skeleton--text');
      expect(skeleton.nativeElement.classList).toContain('skeleton--pulse');
    });

    it('should apply correct width and height', () => {
      hostComponent.width = '200px';
      hostComponent.height = '2rem';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.style.width).toBe('200px');
      expect(skeleton.nativeElement.style.height).toBe('2rem');
    });

    it('should have correct ARIA attributes', () => {
      const skeleton = fixture.debugElement.query(By.css('[role="status"]'));
      expect(skeleton).toBeTruthy();
      expect(skeleton.nativeElement.getAttribute('aria-label')).toBe('Loading...');
    });

    it('should have screen reader text', () => {
      const srOnly = fixture.debugElement.query(By.css('.sr-only'));
      expect(srOnly).toBeTruthy();
      expect(srOnly.nativeElement.textContent).toBe('Loading...');
    });

    it('should apply custom aria-label', () => {
      hostComponent.ariaLabel = 'Loading product data...';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('[role="status"]'));
      expect(skeleton.nativeElement.getAttribute('aria-label')).toBe('Loading product data...');
    });

    it('should apply custom testId', () => {
      hostComponent.testId = 'custom-skeleton';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('[data-testid="custom-skeleton"]'));
      expect(skeleton).toBeTruthy();
    });
  });

  describe('Variants', () => {
    it('should apply text variant class', () => {
      hostComponent.variant = 'text';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--text');
    });

    it('should apply circular variant class', () => {
      hostComponent.variant = 'circular';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--circular');
    });

    it('should apply rectangular variant class', () => {
      hostComponent.variant = 'rectangular';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--rectangular');
    });

    it('should apply rounded variant class', () => {
      hostComponent.variant = 'rounded';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--rounded');
    });

    it('should use height for width in circular variant', () => {
      hostComponent.variant = 'circular';
      hostComponent.width = '200px';
      hostComponent.height = '48px';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      // Circular variant should use height for both dimensions
      expect(skeleton.nativeElement.style.width).toBe('48px');
      expect(skeleton.nativeElement.style.height).toBe('48px');
    });
  });

  describe('Animations', () => {
    it('should apply pulse animation class', () => {
      hostComponent.animation = 'pulse';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--pulse');
    });

    it('should apply wave animation class', () => {
      hostComponent.animation = 'wave';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).toContain('skeleton--wave');
    });

    it('should not apply animation class when none', () => {
      hostComponent.animation = 'none';
      fixture.detectChanges();

      const skeleton = fixture.debugElement.query(By.css('.skeleton'));
      expect(skeleton.nativeElement.classList).not.toContain('skeleton--pulse');
      expect(skeleton.nativeElement.classList).not.toContain('skeleton--wave');
    });
  });

  describe('Multi-line text', () => {
    it('should render single skeleton for single line', () => {
      hostComponent.lines = 1;
      fixture.detectChanges();

      const skeletons = fixture.debugElement.queryAll(By.css('.skeleton'));
      expect(skeletons.length).toBe(1);
    });

    it('should render multiple skeletons for multiple lines', () => {
      hostComponent.lines = 3;
      fixture.detectChanges();

      const skeletons = fixture.debugElement.queryAll(By.css('.skeleton'));
      expect(skeletons.length).toBe(3);
    });

    it('should apply lastLineWidth to last line', () => {
      hostComponent.lines = 3;
      hostComponent.lastLineWidth = '60%';
      fixture.detectChanges();

      const skeletons = fixture.debugElement.queryAll(By.css('.skeleton'));
      const lastSkeleton = skeletons[skeletons.length - 1];
      expect(lastSkeleton.nativeElement.style.width).toBe('60%');
    });

    it('should apply full width to non-last lines', () => {
      hostComponent.lines = 3;
      hostComponent.width = '100%';
      fixture.detectChanges();

      const skeletons = fixture.debugElement.queryAll(By.css('.skeleton'));
      expect(skeletons[0].nativeElement.style.width).toBe('100%');
      expect(skeletons[1].nativeElement.style.width).toBe('100%');
    });

    it('should render container for multi-line', () => {
      hostComponent.lines = 3;
      fixture.detectChanges();

      const container = fixture.debugElement.query(By.css('.skeleton-container'));
      expect(container).toBeTruthy();
    });
  });
});
