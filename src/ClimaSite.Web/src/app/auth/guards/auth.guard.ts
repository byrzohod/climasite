import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * AUTH-001 FIX: Auth guard that waits for auth initialization before checking
 * This prevents the issue where users get logged out when navigating to protected routes
 * on page refresh because the guard runs before the auth state is restored.
 */
export const authGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for auth to be ready before checking authentication
  return toObservable(authService.authReady).pipe(
    filter(ready => ready === true),
    take(1),
    map(() => {
      if (authService.isAuthenticated()) {
        return true;
      }

      // Store return URL and redirect to login
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url }
      });
    })
  );
};

export const adminGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for auth to be ready before checking admin status
  return toObservable(authService.authReady).pipe(
    filter(ready => ready === true),
    take(1),
    map(() => {
      if (authService.isAuthenticated() && authService.isAdmin()) {
        return true;
      }

      if (authService.isAuthenticated()) {
        return router.createUrlTree(['/']);
      }

      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url }
      });
    })
  );
};

export const guestGuard: CanActivateFn = (route, state): Observable<boolean | UrlTree> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for auth to be ready before checking guest status
  return toObservable(authService.authReady).pipe(
    filter(ready => ready === true),
    take(1),
    map(() => {
      if (!authService.isAuthenticated()) {
        return true;
      }

      return router.createUrlTree(['/']);
    })
  );
};
