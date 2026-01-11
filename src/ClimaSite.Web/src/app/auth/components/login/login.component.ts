import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
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
            data-testid="login-email"
          />

          <app-input
            formControlName="password"
            type="password"
            [label]="'auth.login.password' | translate"
            [placeholder]="'auth.login.password' | translate"
            [error]="getPasswordError()"
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
      display: flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      color: var(--color-text-secondary);

      input[type="checkbox"] {
        width: 1rem;
        height: 1rem;
        accent-color: var(--color-primary);
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

  readonly errorMessage = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
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
      if (email.errors['required']) return 'Email is required';
      if (email.errors['email']) return 'Please enter a valid email';
    }
    return '';
  }

  getPasswordError(): string {
    const password = this.loginForm.get('password');
    if (password?.touched && password?.errors) {
      if (password.errors['required']) return 'Password is required';
      if (password.errors['minlength']) return 'Password must be at least 6 characters';
    }
    return '';
  }
}
