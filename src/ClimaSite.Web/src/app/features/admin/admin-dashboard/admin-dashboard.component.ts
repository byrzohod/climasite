import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="admin-container">
      <h1>{{ 'admin.title' | translate }}</h1>

      <div class="admin-links">
        <a routerLink="products" class="admin-link">
          <span class="icon">📦</span>
          <span>{{ 'admin.products.title' | translate }}</span>
        </a>
        <a routerLink="orders" class="admin-link">
          <span class="icon">📋</span>
          <span>{{ 'admin.orders.title' | translate }}</span>
        </a>
        <a routerLink="users" class="admin-link">
          <span class="icon">👥</span>
          <span>{{ 'admin.users.title' | translate }}</span>
        </a>
        <a routerLink="moderation" class="admin-link">
          <span class="icon">🛡️</span>
          <span>{{ 'admin.moderation.title' | translate }}</span>
        </a>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .admin-links {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .admin-link {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      text-decoration: none;
      color: var(--color-text-primary);
      transition: border-color 0.2s, box-shadow 0.2s;

      &:hover {
        border-color: var(--color-primary);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }

      .icon {
        font-size: 2.5rem;
      }
    }
  `]
})
export class AdminDashboardComponent {}
