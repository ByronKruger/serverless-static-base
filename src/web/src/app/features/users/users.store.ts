import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { UsersApi } from './data-access/users.api';
import { PersonProfile, toPersonProfile } from './data-access/users.models';

/**
 * State for the users feature. Provided on the feature's route, so it dies with the
 * feature rather than living for the life of the app - unlike the header search, which
 * genuinely is app-wide and lives in `core/user-search/`.
 */
@Injectable()
export class UsersStore {
  private readonly api = inject(UsersApi);

  private readonly profileState = signal<PersonProfile | null>(null);

  readonly profile = this.profileState.asReadonly();

  readonly hasPreferences = computed(() => (this.profileState()?.preferences.length ?? 0) > 0);

  /** How many they have saved, for the page header. */
  readonly preferenceCount = computed(() => this.profileState()?.preferences.length ?? 0);

  loadProfile(userId: number): Observable<PersonProfile> {
    return this.api.getProfile(userId).pipe(
      map(toPersonProfile),
      tap((profile) => this.profileState.set(profile)),
    );
  }
}
