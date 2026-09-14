import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  readonly expanded = signal(true);
  readonly mobileOpen = signal(false);

  toggle(): void {
    if (window.innerWidth < 900) {
      this.mobileOpen.update((open) => !open);
      return;
    }

    this.expanded.update((expanded) => !expanded);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }
}
