import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ProductQaComponent } from './product-qa.component';
import { QuestionsService, ProductQuestions, Question } from '../../services/questions.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { environment } from '../../../../../environments/environment';

class MockAuthService {
  isAuthenticated = signal(true);
  user = signal({ id: 'test-user-id', firstName: 'Test', lastName: 'User', email: 'test@test.com' });
}

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({
      'products.qa.title': 'Questions & Answers',
      'products.qa.totalQuestions': '{{count}} Questions',
      'products.qa.answeredQuestions': '{{count}} Answered',
      'products.qa.askQuestion': 'Ask a Question',
      'products.qa.yourQuestion': 'Your Question',
      'products.qa.questionPlaceholder': 'Type your question here...',
      'products.qa.yourName': 'Your Name',
      'products.qa.namePlaceholder': 'Optional',
      'products.qa.yourEmail': 'Your Email',
      'products.qa.emailPlaceholder': 'For notifications',
      'products.qa.emailHint': 'We\'ll notify you when answered',
      'products.qa.submitQuestion': 'Submit Question',
      'products.qa.submitting': 'Submitting...',
      'products.qa.questionSubmittedSuccess': 'Question submitted for review!',
      'products.qa.errors.questionTooShort': 'Question must be at least 10 characters',
      'products.qa.errors.sessionExpired': 'Session expired',
      'products.qa.errors.submitQuestionFailed': 'Question submit failed',
      'products.qa.errors.submitAnswerFailed': 'Answer submit failed',
      'products.qa.helpful': 'Helpful',
      'products.qa.markHelpful': 'Mark as helpful',
      'products.qa.markUnhelpful': 'Mark as unhelpful',
      'products.qa.youFoundHelpful': 'You found this helpful',
      'products.qa.youFoundUnhelpful': 'You found this unhelpful',
      'products.qa.loginToVote': 'Log in to vote',
      'products.qa.answer': 'Answer',
      'products.qa.answerPlaceholder': 'Type your answer here...',
      'products.qa.submitAnswer': 'Submit',
      'products.qa.anonymous': 'Anonymous',
      'products.qa.communityMember': 'Community Member',
      'products.qa.officialAnswer': 'Official Answer',
      'products.qa.noAnswersYet': 'No answers yet. Be the first to help!',
      'products.qa.noQuestionsYet': 'No questions yet',
      'products.qa.beFirstToAsk': 'Be the first to ask about this product',
      'products.qa.loginToAsk': 'Log in to ask a question',
      'products.qa.loginNow': 'Log in now',
      'common.loading': 'Loading...',
      'common.previous': 'Previous',
      'common.next': 'Next',
      'common.page': 'Page'
    });
  }
}

@Component({
  standalone: true,
  imports: [ProductQaComponent],
  template: '<app-product-qa [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('ProductQaComponent', () => {
  let component: ProductQaComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const mockQuestions: ProductQuestions = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    totalQuestions: 2,
    answeredQuestions: 1,
    questions: [
      {
        id: 'q1',
        productId: '123e4567-e89b-12d3-a456-426614174000',
        questionText: 'Is this product energy efficient?',
        askerName: 'John Doe',
        helpfulCount: 5,
        createdAt: '2024-01-15T10:00:00Z',
        answeredAt: '2024-01-16T14:00:00Z',
        answerCount: 1,
        hasVotedHelpful: false,
        answers: [
          {
            id: 'a1',
            questionId: 'q1',
            answerText: 'Yes, it has an A++ energy rating.',
            answererName: 'ClimaSite Support',
            isOfficial: true,
            helpfulCount: 10,
            unhelpfulCount: 0,
            createdAt: '2024-01-16T14:00:00Z',
            userVoteHelpful: null
          }
        ]
      },
      {
        id: 'q2',
        productId: '123e4567-e89b-12d3-a456-426614174000',
        questionText: 'Does this come with a warranty?',
        askerName: null,
        helpfulCount: 2,
        createdAt: '2024-01-14T09:00:00Z',
        answeredAt: null,
        answerCount: 0,
        hasVotedHelpful: false,
        answers: []
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        ProductQaComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        QuestionsService,
        { provide: AuthService, useClass: MockAuthService },
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
    // Don't call detectChanges here - let each test handle it
    // since the effect triggers loadQuestions() immediately
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    fixture.detectChanges(); // Trigger the effect
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('should load questions on init', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    expect(component.questions().length).toBe(2);
    expect(component.questionsData()?.totalQuestions).toBe(2);
    expect(component.questionsData()?.answeredQuestions).toBe(1);
  }));

  it('should display questions in template', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const questionCards = fixture.nativeElement.querySelectorAll('.question-card');
    expect(questionCards.length).toBe(2);

    const firstQuestion = questionCards[0];
    expect(firstQuestion.textContent).toContain('Is this product energy efficient?');
    expect(firstQuestion.textContent).toContain('John Doe');
  }));

  it('should display answers for questions', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const answerCards = fixture.nativeElement.querySelectorAll('.answer-card');
    expect(answerCards.length).toBe(1);

    const answer = answerCards[0];
    expect(answer.textContent).toContain('Yes, it has an A++ energy rating.');
    expect(answer.classList.contains('official')).toBeTruthy();
  }));

  it('should toggle ask question form', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    expect(component.showAskForm()).toBeFalsy();

    const toggleBtn = fixture.nativeElement.querySelector('.ask-question-toggle');
    toggleBtn.click();
    fixture.detectChanges();

    expect(component.showAskForm()).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.question-form')).toBeTruthy();

    toggleBtn.click();
    fixture.detectChanges();

    expect(component.showAskForm()).toBeFalsy();
  }));

  it('should validate question form', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    component.showAskForm.set(true);
    fixture.detectChanges();

    expect(component.questionForm.valid).toBeFalsy();

    component.questionForm.patchValue({ questionText: 'short' });
    expect(component.questionForm.valid).toBeFalsy();

    component.questionForm.patchValue({ questionText: 'This is a valid question that is long enough' });
    expect(component.questionForm.valid).toBeTruthy();
  }));

  it('should submit question', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    component.showAskForm.set(true);
    component.questionForm.patchValue({
      questionText: 'This is a test question about the product?',
      askerName: 'Test User',
      askerEmail: 'test@example.com'
    });
    fixture.detectChanges();

    component.submitQuestion();
    fixture.detectChanges();

    const submitReq = httpMock.expectOne(`${environment.apiUrl}/api/questions`);
    expect(submitReq.request.method).toBe('POST');
    expect(submitReq.request.body.questionText).toBe('This is a test question about the product?');
    submitReq.flush({ id: 'new-id', message: 'Question submitted' });
    tick();
    fixture.detectChanges();

    expect(component.questionSubmitted()).toBeTruthy();
  }));

  it('should point the ask-question login link to /login (not /auth/login)', fakeAsync(() => {
    (component as unknown as { authService: MockAuthService }).authService.isAuthenticated.set(false);
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const loginLink: HTMLAnchorElement | null =
      fixture.nativeElement.querySelector('[data-testid="qa-login-link"]');
    expect(loginLink).toBeTruthy();
    // routerLink resolves to the href attribute in tests.
    expect(loginLink!.getAttribute('href')).toBe('/login');
  }));

  it('should point the login-to-answer link to /login (not /auth/login)', fakeAsync(() => {
    (component as unknown as { authService: MockAuthService }).authService.isAuthenticated.set(false);
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const answerLink: HTMLAnchorElement | null =
      fixture.nativeElement.querySelector('[data-testid="login-to-answer"]');
    expect(answerLink).toBeTruthy();
    expect(answerLink!.getAttribute('href')).toBe('/login');
  }));

  it('should map raw question submission errors to a translation key', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();

    component.showAskForm.set(true);
    component.questionForm.patchValue({
      questionText: 'This is a test question about the product?'
    });

    component.submitQuestion();
    const submitReq = httpMock.expectOne(`${environment.apiUrl}/api/questions`);
    submitReq.flush({ message: 'Raw backend question error' }, { status: 500, statusText: 'Server Error' });
    tick();

    expect(component.questionError()).toBe('products.qa.errors.submitQuestionFailed');
    expect(component.questionError()).not.toBe('Raw backend question error');
  }));

  it('should map raw answer submission errors to a translation key', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();

    component.toggleAnswerForm('q1');
    component.answerForm.patchValue({
      answerText: 'This is a detailed test answer.'
    });

    component.submitAnswer('q1');
    const submitReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/answers`);
    submitReq.flush({ message: 'Raw backend answer error' }, { status: 500, statusText: 'Server Error' });
    tick();

    expect(component.answerError()).toBe('products.qa.errors.submitAnswerFailed');
    expect(component.answerError()).not.toBe('Raw backend answer error');
  }));

  it('should toggle answer form for a question', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    expect(component.showAnswerFormFor()).toBeNull();

    component.toggleAnswerForm('q1');
    expect(component.showAnswerFormFor()).toBe('q1');

    component.toggleAnswerForm('q1');
    expect(component.showAnswerFormFor()).toBeNull();
  }));

  const loadInitialQuestions = (fix = fixture): void => {
    fix.detectChanges();
    const req = httpMock.expectOne(r => r.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fix.detectChanges();
  };

  it('should vote on a question and reconcile with the authoritative server state', fakeAsync(() => {
    loadInitialQuestions();

    expect(component.questions()[0].hasVotedHelpful).toBeFalse();
    expect(component.questions()[0].helpfulCount).toBe(5);

    component.voteQuestion(component.questions()[0]);
    fixture.detectChanges();

    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    expect(voteReq.request.method).toBe('POST');
    // Server is authoritative — return a count that differs from a naive +1.
    voteReq.flush({ helpfulCount: 9, hasVotedHelpful: true });
    tick();
    fixture.detectChanges();

    expect(component.questions()[0].helpfulCount).toBe(9);
    expect(component.questions()[0].hasVotedHelpful).toBeTrue();
  }));

  it('should toggle a question vote off on a second click (no localStorage block)', fakeAsync(() => {
    loadInitialQuestions();

    // First click votes helpful.
    component.voteQuestion(component.questions()[0]);
    let voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    voteReq.flush({ helpfulCount: 6, hasVotedHelpful: true });
    tick();
    fixture.detectChanges();

    expect(component.questions()[0].hasVotedHelpful).toBeTrue();
    expect(component.questions()[0].helpfulCount).toBe(6);

    // Second click toggles the vote off — a NEW request is made (not blocked).
    component.voteQuestion(component.questions()[0]);
    voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    voteReq.flush({ helpfulCount: 5, hasVotedHelpful: false });
    tick();
    fixture.detectChanges();

    expect(component.questions()[0].hasVotedHelpful).toBeFalse();
    expect(component.questions()[0].helpfulCount).toBe(5);
  }));

  it('should ignore an overlapping question vote while one is in flight (single POST, aria-busy)', fakeAsync(() => {
    loadInitialQuestions();

    // First click — request is now in flight (not yet flushed).
    component.voteQuestion(component.questions()[0]);
    fixture.detectChanges();

    const questionBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="question-vote-btn"]');
    expect(questionBtn.disabled).toBeTrue();
    expect(questionBtn.getAttribute('aria-busy')).toBe('true');

    // Rapid second click while pending — must be ignored (no second POST).
    component.voteQuestion(component.questions()[0]);
    tick();

    // expectOne asserts EXACTLY ONE outstanding request was ever made for this id.
    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    voteReq.flush({ helpfulCount: 6, hasVotedHelpful: true });
    tick();
    fixture.detectChanges();

    // Reconciled to the single authoritative response (6, not a double-applied 7),
    // and the button is no longer busy/disabled.
    expect(component.questions()[0].helpfulCount).toBe(6);
    expect(component.questions()[0].hasVotedHelpful).toBeTrue();
    expect(questionBtn.disabled).toBeFalse();
    expect(questionBtn.getAttribute('aria-busy')).toBe('false');
  }));

  it('should not let a stale in-flight answer response clobber the newer authoritative state', fakeAsync(() => {
    loadInitialQuestions();

    const answer = component.questions()[0].answers[0];

    // First click (helpful) — in flight.
    component.voteAnswer(answer, true);
    fixture.detectChanges();

    // While pending, BOTH answer vote controls are disabled/busy — this guards the
    // helpful↔unhelpful flip race (a slower first response arriving after a flip).
    const helpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-helpful-btn"]');
    const unhelpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-unhelpful-btn"]');
    expect(helpfulBtn.disabled).toBeTrue();
    expect(unhelpfulBtn.disabled).toBeTrue();
    expect(helpfulBtn.getAttribute('aria-busy')).toBe('true');

    // Rapid flip attempt while the first is still pending — ignored (no second POST).
    component.voteAnswer(component.questions()[0].answers[0], false);
    tick();

    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/answers/a1/vote`);
    expect(voteReq.request.body.isHelpful).toBeTrue();
    voteReq.flush({ helpfulCount: 11, unhelpfulCount: 0, userVoteHelpful: true });
    tick();
    fixture.detectChanges();

    // Final state is the single authoritative "helpful" response, not a stale flip.
    const settled = component.questions()[0].answers[0];
    expect(settled.helpfulCount).toBe(11);
    expect(settled.unhelpfulCount).toBe(0);
    expect(settled.userVoteHelpful).toBeTrue();
    expect(helpfulBtn.disabled).toBeFalse();
    expect(helpfulBtn.getAttribute('aria-busy')).toBe('false');
  }));

  it('should reflect the user\'s own vote as an active/pressed button in the DOM', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(r => r.url.includes('/questions/product/'));
    // A payload where the current user has already voted the question + answer helpful.
    req.flush({
      ...mockQuestions,
      questions: [
        {
          ...mockQuestions.questions[0],
          hasVotedHelpful: true,
          answers: [{ ...mockQuestions.questions[0].answers[0], userVoteHelpful: true }]
        },
        mockQuestions.questions[1]
      ]
    });
    tick();
    fixture.detectChanges();

    const questionBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="question-vote-btn"]');
    expect(questionBtn.classList.contains('active')).toBeTrue();
    expect(questionBtn.getAttribute('aria-pressed')).toBe('true');

    const helpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-helpful-btn"]');
    expect(helpfulBtn.classList.contains('active')).toBeTrue();
    expect(helpfulBtn.getAttribute('aria-pressed')).toBe('true');

    const unhelpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-unhelpful-btn"]');
    expect(unhelpfulBtn.classList.contains('active')).toBeFalse();
    expect(unhelpfulBtn.getAttribute('aria-pressed')).toBe('false');
  }));

  it('should vote an answer helpful and reconcile counts + userVoteHelpful from the server', fakeAsync(() => {
    loadInitialQuestions();

    component.voteAnswer(component.questions()[0].answers[0], true);
    fixture.detectChanges();

    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/answers/a1/vote`);
    expect(voteReq.request.body.isHelpful).toBeTrue();
    voteReq.flush({ helpfulCount: 11, unhelpfulCount: 0, userVoteHelpful: true });
    tick();
    fixture.detectChanges();

    const answer = component.questions()[0].answers[0];
    expect(answer.helpfulCount).toBe(11);
    expect(answer.userVoteHelpful).toBeTrue();
  }));

  it('should flip an answer vote from helpful to unhelpful', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(r => r.url.includes('/questions/product/'));
    req.flush({
      ...mockQuestions,
      questions: [
        {
          ...mockQuestions.questions[0],
          answers: [{ ...mockQuestions.questions[0].answers[0], helpfulCount: 10, unhelpfulCount: 0, userVoteHelpful: true }]
        },
        mockQuestions.questions[1]
      ]
    });
    tick();
    fixture.detectChanges();

    // Click "unhelpful" while currently voted "helpful" → flip.
    component.voteAnswer(component.questions()[0].answers[0], false);
    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/answers/a1/vote`);
    expect(voteReq.request.body.isHelpful).toBeFalse();
    voteReq.flush({ helpfulCount: 9, unhelpfulCount: 1, userVoteHelpful: false });
    tick();
    fixture.detectChanges();

    const answer = component.questions()[0].answers[0];
    expect(answer.helpfulCount).toBe(9);
    expect(answer.unhelpfulCount).toBe(1);
    expect(answer.userVoteHelpful).toBeFalse();
  }));

  it('should not vote and should disable vote buttons when anonymous', fakeAsync(() => {
    (component as unknown as { authService: MockAuthService }).authService.isAuthenticated.set(false);
    loadInitialQuestions();

    const questionBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="question-vote-btn"]');
    const helpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-helpful-btn"]');
    const unhelpfulBtn: HTMLButtonElement =
      fixture.nativeElement.querySelector('[data-testid="answer-unhelpful-btn"]');

    expect(questionBtn.disabled).toBeTrue();
    expect(helpfulBtn.disabled).toBeTrue();
    expect(unhelpfulBtn.disabled).toBeTrue();

    // Calling the handlers directly is also guarded — no HTTP request is issued.
    component.voteQuestion(component.questions()[0]);
    component.voteAnswer(component.questions()[0].answers[0], true);
    tick();

    httpMock.expectNone(`${environment.apiUrl}/api/questions/q1/vote`);
    httpMock.expectNone(`${environment.apiUrl}/api/questions/answers/a1/vote`);
  }));

  it('should surface a session-expired error and roll back on a 401 vote', fakeAsync(() => {
    loadInitialQuestions();

    component.voteQuestion(component.questions()[0]);
    const voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    voteReq.flush({}, { status: 401, statusText: 'Unauthorized' });
    tick();
    fixture.detectChanges();

    expect(component.voteError()).toBe('products.qa.errors.sessionExpired');
    // Optimistic update rolled back to the pre-vote state.
    expect(component.questions()[0].hasVotedHelpful).toBeFalse();
    expect(component.questions()[0].helpfulCount).toBe(5);

    const banner: HTMLElement = fixture.nativeElement.querySelector('[data-testid="qa-vote-error"]');
    expect(banner).toBeTruthy();
  }));

  it('should not read or write the retired localStorage vote-tracking keys', fakeAsync(() => {
    const setItemSpy = spyOn(Storage.prototype, 'setItem').and.callThrough();
    loadInitialQuestions();

    component.voteQuestion(component.questions()[0]);
    let voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/q1/vote`);
    voteReq.flush({ helpfulCount: 6, hasVotedHelpful: true });
    tick();

    component.voteAnswer(component.questions()[0].answers[0], true);
    voteReq = httpMock.expectOne(`${environment.apiUrl}/api/questions/answers/a1/vote`);
    voteReq.flush({ helpfulCount: 11, unhelpfulCount: 0, userVoteHelpful: true });
    tick();

    expect(setItemSpy).not.toHaveBeenCalledWith('climasite_voted_questions', jasmine.anything());
    expect(setItemSpy).not.toHaveBeenCalledWith('climasite_voted_answers', jasmine.anything());
    expect(localStorage.getItem('climasite_voted_questions')).toBeNull();
    expect(localStorage.getItem('climasite_voted_answers')).toBeNull();
  }));

  it('should display empty state when no questions', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush({
      productId: '123e4567-e89b-12d3-a456-426614174000',
      totalQuestions: 0,
      answeredQuestions: 0,
      questions: []
    });
    tick();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.question-card')).toBeFalsy();
  }));

  it('should calculate total pages correctly', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush({
      ...mockQuestions,
      totalQuestions: 25
    });
    tick();
    fixture.detectChanges();

    expect(component.totalPages()).toBe(3); // 25 / 10 = 2.5, rounded up = 3
  }));

  it('should navigate pages', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush({
      ...mockQuestions,
      totalQuestions: 25
    });
    tick();
    fixture.detectChanges();

    expect(component.currentPage()).toBe(1);
    expect(component.totalPages()).toBe(3);

    // Test next page navigation
    component.nextPage();
    expect(component.currentPage()).toBe(2);

    // Flush the HTTP request triggered by nextPage
    const nextReq = httpMock.expectOne(req =>
      req.url.includes('/questions/product/') && req.params.get('pageNumber') === '2'
    );
    nextReq.flush(mockQuestions);
    tick();

    // Test previous page navigation
    component.previousPage();
    expect(component.currentPage()).toBe(1);

    // Flush the HTTP request triggered by previousPage
    const prevReq = httpMock.expectOne(req =>
      req.url.includes('/questions/product/') && req.params.get('pageNumber') === '1'
    );
    prevReq.flush(mockQuestions);
    tick();
  }));

  it('should show loading state', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    expect(component.loading()).toBeTruthy();

    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBeFalsy();
  }));

  it('should track questions by id', () => {
    const question: Question = {
      id: 'test-id',
      productId: '123',
      questionText: 'Test',
      askerName: 'Test',
      helpfulCount: 0,
      createdAt: '2024-01-01',
      answeredAt: null,
      answerCount: 0,
      hasVotedHelpful: false,
      answers: []
    };

    expect(component.trackQuestion(0, question)).toBe('test-id');
  });
});
