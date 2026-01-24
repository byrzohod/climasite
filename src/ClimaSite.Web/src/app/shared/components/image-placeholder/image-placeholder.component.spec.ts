import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImagePlaceholderComponent, PlaceholderVariant } from './image-placeholder.component';

describe('ImagePlaceholderComponent', () => {
  let component: ImagePlaceholderComponent;
  let fixture: ComponentFixture<ImagePlaceholderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImagePlaceholderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ImagePlaceholderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default variant of product', () => {
    expect(component.variant()).toBe('product');
  });

  it('should have default size of md', () => {
    expect(component.size()).toBe('md');
  });

  it('should show icon by default', () => {
    expect(component.showIcon()).toBe(true);
  });

  it('should render correct CSS classes for variant', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('.placeholder');
    expect(placeholder?.classList.contains('placeholder--product')).toBe(true);
  });

  it('should apply animation class when animated is true', () => {
    fixture.componentRef.setInput('animated', true);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('.placeholder');
    expect(placeholder?.classList.contains('placeholder--animated')).toBe(true);
  });

  it('should compute icon size based on size input', () => {
    expect(component.iconSize()).toBe(32); // md default
    
    fixture.componentRef.setInput('size', 'xs');
    expect(component.iconSize()).toBe(16);
    
    fixture.componentRef.setInput('size', 'xl');
    expect(component.iconSize()).toBe(64);
  });

  it('should display initials for avatar variant', () => {
    fixture.componentRef.setInput('variant', 'avatar');
    fixture.componentRef.setInput('initials', 'JD');
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const initials = compiled.querySelector('.initials');
    expect(initials?.textContent).toBe('JD');
  });

  it('should render SVG icon when showIcon is true', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('svg');
    expect(svg).toBeTruthy();
    expect(svg?.getAttribute('aria-hidden')).toBe('true');
  });

  it('should not render icon when showIcon is false', () => {
    fixture.componentRef.setInput('showIcon', false);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('svg');
    expect(svg).toBeFalsy();
  });

  it('should have role="img" for accessibility', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('.placeholder');
    expect(placeholder?.getAttribute('role')).toBe('img');
  });

  it('should use data-testid attribute', () => {
    fixture.componentRef.setInput('testId', 'test-placeholder');
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('[data-testid="test-placeholder"]');
    expect(placeholder).toBeTruthy();
  });

  describe('variant icons', () => {
    const variants: PlaceholderVariant[] = ['product', 'category', 'avatar', 'hero', 'brand', 'generic'];
    
    variants.forEach(variant => {
      it(`should render ${variant} variant without errors`, () => {
        fixture.componentRef.setInput('variant', variant);
        fixture.detectChanges();
        
        const compiled = fixture.nativeElement as HTMLElement;
        const placeholder = compiled.querySelector(`.placeholder--${variant}`);
        expect(placeholder).toBeTruthy();
      });
    });
  });

  it('should apply custom width and height', () => {
    fixture.componentRef.setInput('width', '200px');
    fixture.componentRef.setInput('height', '150px');
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('.placeholder') as HTMLElement;
    expect(placeholder.style.width).toBe('200px');
    expect(placeholder.style.height).toBe('150px');
  });

  it('should apply aspect ratio when provided', () => {
    fixture.componentRef.setInput('aspectRatio', '16/9');
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('.placeholder') as HTMLElement;
    expect(placeholder.style.aspectRatio).toBe('16 / 9');
  });
});
