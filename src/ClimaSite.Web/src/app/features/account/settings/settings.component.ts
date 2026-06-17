import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ReduceMotionToggleComponent } from '../../../shared/components/reduce-motion-toggle/reduce-motion-toggle.component';

/**
 * SettingsComponent - Account settings, including accessibility preferences.
 *
 * Currently surfaces the reduced-motion control so users can override the
 * operating-system motion preference. Other account-level preferences can be
 * added here as additional sections. All copy is rendered by the embedded
 * toggle (label + description), so this page owns no extra translation keys.
 */
@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [TranslateModule, ReduceMotionToggleComponent],
  template: `
    <div class="settings-container" data-testid="settings-page">
      <h1>{{ 'accessibility.reduceMotion.label' | translate }}</h1>
      <app-reduce-motion-toggle />
    </div>
  `,
  styles: [`
    .settings-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 1.5rem;
        color: var(--color-text-primary);
      }
    }
  `]
})
export class SettingsComponent {}
