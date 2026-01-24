import { Component, ElementRef, inject, signal, OnInit, OnDestroy, PLATFORM_ID, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

interface Step {
  number: string;
  titleKey: string;
  descriptionKey: string;
  icon: string;
  color: string;
  linkKey: string;
  link: string;
}

@Component({
  selector: 'app-how-it-works',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <section class="how-it-works-section" #sectionRef data-testid="how-it-works">
      <div class="how-it-works-container">
        <div class="how-it-works-header">
          <span class="section-label">{{ 'home.howItWorks.label' | translate }}</span>
          <h2>{{ 'home.howItWorks.title' | translate }}</h2>
          <p>{{ 'home.howItWorks.subtitle' | translate }}</p>
        </div>

        <div class="steps-wrapper">
          <div class="steps-content">
            @for (step of steps; track step.number; let i = $index) {
              <div
                class="step-item"
                [class.active]="activeStep() === i"
                (mouseenter)="setActiveStep(i)"
                #stepItem
              >
                <div class="step-number" [style.--step-color]="step.color">
                  [{{ step.number }}]
                </div>
                <div class="step-content">
                  <h3>{{ step.titleKey | translate }}</h3>
                  <p>{{ step.descriptionKey | translate }}</p>
                  <a [routerLink]="step.link" class="step-link" [style.--step-color]="step.color">
                    {{ step.linkKey | translate }}
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/>
                    </svg>
                  </a>
                </div>
              </div>
            }
          </div>

          <div class="steps-visual">
            <div class="visual-container">
              @for (step of steps; track step.number; let i = $index) {
                <div
                  class="visual-item"
                  [class.active]="activeStep() === i"
                  [style.--step-color]="step.color"
                >
                  <div class="visual-icon" [innerHTML]="step.icon"></div>
                  <div class="visual-number">{{ step.number }}</div>
                </div>
              }
              <!-- Progress ring -->
              <svg class="progress-ring" viewBox="0 0 200 200">
                <circle
                  class="progress-ring-bg"
                  cx="100"
                  cy="100"
                  r="90"
                  fill="none"
                  stroke-width="2"
                />
                <circle
                  class="progress-ring-fill"
                  cx="100"
                  cy="100"
                  r="90"
                  fill="none"
                  stroke-width="3"
                  [style.stroke-dashoffset]="getProgressOffset()"
                />
              </svg>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .how-it-works-section {
      padding: 6rem 2rem;
      background: var(--color-bg-primary);
      position: relative;
    }

    .how-it-works-container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .how-it-works-header {
      text-align: center;
      margin-bottom: 4rem;

      .section-label {
        display: inline-block;
        font-size: 0.875rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        color: var(--color-primary);
        background: var(--color-primary-light);
        padding: 0.5rem 1rem;
        border-radius: 50px;
        margin-bottom: 1.5rem;
      }

      h2 {
        font-size: 2.75rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 1rem;
        line-height: 1.2;
      }

      p {
        font-size: 1.25rem;
        color: var(--color-text-secondary);
        margin: 0 auto;
        max-width: 600px;
        line-height: 1.6;
      }
    }

    .steps-wrapper {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4rem;
      align-items: center;
    }

    .steps-content {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .step-item {
      display: flex;
      gap: 1.5rem;
      padding: 1.5rem;
      border-radius: 16px;
      background: var(--color-bg-secondary);
      border: 2px solid transparent;
      cursor: pointer;
      transition: all 0.3s ease;

      &:hover,
      &.active {
        background: var(--color-bg-primary);
        border-color: var(--step-color, var(--color-primary));
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.08);
        transform: translateX(8px);
      }

      &.active {
        .step-number {
          color: var(--step-color, var(--color-primary));
        }

        .step-content h3 {
          color: var(--step-color, var(--color-primary));
        }
      }
    }

    .step-number {
      font-family: 'SF Mono', 'Fira Code', monospace;
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-tertiary);
      white-space: nowrap;
      transition: color 0.3s;
    }

    .step-content {
      flex: 1;

      h3 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
        transition: color 0.3s;
      }

      p {
        font-size: 0.9375rem;
        color: var(--color-text-secondary);
        line-height: 1.6;
        margin: 0 0 1rem;
      }
    }

    .step-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--step-color, var(--color-primary));
      text-decoration: none;
      transition: gap 0.3s;

      svg {
        width: 18px;
        height: 18px;
        transition: transform 0.3s;
      }

      &:hover {
        gap: 0.75rem;

        svg {
          transform: translateX(4px);
        }
      }
    }

    .steps-visual {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 450px;
    }

    .visual-container {
      position: relative;
      width: 320px;
      height: 320px;
    }

    .visual-item {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%) scale(0.8);
      opacity: 0;
      transition: all 0.5s cubic-bezier(0.4, 0, 0.2, 1);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;

      &.active {
        transform: translate(-50%, -50%) scale(1);
        opacity: 1;
      }
    }

    .visual-icon {
      width: 140px;
      height: 140px;
      border-radius: 32px;
      background: linear-gradient(135deg, var(--step-color) 0%, color-mix(in srgb, var(--step-color) 70%, black) 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      box-shadow: 0 20px 60px color-mix(in srgb, var(--step-color) 40%, transparent);

      :deep(svg) {
        width: 64px;
        height: 64px;
      }
    }

    .visual-number {
      font-size: 4rem;
      font-weight: 800;
      color: var(--step-color);
      opacity: 0.2;
      position: absolute;
      bottom: -20px;
      right: -20px;
    }

    .progress-ring {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 100%;
      height: 100%;
    }

    .progress-ring-bg {
      stroke: var(--color-border-primary);
    }

    .progress-ring-fill {
      stroke: var(--color-primary);
      stroke-linecap: round;
      stroke-dasharray: 565.48;
      transform: rotate(-90deg);
      transform-origin: center;
      transition: stroke-dashoffset 0.5s ease;
    }

    @media (max-width: 1024px) {
      .steps-wrapper {
        grid-template-columns: 1fr;
        gap: 3rem;
      }

      .steps-visual {
        order: -1;
        min-height: 300px;
      }

      .visual-container {
        width: 250px;
        height: 250px;
      }

      .visual-icon {
        width: 100px;
        height: 100px;
        border-radius: 24px;

        :deep(svg) {
          width: 48px;
          height: 48px;
        }
      }
    }

    @media (max-width: 640px) {
      .how-it-works-section {
        padding: 4rem 1rem;
      }

      .how-it-works-header {
        h2 {
          font-size: 2rem;
        }

        p {
          font-size: 1rem;
        }
      }

      .step-item {
        flex-direction: column;
        gap: 0.75rem;
        padding: 1.25rem;

        &:hover,
        &.active {
          transform: none;
        }
      }
    }
  `]
})
export class HowItWorksComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('sectionRef') sectionRef!: ElementRef;

  private readonly platformId = inject(PLATFORM_ID);
  private autoPlayInterval: ReturnType<typeof setInterval> | null = null;

  activeStep = signal(0);

  steps: Step[] = [
    {
      number: '01',
      titleKey: 'home.howItWorks.steps.browse.title',
      descriptionKey: 'home.howItWorks.steps.browse.description',
      linkKey: 'home.howItWorks.steps.browse.link',
      link: '/products',
      color: 'var(--color-step-1)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M10.5 3.75a6.75 6.75 0 100 13.5 6.75 6.75 0 000-13.5zM2.25 10.5a8.25 8.25 0 1114.59 5.28l4.69 4.69a.75.75 0 11-1.06 1.06l-4.69-4.69A8.25 8.25 0 012.25 10.5z" clip-rule="evenodd"/>
      </svg>`
    },
    {
      number: '02',
      titleKey: 'home.howItWorks.steps.compare.title',
      descriptionKey: 'home.howItWorks.steps.compare.description',
      linkKey: 'home.howItWorks.steps.compare.link',
      link: '/products',
      color: 'var(--color-step-2)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M3 6a3 3 0 013-3h2.25a3 3 0 013 3v2.25a3 3 0 01-3 3H6a3 3 0 01-3-3V6zm9.75 0a3 3 0 013-3H18a3 3 0 013 3v2.25a3 3 0 01-3 3h-2.25a3 3 0 01-3-3V6zM3 15.75a3 3 0 013-3h2.25a3 3 0 013 3V18a3 3 0 01-3 3H6a3 3 0 01-3-3v-2.25zm9.75 0a3 3 0 013-3H18a3 3 0 013 3V18a3 3 0 01-3 3h-2.25a3 3 0 01-3-3v-2.25z" clip-rule="evenodd"/>
      </svg>`
    },
    {
      number: '03',
      titleKey: 'home.howItWorks.steps.order.title',
      descriptionKey: 'home.howItWorks.steps.order.description',
      linkKey: 'home.howItWorks.steps.order.link',
      link: '/cart',
      color: 'var(--color-step-3)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path d="M2.25 2.25a.75.75 0 000 1.5h1.386c.17 0 .318.114.362.278l2.558 9.592a3.752 3.752 0 00-2.806 3.63c0 .414.336.75.75.75h15.75a.75.75 0 000-1.5H5.378A2.25 2.25 0 017.5 15h11.218a.75.75 0 00.674-.421 60.358 60.358 0 002.96-7.228.75.75 0 00-.525-.965A60.864 60.864 0 005.68 4.509l-.232-.867A1.875 1.875 0 003.636 2.25H2.25zM3.75 20.25a1.5 1.5 0 113 0 1.5 1.5 0 01-3 0zM16.5 20.25a1.5 1.5 0 113 0 1.5 1.5 0 01-3 0z"/>
      </svg>`
    },
    {
      number: '04',
      titleKey: 'home.howItWorks.steps.install.title',
      descriptionKey: 'home.howItWorks.steps.install.description',
      linkKey: 'home.howItWorks.steps.install.link',
      link: '/resources',
      color: 'var(--color-step-4)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
        <path fill-rule="evenodd" d="M12 6.75a5.25 5.25 0 016.775-5.025.75.75 0 01.313 1.248l-3.32 3.319c.063.475.276.934.641 1.299.365.365.824.578 1.3.64l3.318-3.319a.75.75 0 011.248.313 5.25 5.25 0 01-5.472 6.756c-1.018-.086-1.87.1-2.309.634L7.344 21.3A3.298 3.298 0 112.7 16.657l8.684-7.151c.533-.44.72-1.291.634-2.309A5.342 5.342 0 0112 6.75zM4.117 19.125a.75.75 0 01.75-.75h.008a.75.75 0 01.75.75v.008a.75.75 0 01-.75.75h-.008a.75.75 0 01-.75-.75v-.008z" clip-rule="evenodd"/>
      </svg>`
    }
  ];

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.startAutoPlay();
    }
  }

  ngAfterViewInit(): void {
    // Could add intersection observer here for scroll-based triggers
  }

  ngOnDestroy(): void {
    this.stopAutoPlay();
  }

  private startAutoPlay(): void {
    this.autoPlayInterval = setInterval(() => {
      const next = (this.activeStep() + 1) % this.steps.length;
      this.activeStep.set(next);
    }, 4000);
  }

  private stopAutoPlay(): void {
    if (this.autoPlayInterval) {
      clearInterval(this.autoPlayInterval);
      this.autoPlayInterval = null;
    }
  }

  setActiveStep(index: number): void {
    this.activeStep.set(index);
    this.stopAutoPlay();
    // Restart autoplay after user interaction
    setTimeout(() => {
      if (isPlatformBrowser(this.platformId)) {
        this.startAutoPlay();
      }
    }, 8000);
  }

  getProgressOffset(): number {
    const circumference = 2 * Math.PI * 90; // 565.48
    const progress = (this.activeStep() + 1) / this.steps.length;
    return circumference * (1 - progress);
  }
}
