import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule, TranslateLoader, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component, signal } from '@angular/core';
import { ProductReviewsComponent } from './product-reviews.component';
import { AuthService } from '../../../auth/services/auth.service';
import { PaginatedReviews, ReviewSummary } from '../../../core/models/review.model';
import { environment } from '../../../../environments/environment';

class MockAuthService {
  isAuthenticated = signal(true);
  user = signal({ id: 'test-user-id', firstName: 'Test', lastName: 'User', email: 'test@test.com' });
}

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({
      'reviews.submitSuccess': 'Thank you! Your review has been posted.',
      'reviews.loginNow': 'Log in now',
      'reviews.loginToReview': 'Log in to write a review',
      'reviews.writeReview': 'Write a Review',
      'reviews.submitReview': 'Submit Review',
      'reviews.submitError': 'Could not submit review',
      'reviews.sessionExpired': 'Session expired',
      'common.loading': 'Loading...',
      'common.close': 'Close'
    });
  }
}

@Component({
  standalone: true,
  imports: [ProductReviewsComponent],
  template: '<app-product-reviews [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('ProductReviewsComponent', () => {
  let component: ProductReviewsComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const emptyReviews: PaginatedReviews = {
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 1
  };

  const emptySummary: ReviewSummary = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    averageRating: 0,
    totalReviews: 0,
    ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        ProductReviewsComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: AuthService, useClass: MockAuthService },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.use('en');

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  /** Flushes the ngOnInit loadReviews()/loadSummary() requests. */
  function flushInitialLoad(): void {
    httpMock.expectOne(req => req.url.includes('/api/reviews/product/') && req.url.endsWith('/summary'))
      .flush(emptySummary);
    httpMock.expectOne(req =>
      req.url.includes('/api/reviews/product/') && !req.url.endsWith('/summary'))
      .flush(emptyReviews);
  }

  it('should create', () => {
    fixture.detectChanges();
    flushInitialLoad();
    expect(component).toBeTruthy();
  });

  it('should show the success banner after a review is submitted', fakeAsync(() => {
    fixture.detectChanges();
    flushInitialLoad();
    tick();
    fixture.detectChanges();

    component.showReviewForm.set(true);
    component.setRating(5);
    fixture.detectChanges();

    component.submitReview();

    const createReq = httpMock.expectOne(`${environment.apiUrl}/api/reviews`);
    expect(createReq.request.method).toBe('POST');
    // Auto-approved review comes back with Approved status.
    createReq.flush({ id: 'new-review', status: 'Approved', rating: 5, isVerifiedPurchase: false });
    tick();
    fixture.detectChanges();

    // The success signal flips on, and the banner renders with its testid.
    expect(component.reviewSubmittedSuccess()).toBeTrue();
    const banner = fixture.nativeElement.querySelector('[data-testid="review-submit-success"]');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('Thank you! Your review has been posted.');

    // submitReview() also reloads the list + summary so the new review appears.
    httpMock.expectOne(req => req.url.includes('/api/reviews/product/') && req.url.endsWith('/summary'))
      .flush(emptySummary);
    httpMock.expectOne(req =>
      req.url.includes('/api/reviews/product/') && !req.url.endsWith('/summary'))
      .flush(emptyReviews);

    // Auto-dismiss after the timeout clears the banner.
    tick(5000);
    fixture.detectChanges();
    expect(component.reviewSubmittedSuccess()).toBeFalse();
    expect(fixture.nativeElement.querySelector('[data-testid="review-submit-success"]')).toBeFalsy();
  }));

  it('should dismiss the success banner when the close button is clicked', fakeAsync(() => {
    fixture.detectChanges();
    flushInitialLoad();
    tick();

    component.reviewSubmittedSuccess.set(true);
    fixture.detectChanges();

    const dismiss = fixture.nativeElement.querySelector('[data-testid="review-submit-success-dismiss"]');
    expect(dismiss).toBeTruthy();
    dismiss.click();
    fixture.detectChanges();

    expect(component.reviewSubmittedSuccess()).toBeFalse();
  }));

  it('should point the login link to /login (not /auth/login)', fakeAsync(() => {
    (component as unknown as { authService: MockAuthService }).authService.isAuthenticated.set(false);
    fixture.detectChanges();
    flushInitialLoad();
    tick();
    fixture.detectChanges();

    const loginLink: HTMLAnchorElement | null =
      fixture.nativeElement.querySelector('[data-testid="reviews-login-link"]');
    expect(loginLink).toBeTruthy();
    // routerLink resolves to the href attribute in tests.
    expect(loginLink!.getAttribute('href')).toBe('/login');
  }));
});
