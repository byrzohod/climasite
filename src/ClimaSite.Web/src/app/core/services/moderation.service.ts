import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminQuestion {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  questionText: string;
  askerName: string | null;
  askerEmail: string | null;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Flagged';
  helpfulCount: number;
  createdAt: string;
  answeredAt: string | null;
  totalAnswers: number;
  pendingAnswers: number;
}

export interface AdminAnswer {
  id: string;
  questionId: string;
  questionText: string;
  productName: string;
  productSlug: string;
  answerText: string;
  answererName: string | null;
  isOfficial: boolean;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Flagged';
  helpfulCount: number;
  unhelpfulCount: number;
  createdAt: string;
}

export interface PendingModeration {
  pendingQuestions: number;
  pendingAnswers: number;
  questions: AdminQuestion[];
  answers: AdminAnswer[];
}

export interface AdminReview {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  rating: number;
  title: string;
  content: string;
  reviewerName: string | null;
  reviewerEmail: string | null;
  isVerifiedPurchase: boolean;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Flagged';
  helpfulCount: number;
  createdAt: string;
}

export interface PendingReviewModeration {
  pendingReviews: number;
  reviews: AdminReview[];
}

@Injectable({
  providedIn: 'root'
})
export class ModerationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/admin`;

  // Q&A Moderation
  getPendingQA(pageNumber = 1, pageSize = 20): Observable<PendingModeration> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PendingModeration>(`${this.baseUrl}/questions/pending`, { params });
  }

  approveQuestion(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/${id}/approve`, {});
  }

  rejectQuestion(id: string, note?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/${id}/reject`, { note });
  }

  flagQuestion(id: string, note?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/${id}/flag`, { note });
  }

  approveAnswer(id: string, markAsOfficial?: boolean): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/answers/${id}/approve`, { markAsOfficial });
  }

  rejectAnswer(id: string, note?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/answers/${id}/reject`, { note });
  }

  flagAnswer(id: string, note?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/questions/answers/${id}/flag`, { note });
  }

  bulkApproveQuestions(ids: string[]): Observable<{ approved: number; failed: number }> {
    return this.http.post<{ approved: number; failed: number }>(`${this.baseUrl}/questions/bulk-approve`, { ids });
  }

  bulkApproveAnswers(ids: string[]): Observable<{ approved: number; failed: number }> {
    return this.http.post<{ approved: number; failed: number }>(`${this.baseUrl}/questions/answers/bulk-approve`, { ids });
  }

  // Reviews Moderation (to be implemented)
  getPendingReviews(pageNumber = 1, pageSize = 20): Observable<PendingReviewModeration> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PendingReviewModeration>(`${this.baseUrl}/reviews/pending`, { params });
  }

  approveReview(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/reviews/${id}/approve`, {});
  }

  rejectReview(id: string, note?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/reviews/${id}/reject`, { note });
  }
}
