import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  emailConfirmed: boolean;
  role: string;
  preferredLanguage: string;
  preferredCurrency: string;
  createdAt: string;
  lastLoginAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
}

export interface LoginResponse {
  accessToken: string;
  user: User;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  preferredLanguage?: string;
  preferredCurrency?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly apiUrl = environment.apiUrl;
  private readonly TOKEN_KEY = 'climasite_token';

  private readonly _user = signal<User | null>(null);
  private readonly _isLoading = signal(false);

  readonly user = this._user.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  // Use token-based auth check to handle async user loading
  private readonly _hasToken = signal(false);

  // AUTH-001 FIX: Track when initial auth check is complete
  // This allows guards to wait for auth initialization before redirecting
  private readonly _authReady = signal(false);
  readonly authReady = this._authReady.asReadonly();

  readonly isAuthenticated = computed(() => this._hasToken());
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');

  private _accessToken: string | null = null;
  private _isLoggingOut = false;
  private _isRefreshing = false;

  constructor() {
    this.loadUserFromToken();
  }

  get accessToken(): string | null {
    return this._accessToken;
  }

  login(credentials: LoginRequest) {
    this._isLoading.set(true);

    return this.http.post<LoginResponse>(`${this.apiUrl}/api/auth/login`, credentials, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this._accessToken = response.accessToken;
        this._hasToken.set(true);
        this._user.set(response.user);
        this.storeToken(response.accessToken);
        this._isLoading.set(false);
      }),
      catchError((error: HttpErrorResponse) => {
        this._isLoading.set(false);
        return throwError(() => error);
      })
    );
  }

  register(data: RegisterRequest) {
    this._isLoading.set(true);

    return this.http.post<User>(`${this.apiUrl}/api/auth/register`, data).pipe(
      tap(() => {
        this._isLoading.set(false);
      }),
      catchError((error: HttpErrorResponse) => {
        this._isLoading.set(false);
        return throwError(() => error);
      })
    );
  }

  logout() {
    // Prevent multiple logout attempts (guards against infinite loops)
    if (this._isLoggingOut) {
      return throwError(() => new Error('Logout already in progress'));
    }

    this._isLoggingOut = true;

    // Clear auth immediately to prevent further authenticated requests
    this.clearAuth();

    return this.http.post(`${this.apiUrl}/api/auth/logout`, {}, {
      withCredentials: true
    }).pipe(
      tap(() => {
        this._isLoggingOut = false;
        this.router.navigate(['/login']);
      }),
      catchError(() => {
        this._isLoggingOut = false;
        this.router.navigate(['/login']);
        return throwError(() => new Error('Logout failed'));
      })
    );
  }

  refreshToken() {
    // Prevent multiple concurrent refresh attempts
    if (this._isRefreshing) {
      return throwError(() => new Error('Token refresh already in progress'));
    }

    this._isRefreshing = true;

    return this.http.post<{ accessToken: string }>(`${this.apiUrl}/api/auth/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap(response => {
        this._isRefreshing = false;
        this._accessToken = response.accessToken;
        this._hasToken.set(true);
        this.storeToken(response.accessToken);
      }),
      catchError((error: HttpErrorResponse) => {
        this._isRefreshing = false;
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  getCurrentUser() {
    return this.http.get<User>(`${this.apiUrl}/api/auth/me`).pipe(
      tap(user => {
        this._user.set(user);
      })
    );
  }

  updateProfile(data: UpdateProfileRequest) {
    return this.http.put<User>(`${this.apiUrl}/api/auth/me`, data).pipe(
      tap(user => {
        this._user.set(user);
      })
    );
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.put(`${this.apiUrl}/api/auth/change-password`, {
      currentPassword,
      newPassword
    });
  }

  forgotPassword(email: string) {
    return this.http.post(`${this.apiUrl}/api/auth/forgot-password`, { email });
  }

  resetPassword(token: string, email: string, newPassword: string) {
    return this.http.post(`${this.apiUrl}/api/auth/reset-password`, {
      token,
      email,
      newPassword
    });
  }

  confirmEmail(token: string, email: string) {
    return this.http.post(`${this.apiUrl}/api/auth/confirm-email`, {
      token,
      email
    });
  }

  private loadUserFromToken(): void {
    // AUTH-001 FIX: Mark auth as ready immediately for non-browser environments
    if (!this.isBrowser) {
      this._authReady.set(true);
      return;
    }

    const token = localStorage.getItem(this.TOKEN_KEY);
    if (token) {
      this._accessToken = token;
      this._hasToken.set(true);
      // Attempt to fetch user profile, but don't clear auth on failure
      // The token might still be valid even if this request fails (e.g., network issue)
      this.getCurrentUser().subscribe({
        next: (user) => {
          console.log('User profile loaded:', user.email);
          // AUTH-001 FIX: Mark auth as ready after user is loaded
          this._authReady.set(true);
        },
        error: (err) => {
          console.error('Failed to load user profile:', err);
          // Only clear auth if it's a 401 (unauthorized) response
          if (err.status === 401) {
            this.clearAuth();
          }
          // AUTH-001 FIX: Mark auth as ready even on error
          this._authReady.set(true);
        }
      });
    } else {
      // AUTH-001 FIX: No token, mark auth as ready immediately
      this._authReady.set(true);
    }
  }

  private storeToken(token: string): void {
    if (this.isBrowser) {
      localStorage.setItem(this.TOKEN_KEY, token);
    }
  }

  private clearAuth(): void {
    this._accessToken = null;
    this._hasToken.set(false);
    this._user.set(null);
    if (this.isBrowser) {
      localStorage.removeItem(this.TOKEN_KEY);
    }
  }
}
