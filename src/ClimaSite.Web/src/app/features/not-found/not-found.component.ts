import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="not-found-container">
      <div class="not-found-content">
        <h1>404</h1>
        <h2>{{ 'errors.notFound' | translate }}</h2>
        <p>The page you're looking for doesn't exist or has been moved.</p>
        <a routerLink="/" class="home-button">{{ 'nav.home' | translate }}</a>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      min-height: 60vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }

    .not-found-content {
      text-align: center;

      h1 {
        font-size: 8rem;
        font-weight: 700;
        color: var(--color-primary);
        margin-bottom: 1rem;
        line-height: 1;
      }

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 2rem;
      }
    }

    .home-button {
      display: inline-block;
      background: var(--color-primary);
      color: white;
      padding: 0.75rem 2rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      transition: background 0.2s;

      &:hover {
        background: var(--color-primary-dark);
      }
    }
  `]
})
export class NotFoundComponent {}
