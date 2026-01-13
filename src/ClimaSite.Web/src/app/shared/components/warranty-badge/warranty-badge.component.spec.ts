import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WarrantyBadgeComponent } from './warranty-badge.component';
import { TranslateModule } from '@ngx-translate/core';

describe('WarrantyBadgeComponent', () => {
  let component: WarrantyBadgeComponent;
  let fixture: ComponentFixture<WarrantyBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WarrantyBadgeComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(WarrantyBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display warranty badge when warrantyMonths is set', () => {
    fixture.componentRef.setInput('warrantyMonths', 24);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const warrantyBadge = compiled.querySelector('[data-testid="warranty-badge"]');
    expect(warrantyBadge).toBeTruthy();
  });

  it('should not display warranty badge when warrantyMonths is 0', () => {
    fixture.componentRef.setInput('warrantyMonths', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const warrantyBadge = compiled.querySelector('[data-testid="warranty-badge"]');
    expect(warrantyBadge).toBeFalsy();
  });

  it('should format warranty as years when divisible by 12', () => {
    fixture.componentRef.setInput('warrantyMonths', 24);
    fixture.detectChanges();
    expect(component.warrantyDisplay()).toBe('2 years');

    fixture.componentRef.setInput('warrantyMonths', 12);
    fixture.detectChanges();
    expect(component.warrantyDisplay()).toBe('1 year');
  });

  it('should format warranty as months when not divisible by 12', () => {
    fixture.componentRef.setInput('warrantyMonths', 18);
    fixture.detectChanges();
    expect(component.warrantyDisplay()).toBe('18 months');
  });

  it('should display return policy badge by default', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const returnBadge = compiled.querySelector('[data-testid="return-badge"]');
    expect(returnBadge).toBeTruthy();
  });

  it('should hide return policy badge when showReturnPolicy is false', () => {
    fixture.componentRef.setInput('showReturnPolicy', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const returnBadge = compiled.querySelector('[data-testid="return-badge"]');
    expect(returnBadge).toBeFalsy();
  });

  it('should display free shipping badge when freeShipping is true', () => {
    fixture.componentRef.setInput('freeShipping', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const shippingBadge = compiled.querySelector('[data-testid="shipping-badge"]');
    expect(shippingBadge).toBeTruthy();
  });

  it('should not display free shipping badge by default', () => {
    const compiled = fixture.nativeElement;
    const shippingBadge = compiled.querySelector('[data-testid="shipping-badge"]');
    expect(shippingBadge).toBeFalsy();
  });

  it('should display stock badge when inStock is true', () => {
    fixture.componentRef.setInput('inStock', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const stockBadge = compiled.querySelector('[data-testid="stock-badge"]');
    expect(stockBadge).toBeTruthy();
  });

  it('should not display stock badge when inStock is false', () => {
    fixture.componentRef.setInput('inStock', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const stockBadge = compiled.querySelector('[data-testid="stock-badge"]');
    expect(stockBadge).toBeFalsy();
  });

  it('should display installation badge when installationAvailable is true', () => {
    fixture.componentRef.setInput('installationAvailable', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const installationBadge = compiled.querySelector('[data-testid="installation-badge"]');
    expect(installationBadge).toBeTruthy();
  });

  it('should use custom return days when provided', () => {
    fixture.componentRef.setInput('returnDays', 14);
    fixture.detectChanges();
    expect(component.returnDays()).toBe(14);
  });

  it('should default to 30 return days', () => {
    expect(component.returnDays()).toBe(30);
  });
});
