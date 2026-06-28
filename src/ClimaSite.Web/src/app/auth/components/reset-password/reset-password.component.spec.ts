import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ResetPasswordComponent } from './reset-password.component';

const translations: Record<string, string> = {
  'auth.resetPassword.title': 'Reset password',
  'auth.resetPassword.newPassword': 'New password',
  'auth.resetPassword.confirmNewPassword': 'Confirm new password',
  'auth.resetPassword.reset': 'Reset',
  'auth.resetPassword.success': 'Your password has been reset.',
  'auth.resetPassword.error': 'Could not reset your password.',
  'auth.resetPassword.invalidToken': 'This reset link is invalid or expired.',
  'auth.forgotPassword.backToLogin': 'Back to login',
  'errors.passwordMatch': 'Passwords do not match'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('ResetPasswordComponent', () => {
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let component: ResetPasswordComponent;
  let authService: jasmine.SpyObj<Pick<AuthService, 'resetPassword'>>;
  let router: Router;
  let routeParams: Record<string, string>;

  function createComponent(): void {
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // triggers ngOnInit
  }

  beforeEach(async () => {
    routeParams = { token: 'tok-123', email: 'user@example.com' };
    authService = jasmine.createSpyObj<Pick<AuthService, 'resetPassword'>>('AuthService', ['resetPassword']);

    await TestBed.configureTestingModule({
      imports: [
        ResetPasswordComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams: routeParams } } }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    router = TestBed.inject(Router);
  });

  describe('ngOnInit query-param guards', () => {
    it('redirects to /forgot-password when the token is missing', () => {
      routeParams['token'] = '';
      const navigateSpy = spyOn(router, 'navigate');

      createComponent();

      expect(navigateSpy).toHaveBeenCalledWith(['/forgot-password']);
    });

    it('redirects to /forgot-password when the email is missing', () => {
      routeParams['email'] = '';
      const navigateSpy = spyOn(router, 'navigate');

      createComponent();

      expect(navigateSpy).toHaveBeenCalledWith(['/forgot-password']);
    });

    it('does not redirect when both token and email are present', () => {
      const navigateSpy = spyOn(router, 'navigate');

      createComponent();

      expect(navigateSpy).not.toHaveBeenCalled();
    });
  });

  describe('password match validation', () => {
    beforeEach(() => createComponent());

    it('flags passwordMismatch when the two fields differ', () => {
      component.form.get('newPassword')?.setValue('password123');
      component.form.get('confirmPassword')?.setValue('different123');

      expect(component.form.errors?.['passwordMismatch']).toBeTrue();
    });

    it('clears passwordMismatch when the two fields match', () => {
      component.form.get('newPassword')?.setValue('password123');
      component.form.get('confirmPassword')?.setValue('password123');

      expect(component.form.errors?.['passwordMismatch']).toBeUndefined();
    });

    it('renders the mismatch field-error in the DOM', () => {
      component.form.get('newPassword')?.setValue('password123');
      component.form.get('confirmPassword')?.setValue('nope123456');
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('.field-error') as HTMLElement;
      expect(error).toBeTruthy();
      expect(error.textContent).toContain(translations['errors.passwordMatch']);
    });

    it('enforces the 8-character minimum on the new password', () => {
      component.form.get('newPassword')?.setValue('short');

      expect(component.form.get('newPassword')?.hasError('minlength')).toBeTrue();
    });
  });

  describe('onSubmit', () => {
    beforeEach(() => createComponent());

    function fillValidForm(): void {
      component.form.get('newPassword')?.setValue('password123');
      component.form.get('confirmPassword')?.setValue('password123');
    }

    it('does not call the service when the form is invalid', () => {
      component.onSubmit();
      expect(authService.resetPassword).not.toHaveBeenCalled();
    });

    it('calls resetPassword with token, email and the new password', fakeAsync(() => {
      authService.resetPassword.and.returnValue(of({}));
      // The success path schedules setTimeout(navigate(['/login']), 3000); stub navigate so it
      // is a deterministic no-op (no real route matching against the empty test router).
      spyOn(router, 'navigate');
      fillValidForm();

      component.onSubmit();
      tick();

      expect(authService.resetPassword).toHaveBeenCalledWith('tok-123', 'user@example.com', 'password123');

      tick(3000); // drain the post-success redirect timer
    }));

    it('sets the success message and redirects to /login after 3s on success', fakeAsync(() => {
      authService.resetPassword.and.returnValue(of({}));
      const navigateSpy = spyOn(router, 'navigate');
      fillValidForm();

      component.onSubmit();
      tick();

      expect(component.successMessage()).toBe(translations['auth.resetPassword.success']);
      expect(navigateSpy).not.toHaveBeenCalled();

      tick(3000);

      expect(navigateSpy).toHaveBeenCalledWith(['/login']);
    }));

    it('falls back to the generic error key for a non-translation-key API message', fakeAsync(() => {
      authService.resetPassword.and.returnValue(throwError(() => ({ error: { message: 'raw failure text' } })));
      fillValidForm();

      component.onSubmit();
      tick();

      expect(component.errorMessage()).toBe(translations['auth.resetPassword.error']);
      expect(component.isLoading()).toBeFalse();
    }));

    it('uses a translation-key API message when the backend returns one', fakeAsync(() => {
      authService.resetPassword.and.returnValue(
        throwError(() => ({ error: { message: 'auth.resetPassword.invalidToken' } }))
      );
      fillValidForm();

      component.onSubmit();
      tick();

      expect(component.errorMessage()).toBe(translations['auth.resetPassword.invalidToken']);
    }));
  });
});
