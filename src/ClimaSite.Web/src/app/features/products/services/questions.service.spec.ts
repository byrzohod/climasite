import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { QuestionsService, ProductQuestions } from './questions.service';
import { environment } from '../../../../environments/environment';

describe('QuestionsService', () => {
  let service: QuestionsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        QuestionsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(QuestionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProductQuestions', () => {
    it('should fetch product questions with default params', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse: ProductQuestions = {
        productId,
        totalQuestions: 5,
        answeredQuestions: 3,
        questions: []
      };

      service.getProductQuestions(productId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(
        `${environment.apiUrl}/questions/product/${productId}?pageNumber=1&pageSize=10&includeUnanswered=true`
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should fetch product questions with custom params', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse: ProductQuestions = {
        productId,
        totalQuestions: 5,
        answeredQuestions: 3,
        questions: []
      };

      service.getProductQuestions(productId, 2, 20, false).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(
        `${environment.apiUrl}/questions/product/${productId}?pageNumber=2&pageSize=20&includeUnanswered=false`
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('askQuestion', () => {
    it('should submit a new question', () => {
      const request = {
        productId: '123e4567-e89b-12d3-a456-426614174000',
        questionText: 'Is this product compatible with my home?',
        askerName: 'John',
        askerEmail: 'john@example.com'
      };
      const mockResponse = { id: 'new-question-id', message: 'Question submitted' };

      service.askQuestion(request).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/questions`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });
  });

  describe('answerQuestion', () => {
    it('should submit an answer to a question', () => {
      const questionId = '123e4567-e89b-12d3-a456-426614174000';
      const request = {
        answerText: 'Yes, it is compatible with most homes.',
        answererName: 'Support Team'
      };
      const mockResponse = { id: 'new-answer-id', message: 'Answer submitted' };

      service.answerQuestion(questionId, request).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/questions/${questionId}/answers`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });
  });

  describe('voteQuestion', () => {
    it('should vote a question as helpful', () => {
      const questionId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse = { helpfulCount: 5 };

      service.voteQuestion(questionId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/questions/${questionId}/vote`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('voteAnswer', () => {
    it('should vote an answer as helpful', () => {
      const answerId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse = { helpfulCount: 10, unhelpfulCount: 2 };

      service.voteAnswer(answerId, true).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/questions/answers/${answerId}/vote`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ isHelpful: true });
      req.flush(mockResponse);
    });

    it('should vote an answer as unhelpful', () => {
      const answerId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse = { helpfulCount: 10, unhelpfulCount: 3 };

      service.voteAnswer(answerId, false).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/questions/answers/${answerId}/vote`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ isHelpful: false });
      req.flush(mockResponse);
    });
  });
});
