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
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
