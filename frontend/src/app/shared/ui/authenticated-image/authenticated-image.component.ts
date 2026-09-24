import { HttpClient } from '@angular/common/http';
import { Component, effect, inject, input, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-authenticated-image',
  template: `
    @if (imageSrc() && !failed()) {
      <img [src]="imageSrc()!" [alt]="alt()" (error)="handleError()" />
    } @else {
      <ng-content />
    }
  `,
  styles: `
    :host { display: grid; width: 100%; height: 100%; place-items: center; overflow: hidden; border-radius: inherit; }
    img { display: block; width: 100%; height: 100%; object-fit: cover; }
  `,
})
export class AuthenticatedImage implements OnDestroy {
  private readonly http = inject(HttpClient);
  private request: Subscription | null = null;
  private objectUrl: string | null = null;

  readonly src = input<string | null>(null);
  readonly alt = input('');
  protected readonly imageSrc = signal<string | null>(null);
  protected readonly failed = signal(false);

  private readonly sourceEffect = effect(() => this.load(this.src()));

  ngOnDestroy(): void {
    this.sourceEffect.destroy();
    this.request?.unsubscribe();
    this.revokeObjectUrl();
  }

  protected handleError(): void {
    this.failed.set(true);
    this.revokeObjectUrl();
    this.imageSrc.set(null);
  }

  private load(source: string | null): void {
    this.request?.unsubscribe();
    this.revokeObjectUrl();
    this.imageSrc.set(null);
    this.failed.set(false);
    if (!source) return;

    this.request = this.http.get(source, { responseType: 'blob', withCredentials: true }).subscribe({
      next: (blob) => {
        this.objectUrl = URL.createObjectURL(blob);
        this.imageSrc.set(this.objectUrl);
      },
      error: () => this.failed.set(true),
    });
  }

  private revokeObjectUrl(): void {
    if (!this.objectUrl) return;
    URL.revokeObjectURL(this.objectUrl);
    this.objectUrl = null;
  }
}
