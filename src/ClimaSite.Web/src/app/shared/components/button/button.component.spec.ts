import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Component } from '@angular/core';
import { ButtonComponent, ButtonVariant, ButtonSize } from './button.component';

// Test host component for content projection tests
@Component({
  standalone: true,
  imports: [ButtonComponent],
  template: `
    <app-button 
      [variant]="variant" 
      [size]="size" 
      [disabled]="disabled"
      [loading]="loading"
      [fullWidth]="fullWidth"
      [iconOnly]="iconOnly"
      [iconLeft]="iconLeft"
      [iconRight]="iconRight"
      [testId]="testId"
      (clicked)="onClick($event)"
    >
      @if (iconLeft) {
        <span icon-left>L</span>
      }
      {{ buttonText }}
      @if (iconRight) {
        <span icon-right>R</span>
      }
    </app-button>
  `
})
class TestHostComponent {
  variant: ButtonVariant = 'primary';
  size: ButtonSize = 'md';
  disabled = false;
  loading = false;
  fullWidth = false;
  iconOnly = false;
  iconLeft = false;
  iconRight = false;
  testId = 'test-button';
  buttonText = 'Click me';
  clickEvent: MouseEvent | null = null;

  onClick(event: MouseEvent): void {
    this.clickEvent = event;
  }
}

describe('ButtonComponent', () => {
  let component: ButtonComponent;
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ==========================================================================
  // BASIC TESTS
  // ==========================================================================

  describe('Basic Rendering', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should render a button element', () => {
      const button = fixture.nativeElement.querySelector('button');
      expect(button).toBeTruthy();
    });

    it('should have default variant as primary', () => {
      expect(component.variant()).toBe('primary');
    });

    it('should have default size as md', () => {
      expect(component.size()).toBe('md');
    });

    it('should have default type as button', () => {
      expect(component.type()).toBe('button');
      const button = fixture.nativeElement.querySelector('button');
      expect(button.type).toBe('button');
    });

    it('should not be disabled by default', () => {
      expect(component.disabled()).toBeFalse();
      const button = fixture.nativeElement.querySelector('button');
      expect(button.disabled).toBeFalse();
    });

    it('should not be loading by default', () => {
      expect(component.loading()).toBeFalse();
    });
  });

  // ==========================================================================
  // VARIANT TESTS
  // ==========================================================================

  describe('Variants', () => {
    const variants: ButtonVariant[] = [
      'primary', 'secondary', 'outline', 'ghost', 
      'destructive', 'success', 'link', 'glass', 'warm'
    ];

    variants.forEach(variant => {
      it(`should apply correct class for ${variant} variant`, () => {
        fixture.componentRef.setInput('variant', variant);
        fixture.detectChanges();

        const button = fixture.nativeElement.querySelector('button');
        const expectedClass = variant === 'danger' ? 'btn-destructive' : `btn-${variant}`;
        expect(button.classList.contains(expectedClass)).toBeTrue();
      });
    });

    it('should map legacy "danger" variant to "destructive"', () => {
      fixture.componentRef.setInput('variant', 'danger');
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-destructive')).toBeTrue();
    });
  });

  // ==========================================================================
  // SIZE TESTS
  // ==========================================================================

  describe('Sizes', () => {
    const sizes: ButtonSize[] = ['xs', 'sm', 'md', 'lg', 'xl'];

    sizes.forEach(size => {
      it(`should apply correct class for ${size} size`, () => {
        fixture.componentRef.setInput('size', size);
        fixture.detectChanges();

        const button = fixture.nativeElement.querySelector('button');
        expect(button.classList.contains(`btn-${size}`)).toBeTrue();
      });
    });
  });

  // ==========================================================================
  // DISABLED STATE
  // ==========================================================================

  describe('Disabled State', () => {
    it('should be disabled when disabled input is true', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.disabled).toBeTrue();
    });

    it('should have aria-disabled when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.getAttribute('aria-disabled')).toBe('true');
    });

    it('should not emit click event when disabled', () => {
      fixture.componentRef.setInput('disabled', true);
      fixture.detectChanges();

      spyOn(component.clicked, 'emit');
      const button = fixture.nativeElement.querySelector('button');
      button.click();
      expect(component.clicked.emit).not.toHaveBeenCalled();
    });
  });

  // ==========================================================================
  // LOADING STATE
  // ==========================================================================

  describe('Loading State', () => {
    it('should show spinner when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const spinner = fixture.nativeElement.querySelector('.btn-spinner');
      expect(spinner).toBeTruthy();
    });

    it('should not show spinner when not loading', () => {
      const spinner = fixture.nativeElement.querySelector('.btn-spinner');
      expect(spinner).toBeFalsy();
    });

    it('should be disabled when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.disabled).toBeTrue();
    });

    it('should have aria-busy when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.getAttribute('aria-busy')).toBe('true');
    });

    it('should not emit click event when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      spyOn(component.clicked, 'emit');
      const button = fixture.nativeElement.querySelector('button');
      button.click();
      expect(component.clicked.emit).not.toHaveBeenCalled();
    });

    it('should add btn-loading class when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-loading')).toBeTrue();
    });

    it('should hide content visually when loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const content = fixture.nativeElement.querySelector('.btn-content');
      expect(content.classList.contains('btn-content-hidden')).toBeTrue();
    });
  });

  // ==========================================================================
  // CLICK EVENT
  // ==========================================================================

  describe('Click Event', () => {
    it('should emit clicked event when clicked', () => {
      spyOn(component.clicked, 'emit');
      const button = fixture.nativeElement.querySelector('button');
      button.click();
      expect(component.clicked.emit).toHaveBeenCalled();
    });

    it('should emit MouseEvent when clicked', () => {
      let emittedEvent: MouseEvent | null = null;
      component.clicked.subscribe((event: MouseEvent) => {
        emittedEvent = event;
      });

      const button = fixture.nativeElement.querySelector('button');
      button.click();

      expect(emittedEvent).toBeInstanceOf(MouseEvent);
    });
  });

  // ==========================================================================
  // FULL WIDTH
  // ==========================================================================

  describe('Full Width', () => {
    it('should apply full width class when fullWidth is true', () => {
      fixture.componentRef.setInput('fullWidth', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-full-width')).toBeTrue();
    });

    it('should not have full width class by default', () => {
      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-full-width')).toBeFalse();
    });
  });

  // ==========================================================================
  // ICON ONLY
  // ==========================================================================

  describe('Icon Only', () => {
    it('should apply icon-only class when iconOnly is true', () => {
      fixture.componentRef.setInput('iconOnly', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-icon-only')).toBeTrue();
    });
  });

  // ==========================================================================
  // TEST ID
  // ==========================================================================

  describe('Test ID', () => {
    it('should have default test id', () => {
      const button = fixture.nativeElement.querySelector('button');
      expect(button.getAttribute('data-testid')).toBe('button');
    });

    it('should have custom test id when provided', () => {
      fixture.componentRef.setInput('testId', 'submit-button');
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.getAttribute('data-testid')).toBe('submit-button');
    });
  });

  // ==========================================================================
  // BUTTON TYPE
  // ==========================================================================

  describe('Button Type', () => {
    it('should have type="button" by default', () => {
      const button = fixture.nativeElement.querySelector('button');
      expect(button.type).toBe('button');
    });

    it('should have type="submit" when set', () => {
      fixture.componentRef.setInput('type', 'submit');
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.type).toBe('submit');
    });

    it('should have type="reset" when set', () => {
      fixture.componentRef.setInput('type', 'reset');
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.type).toBe('reset');
    });
  });

  // ==========================================================================
  // MAGNETIC EFFECT
  // ==========================================================================

  describe('Magnetic Effect', () => {
    it('should apply magnetic class when magnetic is true', () => {
      fixture.componentRef.setInput('magnetic', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button');
      expect(button.classList.contains('btn-magnetic')).toBeTrue();
    });
  });
});

// ==========================================================================
// HOST COMPONENT TESTS
// ==========================================================================

describe('ButtonComponent with Host', () => {
  let hostComponent: TestHostComponent;
  let hostFixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, ButtonComponent]
    }).compileComponents();

    hostFixture = TestBed.createComponent(TestHostComponent);
    hostComponent = hostFixture.componentInstance;
    hostFixture.detectChanges();
  });

  it('should render button text content', () => {
    const button = hostFixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Click me');
  });

  it('should render left icon when iconLeft is true', () => {
    hostComponent.iconLeft = true;
    hostFixture.detectChanges();

    const iconLeft = hostFixture.nativeElement.querySelector('.btn-icon-left');
    expect(iconLeft).toBeTruthy();
    expect(iconLeft.textContent).toContain('L');
  });

  it('should render right icon when iconRight is true', () => {
    hostComponent.iconRight = true;
    hostFixture.detectChanges();

    const iconRight = hostFixture.nativeElement.querySelector('.btn-icon-right');
    expect(iconRight).toBeTruthy();
    expect(iconRight.textContent).toContain('R');
  });

  it('should hide left icon when loading', () => {
    hostComponent.iconLeft = true;
    hostComponent.loading = true;
    hostFixture.detectChanges();

    const iconLeft = hostFixture.nativeElement.querySelector('.btn-icon-left');
    expect(iconLeft).toBeFalsy();
  });

  it('should pass click event to host', () => {
    const button = hostFixture.nativeElement.querySelector('button');
    button.click();

    expect(hostComponent.clickEvent).toBeTruthy();
    expect(hostComponent.clickEvent).toBeInstanceOf(MouseEvent);
  });

  it('should not emit click when disabled', () => {
    hostComponent.disabled = true;
    hostFixture.detectChanges();

    const button = hostFixture.nativeElement.querySelector('button');
    button.click();

    expect(hostComponent.clickEvent).toBeNull();
  });
});
