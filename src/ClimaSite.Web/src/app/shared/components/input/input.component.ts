import { Component, input, output, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ],
  template: `
    <div class="input-wrapper">
      @if (label()) {
        <label [for]="inputId" class="input-label">
          {{ label() }}
          @if (required()) {
            <span class="required-mark">*</span>
          }
        </label>
      }
      <div class="input-container" [class.input-error]="error()" [class.input-disabled]="disabled()">
        @if (prefixIcon()) {
          <span class="input-icon input-icon-prefix">
            <ng-content select="[prefix-icon]"></ng-content>
          </span>
        }
        <input
          [id]="inputId"
          [type]="type()"
          [placeholder]="placeholder()"
          [disabled]="disabled()"
          [readonly]="readonly()"
          [attr.aria-label]="ariaLabel() || label()"
          [attr.aria-describedby]="error() ? inputId + '-error' : null"
          [attr.data-testid]="testId()"
          [(ngModel)]="value"
          (ngModelChange)="onInputChange($event)"
          (blur)="onBlur()"
          class="input-field"
          [class.has-prefix]="prefixIcon()"
          [class.has-suffix]="suffixIcon()"
        />
        @if (suffixIcon()) {
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
      transition: border-color 0.2s ease, box-shadow 0.2s ease;

      &:focus-within {
        border-color: var(--color-border-focus);
        box-shadow: 0 0 0 3px var(--color-primary-light);
      }

      &.input-error {
        border-color: var(--color-border-error);

        &:focus-within {
          box-shadow: 0 0 0 3px var(--color-error-light);
        }
      }

      &.input-disabled {
        background-color: var(--color-bg-disabled);
        cursor: not-allowed;
      }
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

      &::placeholder {
        color: var(--color-text-placeholder);
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

    .input-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-text-tertiary);
      width: 2.5rem;
      flex-shrink: 0;
    }

    .input-error-message {
      font-size: 0.75rem;
      color: var(--color-error);
      margin: 0;
    }

    .input-hint {
      font-size: 0.75rem;
      color: var(--color-text-tertiary);
      margin: 0;
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

  readonly valueChange = output<string>();
  readonly blurred = output<void>();

  protected value: string = '';
  protected readonly inputId = `input-${Math.random().toString(36).substr(2, 9)}`;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

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

  protected onBlur(): void {
    this.onTouched();
    this.blurred.emit();
  }
}
