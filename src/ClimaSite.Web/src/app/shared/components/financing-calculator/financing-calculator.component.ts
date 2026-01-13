import { Component, input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

export interface FinancingOption {
  months: number;
  interestRate: number; // Annual percentage rate
  label?: string;
}

const DEFAULT_OPTIONS: FinancingOption[] = [
  { months: 6, interestRate: 0, label: '6 months - 0% APR' },
  { months: 12, interestRate: 0, label: '12 months - 0% APR' },
  { months: 24, interestRate: 9.9, label: '24 months - 9.9% APR' },
  { months: 36, interestRate: 12.9, label: '36 months - 12.9% APR' }
];

@Component({
  selector: 'app-financing-calculator',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="financing-calculator" data-testid="financing-calculator">
      <h4 class="title">{{ 'products.financing.title' | translate }}</h4>

      @if (hasZeroInterestOption()) {
        <div class="promo-badge">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <path d="M16 8l-8 8"/>
            <circle cx="9" cy="9" r="2"/>
            <circle cx="15" cy="15" r="2"/>
          </svg>
          <span>{{ 'products.financing.zeroInterest' | translate }}</span>
        </div>
      }

      <div class="monthly-highlight">
        <span class="from-text">{{ 'products.financing.monthlyFrom' | translate:{ amount: (lowestMonthlyPayment() | currency:'EUR') } }}</span>
      </div>

      <div class="options-selector">
        @for (option of financingOptions(); track option.months) {
          <button
            class="option-btn"
            [class.selected]="selectedOption().months === option.months"
            (click)="selectOption(option)"
            [attr.data-testid]="'financing-option-' + option.months">
            <span class="months">{{ 'products.financing.months' | translate:{ count: option.months } }}</span>
            <span class="rate">
              @if (option.interestRate === 0) {
                {{ 'products.financing.zeroInterest' | translate }}
              } @else {
                {{ 'products.financing.interestRate' | translate:{ rate: option.interestRate } }}
              }
            </span>
          </button>
        }
      </div>

      <div class="calculation-result">
        <div class="result-row">
          <span class="label">{{ 'products.financing.monthlyPayment' | translate }}:</span>
          <span class="value highlight">{{ monthlyPayment() | currency:'EUR' }}</span>
        </div>
        <div class="result-row">
          <span class="label">{{ 'products.financing.totalCost' | translate }}:</span>
          <span class="value">{{ totalCost() | currency:'EUR' }}</span>
        </div>
        @if (interestCost() > 0) {
          <div class="result-row interest">
            <span class="label">Interest:</span>
            <span class="value">{{ interestCost() | currency:'EUR' }}</span>
          </div>
        }
      </div>

      <p class="disclaimer">
        *Representative example. Subject to credit approval. Terms and conditions apply.
      </p>
    </div>
  `,
  styles: [`
    .financing-calculator {
      padding: 1.25rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
    }

    .title {
      font-size: 1rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0 0 1rem;
    }

    .promo-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.375rem 0.75rem;
      background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
      color: white;
      font-size: 0.75rem;
      font-weight: 700;
      text-transform: uppercase;
      border-radius: 20px;
      margin-bottom: 1rem;

      svg {
        width: 16px;
        height: 16px;
      }
    }

    .monthly-highlight {
      margin-bottom: 1rem;

      .from-text {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-primary);
      }
    }

    .options-selector {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .option-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 0.75rem;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      background: var(--color-bg-primary);
      cursor: pointer;
      transition: all 0.2s;

      .months {
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--color-text-primary);
      }

      .rate {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
      }

      &:hover {
        border-color: var(--color-primary-light);
      }

      &.selected {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.05);

        .months {
          color: var(--color-primary);
        }
      }
    }

    .calculation-result {
      padding: 1rem;
      background: var(--color-bg-primary);
      border-radius: 8px;
      margin-bottom: 0.75rem;
    }

    .result-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.375rem 0;

      .label {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      .value {
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--color-text-primary);

        &.highlight {
          font-size: 1.125rem;
          color: var(--color-primary);
        }
      }

      &.interest {
        padding-top: 0.5rem;
        border-top: 1px dashed var(--color-border);
        margin-top: 0.25rem;

        .value {
          color: var(--color-text-secondary);
        }
      }
    }

    .disclaimer {
      font-size: 0.625rem;
      color: var(--color-text-secondary);
      line-height: 1.4;
      margin: 0;
    }

    @media (max-width: 480px) {
      .options-selector {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class FinancingCalculatorComponent {
  price = input.required<number>();
  options = input<FinancingOption[]>(DEFAULT_OPTIONS);

  selectedOption = signal<FinancingOption>(DEFAULT_OPTIONS[0]);

  financingOptions = computed(() => this.options());

  hasZeroInterestOption = computed(() =>
    this.financingOptions().some(o => o.interestRate === 0)
  );

  monthlyPayment = computed(() => {
    const principal = this.price();
    const option = this.selectedOption();

    if (option.interestRate === 0) {
      return principal / option.months;
    }

    // Calculate monthly payment with interest using amortization formula
    const monthlyRate = option.interestRate / 100 / 12;
    const payment = principal * (monthlyRate * Math.pow(1 + monthlyRate, option.months)) /
      (Math.pow(1 + monthlyRate, option.months) - 1);

    return payment;
  });

  totalCost = computed(() => {
    return this.monthlyPayment() * this.selectedOption().months;
  });

  interestCost = computed(() => {
    return this.totalCost() - this.price();
  });

  lowestMonthlyPayment = computed(() => {
    const principal = this.price();
    const options = this.financingOptions();

    let lowest = Infinity;
    for (const option of options) {
      let payment: number;
      if (option.interestRate === 0) {
        payment = principal / option.months;
      } else {
        const monthlyRate = option.interestRate / 100 / 12;
        payment = principal * (monthlyRate * Math.pow(1 + monthlyRate, option.months)) /
          (Math.pow(1 + monthlyRate, option.months) - 1);
      }
      if (payment < lowest) {
        lowest = payment;
      }
    }
    return lowest;
  });

  selectOption(option: FinancingOption): void {
    this.selectedOption.set(option);
  }
}
