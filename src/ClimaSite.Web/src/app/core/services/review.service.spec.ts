import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ReviewService } from './review.service';
import { Review, ReviewSummary, CreateReviewRequest, PaginatedReviews } from '../models/review.model';
import { environment } from '../../../environments/environment';

describe('ReviewService', () => {
  let service: ReviewService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/reviews`;

  const mockReview: Review = {
    id: 'review-1',
    productId: 'product-1',
    userId: 'user-1',
    userName: 'Jane Doe',
    rating: 5,
    title: 'Excellent unit',
    content: 'Cools the room fast and quietly.',
    status: 'Approved',
    isVerifiedPurchase: true,
    helpfulCount: 3,
    unhelpfulCount: 0,
    adminResponse: null,
    adminRespondedAt: null,
    createdAt: '2025-01-01T00:00:00Z'
  };

  const mockPaginatedReviews: PaginatedReviews = {
    items: [mockReview],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 1
  };

  const mockSummary: ReviewSummary = {
    productId: 'product-1',
    averageRating: 4.5,
    totalReviews: 10,
    ratingDistribution: { 5: 6, 4: 2, 3: 1, 2: 1, 1: 0 }
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ReviewService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ReviewService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProductReviews', () => {
    it('should GET reviews for a product with default paging and sort', () => {
      let result: PaginatedReviews | undefined;
      service.getProductReviews('product-1').subscribe(r => (result = r));

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/product/product-1`
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.get('sortBy')).toBe('newest');
      req.flush(mockPaginatedReviews);

      expect(result).toEqual(mockPaginatedReviews);
      expect(result?.items.length).toBe(1);
    });

    it('should pass through explicit paging and sort parameters', () => {
      service.getProductReviews('product-9', 3, 25, 'rating_high').subscribe();

      const req = httpMock.expectOne(r =>
        r.url === `${apiUrl}/product/product-9`
      );
      expect(req.request.params.get('pageNumber')).toBe('3');
      expect(req.request.params.get('pageSize')).toBe('25');
      expect(req.request.params.get('sortBy')).toBe('rating_high');
      req.flush(mockPaginatedReviews);
    });

    it('should map an empty result set', () => {
      let result: PaginatedReviews | undefined;
      const empty: PaginatedReviews = { items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0 };
      service.getProductReviews('product-empty').subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/product/product-empty`);
      req.flush(empty);

      expect(result?.items).toEqual([]);
      expect(result?.totalCount).toBe(0);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.getProductReviews('product-1').subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/product/product-1`);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });

  describe('getProductReviewSummary', () => {
    it('should GET the review summary for a product', () => {
      let result: ReviewSummary | undefined;
      service.getProductReviewSummary('product-1').subscribe(r => (result = r));

      const req = httpMock.expectOne(`${apiUrl}/product/product-1/summary`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSummary);

      expect(result).toEqual(mockSummary);
      expect(result?.ratingDistribution[5]).toBe(6);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.getProductReviewSummary('product-1').subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(`${apiUrl}/product/product-1/summary`);
      req.flush('nope', { status: 404, statusText: 'Not Found' });

      expect(errored).toBeTrue();
    });
  });

  describe('createReview', () => {
    it('should POST the create-review request body and return the created review', () => {
      const request: CreateReviewRequest = {
        productId: 'product-1',
        rating: 5,
        title: 'Great',
        content: 'Loved it'
      };
      let result: Review | undefined;
      service.createReview(request).subscribe(r => (result = r));

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockReview);

      expect(result).toEqual(mockReview);
    });

    it('should POST a request without optional title/content', () => {
      const request: CreateReviewRequest = { productId: 'product-2', rating: 3 };
      service.createReview(request).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body).toEqual({ productId: 'product-2', rating: 3 });
      expect(req.request.body.title).toBeUndefined();
      req.flush(mockReview);
    });

    it('should propagate validation errors', () => {
      let status = 0;
      service.createReview({ productId: 'product-1', rating: 0 }).subscribe({
        error: (e) => (status = e.status)
      });

      const req = httpMock.expectOne(apiUrl);
      req.flush('invalid', { status: 400, statusText: 'Bad Request' });

      expect(status).toBe(400);
    });
  });

  describe('markHelpful', () => {
    it('should POST to the helpful endpoint with an empty body', () => {
      let completed = false;
      service.markHelpful('review-1').subscribe(() => (completed = true));

      const req = httpMock.expectOne(`${apiUrl}/review-1/helpful`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(null);

      expect(completed).toBeTrue();
    });

    it('should propagate errors', () => {
      let errored = false;
      service.markHelpful('review-1').subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(`${apiUrl}/review-1/helpful`);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });

  describe('markUnhelpful', () => {
    it('should POST to the unhelpful endpoint with an empty body', () => {
      let completed = false;
      service.markUnhelpful('review-7').subscribe(() => (completed = true));

      const req = httpMock.expectOne(`${apiUrl}/review-7/unhelpful`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(null);

      expect(completed).toBeTrue();
    });

    it('should propagate errors', () => {
      let errored = false;
      service.markUnhelpful('review-7').subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(`${apiUrl}/review-7/unhelpful`);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });
});
