import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AuthService, User } from '../../../auth/services/auth.service';
import { AccountDashboardComponent } from './account-dashboard.component';

const translations: Record<string, string> = {
  'account.title': 'My account',
  'account.welcome': 'Welcome, {{name}}',
  'account.profile.title': 'Profile',
  'account.orders.title': 'Orders',
  'account.addresses.title': 'Addresses',
  'accessibility.reduceMotion.label': 'Settings'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

const mockUser: User = {
  id: '1',
  email: 'jane@example.com',
  firstName: 'Jane',
  lastName: 'Doe',
  emailConfirmed: true,
  role: 'User',
  preferredLanguage: 'en',
  preferredCurrency: 'EUR',
  createdAt: '2024-01-01'
};

describe('AccountDashboardComponent', () => {
  let fixture: ComponentFixture<AccountDashboardComponent>;
  let component: AccountDashboardComponent;
  let userSignal: WritableSignal<User | null>;

  beforeEach(async () => {
    userSignal = signal<User | null>(mockUser);
    const authService = jasmine.createSpyObj<AuthService>('AuthService', [], { user: userSignal });

    await TestBed.configureTestingModule({
      imports: [
        AccountDashboardComponent,
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

    fixture = TestBed.createComponent(AccountDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and render the account page container', () => {
    expect(component).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="account-page"]')).toBeTruthy();
  });

  it('renders the welcome message with the full user name when authenticated', () => {
    const welcome = fixture.nativeElement.querySelector('[data-testid="account-welcome"]') as HTMLElement;
    expect(welcome).toBeTruthy();
    expect(welcome.textContent).toContain('Jane Doe');
  });

  it('hides the welcome section when there is no user', () => {
    userSignal.set(null);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="account-welcome"]')).toBeNull();
  });

  it('renders the four account navigation links', () => {
    const links = fixture.nativeElement.querySelectorAll('.account-link');
    expect(links.length).toBe(4);
  });

  it('exposes the settings link with its test id', () => {
    expect(fixture.nativeElement.querySelector('[data-testid="account-settings-link"]')).toBeTruthy();
  });
});
