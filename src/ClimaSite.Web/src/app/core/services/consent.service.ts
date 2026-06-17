import { Injectable, signal, computed, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const STORAGE_KEY = 'climasite_cookie_consent';

/** Persisted cookie-consent decision. `null` means the user has not decided yet. */
export type CookieConsentDecision = 'accepted' | 'rejected' | null;

/**
 * Manages the visitor's cookie-consent decision.
 *
 * Until a decision is made, non-essential storage MUST be treated as NOT granted.
 * The decision is persisted to localStorage so it survives reloads. Signal-based so
 * components (e.g. the consent banner, analytics gates) can react to changes.
 */
@Injectable({
  providedIn: 'root'
})
export class ConsentService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly decision = signal<CookieConsentDecision>(null);

  /** True once the user has explicitly accepted or rejected. */
  readonly hasDecided = computed(() => this.decision() !== null);

  /** True only when the user has explicitly accepted non-essential cookies. */
  readonly accepted = computed(() => this.decision() === 'accepted');

  /**
   * Whether non-essential storage (analytics, marketing) may be used.
   * Anything other than an explicit "accepted" decision returns false.
   */
  readonly nonEssentialGranted = computed(() => this.decision() === 'accepted');

  constructor() {
    this.loadFromStorage();
  }

  /** Grant consent for non-essential cookies and persist the choice. */
  accept(): void {
    this.setDecision('accepted');
  }

  /** Decline non-essential cookies and persist the choice. */
  reject(): void {
    this.setDecision('rejected');
  }

  private setDecision(value: CookieConsentDecision): void {
    this.decision.set(value);
    this.saveToStorage(value);
  }

  private loadFromStorage(): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'accepted' || stored === 'rejected') {
        this.decision.set(stored);
      }
    } catch {
      // Storage unavailable (private mode, disabled) — keep undecided.
      this.decision.set(null);
    }
  }

  private saveToStorage(value: CookieConsentDecision): void {
    if (!this.isBrowser || value === null) {
      return;
    }

    try {
      localStorage.setItem(STORAGE_KEY, value);
    } catch {
      // Storage full or unavailable — the in-memory signal still reflects the choice.
    }
  }
}
