import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { InputComponent } from '../../../shared/components/input/input.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule, InputComponent, ButtonComponent],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <h1>{{ 'auth.forgotPassword.title' | translate }}</h1>
          <p>{{ 'auth.forgotPassword.subtitle' | translate }}</p>
        </div>

        @if (successMessage()) {
          <div class="success-alert">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="error-alert">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form">
          <app-input
            formControlName="email"
            type="email"
            [label]="'auth.forgotPassword.email' | translate"
            [placeholder]="'auth.forgotPassword.email' | translate"
          />

          <app-button type="submit" variant="primary" [fullWidth]="true" [loading]="isLoading()" [disabled]="form.invalid || isLoading()">
            {{ 'auth.forgotPassword.submit' | translate }}
          </app-button>
        </form>

        <div class="auth-footer">
          <a routerLink="/login">{{ 'auth.forgotPassword.backToLogin' | translate }}</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 2rem; background-color: var(--color-bg-secondary); }
    .auth-card { width: 100%; max-width: 420px; background: var(--color-bg-primary); border-radius: 12px; padding: 2.5rem; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); }
    .auth-header { text-align: center; margin-bottom: 2rem; h1 { font-size: 1.75rem; font-weight: 700; color: var(--color-text-primary); margin-bottom: 0.5rem; } p { color: var(--color-text-secondary); } }
    .error-alert { background-color: var(--color-error-bg); color: var(--color-error); padding: 0.75rem 1rem; border-radius: 6px; margin-bottom: 1.5rem; font-size: 0.875rem; }
    .success-alert { background-color: var(--color-success-bg); color: var(--color-success); padding: 0.75rem 1rem; border-radius: 6px; margin-bottom: 1.5rem; font-size: 0.875rem; }
    .auth-form { display: flex; flex-direction: column; gap: 1.25rem; }
    .auth-footer { text-align: center; margin-top: 2rem; padding-top: 1.5rem; border-top: 1px solid var(--color-border); a { color: var(--color-primary); text-decoration: none; font-weight: 500; &:hover { text-decoration: underline; } } }
  `]
})
export class ForgotPasswordComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly isLoading = signal(false);

  form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.successMessage.set('If the email exists, a password reset link has been sent.');
      },
      error: () => {
        this.isLoading.set(false);
        this.successMessage.set('If the email exists, a password reset link has been sent.');
      }
    });
  }
}
