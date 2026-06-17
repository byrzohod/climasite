import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AnimationService, MotionPreference } from '../../../core/services/animation.service';

/**
 * ReduceMotionToggleComponent - Accessibility control for reduced motion.
 *
 * Exposes the three motion preferences (System / On / Off) backed by
 * {@link AnimationService}. The selection persists to localStorage and toggles
 * the global `reduce-motion` class on <html>, so the user can force-disable (or
 * re-enable) animations regardless of their operating-system setting.
 *
 * Rendered as a labelled radiogroup for keyboard and screen-reader support.
 */
@Component({
  selector: 'app-reduce-motion-toggle',
  standalone: true,
  imports: [TranslateModule],
  template: `
    <fieldset
      class="reduce-motion-toggle"
      data-testid="reduce-motion-toggle"
      role="radiogroup"
      [attr.aria-label]="'accessibility.reduceMotion.label' | translate"
    >
      <legend class="reduce-motion-toggle__legend">
        {{ 'accessibility.reduceMotion.label' | translate }}
      </legend>

      <p class="reduce-motion-toggle__description">
        {{ 'accessibility.reduceMotion.description' | translate }}
      </p>

      <div class="reduce-motion-toggle__options">
        @for (option of options; track option.value) {
          <label
            class="reduce-motion-toggle__option"
            [class.reduce-motion-toggle__option--active]="isSelected(option.value)"
          >
            <input
              type="radio"
              name="reduce-motion"
              class="reduce-motion-toggle__input"
              [attr.data-testid]="'reduce-motion-option-' + option.value"
              [value]="option.value"
              [checked]="isSelected(option.value)"
              (change)="select(option.value)"
            />
            <span class="reduce-motion-toggle__label">
              {{ option.labelKey | translate }}
            </span>
          </label>
        }
      </div>
    </fieldset>
  `,
  styles: [`
    .reduce-motion-toggle {
      border: 1px solid var(--color-border);
      border-radius: 0.5rem;
      padding: 1.25rem;
      margin: 0;
      background: var(--color-bg-primary);
    }

    .reduce-motion-toggle__legend {
      font-weight: 600;
      color: var(--color-text-primary);
      padding: 0 0.5rem;
    }

    .reduce-motion-toggle__description {
      margin: 0 0 1rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .reduce-motion-toggle__options {
      display: inline-flex;
      gap: 0.25rem;
      padding: 0.25rem;
      border-radius: 0.5rem;
      background: var(--color-bg-secondary);
    }

    .reduce-motion-toggle__option {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      cursor: pointer;
      color: var(--color-text-secondary);
      transition: background-color 0.2s ease, color 0.2s ease;

      &:hover {
        background: var(--color-bg-hover);
        color: var(--color-text-primary);
      }

      &--active {
        background: var(--color-primary);
        color: var(--color-text-inverse);
      }

      &:focus-within {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
    }

    .reduce-motion-toggle__input {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    .reduce-motion-toggle__label {
      font-size: 0.875rem;
      font-weight: 500;
    }
  `]
})
export class ReduceMotionToggleComponent {
  private readonly animationService = inject(AnimationService);

  protected readonly options: ReadonlyArray<{ value: MotionPreference; labelKey: string }> = [
    { value: 'system', labelKey: 'accessibility.reduceMotion.system' },
    { value: 'on', labelKey: 'accessibility.reduceMotion.on' },
    { value: 'off', labelKey: 'accessibility.reduceMotion.off' }
  ];

  protected isSelected(value: MotionPreference): boolean {
    return this.animationService.motionPreference() === value;
  }

  protected select(value: MotionPreference): void {
    this.animationService.setMotionPreference(value);
  }
}
