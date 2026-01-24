import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { TrustBadgeComponent } from './trust-badge.component';
import { TrustBadgeStripComponent, TrustBadgeItem } from './trust-badge-strip.component';
import { WarrantyCardComponent } from './warranty-card.component';
import { provideRouter } from '@angular/router';
import { LucideAngularModule, ShieldCheck, Truck, BadgeCheck, Headphones, Award, CheckCircle, ChevronUp, ChevronDown, Check, ExternalLink } from 'lucide-angular';

describe('TrustBadgeComponent', () => {
  let component: TrustBadgeComponent;
  let fixture: ComponentFixture<TrustBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TrustBadgeComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick({ ShieldCheck, Truck, BadgeCheck, Headphones, Award, CheckCircle })
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TrustBadgeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render with default security variant', () => {
    fixture.componentRef.setInput('title', 'trust.secureCheckout');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--security']).toBeTruthy();
    expect(badge.classes['trust-badge--md']).toBeTruthy();
  });

  it('should render with shipping variant', () => {
    fixture.componentRef.setInput('variant', 'shipping');
    fixture.componentRef.setInput('title', 'trust.freeShipping');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--shipping']).toBeTruthy();
  });

  it('should render with warranty variant', () => {
    fixture.componentRef.setInput('variant', 'warranty');
    fixture.componentRef.setInput('title', 'trust.warranty');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--warranty']).toBeTruthy();
  });

  it('should render with support variant', () => {
    fixture.componentRef.setInput('variant', 'support');
    fixture.componentRef.setInput('title', 'trust.support');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--support']).toBeTruthy();
  });

  it('should render with certification variant', () => {
    fixture.componentRef.setInput('variant', 'certification');
    fixture.componentRef.setInput('title', 'trust.certification');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--certification']).toBeTruthy();
  });

  it('should render with guarantee variant', () => {
    fixture.componentRef.setInput('variant', 'guarantee');
    fixture.componentRef.setInput('title', 'trust.guarantee');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--guarantee']).toBeTruthy();
  });

  it('should render in small size', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.componentRef.setInput('size', 'sm');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--sm']).toBeTruthy();
  });

  it('should render in large size', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.trust-badge'));
    expect(badge.classes['trust-badge--lg']).toBeTruthy();
  });

  it('should display description when provided', () => {
    fixture.componentRef.setInput('title', 'trust.freeShipping');
    fixture.componentRef.setInput('description', 'trust.freeShippingDesc');
    fixture.detectChanges();

    const description = fixture.debugElement.query(By.css('.trust-badge__description'));
    expect(description).toBeTruthy();
  });

  it('should not display description when not provided', () => {
    fixture.componentRef.setInput('title', 'trust.secureCheckout');
    fixture.detectChanges();

    const description = fixture.debugElement.query(By.css('.trust-badge__description'));
    expect(description).toBeFalsy();
  });

  it('should use custom icon when provided', () => {
    fixture.componentRef.setInput('title', 'test.title');
    // Use truck icon which is registered
    fixture.componentRef.setInput('icon', 'truck');
    fixture.detectChanges();

    expect(component.displayIcon()).toBe('truck');
  });

  it('should use default icon based on variant', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.componentRef.setInput('variant', 'security');
    fixture.detectChanges();

    expect(component.displayIcon()).toBe('shield-check');
  });

  it('should have correct data-testid attribute', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.componentRef.setInput('variant', 'shipping');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('[data-testid="trust-badge-shipping"]'));
    expect(badge).toBeTruthy();
  });

  it('should compute correct icon size based on badge size', () => {
    fixture.componentRef.setInput('title', 'test.title');
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    expect(component.iconSize()).toBe('lg');
  });
});

describe('TrustBadgeStripComponent', () => {
  let component: TrustBadgeStripComponent;
  let fixture: ComponentFixture<TrustBadgeStripComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TrustBadgeStripComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick({ ShieldCheck, Truck, BadgeCheck, Headphones, Award, CheckCircle })
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TrustBadgeStripComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display default badges', () => {
    fixture.detectChanges();

    const badges = fixture.debugElement.queryAll(By.directive(TrustBadgeComponent));
    expect(badges.length).toBe(4);
  });

  it('should display custom badges when provided', () => {
    const customBadges: TrustBadgeItem[] = [
      { variant: 'security', title: 'custom.security' },
      { variant: 'shipping', title: 'custom.shipping' }
    ];
    fixture.componentRef.setInput('badges', customBadges);
    fixture.detectChanges();

    const badges = fixture.debugElement.queryAll(By.directive(TrustBadgeComponent));
    expect(badges.length).toBe(2);
  });

  it('should render in compact mode', () => {
    fixture.componentRef.setInput('compact', true);
    fixture.detectChanges();

    const strip = fixture.debugElement.query(By.css('.trust-strip'));
    expect(strip.classes['trust-strip--compact']).toBeTruthy();
  });

  it('should render centered', () => {
    fixture.componentRef.setInput('centered', true);
    fixture.detectChanges();

    const strip = fixture.debugElement.query(By.css('.trust-strip'));
    expect(strip.classes['trust-strip--centered']).toBeTruthy();
  });

  it('should have correct aria-label', () => {
    fixture.detectChanges();

    const strip = fixture.debugElement.query(By.css('.trust-strip'));
    expect(strip.attributes['aria-label']).toBeDefined();
  });

  it('should have correct data-testid', () => {
    fixture.detectChanges();

    const strip = fixture.debugElement.query(By.css('[data-testid="trust-badge-strip"]'));
    expect(strip).toBeTruthy();
  });
});

describe('WarrantyCardComponent', () => {
  let component: WarrantyCardComponent;
  let fixture: ComponentFixture<WarrantyCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        WarrantyCardComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick({ ShieldCheck, ChevronUp, ChevronDown, Check, ExternalLink })
      ],
      providers: [
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WarrantyCardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display default 2 year warranty', () => {
    fixture.detectChanges();
    expect(component.years()).toBe(2);
  });

  it('should display custom warranty years', () => {
    fixture.componentRef.setInput('years', 5);
    fixture.detectChanges();
    expect(component.years()).toBe(5);
  });

  it('should render standard warranty type by default', () => {
    fixture.detectChanges();

    expect(component.type()).toBe('standard');
    expect(component.getTypeLabel()).toBe('warranty.manufacturerWarranty');
  });

  it('should render extended warranty type', () => {
    fixture.componentRef.setInput('type', 'extended');
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.css('.warranty-card'));
    expect(card.classes['warranty-card--extended']).toBeTruthy();
    expect(component.getTypeLabel()).toBe('warranty.extendedWarranty');
  });

  it('should render premium warranty type', () => {
    fixture.componentRef.setInput('type', 'premium');
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.css('.warranty-card'));
    expect(card.classes['warranty-card--premium']).toBeTruthy();
    expect(component.getTypeLabel()).toBe('warranty.premiumWarranty');
  });

  it('should start collapsed', () => {
    fixture.detectChanges();
    expect(component.expanded()).toBe(false);
  });

  it('should toggle expanded state on click', () => {
    fixture.detectChanges();
    
    const header = fixture.debugElement.query(By.css('.warranty-card__header'));
    header.triggerEventHandler('click', null);
    fixture.detectChanges();

    expect(component.expanded()).toBe(true);

    header.triggerEventHandler('click', null);
    fixture.detectChanges();

    expect(component.expanded()).toBe(false);
  });

  it('should show details when expanded', () => {
    fixture.detectChanges();

    let details = fixture.debugElement.query(By.css('.warranty-card__details'));
    expect(details).toBeFalsy();

    component.toggleExpanded();
    fixture.detectChanges();

    details = fixture.debugElement.query(By.css('.warranty-card__details'));
    expect(details).toBeTruthy();
  });

  it('should display default features for standard warranty', () => {
    fixture.detectChanges();
    
    const features = component.displayFeatures();
    expect(features.length).toBe(3);
    expect(features).toContain('warranty.features.parts');
    expect(features).toContain('warranty.features.labor');
  });

  it('should display more features for premium warranty', () => {
    fixture.componentRef.setInput('type', 'premium');
    fixture.detectChanges();

    const features = component.displayFeatures();
    expect(features.length).toBe(6);
    expect(features).toContain('warranty.features.prioritySupport');
  });

  it('should display custom features when provided', () => {
    const customFeatures = ['custom.feature1', 'custom.feature2'];
    fixture.componentRef.setInput('features', customFeatures);
    fixture.detectChanges();

    const features = component.displayFeatures();
    expect(features).toEqual(customFeatures);
  });

  it('should show warranty badge for extended type', () => {
    fixture.componentRef.setInput('type', 'extended');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.warranty-card__badge'));
    expect(badge).toBeTruthy();
    expect(badge.classes['warranty-card__badge--extended']).toBeTruthy();
  });

  it('should show warranty badge for premium type', () => {
    fixture.componentRef.setInput('type', 'premium');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.warranty-card__badge'));
    expect(badge).toBeTruthy();
    expect(badge.classes['warranty-card__badge--premium']).toBeTruthy();
  });

  it('should not show badge for standard type', () => {
    fixture.componentRef.setInput('type', 'standard');
    fixture.detectChanges();

    const badge = fixture.debugElement.query(By.css('.warranty-card__badge'));
    expect(badge).toBeFalsy();
  });

  it('should have correct aria-expanded attribute', () => {
    fixture.detectChanges();

    const header = fixture.debugElement.query(By.css('.warranty-card__header'));
    expect(header.attributes['aria-expanded']).toBe('false');

    component.toggleExpanded();
    fixture.detectChanges();

    expect(header.attributes['aria-expanded']).toBe('true');
  });

  it('should show terms link by default when expanded', () => {
    fixture.detectChanges();
    component.toggleExpanded();
    fixture.detectChanges();

    const termsLink = fixture.debugElement.query(By.css('[data-testid="warranty-terms-link"]'));
    expect(termsLink).toBeTruthy();
  });

  it('should hide terms link when showTermsLink is false', () => {
    fixture.componentRef.setInput('showTermsLink', false);
    fixture.detectChanges();
    component.toggleExpanded();
    fixture.detectChanges();

    const termsLink = fixture.debugElement.query(By.css('[data-testid="warranty-terms-link"]'));
    expect(termsLink).toBeFalsy();
  });

  it('should have correct data-testid', () => {
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.css('[data-testid="warranty-card"]'));
    expect(card).toBeTruthy();
  });
});
