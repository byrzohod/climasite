import { Routes } from '@angular/router';

export const accountRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./account-dashboard/account-dashboard.component').then(m => m.AccountDashboardComponent)
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'orders',
    loadComponent: () => import('./orders/orders.component').then(m => m.OrdersComponent)
  },
  {
    path: 'orders/:id',
    loadComponent: () => import('./order-details/order-details.component').then(m => m.OrderDetailsComponent)
  },
  {
    path: 'addresses',
    loadComponent: () => import('./addresses/addresses.component').then(m => m.AddressesComponent)
  }
];
