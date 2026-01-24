import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { CountUpDirective } from '../../directives/count-up.directive';
import { AnimateOnScrollDirective } from '../../directives/animate-on-scroll.directive';

interface Stat {
  value: number;
  suffix: string;
  labelKey: string;
  icon: string;
}

@Component({
  selector: 'app-stats-counter',
  standalone: true,
  imports: [CommonModule, TranslateModule, CountUpDirective, AnimateOnScrollDirective],
  template: `
    <section class="stats-section" data-testid="stats-section">
      <div class="stats-container">
        <div class="stats-header" appAnimateOnScroll [animation]="'fade-in-up'">
          <h2>{{ 'home.stats.title' | translate }}</h2>
          <p>{{ 'home.stats.subtitle' | translate }}</p>
        </div>
        <div class="stats-grid">
          @for (stat of stats; track stat.labelKey; let i = $index) {
            <div
              class="stat-card"
              appAnimateOnScroll
              [animation]="'fade-in-up'"
              [delay]="i * 100"
            >
              <div class="stat-icon" [innerHTML]="stat.icon"></div>
              <div class="stat-value">
                <span [appCountUp]="stat.value" [suffix]="stat.suffix" [duration]="2500"></span>
              </div>
              <div class="stat-label">{{ stat.labelKey | translate }}</div>
            </div>
          }
        </div>
      </div>
    </section>
  `,
  styles: [`
    .stats-section {
      padding: 5rem 2rem;
      background: linear-gradient(135deg, var(--color-primary-950) 0%, var(--color-secondary-900) 100%);
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background:
          radial-gradient(circle at 20% 80%, rgba(59, 130, 246, 0.15) 0%, transparent 50%),
          radial-gradient(circle at 80% 20%, rgba(6, 182, 212, 0.15) 0%, transparent 50%);
        pointer-events: none;
      }
    }

    .stats-container {
      max-width: 1200px;
      margin: 0 auto;
      position: relative;
      z-index: 1;
    }

    .stats-header {
      text-align: center;
      margin-bottom: 4rem;

      h2 {
        font-size: 2.5rem;
        font-weight: 700;
        color: white;
        margin: 0 0 1rem;
      }

      p {
        font-size: 1.125rem;
        color: rgba(255, 255, 255, 0.7);
        margin: 0;
        max-width: 600px;
        margin: 0 auto;
      }
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 2rem;
    }

    .stat-card {
      text-align: center;
      padding: 2rem;
      background: rgba(255, 255, 255, 0.05);
      border-radius: 20px;
      border: 1px solid rgba(255, 255, 255, 0.1);
      backdrop-filter: blur(10px);
      transition: all 0.3s ease;

      &:hover {
        transform: translateY(-8px);
        background: rgba(255, 255, 255, 0.1);
        border-color: rgba(255, 255, 255, 0.2);
      }
    }

    .stat-icon {
      width: 64px;
      height: 64px;
      margin: 0 auto 1.5rem;
      background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-accent-500) 100%);
      border-radius: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;

      :deep(svg) {
        width: 32px;
        height: 32px;
      }
    }

    .stat-value {
      font-size: 3rem;
      font-weight: 800;
      color: white;
      line-height: 1;
      margin-bottom: 0.5rem;
    }

    .stat-label {
      font-size: 1rem;
      color: rgba(255, 255, 255, 0.7);
      font-weight: 500;
    }

    @media (max-width: 1024px) {
      .stats-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 640px) {
      .stats-section {
        padding: 3rem 1rem;
      }

      .stats-header h2 {
        font-size: 1.75rem;
      }

      .stats-grid {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .stat-card {
        padding: 1.5rem;
      }

      .stat-value {
        font-size: 2.5rem;
      }
    }
  `]
})
export class StatsCounterComponent {
  stats: Stat[] = [
    {
      value: 10000,
      suffix: '+',
      labelKey: 'home.stats.customers',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path d="M4.5 6.375a4.125 4.125 0 118.25 0 4.125 4.125 0 01-8.25 0zM14.25 8.625a3.375 3.375 0 116.75 0 3.375 3.375 0 01-6.75 0zM1.5 19.125a7.125 7.125 0 0114.25 0v.003l-.001.119a.75.75 0 01-.363.63 13.067 13.067 0 01-6.761 1.873c-2.472 0-4.786-.684-6.76-1.873a.75.75 0 01-.364-.63l-.001-.122zM17.25 19.128l-.001.144a2.25 2.25 0 01-.233.96 10.088 10.088 0 005.06-1.01.75.75 0 00.42-.643 4.875 4.875 0 00-6.957-4.611 8.586 8.586 0 011.71 5.157v.003z"/>
      </svg>`
    },
    {
      value: 500,
      suffix: '+',
      labelKey: 'home.stats.products',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path d="M11.644 1.59a.75.75 0 01.712 0l9.75 5.25a.75.75 0 010 1.32l-9.75 5.25a.75.75 0 01-.712 0l-9.75-5.25a.75.75 0 010-1.32l9.75-5.25z"/>
        <path d="M3.265 10.602l7.668 4.129a2.25 2.25 0 002.134 0l7.668-4.13 1.37.739a.75.75 0 010 1.32l-9.75 5.25a.75.75 0 01-.71 0l-9.75-5.25a.75.75 0 010-1.32l1.37-.738z"/>
        <path d="M10.933 19.231l-7.668-4.13-1.37.739a.75.75 0 000 1.32l9.75 5.25c.221.12.489.12.71 0l9.75-5.25a.75.75 0 000-1.32l-1.37-.738-7.668 4.13a2.25 2.25 0 01-2.134-.001z"/>
      </svg>`
    },
    {
      value: 15,
      suffix: '+',
      labelKey: 'home.stats.experience',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M12.516 2.17a.75.75 0 00-1.032 0 11.209 11.209 0 01-7.877 3.08.75.75 0 00-.722.515A12.74 12.74 0 002.25 9.75c0 5.942 4.064 10.933 9.563 12.348a.749.749 0 00.374 0c5.499-1.415 9.563-6.406 9.563-12.348 0-1.39-.223-2.73-.635-3.985a.75.75 0 00-.722-.516 11.209 11.209 0 01-7.877-3.08z" clip-rule="evenodd"/>
      </svg>`
    },
    {
      value: 4.8,
      suffix: '/5',
      labelKey: 'home.stats.rating',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.007 5.404.433c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.433 2.082-5.006z" clip-rule="evenodd"/>
      </svg>`
    }
  ];
}
