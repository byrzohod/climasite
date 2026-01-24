import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { OptimizedImageDirective } from './optimized-image.directive';

@Component({
  standalone: true,
  imports: [OptimizedImageDirective],
  template: `
    <img 
      appOptimizedImage 
      [src]="imageSrc" 
      [fallbackSrc]="fallbackSrc"
      [loading]="loading"
      [fallbackStrategy]="fallbackStrategy"
      alt="Test image"
    />
  `
})
class TestHostComponent {
  imageSrc = 'test-image.jpg';
  fallbackSrc = '/assets/images/fallbacks/no-product-image.svg';
  loading: 'lazy' | 'eager' = 'lazy';
  fallbackStrategy: 'placeholder' | 'hide' | 'custom' = 'placeholder';
}

describe('OptimizedImageDirective', () => {
  let component: TestHostComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let imgElement: HTMLImageElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    imgElement = fixture.nativeElement.querySelector('img');
  });

  it('should create an instance', () => {
    expect(imgElement).toBeTruthy();
  });

  it('should apply optimized-image class', () => {
    expect(imgElement.classList.contains('optimized-image')).toBe(true);
  });

  it('should set loading attribute to lazy by default', () => {
    expect(imgElement.getAttribute('loading')).toBe('lazy');
  });

  it('should set loading attribute based on input', async () => {
    // The loading attribute is set once during ngOnInit
    // Test with a new fixture with eager loading
    @Component({
      standalone: true,
      imports: [OptimizedImageDirective],
      template: `<img appOptimizedImage src="test.jpg" loading="eager" alt="Test" />`
    })
    class EagerLoadingTestComponent {}
    
    const eagerFixture = TestBed.createComponent(EagerLoadingTestComponent);
    eagerFixture.detectChanges();
    const eagerImg = eagerFixture.nativeElement.querySelector('img');
    expect(eagerImg.getAttribute('loading')).toBe('eager');
  });

  it('should set decoding attribute to async', () => {
    expect(imgElement.getAttribute('decoding')).toBe('async');
  });

  it('should apply loading class initially', () => {
    expect(imgElement.classList.contains('optimized-image--loading')).toBe(true);
  });

  it('should apply loaded class after load event', fakeAsync(() => {
    imgElement.dispatchEvent(new Event('load'));
    tick();
    fixture.detectChanges();
    
    expect(imgElement.classList.contains('optimized-image--loaded')).toBe(true);
    expect(imgElement.classList.contains('optimized-image--loading')).toBe(false);
  }));

  it('should apply error class and fallback src on error', fakeAsync(() => {
    imgElement.dispatchEvent(new Event('error'));
    tick();
    fixture.detectChanges();
    
    expect(imgElement.classList.contains('optimized-image--error')).toBe(true);
    expect(imgElement.src).toContain('no-product-image.svg');
  }));

  it('should hide image when fallbackStrategy is hide', fakeAsync(() => {
    component.fallbackStrategy = 'hide';
    fixture.detectChanges();
    
    imgElement.dispatchEvent(new Event('error'));
    tick();
    fixture.detectChanges();
    
    expect(imgElement.style.display).toBe('none');
  }));

  it('should ensure alt attribute exists', () => {
    expect(imgElement.hasAttribute('alt')).toBe(true);
  });
});

@Component({
  standalone: true,
  imports: [OptimizedImageDirective],
  template: `<img appOptimizedImage src="test.jpg" />`
})
class TestHostWithoutAltComponent {}

describe('OptimizedImageDirective without alt', () => {
  let fixture: ComponentFixture<TestHostWithoutAltComponent>;
  let imgElement: HTMLImageElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostWithoutAltComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostWithoutAltComponent);
    fixture.detectChanges();
    imgElement = fixture.nativeElement.querySelector('img');
  });

  it('should add empty alt attribute if missing', () => {
    expect(imgElement.hasAttribute('alt')).toBe(true);
    expect(imgElement.getAttribute('alt')).toBe('');
  });
});
