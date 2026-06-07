import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import type { RecommendedProduct } from '../../models/home-v3.models';
import { RecommendationsComponent } from './recommendations.component';

class TestTranslateLoader implements TranslateLoader {
  getTranslation(): Observable<Record<string, string>> {
    return of({
      'homeV3.recommendations.eyebrow': 'Recommended for you',
      'homeV3.recommendations.title': 'Best matches',
      'homeV3.recommendations.lede': 'Products sized for this room.',
      'homeV3.recommendations.bestMatch': 'Best match',
      'homeV3.recommendations.viewProduct': 'View {{name}}',
      'homeV3.recommendations.btuUnit': 'BTU/h',
      'homeV3.recommendations.inverter': 'Inverter',
      'homeV3.recommendations.noiseUnit': 'dB',
      'homeV3.recommendations.viewDetails': 'View details',
      'homeV3.recommendations.empty': 'No matching products yet.',
      'homeV3.recommendations.loadError': 'Could not load recommendations.',
      'homeV3.matchReason.perfectFit': 'Perfect fit for this room.',
    });
  }
}

describe('RecommendationsComponent', () => {
  let fixture: ComponentFixture<RecommendationsComponent>;

  const product: RecommendedProduct = {
    id: 'product-1',
    name: 'Nordic Comfort 12',
    slug: 'nordic-comfort-12',
    basePrice: 1200,
    salePrice: 999,
    isOnSale: true,
    discountPercentage: 17,
    brand: 'ClimaPro',
    averageRating: 4.8,
    reviewCount: 32,
    inStock: true,
    score: 0.97,
    matchReason: 'homeV3.matchReason.perfectFit',
    btuCapacity: 12000,
    isInverter: true,
    noiseLevel: 22
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RecommendationsComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TestTranslateLoader
          }
        })
      ],
      providers: [provideRouter([])]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setDefaultLang('en');
    translate.use('en');

    fixture = TestBed.createComponent(RecommendationsComponent);
  });

  it('renders loading skeleton cards without exposing them to assistive tech', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const skeletons = fixture.debugElement.queryAll(By.css('.rec-card.skeleton'));
    expect(skeletons.length).toBe(3);
    expect(skeletons[0].nativeElement.getAttribute('aria-hidden')).toBe('true');
  });

  it('renders a translated error key as an alert', () => {
    fixture.componentRef.setInput('error', 'homeV3.recommendations.loadError');
    fixture.detectChanges();

    const alert = fixture.debugElement.query(By.css('[role="alert"]')).nativeElement as HTMLElement;
    expect(alert.textContent?.trim()).toBe('Could not load recommendations.');
  });

  it('renders recommendation cards with product links and localized match reasons', () => {
    fixture.componentRef.setInput('products', [product]);
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.css('[data-testid="home-v3-rec-card-0"]')).nativeElement as HTMLElement;
    const title = fixture.debugElement.query(By.css('[data-testid="home-v3-rec-title-0"]')).nativeElement as HTMLAnchorElement;
    const cta = fixture.debugElement.query(By.css('[data-testid="home-v3-rec-cta-0"]')).nativeElement as HTMLAnchorElement;

    expect(card.classList.contains('featured')).toBeTrue();
    expect(title.textContent?.trim()).toBe('Nordic Comfort 12');
    expect(title.getAttribute('href')).toBe('/products/nordic-comfort-12');
    expect(cta.getAttribute('href')).toBe('/products/nordic-comfort-12');
    expect(cta.textContent?.trim()).toBe('View details');
    expect(card.textContent).toContain('Perfect fit for this room.');
    expect(card.textContent).not.toContain('homeV3.matchReason.perfectFit');
    expect(card.textContent).toContain('ClimaPro');
  });

  it('renders the empty state when recommendations are loaded but empty', () => {
    fixture.componentRef.setInput('products', []);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.rec-empty')).nativeElement.textContent).toContain('No matching products yet.');
  });
});
