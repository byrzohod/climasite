import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import type { Subscription } from 'rxjs';
import { ThemeService } from '../../core/services/theme.service';
import { RecommendationsComponent } from './components/recommendations/recommendations.component';
import { RoomPreviewComponent } from './components/room-preview/room-preview.component';
import { SecondaryContentComponent } from './components/secondary-content/secondary-content.component';
import { WizardComponent } from './components/wizard/wizard.component';
import type { RecommendedProduct } from './models/home-v3.models';
import { HomeWizardStateService } from './services/home-wizard-state.service';
import { ProductRecommendationsService } from './services/product-recommendations.service';

/**
 * Home v3 — Configurator-First.
 *
 * Container that wires the wizard state, the live room preview, and the
 * product recommendation panel. The wizard is above the fold; the
 * recommendations slab + secondary content sits below.
 *
 * Per ADR 001 + ADR 002. The recommendation slab consumes the real
 * `GET /api/products/recommendations` endpoint — no mock data.
 */
@Component({
  selector: 'app-home-v3',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    WizardComponent,
    RoomPreviewComponent,
    RecommendationsComponent,
    SecondaryContentComponent,
  ],
  templateUrl: './home-v3.component.html',
  styleUrl: './home-v3.component.scss',
})
export class HomeV3Component {
  private readonly state = inject(HomeWizardStateService);
  private readonly recommendationsService = inject(ProductRecommendationsService);
  private readonly themeService = inject(ThemeService);

  readonly area = this.state.area;
  readonly roomType = this.state.roomType;
  readonly zone = this.state.zone;
  readonly outsideC = this.state.outsideSampleC;
  readonly insideC = this.state.insideTargetC;
  readonly watts = this.state.estimatedWatts;
  readonly theme = computed<'dark' | 'light'>(() => (this.themeService.isDarkMode() ? 'dark' : 'light'));

  readonly recommendations = signal<RecommendedProduct[] | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  private hasObservedInitialState = false;

  constructor() {
    // Watch wizard state and re-fetch recommendations after a short debounce.
    effect((onCleanup) => {
      const a = this.area();
      const r = this.roomType();
      const z = this.zone();
      let subscription: Subscription | null = null;
      const delayMs = this.hasObservedInitialState ? 350 : 1200;
      this.hasObservedInitialState = true;
      const timer = setTimeout(() => {
        subscription = this.fetchRecommendations(a, r, z);
      }, delayMs);

      onCleanup(() => {
        clearTimeout(timer);
        subscription?.unsubscribe();
      });
    });
  }

  private fetchRecommendations(area: number, type: ReturnType<typeof this.roomType>, zone: ReturnType<typeof this.zone>): Subscription {
    this.loading.set(true);
    this.error.set(null);
    return this.recommendationsService.getRecommendations(area, type, zone).subscribe({
      next: (products) => {
        this.recommendations.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('homeV3.recommendations.loadError');
        this.loading.set(false);
      },
    });
  }
}
