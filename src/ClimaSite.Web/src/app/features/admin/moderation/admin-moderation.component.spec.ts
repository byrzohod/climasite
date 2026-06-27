import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';

import { AdminModerationComponent } from './admin-moderation.component';
import {
  ModerationService,
  AdminQuestion,
  AdminAnswer,
  AdminReview,
  PendingModeration,
  PendingReviewModeration
} from '../../../core/services/moderation.service';

/**
 * Plan-19 B2 (batch 2): unit coverage for the admin Q&A / review moderation page.
 *
 * The component drives the backend through ModerationService and reflects results in signals
 * (data, reviewData, loading, error, activeTab, processingId). loadData() fans out two calls
 * via forkJoin, and each approve/reject sets processingId, calls the service, then either
 * reloads (success) or just clears the spinner (failure). We provide a jasmine spy double and
 * instantiate the component without rendering the template (RouterLink + ngx-translate),
 * mirroring checkout.component.spec.ts / cart.component.spec.ts. Spy observables resolve
 * synchronously so the subscribe completes within the method call.
 */

function makeQuestion(overrides: Partial<AdminQuestion> = {}): AdminQuestion {
  return {
    id: 'q-1',
    productId: 'p-1',
    productName: 'Test AC',
    productSlug: 'test-ac',
    questionText: 'Is it quiet?',
    askerName: 'Asker',
    askerEmail: 'asker@test.com',
    status: 'Pending',
    helpfulCount: 0,
    createdAt: '2026-06-01T00:00:00Z',
    answeredAt: null,
    totalAnswers: 0,
    pendingAnswers: 0,
    ...overrides
  };
}

function makeAnswer(overrides: Partial<AdminAnswer> = {}): AdminAnswer {
  return {
    id: 'a-1',
    questionId: 'q-1',
    questionText: 'Is it quiet?',
    productName: 'Test AC',
    productSlug: 'test-ac',
    answerText: 'Yes, very.',
    answererName: 'Expert',
    isOfficial: false,
    status: 'Pending',
    helpfulCount: 0,
    unhelpfulCount: 0,
    createdAt: '2026-06-01T00:00:00Z',
    ...overrides
  };
}

function makeReview(overrides: Partial<AdminReview> = {}): AdminReview {
  return {
    id: 'r-1',
    productId: 'p-1',
    productName: 'Test AC',
    productSlug: 'test-ac',
    rating: 5,
    title: 'Great',
    content: 'Works well.',
    reviewerName: 'Buyer',
    reviewerEmail: 'buyer@test.com',
    isVerifiedPurchase: true,
    status: 'Pending',
    helpfulCount: 0,
    createdAt: '2026-06-01T00:00:00Z',
    ...overrides
  };
}

function makePending(overrides: Partial<PendingModeration> = {}): PendingModeration {
  return {
    pendingQuestions: 1,
    pendingAnswers: 1,
    questions: [makeQuestion()],
    answers: [makeAnswer()],
    ...overrides
  };
}

function makePendingReviews(overrides: Partial<PendingReviewModeration> = {}): PendingReviewModeration {
  return {
    pendingReviews: 1,
    reviews: [makeReview()],
    ...overrides
  };
}

function setup(): {
  component: AdminModerationComponent;
  service: jasmine.SpyObj<ModerationService>;
} {
  const service = jasmine.createSpyObj<ModerationService>('ModerationService', [
    'getPendingQA',
    'getPendingReviews',
    'approveQuestion',
    'rejectQuestion',
    'approveAnswer',
    'rejectAnswer',
    'approveReview',
    'rejectReview'
  ]);
  service.getPendingQA.and.returnValue(of(makePending()));
  service.getPendingReviews.and.returnValue(of(makePendingReviews()));
  const okResult = of({ success: true, message: 'ok' });
  service.approveQuestion.and.returnValue(okResult);
  service.rejectQuestion.and.returnValue(okResult);
  service.approveAnswer.and.returnValue(okResult);
  service.rejectAnswer.and.returnValue(okResult);
  service.approveReview.and.returnValue(okResult);
  service.rejectReview.and.returnValue(okResult);

  TestBed.configureTestingModule({
    providers: [{ provide: ModerationService, useValue: service }]
  });

  const component = TestBed.runInInjectionContext(() => new AdminModerationComponent());
  return { component, service };
}

describe('AdminModerationComponent', () => {
  it('creates and defaults to the questions tab', () => {
    const { component } = setup();
    expect(component).toBeTruthy();
    expect(component.activeTab()).toBe('questions');
  });

  describe('ngOnInit / loadData', () => {
    it('fans out both pending fetches and populates data + reviewData on success', () => {
      const { component, service } = setup();
      component.ngOnInit();

      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
      expect(service.getPendingReviews).toHaveBeenCalledTimes(1);
      expect(component.data()?.pendingQuestions).toBe(1);
      expect(component.reviewData()?.pendingReviews).toBe(1);
      expect(component.loading()).toBeFalse();
      expect(component.error()).toBeNull();
    });

    it('surfaces a load-error key and clears loading when either fetch fails', () => {
      const { component, service } = setup();
      service.getPendingReviews.and.returnValue(throwError(() => new Error('500')));

      component.loadData();

      expect(component.error()).toBe('admin.moderation.errors.loadFailed');
      expect(component.loading()).toBeFalse();
      // forkJoin errors before emitting, so neither signal is populated.
      expect(component.data()).toBeNull();
      expect(component.reviewData()).toBeNull();
    });

    it('clears a prior error on a successful retry', () => {
      const { component, service } = setup();
      service.getPendingQA.and.returnValue(throwError(() => new Error('500')));
      component.loadData();
      expect(component.error()).toBe('admin.moderation.errors.loadFailed');

      service.getPendingQA.and.returnValue(of(makePending()));
      component.loadData();

      expect(component.error()).toBeNull();
      expect(component.data()).not.toBeNull();
    });

    it('handles empty pending lists without error', () => {
      const { component, service } = setup();
      service.getPendingQA.and.returnValue(
        of(makePending({ pendingQuestions: 0, pendingAnswers: 0, questions: [], answers: [] }))
      );
      service.getPendingReviews.and.returnValue(
        of(makePendingReviews({ pendingReviews: 0, reviews: [] }))
      );

      component.loadData();

      expect(component.error()).toBeNull();
      expect(component.data()?.questions).toEqual([]);
      expect(component.reviewData()?.reviews).toEqual([]);
    });
  });

  describe('setTab', () => {
    it('switches the active tab', () => {
      const { component } = setup();
      component.setTab('answers');
      expect(component.activeTab()).toBe('answers');
      component.setTab('reviews');
      expect(component.activeTab()).toBe('reviews');
      component.setTab('questions');
      expect(component.activeTab()).toBe('questions');
    });
  });

  describe('approveQuestion', () => {
    it('marks the item processing during the call and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();
      service.getPendingReviews.calls.reset();

      component.approveQuestion(makeQuestion({ id: 'q-9' }));

      expect(service.approveQuestion).toHaveBeenCalledOnceWith('q-9');
      // Spinner cleared and a fresh reload triggered after success.
      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
    });

    it('sets processingId to the item id while the call is in flight', () => {
      const { component, service } = setup();
      // Never-completing observable keeps the call in flight.
      service.approveQuestion.and.returnValue(
        new Subject<{ success: boolean; message: string }>().asObservable()
      );

      component.approveQuestion(makeQuestion({ id: 'q-inflight' }));

      expect(component.processingId()).toBe('q-inflight');
    });

    it('clears the spinner and does NOT reload on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();
      service.approveQuestion.and.returnValue(throwError(() => new Error('500')));

      component.approveQuestion(makeQuestion({ id: 'q-err' }));

      expect(component.processingId()).toBeNull();
      // Failure must not refetch the list (the optimistic spinner just reverts).
      expect(service.getPendingQA).not.toHaveBeenCalled();
    });
  });

  describe('rejectQuestion', () => {
    it('rejects, clears the spinner, and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();

      component.rejectQuestion(makeQuestion({ id: 'q-5' }));

      expect(service.rejectQuestion).toHaveBeenCalledOnceWith('q-5');
      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
    });

    it('clears the spinner without reloading on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();
      service.rejectQuestion.and.returnValue(throwError(() => new Error('500')));

      component.rejectQuestion(makeQuestion({ id: 'q-5' }));

      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).not.toHaveBeenCalled();
    });
  });

  describe('approveAnswer', () => {
    it('passes the markAsOfficial=false flag and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();

      component.approveAnswer(makeAnswer({ id: 'a-7' }), false);

      expect(service.approveAnswer).toHaveBeenCalledOnceWith('a-7', false);
      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
    });

    it('passes markAsOfficial=true for the "approve as official" action', () => {
      const { component, service } = setup();
      component.ngOnInit();

      component.approveAnswer(makeAnswer({ id: 'a-7' }), true);

      expect(service.approveAnswer).toHaveBeenCalledOnceWith('a-7', true);
    });

    it('clears the spinner without reloading on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();
      service.approveAnswer.and.returnValue(throwError(() => new Error('500')));

      component.approveAnswer(makeAnswer({ id: 'a-7' }), false);

      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).not.toHaveBeenCalled();
    });
  });

  describe('rejectAnswer', () => {
    it('rejects the answer and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();

      component.rejectAnswer(makeAnswer({ id: 'a-3' }));

      expect(service.rejectAnswer).toHaveBeenCalledOnceWith('a-3');
      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
    });

    it('clears the spinner without reloading on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingQA.calls.reset();
      service.rejectAnswer.and.returnValue(throwError(() => new Error('500')));

      component.rejectAnswer(makeAnswer({ id: 'a-3' }));

      expect(component.processingId()).toBeNull();
      expect(service.getPendingQA).not.toHaveBeenCalled();
    });
  });

  describe('approveReview', () => {
    it('approves the review and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingReviews.calls.reset();
      service.getPendingQA.calls.reset();

      component.approveReview(makeReview({ id: 'r-2' }));

      expect(service.approveReview).toHaveBeenCalledOnceWith('r-2');
      expect(component.processingId()).toBeNull();
      // loadData refetches both lists.
      expect(service.getPendingReviews).toHaveBeenCalledTimes(1);
      expect(service.getPendingQA).toHaveBeenCalledTimes(1);
    });

    it('clears the spinner without reloading on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingReviews.calls.reset();
      service.approveReview.and.returnValue(throwError(() => new Error('500')));

      component.approveReview(makeReview({ id: 'r-2' }));

      expect(component.processingId()).toBeNull();
      expect(service.getPendingReviews).not.toHaveBeenCalled();
    });
  });

  describe('rejectReview', () => {
    it('rejects the review and reloads on success', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingReviews.calls.reset();

      component.rejectReview(makeReview({ id: 'r-4' }));

      expect(service.rejectReview).toHaveBeenCalledOnceWith('r-4');
      expect(component.processingId()).toBeNull();
      expect(service.getPendingReviews).toHaveBeenCalledTimes(1);
    });

    it('clears the spinner without reloading on failure', () => {
      const { component, service } = setup();
      component.ngOnInit();
      service.getPendingReviews.calls.reset();
      service.rejectReview.and.returnValue(throwError(() => new Error('500')));

      component.rejectReview(makeReview({ id: 'r-4' }));

      expect(component.processingId()).toBeNull();
      expect(service.getPendingReviews).not.toHaveBeenCalled();
    });
  });
});
