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
  /**
   * The current user's own vote on this answer, from authoritative server state:
   * `true` = voted helpful, `false` = voted unhelpful, `null`/absent = no vote (or anonymous).
   */
  userVoteHelpful?: boolean | null;
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
  /** Whether the current user has cast a "helpful" vote on this question (`false` when anonymous). */
  hasVotedHelpful: boolean;
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

/** Authoritative result of a question vote toggle. */
export interface QuestionVoteResult {
  helpfulCount: number;
  hasVotedHelpful: boolean;
}

/**
 * Authoritative result of an answer vote toggle/flip.
 * `userVoteHelpful` is omitted by the API when the user has no vote after the operation.
 */
export interface AnswerVoteResult {
  helpfulCount: number;
  unhelpfulCount: number;
  userVoteHelpful?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class QuestionsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/questions`;

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

  /** Toggle the current user's "helpful" vote on a question (requires auth; 401 otherwise). */
  voteQuestion(questionId: string): Observable<QuestionVoteResult> {
    return this.http.post<QuestionVoteResult>(`${this.apiUrl}/${questionId}/vote`, {});
  }

  /** Toggle/flip the current user's vote on an answer (requires auth; 401 otherwise). */
  voteAnswer(answerId: string, isHelpful: boolean): Observable<AnswerVoteResult> {
    return this.http.post<AnswerVoteResult>(
      `${this.apiUrl}/answers/${answerId}/vote`,
      { isHelpful }
    );
  }
}
