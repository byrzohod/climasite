import { Routes } from '@angular/router';
import { authGuard, adminGuard, guestGuard } from './auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },
  {
    path: 'products',
    children: [
      {
        path: '',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent)
      },
      // NAV-001 FIX: Add category route for filtering products by category
      {
        path: 'category/:categorySlug',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        title: 'Products by Category - ClimaSite'
      },
      {
        path: ':slug',
        loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
      }
    ]
  },
  {
    path: 'categories',
    loadComponent: () => import('./features/categories/category-list/category-list.component').then(m => m.CategoryListComponent)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent)
  },
  // NAV-002: Add wishlist route
  {
    path: 'wishlist',
    loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent),
    title: 'Wishlist - ClimaSite'
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent)
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () => import('./features/account/account.routes').then(m => m.accountRoutes)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent)
  },
  {
    path: 'about',
    loadComponent: () => import('./features/about/about.component').then(m => m.AboutComponent)
  },
  {
    path: 'promotions',
    loadChildren: () => import('./features/promotions/promotions.routes').then(m => m.PROMOTIONS_ROUTES)
  },
  {
    path: 'brands',
    loadChildren: () => import('./features/brands/brands.routes').then(m => m.BRANDS_ROUTES)
  },
  {
    path: 'resources',
    loadComponent: () => import('./features/resources/resources.component').then(m => m.ResourcesComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
