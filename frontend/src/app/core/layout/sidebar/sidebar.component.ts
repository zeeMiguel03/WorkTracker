import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarService } from './sidebar.service';
import { SidebarIcon, SidebarIconName } from './sidebar-icon.component';
import { TranslatePipe } from '../../i18n/translate.pipe';

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
  imports: [RouterLink, RouterLinkActive, SidebarIcon, TranslatePipe],
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
        { label: 'Produtos', route: '/products', icon: 'products' },
        { label: 'Encomendas', route: '/orders', icon: 'orders' },
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
