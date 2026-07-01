import { Component, input, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { QuestionsService, Question, Answer, ProductQuestions } from '../../services/questions.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { apiErrorToTranslationKey } from '../../../../core/utils/translation-key.util';

@Component({
  selector: 'app-product-qa',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, TranslateModule],
  template: `
    <section class="product-qa" data-testid="product-qa">
      <div class="qa-header">
        <h2>{{ 'products.qa.title' | translate }}</h2>
        @if (questionsData()) {
          <div class="qa-stats">
            <span class="total">
              {{ 'products.qa.totalQuestions' | translate: { count: questionsData()!.totalQuestions } }}
            </span>
            <span class="answered">
              {{ 'products.qa.answeredQuestions' | translate: { count: questionsData()!.answeredQuestions } }}
            </span>
          </div>
        }
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

          @if (showAskForm()) {
            <form
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
                @if (questionForm.get('questionText')?.errors?.['minlength']) {
                  <span class="error">
                    {{ 'products.qa.errors.questionTooShort' | translate }}
                  </span>
                }
              </div>

              <button
                type="submit"
                class="submit-question-btn"
                [disabled]="questionForm.invalid || submittingQuestion()"
                data-testid="submit-question-btn">
                @if (submittingQuestion()) {
                  <span class="spinner"></span>
                }
                {{ (submittingQuestion() ? 'products.qa.submitting' : 'products.qa.submitQuestion') | translate }}
              </button>

              @if (questionSubmitted()) {
                <div class="success-message">
                  {{ 'products.qa.questionSubmittedSuccess' | translate }}
                </div>
              }

              @if (questionError()) {
                <div class="error-message">
                  {{ questionError() | translate }}
                </div>
              }
            </form>
          }
        } @else {
          <div class="login-prompt" data-testid="qa-login-prompt">
            <span class="icon">?</span>
            <span>{{ 'products.qa.loginToAsk' | translate }}</span>
            <a [routerLink]="['/login']" class="login-link" data-testid="qa-login-link">
              {{ 'products.qa.loginNow' | translate }}
            </a>
          </div>
        }
      </div>

      <!-- Vote error (e.g. session expired mid-session) -->
      @if (voteError()) {
        <div class="vote-error" role="alert" data-testid="qa-vote-error">
          <span>{{ voteError()! | translate }}</span>
          <a [routerLink]="['/login']" class="login-link" data-testid="qa-vote-login-link">
            {{ 'products.qa.loginNow' | translate }}
          </a>
        </div>
      }

      <!-- Questions List -->
      @if (questions().length > 0) {
        <div class="questions-list">
          @for (question of questions(); track question.id) {
            <div class="question-card">
              <div class="question-content">
                <div class="question-header">
                  <span class="question-icon">Q</span>
                  <span class="asker-name">{{ question.askerName || ('products.qa.anonymous' | translate) }}</span>
                  <span class="question-date">{{ question.createdAt | date:'mediumDate' }}</span>
                </div>
                <p class="question-text">{{ question.questionText }}</p>
                <div class="question-actions">
                  <button
                    type="button"
                    class="helpful-btn"
                    [class.active]="question.hasVotedHelpful"
                    [attr.aria-pressed]="isAuthenticated() ? question.hasVotedHelpful : null"
                    [attr.aria-busy]="votePending().has(question.id)"
                    [disabled]="!isAuthenticated() || votePending().has(question.id)"
                    [title]="(question.hasVotedHelpful
                      ? 'products.qa.youFoundHelpful'
                      : (isAuthenticated() ? 'products.qa.helpful' : 'products.qa.loginToVote')) | translate"
                    (click)="voteQuestion(question)"
                    data-testid="question-vote-btn">
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
                    <a [routerLink]="['/login']" class="answer-btn login-to-answer" data-testid="login-to-answer">
                      {{ 'products.qa.loginToAnswer' | translate }}
                    </a>
                  }
                </div>
              </div>

              <!-- Answer Form -->
              @if (showAnswerFormFor() === question.id) {
                <form
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
                      @if (submittingAnswer()) {
                        <span class="spinner"></span>
                      }
                      {{ 'products.qa.submitAnswer' | translate }}
                    </button>
                    <button
                      type="button"
                      class="cancel-answer-btn"
                      (click)="showAnswerFormFor.set(null)">
                      {{ 'common.cancel' | translate }}
                    </button>
                  </div>
                  @if (answerError()) {
                    <div class="error-message">
                      {{ answerError() | translate }}
                    </div>
                  }
                </form>
              }

              <!-- Answers -->
              @if (question.answers.length > 0) {
                <div class="answers-list">
                  @for (answer of question.answers; track answer.id) {
                    <div class="answer-card" [class.official]="answer.isOfficial">
                      <div class="answer-header">
                        <span class="answer-icon" [class.official]="answer.isOfficial">A</span>
                        <span class="answerer-name">
                          {{ answer.answererName || ('products.qa.communityMember' | translate) }}
                        </span>
                        @if (answer.isOfficial) {
                          <span class="official-badge">
                            {{ 'products.qa.officialAnswer' | translate }}
                          </span>
                        }
                        <span class="answer-date">{{ answer.createdAt | date:'mediumDate' }}</span>
                      </div>
                      <p class="answer-text">{{ answer.answerText }}</p>
                      <div class="answer-actions">
                        <button
                          type="button"
                          class="vote-btn helpful"
                          [class.active]="answer.userVoteHelpful === true"
                          [attr.aria-pressed]="isAuthenticated() ? (answer.userVoteHelpful === true) : null"
                          [attr.aria-label]="(answer.userVoteHelpful === true
                            ? 'products.qa.youFoundHelpful' : 'products.qa.markHelpful') | translate"
                          [attr.aria-busy]="votePending().has(answer.id)"
                          [disabled]="!isAuthenticated() || votePending().has(answer.id)"
                          [title]="(answer.userVoteHelpful === true
                            ? 'products.qa.youFoundHelpful'
                            : (isAuthenticated() ? 'products.qa.markHelpful' : 'products.qa.loginToVote')) | translate"
                          (click)="voteAnswer(answer, true)"
                          data-testid="answer-helpful-btn">
                          <span class="thumb-icon">&#128077;</span> {{ answer.helpfulCount }}
                        </button>
                        <button
                          type="button"
                          class="vote-btn unhelpful"
                          [class.active]="answer.userVoteHelpful === false"
                          [attr.aria-pressed]="isAuthenticated() ? (answer.userVoteHelpful === false) : null"
                          [attr.aria-label]="(answer.userVoteHelpful === false
                            ? 'products.qa.youFoundUnhelpful' : 'products.qa.markUnhelpful') | translate"
                          [attr.aria-busy]="votePending().has(answer.id)"
                          [disabled]="!isAuthenticated() || votePending().has(answer.id)"
                          [title]="(answer.userVoteHelpful === false
                            ? 'products.qa.youFoundUnhelpful'
                            : (isAuthenticated() ? 'products.qa.markUnhelpful' : 'products.qa.loginToVote')) | translate"
                          (click)="voteAnswer(answer, false)"
                          data-testid="answer-unhelpful-btn">
                          <span class="thumb-icon">&#128078;</span> {{ answer.unhelpfulCount }}
                        </button>
                      </div>
                    </div>
                  }
                </div>
              }

              <!-- No answers yet -->
              @if (question.answers.length === 0) {
                <div class="no-answers">
                  <span class="icon">&#128172;</span>
                  {{ 'products.qa.noAnswersYet' | translate }}
                </div>
              }
            </div>
          }
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && questions().length === 0) {
        <div class="empty-state">
          <span class="icon">&#128172;</span>
          <p>{{ 'products.qa.noQuestionsYet' | translate }}</p>
          <p class="hint">{{ 'products.qa.beFirstToAsk' | translate }}</p>
        </div>
      }

      <!-- Loading State -->
      @if (loading()) {
        <div class="loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Pagination -->
      @if (totalPages() > 1) {
        <div class="pagination">
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
      }
    </section>
  `,
  styles: [`
    :host {
      display: block;
    }

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

      /* The current user's own vote — pressed/active state. */
      &.active {
        background: var(--color-primary-light);
        border-color: var(--color-primary);
        color: var(--color-primary);
        font-weight: 600;
      }

      .thumb-icon {
        font-size: 1rem;
      }
    }

    .vote-error {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1.5rem;
      padding: 0.875rem 1.25rem;
      background: var(--color-error-light);
      border: 1px solid var(--color-error);
      border-radius: 8px;
      color: var(--color-error-dark);
      font-size: 0.875rem;

      .login-link {
        margin-left: auto;
        padding: 0.5rem 1rem;
        background: var(--color-primary);
        color: var(--color-text-inverse);
        border-radius: 6px;
        text-decoration: none;
        font-weight: 500;
        white-space: nowrap;
        transition: background 0.2s;

        &:hover {
          background: var(--color-primary-dark);
        }
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

  // Shown when a vote fails because the session expired mid-session (401).
  voteError = signal<string | null>(null);

  // Question-/answer-ids with a vote request currently in flight. Guards against
  // overlapping requests (rapid double-click / helpful↔unhelpful flip): a stale,
  // out-of-order response can otherwise clobber the newer authoritative state.
  votePending = signal<Set<string>>(new Set());

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
          this.questionError.set('products.qa.errors.sessionExpired');
        } else {
          this.questionError.set(apiErrorToTranslationKey(err, 'products.qa.errors.submitQuestionFailed'));
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
          this.answerError.set('products.qa.errors.sessionExpired');
        } else {
          this.answerError.set(apiErrorToTranslationKey(err, 'products.qa.errors.submitAnswerFailed'));
        }
      }
    });
  }

  voteQuestion(question: Question): void {
    // Voting requires auth; the button is disabled for anonymous users, but guard here too.
    // Ignore while a vote for this question is already in flight (prevents overlapping,
    // out-of-order responses that could clobber the newer authoritative state).
    if (!this.isAuthenticated() || this.votePending().has(question.id)) return;
    this.voteError.set(null);

    const wasVoted = question.hasVotedHelpful;
    // Optimistic toggle for instant feedback; reconciled with the authoritative response below.
    this.patchQuestion(question.id, {
      hasVotedHelpful: !wasVoted,
      helpfulCount: Math.max(0, question.helpfulCount + (wasVoted ? -1 : 1))
    });
    this.addVotePending(question.id);

    this.questionsService.voteQuestion(question.id).subscribe({
      next: (result) => {
        // Reconcile with the authoritative server state.
        this.patchQuestion(question.id, {
          hasVotedHelpful: result.hasVotedHelpful,
          helpfulCount: result.helpfulCount
        });
        this.removeVotePending(question.id);
      },
      error: (err) => {
        // Roll back the optimistic update.
        this.patchQuestion(question.id, {
          hasVotedHelpful: wasVoted,
          helpfulCount: question.helpfulCount
        });
        this.removeVotePending(question.id);
        if (err.status === 401) {
          this.voteError.set('products.qa.errors.sessionExpired');
        }
      }
    });
  }

  voteAnswer(answer: Answer, isHelpful: boolean): void {
    // Ignore while a vote for this answer is already in flight — this also guards the
    // helpful↔unhelpful flip race (both buttons share the answer id).
    if (!this.isAuthenticated() || this.votePending().has(answer.id)) return;
    this.voteError.set(null);

    // Snapshot for rollback on failure.
    const previous: Partial<Answer> = {
      userVoteHelpful: answer.userVoteHelpful ?? null,
      helpfulCount: answer.helpfulCount,
      unhelpfulCount: answer.unhelpfulCount
    };

    // Optimistic toggle/flip; reconciled with the authoritative response below.
    this.patchAnswer(answer.questionId, answer.id, this.computeAnswerVote(answer, isHelpful));
    this.addVotePending(answer.id);

    this.questionsService.voteAnswer(answer.id, isHelpful).subscribe({
      next: (result) => {
        this.patchAnswer(answer.questionId, answer.id, {
          helpfulCount: result.helpfulCount,
          unhelpfulCount: result.unhelpfulCount,
          userVoteHelpful: result.userVoteHelpful ?? null
        });
        this.removeVotePending(answer.id);
      },
      error: (err) => {
        this.patchAnswer(answer.questionId, answer.id, previous);
        this.removeVotePending(answer.id);
        if (err.status === 401) {
          this.voteError.set('products.qa.errors.sessionExpired');
        }
      }
    });
  }

  /** Mark a question-/answer-id as having a vote request in flight. */
  private addVotePending(id: string): void {
    this.votePending.update(pending => {
      const next = new Set(pending);
      next.add(id);
      return next;
    });
  }

  /** Clear the in-flight marker for a question-/answer-id once its vote settles. */
  private removeVotePending(id: string): void {
    this.votePending.update(pending => {
      const next = new Set(pending);
      next.delete(id);
      return next;
    });
  }

  /**
   * Pure toggle/flip transition for an answer vote (mirrors the backend semantics):
   * same vote again → un-vote; opposite vote → flip; no vote → add.
   */
  private computeAnswerVote(answer: Answer, isHelpful: boolean): Partial<Answer> {
    const current = answer.userVoteHelpful ?? null;
    let helpfulCount = answer.helpfulCount;
    let unhelpfulCount = answer.unhelpfulCount;
    let userVoteHelpful: boolean | null;

    if (current === isHelpful) {
      // Same vote again → un-vote.
      if (isHelpful) helpfulCount = Math.max(0, helpfulCount - 1);
      else unhelpfulCount = Math.max(0, unhelpfulCount - 1);
      userVoteHelpful = null;
    } else if (current === null) {
      // First vote.
      if (isHelpful) helpfulCount += 1;
      else unhelpfulCount += 1;
      userVoteHelpful = isHelpful;
    } else {
      // Flip to the opposite vote.
      if (isHelpful) {
        helpfulCount += 1;
        unhelpfulCount = Math.max(0, unhelpfulCount - 1);
      } else {
        unhelpfulCount += 1;
        helpfulCount = Math.max(0, helpfulCount - 1);
      }
      userVoteHelpful = isHelpful;
    }

    return { userVoteHelpful, helpfulCount, unhelpfulCount };
  }

  /** Immutably patch a single question in the questions signal. */
  private patchQuestion(questionId: string, patch: Partial<Question>): void {
    this.questionsData.update(data => {
      if (!data) return data;
      return {
        ...data,
        questions: data.questions.map(q =>
          q.id === questionId ? { ...q, ...patch } : q
        )
      };
    });
  }

  /** Immutably patch a single answer within its parent question in the questions signal. */
  private patchAnswer(questionId: string, answerId: string, patch: Partial<Answer>): void {
    this.questionsData.update(data => {
      if (!data) return data;
      return {
        ...data,
        questions: data.questions.map(q =>
          q.id === questionId
            ? { ...q, answers: q.answers.map(a => a.id === answerId ? { ...a, ...patch } : a) }
            : q
        )
      };
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
}
