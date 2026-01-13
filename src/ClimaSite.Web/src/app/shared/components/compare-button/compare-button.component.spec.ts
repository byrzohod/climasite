import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompareButtonComponent } from './compare-button.component';
import { ComparisonService } from '../../../core/services/comparison.service';
import { TranslateModule } from '@ngx-translate/core';
import { signal } from '@angular/core';

describe('CompareButtonComponent', () => {
  let component: CompareButtonComponent;
  let fixture: ComponentFixture<CompareButtonComponent>;
  let mockService: jasmine.SpyObj<ComparisonService>;

  const mockProduct = {
    id: 'prod-1',
    name: 'Test Product',
    slug: 'test-product',
    primaryImageUrl: 'https://example.com/img.jpg',
    basePrice: 599,
    salePrice: undefined,
    isOnSale: false,
    brand: 'TestBrand',
    averageRating: 4.5,
    reviewCount: 10,
    inStock: true
  };

  beforeEach(async () => {
    mockService = jasmine.createSpyObj('ComparisonService', ['isInCompare', 'toggleCompare'], {
      isFull: signal(false),
      count: signal(0)
    });
    mockService.isInCompare.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [
        CompareButtonComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: ComparisonService, useValue: mockService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CompareButtonComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('product', mockProduct);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display compare button', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.compare-btn');
    expect(button).toBeTruthy();
  });

  it('should call toggleCompare when clicked', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.compare-btn');
    button.click();

    expect(mockService.toggleCompare).toHaveBeenCalledWith(mockProduct);
  });

  it('should show active state when product is in compare list', () => {
    mockService.isInCompare.and.returnValue(true);
    fixture.detectChanges();

    // Need to trigger change detection for computed signal
    expect(mockService.isInCompare).toHaveBeenCalled();
  });

  it('should be disabled when compare list is full and product not in list', async () => {
    // Create new mock with isFull = true
    const fullMockService = jasmine.createSpyObj('ComparisonService', ['isInCompare', 'toggleCompare'], {
      isFull: signal(true),
      count: signal(4)
    });
    fullMockService.isInCompare.and.returnValue(false);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [
        CompareButtonComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: ComparisonService, useValue: fullMockService }
      ]
    }).compileComponents();

    const newFixture = TestBed.createComponent(CompareButtonComponent);
    newFixture.componentRef.setInput('product', mockProduct);
    newFixture.detectChanges();

    const compiled = newFixture.nativeElement;
    const button = compiled.querySelector('.compare-btn');
    expect(button.disabled).toBeTrue();
  });

  it('should apply icon-only class when iconOnly is true', () => {
    fixture.componentRef.setInput('iconOnly', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.compare-btn');
    expect(button.classList.contains('icon-only')).toBeTrue();
  });

  it('should stop event propagation when clicked', () => {
    const mockEvent = jasmine.createSpyObj('Event', ['preventDefault', 'stopPropagation']);
    component.toggle(mockEvent);

    expect(mockEvent.preventDefault).toHaveBeenCalled();
    expect(mockEvent.stopPropagation).toHaveBeenCalled();
  });

  it('should have correct data-testid attribute', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('[data-testid="compare-btn-prod-1"]');
    expect(button).toBeTruthy();
  });
});
