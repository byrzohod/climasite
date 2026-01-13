import { Component, input, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { QuestionsService, Question, Answer, ProductQuestions } from '../../services/questions.service';

@Component({
  selector: 'app-product-qa',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
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
        <button
          class="ask-question-toggle"
          (click)="showAskForm.set(!showAskForm())"
          [class.active]="showAskForm()">
          <span class="icon">?</span>
          {{ 'products.qa.askQuestion' | translate }}
        </button>

        <form
          *ngIf="showAskForm()"
          [formGroup]="questionForm"
          (ngSubmit)="submitQuestion()"
          class="question-form">
          <div class="form-group">
            <label for="questionText">{{ 'products.qa.yourQuestion' | translate }}</label>
            <textarea
              id="questionText"
              formControlName="questionText"
              [placeholder]="'products.qa.questionPlaceholder' | translate"
              rows="3"
              maxlength="2000">
            </textarea>
            <span class="char-count">{{ questionForm.get('questionText')?.value?.length || 0 }}/2000</span>
            <span class="error" *ngIf="questionForm.get('questionText')?.errors?.['minlength']">
              {{ 'products.qa.errors.questionTooShort' | translate }}
            </span>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="askerName">{{ 'products.qa.yourName' | translate }}</label>
              <input
                type="text"
                id="askerName"
                formControlName="askerName"
                [placeholder]="'products.qa.namePlaceholder' | translate"
                maxlength="100">
            </div>
            <div class="form-group">
              <label for="askerEmail">{{ 'products.qa.yourEmail' | translate }}</label>
              <input
                type="email"
                id="askerEmail"
                formControlName="askerEmail"
                [placeholder]="'products.qa.emailPlaceholder' | translate">
              <span class="hint">{{ 'products.qa.emailHint' | translate }}</span>
            </div>
          </div>

          <button
            type="submit"
            class="submit-question-btn"
            [disabled]="questionForm.invalid || submittingQuestion()">
            <span *ngIf="submittingQuestion()" class="spinner"></span>
            {{ (submittingQuestion() ? 'products.qa.submitting' : 'products.qa.submitQuestion') | translate }}
          </button>

          <div class="success-message" *ngIf="questionSubmitted()">
            {{ 'products.qa.questionSubmittedSuccess' | translate }}
          </div>
        </form>
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
              <button
                class="answer-btn"
                (click)="toggleAnswerForm(question.id)">
                {{ 'products.qa.answer' | translate }}
              </button>
            </div>
          </div>

          <!-- Answer Form -->
          <form
            *ngIf="showAnswerFormFor() === question.id"
            [formGroup]="answerForm"
            (ngSubmit)="submitAnswer(question.id)"
            class="answer-form">
            <div class="form-group">
              <textarea
                formControlName="answerText"
                [placeholder]="'products.qa.answerPlaceholder' | translate"
                rows="2"
                maxlength="5000">
              </textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <input
                  type="text"
                  formControlName="answererName"
                  [placeholder]="'products.qa.yourName' | translate"
                  maxlength="100">
              </div>
              <button
                type="submit"
                class="submit-answer-btn"
                [disabled]="answerForm.invalid || submittingAnswer()">
                {{ 'products.qa.submitAnswer' | translate }}
              </button>
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
      padding: var(--spacing-lg);
      background: var(--color-surface);
      border-radius: var(--radius-lg);
    }

    .qa-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--spacing-lg);
      flex-wrap: wrap;
      gap: var(--spacing-sm);

      h2 {
        font-size: var(--font-size-xl);
        color: var(--color-text-primary);
        margin: 0;
      }

      .qa-stats {
        display: flex;
        gap: var(--spacing-md);
        font-size: var(--font-size-sm);
        color: var(--color-text-secondary);
      }
    }

    .ask-question-section {
      margin-bottom: var(--spacing-xl);
    }

    .ask-question-toggle {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      padding: var(--spacing-md) var(--spacing-lg);
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: var(--radius-md);
      font-size: var(--font-size-md);
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
      margin-top: var(--spacing-md);
      padding: var(--spacing-lg);
      background: var(--color-background);
      border-radius: var(--radius-md);
      border: 1px solid var(--color-border);
    }

    .form-group {
      margin-bottom: var(--spacing-md);
      position: relative;

      label {
        display: block;
        margin-bottom: var(--spacing-xs);
        font-weight: 500;
        color: var(--color-text-primary);
      }

      input,
      textarea {
        width: 100%;
        padding: var(--spacing-sm) var(--spacing-md);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);
        font-size: var(--font-size-md);
        background: var(--color-surface);
        color: var(--color-text-primary);

        &:focus {
          outline: none;
          border-color: var(--color-primary);
          box-shadow: 0 0 0 2px var(--color-primary-light);
        }
      }

      textarea {
        resize: vertical;
        min-height: 80px;
      }

      .char-count {
        position: absolute;
        right: var(--spacing-sm);
        bottom: var(--spacing-xs);
        font-size: var(--font-size-xs);
        color: var(--color-text-tertiary);
      }

      .hint {
        font-size: var(--font-size-xs);
        color: var(--color-text-tertiary);
        margin-top: var(--spacing-xs);
      }

      .error {
        font-size: var(--font-size-xs);
        color: var(--color-error);
        margin-top: var(--spacing-xs);
      }
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--spacing-md);

      @media (max-width: 600px) {
        grid-template-columns: 1fr;
      }
    }

    .submit-question-btn,
    .submit-answer-btn {
      padding: var(--spacing-sm) var(--spacing-lg);
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: var(--radius-sm);
      font-size: var(--font-size-md);
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
      margin-top: var(--spacing-md);
      padding: var(--spacing-md);
      background: var(--color-success-light);
      color: var(--color-success);
      border-radius: var(--radius-sm);
      text-align: center;
    }

    .questions-list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-lg);
    }

    .question-card {
      padding: var(--spacing-lg);
      background: var(--color-background);
      border-radius: var(--radius-md);
      border: 1px solid var(--color-border);
    }

    .question-header,
    .answer-header {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
      margin-bottom: var(--spacing-sm);
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
      color: white;
      border-radius: 50%;
      font-weight: bold;
      font-size: var(--font-size-sm);

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
      font-size: var(--font-size-sm);
      color: var(--color-text-tertiary);
      margin-left: auto;
    }

    .official-badge {
      padding: var(--spacing-xs) var(--spacing-sm);
      background: var(--color-success-light);
      color: var(--color-success);
      border-radius: var(--radius-sm);
      font-size: var(--font-size-xs);
      font-weight: 500;
    }

    .question-text,
    .answer-text {
      margin: 0 0 var(--spacing-md);
      color: var(--color-text-primary);
      line-height: 1.6;
    }

    .question-actions,
    .answer-actions {
      display: flex;
      gap: var(--spacing-md);
    }

    .helpful-btn,
    .answer-btn,
    .vote-btn {
      display: flex;
      align-items: center;
      gap: var(--spacing-xs);
      padding: var(--spacing-xs) var(--spacing-sm);
      background: transparent;
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      font-size: var(--font-size-sm);
      color: var(--color-text-secondary);
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-background);
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .thumb-icon {
        font-size: var(--font-size-md);
      }
    }

    .answers-list {
      margin-top: var(--spacing-lg);
      padding-left: var(--spacing-xl);
      border-left: 2px solid var(--color-border);
    }

    .answer-card {
      padding: var(--spacing-md);
      margin-bottom: var(--spacing-md);
      background: var(--color-surface);
      border-radius: var(--radius-sm);

      &.official {
        background: var(--color-success-light);
        border-left: 3px solid var(--color-success);
      }

      &:last-child {
        margin-bottom: 0;
      }
    }

    .no-answers {
      margin-top: var(--spacing-md);
      padding: var(--spacing-md);
      text-align: center;
      color: var(--color-text-tertiary);
      font-style: italic;

      .icon {
        display: block;
        font-size: 24px;
        margin-bottom: var(--spacing-xs);
      }
    }

    .empty-state {
      text-align: center;
      padding: var(--spacing-xl);
      color: var(--color-text-secondary);

      .icon {
        font-size: 48px;
        display: block;
        margin-bottom: var(--spacing-md);
      }

      p {
        margin: 0 0 var(--spacing-xs);
      }

      .hint {
        font-size: var(--font-size-sm);
        color: var(--color-text-tertiary);
      }
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: var(--spacing-xl);
      color: var(--color-text-secondary);
    }

    .spinner {
      width: 24px;
      height: 24px;
      border: 2px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin-bottom: var(--spacing-sm);
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: var(--spacing-md);
      margin-top: var(--spacing-xl);
      padding-top: var(--spacing-lg);
      border-top: 1px solid var(--color-border);
    }

    .page-btn {
      padding: var(--spacing-sm) var(--spacing-md);
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
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
      font-size: var(--font-size-sm);
      color: var(--color-text-secondary);
    }
  `]
})
export class ProductQaComponent {
  productId = input.required<string>();

  private readonly questionsService = inject(QuestionsService);
  private readonly fb = inject(FormBuilder);

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

  votedQuestions = signal<Set<string>>(new Set());
  votedAnswers = signal<Set<string>>(new Set());

  questionForm: FormGroup;
  answerForm: FormGroup;

  constructor() {
    this.questionForm = this.fb.group({
      questionText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
      askerName: ['', Validators.maxLength(100)],
      askerEmail: ['', Validators.email]
    });

    this.answerForm = this.fb.group({
      answerText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(5000)]],
      answererName: ['', Validators.maxLength(100)]
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
    if (this.questionForm.invalid) return;

    this.submittingQuestion.set(true);
    this.questionSubmitted.set(false);

    const request = {
      productId: this.productId(),
      questionText: this.questionForm.value.questionText,
      askerName: this.questionForm.value.askerName || undefined,
      askerEmail: this.questionForm.value.askerEmail || undefined
    };

    this.questionsService.askQuestion(request).subscribe({
      next: () => {
        this.submittingQuestion.set(false);
        this.questionSubmitted.set(true);
        this.questionForm.reset();
        // Questions need approval, so don't reload immediately
        setTimeout(() => this.questionSubmitted.set(false), 5000);
      },
      error: () => {
        this.submittingQuestion.set(false);
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
    if (this.answerForm.invalid) return;

    this.submittingAnswer.set(true);

    const request = {
      answerText: this.answerForm.value.answerText,
      answererName: this.answerForm.value.answererName || undefined
    };

    this.questionsService.answerQuestion(questionId, request).subscribe({
      next: () => {
        this.submittingAnswer.set(false);
        this.showAnswerFormFor.set(null);
        this.answerForm.reset();
        // Answers need approval, so don't reload immediately
      },
      error: () => {
        this.submittingAnswer.set(false);
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
