import { Routes } from '@angular/router';

/**
 * Root-level legal & support routes. The footer links to these via ABSOLUTE paths
 * (`/terms`, `/privacy`, ...), so they are registered at the application root in
 * app.routes.ts. Each shared LegalPageComponent route selects its copy via `data.pageKey`.
 * Titles/descriptions are driven by `data.seo` (the SeoTitleStrategy is the single
 * brand-title authority — no hardcoded ` - ClimaSite` strings here).
 */
export const LEGAL_ROUTES: Routes = [
  {
    path: 'terms',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'terms', animation: 'legal-terms', seo: { titleKey: 'seo.terms.title', descriptionKey: 'seo.terms.description' } }
  },
  {
    path: 'privacy',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'privacy', animation: 'legal-privacy', seo: { titleKey: 'seo.privacy.title', descriptionKey: 'seo.privacy.description' } }
  },
  {
    path: 'cookies',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'cookies', animation: 'legal-cookies', seo: { titleKey: 'seo.cookies.title', descriptionKey: 'seo.cookies.description' } }
  },
  {
    path: 'returns',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'returns', animation: 'legal-returns', seo: { titleKey: 'seo.returns.title', descriptionKey: 'seo.returns.description' } }
  },
  {
    path: 'shipping',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'shipping', animation: 'legal-shipping', seo: { titleKey: 'seo.shipping.title', descriptionKey: 'seo.shipping.description' } }
  },
  {
    path: 'impressum',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    data: { pageKey: 'impressum', animation: 'legal-impressum', seo: { titleKey: 'seo.impressum.title', descriptionKey: 'seo.impressum.description' } }
  },
  {
    path: 'faq',
    loadComponent: () => import('./faq.component').then(m => m.FaqComponent),
    data: { animation: 'faq', seo: { titleKey: 'seo.faq.title', descriptionKey: 'seo.faq.description' } }
  }
];
