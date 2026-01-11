import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { InputComponent } from '../../../shared/components/input/input.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    InputComponent,
    ButtonComponent
  ],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <h1>{{ 'auth.register.title' | translate }}</h1>
          <p>{{ 'auth.register.subtitle' | translate }}</p>
        </div>

        @if (errorMessage()) {
          <div class="error-alert" data-testid="register-error">
            {{ errorMessage() }}
          </div>
        }

        @if (successMessage()) {
          <div class="success-alert" data-testid="register-success">
            {{ successMessage() }}
          </div>
        }

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="name-row">
            <app-input
              formControlName="firstName"
              [label]="'auth.register.firstName' | translate"
              [placeholder]="'auth.register.firstName' | translate"
              data-testid="register-firstname"
            />

            <app-input
              formControlName="lastName"
              [label]="'auth.register.lastName' | translate"
              [placeholder]="'auth.register.lastName' | translate"
              data-testid="register-lastname"
            />
          </div>

          <app-input
            formControlName="email"
            type="email"
            [label]="'auth.register.email' | translate"
            [placeholder]="'auth.register.email' | translate"
            data-testid="register-email"
          />

          <app-input
            formControlName="password"
            type="password"
            [label]="'auth.register.password' | translate"
            [placeholder]="'auth.register.password' | translate"
            data-testid="register-password"
          />

          <app-input
            formControlName="confirmPassword"
            type="password"
            [label]="'auth.register.confirmPassword' | translate"
            [placeholder]="'auth.register.confirmPassword' | translate"
            data-testid="register-confirm-password"
          />

          @if (registerForm.errors?.['passwordMismatch']) {
            <div class="field-error" data-testid="validation-error">
              {{ 'errors.passwordMatch' | translate }}
            </div>
          }

          <label class="checkbox-label">
            <input type="checkbox" formControlName="terms">
            <span>{{ 'auth.register.terms' | translate }}</span>
          </label>

          <app-button
            type="submit"
            variant="primary"
            [fullWidth]="true"
            [loading]="authService.isLoading()"
            [disabled]="registerForm.invalid || authService.isLoading()"
            data-testid="register-submit"
          >
            {{ 'auth.register.submit' | translate }}
          </app-button>
        </form>

        <div class="auth-footer">
          <p>
            {{ 'auth.register.hasAccount' | translate }}
            <a routerLink="/login">{{ 'auth.register.signIn' | translate }}</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      background-color: var(--color-bg-secondary);
    }

    .auth-card {
      width: 100%;
      max-width: 480px;
      background: var(--color-bg-primary);
      border-radius: 12px;
      padding: 2.5rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
    }

    .auth-header {
      text-align: center;
      margin-bottom: 2rem;

      h1 {
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
      }
    }

    .error-alert {
      background-color: var(--color-error-bg);
      color: var(--color-error);
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin-bottom: 1.5rem;
      font-size: 0.875rem;
    }

    .success-alert {
      background-color: var(--color-success-bg);
      color: var(--color-success);
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin-bottom: 1.5rem;
      font-size: 0.875rem;
    }

    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .name-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .checkbox-label {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
      cursor: pointer;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      line-height: 1.4;

      input[type="checkbox"] {
        width: 1rem;
        height: 1rem;
        accent-color: var(--color-primary);
        margin-top: 0.125rem;
      }
    }

    .field-error {
      color: var(--color-error);
      font-size: 0.75rem;
      margin-top: -0.5rem;
    }

    .auth-footer {
      text-align: center;
      margin-top: 2rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border);

      p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;

        a {
          color: var(--color-primary);
          text-decoration: none;
          font-weight: 500;

          &:hover {
            text-decoration: underline;
          }
        }
      }
    }

    @media (max-width: 480px) {
      .name-row {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class RegisterComponent {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  registerForm: FormGroup = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
    terms: [false, [Validators.requiredTrue]]
  }, {
    validators: this.passwordMatchValidator
  });

  passwordMatchValidator(control: AbstractControl) {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.errorMessage.set(null);
    this.successMessage.set(null);

    const { firstName, lastName, email, password } = this.registerForm.value;

    this.authService.register({ firstName, lastName, email, password }).subscribe({
      next: () => {
        this.successMessage.set('Registration successful! Please check your email to verify your account.');
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 3000);
      },
      error: (error) => {
        this.errorMessage.set(error.error?.message || 'Registration failed. Please try again.');
      }
    });
  }
}
