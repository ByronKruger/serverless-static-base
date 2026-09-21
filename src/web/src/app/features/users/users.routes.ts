import { Routes } from '@angular/router';
import { authenticatedGuard } from '../../core/auth/authenticated.guard';
import { UsersApi } from './data-access/users.api';
import { UsersStore } from './users.store';

/**
 * The feature's own routes, lazily loaded from `app.routes.ts`.
 *
 * There is no search route: the search box is app chrome and lives in
 * `core/user-search/` (D-0019). This feature owns the profile and nothing else, which is
 * why it registers no icons of its own - the page uses none.
 *
 * The title is static. The person's name is not known until the profile has loaded, and a
 * dynamic document title is not worth injecting `Title` for.
 */
export const routes: Routes = [
  {
    path: '',
    canActivate: [authenticatedGuard],
    providers: [UsersStore, UsersApi],
    children: [
      {
        path: ':id',
        title: 'Profile - Coffeeg',
        loadComponent: () =>
          import('./pages/profile-page/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
];
