import { Routes } from '@angular/router';

export const BRANDS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./brands-list/brands-list.component').then(m => m.BrandsListComponent),
    data: { seo: { titleKey: 'seo.brands.title', descriptionKey: 'seo.brands.description' } }
  },
  {
    path: ':slug',
    loadComponent: () => import('./brand-detail/brand-detail.component').then(m => m.BrandDetailComponent),
    data: { seo: { titleKey: 'seo.brand.title', descriptionKey: 'seo.brand.description' } }
  }
];
