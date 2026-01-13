import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StockDeliveryComponent, DeliveryOption } from './stock-delivery.component';
import { TranslateModule } from '@ngx-translate/core';

describe('StockDeliveryComponent', () => {
  let component: StockDeliveryComponent;
  let fixture: ComponentFixture<StockDeliveryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockDeliveryComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(StockDeliveryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show in stock status when stockQuantity > 0', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();
    expect(component.inStock()).toBeTrue();
    expect(component.stockStatusClass()).toBe('in-stock');
  });

  it('should show out of stock status when stockQuantity is 0', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();
    expect(component.inStock()).toBeFalse();
    expect(component.stockStatusClass()).toBe('out-of-stock');
  });

  it('should show low stock status when quantity <= threshold', () => {
    fixture.componentRef.setInput('stockQuantity', 3);
    fixture.componentRef.setInput('lowStockThreshold', 5);
    fixture.detectChanges();
    expect(component.lowStock()).toBeTrue();
    expect(component.stockStatusClass()).toBe('low-stock');
  });

  it('should not show low stock when quantity > threshold', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.componentRef.setInput('lowStockThreshold', 5);
    fixture.detectChanges();
    expect(component.lowStock()).toBeFalse();
    expect(component.stockStatusClass()).toBe('in-stock');
  });

  it('should have default delivery options', () => {
    expect(component.deliveryOptions().length).toBe(2);
    expect(component.deliveryOptions()[0].type).toBe('standard');
    expect(component.deliveryOptions()[1].type).toBe('express');
  });

  it('should calculate delivery estimate string correctly', () => {
    const option: DeliveryOption = {
      type: 'standard',
      name: 'Standard',
      price: 0,
      estimatedDays: { min: 3, max: 5 }
    };
    expect(component.getDeliveryEstimate(option)).toBe('3-5 days');
  });

  it('should show singular day when min equals max and is 1', () => {
    const option: DeliveryOption = {
      type: 'express',
      name: 'Express',
      price: 9.99,
      estimatedDays: { min: 1, max: 1 }
    };
    expect(component.getDeliveryEstimate(option)).toBe('1 day');
  });

  it('should show plural days when min equals max and is > 1', () => {
    const option: DeliveryOption = {
      type: 'standard',
      name: 'Standard',
      price: 0,
      estimatedDays: { min: 3, max: 3 }
    };
    expect(component.getDeliveryEstimate(option)).toBe('3 days');
  });

  it('should calculate estimated delivery date when in stock', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    const estimate = component.estimatedDeliveryDate();
    expect(estimate).toBeTruthy();
    expect(estimate).toContain('-'); // Should contain date range
  });

  it('should return null for estimated delivery date when out of stock', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();

    expect(component.estimatedDeliveryDate()).toBeNull();
  });

  it('should accept custom delivery options', () => {
    const customOptions: DeliveryOption[] = [
      { type: 'pickup', name: 'Store Pickup', price: 0, estimatedDays: { min: 0, max: 0 } }
    ];
    fixture.componentRef.setInput('deliveryOptions', customOptions);
    fixture.detectChanges();

    expect(component.deliveryOptions().length).toBe(1);
    expect(component.deliveryOptions()[0].type).toBe('pickup');
  });

  it('should render delivery options when in stock', () => {
    fixture.componentRef.setInput('stockQuantity', 10);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const options = compiled.querySelectorAll('.delivery-option');
    expect(options.length).toBe(2);
  });

  it('should not render delivery options when out of stock', () => {
    fixture.componentRef.setInput('stockQuantity', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const options = compiled.querySelectorAll('.delivery-option');
    expect(options.length).toBe(0);
  });
});
