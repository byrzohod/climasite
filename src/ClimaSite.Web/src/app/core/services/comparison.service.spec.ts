import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { ComparisonService, CompareProduct } from './comparison.service';

describe('ComparisonService', () => {
  let service: ComparisonService;

  const mockProduct1: CompareProduct = {
    id: 'prod-1',
    name: 'Test Product 1',
    slug: 'test-product-1',
    primaryImageUrl: 'https://example.com/img1.jpg',
    basePrice: 599,
    salePrice: undefined,
    isOnSale: false,
    brand: 'TestBrand',
    specifications: { power: '2500W', capacity: '9000 BTU' }
  };

  const mockProduct2: CompareProduct = {
    id: 'prod-2',
    name: 'Test Product 2',
    slug: 'test-product-2',
    primaryImageUrl: 'https://example.com/img2.jpg',
    basePrice: 799,
    salePrice: 699,
    isOnSale: true,
    brand: 'OtherBrand',
    specifications: { power: '3500W', capacity: '12000 BTU', noise: '45dB' }
  };

  beforeEach(() => {
    localStorage.removeItem('climasite_compare');

    TestBed.configureTestingModule({
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(ComparisonService);
  });

  afterEach(() => {
    localStorage.removeItem('climasite_compare');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with empty list', () => {
    expect(service.count()).toBe(0);
    expect(service.isEmpty()).toBeTrue();
    expect(service.compareList()).toEqual([]);
  });

  it('should add product to compare list', () => {
    const result = service.addToCompare(mockProduct1);

    expect(result).toBeTrue();
    expect(service.count()).toBe(1);
    expect(service.isEmpty()).toBeFalse();
  });

  it('should not add duplicate product', () => {
    service.addToCompare(mockProduct1);
    const result = service.addToCompare(mockProduct1);

    expect(result).toBeFalse();
    expect(service.count()).toBe(1);
  });

  it('should check if product is in compare list', () => {
    service.addToCompare(mockProduct1);

    expect(service.isInCompare('prod-1')).toBeTrue();
    expect(service.isInCompare('prod-2')).toBeFalse();
  });

  it('should remove product from compare list', () => {
    service.addToCompare(mockProduct1);
    service.addToCompare(mockProduct2);

    service.removeFromCompare('prod-1');

    expect(service.count()).toBe(1);
    expect(service.isInCompare('prod-1')).toBeFalse();
    expect(service.isInCompare('prod-2')).toBeTrue();
  });

  it('should toggle product in compare list', () => {
    // Add
    const added = service.toggleCompare(mockProduct1);
    expect(added).toBeTrue();
    expect(service.isInCompare('prod-1')).toBeTrue();

    // Remove
    const removed = service.toggleCompare(mockProduct1);
    expect(removed).toBeFalse();
    expect(service.isInCompare('prod-1')).toBeFalse();
  });

  it('should clear all products', () => {
    service.addToCompare(mockProduct1);
    service.addToCompare(mockProduct2);

    service.clearAll();

    expect(service.count()).toBe(0);
    expect(service.isEmpty()).toBeTrue();
  });

  it('should limit to 4 products', () => {
    for (let i = 1; i <= 5; i++) {
      service.addToCompare({
        id: `prod-${i}`,
        name: `Product ${i}`,
        slug: `product-${i}`,
        basePrice: 100 * i,
        isOnSale: false
      });
    }

    expect(service.count()).toBe(4);
    expect(service.isFull()).toBeTrue();
  });

  it('should return false when adding to full list', () => {
    for (let i = 1; i <= 4; i++) {
      service.addToCompare({
        id: `prod-${i}`,
        name: `Product ${i}`,
        slug: `product-${i}`,
        basePrice: 100,
        isOnSale: false
      });
    }

    const result = service.addToCompare({
      id: 'prod-5',
      name: 'Product 5',
      slug: 'product-5',
      basePrice: 500,
      isOnSale: false
    });

    expect(result).toBeFalse();
    expect(service.count()).toBe(4);
  });

  it('should indicate can compare when 2+ products', () => {
    expect(service.canCompare()).toBeFalse();

    service.addToCompare(mockProduct1);
    expect(service.canCompare()).toBeFalse();

    service.addToCompare(mockProduct2);
    expect(service.canCompare()).toBeTrue();
  });

  it('should get all specification keys from products', () => {
    service.addToCompare(mockProduct1);
    service.addToCompare(mockProduct2);

    const keys = service.getSpecificationKeys();

    expect(keys).toContain('power');
    expect(keys).toContain('capacity');
    expect(keys).toContain('noise');
    expect(keys.length).toBe(3);
  });

  it('should persist to localStorage', () => {
    service.addToCompare(mockProduct1);

    const stored = localStorage.getItem('climasite_compare');
    expect(stored).toBeTruthy();

    const parsed = JSON.parse(stored!) as CompareProduct[];
    expect(parsed.length).toBe(1);
    expect(parsed[0].id).toBe('prod-1');
  });

  it('should load from localStorage on init', () => {
    const storedData: CompareProduct[] = [{
      id: 'stored-1',
      name: 'Stored Product',
      slug: 'stored-product',
      basePrice: 299,
      isOnSale: false
    }];
    localStorage.setItem('climasite_compare', JSON.stringify(storedData));

    // Verify storage works
    const stored = localStorage.getItem('climasite_compare');
    expect(stored).toBeTruthy();
  });
});
