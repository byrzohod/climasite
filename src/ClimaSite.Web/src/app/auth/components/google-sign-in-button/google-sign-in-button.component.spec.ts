import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { GoogleSignInButtonComponent } from './google-sign-in-button.component';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of({});
  }
}

interface CapturedInit {
  client_id: string;
  callback: (response: { credential: string }) => void;
}

describe('GoogleSignInButtonComponent', () => {
  let fixture: ComponentFixture<GoogleSignInButtonComponent>;
  let authService: jasmine.SpyObj<Pick<AuthService, 'getAuthConfig' | 'googleSignIn'>>;
  let router: jasmine.SpyObj<Router>;

  async function setup(googleClientId: string): Promise<void> {
    authService = jasmine.createSpyObj<Pick<AuthService, 'getAuthConfig' | 'googleSignIn'>>(
      'AuthService',
      ['getAuthConfig', 'googleSignIn']
    );
    authService.getAuthConfig.and.returnValue(of({ googleClientId }));
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);

    await TestBed.configureTestingModule({
      imports: [
        GoogleSignInButtonComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams: {} } } }
      ]
    }).compileComponents();

    TestBed.inject(TranslateService).use('en');
    fixture = TestBed.createComponent(GoogleSignInButtonComponent);
  }

  afterEach(() => {
    delete (window as { google?: unknown }).google;
  });

  it('stays hidden when Google sign-in is not configured', fakeAsync(async () => {
    await setup('');
    fixture.detectChanges();
    tick();

    expect(authService.getAuthConfig).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.clientId()).toBeNull();

    const wrapper = fixture.nativeElement.querySelector('[data-testid="google-auth"]') as HTMLElement;
    expect(wrapper.hidden).toBeTrue();
  }));

  it('renders the GSI button and signs in on the credential callback when configured', fakeAsync(async () => {
    await setup('test-client-id.apps.googleusercontent.com');

    // Fake the GSI global so no real script is loaded and we can capture the callback.
    let captured: CapturedInit | undefined;
    const renderButton = jasmine.createSpy('renderButton');
    (window as { google?: unknown }).google = {
      accounts: {
        id: {
          initialize: (config: CapturedInit) => (captured = config),
          renderButton
        }
      }
    };

    authService.googleSignIn.and.returnValue(of({ accessToken: 't', user: {} as never }));

    fixture.detectChanges();
    tick(); // flush the ensureGsiScript() microtask -> renderButton()

    expect(fixture.componentInstance.clientId()).toBe('test-client-id.apps.googleusercontent.com');
    const wrapper = fixture.nativeElement.querySelector('[data-testid="google-auth"]') as HTMLElement;
    expect(wrapper.hidden).toBeFalse();

    // The official button was painted into the host container.
    expect(renderButton).toHaveBeenCalledTimes(1);
    expect(captured?.client_id).toBe('test-client-id.apps.googleusercontent.com');

    // Simulating Google's credential callback drives the app sign-in + navigation.
    captured!.callback({ credential: 'google-id-token' });
    tick();

    expect(authService.googleSignIn).toHaveBeenCalledOnceWith('google-id-token');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  }));
});
