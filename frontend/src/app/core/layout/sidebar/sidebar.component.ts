import { Component, inject } from '@angular/core';
import { SidebarService } from './sidebar.service';

@Component({
  imports: [],
  selector: 'app-sidebar',
  styleUrl: './sidebar.component.scss',
  templateUrl: './sidebar.component.html',
})
export class Sidebar {
  protected readonly sidebar = inject(SidebarService);
}
