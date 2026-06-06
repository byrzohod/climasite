import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import type { ClimateZone, RoomType } from '../../models/home-v3.models';
import { HomeWizardStateService } from '../../services/home-wizard-state.service';

/**
 * The configurator wizard — area slider, room type segmented control,
 * climate zone picker. Writes into HomeWizardStateService which the
 * container watches for recommendation fetches.
 */
@Component({
  selector: 'app-home-v3-wizard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslateModule],
  templateUrl: './wizard.component.html',
  styleUrl: './wizard.component.scss',
})
export class WizardComponent {
  private readonly state = inject(HomeWizardStateService);

  readonly area = this.state.area;
  readonly roomType = this.state.roomType;
  readonly zone = this.state.zone;
  readonly estimatedBtu = this.state.estimatedBtu;

  readonly roomTypes: RoomType[] = ['living', 'bedroom', 'office', 'commercial'];
  readonly zones: ClimateZone[] = ['A', 'B', 'C'];

  onAreaInput(event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.state.setArea(value);
  }

  setRoomType(type: RoomType): void {
    this.state.setRoomType(type);
  }

  setZone(zone: ClimateZone): void {
    this.state.setZone(zone);
  }
}
