import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly storageKey = 'worktracker-sidebar-expanded';
  readonly expanded = signal(this.readExpandedPreference());
  readonly mobileOpen = signal(false);

  toggle(): void {
    if (window.innerWidth < 900) {
      this.mobileOpen.update((open) => !open);
      return;
    }

    const expanded = !this.expanded();
    this.expanded.set(expanded);
    try {
      window.localStorage.setItem(this.storageKey, String(expanded));
    } catch {
      // The sidebar still works when browser storage is unavailable.
    }
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  private readExpandedPreference(): boolean {
    try {
      return typeof window === 'undefined'
        ? true
        : window.localStorage.getItem(this.storageKey) !== 'false';
    } catch {
      return true;
    }
  }
}
