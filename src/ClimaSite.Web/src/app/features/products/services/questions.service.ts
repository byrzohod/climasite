import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface Answer {
  id: string;
  questionId: string;
  answerText: string;
  answererName: string | null;
  isOfficial: boolean;
  helpfulCount: number;
  unhelpfulCount: number;
  createdAt: string;
}

export interface Question {
  id: string;
  productId: string;
  questionText: string;
  askerName: string | null;
  helpfulCount: number;
  createdAt: string;
  answeredAt: string | null;
  answerCount: number;
  answers: Answer[];
}

export interface ProductQuestions {
  productId: string;
  totalQuestions: number;
  answeredQuestions: number;
  questions: Question[];
}

export interface AskQuestionRequest {
  productId: string;
  questionText: string;
  askerName?: string;
  askerEmail?: string;
}

export interface AnswerQuestionRequest {
  answerText: string;
  answererName?: string;
}

export interface VoteResult {
  helpfulCount: number;
  unhelpfulCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class QuestionsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/questions`;

  getProductQuestions(
    productId: string,
    pageNumber = 1,
    pageSize = 10,
    includeUnanswered = true
  ): Observable<ProductQuestions> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('includeUnanswered', includeUnanswered.toString());

    return this.http.get<ProductQuestions>(
      `${this.apiUrl}/product/${productId}`,
      { params }
    );
  }

  askQuestion(request: AskQuestionRequest): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(this.apiUrl, request);
  }

  answerQuestion(
    questionId: string,
    request: AnswerQuestionRequest
  ): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(
      `${this.apiUrl}/${questionId}/answers`,
      request
    );
  }

  voteQuestion(questionId: string): Observable<VoteResult> {
    return this.http.post<VoteResult>(`${this.apiUrl}/${questionId}/vote`, {});
  }

  voteAnswer(answerId: string, isHelpful: boolean): Observable<VoteResult> {
    return this.http.post<VoteResult>(
      `${this.apiUrl}/answers/${answerId}/vote`,
      { isHelpful }
    );
  }
}
