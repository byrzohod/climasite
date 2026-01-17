import { Component, input, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { InstallationService, InstallationOption, ProductInstallationOptions } from '../../services/installation.service';

@Component({
  selector: 'app-installation-service',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  template: `
    <section class="installation-service" *ngIf="options() && options()!.installationAvailable">
      <div class="installation-header">
        <h3>{{ 'products.installation.title' | translate }}</h3>
        <p class="subtitle">{{ 'products.installation.subtitle' | translate }}</p>
      </div>

      <!-- Installation Options -->
      <div class="options-grid">
        <div
          class="option-card"
          *ngFor="let option of options()!.options"
          [class.selected]="selectedOption()?.type === option.type"
          [class.recommended]="option.type === 'Premium'"
          (click)="selectOption(option)">
          <div class="option-badge" *ngIf="option.type === 'Premium'">
            {{ 'products.installation.recommended' | translate }}
          </div>
          <div class="option-badge express" *ngIf="option.type === 'Express'">
            {{ 'products.installation.fastest' | translate }}
          </div>
          <h4 class="option-name">{{ option.name }}</h4>
          <p class="option-description">{{ option.description }}</p>
          <div class="option-price">
            {{ 'common.currency' | translate: { amount: option.price | number:'1.2-2' } }}
          </div>
          <div class="option-timeline">
            <span class="icon">&#128197;</span>
            {{ 'products.installation.estimatedTime' | translate: { days: option.estimatedDays } }}
          </div>
          <ul class="option-features">
            <li *ngFor="let feature of option.features">
              <span class="check">&#10003;</span>
              {{ feature }}
            </li>
          </ul>
          <button class="select-btn" [class.selected]="selectedOption()?.type === option.type">
            {{ (selectedOption()?.type === option.type ? 'products.installation.selected' : 'products.installation.select') | translate }}
          </button>
        </div>
      </div>

      <!-- Request Form (shown when option selected) -->
      <div class="request-form-section" *ngIf="selectedOption() && showForm()">
        <h4>{{ 'products.installation.requestTitle' | translate }}</h4>
        <form [formGroup]="requestForm" (ngSubmit)="submitRequest()">
          <div class="form-row">
            <div class="form-group">
              <label for="customerName">{{ 'products.installation.customerName' | translate }} *</label>
              <input
                type="text"
                id="customerName"
                formControlName="customerName"
                [placeholder]="'products.installation.namePlaceholder' | translate">
            </div>
            <div class="form-group">
              <label for="customerEmail">{{ 'products.installation.customerEmail' | translate }} *</label>
              <input
                type="email"
                id="customerEmail"
                formControlName="customerEmail"
                [placeholder]="'products.installation.emailPlaceholder' | translate">
            </div>
          </div>

          <div class="form-group">
            <label for="customerPhone">{{ 'products.installation.customerPhone' | translate }} *</label>
            <input
              type="tel"
              id="customerPhone"
              formControlName="customerPhone"
              [placeholder]="'products.installation.phonePlaceholder' | translate">
          </div>

          <div class="form-group">
            <label for="addressLine1">{{ 'products.installation.address' | translate }} *</label>
            <input
              type="text"
              id="addressLine1"
              formControlName="addressLine1"
              [placeholder]="'products.installation.addressPlaceholder' | translate">
          </div>

          <div class="form-group">
            <label for="addressLine2">{{ 'products.installation.addressLine2' | translate }}</label>
            <input
              type="text"
              id="addressLine2"
              formControlName="addressLine2"
              [placeholder]="'products.installation.addressLine2Placeholder' | translate">
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="city">{{ 'products.installation.city' | translate }} *</label>
              <input
                type="text"
                id="city"
                formControlName="city"
                [placeholder]="'products.installation.cityPlaceholder' | translate">
            </div>
            <div class="form-group">
              <label for="postalCode">{{ 'products.installation.postalCode' | translate }} *</label>
              <input
                type="text"
                id="postalCode"
                formControlName="postalCode"
                [placeholder]="'products.installation.postalCodePlaceholder' | translate">
            </div>
          </div>

          <div class="form-group">
            <label for="country">{{ 'products.installation.country' | translate }} *</label>
            <select id="country" formControlName="country">
              <option value="">{{ 'products.installation.selectCountry' | translate }}</option>
              <option value="Bulgaria">Bulgaria</option>
              <option value="Germany">Germany</option>
              <option value="Austria">Austria</option>
              <option value="Romania">Romania</option>
              <option value="Greece">Greece</option>
            </select>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="preferredDate">{{ 'products.installation.preferredDate' | translate }}</label>
              <input
                type="date"
                id="preferredDate"
                formControlName="preferredDate"
                [min]="minDate">
            </div>
            <div class="form-group">
              <label for="preferredTimeSlot">{{ 'products.installation.preferredTime' | translate }}</label>
              <select id="preferredTimeSlot" formControlName="preferredTimeSlot">
                <option value="">{{ 'products.installation.anyTime' | translate }}</option>
                <option value="morning">{{ 'products.installation.morning' | translate }} (8:00 - 12:00)</option>
                <option value="afternoon">{{ 'products.installation.afternoon' | translate }} (12:00 - 17:00)</option>
                <option value="evening">{{ 'products.installation.evening' | translate }} (17:00 - 20:00)</option>
              </select>
            </div>
          </div>

          <div class="form-group">
            <label for="notes">{{ 'products.installation.notes' | translate }}</label>
            <textarea
              id="notes"
              formControlName="notes"
              rows="3"
              [placeholder]="'products.installation.notesPlaceholder' | translate">
            </textarea>
          </div>

          <div class="form-summary">
            <div class="summary-row">
              <span>{{ 'products.installation.selectedService' | translate }}</span>
              <span class="value">{{ selectedOption()!.name }}</span>
            </div>
            <div class="summary-row total">
              <span>{{ 'products.installation.estimatedCost' | translate }}</span>
              <span class="value">{{ 'common.currency' | translate: { amount: selectedOption()!.price | number:'1.2-2' } }}</span>
            </div>
          </div>

          <button
            type="submit"
            class="submit-btn"
            [disabled]="requestForm.invalid || submitting()">
            <span *ngIf="submitting()" class="spinner"></span>
            {{ (submitting() ? 'products.installation.submitting' : 'products.installation.submitRequest') | translate }}
          </button>
        </form>
      </div>

      <!-- Success Message -->
      <div class="success-message" *ngIf="requestSubmitted()">
        <span class="icon">&#10003;</span>
        <h4>{{ 'products.installation.requestSubmitted' | translate }}</h4>
        <p>{{ 'products.installation.requestSubmittedMessage' | translate }}</p>
      </div>

      <!-- Toggle Form Button (when option is selected but form is hidden) -->
      <button
        *ngIf="selectedOption() && !showForm() && !requestSubmitted()"
        class="show-form-btn"
        (click)="showForm.set(true)">
        {{ 'products.installation.requestInstallation' | translate }}
      </button>
    </section>

    <!-- No Installation Available -->
    <div class="no-installation" *ngIf="options() && !options()!.installationAvailable">
      <p>{{ 'products.installation.notAvailable' | translate }}</p>
    </div>
  `,
  styles: [`
    .installation-service {
      padding: 1.5rem;
      background: var(--color-bg-card);
      border-radius: 12px;
    }

    .installation-header {
      text-align: center;
      margin-bottom: 2rem;

      h3 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin: 0 0 0.75rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .options-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .option-card {
      position: relative;
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border: 2px solid var(--color-border-primary);
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-primary);
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }

      &.selected {
        border-color: var(--color-primary);
        background: var(--color-primary-light);
      }

      &.recommended {
        border-color: var(--color-success);
      }
    }

    .option-badge {
      position: absolute;
      top: -10px;
      right: 1rem;
      padding: 0.5rem 0.75rem;
      background: var(--color-success);
      color: white;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: 4px;

      &.express {
        background: var(--color-warning);
      }
    }

    .option-name {
      font-size: 1.25rem;
      color: var(--color-text-primary);
      margin: 0 0 0.75rem;
    }

    .option-description {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin: 0 0 1rem;
    }

    .option-price {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--color-primary);
      margin-bottom: 0.75rem;
    }

    .option-timeline {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin-bottom: 1rem;
    }

    .option-features {
      list-style: none;
      padding: 0;
      margin: 0 0 1rem;

      li {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 0;
        font-size: 0.875rem;
        color: var(--color-text-secondary);

        .check {
          color: var(--color-success);
          font-weight: bold;
        }
      }
    }

    .select-btn {
      width: 100%;
      padding: 0.75rem;
      background: transparent;
      border: 1px solid var(--color-primary);
      color: var(--color-primary);
      border-radius: 4px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        background: var(--color-primary);
        color: white;
      }

      &.selected {
        background: var(--color-primary);
        color: white;
      }
    }

    .request-form-section {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      border: 1px solid var(--color-border-primary);
      margin-top: 1.5rem;

      h4 {
        font-size: 1.25rem;
        color: var(--color-text-primary);
        margin: 0 0 1.5rem;
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

    .form-group {
      margin-bottom: 1rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        font-weight: 500;
        color: var(--color-text-primary);
      }

      input,
      select,
      textarea {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border-primary);
        border-radius: 4px;
        font-size: 1rem;
        background: var(--color-bg-card);
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
    }

    .form-summary {
      padding: 1rem;
      background: var(--color-bg-card);
      border-radius: 4px;
      margin-bottom: 1.5rem;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      color: var(--color-text-secondary);

      .value {
        font-weight: 500;
        color: var(--color-text-primary);
      }

      &.total {
        border-top: 1px solid var(--color-border-primary);
        margin-top: 0.75rem;
        padding-top: 1rem;
        font-size: 1.25rem;

        .value {
          color: var(--color-primary);
          font-weight: 700;
        }
      }
    }

    .submit-btn,
    .show-form-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      width: 100%;
      padding: 1rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
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

    .show-form-btn {
      margin-top: 1.5rem;
    }

    .success-message {
      text-align: center;
      padding: 2rem;
      background: var(--color-success-light);
      border-radius: 8px;
      margin-top: 1.5rem;

      .icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 48px;
        height: 48px;
        margin: 0 auto 1rem;
        background: var(--color-success);
        color: white;
        border-radius: 50%;
        font-size: 24px;
      }

      h4 {
        color: var(--color-success);
        margin: 0 0 0.75rem;
      }

      p {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .no-installation {
      padding: 1.5rem;
      text-align: center;
      color: var(--color-text-secondary);
      background: var(--color-bg-card);
      border-radius: 12px;
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid white;
      border-top-color: transparent;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class InstallationServiceComponent {
  productId = input.required<string>();

  private readonly installationService = inject(InstallationService);
  private readonly fb = inject(FormBuilder);

  options = signal<ProductInstallationOptions | null>(null);
  selectedOption = signal<InstallationOption | null>(null);
  showForm = signal(false);
  submitting = signal(false);
  requestSubmitted = signal(false);

  requestForm: FormGroup;
  minDate: string;

  constructor() {
    this.requestForm = this.fb.group({
      customerName: ['', [Validators.required, Validators.maxLength(200)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      customerPhone: ['', [Validators.required, Validators.maxLength(50)]],
      addressLine1: ['', [Validators.required, Validators.maxLength(255)]],
      addressLine2: ['', Validators.maxLength(255)],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      postalCode: ['', [Validators.required, Validators.maxLength(20)]],
      country: ['', Validators.required],
      preferredDate: [''],
      preferredTimeSlot: [''],
      notes: ['', Validators.maxLength(2000)]
    });

    // Set min date to tomorrow
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.minDate = tomorrow.toISOString().split('T')[0];

    // Load installation options when productId changes
    effect(() => {
      const id = this.productId();
      if (id) {
        this.loadOptions();
      }
    });
  }

  loadOptions(): void {
    this.installationService.getInstallationOptions(this.productId()).subscribe({
      next: (options) => {
        this.options.set(options);
      },
      error: () => {
        this.options.set(null);
      }
    });
  }

  selectOption(option: InstallationOption): void {
    this.selectedOption.set(option);
  }

  submitRequest(): void {
    if (this.requestForm.invalid || !this.selectedOption()) return;

    this.submitting.set(true);

    const data = {
      productId: this.productId(),
      installationType: this.selectedOption()!.type,
      customerName: this.requestForm.value.customerName,
      customerEmail: this.requestForm.value.customerEmail,
      customerPhone: this.requestForm.value.customerPhone,
      addressLine1: this.requestForm.value.addressLine1,
      addressLine2: this.requestForm.value.addressLine2 || undefined,
      city: this.requestForm.value.city,
      postalCode: this.requestForm.value.postalCode,
      country: this.requestForm.value.country,
      preferredDate: this.requestForm.value.preferredDate || undefined,
      preferredTimeSlot: this.requestForm.value.preferredTimeSlot || undefined,
      notes: this.requestForm.value.notes || undefined
    };

    this.installationService.createInstallationRequest(data).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.requestSubmitted.set(true);
        this.requestForm.reset();
      },
      error: () => {
        this.submitting.set(false);
      }
    });
  }
}
