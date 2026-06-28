import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { TrustBadgeStripComponent, TrustBadgeItem } from './trust-badge-strip.component';

describe('TrustBadgeStripComponent', () => {
  let component: TrustBadgeStripComponent;
  let fixture: ComponentFixture<TrustBadgeStripComponent>;

  const strip = () => fixture.nativeElement.querySelector('[data-testid="trust-badge-strip"]') as HTMLElement;
  const badges = () => fixture.nativeElement.querySelectorAll('app-trust-badge');

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrustBadgeStripComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(TrustBadgeStripComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the strip container with its testid', () => {
    expect(strip()).toBeTruthy();
  });

  describe('Default badges', () => {
    it('displayBadges() should return the four default badges', () => {
      const result = component.displayBadges();
      expect(result.length).toBe(4);
      expect(result.map(b => b.variant)).toEqual(['security', 'shipping', 'warranty', 'support']);
    });

    it('should render one app-trust-badge per default badge', () => {
      expect(badges().length).toBe(4);
    });
  });

  describe('Custom badges', () => {
    const customBadges: TrustBadgeItem[] = [
      { variant: 'guarantee', title: 'trust.moneyBack', description: 'trust.thirtyDays' },
      { variant: 'certification', title: 'trust.certified' }
    ];

    it('displayBadges() should return the custom badges when provided', () => {
      fixture.componentRef.setInput('badges', customBadges);
      fixture.detectChanges();
      expect(component.displayBadges()).toEqual(customBadges);
    });

    it('should render one app-trust-badge per custom badge', () => {
      fixture.componentRef.setInput('badges', customBadges);
      fixture.detectChanges();
      expect(badges().length).toBe(2);
    });

    it('should fall back to defaults when an empty-ish (undefined) badges input is used', () => {
      // undefined input -> defaults
      expect(component.displayBadges().length).toBe(4);
    });
  });

  describe('Layout modifiers', () => {
    it('should not apply compact or centered classes by default', () => {
      expect(strip().classList.contains('trust-strip--compact')).toBeFalse();
      expect(strip().classList.contains('trust-strip--centered')).toBeFalse();
    });

    it('should apply the compact class when compact is true', () => {
      fixture.componentRef.setInput('compact', true);
      fixture.detectChanges();
      expect(strip().classList.contains('trust-strip--compact')).toBeTrue();
    });

    it('should apply the centered class when centered is true', () => {
      fixture.componentRef.setInput('centered', true);
      fixture.detectChanges();
      expect(strip().classList.contains('trust-strip--centered')).toBeTrue();
    });
  });

  describe('Compact propagation to badges', () => {
    it('should pass size "md" to badges in normal mode', () => {
      const badge = fixture.debugElement.nativeElement.querySelector('app-trust-badge');
      // The first default badge renders at md and shows its description container in normal mode.
      expect(badge.querySelector('.trust-badge--md')).toBeTruthy();
    });

    it('should pass size "sm" to badges in compact mode', () => {
      fixture.componentRef.setInput('compact', true);
      fixture.detectChanges();
      const badge = fixture.nativeElement.querySelector('app-trust-badge');
      expect(badge.querySelector('.trust-badge--sm')).toBeTruthy();
    });

    it('should suppress badge descriptions in compact mode', () => {
      fixture.componentRef.setInput('compact', true);
      fixture.detectChanges();
      // Empty description input means the description span is not rendered by trust-badge.
      const descriptions = fixture.nativeElement.querySelectorAll('.trust-badge__description');
      expect(descriptions.length).toBe(0);
    });
  });
});
