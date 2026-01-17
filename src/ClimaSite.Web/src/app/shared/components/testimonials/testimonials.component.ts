import { Component, signal, OnInit, OnDestroy, PLATFORM_ID, inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

interface Testimonial {
  id: number;
  name: string;
  location: string;
  rating: number;
  text: string;
  initials: string;
}

@Component({
  selector: 'app-testimonials',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <section class="testimonials-section" data-testid="testimonials-section">
      <div class="testimonials-header">
        <h2>{{ 'home.testimonials.title' | translate }}</h2>
        <p class="subtitle">{{ 'home.testimonials.subtitle' | translate }}</p>
      </div>

      <div class="testimonials-carousel" (mouseenter)="pauseAutoplay()" (mouseleave)="resumeAutoplay()">
        <div class="testimonials-track" [style.transform]="'translateX(-' + (currentIndex() * 100) + '%)'">
          @for (testimonial of testimonials; track testimonial.id) {
            <div class="testimonial-card">
              <div class="testimonial-header">
                <div class="avatar">{{ testimonial.initials }}</div>
                <div class="customer-info">
                  <div class="name">{{ testimonial.name }}</div>
                  <div class="location">{{ testimonial.location }}</div>
                </div>
              </div>
              <div class="rating">
                @for (star of [1,2,3,4,5]; track star) {
                  <span class="star" [class.filled]="star <= testimonial.rating">★</span>
                }
              </div>
              <p class="testimonial-text">"{{ testimonial.text }}"</p>
            </div>
          }
        </div>

        <div class="carousel-controls">
          <button class="carousel-btn prev" (click)="prevSlide()" [disabled]="currentIndex() === 0">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M15.75 19.5L8.25 12l7.5-7.5" stroke="currentColor" stroke-width="2" fill="none"/>
            </svg>
          </button>
          <div class="carousel-dots">
            @for (dot of testimonials; track dot.id; let i = $index) {
              <button
                class="dot"
                [class.active]="currentIndex() === i"
                (click)="goToSlide(i)"
              ></button>
            }
          </div>
          <button class="carousel-btn next" (click)="nextSlide()" [disabled]="currentIndex() >= testimonials.length - 1">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M8.25 4.5l7.5 7.5-7.5 7.5" stroke="currentColor" stroke-width="2" fill="none"/>
            </svg>
          </button>
        </div>
      </div>
    </section>
  `,
  styles: [`
    .testimonials-section {
      padding: 4rem 2rem;
      background: var(--color-bg-secondary);
    }

    .testimonials-header {
      text-align: center;
      margin-bottom: 3rem;

      h2 {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        font-size: 1.125rem;
        margin: 0;
      }
    }

    .testimonials-carousel {
      max-width: 1000px;
      margin: 0 auto;
      overflow: hidden;
      position: relative;
    }

    .testimonials-track {
      display: flex;
      transition: transform 0.5s ease;
    }

    .testimonial-card {
      flex: 0 0 100%;
      padding: 2rem;
      background: var(--color-bg-primary);
      border-radius: 16px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
      margin: 0 1rem;
      box-sizing: border-box;
    }

    .testimonial-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .avatar {
      width: 56px;
      height: 56px;
      border-radius: 50%;
      background: linear-gradient(135deg, var(--color-primary), var(--color-primary-dark));
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 1.25rem;
    }

    .customer-info {
      .name {
        font-weight: 600;
        color: var(--color-text-primary);
        font-size: 1.125rem;
      }

      .location {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .rating {
      margin-bottom: 1rem;
      font-size: 1.25rem;

      .star {
        color: var(--color-border);
        &.filled {
          color: #ffc107;
        }
      }
    }

    .testimonial-text {
      color: var(--color-text-secondary);
      font-size: 1.125rem;
      line-height: 1.7;
      font-style: italic;
      margin: 0;
    }

    .carousel-controls {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1.5rem;
      margin-top: 2rem;
    }

    .carousel-btn {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      color: var(--color-text-primary);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;

      svg {
        width: 20px;
        height: 20px;
      }

      &:hover:not(:disabled) {
        background: var(--color-primary);
        color: white;
        border-color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.4;
        cursor: not-allowed;
      }
    }

    .carousel-dots {
      display: flex;
      gap: 0.5rem;
    }

    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--color-border);
      border: none;
      cursor: pointer;
      padding: 0;
      transition: all 0.2s;

      &.active {
        background: var(--color-primary);
        transform: scale(1.2);
      }

      &:hover:not(.active) {
        background: var(--color-text-secondary);
      }
    }

    @media (max-width: 768px) {
      .testimonials-section {
        padding: 3rem 1rem;
      }

      .testimonials-header h2 {
        font-size: 1.5rem;
      }

      .testimonial-card {
        padding: 1.5rem;
        margin: 0 0.5rem;
      }

      .testimonial-text {
        font-size: 1rem;
      }
    }
  `]
})
export class TestimonialsComponent implements OnInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private autoplayInterval: ReturnType<typeof setInterval> | null = null;

  currentIndex = signal(0);

  testimonials: Testimonial[] = [
    {
      id: 1,
      name: 'Maria Petrova',
      location: 'Sofia, Bulgaria',
      rating: 5,
      text: 'Excellent service and product quality! The air conditioner was installed quickly and works perfectly. Very happy with my purchase.',
      initials: 'MP'
    },
    {
      id: 2,
      name: 'Hans Mueller',
      location: 'Berlin, Germany',
      rating: 5,
      text: 'Fast delivery and professional installation. The team was very knowledgeable and helped me choose the right system for my home.',
      initials: 'HM'
    },
    {
      id: 3,
      name: 'Stefan Ivanov',
      location: 'Plovdiv, Bulgaria',
      rating: 5,
      text: 'Great prices and amazing customer support. They answered all my questions and the heating system is working flawlessly.',
      initials: 'SI'
    },
    {
      id: 4,
      name: 'Anna Schmidt',
      location: 'Munich, Germany',
      rating: 4,
      text: 'Good selection of products and competitive prices. The website is easy to navigate and checkout was simple.',
      initials: 'AS'
    },
    {
      id: 5,
      name: 'Georgi Dimitrov',
      location: 'Varna, Bulgaria',
      rating: 5,
      text: 'Highly recommend! Professional service from start to finish. The energy-efficient AC unit has already saved me money on bills.',
      initials: 'GD'
    }
  ];

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.startAutoplay();
    }
  }

  ngOnDestroy(): void {
    this.stopAutoplay();
  }

  private startAutoplay(): void {
    this.autoplayInterval = setInterval(() => {
      this.nextSlide();
    }, 5000);
  }

  private stopAutoplay(): void {
    if (this.autoplayInterval) {
      clearInterval(this.autoplayInterval);
      this.autoplayInterval = null;
    }
  }

  pauseAutoplay(): void {
    this.stopAutoplay();
  }

  resumeAutoplay(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.startAutoplay();
    }
  }

  nextSlide(): void {
    const next = this.currentIndex() + 1;
    if (next >= this.testimonials.length) {
      this.currentIndex.set(0);
    } else {
      this.currentIndex.set(next);
    }
  }

  prevSlide(): void {
    const prev = this.currentIndex() - 1;
    if (prev < 0) {
      this.currentIndex.set(this.testimonials.length - 1);
    } else {
      this.currentIndex.set(prev);
    }
  }

  goToSlide(index: number): void {
    this.currentIndex.set(index);
  }
}
