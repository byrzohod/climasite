import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

/**
 * Number of sections defined in i18n for each legal page. Sections are modelled as
 * numbered keys (e.g. `legal.terms.sections.0.heading` / `.body`) because ngx-translate
 * cannot iterate a JSON array via the `| translate` pipe. The component reads this count
 * and resolves each section's keys, while body paragraphs are split on newlines.
 */
export const LEGAL_SECTION_COUNTS: Record<string, number> = {
  terms: 8,
  privacy: 8,
  cookies: 6,
  returns: 6,
  shipping: 6,
  impressum: 6
};

/** Pages that should display the visible "placeholder content — pre-launch" note. */
const PLACEHOLDER_PAGES = new Set(['terms', 'privacy', 'cookies', 'returns', 'shipping']);

interface LegalSection {
  heading: string;
  paragraphs: string[];
}

/**
 * Shared, data-driven legal/support prose page. The page to render is selected via the
 * route's `data.pageKey` (terms, privacy, cookies, returns, shipping, impressum). All copy
 * lives in i18n under `legal.<pageKey>.*` so it is fully translatable and theme-agnostic.
 */
@Component({
  selector: 'app-legal-page',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <article class="legal-page" data-testid="legal-page" [attr.data-page]="pageKey()">
      <header class="legal-header">
        <h1 data-testid="legal-title">{{ baseKey() + '.title' | translate }}</h1>
        <p class="legal-intro" data-testid="legal-intro">{{ baseKey() + '.intro' | translate }}</p>
        <p class="legal-updated" data-testid="legal-last-updated">
          {{ 'legal.common.lastUpdatedLabel' | translate }}: {{ baseKey() + '.lastUpdated' | translate }}
        </p>
        @if (showPlaceholderNote()) {
          <p class="legal-placeholder" role="note" data-testid="legal-placeholder-note">
            {{ 'legal.common.placeholderNote' | translate }}
          </p>
        }
      </header>

      <div class="legal-body">
        @for (section of sections(); track $index) {
          <section class="legal-section" [attr.data-testid]="'legal-section-' + $index">
            <h2>{{ section.heading }}</h2>
            @for (paragraph of section.paragraphs; track $index) {
              <p>{{ paragraph }}</p>
            }
          </section>
        }
      </div>
    </article>
  `,
  styles: [`
    .legal-page {
      max-width: 56rem;
      margin: 0 auto;
      padding: 3rem 1.5rem 4rem;
      color: var(--color-text-primary);
    }

    .legal-header {
      margin-bottom: 2.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border-primary);
    }

    .legal-header h1 {
      font-size: 2.25rem;
      font-weight: 700;
      margin: 0 0 0.75rem;
      color: var(--color-text-primary);
    }

    .legal-intro {
      font-size: 1.125rem;
      line-height: 1.6;
      color: var(--color-text-secondary);
      margin: 0 0 1rem;
    }

    .legal-updated {
      font-size: 0.875rem;
      color: var(--color-text-tertiary);
      margin: 0;
    }

    .legal-placeholder {
      margin: 1rem 0 0;
      padding: 0.75rem 1rem;
      font-size: 0.875rem;
      color: var(--color-warning, var(--color-text-secondary));
      background-color: var(--color-warning-bg, var(--color-bg-secondary));
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
    }

    .legal-section {
      margin-bottom: 2rem;
    }

    .legal-section h2 {
      font-size: 1.375rem;
      font-weight: 600;
      margin: 0 0 0.75rem;
      color: var(--color-text-primary);
    }

    .legal-section p {
      font-size: 1rem;
      line-height: 1.7;
      color: var(--color-text-secondary);
      margin: 0 0 0.75rem;
    }

    @media (max-width: 640px) {
      .legal-page {
        padding: 2rem 1rem 3rem;
      }

      .legal-header h1 {
        font-size: 1.75rem;
      }
    }
  `]
})
export class LegalPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);

  /** The active page key resolved from route data; reacts to route changes. */
  readonly pageKey = toSignal(
    this.route.data.pipe(map(data => (data['pageKey'] as string) ?? 'terms')),
    { initialValue: (this.route.snapshot.data['pageKey'] as string) ?? 'terms' }
  );

  readonly baseKey = computed(() => `legal.${this.pageKey()}`);

  readonly showPlaceholderNote = computed(() => PLACEHOLDER_PAGES.has(this.pageKey()));

  /** Re-resolves whenever the active language changes so translations stay live. */
  private readonly lang = signal(this.translate.currentLang);

  readonly sections = computed<LegalSection[]>(() => {
    // Touch the lang signal so this recomputes on language switch.
    this.lang();
    const base = this.baseKey();
    const count = LEGAL_SECTION_COUNTS[this.pageKey()] ?? 0;
    const result: LegalSection[] = [];

    for (let i = 0; i < count; i++) {
      const heading = this.translate.instant(`${base}.sections.${i}.heading`);
      const body = this.translate.instant(`${base}.sections.${i}.body`);
      result.push({
        heading,
        paragraphs: this.splitParagraphs(body)
      });
    }

    return result;
  });

  constructor() {
    // Keep the lang signal in sync to recompute resolved section copy on switch.
    this.translate.onLangChange.subscribe(event => this.lang.set(event.lang));
  }

  private splitParagraphs(body: unknown): string[] {
    if (typeof body !== 'string') {
      return [];
    }
    return body
      .split('\n')
      .map(paragraph => paragraph.trim())
      .filter(paragraph => paragraph.length > 0);
  }
}
