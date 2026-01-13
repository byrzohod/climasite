import { Routes } from '@angular/router';

export const BRANDS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./brands-list/brands-list.component').then(m => m.BrandsListComponent)
  },
  {
    path: ':slug',
    loadComponent: () => import('./brand-detail/brand-detail.component').then(m => m.BrandDetailComponent)
  }
];
