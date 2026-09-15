import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarService } from './sidebar.service';
import { SidebarIcon, SidebarIconName } from './sidebar-icon.component';

interface SidebarMenuItem {
  readonly label: string;
  readonly route: string;
  readonly icon: SidebarIconName;
  readonly exact?: boolean;
}

interface SidebarMenuSection {
  readonly label: string;
  readonly spaced?: boolean;
  readonly items: readonly SidebarMenuItem[];
}

@Component({
  imports: [RouterLink, RouterLinkActive, SidebarIcon],
  selector: 'app-sidebar',
  styleUrl: './sidebar.component.scss',
  templateUrl: './sidebar.component.html',
})
export class Sidebar {
  protected readonly sidebar = inject(SidebarService);

  protected readonly menuSections: readonly SidebarMenuSection[] = [
    {
      label: 'MENU',
      items: [{ label: 'Dashboard', route: '/dashboard', icon: 'dashboard', exact: true }],
    },
    {
      label: 'FINANÇAS',
      spaced: true,
      items: [
        { label: 'Lançamentos', route: '/entries', icon: 'entries' },
        { label: 'Contas', route: '/accounts', icon: 'accounts' },
      ],
    },
    {
      label: 'TRABALHO',
      spaced: true,
      items: [
        { label: 'Fontes de trabalho', route: '/sources', icon: 'sources' },
        { label: 'Tarefas', route: '/tasks', icon: 'tasks' },
      ],
    },
    {
      label: 'CONTA',
      spaced: true,
      items: [{ label: 'Perfil', route: '/profile', icon: 'profile' }],
    },
  ];
}
