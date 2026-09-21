import { Routes } from '@angular/router';

/**
 * Every feature route is lazy-loaded and owns its own routes file. Add one line per
 * feature under the shell's children.
 */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./core/layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./core/layout/start-page/start-page').then((m) => m.StartPage),
      },
      {
        path: 'preferences',
        loadChildren: () =>
          import('./features/preferences/preferences.routes').then((m) => m.routes),
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.routes),
      },
      {
        path: 'auth',
        loadChildren: () => import('./features/auth/auth.routes').then((m) => m.routes),
      },
      {
        path: '**',
        loadComponent: () => import('./shared/ui/not-found/not-found').then((m) => m.NotFound),
      },
    ],
  },
];
