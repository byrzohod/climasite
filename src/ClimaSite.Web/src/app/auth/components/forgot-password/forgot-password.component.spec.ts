import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ForgotPasswordComponent } from './forgot-password.component';

const translations: Record<string, string> = {
  'auth.forgotPassword.title': 'Forgot password',
  'auth.forgotPassword.subtitle': 'Reset your password',
  'auth.forgotPassword.email': 'Email',
  'auth.forgotPassword.submit': 'Send reset link',
  'auth.forgotPassword.backToLogin': 'Back to login',
  'auth.forgotPassword.success': 'If that email exists, a reset link was sent.',
  'auth.forgotPassword.error': 'Something went wrong. Please try again.'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('ForgotPasswordComponent', () => {
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let component: ForgotPasswordComponent;
  let authService: jasmine.SpyObj<Pick<AuthService, 'forgotPassword'>>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<Pick<AuthService, 'forgotPassword'>>('AuthService', ['forgotPassword']);

    await TestBed.configureTestingModule({
      imports: [
        ForgotPasswordComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('form validation', () => {
    it('is invalid when the email is empty', () => {
      expect(component.form.invalid).toBeTrue();
      expect(component.form.get('email')?.hasError('required')).toBeTrue();
    });

    it('is invalid for a malformed email', () => {
      component.form.get('email')?.setValue('not-an-email');
      expect(component.form.get('email')?.hasError('email')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('is valid for a well-formed email', () => {
      component.form.get('email')?.setValue('user@example.com');
      expect(component.form.valid).toBeTrue();
    });

    it('disables the submit button while the form is invalid', () => {
      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.disabled).toBeTrue();

      component.form.get('email')?.setValue('user@example.com');
      fixture.detectChanges();

      expect(button.disabled).toBeFalse();
    });
  });

  describe('onSubmit', () => {
    it('does not call the service when the form is invalid', () => {
      component.onSubmit();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });

    it('calls forgotPassword with the entered email', fakeAsync(() => {
      authService.forgotPassword.and.returnValue(of({}));
      component.form.get('email')?.setValue('user@example.com');

      component.onSubmit();
      tick();

      expect(authService.forgotPassword).toHaveBeenCalledWith('user@example.com');
    }));

    it('toggles isLoading true during the request and false after completion', fakeAsync(() => {
      const gate = new Subject<object>();
      authService.forgotPassword.and.returnValue(gate.asObservable());
      component.form.get('email')?.setValue('user@example.com');

      component.onSubmit();
      expect(component.isLoading()).toBeTrue();

      gate.next({});
      gate.complete();
      tick();

      expect(component.isLoading()).toBeFalse();
    }));

    it('shows the translated success message on success', fakeAsync(() => {
      authService.forgotPassword.and.returnValue(of({}));
      component.form.get('email')?.setValue('user@example.com');

      component.onSubmit();
      tick();
      fixture.detectChanges();

      expect(component.successMessage()).toBe(translations['auth.forgotPassword.success']);
      expect(component.errorMessage()).toBeNull();

      const alert = fixture.nativeElement.querySelector('.success-alert') as HTMLElement;
      expect(alert).toBeTruthy();
      expect(alert.textContent).toContain(translations['auth.forgotPassword.success']);
    }));

    it('shows the translated error message on a network/server failure', fakeAsync(() => {
      authService.forgotPassword.and.returnValue(throwError(() => new Error('network down')));
      component.form.get('email')?.setValue('user@example.com');

      component.onSubmit();
      tick();
      fixture.detectChanges();

      expect(component.errorMessage()).toBe(translations['auth.forgotPassword.error']);
      expect(component.successMessage()).toBeNull();
      expect(component.isLoading()).toBeFalse();

      const alert = fixture.nativeElement.querySelector('.error-alert') as HTMLElement;
      expect(alert).toBeTruthy();
      expect(alert.textContent).toContain(translations['auth.forgotPassword.error']);
    }));

    it('clears a previous error before a new submission', fakeAsync(() => {
      component.errorMessage.set('stale error');
      authService.forgotPassword.and.returnValue(of({}));
      component.form.get('email')?.setValue('user@example.com');

      component.onSubmit();
      tick();

      expect(component.errorMessage()).toBeNull();
    }));
  });
});
