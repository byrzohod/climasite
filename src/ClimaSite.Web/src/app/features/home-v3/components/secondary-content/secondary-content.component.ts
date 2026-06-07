import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-home-v3-secondary-content',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule],
  templateUrl: './secondary-content.component.html',
  styleUrl: './secondary-content.component.scss',
})
export class SecondaryContentComponent {
  readonly trustStats = [
    {
      valueKey: 'homeV3.trust.stats.installsValue',
      labelKey: 'homeV3.trust.installs',
      testId: 'home-v3-trust-installs'
    },
    {
      valueKey: 'homeV3.trust.stats.ratingValue',
      labelKey: 'homeV3.trust.rating',
      testId: 'home-v3-trust-rating'
    },
    {
      valueKey: 'homeV3.trust.stats.warrantyValue',
      labelKey: 'homeV3.trust.warranty',
      testId: 'home-v3-trust-warranty'
    },
    {
      valueKey: 'homeV3.trust.stats.deliveryValue',
      labelKey: 'homeV3.trust.delivery',
      testId: 'home-v3-trust-delivery'
    }
  ] as const;
}
