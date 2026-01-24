import { Component, input, output, forwardRef, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search';
export type InputValidationState = 'default' | 'valid' | 'invalid';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ],
  template: `
    <div class="input-wrapper" [class.floating-label]="floatingLabel()">
      @if (label() && !floatingLabel()) {
        <label [for]="inputId" class="input-label">
          {{ label() }}
          @if (required()) {
            <span class="required-mark">*</span>
          }
        </label>
      }
      <div 
        class="input-container" 
        [class.input-error]="error()" 
        [class.input-valid]="showSuccess() && !error() && value"
        [class.input-disabled]="disabled()"
        [class.input-focused]="isFocused()"
        [class.shake-animation]="shouldShake()"
      >
        @if (prefixIcon()) {
          <span class="input-icon input-icon-prefix">
            <ng-content select="[prefix-icon]"></ng-content>
          </span>
        }
        <input
          [id]="inputId"
          [type]="effectiveType()"
          [placeholder]="floatingLabel() ? ' ' : placeholder()"
          [disabled]="disabled()"
          [readonly]="readonly()"
          [attr.aria-label]="ariaLabel() || label()"
          [attr.aria-describedby]="error() ? inputId + '-error' : null"
          [attr.aria-invalid]="error() ? 'true' : null"
          [attr.data-testid]="testId()"
          [attr.autocomplete]="autocomplete() || null"
          [(ngModel)]="value"
          (ngModelChange)="onInputChange($event)"
          (focus)="onFocus()"
          (blur)="onBlur()"
          class="input-field"
          [class.has-prefix]="prefixIcon()"
          [class.has-suffix]="suffixIcon() || type() === 'password' || (showSuccess() && !error() && value)"
        />
        @if (floatingLabel() && label()) {
          <label [for]="inputId" class="input-label-floating">
            {{ label() }}
            @if (required()) {
              <span class="required-mark">*</span>
            }
          </label>
        }
        @if (showSuccess() && !error() && value) {
          <span class="input-icon input-icon-suffix success-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" class="success-checkmark">
              <polyline points="20 6 9 17 4 12"/>
            </svg>
          </span>
        } @else if (type() === 'password') {
          <button
            type="button"
            class="password-toggle"
            (click)="togglePasswordVisibility()"
            [attr.aria-label]="passwordVisible() ? ('auth.hidePassword' | translate) : ('auth.showPassword' | translate)"
            [attr.aria-pressed]="passwordVisible()"
            tabindex="-1"
            data-testid="password-toggle"
          >
            @if (passwordVisible()) {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="toggle-icon">
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                <line x1="1" y1="1" x2="23" y2="23"/>
              </svg>
            } @else {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="toggle-icon">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                <circle cx="12" cy="12" r="3"/>
              </svg>
            }
          </button>
        } @else if (suffixIcon()) {
          <span class="input-icon input-icon-suffix">
            <ng-content select="[suffix-icon]"></ng-content>
          </span>
        }
      </div>
      @if (error()) {
        <p [id]="inputId + '-error'" class="input-error-message" role="alert" data-testid="validation-error">
          {{ error() }}
        </p>
      }
      @if (hint() && !error()) {
        <p class="input-hint">{{ hint() }}</p>
      }
    </div>
  `,
  styles: [`
    .input-wrapper {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
    }

    .input-label {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
      transition: color 0.2s ease-out;
    }

    .required-mark {
      color: var(--color-error);
      margin-left: 0.125rem;
    }

    .input-container {
      position: relative;
      display: flex;
      align-items: center;
      background-color: var(--color-bg-input);
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
      transition: border-color 0.2s ease-out, box-shadow 0.2s ease-out, background-color 0.2s ease-out;

      &:hover:not(.input-disabled):not(.input-focused) {
        border-color: var(--color-border-secondary);
      }

      &.input-focused,
      &:focus-within {
        border-color: var(--color-border-focus);
        box-shadow: 0 0 0 3px var(--color-primary-light);
      }

      &.input-error {
        border-color: var(--color-border-error);

        &.input-focused,
        &:focus-within {
          box-shadow: 0 0 0 3px var(--color-error-light);
        }
      }

      &.input-valid {
        border-color: var(--color-success);

        &.input-focused,
        &:focus-within {
          box-shadow: 0 0 0 3px var(--color-success-light);
        }
      }

      &.input-disabled {
        background-color: var(--color-bg-disabled);
        cursor: not-allowed;
      }

      &.shake-animation {
        animation: inputShake 0.4s ease-out;
      }
    }

    @keyframes inputShake {
      0%, 100% { transform: translateX(0); }
      20% { transform: translateX(-6px); }
      40% { transform: translateX(6px); }
      60% { transform: translateX(-4px); }
      80% { transform: translateX(4px); }
    }

    .input-field {
      flex: 1;
      width: 100%;
      padding: 0.625rem 0.75rem;
      border: none;
      background: transparent;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      line-height: 1.25rem;
      transition: color 0.2s ease-out;

      &::placeholder {
        color: var(--color-text-placeholder);
        transition: opacity 0.2s ease-out;
      }

      &:focus {
        outline: none;
      }

      &:disabled {
        cursor: not-allowed;
        color: var(--color-text-disabled);
      }

      &.has-prefix {
        padding-left: 0.25rem;
      }

      &.has-suffix {
        padding-right: 0.25rem;
      }
    }

    /* Floating Label Styles */
    .floating-label {
      .input-container {
        padding-top: 0.5rem;
      }

      .input-field {
        padding-top: 1rem;
        padding-bottom: 0.375rem;

        &::placeholder {
          opacity: 0;
        }

        &:focus::placeholder {
          opacity: 1;
          transition-delay: 0.1s;
        }
      }

      .input-label-floating {
        position: absolute;
        left: 0.75rem;
        top: 50%;
        transform: translateY(-50%);
        font-size: 0.875rem;
        font-weight: 400;
        color: var(--color-text-placeholder);
        pointer-events: none;
        background: transparent;
        padding: 0 0.25rem;
        transition: all 0.2s ease-out;
        z-index: 1;
      }

      .input-field:focus ~ .input-label-floating,
      .input-field:not(:placeholder-shown) ~ .input-label-floating {
        top: 0.25rem;
        transform: translateY(0);
        font-size: 0.75rem;
        font-weight: 500;
        color: var(--color-primary);
      }

      .input-error .input-field:focus ~ .input-label-floating,
      .input-error .input-field:not(:placeholder-shown) ~ .input-label-floating {
        color: var(--color-error);
      }

      .input-valid .input-field:focus ~ .input-label-floating,
      .input-valid .input-field:not(:placeholder-shown) ~ .input-label-floating {
        color: var(--color-success);
      }
    }

    .input-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-text-tertiary);
      width: 2.5rem;
      flex-shrink: 0;
      transition: color 0.2s ease-out;
    }

    .success-icon {
      color: var(--color-success);
    }

    .success-checkmark {
      width: 1.25rem;
      height: 1.25rem;
      animation: successPop 0.3s ease-out forwards;
    }

    @keyframes successPop {
      0% { transform: scale(0); opacity: 0; }
      60% { transform: scale(1.2); }
      100% { transform: scale(1); opacity: 1; }
    }

    .input-error-message {
      font-size: 0.75rem;
      color: var(--color-error);
      margin: 0;
      animation: errorSlideIn 0.2s ease-out forwards;
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    @keyframes errorSlideIn {
      from {
        opacity: 0;
        transform: translateY(-5px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .input-hint {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
      margin: 0;
      transition: opacity 0.2s ease-out;
    }

    .password-toggle {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 100%;
      padding: 0;
      border: none;
      background: transparent;
      cursor: pointer;
      color: var(--color-text-tertiary);
      transition: color 0.2s ease-out, transform 0.15s ease-out;
      flex-shrink: 0;

      &:hover {
        color: var(--color-text-primary);
      }

      &:focus {
        outline: none;
        color: var(--color-primary);
      }

      &:active {
        transform: scale(0.9);
      }
    }

    .toggle-icon {
      width: 1.25rem;
      height: 1.25rem;
    }

    /* Reduced Motion */
    @media (prefers-reduced-motion: reduce) {
      .input-container,
      .input-field,
      .input-label,
      .input-label-floating,
      .input-icon,
      .input-error-message,
      .input-hint,
      .password-toggle,
      .success-checkmark {
        transition: none !important;
        animation: none !important;
      }

      .input-error {
        border-color: var(--color-error);
      }

      .input-valid {
        border-color: var(--color-success);
      }

      .success-checkmark {
        transform: scale(1);
        opacity: 1;
      }

      .input-error-message {
        opacity: 1;
        transform: translateY(0);
      }
    }
  `]
})
export class InputComponent implements ControlValueAccessor {
  readonly type = input<InputType>('text');
  readonly label = input<string>('');
  readonly placeholder = input<string>('');
  readonly error = input<string>('');
  readonly hint = input<string>('');
  readonly disabled = input<boolean>(false);
  readonly readonly = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly prefixIcon = input<boolean>(false);
  readonly suffixIcon = input<boolean>(false);
  readonly ariaLabel = input<string>('');
  readonly testId = input<string>('input');
  readonly autocomplete = input<string>('');
  readonly floatingLabel = input<boolean>(false);
  readonly showSuccess = input<boolean>(false);

  readonly valueChange = output<string>();
  readonly blurred = output<void>();
  readonly focused = output<void>();

  protected value: string = '';
  protected readonly inputId = `input-${Math.random().toString(36).substr(2, 9)}`;
  
  // Focus state
  protected readonly isFocused = signal(false);
  
  // Shake animation for errors
  protected readonly shouldShake = signal(false);
  private lastError = '';
  
  // Password visibility toggle
  protected readonly passwordVisible = signal(false);
  protected readonly effectiveType = computed(() => {
    if (this.type() === 'password' && this.passwordVisible()) {
      return 'text';
    }
    return this.type();
  });

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    // Effect to trigger shake animation when error appears
    effect(() => {
      const currentError = this.error();
      if (currentError && currentError !== this.lastError && this.lastError !== currentError) {
        this.triggerShake();
      }
      this.lastError = currentError;
    });
  }

  writeValue(value: string): void {
    this.value = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    // Handled by input signal
  }

  protected onInputChange(value: string): void {
    this.value = value;
    this.onChange(value);
    this.valueChange.emit(value);
  }

  protected onFocus(): void {
    this.isFocused.set(true);
    this.focused.emit();
  }

  protected onBlur(): void {
    this.isFocused.set(false);
    this.onTouched();
    this.blurred.emit();
  }

  protected togglePasswordVisibility(): void {
    this.passwordVisible.update(v => !v);
  }

  private triggerShake(): void {
    this.shouldShake.set(true);
    setTimeout(() => this.shouldShake.set(false), 400);
  }
}
