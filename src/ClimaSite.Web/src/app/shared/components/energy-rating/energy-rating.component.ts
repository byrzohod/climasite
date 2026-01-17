import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export type EnergyRatingLevel = 'A+++' | 'A++' | 'A+' | 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G';

@Component({
  selector: 'app-energy-rating',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="energy-rating" [attr.data-rating]="rating()" data-testid="energy-rating">
      <div class="rating-scale">
        @for (level of levels; track level) {
          <div class="level"
               [class.active]="level === rating()"
               [style.background-color]="getColor(level)"
               [style.color]="getTextColor(level)"
               [style.width]="getWidth(level)">
            <span class="level-label">{{ level }}</span>
            @if (level === rating()) {
              <span class="indicator" [style.background-color]="getTextColor(level)" [style.color]="getColor(level)">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M10 17l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
                </svg>
              </span>
            }
          </div>
        }
      </div>
      <div class="rating-info">
        <span class="label">{{ label() | translate }}</span>
        <span class="value" [style.background-color]="getColor(rating())" [style.color]="getTextColor(rating())">
          {{ rating() }}
        </span>
      </div>
    </div>
  `,
  styles: [`
    .energy-rating {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      padding: 1rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
    }

    .rating-scale {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .level {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.25rem 0.5rem;
      border-radius: 2px;
      font-size: 0.75rem;
      font-weight: 600;
      opacity: 0.6;
      transition: all 0.2s;

      &.active {
        opacity: 1;
        transform: scaleX(1.02);
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
      }
    }

    .level-label {
      min-width: 35px;
    }

    .indicator {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 18px;
      height: 18px;
      border-radius: 50%;

      svg {
        width: 14px;
        height: 14px;
        color: inherit;
      }
    }

    .rating-info {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-top: 0.5rem;
      border-top: 1px solid var(--color-border);
    }

    .label {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .value {
      padding: 0.25rem 0.75rem;
      border-radius: 4px;
      font-weight: 700;
      font-size: 1rem;
    }

    @media (max-width: 768px) {
      .energy-rating {
        padding: 0.75rem;
      }

      .level {
        font-size: 0.625rem;
        padding: 0.2rem 0.4rem;
      }
    }
  `]
})
export class EnergyRatingComponent {
  rating = input.required<EnergyRatingLevel>();
  label = input<string>('products.energyRating');

  levels: EnergyRatingLevel[] = ['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D', 'E', 'F', 'G'];

  private readonly colors: Record<EnergyRatingLevel, string> = {
    'A+++': '#00A651',
    'A++': '#50B848',
    'A+': '#B6D433',
    'A': '#FEF200',
    'B': '#FBBA00',
    'C': '#F37021',
    'D': '#ED1C24',
    'E': '#E30613',
    'F': '#C7132A',
    'G': '#A11131'
  };

  // Width percentages for the energy label arrow effect
  private readonly widths: Record<EnergyRatingLevel, string> = {
    'A+++': '40%',
    'A++': '50%',
    'A+': '60%',
    'A': '70%',
    'B': '75%',
    'C': '80%',
    'D': '85%',
    'E': '90%',
    'F': '95%',
    'G': '100%'
  };

  getColor(level: EnergyRatingLevel): string {
    return this.colors[level] || '#999';
  }

  getWidth(level: EnergyRatingLevel): string {
    return this.widths[level] || '100%';
  }

  // Levels with light backgrounds that need dark text for WCAG contrast
  private readonly lightBackgrounds: EnergyRatingLevel[] = ['A+', 'A', 'B', 'C'];

  getTextColor(level: EnergyRatingLevel): string {
    return this.lightBackgrounds.includes(level) ? '#1a1a1a' : '#ffffff';
  }

  // Get the rating index for comparison
  ratingIndex = computed(() => {
    return this.levels.indexOf(this.rating());
  });
}
