import { Component, ElementRef, NgZone, AfterViewInit, ViewChild, inject, input, output, signal } from '@angular/core';
import { environment } from '../../../environments/environment';

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
          theme: 'outline';
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

function loadGoogleIdentityServices(): Promise<GoogleIdentityApi> {
  if (window.google?.accounts?.id) {
    return Promise.resolve(window.google);
  }

  if (googleScriptPromise) {
    return googleScriptPromise;
  }

  googleScriptPromise = new Promise<GoogleIdentityApi>((resolve, reject) => {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client?hl=pt';
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
  styles: [':host { display: block; }'],
  template: `
    <div #buttonHost class="google-sign-in-button"></div>
    @if (errorMessage()) {
      <p class="mt-2 text-center text-sm text-error-500" role="alert">{{ errorMessage() }}</p>
    }
  `,
})
export class GoogleSignInButton implements AfterViewInit {
  readonly text = input<GoogleButtonText>('signin_with');
  readonly credential = output<string>();

  protected readonly errorMessage = signal<string | null>(null);

  @ViewChild('buttonHost', { static: true })
  private buttonHost!: ElementRef<HTMLDivElement>;

  private readonly zone = inject(NgZone);

  ngAfterViewInit(): void {
    if (!environment.googleClientId) {
      this.errorMessage.set('A autenticação Google ainda não está configurada.');
      return;
    }

    void loadGoogleIdentityServices()
      .then((google) => {
        google.accounts.id.initialize({
          client_id: environment.googleClientId,
          callback: (response) => {
            if (response.credential) {
              this.zone.run(() => this.credential.emit(response.credential));
            }
          },
        });

        google.accounts.id.renderButton(this.buttonHost.nativeElement, {
          theme: 'outline',
          size: 'large',
          text: this.text(),
          shape: 'rectangular',
          width: Math.max(240, Math.floor(this.buttonHost.nativeElement.clientWidth)),
          locale: 'pt',
        });
      })
      .catch(() => {
        this.errorMessage.set('Não foi possível carregar a autenticação Google.');
      });
  }
}
