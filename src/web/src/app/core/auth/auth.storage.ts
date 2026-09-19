import { Injectable } from '@angular/core';
import { AuthSession, isExpired } from './auth.session';

const STORAGE_KEY = 'coffeeg.auth.session';

/**
 * Persists the session in `localStorage` so a reload keeps the user signed in.
 *
 * Every access is guarded: storage throws outright in some contexts (private windows,
 * blocked site data), and a page that cannot remember a session must still render as a
 * signed-out one rather than fail to start.
 */
@Injectable({ providedIn: 'root' })
export class AuthStorage {
  /** The stored session, or `null` if there is none, it is unreadable, or it has expired. */
  read(): AuthSession | null {
    let raw: string | null = null;

    try {
      raw = localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }

    if (raw === null) {
      return null;
    }

    try {
      const session = JSON.parse(raw) as AuthSession;

      // A stored token that has already expired is worse than none: it would put the
      // header into a signed-in state whose every request gets a 401.
      if (!isValid(session) || isExpired(session)) {
        this.clear();
        return null;
      }

      return session;
    } catch {
      this.clear();
      return null;
    }
  }

  write(session: AuthSession): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Not fatal: the session still lives in memory for this tab.
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing to do - there is no way to clear what cannot be reached.
    }
  }
}

/** Anything could be sitting under that key; only trust a value with the right shape. */
function isValid(session: AuthSession | null): session is AuthSession {
  return (
    typeof session === 'object' &&
    session !== null &&
    typeof session.accessToken === 'string' &&
    typeof session.expiresAt === 'number' &&
    typeof session.user?.userName === 'string'
  );
}
