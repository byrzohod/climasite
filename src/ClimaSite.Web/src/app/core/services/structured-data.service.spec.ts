import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { StructuredDataService } from './structured-data.service';
import { Product, ProductBrief } from '../models/product.model';

describe('StructuredDataService', () => {
  let service: StructuredDataService;
  let document: Document;

  const mockProduct: Product = {
    id: 'prod-1',
    sku: 'AC-001',
    name: 'Test Air Conditioner',
    slug: 'test-air-conditioner',
    shortDescription: 'A great AC unit',
    description: 'Full description here',
    brand: 'TestBrand',
    model: 'TB-100',
    basePrice: 599,
    salePrice: 499,
    isOnSale: true,
    discountPercentage: 17,
    isActive: true,
    isFeatured: true,
    averageRating: 4.5,
    reviewCount: 25,
    images: [
      { id: 'img-1', url: 'https://example.com/img1.jpg', isPrimary: true, sortOrder: 0 },
      { id: 'img-2', url: 'https://example.com/img2.jpg', isPrimary: false, sortOrder: 1 }
    ],
    variants: [],
    warrantyMonths: 24,
    requiresInstallation: true,
    createdAt: '2024-01-01'
  };

  const mockProductBrief: ProductBrief = {
    id: 'prod-1',
    name: 'Test Product',
    slug: 'test-product',
    basePrice: 299,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 4.0,
    reviewCount: 10,
    primaryImageUrl: 'https://example.com/img.jpg',
    inStock: true
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(StructuredDataService);
    document = TestBed.inject(DOCUMENT);

    // Clean up any existing scripts
    service.clearAll();
  });

  afterEach(() => {
    service.clearAll();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('setProductData', () => {
    it('should add JSON-LD script to document head', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      expect(script).toBeTruthy();
      expect(script?.getAttribute('type')).toBe('application/ld+json');
    });

    it('should include product name in JSON-LD', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.name).toBe('Test Air Conditioner');
    });

    it('should include brand information', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.brand).toEqual({
        '@type': 'Brand',
        name: 'TestBrand'
      });
    });

    it('should include offer with sale price', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.offers.price).toBe(499);
      expect(data.offers.priceCurrency).toBe('EUR');
    });

    it('should include aggregate rating when reviews exist', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.aggregateRating).toBeTruthy();
      expect(data.aggregateRating.ratingValue).toBe(4.5);
      expect(data.aggregateRating.reviewCount).toBe(25);
    });

    it('should include multiple images', () => {
      service.setProductData(mockProduct, 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.image.length).toBe(2);
    });
  });

  describe('setProductListData', () => {
    it('should create ItemList schema', () => {
      service.setProductListData([mockProductBrief], 'Test List', 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data['@type']).toBe('ItemList');
      expect(data.name).toBe('Test List');
    });

    it('should include products with positions', () => {
      const products = [mockProductBrief, { ...mockProductBrief, id: 'prod-2', slug: 'prod-2' }];
      service.setProductListData(products, 'List', 'https://example.com');

      const script = document.getElementById('structured-data-default');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.itemListElement.length).toBe(2);
      expect(data.itemListElement[0].position).toBe(1);
      expect(data.itemListElement[1].position).toBe(2);
    });
  });

  describe('setBreadcrumbData', () => {
    it('should create BreadcrumbList schema', () => {
      const breadcrumbs = [
        { name: 'Home', url: 'https://example.com' },
        { name: 'Products', url: 'https://example.com/products' },
        { name: 'AC Units', url: 'https://example.com/products/ac' }
      ];

      service.setBreadcrumbData(breadcrumbs);

      const script = document.getElementById('structured-data-breadcrumb');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data['@type']).toBe('BreadcrumbList');
      expect(data.itemListElement.length).toBe(3);
    });

    it('should use separate script ID for breadcrumbs', () => {
      service.setBreadcrumbData([{ name: 'Home', url: '/' }]);

      const script = document.getElementById('structured-data-breadcrumb');
      expect(script).toBeTruthy();
    });
  });

  describe('setOrganizationData', () => {
    it('should create Organization schema', () => {
      service.setOrganizationData({
        name: 'ClimaSite',
        url: 'https://climasite.com',
        logo: 'https://climasite.com/logo.png'
      });

      const script = document.getElementById('structured-data-organization');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data['@type']).toBe('Organization');
      expect(data.name).toBe('ClimaSite');
    });

    it('should include contact point if provided', () => {
      service.setOrganizationData({
        name: 'ClimaSite',
        url: 'https://climasite.com',
        logo: 'https://climasite.com/logo.png',
        contactPoint: {
          telephone: '+1-800-123-4567',
          contactType: 'customer service',
          availableLanguage: ['English', 'German', 'Bulgarian']
        }
      });

      const script = document.getElementById('structured-data-organization');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data.contactPoint).toBeTruthy();
      expect(data.contactPoint.telephone).toBe('+1-800-123-4567');
    });
  });

  describe('setWebsiteData', () => {
    it('should create WebSite schema with search action', () => {
      service.setWebsiteData('ClimaSite', 'https://climasite.com', 'https://climasite.com/search');

      const script = document.getElementById('structured-data-website');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data['@type']).toBe('WebSite');
      expect(data.potentialAction['@type']).toBe('SearchAction');
    });
  });

  describe('setFaqData', () => {
    it('should create FAQPage schema', () => {
      const faqs = [
        { question: 'What is HVAC?', answer: 'HVAC stands for Heating, Ventilation, and Air Conditioning.' },
        { question: 'How often to service AC?', answer: 'Annually recommended.' }
      ];

      service.setFaqData(faqs);

      const script = document.getElementById('structured-data-faq');
      const data = JSON.parse(script?.textContent || '{}');

      expect(data['@type']).toBe('FAQPage');
      expect(data.mainEntity.length).toBe(2);
      expect(data.mainEntity[0]['@type']).toBe('Question');
    });
  });

  describe('clearAll', () => {
    it('should remove all structured data scripts', () => {
      service.setProductData(mockProduct, 'https://example.com');
      service.setBreadcrumbData([{ name: 'Home', url: '/' }]);

      service.clearAll();

      const scripts = document.querySelectorAll('script[type="application/ld+json"]');
      expect(scripts.length).toBe(0);
    });
  });

  describe('clearById', () => {
    it('should remove specific structured data script', () => {
      service.setProductData(mockProduct, 'https://example.com');
      service.setBreadcrumbData([{ name: 'Home', url: '/' }]);

      service.clearById('breadcrumb');

      expect(document.getElementById('structured-data-breadcrumb')).toBeFalsy();
      expect(document.getElementById('structured-data-default')).toBeTruthy();
    });
  });
});
