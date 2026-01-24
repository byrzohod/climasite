import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject, Injector, runInInjectionContext } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Use Injector to lazily get AuthService to avoid circular dependency
  // AuthService -> CartService -> HTTP -> authInterceptor -> AuthService
  const injector = inject(Injector);
  
  // Lazy getter to avoid circular dependency at initialization time
  const getAuthService = () => injector.get(AuthService);

  // Skip auth header for auth endpoints (except me, change-password)
  // Important: Include /auth/logout to prevent infinite loop when logout is called after token refresh fails
  const noAuthEndpoints = ['/auth/login', '/auth/register', '/auth/forgot-password', '/auth/reset-password', '/auth/confirm-email', '/auth/refresh', '/auth/logout'];
  const isNoAuthEndpoint = noAuthEndpoints.some(endpoint => req.url.includes(endpoint));

  if (isNoAuthEndpoint) {
    return next(req);
  }

  const authService = getAuthService();
  const token = authService.accessToken;

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token) {
        // Try to refresh the token
        const authSvc = getAuthService();
        return authSvc.refreshToken().pipe(
          switchMap(() => {
            const newToken = authSvc.accessToken;
            const clonedReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`
              }
            });
            return next(clonedReq);
          }),
          catchError((refreshError) => {
            // Refresh failed - clear auth state but don't navigate
            // This allows components to handle the error gracefully
            // and show a "session expired" message
            authSvc.clearAuthState();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
