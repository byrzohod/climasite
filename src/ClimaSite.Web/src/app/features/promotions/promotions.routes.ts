import { Routes } from '@angular/router';

export const PROMOTIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./promotions-list/promotions-list.component').then(m => m.PromotionsListComponent),
    data: { seo: { titleKey: 'seo.promotions.title', descriptionKey: 'seo.promotions.description' } }
  },
  {
    path: ':slug',
    loadComponent: () =>
      import('./promotion-detail/promotion-detail.component').then(m => m.PromotionDetailComponent),
    data: { seo: { titleKey: 'seo.promotion.title', descriptionKey: 'seo.promotion.description' } }
  }
];
