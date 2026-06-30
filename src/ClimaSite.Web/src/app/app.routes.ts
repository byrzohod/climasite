import { Routes } from '@angular/router';
import { authGuard, adminGuard, guestGuard } from './auth/guards/auth.guard';
import { LEGAL_ROUTES } from './features/legal/legal.routes';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home-v3/home-v3.component').then(m => m.HomeV3Component),
    data: { animation: 'home', seo: { titleKey: 'seo.home.title', descriptionKey: 'seo.home.description' } }
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/login/login.component').then(m => m.LoginComponent),
    data: { animation: 'login', seo: { titleKey: 'seo.login.title', descriptionKey: 'seo.login.description', robots: 'noindex,follow' } }
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/register/register.component').then(m => m.RegisterComponent),
    data: { animation: 'register', seo: { titleKey: 'seo.register.title', descriptionKey: 'seo.register.description', robots: 'noindex,follow' } }
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
    data: { animation: 'forgot-password', seo: { titleKey: 'seo.forgotPassword.title', descriptionKey: 'seo.forgotPassword.description', robots: 'noindex,follow' } }
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
    data: { animation: 'reset-password', seo: { titleKey: 'seo.resetPassword.title', descriptionKey: 'seo.resetPassword.description', robots: 'noindex,follow' } }
  },
  {
    path: 'products',
    data: { animation: 'products' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        data: { animation: 'product-list', seo: { titleKey: 'seo.products.title', descriptionKey: 'seo.products.description' } }
      },
      // NAV-001 FIX: Add category route for filtering products by category
      {
        path: 'category/:categorySlug',
        loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent),
        data: { animation: 'product-category', seo: { titleKey: 'seo.products.title', descriptionKey: 'seo.products.description' } }
      },
      {
        path: ':slug',
        loadComponent: () => import('./features/products/product-detail/product-detail.component').then(m => m.ProductDetailComponent),
        data: { animation: 'product-detail', seo: { titleKey: 'seo.product.title', descriptionKey: 'seo.product.description' } }
      }
    ]
  },
  {
    path: 'categories',
    loadComponent: () => import('./features/categories/category-list/category-list.component').then(m => m.CategoryListComponent),
    data: { animation: 'categories', seo: { titleKey: 'seo.categories.title', descriptionKey: 'seo.categories.description' } }
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent),
    data: { animation: 'cart', seo: { titleKey: 'seo.cart.title', descriptionKey: 'seo.cart.description', robots: 'noindex,follow' } }
  },
  // NAV-002: Add wishlist route
  {
    path: 'wishlist',
    loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent),
    data: { animation: 'wishlist', seo: { titleKey: 'seo.wishlist.title', descriptionKey: 'seo.wishlist.description', robots: 'noindex,follow' } }
  },
  {
    path: 'wishlist/shared/:shareToken',
    loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent),
    data: { animation: 'wishlist-shared', seo: { titleKey: 'seo.wishlistShared.title', descriptionKey: 'seo.wishlistShared.description', robots: 'noindex,follow' } }
  },
  {
    path: 'checkout',
    children: [
      {
        // GAP-07: guest checkout is enabled — no auth guard. The checkout flow uses the guest
        // cart session for anonymous users and issues a token-protected confirmation.
        path: '',
        loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent),
        data: { animation: 'checkout', seo: { titleKey: 'seo.checkout.title', descriptionKey: 'seo.checkout.description', robots: 'noindex,follow' } }
      },
      {
        path: 'confirmation/:orderId',
        loadComponent: () => import('./features/checkout/order-confirmation/order-confirmation.component').then(m => m.OrderConfirmationComponent),
        data: { animation: 'order-confirmation', seo: { titleKey: 'seo.checkoutConfirmation.title', descriptionKey: 'seo.checkoutConfirmation.description', robots: 'noindex,follow' } }
      }
    ]
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () => import('./features/account/account.routes').then(m => m.accountRoutes),
    data: { animation: 'account', seo: { titleKey: 'seo.account.title', descriptionKey: 'seo.account.description', robots: 'noindex,follow' } }
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
    data: { animation: 'admin', seo: { titleKey: 'seo.admin.title', descriptionKey: 'seo.admin.description', robots: 'noindex,follow' } }
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
    data: { animation: 'contact', seo: { titleKey: 'seo.contact.title', descriptionKey: 'seo.contact.description' } }
  },
  {
    path: 'about',
    loadComponent: () => import('./features/about/about.component').then(m => m.AboutComponent),
    data: { animation: 'about', seo: { titleKey: 'seo.about.title', descriptionKey: 'seo.about.description' } }
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
    data: { animation: 'resources', seo: { titleKey: 'seo.resources.title', descriptionKey: 'seo.resources.description' } }
  },
  // GAP-04: Legal & support pages (terms, privacy, cookies, returns, shipping, impressum, faq)
  // registered at the root so the footer's absolute-path links resolve.
  ...LEGAL_ROUTES,
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
    data: { animation: 'not-found', seo: { titleKey: 'seo.notFound.title', descriptionKey: 'seo.notFound.description', robots: 'noindex,follow' } }
  }
];
