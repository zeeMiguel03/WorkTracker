import { Component, inject } from '@angular/core';
import { ThemeService } from '../../../core/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  template: `
    <button
      class="theme-toggle"
      type="button"
      [attr.aria-label]="theme.isLight() ? 'Mudar para modo escuro' : 'Mudar para modo claro'"
      [attr.title]="theme.isLight() ? 'Mudar para modo escuro' : 'Mudar para modo claro'"
      (click)="theme.toggle()"
    >
      @if (theme.isLight()) {
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M21 12.8A8.5 8.5 0 1 1 11.2 3 6.7 6.7 0 0 0 21 12.8Z" />
        </svg>
      } @else {
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32 1.41 1.41M2 12h2m16 0h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
        </svg>
      }
    </button>
  `,
  styles: [`
    :host { display: inline-flex; }
    .theme-toggle {
      width: 44px;
      height: 44px;
      display: grid;
      place-items: center;
      color: var(--app-text-muted);
      background: transparent;
      border: 1px solid var(--app-border);
      border-radius: 50%;
      cursor: pointer;
      transition: color .15s ease, background .15s ease, border-color .15s ease;
    }
    .theme-toggle:hover { color: var(--app-text); background: var(--app-surface-muted); border-color: var(--app-border-strong); }
    .theme-toggle svg { width: 20px; height: 20px; fill: none; stroke: currentColor; stroke-linecap: round; stroke-linejoin: round; stroke-width: 1.6; }
  `],
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);
}
