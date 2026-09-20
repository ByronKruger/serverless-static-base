import { Routes } from '@angular/router';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  ArrowLeftOutline,
  DeleteOutline,
  InboxOutline,
  PlusOutline,
  SaveOutline,
} from '@ant-design/icons-angular/icons';
import { authenticatedGuard } from '../../core/auth/authenticated.guard';
import { PreferencesApi } from './data-access/preferences.api';
import { PreferencesStore } from './preferences.store';

/**
 * The feature's own routes, lazily loaded from `app.routes.ts`.
 *
 * The store and its API service are provided here rather than in root, so they are
 * destroyed with the feature. The icons this feature uses are registered here too, which
 * is what keeps `core/icons` from growing into a list of everything.
 */
export const routes: Routes = [
  {
    path: '',
    canActivate: [authenticatedGuard],
    providers: [
      PreferencesStore,
      PreferencesApi,
      provideNzIcons([ArrowLeftOutline, DeleteOutline, InboxOutline, PlusOutline, SaveOutline]),
    ],
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'My preferences - Coffeeg',
        loadComponent: () =>
          import('./pages/preferences-page/preferences-page').then((m) => m.PreferencesPage),
      },
      {
        path: 'new',
        title: 'New preference - Coffeeg',
        loadComponent: () =>
          import('./pages/create-preference-page/create-preference-page').then(
            (m) => m.CreatePreferencePage,
          ),
      },
    ],
  },
];
