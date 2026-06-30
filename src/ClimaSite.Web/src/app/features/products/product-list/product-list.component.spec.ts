import { WritableSignal, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { ProductListComponent } from './product-list.component';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { LanguageService } from '../../../core/services/language.service';
import { SeoService } from '../../../core/services/seo.service';
import { StructuredDataService } from '../../../core/services/structured-data.service';
import { Category } from '../../../core/models/category.model';
import { FilterOptions, PaginatedResult, ProductBrief, ProductFilter } from '../../../core/models/product.model';

/**
 * Plan-19 B2: unit coverage for the product-list filtering / sorting / pagination engine
 * and its loading / empty / error branches.
 *
 * The component is instantiated without rendering the template (it imports product-card,
 * category-header, RevealDirective, empty-state and uses ngx-translate). We mock the two
 * data services, the Router (for query-param sync + redirects) and ActivatedRoute's
 * params/queryParams as Subjects so we control ngOnInit timing precisely. The constructor
 * registers an effect() reading LanguageService.currentLanguage, so a writable signal
 * double is supplied for it. This mirrors checkout.component.spec.ts.
 */

function brief(id: string, name = `Product ${id}`): ProductBrief {
  return {
    id,
    name,
    slug: name.toLowerCase().replace(/\s+/g, '-'),
    basePrice: 100,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 4,
    reviewCount: 3,
    inStock: true
  };
}

function page(items: ProductBrief[], totalCount: number, totalPages: number): PaginatedResult<ProductBrief> {
  return {
    items,
    pageNumber: 1,
    pageSize: 12,
    totalCount,
    totalPages,
    hasPreviousPage: false,
    hasNextPage: totalPages > 1
  };
}

const emptyFilterOptions: FilterOptions = {
  brands: [],
  priceRange: { min: 0, max: 10000 },
  specifications: {},
  tags: []
};

const mockCategory: Category = {
  id: 'cat-1',
  name: 'Air Conditioners',
  slug: 'air-conditioners',
  sortOrder: 0,
  isActive: true,
  children: [],
  productCount: 5
};

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let productService: jasmine.SpyObj<Pick<ProductService, 'getProducts' | 'getFilterOptions'>>;
  let categoryService: jasmine.SpyObj<Pick<CategoryService, 'getCategoryBySlug'>>;
  let router: jasmine.SpyObj<Router>;
  let langSignal: WritableSignal<string>;
  let seo: jasmine.SpyObj<SeoService>;
  let structuredData: jasmine.SpyObj<StructuredDataService>;
  let routeParams: Subject<Record<string, string>>;
  let routeQueryParams: Subject<Record<string, string>>;

  beforeEach(() => {
    productService = jasmine.createSpyObj<Pick<ProductService, 'getProducts' | 'getFilterOptions'>>('ProductService', [
      'getProducts',
      'getFilterOptions'
    ]);
    productService.getProducts.and.returnValue(of(page([brief('1')], 1, 1)));
    productService.getFilterOptions.and.returnValue(of(emptyFilterOptions));

    categoryService = jasmine.createSpyObj<Pick<CategoryService, 'getCategoryBySlug'>>('CategoryService', [
      'getCategoryBySlug'
    ]);
    categoryService.getCategoryBySlug.and.returnValue(of(mockCategory));

    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    langSignal = signal('en');
    seo = jasmine.createSpyObj<SeoService>('SeoService', ['setMeta']);
    structuredData = jasmine.createSpyObj<StructuredDataService>('StructuredDataService', ['setProductListData', 'setBreadcrumbData']);
    routeParams = new Subject<Record<string, string>>();
    routeQueryParams = new Subject<Record<string, string>>();

    TestBed.configureTestingModule({
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: CategoryService, useValue: categoryService },
        { provide: LanguageService, useValue: { currentLanguage: langSignal, instant: () => 'Products' } },
        { provide: SeoService, useValue: seo },
        { provide: StructuredDataService, useValue: structuredData },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { params: routeParams.asObservable(), queryParams: routeQueryParams.asObservable() }
        }
      ]
    });

    component = TestBed.runInInjectionContext(() => new ProductListComponent());
  });

  it('creates with sensible defaults', () => {
    expect(component).toBeTruthy();
    expect(component.loading()).toBeTrue();
    expect(component.currentPage()).toBe(1);
    expect(component.sortBy).toBe('newest');
    expect(component.viewMode()).toBe('grid');
    expect(component.hasActiveFilters()).toBeFalse();
  });

  describe('fetchProducts', () => {
    it('populates products + counts and lands in the loaded (non-error) state', () => {
      productService.getProducts.and.returnValue(of(page([brief('1'), brief('2')], 24, 2)));

      component.fetchProducts();

      expect(component.products().length).toBe(2);
      expect(component.totalCount()).toBe(24);
      expect(component.totalPages()).toBe(2);
      expect(component.loading()).toBeFalse();
      expect(component.error()).toBeNull();
    });

    it('renders the empty branch when the API returns zero items', () => {
      productService.getProducts.and.returnValue(of(page([], 0, 1)));

      component.fetchProducts();

      expect(component.products().length).toBe(0);
      expect(component.totalCount()).toBe(0);
      expect(component.error()).toBeNull();
      expect(component.loading()).toBeFalse();
    });

    it('sets the load-failed error key and stops loading when the API errors', () => {
      productService.getProducts.and.returnValue(throwError(() => new Error('500')));

      component.fetchProducts();

      expect(component.error()).toBe('products.error.loadFailed');
      expect(component.loading()).toBeFalse();
    });

    it('builds the request filter from current state, mapping sort variants correctly', () => {
      component.selectedBrand.set('Daikin');
      component.inStockOnly.set(true);
      component.onSaleOnly.set(true);
      component.minPrice = 50;
      component.maxPrice = 500;
      component.sortBy = 'price-desc';
      component.currentPage.set(2);

      component.fetchProducts('cat-1');

      const filter = productService.getProducts.calls.mostRecent().args[0] as ProductFilter;
      expect(filter.pageNumber).toBe(2);
      expect(filter.pageSize).toBe(12);
      expect(filter.categoryId).toBe('cat-1');
      expect(filter.brand).toBe('Daikin');
      expect(filter.minPrice).toBe(50);
      expect(filter.maxPrice).toBe(500);
      expect(filter.inStock).toBeTrue();
      expect(filter.onSale).toBeTrue();
      // price-desc => sortBy 'price' + descending.
      expect(filter.sortBy).toBe('price');
      expect(filter.sortDescending).toBeTrue();
    });

    it('maps name-asc to sortBy=name ascending', () => {
      component.sortBy = 'name-asc';
      component.fetchProducts();
      const filter = productService.getProducts.calls.mostRecent().args[0] as ProductFilter;
      expect(filter.sortBy).toBe('name');
      expect(filter.sortDescending).toBeFalse();
    });

    it('passes the raw key through for the default newest sort', () => {
      component.sortBy = 'newest';
      component.fetchProducts();
      const filter = productService.getProducts.calls.mostRecent().args[0] as ProductFilter;
      expect(filter.sortBy).toBe('newest');
      expect(filter.sortDescending).toBeFalse();
    });
  });

  describe('SEO (B-044): robots + ItemList', () => {
    function lastRobots(): string | undefined {
      const args = seo.setMeta.calls.mostRecent().args[0] as { robots?: string };
      return args.robots;
    }

    it('clean /products (no thin params) is index,follow', () => {
      component.fetchProducts();
      expect(lastRobots()).toBe('index,follow');
    });

    it('a category page with no thin params is index,follow and titled with the category name', () => {
      component.category.set(mockCategory);
      component.fetchProducts('cat-1');
      const args = seo.setMeta.calls.mostRecent().args[0] as { robots?: string; title?: string };
      expect(args.robots).toBe('index,follow');
      expect(args.title).toBe('Air Conditioners');
    });

    it('paginated state (page > 1) is noindex,follow', () => {
      component.currentPage.set(2);
      component.fetchProducts();
      expect(lastRobots()).toBe('noindex,follow');
    });

    it('a brand filter is noindex,follow', () => {
      component.selectedBrand.set('Daikin');
      component.fetchProducts();
      expect(lastRobots()).toBe('noindex,follow');
    });

    it('a sort change is noindex,follow', () => {
      component.sortBy = 'price-asc';
      component.fetchProducts();
      expect(lastRobots()).toBe('noindex,follow');
    });

    it('a search term is noindex,follow with the search title', () => {
      component.searchQuery.set('inverter ac');
      component.fetchProducts();
      const args = seo.setMeta.calls.mostRecent().args[0] as { robots?: string; titleKey?: string };
      expect(args.robots).toBe('noindex,follow');
      expect(args.titleKey).toBe('seo.products.searchTitle');
    });

    it('does NOT noindex when only marketing params are present (utm_*/gclid/fbclid)', () => {
      // Drive ngOnInit so the real param subscriptions are wired, then emit genuine
      // marketing params: they must NOT flip robots to noindex (council R2 #2 / L2).
      component.ngOnInit();
      routeParams.next({});
      routeQueryParams.next({ utm_source: 'newsletter', gclid: 'abc123', fbclid: 'fb789' });

      const args = seo.setMeta.calls.mostRecent().args[0] as { robots?: string; canonicalPath?: string };
      expect(args.robots).toBe('index,follow');
      // The component sets no canonicalPath — SeoService strips the query for the canonical
      // (proven in seo.service.spec 'strips the query string and fragment').
      expect(args.canonicalPath).toBeUndefined();
    });

    it('emits the ItemList structured data for the loaded page', () => {
      productService.getProducts.and.returnValue(of(page([brief('1'), brief('2')], 2, 1)));
      component.fetchProducts();
      expect(structuredData.setProductListData).toHaveBeenCalled();
      const items = structuredData.setProductListData.calls.mostRecent().args[0];
      expect(items.length).toBe(2);
    });

    it('rejects a stale out-of-order fetch response (fetchSeq guard)', () => {
      // Two in-flight fetches: the LATER one (B) completes first, then the earlier (A)
      // completes late. The guard must keep B and ignore A.
      const subjectA = new Subject<PaginatedResult<ProductBrief>>();
      const subjectB = new Subject<PaginatedResult<ProductBrief>>();
      productService.getProducts.and.returnValues(subjectA, subjectB);

      component.fetchProducts(); // seq 1 -> subjectA
      component.fetchProducts(); // seq 2 -> subjectB

      subjectB.next(page([brief('B1'), brief('B2')], 2, 1)); // latest lands first
      subjectA.next(page([brief('A1')], 1, 1));              // stale lands late

      expect(component.products().map(p => p.id)).toEqual(['B1', 'B2']);
      const lastItems = structuredData.setProductListData.calls.mostRecent().args[0];
      expect(lastItems.map(p => p.id)).toEqual(['B1', 'B2']);
    });

    it('rejects a stale out-of-order CATEGORY response (loadSeq guard)', () => {
      // The category-resolution layer feeds fetchProducts. A stale getCategoryBySlug(A)
      // landing after a newer load(B) must NOT set stale category or trigger fetchProducts(A)
      // (which would otherwise become the latest fetchSeq and apply stale products/meta/JSON-LD).
      const catA = { ...mockCategory, id: 'cat-A', slug: 'cat-a', name: 'Cat A' };
      const catB = { ...mockCategory, id: 'cat-B', slug: 'cat-b', name: 'Cat B' };
      const subjectCatA = new Subject<typeof mockCategory>();
      const subjectCatB = new Subject<typeof mockCategory>();
      categoryService.getCategoryBySlug.and.returnValues(subjectCatA, subjectCatB);
      productService.getProducts.calls.reset();
      productService.getProducts.and.returnValue(of(page([brief('B1')], 1, 1)));

      component.loadProducts('cat-a'); // loadSeq 1 -> subjectCatA
      component.loadProducts('cat-b'); // loadSeq 2 -> subjectCatB

      subjectCatB.next(catB); // latest resolves first -> sets catB, fetches catB
      subjectCatA.next(catA); // stale resolves late -> guarded out

      expect(component.category()?.id).toBe('cat-B');
      // fetchProducts must have been called only for the latest category (catB), never catA.
      const fetchedCategoryIds = productService.getProducts.calls.allArgs().map(args => args[0]?.categoryId);
      expect(fetchedCategoryIds).toContain('cat-B');
      expect(fetchedCategoryIds).not.toContain('cat-A');
    });
  });

  describe('filter toggles', () => {
    it('toggleBrand selects then deselects the same brand', () => {
      component.toggleBrand('Mitsubishi');
      expect(component.selectedBrand()).toBe('Mitsubishi');

      component.toggleBrand('Mitsubishi');
      expect(component.selectedBrand()).toBeNull();
    });

    it('toggleInStock / toggleOnSale flip their flags', () => {
      component.toggleInStock();
      expect(component.inStockOnly()).toBeTrue();
      component.toggleOnSale();
      expect(component.onSaleOnly()).toBeTrue();
    });

    it('hasActiveFilters becomes true once a signal-backed filter is set', () => {
      expect(component.hasActiveFilters()).toBeFalse();
      component.selectedBrand.set('Daikin');
      expect(component.hasActiveFilters()).toBeTrue();
    });

    it('hasActiveFilters accounts for a price bound once the computed re-evaluates', () => {
      // minPrice/maxPrice are plain properties (not signals); the computed only re-reads
      // them when one of its signal dependencies changes. We set the price, then nudge a
      // signal back-and-forth to force a recompute, proving the price bound is counted.
      component.minPrice = 100;
      component.selectedBrand.set('X');
      component.selectedBrand.set(null);
      expect(component.hasActiveFilters()).toBeTrue();
    });
  });

  describe('applyFilters', () => {
    it('resets to page 1, syncs the URL, and refetches', () => {
      component.currentPage.set(4);
      component.selectedBrand.set('Bosch');

      component.applyFilters();

      expect(component.currentPage()).toBe(1);
      expect(router.navigate).toHaveBeenCalled();
      expect(productService.getProducts).toHaveBeenCalled();
    });
  });

  describe('clearFilters', () => {
    it('resets every filter to its default and refetches', () => {
      component.selectedBrand.set('Bosch');
      component.inStockOnly.set(true);
      component.onSaleOnly.set(true);
      component.searchQuery.set('split');
      component.minPrice = 100;
      component.maxPrice = 900;
      component.sortBy = 'price-asc';

      component.clearFilters();

      expect(component.selectedBrand()).toBeNull();
      expect(component.inStockOnly()).toBeFalse();
      expect(component.onSaleOnly()).toBeFalse();
      expect(component.searchQuery()).toBeNull();
      expect(component.minPrice).toBeNull();
      expect(component.maxPrice).toBeNull();
      expect(component.sortBy).toBe('newest');
      expect(component.hasActiveFilters()).toBeFalse();
      // Clears the search param off the URL.
      expect(router.navigate).toHaveBeenCalled();
    });
  });

  describe('pagination', () => {
    beforeEach(() => {
      component.totalPages.set(5);
      component.currentPage.set(3);
      spyOn(window, 'scrollTo');
    });

    it('goToPage moves to a valid in-range page and refetches', () => {
      component.goToPage(4);
      expect(component.currentPage()).toBe(4);
      expect(productService.getProducts).toHaveBeenCalled();
      expect(window.scrollTo).toHaveBeenCalled();
    });

    it('ignores a page below 1', () => {
      productService.getProducts.calls.reset();
      component.goToPage(0);
      expect(component.currentPage()).toBe(3);
      expect(productService.getProducts).not.toHaveBeenCalled();
    });

    it('ignores a page beyond totalPages', () => {
      productService.getProducts.calls.reset();
      component.goToPage(6);
      expect(component.currentPage()).toBe(3);
      expect(productService.getProducts).not.toHaveBeenCalled();
    });

    it('visiblePages exposes a centered window clamped to bounds', () => {
      // current=3, total=5 -> [1,2,3,4,5]
      expect(component.visiblePages()).toEqual([1, 2, 3, 4, 5]);

      component.currentPage.set(1);
      // start clamps at 1.
      expect(component.visiblePages()).toEqual([1, 2, 3]);

      component.totalPages.set(10);
      component.currentPage.set(8);
      expect(component.visiblePages()).toEqual([6, 7, 8, 9, 10]);
    });
  });

  describe('loadProducts', () => {
    it('with no slug clears the category and fetches all products + global filter options', () => {
      component.loadProducts();

      expect(component.category()).toBeNull();
      // No slug => the component calls getFilterOptions(undefined) / a global fetch.
      expect(productService.getFilterOptions).toHaveBeenCalledWith(undefined);
      expect(productService.getProducts).toHaveBeenCalled();
    });

    it('with a slug resolves the category, then loads its filter options + products', () => {
      component.loadProducts('air-conditioners');

      expect(categoryService.getCategoryBySlug).toHaveBeenCalledWith('air-conditioners');
      expect(component.category()).toEqual(mockCategory);
      expect(productService.getFilterOptions).toHaveBeenCalledWith('air-conditioners');
      const filter = productService.getProducts.calls.mostRecent().args[0] as ProductFilter;
      expect(filter.categoryId).toBe('cat-1');
    });

    it('redirects to /products and stops loading when the category lookup fails', () => {
      categoryService.getCategoryBySlug.and.returnValue(throwError(() => new Error('404')));

      component.loadProducts('does-not-exist');

      expect(component.loading()).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/products']);
    });
  });

  describe('loadFilterOptions', () => {
    it('stores the returned options', () => {
      const opts: FilterOptions = { ...emptyFilterOptions, brands: [{ name: 'Daikin', count: 4 }] };
      productService.getFilterOptions.and.returnValue(of(opts));

      component.loadFilterOptions();

      expect(component.filterOptions()?.brands.length).toBe(1);
    });

    it('falls back to safe empty options when the fetch fails', () => {
      productService.getFilterOptions.and.returnValue(throwError(() => new Error('500')));

      component.loadFilterOptions();

      const fallback = component.filterOptions();
      expect(fallback).toBeTruthy();
      expect(fallback?.brands).toEqual([]);
      expect(fallback?.priceRange).toEqual({ min: 0, max: 10000 });
    });
  });

  describe('retryLoad', () => {
    it('clears the error and reloads using the current category slug', () => {
      component.error.set('products.error.loadFailed');
      component.category.set(mockCategory);

      component.retryLoad();

      expect(component.error()).toBeNull();
      expect(categoryService.getCategoryBySlug).toHaveBeenCalledWith('air-conditioners');
    });
  });

  describe('categoryInfo computed', () => {
    it('is null without a category', () => {
      expect(component.categoryInfo()).toBeNull();
    });

    it('projects the category plus the live totalCount', () => {
      component.category.set(mockCategory);
      component.totalCount.set(42);

      const info = component.categoryInfo();
      expect(info?.id).toBe('cat-1');
      expect(info?.slug).toBe('air-conditioners');
      expect(info?.productCount).toBe(42);
    });
  });

  describe('filter sidebar (mobile drawer)', () => {
    it('open then close toggles the filterOpen flag', () => {
      expect(component.filterOpen()).toBeFalse();
      component.openFilterSidebar();
      expect(component.filterOpen()).toBeTrue();
      component.closeFilterSidebar();
      expect(component.filterOpen()).toBeFalse();
    });
  });

  describe('ngOnInit route wiring', () => {
    it('loads products from the route params and marks itself initialized', fakeAsync(() => {
      component.ngOnInit();
      productService.getProducts.calls.reset();

      routeParams.next({});
      tick();

      expect(component.currentPage()).toBe(1);
      expect(productService.getProducts).toHaveBeenCalled();
    }));

    it('hydrates filter state from query params', fakeAsync(() => {
      component.ngOnInit();
      routeParams.next({});
      tick();

      routeQueryParams.next({
        page: '3',
        brand: 'Daikin',
        inStock: 'true',
        onSale: 'true',
        minPrice: '120',
        maxPrice: '800',
        sort: 'price-asc'
      });
      tick();

      expect(component.currentPage()).toBe(3);
      expect(component.selectedBrand()).toBe('Daikin');
      expect(component.inStockOnly()).toBeTrue();
      expect(component.onSaleOnly()).toBeTrue();
      expect(component.minPrice).toBe(120);
      expect(component.maxPrice).toBe(800);
      expect(component.sortBy).toBe('price-asc');
    }));

    it('refetches when a search query arrives after initialization', fakeAsync(() => {
      component.ngOnInit();
      routeParams.next({});
      tick();
      productService.getProducts.calls.reset();

      routeQueryParams.next({ search: 'inverter' });
      tick();

      expect(component.searchQuery()).toBe('inverter');
      expect(productService.getProducts).toHaveBeenCalled();
      expect(component.hasActiveFilters()).toBeTrue();
    }));
  });

  describe('language-change effect', () => {
    it('refetches products when the active language changes after init', fakeAsync(() => {
      component.ngOnInit();
      routeParams.next({});
      tick();
      // Flush once now that isInitialized is true so the effect captures lastLanguage='en'
      // under the post-init guard. (Effects only run on flush in tests.)
      TestBed.flushEffects();
      productService.getProducts.calls.reset();

      langSignal.set('de');
      TestBed.flushEffects();

      expect(productService.getProducts).toHaveBeenCalled();
    }));

    it('does NOT refetch when the language signal re-emits the same value', fakeAsync(() => {
      component.ngOnInit();
      routeParams.next({});
      tick();
      TestBed.flushEffects();
      productService.getProducts.calls.reset();

      // Same language -> guard short-circuits, no extra fetch.
      langSignal.set('en');
      TestBed.flushEffects();

      expect(productService.getProducts).not.toHaveBeenCalled();
    }));
  });

  it('tears down its route subscriptions on destroy without throwing', () => {
    component.ngOnInit();
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
