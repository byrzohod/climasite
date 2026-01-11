import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TranslateModule],
  template: `
    <div class="contact-container" data-testid="contact-page">
      <div class="contact-header">
        <h1>{{ 'contact.title' | translate }}</h1>
        <p>{{ 'contact.subtitle' | translate }}</p>
      </div>

      <div class="contact-content">
        <div class="contact-form-section">
          <form [formGroup]="contactForm" (ngSubmit)="onSubmit()" data-testid="contact-form">
            @if (submitSuccess()) {
              <div class="success-message" data-testid="contact-success">
                {{ 'contact.form.success' | translate }}
              </div>
            }
            @if (submitError()) {
              <div class="error-message" data-testid="contact-error">
                {{ 'contact.form.error' | translate }}
              </div>
            }

            <div class="form-group">
              <label for="name">{{ 'contact.form.name' | translate }}</label>
              <input
                type="text"
                id="name"
                formControlName="name"
                data-testid="contact-name"
                [class.invalid]="contactForm.get('name')?.invalid && contactForm.get('name')?.touched"
              />
              @if (contactForm.get('name')?.invalid && contactForm.get('name')?.touched) {
                <span class="error">{{ 'errors.required' | translate }}</span>
              }
            </div>

            <div class="form-group">
              <label for="email">{{ 'contact.form.email' | translate }}</label>
              <input
                type="email"
                id="email"
                formControlName="email"
                data-testid="contact-email"
                [class.invalid]="contactForm.get('email')?.invalid && contactForm.get('email')?.touched"
              />
              @if (contactForm.get('email')?.errors?.['required'] && contactForm.get('email')?.touched) {
                <span class="error">{{ 'errors.required' | translate }}</span>
              }
              @if (contactForm.get('email')?.errors?.['email'] && contactForm.get('email')?.touched) {
                <span class="error">{{ 'errors.email' | translate }}</span>
              }
            </div>

            <div class="form-group">
              <label for="subject">{{ 'contact.form.subject' | translate }}</label>
              <input
                type="text"
                id="subject"
                formControlName="subject"
                data-testid="contact-subject"
                [class.invalid]="contactForm.get('subject')?.invalid && contactForm.get('subject')?.touched"
              />
              @if (contactForm.get('subject')?.invalid && contactForm.get('subject')?.touched) {
                <span class="error">{{ 'errors.required' | translate }}</span>
              }
            </div>

            <div class="form-group">
              <label for="message">{{ 'contact.form.message' | translate }}</label>
              <textarea
                id="message"
                formControlName="message"
                rows="5"
                data-testid="contact-message"
                [class.invalid]="contactForm.get('message')?.invalid && contactForm.get('message')?.touched"
              ></textarea>
              @if (contactForm.get('message')?.invalid && contactForm.get('message')?.touched) {
                <span class="error">{{ 'errors.required' | translate }}</span>
              }
            </div>

            <button
              type="submit"
              class="submit-button"
              [disabled]="contactForm.invalid || isSubmitting()"
              data-testid="contact-submit"
            >
              @if (isSubmitting()) {
                {{ 'common.loading' | translate }}
              } @else {
                {{ 'contact.form.submit' | translate }}
              }
            </button>
          </form>
        </div>

        <div class="contact-info-section">
          <h2>{{ 'contact.info.title' | translate }}</h2>

          <div class="info-item">
            <div class="info-icon">📍</div>
            <div class="info-content">
              <h3>{{ 'contact.info.address' | translate }}</h3>
              <p>{{ 'contact.info.addressValue' | translate }}</p>
            </div>
          </div>

          <div class="info-item">
            <div class="info-icon">📞</div>
            <div class="info-content">
              <h3>{{ 'contact.info.phone' | translate }}</h3>
              <p>{{ 'contact.info.phoneValue' | translate }}</p>
            </div>
          </div>

          <div class="info-item">
            <div class="info-icon">✉️</div>
            <div class="info-content">
              <h3>{{ 'contact.info.email' | translate }}</h3>
              <p>{{ 'contact.info.emailValue' | translate }}</p>
            </div>
          </div>

          <div class="info-item">
            <div class="info-icon">🕐</div>
            <div class="info-content">
              <h3>{{ 'contact.info.hours' | translate }}</h3>
              <p>{{ 'contact.info.hoursValue' | translate }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .contact-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .contact-header {
      text-align: center;
      margin-bottom: 3rem;

      h1 {
        font-size: 2.5rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        font-size: 1.125rem;
        color: var(--color-text-secondary);
      }
    }

    .contact-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
    }

    .contact-form-section {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 2rem;
    }

    .form-group {
      margin-bottom: 1.5rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        color: var(--color-text-primary);
        font-weight: 500;
      }

      input, textarea {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        font-size: 1rem;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        transition: border-color 0.2s;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }

        &.invalid {
          border-color: var(--color-error);
        }
      }

      textarea {
        resize: vertical;
        min-height: 120px;
      }

      .error {
        display: block;
        color: var(--color-error);
        font-size: 0.875rem;
        margin-top: 0.25rem;
      }
    }

    .submit-button {
      width: 100%;
      padding: 1rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .success-message {
      background: var(--color-success-bg, #d4edda);
      color: var(--color-success, #155724);
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }

    .error-message {
      background: var(--color-error-bg, #f8d7da);
      color: var(--color-error, #721c24);
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }

    .contact-info-section {
      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 2rem;
      }
    }

    .info-item {
      display: flex;
      gap: 1rem;
      margin-bottom: 1.5rem;
      padding: 1rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;

      .info-icon {
        font-size: 1.5rem;
      }

      .info-content {
        h3 {
          font-size: 1rem;
          color: var(--color-text-primary);
          margin-bottom: 0.25rem;
        }

        p {
          color: var(--color-text-secondary);
          margin: 0;
        }
      }
    }

    @media (max-width: 768px) {
      .contact-content {
        grid-template-columns: 1fr;
      }

      .contact-header h1 {
        font-size: 2rem;
      }
    }
  `]
})
export class ContactComponent {
  contactForm: FormGroup;
  isSubmitting = signal(false);
  submitSuccess = signal(false);
  submitError = signal(false);

  constructor(private fb: FormBuilder) {
    this.contactForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      subject: ['', Validators.required],
      message: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.contactForm.invalid) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitSuccess.set(false);
    this.submitError.set(false);

    // Simulate API call
    setTimeout(() => {
      this.isSubmitting.set(false);
      this.submitSuccess.set(true);
      this.contactForm.reset();
    }, 1000);
  }
}
