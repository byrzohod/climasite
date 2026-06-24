import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CategoryService } from './category.service';
import { Category, CategoryTree } from '../models/category.model';
import { environment } from '../../../environments/environment';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/categories`;

  const mockTree: CategoryTree[] = [
    {
      id: 'cat-1',
      name: 'Air Conditioners',
      slug: 'air-conditioners',
      imageUrl: 'https://example.com/ac.png',
      children: [
        {
          id: 'cat-1-1',
          name: 'Split Systems',
          slug: 'split-systems',
          children: []
        }
      ]
    },
    {
      id: 'cat-2',
      name: 'Heating',
      slug: 'heating',
      children: []
    }
  ];

  const mockCategory: Category = {
    id: 'cat-1',
    name: 'Air Conditioners',
    slug: 'air-conditioners',
    description: 'Cooling products',
    icon: 'snowflake',
    imageUrl: 'https://example.com/ac.png',
    sortOrder: 1,
    isActive: true,
    children: [],
    productCount: 12
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CategoryService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCategoryTree', () => {
    it('should GET the category tree from the categories endpoint', () => {
      let result: CategoryTree[] | undefined;
      service.getCategoryTree().subscribe(r => (result = r));

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockTree);

      expect(result).toEqual(mockTree);
    });

    it('should preserve nested children in the returned tree', () => {
      let result: CategoryTree[] | undefined;
      service.getCategoryTree().subscribe(r => (result = r));

      const req = httpMock.expectOne(apiUrl);
      req.flush(mockTree);

      expect(result?.[0].children.length).toBe(1);
      expect(result?.[0].children[0].slug).toBe('split-systems');
      expect(result?.[1].children).toEqual([]);
    });

    it('should map an empty tree', () => {
      let result: CategoryTree[] | undefined;
      service.getCategoryTree().subscribe(r => (result = r));

      const req = httpMock.expectOne(apiUrl);
      req.flush([]);

      expect(result).toEqual([]);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.getCategoryTree().subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(apiUrl);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });

  describe('getCategoryBySlug', () => {
    it('should GET a single category by slug', () => {
      let result: Category | undefined;
      service.getCategoryBySlug('air-conditioners').subscribe(r => (result = r));

      const req = httpMock.expectOne(`${apiUrl}/air-conditioners`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCategory);

      expect(result).toEqual(mockCategory);
      expect(result?.productCount).toBe(12);
    });

    it('should map a category that omits optional fields', () => {
      const minimal: Category = {
        id: 'cat-3',
        name: 'Accessories',
        slug: 'accessories',
        sortOrder: 3,
        isActive: true,
        children: [],
        productCount: 0
      };
      let result: Category | undefined;
      service.getCategoryBySlug('accessories').subscribe(r => (result = r));

      const req = httpMock.expectOne(`${apiUrl}/accessories`);
      req.flush(minimal);

      expect(result?.description).toBeUndefined();
      expect(result?.icon).toBeUndefined();
      expect(result?.imageUrl).toBeUndefined();
    });

    it('should propagate a 404 for an unknown slug', () => {
      let status = 0;
      service.getCategoryBySlug('does-not-exist').subscribe({
        error: (e) => (status = e.status)
      });

      const req = httpMock.expectOne(`${apiUrl}/does-not-exist`);
      req.flush('not found', { status: 404, statusText: 'Not Found' });

      expect(status).toBe(404);
    });
  });
});
