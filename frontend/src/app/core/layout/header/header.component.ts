import { Component, inject } from '@angular/core';
import { Auth } from '../../../features/auth/auth.service';
import { ThemeToggle } from '../../../shared/ui/theme-toggle/theme-toggle.component';
import { SidebarService } from '../sidebar/sidebar.service';

@Component({
  imports: [ThemeToggle],
  selector: 'app-header',
  styleUrl: './header.component.scss',
  templateUrl: './header.component.html',
})
export class Header {
  protected readonly sidebar = inject(SidebarService);
  protected readonly auth = inject(Auth);
}
