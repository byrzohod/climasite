import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { PromotionsListComponent } from './promotions-list.component';
import { PromotionService } from '../../../core/services/promotion.service';
import { PromotionBrief, PromotionType } from '../../../core/models/promotion.model';
import { PaginatedResult } from '../../../core/models/product.model';

/**
 * Plan-19 B3: unit coverage for PromotionsListComponent — initial load, empty/loading
 * branches, error fallback (no error signal; just stops loading), the load-more append +
 * hasMore + page-rollback-on-error behaviour, and the getDiscountText() formatter.
 */

function brief(overrides: Partial<PromotionBrief> = {}): PromotionBrief {
  return {
    id: 'promo1',
    name: 'Summer Sale',
    slug: 'summer-sale',
    type: PromotionType.Percentage,
    discountValue: 25,
    startDate: '2026-06-01',
    endDate: '2026-07-01',
    productCount: 5,
    ...overrides
  };
}

function page(items: PromotionBrief[], totalPages = 1, pageNumber = 1): PaginatedResult<PromotionBrief> {
  return {
    items,
    pageNumber,
    pageSize: 12,
    totalCount: items.length,
    totalPages,
    hasPreviousPage: pageNumber > 1,
    hasNextPage: pageNumber < totalPages
  };
}

describe('PromotionsListComponent', () => {
  let promotionService: jasmine.SpyObj<PromotionService>;

  beforeEach(async () => {
    promotionService = jasmine.createSpyObj<PromotionService>('PromotionService', ['getActivePromotions']);
    promotionService.getActivePromotions.and.returnValue(of(page([brief()])));

    await TestBed.configureTestingModule({
      imports: [PromotionsListComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: PromotionService, useValue: promotionService }
      ]
    }).compileComponents();
  });

  function create(): ComponentFixture<PromotionsListComponent> {
    const fixture = TestBed.createComponent(PromotionsListComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('should create', () => {
    expect(create().componentInstance).toBeTruthy();
  });

  it('loads page 1 with the default page size on init', () => {
    const fixture = create();
    expect(promotionService.getActivePromotions).toHaveBeenCalledWith(1, 12);
    expect(fixture.componentInstance.promotions().length).toBe(1);
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });

  it('renders one card per promotion', () => {
    promotionService.getActivePromotions.and.returnValue(
      of(page([brief({ id: 'a' }), brief({ id: 'b', name: 'Winter' })]))
    );
    const fixture = create();
    expect(fixture.nativeElement.querySelectorAll('.promotion-card').length).toBe(2);
  });

  it('shows the loading indicator while the request is in flight', () => {
    promotionService.getActivePromotions.and.returnValue(new Subject<PaginatedResult<PromotionBrief>>());
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
  });

  it('shows the empty state when no promotions are returned', () => {
    promotionService.getActivePromotions.and.returnValue(of(page([])));
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
  });

  it('renders the discount placeholder badge when no images are present', () => {
    promotionService.getActivePromotions.and.returnValue(
      of(page([brief({ bannerImageUrl: undefined, thumbnailImageUrl: undefined, discountValue: 25 })]))
    );
    const fixture = create();
    expect(fixture.nativeElement.querySelector('.placeholder-image .discount-badge')?.textContent).toContain('-25%');
  });

  it('stops loading and shows the empty state when the request errors', () => {
    spyOn(console, 'error');
    promotionService.getActivePromotions.and.returnValue(throwError(() => new Error('boom')));
    const fixture = create();
    expect(fixture.componentInstance.isLoading()).toBeFalse();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
  });

  describe('load more pagination', () => {
    it('renders the load-more button when more pages exist', () => {
      promotionService.getActivePromotions.and.returnValue(of(page([brief()], 2)));
      const fixture = create();
      expect(fixture.componentInstance.hasMore()).toBeTrue();
      expect(fixture.nativeElement.querySelector('.btn-load-more')).toBeTruthy();
    });

    it('appends the next page and updates hasMore', () => {
      promotionService.getActivePromotions.and.returnValue(of(page([brief({ id: 'a' })], 2, 1)));
      const fixture = create();

      promotionService.getActivePromotions.and.returnValue(of(page([brief({ id: 'b' })], 2, 2)));
      fixture.componentInstance.loadMore();

      expect(promotionService.getActivePromotions).toHaveBeenCalledWith(2, 12);
      expect(fixture.componentInstance.promotions().map(p => p.id)).toEqual(['a', 'b']);
      expect(fixture.componentInstance.hasMore()).toBeFalse();
      expect(fixture.componentInstance.isLoadingMore()).toBeFalse();
    });

    it('rolls the page back when loadMore errors', () => {
      spyOn(console, 'error');
      promotionService.getActivePromotions.and.returnValue(of(page([brief({ id: 'a' })], 2, 1)));
      const fixture = create();

      promotionService.getActivePromotions.and.returnValue(throwError(() => new Error('boom')));
      fixture.componentInstance.loadMore();

      // page rolled back to 1, so a subsequent successful loadMore re-requests page 2
      promotionService.getActivePromotions.and.returnValue(of(page([brief({ id: 'b' })], 2, 2)));
      fixture.componentInstance.loadMore();
      expect(promotionService.getActivePromotions).toHaveBeenCalledWith(2, 12);
      expect(fixture.componentInstance.isLoadingMore()).toBeFalse();
    });

    it('ignores loadMore while another load-more is in flight', () => {
      promotionService.getActivePromotions.and.returnValue(of(page([brief()], 2)));
      const fixture = create();
      promotionService.getActivePromotions.and.returnValue(new Subject<PaginatedResult<PromotionBrief>>());

      fixture.componentInstance.loadMore();
      fixture.componentInstance.loadMore();

      // first init call + exactly one in-flight loadMore call
      expect(promotionService.getActivePromotions).toHaveBeenCalledTimes(2);
      expect(fixture.componentInstance.isLoadingMore()).toBeTrue();
    });
  });

  describe('getDiscountText', () => {
    let component: PromotionsListComponent;
    beforeEach(() => { component = create().componentInstance; });

    it('formats a percentage discount', () => {
      expect(component.getDiscountText(brief({ type: PromotionType.Percentage, discountValue: 30 }))).toBe('-30%');
    });

    it('formats a fixed-amount discount', () => {
      expect(component.getDiscountText(brief({ type: PromotionType.FixedAmount, discountValue: 50 }))).toBe('-€50');
    });

    it('labels free shipping', () => {
      expect(component.getDiscountText(brief({ type: PromotionType.FreeShipping }))).toBe('Free Shipping');
    });

    it('labels buy-one-get-one', () => {
      expect(component.getDiscountText(brief({ type: PromotionType.BuyOneGetOne }))).toBe('BOGO');
    });

    it('falls back to SALE for unknown types', () => {
      expect(component.getDiscountText(brief({ type: 'Mystery' as unknown as PromotionType }))).toBe('SALE');
    });
  });
});
