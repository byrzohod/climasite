import { Routes } from '@angular/router';

/**
 * Root-level legal & support routes. The footer links to these via ABSOLUTE paths
 * (`/terms`, `/privacy`, ...), so they are registered at the application root in
 * app.routes.ts. Each shared LegalPageComponent route selects its copy via `data.pageKey`.
 */
export const LEGAL_ROUTES: Routes = [
  {
    path: 'terms',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Terms of Service - ClimaSite',
    data: { pageKey: 'terms', animation: 'legal-terms' }
  },
  {
    path: 'privacy',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Privacy Policy - ClimaSite',
    data: { pageKey: 'privacy', animation: 'legal-privacy' }
  },
  {
    path: 'cookies',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Cookie Policy - ClimaSite',
    data: { pageKey: 'cookies', animation: 'legal-cookies' }
  },
  {
    path: 'returns',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Returns & Refunds - ClimaSite',
    data: { pageKey: 'returns', animation: 'legal-returns' }
  },
  {
    path: 'shipping',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Shipping Information - ClimaSite',
    data: { pageKey: 'shipping', animation: 'legal-shipping' }
  },
  {
    path: 'impressum',
    loadComponent: () => import('./legal-page.component').then(m => m.LegalPageComponent),
    title: 'Impressum - ClimaSite',
    data: { pageKey: 'impressum', animation: 'legal-impressum' }
  },
  {
    path: 'faq',
    loadComponent: () => import('./faq.component').then(m => m.FaqComponent),
    title: 'FAQ - ClimaSite',
    data: { animation: 'faq' }
  }
];
