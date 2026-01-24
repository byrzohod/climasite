import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import { IconComponent, IconSize, ICON_SIZE_MAP } from './icon.component';
import { ICON_REGISTRY } from './icon-registry';
import { LucideAngularModule } from 'lucide-angular';

// Test host component to test input bindings
@Component({
  standalone: true,
  imports: [IconComponent],
  template: `
    <app-icon 
      [name]="name"
      [size]="size"
      [strokeWidth]="strokeWidth"
      [class]="cssClass"
      [spin]="spin"
      [ariaHidden]="ariaHidden"
      [ariaLabel]="ariaLabel"
      [testId]="testId"
    />
  `
})
class TestHostComponent {
  name = 'shopping-cart';
  size: IconSize = 'md';
  strokeWidth = 2;
  cssClass = '';
  spin = false;
  ariaHidden = true;
  ariaLabel = '';
  testId = 'icon';
}

describe('IconComponent', () => {
  let component: IconComponent;
  let fixture: ComponentFixture<IconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        IconComponent,
        LucideAngularModule.pick(ICON_REGISTRY)
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(IconComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.componentRef.setInput('name', 'shopping-cart');
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should render lucide-icon element', () => {
      fixture.componentRef.setInput('name', 'heart');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon).toBeTruthy();
    });

    it('should pass icon name to lucide-icon', () => {
      fixture.componentRef.setInput('name', 'search');
      fixture.detectChanges();
      
      // Verify the component input is correctly set
      expect(component.name()).toBe('search');
    });
  });

  describe('Size Mapping', () => {
    const sizeTests: { size: IconSize; expectedPx: number }[] = [
      { size: 'xs', expectedPx: 14 },
      { size: 'sm', expectedPx: 16 },
      { size: 'md', expectedPx: 20 },
      { size: 'lg', expectedPx: 24 },
      { size: 'xl', expectedPx: 32 }
    ];

    sizeTests.forEach(({ size, expectedPx }) => {
      it(`should map size '${size}' to ${expectedPx}px`, () => {
        fixture.componentRef.setInput('name', 'user');
        fixture.componentRef.setInput('size', size);
        fixture.detectChanges();
        
        expect(component.computedSize()).toBe(expectedPx);
        expect(ICON_SIZE_MAP[size]).toBe(expectedPx);
      });
    });

    it('should default to md size (20px)', () => {
      fixture.componentRef.setInput('name', 'user');
      fixture.detectChanges();
      
      expect(component.size()).toBe('md');
      expect(component.computedSize()).toBe(20);
    });
  });

  describe('Stroke Width', () => {
    it('should default to stroke width 2', () => {
      fixture.componentRef.setInput('name', 'edit');
      fixture.detectChanges();
      
      expect(component.strokeWidth()).toBe(2);
    });

    it('should accept custom stroke width', () => {
      fixture.componentRef.setInput('name', 'edit');
      fixture.componentRef.setInput('strokeWidth', 1.5);
      fixture.detectChanges();
      
      expect(component.strokeWidth()).toBe(1.5);
    });
  });

  describe('CSS Classes', () => {
    it('should apply custom CSS classes', () => {
      fixture.componentRef.setInput('name', 'star');
      fixture.componentRef.setInput('class', 'text-primary custom-class');
      fixture.detectChanges();
      
      expect(component.iconClasses()).toContain('text-primary');
      expect(component.iconClasses()).toContain('custom-class');
    });

    it('should handle empty class input', () => {
      fixture.componentRef.setInput('name', 'star');
      fixture.componentRef.setInput('class', '');
      fixture.detectChanges();
      
      expect(component.iconClasses()).toBe('');
    });
  });

  describe('Spin Animation', () => {
    it('should not have spin class by default', () => {
      fixture.componentRef.setInput('name', 'loader-2');
      fixture.detectChanges();
      
      expect(component.spin()).toBe(false);
      expect(fixture.nativeElement.classList.contains('spin')).toBe(false);
    });

    it('should add spin class when spin is true', () => {
      fixture.componentRef.setInput('name', 'loader-2');
      fixture.componentRef.setInput('spin', true);
      fixture.detectChanges();
      
      expect(component.spin()).toBe(true);
      expect(fixture.nativeElement.classList.contains('spin')).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should be aria-hidden by default', () => {
      fixture.componentRef.setInput('name', 'info');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['aria-hidden']).toBe('true');
    });

    it('should not have role when aria-hidden', () => {
      fixture.componentRef.setInput('name', 'info');
      fixture.componentRef.setInput('ariaHidden', true);
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['role']).toBeFalsy();
    });

    it('should have role="img" when not aria-hidden', () => {
      fixture.componentRef.setInput('name', 'alert-triangle');
      fixture.componentRef.setInput('ariaHidden', false);
      fixture.componentRef.setInput('ariaLabel', 'Warning');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['role']).toBe('img');
    });

    it('should apply aria-label when not hidden', () => {
      fixture.componentRef.setInput('name', 'check-circle');
      fixture.componentRef.setInput('ariaHidden', false);
      fixture.componentRef.setInput('ariaLabel', 'Success');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['aria-label']).toBe('Success');
    });
  });

  describe('Test ID', () => {
    it('should have default testId of "icon"', () => {
      fixture.componentRef.setInput('name', 'user');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['data-testid']).toBe('icon');
    });

    it('should accept custom testId', () => {
      fixture.componentRef.setInput('name', 'user');
      fixture.componentRef.setInput('testId', 'user-icon');
      fixture.detectChanges();
      
      const lucideIcon = fixture.debugElement.query(By.css('lucide-icon'));
      expect(lucideIcon.attributes['data-testid']).toBe('user-icon');
    });
  });
});

describe('IconComponent with TestHost', () => {
  let hostFixture: ComponentFixture<TestHostComponent>;
  let hostComponent: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        LucideAngularModule.pick(ICON_REGISTRY)
      ]
    }).compileComponents();

    hostFixture = TestBed.createComponent(TestHostComponent);
    hostComponent = hostFixture.componentInstance;
    hostFixture.detectChanges();
  });

  it('should create with host component', () => {
    expect(hostComponent).toBeTruthy();
  });

  it('should update when host inputs change', () => {
    hostComponent.size = 'lg';
    hostFixture.detectChanges();
    
    const iconComponent = hostFixture.debugElement.query(By.directive(IconComponent));
    expect(iconComponent.componentInstance.size()).toBe('lg');
    expect(iconComponent.componentInstance.computedSize()).toBe(24);
  });

  it('should update icon name', () => {
    hostComponent.name = 'heart';
    hostFixture.detectChanges();
    
    const iconComponent = hostFixture.debugElement.query(By.directive(IconComponent));
    expect(iconComponent.componentInstance.name()).toBe('heart');
  });
});

describe('ICON_SIZE_MAP', () => {
  it('should have correct size mappings', () => {
    expect(ICON_SIZE_MAP.xs).toBe(14);
    expect(ICON_SIZE_MAP.sm).toBe(16);
    expect(ICON_SIZE_MAP.md).toBe(20);
    expect(ICON_SIZE_MAP.lg).toBe(24);
    expect(ICON_SIZE_MAP.xl).toBe(32);
  });

  it('should have entries for all IconSize values', () => {
    const sizes: IconSize[] = ['xs', 'sm', 'md', 'lg', 'xl'];
    sizes.forEach(size => {
      expect(ICON_SIZE_MAP[size]).toBeDefined();
      expect(typeof ICON_SIZE_MAP[size]).toBe('number');
    });
  });
});
