import { AuthenticationResponse } from './auth.contracts';

/** The signed-in person, as the UI talks about them. */
export interface AuthenticatedUser {
  readonly id: number;
  readonly email: string;
  readonly userName: string;
  /** `null` on an account registered before names were collected (D-0016). */
  readonly firstName: string | null;
  readonly lastName: string | null;
  readonly roles: readonly string[];
}

/** A user plus the token that proves it, and when that token dies. */
export interface AuthSession {
  readonly user: AuthenticatedUser;
  readonly accessToken: string;
  /** Epoch milliseconds, so the stored form survives JSON without a Date revival step. */
  readonly expiresAt: number;
}

/** Maps the transport shape to the domain one. The only place the two meet. */
export function toSession(response: AuthenticationResponse): AuthSession {
  return {
    user: {
      id: response.user.id,
      email: response.user.email,
      userName: response.user.userName,
      // Normalised, because the API omits nulls: both arrive as `undefined` for an
      // account that has no names.
      firstName: response.user.firstName ?? null,
      lastName: response.user.lastName ?? null,
      roles: [...response.user.roles],
    },
    accessToken: response.accessToken,
    expiresAt: new Date(response.expiresAtUtc).getTime(),
  };
}

/** True when the token is past its expiry, or close enough that a call would race it. */
export function isExpired(session: AuthSession, skewMs = 5_000): boolean {
  return !Number.isFinite(session.expiresAt) || session.expiresAt - skewMs <= Date.now();
}
