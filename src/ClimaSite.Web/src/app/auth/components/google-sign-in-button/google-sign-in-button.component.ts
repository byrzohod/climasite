import {
  Component,
  ElementRef,
  NgZone,
  OnInit,
  PLATFORM_ID,
  inject,
  signal
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { apiErrorToTranslationKey } from '../../../core/utils/translation-key.util';

/**
 * Minimal typings for the Google Identity Services (GSI) global, loaded lazily from
 * https://accounts.google.com/gsi/client. Only the surface we use is declared.
 */
interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleIdInitConfig {
  client_id: string;
  callback: (response: GoogleCredentialResponse) => void;
}

interface GoogleButtonOptions {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'small' | 'medium' | 'large';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
}

interface GoogleAccountsId {
  initialize(config: GoogleIdInitConfig): void;
  renderButton(parent: HTMLElement, options: GoogleButtonOptions): void;
}

interface GoogleNamespace {
  accounts: { id: GoogleAccountsId };
}

declare global {
  interface Window {
    google?: GoogleNamespace;
  }
}

const GSI_SCRIPT_ID = 'google-identity-services';
const GSI_SCRIPT_SRC = 'https://accounts.google.com/gsi/client';

/**
 * Renders the official "Sign in with Google" button (GSI ID-token flow) — but ONLY when the backend
 * reports a configured Google client id (`GET /api/auth/config`). On the GSI credential callback it
 * calls {@link AuthService.googleSignIn}, which logs the user in (merging their guest cart + wishlist)
 * and then navigates to `returnUrl` (or home). SSR-safe: does nothing on the server.
 *
 * The wrapper is always present in the DOM but hidden until a client id is known, so the GSI button
 * can be painted into a stable container without change-detection gymnastics.
 *
 * Used by both the login and register pages.
 */
@Component({
  selector: 'app-google-sign-in-button',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="google-auth" [hidden]="!clientId()" data-testid="google-auth">
      <div class="auth-divider">
        <span>{{ 'auth.google.or' | translate }}</span>
      </div>

      <!-- GSI renders the real, accessibly-named button inside this host. aria-label is prohibited on a
           role-less container (axe aria-prohibited-attr), so the host carries none. -->
      <div
        #googleButton
        class="google-button-host"
        data-testid="google-signin-button"
      ></div>

      @if (errorMessage()) {
        <div class="error-alert" data-testid="google-signin-error">
          {{ errorMessage()! | translate }}
        </div>
      }
    </div>
  `,
  styles: [`
    .google-auth {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-top: 1.25rem;
    }

    .auth-divider {
      display: flex;
      align-items: center;
      text-align: center;
      color: var(--color-text-secondary);
      font-size: 0.8125rem;

      &::before,
      &::after {
        content: '';
        flex: 1;
        border-bottom: 1px solid var(--color-border);
      }

      span {
        padding: 0 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
    }

    .google-button-host {
      display: flex;
      justify-content: center;
      min-height: 40px;
    }

    .error-alert {
      background-color: var(--color-error-bg);
      color: var(--color-error);
      padding: 0.75rem 1rem;
      border-radius: 6px;
      font-size: 0.875rem;
      text-align: center;
    }
  `]
})
export class GoogleSignInButtonComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly zone = inject(NgZone);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly clientId = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }

    this.authService.getAuthConfig().subscribe({
      next: config => {
        if (!config.googleClientId) {
          return; // Not configured — keep the button hidden (feature ships dark).
        }
        this.clientId.set(config.googleClientId);
        this.ensureGsiScript()
          .then(() => this.renderButton(config.googleClientId))
          .catch(() => {
            /* GSI script failed to load — leave the divider only; nothing actionable to show. */
          });
      },
      error: () => {
        /* Config endpoint unreachable — keep the button hidden. */
      }
    });
  }

  private renderButton(clientId: string): void {
    const google = window.google;
    const container = this.host.nativeElement.querySelector<HTMLElement>(
      '[data-testid="google-signin-button"]'
    );
    if (!google || !container) {
      return;
    }

    google.accounts.id.initialize({
      client_id: clientId,
      callback: response => this.zone.run(() => this.onCredential(response.credential))
    });

    google.accounts.id.renderButton(container, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'signin_with',
      shape: 'rectangular',
      logo_alignment: 'left'
    });
  }

  private onCredential(idToken: string): void {
    this.errorMessage.set(null);
    this.authService.googleSignIn(idToken).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
        this.router.navigateByUrl(returnUrl);
      },
      error: error => {
        this.errorMessage.set(apiErrorToTranslationKey(error, 'auth.google.error'));
      }
    });
  }

  private ensureGsiScript(): Promise<void> {
    if (!this.isBrowser) {
      return Promise.reject(new Error('not a browser'));
    }
    if (window.google?.accounts?.id) {
      return Promise.resolve();
    }

    const existing = document.getElementById(GSI_SCRIPT_ID) as HTMLScriptElement | null;
    if (existing) {
      if (existing.dataset['loaded'] === 'true') {
        return Promise.resolve();
      }
      return new Promise<void>((resolve, reject) => {
        existing.addEventListener('load', () => resolve());
        existing.addEventListener('error', () => reject(new Error('GSI script failed to load')));
      });
    }

    return new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.id = GSI_SCRIPT_ID;
      script.src = GSI_SCRIPT_SRC;
      script.async = true;
      script.defer = true;
      script.addEventListener('load', () => {
        script.dataset['loaded'] = 'true';
        resolve();
      });
      script.addEventListener('error', () => reject(new Error('GSI script failed to load')));
      document.head.appendChild(script);
    });
  }
}
