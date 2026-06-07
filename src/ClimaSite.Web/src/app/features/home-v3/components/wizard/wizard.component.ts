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

  onRoomTypeKeydown(event: KeyboardEvent, type: RoomType): void {
    this.handleRadioKeydown(event, this.roomTypes, type, (next) => this.setRoomType(next), 'home-v3-room-');
  }

  setZone(zone: ClimateZone): void {
    this.state.setZone(zone);
  }

  onZoneKeydown(event: KeyboardEvent, zone: ClimateZone): void {
    this.handleRadioKeydown(event, this.zones, zone, (next) => this.setZone(next), 'home-v3-zone-');
  }

  private handleRadioKeydown<T extends string>(
    event: KeyboardEvent,
    values: readonly T[],
    current: T,
    select: (value: T) => void,
    testIdPrefix: string
  ): void {
    const currentIndex = values.indexOf(current);
    if (currentIndex === -1) {
      return;
    }

    let nextIndex: number;
    if (event.key === 'Home') {
      nextIndex = 0;
    } else if (event.key === 'End') {
      nextIndex = values.length - 1;
    } else {
      const keyActions: Record<string, number> = {
        ArrowRight: 1,
        ArrowDown: 1,
        ArrowLeft: -1,
        ArrowUp: -1,
      };
      const direction = keyActions[event.key];
      if (!direction) {
        return;
      }
      nextIndex = (currentIndex + direction + values.length) % values.length;
    }

    event.preventDefault();
    if (nextIndex === currentIndex) {
      return;
    }

    const currentElement = event.currentTarget as HTMLElement | null;
    const next = values[nextIndex];
    select(next);

    queueMicrotask(() => {
      const group = currentElement?.parentElement;
      group?.querySelector<HTMLElement>(`[data-testid="${testIdPrefix}${next}"]`)?.focus();
    });
  }
}
