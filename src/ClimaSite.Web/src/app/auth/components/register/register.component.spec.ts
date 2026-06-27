import { WritableSignal, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { AuthService, User } from '../../services/auth.service';
import { RegisterComponent } from './register.component';

/**
 * Plan-19 B2: unit coverage for the registration reactive form.
 *
 * The component is instantiated inside its injection context WITHOUT rendering the
 * template (the template imports app-input / app-button which expect ngx-translate
 * wiring and reactive-form host bindings we don't want to set up here). We drive the
 * FormGroup and public onSubmit() directly and assert form-validity rules + the
 * success/error submit branches, mirroring the style of checkout.component.spec.ts.
 */
describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let authService: jasmine.SpyObj<Pick<AuthService, 'register'>> & { isLoading: WritableSignal<boolean> };
  let router: jasmine.SpyObj<Router>;
  let translate: jasmine.SpyObj<Pick<TranslateService, 'instant' | 'get'>>;

  const validForm = {
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada@example.com',
    password: 'Sup3rSecret!',
    confirmPassword: 'Sup3rSecret!',
    terms: true
  };

  const mockUser = { id: 'u-1', email: 'ada@example.com', firstName: 'Ada', lastName: 'Lovelace' } as User;

  beforeEach(() => {
    authService = jasmine.createSpyObj<Pick<AuthService, 'register'>>('AuthService', ['register']) as jasmine.SpyObj<
      Pick<AuthService, 'register'>
    > & { isLoading: WritableSignal<boolean> };
    authService.isLoading = signal(false);

    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    translate = jasmine.createSpyObj<Pick<TranslateService, 'instant' | 'get'>>('TranslateService', ['instant', 'get']);
    // instant() echoes the key so we can assert exactly which key was surfaced.
    translate.instant.and.callFake((key: string | string[]) => key as string);
    translate.get.and.callFake((key: string | string[]) => of(key as string));

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: TranslateService, useValue: translate }
      ]
    });

    component = TestBed.runInInjectionContext(() => new RegisterComponent());
  });

  it('creates and starts with an invalid, empty form', () => {
    expect(component).toBeTruthy();
    expect(component.registerForm.invalid).toBeTrue();
    expect(component.errorMessage()).toBeNull();
    expect(component.successMessage()).toBeNull();
  });

  describe('field validation rules', () => {
    it('marks the form valid only when every rule is satisfied', () => {
      component.registerForm.setValue(validForm);
      expect(component.registerForm.valid).toBeTrue();
    });

    it('requires firstName and lastName with a 2-char minimum', () => {
      component.registerForm.patchValue({ ...validForm, firstName: '', lastName: 'X' });
      expect(component.registerForm.get('firstName')?.hasError('required')).toBeTrue();
      expect(component.registerForm.get('lastName')?.hasError('minlength')).toBeTrue();
    });

    it('rejects a malformed email and accepts a well-formed one', () => {
      component.registerForm.patchValue({ ...validForm, email: 'not-an-email' });
      expect(component.registerForm.get('email')?.hasError('email')).toBeTrue();

      component.registerForm.patchValue({ email: 'good@example.com' });
      expect(component.registerForm.get('email')?.valid).toBeTrue();
    });

    it('enforces an 8-character minimum password', () => {
      component.registerForm.patchValue({ ...validForm, password: 'short', confirmPassword: 'short' });
      expect(component.registerForm.get('password')?.hasError('minlength')).toBeTrue();
    });

    it('requires the terms checkbox to be checked (requiredTrue)', () => {
      component.registerForm.patchValue({ ...validForm, terms: false });
      expect(component.registerForm.get('terms')?.hasError('required')).toBeTrue();
      expect(component.registerForm.valid).toBeFalse();
    });
  });

  describe('password-confirm cross-field validator', () => {
    it('flags passwordMismatch when the two passwords differ', () => {
      component.registerForm.patchValue({ ...validForm, password: 'Sup3rSecret!', confirmPassword: 'Different1!' });
      expect(component.registerForm.errors?.['passwordMismatch']).toBeTrue();
      expect(component.registerForm.invalid).toBeTrue();
    });

    it('clears passwordMismatch once the confirmation matches', () => {
      component.registerForm.patchValue({ ...validForm, password: 'Sup3rSecret!', confirmPassword: 'Different1!' });
      expect(component.registerForm.errors?.['passwordMismatch']).toBeTrue();

      component.registerForm.patchValue({ confirmPassword: 'Sup3rSecret!' });
      expect(component.registerForm.errors).toBeNull();
    });

    it('treats two empty passwords as matching at the cross-field level', () => {
      // The required validators still keep the controls invalid, but the group-level
      // passwordMismatch error must not fire when both sides are equal (even when empty).
      expect(component.passwordMatchValidator(component.registerForm)).toBeNull();
    });
  });

  describe('onSubmit', () => {
    it('does nothing when the form is invalid', () => {
      // Form is empty/invalid out of the box.
      component.onSubmit();
      expect(authService.register).not.toHaveBeenCalled();
    });

    it('submits only the four account fields (never confirmPassword/terms) on a valid form', () => {
      authService.register.and.returnValue(of(mockUser));
      component.registerForm.setValue(validForm);

      component.onSubmit();

      expect(authService.register).toHaveBeenCalledOnceWith({
        firstName: 'Ada',
        lastName: 'Lovelace',
        email: 'ada@example.com',
        password: 'Sup3rSecret!'
      });
    });

    it('shows the localized success message and redirects to /login after the delay', fakeAsync(() => {
      authService.register.and.returnValue(of(mockUser));
      component.registerForm.setValue(validForm);

      component.onSubmit();
      // get() resolves synchronously via the of() spy.
      expect(component.successMessage()).toBe('auth.register.success');
      expect(component.errorMessage()).toBeNull();
      // Redirect is deferred 3s.
      expect(router.navigate).not.toHaveBeenCalled();

      tick(3000);
      expect(router.navigate).toHaveBeenCalledOnceWith(['/login']);
    }));

    it('surfaces a translated fallback error key and stays on the page on failure', () => {
      authService.register.and.returnValue(throwError(() => ({ error: { message: 'Raw server boom' } })));
      component.registerForm.setValue(validForm);

      component.onSubmit();

      // apiErrorToTranslationKey() rejects the non-key raw message -> fallback key, run through instant().
      expect(component.errorMessage()).toBe('auth.register.error');
      expect(component.successMessage()).toBeNull();
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('preserves a structured translation key returned by the API', () => {
      authService.register.and.returnValue(throwError(() => ({ error: { message: 'auth.register.emailTaken' } })));
      component.registerForm.setValue(validForm);

      component.onSubmit();

      expect(component.errorMessage()).toBe('auth.register.emailTaken');
    });

    it('clears a prior error before a fresh submit attempt', fakeAsync(() => {
      // First attempt fails.
      authService.register.and.returnValue(throwError(() => ({ error: { message: 'boom' } })));
      component.registerForm.setValue(validForm);
      component.onSubmit();
      expect(component.errorMessage()).toBe('auth.register.error');

      // Second attempt succeeds: the error must be reset to null up-front.
      authService.register.and.returnValue(of(mockUser));
      component.onSubmit();
      expect(component.errorMessage()).toBeNull();
      expect(component.successMessage()).toBe('auth.register.success');
      tick(3000);
    }));
  });
});
