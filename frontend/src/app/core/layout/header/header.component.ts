import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../../features/auth/auth.service';
import { environment } from '../../../../environments/environment';
import { AuthenticatedImage } from '../../../shared/ui/authenticated-image/authenticated-image.component';
import { ThemeToggle } from '../../../shared/ui/theme-toggle/theme-toggle.component';
import { ThemeService } from '../../../core/theme/theme.service';
import { SidebarService } from '../sidebar/sidebar.service';
import { LanguageToggle } from '../../../shared/ui/language-toggle/language-toggle.component';
import { TranslatePipe } from '../../i18n/translate.pipe';

@Component({
  imports: [ThemeToggle, LanguageToggle, AuthenticatedImage, RouterLink, TranslatePipe],
  selector: 'app-header',
  styleUrl: './header.component.scss',
  templateUrl: './header.component.html',
})
export class Header {
  protected readonly sidebar = inject(SidebarService);
  protected readonly auth = inject(Auth);
  protected readonly router = inject(Router);
  protected readonly theme = inject(ThemeService);
  protected readonly userMenuOpen = signal(false);
  protected readonly profileImageSource = computed(() => {
    const user = this.auth.currentUser();
    return user?.profileImageUrl
      ? `${environment.apiUrl}/users/me/image?v=${this.auth.profileImageRevision()}`
      : null;
  });

  protected toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.userMenuOpen.update((open) => !open);
  }

  protected closeUserMenu(): void { this.userMenuOpen.set(false); }

  protected toggleTheme(): void {
    this.theme.toggle();
    this.closeUserMenu();
  }

  protected logout(): void {
    this.closeUserMenu();
    this.auth.logout().subscribe({
      next: () => void this.router.navigateByUrl('/signin'),
      error: () => void this.router.navigateByUrl('/signin'),
    });
  }
}
