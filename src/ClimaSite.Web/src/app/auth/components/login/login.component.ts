import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { InputComponent } from '../../../shared/components/input/input.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-login',
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
          <h1>{{ 'auth.login.title' | translate }}</h1>
          <p>{{ 'auth.login.subtitle' | translate }}</p>
        </div>

        @if (errorMessage()) {
          <div class="error-alert" data-testid="login-error">
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="auth-form">
          <app-input
            formControlName="email"
            type="email"
            [label]="'auth.login.email' | translate"
            [placeholder]="'auth.login.email' | translate"
            [error]="getEmailError()"
            autocomplete="email"
            data-testid="login-email"
          />

          <app-input
            formControlName="password"
            type="password"
            [label]="'auth.login.password' | translate"
            [placeholder]="'auth.login.password' | translate"
            [error]="getPasswordError()"
            autocomplete="current-password"
            data-testid="login-password"
          />

          <div class="form-options">
            <label class="checkbox-label">
              <input type="checkbox" formControlName="rememberMe">
              <span>{{ 'auth.login.rememberMe' | translate }}</span>
            </label>
            <a routerLink="/forgot-password" class="forgot-link">
              {{ 'auth.login.forgotPassword' | translate }}
            </a>
          </div>

          <app-button
            type="submit"
            variant="primary"
            [fullWidth]="true"
            [loading]="authService.isLoading()"
            [disabled]="loginForm.invalid || authService.isLoading()"
            data-testid="login-submit"
          >
            {{ 'auth.login.submit' | translate }}
          </app-button>
        </form>

        <div class="auth-footer">
          <p>
            {{ 'auth.login.noAccount' | translate }}
            <a routerLink="/register" data-testid="register-link">{{ 'auth.login.signUp' | translate }}</a>
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
      max-width: 420px;
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

    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .form-options {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.875rem;
    }

    .checkbox-label {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      color: var(--color-text-secondary);
      user-select: none;

      input[type="checkbox"] {
        position: relative;
        width: 1.125rem;
        height: 1.125rem;
        accent-color: var(--color-primary);
        cursor: pointer;
        appearance: none;
        -webkit-appearance: none;
        background-color: var(--color-bg-input);
        border: 2px solid var(--color-border-primary);
        border-radius: 0.25rem;
        transition: background-color 0.2s ease-out, border-color 0.2s ease-out, transform 0.15s ease-out;

        &:hover {
          border-color: var(--color-primary);
        }

        &:checked {
          background-color: var(--color-primary);
          border-color: var(--color-primary);
        }

        &:checked::after {
          content: '';
          position: absolute;
          left: 0.25rem;
          top: 0.0625rem;
          width: 0.375rem;
          height: 0.625rem;
          border: solid var(--color-text-inverse);
          border-width: 0 2px 2px 0;
          transform: rotate(45deg);
          animation: checkboxPop 0.2s ease-out forwards;
        }

        &:focus-visible {
          outline: 2px solid var(--color-primary);
          outline-offset: 2px;
          box-shadow: 0 0 0 3px var(--color-primary-light);
        }

        &:active {
          transform: scale(0.9);
        }
      }

      @keyframes checkboxPop {
        0% { opacity: 0; transform: rotate(45deg) scale(0); }
        60% { transform: rotate(45deg) scale(1.2); }
        100% { opacity: 1; transform: rotate(45deg) scale(1); }
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .checkbox-label input[type="checkbox"] {
        transition: none !important;

        &:checked::after {
          animation: none !important;
          opacity: 1;
          transform: rotate(45deg) scale(1);
        }
      }
    }

    .forgot-link {
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 500;

      &:hover {
        text-decoration: underline;
      }
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
  `]
})
export class LoginComponent {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);

  readonly errorMessage = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false]
  });

  onSubmit(): void {
    // Mark all fields as touched to show validation errors
    this.loginForm.markAllAsTouched();

    if (this.loginForm.invalid) return;

    this.errorMessage.set(null);
    const { email, password } = this.loginForm.value;

    this.authService.login({ email, password }).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
        this.router.navigateByUrl(returnUrl);
      },
      error: (error) => {
        this.errorMessage.set(error.error?.message || 'Login failed. Please try again.');
      }
    });
  }

  getEmailError(): string {
    const email = this.loginForm.get('email');
    if (email?.touched && email?.errors) {
      if (email.errors['required']) return this.translate.instant('auth.validation.emailRequired');
      if (email.errors['email']) return this.translate.instant('auth.validation.emailInvalid');
    }
    return '';
  }

  getPasswordError(): string {
    const password = this.loginForm.get('password');
    if (password?.touched && password?.errors) {
      if (password.errors['required']) return this.translate.instant('auth.validation.passwordRequired');
    }
    return '';
  }
}
