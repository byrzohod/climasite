import { Routes } from '@angular/router';
import { authGuard, adminGuard, guestGuard } from './auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
    data: { animation: 'home' }
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/login/login.component').then(m => m.LoginComponent),
    data: { animation: 'login' }
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/register/register.component').then(m => m.RegisterComponent),
    data: { animation: 'register' }
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
    data: { animation: 'forgot-password' }
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
    data: { animation: 'reset-password' }
  },
  {
    path: 'products',
    data: { animation: 'products' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        data: { animation: 'product-list' }
      },
      // NAV-001 FIX: Add category route for filtering products by category
      {
        path: 'category/:categorySlug',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        title: 'Products by Category - ClimaSite',
        data: { animation: 'product-category' }
      },
      {
        path: ':slug',
        loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent),
        data: { animation: 'product-detail' }
      }
    ]
  },
  {
    path: 'categories',
    loadComponent: () => import('./features/categories/category-list/category-list.component').then(m => m.CategoryListComponent),
    data: { animation: 'categories' }
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent),
    data: { animation: 'cart' }
  },
  // NAV-002: Add wishlist route
  {
    path: 'wishlist',
    loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent),
    title: 'Wishlist - ClimaSite',
    data: { animation: 'wishlist' }
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent),
    data: { animation: 'checkout' }
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () => import('./features/account/account.routes').then(m => m.accountRoutes),
    data: { animation: 'account' }
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
    data: { animation: 'admin' }
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
    data: { animation: 'contact' }
  },
  {
    path: 'about',
    loadComponent: () => import('./features/about/about.component').then(m => m.AboutComponent),
    data: { animation: 'about' }
  },
  {
    path: 'promotions',
    loadChildren: () => import('./features/promotions/promotions.routes').then(m => m.PROMOTIONS_ROUTES),
    data: { animation: 'promotions' }
  },
  {
    path: 'brands',
    loadChildren: () => import('./features/brands/brands.routes').then(m => m.BRANDS_ROUTES),
    data: { animation: 'brands' }
  },
  {
    path: 'resources',
    loadComponent: () => import('./features/resources/resources.component').then(m => m.ResourcesComponent),
    data: { animation: 'resources' }
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
    data: { animation: 'not-found' }
  }
];
