import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component, signal } from '@angular/core';
import { ProductQaComponent } from './product-qa.component';
import { QuestionsService, ProductQuestions, Question } from '../../services/questions.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { environment } from '../../../../../environments/environment';

class MockAuthService {
  isAuthenticated = signal(true);
  user = signal({ id: 'test-user-id', firstName: 'Test', lastName: 'User', email: 'test@test.com' });
}

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<Record<string, string>> {
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
      'products.qa.helpful': 'Helpful',
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
        answers: [
          {
            id: 'a1',
            questionId: 'q1',
            answerText: 'Yes, it has an A++ energy rating.',
            answererName: 'ClimaSite Support',
            isOfficial: true,
            helpfulCount: 10,
            unhelpfulCount: 0,
            createdAt: '2024-01-16T14:00:00Z'
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
        answers: []
      }
    ]
  };

  beforeEach(async () => {
    // Clear localStorage before each test
    localStorage.removeItem('climasite_voted_questions');
    localStorage.removeItem('climasite_voted_answers');

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

    const submitReq = httpMock.expectOne(`${environment.apiUrl}/questions`);
    expect(submitReq.request.method).toBe('POST');
    expect(submitReq.request.body.questionText).toBe('This is a test question about the product?');
    submitReq.flush({ id: 'new-id', message: 'Question submitted' });
    tick();
    fixture.detectChanges();

    expect(component.questionSubmitted()).toBeTruthy();
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

  it('should vote on a question', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const question = component.questions()[0];
    const initialCount = question.helpfulCount;

    component.voteQuestion(question);
    fixture.detectChanges();

    const voteReq = httpMock.expectOne(`${environment.apiUrl}/questions/q1/vote`);
    voteReq.flush({ helpfulCount: initialCount + 1 });
    tick();
    fixture.detectChanges();

    expect(question.helpfulCount).toBe(initialCount + 1);
    expect(component.votedQuestions().has('q1')).toBeTruthy();
  }));

  it('should not allow double voting on question', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    // Add q1 to voted set
    component.voteQuestion(component.questions()[0]);
    const voteReq = httpMock.expectOne(`${environment.apiUrl}/questions/q1/vote`);
    voteReq.flush({ helpfulCount: 6 });
    tick();

    // Try to vote again - should be blocked
    const initialHelpfulCount = component.questions()[0].helpfulCount;
    component.voteQuestion(component.questions()[0]);
    tick();

    // Should not make another HTTP request
    httpMock.expectNone(`${environment.apiUrl}/questions/q1/vote`);

    // Helpful count should remain the same
    expect(component.questions()[0].helpfulCount).toBe(initialHelpfulCount);
  }));

  it('should vote on an answer', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/questions/product/'));
    req.flush(mockQuestions);
    tick();
    fixture.detectChanges();

    const answer = component.questions()[0].answers[0];

    component.voteAnswer(answer, true);
    fixture.detectChanges();

    const voteReq = httpMock.expectOne(`${environment.apiUrl}/questions/answers/a1/vote`);
    expect(voteReq.request.body.isHelpful).toBeTruthy();
    voteReq.flush({ helpfulCount: 11, unhelpfulCount: 0 });
    tick();
    fixture.detectChanges();

    expect(answer.helpfulCount).toBe(11);
    expect(component.votedAnswers().has('a1')).toBeTruthy();
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
      answers: []
    };

    expect(component.trackQuestion(0, question)).toBe('test-id');
  });
});
