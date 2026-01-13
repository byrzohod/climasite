import { Routes } from '@angular/router';

export const PROMOTIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./promotions-list/promotions-list.component').then(m => m.PromotionsListComponent)
  },
  {
    path: ':slug',
    loadComponent: () =>
      import('./promotion-detail/promotion-detail.component').then(m => m.PromotionDetailComponent)
  }
];
