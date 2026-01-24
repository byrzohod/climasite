import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ParallaxDirective } from '../../shared/directives/parallax.directive';

interface ResourceCategory {
  id: string;
  icon: string;
  titleKey: string;
  descriptionKey: string;
  resources: Resource[];
}

interface Resource {
  id: string;
  titleKey: string;
  descriptionKey: string;
  type: 'pdf' | 'video' | 'article' | 'link';
  url?: string;
}

@Component({
  selector: 'app-resources',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, ParallaxDirective],
  template: `
    <div class="resources-page">
<!-- Hero Section with Parallax -->
      <div class="hero-section">
        <div class="hero-bg" appParallax [speed]="0.15" [direction]="'down'" [scaleOnScroll]="1.05"></div>
        <div class="hero-content" appParallax [speed]="0.08" [direction]="'up'">
          <h1>{{ 'resources.title' | translate }}</h1>
          <p>{{ 'resources.subtitle' | translate }}</p>
        </div>
      </div>

      <!-- Resource Categories -->
      <div class="categories-section">
        @for (category of categories(); track category.id) {
          <div class="category-card">
            <div class="category-header">
              <span class="category-icon">{{ category.icon }}</span>
              <h2>{{ category.titleKey | translate }}</h2>
              <p>{{ category.descriptionKey | translate }}</p>
            </div>
            <div class="resources-list">
              @for (resource of category.resources; track resource.id) {
                <a [href]="resource.url || '#'" class="resource-item" [class.no-link]="!resource.url">
                  <span class="resource-type-icon">{{ getTypeIcon(resource.type) }}</span>
                  <div class="resource-info">
                    <h3>{{ resource.titleKey | translate }}</h3>
                    <p>{{ resource.descriptionKey | translate }}</p>
                  </div>
                  <span class="resource-type-badge">{{ ('resources.types.' + resource.type) | translate }}</span>
                </a>
              }
            </div>
          </div>
        }
      </div>

      <!-- FAQ Section -->
      <div class="faq-section">
        <h2>{{ 'resources.faq.title' | translate }}</h2>
        <p class="faq-subtitle">{{ 'resources.faq.subtitle' | translate }}</p>

        <div class="faq-list">
          @for (faq of faqs(); track faq.questionKey; let i = $index) {
            <div class="faq-item" [class.expanded]="expandedFaq() === i">
              <button class="faq-question" (click)="toggleFaq(i)">
                <span>{{ faq.questionKey | translate }}</span>
                <span class="toggle-icon">{{ expandedFaq() === i ? '−' : '+' }}</span>
              </button>
              @if (expandedFaq() === i) {
                <div class="faq-answer">
                  <p>{{ faq.answerKey | translate }}</p>
                </div>
              }
            </div>
          }
        </div>
      </div>

      <!-- Contact CTA -->
      <div class="contact-cta">
        <h2>{{ 'resources.cta.title' | translate }}</h2>
        <p>{{ 'resources.cta.description' | translate }}</p>
        <a routerLink="/contact" class="btn-contact">{{ 'resources.cta.button' | translate }}</a>
      </div>
    </div>
  `,
  styles: [`
    .resources-page {
      max-width: 1200px;
      margin: 0 auto;
    }

.hero-section {
      position: relative;
      padding: 4rem 2rem;
      text-align: center;
      border-radius: 0 0 24px 24px;
      margin-bottom: 3rem;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      inset: -20%;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      z-index: 0;
    }

    .hero-content {
      position: relative;
      z-index: 1;
      max-width: 600px;
      margin: 0 auto;
      color: white;

      h1 {
        font-size: 2.5rem;
        font-weight: 700;
        margin: 0 0 1rem;
      }

      p {
        font-size: 1.125rem;
        opacity: 0.9;
        margin: 0;
      }
    }

    .categories-section {
      padding: 0 2rem;
      display: grid;
      gap: 2rem;
    }

    .category-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 16px;
      overflow: hidden;
    }

    .category-header {
      padding: 2rem;
      border-bottom: 1px solid var(--color-border);

      .category-icon {
        display: inline-block;
        font-size: 2rem;
        margin-bottom: 1rem;
      }

      h2 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .resources-list {
      padding: 1rem;
    }

    .resource-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border-radius: 8px;
      text-decoration: none;
      color: inherit;
      transition: all 0.2s;

      &:hover:not(.no-link) {
        background: var(--color-bg-secondary);
      }

      &.no-link {
        cursor: default;
      }
    }

    .resource-type-icon {
      font-size: 1.5rem;
      width: 40px;
      text-align: center;
    }

    .resource-info {
      flex: 1;

      h3 {
        font-size: 1rem;
        font-weight: 500;
        color: var(--color-text-primary);
        margin: 0 0 0.25rem;
      }

      p {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .resource-type-badge {
      padding: 0.25rem 0.75rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-secondary);
      font-size: 0.75rem;
      font-weight: 500;
      border-radius: 20px;
      text-transform: uppercase;
    }

    .faq-section {
      padding: 4rem 2rem;
      text-align: center;

      h2 {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .faq-subtitle {
        color: var(--color-text-secondary);
        margin: 0 0 2rem;
      }
    }

    .faq-list {
      max-width: 800px;
      margin: 0 auto;
      text-align: left;
    }

    .faq-item {
      border: 1px solid var(--color-border);
      border-radius: 8px;
      margin-bottom: 1rem;
      overflow: hidden;

      &.expanded {
        border-color: var(--color-primary);
      }
    }

    .faq-question {
      width: 100%;
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.5rem;
      background: var(--color-bg-primary);
      border: none;
      cursor: pointer;
      font-size: 1rem;
      font-weight: 500;
      color: var(--color-text-primary);
      text-align: left;
      transition: background 0.2s;

      &:hover {
        background: var(--color-bg-secondary);
      }

      .toggle-icon {
        font-size: 1.5rem;
        font-weight: 300;
        color: var(--color-primary);
      }
    }

    .faq-answer {
      padding: 1rem 1.5rem;
      background: var(--color-bg-secondary);
      border-top: 1px solid var(--color-border);

      p {
        color: var(--color-text-secondary);
        line-height: 1.6;
        margin: 0;
      }
    }

    .contact-cta {
      background: var(--color-bg-secondary);
      padding: 4rem 2rem;
      text-align: center;
      border-radius: 24px;
      margin: 3rem 2rem;

      h2 {
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 1rem;
      }

      p {
        color: var(--color-text-secondary);
        margin: 0 0 2rem;
        max-width: 500px;
        margin-left: auto;
        margin-right: auto;
      }

      .btn-contact {
        display: inline-block;
        padding: 1rem 2rem;
        background: var(--color-primary);
        color: white;
        border-radius: 8px;
        text-decoration: none;
        font-weight: 600;
        transition: all 0.2s;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    @media (max-width: 768px) {
      .hero-content h1 {
        font-size: 1.75rem;
      }

      .category-header {
        padding: 1.5rem;

        h2 {
          font-size: 1.25rem;
        }
      }

      .resource-item {
        flex-wrap: wrap;

        .resource-info {
          flex-basis: calc(100% - 56px);
        }

        .resource-type-badge {
          margin-left: 56px;
          margin-top: 0.5rem;
        }
      }
    }
  `]
})
export class ResourcesComponent {
  expandedFaq = signal<number | null>(null);

  categories = signal<ResourceCategory[]>([
    {
      id: 'installation',
      icon: '🔧',
      titleKey: 'resources.categories.installation.title',
      descriptionKey: 'resources.categories.installation.description',
      resources: [
        {
          id: 'ac-install',
          titleKey: 'resources.categories.installation.items.acInstall.title',
          descriptionKey: 'resources.categories.installation.items.acInstall.description',
          type: 'pdf'
        },
        {
          id: 'heat-pump',
          titleKey: 'resources.categories.installation.items.heatPump.title',
          descriptionKey: 'resources.categories.installation.items.heatPump.description',
          type: 'pdf'
        },
        {
          id: 'duct-install',
          titleKey: 'resources.categories.installation.items.ductInstall.title',
          descriptionKey: 'resources.categories.installation.items.ductInstall.description',
          type: 'video'
        }
      ]
    },
    {
      id: 'maintenance',
      icon: '🛠️',
      titleKey: 'resources.categories.maintenance.title',
      descriptionKey: 'resources.categories.maintenance.description',
      resources: [
        {
          id: 'seasonal',
          titleKey: 'resources.categories.maintenance.items.seasonal.title',
          descriptionKey: 'resources.categories.maintenance.items.seasonal.description',
          type: 'article'
        },
        {
          id: 'filter',
          titleKey: 'resources.categories.maintenance.items.filter.title',
          descriptionKey: 'resources.categories.maintenance.items.filter.description',
          type: 'article'
        },
        {
          id: 'troubleshoot',
          titleKey: 'resources.categories.maintenance.items.troubleshoot.title',
          descriptionKey: 'resources.categories.maintenance.items.troubleshoot.description',
          type: 'pdf'
        }
      ]
    },
    {
      id: 'energy',
      icon: '⚡',
      titleKey: 'resources.categories.energy.title',
      descriptionKey: 'resources.categories.energy.description',
      resources: [
        {
          id: 'efficiency',
          titleKey: 'resources.categories.energy.items.efficiency.title',
          descriptionKey: 'resources.categories.energy.items.efficiency.description',
          type: 'article'
        },
        {
          id: 'calculator',
          titleKey: 'resources.categories.energy.items.calculator.title',
          descriptionKey: 'resources.categories.energy.items.calculator.description',
          type: 'link'
        },
        {
          id: 'rebates',
          titleKey: 'resources.categories.energy.items.rebates.title',
          descriptionKey: 'resources.categories.energy.items.rebates.description',
          type: 'article'
        }
      ]
    }
  ]);

  faqs = signal([
    {
      questionKey: 'resources.faq.items.sizing.question',
      answerKey: 'resources.faq.items.sizing.answer'
    },
    {
      questionKey: 'resources.faq.items.maintenance.question',
      answerKey: 'resources.faq.items.maintenance.answer'
    },
    {
      questionKey: 'resources.faq.items.efficiency.question',
      answerKey: 'resources.faq.items.efficiency.answer'
    },
    {
      questionKey: 'resources.faq.items.warranty.question',
      answerKey: 'resources.faq.items.warranty.answer'
    },
    {
      questionKey: 'resources.faq.items.installation.question',
      answerKey: 'resources.faq.items.installation.answer'
    }
  ]);

  getTypeIcon(type: string): string {
    const icons: Record<string, string> = {
      'pdf': '📄',
      'video': '🎬',
      'article': '📰',
      'link': '🔗'
    };
    return icons[type] || '📄';
  }

  toggleFaq(index: number): void {
    if (this.expandedFaq() === index) {
      this.expandedFaq.set(null);
    } else {
      this.expandedFaq.set(index);
    }
  }
}
