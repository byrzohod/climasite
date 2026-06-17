import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

/**
 * Number of FAQ entries defined in i18n. Items are modelled as numbered keys
 * (`faq.items.0.question` / `faq.items.0.answer`) so they can be iterated without
 * relying on array support in the `| translate` pipe.
 */
export const FAQ_ITEM_COUNT = 8;

interface FaqItem {
  question: string;
  answer: string;
}

/**
 * Accessible FAQ accordion. Each entry is a button-toggled disclosure with
 * `aria-expanded`/`aria-controls` and full keyboard support (native button semantics).
 */
@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <section class="faq-page" data-testid="faq-page">
      <header class="faq-header">
        <h1 data-testid="faq-title">{{ 'faq.title' | translate }}</h1>
        <p class="faq-intro">{{ 'faq.intro' | translate }}</p>
      </header>

      <div class="faq-list">
        @for (item of items(); track $index) {
          <div class="faq-item" [class.faq-item--open]="isOpen($index)">
            <h2 class="faq-question-heading">
              <button
                type="button"
                class="faq-question"
                [attr.data-testid]="'faq-question-' + $index"
                [attr.aria-expanded]="isOpen($index)"
                [attr.aria-controls]="'faq-answer-' + $index"
                [id]="'faq-question-button-' + $index"
                (click)="toggle($index)"
              >
                <span class="faq-question-text">{{ item.question }}</span>
                <span class="faq-icon" aria-hidden="true">{{ isOpen($index) ? '−' : '+' }}</span>
              </button>
            </h2>
            @if (isOpen($index)) {
              <div
                class="faq-answer"
                role="region"
                [id]="'faq-answer-' + $index"
                [attr.aria-labelledby]="'faq-question-button-' + $index"
                [attr.data-testid]="'faq-answer-' + $index"
              >
                <p>{{ item.answer }}</p>
              </div>
            }
          </div>
        }
      </div>
    </section>
  `,
  styles: [`
    .faq-page {
      max-width: 48rem;
      margin: 0 auto;
      padding: 3rem 1.5rem 4rem;
      color: var(--color-text-primary);
    }

    .faq-header {
      text-align: center;
      margin-bottom: 2.5rem;
    }

    .faq-header h1 {
      font-size: 2.25rem;
      font-weight: 700;
      margin: 0 0 0.75rem;
    }

    .faq-intro {
      font-size: 1.125rem;
      color: var(--color-text-secondary);
      margin: 0;
    }

    .faq-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .faq-item {
      border: 1px solid var(--color-border-primary);
      border-radius: 0.625rem;
      background-color: var(--color-bg-secondary);
      overflow: hidden;
    }

    .faq-question-heading {
      margin: 0;
      font-size: inherit;
      font-weight: inherit;
    }

    .faq-question {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1.125rem 1.25rem;
      background: transparent;
      border: none;
      cursor: pointer;
      text-align: left;
      font-size: 1.0625rem;
      font-weight: 600;
      color: var(--color-text-primary);
      transition: background-color 0.2s ease;
    }

    .faq-question:hover {
      background-color: var(--color-bg-tertiary);
    }

    .faq-question:focus-visible {
      outline: 2px solid var(--color-border-focus);
      outline-offset: -2px;
    }

    .faq-icon {
      flex-shrink: 0;
      font-size: 1.5rem;
      line-height: 1;
      color: var(--color-primary);
    }

    .faq-answer {
      padding: 0 1.25rem 1.25rem;
    }

    .faq-answer p {
      margin: 0;
      font-size: 1rem;
      line-height: 1.7;
      color: var(--color-text-secondary);
    }

    @media (max-width: 640px) {
      .faq-page {
        padding: 2rem 1rem 3rem;
      }

      .faq-header h1 {
        font-size: 1.75rem;
      }
    }
  `]
})
export class FaqComponent {
  private readonly translate = inject(TranslateService);

  private readonly openIndex = signal<number | null>(null);
  private readonly lang = signal(this.translate.currentLang);

  readonly items = computed<FaqItem[]>(() => {
    // Touch the lang signal so the list recomputes on language switch.
    this.lang();
    const result: FaqItem[] = [];
    for (let i = 0; i < FAQ_ITEM_COUNT; i++) {
      result.push({
        question: this.translate.instant(`faq.items.${i}.question`),
        answer: this.translate.instant(`faq.items.${i}.answer`)
      });
    }
    return result;
  });

  constructor() {
    this.translate.onLangChange.subscribe(event => this.lang.set(event.lang));
  }

  isOpen(index: number): boolean {
    return this.openIndex() === index;
  }

  toggle(index: number): void {
    this.openIndex.set(this.isOpen(index) ? null : index);
  }
}
