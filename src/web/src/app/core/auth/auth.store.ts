import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { AuthApi } from './auth.api';
import { LoginRequest, RegisterRequest } from './auth.contracts';
import { AuthSession, AuthenticatedUser, toSession } from './auth.session';
import { AuthStorage } from './auth.storage';

/**
 * Who is signed in. A true application-wide singleton - the navigation header and the
 * auth interceptor both read it - so it lives in `core/` and is `providedIn: 'root'`.
 *
 * The session is restored from `localStorage` at construction, which is what keeps a
 * reload from signing the user out.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly storage = inject(AuthStorage);

  private readonly sessionState = signal<AuthSession | null>(this.storage.read());

  readonly session = this.sessionState.asReadonly();
  readonly user = computed<AuthenticatedUser | null>(() => this.sessionState()?.user ?? null);
  readonly userName = computed<string | null>(() => this.user()?.userName ?? null);
  readonly isAuthenticated = computed(() => this.sessionState() !== null);
  readonly roles = computed<readonly string[]>(() => this.user()?.roles ?? []);

  /** The bearer token, or `null` when signed out. Read by the auth interceptor. */
  accessToken(): string | null {
    return this.sessionState()?.accessToken ?? null;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  /** Registers an account and signs in with it. */
  register(request: RegisterRequest): Observable<AuthenticatedUser> {
    return this.api.register(request).pipe(
      map(toSession),
      tap((session) => this.startSession(session)),
      map((session) => session.user),
    );
  }

  /** Signs in an existing account. */
  login(request: LoginRequest): Observable<AuthenticatedUser> {
    return this.api.login(request).pipe(
      map(toSession),
      tap((session) => this.startSession(session)),
      map((session) => session.user),
    );
  }

  /**
   * Signs out locally. The token is self-contained and stays technically valid until it
   * expires; revoking it server-side needs a denylist, which this slice does not have.
   */
  signOut(): void {
    this.sessionState.set(null);
    this.storage.clear();
  }

  private startSession(session: AuthSession): void {
    this.sessionState.set(session);
    this.storage.write(session);
  }
}
