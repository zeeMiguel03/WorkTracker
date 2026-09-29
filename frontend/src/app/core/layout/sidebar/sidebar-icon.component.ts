import { Component, input } from '@angular/core';

export type SidebarIconName = 'dashboard' | 'products' | 'orders' | 'sources' | 'tasks' | 'profile';

@Component({
  selector: 'app-sidebar-icon',
  template: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      @switch (name()) {
        @case ('dashboard') {
          <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
          <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
        }
        @case ('products') {
          <path d="m4 7 8-4 8 4-8 4-8-4Z"/><path d="m4 7 8 4 8-4v10l-8 4-8-4V7Z"/><path d="M12 11v10"/>
        }
        @case ('orders') {
          <path d="M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Z"/><path d="m4 7.5 8 4.5 8-4.5M12 12v9M8 5.25l8 4.5"/>
        }
        @case ('sources') {
          <path d="M4 8.5h16M6 4h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Z"/>
          <path d="M8 4v4m8-4v4M8 13h3m2 0h3m-8 3h3m2 0h3"/>
        }
        @case ('tasks') {
          <rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 3v3h6V3M9 11l1.5 1.5L13 10m-4 5h6"/>
        }
        @case ('profile') {
          <circle cx="12" cy="12" r="9"/><circle cx="12" cy="9" r="3"/><path d="M6.5 19a6 6 0 0 1 11 0"/>
        }
      }
    </svg>
  `,
  styles: [`
    :host {
      display: inline-flex;
      width: 21px;
      height: 21px;
      flex: 0 0 21px;
    }

    svg {
      width: 100%;
      height: 100%;
      fill: none;
      stroke: #98a2b3;
      stroke-linecap: round;
      stroke-linejoin: round;
      stroke-width: 1.5;
    }
  `],
})
export class SidebarIcon {
  readonly name = input.required<SidebarIconName>();
}
