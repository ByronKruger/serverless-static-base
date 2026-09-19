import { Routes } from '@angular/router';

/**
 * Sign-in and registration are separate pages, each lazily loaded.
 */
export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in - Coffeeg',
    loadComponent: () => import('./pages/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create account - Coffeeg',
    loadComponent: () => import('./pages/register-page/register-page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },
];
