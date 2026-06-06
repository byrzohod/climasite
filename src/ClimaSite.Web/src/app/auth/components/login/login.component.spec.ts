import { WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { LoginComponent } from './login.component';

const translations: Record<string, string> = {
  'auth.login.title': 'Log in',
  'auth.login.subtitle': 'Access your account',
  'auth.login.email': 'Email',
  'auth.login.password': 'Password',
  'auth.login.rememberMe': 'Remember me',
  'auth.login.forgotPassword': 'Forgot password?',
  'auth.login.submit': 'Log in',
  'auth.login.noAccount': 'No account?',
  'auth.login.signUp': 'Sign up',
  'auth.login.error': 'Localized login failure',
  'auth.validation.emailRequired': 'Email is required',
  'auth.validation.emailInvalid': 'Enter a valid email address',
  'auth.validation.passwordRequired': 'Password is required'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authService: jasmine.SpyObj<Pick<AuthService, 'login'>> & { isLoading: WritableSignal<boolean> };

  beforeEach(async () => {
    authService = jasmine.createSpyObj<Pick<AuthService, 'login'>>('AuthService', ['login']) as jasmine.SpyObj<Pick<AuthService, 'login'>> & {
      isLoading: WritableSignal<boolean>;
    };
    authService.isLoading = signal(false);

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParams: {}
            }
          }
        }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the translated fallback login error for raw API messages', fakeAsync(() => {
    authService.login.and.returnValue(throwError(() => ({ error: { message: 'Raw login failure' } })));
    component.loginForm.setValue({
      email: 'user@example.com',
      password: 'bad-password',
      rememberMe: false
    });

    component.onSubmit();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="login-error"]') as HTMLElement;
    expect(component.errorMessage()).toBe('auth.login.error');
    expect(error.textContent).toContain('Localized login failure');
    expect(error.textContent).not.toContain('Raw login failure');
    expect(error.textContent).not.toContain('Login failed. Please try again.');
  }));
});
