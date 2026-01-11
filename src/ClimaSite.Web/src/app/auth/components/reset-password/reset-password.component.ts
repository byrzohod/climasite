import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { InputComponent } from '../../../shared/components/input/input.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule, InputComponent, ButtonComponent],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <h1>{{ 'auth.resetPassword.title' | translate }}</h1>
        </div>

        @if (successMessage()) {
          <div class="success-alert">{{ successMessage() }}</div>
        }

        @if (errorMessage()) {
          <div class="error-alert">{{ errorMessage() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form">
          <app-input
            formControlName="newPassword"
            type="password"
            [label]="'auth.resetPassword.newPassword' | translate"
            [placeholder]="'auth.resetPassword.newPassword' | translate"
          />

          <app-input
            formControlName="confirmPassword"
            type="password"
            [label]="'auth.resetPassword.confirmNewPassword' | translate"
            [placeholder]="'auth.resetPassword.confirmNewPassword' | translate"
          />

          @if (form.errors?.['passwordMismatch']) {
            <div class="field-error">{{ 'errors.passwordMatch' | translate }}</div>
          }

          <app-button type="submit" variant="primary" [fullWidth]="true" [loading]="isLoading()" [disabled]="form.invalid || isLoading()">
            {{ 'auth.resetPassword.reset' | translate }}
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
    .auth-header { text-align: center; margin-bottom: 2rem; h1 { font-size: 1.75rem; font-weight: 700; color: var(--color-text-primary); } }
    .error-alert { background-color: var(--color-error-bg); color: var(--color-error); padding: 0.75rem 1rem; border-radius: 6px; margin-bottom: 1.5rem; font-size: 0.875rem; }
    .success-alert { background-color: var(--color-success-bg); color: var(--color-success); padding: 0.75rem 1rem; border-radius: 6px; margin-bottom: 1.5rem; font-size: 0.875rem; }
    .auth-form { display: flex; flex-direction: column; gap: 1.25rem; }
    .field-error { color: var(--color-error); font-size: 0.75rem; margin-top: -0.5rem; }
    .auth-footer { text-align: center; margin-top: 2rem; padding-top: 1.5rem; border-top: 1px solid var(--color-border); a { color: var(--color-primary); text-decoration: none; font-weight: 500; &:hover { text-decoration: underline; } } }
  `]
})
export class ResetPasswordComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly isLoading = signal(false);

  private token = '';
  private email = '';

  form: FormGroup = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, {
    validators: this.passwordMatchValidator
  });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParams['token'] || '';
    this.email = this.route.snapshot.queryParams['email'] || '';

    if (!this.token || !this.email) {
      this.router.navigate(['/forgot-password']);
    }
  }

  passwordMatchValidator(control: AbstractControl) {
    const password = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authService.resetPassword(this.token, this.email, this.form.value.newPassword).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.successMessage.set('Password has been reset successfully. Redirecting to login...');
        setTimeout(() => this.router.navigate(['/login']), 3000);
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.error?.message || 'Failed to reset password. Please try again.');
      }
    });
  }
}
