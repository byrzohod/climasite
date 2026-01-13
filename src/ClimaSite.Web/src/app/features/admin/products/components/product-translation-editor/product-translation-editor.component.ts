import { Component, OnInit, inject, input, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import {
  AdminTranslationsService,
  ProductTranslationsDto,
  ProductTranslationDto,
  AddTranslationRequest,
  UpdateTranslationRequest
} from '../../services/admin-translations.service';

interface LanguageOption {
  code: string;
  name: string;
  flag: string;
}

@Component({
  selector: 'app-product-translation-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="translation-editor">
      <div class="editor-header">
        <h3>{{ 'admin.products.translations.title' | translate }}</h3>
        <p class="subtitle">{{ 'admin.products.translations.subtitle' | translate }}</p>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <span class="spinner"></span>
          {{ 'common.loading' | translate }}
        </div>
      } @else if (error()) {
        <div class="error-state">
          <span class="error-icon">⚠️</span>
          {{ error() }}
        </div>
      } @else {
        <div class="language-tabs">
          @for (lang of allLanguages; track lang.code) {
            <button
              class="tab"
              [class.active]="activeLanguage() === lang.code"
              [class.has-translation]="hasTranslation(lang.code)"
              (click)="setActiveLanguage(lang.code)">
              <span class="flag">{{ lang.flag }}</span>
              <span class="name">{{ lang.name }}</span>
              @if (hasTranslation(lang.code)) {
                <span class="check">✓</span>
              }
            </button>
          }
        </div>

        <div class="translation-form">
          @if (activeLanguage() === 'en') {
            <div class="default-language-notice">
              <span class="info-icon">ℹ️</span>
              {{ 'admin.products.translations.defaultLanguageNotice' | translate }}
            </div>
            <div class="readonly-content">
              <div class="form-group">
                <label>{{ 'admin.products.name' | translate }}</label>
                <div class="readonly-value">{{ translations()?.productName }}</div>
              </div>
            </div>
          } @else {
            <div class="form-group">
              <label for="name">
                {{ 'admin.products.name' | translate }}
                <span class="required">*</span>
              </label>
              <input
                type="text"
                id="name"
                [(ngModel)]="editForm.name"
                [placeholder]="translations()?.productName ?? ''"
                maxlength="255"
                required />
            </div>

            <div class="form-group">
              <label for="shortDescription">{{ 'admin.products.shortDescription' | translate }}</label>
              <textarea
                id="shortDescription"
                [(ngModel)]="editForm.shortDescription"
                rows="3"
                maxlength="500"></textarea>
            </div>

            <div class="form-group">
              <label for="description">{{ 'admin.products.description' | translate }}</label>
              <textarea
                id="description"
                [(ngModel)]="editForm.description"
                rows="6"></textarea>
            </div>

            <div class="seo-section">
              <h4>{{ 'admin.products.translations.seoSection' | translate }}</h4>
              <div class="form-group">
                <label for="metaTitle">{{ 'admin.products.metaTitle' | translate }}</label>
                <input
                  type="text"
                  id="metaTitle"
                  [(ngModel)]="editForm.metaTitle"
                  maxlength="200" />
              </div>

              <div class="form-group">
                <label for="metaDescription">{{ 'admin.products.metaDescription' | translate }}</label>
                <textarea
                  id="metaDescription"
                  [(ngModel)]="editForm.metaDescription"
                  rows="2"
                  maxlength="500"></textarea>
              </div>
            </div>

            <div class="form-actions">
              @if (hasTranslation(activeLanguage())) {
                <button class="btn-secondary" (click)="confirmDelete()">
                  {{ 'common.delete' | translate }}
                </button>
                <button class="btn-primary" (click)="saveTranslation()" [disabled]="saving()">
                  @if (saving()) {
                    <span class="spinner-small"></span>
                  }
                  {{ 'common.save' | translate }}
                </button>
              } @else {
                <button class="btn-primary" (click)="addTranslation()" [disabled]="saving() || !editForm.name">
                  @if (saving()) {
                    <span class="spinner-small"></span>
                  }
                  {{ 'admin.products.translations.addTranslation' | translate }}
                </button>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .translation-editor {
      background: var(--color-surface);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      padding: 1.5rem;
    }

    .editor-header {
      margin-bottom: 1.5rem;

      h3 {
        margin: 0 0 0.25rem;
        font-size: 1.125rem;
        color: var(--color-text-primary);
      }

      .subtitle {
        margin: 0;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }
    }

    .loading-state,
    .error-state {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 2rem;
      justify-content: center;
      color: var(--color-text-secondary);
    }

    .error-state {
      color: var(--color-error);
    }

    .spinner {
      display: inline-block;
      width: 20px;
      height: 20px;
      border: 2px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .language-tabs {
      display: flex;
      gap: 0.5rem;
      border-bottom: 1px solid var(--color-border);
      padding-bottom: 1rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .tab {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border);
      background: var(--color-surface);
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        background: var(--color-surface-hover);
        border-color: var(--color-primary);
      }

      &.active {
        background: var(--color-primary);
        color: white;
        border-color: var(--color-primary);

        .check {
          color: rgba(255, 255, 255, 0.8);
        }
      }

      &.has-translation:not(.active) {
        border-color: var(--color-success);

        .check {
          color: var(--color-success);
        }
      }

      .flag {
        font-size: 1.25rem;
      }

      .name {
        font-size: 0.875rem;
        font-weight: 500;
      }

      .check {
        font-size: 0.75rem;
      }
    }

    .default-language-notice {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem;
      background: var(--color-info-bg);
      border: 1px solid var(--color-info-border);
      border-radius: 6px;
      margin-bottom: 1rem;
      font-size: 0.875rem;
      color: var(--color-info);

      .info-icon {
        font-size: 1rem;
      }
    }

    .readonly-content {
      .readonly-value {
        padding: 0.75rem;
        background: var(--color-surface-secondary);
        border-radius: 4px;
        color: var(--color-text-secondary);
      }
    }

    .form-group {
      margin-bottom: 1rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        color: var(--color-text-primary);

        .required {
          color: var(--color-error);
        }
      }

      input,
      textarea {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 6px;
        font-size: 0.875rem;
        background: var(--color-surface);
        color: var(--color-text-primary);
        transition: border-color 0.2s;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }

        &::placeholder {
          color: var(--color-text-muted);
        }
      }

      textarea {
        resize: vertical;
      }
    }

    .seo-section {
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border);

      h4 {
        margin: 0 0 1rem;
        font-size: 1rem;
        color: var(--color-text-primary);
      }
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border);
    }

    .btn-primary,
    .btn-secondary {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .btn-primary {
      background: var(--color-primary);
      color: white;

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }
    }

    .btn-secondary {
      background: var(--color-surface);
      color: var(--color-error);
      border: 1px solid var(--color-error);

      &:hover:not(:disabled) {
        background: var(--color-error);
        color: white;
      }
    }

    .spinner-small {
      display: inline-block;
      width: 14px;
      height: 14px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
  `]
})
export class ProductTranslationEditorComponent implements OnInit {
  private translationsService = inject(AdminTranslationsService);
  private translate = inject(TranslateService);

  productId = input.required<string>();

  translations = signal<ProductTranslationsDto | null>(null);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);
  activeLanguage = signal('en');

  allLanguages: LanguageOption[] = [
    { code: 'en', name: 'English', flag: '🇬🇧' },
    { code: 'bg', name: 'Български', flag: '🇧🇬' },
    { code: 'de', name: 'Deutsch', flag: '🇩🇪' }
  ];

  editForm = {
    name: '',
    shortDescription: '',
    description: '',
    metaTitle: '',
    metaDescription: ''
  };

  constructor() {
    // Load translation when active language changes
    effect(() => {
      const lang = this.activeLanguage();
      const data = this.translations();
      if (data && lang !== 'en') {
        const existing = data.translations.find(t => t.languageCode === lang);
        if (existing) {
          this.editForm = {
            name: existing.name,
            shortDescription: existing.shortDescription ?? '',
            description: existing.description ?? '',
            metaTitle: existing.metaTitle ?? '',
            metaDescription: existing.metaDescription ?? ''
          };
        } else {
          this.clearForm();
        }
      }
    });
  }

  ngOnInit(): void {
    this.loadTranslations();
  }

  loadTranslations(): void {
    this.loading.set(true);
    this.error.set(null);

    this.translationsService.getProductTranslations(this.productId()).subscribe({
      next: (data) => {
        this.translations.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load translations');
        this.loading.set(false);
      }
    });
  }

  setActiveLanguage(code: string): void {
    this.activeLanguage.set(code);
  }

  hasTranslation(languageCode: string): boolean {
    if (languageCode === 'en') return true;
    return this.translations()?.translations.some(t => t.languageCode === languageCode) ?? false;
  }

  clearForm(): void {
    this.editForm = {
      name: '',
      shortDescription: '',
      description: '',
      metaTitle: '',
      metaDescription: ''
    };
  }

  addTranslation(): void {
    if (!this.editForm.name.trim()) return;

    this.saving.set(true);
    const request: AddTranslationRequest = {
      languageCode: this.activeLanguage(),
      name: this.editForm.name.trim(),
      shortDescription: this.editForm.shortDescription?.trim() || undefined,
      description: this.editForm.description?.trim() || undefined,
      metaTitle: this.editForm.metaTitle?.trim() || undefined,
      metaDescription: this.editForm.metaDescription?.trim() || undefined
    };

    this.translationsService.addTranslation(this.productId(), request).subscribe({
      next: () => {
        this.saving.set(false);
        this.loadTranslations();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message || 'Failed to add translation');
      }
    });
  }

  saveTranslation(): void {
    if (!this.editForm.name.trim()) return;

    this.saving.set(true);
    const request: UpdateTranslationRequest = {
      name: this.editForm.name.trim(),
      shortDescription: this.editForm.shortDescription?.trim() || undefined,
      description: this.editForm.description?.trim() || undefined,
      metaTitle: this.editForm.metaTitle?.trim() || undefined,
      metaDescription: this.editForm.metaDescription?.trim() || undefined
    };

    this.translationsService.updateTranslation(this.productId(), this.activeLanguage(), request).subscribe({
      next: () => {
        this.saving.set(false);
        this.loadTranslations();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message || 'Failed to update translation');
      }
    });
  }

  confirmDelete(): void {
    const langName = this.allLanguages.find(l => l.code === this.activeLanguage())?.name ?? this.activeLanguage();
    if (confirm(this.translate.instant('admin.products.translations.confirmDelete', { language: langName }))) {
      this.deleteTranslation();
    }
  }

  private deleteTranslation(): void {
    this.saving.set(true);

    this.translationsService.deleteTranslation(this.productId(), this.activeLanguage()).subscribe({
      next: () => {
        this.saving.set(false);
        this.clearForm();
        this.loadTranslations();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message || 'Failed to delete translation');
      }
    });
  }
}
