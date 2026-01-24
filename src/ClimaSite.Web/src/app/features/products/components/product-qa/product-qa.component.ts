import { Component, input, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { QuestionsService, Question, Answer, ProductQuestions } from '../../services/questions.service';
import { AuthService } from '../../../../auth/services/auth.service';

@Component({
  selector: 'app-product-qa',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, TranslateModule],
  template: `
    <section class="product-qa">
      <div class="qa-header">
        <h2>{{ 'products.qa.title' | translate }}</h2>
        <div class="qa-stats" *ngIf="questionsData()">
          <span class="total">
            {{ 'products.qa.totalQuestions' | translate: { count: questionsData()!.totalQuestions } }}
          </span>
          <span class="answered">
            {{ 'products.qa.answeredQuestions' | translate: { count: questionsData()!.answeredQuestions } }}
          </span>
        </div>
      </div>

      <!-- Ask Question Form -->
      <div class="ask-question-section">
        @if (isAuthenticated()) {
          <button
            class="ask-question-toggle"
            (click)="showAskForm.set(!showAskForm())"
            [class.active]="showAskForm()"
            data-testid="ask-question-btn">
            <span class="icon">?</span>
            {{ 'products.qa.askQuestion' | translate }}
          </button>

          <form
            *ngIf="showAskForm()"
            [formGroup]="questionForm"
            (ngSubmit)="submitQuestion()"
            class="question-form"
            data-testid="question-form">
            <div class="form-group">
              <label for="questionText">{{ 'products.qa.yourQuestion' | translate }}</label>
              <textarea
                id="questionText"
                formControlName="questionText"
                [placeholder]="'products.qa.questionPlaceholder' | translate"
                rows="3"
                maxlength="2000"
                data-testid="question-text">
              </textarea>
              <span class="char-count">{{ questionForm.get('questionText')?.value?.length || 0 }}/2000</span>
              <span class="error" *ngIf="questionForm.get('questionText')?.errors?.['minlength']">
                {{ 'products.qa.errors.questionTooShort' | translate }}
              </span>
            </div>

            <button
              type="submit"
              class="submit-question-btn"
              [disabled]="questionForm.invalid || submittingQuestion()"
              data-testid="submit-question-btn">
              <span *ngIf="submittingQuestion()" class="spinner"></span>
              {{ (submittingQuestion() ? 'products.qa.submitting' : 'products.qa.submitQuestion') | translate }}
            </button>

            <div class="success-message" *ngIf="questionSubmitted()">
              {{ 'products.qa.questionSubmittedSuccess' | translate }}
            </div>

            <div class="error-message" *ngIf="questionError()">
              {{ questionError() }}
            </div>
          </form>
        } @else {
          <div class="login-prompt" data-testid="qa-login-prompt">
            <span class="icon">?</span>
            <span>{{ 'products.qa.loginToAsk' | translate }}</span>
            <a [routerLink]="['/auth/login']" class="login-link" data-testid="qa-login-link">
              {{ 'products.qa.loginNow' | translate }}
            </a>
          </div>
        }
      </div>

      <!-- Questions List -->
      <div class="questions-list" *ngIf="questions().length > 0">
        <div class="question-card" *ngFor="let question of questions(); trackBy: trackQuestion">
          <div class="question-content">
            <div class="question-header">
              <span class="question-icon">Q</span>
              <span class="asker-name">{{ question.askerName || ('products.qa.anonymous' | translate) }}</span>
              <span class="question-date">{{ question.createdAt | date:'mediumDate' }}</span>
            </div>
            <p class="question-text">{{ question.questionText }}</p>
            <div class="question-actions">
              <button
                class="helpful-btn"
                (click)="voteQuestion(question)"
                [disabled]="votedQuestions().has(question.id)">
                <span class="thumb-icon">&#128077;</span>
                {{ 'products.qa.helpful' | translate }} ({{ question.helpfulCount }})
              </button>
              @if (isAuthenticated()) {
                <button
                  class="answer-btn"
                  (click)="toggleAnswerForm(question.id)"
                  data-testid="answer-btn">
                  {{ 'products.qa.answer' | translate }}
                </button>
              } @else {
                <a [routerLink]="['/auth/login']" class="answer-btn login-to-answer" data-testid="login-to-answer">
                  {{ 'products.qa.loginToAnswer' | translate }}
                </a>
              }
            </div>
          </div>

          <!-- Answer Form -->
          <form
            *ngIf="showAnswerFormFor() === question.id"
            [formGroup]="answerForm"
            (ngSubmit)="submitAnswer(question.id)"
            class="answer-form"
            data-testid="answer-form">
            <div class="form-group">
              <textarea
                formControlName="answerText"
                [placeholder]="'products.qa.answerPlaceholder' | translate"
                rows="2"
                maxlength="5000"
                data-testid="answer-text">
              </textarea>
            </div>
            <div class="form-actions">
              <button
                type="submit"
                class="submit-answer-btn"
                [disabled]="answerForm.invalid || submittingAnswer()"
                data-testid="submit-answer-btn">
                <span *ngIf="submittingAnswer()" class="spinner"></span>
                {{ 'products.qa.submitAnswer' | translate }}
              </button>
              <button
                type="button"
                class="cancel-answer-btn"
                (click)="showAnswerFormFor.set(null)">
                {{ 'common.cancel' | translate }}
              </button>
            </div>
            <div class="error-message" *ngIf="answerError()">
              {{ answerError() }}
            </div>
          </form>

          <!-- Answers -->
          <div class="answers-list" *ngIf="question.answers.length > 0">
            <div
              class="answer-card"
              *ngFor="let answer of question.answers; trackBy: trackAnswer"
              [class.official]="answer.isOfficial">
              <div class="answer-header">
                <span class="answer-icon" [class.official]="answer.isOfficial">A</span>
                <span class="answerer-name">
                  {{ answer.answererName || ('products.qa.communityMember' | translate) }}
                </span>
                <span class="official-badge" *ngIf="answer.isOfficial">
                  {{ 'products.qa.officialAnswer' | translate }}
                </span>
                <span class="answer-date">{{ answer.createdAt | date:'mediumDate' }}</span>
              </div>
              <p class="answer-text">{{ answer.answerText }}</p>
              <div class="answer-actions">
                <button
                  class="vote-btn helpful"
                  (click)="voteAnswer(answer, true)"
                  [disabled]="votedAnswers().has(answer.id)">
                  <span class="thumb-icon">&#128077;</span> {{ answer.helpfulCount }}
                </button>
                <button
                  class="vote-btn unhelpful"
                  (click)="voteAnswer(answer, false)"
                  [disabled]="votedAnswers().has(answer.id)">
                  <span class="thumb-icon">&#128078;</span> {{ answer.unhelpfulCount }}
                </button>
              </div>
            </div>
          </div>

          <!-- No answers yet -->
          <div class="no-answers" *ngIf="question.answers.length === 0">
            <span class="icon">&#128172;</span>
            {{ 'products.qa.noAnswersYet' | translate }}
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div class="empty-state" *ngIf="!loading() && questions().length === 0">
        <span class="icon">&#128172;</span>
        <p>{{ 'products.qa.noQuestionsYet' | translate }}</p>
        <p class="hint">{{ 'products.qa.beFirstToAsk' | translate }}</p>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading()">
        <div class="spinner"></div>
        <span>{{ 'common.loading' | translate }}</span>
      </div>

      <!-- Pagination -->
      <div class="pagination" *ngIf="totalPages() > 1">
        <button
          class="page-btn prev"
          (click)="previousPage()"
          [disabled]="currentPage() === 1">
          &laquo; {{ 'common.previous' | translate }}
        </button>
        <span class="page-info">
          {{ 'common.page' | translate }} {{ currentPage() }} / {{ totalPages() }}
        </span>
        <button
          class="page-btn next"
          (click)="nextPage()"
          [disabled]="currentPage() === totalPages()">
          {{ 'common.next' | translate }} &raquo;
        </button>
      </div>
    </section>
  `,
  styles: [`
    .product-qa {
      padding: 1.5rem;
      background: var(--color-bg-card);
      border-radius: 12px;
      border: 1px solid var(--color-border-primary);
    }

    .qa-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 0.5rem;

      h2 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0;
      }

      .qa-stats {
        display: flex;
        gap: 1rem;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }
    }

    .ask-question-section {
      margin-bottom: 2rem;
    }

    .ask-question-toggle {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.25rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s;

      &:hover {
        background: var(--color-primary-dark);
      }

      &.active {
        background: var(--color-primary-dark);
      }

      .icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 24px;
        height: 24px;
        background: white;
        color: var(--color-primary);
        border-radius: 50%;
        font-weight: bold;
      }
    }

    .question-form,
    .answer-form {
      margin-top: 1rem;
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      border: 1px solid var(--color-border-primary);
    }

    .form-group {
      margin-bottom: 1rem;
      position: relative;

      label {
        display: block;
        margin-bottom: 0.375rem;
        font-weight: 500;
        font-size: 0.875rem;
        color: var(--color-text-primary);
      }

      input,
      textarea {
        width: 100%;
        padding: 0.625rem 0.875rem;
        border: 1px solid var(--color-border-primary);
        border-radius: 6px;
        font-size: 0.875rem;
        background: var(--color-bg-input);
        color: var(--color-text-primary);

        &:focus {
          outline: none;
          border-color: var(--color-primary);
          box-shadow: 0 0 0 3px var(--color-primary-light);
        }

        &::placeholder {
          color: var(--color-text-placeholder);
        }
      }

      textarea {
        resize: vertical;
        min-height: 80px;
      }

      .char-count {
        position: absolute;
        right: 0.5rem;
        bottom: 0.25rem;
        font-size: 0.75rem;
        color: var(--color-text-tertiary);
      }

      .hint {
        font-size: 0.75rem;
        color: var(--color-text-tertiary);
        margin-top: 0.25rem;
      }

      .error {
        font-size: 0.75rem;
        color: var(--color-error);
        margin-top: 0.25rem;
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;

      @media (max-width: 600px) {
        grid-template-columns: 1fr;
      }
    }

    .submit-question-btn,
    .submit-answer-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1.25rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .success-message {
      margin-top: 1rem;
      padding: 1rem;
      background: var(--color-success-light);
      color: var(--color-success-dark);
      border-radius: 6px;
      text-align: center;
      font-size: 0.875rem;
    }

    .error-message {
      margin-top: 1rem;
      padding: 1rem;
      background: var(--color-error-light);
      color: var(--color-error-dark);
      border-radius: 6px;
      text-align: center;
      font-size: 0.875rem;
    }

    .login-prompt {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem 1.25rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
      color: var(--color-text-secondary);

      .icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        background: var(--color-primary-light);
        color: var(--color-primary);
        border-radius: 50%;
        font-weight: bold;
        flex-shrink: 0;
      }

      .login-link {
        margin-left: auto;
        padding: 0.5rem 1rem;
        background: var(--color-primary);
        color: var(--color-text-inverse);
        border-radius: 6px;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.875rem;
        transition: background 0.2s;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    .login-to-answer {
      text-decoration: none;
      color: var(--color-primary) !important;
      font-size: 0.875rem;
      font-weight: 500;

      &:hover {
        text-decoration: underline;
      }
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .cancel-answer-btn {
      padding: 0.5rem 1rem;
      background: transparent;
      color: var(--color-text-secondary);
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-border-secondary);
        color: var(--color-text-primary);
      }
    }

    .questions-list {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .question-card {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      border: 1px solid var(--color-border-primary);
    }

    .question-header,
    .answer-header {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
      flex-wrap: wrap;
    }

    .question-icon,
    .answer-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border-radius: 50%;
      font-weight: bold;
      font-size: 0.875rem;

      &.official {
        background: var(--color-success);
      }
    }

    .asker-name,
    .answerer-name {
      font-weight: 500;
      color: var(--color-text-primary);
    }

    .question-date,
    .answer-date {
      font-size: 0.875rem;
      color: var(--color-text-tertiary);
      margin-left: auto;
    }

    .official-badge {
      padding: 0.25rem 0.5rem;
      background: var(--color-success-light);
      color: var(--color-success-dark);
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .question-text,
    .answer-text {
      margin: 0 0 1rem;
      color: var(--color-text-primary);
      line-height: 1.6;
    }

    .question-actions,
    .answer-actions {
      display: flex;
      gap: 1rem;
    }

    .helpful-btn,
    .answer-btn,
    .vote-btn {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.5rem;
      background: transparent;
      border: 1px solid var(--color-border-primary);
      border-radius: 4px;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-bg-hover);
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .thumb-icon {
        font-size: 1rem;
      }
    }

    .answers-list {
      margin-top: 1.5rem;
      padding-left: 2rem;
      border-left: 2px solid var(--color-border-primary);
    }

    .answer-card {
      padding: 1rem;
      margin-bottom: 1rem;
      background: var(--color-bg-card);
      border-radius: 6px;

      &.official {
        background: var(--color-success-light);
        border-left: 3px solid var(--color-success);
      }

      &:last-child {
        margin-bottom: 0;
      }
    }

    .no-answers {
      margin-top: 1rem;
      padding: 1rem;
      text-align: center;
      color: var(--color-text-tertiary);
      font-style: italic;

      .icon {
        display: block;
        font-size: 24px;
        margin-bottom: 0.25rem;
      }
    }

    .empty-state {
      text-align: center;
      padding: 2rem;
      color: var(--color-text-secondary);

      .icon {
        font-size: 48px;
        display: block;
        margin-bottom: 1rem;
      }

      p {
        margin: 0 0 0.25rem;
      }

      .hint {
        font-size: 0.875rem;
        color: var(--color-text-tertiary);
      }
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 2rem;
      color: var(--color-text-secondary);
    }

    .spinner {
      width: 24px;
      height: 24px;
      border: 2px solid var(--color-border-primary);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin-bottom: 0.5rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      margin-top: 2rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border-primary);
    }

    .page-btn {
      padding: 0.5rem 1rem;
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      color: var(--color-text-primary);
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .page-info {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }
  `]
})
export class ProductQaComponent {
  productId = input.required<string>();

  private readonly questionsService = inject(QuestionsService);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  // Auth state
  isAuthenticated = computed(() => this.authService.isAuthenticated());
  currentUser = computed(() => this.authService.user());

  questionsData = signal<ProductQuestions | null>(null);
  questions = computed(() => this.questionsData()?.questions || []);
  loading = signal(false);
  currentPage = signal(1);
  pageSize = 10;
  totalPages = computed(() => {
    const data = this.questionsData();
    return data ? Math.ceil(data.totalQuestions / this.pageSize) : 0;
  });

  showAskForm = signal(false);
  showAnswerFormFor = signal<string | null>(null);
  submittingQuestion = signal(false);
  submittingAnswer = signal(false);
  questionSubmitted = signal(false);
  questionError = signal<string | null>(null);
  answerError = signal<string | null>(null);

  votedQuestions = signal<Set<string>>(new Set());
  votedAnswers = signal<Set<string>>(new Set());

  questionForm: FormGroup;
  answerForm: FormGroup;

  constructor() {
    // Form for authenticated users - no name/email needed
    this.questionForm = this.fb.group({
      questionText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]]
    });

    this.answerForm = this.fb.group({
      answerText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(5000)]]
    });

    // Load voted items from localStorage
    this.loadVotedItems();

    // Reload questions when productId changes
    effect(() => {
      const id = this.productId();
      if (id) {
        this.loadQuestions();
      }
    });
  }

  loadQuestions(): void {
    this.loading.set(true);
    this.questionsService.getProductQuestions(
      this.productId(),
      this.currentPage(),
      this.pageSize
    ).subscribe({
      next: (data) => {
        this.questionsData.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  submitQuestion(): void {
    if (this.questionForm.invalid || !this.isAuthenticated()) return;

    this.submittingQuestion.set(true);
    this.questionSubmitted.set(false);
    this.questionError.set(null);

    const user = this.currentUser();
    const request = {
      productId: this.productId(),
      questionText: this.questionForm.value.questionText,
      askerName: user ? `${user.firstName} ${user.lastName}`.trim() : undefined,
      askerEmail: user?.email
    };

    this.questionsService.askQuestion(request).subscribe({
      next: () => {
        this.submittingQuestion.set(false);
        this.questionSubmitted.set(true);
        this.questionForm.reset();
        this.showAskForm.set(false);
        // Questions need approval, so don't reload immediately
        setTimeout(() => this.questionSubmitted.set(false), 5000);
      },
      error: (err) => {
        this.submittingQuestion.set(false);
        if (err.status === 401) {
          this.questionError.set('Your session has expired. Please log in again.');
        } else {
          this.questionError.set(err.error?.message || 'Failed to submit question. Please try again.');
        }
      }
    });
  }

  toggleAnswerForm(questionId: string): void {
    if (this.showAnswerFormFor() === questionId) {
      this.showAnswerFormFor.set(null);
    } else {
      this.showAnswerFormFor.set(questionId);
      this.answerForm.reset();
    }
  }

  submitAnswer(questionId: string): void {
    if (this.answerForm.invalid || !this.isAuthenticated()) return;

    this.submittingAnswer.set(true);
    this.answerError.set(null);

    const user = this.currentUser();
    const request = {
      answerText: this.answerForm.value.answerText,
      answererName: user ? `${user.firstName} ${user.lastName}`.trim() : undefined
    };

    this.questionsService.answerQuestion(questionId, request).subscribe({
      next: () => {
        this.submittingAnswer.set(false);
        this.showAnswerFormFor.set(null);
        this.answerForm.reset();
        // Answers need approval, so don't reload immediately
      },
      error: (err) => {
        this.submittingAnswer.set(false);
        if (err.status === 401) {
          this.answerError.set('Your session has expired. Please log in again.');
        } else {
          this.answerError.set(err.error?.message || 'Failed to submit answer. Please try again.');
        }
      }
    });
  }

  voteQuestion(question: Question): void {
    if (this.votedQuestions().has(question.id)) return;

    this.questionsService.voteQuestion(question.id).subscribe({
      next: (result) => {
        question.helpfulCount = result.helpfulCount;
        this.addVotedQuestion(question.id);
      }
    });
  }

  voteAnswer(answer: Answer, isHelpful: boolean): void {
    if (this.votedAnswers().has(answer.id)) return;

    this.questionsService.voteAnswer(answer.id, isHelpful).subscribe({
      next: (result) => {
        answer.helpfulCount = result.helpfulCount;
        answer.unhelpfulCount = result.unhelpfulCount || 0;
        this.addVotedAnswer(answer.id);
      }
    });
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadQuestions();
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadQuestions();
    }
  }

  trackQuestion(index: number, question: Question): string {
    return question.id;
  }

  trackAnswer(index: number, answer: Answer): string {
    return answer.id;
  }

  private loadVotedItems(): void {
    try {
      const votedQs = localStorage.getItem('climasite_voted_questions');
      const votedAs = localStorage.getItem('climasite_voted_answers');
      if (votedQs) {
        this.votedQuestions.set(new Set(JSON.parse(votedQs)));
      }
      if (votedAs) {
        this.votedAnswers.set(new Set(JSON.parse(votedAs)));
      }
    } catch {
      // Ignore localStorage errors
    }
  }

  private addVotedQuestion(questionId: string): void {
    const updated = new Set(this.votedQuestions());
    updated.add(questionId);
    this.votedQuestions.set(updated);
    try {
      localStorage.setItem('climasite_voted_questions', JSON.stringify([...updated]));
    } catch {
      // Ignore localStorage errors
    }
  }

  private addVotedAnswer(answerId: string): void {
    const updated = new Set(this.votedAnswers());
    updated.add(answerId);
    this.votedAnswers.set(updated);
    try {
      localStorage.setItem('climasite_voted_answers', JSON.stringify([...updated]));
    } catch {
      // Ignore localStorage errors
    }
  }
}
