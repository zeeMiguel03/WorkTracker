import { Component, ElementRef, Injector, NgZone, AfterViewInit, ViewChild, effect, inject, input, output, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

type GoogleButtonText = 'signin_with' | 'signup_with' | 'continue_with';

interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleIdentityApi {
  accounts: {
    id: {
      initialize(options: {
        client_id: string;
        callback: (response: GoogleCredentialResponse) => void;
      }): void;
      renderButton(
        parent: HTMLElement,
        options: {
          theme: 'outline_dark' | 'filled_black';
          size: 'large';
          text: GoogleButtonText;
          shape: 'rectangular';
          width: number;
          locale: string;
        },
      ): void;
    };
  };
}

declare global {
  interface Window {
    google?: GoogleIdentityApi;
  }
}

let googleScriptPromise: Promise<GoogleIdentityApi> | null = null;

function loadGoogleIdentityServices(locale: string): Promise<GoogleIdentityApi> {
  if (window.google?.accounts?.id) {
    return Promise.resolve(window.google);
  }

  if (googleScriptPromise) {
    return googleScriptPromise;
  }

  googleScriptPromise = new Promise<GoogleIdentityApi>((resolve, reject) => {
    const script = document.createElement('script');
    script.src = `https://accounts.google.com/gsi/client?hl=${encodeURIComponent(locale)}`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      if (window.google?.accounts?.id) {
        resolve(window.google);
      } else {
        googleScriptPromise = null;
        reject(new Error('O serviço de autenticação Google não ficou disponível.'));
      }
    };
    script.onerror = () => {
      googleScriptPromise = null;
      reject(new Error('Não foi possível carregar a autenticação Google.'));
    };
    document.head.appendChild(script);
  });

  return googleScriptPromise;
}

@Component({
  selector: 'app-google-sign-in-button',
  styles: [`
    :host { display: block; width: 100%; }
    .google-button-host {
      box-sizing: border-box;
      display: flex;
      width: min(100%, 400px);
      margin-inline: auto;
      justify-content: center;
      overflow: hidden;
      border-radius: 5px;
      background: transparent;
    }
    .google-button__error { margin: 8px 0 0; color: #f97066; text-align: center; font-size: 13px; line-height: 18px; }
  `],
  imports: [TranslatePipe],
  template: `
    <div #buttonHost class="google-button-host"></div>
    @if (errorMessage()) {
      <p class="google-button__error" role="alert">{{ errorMessage() | translate }}</p>
    }
  `,
})
export class GoogleSignInButton implements AfterViewInit {
  readonly text = input<GoogleButtonText>('signin_with');
  readonly credential = output<string>();

  protected readonly errorMessage = signal<string | null>(null);

  private readonly zone = inject(NgZone);
  private readonly language = inject(LanguageService);
  private readonly injector = inject(Injector);

  @ViewChild('buttonHost', { static: true })
  private buttonHost!: ElementRef<HTMLDivElement>;

  ngAfterViewInit(): void {
    effect(() => {
      const locale = this.language.locale();
      if (!environment.googleClientId) {
        this.errorMessage.set('A autenticação Google ainda não está configurada.');
        return;
      }

      void loadGoogleIdentityServices(locale)
        .then((google) => {
          if (this.language.locale() !== locale) return;
          google.accounts.id.initialize({
            client_id: environment.googleClientId,
            callback: (response) => {
              if (response.credential) {
                this.zone.run(() => this.credential.emit(response.credential));
              }
            },
          });
          this.buttonHost.nativeElement.replaceChildren();
          google.accounts.id.renderButton(this.buttonHost.nativeElement, {
            theme: 'filled_black',
            size: 'large',
            text: this.text(),
            shape: 'rectangular',
            width: Math.min(400, Math.max(240, Math.floor(this.buttonHost.nativeElement.clientWidth))),
            locale,
          });
        })
        .catch(() => {
          this.errorMessage.set('Não foi possível carregar a autenticação Google.');
        });
    }, { injector: this.injector });
  }
}
