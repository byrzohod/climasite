import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { PLATFORM_ID } from '@angular/core';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { AuthService, LoginResponse, User } from './auth.service';

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  const adminUser: User = {
    id: 'admin-1',
    email: 'admin@test.com',
    firstName: 'Admin',
    lastName: 'User',
    emailConfirmed: true,
    role: 'Admin',
    preferredLanguage: 'en',
    preferredCurrency: 'EUR',
    createdAt: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'browser' },
        // Stub the lazily-resolved merge collaborators so the auth pipeline makes no extra HTTP.
        { provide: CartService, useValue: { mergeCart: () => of(null) } },
        { provide: WishlistService, useValue: { mergeWithUserWishlist: () => of(null) } }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('restores the current admin user from a persisted token', fakeAsync(() => {
    localStorage.setItem('climasite_token', 'stored-admin-token');

    const service = TestBed.inject(AuthService);

    const req = httpMock.expectOne(`${environment.apiUrl}/api/auth/me`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe('Bearer stored-admin-token');

    req.flush(adminUser);
    tick();

    expect(service.authReady()).toBeTrue();
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isAdmin()).toBeTrue();
    expect(service.user()).toEqual(adminUser);
  }));

  it('refreshes an expired persisted token and retries current user restore', fakeAsync(() => {
    localStorage.setItem('climasite_token', 'expired-admin-token');

    const service = TestBed.inject(AuthService);

    const initialReq = httpMock.expectOne(`${environment.apiUrl}/api/auth/me`);
    expect(initialReq.request.headers.get('Authorization')).toBe('Bearer expired-admin-token');
    initialReq.flush({ message: 'Token expired' }, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne(`${environment.apiUrl}/api/auth/refresh`);
    expect(refreshReq.request.method).toBe('POST');
    expect(refreshReq.request.withCredentials).toBeTrue();
    refreshReq.flush({ accessToken: 'refreshed-admin-token' });

    const retryReq = httpMock.expectOne(`${environment.apiUrl}/api/auth/me`);
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer refreshed-admin-token');
    retryReq.flush(adminUser);
    tick();

    expect(localStorage.getItem('climasite_token')).toBe('refreshed-admin-token');
    expect(service.authReady()).toBeTrue();
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.isAdmin()).toBeTrue();
    expect(service.user()).toEqual(adminUser);
  }));

  it('marks auth ready immediately when no persisted token exists', () => {
    const service = TestBed.inject(AuthService);

    expect(service.authReady()).toBeTrue();
    expect(service.isAuthenticated()).toBeFalse();
    httpMock.expectNone(`${environment.apiUrl}/api/auth/me`);
  });

  it('signs in with Google: posts the id token and stores the session', fakeAsync(() => {
    const service = TestBed.inject(AuthService);

    const googleUser: User = {
      ...adminUser,
      id: 'google-1',
      email: 'google.user@test.com',
      role: 'Customer'
    };

    let result: LoginResponse | undefined;
    service.googleSignIn('google-id-token').subscribe(response => (result = response));

    const req = httpMock.expectOne(`${environment.apiUrl}/api/auth/google`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ idToken: 'google-id-token' });
    expect(req.request.withCredentials).toBeTrue();

    req.flush({ accessToken: 'google-access-token', user: googleUser });
    tick();

    expect(result?.accessToken).toBe('google-access-token');
    expect(localStorage.getItem('climasite_token')).toBe('google-access-token');
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.user()).toEqual(googleUser);
  }));

  it('propagates an error and clears loading when Google sign-in is rejected', fakeAsync(() => {
    const service = TestBed.inject(AuthService);

    let errorStatus: number | undefined;
    service.googleSignIn('bad-token').subscribe({
      next: () => fail('expected Google sign-in to fail'),
      error: err => (errorStatus = err.status)
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/auth/google`);
    req.flush({ message: 'Invalid Google credentials' }, { status: 401, statusText: 'Unauthorized' });
    tick();

    expect(errorStatus).toBe(401);
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.isLoading()).toBeFalse();
  }));
});
