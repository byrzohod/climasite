import { Component, inject, signal, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService, User, UpdateProfileRequest } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TranslateModule],
  template: `
    <div class="profile-container" data-testid="profile-page">
      <h1>{{ 'account.profile.title' | translate }}</h1>

      <!-- Personal Information Section -->
      <section class="profile-section">
        <h2>{{ 'account.profile.personalInfo' | translate }}</h2>

        @if (profileSuccess()) {
          <div class="success-message" data-testid="profile-success">
            {{ 'profile.updateSuccess' | translate }}
          </div>
        }

        @if (profileError()) {
          <div class="error-message" data-testid="profile-error">
            {{ profileError() }}
          </div>
        }

        <form [formGroup]="profileForm" (ngSubmit)="updateProfile()" data-testid="profile-form">
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">{{ 'auth.register.firstName' | translate }}</label>
              <input
                type="text"
                id="firstName"
                formControlName="firstName"
                data-testid="profile-firstName"
                [class.invalid]="profileForm.get('firstName')?.invalid && profileForm.get('firstName')?.touched"
              />
            </div>

            <div class="form-group">
              <label for="lastName">{{ 'auth.register.lastName' | translate }}</label>
              <input
                type="text"
                id="lastName"
                formControlName="lastName"
                data-testid="profile-lastName"
                [class.invalid]="profileForm.get('lastName')?.invalid && profileForm.get('lastName')?.touched"
              />
            </div>
          </div>

          <div class="form-group">
            <label for="email">{{ 'auth.register.email' | translate }}</label>
            <input
              type="email"
              id="email"
              [value]="user()?.email || ''"
              disabled
              class="disabled-input"
            />
            <span class="hint">{{ 'profile.emailHint' | translate }}</span>
          </div>

          <!-- TODO: Consider adding phone validation pattern if stricter validation is required -->
          <div class="form-group">
            <label for="phone">{{ 'profile.phone' | translate }}</label>
            <input
              type="tel"
              id="phone"
              formControlName="phone"
              data-testid="profile-phone"
              [placeholder]="'profile.phonePlaceholder' | translate"
            />
          </div>

          <button
            type="submit"
            class="btn-primary"
            [disabled]="profileForm.invalid || isUpdatingProfile()"
            data-testid="profile-submit"
          >
            @if (isUpdatingProfile()) {
              {{ 'common.loading' | translate }}
            } @else {
              {{ 'common.save' | translate }}
            }
          </button>
        </form>
      </section>

      <!-- Preferences Section -->
      <section class="profile-section">
        <h2>{{ 'account.profile.preferences' | translate }}</h2>

        @if (preferencesSuccess()) {
          <div class="success-message" data-testid="preferences-success">
            {{ 'profile.preferencesSuccess' | translate }}
          </div>
        }

        @if (preferencesError()) {
          <div class="error-message" data-testid="preferences-error">
            {{ preferencesError() }}
          </div>
        }

        <form [formGroup]="preferencesForm" (ngSubmit)="updatePreferences()">
          <div class="form-row">
            <div class="form-group">
              <label for="language">{{ 'account.profile.language' | translate }}</label>
              <select
                id="language"
                formControlName="preferredLanguage"
                data-testid="profile-language"
              >
                <option value="en">English</option>
                <option value="bg">Български</option>
                <option value="de">Deutsch</option>
              </select>
            </div>

            <!-- Currency labels use universal symbols (€, лв, $) - no translation needed -->
            <div class="form-group">
              <label for="currency">{{ 'account.profile.currency' | translate }}</label>
              <select
                id="currency"
                formControlName="preferredCurrency"
                data-testid="profile-currency"
              >
                <option value="EUR">EUR (€)</option>
                <option value="BGN">BGN (лв)</option>
                <option value="USD">USD ($)</option>
              </select>
            </div>
          </div>

          <button
            type="submit"
            class="btn-primary"
            [disabled]="preferencesForm.invalid || isUpdatingPreferences()"
            data-testid="preferences-submit"
          >
            @if (isUpdatingPreferences()) {
              {{ 'common.loading' | translate }}
            } @else {
              {{ 'common.save' | translate }}
            }
          </button>
        </form>
      </section>

      <!-- Change Password Section -->
      <section class="profile-section">
        <h2>{{ 'account.profile.changePassword' | translate }}</h2>

        @if (passwordSuccess()) {
          <div class="success-message" data-testid="password-success">
            {{ 'profile.passwordSuccess' | translate }}
          </div>
        }

        @if (passwordError()) {
          <div class="error-message" data-testid="password-error">
            {{ passwordError() }}
          </div>
        }

        <form [formGroup]="passwordForm" (ngSubmit)="changePassword()" data-testid="password-form">
          <div class="form-group">
            <label for="currentPassword">{{ 'profile.currentPassword' | translate }}</label>
            <input
              type="password"
              id="currentPassword"
              formControlName="currentPassword"
              data-testid="current-password"
              [class.invalid]="passwordForm.get('currentPassword')?.invalid && passwordForm.get('currentPassword')?.touched"
            />
          </div>

          <div class="form-group">
            <label for="newPassword">{{ 'profile.newPassword' | translate }}</label>
            <input
              type="password"
              id="newPassword"
              formControlName="newPassword"
              data-testid="new-password"
              [class.invalid]="passwordForm.get('newPassword')?.invalid && passwordForm.get('newPassword')?.touched"
            />
            @if (passwordForm.get('newPassword')?.errors?.['minlength'] && passwordForm.get('newPassword')?.touched) {
              <span class="error">{{ 'errors.minLength' | translate:{ min: 8 } }}</span>
            }
          </div>

          <div class="form-group">
            <label for="confirmPassword">{{ 'auth.register.confirmPassword' | translate }}</label>
            <input
              type="password"
              id="confirmPassword"
              formControlName="confirmPassword"
              data-testid="confirm-password"
              [class.invalid]="passwordForm.get('confirmPassword')?.invalid && passwordForm.get('confirmPassword')?.touched"
            />
            @if (passwordForm.errors?.['passwordMismatch'] && passwordForm.get('confirmPassword')?.touched) {
              <span class="error">{{ 'errors.passwordMatch' | translate }}</span>
            }
          </div>

          <button
            type="submit"
            class="btn-primary"
            [disabled]="passwordForm.invalid || isChangingPassword()"
            data-testid="password-submit"
          >
            @if (isChangingPassword()) {
              {{ 'common.loading' | translate }}
            } @else {
              {{ 'profile.updatePassword' | translate }}
            }
          </button>
        </form>
      </section>
    </div>
  `,
  styles: [`
    .profile-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .profile-section {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 2rem;

      h2 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 1.5rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid var(--color-border);
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
      margin-bottom: 1.25rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        color: var(--color-text-primary);
        font-weight: 500;
        font-size: 0.875rem;
      }

      input, select {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        font-size: 1rem;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        transition: border-color 0.2s, box-shadow 0.2s;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
          box-shadow: 0 0 0 3px var(--color-primary-light);
        }

        &.invalid {
          border-color: var(--color-error);
        }

        &.disabled-input {
          background: var(--color-bg-tertiary);
          color: var(--color-text-secondary);
          cursor: not-allowed;
        }
      }

      .hint {
        display: block;
        margin-top: 0.25rem;
        font-size: 0.75rem;
        color: var(--color-text-secondary);
      }

      .error {
        display: block;
        color: var(--color-error);
        font-size: 0.75rem;
        margin-top: 0.25rem;
      }
    }

    .btn-primary {
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
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
      background: var(--color-success-bg);
      color: var(--color-success);
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }

    .error-message {
      background: var(--color-error-bg);
      color: var(--color-error);
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
    }
  `]
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly translateService = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  user = this.authService.user;

  profileForm: FormGroup;
  preferencesForm: FormGroup;
  passwordForm: FormGroup;

  isUpdatingProfile = signal(false);
  isUpdatingPreferences = signal(false);
  isChangingPassword = signal(false);

  profileSuccess = signal(false);
  profileError = signal<string | null>(null);
  preferencesSuccess = signal(false);
  preferencesError = signal<string | null>(null);
  passwordSuccess = signal(false);
  passwordError = signal<string | null>(null);

  constructor() {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phone: ['']
    });

    this.preferencesForm = this.fb.group({
      preferredLanguage: ['en'],
      preferredCurrency: ['EUR']
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    // Watch for user changes and update forms
    effect(() => {
      const user = this.user();
      if (user) {
        this.profileForm.patchValue({
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          phone: user.phone || ''
        });

        this.preferencesForm.patchValue({
          preferredLanguage: user.preferredLanguage || 'en',
          preferredCurrency: user.preferredCurrency || 'EUR'
        });
      }
    });
  }

  ngOnInit(): void {
    // Forms are now updated via effect when user data changes
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (newPassword && confirmPassword && newPassword.value !== confirmPassword.value) {
      return { passwordMismatch: true };
    }
    return null;
  }

  updateProfile(): void {
    if (this.profileForm.invalid) return;

    this.isUpdatingProfile.set(true);
    this.profileSuccess.set(false);
    this.profileError.set(null);

    const data: UpdateProfileRequest = {
      firstName: this.profileForm.value.firstName,
      lastName: this.profileForm.value.lastName,
      phone: this.profileForm.value.phone || undefined
    };

    this.authService.updateProfile(data).subscribe({
      next: () => {
        this.isUpdatingProfile.set(false);
        this.profileSuccess.set(true);
        setTimeout(() => this.profileSuccess.set(false), 3000);
      },
      error: (err) => {
        this.isUpdatingProfile.set(false);
        this.profileError.set(err.error?.message || 'Failed to update profile');
      }
    });
  }

  updatePreferences(): void {
    if (this.preferencesForm.invalid) return;

    this.isUpdatingPreferences.set(true);

    const data: UpdateProfileRequest = {
      preferredLanguage: this.preferencesForm.value.preferredLanguage,
      preferredCurrency: this.preferencesForm.value.preferredCurrency
    };

    this.authService.updateProfile(data).subscribe({
      next: () => {
        this.isUpdatingPreferences.set(false);
        this.preferencesSuccess.set(true);
        this.preferencesError.set(null);
        // Apply language change immediately
        this.translateService.use(data.preferredLanguage!);
        setTimeout(() => this.preferencesSuccess.set(false), 3000);
      },
      error: (err) => {
        this.isUpdatingPreferences.set(false);
        this.preferencesSuccess.set(false);
        this.preferencesError.set(err.error?.message || 'Failed to update preferences');
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;

    this.isChangingPassword.set(true);
    this.passwordSuccess.set(false);
    this.passwordError.set(null);

    this.authService.changePassword(
      this.passwordForm.value.currentPassword,
      this.passwordForm.value.newPassword
    ).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.passwordSuccess.set(true);
        this.passwordForm.reset();
        setTimeout(() => this.passwordSuccess.set(false), 3000);
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        this.passwordError.set(err.error?.message || 'Failed to change password');
      }
    });
  }
}
