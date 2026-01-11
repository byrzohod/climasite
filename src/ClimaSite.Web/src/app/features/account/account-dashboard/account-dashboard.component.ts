import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-account-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="account-container" data-testid="account-page">
      <h1>{{ 'account.title' | translate }}</h1>

      @if (authService.user(); as user) {
        <div class="welcome-section">
          <p>Welcome, {{ user.firstName }} {{ user.lastName }}!</p>
        </div>
      }

      <div class="account-links">
        <a routerLink="profile" class="account-link">
          <span class="icon">👤</span>
          <span>{{ 'account.profile.title' | translate }}</span>
        </a>
        <a routerLink="orders" class="account-link">
          <span class="icon">📦</span>
          <span>{{ 'account.orders.title' | translate }}</span>
        </a>
        <a routerLink="addresses" class="account-link">
          <span class="icon">📍</span>
          <span>{{ 'account.addresses.title' | translate }}</span>
        </a>
      </div>
    </div>
  `,
  styles: [`
    .account-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 1.5rem;
        color: var(--color-text-primary);
      }
    }

    .welcome-section {
      background: var(--color-bg-secondary);
      padding: 1.5rem;
      border-radius: 8px;
      margin-bottom: 2rem;

      p {
        font-size: 1.125rem;
        color: var(--color-text-primary);
      }
    }

    .account-links {
      display: grid;
      gap: 1rem;
    }

    .account-link {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.25rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      text-decoration: none;
      color: var(--color-text-primary);
      transition: border-color 0.2s, box-shadow 0.2s;

      &:hover {
        border-color: var(--color-primary);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
      }

      .icon {
        font-size: 1.5rem;
      }
    }
  `]
})
export class AccountDashboardComponent {
  readonly authService = inject(AuthService);
}
