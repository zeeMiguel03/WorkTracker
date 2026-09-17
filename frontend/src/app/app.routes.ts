import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'signin',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((module) => module.Login),
    title: 'Angular Sign In Dashboard | TailAdmin - Angular Admin Dashboard Template',
  },
  {
    path: 'signup',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register.component').then((module) => module.Register),
    title: 'Angular Sign Up Dashboard | TailAdmin - Angular Admin Dashboard Template',
  },
  { path: 'login', redirectTo: 'signin' },
  { path: 'register', redirectTo: 'signup' },
  {
    path: '',
    canActivateChild: [authGuard],
    loadComponent: () =>
      import('./core/layout/app-shell/app-shell.component').then(
        (module) => module.AppShell,
      ),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard/dashboard.component').then(
            (module) => module.Dashboard,
          ),
        title: 'Dashboard | WorkTracker',
      },
      {
        path: 'entries',
        loadComponent: () =>
          import('./features/entries/entry-list/entry-list.component').then(
            (module) => module.EntryList,
          ),
        title: 'Movimentos | WorkTracker',
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./features/products/product-list/product-list.component').then(
            (module) => module.ProductList,
          ),
        title: 'Produtos | WorkTracker',
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./features/accounts/account-list/account-list.component').then(
            (module) => module.AccountList,
          ),
        title: 'Contas | WorkTracker',
      },
      {
        path: 'sources',
        loadComponent: () =>
          import('./features/sources/source-list/source-list.component').then(
            (module) => module.SourceList,
          ),
        title: 'Fontes de trabalho | WorkTracker',
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/tasks/task-board/task-board.component').then(
            (module) => module.TaskBoard,
          ),
        title: 'Tarefas | WorkTracker',
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/profile/profile.component').then(
            (module) => module.Profile,
          ),
        title: 'Perfil | WorkTracker',
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
