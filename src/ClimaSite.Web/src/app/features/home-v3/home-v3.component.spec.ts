import { signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ThemeService } from '../../core/services/theme.service';
import { HomeV3Component } from './home-v3.component';
import type { RecommendedProduct } from './models/home-v3.models';
import { HomeWizardStateService } from './services/home-wizard-state.service';
import { ProductRecommendationsService } from './services/product-recommendations.service';

describe('HomeV3Component', () => {
  let fixture: ComponentFixture<HomeV3Component>;
  let component: HomeV3Component;
  let state: HomeWizardStateService;
  let recommendationsService: jasmine.SpyObj<ProductRecommendationsService>;

  const createProduct = (name: string): RecommendedProduct => ({
    id: name,
    name,
    slug: name.toLowerCase(),
    basePrice: 100,
    isOnSale: false,
    discountPercentage: 0,
    averageRating: 0,
    reviewCount: 0,
    inStock: true,
    score: 0.9,
    matchReason: 'homeV3.matchReason.perfectFit',
    isInverter: true
  });

  beforeEach(async () => {
    recommendationsService = jasmine.createSpyObj<ProductRecommendationsService>('ProductRecommendationsService', [
      'getRecommendations'
    ]);

    await TestBed.configureTestingModule({
      imports: [HomeV3Component],
      providers: [
        { provide: ProductRecommendationsService, useValue: recommendationsService },
        { provide: ThemeService, useValue: { isDarkMode: signal(false) } }
      ]
    })
      .overrideComponent(HomeV3Component, { set: { template: '' } })
      .compileComponents();

    state = TestBed.inject(HomeWizardStateService);
  });

  it('ignores stale recommendation responses after wizard state changes', fakeAsync(() => {
    const firstRequest = new Subject<RecommendedProduct[]>();
    const secondRequest = new Subject<RecommendedProduct[]>();
    recommendationsService.getRecommendations.and.returnValues(firstRequest, secondRequest);

    fixture = TestBed.createComponent(HomeV3Component);
    component = fixture.componentInstance;
    fixture.detectChanges();

    tick(350);
    expect(recommendationsService.getRecommendations).toHaveBeenCalledOnceWith(24, 'living', 'B');

    state.setArea(36);
    fixture.detectChanges();
    tick(350);

    expect(firstRequest.observed).toBeFalse();
    expect(recommendationsService.getRecommendations).toHaveBeenCalledTimes(2);
    expect(recommendationsService.getRecommendations).toHaveBeenCalledWith(36, 'living', 'B');

    firstRequest.next([createProduct('stale')]);
    tick();
    expect(component.recommendations()).toBeNull();

    secondRequest.next([createProduct('fresh')]);
    tick();
    expect(component.recommendations()?.[0].name).toBe('fresh');
  }));
});
