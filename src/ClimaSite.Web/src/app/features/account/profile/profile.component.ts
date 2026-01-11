import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="profile-container">
      <h1>{{ 'account.profile.title' | translate }}</h1>
      <p>Profile management coming soon...</p>
    </div>
  `,
  styles: [`
    .profile-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 1rem;
        color: var(--color-text-primary);
      }
    }
  `]
})
export class ProfileComponent {}
