import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { GlassCardComponent, GlassVariant } from './glass-card.component';

describe('GlassCardComponent', () => {
  let component: GlassCardComponent;
  let fixture: ComponentFixture<GlassCardComponent>;

  const cardEl = () => fixture.nativeElement.querySelector('.glass-card') as HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlassCardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(GlassCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the glass-card container', () => {
    expect(cardEl()).toBeTruthy();
  });

  describe('Variants', () => {
    const variants: GlassVariant[] = ['default', 'light', 'heavy', 'primary', 'accent', 'warm'];

    it('should default to the "default" variant', () => {
      expect(component.variant()).toBe('default');
      expect(cardEl().classList.contains('variant-default')).toBeTrue();
    });

    variants.forEach(variant => {
      it(`should apply the variant-${variant} class`, () => {
        fixture.componentRef.setInput('variant', variant);
        fixture.detectChanges();
        expect(cardEl().classList.contains(`variant-${variant}`)).toBeTrue();
      });
    });
  });

  describe('Modifier flags', () => {
    it('should not apply modifier classes by default', () => {
      const el = cardEl();
      expect(el.classList.contains('hoverable')).toBeFalse();
      expect(el.classList.contains('has-glow')).toBeFalse();
      expect(el.classList.contains('has-border-gradient')).toBeFalse();
    });

    it('should add hoverable class when hoverable is true', () => {
      fixture.componentRef.setInput('hoverable', true);
      fixture.detectChanges();
      expect(cardEl().classList.contains('hoverable')).toBeTrue();
    });

    it('should add has-glow class when glow is true', () => {
      fixture.componentRef.setInput('glow', true);
      fixture.detectChanges();
      expect(cardEl().classList.contains('has-glow')).toBeTrue();
    });

    it('should add has-border-gradient class when borderGradient is true', () => {
      fixture.componentRef.setInput('borderGradient', true);
      fixture.detectChanges();
      expect(cardEl().classList.contains('has-border-gradient')).toBeTrue();
    });
  });

  describe('Header / Footer sections', () => {
    it('should not render header or footer sections by default', () => {
      expect(fixture.nativeElement.querySelector('.glass-card-header')).toBeNull();
      expect(fixture.nativeElement.querySelector('.glass-card-footer')).toBeNull();
    });

    it('should render header section when header is true', () => {
      fixture.componentRef.setInput('header', true);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.glass-card-header')).toBeTruthy();
    });

    it('should render footer section when footer is true', () => {
      fixture.componentRef.setInput('footer', true);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.glass-card-footer')).toBeTruthy();
    });

    it('should always render the body', () => {
      expect(fixture.nativeElement.querySelector('.glass-card-body')).toBeTruthy();
    });

    it('should add no-padding class to body when noPadding is true', () => {
      const body = fixture.nativeElement.querySelector('.glass-card-body') as HTMLElement;
      expect(body.classList.contains('no-padding')).toBeFalse();

      fixture.componentRef.setInput('noPadding', true);
      fixture.detectChanges();
      expect(body.classList.contains('no-padding')).toBeTrue();
    });
  });
});

// ============================================================================
// Content projection (host component)
// ============================================================================

@Component({
  standalone: true,
  imports: [GlassCardComponent],
  template: `
    <app-glass-card [header]="true" [footer]="true">
      <span card-header data-testid="proj-header">My header</span>
      <p data-testid="proj-body">Body content</p>
      <span card-footer data-testid="proj-footer">My footer</span>
    </app-glass-card>
  `
})
class GlassCardHostComponent {}

describe('GlassCardComponent (content projection)', () => {
  let fixture: ComponentFixture<GlassCardHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlassCardHostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(GlassCardHostComponent);
    fixture.detectChanges();
  });

  it('should project header content into the header slot', () => {
    const header = fixture.nativeElement.querySelector('.glass-card-header');
    expect(header.textContent).toContain('My header');
  });

  it('should project default content into the body', () => {
    const body = fixture.nativeElement.querySelector('.glass-card-body');
    expect(body.textContent).toContain('Body content');
  });

  it('should project footer content into the footer slot', () => {
    const footer = fixture.nativeElement.querySelector('.glass-card-footer');
    expect(footer.textContent).toContain('My footer');
  });
});
