import { ComponentFixture, TestBed, fakeAsync, tick, flush } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component } from '@angular/core';
import { PriceHistoryChartComponent } from './price-history-chart.component';
import { PriceHistoryService, ProductPriceHistory } from '../../services/price-history.service';
import { environment } from '../../../../../environments/environment';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<Record<string, string>> {
    return of({
      'products.priceHistory.title': 'Price History',
      'products.priceHistory.days30': '30 Days',
      'products.priceHistory.days60': '60 Days',
      'products.priceHistory.days90': '90 Days',
      'products.priceHistory.current': 'Current',
      'products.priceHistory.lowest': 'Lowest',
      'products.priceHistory.highest': 'Highest',
      'products.priceHistory.average': 'Average',
      'products.priceHistory.noHistory': 'No price history available.',
      'products.priceHistory.notAvailable': 'Price history not available.',
      'common.loading': 'Loading...'
    });
  }
}

@Component({
  standalone: true,
  imports: [PriceHistoryChartComponent],
  template: '<app-price-history-chart [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('PriceHistoryChartComponent', () => {
  let component: PriceHistoryChartComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const mockPriceHistory: ProductPriceHistory = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    productName: 'Test AC Unit',
    currentPrice: 599.99,
    currentCompareAtPrice: 699.99,
    lowestPrice: 549.99,
    highestPrice: 699.99,
    averagePrice: 599.99,
    pricePoints: [
      {
        date: '2024-01-01T00:00:00Z',
        price: 699.99,
        reason: 'Initial'
      },
      {
        date: '2024-01-15T00:00:00Z',
        price: 549.99,
        reason: 'Promotion'
      },
      {
        date: '2024-02-01T00:00:00Z',
        price: 599.99,
        reason: 'PromotionEnd'
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        PriceHistoryChartComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        PriceHistoryService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    expect(component).toBeTruthy();
  }));

  it('should load price history on init', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    expect(component.priceHistory()).toEqual(mockPriceHistory);
    expect(component.loading()).toBeFalsy();
  }));

  it('should display price stats', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    const stats = fixture.nativeElement.querySelectorAll('.stat');
    expect(stats.length).toBe(4);
  }));

  it('should update selected period when changePeriod is called', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    expect(component.selectedPeriod()).toBe(90);

    component.changePeriod(30);
    expect(component.selectedPeriod()).toBe(30);

    component.changePeriod(60);
    expect(component.selectedPeriod()).toBe(60);
  }));

  it('should compute chart points from price history', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    const chartPoints = component.chartPoints();
    expect(chartPoints.length).toBe(3);
    expect(chartPoints[0].price).toBe(699.99);
    expect(chartPoints[1].price).toBe(549.99);
    expect(chartPoints[2].price).toBe(599.99);
  }));

  it('should generate line path', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    const linePath = component.linePath();
    expect(linePath).toBeTruthy();
    expect(linePath.length).toBeGreaterThan(0);
  }));

  it('should show tooltip on hover', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    expect(component.tooltipPoint()).toBeNull();

    const chartPoints = component.chartPoints();
    component.showTooltip(chartPoints[0]);
    expect(component.tooltipPoint()).toEqual(chartPoints[0]);

    component.hideTooltip();
    expect(component.tooltipPoint()).toBeNull();
  }));

  it('should display message when no price history', fakeAsync(() => {
    const emptyHistory = {
      ...mockPriceHistory,
      pricePoints: [
        { date: '2024-01-01T00:00:00Z', price: 599.99, reason: 'Current' }
      ]
    };

    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(emptyHistory);
    tick();
    fixture.detectChanges();

    // With only one point, the chart won't show (needs > 1 points)
    const noDataMessage = fixture.nativeElement.querySelector('.no-data-message');
    expect(noDataMessage).toBeTruthy();
  }));

  it('should show loading state initially', fakeAsync(() => {
    fixture.detectChanges();

    expect(component.loading()).toBeTruthy();
    const loadingElement = fixture.nativeElement.querySelector('.loading-state');
    expect(loadingElement).toBeTruthy();

    const req = httpMock.expectOne(req => req.url.includes('/price-history/'));
    req.flush(mockPriceHistory);
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBeFalsy();
  }));
});
