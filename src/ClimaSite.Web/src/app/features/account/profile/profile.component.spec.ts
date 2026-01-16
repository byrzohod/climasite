import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { TranslateModule } from '@ngx-translate/core';
import { ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../auth/services/auth.service';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

describe('ProfileComponent', () => {
  let component: ProfileComponent;
  let fixture: ComponentFixture<ProfileComponent>;
  let authServiceMock: jasmine.SpyObj<AuthService>;

  const mockUser = {
    id: '1',
    email: 'test@example.com',
    firstName: 'John',
    lastName: 'Doe',
    phone: '+1234567890',
    emailConfirmed: true,
    role: 'User',
    preferredLanguage: 'en',
    preferredCurrency: 'EUR',
    createdAt: '2024-01-01'
  };

  beforeEach(async () => {
    authServiceMock = jasmine.createSpyObj('AuthService', ['updateProfile', 'changePassword'], {
      user: signal(mockUser)
    });
    authServiceMock.updateProfile.and.returnValue(of(mockUser));
    authServiceMock.changePassword.and.returnValue(of({}));

    await TestBed.configureTestingModule({
      imports: [
        ProfileComponent,
        TranslateModule.forRoot(),
        ReactiveFormsModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize profile form with user data', () => {
    expect(component.profileForm.get('firstName')?.value).toBe('John');
    expect(component.profileForm.get('lastName')?.value).toBe('Doe');
    expect(component.profileForm.get('phone')?.value).toBe('+1234567890');
  });

  it('should initialize preferences form with user preferences', () => {
    expect(component.preferencesForm.get('preferredLanguage')?.value).toBe('en');
    expect(component.preferencesForm.get('preferredCurrency')?.value).toBe('EUR');
  });

  it('should have empty password form initially', () => {
    expect(component.passwordForm.get('currentPassword')?.value).toBe('');
    expect(component.passwordForm.get('newPassword')?.value).toBe('');
    expect(component.passwordForm.get('confirmPassword')?.value).toBe('');
  });

  describe('Profile Update', () => {
    it('should call updateProfile on valid form submission', fakeAsync(() => {
      component.profileForm.patchValue({
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+9876543210'
      });

      component.updateProfile();
      tick();

      expect(authServiceMock.updateProfile).toHaveBeenCalledWith({
        firstName: 'Jane',
        lastName: 'Smith',
        phone: '+9876543210'
      });
    }));

    it('should show success message on successful update', fakeAsync(() => {
      component.updateProfile();
      tick();

      expect(component.profileSuccess()).toBeTrue();
    }));

    it('should show error message on failed update', fakeAsync(() => {
      authServiceMock.updateProfile.and.returnValue(throwError(() => ({ error: { message: 'Update failed' } })));

      component.updateProfile();
      tick();

      expect(component.profileError()).toBe('Update failed');
    }));

    it('should not call updateProfile if form is invalid', () => {
      component.profileForm.patchValue({
        firstName: '',
        lastName: ''
      });

      component.updateProfile();

      expect(authServiceMock.updateProfile).not.toHaveBeenCalled();
    });
  });

  describe('Preferences Update', () => {
    it('should call updateProfile with preferences on submission', fakeAsync(() => {
      component.preferencesForm.patchValue({
        preferredLanguage: 'bg',
        preferredCurrency: 'BGN'
      });

      component.updatePreferences();
      tick();

      expect(authServiceMock.updateProfile).toHaveBeenCalledWith({
        preferredLanguage: 'bg',
        preferredCurrency: 'BGN'
      });
    }));

    it('should apply language change after successful update', fakeAsync(() => {
      component.preferencesForm.patchValue({
        preferredLanguage: 'de',
        preferredCurrency: 'EUR'
      });

      component.updatePreferences();
      tick();

      // Verify updateProfile was called with the language preference
      expect(authServiceMock.updateProfile).toHaveBeenCalledWith(
        jasmine.objectContaining({
          preferredLanguage: 'de'
        })
      );
    }));
  });

  describe('Password Change', () => {
    it('should call changePassword on valid form submission', fakeAsync(() => {
      component.passwordForm.patchValue({
        currentPassword: 'oldpass123',
        newPassword: 'newpass12345',
        confirmPassword: 'newpass12345'
      });

      component.changePassword();
      tick();

      expect(authServiceMock.changePassword).toHaveBeenCalledWith('oldpass123', 'newpass12345');
    }));

    it('should show success message on successful password change', fakeAsync(() => {
      component.passwordForm.patchValue({
        currentPassword: 'oldpass123',
        newPassword: 'newpass12345',
        confirmPassword: 'newpass12345'
      });

      component.changePassword();
      tick();

      expect(component.passwordSuccess()).toBeTrue();
    }));

    it('should reset password form after successful change', fakeAsync(() => {
      component.passwordForm.patchValue({
        currentPassword: 'oldpass123',
        newPassword: 'newpass12345',
        confirmPassword: 'newpass12345'
      });

      component.changePassword();
      tick();

      expect(component.passwordForm.get('currentPassword')?.value).toBeFalsy();
      expect(component.passwordForm.get('newPassword')?.value).toBeFalsy();
      expect(component.passwordForm.get('confirmPassword')?.value).toBeFalsy();
    }));

    it('should show error message on failed password change', fakeAsync(() => {
      authServiceMock.changePassword.and.returnValue(throwError(() => ({ error: { message: 'Incorrect password' } })));

      component.passwordForm.patchValue({
        currentPassword: 'wrongpass',
        newPassword: 'newpass12345',
        confirmPassword: 'newpass12345'
      });

      component.changePassword();
      tick();

      expect(component.passwordError()).toBe('Incorrect password');
    }));

    it('should not call changePassword if form is invalid', () => {
      component.passwordForm.patchValue({
        currentPassword: '',
        newPassword: 'short',
        confirmPassword: 'short'
      });

      component.changePassword();

      expect(authServiceMock.changePassword).not.toHaveBeenCalled();
    });

    it('should show validation error for password mismatch', () => {
      component.passwordForm.patchValue({
        currentPassword: 'oldpass123',
        newPassword: 'newpass12345',
        confirmPassword: 'differentpass'
      });

      expect(component.passwordForm.errors?.['passwordMismatch']).toBeTrue();
    });

    it('should show validation error for short password', () => {
      component.passwordForm.patchValue({
        newPassword: 'short'
      });
      component.passwordForm.get('newPassword')?.markAsTouched();

      expect(component.passwordForm.get('newPassword')?.errors?.['minlength']).toBeTruthy();
    });
  });

  describe('Loading States', () => {
    it('should have isUpdatingProfile signal initialized to false', () => {
      // After completion of any previous calls
      expect(component.isUpdatingProfile()).toBeFalse();
    });

    it('should have isChangingPassword signal initialized to false', () => {
      // After completion of any previous calls
      expect(component.isChangingPassword()).toBeFalse();
    });

    it('should reset isUpdatingProfile after profile update completes', fakeAsync(() => {
      component.updateProfile();
      tick();
      expect(component.isUpdatingProfile()).toBeFalse();
    }));

    it('should reset isChangingPassword after password change completes', fakeAsync(() => {
      component.passwordForm.patchValue({
        currentPassword: 'oldpass123',
        newPassword: 'newpass12345',
        confirmPassword: 'newpass12345'
      });
      component.changePassword();
      tick();
      expect(component.isChangingPassword()).toBeFalse();
    }));
  });
});
