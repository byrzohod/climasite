import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="about-container" data-testid="about-page">
      <section class="about-header">
        <h1>{{ 'about.title' | translate }}</h1>
        <p>{{ 'about.subtitle' | translate }}</p>
      </section>

      <section class="stats-section">
        <div class="stats-grid">
          <div class="stat-item" data-testid="stat-customers">
            <span class="stat-number">10,000+</span>
            <span class="stat-label">{{ 'about.stats.customers' | translate }}</span>
          </div>
          <div class="stat-item" data-testid="stat-products">
            <span class="stat-number">500+</span>
            <span class="stat-label">{{ 'about.stats.products' | translate }}</span>
          </div>
          <div class="stat-item" data-testid="stat-years">
            <span class="stat-number">15+</span>
            <span class="stat-label">{{ 'about.stats.years' | translate }}</span>
          </div>
          <div class="stat-item" data-testid="stat-support">
            <span class="stat-number">24/7</span>
            <span class="stat-label">{{ 'about.stats.support' | translate }}</span>
          </div>
        </div>
      </section>

      <section class="story-section">
        <div class="content-card">
          <h2>{{ 'about.story.title' | translate }}</h2>
          <p>{{ 'about.story.content' | translate }}</p>
        </div>
      </section>

      <section class="mission-section">
        <div class="content-card">
          <h2>{{ 'about.mission.title' | translate }}</h2>
          <p>{{ 'about.mission.content' | translate }}</p>
        </div>
      </section>

      <section class="values-section">
        <h2>{{ 'about.values.title' | translate }}</h2>
        <div class="values-grid">
          <div class="value-card" data-testid="value-quality">
            <div class="value-icon">✨</div>
            <h3>{{ 'about.values.quality' | translate }}</h3>
            <p>{{ 'about.values.qualityDesc' | translate }}</p>
          </div>
          <div class="value-card" data-testid="value-service">
            <div class="value-icon">🤝</div>
            <h3>{{ 'about.values.service' | translate }}</h3>
            <p>{{ 'about.values.serviceDesc' | translate }}</p>
          </div>
          <div class="value-card" data-testid="value-expertise">
            <div class="value-icon">🎓</div>
            <h3>{{ 'about.values.expertise' | translate }}</h3>
            <p>{{ 'about.values.expertiseDesc' | translate }}</p>
          </div>
          <div class="value-card" data-testid="value-sustainability">
            <div class="value-icon">🌱</div>
            <h3>{{ 'about.values.sustainability' | translate }}</h3>
            <p>{{ 'about.values.sustainabilityDesc' | translate }}</p>
          </div>
        </div>
      </section>

      <section class="team-section">
        <h2>{{ 'about.team.title' | translate }}</h2>
        <p class="team-subtitle">{{ 'about.team.subtitle' | translate }}</p>
        <div class="team-grid">
          <div class="team-member">
            <div class="member-avatar">👨‍💼</div>
            <h4>Ivan Petrov</h4>
            <span>CEO & Founder</span>
          </div>
          <div class="team-member">
            <div class="member-avatar">👩‍💼</div>
            <h4>Maria Dimitrova</h4>
            <span>Head of Sales</span>
          </div>
          <div class="team-member">
            <div class="member-avatar">👨‍🔧</div>
            <h4>Georgi Ivanov</h4>
            <span>Technical Director</span>
          </div>
          <div class="team-member">
            <div class="member-avatar">👩‍💻</div>
            <h4>Elena Todorova</h4>
            <span>Customer Support Lead</span>
          </div>
        </div>
      </section>

      <section class="cta-section">
        <h2>Ready to improve your comfort?</h2>
        <p>Browse our selection of premium HVAC products.</p>
        <a routerLink="/products" class="cta-button" data-testid="about-cta">
          {{ 'common.viewAll' | translate }} {{ 'nav.products' | translate }}
        </a>
      </section>
    </div>
  `,
  styles: [`
    .about-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .about-header {
      text-align: center;
      padding: 3rem 0;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      color: white;
      margin: -2rem -2rem 3rem -2rem;
      padding: 4rem 2rem;

      h1 {
        font-size: 2.5rem;
        margin-bottom: 0.5rem;
      }

      p {
        font-size: 1.25rem;
        opacity: 0.9;
      }
    }

    .stats-section {
      margin-bottom: 3rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
    }

    .stat-item {
      text-align: center;
      padding: 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;

      .stat-number {
        display: block;
        font-size: 2.5rem;
        font-weight: 700;
        color: var(--color-primary);
        margin-bottom: 0.5rem;
      }

      .stat-label {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .story-section, .mission-section {
      margin-bottom: 3rem;
    }

    .content-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 2rem;

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
      }

      p {
        color: var(--color-text-secondary);
        line-height: 1.8;
      }
    }

    .values-section {
      margin-bottom: 3rem;

      h2 {
        text-align: center;
        font-size: 1.75rem;
        color: var(--color-text-primary);
        margin-bottom: 2rem;
      }
    }

    .values-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1.5rem;
    }

    .value-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 2rem;
      text-align: center;

      .value-icon {
        font-size: 2.5rem;
        margin-bottom: 1rem;
      }

      h3 {
        font-size: 1.125rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        line-height: 1.6;
      }
    }

    .team-section {
      margin-bottom: 3rem;
      text-align: center;

      h2 {
        font-size: 1.75rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      .team-subtitle {
        color: var(--color-text-secondary);
        margin-bottom: 2rem;
      }
    }

    .team-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
    }

    .team-member {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;
      text-align: center;

      .member-avatar {
        font-size: 3rem;
        margin-bottom: 1rem;
      }

      h4 {
        font-size: 1rem;
        color: var(--color-text-primary);
        margin-bottom: 0.25rem;
      }

      span {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .cta-section {
      text-align: center;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      padding: 3rem;

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }
    }

    .cta-button {
      display: inline-block;
      background: var(--color-primary);
      color: white;
      padding: 1rem 2rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      transition: background-color 0.2s;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    @media (max-width: 1024px) {
      .stats-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .team-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 768px) {
      .about-header h1 {
        font-size: 2rem;
      }

      .values-grid {
        grid-template-columns: 1fr;
      }

      .stats-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .stat-item .stat-number {
        font-size: 2rem;
      }
    }
  `]
})
export class AboutComponent {}
