import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  ModerationService,
  PendingModeration,
  PendingReviewModeration,
  AdminQuestion,
  AdminAnswer,
  AdminReview
} from './moderation.service';
import { environment } from '../../../environments/environment';

describe('ModerationService', () => {
  let service: ModerationService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/api/admin`;

  const mockQuestion: AdminQuestion = {
    id: 'q-1',
    productId: 'product-1',
    productName: 'Test AC',
    productSlug: 'test-ac',
    questionText: 'Does it heat?',
    askerName: 'Bob',
    askerEmail: 'bob@example.com',
    status: 'Pending',
    helpfulCount: 0,
    createdAt: '2025-01-01T00:00:00Z',
    answeredAt: null,
    totalAnswers: 0,
    pendingAnswers: 0
  };

  const mockAnswer: AdminAnswer = {
    id: 'a-1',
    questionId: 'q-1',
    questionText: 'Does it heat?',
    productName: 'Test AC',
    productSlug: 'test-ac',
    answerText: 'Yes, it has a heat pump.',
    answererName: 'Support',
    isOfficial: true,
    status: 'Pending',
    helpfulCount: 0,
    unhelpfulCount: 0,
    createdAt: '2025-01-01T00:00:00Z'
  };

  const mockReview: AdminReview = {
    id: 'r-1',
    productId: 'product-1',
    productName: 'Test AC',
    productSlug: 'test-ac',
    rating: 4,
    title: 'Good',
    content: 'Works well',
    reviewerName: 'Alice',
    reviewerEmail: 'alice@example.com',
    isVerifiedPurchase: true,
    status: 'Pending',
    helpfulCount: 0,
    createdAt: '2025-01-01T00:00:00Z'
  };

  const mockPendingQA: PendingModeration = {
    pendingQuestions: 1,
    pendingAnswers: 1,
    questions: [mockQuestion],
    answers: [mockAnswer]
  };

  const mockPendingReviews: PendingReviewModeration = {
    pendingReviews: 1,
    reviews: [mockReview]
  };

  const successResult = { success: true, message: 'ok' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ModerationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ModerationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getPendingQA', () => {
    it('should GET pending Q&A with default paging', () => {
      let result: PendingModeration | undefined;
      service.getPendingQA().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${baseUrl}/questions/pending`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(mockPendingQA);

      expect(result).toEqual(mockPendingQA);
      expect(result?.questions.length).toBe(1);
      expect(result?.answers.length).toBe(1);
    });

    it('should pass explicit paging parameters', () => {
      service.getPendingQA(2, 50).subscribe();

      const req = httpMock.expectOne(r => r.url === `${baseUrl}/questions/pending`);
      expect(req.request.params.get('pageNumber')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('50');
      req.flush(mockPendingQA);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.getPendingQA().subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(r => r.url === `${baseUrl}/questions/pending`);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });

  describe('approveQuestion', () => {
    it('should POST to the approve endpoint with an empty body', () => {
      let result: { success: boolean; message: string } | undefined;
      service.approveQuestion('q-1').subscribe(r => (result = r));

      const req = httpMock.expectOne(`${baseUrl}/questions/q-1/approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(successResult);

      expect(result).toEqual(successResult);
    });
  });

  describe('rejectQuestion', () => {
    it('should POST a note when supplied', () => {
      service.rejectQuestion('q-1', 'spam').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/q-1/reject`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ note: 'spam' });
      req.flush(successResult);
    });

    it('should POST an undefined note when omitted', () => {
      service.rejectQuestion('q-1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/q-1/reject`);
      expect(req.request.body).toEqual({ note: undefined });
      req.flush(successResult);
    });
  });

  describe('flagQuestion', () => {
    it('should POST a note to the flag endpoint', () => {
      service.flagQuestion('q-1', 'needs review').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/q-1/flag`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ note: 'needs review' });
      req.flush(successResult);
    });
  });

  describe('approveAnswer', () => {
    it('should POST markAsOfficial when supplied', () => {
      service.approveAnswer('a-1', true).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/answers/a-1/approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ markAsOfficial: true });
      req.flush(successResult);
    });

    it('should POST an undefined markAsOfficial when omitted', () => {
      service.approveAnswer('a-1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/answers/a-1/approve`);
      expect(req.request.body).toEqual({ markAsOfficial: undefined });
      req.flush(successResult);
    });
  });

  describe('rejectAnswer', () => {
    it('should POST a note to the answer reject endpoint', () => {
      service.rejectAnswer('a-1', 'off topic').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/answers/a-1/reject`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ note: 'off topic' });
      req.flush(successResult);
    });
  });

  describe('flagAnswer', () => {
    it('should POST a note to the answer flag endpoint', () => {
      service.flagAnswer('a-1', 'suspicious').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/answers/a-1/flag`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ note: 'suspicious' });
      req.flush(successResult);
    });
  });

  describe('bulkApproveQuestions', () => {
    it('should POST the id list and return approved/failed counts', () => {
      const ids = ['q-1', 'q-2', 'q-3'];
      let result: { approved: number; failed: number } | undefined;
      service.bulkApproveQuestions(ids).subscribe(r => (result = r));

      const req = httpMock.expectOne(`${baseUrl}/questions/bulk-approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ ids });
      req.flush({ approved: 2, failed: 1 });

      expect(result).toEqual({ approved: 2, failed: 1 });
    });

    it('should handle an empty id list', () => {
      service.bulkApproveQuestions([]).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/questions/bulk-approve`);
      expect(req.request.body).toEqual({ ids: [] });
      req.flush({ approved: 0, failed: 0 });
    });
  });

  describe('bulkApproveAnswers', () => {
    it('should POST the id list to the answers bulk-approve endpoint', () => {
      const ids = ['a-1', 'a-2'];
      let result: { approved: number; failed: number } | undefined;
      service.bulkApproveAnswers(ids).subscribe(r => (result = r));

      const req = httpMock.expectOne(`${baseUrl}/questions/answers/bulk-approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ ids });
      req.flush({ approved: 2, failed: 0 });

      expect(result).toEqual({ approved: 2, failed: 0 });
    });
  });

  describe('getPendingReviews', () => {
    it('should GET pending reviews with default paging', () => {
      let result: PendingReviewModeration | undefined;
      service.getPendingReviews().subscribe(r => (result = r));

      const req = httpMock.expectOne(r => r.url === `${baseUrl}/reviews/pending`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('pageNumber')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(mockPendingReviews);

      expect(result).toEqual(mockPendingReviews);
      expect(result?.reviews.length).toBe(1);
    });

    it('should pass explicit paging parameters', () => {
      service.getPendingReviews(4, 5).subscribe();

      const req = httpMock.expectOne(r => r.url === `${baseUrl}/reviews/pending`);
      expect(req.request.params.get('pageNumber')).toBe('4');
      expect(req.request.params.get('pageSize')).toBe('5');
      req.flush(mockPendingReviews);
    });
  });

  describe('approveReview', () => {
    it('should POST to the review approve endpoint with an empty body', () => {
      let result: { success: boolean; message: string } | undefined;
      service.approveReview('r-1').subscribe(r => (result = r));

      const req = httpMock.expectOne(`${baseUrl}/reviews/r-1/approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(successResult);

      expect(result).toEqual(successResult);
    });
  });

  describe('rejectReview', () => {
    it('should POST a note to the review reject endpoint', () => {
      service.rejectReview('r-1', 'fake review').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/reviews/r-1/reject`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ note: 'fake review' });
      req.flush(successResult);
    });

    it('should POST an undefined note when omitted', () => {
      service.rejectReview('r-1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/reviews/r-1/reject`);
      expect(req.request.body).toEqual({ note: undefined });
      req.flush(successResult);
    });

    it('should propagate errors', () => {
      let errored = false;
      service.rejectReview('r-1', 'bad').subscribe({ error: () => (errored = true) });

      const req = httpMock.expectOne(`${baseUrl}/reviews/r-1/reject`);
      req.flush('boom', { status: 500, statusText: 'Server Error' });

      expect(errored).toBeTrue();
    });
  });
});
