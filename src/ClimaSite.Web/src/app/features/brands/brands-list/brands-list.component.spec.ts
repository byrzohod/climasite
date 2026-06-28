import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { BrandsListComponent } from './brands-list.component';
import { BrandService } from '../../../core/services/brand.service';
import { BrandBrief, BrandListResponse } from '../../../core/models/brand.model';

/**
 * Plan-19 B3: unit coverage for BrandsListComponent — initial load, error + retry,
 * empty state, the "load more" pagination (page increment + append + hasNextPage),
 * and the loading template branch.
 */

function brief(overrides: Partial<BrandBrief> = {}): BrandBrief {
  return {
    id: 'b1',
    name: 'Daikin',
    slug: 'daikin',
    isFeatured: false,
    productCount: 5,
    ...overrides
  };
}

function response(items: BrandBrief[], hasNextPage = false): BrandListResponse {
  return {
    items,
    pageNumber: 1,
    totalPages: hasNextPage ? 2 : 1,
    totalCount: items.length,
    hasPreviousPage: false,
    hasNextPage
  };
}

describe('BrandsListComponent', () => {
  let brandService: jasmine.SpyObj<BrandService>;

  beforeEach(async () => {
    brandService = jasmine.createSpyObj<BrandService>('BrandService', ['getBrands']);
    brandService.getBrands.and.returnValue(of(response([brief()])));

    await TestBed.configureTestingModule({
      imports: [BrandsListComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: BrandService, useValue: brandService }
      ]
    }).compileComponents();
  });

  function create(): ComponentFixture<BrandsListComponent> {
    const fixture = TestBed.createComponent(BrandsListComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('should create', () => {
    expect(create().componentInstance).toBeTruthy();
  });

  it('loads page 1 with the configured page size on init', () => {
    const fixture = create();
    expect(brandService.getBrands).toHaveBeenCalledWith(1, 24);
    expect(fixture.componentInstance.brands().length).toBe(1);
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('renders one card per brand and the page header', () => {
    brandService.getBrands.and.returnValue(of(response([brief({ id: 'a' }), brief({ id: 'b', name: 'Mitsubishi' })])));
    const fixture = create();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.brand-card').length).toBe(2);
    expect(el.querySelector('.page-header h1')).toBeTruthy();
  });

  it('shows a logo placeholder with the first letter when no logoUrl', () => {
    brandService.getBrands.and.returnValue(of(response([brief({ logoUrl: undefined, name: 'Bosch' })])));
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.logo-placeholder')?.textContent?.trim()).toBe('B');
  });

  it('renders the featured badge only for featured brands', () => {
    brandService.getBrands.and.returnValue(of(response([brief({ isFeatured: true })])));
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.featured-badge')).toBeTruthy();
  });

  it('shows the loading indicator while the first page is in flight', () => {
    brandService.getBrands.and.returnValue(new Subject<BrandListResponse>());
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
  });

  it('shows the empty state when no brands are returned', () => {
    brandService.getBrands.and.returnValue(of(response([])));
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.no-brands')).toBeTruthy();
  });

  describe('error handling', () => {
    it('sets the load-failed error and renders the error container', () => {
      spyOn(console, 'error');
      brandService.getBrands.and.returnValue(throwError(() => new Error('boom')));
      const fixture = create();
      expect(fixture.componentInstance.error()).toBe('brands.errors.loadFailed');
      expect(fixture.nativeElement.querySelector('.error-container')).toBeTruthy();
    });

    it('retries loading when the retry button is clicked', () => {
      spyOn(console, 'error');
      brandService.getBrands.and.returnValue(throwError(() => new Error('boom')));
      const fixture = create();
      brandService.getBrands.calls.reset();
      brandService.getBrands.and.returnValue(of(response([brief()])));

      fixture.nativeElement.querySelector('.retry-btn').click();
      fixture.detectChanges();

      expect(brandService.getBrands).toHaveBeenCalledTimes(1);
      expect(fixture.componentInstance.brands().length).toBe(1);
    });
  });

  describe('load more pagination', () => {
    it('renders the load-more button when there is a next page', () => {
      brandService.getBrands.and.returnValue(of(response([brief()], true)));
      const fixture = create();
      expect(fixture.componentInstance.hasNextPage()).toBeTrue();
      expect(fixture.nativeElement.querySelector('.btn-load-more')).toBeTruthy();
    });

    it('increments the page and appends results on loadMore', () => {
      brandService.getBrands.and.returnValue(of(response([brief({ id: 'a' })], true)));
      const fixture = create();

      brandService.getBrands.and.returnValue(of(response([brief({ id: 'b' })], false)));
      fixture.componentInstance.loadMore();

      expect(brandService.getBrands).toHaveBeenCalledWith(2, 24);
      expect(fixture.componentInstance.currentPage()).toBe(2);
      expect(fixture.componentInstance.brands().map(b => b.id)).toEqual(['a', 'b']);
      expect(fixture.componentInstance.hasNextPage()).toBeFalse();
    });
  });
});
