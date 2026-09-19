/**
 * Transport shapes for the auth endpoints. These stop here: the rest of the app speaks
 * `AuthenticatedUser` and `AuthSession`, not whatever the API happens to send.
 */
export interface RegisterRequest {
  readonly email: string;
  readonly userName: string;
  readonly password: string;
}

export interface LoginRequest {
  /** Email is the sign-in credential; the username is only a display name (D-0009). */
  readonly email: string;
  readonly password: string;
}

export interface AuthenticationResponse {
  readonly accessToken: string;
  readonly expiresAtUtc: string;
  readonly user: {
    readonly id: number;
    readonly email: string;
    readonly userName: string;
    readonly roles: readonly string[];
  };
}
