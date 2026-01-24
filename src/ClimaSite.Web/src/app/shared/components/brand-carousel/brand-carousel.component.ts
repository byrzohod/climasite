import { Component, OnInit, OnDestroy, PLATFORM_ID, inject, signal } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

interface Brand {
  name: string;
  logo: string;
}

@Component({
  selector: 'app-brand-carousel',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <section class="brand-carousel-section" data-testid="brand-carousel">
      <div class="brand-carousel-header">
        <p class="brand-label">{{ 'home.brands.trustedBy' | translate }}</p>
      </div>
      <div class="brand-carousel-wrapper">
        <div class="brand-carousel-track" [class.paused]="isPaused()">
          @for (brand of duplicatedBrands; track $index) {
            <div class="brand-item" (mouseenter)="pause()" (mouseleave)="resume()">
              <span class="brand-name">{{ brand.name }}</span>
            </div>
          }
        </div>
      </div>
    </section>
  `,
  styles: [`
    .brand-carousel-section {
      padding: 3rem 0;
      background: var(--color-bg-secondary);
      overflow: hidden;
    }

    .brand-carousel-header {
      text-align: center;
      margin-bottom: 2rem;
    }

    .brand-label {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      font-weight: 500;
      margin: 0;
    }

    .brand-carousel-wrapper {
      width: 100%;
      overflow: hidden;
      mask-image: linear-gradient(
        to right,
        transparent,
        black 10%,
        black 90%,
        transparent
      );
      -webkit-mask-image: linear-gradient(
        to right,
        transparent,
        black 10%,
        black 90%,
        transparent
      );
    }

    .brand-carousel-track {
      display: flex;
      animation: scroll 30s linear infinite;
      width: fit-content;

      &.paused {
        animation-play-state: paused;
      }
    }

    @keyframes scroll {
      0% {
        transform: translateX(0);
      }
      100% {
        transform: translateX(-50%);
      }
    }

    .brand-item {
      flex-shrink: 0;
      padding: 1rem 3rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.3s ease;
    }

    .brand-name {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--color-text-tertiary);
      white-space: nowrap;
      transition: all 0.3s ease;
      opacity: 0.6;

      &:hover {
        color: var(--color-text-primary);
        opacity: 1;
      }
    }

    @media (max-width: 768px) {
      .brand-carousel-section {
        padding: 2rem 0;
      }

      .brand-item {
        padding: 0.75rem 2rem;
      }

      .brand-name {
        font-size: 1rem;
      }
    }
  `]
})
export class BrandCarouselComponent implements OnInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);

  isPaused = signal(false);

  brands: Brand[] = [
    { name: 'Daikin', logo: '' },
    { name: 'Mitsubishi Electric', logo: '' },
    { name: 'Samsung', logo: '' },
    { name: 'LG', logo: '' },
    { name: 'Panasonic', logo: '' },
    { name: 'Toshiba', logo: '' },
    { name: 'Fujitsu', logo: '' },
    { name: 'Carrier', logo: '' },
    { name: 'Gree', logo: '' },
    { name: 'Midea', logo: '' },
  ];

  duplicatedBrands: Brand[] = [];

  ngOnInit(): void {
    // Duplicate brands for seamless infinite scroll
    this.duplicatedBrands = [...this.brands, ...this.brands];
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  pause(): void {
    this.isPaused.set(true);
  }

  resume(): void {
    this.isPaused.set(false);
  }
}
