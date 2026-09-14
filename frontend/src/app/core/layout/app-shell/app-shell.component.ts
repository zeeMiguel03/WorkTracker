import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from '../header/header.component';
import { Sidebar } from '../sidebar/sidebar.component';
import { SidebarService } from '../sidebar/sidebar.service';

@Component({
  imports: [RouterOutlet, Header, Sidebar],
  selector: 'app-app-shell',
  styleUrl: './app-shell.component.scss',
  templateUrl: './app-shell.component.html',
})
export class AppShell {
  protected readonly sidebar = inject(SidebarService);
}
