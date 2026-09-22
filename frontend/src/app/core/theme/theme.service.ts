import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';

export type AppTheme = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly storageKey = 'worktracker-theme';
  private readonly document = inject(DOCUMENT);
  private readonly currentTheme = signal<AppTheme>(this.readTheme());

  readonly theme = this.currentTheme.asReadonly();
  readonly isLight = computed(() => this.currentTheme() === 'light');

  constructor() {
    this.applyTheme(this.currentTheme());
  }

  toggle(): void {
    this.setTheme(this.currentTheme() === 'dark' ? 'light' : 'dark');
  }

  setTheme(theme: AppTheme): void {
    this.currentTheme.set(theme);
    this.applyTheme(theme);

    try {
      window.localStorage.setItem(ThemeService.storageKey, theme);
    } catch {
      // The visual theme still works when storage is unavailable.
    }
  }

  private readTheme(): AppTheme {
    try {
      return window.localStorage.getItem(ThemeService.storageKey) === 'light'
        ? 'light'
        : 'dark';
    } catch {
      return 'dark';
    }
  }

  private applyTheme(theme: AppTheme): void {
    const root = this.document.documentElement;
    root.classList.toggle('dark', theme === 'dark');
    root.dataset['colorScheme'] = theme;
  }
}
