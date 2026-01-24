import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AnimateOnScrollDirective } from '../../directives/animate-on-scroll.directive';
import { ParallaxDirective } from '../../directives/parallax.directive';
import { FloatingDirective } from '../../directives/floating.directive';

@Component({
  selector: 'app-final-cta',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, AnimateOnScrollDirective, ParallaxDirective, FloatingDirective],
  template: `
    <section class="final-cta-section" data-testid="final-cta">
      <div class="cta-background">
        <!-- Animated shapes with parallax -->
        <div class="shape shape-1" appParallax [mode]="'both'" [speed]="0.2" [intensity]="30" [direction]="'down'"></div>
        <div class="shape shape-2" appParallax [mode]="'both'" [speed]="0.15" [intensity]="25" [direction]="'up'"></div>
        <div class="shape shape-3" appParallax [mode]="'mouse'" [intensity]="15" [smoothing]="0.08"></div>
      </div>
      <div class="cta-container">
        <div class="cta-content" appAnimateOnScroll [animation]="'fade-in-up'">
          <h2>{{ 'home.finalCta.title' | translate }}</h2>
          <p>{{ 'home.finalCta.subtitle' | translate }}</p>
          <div class="cta-buttons">
            <a routerLink="/products" class="cta-button cta-button--primary">
              {{ 'home.finalCta.shopNow' | translate }}
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/>
              </svg>
            </a>
            <a routerLink="/contact" class="cta-button cta-button--secondary">
              {{ 'home.finalCta.contactExpert' | translate }}
            </a>
          </div>
        </div>
        <div class="cta-visual" appAnimateOnScroll [animation]="'fade-in-right'" [delay]="200">
          <div class="visual-card">
            <div class="card-icon card-icon--cooling" appFloating [variant]="'medium'" [duration]="4000" [delay]="0">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.25a.75.75 0 01.75.75v2.25a.75.75 0 01-1.5 0V3a.75.75 0 01.75-.75zM7.5 12a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM18.894 6.166a.75.75 0 00-1.06-1.06l-1.591 1.59a.75.75 0 101.06 1.061l1.591-1.59zM21.75 12a.75.75 0 01-.75.75h-2.25a.75.75 0 010-1.5H21a.75.75 0 01.75.75zM17.834 18.894a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 10-1.061 1.06l1.59 1.591zM12 18a.75.75 0 01.75.75V21a.75.75 0 01-1.5 0v-2.25A.75.75 0 0112 18zM7.758 17.303a.75.75 0 00-1.061-1.06l-1.591 1.59a.75.75 0 001.06 1.061l1.591-1.59zM6 12a.75.75 0 01-.75.75H3a.75.75 0 010-1.5h2.25A.75.75 0 016 12zM6.697 7.757a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 00-1.061 1.06l1.59 1.591z"/>
              </svg>
            </div>
            <div class="card-icon card-icon--heating" appFloating [variant]="'medium'" [duration]="4500" [delay]="300">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12.963 2.286a.75.75 0 00-1.071-.136 9.742 9.742 0 00-3.539 6.177A7.547 7.547 0 016.648 6.61a.75.75 0 00-1.152.082A9 9 0 1015.68 4.534a7.46 7.46 0 01-2.717-2.248zM15.75 14.25a3.75 3.75 0 11-7.313-1.172c.628.465 1.35.81 2.133 1a5.99 5.99 0 011.925-3.545 3.75 3.75 0 013.255 3.717z" clip-rule="evenodd"/>
              </svg>
            </div>
            <div class="card-icon card-icon--vent" appFloating [variant]="'medium'" [duration]="5000" [delay]="600">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25zm-.53 5.47a.75.75 0 011.06 0l3 3a.75.75 0 010 1.06l-3 3a.75.75 0 11-1.06-1.06l1.72-1.72H8.25a.75.75 0 010-1.5h4.94l-1.72-1.72a.75.75 0 010-1.06z"/>
              </svg>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .final-cta-section {
      padding: 6rem 2rem;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      position: relative;
      overflow: hidden;
    }

    .cta-background {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      pointer-events: none;
    }

    .shape {
      position: absolute;
      border-radius: 50%;
      opacity: 0.1;
    }

    .shape-1 {
      width: 400px;
      height: 400px;
      background: white;
      top: -200px;
      right: -100px;
      animation: float 20s ease-in-out infinite;
    }

    .shape-2 {
      width: 300px;
      height: 300px;
      background: white;
      bottom: -150px;
      left: -100px;
      animation: float 15s ease-in-out infinite reverse;
    }

    .shape-3 {
      width: 200px;
      height: 200px;
      background: white;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      animation: pulse 10s ease-in-out infinite;
    }

    @keyframes float {
      0%, 100% { transform: translateY(0) rotate(0deg); }
      50% { transform: translateY(-30px) rotate(10deg); }
    }

    @keyframes pulse {
      0%, 100% { transform: translate(-50%, -50%) scale(1); opacity: 0.1; }
      50% { transform: translate(-50%, -50%) scale(1.2); opacity: 0.05; }
    }

    .cta-container {
      max-width: 1200px;
      margin: 0 auto;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4rem;
      align-items: center;
      position: relative;
      z-index: 1;
    }

    .cta-content {
      h2 {
        font-size: 3rem;
        font-weight: 700;
        color: white;
        margin: 0 0 1.5rem;
        line-height: 1.2;
      }

      p {
        font-size: 1.25rem;
        color: rgba(255, 255, 255, 0.85);
        margin: 0 0 2.5rem;
        line-height: 1.6;
      }
    }

    .cta-buttons {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .cta-button {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1rem 2rem;
      border-radius: 50px;
      font-weight: 600;
      font-size: 1rem;
      text-decoration: none;
      transition: all 0.3s ease;

      svg {
        width: 20px;
        height: 20px;
        transition: transform 0.3s;
      }

      &--primary {
        background: white;
        color: var(--color-primary);

        &:hover {
          transform: translateY(-3px);
          box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);

          svg {
            transform: translateX(4px);
          }
        }
      }

      &--secondary {
        background: rgba(255, 255, 255, 0.15);
        color: white;
        border: 2px solid rgba(255, 255, 255, 0.3);
        backdrop-filter: blur(10px);

        &:hover {
          background: rgba(255, 255, 255, 0.25);
          border-color: rgba(255, 255, 255, 0.5);
        }
      }
    }

    .cta-visual {
      display: flex;
      justify-content: center;
    }

    .visual-card {
      position: relative;
      width: 280px;
      height: 280px;
    }

    .card-icon {
      position: absolute;
      width: 100px;
      height: 100px;
      border-radius: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
      animation: float 6s ease-in-out infinite;

      svg {
        width: 48px;
        height: 48px;
      }

      &--cooling {
        background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
        top: 0;
        left: 50%;
        transform: translateX(-50%);
        animation-delay: 0s;
      }

      &--heating {
        background: linear-gradient(135deg, #f97316 0%, #ea580c 100%);
        bottom: 20px;
        left: 0;
        animation-delay: -2s;
      }

      &--vent {
        background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
        bottom: 20px;
        right: 0;
        animation-delay: -4s;
      }
    }

    @media (max-width: 1024px) {
      .cta-container {
        grid-template-columns: 1fr;
        text-align: center;
      }

      .cta-content h2 {
        font-size: 2.25rem;
      }

      .cta-buttons {
        justify-content: center;
      }

      .cta-visual {
        order: -1;
      }

      .visual-card {
        width: 220px;
        height: 220px;
      }

      .card-icon {
        width: 80px;
        height: 80px;

        svg {
          width: 36px;
          height: 36px;
        }
      }
    }

    @media (max-width: 640px) {
      .final-cta-section {
        padding: 4rem 1rem;
      }

      .cta-content {
        h2 {
          font-size: 1.75rem;
        }

        p {
          font-size: 1rem;
        }
      }

      .cta-buttons {
        flex-direction: column;
      }

      .cta-button {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class FinalCtaComponent {}
