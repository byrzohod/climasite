import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { RecentlyViewedComponent } from './recently-viewed.component';
import { RecentlyViewedService } from '../../../core/services/recently-viewed.service';
import { TranslateModule } from '@ngx-translate/core';
import { signal } from '@angular/core';

describe('RecentlyViewedComponent', () => {
  let component: RecentlyViewedComponent;
  let fixture: ComponentFixture<RecentlyViewedComponent>;
  let mockService: jasmine.SpyObj<RecentlyViewedService>;

  const mockProducts = [
    {
      id: 'prod-1',
      name: 'Test Product 1',
      slug: 'test-product-1',
      primaryImageUrl: 'https://example.com/img1.jpg',
      basePrice: 599,
      salePrice: undefined,
      isOnSale: false,
      brand: 'TestBrand',
      viewedAt: Date.now()
    },
    {
      id: 'prod-2',
      name: 'Test Product 2',
      slug: 'test-product-2',
      primaryImageUrl: 'https://example.com/img2.jpg',
      basePrice: 799,
      salePrice: 699,
      isOnSale: true,
      brand: 'OtherBrand',
      viewedAt: Date.now() - 1000
    }
  ];

  beforeEach(async () => {
    mockService = jasmine.createSpyObj('RecentlyViewedService', ['clearAll', 'getProductsExcluding'], {
      recentlyViewed: signal(mockProducts),
      count: signal(2)
    });
    mockService.getProductsExcluding.and.returnValue(mockProducts.slice(1));

    await TestBed.configureTestingModule({
      imports: [
        RecentlyViewedComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: RecentlyViewedService, useValue: mockService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RecentlyViewedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display recently viewed products', () => {
    const compiled = fixture.nativeElement;
    const productCards = compiled.querySelectorAll('.product-card');
    expect(productCards.length).toBe(2);
  });

  it('should display product names', () => {
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Test Product 1');
    expect(compiled.textContent).toContain('Test Product 2');
  });

  it('should display brand names', () => {
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('TestBrand');
    expect(compiled.textContent).toContain('OtherBrand');
  });

  it('should show sale badge for on-sale products', () => {
    const compiled = fixture.nativeElement;
    const saleBadges = compiled.querySelectorAll('.sale-badge');
    expect(saleBadges.length).toBe(1);
  });

  it('should show sale price when product is on sale', () => {
    const compiled = fixture.nativeElement;
    const salePrice = compiled.querySelector('.price .sale');
    expect(salePrice).toBeTruthy();
  });

  it('should show clear button by default', () => {
    const compiled = fixture.nativeElement;
    const clearButton = compiled.querySelector('[data-testid="clear-history"]');
    expect(clearButton).toBeTruthy();
  });

  it('should hide clear button when showClearButton is false', () => {
    fixture.componentRef.setInput('showClearButton', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const clearButton = compiled.querySelector('[data-testid="clear-history"]');
    expect(clearButton).toBeFalsy();
  });

  it('should call clearAll when clear button is clicked', () => {
    const compiled = fixture.nativeElement;
    const clearButton = compiled.querySelector('[data-testid="clear-history"]');
    clearButton.click();

    expect(mockService.clearAll).toHaveBeenCalled();
  });

  it('should exclude product when excludeProductId is set', () => {
    fixture.componentRef.setInput('excludeProductId', 'prod-1');
    fixture.detectChanges();

    expect(mockService.getProductsExcluding).toHaveBeenCalledWith('prod-1', 8);
  });

  it('should apply compact class when compact input is true', () => {
    fixture.componentRef.setInput('compact', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const scrollContainer = compiled.querySelector('.products-scroll');
    expect(scrollContainer.classList.contains('compact')).toBeTrue();
  });

  it('should not render when no products', async () => {
    // Create a new mock with empty products
    const emptyMockService = jasmine.createSpyObj('RecentlyViewedService', ['clearAll', 'getProductsExcluding'], {
      recentlyViewed: signal([]),
      count: signal(0)
    });
    emptyMockService.getProductsExcluding.and.returnValue([]);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [
        RecentlyViewedComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: RecentlyViewedService, useValue: emptyMockService }
      ]
    }).compileComponents();

    const emptyFixture = TestBed.createComponent(RecentlyViewedComponent);
    emptyFixture.detectChanges();

    const compiled = emptyFixture.nativeElement;
    const section = compiled.querySelector('.recently-viewed');
    expect(section).toBeFalsy();
  });

  it('should link to product detail page', () => {
    const compiled = fixture.nativeElement;
    const firstLink = compiled.querySelector('.product-card');
    expect(firstLink.getAttribute('href')).toBe('/products/test-product-1');
  });

  it('should display product images', () => {
    const compiled = fixture.nativeElement;
    const images = compiled.querySelectorAll('.product-image img');
    expect(images.length).toBe(2);
    expect(images[0].getAttribute('src')).toBe('https://example.com/img1.jpg');
  });
});
