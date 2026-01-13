import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FinancingCalculatorComponent, FinancingOption } from './financing-calculator.component';
import { TranslateModule } from '@ngx-translate/core';

describe('FinancingCalculatorComponent', () => {
  let component: FinancingCalculatorComponent;
  let fixture: ComponentFixture<FinancingCalculatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FinancingCalculatorComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FinancingCalculatorComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('price', 1200);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display financing calculator', () => {
    const compiled = fixture.nativeElement;
    const calculator = compiled.querySelector('[data-testid="financing-calculator"]');
    expect(calculator).toBeTruthy();
  });

  it('should display default financing options', () => {
    const compiled = fixture.nativeElement;
    const options = compiled.querySelectorAll('.option-btn');
    expect(options.length).toBe(4); // 6, 12, 24, 36 months
  });

  it('should calculate correct monthly payment for 0% interest', () => {
    // Price: 1200, 6 months, 0% = 200/month
    component.selectOption({ months: 6, interestRate: 0 });
    fixture.detectChanges();

    expect(component.monthlyPayment()).toBe(200);
    expect(component.totalCost()).toBe(1200);
    expect(component.interestCost()).toBe(0);
  });

  it('should calculate correct monthly payment for 12 months 0%', () => {
    // Price: 1200, 12 months, 0% = 100/month
    component.selectOption({ months: 12, interestRate: 0 });
    fixture.detectChanges();

    expect(component.monthlyPayment()).toBe(100);
    expect(component.totalCost()).toBe(1200);
  });

  it('should calculate monthly payment with interest', () => {
    // Price: 1200, 24 months, 9.9% APR
    component.selectOption({ months: 24, interestRate: 9.9 });
    fixture.detectChanges();

    const monthly = component.monthlyPayment();
    // Expected approximately 55.08/month
    expect(monthly).toBeGreaterThan(54);
    expect(monthly).toBeLessThan(56);

    // Total should be more than principal
    expect(component.totalCost()).toBeGreaterThan(1200);
    expect(component.interestCost()).toBeGreaterThan(0);
  });

  it('should show zero interest badge when available', () => {
    expect(component.hasZeroInterestOption()).toBeTrue();

    const compiled = fixture.nativeElement;
    const badge = compiled.querySelector('.promo-badge');
    expect(badge).toBeTruthy();
  });

  it('should not show zero interest badge when not available', () => {
    const noZeroOptions: FinancingOption[] = [
      { months: 12, interestRate: 5 },
      { months: 24, interestRate: 10 }
    ];
    fixture.componentRef.setInput('options', noZeroOptions);
    fixture.detectChanges();

    expect(component.hasZeroInterestOption()).toBeFalse();
  });

  it('should select option when clicked', () => {
    const compiled = fixture.nativeElement;
    const option24 = compiled.querySelector('[data-testid="financing-option-24"]');
    option24.click();
    fixture.detectChanges();

    expect(component.selectedOption().months).toBe(24);
  });

  it('should highlight selected option', () => {
    const compiled = fixture.nativeElement;
    const option = compiled.querySelector('.option-btn.selected');
    expect(option).toBeTruthy();
  });

  it('should calculate lowest monthly payment', () => {
    // With 1200 price and 36 months, lowest should be for longest term
    const lowest = component.lowestMonthlyPayment();
    // 36 months with 12.9% should give approximately 40/month
    expect(lowest).toBeGreaterThan(35);
    expect(lowest).toBeLessThan(45);
  });

  it('should use custom options when provided', () => {
    const customOptions: FinancingOption[] = [
      { months: 3, interestRate: 0 },
      { months: 6, interestRate: 5 }
    ];
    fixture.componentRef.setInput('options', customOptions);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const options = compiled.querySelectorAll('.option-btn');
    expect(options.length).toBe(2);
  });

  it('should display monthly payment in calculation result', () => {
    const compiled = fixture.nativeElement;
    const result = compiled.querySelector('.calculation-result');
    expect(result).toBeTruthy();
  });

  it('should update calculations when price changes', () => {
    const initialMonthly = component.monthlyPayment();

    fixture.componentRef.setInput('price', 2400);
    fixture.detectChanges();

    // Should double with same options
    expect(component.monthlyPayment()).toBeCloseTo(initialMonthly * 2, 0);
  });

  it('should show interest cost row only when interest > 0', () => {
    // First with 0% interest
    component.selectOption({ months: 6, interestRate: 0 });
    fixture.detectChanges();

    let compiled = fixture.nativeElement;
    let interestRow = compiled.querySelector('.result-row.interest');
    expect(interestRow).toBeFalsy();

    // Now with interest
    component.selectOption({ months: 24, interestRate: 9.9 });
    fixture.detectChanges();

    compiled = fixture.nativeElement;
    interestRow = compiled.querySelector('.result-row.interest');
    expect(interestRow).toBeTruthy();
  });
});
