import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { WarrantyCardComponent, WarrantyType } from './warranty-card.component';

describe('WarrantyCardComponent', () => {
  let component: WarrantyCardComponent;
  let fixture: ComponentFixture<WarrantyCardComponent>;

  const card = () => fixture.nativeElement.querySelector('[data-testid="warranty-card"]') as HTMLElement;
  const header = () => fixture.nativeElement.querySelector('.warranty-card__header') as HTMLButtonElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WarrantyCardComponent, RouterTestingModule, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(WarrantyCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Defaults', () => {
    it('should default to 2 years, standard type, collapsed', () => {
      expect(component.years()).toBe(2);
      expect(component.type()).toBe('standard');
      expect(component.expanded()).toBeFalse();
    });

    it('should apply the type modifier class', () => {
      expect(card().classList.contains('warranty-card--standard')).toBeTrue();
    });

    it('should not render the expandable details while collapsed', () => {
      expect(fixture.nativeElement.querySelector('.warranty-card__details')).toBeNull();
    });
  });

  describe('getTypeLabel()', () => {
    const cases: Array<[WarrantyType, string]> = [
      ['standard', 'warranty.manufacturerWarranty'],
      ['extended', 'warranty.extendedWarranty'],
      ['premium', 'warranty.premiumWarranty']
    ];

    cases.forEach(([type, expected]) => {
      it(`should return ${expected} for ${type}`, () => {
        fixture.componentRef.setInput('type', type);
        fixture.detectChanges();
        expect(component.getTypeLabel()).toBe(expected);
      });
    });
  });

  describe('displayFeatures()', () => {
    it('should return 3 default features for standard', () => {
      fixture.componentRef.setInput('type', 'standard');
      fixture.detectChanges();
      expect(component.displayFeatures().length).toBe(3);
    });

    it('should return 4 default features for extended', () => {
      fixture.componentRef.setInput('type', 'extended');
      fixture.detectChanges();
      expect(component.displayFeatures()).toContain('warranty.features.accidentalDamage');
      expect(component.displayFeatures().length).toBe(4);
    });

    it('should return 6 default features for premium', () => {
      fixture.componentRef.setInput('type', 'premium');
      fixture.detectChanges();
      expect(component.displayFeatures().length).toBe(6);
    });

    it('should use custom features when provided (overrides defaults)', () => {
      const custom = ['Parts', 'Labor', 'Compressor'];
      fixture.componentRef.setInput('features', custom);
      fixture.detectChanges();
      expect(component.displayFeatures()).toEqual(custom);
    });

    it('should fall back to defaults when custom features is an empty array', () => {
      fixture.componentRef.setInput('features', []);
      fixture.componentRef.setInput('type', 'standard');
      fixture.detectChanges();
      expect(component.displayFeatures().length).toBe(3);
    });
  });

  describe('Expand / collapse', () => {
    it('toggleExpanded() should flip the expanded signal', () => {
      expect(component.expanded()).toBeFalse();
      component.toggleExpanded();
      expect(component.expanded()).toBeTrue();
      component.toggleExpanded();
      expect(component.expanded()).toBeFalse();
    });

    it('should expand when the header is clicked and render details', () => {
      header().click();
      fixture.detectChanges();
      expect(component.expanded()).toBeTrue();
      expect(fixture.nativeElement.querySelector('.warranty-card__details')).toBeTruthy();
    });

    it('should reflect expanded state in aria-expanded', () => {
      expect(header().getAttribute('aria-expanded')).toBe('false');
      header().click();
      fixture.detectChanges();
      expect(header().getAttribute('aria-expanded')).toBe('true');
    });

    it('should render one coverage item per feature when expanded', () => {
      fixture.componentRef.setInput('type', 'premium');
      component.toggleExpanded();
      fixture.detectChanges();
      const items = fixture.nativeElement.querySelectorAll('.warranty-card__coverage-item');
      expect(items.length).toBe(6);
    });
  });

  describe('Badge', () => {
    it('should NOT render the badge for the standard type', () => {
      expect(fixture.nativeElement.querySelector('.warranty-card__badge')).toBeNull();
    });

    it('should render an extended badge for the extended type', () => {
      fixture.componentRef.setInput('type', 'extended');
      fixture.detectChanges();
      const badge = fixture.nativeElement.querySelector('.warranty-card__badge') as HTMLElement;
      expect(badge).toBeTruthy();
      expect(badge.classList.contains('warranty-card__badge--extended')).toBeTrue();
    });

    it('should render a premium badge for the premium type', () => {
      fixture.componentRef.setInput('type', 'premium');
      fixture.detectChanges();
      const badge = fixture.nativeElement.querySelector('.warranty-card__badge') as HTMLElement;
      expect(badge.classList.contains('warranty-card__badge--premium')).toBeTrue();
    });
  });

  describe('Duration label', () => {
    it('should render the years value in the header', () => {
      fixture.componentRef.setInput('years', 5);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.warranty-card__duration').textContent).toContain('5');
    });
  });

  describe('Terms link', () => {
    it('should render the terms link when expanded and showTermsLink is true (default)', () => {
      component.toggleExpanded();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="warranty-terms-link"]')).toBeTruthy();
    });

    it('should hide the terms link when showTermsLink is false', () => {
      fixture.componentRef.setInput('showTermsLink', false);
      component.toggleExpanded();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="warranty-terms-link"]')).toBeNull();
    });
  });
});
