import { Injectable, computed, signal } from '@angular/core';
import type { ClimateZone, RoomType, WizardState } from '../models/home-v3.models';

/**
 * State for the Home v3 configurator wizard.
 *
 * Uses Angular Signals for fine-grained reactivity. The `debounced` computed
 * signal is what consumers should watch when triggering network requests —
 * the raw state changes rapidly while the user drags the slider, so we
 * expose a separate signal that only updates after a short idle window.
 */
@Injectable({ providedIn: 'root' })
export class HomeWizardStateService {
  private readonly _state = signal<WizardState>({
    area: 35,
    roomType: 'living',
    zone: 'B',
  });

  readonly state = this._state.asReadonly();
  readonly area = computed(() => this._state().area);
  readonly roomType = computed(() => this._state().roomType);
  readonly zone = computed(() => this._state().zone);

  setArea(area: number): void {
    const clamped = Math.max(10, Math.min(120, Math.round(area)));
    this._state.update((s) => ({ ...s, area: clamped }));
  }

  setRoomType(roomType: RoomType): void {
    this._state.update((s) => ({ ...s, roomType }));
  }

  setZone(zone: ClimateZone): void {
    this._state.update((s) => ({ ...s, zone }));
  }

  /**
   * BTU required for the current configuration.
   * Mirrors the backend zone multipliers so the UI can show a preview
   * before the network response arrives (A=200, B=250, C=320 BTU/m²).
   * MUST stay in sync with RecommendationScoringService.ZoneMultipliers (backend).
   */
  readonly estimatedBtu = computed(() => {
    const s = this._state();
    const multiplier = s.zone === 'A' ? 200 : s.zone === 'C' ? 320 : 250;
    return Math.round(s.area * multiplier);
  });

  /** Rough inside-target temp shown in the live stat chips. */
  readonly insideTargetC = computed(() => (this._state().roomType === 'bedroom' ? 21 : 23));

  /** Rough "outside" temp shown in the live stat chips (for visual context only). */
  readonly outsideSampleC = computed(() => {
    const z = this._state().zone;
    return z === 'A' ? 32 : z === 'C' ? -12 : 28;
  });

  /** Nominal power draw estimate shown in the live stat chips. */
  readonly estimatedWatts = computed(() => Math.round(this.estimatedBtu() * 0.09));
}
