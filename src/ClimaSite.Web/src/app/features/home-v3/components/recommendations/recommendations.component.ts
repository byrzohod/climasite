import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import type { RecommendedProduct } from '../../models/home-v3.models';
import { DualPricePipe } from '../../../../shared/pipes/dual-price.pipe';

@Component({
  selector: 'app-home-v3-recommendations',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, TranslateModule, DualPricePipe],
  templateUrl: './recommendations.component.html',
  styleUrl: './recommendations.component.scss',
})
export class RecommendationsComponent {
  readonly products = input<RecommendedProduct[] | null>(null);
  readonly loading = input<boolean>(false);
  readonly error = input<string | null>(null);

  readonly hasResults = computed(() => {
    const p = this.products();
    return p !== null && p.length > 0;
  });
}
